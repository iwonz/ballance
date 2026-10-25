#!/usr/bin/env python3
"""Package a local Unity build as a GitHub Release asset."""
import argparse
import hashlib
from pathlib import Path
import re
import plistlib
import shutil
import subprocess
import sys
import tarfile
import zipfile
from release_identity import asset_name, checkout_edition, validate_build_identity

ROOT = Path(__file__).resolve().parent.parent
PLATFORMS = ("macOS", "Windows", "Linux", "Android", "iOS", "WebGL")


def validate_web(folder):
    if not (folder / "index.html").is_file() or not (folder / ".nojekyll").is_file():
        raise RuntimeError("Not a Ballance Pages build: missing index.html or .nojekyll")
    files = list((folder / "Build").glob("*"))
    for suffix in (".loader.js", ".wasm.unityweb", ".data.unityweb", ".framework.js.unityweb"):
        if not any(path.name.endswith(suffix) and path.stat().st_size > 0 for path in files):
            raise RuntimeError(f"Incomplete Web build: missing {suffix}")
    for number in range(1, 14):
        path = folder / "StreamingAssets/BuiltInPackages/Levels" / f"level{number:02}.ballance"
        if not path.is_file() or path.stat().st_size == 0:
            raise RuntimeError(f"Missing Web level: {path}")
    for name in ("core", "core.assets.skys", "core.sounds", "core.sounds.music",
                 "mod.invlife", "mod.selfkill", "mod.speedball"):
        path = folder / "StreamingAssets/BuiltInPackages" / ("Core" if name == "core" else "Packages") / (name + ".ballance")
        with zipfile.ZipFile(path) as bundle:
            if bundle.testzip() is not None or f"assets/{name}.assetbundle" not in bundle.namelist():
                raise RuntimeError(f"Incomplete Web package: {path}")
    if any(path.is_symlink() for path in folder.rglob("*")):
        raise RuntimeError("GitHub Pages artifacts must not contain symlinks")
    if sum(path.stat().st_size for path in folder.rglob("*") if path.is_file()) > 1_000_000_000:
        raise RuntimeError("Web build exceeds the GitHub Pages 1 GB site limit")


def package(platform):
    settings = (ROOT / "ProjectSettings/ProjectSettings.asset").read_text()
    version = re.search(r"^  bundleVersion: ([a-zA-Z0-9._-]+)$", settings, re.MULTILINE)
    if not version:
        raise RuntimeError("Invalid bundleVersion for a release filename")
    folder = ROOT / "Builds" / platform
    edition = checkout_edition(ROOT)
    product = edition["productName"] if edition else "Ballance"
    mac_app = product + ("-dev" if "-dev." in version[1] else "") + ".app"
    required = {"macOS": mac_app + "/Contents/Info.plist", "Windows": product + ".exe",
                "Linux": product + ".x86_64", "Android": product + ".apk",
                "iOS": "Unity-iPhone.xcodeproj/project.pbxproj", "WebGL": "index.html"}
    if not (folder / required[platform]).is_file():
        raise RuntimeError(f"Build {platform} first with scripts/build.sh")
    tag = validate_build_identity(ROOT, folder, version[1])
    archive = ROOT / "Builds/releases" / asset_name(tag, platform)
    if archive.exists() or Path(str(archive) + ".sha256").exists():
        raise RuntimeError(f"Move previous release files aside first: {archive}")
    archive.parent.mkdir(parents=True, exist_ok=True)
    if platform == "macOS":
        info_path = folder / mac_app / "Contents/Info.plist"
        info = plistlib.loads(info_path.read_bytes())
        info["CFBundleDisplayName"] = Path(mac_app).stem
        info_path.write_bytes(plistlib.dumps(info))
        subprocess.run([sys.executable, str(ROOT / "scripts/package-macos.py"),
                        str(folder / mac_app), str(archive)], check=True)
        return archive
    if platform == "WebGL":
        validate_web(folder)
    if platform == "Android":
        shutil.copyfile(folder / (product + ".apk"), archive)
    elif platform == "Windows":
        with zipfile.ZipFile(archive, "w", zipfile.ZIP_DEFLATED) as output:
            for path in sorted(folder.rglob("*")):
                if path.is_file():
                    output.write(path, path.relative_to(folder))
    else:
        with tarfile.open(archive, "w:gz") as output:
            for path in sorted(folder.iterdir()):
                output.add(path, arcname=path.name)
    with archive.open("rb") as stream:
        digest = hashlib.file_digest(stream, "sha256").hexdigest()
    Path(str(archive) + ".sha256").write_text(f"{digest}  {archive.name}\n")
    return archive


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("platform", choices=PLATFORMS)
    print(package(parser.parse_args().platform))
