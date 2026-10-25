# Contributing game translations

Repository documentation and code comments are maintained in English. Game language
resources remain multilingual; do not translate their keys, package IDs, or format
placeholders. Use the [OpenSpec workflow](openspec/README.md) for translation changes.

Read the [localization source map](docs/architecture.md#localization). Each resource
contains language sections: English is loaded first, then nonempty values from the
selected language override it. Add translations to the relevant system/core/package
XML files, keeping the English key set and formatting placeholders consistent.
Check both regular package resources and the metadata resources loaded before a
package starts. Do not replace player-entered names or original license texts.

```sh
python3 scripts/check-localization.py
```

Also run `Ballance2.Editor.LocalizationChecks.Run` and inspect rendered menus,
dialogs, tutorials, and mod settings in Unity. Verify font coverage, fallback text,
line wrapping, and language selection after restart. Follow
[validation.md](docs/validation.md) for the editor commands. Extend check expectations
when intentionally adding a supported language; do not weaken existing coverage to
make an incomplete translation pass.
