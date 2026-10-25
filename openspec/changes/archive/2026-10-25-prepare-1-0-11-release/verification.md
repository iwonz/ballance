## Checks

Base source: `73a32e48963dc60d4fe8e317061672cfa746b354`. Unity: 6000.6.7f1.

- Source metadata changed to version 1.0.11 and Android version code 3. Previous
  players were moved intact to ignored `Builds/archive-before-1.0.11`.
- Unity batch builds use `-batchmode -nographics -quit -buildTarget <target>
  -executeMethod Ballance2.Editor.PlayerBuild.Build`; logs are
  `Logs/build-<platform>.log`. WebGL, Android, macOS, Windows, and Linux all passed
  with exit 0 and rebuilt content for all 13 levels. After all target switches,
  the source configuration diff still contains only the two version fields.
- Existing packagers produced all five 1.0.11 assets and SHA-256 sidecars.
  macOS packaging passed ARM64/dependency checks, deep/strict ad-hoc signing,
  extracted ZIP validation, executable permissions, and IVP smoke (ABI 2302).
- Android SDK 36 `aapt2 dump badging` reports versionName 1.0.11, versionCode 3,
  ARM64, minimum SDK 26, and target SDK 36. `apksigner verify --verbose` passed
  (v2 signature), as did `zipalign -c -P 16 4`.
- Independent Python archive inspection (`Builds/checks/verify-release-1.0.11.py`)
  passed checksums, ZIP CRCs, all 13 UnityFS level bundles, all seven core/package
  archives and canonical metadata/bundle paths. Web/macOS/Windows/Linux embedded
  versions match 1.0.11. Windows PE and Linux ELF are x86-64; all six Android native
  libraries are ARM64 with LOAD alignment of at least 16 KB. The exact local asset
  inventory/digests are in `Builds/release-1.0.11-local-manifest.json`.
- `prepare-pages.py` accepted the exact Web archive/checksum and v1.0.11 after its
  required empty destination was created. Served at
  `http://127.0.0.1:8011/ballance/`: player and release.json both report 1.0.11;
  menu and graphics settings show the Apple M3/WebGL 2 renderer. Local screenshots
  are `Builds/checks/output/playwright/release-1.0.11-*.png`.
- `measure-web-performance.js` and `check-web-performance.py` passed against the
  exact extracted release at that URL. Report:
  `Builds/checks/output/playwright/performance-1792928918240.json`. Chrome
  155.0.8059.85, Apple M3 through ANGLE/Metal, High quality, visible 1920x1200 canvas,
  DPR 2, measured refresh 59.999 Hz, actual context high-performance. The script
  loaded level 01, skipped the tutorial, moved the ball, and measured unpaused play.
  Before/after screenshots show the live level and decreasing score.
- `openspec validate --all --strict --no-interactive`: 14 items passed before
  archive. `python3 scripts/check-agent-context.py`: passed, 13 capabilities.
- After archive, strict validation passed all 13 specs; the context delivery check
  passed against the base source commit, and `git diff --check` passed.

| Scenario | FPS | Frame p95 / p99 / max (ms) | GPU p95 / p99 / max (ms) | Frames over 33.34 ms |
| --- | ---: | --- | --- | ---: |
| Menu | 59.96 | 18.00 / 18.60 / 21.80 | 3.68 / 3.98 / 31.09 | 0 |
| Level 01 start area | 59.96 | 18.00 / 18.70 / 26.20 | 3.82 / 4.13 / 10.23 | 0 |

Web archive SHA-256:
`3557b247f90e6b3877a5c94ad409da016c669c1b9ca95bb1845208f203b15610`.
Measured Wasm SHA-256:
`c95e101492554f7b2b8e3d600e8a53ba8df67bbd66bbb6c891f44d051bd69094`.
This qualifies the documented regression profile; it is not an A/B speedup claim
or a guarantee that each individual frame completes in 16.67 ms.

## Results

All five source/readiness tasks are complete. Correctness review covers the five
platform outputs, safe no-overwrite packaging, matching versions, checksums, and
Web behavior under the repository prefix. Coherence review confirms only version
metadata changed beyond the previously verified implementation. No unresolved
readiness issue was found. This is a versioned delivery of already specified
behavior; no delta spec or behavioral documentation change is required. The
existing platform-builds and web-publication contracts remain current.

Archive readiness does not claim a future public rollout. After the source commit,
delivery must still upload all five assets/checksums, publish v1.0.11, wait for Pages,
verify the public release identity, and exercise live gameplay. Those outcomes
belong in the release notes and linked Actions runs.

## Limitations

- No physical Windows/Linux/Android gameplay, full 13-level playthrough, audible
  audio confirmation, or multi-GPU switching matrix. Browser performance evidence
  is limited to the documented reference fixture on Apple M3.
- Android remains test-signed. macOS remains ad-hoc signed without notarization;
  this is not a mobile store submission. iOS remains deferred.
- The existing Web resolution selector can show an unknown label; actual canvas
  size is measured independently. Browser/OS power policy may ignore GPU preference.
