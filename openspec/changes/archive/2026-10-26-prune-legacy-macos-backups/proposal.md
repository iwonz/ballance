## Why

An old unpacked `.app-backup` bundle survives cleanup because the installer only
recognizes `.app` and `.app.disabled`. It wastes disk space after successful installation.

## What Changes

- Recognize the exact legacy `.app-backup` suffix as a retired application.
- Preserve existing identity, version, process and replacement validation.
- Transfer the shared fix from main and add a focused regression check.

## Capabilities

### Modified Capabilities

- `platform-builds`: retired application cleanup includes the known legacy backup suffix.

## Impact

Only the macOS installer, its tests and maintenance documentation change.
Game content, installed version identities, saved data and release assets stay unchanged.
