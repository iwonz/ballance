#!/usr/bin/env python3
"""Replace a canonical macOS installation and remove obsolete build copies."""
import argparse
import hashlib
import os
from pathlib import Path
import plistlib
import re
import shutil
import subprocess
import sys
import uuid
from xml.parsers.expat import ExpatError

REPO = Path(__file__).resolve().parent.parent
EDITIONS = {
    "original": ("com.MengyuGame.Ballance", "Ballance.app"),
    "remaster": ("com.iwonz.BallanceRemaster", "Reballance.app"),
}
APPLICATIONS = Path("/Applications")
LSREGISTER = Path("/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister")


def metadata(app):
    if app.is_symlink() or not app.is_dir():
        raise ValueError(f"Not a regular application bundle: {app}")
    try:
        with (app / "Contents/Info.plist").open("rb") as stream:
            info = plistlib.load(stream)
    except (plistlib.InvalidFileException, ExpatError) as error:
        raise ValueError(f"Invalid bundle metadata: {app}") from error
    executable = info.get("CFBundleExecutable", "")
    if not executable or Path(executable).name != executable:
        raise ValueError(f"Invalid bundle executable: {app}")
    if not (app / "Contents/MacOS" / executable).is_file():
        raise ValueError(f"Missing bundle executable: {app}")
    return info


def version(value):
    match = re.fullmatch(r"(\d+)\.(\d+)\.(\d+)(?:-dev\.(\d+))?", value)
    if not match:
        raise ValueError(f"Unsupported edition version: {value}")
    major, minor, patch, development = match.groups()
    return int(major), int(minor), int(patch), development is None, int(development or 0)


def fingerprint(app):
    digest = hashlib.sha256()
    for path in sorted(app.rglob("*")):
        relative = str(path.relative_to(app)).encode()
        if path.is_symlink():
            if not path.resolve().is_relative_to(app.resolve()):
                raise ValueError(f"External bundle symlink: {path}")
            digest.update(b"L" + relative + b"\0" + os.readlink(path).encode() + b"\0")
        elif path.is_file():
            digest.update(b"F" + relative + b"\0")
            with path.open("rb") as stream:
                while chunk := stream.read(1024 * 1024):
                    digest.update(chunk)
    return digest.hexdigest()


def verify_signature(app):
    subprocess.run(["codesign", "--verify", "--deep", "--strict", str(app)], check=True)


def unregister(app):
    result = subprocess.run([str(LSREGISTER), "-u", str(app)], capture_output=True, text=True)
    if result.returncode and "-10814" not in result.stdout + result.stderr:
        raise subprocess.CalledProcessError(result.returncode, result.args, result.stdout, result.stderr)


def remove_build_copy(app, edition, installed_version):
    if metadata(app).get("CFBundleIdentifier") != EDITIONS[edition][0]:
        raise ValueError(f"Application identity changed during cleanup: {app}")
    if version(metadata(app).get("CFBundleShortVersionString", "")) > version(installed_version):
        return
    unregister(app.resolve())
    if app != app.resolve():
        unregister(app)
    shutil.rmtree(app)


def application_name(edition, release_version):
    version(release_version)
    name = EDITIONS[edition][1]
    return name[:-4] + "-dev.app" if "-dev." in release_version else name


def registered_paths(dump, edition):
    result = []
    for block in re.split(r"\n-+\n", dump):
        identifier = re.search(r"^identifier:\s+(\S+)\s*$", block, re.MULTILINE)
        if not identifier or identifier[1] != EDITIONS[edition][0]:
            continue
        path = re.search(r"^path:\s+(.+?)\s+\(0x[0-9a-f]+\)\s*$", block, re.MULTILINE)
        if not path or not Path(path[1]).is_absolute():
            raise ValueError("Cannot safely parse a matching LaunchServices registration")
        result.append(Path(path[1]))
    return result


def bundles(roots, include_retired=False):
    for root in roots:
        if not root.is_dir():
            continue
        for parent, directories, _ in os.walk(root, followlinks=False):
            for name in list(directories):
                path = Path(parent) / name
                retired = bool(re.search(r"\.app(?:\.disabled(?:-\d+)?|-backup)$", name))
                if name.endswith(".app") or retired:
                    directories.remove(name)
                    if not path.is_symlink() and (include_retired or not retired):
                        yield path


def duplicates(edition, target, roots, include_retired=False):
    identifier = EDITIONS[edition][0]
    result = []
    for app in bundles(roots, include_retired):
        if app.resolve() == target.resolve():
            continue
        try:
            if metadata(app).get("CFBundleIdentifier") == identifier:
                result.append(app)
        except (OSError, ValueError, plistlib.InvalidFileException):
            continue
    return sorted(set(result))


def exclude_builds_from_index(root):
    preserved = root.with_name(root.name + ".noindex")
    if root.is_symlink():
        if root.resolve() != preserved.resolve():
            raise ValueError("Builds points to an unexpected directory")
        return
    if not root.exists():
        return
    if preserved.exists():
        raise ValueError("Builds.noindex already exists; refusing to overwrite it")
    root.rename(preserved)
    try:
        root.symlink_to(preserved.name, target_is_directory=True)
    except BaseException:
        preserved.rename(root)
        raise


def install(source, edition, target):
    info = metadata(source)
    identifier = EDITIONS[edition][0]
    if info.get("CFBundleIdentifier") != identifier:
        raise ValueError("Source bundle belongs to a different edition")
    source_version = version(info.get("CFBundleShortVersionString", ""))
    if target.exists():
        previous = metadata(target)
        if previous.get("CFBundleIdentifier") != identifier:
            raise ValueError("Canonical path is occupied by a different application")
        if source_version < version(previous.get("CFBundleShortVersionString", "")):
            raise ValueError("Refusing to replace a newer installed edition")
    verify_signature(source)
    expected = fingerprint(source)
    if source.resolve() == target.resolve():
        return
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.parent / (".ballance-install-" + uuid.uuid4().hex + ".noindex")
    staging = temporary / target.name
    backup = temporary / "previous.app"
    temporary.mkdir()
    try:
        shutil.copytree(source, staging, symlinks=True)
        if fingerprint(staging) != expected:
            raise ValueError("Copied application differs from its source")
        verify_signature(staging)
        if target.exists():
            target.rename(backup)
        try:
            staging.rename(target)
        except BaseException:
            if backup.exists():
                backup.rename(target)
            raise
    finally:
        if staging.exists():
            shutil.rmtree(staging)
        if not backup.exists():
            temporary.rmdir()
    if temporary.exists():
        shutil.rmtree(temporary)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("edition", choices=EDITIONS)
    parser.add_argument("source", type=Path)
    parser.add_argument("--apply", action="store_true", help="Install and clean; otherwise show the plan")
    args = parser.parse_args()
    source = args.source.absolute()
    info = metadata(source)
    target = APPLICATIONS / application_name(args.edition, info.get("CFBundleShortVersionString", ""))
    roots = [APPLICATIONS, Path.home()/"Applications"]
    build_roots = [REPO/"Builds", REPO/".worktrees/original/Builds"]
    if info.get("CFBundleIdentifier") != EDITIONS[args.edition][0]:
        raise ValueError("Source bundle belongs to a different edition")
    obsolete = duplicates(args.edition, target, roots, include_retired=True)
    build_copies = [app for app in duplicates(args.edition, target, build_roots, include_retired=True)
                    if version(metadata(app).get("CFBundleShortVersionString", "")) <= version(info["CFBundleShortVersionString"])]
    for app in obsolete:
        if version(metadata(app).get("CFBundleShortVersionString", "")) > version(info.get("CFBundleShortVersionString", "")):
            raise ValueError(f"Refusing to remove a newer installed edition: {app}")
    print(f"Install {args.edition} {info['CFBundleShortVersionString']}: {source} -> {target}")
    for app in obsolete:
        print(f"Remove obsolete installed application: {app}")
    for app in build_copies:
        print(f"Remove obsolete build application: {app}")
    if not args.apply:
        return
    if sys.platform != "darwin":
        raise ValueError("macOS installation requires macOS")
    running = subprocess.run(["ps", "-axo", "command="], capture_output=True, text=True, check=True).stdout
    replaced = [] if source.resolve() == target.resolve() else [target]
    for app in [*replaced, *obsolete, *build_copies]:
        if any(str(path / "Contents/MacOS/") in running for path in {app, app.resolve()}):
            raise ValueError(f"Quit this player before replacing it: {app}")
    install(source, args.edition, target)
    exclude_builds_from_index(REPO / "Builds")
    # LaunchServices stores real paths; a moved directory can retain its old aliases.
    for app in build_copies:
        remove_build_copy(app, args.edition, info["CFBundleShortVersionString"])
    for app in obsolete:
        # Recheck ownership immediately before deleting only the application directory.
        if metadata(app).get("CFBundleIdentifier") != EDITIONS[args.edition][0]:
            raise ValueError(f"Application identity changed during installation: {app}")
        unregister(app)
        shutil.rmtree(app)
    catalog = subprocess.run([str(LSREGISTER), "-dump"], capture_output=True, text=True, check=True).stdout
    for app in registered_paths(catalog, args.edition):
        if app != target and (not app.exists() or "/AppTranslocation/" in str(app)):
            unregister(app)
    subprocess.run([str(LSREGISTER), "-f", str(target)], check=True)
    subprocess.run([str(LSREGISTER), "-gc"], check=True)
    print(f"Installed and registered: {target}. Saved data and release archives were not modified.")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        sys.exit(str(error))
