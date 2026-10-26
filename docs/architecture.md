# Implementation map

Start with the relevant capability below, then follow its source links and callers.
This map describes the current implementation; keep it current when code changes. Behavioral
contracts live in [OpenSpec specs](../openspec/specs), and execution evidence belongs
in change records. Check selection is centralized in [validation.md](validation.md).

## Repository boundaries

| Path | Responsibility |
| --- | --- |
| `Assets/System` | Startup, package/services framework, UI primitives, resource paths, editor checks |
| `Assets/Game` | Core package, gameplay, menus, original levels, level loading and editor authoring |
| `Assets/Game/Remaster` | Remaster environment, rendering, UI and isolated settings/records |
| `site` | Static bilingual landing and explicit two-edition release manifest |
| `Assets/Packages` | Six built-in resource/mod packages and package templates |
| `Assets/BallancePhysics` | Managed native-physics wrapper, editor inspectors, shipped plugins |
| `BallancePhysics` | C++ bridge and pinned-source patch; see its AGENTS.md |
| `VirtoolsNMOLoader` | Legacy Windows importer and bundled Virtools SDK headers; not the current player pipeline |
| `Assets/Plugins` | Mostly third-party components; `AndroidInput.cs` is project-owned |
| `Packages`, `ProjectSettings` | Unity dependency locks, editor version, player/graphics/input configuration |
| `scripts`, `.github/workflows` | Local checks, native rebuilds, player packaging, CI, and Pages publication |
| `Tools`, `Other`, `DemoImages` | Historical authoring/reference material, not generated release inputs |

Most first-party runtime code compiles into Unity's default Assembly-CSharp;
editor folders compile separately. Vendor asmdefs define their own boundaries.
AssetBundle contents still need player-side types: see [link.xml](../Assets/link.xml).
Serialized field/type names, `.meta` GUIDs, prefab hierarchy names, package IDs,
event strings, and JSON/XML keys are compatibility interfaces, including historic
spellings such as `Wapper`, `Modul`, and `Scense`.

## Runtime lifecycle

[Contract](../openspec/specs/runtime-lifecycle/spec.md).
[GameEntry](../Assets/System/Scripts/Entry/GameEntry.cs) in
[Main.unity](../Assets/Scenes/Main.unity) starts
[GameSystem](../Assets/System/Scripts/GameSystem.cs) and
[GameSystemInit](../Assets/System/Scripts/Services/Init/GameSystemInit.cs).
Pre-initialization loads settings and language; initialization registers the
mediator, manager, packages, UI, time, pools, sound, and physics.
[GameManager](../Assets/System/Scripts/Services/GameManager.cs) reads
[SystemInit.xml](../Assets/System/Resources/SystemInit.xml), loads core/system/user
packages, and switches logical scenes. These are event-driven states within the
main Unity scene, not separate player scene builds.
[GameMediator](../Assets/System/Scripts/Services/GameMediator.cs) and
[GameEventEmitter](../Assets/System/Scripts/Base/GameEventEmitter.cs) provide the
global/package and object-level event paths. Read teardown as well as registration
when adding listeners. Check editor startup plus a player menu/level/quit cycle.

Unity async operations use native Unity 6 await support. Package APIs retain
`Task<bool>` for mod compatibility; coroutine startup consumers wait for completion
and call `GetAwaiter().GetResult()` so faults/cancellation propagate. The legacy
AsyncAwaitUtil runner is removed. Gameplay coroutines still own frame/state sequences.

## Package management

[Contract](../openspec/specs/package-management/spec.md).
[GamePackageManager](../Assets/System/Scripts/Services/GamePackageManager.cs) owns
discovery, dependency/version checks, state, and load/unload events.
[GamePackage](../Assets/System/Scripts/Package/GamePackage.cs) owns metadata,
resources, code entry points, and lifecycle hooks;
[GameZipPackage](../Assets/System/Scripts/Package/GameZipPackage.cs) reads the
`.ballance` ZIP containing metadata, language/logo resources, and an AssetBundle.
Levels use a different bundle format despite sharing the `.ballance` extension.
[GamePathManager](../Assets/System/Scripts/Res/GamePathManager.cs) resolves package
resources; [PathUtils](../Assets/System/Scripts/Utils/PathUtils.cs) preserves
HTTP/HTTPS/jar/file semantics.
[ZipUtils](../Assets/System/Scripts/Utils/ZipUtils.cs) uses the pinned Unity registry
package `com.unity.sharp-zip-lib` 1.4.2. Reads locate entries through the central
directory, accept legacy leading-slash metadata names, check size and CRC, and cap
each expanded entry at 512 MiB. Async reads yield between 1 MiB chunks through
`Awaitable.NextFrameAsync`, without Web worker threads. Local archives use a file
stream; HTTP/jar archives still download into memory before indexed reads. This is
not network streaming or an aggregate memory budget. Writers copy file streams
directly and let the library calculate CRC. Requests and archive streams are disposed.

The three built-in mods under `Assets/Packages/mod.*` are compiled into the player;
their historical DLL imports are disabled. External DLL loading is Mono-only.
`PackageStatus.xml` persists choices. Trust UI is not a sandbox: inspected code
defaults newly discovered entries to trusted, and the two trust-result accessors
appear reversed. Treat this as an existing gap requiring a separate security change,
not a guarantee that arbitrary code is safe. Check package discovery, dependencies,
enablement, resources, and cleanup on every affected backend.

## Level loading

[Contract](../openspec/specs/level-loading/spec.md).
[Core package entry](../Assets/Game/PackageEntry.cs) routes `CoreStartLoadLevel` to
the Level state. [LevelBuilder](../Assets/Game/LevelBuilder/Scripts/LevelBuilder.cs)
coordinates loading, validation, dependency checks, mechanism replacement, floors,
sectors, lighting, music, preview, and cleanup.
[LevelLoaderNative](../Assets/Game/LevelBuilder/Scripts/LevelLoaderNative.cs) loads
`Level.json` and `Level.prefab`; [LevelJson](../Assets/Game/LevelBuilder/Scripts/LevelJson.cs)
defines the persisted format. The 13 shipped definitions live in `Assets/Game/Levels`.
[GamePreviewManager](../Assets/Game/GamePlay/Scripts/Preview/GamePreviewManager.cs)
and [StartCustomLevel](../Assets/Game/Menu/Scripts/MenuLevel/StartCustomLevel.cs)
own preview/custom-level entry.

[LevelLoaderNMO](../Assets/Game/LevelBuilder/Scripts/LevelLoaderNMO.cs) and
[VirtoolsLoader](../Assets/Game/Common/Scripts/VirtoolsLoader/VirtoolsLoader.cs) are
legacy integrations requiring a Windows 32-bit Virtools helper; they do not establish
NMO support in the current Windows x64 release. Test missing/malformed content,
dependency failure, preview permission, unload/reload, and target bundle compatibility.

## Gameplay

[Contract](../openspec/specs/gameplay/spec.md).
[GamePlayInitManager](../Assets/Game/GamePlay/Scripts/GamePlayInitManager.cs) creates
managers and UI from core prefabs.
[GamePlayManager](../Assets/Game/GamePlay/Scripts/GamePlayManager.cs) owns the run,
points/lives, fall/rebirth, pause, transformation, and ending flow.
[BallManager](../Assets/Game/GamePlay/Scripts/BallManager.cs) owns ball registration,
forces, control state, and effects; [CamManager](../Assets/Game/GamePlay/Scripts/CamManager.cs)
provides camera-relative directions.
[SectorManager](../Assets/Game/GamePlay/Scripts/SectorManager.cs) activates and resets
mechanisms through [ModulBase](../Assets/Game/GamePlay/Scripts/ModulBase/ModulBase.cs).
`Balls`, `Moduls`, `Tranfo`, and `Other` below that gameplay directory implement the
materials, obstacles, transformers, pickups, tutorial, and UFO ending.

Ball/floor parameters are in [GamePhysBall](../Assets/Game/GamePlay/Scripts/Data/GamePhysBall.cs)
and [GamePhysFloor](../Assets/Game/GamePlay/Scripts/Data/GamePhysFloor.cs).
Do not substitute Unity Rigidbody physics for IVP. Validate movement, collision,
transformation, checkpoint recovery, zero-life failure, pause/resume, restart,
completion, and return to menu; one successful level load does not cover these.

## Remaster environment and editions

[Active contract](../openspec/changes/build-original-and-remaster-editions/specs/remaster-environment/spec.md);
[edition maintenance](editions.md). The Original worktree and immutable baseline
are independent of `main`; there is no runtime edition switch. The Remaster is in
development, with outstanding acceptance recorded in the active change.

[EnvironmentState](../Assets/Game/Remaster/Scripts/EnvironmentState.cs) owns the seven
preset IDs and simulation-time run state.
[LevelEnvironmentProfile](../Assets/Game/Remaster/Scripts/LevelEnvironmentProfile.cs)
validates separate resource profiles for thirteen built-in levels.
[EnvironmentController](../Assets/Game/Remaster/Scripts/EnvironmentController.cs)
attaches at level completion, subscribes to IVP's simulation loop and releases on
unload. GamePlayManager/SectorManager connect restart, recovery and checkpoints;
preview uses the same profiles. Attaching a built-in profile removes the legacy
scrolling cloud plane, including its restart/settings/preview reference. Texture
scrolling for custom content uses renderer properties without changing shared
materials. Custom content without a matching built-in path
keeps authored visuals and dry physics.

[EnvironmentSurfaces](../Assets/Game/Remaster/Scripts/EnvironmentSurfaces.cs) binds
local regions to physical transforms and render property blocks.
[EnvironmentPresentation](../Assets/Game/Remaster/Scripts/EnvironmentPresentation.cs)
owns sky, lighting and precipitation lifetimes, placing the lower cloud volume
below the lowest covered floor.
[RemasterSkyRenderer](../Assets/Game/Remaster/Scripts/RemasterSkyRenderer.cs) renders
the sky into bounded cubemaps, updates one face per frame from a fixed snapshot,
and blends completed captures. It restores the source sky after the scene pass;
reflections retain the volumetric material.
[SurfaceState](../Assets/BallancePhysics/Api/SurfaceState.cs) is shared with the
native bridge. The Standard-based surface and layered-sky shaders live in
`Assets/Game/Remaster/Resources/Remaster`.
[Material authoring](../Assets/System/Scripts/Editor/RemasterMaterialAuthoring.cs)
updates shared materials without changing collision meshes; source assets and
licenses are documented in [remaster-art.md](remaster-art.md).

Known Remaster gaps: floor footprints and sector weather remain draft placements;
moving supports, weather sound, vegetation, silhouettes and all-level playability
need implementation or acceptance. Native builds disable floating-point contraction
to preserve the shipped Original's dry replay; the diagnostic comparison matched
all three balls exactly. Full Unity input/camera/mechanism acceptance remains open.

## Physics runtime

[Contract](../openspec/specs/physics-runtime/spec.md).
[PhysicsApi](../Assets/BallancePhysics/PhysicsApi.cs),
[ApiDefine](../Assets/BallancePhysics/Api/ApiDefine.cs), and
[ApiStruct](../Assets/BallancePhysics/Api/ApiStruct.cs) define the managed ABI.
[PhysicsEnvironment](../Assets/BallancePhysics/Wapper/PhysicsEnvironment.cs) owns
the world loop; [PhysicsObject](../Assets/BallancePhysics/Wapper/PhysicsObject.cs)
owns bodies, forces, phantoms, and callback dispatch. Constraint wrappers share
their base lifecycle. Native exports are in
[ballance_physics.cpp](../BallancePhysics/ballance_physics.cpp).
Read [native instructions](../BallancePhysics/AGENTS.md) before changes.

Web uses `__Internal` and a static `.a`; other current targets use dynamic native
libraries. Keep AOT callbacks static and rooted, and preserve destruction order.
Collision layers are independent of Unity's layer system. Run ABI and native smoke
checks, then affected player gameplay; static signature checks do not execute every
native function or cover all targets. Remaster ABI 2304 appends material-region,
world-weather and contact-state entries. The material manager refreshes persistent
contacts and applies rolling resistance/wind in the native simulation. Original
retains ABI 2302. Rebuild sources and per-target hashes are recorded in
`BallancePhysics/provenance`; target execution limits remain in verification records.

## Player input

[Contract](../openspec/specs/player-input/spec.md).
[KeyListener](../Assets/System/Scripts/Services/InputManager/KeyListener.cs) dispatches
bindings; [SettingConstants](../Assets/Game/Common/Scripts/Settings/SettingConstants.cs)
defines defaults. Android aliases use
[AndroidInput](../Assets/Plugins/AndroidInput.cs), including key conversion, mouse
delta scaling, primary touch, and EnhancedTouch. The Android build temporarily
selects Input System only and restores editor configuration afterward.
Touch controllers live in `Assets/Game/Menu/Scripts/Touch`; keypad layout and settings
live under `Menu/Scripts/Game` and `Menu/Scripts/Control`.
[InputChecks](../Assets/System/Scripts/Editor/InputChecks.cs) uses virtual devices;
physical touch layout, timing, and ergonomics still need an Android device.

## User interface

[Contract](../openspec/specs/user-interface/spec.md).
[GameUIManager](../Assets/System/Scripts/Services/GameUIManager.cs) owns registered
prefabs/pages, navigation stack, dialogs, overlays, and key listeners.
[GameUIPage](../Assets/System/Scripts/UI/Core/GameUIPage.cs) and
[GameUIMessageCenter](../Assets/System/Scripts/UI/Core/GameUIMessageCenter.cs) connect
prefab controls to handlers. [MenuManager](../Assets/Game/Menu/Scripts/MenuManager.cs)
initializes menu and gameplay screens; sibling managers provide settings, high
scores, packages, and touch UI. Read prefab bindings when changing handlers.

[GameObjectPool](../Assets/System/Scripts/Services/Pool/GameObjectPool.cs) and
[GamePoolManager](../Assets/System/Scripts/Services/GamePoolManager.cs) retain the
project API over `UnityEngine.Pool.ObjectPool`. A rented-item set enforces ownership
and total capacity, since Unity's `maxSize` only limits inactive retention. Repeated
clear returns rented items once; embedded row templates are explicitly inactive. Disposal destroys that pool's active and inactive
objects even when pools share a root. Callers handle exhausted capacity.
[ScrollView](../Assets/System/Scripts/UI/Core/Controls/ScrollView/ScrollView.cs) uses
Unity's pool directly and destroys rows beyond retained capacity. Display settings
use `Resolution.refreshRateRatio` and the explicit fullscreen-mode overload; saved
resolution indices and keys retain their existing format.

[GraphicsDeviceInfo](../Assets/Game/Menu/Scripts/Control/GraphicsDeviceInfo.cs) refreshes
the reported device/API when graphics settings become visible. It shows localized
advice for recognized software renderers, without changing preferences or running
per-frame diagnostics. Device names are displayed as literal text; an unfamiliar
name is not treated as certified hardware acceleration.

[RemasterTheme](../Assets/Game/Remaster/Scripts/RemasterTheme.cs) styles existing
uGUI pages and embeds Noto CJK for the maintained languages.
[RemasterMenuLayout](../Assets/Game/Remaster/Scripts/RemasterMenuLayout.cs) arranges
main/level menus without replacing their bound buttons;
[TutorialController](../Assets/Game/GamePlay/Scripts/Other/TutorialController.cs)
provides skip/replay and binding-aware contextual cards.
[RemasterHud](../Assets/Game/Remaster/Scripts/RemasterHud.cs) presents scores, lives
without panel backings, a preset heading or practice-run classification. Existing dialogs/package pages still require
complete visual and navigation acceptance.

[RemasterSettingsPanel](../Assets/Game/Remaster/Scripts/RemasterSettingsPanel.cs)
separates environment selection from graphics quality.
[RemasterGraphics](../Assets/Game/Remaster/Scripts/RemasterGraphics.cs) renders the
3D scene into an aspect-preserving target independently of uGUI scale, displays
actual dimensions and refreshes sky reflections.
[RemasterSafeArea](../Assets/Game/Remaster/Scripts/RemasterSafeArea.cs) fits UI roots
to mobile safe areas. The responsive Web host is under `Assets/WebGLTemplates/Remaster`.

[GameSoundManager](../Assets/System/Scripts/Services/GameSoundManager.cs) owns mixers
and registered sources; [MusicManager](../Assets/Game/GamePlay/Scripts/MusicManager.cs)
selects music/ambient themes. Ball sound effects live in `Balls/Effects`.
Check navigation, confirmation, focus, HUD updates, and audio with interaction;
browser autoplay restrictions mean source creation alone does not prove audible output.

## Localization

[Contract](../openspec/specs/localization/spec.md).
[I18NProvider](../Assets/System/Scripts/Services/I18N/I18NProvider.cs) loads English
then overlays selected-language XML values; [I18N](../Assets/System/Scripts/Services/I18N/I18N.cs)
handles lookups and formatting.
[SettingsUIControl](../Assets/Game/Menu/Scripts/Control/SettingsUIControl.cs) saves the
language before prompting restart. Resources live in `Assets/Resources/StaticLangResource.xml`,
`Assets/Game/PackageLanguageRes*.xml`, and package language files. `I18NText` prefab
bindings and the bundled fonts are part of this boundary. Original retains Bank
Gothic/Roboto; Remaster uses the licensed Noto CJK font described in the art guide.
Use [translation guidance](../HelpTranslate.md), the Python checker, editor resource
checks, and rendered UI checks. English-only maintenance prose does not apply to
language resource values or their test fixtures.

## Player persistence

[Contract](../openspec/specs/player-persistence/spec.md).
[GameSettingsManager](../Assets/System/Scripts/Services/GameSettingsManager.cs) and
[GameCoreSettingsActuator](../Assets/Game/Common/Scripts/Settings/GameCoreSettingsActuator.cs)
use [RemasterPreferences](../Assets/Game/Remaster/Scripts/RemasterPreferences.cs)
with an edition namespace and a registered-key reset boundary. Original keys are
not imported or deleted.
[RemasterRecords](../Assets/Game/Remaster/Scripts/RemasterRecords.cs) stores the
legacy score payload in one environment-independent table, using temporary files
and replacement. It validates and merges earlier preset/rules files once, preserving
the original files; the shared file prevents repeated imports.
[HighscoreManager](../Assets/Game/GamePlay/Scripts/HighScore/HighscoreManager.cs)
keeps scores and level unlocks shared within Reballance regardless of environment
selection. Score arithmetic and gameplay timers have no weather dependency. Product/application identity isolates
the native data root; Web editions use different page-directory roots.

Known limits: there is no general corrupt-file recovery or cross-device sync. Web
file replacement and browser-close persistence still need acceptance; enabling
auto-sync in the Web template is not proof of crash-safe persistence. Validate
save/reload, reset isolation and malformed data using temporary profiles.

## Developer tools

[Contract](../openspec/specs/developer-tools/spec.md).
[GameDebugCommandServer](../Assets/System/Scripts/Services/GameDebugCommandServer.cs)
registers and validates commands; [DebugConsole](../Assets/System/Scripts/Services/DebugTools/DebugConsole.cs)
displays them. Debug mode exposes flight, level/sector controls, inspection, and score
commands. Use `help` instead of assuming command names from old guides.
`Assets/Game/LevelEditor/Scripts/Editor/Modding` contains package/level makers and
packers, while `Assets/System/Scripts/Editor` contains compiler helpers, inspectors,
and current build checks. The legacy Editor logical-scene name is not evidence of a
finished standalone level editor. [Bugs.txt](../Bugs.txt) preserves unverified historical
notes; reproduce them before proposing fixes.

[Web performance capture](../scripts/measure-web-performance.js) measures game draw
submission cadence externally in an isolated browser profile. The
[report gate](../scripts/check-web-performance.py) and its
[adversarial fixtures](../scripts/test-web-performance.py) reject slow or unqualified
captures. [Validation guidance](validation.md#web-frame-performance) owns the
reference profile, budget, commands, and limits; hosted tests validate the gate,
not hardware FPS.

## Dependency and source-mod migration

The vendored `ICSharpCode.SharpZipLib.dll`, AsyncAwaitUtil, unused FastCachePool, and
SimpleObjPool are removed. External source mods using those implementation helpers
must migrate to `Unity.SharpZipLib`, native Unity awaits/`Awaitable`, and
`UnityEngine.Pool`. Existing package `Task<bool>` methods and serialized pool fields
remain compatible. The generic pool facade now creates parameterless instances on
an empty `Get`; callers must return only objects obtained from that pool.
`ZipUtils.AddFileToZip` now takes the stream, file path, and archive entry name;
readers use `FindEntry` and `ReadEntry`/`ReadEntryAsync` instead of sequential ZIP helpers.
Archives with an individual expanded entry above 512 MiB are now rejected.

Local vendor patches are limited to supported Unity APIs: AssetUsageDetector uses
Shader property reflection, Graphy uses refresh ratios and AssetPackage import,
and RuntimeTransformGizmo uses `FindAnyObjectByType` for its optional single-controller lookup.
Scenes with multiple controllers must assign the existing serialized reference.
Retain their licenses when updating vendors. The deprecated VS Code integration
package is removed; Unity's Visual Studio editor integration remains installed.

## Platform builds

[Contract](../openspec/specs/platform-builds/spec.md).
[PlayerBuild](../Assets/System/Scripts/Editor/PlayerBuild.cs) is the content-aware
entry point. It builds the core, all package definitions, and 13 levels, stages content
for mobile/Web or copies it beside desktop data, configures target backends, and
cleans staging. [PackagePacker](../Assets/Game/LevelEditor/Scripts/Editor/Modding/PackagePacker.cs)
and [LevelPacker](../Assets/Game/LevelEditor/Scripts/Editor/Modding/LevelMaker/LevelPacker.cs)
produce their distinct bundle formats.
[package-build.py](../scripts/package-build.py) validates and packages each platform;
[package-macos.py](../scripts/package-macos.py) owns ARM64 signing and round-trip checks.
Use [build-and-release.md](build-and-release.md) for commands, platform/signing limits,
and CI requirements. Native rebuild provenance lives in
[build-physics.py](../scripts/build-physics.py), not an unpinned external checkout.

## Web publication

[Contract](../openspec/specs/web-publication/spec.md).
[Pages workflow](../.github/workflows/github-pages.yml) selects the unified distribution
pin from `main`, checks out its immutable `release/v*` source and deploys the attached
site; it does not build Unity. [release_identity.py](../scripts/release_identity.py)
owns source/distribution tag parsing and the schema-2 five-platform manifest.
[prepare-release.py](../scripts/prepare-release.py) stages and verifies both sets of
five platform artifacts, notices and the complete site archive.
[prepare-pages.py](../scripts/prepare-pages.py) validates individual Web archive
safety, checksum, size, content and identity. [prepare-site.py](../scripts/prepare-site.py)
stages the landing and both players atomically, adds edition navigation and enforces
the combined Pages size limit. Original's HTML stays in the previous page directory.
[verify-site.py](../scripts/verify-site.py) checks the distribution and both public
identities. Null distribution/Reballance pins intentionally prevent deployment.

The [release guide](build-and-release.md#github-pages) owns setup and independent
rollback through complete distribution releases. HTTP identity checks do not cover
gameplay: test both public players, preserved Original storage, navigation, menu,
control, fall/rebirth and return. [README](../README.md) introduces both games;
[Credits](../CREDITS.md) owns upstream attribution and the preserved rights notice.

## Known gaps

- Dormant ray-query wrappers need a separately verified repair before use: the managed
  single-hit wrapper calls the object-query delegate; the multi-hit wrapper leaves its
  lists uninitialized, and native multi-hit allocation/iteration/cleanup is inconsistent.

## Agent development

[Contract](../openspec/specs/agent-development/spec.md).
[AGENTS.md](../AGENTS.md) is the entry point;
[OpenSpec guide](../openspec/README.md) owns the lifecycle;
[setup-openspec.sh](../scripts/setup-openspec.sh) regenerates local skills.
The context checker and its self-tests validate structure and delivery records;
strict OpenSpec validation owns requirement/delta parsing. These are guardrails,
not substitutes for reading changed code or reviewing scenario evidence.
