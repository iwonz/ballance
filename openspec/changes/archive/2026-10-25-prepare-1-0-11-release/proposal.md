## Why

The current published 1.0.10 players do not contain the completed runtime modernization, Web performance, and graphics diagnostics changes. The user requires current binaries for all five supported release targets and the same version on GitHub Pages.

## What Changes

- Set player version 1.0.11 and increment the Android version code.
- Rebuild and verify Windows, Linux, macOS, Android, and Web players and their release archives/checksums from the current implementation.
- Prepare a verified release commit and notes, then publish its five assets and mandatory matching Pages deployment.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

None. This is release preparation under the existing platform-builds and web-publication contracts; it changes version metadata, not their behavior. `skip_specs: true` avoids manufacturing a redundant delta.

## Impact

ProjectSettings version fields, complete target-specific builds/content, release assets, Git tag/release, and GitHub Pages. Preserve previous outputs and player saves; iOS remains deferred. Source/readiness verification is archived before tagging. Actual publication and public-site verification remain mandatory delivery actions after that commit, with outcomes in release notes and Actions links.
