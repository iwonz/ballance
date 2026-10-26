"""Release identities shared by packaging and Pages; legacy tags remain Original."""
import json
import hashlib
from pathlib import Path
import re


VERSION = r"[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?"
EDITIONS = {
    "original": ("Ballance", "com.MengyuGame.Ballance"),
    "remaster": ("Reballance", "com.iwonz.BallanceRemaster"),
}
RELEASE_PLATFORMS = ("WebGL", "Windows", "Linux", "macOS", "Android")


def distribution_version(tag):
    match = re.fullmatch(rf"release/v({VERSION})", tag) if isinstance(tag, str) else None
    if not match:
        raise RuntimeError("Pin a verified unified release using release/v<version>")
    return match[1]


def site_asset_name(tag):
    return f"Ballance-Reballance-{distribution_version(tag)}-site.tar.gz"


def read_manifest(path):
    manifest = json.loads(path.read_text())
    if not isinstance(manifest, dict) or set(manifest) != {"schema", "release", "editions"} or manifest["schema"] != 2:
        raise RuntimeError("The unified release requires manifest schema 2")
    distribution_version(manifest["release"])
    if not isinstance(manifest["editions"], dict) or set(manifest["editions"]) != set(EDITIONS):
        raise RuntimeError("The release requires both edition pins")
    for edition, pin in manifest["editions"].items():
        if not isinstance(pin, dict) or set(pin) != {"tag", "assets"}:
            raise RuntimeError(f"{edition} is not ready: pin a verified source tag and five platform checksums")
        if not isinstance(pin["tag"], str) or parse_tag(pin["tag"])[0] != edition:
            raise RuntimeError(f"Invalid {edition} source tag")
        if not isinstance(pin["assets"], dict) or set(pin["assets"]) != set(RELEASE_PLATFORMS):
            raise RuntimeError(f"{edition} requires all five platform checksums")
        if any(not isinstance(value, str) or not re.fullmatch(r"[0-9a-f]{64}", value) for value in pin["assets"].values()):
            raise RuntimeError(f"Invalid {edition} artifact checksum")
    return manifest


def checksum(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def verify_checksum(path, pinned=None):
    if path.is_symlink() or Path(str(path) + ".sha256").is_symlink():
        raise RuntimeError("Release inputs must not be symlinks")
    fields = Path(str(path) + ".sha256").read_text().split()
    if len(fields) not in (1, 2) or (len(fields) == 2 and fields[1] != path.name):
        raise RuntimeError("Invalid checksum sidecar: " + path.name)
    actual = checksum(path)
    if actual != fields[0]:
        raise RuntimeError("Archive checksum mismatch: " + path.name)
    if pinned is not None and actual != pinned:
        raise RuntimeError("Archive checksum differs from the pinned release: " + path.name)
    return actual


def parse_tag(tag):
    match = re.fullmatch(rf"(?:(original|remaster)/)?v({VERSION})", tag)
    if not match:
        raise RuntimeError("Invalid release tag; use original/v1.0.12 or remaster/v1.0.0")
    return match[1] or "original", match[2], match[1] is None


def asset_name(tag, platform="WebGL"):
    edition, version, legacy = parse_tag(tag)
    prefix = "Ballance" if edition == "original" else "Reballance"
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
