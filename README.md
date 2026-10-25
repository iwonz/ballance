# Ballance and Reballance

Two independently maintained editions of the open-source Unity recreation of
Ballance. Both retain the 13 classic levels, native IVP physics, custom-level
interfaces, built-in mods, and English, Chinese, and Russian localization.
The pinned editor is **Unity 6.6 (6000.6.7f1)**.

[Play Ballance in your browser](https://iwonz.github.io/ballance/) ·
[Download releases](https://github.com/iwonz/ballance/releases) ·
[Agent instructions](AGENTS.md) · [OpenSpec workflow](openspec/README.md)

![Ballance logo](Assets/Game/Common/Textures/UI/LogoPicture.jpg)

## Editions

**Ballance** is the preserved classic experience, with the
existing visuals, menus, tutorials and gameplay. Its immutable baseline is
[v1.0.11](https://github.com/iwonz/ballance/releases/tag/v1.0.11), commit
`d066bfe5`, also named [original/v1.0.11](https://github.com/iwonz/ballance/tree/original/v1.0.11). The `original` branch is for compatible
maintenance fixes. This is the classic Unity reconstruction, not the commercial
Cyparade game or its original executable.

**Reballance** is developed on `main`: new presentation, interface and
authored weather with independent saves and releases. It is **under development**;
the published browser game remains Ballance until a verified Remaster is available.
See the [active implementation record](openspec/changes/build-original-and-remaster-editions/proposal.md)
and [edition maintenance guide](docs/editions.md) for scope and current boundaries.

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

The original Unity remake is by mengyu; this fork continues its code and content.
Native physics derives from [source-physics](https://github.com/nillerusr/source-physics).
Bundled components include [QuickOutline](https://github.com/chrisnolet/QuickOutline),
[Unity SharpZipLib](https://docs.unity3d.com/Packages/com.unity.sharp-zip-lib@1.4/manual/index.html),
[Graphy](https://github.com/Tayx94/graphy),
[RuntimeSceneGizmo](https://github.com/yasirkula/UnityRuntimeSceneGizmo),
[RuntimeInspector](https://github.com/yasirkula/UnityRuntimeInspector), and
[SimpleFileBrowser](https://github.com/yasirkula/UnitySimpleFileBrowser).
Retain their notices under `Assets/Plugins` and `Assets/Resources/ThirdPartAllLincnse`.

![Original levels](DemoImages/11.jpg)
![Level preview](DemoImages/1.jpg)
