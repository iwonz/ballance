#!/usr/bin/env python3
"""Check bundled Russian translations without requiring Unity or a license."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []
count = 0
resources = list((ROOT / 'Assets').rglob('PackageLanguageRes*.xml'))
resources.append(ROOT / 'Assets/Resources/StaticLangResource.xml')
for path in resources:
    root = ET.parse(path).getroot()
    languages = {
        node.get('name'): {t.get('name'): ''.join(t.itertext()) for t in node.findall('Text')}
        for node in root.findall('Language')
    }
    russian = languages.get('Russian', {})
    english = languages.get('English', {})
    keys = set().union(*(set(values) for values in languages.values()))
    for key in sorted(keys - russian.keys()):
        errors.append(f'{path.relative_to(ROOT)}: missing Russian key {key}')
    for key, value in russian.items():
        count += 1
        if not value.strip():
            errors.append(f'{path.name}: empty translation {key}')
        if key in english:
            placeholders = lambda text: sorted(re.findall(r'\{\d+(?:[^{}]*)\}', text))
            if placeholders(value) != placeholders(english[key]):
                errors.append(f'{path.name}: format placeholders differ for {key}')
    russian_nodes = root.findall("./Language[@name='Russian']/Text")
    if len(russian_nodes) != len(russian):
        errors.append(f'{path.name}: duplicate Russian keys')

page = (ROOT / 'Assets/Game/Menu/Prefabs/Pages/PageLanguage.prefab').read_text()
if 'value: BtnRussianClick' not in page or 'value: Русский' not in page:
    errors.append('Russian language button is missing')
controller = (ROOT / 'Assets/Game/Menu/Scripts/Control/SettingsUIControl.cs').read_text()
if 'ApplyLanguage(SystemLanguage.Russian)' not in controller or 'PlayerPrefs.Save();' not in controller:
    errors.append('Russian language selection/persistence is missing')
if errors:
    raise SystemExit('\n'.join(errors))
print(f'PASS: {count} Russian translations across {len(resources)} resources; placeholders and language selection checked.')
