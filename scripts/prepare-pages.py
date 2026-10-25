#!/usr/bin/env python3
"""Validate and extract one Web release for the GitHub Pages workflow (Python 3.12+)."""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import tarfile
from release_identity import EDITIONS, asset_name, parse_tag


def prepare(download, destination, tag, pinned_sha256=None):
    edition, version, legacy = parse_tag(tag)
    archives = list(download.glob("Ballance-*-WebGL.tar.gz"))
    if len(archives) != 1:
        raise RuntimeError("Expected exactly one Web release archive")
    archive = archives[0]
    if archive.name != asset_name(tag):
        raise RuntimeError("Web archive version does not match the release tag")
    expected = Path(str(archive) + ".sha256").read_text().split()[0]
    with archive.open("rb") as stream:
        actual = hashlib.file_digest(stream, "sha256").hexdigest()
    if actual != expected:
        raise RuntimeError("Web archive checksum mismatch")
    if pinned_sha256 is not None and actual != pinned_sha256:
        raise RuntimeError("Web archive checksum differs from the pinned release")
    if any(destination.iterdir()):
        raise RuntimeError("The Pages staging directory must be empty")
    with tarfile.open(archive) as bundle:
        members = bundle.getmembers()
        if any(not member.isfile() and not member.isdir() for member in members):
            raise RuntimeError("Pages archive contains links or special files")
        if sum(member.size for member in members) > 1_000_000_000:
            raise RuntimeError("Pages archive exceeds the 1 GB site limit")
        bundle.extractall(destination, filter="data")
    spec = importlib.util.spec_from_file_location("package_build", Path(__file__).with_name("package-build.py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    module.validate_web(destination)
    versions = re.findall(r'productVersion:\s*"([^"]+)"', (destination / "index.html").read_text())
    if versions != [version]:
        raise RuntimeError("Embedded Web player version does not match the release tag")
    if not legacy:
        identity = json.loads((destination / "build-identity.json").read_text())
        product, application_id = EDITIONS[edition]
        products = re.findall(r'productName:\s*"([^"\n]+)"', (destination / "index.html").read_text())
        expected_identity = {"edition": edition, "version": version, "productName": product,
                             "applicationIdentifier": application_id}
        if identity != expected_identity or products != [product]:
            raise RuntimeError("Embedded Web player edition does not match the release tag")
    identity = {
        "tag": tag, "version": version, "archive": archive.name, "sha256": actual,
    }
    if not legacy:
        identity["edition"] = edition
    (destination / "release.json").write_text(json.dumps(identity, indent=2) + "\n")
    return identity


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("download", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("tag", help="release tag matching the Web player, for example v1.0.10")
    args = parser.parse_args()
    prepare(args.download, args.destination, args.tag)
