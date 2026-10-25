#!/usr/bin/env python3
"""Assemble an atomic Pages site from two explicitly pinned Web releases."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
from release_identity import asset_name, parse_tag


SITE_LIMIT = 1_000_000_000


def read_manifest(path):
    manifest = json.loads(path.read_text())
    if manifest.get("schema") != 1 or set(manifest.get("editions", {})) != {"original", "remaster"}:
        raise RuntimeError("The site requires both edition pins and schema 1")
    for edition, pin in manifest["editions"].items():
        if not isinstance(pin, dict) or set(pin) != {"tag", "sha256"}:
            raise RuntimeError(f"{edition} is not ready: pin a verified release and checksum")
        if parse_tag(pin["tag"])[0] != edition or not re.fullmatch(r"[0-9a-f]{64}", pin["sha256"]):
            raise RuntimeError(f"Invalid {edition} release pin")
    return manifest


def original_host(html):
    # Keep the page directory stable: Unity derives its Web save identity from it.
    html, builds = re.subn(r'var buildUrl = "Build";', 'var buildUrl = "original/Build";', html)
    html, streaming = re.subn(r'streamingAssetsUrl: "StreamingAssets"',
                             'streamingAssetsUrl: "original/StreamingAssets"', html)
    if builds != 1 or streaming != 1 or "<base " in html.lower():
        raise RuntimeError("Unrecognized Original Web template; audit resource paths before relocation")
    html = html.replace('href="TemplateData/', 'href="original/TemplateData/')
    html = html.replace('src="TemplateData/', 'src="original/TemplateData/')
    return html


def prepare(download, destination, site, manifest_path, limit=SITE_LIMIT):
    manifest = read_manifest(manifest_path)
    if destination.exists() and any(destination.iterdir()):
        raise RuntimeError("The site destination must be empty")
    spec = importlib.util.spec_from_file_location("prepare_pages", Path(__file__).with_name("prepare-pages.py"))
    pages = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(pages)
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="ballance-site-", dir=destination.parent) as temporary:
        staging = Path(temporary) / "public"
        staging.mkdir()
        for source in site.rglob("*"):
            relative = source.relative_to(site)
            if source.is_symlink() or relative.parts[0] in {"original", "remaster", "original.html", "release.json"}:
                raise RuntimeError("Unsafe or reserved site source: " + str(relative))
            if source.is_file():
                target = staging / relative
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(source, target)
        if not (staging / "index.html").is_file():
            raise RuntimeError("Missing landing page")
        identities = {}
        for edition, pin in manifest["editions"].items():
            output = staging / edition
            output.mkdir()
            identities[edition] = pages.prepare(download / edition, output, pin["tag"], pin["sha256"])
        original = staging / "original/index.html"
        (staging / "original.html").write_text(original_host(original.read_text()))
        original.unlink()
        (staging / ".nojekyll").touch()
        (staging / "release.json").write_text(json.dumps({"schema": 1, "editions": identities}, indent=2) + "\n")
        size = sum(path.stat().st_size for path in staging.rglob("*") if path.is_file())
        if size > limit:
            raise RuntimeError(f"Combined Pages site exceeds {limit} bytes: {size}")
        os.replace(staging, destination)
    return identities


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("download", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--site", type=Path, default=Path("site"))
    parser.add_argument("--manifest", type=Path, default=Path("site/releases.json"))
    parser.add_argument("--fetch", action="store_true", help="Download both pinned releases with gh")
    parser.add_argument("--release-tag", help="Require the triggering release to appear in the manifest")
    args = parser.parse_args()
    manifest = read_manifest(args.manifest)
    if args.release_tag and args.release_tag not in [pin["tag"] for pin in manifest["editions"].values()]:
        raise RuntimeError("The published release is not pinned in main; update the manifest before publication")
    if args.fetch:
        for edition, pin in manifest["editions"].items():
            directory = args.download / edition
            directory.mkdir(parents=True, exist_ok=False)
            subprocess.run(["gh", "release", "download", pin["tag"], "--pattern", asset_name(pin["tag"]),
                            "--pattern", asset_name(pin["tag"]) + ".sha256", "--dir", str(directory)], check=True)
    prepare(args.download, args.destination, args.site, args.manifest)


if __name__ == "__main__":
    main()
