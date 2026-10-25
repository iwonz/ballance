#!/usr/bin/env python3
"""Exercise Pages packaging and reject corrupt or unsafe release artifacts."""
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tarfile
import tempfile
import shutil
import unittest
import zipfile
from release_identity import asset_name, checkout_edition, parse_tag, validate_build_identity


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + ".py"))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


package = load("package-build")
pages = load("prepare-pages")
site = load("prepare-site")


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
        (self.web / "index.html").write_text('productVersion: "1.0.0"')
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
        self.archive = self.download / "Ballance-1.0.0-WebGL.tar.gz"
        self.tag = "v1.0.0"

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
        self.assertEqual((self.destination / "index.html").read_text(), 'productVersion: "1.0.0"')
        identity = json.loads((self.destination / "release.json").read_text())
        self.assertEqual(identity, {"tag": self.tag, "version": "1.0.0",
                                   "archive": self.archive.name,
                                   "sha256": hashlib.sha256(self.archive.read_bytes()).hexdigest()})

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
            pages.prepare(self.download, self.destination, "v1.0.1")
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_wrong_embedded_version(self):
        (self.web / "index.html").write_text('productVersion: "0.9.0"')
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "Embedded Web player version"):
            pages.prepare(self.download, self.destination, self.tag)
        self.assertFalse((self.destination / "release.json").exists())

    def test_invalid_tag(self):
        with self.assertRaisesRegex(RuntimeError, "release tag"):
            pages.prepare(self.download, self.destination, "../v1.0.0")

    def test_edition_round_trip_and_wrong_product(self):
        self.tag = "remaster/v1.0.0"
        self.archive = self.download / asset_name(self.tag)
        (self.web / "index.html").write_text('productName: "Ballance Remaster", productVersion: "1.0.0"')
        (self.web / "build-identity.json").write_text(json.dumps({"edition": "remaster", "version": "1.0.0", "productName": "Ballance Remaster", "applicationIdentifier": "com.iwonz.BallanceRemaster"}))
        self.archive_web()
        digest = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        result = pages.prepare(self.download, self.destination, self.tag, digest)
        self.assertEqual(result["edition"], "remaster")
        self.assertEqual(result["sha256"], digest)
        wrong = self.root / "wrong"
        wrong.mkdir()
        (self.web / "index.html").write_text('productName: "Ballance", productVersion: "1.0.0"')
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "edition"):
            pages.prepare(self.download, wrong, self.tag)

    def test_wrong_application_identity_rejected(self):
        self.tag = "remaster/v1.0.0"
        self.archive = self.download / asset_name(self.tag)
        (self.web / "index.html").write_text('productName: "Ballance Remaster", productVersion: "1.0.0"')
        (self.web / "build-identity.json").write_text(json.dumps({"edition": "remaster", "version": "1.0.0",
            "productName": "Ballance Remaster", "applicationIdentifier": "com.MengyuGame.Ballance"}))
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "edition"):
            pages.prepare(self.download, self.destination, self.tag)

    def test_pinned_checksum_and_cross_edition_rejected(self):
        self.archive_web()
        with self.assertRaisesRegex(RuntimeError, "pinned"):
            pages.prepare(self.download, self.destination, self.tag, "0" * 64)
        with self.assertRaisesRegex(RuntimeError, "archive version"):
            pages.prepare(self.download, self.destination, "remaster/v1.0.0")
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_checkout_identity_prevents_relabeling(self):
        self.assertEqual(parse_tag("v1.0.11"), ("original", "1.0.11", True))
        self.assertEqual(asset_name("original/v1.0.12", "Windows"), "Ballance-Original-1.0.12-Windows.zip")
        self.assertEqual(asset_name("remaster/v1.0.0-dev.1", "Android"), "Ballance-Remaster-1.0.0-dev.1-Android.apk")
        settings = self.root / "ProjectSettings"
        settings.mkdir()
        descriptor = {"id": "remaster", "productName": "Ballance Remaster", "applicationIdentifier": "com.iwonz.BallanceRemaster"}
        (settings / "Edition.json").write_text(json.dumps(descriptor))
        self.assertEqual(checkout_edition(self.root), descriptor)
        with self.assertRaisesRegex(RuntimeError, "Missing build identity"):
            validate_build_identity(self.root, self.web, "1.0.0")
        built = {"edition": "remaster", "version": "1.0.0", "productName": descriptor["productName"], "applicationIdentifier": descriptor["applicationIdentifier"]}
        (self.web / "build-identity.json").write_text(json.dumps(built))
        self.assertEqual(validate_build_identity(self.root, self.web, "1.0.0"), "remaster/v1.0.0")
        with self.assertRaisesRegex(RuntimeError, "do not relabel"):
            validate_build_identity(self.root, self.web, "1.0.1")

    def site_fixture(self):
        (self.web / "index.html").write_text('<html><link href="TemplateData/style.css"><script>var buildUrl = "Build"; '
            'var config = {streamingAssetsUrl: "StreamingAssets", productVersion: "1.0.0"};</script></html>')
        self.archive_web()
        downloads = self.root / "pair"
        shutil.copytree(self.download, downloads / "original")
        original_digest = hashlib.sha256(self.archive.read_bytes()).hexdigest()
        self.archive.unlink()
        Path(str(self.archive) + ".sha256").unlink()
        self.tag = "remaster/v1.0.0"
        self.archive = self.download / asset_name(self.tag)
        (self.web / "index.html").write_text('productName: "Ballance Remaster", productVersion: "1.0.0"')
        (self.web / "build-identity.json").write_text(json.dumps({"edition": "remaster", "version": "1.0.0", "productName": "Ballance Remaster", "applicationIdentifier": "com.iwonz.BallanceRemaster"}))
        self.archive_web()
        shutil.copytree(self.download, downloads / "remaster")
        source = self.root / "site"
        source.mkdir()
        (source / "index.html").write_text('<a href="original.html">Original</a><a href="remaster/">Remaster</a>')
        manifest = source / "releases.json"
        manifest.write_text(json.dumps({"schema": 1, "editions": {
            "original": {"tag": "v1.0.0", "sha256": original_digest},
            "remaster": {"tag": self.tag, "sha256": hashlib.sha256(self.archive.read_bytes()).hexdigest()}}}))
        return downloads, source, manifest

    def test_two_editions_and_original_page_directory(self):
        downloads, source, manifest = self.site_fixture()
        identities = site.prepare(downloads, self.destination, source, manifest)
        self.assertEqual(set(identities), {"original", "remaster"})
        host = (self.destination / "original.html").read_text()
        for path in ('"original/Build"', '"original/StreamingAssets"', '"original/TemplateData/style.css"'):
            self.assertIn(path, host)
        self.assertFalse((self.destination / "original/index.html").exists())
        self.assertTrue((self.destination / "remaster/index.html").is_file())
        self.assertEqual(json.loads((self.destination / "release.json").read_text())["editions"], identities)

    def test_pair_failure_never_publishes_half_a_site(self):
        downloads, source, manifest = self.site_fixture()
        with self.assertRaisesRegex(RuntimeError, "Combined"):
            site.prepare(downloads, self.destination, source, manifest, limit=1)
        self.assertEqual(list(self.destination.iterdir()), [])
        (downloads / "remaster" / self.archive.name).write_bytes(b"bad")
        with self.assertRaisesRegex(RuntimeError, "checksum"):
            site.prepare(downloads, self.destination, source, manifest)
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_unready_cross_edition_and_bad_manifest_rejected(self):
        downloads, source, manifest = self.site_fixture()
        valid = json.loads(manifest.read_text())
        for value in (None, {"tag": "v1.0.0", "sha256": "0" * 64},
                      {"tag": "remaster/v1.0.0", "sha256": "not-a-hash"}):
            valid["editions"]["remaster"] = value
            manifest.write_text(json.dumps(valid))
            with self.assertRaises(RuntimeError):
                site.prepare(downloads, self.destination, source, manifest)
        self.assertEqual(list(self.destination.iterdir()), [])

    def test_original_template_requires_path_audit_on_change(self):
        with self.assertRaisesRegex(RuntimeError, "Unrecognized"):
            site.original_host('<html><base href="original/"></html>')


if __name__ == "__main__":
    unittest.main()
