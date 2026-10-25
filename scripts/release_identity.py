"""Release identities shared by packaging and Pages; legacy tags remain Original."""
import json
from pathlib import Path
import re


VERSION = r"[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?"
EDITIONS = {
    "original": ("Ballance", "com.MengyuGame.Ballance"),
    "remaster": ("Ballance Remaster", "com.iwonz.BallanceRemaster"),
}


def parse_tag(tag):
    match = re.fullmatch(rf"(?:(original|remaster)/)?v({VERSION})", tag)
    if not match:
        raise RuntimeError("Invalid release tag; use original/v1.0.12 or remaster/v1.0.0")
    return match[1] or "original", match[2], match[1] is None


def asset_name(tag, platform="WebGL"):
    edition, version, legacy = parse_tag(tag)
    prefix = "Ballance" if legacy else "Ballance-" + edition.title()
    extension = "zip" if platform in ("macOS", "Windows") else "apk" if platform == "Android" else "tar.gz"
    return f"{prefix}-{version}-{platform}.{extension}"


def checkout_edition(root):
    path = Path(root) / "ProjectSettings/Edition.json"
    if not path.exists():
        return None
    data = json.loads(path.read_text())
    edition = data.get("id")
    if edition not in EDITIONS or (data.get("productName"), data.get("applicationIdentifier")) != EDITIONS[edition]:
        raise RuntimeError("Invalid checkout edition identity")
    return data


def validate_build_identity(root, folder, version):
    edition = checkout_edition(root)
    if edition is None:
        return "v" + version
    path = folder / "build-identity.json"
    if not path.is_file():
        raise RuntimeError("Missing build identity; rebuild this edition before packaging")
    built = json.loads(path.read_text())
    expected = {"edition": edition["id"], "version": version,
                "productName": edition["productName"],
                "applicationIdentifier": edition["applicationIdentifier"]}
    if built != expected:
        raise RuntimeError("Build identity does not match this checkout; do not relabel an older player")
    return f"{edition['id']}/v{version}"
