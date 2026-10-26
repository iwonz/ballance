## Checks

Work in progress; this is not release acceptance.

- Verified original/v1.0.11, original and legacy v1.0.11 all resolve to
  d066bfe5017b4b541cca9e0747612f167da66291. The annotated alias and maintenance
  branch were pushed without changing main or any published release asset.
  The separate .worktrees/original checkout was clean after creation.
- python3 scripts/test-release-tools.py: 17 tests passed, including edition
  identity, checksum pins, archive rejection, atomic two-player staging, combined
  size rejection and preservation of Original's host page directory.
- Unity 6000.6.7f1, StandaloneOSX, batchmode EnvironmentChecks.Run: passed after
  compilation. Covered 13 profiles with exact built-in sector counts, all presets,
  interrupted eight-second blends, zero-time pause, rebased checkpoint state,
  irreversible mixed-run eligibility, custom-level dry fallback, invalid profiles,
  temporary per-preset records, reload, Original sentinel preservation, protected
  settings reset, and aspect-preserving Auto/QHD/4K/portrait/ultrawide calculations.
  Log: /tmp/ballance-environment-checks.log.
- An initial compile rejected the three-argument File.Move overload. Replaced it
  with File.Replace for an existing file and File.Move for a new file; rerun passed.
- python3 scripts/check-localization.py: passed, 322 Russian translations across
  12 resources; new settings/preset strings are present in all four languages.
- python3 scripts/check-physics-abi.py: passed, 92 ABI 2303 delegates. Native surface
  structs are 20/40/52 bytes; matching managed layout assertions pass in Unity.
- Native macOS ARM64 and Web Wasm smoke passed ABI 2303, existing collision and
  constraint cleanup, wet material pairs, local ice, region exclusion, snow
  resistance, dry restoration, NaN rejection and bounded wind validation.
  Android ARM64 rebuilt with 16 KB LOAD alignment; no device execution occurred.
  Logs: /tmp/ballance-physics-macos-2.log, /tmp/ballance-physics-web-2303.log,
  /tmp/ballance-physics-android-2303.log. Source-controlled provenance records identify
  the compiler, bridge, upstream revision, patch and output hashes.
- The upstream recompute_friction implementation recalculated geometry without
  reading materials. The patch now refreshes the material factor and fixes two
  core paths that passed an uninitialized contact situation to material lookup.
- Content-aware Web preview built with all thirteen levels at
  Builds/RemasterPreview; log /tmp/ballance-remaster-web.log. This first preview
  predates ABI 2303 and local surface binding. Chrome loaded it and displayed
  the menu, level selection, settings and environment/graphics panel. Auto
  reported 1728x1080 for the 1920x1200 canvas. Screenshots are under
  Builds/checks/output/playwright/remaster-*.png. This is a visual inspection,
  not the required performance or gameplay acceptance.
- openspec validate --all --strict --no-interactive: 14 items passed.
- python3 scripts/check-agent-context.py: passed.
- node --check site/assets/site.js: passed.

## Results

The Original baseline is preserved remotely. Main has independent Remaster product
identity, settings storage, environment-independent records with legacy imports, a tested weather state model
and lifecycle hooks, an initial graphics settings panel/render-resolution path,
and draft landing/atomic Pages preparation. Presentation and gameplay integration
still require player validation; these are development changes, not a finished
Remaster release. The manifest intentionally has no Remaster pin and refuses to
deploy until a verified real release exists.

### Original Web baseline

Existing local v1.0.11 Web build, Playwright CLI 0.1.22, Chrome 155.0.8059.85,
Apple M3 via ANGLE Metal, 1920x1200 canvas, visible tab, approximately 60 Hz.
Wasm SHA-256: c95e101492554f7b2b8e3d600e8a53ba8df67bbd66bbb6c891f44d051bd69094.
Raw report and before/after screenshots are retained under
Builds/checks/output/playwright/performance-1792930994394*.
python3 scripts/check-web-performance.py accepted both scenarios:

| Scenario | FPS | p95/p99 frame ms | GPU p95/p99 ms |
| --- | --- | --- | --- |
| Menu | 59.99 | 18.5 / 18.9 | 3.49 / 3.59 |
| Level 01 start, after a short input | 59.96 | 18.7 / 19.3 | 3.70 / 3.82 |

This single capture is an initial baseline, not a repeated regression comparison,
full-level playthrough, deterministic three-ball replay, or RTX 3060 measurement.
The browser logged refused Unity analytics requests; menu and level loading still
completed. No user's persistent browser profile was used.

## Current limitations

- ABI 2304 is rebuilt for all five platforms. macOS, Wasm/Node, Linux x86-64
  container and Windows/Wine smoke pass, including transformed contacts, wet
  pairs, rolling resistance and bounded wind. Android execution and real moving
  mechanism traversal remain unverified. All authored profile winds remain zero
  pending calibration; 229 floors and 96 mechanisms have draft coverage footprints.
- Nine generated albedo sources, fern cutouts, 72 shared materials, refined ball
  silhouettes, layered skies, precipitation without footprint trails and rain audio are
  implemented. Seamless tiling, material scale, audible sound and final placement
  require player acceptance across all levels. The integrated art is developmental.
- 656 editor interface previews pass across four languages and four
  resolutions. Full menu/tutorial navigation, mobile safe areas, actual resolution
  changes, fullscreen and all seven player presets still need complete checks.
- State-model and persistence fixtures pass; actual player checkpoint/rebirth,
  transformed supports, completion after environment changes, Web/Android save replacement
  and isolated reset require further browser/device acceptance.
- No 91-run acceptance, target PC performance, QHD/4K performance, physical Android
  input/audio/lifecycle, or Windows/Linux gameplay has been performed.
- Pages has not been deployed. A disposable Original language preference survives
  host/resource relocation. The October 26 disposable score/progress fixture also
  survives relocation and deletion of the separate Reballance storage root. Both
  public players and independent rollback still need browser acceptance.
- All five development players and archives were built. The latest covering, sky,
  loading-screen and HUD refinements still require refreshed players; there is no
  Remaster release. Keep this change active where evidence is incomplete.

### Native dry replay comparison

scripts/measure-physics.py captured 36 ten-second flat-course replays using the
shipped ball parameters, the paper OBJ convex vertices, 66 Hz simulation, and
30/60/120/144 Hz frame grouping. The scope excludes Unity input sampling, imported
mesh conversion, the camera, and level mechanisms. Artifacts:

- Builds/checks/physics-original-2302.json: shipped Original library.
- Builds/checks/physics-remaster-2303-dry.json: new bridge and pinned-source rebuild.
- Builds/checks/physics-original-rebuilt.json: diagnostic old bridge rebuilt with
  the same compiler/source patch (version scalar changed for the smoke harness).
- Builds/checks/physics-remaster-rebuilt-comparison.json: repeated comparison.

The shipped baseline's maximum repeat delta was 0.000958644; the pre-change
comparison tolerance is twice this value, 0.001917288. Wood and stone matched
exactly. Paper exceeded tolerance: maximum transform-component delta 0.643489838.
The diagnostic old bridge produced the same paper result as the new bridge;
new-versus-rebuilt-old maximum delta was zero. This narrows the discrepancy to
the source/toolchain rebuild rather than weather additions; it does not resolve
the shipped-baseline regression or establish gameplay equivalence. Keep this
acceptance gate open; do not loosen the recorded tolerance to make it pass.

### Follow-up implementation checks

- Docker actionlint 1.7.12 checked the Pages workflow successfully (no network,
  repository mounted read-only).
- All five ABI 2303 libraries built from the pinned source. Windows required
  explicit wide-character crash APIs, MinGW string compatibility and legacy
  upstream diagnostic pointer-cast permissiveness; the bridge itself is strict.
  Windows smoke passed under Wine in the x86-64 container; Wine's 32-bit setup
  logged a missing rundll32/Rosetta diagnostic before the 64-bit smoke succeeded.
  This is not a Windows player or physical-PC test. Logs:
  /tmp/ballance-physics-windows-2303-5.log and
  /tmp/ballance-physics-{macos,linux,web,android}-final.log.
- LocalizationChecks.Run compiled the new menus/tutorial and validated every
  character in all maintained game language resources against bundled Noto CJK.
  python3 scripts/check-localization.py now passes 339 Russian translations in
  twelve resources. Noto's exact revision, checksum and license are retained.
- RemasterUIVisualChecks rendered main/level-menu previews for four languages at
  720x1280, 1920x1080, 2560x1440 and 3840x2160. Inspection found the editor's
  first layout used stale canvas dimensions; the render harness now forces the
  canvas before installing the layout. Those original captures are not accepted
  landscape layout evidence until rerendered and inspected.
- Builds/RemasterPreview2 loaded in Chrome with ABI 2303. The responsive host and
  graphics panel displayed Auto at 1920x1009 for the current wide canvas.
  Inspection found cubemap face seams; the sky shader was changed to use Unity's
  six face UV passes and awaits the next player inspection.

### Dry replay discrepancy resolved in diagnostic build

Disassembly counted zero fused multiply-add instructions in the shipped Original
macOS library and 2,584 in the default new compiler output. A diagnostic rebuild
with `-ffp-contract=off` passed native smoke and all 36 comparisons against the
original recorded fixture with maximum delta **0**, including paper. Report:
`Builds/checks/physics-fp-off.json`; log `/tmp/ballance-physics-fp-diagnostic.log`.
The original repeatability tolerance was not changed. The build script now fixes
contraction to off for every target and records it in provenance. All five installed plugins were subsequently refreshed with this flag (see below).
This resolves the native flat-course discrepancy; full Unity input/camera and
mechanism comparisons remain unverified.

### Moving supports, ball visuals and browser storage

- All five plugins rebuilt after the world-normal support fix with contraction off.
  macOS and Web smoke passed natively/Node; Linux x86-64 container and Windows
  Wine smoke passed. The smoke now translates and rotates a support through 90
  degrees and verifies its local ice still changes contact grip. Android ARM64
  has 16 KB LOAD alignment but was not executed. Logs:
  `/tmp/ballance-physics-{macos,web,linux,windows,android}-support.log`.
  Linux now builds in pinned Ubuntu 22.04/clang14; maximum GLIBC symbol is 2.29.
- The installed macOS plugin again matched all 36 dry fixture replays with maximum
  delta zero and the unchanged tolerance. Report:
  `Builds/checks/physics-remaster-support.json`.
- `EnvironmentChecks.Run` passed after finding and fixing the vertically stored
  folding bridge's narrow local-X geometry. All six configured support types have
  collision-derived regions; 96 mechanism instance references exist in level data.
  These checks do not establish real mechanism traversal or tuned placement.
  Log: `/tmp/ballance-moving-support-checks-4.log`.
- `RemasterMaterialAuthoring.Apply` compiled and verified the three player ball
  refinements: wood 316 to 2,887 render vertices, stone 392 to 5,882, paper 186 to
  15,360. Original imported vertices and paper's native convex source remain
  unchanged; sphere radii are unchanged. New paper albedo was generated via
  imagegen and its prompt/checksum recorded in docs/remaster-art.md. Log:
  `/tmp/ballance-ball-visual-checks.log`. In-game silhouette acceptance is pending.
- Content-aware `Builds/RemasterPreview3` built successfully. Chrome displayed
  the landscape main menu and thirteen-level grid correctly. Replay tutorial
  launched; Enter advanced once to the binding-aware camera card. Inspection
  found HUD overlap and the misleading Skip label; those are corrected in source
  and await the next player. The six-pass sky no longer showed the previous
  cubemap side seams in the inspected level-01 view; other levels remain unchecked.
- The verified published Original archive was extracted to a disposable local
  `/checks/storage-compat/ballance/` directory. Through the real UI, English was
  selected and survived a browser reload. The actual `original_host` transformation
  then moved only resources under `original/` and the host to `original.html`.
  A fresh load still showed English and the same 355-byte PlayerPrefs file at
  `/idbfs/f09af4570b6b0796f0eeda2e09b81a5f/PlayerPrefs`. Screenshots:
  `original-language-persisted.png` and `original-after-relocation.png` under
  `Builds/checks/output/playwright`. No real user browser profile was used.
  This covers a persisted preference, not a completed-game score fixture.
- Pages preparation now rejects a Remaster archive carrying the Original app ID;
  all 17 release-tool tests pass. No site or Remaster release was published.

- Corrected UI render previews passed for all four languages at portrait/FHD/QHD/4K;
  the Russian landscape main menu and Traditional Chinese portrait grid were
  inspected. Log `/tmp/ballance-ui-visual-final.log`. These are editor layout
  previews, not physical mobile input acceptance.
- A local pair assembled the verified Original archive and real development Web
  preview without changing the source release pins. `verify-site.py` passed both
  HTTP identities. Landing browser assertions passed: no Unity resource preload,
  skip-link first focus, RU/EN switching, keyboard preset selection, 390 px width
  without horizontal overflow, and reduced-motion scroll behavior. Captures:
  `site-desktop.png`, `site-mobile.png`. Full-page screenshots initially captured
  the lazy concept image before scrolling it into view; verify its loaded state
  separately before accepting final captures. This was not a Pages deployment.

### ABI 2304, integrated presentation and branch maintenance

- ABI 2304 appends a support ID to the contact record (56 bytes); 92 delegates
  remain aligned. All five libraries rebuilt with recorded hashes and contraction
  off. Logs: `/tmp/ballance-physics-{macos,web,linux,windows,android}-2304.log`.
  macOS, Wasm/Node, Linux/container and Windows/Wine smoke passed; Android only
  passed binary/page-alignment validation. Expanded contact-pair checks passed
  on all four executable targets; the final bounded-wind assertions also passed
  on macOS, Wasm/Node, Linux/container and Windows/Wine. No target-device acceptance is implied.
- The installed macOS library passed all 36 baseline dry replays with maximum
  transform delta 0: `Builds/checks/physics-remaster-2304.json`. Original tolerance
  remains 0.001917288. Unity input/camera/mechanism equivalence is still pending.
- EnvironmentChecks passed contact-layout, pooled mark pose-following, pause,
  checkpoint rewind, destroyed-support cleanup, all environment shader compilation
  and rain sample range/DC checks. Log `/tmp/ballance-environment-final-2304.log`.
  Tracks use the native support identity and game time. Rain uses a deterministic
  eight-second stereo noise loop through the existing effects mixer. Listening and
  device audio/lifecycle checks remain outstanding.
- Material authoring now covers 72 explicit shared materials, including concrete,
  brass, rubber and Standard transparent lantern glass. Generated concrete and
  fern assets have prompt/hash provenance in docs/remaster-art.md. Side foliage
  and covering locations remain draft placements, not accepted level art.
- RemasterPreview4 built and loaded in Chrome. Tutorial skip starts a movable ball;
  ArrowDown and the camera input worked. Camera appearance was compared with the
  baseline level-01 capture; the steep starting angle also exists in Original.
  HUD inspection found clipped counter/caption text with Noto's metrics. The
  repaired HUD now passes text-geometry assertions and 48 menu/grid/HUD renders
  across four languages and portrait/FHD/QHD/4K; Russian FHD HUD was inspected.
  Log `/tmp/ballance-ui-hud-checks.log`. The updated player is being built separately.
- Localization checker passes 342 Russian translations across twelve resources.
  Strict OpenSpec (14 items), context checks and Docker actionlint 1.7.12 pass.
- Shared build/publication commit `868f41f9` was cherry-picked as `13f694e6` into
  Original. Its scoped maintenance change records descriptor/README setup and
  validation; no Remaster runtime, materials or ABI were transferred. No new
  release or Pages deployment has been published.
- Original maintenance change `prepare-original-maintenance` was synchronized and
  archived on its own branch. Post-archive strict validation (13 specs) and delivery
  context check against d066bfe5 passed. Commit `4c2f159f` contains its descriptor,
  README and archived evidence; the worktree is clean. Its Unity compile/localization
  log is `/tmp/ballance-original-maintenance-compile.log` (205 prefab labels).

### Web lifecycle and integrated preview follow-up

- Final expanded native smoke passed on macOS, Wasm/Node, Linux/container and
  Windows/Wine. Logs: /tmp/ballance-smoke-linux-final.log and
  /tmp/ballance-smoke-windows-final.log; Web executable
  Builds/checks/physics-smoke-web-final.js. Tests include all twelve wet material
  pairs, three-ball snow/sand resistance, transformed support identity and wind
  sensitivity ordering. These are native fixtures, not level playthroughs.
- Preview5 exposed a NullReferenceException after skipping the first tutorial:
  the new track code read Ball._Rigidbody before that component's Start callback.
  It now uses BallManager's existing registered body. A rebuilt player must verify
  that tutorial/start, transformation and recovery produce no repeated exception.
- EnvironmentChecks.Run passed with side-vegetation placement, collision exclusion,
  support following, visibility and disposal assertions. Log:
  /tmp/ballance-environment-web-lifecycle.log.
- node scripts/test-web-lifecycle.js passed six save-flush cases: normal exit,
  restart, pending automatic IDBFS work, timeout, callback error and synchronous
  failure. The host waits for a successful flush and Unity shutdown before reload
  or returning to the parent landing; browser execution is still pending.

### Five-platform development build and browser acceptance subset

- Content-aware development players built for Web, macOS ARM64, Windows x86-64,
  Linux x86-64 and Android ARM64, version 1.0.0-dev.1, with matching Remaster
  identity files. The old Original outputs were moved intact to Builds/OriginalBaseline.
  Logs: /tmp/ballance-remaster-candidate-web.log and Logs/build-{macOS,Windows,Linux,Android}.log.
  Archives and checksum sidecars exist in Builds/releases; no release was published.
  These builds predate the subsequent textured-covering, themed-sky and loading/HUD
  refinements; do not call them the final source build.
- Initial archive SHA-256 values:
  Web 6a237316763482487eff5920fcf8a6dbe48073b17348a9ff6fbbb85f7df83473;
  macOS addecf25bd4f4c574bd25a8177784aa551195f728c79262d7a897b714b0c0908;
  Windows 45e0d3a1705820e88d1516c1d0fdc5a4d502c6ffc410183b009d122cd2135b12;
  Linux c069581b3ab500faedb9fcce798d262fd33e94ed5e74885b58fe6b5015cb1837;
  Android 2b19008c3e67b6a2ff2c956a2bbbec8f349e8b224542904873bb21a57179d7a0.
- Android aapt confirms com.iwonz.BallanceRemaster, version 1.0.0-dev.1, API 26
  minimum and ARM64 only. apksigner verifies v2 signing; zipalign -c -P 16 -v 4
  passes. ZIP CRC and thirteen level assets pass. A naive whole-file native hash
  comparison failed because Unity strips nonloaded symbols; all 21 loadable ELF
  sections match the source plugin byte-for-byte. This is not device execution.
- The rebuilt Web player no longer throws the track NullReferenceException during
  tutorial skip and movement. All seven presets were selected through real UI with
  completed transitions in the level-01 start area; screenshots candidate-*.png
  are retained in Builds/checks/output/playwright. Mixed-run status appeared and
  persisted. Returning to the menu cleaned up the environment without a runtime
  exception. This covers the start area, not a completed test course or level.
- QHD displayed 2560x1346 and 4K 3840x2019 for the 1200x631 viewport, with unchanged
  UI size and preserved aspect. No performance claim follows from these captures.
- Actual in-game Web exit navigated to the parent URL after flushing saves. The
  candidate paired site launches the real Remaster through its header; its in-game
  English selection and restart reloaded the same nested URL and showed English.
  Capture: candidate-language-restarted.png. The local pair passes verify-site.py;
  source site/releases.json remains unchanged with a null Remaster release pin.
- Browser inspection found flat-looking coverage and warm sunset leakage into fixed
  themes. Four further imagegen textures (Snow, Ice, Sand, Moss) now have exact
  prompt/hash provenance. Actual-shader four-repeat previews were rendered and
  inspected under Builds/checks/coverings; no conspicuous boundary seam was observed,
  but repetition and in-level scale still require acceptance. Log:
  /tmp/ballance-covering-renders.log. Theme palette blending/checkpoint assertions
  pass in /tmp/ballance-theme-covering-checks.log.
- The loading screen now shares the Remaster typography/palette, and preset/ranking
  HUD text has a dark backing. The level-exit dialog distinguishes returning to the
  menu from quitting the application. Eighty editor previews cover five screens,
  four languages and four sizes; log /tmp/ballance-ui-loading-final.log. Localization
  now passes 343 Russian translations across twelve resources.

### Hardware availability and native presentation follow-up

- The user confirmed that a physical Android device and the RTX 3060/i5-12400
  reference PC are unavailable. Record their input, gameplay and performance
  acceptance as **not run**; Mac or emulated native smoke results do not substitute.
- The aggregate Unity BuildChecks.Run completed successfully in
  /tmp/ballance-remaster-build-checks.log. Its six platform configuration checks
  include the deferred iOS configuration; this is not an iOS build or delivery.
- Launching the development macOS ARM64 player exposed a frozen Unity splash even
  though the log reached MenuLevel. Web had not exposed this presentation failure.
  A screen-targeted, geometry-free camera is being verified while scene cameras
  remain redirected to the resolution-controlled texture. The dev.1 macOS archive
  must not be treated as a working release; retain it only as historical evidence.

## Branding and complete-control follow-up

The user requested removal of upstream promotional branding from Remaster startup/About and all visible game links, a generated Ballance Remaster cloud-reveal illustration, a distinct records page, and replacement of remaining legacy controls. The same active change now owns these requirements. Source copyright and third-party license text remain intact.

- Removed the custom upstream splash entry, changed Remaster company metadata to iwonz, replaced the registered system logo, and updated all four ProjectAbout resources plus bundled package support links. Original worktree was not edited.
- Generated and integrated TitleArtwork.png and IntroCloud.png; exact prompts, dimensions and hashes are in docs/remaster-art.md. About/Project pages use aspect-preserving artwork.
- Added a separate records layout with preset context and ranked rows; fixed empty-record level navigation and short score-list handling.
- Reworked shared buttons, arrows, up/down controls, key bindings, toggle states, sliders, dropdowns, scrollbars and text. Dynamic package settings also receive the theme, and AddSlider now instantiates its existing slider prefab.
- Unity UI compilation/render pass succeeded after correcting the title's actual prefab hierarchy. Inspected records, About, project text, audio and keyboard controls; found and corrected nested branding, old arrows and overlapping labels. The latest package-screen additions still require another render and live-player navigation. Static fixture placeholder values are not saved gameplay records.
- Managed/native teardown fixture passed previously; a rebuilt player containing the subsequent managed cleanup and UI changes remains required.

## Volumetric and material-depth follow-up

The user clarified that running-game clouds must approach the dense soft title illustration and that materials require real normal/height/roughness data, with living precipitation and greenery. Explicit requirements and task 4.7 were added.

The first relief authoring pass generated periodic maps but failed surface compilation at shader model 3.0's interpolator limit. The shader now targets 3.5, supported by the five existing release graphics targets, and the covering render check passed. Moving-light previews exposed excessive relief and clipping; strengths and the inspection lighting were adjusted before regenerating. The first full sky fixture stopped on the intentionally unsupported EnvironmentState.For(Weather) call; it now resolves Weather through level01's authored profile. These are development checks, not final visual/performance acceptance.

## Cloud distribution, density and local installations

- The user rejected copied cloud rows and then requested a denser deck. The 128³ source now varies billow centers/radii; continuous world-space groups distort density coordinates, height and coverage. The deck is thicker with fewer large gaps. Stock A–M palettes and menu C/M use the same renderer. Dry physics and authored weather rules were not changed for these visual requests.
- Sparse ray integration produced visible bands; an adaptive-step experiment produced contours and was rejected. The current camera-aligned sky cache preserves smooth integration, bounds capture resolution by quality and restores the source sky after rendering. The source density is now R8 with mipmaps; nearby light samples share the slow macro field. This remains subject to player performance measurements.
- `RemasterArtChecks.RenderClouds` passed on Unity 6000.6.7f1 / Metal with seven presets at three qualities, thirteen stock palettes, four additional headings, motion and translation by the former texture period. The latest normalized translated-image difference was 0.06460 (minimum 0.015). These checks prevent the former exact repetition but do not establish final artistic acceptance or FPS.
- `RemasterUIVisualChecks.Run` passed at portrait/FHD/QHD/4K in four languages. The first expanded manager-page run failed because it treated row prefabs as pages; the inventory was corrected. Actual dev.3 Mac checks confirmed the records table and navigation from empty level 1 to level 3. Package rows retained legacy styling and the details title clipped; these were corrected, and a new details render passed. Dynamic rows still require checking in the newer player.
- Content-aware macOS dev.3 and dev.4 builds, ad-hoc signatures, archive round trips and native ABI 2304 smoke passed. Dev.3 menu rendering and quit were exercised. An informal built-in FPS display showed about 18 FPS at medium in the animated menu; this is a failed performance observation, not a qualified benchmark. The denser R8/shared-field optimization in dev.4 must be measured separately. No new five-platform release or public Pages deployment occurred.
- Original 1.0.11 was verified and installed at `/Applications/Ballance Original.app`; the old installed 1.0.9 bundle was removed. Remaster has a canonical `/Applications/Ballance Remaster.app`. Automatic approval review rejected deleting build/archive app bundles, so their files were preserved and registrations removed. Spotlight rediscovered these copies; moving the generated tree to `Builds.noindex` with a compatible `Builds` symlink resolved the search duplication. A subsequent `mdfind` returned only the two canonical applications. Historical LaunchServices cache entries need final verification after the latest update.
- Installer fixtures passed 10 tests: replacement, staging exclusion, corruption/signature failure, edition/downgrade checks, cleanup scope, external symlinks, unregistration errors, preserved archive paths and rollback on a failed directory link. Strict OpenSpec passed 14 items; localization passed 343 Russian translations across 12 resources; agent context passed 13 capabilities. The unavailable physical Android and RTX 3060/i5-12400 checks remain not run at the user's direction.

## Reballance naming and installation (development 6)

- Final public names: Ballance and Reballance. Updated runtime/menu text, four About localizations, core package labels, editor About, generated startup artwork, native product metadata, Web template, RU/EN site and maintained edition guides. Technical identifiers and immutable published assets stay compatible. The title asset provenance and exact imagegen prompt are in `docs/remaster-art.md`.
- Unity 6000.6.7f1 rendered all UI fixtures in four languages at portrait/Full HD/QHD/4K: `/tmp/reballance-ui-checks.log`. Environment/storage/output-name assertions passed in `/tmp/reballance-final-checks.log`.
- Content-aware macOS build 1.0.0-dev.6 succeeded. Signed `Reballance-1.0.0-dev.6-macOS.zip` includes `Reballance.app`; archive round-trip signature/content and native ABI 2304 smoke passed. This is a local development package, not a published release.
- Installer tests: 13 passed, including old-name migration and rejection of a newer noncanonical installation. Release tooling: 17 passed. Localization: 343 Russian translations across 12 resources passed. Strict OpenSpec: 14 passed; context: 13 capabilities passed.
- `/Applications/Ballance.app` is 1.0.11 and `/Applications/Reballance.app` is 1.0.0-dev.6. Replaced the former suffixed application paths. Spotlight, application-directory inspection and active LaunchServices records each show exactly these two game installations. Dock was restarted to refresh its cached list without resetting layout. Disabled historical LaunchServices records and archived build files remain preserved.
- Compared saved-file hashes and all existing namespaced preferences immediately before/after installation: unchanged. Reballance retains `com.iwonz.BallanceRemaster` for macOS/Android preferences, the existing Web URL, and a stable desktop file directory through `GamePathManager`. Classic Ballance data was not imported. Unreleased Windows/Linux development preference migration has not been validated.
- Launched both canonical applications. Reballance's window title/menu and main-menu logo show the new name. Observed the active Moscow gameplay view; no managed exceptions in the current player log. The user was actively playing, so interactive controls and quit were not taken over.
- The Original worktree has a separate completed/archived `align-classic-product-name` change for README and shared product/archive validation. Its release tests, strict specs, context and delivery-context checks passed; no classic runtime behavior or native library changed. No naming changes have been pushed/published yet.

## HUD, organic coverage and performance iteration (in progress)

The user reported solid HUD blocks, an unwanted centered preset title, unnatural ice strips and visible FPS drops in dev.6. Planning artifacts now cover compact counters without panels, no preset heading, matching irregular physical/rendered coverage, and rendering-cost corrections.

- A three-second `sample` of the active dev.6 native process is `/tmp/reballance-player-sample.txt`. It shows substantial Metal presentation waits and rendering work, with approximately 780 MB resident physical footprint and 1.1 GB peak. This is CPU stack sampling, not a frame-time or GPU budget result. Concurrent development activity may affect observed performance; obtain isolated before/after player measurements.
- The latest cloud editor fixture before the cache redesign passed with normalized translated-image difference 0.083895 (`/tmp/ballance-cloud-final.log`). It does not validate the new incremental cubemap cache, which still needs orientation, seam, motion and performance checks.
- Native macOS physics rebuilt with matching irregular feathered region sampling; new corner-exclusion and partial-ice-boundary smoke cases passed at ABI 2304. Other native targets still need this latest rebuild. Weather rules advance to version 2; prior records remain in their own files and unlocks are unchanged.
- New HUD, organic region placement, static binding upload avoidance, selective covering texture sampling and incremental cloud cubemap cache are source changes only at this point. No player including them has been delivered or performance-qualified.

### HUD and organic coverage verification, development 7

- `check-physics-abi.py`: passed, 92 delegates, ABI 2304 unchanged. The new contour uses contact/mesh normals for its projection plane; the native smoke additionally checks unchanged grip when region thickness exceeds its radius.
- `RemasterArtChecks.Render`: initial and corrected material fixtures passed EnvironmentChecks, shader compilation, fourteen material families, seven presets at three qualities, thirteen stock palettes and directional views. The first covering fixture referenced a nonexistent Stone resource; corrected it to Sandstone and reran. Added dry/Moscow/Sahara/Hong Kong coverage previews and a direct-versus-cached Moscow sky comparison. Final edge refinement is being checked separately.
- `RemasterUIVisualChecks.Run`: first run failed because HUD caption fixture paths still used the former names. Corrected those paths; rerun completed successfully. Inspected Russian Full HD and portrait counters: no backing panels or preset heading; existing debug-mode text is now disabled in this non-debug fixture as in normal gameplay. Four-language navigation and actual sky-background readability still need player acceptance.
- Release tools: 17 tests passed; macOS installer: 13 tests passed; localization: 343 Russian entries across 12 resources passed. Strict OpenSpec: 14 items passed; context check: 13 capabilities passed.
- All native rebuilds are now complete, with per-target execution limits recorded below. No development-7 player has been installed or measured yet. The running development-6 game is left intact. No new public release or Pages deployment has occurred.

- Final covering refinement passed `RemasterArtChecks.Render` (`/tmp/reballance-art-acceptance.log`): rough, feathered edges inspected in Moscow; cached/direct sky images visually compared, translation difference 0.08317. A separate optional image-difference command was not run successfully because system Python lacks Pillow; no numerical cached/direct metric is claimed.
- Final macOS/Web native smoke passed ABI 2304; Android cross-build completed without device execution. Linux/Windows final rebuilds are continuing.
- The latest macOS library matched all 36 three-ball dry replays exactly (maximum delta 0; pre-existing Original tolerance 0.0019172877). The initial comparison command used a missing baseline filename after writing its report; compared that recorded report with the actual `physics-original-2302.json`, checking library and fixture hashes and all sample counts.
- Content-aware macOS development-7 build succeeded. `package-build.py macOS` verified ARM64, ad-hoc signature, native smoke and archive round trip: `Reballance-1.0.0-dev.7-macOS.zip` plus checksum. Installer dry run targets only `/Applications/Reballance.app` and unregisters preserved diagnostic bundles. Installation and runtime FPS acceptance await the user because their current level is still running.
- `git diff --check` reports Unity-serialized empty material-property whitespace and preserved upstream patch context; no claim of a clean whole-diff whitespace gate.

- All five final native plugin provenance records now match the current bridge hash. macOS, Web/Node, Linux/x86-64 and Windows/Wine smoke tests passed; Android ARM64 cross-build succeeded with `smokeExecuted: false`. This does not establish physical Windows/Linux/Android gameplay.

### Web diagnostics and second development-7 candidate

- First development-7 Web player built and packaged successfully, loaded under `/ballance/remaster/`, showed the Reballance main menu and functional graphics controls. The browser uses `ANGLE Metal Renderer: Apple M3`, high-performance preference and a visible tab. The initial host header/loading labels still said Ballance / Remaster; corrected the maintained Web template in all four host locales.
- Informal Medium counter observations varied from about 16 to 30 FPS. Two separate 30-second captures using the existing diagnostic instrumentation recorded 26.53 FPS with clouds and 28.80 FPS without them, CPU median 3.7/3.5 ms and GPU-query median 121.73/91.96 ms. These are not valid idle-machine acceptance captures: the user's native development-6 player remained running, and the unusually large GPU query elapsed times cannot be equated with display frame latency. Files: `sky-diagnostic-1792953955207.json` and `sky-diagnostic-1792954039483.json` under `Builds/checks/output/playwright`. Both before/after screenshots confirm the corresponding cloud setting throughout each capture. A cloud-off UI action was queued while the first CLI command was still completing; its final screenshot still shows clouds on.
- Removed five identical atmospheric shader passes and unused interpolators/sampler reads. `RemasterArtChecks.Render` passed again (`/tmp/reballance-single-pass-sky.log`), including source-sky restoration and unchanged cloud directions. Inspected the cached Moscow render; translation difference 0.0831748. Six-pass reference images were retained in `Builds/checks/dev7-six-pass-sky`. Pixel files differ slightly; no bit-identical render claim. Native/Web players need rebuilding for this second candidate.
- Browser log errors observed so far are blocked Unity telemetry, a missing test-host favicon, and existing Web FMOD metadata/unimplemented calls; no cloud-capture or managed exception was observed in menu navigation. Actual audio acceptance remains open.

- A later settings screenshot showed that the rapid click intended to restore Medium had not registered in Unity: both 30-second cloud diagnostics above were actually at Low. They cannot represent Medium. Subsequent Web actions use the existing measurement helper’s hover delay and 150 ms press and verify displayed values before continuing.

### Authorized native comparison and installation

- The user authorized ending the old run and updating now. At the start of this
  comparison, no Unity build, test Chrome or native player remained running.
  Relaunched installed dev.6, then compared dev.7 on Apple M3 / Metal, 1280x800,
  Medium, Auto, 100% scale, Moscow level 01. Namespaced preferences were unchanged
  between runs. VSync was not enabled; the display counter is not presented FPS.
- Dev.6 F10 observations: animated menu 45.4-46.9 FPS; level start 126.5;
  following Down/Left/Down/Right movement samples 119.7, 107.2, 134.0 and 100.3.
  Dev.7 observations: menu 413.2-564.2; level start 520.9; corresponding movement
  samples 347.4, 137.6, 71.9 and 327.8; later stationary sample 337.8. These are
  short 0.4-second counter readings at manually sampled moments, not a timed input
  replay or aggregate benchmark. The 71.9 reading prevents claiming every movement
  sample improved. No native p95/p99, 1% low, GPU frame budget or heavy-sector
  acceptance is claimed. The initial high readings alone did not establish smoothness.
- Installed the second macOS dev.7 candidate into `/Applications/Reballance.app`.
  Compared all 293 saved-file hashes and 13 namespaced preferences immediately
  before/after installation: identical. Installer preserved/unregistered diagnostic
  builds and retained Ballance 1.0.11 separately. No public release was published.
- Native gameplay visibly has compact score/life counters, no centered preset
  heading, and irregular feathered snow/ice footprints. Verified score updates,
  movement, pause/resume, camera rotation, return to menu and normal quit. The player
  log shows level and physics teardown without managed exceptions; blocked Unity
  telemetry remains unrelated log noise. Checkpoints, life loss and all 91 runs
  were not exercised in this short comparison.
- Found a clipped Russian exit warning during that navigation. Increased its
  existing message area and added preferred-height validation to the four-language
  UI fixture. Final UI check/rebuild results follow below; until then the installed
  second candidate still has that dialog defect.

### Final development-7 package and follow-up scope

- The exit-warning fixture passed in four languages/resolutions and the Russian
  Full HD render shows the complete warning. Final dev.7 macOS/Web content builds
  and packaging passed; macOS signature/ABI smoke/round-trip passed. Installed the
  final macOS package and compared saved-file hashes/preferences again: unchanged.
  Spotlight now returns exactly `/Applications/Ballance.app` and
  `/Applications/Reballance.app`.
- The Web header line was the canvas focus ring. Restricted the accent focus rule
  to links/buttons and removed the canvas outline. Loaded final dev.7 via HTTP:
  product/header Reballance, focused canvas outline style `none`, no line in the
  captured image. Shift+Tab from the running game remained captured by Unity;
  keyboard traversal from the canvas to host controls is still an accessibility gap,
  not a passed navigation check.
- A fresh Web FPS capture did not complete because the page navigated and its canvas
  was gone before sampling. No final Web performance result is claimed.
- Subsequent user instructions supersede the original preset/rules record design:
  remove practice-run logic, share records across environments, center menus, add
  language flags, remove dark weather trails, and generate consistent UI/mod icons.
  Development 8 implements these in the same active change. The broader release,
  native frame-time acceptance and unavailable hardware checks remain open.

### Development-8 interface and records checks

- Generated one transparent 16-icon atlas with imagegen and reused its sprites for
  built-in packages/modifications, generic packages, score/lives, directional and
  adjustment controls, dependency status and error dialogs. Original artwork and
  provenance are recorded in `docs/remaster-art.md`.
- Removed weather footprint rendering and practice-run classification. The score
  manager now uses one shared table regardless of preset; score arithmetic and
  timers remain unchanged. `EnvironmentChecks.Run` passed in Unity
  (`/tmp/reballance-dev8-records.log`), including all-preset eligibility, legacy
  merge/deduplication/order, save/reload/reset and malformed-data rejection without
  a partially migrated file. Legacy sources and Original data remain untouched by
  those temporary-profile fixtures.
- Strict OpenSpec validation passed all 14 items; context check passed 13
  capabilities; localization passed 341 Russian translations across 12 resources;
  ABI check passed 92 delegates; Web lifecycle fixtures passed.
- UI refinement initially exposed incorrect nested paths in trust/error dialogs;
  corrected both against the serialized hierarchies. Final render, native/Web
  builds, installation and live checks are recorded below when completed.

- Final `RemasterUIVisualChecks.Run` passed (`/tmp/reballance-dev8-ui-complete.log`):
  four languages at 720x1280, Full HD, QHD and 4K. Inspected centered Russian main
  menu, all four language flags, modification icons, score/life icons and the global
  error dialog. The image inventory shows the new atlas for interface symbols;
  third-party technology credits and clipping masks retain their proper assets.
  These rendered fixtures do not substitute for physical touch or live navigation.

- Content-aware macOS dev.8 build and packaging passed, including ARM64, ad-hoc
  signature, ABI 2304 native smoke, archive round trip and checksum. Installed at
  `/Applications/Reballance.app`; Ballance 1.0.11 stays separate. All 294 profile
  files and 13 namespaced preferences matched immediately before/after installation.
- Started installed dev.8: player log reached the menu without a managed exception.
  Its first launch created the shared score file containing all 10 distinct legacy
  records. The three legacy table hashes and unlock-file bytes remained unchanged.
  The native UI automation repeatedly returned `cgWindowNotFound`, including after
  reconnect/reset, so no dev.8 native navigation or screenshot pass is claimed.

### Repeated macOS duplicates and publication decision

- The user's screenshot disproved completion of task 6.6; reopened it. Spotlight
  returned two apps while LaunchServices still registered four unpacked diagnostic
  remasters and three obsolete Original translocation paths. A noindex directory
  and unregistration alone were insufficient.
- The installer now retires matching build bundles with a reversible non-app suffix,
  preserving their contents and archives, and removes only matching stale catalog
  entries. Sixteen installer fixtures passed, including replacement, identity,
  data preservation, discovery exclusion, repeated backups and safe catalog parsing.
- Applied retirement to 13 Remaster and seven Original diagnostic bundles. A fresh
  LaunchServices dump contains only `/Applications/Ballance.app` and
  `/Applications/Reballance.app`; no discoverable `.app` remains under the two build
  roots. Native UI capture still returns `cgWindowNotFound`, so the visible launcher
  has not yet been independently rechecked.
- The user explicitly requests commits/tags/push and both releases/Pages, and chose
  publication of Reballance only after full gameplay acceptance. No prerelease or
  public site deployment is an acceptable substitute for the remaining checks.

- Subsequent explicit user authorization superseded preservation of disabled apps:
  removed 20 obsolete matching unpacked bundles (5.6 GB summed file sizes) after
  verifying both installed signatures. No retired/discoverable bundle remains in
  the build roots. Manifest: `Builds/checks/development-bundles-removed.json`.
  Installer tests now pass 17 cases, including deletion of retired copies while
  preserving archives/newer builds and selecting `-dev.app` names. Release tooling
  passes 18 cases, including required development bundle paths.
- Web dev.8 content build and archive/checksum passed. The isolated browser `qa8`
  loaded real modification rows with the imagegen icons. Shift+Tab from the focused
  canvas moved focus to Full screen; canvas outline is `none`. The first headed
  browser had navigated to about:blank before this test and timed out, so that attempt
  is not a pass. `qa8` is functional evidence, not a hardware FPS measurement.

### Development-9 installation and cleanup verification

- The final macOS dev.9 archive passed packaging, ARM64/ABI/signature checks and
  extraction verification. Installed as `/Applications/Reballance-dev.app`, with
  display name Reballance-dev and the unchanged application identifier/data root.
  All 295 saved-file hashes matched immediately before/after installation.
- Rechecked both installed signatures and LaunchServices: only Ballance 1.0.11
  and Reballance-dev 1.0.0-dev.9 are registered. A scoped inventory of Applications,
  user Applications, Downloads and Desktop found only those two matching bundles.
  No matching disabled application remains. Visible launcher capture is still
  unavailable because native automation returns `cgWindowNotFound`.
- Cleanup now includes disabled copies in application folders, checks real-path
  running executables through the Builds symlink, and the legacy macOS build
  wrapper delegates to the same edition-aware build/package commands. Nineteen
  installer tests, eighteen release-tool tests, shell syntax, strict change
  validation and repository context checks passed.
- The final touch fixture passed (`/tmp/reballance-touch-ui-final.log`), including
  all five keypad prefabs. Inspected directional layout, the generated view icon
  and joystick thumb. These are rendered fixtures, not physical Android acceptance.
- Web dev.8 loaded level 1, displayed the skippable tutorial, entered gameplay and
  opened the pause menu. The Web player does not yet include the dev.9 touch fixes.
  This is a short functional check, not a completed level or performance result.
- Original naming change was committed as `5936e618` and pushed to `origin/original`.
  No new release or Pages deployment has been published. Full gameplay acceptance
  remains the user's publication condition.

- Rechecked all native provenance hashes against current bridge/header/patch and
  shipped binaries: five targets matched. macOS ABI 2304 smoke passed again.
  Android execution remains unavailable; its provenance correctly records no smoke.
- Main strict/context, localization, Web lifecycle, eleven performance-tool fixtures
  and JavaScript syntax checks passed. The completed-delivery check against
  `origin/main` fails because the Remaster change is still active. This is an open
  delivery gate; it has not been bypassed by archiving unfinished work.
- Shared installation tooling was cherry-picked into Original as 1010208e. Its
  independent maintenance change completed verification and was archived in
  11f87081 after Unity 6000.6.7f1 compilation and all installer/release tests passed.
  The broader Remaster lifecycle remains active.

### Development-9 Web and source checkpoint

- Content-aware Unity Web build completed successfully and was packaged with its
  matching identity and checksum. Web archive SHA-256:
  `8cc3a6e38db56f8a8f5995dbb11db156cd7108133e33e5d5f9407f6490c7c2fe`.
  The isolated local browser reached the centered Reballance main menu and the
  modification list with the generated icon family and updated toggle glyphs.
- Local landing checks passed: automatic Russian selection, RU/EN toggle, seven
  preset buttons, first-tab skip link, 390px mobile width without horizontal
  overflow, and no Unity network requests. Inspected the settled mobile hero and
  desktop preset layout. The first capture immediately after resize was replaced
  with a settled capture. Full site acceptance and current Remaster gameplay
  imagery remain outstanding.
- Refreshed the macOS index. Spotlight returns exactly `/Applications/Ballance.app`
  and `/Applications/Reballance-dev.app`; filesystem and LaunchServices results
  agree. Profiles and release archives were not cleaned.
- Pushed source commits to main and original. Hosted
  [Build checks](https://github.com/iwonz/ballance/actions/runs/40232568286) passed
  native/script checks and specification/context validation, but failed the
  completed-change delivery gate because the Remaster lifecycle is unfinished.
  No release or Pages deployment was triggered.


### October 26: remaining macOS backup and browser-profile compatibility

- Found one legacy `Builds/checks/Ballance-1.0.8.app-backup` bundle that the earlier
  `.app.disabled` scan did not cover. Extended the installer suffix match narrowly,
  added a regression test, and removed the verified 102,348,863-byte bundle through
  the installer. Compared 1,027 file hashes across both installed applications and
  real saved profiles/preferences: unchanged. Local evidence is retained in
  `Builds/checks/legacy-backup-cleanup-{before,result}.json`.
- Main commit `5e818a1c` was cherry-picked with provenance onto Original as
  `52d10c9c`; Original's completed cleanup record was archived in `cb55a241` and
  pushed. Original passed 20 installer tests, 18 release-tool tests, strict specs,
  context and delivery checks. No hosted run was returned for the Original branch.
- Finder's Applications accessibility list exposed one Ballance and one
  Reballance-dev. The dedicated macOS application launcher did not expose a usable
  window; a later Finder attempt returned `cgWindowNotFound`. This is not visual
  acceptance of the user's launcher search grid.
- Used a separate Playwright Chromium profile on `http://127.0.0.1:8019/ballance/`
  with the checksum-verified published v1.0.11 Web archive. Seeded explicit synthetic
  classic-format score/progress files, then verified that the old player displayed
  `Storage fixture / 12345`. This fixture is not an earned score or a playthrough.
- Relocated resources into `original/` and generated `original.html` through the
  actual `prepare-site.py` host rewrite. In the same browser profile, the relocated
  player displayed the same records and allowed level 02 to load. All requested
  local resources, including `original/StreamingAssets/BuiltInPackages/Levels/level02.ballance`,
  returned 200/304. Only external Unity telemetry requests failed.
- Original retained IDBFS root `/idbfs/5d12899e7756fa13c9a642bd216b0069`;
  Reballance used `/idbfs/4868d52aa1adda0d03b0b851db46edf7`. Reballance's records page
  was empty, with no automatic import. After closing the player, deleted only the
  Reballance root in this disposable browser fixture, then reopened Original and
  rechecked its visible record, unlocked level and file hashes. This was a browser
  storage-isolation test, not a shipped settings-reset UI interaction; the
  `RemasterPreferences.Reset` boundary is covered separately by `EnvironmentChecks`.
- Score SHA-256 stayed `8ab922d8e519a1eaf5cb16906da932aa1dbb5fa348a43c4aeef92d158bfc36b7`;
  progress SHA-256 stayed `15c7de43630607b4e16778c3633d7bd55c3f463aa2c07e9ce3b41146de415d68`.
  Unity session telemetry changes PlayerPrefs on reload, so that entire file is not
  claimed byte-identical. Evidence: `Builds/checks/storage-after-reset.json`,
  `storage-original-requests.txt`, and `output/playwright/storage-*.png` under checks.
- Main fast checks passed: 14 strict OpenSpec items, context, 341 Russian
  translations, 20 installer tests and 18 release-tool tests. This does not close
  the active change's full gameplay or publication gates.

- Follow-up actual reset test: created a complete Original profile by running its
  player, then seeded the same explicit score/progress fixture. Enabled Reballance's
  built-in developer console by eight version-button clicks and reload; executed
  `/s reset`. The indexed Reballance DebugMode key was removed and the environment
  reset to Standard, while all three Original files (scores, progress, PlayerPrefs)
  retained their exact hashes. Evidence: `Builds/checks/resetqa-valid-before.json`
  and `resetqa-valid-after.json`. A preliminary `s reset` without the required slash
  only logged text and is not counted as execution. An earlier fixture without its
  parent directory record was discarded and recreated through normal Original startup.
- Opening the developer console exposed a missing `_MainTex` property on the legacy
  UI blur shader, followed by recursive uGUI rebuild diagnostics. The successful
  reset boundary does not mean console rendering passed; its correction and a new
  player check are still required.


### Complete directed lifecycle matrix (development 10)

- Ran `Ballance2.Editor.RemasterRuntimeChecks.Run` in Unity 6000.6.7f1's normal
  rendered editor on Apple M3; no batchmode or headless flag. All 91 unique
  level/preset combinations passed, covering 476 sector activations, normal menu
  loading, three native ball bodies, pause/resume, interrupted weather selection,
  fall/life loss, checkpoint recovery, restart and native-world release.
- Report: `Builds/checks/runtime-20261026-065537/report.json`; 91 screenshots are
  beside it. Duration 2,939.83 seconds; process exit 0, `passed: true`, no game
  errors, no editor diagnostics and no exit errors. All 1,027 installed-app and
  real-profile file hashes remained unchanged after the complete run.
- Earlier attempts exposed a missing HUD CanvasGroup from Unity's missing-component
  wrapper, and shared-material UV mutation by TextureScroller. The component
  creation paths now use TryGetComponent; scrolling uses renderer property blocks.
  The preflight also correctly recognizes `Reballance-dev.app`. The old built-in
  cloud plane is removed on environment attachment; custom levels retain theirs.
- The runner sets sectors and creates balls directly. This evidence does not count
  as any of the 91 required complete playthroughs, actual transformation-trigger
  interaction, calibrated input replay, audible output or a performance capture.
  The later cloud-clearance and console-material corrections require their own
  rendered checks; this matrix predates those two visual changes.


### Cloud clearance and diagnostic console follow-up

- Kept the lower volume 600 units below the lowest covered floor; its shader
  integration band extends at most 408 units above the base. Lower platforms no
  longer sit inside that volume. The seven-preset art render, all three quality
  settings, thirteen stock palettes, headings, motion and translation checks passed.
  Final art log: `/tmp/reballance-dev10-art-final.log`.
- Shortened existing light taps to the billow scale and used their directional
  density difference for shading, without more ray steps, samples or larger caches.
  Compared cached/direct clouds and several headings. A sharper density experiment
  looked worse and was reverted; its captures remain in
  `Builds/checks/cloud-density-dev10-rejected`. Distant blur and final reference-art
  fidelity still need visual/performance acceptance; these renders do not certify it.
- Added the uGUI per-renderer `_MainTex` property to the existing console blur
  shader. A new play-mode check opens/renders the real diagnostic console before
  running the requested scenario. `level06/Moscow` passed all five sectors with no
  game or exit errors, including the corrected cloud clearance. Evidence:
  `Builds/checks/runtime-20261026-075027/report.json`, process exit 0. The subsequent
  light-tap adjustment is covered by the art renders, not a second full lifecycle matrix.
- The user cannot open the macOS launcher now. Its visual check remains explicitly
  unrun; this does not change the verified file/index cleanup.

- `RemasterUIVisualChecks.Run` completed with exit 0 and
  `REMASTER_UI_PREVIEWS_RENDERED` in `/tmp/reballance-dev10-ui.log`: 656 page/HUD/control
  captures across four languages and portrait/Full HD/QHD/4K. New checks require
  actual HUD CanvasGroup, Shadow and Outline components. Inspected the Russian
  portrait main menu and Full HD HUD; full keyboard/touch navigation remains open.
  The ABI checker also passed all 92 delegates.

### Development 10 packaged-player follow-up

- Content-aware macOS and WebGL builds completed successfully with Unity
  6000.6.7f1. Both development-10 archives and SHA-256 sidecars were packaged;
  macOS packaging verified ARM64, ad-hoc signing, ABI 2304 smoke and archive contents.
- Installed `/Applications/Reballance-dev.app` 1.0.0-dev.10 using the verified
  installer, which removed its unpacked source after replacement. The remaining
  834 baseline Original/profile files matched their prior hashes. The inventory
  `Builds/checks/dev10-app-inventory.json` and Spotlight both showed only this app
  and `/Applications/Ballance.app` 1.0.11 in the inspected roots.
- Opened the installed native player through macOS UI automation. The generated
  Reballance title, first-level loading, movement, pause, settings and interrupted
  Moscow/London selection worked. Restored Moscow before quitting. The graphics
  page reported Medium, Auto, 100% and actual 1280 x 800. The HUD had corner
  counters without solid panels or a centered preset label. This smoke did not
  complete a level or measure frame-time percentiles; the application log had no
  gameplay exception. External Unity telemetry connection failures remain present.
- Native observation still shows overly large, soft cloud forms. The current
  cloud presentation is not accepted against the reference artwork. Further
  shape/detail and cost calibration is required before visual completion.
- Five context-checker self-tests and Web lifecycle checks passed again. The
  macOS launcher grid remains uninspected because no usable launcher window was
  available and the user cannot open it now.

- Web archive SHA-256: `d9cdb2a75b5ee09b8b43c067b1b2af90bb618e0d1fb4867e8a08ce49fdb6902d`;
  macOS archive SHA-256: `6ffcfeeb2a0f1c055066b2b4dcd3bda387d5535bbbd10b9c63977485edd0b67c`.
- Ran the Web player in an isolated Chrome 155 profile on Apple M3, with Unity and
  the native game closed. The 1280 x 768 CSS viewport at DPR 2 produced a 2560 x 1439
  canvas and displayed 1920 x 1079 internal 3D resolution, Medium/Auto/100%.
  `dev10-frame-capture-1793002727365.json` recorded 59.96 FPS in the animated menu
  over 30 seconds, frame p95/p99 18.6/19.7 ms and CPU callback p95/p99 4.4/5.2 ms.
- Two subsequent Moscow level-01 captures recorded 29.96/29.86 FPS, frame p95
  35.8/35.8 ms and p99 40.2/44.5 ms. Evidence and before/after images are beside
  `Builds/checks/output/playwright/dev10-moscow-capture-1793003148026.json`.
  These runs fail the performance target. Chrome's UI showed Energy Saver enabled;
  `pmset -g batt` reported 17% and discharging despite the AC Power label. The
  hardware power condition must be resolved before a qualified comparison.
  A separate visible blank-page animation calibration returned 29.9914 Hz, so the
  approximately 30 FPS interval is also present without Unity rendering.
- Raw WebGL GPU query p95 values were 82.99 ms in the menu and 205.41/204.01 ms in
  Moscow despite much shorter draw-submission intervals. All queries resolved
  without a disjoint flag, but these timings cannot be treated as per-frame GPU
  execution cost without investigating the Metal/browser timing behavior. No GPU
  budget pass is claimed. Initial screenshot attempts in an inactive/oversized
  test window timed out; explicit foregrounding and a Retina context produced the
  retained captures. These observations do not substitute for complete gameplay.

### Development 10 console and rejected cloud candidate

- Enabled diagnostic mode through the real About/version control in the isolated
  Web profile, reloaded, and opened the console with F12. The console and its
  background blur rendered without the former missing `_MainTex` exception or
  recursive Graphic errors. Evidence: `Builds/checks/output/playwright/dev10-console-report.json`
  and `dev10-console-canvas.png`. A normal page screenshot timed out; the retained
  image is the actual 2560 x 1439 WebGL canvas captured during an animation frame.
  External connection failures, a test-host 404 and FMOD metadata-query warnings
  remain in the log. This does not establish audible sound acceptance.
- Compared a smaller-scale density volume, a second billow octave and jittered
  ray starts against the packaged development-10 baseline. The editor art check
  compiled and completed (`/tmp/reballance-cloud-jitter-candidate.log`, exit 0),
  but visible folds/ridges made the result unsuitable. Preserved the rejected
  images, shader, authoring source and volume in
  `Builds/checks/cloud-detail-jitter-rejected`, then restored those three source
  files to commit `154d0920`. No rejected cloud change enters a player or release.
- Corrected the first Remaster tutorial greeting to Reballance in all four
  maintained languages. Legacy tutorial text and third-party notices are intact.
  This resource correction is not yet included in the installed development-10
  player and still needs its next content build and rendered tutorial check.
- Re-ran `check-localization.py` (341 Russian translations across 12 resources),
  strict OpenSpec validation (14 items), `check-agent-context.py` (13 capabilities)
  and `git diff --check`; all passed for the greeting and evidence update.
- CI run `40292790386` passed specification/context validation and the shipped
  ARM64 checks; its completed-change-record step failed as expected for this
  active, unfinished change. No release tag, release or Pages publication occurred.
- The battery subsequently reached 9% and was still discharging despite the AC
  Power label. Native UI capture again returned `cgWindowNotFound`. Closed the
  isolated test browser and its localhost server after retaining the evidence;
  further qualified frame measurements need a charging, available test machine.
  Full playthroughs and remaining acceptance items are still unchecked.
