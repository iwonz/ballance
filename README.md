# Ballance

The familiar journey above the clouds.

**Ballance** is iwonz's maintained fork of
**[imengyu/Ballance](https://github.com/imengyu/Ballance)**, the Unity recreation of
Cyparade's game. It adds Russian localization, compatibility improvements and
multiplatform builds, including a browser version, while keeping the classic menus,
tutorial, tracks and mechanics.

Guide wood, stone and paper balls through the 13 classic levels above the clouds.

[Play and download](#play-and-download) · [Project history](#project-history) · [Screenshots](#screenshots) · [Development](#development) · [Credits](#credits)

## Project history

1. **Ballance — Cyparade.** The original game introduced the wood, stone and paper balls and their paths above the clouds.
2. **[imengyu/Ballance](https://github.com/imengyu/Ballance) — mengyu / Happy Dreamfish.** The open-source Unity recreation provided the foundation for these projects.
3. **[iwonz/ballance](https://github.com/iwonz/ballance) — iwonz.** A maintained fork of that recreation, adding Russian localization, compatibility updates and builds for Windows, Linux, macOS, Android and the Web, while preserving the classic experience.
4. **[iwonz/reballance](https://github.com/iwonz/reballance) — iwonz.** A personal hobby project built on **iwonz/ballance**: an imaginative take on the familiar game, experimenting with materials, clouds, menus and weather.

Each step builds on the work before it. The Unity projects are community reconstructions and adaptations; Cyparade created the original game.

## Project status

**Current release: [1.0.12](https://github.com/iwonz/ballance/releases/tag/v1.0.12).**
This is the classic Unity reconstruction, not the original commercial Cyparade
executable. Its menus, tutorial, tracks and mechanics preserve the familiar experience.

This repository's **`main` develops Ballance only**. The remastered project is now
independent at **[iwonz/reballance](https://github.com/iwonz/reballance)**, with its own
source, README, releases and Pages. There is no combined release or shared player.
The two games keep separate application identities, settings and saves.

The preserved baseline is `d066bfe5`, available through
[`v1.0.11`](https://github.com/iwonz/ballance/tree/v1.0.11) and the annotated
[`original/v1.0.11`](https://github.com/iwonz/ballance/tree/original/v1.0.11).
Existing release files remain unchanged. The former `original` maintenance tip is
retained as `archive/original-before-repository-split`; classic development continues
on `main`. [Repository boundaries](docs/editions.md).

## Play and download

[Play Ballance in the browser](https://iwonz.github.io/ballance/) or [download the published builds](https://github.com/iwonz/ballance/releases).

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

## Screenshots

Actual Ballance 1.0.11 Web captures. Click an image to view it full size.
[Capture provenance](docs/images/README.md).

**Captured build: Ballance · 1.0.11**

| Main menu | Level selection |
| --- | --- |
| [![Ballance classic main menu](docs/images/ballance-1.0.11-menu.png)](docs/images/ballance-1.0.11-menu.png) | [![Ballance level selection](docs/images/ballance-1.0.11-levels.png)](docs/images/ballance-1.0.11-levels.png) |
| **Level 2 · gameplay** | **Settings** |
| [![Ballance wooden ball at the start of level 2](docs/images/ballance-1.0.11-gameplay.png)](docs/images/ballance-1.0.11-gameplay.png) | [![Ballance classic settings menu](docs/images/ballance-1.0.11-settings.png)](docs/images/ballance-1.0.11-settings.png) |

## Development

Use **Unity 6000.6.7f1**, pinned in `ProjectSettings/ProjectVersion.txt`, with the
platform modules you need. Install Git and Python 3.12+; the [OpenSpec setup](openspec/README.md#tooling)
specifies Node and CLI requirements. The game uses Built-in rendering, uGUI and native
IVP ball physics.

```sh
git clone https://github.com/iwonz/ballance.git
cd ballance
```

Open this directory in Unity and use `Assets/Scenes/Main.unity`. Select `GameEntry`
and set **Debug Type → NoDebug** for normal play. Keep this clone's `Library`, `Temp`,
`Builds` and physics plugins separate from Reballance.

1. Read [AGENTS.md](AGENTS.md), the [source map](docs/architecture.md) and [OpenSpec workflow](openspec/README.md).
2. Develop on `main` or focused feature branches. Transfer applicable fixes between repositories as small tested commits; do not merge the complete games.
3. Run the [relevant checks](docs/validation.md), record actual results and archive the change only when complete.

Build the player **with all game content**:

```sh
bash scripts/build.sh WebGL
python3 scripts/package-build.py WebGL
python3 -m http.server 8000 --directory Builds/WebGL
# Open http://localhost:8000/
```

Other targets: `macOS`, `Windows`, `Linux`, `Android`. See the
[build guide](docs/build-and-release.md) for modules, packaging and local Mac cleanup.
Stable Mac installs use `Ballance.app`; temporary builds use `Ballance-dev.app`.

For debugging, click the About version eight times, enable developer mode and restart.
F12 opens the console; `help` lists commands. Use a test profile for commands that
change progress or scores.

## Releases and Pages

Each new **`v<version>`** release in this repository contains only Ballance: the five
supported platform builds and SHA-256 checksums. The Pages workflow deploys the matching
Web archive at **https://iwonz.github.io/ballance/**, preserving the existing Web save
directory. A manual dispatch with an older tag rolls back Ballance independently.

Follow the [release procedure](docs/build-and-release.md#github-pages). Reballance
acceptance does not block Ballance releases. Repository/documentation changes do not
require relabelling the unchanged 1.0.11 binaries as a new game version.

## Documentation

| Guide | Contents |
| --- | --- |
| [Repository independence](docs/editions.md) | Source ownership, identities and shared fixes |
| [Architecture](docs/architecture.md) | Capability map and implementation boundaries |
| [Build and release](docs/build-and-release.md) | Complete player/content builds, releases and Pages |
| [Validation](docs/validation.md) | Automated checks, gameplay and device evidence |
| [OpenSpec](openspec/README.md) | Proposal through verification and archive |
| [macOS](docs/macos-apple-silicon.md) | ARM64 plugin, signing and first launch |
| [Translations](HelpTranslate.md) | Localized resources and UI checks |
| [Native physics](BallancePhysics/README.md) | C++/C# bridge and reproducible builds |

Maintained documentation is English; gameplay localization and original notices
retain their languages.

## Credits

**Direct upstream: [imengyu/Ballance](https://github.com/imengyu/Ballance)** by
**mengyu / Happy Dreamfish (imengyu)**. This repository builds on that Unity
recreation and its contributors' work.

**Cyparade** created the original Ballance. **iwonz** maintains this fork's Russian
localization, compatibility and multiplatform/Web builds, and uses it as the base
for the separate hobby project **[Reballance](https://github.com/iwonz/reballance)**.

Native physics derives from [nillerusr/source-physics](https://github.com/nillerusr/source-physics).
Thanks also to the bundled tool, UI, compression, font and localization contributors;
[CREDITS.md](CREDITS.md) lists their projects and retained notices.

## License and original game rights

The project is maintained for learning purposes. Its code uses [GPL-3.0](LICENSE); third-party components and Ballance game data have separate rights. This applies to **both Ballance and Reballance**. New branding and generated presentation assets do not relicense inherited content. [License boundaries and component notices](CREDITS.md#license-scope).

The upstream notice is preserved verbatim:

> Note: Ballance belongs to Cyparade's work, and the copyright belongs to the original developer Cyparade. The development of this project is for learning purposes only and cannot be used for any commercial purposes. This project follows the GPL-3.0 protocol, but is limited to the code of this project. Any 3D model data, physical engine, images, audio and video, and game data related to Ballance are not within this scope. Any use of these game data for commercial activities without the permission of the copyright owner is illegal.
