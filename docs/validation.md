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
then invokes the virtual Input System and localization checks. The iOS availability check is not an
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

## Native physics

On Apple Silicon with Xcode Command Line Tools:

```sh
xcrun clang++ -arch arm64 -std=c++11 -Wall -Wextra -Werror \
  scripts/physics-smoke.cpp -o /tmp/ballance-physics-smoke
/tmp/ballance-physics-smoke Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib
```

The smoke test checks ABI 2302, vectors, a gravity/collision simulation, a fixed
constraint, and cleanup. `check-physics-abi.py` compares the managed signatures with
the bridge declarations. Neither proves every exported function or target works.
The Web native rebuild runs an equivalent Wasm smoke before installing its output.
Use the [release guide](build-and-release.md) for Android/Web rebuild commands.

## Player and browser smoke

Use the content-aware build commands, then the corresponding packaging command.
Check menu rendering and sound, load level 01, move the ball, reach a checkpoint,
fall and respawn, pause/resume, restart, and return to the menu. Exercise the specific
mechanism/level affected by the change. Full 13-level completion is a separate test.

For Web, serve over HTTP beneath a prefix such as `/ballance/`; test packaged
release content as well as build output when publication code changes. Observe
network failures and browser/Unity logs. Check user-initiated audio playback;
an AudioContext or source counter alone does not establish audible sound.
For Pages, wait for the workflow's public `release.json` check, verify its tag matches
the published release, and test the deployed player. A release is not fully delivered
until Pages serves that version successfully.

For Android, signature, zipalign, native LOAD alignment, and APK content inspection
are packaging checks. They do not replace touch, audio, lifecycle, or gameplay on a
physical device. Likewise, Windows/Linux build success does not prove runtime play.
The current macOS release is ARM64 and ad-hoc signed; it is not notarized.

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
