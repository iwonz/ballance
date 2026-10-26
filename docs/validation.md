# Validation guide

Select checks from the changed capability and its callers. Record actual commands,
results, and untested limits in the OpenSpec change's `verification.md`. A spec
scenario is an acceptance condition, not a claim that an automated test exists.

## Fast repository checks

From the repository root, with Python 3.12+ and the pinned OpenSpec CLI:

```sh
openspec validate --all --strict --no-interactive
python3 scripts/check-agent-context.py
python3 scripts/test-agent-context.py
python3 scripts/check-localization.py
python3 scripts/check-physics-abi.py
python3 scripts/test-release-tools.py
python3 scripts/test-install-macos.py
python3 scripts/test-web-performance.py
node --check scripts/measure-web-performance.js
node scripts/test-web-lifecycle.js
bash -n scripts/build.sh scripts/build-macos.sh scripts/setup-openspec.sh
```

Before delivering a completed change, also run:

```sh
python3 scripts/check-agent-context.py --base <base-commit>
```

The base check compares the delivered diff with that commit and requires a completed
archived change record. Use the branch base, not an arbitrary commit that hides
part of the change. Run `actionlint` when editing workflows. The existing release
checks exercise successful Web preparation plus corruption, path traversal, symlink,
and incomplete-content rejection. Context self-tests cover its own failure paths.

## Check selection

| Changed area | Required evidence |
| --- | --- |
| Documentation, comments, instructions | Context/language/link checks, strict specs; review meaning and scope |
| Validation script or workflow | Its self-tests, syntax/lint, success and failure cases, consistent CI invocation |
| Shared C# runtime | Unity compile, relevant editor checks, player smoke through affected flow |
| Levels, bundles, package paths | Target content build; valid/missing content, preview, load/unload/reload |
| Ball/sector/physics behavior | ABI/native checks where relevant; movement, checkpoint, fall/rebirth, pause, restart |
| Native API or binary | Both C/C# signatures, target toolchain provenance, target smoke, packaged player |
| Input | Virtual device checks plus physical input on each affected platform |
| UI/localization | Python and editor checks, rendered text/layout, navigation and focus, language restart |
| Persistence | Temporary profile/files, save/reload, compatibility and invalid-data behavior; protect real saves |
| Packaging/Pages | Archive checks, checksums, permissions/signatures where relevant, HTTP subpath browser smoke |

Do not run every player build for a comment-only edit if compilation and a lexical
comparison show executable source is unchanged. New behavior, changed compiler
branches, or a failed check justifies broader validation.

## Unity editor checks

Close the editor using this project first. Example on macOS:

```sh
UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" \
  -buildTarget StandaloneOSX -executeMethod Ballance2.Editor.BuildChecks.Run \
  -logFile /tmp/ballance-build-checks.log
```

`BuildChecks.Run` checks six installed target modules and HTTP/jar/file path handling,
then invokes virtual Input System, localization, modernization, and graphics checks. The iOS availability check is not an
iOS export or release. If the optional module is absent, report that prerequisite
and run relevant checks individually instead of claiming the aggregate passed.

`Ballance2.Editor.InputChecks.Run` exercises supported key mapping, held keyboard
state, mouse position/scaling, and touch state on virtual devices. It is not a
real-device or full frame-timing test.

`Ballance2.Editor.LocalizationChecks.Run` can also run separately to validate runtime
XML, fallbacks, Cyrillic coverage, and prefab bindings. For visual previews, use
`Ballance2.Editor.LocalizationChecks.Render` **without** `-nographics`; it writes
`Builds/checks/localization` and checks tutorial fit. These checks preserve existing
player preferences. Always inspect the log for compilation errors and the method's
completion message, not just whether Unity started.

`Ballance2.Editor.ModernizationChecks.Run` uses temporary files and objects to check
UTF-8/file URL round trips, short headers, legacy package metadata and translation
order, ZIP CRC/length/size rejection, archive stream ownership, streamed writer
allocation, indexed metadata reads, and pool capacity/ownership/clear/disposal.
It also checks the package page's inactive row template and ScrollView overflow cleanup. Small async reads complete in the
editor check; the frame-yielding path for large bundles must be exercised in a
player, particularly Web. The allocation/read-byte checks are bounded fixtures,
not whole-game FPS or peak-memory benchmarks. Display ratio formatting and saved
setting keys are reviewed at their UI/startup callers; physical monitor mode
switching still needs representative hardware.

`Ballance2.Editor.GraphicsChecks.Run` deliberately changes API/power preferences,
verifies that the content-aware build policy restores them for the five release
targets, and restores prior values. It checks recognized software names, missing
device information, prefab wiring, literal text, and four-language text fit. These
are configuration/UI checks, not evidence that a multi-GPU system chose a discrete
GPU. Inspect the rebuilt player's graphics settings and back navigation; use a
separate software-rendered browser session to exercise the warning, then close it
before any hardware FPS capture.

## Native physics

On Apple Silicon with Xcode Command Line Tools:

```sh
xcrun clang++ -arch arm64 -std=c++11 -Wall -Wextra -Werror \
  scripts/physics-smoke.cpp -o /tmp/ballance-physics-smoke
/tmp/ballance-physics-smoke Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib
```

The Remaster smoke test checks ABI 2304, vectors, gravity/collision, a fixed
constraint, unchanged dry contact behavior, wet material pairs, local ice,
three-ball rolling resistance/wind, transformed supports, and cleanup.
`check-physics-abi.py` compares the managed signatures with
the bridge declarations. Neither proves every exported function or target works.
The Web native rebuild runs an equivalent Wasm smoke before installing its output.
Use the [release guide](build-and-release.md) for Android/Web rebuild commands.

`Ballance2.Editor.EnvironmentChecks.Run` validates the thirteen profile inventories,
continuous/pause/checkpoint state transitions, records and preference isolation,
render dimensions, moving coverage regions, shared-score migration, side vegetation,
generated rain samples, and shader compilation. It does not establish level
completion or visual/audio quality. `test-web-lifecycle.js` checks save flushing,
the automatic IDBFS queue, restart/exit signals, timeout and write failures; exercise
the actual host navigation and persisted profile in a rebuilt browser player too.
`Ballance2.Editor.EnvironmentChecks.CheckShutdown` creates a settled native contact,
destroys the global physics API, and checks body/world cleanup plus component
teardown without live services. Also quit a native player with a level loaded;
the fixture cannot validate platform render-target presentation or OS quit events.

## Player and browser smoke

Use the content-aware build commands, then the corresponding packaging command.
Check menu rendering and sound, load level 01, move the ball, reach a checkpoint,
fall and respawn, pause/resume, restart, and return to the menu. Exercise the specific
mechanism/level affected by the change. Full 13-level completion is a separate test.

For Web, serve over HTTP beneath a prefix such as `/ballance/`; test packaged
release content as well as build output when publication code changes. Observe
network failures and browser/Unity logs. Check user-initiated audio playback;
an AudioContext or source counter alone does not establish audible sound.
For Pages, wait for the workflow's public `release.json` check, verify its distribution tag and both edition pins
match the published release, and test both deployed players and switch links. A release is not fully delivered
until Pages serves that version successfully.

For Android, signature, zipalign, native LOAD alignment, and APK content inspection
are packaging checks. They do not replace touch, audio, lifecycle, or gameplay on a
physical device. Likewise, Windows/Linux build success does not prove runtime play.
The current macOS release is ARM64 and ad-hoc signed; it is not notarized.

## Web frame performance

Use [measure-web-performance.js](../scripts/measure-web-performance.js) with
**Playwright CLI 0.1.22** and a headed Chrome browser on an otherwise idle machine.
Serve a complete content-aware Web build over HTTP. The script creates an isolated
profile, measures the animated menu and the unpaused level 01 start area, and closes
that profile without changing real player saves. It skips the tutorial and briefly
moves the ball before the gameplay capture. This is not a complete level playthrough.

Example from the repository root, with the server running in another terminal:

```sh
python3 -m http.server 8010 --bind 127.0.0.1 --directory Builds
```

Run the browser commands from an ignored artifact directory:

```sh
mkdir -p Builds/checks
cd Builds/checks
npx --yes @playwright/cli@0.1.22 --session fps open http://127.0.0.1:8010/WebGL/ --headed
npx --yes @playwright/cli@0.1.22 --session fps run-code --filename ../../scripts/measure-web-performance.js
# Use the exact JSON path printed by the capture command:
python3 ../../scripts/check-web-performance.py output/playwright/performance-<timestamp>.json
npx --yes @playwright/cli@0.1.22 --session fps close
```

The reference profile is a 1280x900 browser viewport, DPR 2, a **1920x1200** Unity
canvas, fresh default High quality, and a visible hardware-accelerated tab on a
display capable of at least 60 Hz, with the WebGL GPU timer extension available.
Keep the browser foregrounded and avoid concurrent
builds or other GPU workloads. Do not force SwiftShader. Repeat captures at least
twice on the same hardware/browser for before/after comparisons. A run that fails
must remain in the evidence; investigate interference instead of selecting only
successful runs. Screenshots before/after each capture verify the intended state.

An initial idle-page calibration records browser refresh cadence; a cadence below
59 Hz cannot qualify for the 60 FPS target. Each scenario warms up for 10 seconds
and records for 30 seconds. The JSON contains
the Wasm SHA-256/build version, browser, GPU, actual WebGL context power preference,
canvas dimensions, visibility,
draw counts, frame timestamps, CPU callback times, and asynchronous GPU elapsed
times. Only animation callbacks
that submit draws to the Unity canvas count as frames. GPU queries must resolve
without disjoint timing; missing results cannot pass. CPU callback time and GPU
elapsed time are separate measurements and must not be added together. This is
render-submission cadence plus GPU work, not physical display latency.
Instrumentation runs only in the test browser, with no shipped
per-frame profiler overhead. Loading and screenshot readbacks are outside captures.

[check-web-performance.py](../scripts/check-web-performance.py) exits nonzero for
unqualified/incomplete data or a failed **60 FPS target with scheduler tolerance**:

| Metric | Required budget |
| --- | --- |
| Average FPS over the entire capture | At least 59 (allows 59.94 Hz and scheduling jitter) |
| p95 frame interval | At most 20 ms |
| p99 frame interval | At most 25 ms |
| Intervals above 33.34 ms | At most 0.1% |
| Longest interval, including capture boundaries | At most 100 ms |
| GPU p95 / p99 elapsed time | At most 16.67 / 20 ms |
| GPU frames above 33.34 ms / longest GPU frame | At most 0.1% / 100 ms |

These are explicit regression tolerances, not a claim that every frame finishes in
16.67 ms. No cap prevents higher FPS on faster displays. Software renderers, hidden
tabs, lost contexts, invalid GPU timings, changed resolution, invalid timestamps, absent draws, and
missing scenarios cannot pass. Hosted CI runs the checker's adversarial self-tests
and JavaScript syntax check; **it does not run or certify hardware game FPS**.
Report schema 2 additionally requires `powerPreference: "high-performance"`; missing,
default, or low-power values fail reference-profile qualification. The preference
does not prove which GPU the browser selected, so hardware-name and GPU-timing
checks remain required. Schema 1 reports are historical evidence and need a new
capture for the current gate; do not relabel old reports to make them pass.
Repeat real measurements on intended player hardware and heavier level sections
before extending the performance claim beyond this reference fixture.

## Evidence boundaries

Release 1.0.9 was built for all five current targets. Its previous validation included
Web menu/level 01 movement, fall/rebirth and menu return, macOS startup/native physics,
Android archive/signature/alignment checks, and Windows/Linux archive checks.
It did not include real Android gameplay, Windows/Linux gameplay, or all-level
playthroughs. Treat those as historical observations, not tests rerun by a later change.

Known baseline gaps are linked from [architecture.md](architecture.md), including
legacy NMO, package trust, persistence robustness, and unfinished editor paths.
Record a regression or newly reproduced defect in a focused OpenSpec change instead
of silently changing the baseline to promise behavior the code does not provide.

## Remaster environment and edition checks

The ongoing Original/Remaster change has its own [verification record](../openspec/changes/build-original-and-remaster-editions/verification.md).
Keep incomplete acceptance items open even when compilation and model tests pass.

```sh
python3 scripts/check-physics-abi.py
python3 scripts/test-release-tools.py
python3 scripts/test-install-macos.py
python3 scripts/check-localization.py
```

With the exact Unity editor and a closed project, run
`Ballance2.Editor.EnvironmentChecks.Run` for profiles, blends, checkpoint/record
state, settings isolation, ABI layouts and resolution calculations. Run
`Ballance2.Editor.RemasterUIVisualChecks.Run` to render the main/level menus in all
four languages at portrait, Full HD, QHD and 4K. Inspect the generated images in
`Builds/checks/remaster-ui`; nonzero rectangles and glyph coverage are not visual
or keyboard acceptance. The interactive graphics page, all other pages, tutorial
and physical touch still need player checks.

`Ballance2.Editor.RemasterRuntimeChecks.Run` enters play mode through the normal
main scene, renders the diagnostic console, and exercises the native world, three ball bodies, all sector
activations, pause, interrupted preset selection, fall recovery, restart and
return to the menu. Invoke it with graphics enabled and **without `-batchmode`,
`-nographics` or `-quit`** (the loader waits for rendered end-of-frame events);
the runner exits the editor with a success/failure code. Optional `-checkLevel
level01 -checkPreset Standard` arguments select one scenario while diagnosing a
failure; the default visits all 13 by 7 combinations. Reports and temporary game
files are retained under a new `Builds/checks/runtime-<timestamp>` directory.
Editor-only preference and file-path overrides keep the player's actual profile
untouched, and temporary preference keys are removed at exit. A known Unity
Editor search-index exception is retained separately from game errors. These
directed checks explicitly set sectors and ball types: they do **not** count as
91 full playthroughs, transformation-platform interactions or performance evidence.

For a native flat-course replay:

```sh
python3 scripts/measure-physics.py .worktrees/original/Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib --output Builds/checks/original-replay.json
python3 scripts/measure-physics.py Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib --output Builds/checks/remaster-replay.json --compare Builds/checks/original-replay.json
```

The harness uses the three shipped parameter sets, raw paper OBJ vertices, a 66 Hz
IVP tick and 30/60/120/144 Hz frame grouping, with three repetitions each. It writes
source/library hashes and refuses to overwrite measurements. Its tolerance comes
from baseline repeatability before comparing the new library. This excludes Unity
input sampling, imported mesh conversion, camera and mechanisms; it cannot replace
the required player replay or 91 full level/preset runs.

The reference PC target is RTX 3060 12 GB, Core i5-12400, 16 GB RAM, Windows 11,
High quality, Full HD at 60 FPS. It remains an unmeasured target. Retain the Apple
M3 gate above as additional evidence; measure QHD/4K separately. Record actual
internal render dimensions, quality, preset, sector, device/API, frame percentiles,
1% low method, CPU/GPU, memory and cold loading.

For publication, run `prepare-release.py verify` against the complete unified
release: all ten pinned artifacts, sidecars, manifest, notices and attached site.
It compares the site against `prepare-site.py` output from the immutable release
source. Check the distribution and both public identities with `verify-site.py`. Use an isolated browser profile to test
Original progress at the old root URL, reload it at `original.html`, then verify
Remaster reset cannot alter it. A two-player HTTP success does not establish input,
recovery, save compatibility or independent release rollback.

## Remaster visual authoring checks

Use the exact Unity editor with graphics enabled for `Ballance2.Editor.RemasterUIVisualChecks.Run` (localized screens in their runtime-width containers), `Ballance2.Editor.RemasterReliefAuthoring.Generate` (reproducible linear relief imports and periodic-edge checks), and `Ballance2.Editor.RemasterArtChecks.Render` (moving-light materials, all cloud qualities, stock palettes, headings and a non-repetition camera translation). `RenderClouds` regenerates only the density volume before rendering the sky fixtures. Check cached sky orientation, resize, source-material restoration and level unload in native and Web players. Preview output lives under `Builds/checks/`; it is generated evidence, not maintained source. Inspect the images and actual players; a successful renderer or shader compile does not establish visual quality, menu input behavior or a frame budget.
