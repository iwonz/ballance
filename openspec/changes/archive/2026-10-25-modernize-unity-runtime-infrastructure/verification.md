## Checks

Base: `1b76c4fe7bbdeee650afed4011661ce561ff6896`. Unity: `6000.6.7f1`.
Commands below use the repository's RTK prefix. Outcomes are recorded as completed;
the final player/content matrix is complete.

- Unity batchmode, StandaloneOSX, `-executeMethod Ballance2.Editor.BuildChecks.Run`:
  passed path/module, virtual input, localization, and modernization checks.
- `python3 scripts/check-agent-context.py`: passed.
- `python3 scripts/test-agent-context.py`: 5 tests passed.
- `python3 scripts/check-physics-abi.py`: 89 managed/native delegate declarations checked.
- `python3 scripts/check-localization.py`: 294 Russian translations across 12 resources passed.
- `python3 scripts/test-release-tools.py`: 9 tests passed.
- `openspec validate modernize-unity-runtime-infrastructure --strict --no-interactive`: passed.
- `git diff --check`: passed.
- Final archive synchronized all three requirements; exact delta comparison confirms prior requirements are preserved.
- `openspec validate --all --strict --no-interactive` after archive: 13 specs passed.
- `python3 scripts/check-agent-context.py --base 1b76c4fe7bbdeee650afed4011661ce561ff6896` after archive: passed.
- Removed script/DLL GUID scan against scene/prefab/other asset content: no references.
  Removed-helper caller search: no runtime references remain. RuntimeInspector's
  `ICSharpCode.` string is a vendor namespace exclusion, not a dependency.

## Results

### Review assessment

- Completeness: 8/8 tasks complete; final builds include the prefab and license changes.
- Correctness: all three added requirements have implementation and scenario evidence. The fractional-monitor scenario is source-reviewed, with physical switching explicitly untested.
- Coherence: implementation follows the revised design; no unresolved critical issue. Migration and hardware limits are listed below.

### Requirement coverage

| Requirement / scenarios | Implementation and evidence |
| --- | --- |
| Compatible bounded archive reads: legacy package | `GameZipPackage`, `ZipUtils`; editor fixture loads leading-slash metadata and preloads translations before reading BaseInfo. Canonical packer paths round-trip. |
| Invalid or oversized entry | Tests corrupt stored-entry bytes, alter declared lengths in both directions, exceed 512 MiB, and open an invalid archive. CRC/length/limit rejection and stream disposal pass. No partial bytes are accepted. |
| Large Web resource | Chunked native `Awaitable.NextFrameAsync` implementation; Web player loaded the large core and system bundles, reached menu and level 01, with native Awaitable continuations in the browser log. |
| Owned reusable list items: refresh/capacity/teardown | `GameObjectPool`, `GamePoolManager`, list callers, `ScrollView`; distinct prewarm/growth, limit, double Clear, duplicate/foreign return, shared-root isolation, idempotent disposal, embedded-template visibility, and scroll overflow/destruction tests pass. |
| Precise display refresh settings | Source review of `SettingsUIControl` and `GameManager`: ratio value formatted to two decimals (60000/1001 → 59.94), ratio passed intact with explicit FullScreenMode. Setting constants and saved indices are unchanged. Physical monitor switching is untested. |

Native Unity async-operation awaits replace the custom scheduler. All five startup
Task consumers wait for completion before observing the result, including fault and
cancellation propagation through `GetAwaiter().GetResult()`. Public package Task
contracts remain unchanged. Web success was observed through menu and level 01; intercepting Core/core.ballance with HTTP 404 produced the existing fatal-error screen and the expected system-package-info failure. It did not continue into gameplay.

The original full compile exposed 20 distinct obsolete API warnings in three vendor
tools. After narrow replacements, the final editor compile has zero CS0618 warnings
and no compilation errors. This does not mean every unrelated warning is eliminated.
The deprecated VS Code package is removed; Unity resolved the pinned ZIP package
without transitive dependencies. New license notices are included in the existing
license resource and UI list.

### Player and content checks

Both the initial and final content-aware matrix built macOS, WebGL, Android, Windows, and Linux successfully; commands were `bash scripts/build.sh <target>` for each of those five targets. New packages pass independent Python ZIP CRC checks and canonical metadata-path checks. Android also passes APK CRC inspection and contains ARM64 physics plus seven packages and thirteen level bundles; macOS, Windows, and Linux contain the same package/level inventory and the Unity ZIP assembly, without the old DLL. `package-macos.py` passed ARM64/native dependency validation, ad-hoc signing, archive extraction and signature/permission round-trip checks, including the native physics ABI 2302 smoke. The final matrix includes the embedded-template correction.

The Web test uses an isolated archive and checksum, processed by `prepare-pages.py` under `/ballance/`. Menu, level 01, arrow-key movement, pause, graphics settings, confirmation and menu return, mod list, and package details were observed. Injected HTTP 404 for core produces the expected error screen. No game-code exception appeared during the successful path; existing Unity cloud telemetry connection failures, Web FMOD messages, and the persistent-data synchronization deprecation remain. Screenshots/logs are local ignored evidence under `Builds/checks/output/playwright` and `Builds/checks/.playwright-cli`.

### Measurements

- Indexed metadata fixture: **423 bytes read** from an archive with a preceding
  **4 MiB** payload. This demonstrates avoiding a linear payload scan; it is not a
  whole-game loading-time benchmark.
- The five principal helper/reader files shrink from **875 to 550 lines**.
- Runtime C# diff removes **1,035 net lines**, including the legacy async scheduler
  and unused/replaced pools. Editor regression checks are counted separately.
- Removed vendored ZIP DLL: **198,656 bytes**. Its replacement is a package dependency,
  so deleted DLL size is not a player-size saving.
- Writer code no longer allocates an input-file-sized byte array; it uses
  `Stream.CopyTo`. Unity Mono's allocation counter returned zero even for a known 1 MiB calibration allocation. The test therefore reports the counter as unavailable; zero is not presented as a performance result. The measured size comparison follows.

Browser smoke exposed the embedded dependency-row template as visible after removing parent-wide clearing. The prefab now explicitly deactivates that template; the editor check verifies both the serialized state and activation of acquired clones. Final rebuilt-browser verification passed: screenshots 21 and 23 show no template label and exactly the expected dependency row after alternating core and core.sounds repeatedly.

Final raw player-directory sizes (Android is the APK); macOS includes the same ad-hoc signing procedure as the baseline. These are local 1.0.10 content builds, not newly published releases.

| Target | Baseline bytes | Final bytes | Change |
| --- | ---: | ---: | ---: |
| macOS | 134,918,297 | 135,040,108 | +121,811 (+0.090%) |
| WebGL | 85,384,264 | 85,483,748 | +99,484 (+0.117%) |
| Android | 97,813,774 | 97,918,200 | +104,426 (+0.107%) |
| Windows | 160,144,872 | 160,266,406 | +121,534 (+0.076%) |
| Linux | 164,293,683 | 164,415,257 | +121,574 (+0.074%) |

## Limitations

- No claim of higher FPS, zero allocation, or lower whole-game peak memory.
  HTTP/jar archives still download into memory; the 512 MiB limit is per expanded
  entry, not a total resource budget. CRC is corruption detection, not authentication.
- Source mods importing removed helpers/namespaces need the migration documented
  in docs/architecture.md. Generic pool acquisition needs a parameterless constructor.
  No save-format, package-ID, serialized field/GUID, or native ABI migration is made.
- No physical Android, Windows, or Linux gameplay, monitor mode-switching matrix,
  audible-audio verification, or complete thirteen-level playthrough.
- Optional vendor tool UI interactions are not exhaustively exercised. The gizmo
  fallback uses any active controller; multiple-controller scenes should assign its
  existing serialized reference. No bundled scene/prefab references that component.
- iOS remains deferred. No new release/version or Pages deployment is part of this change.
