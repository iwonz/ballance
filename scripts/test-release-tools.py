#!/usr/bin/env python3
"""Exercise Pages packaging and reject corrupt or unsafe release artifacts."""
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import tarfile
import tempfile
import unittest
import zipfile


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


if __name__ == "__main__":
    unittest.main()
