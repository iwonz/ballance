# Verification record

## Checks

Verified on 2026-10-30 with Unity 6000.6.7f1.

- `Ballance2.Editor.BuildChecks.Run` passed: resource paths, installed target modules, localization, virtual mobile input, Android saved bindings and other repository build checks. Final log: `/private/tmp/ballance-mobile-release-checks-latest2.log`.
- Rendered `Ballance2.Editor.MobileInputChecks.RunPrefabIntegration` passed (`/private/tmp/ballance-mobile-card-final2.log`). Real tutorial/keypad/menu prefabs, Canvas, GraphicRaycaster and EventSystem verify Continue hit ownership above swipes, text-scroll routing, analog dispatch and release/cancel/menu cleanup. Five saved presets at 320x522, 412x793, 568x274 and 844x344, each at minimum/default/maximum requested size, verify separate control/card regions, wrapped action labels and 48-logical-pixel targets.
- Rendered `Ballance2.Editor.MobileTutorialChecks.Run` passed (`/private/tmp/ballance-mobile-tutorial-lifecycle3.log`, generated `Builds/checks/tutorial-*/report.txt`). The isolated real first-level fixture invokes bound Continue/Skip buttons and authored extra-life/transformation callbacks. It verifies introductory stages, rapid duplicate input, exact pause snapshots on each stage, paused spawn, later Continue/Skip, camera-stage skip, fall/pause/recovery, restart during spawn and exit cleanup. This is directed integration, not manual traversal to every trigger.
- `python3 scripts/check-agent-context.py`, `test-agent-context.py`, `check-localization.py`, `check-physics-abi.py`, `test-release-tools.py`, `test-web-performance.py`, `node scripts/test-mobile-tilt.js`, strict OpenSpec validation and `git diff --check` passed. Counts: 316 Russian strings, 89 ABI delegates, 5 context tests, 17 release-tool tests and 11 Web-performance-tool tests. Performance-tool self-tests do not measure phone performance.
- Content-aware WebGL build and `bash scripts/build.sh` for macOS, Windows, Linux and Android passed. `scripts/package-build.py` produced all five version 1.0.14 archives and SHA-256 sidecars. Archive CRCs/content, identity, version, Linux executable mode and checksums passed. Web and native runtime-source hashes match. The subsequently added lifecycle verifier is editor-only.
- Android APK inspection passed: version 1.0.14/code 6, ARM64-only native libraries, v2 signature, `zipalign -P 16` and all six native libraries with LOAD alignment at least 16384. Windows/Linux executables and physics libraries are x86-64. Packaging ran the macOS native physics smoke (ABI 2302); canonical installation and player startup to MenuLevel passed.

## Results

All five changed behavioral requirements are implemented and covered:

| Requirement | Implementation and evidence |
| --- | --- |
| Permission-aware touch priority | MobileControls and keypad controllers; real raycasts, browser Continue, held joystick, release/cancel and paused layout changes |
| Pause and resume | GamePlayManager and lightning lifecycle; browser pause during spawn/fall and directed snapshot/cancellation checks |
| Single tutorial progression | TutorialController; bound buttons share keyboard/gamepad handlers, debounce, owned listeners, later hints and restart/exit checks |
| Responsive touch guidance | TutorialCardLayout and MobileUILayout; real original assets, four sizes, text scrolling and fixed action row |
| Compact gameplay menu | MobileControls, MobilePauseLayout and MobileMenuList; touch settings/restart/exit/resume, original menu scrolling and preserved theme |

Chrome 156 and actual WebKit 26.6 were exercised with mobile viewports; WebKit was not replaced by a Chrome User-Agent. Continue progressed through camera, overhead and movement without skipping or keyboard input. Chromium additionally exercised held movement with camera input, repeated taps, settings/center-layout changes, rotation, pause during spawn/fall, recovery and movement along the start path, bridge and spiral. WebKit completed the same introductory Continue sequence with pause/resume on every stage and during spawn, 320-pixel portrait and 568-pixel landscape resizing, and joystick taps that visibly displaced the ball. Screenshots are under `/private/tmp/final-Ballance-*` and `/private/tmp/release-Ballance-webkit-*`.

The joystick is deliberately hidden while movement is forbidden by a tutorial or lightning recovery. It returned after recovery and moved the ball. The shortened sensor-unavailable label was inspected at 320-pixel width; it stays above the card/control area. WebKit with zero reported maxTouchPoints still receives the mobile layout through its touch/coarse-pointer capability. Shared sensor-modal behavior was also checked in the final Reballance Chromium player; Node checks cover opt-in, denial, late permission responses, stale data and cleanup.

Earlier checks exposed center-layout/card overlap and clipped menu labels; those defects were fixed and the checks passed. Initial lifecycle-fixture failures were test-profile setup and an overstrict object-destruction expectation; final assertions check removed cards, key listeners and trigger callbacks. Unity's editor search-index exception and an earlier Analytics shutdown crash were editor diagnostics, not player gameplay exceptions. Successful isolated reruns are recorded above. Generated editor asset/settings churn and the staging meta GUID were restored; the pre-existing .DS_Store change is excluded.

Release delivery uses the five verified archives, matching source tag and matching Pages player under the existing URL. Upload digests and public browser behavior are checked after publication; this record documents pre-publication source/build acceptance, not a claim that hosted CI tests Unity gameplay.

## Limitations

No physical Android/iPhone, real sensor, device ergonomics, audible-output check or budget-phone frame-rate claim. `adb devices -l` found no device; no local Android emulator was available. Native Windows/Linux execution and all thirteen complete levels were not tested. Browser viewport emulation and directed trigger callbacks are separate from physical-device/full-playthrough evidence. Pointer dragging and prefab EventSystem checks cover scroll routing; they do not certify physical finger scrolling on Safari. Android uses test signing; macOS is ad-hoc signed without notarization. Native iOS remains excluded.
