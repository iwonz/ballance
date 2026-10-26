#!/usr/bin/env python3
"""Verify the public identity of both pinned editions after a Pages deployment."""
import argparse
import json
from pathlib import Path
import time
from urllib.request import urlopen
from urllib.error import URLError
from urllib.parse import quote
from release_identity import parse_tag, read_manifest


def verify(base, manifest):
    def get(path):
        with urlopen(base.rstrip("/") + "/" + path + "?verify=" + str(time.time_ns()), timeout=5) as response:
            body = response.read(1_000_001)
            if len(body) > 1_000_000:
                raise RuntimeError("Unexpectedly large public identity or host page")
            return body.decode("utf-8")
    aggregate = json.loads(get("release.json"))
    if aggregate.get("schema") != 2 or aggregate.get("release") != manifest["release"]:
        raise RuntimeError("Pages is not serving the two-edition site")
    for edition, pin in manifest["editions"].items():
        identity = json.loads(get(quote(edition) + "/release.json"))
        if identity.get("tag") != pin["tag"] or identity.get("sha256") != pin["assets"]["WebGL"]:
            raise RuntimeError("Pages serves the wrong " + edition + " release")
        if aggregate.get("editions", {}).get(edition) != identity:
            raise RuntimeError("Public identities do not belong to one atomic deployment")
        host = get("original.html" if edition == "original" else "remaster/")
        version = parse_tag(pin["tag"])[1]
        if f'productVersion: "{version}"' not in host:
            raise RuntimeError("Public player host has the wrong " + edition + " version")
        target = 'href="remaster/"' if edition == "original" else 'href="../original.html"'
        if target not in host or "data-edition-switch" not in host:
            raise RuntimeError("Missing public edition switch: " + edition)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("url")
    parser.add_argument("manifest", type=Path)
    args = parser.parse_args()
    manifest = read_manifest(args.manifest)
    for attempt in range(30):
        try:
            verify(args.url, manifest)
            print("Verified both pinned player identities at " + args.url)
            break
        except (OSError, URLError, ValueError, RuntimeError) as error:
            if attempt == 29:
                raise
            print(f"Waiting for Pages ({attempt + 1}/30): {error}", flush=True)
            time.sleep(5)
