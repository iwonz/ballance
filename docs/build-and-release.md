# Build and release

Use **Unity 6000.6.7f1**, as pinned in ProjectSettings. Do not mix Unity 2021
players or AssetBundles with current outputs. Every release rebuilds the player,
core, six built-in packages, and levels 01-13 for its target.

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

Every release must include its matching Web archive and checksum and deploy that
version to GitHub Pages, including releases whose main changes are documentation
or tooling. Release delivery is complete only after the Pages workflow and public
site checks succeed. Do not reuse or rename an older player as a new version.

Web uses gzip with **Decompression Fallback**, single-threaded Wasm physics, and
relative URLs. Custom Content-Encoding or COOP/COEP headers are unnecessary.
Web releases explicitly use runtime-speed LTO and WebAssembly 2023, retaining full
managed exception handling. This enables native Wasm exceptions and SIMD without
reducing graphics quality or changing simulation frequency. Use current browsers;
the generated Unity 6.6 compiler targets Chrome 95+, Firefox 100+, or Safari 16.4+
(these minimum versions are not a tested browser matrix). See Unity's [Wasm setting](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/PlayerSettings.WebGL-wasm2023.html)
and [performance guidance](https://docs.unity3d.com/6000.6/Documentation/Manual/webgl-performance.html).
Run the [hardware frame checks](validation.md#web-frame-performance) when changing
rendering, physics hot paths, Web compilation, or gameplay performance.
The player works below a repository prefix such as `/ballance/`. Generated Web
files are release assets, not source-controlled content.

1. Set **Settings -> Pages -> Source -> GitHub Actions**.
2. In the `github-pages` environment, allow deployment from `main` and tags `v*`.
   A release event runs from its tag. These rules were configured for this fork
   during release 1.0.9; verify them if repository/environment settings change.
3. Complete the source change and its OpenSpec record. Build and check Web from the
   intended release commit using the [validation guide](validation.md).
4. Create a draft release on that commit's tag. Attach
   `Ballance-<version>-WebGL.tar.gz` and the matching `.sha256` file, plus the other
   verified platform assets and checksums. Use a tag such as `v1.0.10`, matching
   Player Settings -> Version and every archive filename.
5. Publish the release. **Publish Web to GitHub Pages** validates checksum, safe
   extraction, required content, size, and the embedded Web player version, then
   deploys the site. Missing Web assets fail this workflow.
6. Wait for the workflow's public `release.json` tag check to pass, then test the
   public URL, including menu, level loading, and controls. Include the release and
   successful Pages run links in the delivery report. If rollout fails, repair and
   rerun it before declaring the release delivered.

The current fork's URL is [iwonz.github.io/ballance](https://iwonz.github.io/ballance/)
unless a custom domain is configured. The site's `release.json` identifies its tag,
player version, archive, and verified SHA-256. To redeploy or explicitly roll back,
manually dispatch the workflow with an
existing release tag; it deploys that release's checked archive. The workflow needs
no Unity license or Unity secrets.

Test over HTTP, not by opening index.html with file://:

```sh
python3 -m http.server 8000 --directory Builds
# Open http://localhost:8000/WebGL/
```

For a deployment-equivalent check, download/extract the release through
`python3 scripts/prepare-pages.py <download-dir> <empty-site-dir> <release-tag>`
and serve the result under a repository subpath. The download directory must contain
exactly one Web archive and its checksum. The extraction limit is 1 GB uncompressed. Built-in packages are under
`StreamingAssets/BuiltInPackages`, with same-origin URLs retaining the site prefix.
Built-in mods are compiled into the player. New external C# DLL mods require Mono;
Web/Android IL2CPP require adding their source and rebuilding.

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
