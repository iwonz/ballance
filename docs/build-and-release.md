# Build and release

Use **Unity 6000.6.7f1**, as pinned in ProjectSettings. Do not mix Unity 2021
players or AssetBundles with current outputs. A new game version rebuilds the player,
core, six built-in packages, and levels 01-13 for its target. A unified distribution
may carry the other game's unchanged verified archives forward without rebuilding it.

## Local builds

Install the required Unity Hub modules: Android with SDK/NDK/OpenJDK, Web,
Linux Mono, and Windows Mono as needed. macOS uses the Apple Silicon editor.
Packaging needs Python 3.12+; macOS also needs Xcode Command Line Tools.
Close any editor instance using this project before batch builds.

```sh
bash scripts/build.sh WebGL
python3 scripts/package-build.py WebGL
```

| Target argument | Output under `Builds/` | Release asset |
| --- | --- | --- |
| `WebGL` | `WebGL/index.html`, Build, StreamingAssets | `Ballance-<version>-WebGL.tar.gz` |
| `macOS` | `macOS/Ballance.app`, ARM64, Mono | `Ballance-<version>-macOS.zip` |
| `Windows` | `Windows/Ballance.exe`, x86-64, Mono | `Ballance-<version>-Windows.zip` |
| `Linux` | `Linux/Ballance.x86_64`, x86-64, Mono | `Ballance-<version>-Linux.tar.gz` |
| `Android` | `Android/Ballance.apk`, ARM64, IL2CPP | `Ballance-<version>-Android.apk` |

The Original descriptor keeps `Ballance` / `com.MengyuGame.Ballance`. New builds
include build-identity.json; packaging verifies it before producing
`Ballance-<version>-<platform>` archives for `original/v<version>` tags.
Legacy published `v1.0.11` assets retain their original names.

Use the same target in both commands. Release assets and SHA-256 sidecars go to
`Builds/releases/`; filenames use Player Settings -> Version. Move previous outputs
aside before rebuilding: these tools deliberately refuse to overwrite them.
Unity logs go to `Logs/build-<platform>.log`.

`UNITY_EDITOR` can override the standard Hub executable path:

```sh
UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity bash scripts/build.sh Linux
```

In the editor, select the platform in Build Profiles and use **Ballance -> Build ->
Build current platform with game content**. Unity's default Build button does not
package the external game content. The batch entry point is
`Ballance2.Editor.PlayerBuild.Build` with the corresponding `-buildTarget`.
Target configuration persists in the project; review settings changes before committing.
Android temporarily selects Input System only because Both is unsupported there;
`AndroidInput` preserves existing bindings and touch controls, and the build restores
the previous editor input setting even on failure.

## Graphics device selection

Rendering already uses Unity's graphics backend; gameplay and IVP physics still
run on the CPU. The content-aware builder explicitly restores Unity's
[automatic graphics API selection](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PlayerSettings.SetUseDefaultGraphicsAPIs.html)
for each target. The engine chooses among compatible APIs; this is not a benchmark
that proves one API is fastest on every driver. The current Web backend remains
WebGL 2. Enabling WebGPU requires separate compatibility/performance validation,
including Unity's [readback limitations](https://docs.unity3d.com/6000.6/Documentation/Manual/WebGPU-limitations.html).

Web builds also request [HighPerformance](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PlayerSettings.WebGL-powerPreference.html)
when creating the graphics context. This was already the project's serialized
default; the builder now enforces it after local overrides. Browsers may ignore
the hint, and a web page cannot enable a browser's disabled hardware acceleration
or override OS GPU policy. On a machine with multiple GPUs, use the OS graphics
preferences for the game or browser, enable browser graphics acceleration, and
restart the affected application. A performance GPU can increase power use.

The game's **Settings -> Graphics** page shows the reported device/API and warns
about recognized software renderers. It does not offer a CPU/GPU switch or change
saved quality settings. Compare the actual renderer and frame evidence using the
[hardware checks](validation.md#web-frame-performance), rather than treating the
high-performance request or an unfamiliar device name as proof of acceleration.

## Signing and deferred platforms

The Android APK is for installation and testing. For Google Play, configure your
own application identifier, version code, and keystore. To generate AAB, invoke
`Ballance2.Editor.PlayerBuild.Build` with `-buildTarget Android` and
`-customBuildPath Builds/Android/Ballance.aab`; the extension selects App Bundle.
Do not describe test signing as store-ready signing.

macOS packaging uses an ad-hoc signature, without notarization. Distribution signed
with Developer ID requires the maintainer's credentials and a separate notarization
step. See [macOS notes](macos-apple-silicon.md) for first launch and validation.

**iOS is deferred.** Existing `iOS` build/package arguments and the manual workflow
choice are retained for a future Xcode export, not a current release commitment.
They would produce `Builds/iOS/Unity-iPhone.xcodeproj` and an Xcode-project archive,
not an installable IPA. Provisioning, signing, Archive, and TestFlight/App Store
submission would require Xcode and the owner's Apple Developer account. No script
submits builds to a mobile store.

## GitHub Pages

The shared publication process is owned by `main`. This branch produces classic
Ballance builds and immutable `original/v<version>` source tags; it does not publish
separate new GitHub Releases. All new public distributions use `release/v<version>`
on main and include **both Ballance and Reballance**, all ten platform artifacts and
checksums, source pins, Credits, license and the complete two-player Pages archive.

Build and verify any changed Ballance version here, then hand its five platform
artifacts and SHA-256 sidecars to the [main release procedure](https://github.com/iwonz/ballance/blob/main/docs/build-and-release.md#github-pages).
An unchanged edition is carried forward byte-for-byte from its verified source tag;
never rename it to pretend it is a new game version. Keep legacy releases intact.

Main's manifest pins the distribution and both source versions. Its workflow verifies
all assets and deploys the exact site attached to that one release. It checks both
public identities, and delivery also requires actual browser gameplay checks. To roll
back only Ballance, prepare a new complete distribution with its older verified pin
and leave Reballance's pin unchanged. Never select either game through “latest”.

The shared landing launches Ballance at `/ballance/original.html`, with resources
under `original/`, preserving its original page-directory save identity. Reballance
uses `/ballance/remaster/`. Both pages link to the other game and the landing. Test
existing browser storage before deployment; independent saves must remain separate.

## GitHub Actions

**Unity 6 platform builds** is manually dispatched for one platform and uploads
release files and logs. It requires a licensed **self-hosted Apple Silicon runner**
with labels `macOS`, `ARM64`, `unity-6000.6.7f1`, the needed Hub modules, and an
Actions runner supporting Node 24 (2.327.1 or newer). Setup does not install that
runner. Never run untrusted pull requests on it.

Local builds can supply release assets without a self-hosted runner. **Build checks**
runs repository/OpenSpec checks, localization, release-tool tests, and ARM64 native
physics checks on hosted runners. It does not build Unity or test all devices.

## Rebuilding native plugins

```sh
python3 scripts/build-physics.py WebGL
python3 scripts/build-physics.py Android
```

The script downloads a pinned IVP revision, applies
`BallancePhysics/source-physics.patch`, and uses the Unity toolchains. Web runs its
Wasm smoke test before replacing the library. Android uses the matching NDK and
checks 16 KB LOAD-segment alignment with llvm-readelf. Override the SDK location
with `--sdk` if needed; Android rebuilding requires a macOS/Linux host.
Web compilation and its smoke link use native Wasm longjmp and SIMD, matching the
Wasm 2023 player. Do not link an older emulated-longjmp IVP archive into that player.
The patch supplies the constraint-deletion callback required by the wrapper and a
header include guard. Public IVP diagnostics go to stdout.

Follow [native instructions](../BallancePhysics/AGENTS.md), run ABI and target checks,
and record provenance in the change. See [validation.md](validation.md) for check
commands and the limits of automated verification.

References: [Unity release](https://unity.com/releases/editor/whats-new/6000.6.7f1),
[Unity Web deployment](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-deploying.html),
[GitHub Pages workflows](https://docs.github.com/en/pages/getting-started-with-github-pages/using-custom-workflows-with-github-pages),
[Pages limits](https://docs.github.com/en/pages/getting-started-with-github-pages/github-pages-limits).

## Local macOS installation

Use `/Applications/Ballance.app` and `/Applications/Reballance.app` for stable
installations. Intermediate development versions use `Ballance-dev.app` and
`Reballance-dev.app`; bundle IDs and saved-data paths remain stable. Package a
completed player before installing it.

```sh
python3 scripts/install-macos.py remaster Builds/macOS/Reballance-dev.app
python3 scripts/install-macos.py remaster Builds/macOS/Reballance-dev.app --apply
```

Use `original` and its corresponding app for the classic edition. A dry run lists
exact paths. Applying verifies identity, version, signature and copied-file hashes,
rejects downgrades and active copies, and replaces the edition's canonical app.
It then deletes obsolete matching installed/build bundles, including former
`.app.disabled` copies, and clears their stale LaunchServices registrations. Newer
work in progress is retained. Saved profiles, release archives and unrelated apps
are outside cleanup. Stable installation removes the superseded `-dev` app.

The local generated tree uses `Builds.noindex` with a `Builds` symlink; this avoids
Spotlight indexing but does not by itself stop LaunchServices discovery. Temporary
staging is hidden. After validating the canonical copy, the installer also removes
its unpacked source bundle; keep the verified release archive until publication
checks finish. It never resets the global application database.

Run `python3 scripts/test-install-macos.py` after changes. Fixtures cover replacement,
corruption/signature failure, edition isolation, downgrade refusal, retired-copy
cleanup, development filenames, newer-build preservation and catalog parsing.
