# Ballance

The classic maintenance branch of the Unity recreation forked from
**[imengyu/Ballance](https://github.com/imengyu/Ballance)**, with the original 13 levels, native
IVP physics, custom-level interfaces, built-in mods, and English, Chinese, and
Russian UI. This fork uses **Unity 6.6 (6000.6.7f1)**.

[Play Ballance in your browser](https://iwonz.github.io/ballance/) ·
[Download releases](https://github.com/iwonz/ballance/releases) ·
[Agent instructions](AGENTS.md) · [OpenSpec workflow](openspec/README.md)

![Ballance logo](Assets/Game/Common/Textures/UI/LogoPicture.jpg)

This is the classic Unity edition for the familiar experience, preserved from
[v1.0.11](https://github.com/iwonz/ballance/releases/tag/v1.0.11), commit `d066bfe5`.
The annotated [original/v1.0.11 source tag](https://github.com/iwonz/ballance/tree/original/v1.0.11)
is immutable. This project is a Unity reconstruction, not
the original commercial Cyparade executable. Menus, tutorial, tracks and gameplay
remain classic; this branch accepts necessary fixes and compatibility maintenance.

## Project structure

| Branch | Game | Responsibility |
| --- | --- | --- |
| `original` (this checkout) | Ballance | Classic gameplay, fixes and compatibility |
| [`main`](https://github.com/iwonz/ballance/tree/main) | Reballance | Active development, shared site and unified publication |

Use separate Git worktrees and Unity caches. Transfer applicable fixes as focused,
verified `cherry-pick` commits; do not merge the complete branches. Each game keeps
its own application identity, settings, saves and source-version sequence.

New Ballance source tags use `original/v<version>`; Reballance uses `remaster/v<version>`.
A new **public release** is a single `release/v<version>` distribution on main containing
both games for all five platforms, their checksums and the complete two-player site.
Updating one game preserves the other game's verified files unchanged. Do not publish
an independent classic release. Follow the [shared release procedure](https://github.com/iwonz/ballance/blob/main/docs/build-and-release.md#github-pages).

The current public game remains Ballance 1.0.11 while Reballance finishes gameplay
acceptance. The [main project guide](https://github.com/iwonz/ballance#readme) contains
both editions' status, galleries and development/release instructions.

## Original game rights

This is a learning project. The code license and separately copyrighted game content
have different scopes; see [Credits and rights](https://github.com/iwonz/ballance/blob/main/CREDITS.md).
The following upstream notice applies to inherited content in both games and is retained verbatim.

**Note: Ballance belongs to Cyparade's work, and the copyright belongs to the original developer Cyparade. The development of this project is for learning purposes only and cannot be used for any commercial purposes. This project follows the GPL-3.0 protocol, but is limited to the code of this project. Any 3D model data, physical engine, images, audio and video, and game data related to Ballance are not within this scope. Any use of these game data for commercial activities without the permission of the copyright owner is illegal.**

## Play and download

| Platform | Release asset | Requirements / launch |
| --- | --- | --- |
| Web | `Ballance-<version>-WebGL.tar.gz` | Play on GitHub Pages in a browser with WebGL 2 and WebAssembly |
| Windows | `Ballance-<version>-Windows.zip` | x86-64, Windows 10 21H1+; extract and run `Ballance.exe` |
| Linux | `Ballance-<version>-Linux.tar.gz` | x86-64, Ubuntu 22.04/24.04; extract and run `./Ballance.x86_64` |
| macOS | `Ballance-<version>-macOS.zip` | Apple Silicon, macOS 13.1+; extract `Ballance.app` |
| Android | `Ballance-<version>-Android.apk` | ARM64, Android 8.0+; install the test-signed APK |

See [Unity player requirements](https://docs.unity3d.com/6000.6/Documentation/Manual/system-requirements.html#player)
for graphics/driver requirements. macOS is ad-hoc signed without notarization;
see [first-launch notes](docs/macos-apple-silicon.md). **iOS is deferred.** Historical
Intel macOS and Unity 2021 builds remain in
[upstream releases](https://github.com/imengyu/Ballance/releases).

Keyboard controls and mobile touch layouts can be changed in Settings -> Controls.
The game includes level preview and package management. External C# DLL mods require
Mono desktop builds; built-in mods are compiled into all current players. The retained
NMO importer requires legacy Windows 32-bit Virtools tooling and is not a supported
feature of the current Windows x64 release.

## Develop

1. Clone this repository and install the pinned Unity editor with the modules you need.
2. Read [AGENTS.md](AGENTS.md) and complete the [OpenSpec setup](openspec/README.md#tooling).
3. Open `Assets/Scenes/Main.unity`, select `GameEntry`, and set Debug Type to `NoDebug`
   for the normal game flow. Enter Play mode.
4. Find the relevant capability in the [implementation map](docs/architecture.md),
   then use the full OpenSpec workflow before changing code or documentation.

| Guide | Purpose |
| --- | --- |
| [OpenSpec](openspec/README.md) | Proposal, specs, design, tasks, apply, verify, sync, archive |
| [Implementation map](docs/architecture.md) | Capability/source ownership, integration boundaries, known gaps |
| [Validation](docs/validation.md) | Check selection, commands, and honest coverage limits |
| [Build and release](docs/build-and-release.md) | Complete player/content builds, release assets, GitHub Pages |
| [macOS](docs/macos-apple-silicon.md) | ARM64 plugin, signing, and first-launch details |
| [Translations](HelpTranslate.md) | Multilingual game resources and rendered UI validation |
| [Native physics](BallancePhysics/README.md) | C++/C# bridge and reproducible plugin rebuilds |

All maintained documentation, OpenSpec artifacts, and source comments are English.
Game localization and original license notices retain their own language.

## Debugging

Click the version in the About page eight times, accept the developer-mode prompt,
and restart. F12 opens the console; `help` lists commands. Debug movement uses Q/E
to raise/lower the ball. `highscore open-all` unlocks built-in levels; `quit-dev`
leaves developer mode. Use a test profile when changing scores or settings.

## Credits and third-party components

**[imengyu/Ballance](https://github.com/imengyu/Ballance)** by **mengyu / Happy Dreamfish (imengyu)** is the upstream Unity recreation this project was forked from. Cyparade created the original game; iwonz maintains this fork and Reballance. See the [shared Credits inventory](https://github.com/iwonz/ballance/blob/main/CREDITS.md) for the full component notices and license boundaries.
Native physics derives from [source-physics](https://github.com/nillerusr/source-physics).
Bundled components include [QuickOutline](https://github.com/chrisnolet/QuickOutline),
[Unity SharpZipLib](https://docs.unity3d.com/Packages/com.unity.sharp-zip-lib@1.4/manual/index.html),
[Graphy](https://github.com/Tayx94/graphy),
[RuntimeSceneGizmo](https://github.com/yasirkula/UnityRuntimeSceneGizmo),
[RuntimeInspector](https://github.com/yasirkula/UnityRuntimeInspector), and
[SimpleFileBrowser](https://github.com/yasirkula/UnitySimpleFileBrowser).
Retain their notices under `Assets/Plugins` and `Assets/Resources/ThirdPartAllLincnse`.

## Screenshots

Actual Ballance 1.0.11 Web captures. See [both galleries and capture provenance](https://github.com/iwonz/ballance#screenshots) for more screens and Reballance development captures.

| Main menu | Gameplay |
| --- | --- |
| ![Ballance main menu](https://raw.githubusercontent.com/iwonz/ballance/main/docs/images/ballance-1.0.11-menu.png) | ![Ballance level 2](https://raw.githubusercontent.com/iwonz/ballance/main/docs/images/ballance-1.0.11-gameplay.png) |
