## Checks

- `python3 scripts/test-release-tools.py`: 9 tests passed, including checksum, traversal, symlink, missing assets, tag/archive/player mismatches, and release identity round-trip.
- `python3 scripts/test-agent-context.py`: 5 tests passed; context/link and strict OpenSpec checks passed during implementation.
- `actionlint` and `git diff --check`: passed. Reviewed every preparation caller and the complete workflow/documentation diff.
- `bash scripts/build.sh <platform>`: WebGL, Android, macOS, Windows, and Linux all succeeded with Unity 6000.6.7f1. Previous outputs were preserved under ignored `Builds/archive-1.0.9`.
- `python3 scripts/package-build.py <platform>`: all five 1.0.10 assets and SHA-256 sidecars created without overwriting previous release archives.
- Inspected all five packaged assets with Python archive readers: SHA-256 matches; all 13 level bundles and seven core/package bundles are present, readable, and contain UnityFS data. Windows PE and Linux ELF are x86-64; their player data embeds 1.0.10. The macOS plist and Web template also report 1.0.10.
- macOS packaging verified ARM64 dependencies, portable library paths, deep/strict ad-hoc signatures, executable permissions after ZIP extraction, and native physics smoke (ABI 2302).
- Android `aapt2 dump badging`: versionName 1.0.10, versionCode 2, ARM64, target SDK 36. `apksigner verify --verbose` and `zipalign -c -P 16 4` passed. ELF inspection found at least 16 KB LOAD alignment in all six native libraries.
- Ran `prepare-pages.py` with the actual Web archive/checksum and v1.0.10. Served its extracted output at `http://127.0.0.1:8010/ballance/` and inspected it in Chrome through Playwright: menu, level selection, level 1 load, tutorial skip, arrow-key movement, fall/respawn with a lost life, pause/resume, and confirmed return to menu all worked. Screenshots are local ignored evidence under `Builds/checks/release-1.0.10-*`.

## Results

All six implementation/build-readiness tasks and all three modified requirements are covered. No critical correctness or coherence issues remain in the reviewed change.

| Requirement | Evidence |
| --- | --- |
| Verified release extraction | Actual archive round-trip and rejection tests; release.json contains the matching version and computed digest. Existing safety checks remain intact. |
| Release-triggered deployment | Published-release trigger has no skip condition; missing assets fail download/preparation. Manual dispatch retains explicit-tag deployment. Workflow lint and missing-asset tests passed. |
| Deployment permissions and browser verification | Existing Pages permissions/environment retained. The new bounded HTTP check fails unless the served tag matches; local HTTP gameplay smoke passed beneath the repository prefix. |

The implementation uses the existing workflow and standard-library helpers as designed. Version metadata is the only Player Settings change; no gameplay or native binary changes were introduced here. Spec synchronization preserves the unrelated static-hosting requirement and existing scenarios.

This record captures source and build readiness before publication. The authorized delivery sequence is commit/push, attach these verified assets to a draft, publish v1.0.10, wait for hosted checks and Pages, and verify the live browser. Hosted rollout results belong in the release notes and linked Actions runs; this record does not claim those future operations have already succeeded.

## Limitations

- No full 13-level playthrough, physical Android/Windows/Linux gameplay, audible-audio confirmation, or mobile lifecycle/touch verification. iOS remains deferred.
- Android is test-signed; macOS is ad-hoc signed and not notarized. These are downloadable player builds, not store submissions.
- Chrome logged refused connections to Unity cloud configuration/analytics endpoints. No game-content request failure or gameplay exception was observed in the exercised path.
- The stale-site timeout and manual rollback were reviewed, not deliberately exercised against production. The hosted success path and live public identity remain delivery checks after publication.
