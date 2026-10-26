#!/usr/bin/env python3
"""Exercise installation replacement, validation failure and cleanup scope."""
import importlib.util
from pathlib import Path
import plistlib
import tempfile
import subprocess
import sys
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location("installer", Path(__file__).with_name("install-macos.py"))
installer = importlib.util.module_from_spec(spec)
spec.loader.exec_module(installer)


class InstallationTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.root = Path(self.temporary.name)

    def tearDown(self):
        self.temporary.cleanup()

    def app(self, name, edition="remaster", version="1.0.0-dev.2", payload="new"):
        path = self.root/name
        (path/"Contents/MacOS").mkdir(parents=True)
        (path/"Contents/Info.plist").write_bytes(plistlib.dumps({
            "CFBundleIdentifier": installer.EDITIONS[edition][0],
            "CFBundleShortVersionString": version, "CFBundleExecutable": "Ballance",
        }))
        (path/"Contents/MacOS/Ballance").write_text(payload)
        return path

    def test_replacement_preserves_other_edition_and_data(self):
        source = self.app("build/Remaster.app")
        target = self.app("Applications/Reballance.app", version="1.0.0-dev.1", payload="old")
        original = self.app("Applications/Ballance.app", edition="original", version="1.0.11")
        saved = self.root/"saved-profile.json"; saved.write_text("sentinel")
        original_hash = installer.fingerprint(original)
        with patch.object(installer, "verify_signature"):
            installer.install(source,"remaster",target)
        self.assertEqual(installer.fingerprint(source),installer.fingerprint(target))
        self.assertEqual(original_hash,installer.fingerprint(original))
        self.assertEqual(saved.read_text(),"sentinel")
        self.assertEqual(list(target.parent.glob(".ballance-*")),[])

    def test_staging_does_not_register_an_extra_application(self):
        source = self.app("build/New.app")
        target = self.root/"Applications/Reballance.app"
        checked = []
        with patch.object(installer,"verify_signature",side_effect=lambda path: checked.append(path)):
            installer.install(source,"remaster",target)
        self.assertEqual(checked[1].parent.suffix,".noindex")
        self.assertEqual(list(target.parent.glob(".ballance-*")),[])

    def test_original_name_migration_preserves_signed_contents(self):
        source = self.app("Applications/Ballance Original.app", edition="original", version="1.0.11")
        target = self.root / "Applications" / installer.EDITIONS["original"][1]
        before = installer.fingerprint(source)
        with patch.object(installer, "verify_signature"):
            installer.install(source, "original", target)
        self.assertEqual(target.name, "Ballance.app")
        self.assertEqual(installer.fingerprint(target), before)
        self.assertEqual(installer.duplicates("original", target, [target.parent]), [source])

    def test_remaster_name_migration_preserves_identity(self):
        source = self.app("Applications/Ballance Remaster.app")
        target = self.root / "Applications" / installer.EDITIONS["remaster"][1]
        with patch.object(installer, "verify_signature"):
            installer.install(source, "remaster", target)
        self.assertEqual(target.name, "Reballance.app")
        self.assertEqual(installer.fingerprint(source), installer.fingerprint(target))
        self.assertEqual(installer.duplicates("remaster", target, [target.parent]), [source])

    def test_migration_cannot_delete_a_newer_noncanonical_installation(self):
        source = self.app("build/Older.app", version="1.0.0-dev.2")
        self.app("Applications/Ballance Remaster.app", version="1.0.0-dev.6")
        with patch.object(installer, "APPLICATIONS", self.root / "Applications"), \
             patch.object(Path, "home", return_value=self.root / "home"), \
             patch.object(sys, "argv", ["install-macos.py", "remaster", str(source)]):
            with self.assertRaisesRegex(ValueError, "newer installed"):
                installer.main()

    def test_corrupt_copy_keeps_installed_version(self):
        source = self.app("build/New.app")
        target = self.app("Applications/Remaster.app",version="1.0.0-dev.1",payload="old")
        before = installer.fingerprint(target)
        with patch.object(installer,"verify_signature"), patch.object(installer,"fingerprint",side_effect=["source","corrupt"]):
            with self.assertRaises(ValueError): installer.install(source,"remaster",target)
        self.assertEqual(before,installer.fingerprint(target))
        self.assertEqual(list(target.parent.glob(".ballance-*")),[])

    def test_wrong_identity_and_downgrade_are_rejected(self):
        source = self.app("Original.app",edition="original",version="1.0.11")
        target = self.app("Remaster.app",version="1.0.0")
        with self.assertRaises(ValueError): installer.install(source,"remaster",target)
        source = self.app("Older.app",version="1.0.0-dev.2")
        with self.assertRaises(ValueError): installer.install(source,"remaster",target)

    def test_signature_failure_preserves_target(self):
        source = self.app("New.app"); target = self.app("Installed.app",payload="old")
        before = installer.fingerprint(target)
        with patch.object(installer,"verify_signature",side_effect=ValueError("invalid signature")):
            with self.assertRaises(ValueError): installer.install(source,"remaster",target)
        self.assertEqual(before,installer.fingerprint(target))

    def test_cleanup_is_scoped_to_matching_application_bundles(self):
        target = self.app("Applications/Reballance.app")
        old = self.app("Builds/checks/Old.app")
        self.app("Applications/Ballance Original.app",edition="original",version="1.0.11")
        (self.root/"Builds/release.tar.gz").write_text("archive")
        unrelated = self.root/"Applications/Unrelated.app/Contents"
        unrelated.mkdir(parents=True); (unrelated/"Info.plist").write_text("<?xml invalid>")
        self.assertEqual(installer.duplicates("remaster",target,[self.root]),[old])

    def test_external_symlink_is_rejected(self):
        app = self.app("Unsafe.app")
        outside = self.root/"profile"; outside.write_text("private")
        (app/"Contents/external").symlink_to(outside)
        with self.assertRaises(ValueError): installer.fingerprint(app)

    def test_cleanup_removes_retired_builds_but_preserves_archives(self):
        app = self.app("Builds/Old.app.disabled")
        archive = self.root / "Builds/release.zip"; archive.write_text("verified archive")
        self.assertEqual(list(installer.bundles([self.root / "Builds"])), [])
        self.assertEqual(list(installer.bundles([self.root / "Builds"], include_retired=True)), [app])
        with patch.object(installer, "unregister") as unregister:
            installer.remove_build_copy(app, "remaster", "1.0.0-dev.3")
        self.assertEqual({call.args[0] for call in unregister.call_args_list}, {app, app.resolve()})
        self.assertFalse(app.exists())
        self.assertEqual(archive.read_text(), "verified archive")

    def test_cleanup_rejects_other_edition_and_preserves_newer_work(self):
        app = self.app("Old.app")
        with self.assertRaises(ValueError): installer.remove_build_copy(app, "original", "1.0.11")
        installer.remove_build_copy(app, "remaster", "1.0.0-dev.1")
        self.assertTrue(app.exists())

    def test_legacy_backup_is_retired_and_cleanup_checks_edition(self):
        app = self.app("Builds/Ballance-1.0.8.app-backup", edition="original", version="1.0.8")
        other = self.app("Builds/Reballance.app-backup")
        target = self.app("Applications/Ballance.app", edition="original", version="1.0.11")
        unrelated = self.root / "Builds/Notes.app-backup-extra"
        unrelated.mkdir()
        self.assertEqual(list(installer.bundles([self.root / "Builds"])), [])
        self.assertEqual(installer.duplicates("original", target, [self.root / "Builds"], include_retired=True), [app])
        with patch.object(installer, "unregister"):
            installer.remove_build_copy(app, "original", "1.0.11")
        self.assertFalse(app.exists())
        self.assertTrue(other.exists() and unrelated.exists() and target.exists())

    def test_install_deletes_disabled_applications_after_validating_replacement(self):
        source = self.app("Builds/New-dev.app", version="1.0.0-dev.3")
        old = self.app("Applications/Reballance.app.disabled", version="1.0.0-dev.1")
        previous = self.app("Applications/Reballance-dev.app", version="1.0.0-dev.2")
        original = self.app("Applications/Ballance.app", edition="original", version="1.0.11")
        expected = installer.fingerprint(source)
        with patch.object(installer, "REPO", self.root), \
             patch.object(installer, "APPLICATIONS", self.root / "Applications"), \
             patch.object(Path, "home", return_value=self.root / "home"), \
             patch.object(sys, "platform", "darwin"), \
             patch.object(sys, "argv", ["install-macos.py", "remaster", str(source), "--apply"]), \
             patch.object(installer, "verify_signature"), \
             patch.object(installer.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "", "")):
            installer.main()
        self.assertEqual(installer.fingerprint(previous), expected)
        self.assertFalse(old.exists())
        self.assertFalse(source.exists())
        self.assertTrue(original.exists())

    def test_development_app_names_are_distinct(self):
        self.assertEqual(installer.application_name("remaster", "1.0.0-dev.8"), "Reballance-dev.app")
        self.assertEqual(installer.application_name("remaster", "1.0.0"), "Reballance.app")
        self.assertEqual(installer.application_name("original", "1.0.12-dev.1"), "Ballance-dev.app")
        self.assertEqual(installer.application_name("original", "1.0.12"), "Ballance.app")

    def test_running_build_through_real_path_is_preserved(self):
        source = self.app("Builds.noindex/New-dev.app", version="1.0.0-dev.3")
        (self.root / "Builds").symlink_to("Builds.noindex", target_is_directory=True)
        with patch.object(installer, "REPO", self.root), \
             patch.object(installer, "APPLICATIONS", self.root / "Applications"), \
             patch.object(Path, "home", return_value=self.root / "home"), \
             patch.object(sys, "platform", "darwin"), \
             patch.object(sys, "argv", ["install-macos.py", "remaster", str(self.root / "Builds/New-dev.app"), "--apply"]), \
             patch.object(installer.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, str(source.resolve() / "Contents/MacOS/Ballance"), "")):
            with self.assertRaisesRegex(ValueError, "Quit this player"):
                installer.main()
        self.assertTrue(source.exists())
        self.assertFalse((self.root / "Applications").exists())

    def test_catalog_selection_is_scoped_and_rejects_unrecognized_paths(self):
        dump = "path: /Applications/Reballance.app (0x123)\nidentifier: com.iwonz.BallanceRemaster\n"
        dump += "---\npath: /Applications/Other.app (0x456)\nidentifier: other.app\n"
        self.assertEqual(installer.registered_paths(dump, "remaster"), [Path("/Applications/Reballance.app")])
        self.assertEqual(installer.registered_paths(dump, "original"), [])
        with self.assertRaises(ValueError):
            installer.registered_paths("path: unknown format\nidentifier: com.iwonz.BallanceRemaster\n", "remaster")

    def test_build_exclusion_preserves_paths_and_archives(self):
        root = self.root/"Builds"; root.mkdir()
        (root/"release.zip").write_bytes(b"unchanged archive")
        installer.exclude_builds_from_index(root)
        self.assertTrue(root.is_symlink())
        self.assertEqual((root/"release.zip").read_bytes(),b"unchanged archive")
        self.assertEqual((self.root/"Builds.noindex/release.zip").read_bytes(),b"unchanged archive")
        installer.exclude_builds_from_index(root)

    def test_build_exclusion_rejects_conflicts_and_rolls_back(self):
        root = self.root/"Builds"; root.mkdir()
        preserved = self.root/"Builds.noindex"; preserved.mkdir()
        with self.assertRaises(ValueError): installer.exclude_builds_from_index(root)
        preserved.rmdir()
        with patch.object(Path,"symlink_to",side_effect=OSError("link failed")):
            with self.assertRaises(OSError): installer.exclude_builds_from_index(root)
        self.assertTrue(root.is_dir()); self.assertFalse(preserved.exists())

    def test_unregistered_copy_is_safe_and_other_errors_are_reported(self):
        app = self.app("Unregistered.app")
        with patch.object(installer.subprocess,"run",return_value=subprocess.CompletedProcess(["lsregister"],1,"","not found: -10814")):
            installer.unregister(app)
        with patch.object(installer.subprocess,"run",return_value=subprocess.CompletedProcess(["lsregister"],1,"","permission denied")):
            with self.assertRaises(subprocess.CalledProcessError): installer.unregister(app)


if __name__ == "__main__":
    unittest.main()
