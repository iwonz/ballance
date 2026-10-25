#!/usr/bin/env python3
"""Validate and extract one Web release for the GitHub Pages workflow (Python 3.12+)."""
import hashlib
import importlib.util
from pathlib import Path
import sys
import tarfile


def prepare(download, destination):
    archives = list(download.glob("Ballance-*-WebGL.tar.gz"))
    if len(archives) != 1:
        raise RuntimeError("Expected exactly one Web release archive")
    archive = archives[0]
    expected = Path(str(archive) + ".sha256").read_text().split()[0]
    with archive.open("rb") as stream:
        actual = hashlib.file_digest(stream, "sha256").hexdigest()
    if actual != expected:
        raise RuntimeError("Web archive checksum mismatch")
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


if __name__ == "__main__":
    prepare(Path(sys.argv[1]), Path(sys.argv[2]))
