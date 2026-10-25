# Implementation map

Start with the relevant capability below, then follow its source links and callers.
This map describes the implementation at the agent-development baseline, based on
release 1.0.9 / commit `52a1835f`; keep it current when code changes. Behavioral
contracts live in [OpenSpec specs](../openspec/specs), and execution evidence belongs
in change records. Check selection is centralized in [validation.md](validation.md).

## Repository boundaries

| Path | Responsibility |
| --- | --- |
| `Assets/System` | Startup, package/services framework, UI primitives, resource paths, editor checks |
| `Assets/Game` | Core package, gameplay, menus, original levels, level loading and editor authoring |
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
HTTP/HTTPS/jar/file semantics. ZIP reads yield without relying on Web worker threads.

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
native function or cover all targets.

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
bindings and the bundled Bank Gothic/Roboto font fallback are part of this boundary.
Use [translation guidance](../HelpTranslate.md), the Python checker, editor resource
checks, and rendered UI checks. English-only maintenance prose does not apply to
language resource values or their test fixtures.

## Player persistence

[Contract](../openspec/specs/player-persistence/spec.md).
[GameSettingsManager](../Assets/System/Scripts/Services/GameSettingsManager.cs) and
[GameCoreSettingsActuator](../Assets/Game/Common/Scripts/Settings/GameCoreSettingsActuator.cs)
store package-prefixed PlayerPrefs values. Reset clears all PlayerPrefs, so do not
run it against a user's real profile during tests.
[HighscoreManager](../Assets/Game/GamePlay/Scripts/HighScore/HighscoreManager.cs) reads
`HighscoreData.json` (JSON strings inside its `data` and `names` fields) and
`LevelPassStateData.json` from persistent storage. Preserve this existing format.
Package state uses XML; mobile/Web writable content paths differ from shipped content.

Known gaps: high-score parsing has no general corrupt-file recovery; writes are not
an atomic transaction; browser close does not guarantee the game's normal quit/save
path. Do not promise crash-safe or cross-device persistence. Validate with temporary
profiles/files and include save/reload and malformed-data behavior when changing it.

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
[Pages workflow](../.github/workflows/github-pages.yml) deploys existing release
assets; it does not build Unity.
[prepare-pages.py](../scripts/prepare-pages.py) checks checksum, archive safety,
size, complete Web content, and tag/archive/player version agreement. Every release
must pass this deployment; missing Web assets fail rather than skip the workflow.
The generated `release.json` is checked over HTTP after deployment to confirm the
requested release is live. gzip fallback and single-threaded physics avoid
custom server headers. Asset requests must retain the repository URL prefix.
The [release guide](build-and-release.md#github-pages) owns setup and rollback steps.
Test the served URL, menu, first level, control, fall/rebirth, and return to menu;
an HTTP 200 response alone is not a gameplay check.

## Agent development

[Contract](../openspec/specs/agent-development/spec.md).
[AGENTS.md](../AGENTS.md) is the entry point;
[OpenSpec guide](../openspec/README.md) owns the lifecycle;
[setup-openspec.sh](../scripts/setup-openspec.sh) regenerates local skills.
The context checker and its self-tests validate structure and delivery records;
strict OpenSpec validation owns requirement/delta parsing. These are guardrails,
not substitutes for reading changed code or reviewing scenario evidence.
