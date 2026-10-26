# Build and release

Use **Unity 6000.6.7f1**, as pinned in ProjectSettings. Do not mix Unity 2021
players or AssetBundles with current outputs. Select the correct independent
[edition worktree](editions.md) before building. A new game version rebuilds the player,
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
| `WebGL` | `WebGL/index.html`, Build, StreamingAssets | `<Product>-<version>-WebGL.tar.gz` |
| `macOS` | `macOS/<Product>.app`, ARM64, Mono | `<Product>-<version>-macOS.zip` |
| `Windows` | `Windows/<Product>.exe`, x86-64, Mono | `<Product>-<version>-Windows.zip` |
| `Linux` | `Linux/<Product>.x86_64`, x86-64, Mono | `<Product>-<version>-Linux.tar.gz` |
| `Android` | `Android/<Product>.apk`, ARM64, IL2CPP | `<Product>-<version>-Android.apk` |

Use the same target in both commands. Release assets and SHA-256 sidecars go to
`Builds/releases/`; filenames use the edition descriptor and Player Settings ->
Version. Original checkouts without an edition descriptor retain legacy naming;
the Original maintenance branch carries the shared edition-aware tooling and an
`original` descriptor for namespaced tags. Move previous outputs
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

Every new public release is a **unified distribution** tagged `release/v<version>`
on `main`. It contains both Ballance and Reballance for all five platforms, matching
checksums and the complete two-player Pages archive. Independent edition source tags
(`original/v*`, `remaster/v*`) are provenance, not separate public releases. Keep all
legacy releases unchanged. Documentation/tooling commits alone need no binary release.
Delivery is complete only after the attached site is deployed and both public games
are checked. Never reuse or rename an older player as a new game version.

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

The root becomes a static RU/EN landing. Original launches at `original.html`,
with assets under `original/`; Remaster launches at `remaster/`. Keeping Original's
host page in the old directory preserves Unity's directory-based Web storage key.
Test an existing browser profile before the first relocation. The landing does not
load Unity.

1. Set **Settings → Pages → Source → GitHub Actions** and allow `main` and
   `release/v*` in the `github-pages` environment. Publication is coordinated on main.
2. Finish the required acceptance and source verification in each affected game's
   worktree. Create and push its immutable source tag. For an unchanged edition,
   reuse its verified tag and files. Reballance must not be published before its
   full gameplay acceptance; do not bypass this with a prerelease.
3. Build and package each changed edition for WebGL, Windows, Linux, macOS and
   Android. Place both sets of artifacts and SHA-256 sidecars in a fresh local
   `Builds/distribution-input/original/` and `Builds/distribution-input/remaster/`.
   Retrieve unchanged files from their existing release without renaming them.
4. Update `site/releases.json` on main: schema 2, the new `release/v<version>`, and
   both edition source tags with the five platform hashes in `assets`. Modify only
   the edition pin that actually changes. A null distribution/Reballance pin is an
   intentional publication barrier. The first accepted pair requires real builds
   of both games, not fixtures or development placeholders.
5. Commit the manifest and source, run the applicable checks, then create and push
   the annotated distribution tag on that main commit. Prepare the complete upload
   directory and verify its attached site using the commands below. A failure leaves
   no partial output; existing output directories are never overwritten.
6. Create **one draft GitHub Release** for the distribution tag, attach every prepared
   file and complete its release notes with actual validation/limitations. Only then
   publish the draft. Do not publish edition source tags as separate new releases.
7. Pages selects the approved distribution from main, checks out that immutable
   release tag, verifies that its manifest agrees, and downloads all assets from
   **that one release**. It checks the ten pinned hashes, validates both Web players,
   reconstructs the site and compares every file against the attached site archive.
   It deploys the attached site only if everything agrees and the combined site fits
   within 1,000,000,000 bytes. Deployments are serialized.
8. Wait for the workflow's public distribution and both edition identity checks.
   Exercise both public games: load, control, recovery, return to menu and switching.
   Record the release and successful Pages workflow links. Upload/build success is
   not full release delivery or gameplay acceptance.

Run these commands from the tagged main checkout, using a new output path:

```sh
python3 scripts/prepare-release.py prepare Builds/distribution-input Builds/distribution
python3 scripts/prepare-release.py verify Builds/distribution Builds/distribution-preview
python3 -m http.server 8000 --directory Builds/distribution-preview
# In another terminal, while the server is running:
python3 scripts/verify-site.py http://localhost:8000/ site/releases.json
```

The upload directory contains both games' archives and sidecars,
`Ballance-Reballance-<distribution-version>-site.tar.gz` and its sidecar,
`releases.json`, `RELEASE-NOTES.md`, `LICENSE` and `CREDITS.md`. Release notes link both
edition source tags; the automatically generated GitHub source archive alone is
only the distribution's main checkout and is not a substitute for Ballance's source.
Use the authenticated GitHub CLI after acceptance, checks and tag pushes:

```sh
release_tag=$(python3 -c 'import json; print(json.load(open("site/releases.json"))["release"])')
gh release create "$release_tag" --verify-tag --draft --title "Ballance + Reballance — $release_tag" --notes-file Builds/distribution/RELEASE-NOTES.md
gh release upload "$release_tag" Builds/distribution/*
# Inspect the uploaded assets and finish the validation notes before publishing.
gh release edit "$release_tag" --draft=false
gh run list --workflow github-pages.yml --limit 5
```

These commands publish through the operator's authenticated CLI, which triggers the
release event. If publication is later automated with `GITHUB_TOKEN`, explicitly
invoke the Pages workflow: GitHub suppresses most events created by that token
([GitHub event rules](https://docs.github.com/en/actions/reference/workflows-and-actions/events-that-trigger-workflows)).

The public URL is [iwonz.github.io/ballance](https://iwonz.github.io/ballance/).
Root `release.json` identifies the distribution and pair; each edition also has its
own `release.json`. The site includes license/Credits links and per-player navigation.
The original player stays in the same page directory to preserve Web storage.

To roll back one edition, create a new complete distribution with its previously
verified source tag/hashes and the other edition unchanged. To redeploy the already
approved pair, manually dispatch Pages on main: it uses that immutable distribution's
source and attached site even when main's landing later changes. Never overwrite an
existing release or select another edition dynamically. No Unity license is needed
for this deployment; Unity and platform checks happen before preparing the release.

Test over HTTP, not by opening index.html with file://:

```sh
python3 -m http.server 8000 --directory Builds
# Open http://localhost:8000/WebGL/
```

For a deployment-equivalent check with a ready manifest:

```sh
python3 scripts/prepare-release.py verify Builds/site-download Builds/site-preview --fetch
python3 -m http.server 8000 --directory Builds/site-preview
python3 scripts/verify-site.py http://localhost:8000/ site/releases.json
```

Use new download/output paths that do not exist. `prepare-pages.py` remains the individual
archive validator used by `prepare-site.py`; it checks checksum, safe extraction,
required content and embedded edition/version. The combined size limit is additional
to each archive's limit. Built-in packages use same-origin URLs below each edition's
`StreamingAssets/BuiltInPackages` directory.
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

Remaster uses ABI 2304; the Original baseline retains ABI 2302 in its own worktree.
Never copy one edition's plugin set into the other. Use the same source/patch for
all five Remaster targets. `-ffp-contract=off` preserves the Original arithmetic
order; enabling fused multiply-add changed the paper-ball replay even with the
old bridge and is not an approved optimization:

```sh
python3 scripts/build-physics.py macOS
python3 scripts/build-physics.py WebGL
python3 scripts/build-physics.py Android
docker build --platform linux/amd64 -f scripts/native/Dockerfile -t ballance-native:2303 scripts/native
docker run --rm --platform linux/amd64 --network none -v "$PWD:/repo" ballance-native:2303 python3 scripts/build-physics.py Linux
docker run --rm --platform linux/amd64 --network none -v "$PWD:/repo" ballance-native:2303 python3 scripts/build-physics.py Windows
```

The script fetches pinned IVP revision `47533475e01cbff05fbc3bbe8b4edc485f292cea`
into `Library/NativePhysics/source` and applies `BallancePhysics/source-physics.patch`.
Build Web or macOS first to populate this source before running an offline container.
The Docker base digest and compiler families are pinned in `scripts/native/Dockerfile`;
exact compiler versions and bridge/header/patch/library hashes are recorded under
`BallancePhysics/provenance`. Docker on Apple Silicon executes x86-64 through the
host's emulation; Windows smoke uses Wine. These are native ABI checks, not physical
Windows/Linux player certification. MinGW permits legacy upstream diagnostic
pointer truncations; that compatibility flag is excluded from the project bridge.

macOS uses Apple clang ARM64. Web uses Unity's Emscripten and runs the Wasm smoke
under its Node runtime before installing the archive. Native Wasm longjmp and SIMD
match the Wasm 2023 player. Android uses Unity's NDK, ARM64/API 26 and 16 KB LOAD
alignment checks; it still requires device execution. Override Unity SDK paths with
`--sdk`. Native Linux x86-64 and Windows compiler hosts can also run the corresponding
platform argument directly with `CXX` configured.

The source patch supplies constraint cleanup, contact-material refresh and pointer/
header portability fixes. Follow [native instructions](../BallancePhysics/AGENTS.md)
and [validation guidance](validation.md). Preserve the measured Original dry replay
tolerance; a successful rebuild does not establish identical gameplay.

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
