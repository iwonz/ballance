## Why

The preserved Original branch needs independent release identities and must not replace the shared Pages site with its historical single-player workflow. Its gameplay, assets, UI, storage identity and ABI 2302 remain the baseline.

## What Changes

- Cherry-pick the shared edition-aware build, packaging and central Pages tooling from main.
- Add the Original edition descriptor, README identity and branch maintenance guidance.
- Keep the immutable original/v1.0.11 alias and existing published assets unchanged. This is source preparation, not a new binary release.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `platform-builds`: validate edition identity and use namespaced release archives.
- `web-publication`: deploy the pinned pair from main and verify both public identities.

## Impact

Editor PlayerBuild, release scripts, Pages workflow, descriptor and maintenance documentation. No runtime or native library changes. The shared site remains unavailable until main has verified pins for both editions.
