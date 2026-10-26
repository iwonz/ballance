# macOS Apple Silicon builds

The Apple Silicon target is native ARM64 for M-series Macs, including M1, M2,
M3 and later models. It does not require Rosetta. The bundled ARM64 physics
library requires **macOS 13.1 or later**. Intel builds remain separate.

## Build locally

Install Unity **6000.6.7f1** through Unity Hub and activate it locally.
See [all platform builds and GitHub Pages](build-and-release.md) for the shared
build workflow. Do not use Unity's default Build button for releases: the game
also needs its external packages.

On an Apple Silicon Mac with Xcode Command Line Tools and Python 3:

```sh
bash scripts/build-macos.sh
```

For an editor installed elsewhere, set its executable path explicitly:

```sh
UNITY_EDITOR="/path/to/Unity.app/Contents/MacOS/Unity" bash scripts/build-macos.sh
```

Close any editor already using this project first. The wrapper delegates to the
shared content-aware build and packaging commands. Outputs are
`Builds/macOS/Ballance.app` (`Ballance-dev.app` for development versions),
`Builds/releases/Ballance-<version>-macOS.zip` and its checksum. Existing outputs
are not overwritten. Follow [local installation and cleanup](build-and-release.md#local-macos-installation)
after packaging; obsolete unpacked apps, including legacy `.app-backup` and `.app.disabled`
copies, are deleted after verified replacement.

The build entry point is `Ballance2.Editor.PlayerBuild.Build`.
It selects ARM64 and Mono, explicitly builds `Assets/Scenes/Main.unity`, builds
the core package, every module in `Assets/Packages`, and levels 01–13 for
`StandaloneOSX`. Packages go under `Ballance.app/Contents/{Core,Packages,Levels}`
to match the game's resource loader and Unity's
[macOS data path](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Application-dataPath.html).
Mono preserves support for dynamically
loaded C# mods. The ARM64/Mono settings remain selected after the build.

Built-in mods compile directly with the player. Their checked-in legacy DLLs
are excluded from player imports and release packages. Desktop Mono builds
still support loading third-party mod DLLs.

The native physics binary is already checked in at
`Assets/BallancePhysics/Plugins/macOS/libbphysics.dylib`. Its importer enables
ARM64 in the macOS editor and player. The `macOS/x64` copy is for Intel only.
No M3-specific compiler target or rebuild of the external IVP source is needed.
The build places this dylib directly in `Contents/PlugIns`: leaving it in the generated `ARM64` subfolder causes `DllNotFoundException` in the Mono
player even though a direct native-library smoke test succeeds.

## Russian interface

Choose **Settings → Language → Russian** and confirm the game
restart. The preference is saved immediately and used on subsequent launches.
A fresh installation also follows a Russian system language automatically.
Built-in menus, settings, tutorials, level labels, mod descriptions and dialogs
are translated. Player-entered names, third-party content, diagnostic stack traces
and original third-party license texts are not rewritten.

Roboto is bundled for Cyrillic; the original Bank Gothic menu font explicitly
references it as a fallback. This does not depend on fonts installed on the Mac.

Translation coverage and format placeholders are checked in CI without Unity:

```sh
python3 scripts/check-localization.py
```

With Unity installed, run `Ballance2.Editor.LocalizationChecks.Run` through
`-executeMethod` to validate runtime resource loading, fallbacks, Cyrillic and
prefab bindings. `Ballance2.Editor.LocalizationChecks.Render` additionally renders
Russian UI previews into `Builds/checks/localization` and checks tutorial text fit;
it needs graphics, so omit `-nographics`. Neither check changes player preferences.

## GitHub Actions and release assets

Use the **Unity 6 platform builds** workflow with a licensed self-hosted Apple
Silicon runner, or build locally and attach the ZIP and checksum to a draft
release. See [the release guide](build-and-release.md). The **Build checks**
workflow checks localization, release tooling and ARM64 physics on hosted
runners; it does not require Unity activation secrets.

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
[Unity's signing guide](https://docs.unity3d.com/6000.6/Documentation/Manual/macos-building-notarization.html).

Before publishing a release, download the workflow artifact onto an M-series
Mac and check the menu, sound, loading level 01, ball movement/collisions, and
quitting/reopening. The automated native and packaging checks do not replace
this gameplay check.
