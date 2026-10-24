# macOS Apple Silicon builds

The Apple Silicon target is native ARM64 for M-series Macs, including M1, M2,
M3 and later models. It does not require Rosetta. The bundled ARM64 physics
library requires **macOS 13.1 or later**. Intel builds remain separate.

## Build locally

Install Unity **2021.3.33f1**, activate it through Unity Hub, and install **Mac
Build Support (Mono)**. The project records the China edition, `2021.3.33f1c1`;
CI explicitly selects the international `2021.3.33f1` editor at the same patch
level. Do not use Unity's default build button for release packaging: it does
not generate the external game packages.

On an Apple Silicon Mac with Xcode Command Line Tools and Python 3:

```sh
bash scripts/build-macos.sh
```

For an editor installed elsewhere, set its executable path explicitly:

```sh
UNITY_EDITOR="/path/to/Unity.app/Contents/MacOS/Unity" bash scripts/build-macos.sh
```

Close any editor already using this project first. Outputs are
`Builds/macOS/Ballance.app`, `Builds/Ballance-macOS-AppleSilicon.zip` and its
`.sha256` checksum. Move a previous output aside before rebuilding; existing
apps and archives are deliberately not overwritten.

The build entry point is `Ballance2.Editor.MacOSBuild.BuildAppleSilicon`.
It selects ARM64 and Mono, explicitly builds `Assets/Scenes/Main.unity`, builds
the core package, every module in `Assets/Packages`, and levels 01–13 for
`StandaloneOSX`. Packages go under `Ballance.app/Contents/{Core,Packages,Levels}`
to match the game's resource loader and Unity's
[macOS data path](https://docs.unity3d.com/2021.3/Documentation/ScriptReference/Application-dataPath.html).
Mono preserves support for dynamically
loaded C# mods. The original architecture and scripting backend settings are
restored when the build finishes.

The native physics binary is already checked in at
`Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib`. Its importer enables
ARM64 in the macOS editor and player. The `macOS/x64` copy is for Intel only.
No M3-specific compiler target or rebuild of the external IVP source is needed.

## GitHub Actions and release assets

The **macOS Apple Silicon** workflow has three entry points:

* Pull requests run the native physics smoke test on an Apple Silicon runner
  without requiring secrets. They do **not** claim to compile or test the Unity
  player. This also works for fork PRs.
* **Actions → macOS Apple Silicon → Run workflow** builds the selected branch
  and uploads the `macOS-AppleSilicon` artifact for review.
* Publishing a GitHub Release builds its **tag**, then attaches
  `Ballance-<tag-without-v>-macOS-AppleSilicon.zip` and its SHA-256 checksum.
  Existing assets for other platforms are preserved. Existing files with the
  same names are not replaced.

Before running a full build, a repository maintainer must configure the
[GameCI activation secrets](https://game.ci/docs/github/activation/):

* `UNITY_EMAIL` and `UNITY_PASSWORD`;
* `UNITY_LICENSE` for Personal, or `UNITY_SERIAL` for Pro.

The Unity Mono player is cross-built on Ubuntu with the macOS module. Validation
and signing run on `macos-15` (ARM64). The Unity step uses GameCI's Linux
activation path for Personal or Pro licenses. The workflow must exist in the
release tag. A tag name may contain letters, digits, dots, underscores and hyphens.

## Release validation and signing

`scripts/package-macos.py` checks the player is ARM64, every bundled Mach-O
library has an ARM64 slice, no native dependency points to a build-machine path,
and the physics plugin, Unity framework, Mono runtime, core packages and all
13 levels are present. The native smoke test loads the actual packaged physics
library, verifies ABI version 2302, exercises vector allocation/readback,
simulates a physics world for 120 frames, and checks initialization/shutdown.
It runs directly on Apple Silicon.

After setting the minimum macOS version, the script signs nested code and the
app with an **ad-hoc signature**, verifies that signature, creates a ZIP with
`ditto`, and extracts it again to recheck signatures, architecture and executable
permissions. Ad-hoc signing does not provide Apple notarization. A downloaded
build may need approval in **System Settings → Privacy & Security → Open
Anyway**. Distribution without this prompt requires the maintainer's Developer
ID and notarization credentials; see
[Unity's signing guide](https://docs.unity3d.com/2021.3/Documentation/Manual/macos-building-notarization.html).

Before publishing a release, download the workflow artifact onto an M-series
Mac and check the menu, sound, loading level 01, ball movement/collisions, and
quitting/reopening. The automated native and packaging checks do not replace
this gameplay check.
