## Why

Obsolete local players are rediscovered by macOS and consume disk space. The user requires one current application per edition and explicit development names.

## What Changes

- Cherry-pick the shared installation, naming and packaging fix from main.
- Delete obsolete matching app and disabled bundles only after validating the replacement; preserve saved data and release archives.
- Route the legacy macOS wrapper through the content-aware edition build.

## Capabilities

### Modified Capabilities

- `platform-builds`: canonical local installation, safe cleanup and development filenames.

## Impact

PlayerBuild, packaging/release validators, installer, shell wrapper, tests and operating documentation. Runtime gameplay, native libraries and saved-data identities are unchanged.
