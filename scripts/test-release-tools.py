#!/usr/bin/env python3
"""Exercise Pages packaging and reject corrupt or unsafe release artifacts."""
import hashlib
import importlib.util
import io
import json
import plistlib
from pathlib import Path
import tarfile
import tempfile
import shutil
import unittest
from unittest.mock import patch
import zipfile
from release_identity import asset_name, checkout_edition, parse_tag, validate_build_identity


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


package = load("package-build")
pages = load("prepare-pages")


class ReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.web = self.root / "web"
        for name in ["index.html", ".nojekyll", "Build/game.loader.js", "Build/game.wasm.unityweb",
                     "Build/game.data.unityweb", "Build/game.framework.js.unityweb"] + [
                         f"StreamingAssets/BuiltInPackages/Levels/level{i:02}.ballance" for i in range(1, 14)]:
            path = self.web / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(b"test fixture")
        (self.web / "index.html").write_text('productName: "Ballance", productVersion: "1.0.12"')
        (self.web / "build-identity.json").write_text(json.dumps({"edition": "original", "version": "1.0.12", "productName": "Ballance", "applicationIdentifier": "com.MengyuGame.Ballance"}))
        for name in ("core", "core.assets.skys", "core.sounds", "core.sounds.music",
                     "mod.invlife", "mod.selfkill", "mod.speedball"):
            path = self.web / "StreamingAssets/BuiltInPackages" / ("Core" if name == "core" else "Packages") / (name + ".ballance")
            path.parent.mkdir(parents=True, exist_ok=True)
            with zipfile.ZipFile(path, "w") as bundle:
                bundle.writestr(f"assets/{name}.assetbundle", b"fixture")
        self.download = self.root / "download"
        self.download.mkdir()
        self.destination = self.root / "public"
        self.destination.mkdir()
        self.archive = self.download / "Ballance-1.0.12-WebGL.tar.gz"
        self.tag = "v1.0.12"

    def checksum(self):
        Path(str(self.archive) + ".sha256").write_text(hashlib.sha256(self.archive.read_bytes()).hexdigest())

    def archive_web(self):
        with tarfile.open(self.archive, "w:gz") as output:
            for path in self.web.iterdir():
                output.add(path, arcname=path.name)
        self.checksum()

    def test_round_trip(self):
        self.archive_web()
        pages.prepare(self.download, self.destination, self.tag)
        package.validate_web(self.destination)
        self.assertEqual((self.destination / "index.html").read_text(), 'productName: "Ballance", productVersion: "1.0.12"')
        identity = json.loads((self.destination / "release.json").read_text())
        self.assertEqual(identity, {"tag": self.tag, "version": "1.0.12",
                                   "archive": self.archive.name, "edition": "original",
                                   "sha256": hashlib.sha256(self.archive.read_bytes()).hexdigest()})

    def test_macos_packager_uses_the_development_bundle_name(self):
        settings = self.root / "ProjectSettings"; settings.mkdir()
        (settings / "ProjectSettings.asset").write_text("  bundleVersion: 1.0.12-dev.9\n")
        (settings / "Edition.json").write_text(json.dumps({"id": "original", "productName": "Ballance",
            "applicationIdentifier": "com.MengyuGame.Ballance"}))
        folder = self.root / "Builds/macOS"; folder.mkdir(parents=True)
        (folder / "build-identity.json").write_text(json.dumps({"edition": "original", "version": "1.0.12-dev.9",
            "productName": "Ballance", "applicationIdentifier": "com.MengyuGame.Ballance"}))
        app = folder / "Ballance.app"; (app / "Contents").mkdir(parents=True)
        (app / "Contents/Info.plist").write_bytes(plistlib.dumps({"CFBundleName": "Ballance"}))
        with patch.object(package, "ROOT", self.root), patch.object(package.subprocess, "run") as run:
            with self.assertRaisesRegex(RuntimeError, "Build macOS first"):
                package.package("macOS")
            app.rename(folder / "Ballance-dev.app")
            archive = package.package("macOS")
        self.assertEqual(Path(run.call_args.args[0][-2]).name, "Ballance-dev.app")
        self.assertEqual(plistlib.loads((folder / "Ballance-dev.app/Contents/Info.plist").read_bytes())["CFBundleDisplayName"], "Ballance-dev")
        self.assertEqual(archive.name, "Ballance-1.0.12-dev.9-macOS.zip")

    def test_missing_level(self):
        (self.web / "StreamingAssets/BuiltInPackages/Levels/level13.ballance").unlink()
        with self.assertRaisesRegex(RuntimeError, "Missing Web level"):
            package.validate_web(self.web)

    def test_corrupt_download(self):
        self.archive_web()
        with self.archive.open("ab") as stream:
            stream.write(b"corruption")
        with self.assertRaisesRegex(RuntimeError, "checksum"):
            pages.prepare(self.download, self.destination, self.tag)

    def test_archive_escape(self):
        with tarfile.open(self.archive, "w:gz") as output:
            member = tarfile.TarInfo("../escaped")
            member.size = 1
            output.addfile(member, io.BytesIO(b"x"))
        self.checksum()
        with self.assertRaises(tarfile.FilterError):
            pages.prepare(self.download, self.destination, self.tag)
        self.assertFalse((self.root / "escaped").exists())

    def test_symlink(self):
        (self.web / "link").symlink_to("index.html")
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "links"):
            pages.prepare(self.download, self.destination, self.tag)

    def test_missing_web_asset_or_checksum(self):
        with self.assertRaisesRegex(RuntimeError, "exactly one"):
            pages.prepare(self.download, self.destination, self.tag)
        self.archive_web()
        Path(str(self.archive) + ".sha256").unlink()
        with self.assertRaises(FileNotFoundError):
            pages.prepare(self.download, self.destination, self.tag)
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_wrong_archive_version(self):
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "archive version"):
            pages.prepare(self.download, self.destination, "v1.0.13")
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_wrong_embedded_version(self):
        (self.web / "index.html").write_text('productVersion: "0.9.0"')
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "Embedded Web player version"):
            pages.prepare(self.download, self.destination, self.tag)
        self.assertFalse((self.destination / "release.json").exists())

    def test_invalid_tag(self):
        with self.assertRaisesRegex(RuntimeError, "release tag"):
            pages.prepare(self.download, self.destination, "../v1.0.12")

    def test_edition_round_trip_and_wrong_product(self):
        self.tag = "original/v1.0.12"
        self.archive = self.download / asset_name(self.tag)
        (self.web / "index.html").write_text('productName: "Ballance", productVersion: "1.0.12"')
        (self.web / "build-identity.json").write_text(json.dumps({"edition": "original", "version": "1.0.12", "productName": "Ballance", "applicationIdentifier": "com.MengyuGame.Ballance"}))
        self.archive_web()
        digest = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        result = pages.prepare(self.download, self.destination, self.tag, digest)
        self.assertEqual(result["edition"], "original")
        self.assertEqual(result["sha256"], digest)
        wrong = self.root / "wrong"
        wrong.mkdir()
        (self.web / "index.html").write_text('productName: "Reballance", productVersion: "1.0.12"')
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "edition"):
            pages.prepare(self.download, wrong, self.tag)

    def test_wrong_application_identity_rejected(self):
        self.tag = "original/v1.0.12"
        self.archive = self.download / asset_name(self.tag)
        (self.web / "index.html").write_text('productName: "Ballance", productVersion: "1.0.12"')
        (self.web / "build-identity.json").write_text(json.dumps({"edition": "original", "version": "1.0.12",
            "productName": "Ballance", "applicationIdentifier": "com.iwonz.BallanceRemaster"}))
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "edition"):
            pages.prepare(self.download, self.destination, self.tag)

    def test_pinned_checksum_and_cross_edition_rejected(self):
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "pinned"):
            pages.prepare(self.download, self.destination, self.tag, "0" * 64)
        with self.assertRaisesRegex(RuntimeError, "release tag"):
            pages.prepare(self.download, self.destination, "remaster/v1.0.12")
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_checkout_identity_prevents_relabeling(self):
        self.assertEqual(parse_tag("v1.0.12"), ("original", "1.0.12", False))
        self.assertEqual(asset_name("v1.0.12", "Windows"), "Ballance-1.0.12-Windows.zip")
        self.assertEqual(asset_name("original/v1.0.12-dev.1", "Android"), "Ballance-1.0.12-dev.1-Android.apk")
        settings = self.root / "ProjectSettings"
        settings.mkdir()
        descriptor = {"id": "original", "productName": "Ballance", "applicationIdentifier": "com.MengyuGame.Ballance"}
        (settings / "Edition.json").write_text(json.dumps(descriptor))
        self.assertEqual(checkout_edition(self.root), descriptor)
        (self.web / "build-identity.json").unlink()
        with self.assertRaisesRegex(RuntimeError, "Missing build identity"):
            validate_build_identity(self.root, self.web, "1.0.12")
        built = {"edition": "original", "version": "1.0.12", "productName": descriptor["productName"], "applicationIdentifier": descriptor["applicationIdentifier"]}
        (self.web / "build-identity.json").write_text(json.dumps(built))
        self.assertEqual(validate_build_identity(self.root, self.web, "1.0.12"), "v1.0.12")
        with self.assertRaisesRegex(RuntimeError, "do not relabel"):
            validate_build_identity(self.root, self.web, "1.0.1")

    def test_historical_v1011_without_build_identity(self):
        self.tag = "v1.0.11"
        self.archive = self.download / asset_name(self.tag)
        (self.web / "index.html").write_text('productVersion: "1.0.11"')
        (self.web / "build-identity.json").unlink()
        self.archive_web()
        identity = pages.prepare(self.download, self.destination, self.tag)
        self.assertNotIn("edition", identity)
        self.assertEqual(identity["version"], "1.0.11")

    def test_new_version_requires_build_identity(self):
        (self.web / "build-identity.json").unlink()
        self.archive_web()
        with self.assertRaises(FileNotFoundError):
            pages.prepare(self.download, self.destination, self.tag)

    def test_no_overwrite(self):
        self.archive_web()
        (self.destination / "keep").write_text("untouched")
        with self.assertRaisesRegex(RuntimeError, "empty"):
            pages.prepare(self.download, self.destination, self.tag)
        self.assertEqual((self.destination / "keep").read_text(), "untouched")


if __name__ == "__main__":
    unittest.main()
