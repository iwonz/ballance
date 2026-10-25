# Verification: improve-web-frame-performance

Date: 2026-10-25. Source baseline: `c28ece4099088d2c0dcf72c03b06732b2a08bfa7`.

| Dimension | Assessment |
| --- | --- |
| Completeness | 7/7 tasks complete; both added requirements implemented |
| Correctness | All four delta scenarios covered by the checks below |
| Coherence | Design followed; no critical discrepancy; measurement limits retained |

## Checks

- `python3 scripts/test-web-performance.py`: all 10 tests passed, including sustained slowdown, tail latency, boundary freezes, overloaded GPU, missing/invalid data, software rendering, and CLI exit status. Synthetic 60/120/144 FPS and 59.94 Hz cases pass; these are checker tests, not physical display measurements.
- `node --check scripts/measure-web-performance.js`, `python3 scripts/check-agent-context.py`, and `openspec validate --all --strict --no-interactive`: passed (13 capabilities and this change).
- `python3 scripts/test-agent-context.py` (5 tests), `python3 scripts/test-release-tools.py` (9 tests), and `python3 scripts/check-physics-abi.py` (89 delegates): passed.
- `actionlint .github/workflows/macos-apple-silicon.yml` and `git diff --check`: passed.
- `python3 scripts/build-physics.py WebGL`: rebuilt the pinned IVP archive and passed the native Wasm smoke, ABI 2302, including gravity/collision settling, constraints, vectors, and cleanup.
- Unity 6000.6.7f1 batch build, `-buildTarget WebGL -executeMethod Ballance2.Editor.PlayerBuild.Build -customBuildPath Builds/checks/performance-WebGL`: passed with all required packages and levels 01-13. Log: `Logs/performance-WebGL.log`. `scripts/package-build.py`'s `validate_web` passed on that output. Generated compiler configuration confirms full LTO, Wasm 2023, native exceptions, SIMD, and native longjmp.
- Hardware captures used `playwright-cli run-code --filename scripts/measure-web-performance.js`, followed by `python3 scripts/check-web-performance.py <report>`; executable commands and budgets are owned by [the validation guide](/docs/validation.md#web-frame-performance).
- Optimized Web gameplay smoke exercised movement, falling/respawn, pause, confirmed restart, tutorial dismissal after restart, and confirmed return to the menu. Browser automation and manual Playwright actions were combined; this was not a fully unattended gameplay suite.
- A separate fresh browser context replaced `Core/core.ballance` with an invalid ZIP response. The optimized player caught `Cannot find central directory`, reported `SystemPackageLoadFailed`, and displayed its error UI. This exercises managed exception handling, not only an HTTP failure.

## Results

### Requirement coverage

| Requirement / scenario | Evidence |
| --- | --- |
| Reproducible Web frame performance gate: slow/frozen renderer | Draw-based capture, frame/GPU budgets, adversarial self-tests, real slow-renderer control; capture boundaries detect end/start freezes |
| Unqualified measurement | Tests reject hidden/software/lost-context/disjoint/missing GPU data and invalid profile/identity; capture records actual GPU, pixels, visibility and idle cadence |
| Reproducible comparison | Two alternating before/after runs below, raw frame reports, build hashes and screenshots; hosted CI only validates tooling |
| Optimized Web release execution: startup/failure | Full player build, native ABI/smoke, live gameplay, corrupt-package catch/error UI, compiler browser requirements documented |
| Native refresh eligibility | Web startup retains the default browser-driven target; the 60 FPS assignment is mobile-only, debug mode is off, and the standalone SetFps component has no scene/prefab references |

Implementation follows the design: external instrumentation, preserved graphics quality and simulation frequency, explicit compiler optimization, and no new runtime dependency. No critical requirement or design discrepancy was found. The coverage limits below remain explicit.

### Hardware measurements

Apple M3, Chrome 155.0.8059.85, ANGLE Metal; visible headed browser, 1280x900 viewport, DPR 2, actual 1920x1200 canvas, fresh default High quality. Idle browser calibration was approximately 60 Hz. Each scenario had 10 seconds of warm-up plus 30 seconds of recording; builds had finished before the comparison. The gameplay fixture samples the unpaused level 01 start area after a brief input; screenshots show the ball rolling down the initial ramp.

All raw files below are in ignored `Builds/checks/output/playwright/`; each identifier refers to `performance-<identifier>.json` and matching before/after screenshots. The table retains the failed baseline run.

| Build / report | Scenario | FPS | p95 / p99 interval, ms | Maximum interval, ms | GPU p95, ms | Verdict |
| --- | --- | ---: | ---: | ---: | ---: | --- |
| Baseline / 1792926232660 | Menu | 59.60 | 18.3 / 19.0 | 217.6 | 3.68 | Fail |
| Baseline / 1792926232660 | Level 01 | 59.96 | 18.0 / 18.9 | 29.2 | 3.88 | Pass |
| Optimized / 1792926344653 | Menu | 59.96 | 18.3 / 18.8 | 27.7 | 3.68 | Pass |
| Optimized / 1792926344653 | Level 01 | 59.97 | 18.6 / 19.3 | 27.5 | 3.78 | Pass |
| Baseline / 1792926476456 | Menu | 59.96 | 18.3 / 18.9 | 29.6 | 3.69 | Pass |
| Baseline / 1792926476456 | Level 01 | 59.96 | 19.0 / 19.9 | 29.7 | 3.78 | Pass |
| Optimized / 1792926602367 | Menu | 59.97 | 18.4 / 19.2 | 22.2 | 3.71 | Pass |
| Optimized / 1792926602367 | Level 01 | 59.96 | 18.6 / 19.4 | 29.8 | 4.18 | Pass |
| Public v1.0.10 / 1792926889012 | Menu | 59.96 | 18.1 / 18.9 | 24.2 | 3.68 | Pass |
| Public v1.0.10 / 1792926889012 | Level 01 | 59.96 | 18.5 / 19.2 | 33.5 | 3.74 | Pass |

The baseline menu's 217.6 ms gap had a 3.4 ms measured CPU callback and 6.70 ms GPU work. Its cause is not established, and it did not repeat in the second baseline run. Do not attribute its disappearance to this optimization. Public level 01 had one interval above 33.34 ms (0.056%), within the documented tolerance. Both optimized runs passed all budgets with zero such stalls; GPU p99 was at most 4.37 ms. CPU p95 ranged 2.9-3.6 ms before and 2.8-3.4 ms after, without a consistent demonstrated speedup.

The public Pages test is separate from the local comparison: v1.0.10 targets `1b76c4fe7bbdeee650afed4011661ce561ff6896`, preceding the c28ece4 runtime refactor. These local test builds also retain the 1.0.10 product version, so Wasm hashes identify the actual binaries:

- Baseline: `c029ee2a2a09ce7828481584c84e79f8d0d545869945a51f76e6593ab2c4d81f`.
- Optimized: `3668d1461dfade1847364ed05397da72f83fa8953e5a2178fa79eaf1b5dc1dd9`.
- Public Pages: `c063a180a51a44b74523427e6e52a00d1c196838ee0991ad065e9e39f85e2bba`.

The final negative control, report 1792927218466, used the same capture and checker with an external wrapper delivering every second animation callback during each capture only. Idle calibration remained 60.00 Hz. Both scenarios rendered 899 frames in 30 seconds (29.97 FPS), with p95 intervals of 34.8/35.3 ms, and failed the budget with checker exit 1 despite GPU p95 below 4 ms.

Setup reports 1792925544503 and 1792925765662 are excluded from the comparison: the first lacked GPU timings, the second established that protocol, and both overlapped build work. The earlier 30 FPS control 1792925915037 preceded idle-cadence calibration and is not evidence for the final report schema. An intermediate control attempt, 1792927043251, also slowed tutorial input and timed out waiting for Resume; it produced screenshots but no completed report or passing verdict. The final control restricts slowdown to measured intervals so navigation is unaffected.

### Build and native compatibility

Compressed Wasm decreased from 17,846,629 to 16,022,719 bytes (10.22%). Complete Web output decreased from 85,483,748 to 83,754,217 bytes (2.02%). These are artifact sizes, not download-time or FPS speedup claims.

The first Wasm 2023 link failed on `emscripten_longjmp` in the old IVP convex-builder archive. Rebuilding the same pinned source revision `47533475e01cbff05fbc3bbe8b4edc485f292cea` and existing patch with `-sSUPPORT_LONGJMP=wasm -msimd128` resolves those references to the native ABI. Archive SHA-256: `27f65674931a6e0ed8506a7de70ced6d860a1ffcfac22d6e04bfcfd5badabe08`. No C/C# ABI, physics algorithm, timestep, save format, or asset GUID changed.

## Limitations

- The 60 FPS target explicitly permits scheduling/59.94 Hz tolerance. These results do not promise every frame below 16.67 ms or stable 60 FPS on all hardware. The reference browser was refresh-limited; greater-than-60 game performance on a high-refresh display was not measured.
- Captures cover the menu and level 01 start area, not all thirteen levels, heavy later sectors, long sessions, or mobile browsers. Native Windows/Linux/macOS/Android FPS and other browser engines were not qualified; iOS remains deferred.
- External GPU queries and callback instrumentation add measurement overhead. Render submission/GPU work is not physical presentation latency, and callback CPU duration is not a zero-overhead Unity profiler result.
- The isolated baseline hitch remains unexplained. The public version did not show sustained low FPS on this reference machine; reproducing a player's different device/browser/scene is still needed to diagnose that report.
- Existing upstream native compiler warnings remain; this change does not audit the IVP implementation. Generated outputs, raw captures and screenshots are local ignored evidence, not committed release assets.
- This is a source/tooling delivery with a verified local Web build. It does not publish a new binary release or replace public v1.0.10 Pages. Any later release must deploy its matching Web archive under the existing release workflow.
