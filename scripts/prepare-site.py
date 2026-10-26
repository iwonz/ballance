#!/usr/bin/env python3
"""Assemble an atomic Pages site from a unified release's pinned Web players."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
from release_identity import asset_name, read_manifest


SITE_LIMIT = 1_000_000_000


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


def player_navigation(html, edition):
    prefix = "../" if edition == "remaster" else ""
    if edition == "original":
        marker = '<div id="unity-footer">'
        navigation = ('<nav id="edition-navigation" aria-label="Games">'
                      '<a href="./" data-edition-home>About the games</a>'
                      '<a href="remaster/" data-edition-switch>Play Reballance</a></nav>')
        replacement = marker + navigation
    else:
        marker = '<button id="fullscreen"'
        navigation = '<a href="../original.html" data-edition-switch>Play Ballance</a>'
        replacement = navigation + marker
    if html.count(marker) != 1 or html.count('</head>') != 1:
        raise RuntimeError("Unrecognized player navigation host: " + edition)
    html = html.replace(marker, replacement, 1)
    return html.replace('</head>', f'<link rel="stylesheet" href="{prefix}assets/player-navigation.css">'
                        f'<script src="{prefix}assets/player-navigation.js" defer></script></head>', 1)


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
        for name in ("player-navigation.js", "player-navigation.css"):
            if not (staging / "assets" / name).is_file():
                raise RuntimeError("Missing player navigation asset: " + name)
        for name in ("LICENSE", "CREDITS.md"):
            shutil.copyfile(site.parent / name, staging / name)
        (staging / "releases.json").write_text(json.dumps(manifest, indent=2) + "\n")
        identities = {}
        for edition, pin in manifest["editions"].items():
            output = staging / edition
            output.mkdir()
            identities[edition] = pages.prepare(download / edition, output, pin["tag"], pin["assets"]["WebGL"])
            host = output / "index.html"
            host.write_text(player_navigation(host.read_text(), edition))
        original = staging / "original/index.html"
        (staging / "original.html").write_text(original_host(original.read_text()))
        original.unlink()
        (staging / ".nojekyll").touch()
        (staging / "release.json").write_text(json.dumps({"schema": 2, "release": manifest["release"], "editions": identities}, indent=2) + "\n")
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
    parser.add_argument("--fetch", action="store_true", help="Download both Web players from the pinned unified release with gh")
    parser.add_argument("--release-tag", help="Require the triggering unified release to match the manifest")
    args = parser.parse_args()
    manifest = read_manifest(args.manifest)
    if args.release_tag and args.release_tag != manifest["release"]:
        raise RuntimeError("The published release is not pinned in main; update the manifest before publication")
    if args.fetch:
        for edition, pin in manifest["editions"].items():
            directory = args.download / edition
            directory.mkdir(parents=True, exist_ok=False)
            subprocess.run(["gh", "release", "download", manifest["release"], "--pattern", asset_name(pin["tag"]),
                            "--pattern", asset_name(pin["tag"]) + ".sha256", "--dir", str(directory)], check=True)
    prepare(args.download, args.destination, args.site, args.manifest)


if __name__ == "__main__":
    main()
