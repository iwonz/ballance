## Checks

- Unity 6000.6.7f1, `-batchmode -nographics -quit -buildTarget WebGL
  -executeMethod Ballance2.Editor.GraphicsChecks.Run`: passed, exit 0
  (`Logs/graphics-checks.log`). Deliberate local overrides were restored by the
  build policy for Windows, Linux, macOS, Android, and Web. Available APIs were
  Direct3D12/11, OpenGLCore/Vulkan, Metal, Vulkan/OpenGLES3, and OpenGLES3 respectively.
- The same editor check passed recognized-software and missing-device cases,
  prefab binding, literal non-interactive text, and bounded text height across
  English, Russian, Simplified Chinese, and Traditional Chinese.
- `python3 scripts/test-web-performance.py`: 11 tests passed, including missing,
  default, and low-power context preferences, historical schema rejection, and
  the existing software-renderer/frame-budget rejection cases.
- `node --check scripts/measure-web-performance.js`: passed.
- `python3 scripts/check-localization.py`: passed (298 Russian translations,
  12 resources).
- Unity 6000.6.7f1 content-aware Web build via `PlayerBuild.Build`, output
  `Builds/checks/gpu-WebGL`: passed, exit 0; all 13 levels included
  (`Logs/gpu-WebGL.log`). `package-build.py`'s `validate_web` passed.
- Headed Chrome settings/navigation checks passed in an isolated hardware context
  (Apple M3 through ANGLE/Metal) and a separate actual SwiftShader browser launched
  with `--use-angle=swiftshader --enable-unsafe-swiftshader`. Both reported
  high-performance, but only SwiftShader displayed the localized software warning.
  Device names wrapped, controls remained visible, and Back returned to Settings.
  Screenshots: `Builds/checks/output/playwright/gpu-{hardware,software}-graphics.png`
  and matching `-back.png` files. The software browser was closed before FPS capture.
- `playwright-cli --session gpu run-code --filename scripts/measure-web-performance.js`
  followed by `python3 scripts/check-web-performance.py
  Builds/checks/output/playwright/performance-1792928346332.json`: passed both
  scenarios. Headed Chrome 155.0.8059.85, Apple M3 via ANGLE/Metal, High quality,
  1920x1200 canvas at DPR 2, visible tab, measured browser cadence 60.002 Hz.
  Both actual contexts reported high-performance. Before/after screenshots show
  the animated menu and unpaused level 01; the score continued decreasing.
- `openspec validate --all --strict --no-interactive`,
  `python3 scripts/check-agent-context.py`, and `git diff --check`: passed before
  archive (14 OpenSpec items, 13 capabilities).
- After synchronization/archive, strict validation passed all 13 specs and the
  context delivery check passed against base
  `5ce0615fa9f2e746ad2107e0d030856ab84bd052`. All three archived additions match
  their current capability specs exactly. All five tasks are complete.

## Results

| Requirement | Implementation and evidence |
| --- | --- |
| Performance-oriented graphics selection | `PlayerBuild.ConfigureGraphics` runs in the content-aware builder; `GraphicsChecks` verifies restoration after deliberate API/power overrides for all five release targets. |
| Reported rendering device | `GraphicsDeviceInfo` refreshes on page activation in the existing graphics settings prefab. Editor checks cover long hardware/software names, unknown state, four languages, and literal text. Hardware/SwiftShader settings and Back navigation passed in the actual player. |
| Web GPU preference evidence | Capture schema 2 records the actual context preference; the checker rejects anything other than high-performance while retaining renderer/timing qualification. Adversarial self-tests and the actual hardware capture pass. SwiftShader's high-performance context demonstrates why the preference alone is insufficient. |

| Scenario (30 seconds after warm-up) | FPS | Frame p95 / p99 / max (ms) | GPU p95 / p99 / max (ms) | Frames over 33.34 ms |
| --- | ---: | --- | --- | ---: |
| Animated menu | 59.96 | 18.50 / 19.00 / 20.40 | 3.63 / 3.85 / 6.59 | 0 |
| Unpaused level 01 start area | 59.96 | 18.50 / 19.30 / 30.50 | 3.82 / 4.11 / 4.52 | 0 |

Measured Wasm SHA-256:
`c95e101492554f7b2b8e3d600e8a53ba8df67bbd66bbb6c891f44d051bd69094`.
The unchanged development version string is 1.0.10, but this hash identifies the
new local build, not the published release. The report and screenshots remain
local ignored evidence, not release assets. This was a regression qualification,
not an A/B comparison or a claim of 60 FPS on every individual frame.

The implementation preserves saved graphics settings and native IVP behavior.
No dependency, runtime polling, CPU/GPU switch, or rendering-pipeline migration was
introduced. The prior serialized Web preference was already HighPerformance;
this change enforces it during builds and does not establish a new FPS gain.

Correctness and coherence review: all three delta requirements and four scenarios
have implementation and check coverage. The change follows the existing build,
prefab/localization, and external-capture paths. No unresolved issue was found in
the requested scope; the pre-existing selector label is noted below.

## Limitations

- Automatic API selection is the engine's compatibility policy, not a measured
  per-driver ranking. Browser/OS power policy can ignore the Web preference.
- Software detection uses reported-name signatures. Unknown or concealed device
  information is not evidence of hardware acceleration.
- The existing Web resolution label still displays `Unknow %`, also visible in
  the earlier `output/playwright/08-graphics.png` baseline. This unrelated selector
  issue is recorded in the source map; capture validates actual canvas dimensions.
  Unity telemetry requests were refused in the test environment; they did not
  block the menu/settings flow and are not graphics failures.
- Desktop/mobile API configuration checks do not replace runtime testing on
  Windows, Linux, Android, or systems with multiple GPUs. WebGPU, discrete-GPU
  switching, higher-refresh displays, and heavier level sections are not tested.
- This is a source change, not a new published release. Public Pages remains the
  previously published v1.0.10 player until a complete release is delivered.
