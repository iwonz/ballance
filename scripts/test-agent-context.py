#!/usr/bin/env python3
"""Regression checks for maintenance boundaries and the completed-delivery gate."""

import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path


spec = importlib.util.spec_from_file_location("context", Path(__file__).with_name("check-agent-context.py"))
context = importlib.util.module_from_spec(spec)
spec.loader.exec_module(context)


class ContextTests(unittest.TestCase):
    def test_comment_lexer_and_localized_fixture(self):
        path = Path("Assets/System/Scripts/Editor/LocalizationChecks.cs")
        localized = '\u0420\u0443\u0441\u0441\u043a\u0438\u0439'
        source = 'var url = "https://example.test/"; var text = "' + localized + '"; // English'
        self.assertEqual(context.language_errors(path, source), [])
        self.assertTrue(context.language_errors(path, source + '\n/* ' + localized + ' */'))
        self.assertTrue(context.language_errors(Path("Assets/Help.cs"), source))
        self.assertTrue(context.language_errors(path, '#region ' + localized))

    def test_xml_values_and_python_fixtures_are_not_documentation(self):
        text = '\u4e2d\u6587'
        self.assertEqual(context.language_errors(Path("language.xml"), '<Text>' + text + '</Text>'), [])
        self.assertTrue(context.language_errors(Path("language.xml"), '<!--' + text + '-->'))
        self.assertEqual(context.language_errors(Path("test.py"), 'value = "' + text + '"'), [])
        self.assertTrue(context.language_errors(Path("test.py"), '"""' + text + '"""'))
        self.assertTrue(context.language_errors(Path("test.py"), '# ' + text))
        self.assertTrue(context.language_errors(Path("template_Level.json"), '{"name":"' + text + '"}'))
        self.assertEqual(context.language_errors(Path("Level.json"), '{"name":"' + text + '"}'), [])
        self.assertTrue(context.language_errors(Path("project.vcxproj.filters"), '<Filter>' + text + '</Filter>'))

    def test_markdown_links_and_asset_identifiers(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "docs").mkdir()
            (root / "docs/map.md").write_text('# Map\n\n## Player input\n')
            links = '[map](/docs/map.md#player-input) [relative](map.md) [web](https://example.test)'
            self.assertEqual(context.link_errors(root, Path("docs/test.md"), links), [])
            for link in ('[bad](missing.md)', '[bad](map.md#missing)', '[bad](../../outside)'):
                self.assertTrue(context.link_errors(root, Path("docs/test.md"), link))
            self.assertEqual(context.language_errors(Path("docs/test.md"),
                             'Asset `\u4e2d\u6587` at [source](../\u4e2d\u6587.txt).'), [])
            self.assertTrue(context.language_errors(Path("docs/test.md"), '\u4e2d\u6587 prose'))

    def write_record(self, root):
        record = root / "openspec/changes/archive/2026-10-25-test"
        record.mkdir(parents=True)
        for name in ("proposal.md", "design.md"):
            (record / name).write_text('# Context\nA small documentation change.\n')
        (record / ".openspec.yaml").write_text('schema: spec-driven\nskip_specs: true\n')
        (record / "tasks.md").write_text('- [x] Update and verify documentation.\n')
        (record / "verification.md").write_text(
            '## Checks\nContext self-tests.\n## Results\nPassed.\n## Limitations\nNo player build.\n')
        return record

    def test_archive_requires_complete_evidence(self):
        with tempfile.TemporaryDirectory() as directory:
            record = self.write_record(Path(directory))
            self.assertEqual(context.record_errors(record), [])
            (record / "tasks.md").write_text('- [ ] Unfinished work\n')
            self.assertTrue(context.record_errors(record))
            (record / "tasks.md").write_text('- [x] Done\n')
            (record / ".openspec.yaml").write_text('schema: spec-driven\n')
            self.assertTrue(context.record_errors(record))
            (record / "specs/area/test").mkdir(parents=True)
            (record / "specs/area/test/spec.md").write_text('Delta syntax is validated by OpenSpec.\n')
            self.assertEqual(context.record_errors(record), [])
            (record / "verification.md").write_text('## Checks\n\n## Results\nPassed.\n## Limitations\nNone.\n')
            self.assertTrue(context.record_errors(record))

    def test_delivery_requires_new_completed_record(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            def git(*args):
                return subprocess.check_output(['git', '-C', str(root), *args], stderr=subprocess.DEVNULL).decode().strip()
            git('init', '-q')
            git('config', 'user.email', 'test@example.invalid')
            git('config', 'user.name', 'Context Test')
            (root / "README.md").write_text('Baseline\n')
            git('add', '.')
            git('-c', 'commit.gpgsign=false', 'commit', '-qm', 'Baseline')
            base = git('rev-parse', 'HEAD')
            self.assertEqual(context.delivery_errors(root, base), [])
            (root / "README.md").write_text('Updated\n')
            self.assertTrue(context.delivery_errors(root, base))
            record = self.write_record(root)
            self.assertEqual(context.delivery_errors(root, base), [])
            (record / "tasks.md").write_text('- [ ] Unfinished\n')
            self.assertTrue(context.delivery_errors(root, base))
            (record / "tasks.md").write_text('- [x] Done\n')
            git('add', '.')
            git('-c', 'commit.gpgsign=false', 'commit', '-qm', 'Completed change')
            self.assertEqual(context.delivery_errors(root, base), [])
            new_base = git('rev-parse', 'HEAD')
            (root / "README.md").write_text('Another update\n')
            self.assertTrue(context.delivery_errors(root, new_base))


if __name__ == "__main__":
    unittest.main()
