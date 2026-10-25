#!/usr/bin/env python3
"""Validate and ad-hoc sign a Unity ARM64 player, then create a release ZIP."""
import argparse
import hashlib
import os
from pathlib import Path
import platform
import plistlib
import subprocess
import tempfile
import zipfile


MINIMUM_MACOS = "13.1"  # LC_BUILD_VERSION of the shipped ARM64 physics dylib.
MACHO_MAGIC = {b"\xcf\xfa\xed\xfe", b"\xfe\xed\xfa\xcf", b"\xca\xfe\xba\xbe",
               b"\xbe\xba\xfe\xca", b"\xca\xfe\xba\xbf", b"\xbf\xba\xfe\xca"}


def run(*args):
    return subprocess.check_output([str(arg) for arg in args], text=True).strip()


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def validate(app):
    require(app.is_dir() and app.suffix == ".app", f"Missing application: {app}")
    contents = app / "Contents"
    with (contents / "Info.plist").open("rb") as stream:
        info = plistlib.load(stream)
    executable = contents / "MacOS" / info["CFBundleExecutable"]
    require(executable.is_file(), f"Missing executable: {executable}")
    require(run("lipo", "-archs", executable) == "arm64", "Player must be native ARM64 only")

    native_files = []
    for path in sorted(contents.rglob("*")):
        if not path.is_file() or path.is_symlink():
            continue
        with path.open("rb") as stream:
            is_native = stream.read(4) in MACHO_MAGIC
        if not is_native:
            continue
        require("arm64" in run("lipo", "-archs", path).split(), f"Intel-only native dependency: {path}")
        # Reject dependencies on the build machine (Homebrew, a developer's home, etc.).
        for line in run("otool", "-arch", "arm64", "-L", path).splitlines()[1:]:
            dependency = line.strip().split(" (", 1)[0]
            require(dependency.startswith(("@", "/usr/lib/", "/System/Library/")),
                    f"Non-portable native dependency in {path}: {dependency}")
        native_files.append(path)

    physics = [path for path in native_files if path.name == "libbphysics.dylib"]
    require(len(physics) == 1, "Expected exactly one ARM64 physics library")
    require(physics[0] == contents / "PlugIns/libbphysics.dylib",
            "The physics library must be in Contents/PlugIns for the Mono player to find it")
    require(any(path.name in ("UnityPlayer", "UnityPlayer.dylib") for path in native_files),
            "Missing UnityPlayer runtime")
    require(any(path.name == "libmonobdwgc-2.0.dylib" for path in native_files), "Missing Mono runtime")

    for folder, name in [("Core", "core"), ("Packages", "core.sounds"),
                         ("Packages", "core.sounds.music"), ("Packages", "core.assets.skys"),
                         ("Packages", "mod.invlife"), ("Packages", "mod.selfkill"),
                         ("Packages", "mod.speedball")]:
        path = contents / folder / (name + ".ballance")
        with zipfile.ZipFile(path) as package:
            require(package.testzip() is None, f"Corrupt package: {path}")
            entries = set(package.namelist())
            bundle = f"assets/{name}.assetbundle"
            require(bool({"PackageDef.xml", "/PackageDef.xml"} & entries) and bundle in entries,
                    f"Incomplete package: {path}")
            with package.open(bundle) as stream:
                require(stream.read(8) == b"UnityFS\x00", f"Invalid asset bundle: {path}")
            if name.startswith("mod."):
                require(f"csharp/{name}.dll" not in entries, f"Stale built-in mod DLL: {path}")
    managed = contents / "Resources/Data/Managed/Assembly-CSharp.dll"
    require(managed.is_file() and managed.stat().st_size > 0, "Missing game and built-in mod code")
    for number in range(1, 14):
        path = contents / "Levels" / f"level{number:02}.ballance"
        with path.open("rb") as stream:
            require(stream.read(8) == b"UnityFS\x00", f"Invalid or missing level bundle: {path}")
    return info, native_files, physics[0]


def package(app, archive):
    require(platform.system() == "Darwin" and platform.machine() == "arm64",
            "Packaging and the native smoke test require an Apple Silicon Mac")
    require(archive.suffix == ".zip", "The release archive must end in .zip")
    require(app not in archive.parents, "The archive must be outside the application")
    require(not archive.exists(), f"Archive already exists: {archive}")
    info, native_files, physics = validate(app)

    # Changing the resource files after Unity builds invalidates its original signature.
    info["LSMinimumSystemVersion"] = MINIMUM_MACOS
    with (app / "Contents/Info.plist").open("wb") as stream:
        plistlib.dump(info, stream)
    executable = app / "Contents/MacOS" / info["CFBundleExecutable"]
    executable.chmod(executable.stat().st_mode | 0o111)
    for path in native_files:
        run("codesign", "--force", "--sign", "-", "--preserve-metadata=entitlements", path)
    bundles = [path for path in app.rglob("*") if path.is_dir() and not path.is_symlink()
               and path.suffix in (".framework", ".bundle", ".app")]
    for path in sorted(bundles, key=lambda item: len(item.parts), reverse=True):
        run("codesign", "--force", "--sign", "-", "--preserve-metadata=entitlements", path)
    run("codesign", "--force", "--sign", "-", "--preserve-metadata=entitlements", app)
    run("codesign", "--verify", "--deep", "--strict", app)

    with tempfile.TemporaryDirectory(prefix="ballance-physics-") as temporary:
        smoke = Path(temporary) / "physics-smoke"
        run("xcrun", "clang++", "-arch", "arm64", "-std=c++11", "-Wall", "-Wextra", "-Werror",
            Path(__file__).with_name("physics-smoke.cpp"), "-o", smoke)
        print(run(smoke, physics))

    archive.parent.mkdir(parents=True, exist_ok=True)
    run("ditto", "-c", "-k", "--sequesterRsrc", "--keepParent", app, archive)
    # Test the distributed copy as well: ZIP creation must preserve links and executable bits.
    with tempfile.TemporaryDirectory(prefix="ballance-archive-") as temporary:
        run("ditto", "-x", "-k", archive, temporary)
        extracted = Path(temporary) / app.name
        validate(extracted)
        require(os.access(extracted / "Contents/MacOS" / info["CFBundleExecutable"], os.X_OK),
                "The ZIP lost the player's executable permission")
        run("codesign", "--verify", "--deep", "--strict", extracted)
    digest = hashlib.sha256()
    with archive.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    archive.with_suffix(".zip.sha256").write_text(f"{digest.hexdigest()}  {archive.name}\n")
    print(f"Release archive verified: {archive}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("app", type=Path)
    parser.add_argument("archive", type=Path)
    arguments = parser.parse_args()
    package(arguments.app.resolve(), arguments.archive.resolve())
