#!/usr/bin/env python3
"""Prepare or verify one release containing Ballance, Reballance and their Pages site."""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tarfile
import tempfile
from release_identity import (RELEASE_PLATFORMS, asset_name, checksum, read_manifest,
                              site_asset_name, verify_checksum)


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


site_tools = load("prepare-site")
pages = load("prepare-pages")


def player_assets(manifest):
    for edition, pin in manifest["editions"].items():
        for platform in RELEASE_PLATFORMS:
            yield edition, asset_name(pin["tag"], platform), pin["assets"][platform]


def release_files(manifest):
    names = [name for _, name, _ in player_assets(manifest)] + [site_asset_name(manifest["release"])]
    return names + [name + ".sha256" for name in names] + ["releases.json", "RELEASE-NOTES.md", "LICENSE", "CREDITS.md"]


def notes(manifest):
    lines = [f'# Ballance + Reballance — {manifest["release"]}', '',
             'One distribution, two independently versioned games. Settings and saves remain separate.', '',
             '| Game | Version | Source |', '| --- | --- | --- |']
    for edition, pin in manifest["editions"].items():
        product = "Ballance" if edition == "original" else "Reballance"
        lines.append(f'| {product} | {pin["tag"]} | [Source](https://github.com/iwonz/ballance/tree/{pin["tag"]}) |')
    lines += ['', 'Each game includes Web, Windows x86-64, Linux x86-64, macOS ARM64 and Android ARM64 downloads.',
              'Every player archive and the complete site archive have a SHA-256 sidecar.',
              f'The site archive is `{site_asset_name(manifest["release"])}`; serve it over HTTP to play either game.',
              '[Play online](https://iwonz.github.io/ballance/) · [Project guide](https://github.com/iwonz/ballance#readme)',
              '', 'Credits: this project is a fork of [imengyu/Ballance](https://github.com/imengyu/Ballance).',
              'Ballance belongs to Cyparade. See the attached CREDITS.md and LICENSE for the preserved notice,',
              'GPL-covered code and separately licensed game data and components.', '']
    return '\n'.join(lines)


def prepare(download, destination, site, manifest_path):
    manifest = read_manifest(manifest_path)
    if destination.exists():
        raise RuntimeError("The release destination must not exist")
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="ballance-release-", dir=destination.parent) as temporary:
        staging = Path(temporary) / "assets"
        staging.mkdir()
        for edition, name, digest in player_assets(manifest):
            source = download / edition / name
            verify_checksum(source, digest)
            for suffix in ("", ".sha256"):
                shutil.copyfile(str(source) + suffix, staging / (name + suffix))
        public = Path(temporary) / "public"
        site_tools.prepare(download, public, site, manifest_path)
        archive = staging / site_asset_name(manifest["release"])
        with tarfile.open(archive, "w:gz") as bundle:
            for path in sorted(public.iterdir()):
                bundle.add(path, arcname=path.name)
        Path(str(archive) + ".sha256").write_text(f"{checksum(archive)}  {archive.name}\n")
        (staging / "releases.json").write_text(json.dumps(manifest, indent=2) + "\n")
        (staging / "RELEASE-NOTES.md").write_text(notes(manifest))
        for name in ("LICENSE", "CREDITS.md"):
            shutil.copyfile(site.parent / name, staging / name)
        os.replace(staging, destination)


def verify(download, destination, site, manifest_path):
    manifest = read_manifest(manifest_path)
    if destination.exists():
        raise RuntimeError("The Pages destination must not exist")
    for name in release_files(manifest):
        if not (download / name).is_file() or (download / name).is_symlink():
            raise RuntimeError("Missing or unsafe unified release asset: " + name)
    if read_manifest(download / "releases.json") != manifest:
        raise RuntimeError("The attached manifest differs from the pinned distribution")
    for name in ("LICENSE", "CREDITS.md"):
        if (download / name).read_bytes() != (site.parent / name).read_bytes():
            raise RuntimeError("Release notice differs from the release source: " + name)
    destination.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="ballance-verify-", dir=destination.parent) as temporary:
        root = Path(temporary)
        for edition, name, digest in player_assets(manifest):
            verify_checksum(download / name, digest)
            if name.endswith("-WebGL.tar.gz"):
                folder = root / "players" / edition
                folder.mkdir(parents=True)
                for suffix in ("", ".sha256"):
                    shutil.copyfile(download / (name + suffix), folder / (name + suffix))
        expected = root / "expected"
        site_tools.prepare(root / "players", expected, site, manifest_path)
        archive = download / site_asset_name(manifest["release"])
        verify_checksum(archive)
        actual = root / "public"
        actual.mkdir()
        pages.extract(archive, actual)
        expected_files = {path.relative_to(expected) for path in expected.rglob("*") if path.is_file()}
        actual_files = {path.relative_to(actual) for path in actual.rglob("*") if path.is_file()}
        if expected_files != actual_files or any(checksum(actual / path) != checksum(expected / path) for path in expected_files):
            raise RuntimeError("The attached site differs from the pinned players or release source")
        os.replace(actual, destination)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("operation", choices=("prepare", "verify"))
    parser.add_argument("download", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--site", type=Path, default=Path("site"))
    parser.add_argument("--manifest", type=Path, default=Path("site/releases.json"))
    parser.add_argument("--fetch", action="store_true", help="Download every asset of the pinned unified release before verification")
    args = parser.parse_args()
    manifest = read_manifest(args.manifest)
    if args.fetch:
        if args.operation != "verify":
            parser.error("--fetch is only supported by verify")
        args.download.mkdir(parents=True, exist_ok=False)
        command = ["gh", "release", "download", manifest["release"], "--dir", str(args.download)]
        for name in release_files(manifest):
            command += ["--pattern", name]
        subprocess.run(command, check=True)
    (prepare if args.operation == "prepare" else verify)(args.download, args.destination, args.site, args.manifest)
    print(f'{args.operation}: verified unified distribution {manifest["release"]}')


if __name__ == "__main__":
    main()
