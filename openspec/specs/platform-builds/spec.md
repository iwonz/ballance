# platform-builds Specification

## Purpose

Produce versioned players and complete game content for the supported release targets with reproducible packaging and explicit signing limits. See the [source map](/docs/architecture.md#platform-builds) for implementation and validation boundaries.

## Requirements

### Requirement: Exact editor and targets
Player builds SHALL require Unity 6000.6.7f1 and the selected target's installed module. The current release set SHALL include Windows/Linux x86-64, macOS Apple Silicon, Android ARM64, and Web. iOS export code SHALL remain deferred from publication.

#### Scenario: Wrong editor
- **WHEN** a build starts with another Unity editor version
- **THEN** the build fails with the required version

### Requirement: Complete target content
Every release player SHALL include the main scene, rebuilt core, six built-in packages, and levels 01-13 for that target. Desktop SHALL use Mono; Android and Web SHALL use IL2CPP with the required compiled mod code and preserved runtime types.

#### Scenario: Missing level
- **WHEN** expected level content is absent during build or packaging validation
- **THEN** the release preparation fails

### Requirement: Safe build outputs
Build and packaging commands SHALL refuse to overwrite existing player/archive outputs. Temporary content staging SHALL be cleaned after the build, and Android's temporary input backend selection SHALL be restored even on failure.

#### Scenario: Existing player
- **WHEN** the requested output already exists
- **THEN** the command asks the operator to move it aside instead of replacing it

### Requirement: Release assets and signing
Packaging SHALL produce platform-specific archives or APK files and SHA-256 sidecars. macOS packaging SHALL validate native architecture, dependencies, signatures, and archive round-trip integrity. Documentation SHALL identify Android test signing and macOS ad-hoc signing, without claiming store submission or notarization.

#### Scenario: macOS archive
- **WHEN** macOS packaging succeeds
- **THEN** the ARM64 app and nested code are ad-hoc signed and the extracted archive is revalidated

### Requirement: CI responsibilities
Hosted checks SHALL run non-Unity validations and the macOS native smoke test. Unity platform builds SHALL require an explicitly configured, licensed self-hosted runner and SHALL NOT execute automatically on untrusted pull requests.

#### Scenario: Manual platform build
- **WHEN** the platform build workflow is dispatched on a correctly configured runner
- **THEN** it builds and packages the selected target and uploads the result and build log

### Requirement: Optimized Web release execution
Web release builds SHALL use an explicit runtime-performance compilation profile and retain managed exception handling and cleanup. The documented browser baseline SHALL match the enabled WebAssembly features. Rendering cadence SHALL remain eligible for the display's native refresh rate without an artificial 60 FPS cap.

#### Scenario: Web release startup and failure handling
- **WHEN** an optimized Web release runs on a supported browser
- **THEN** normal content and physics load, and failed required-content loading still reaches the game's error path rather than aborting through disabled exception handling

### Requirement: Performance-oriented graphics selection
Content-aware release builds SHALL explicitly use the engine's automatic graphics API selection for the supported release targets and request the high-performance GPU preference for Web. This preference SHALL NOT be described as a guarantee that the browser or operating system selects a particular GPU.

#### Scenario: Rebuild after local graphics overrides
- **WHEN** a supported release target is built after local graphics settings were changed
- **THEN** the build restores automatic graphics API selection and, for Web, the high-performance preference

### Requirement: Independent edition identity
New Original builds SHALL use the original descriptor, historical Ballance product/application identity, original/v<version> tags and product-based archive names. Packaging SHALL verify the embedded edition, version, product and application identifier. Existing legacy releases SHALL remain valid and unchanged.

#### Scenario: Incorrect build relabeling
- **WHEN** a build identity does not match the checkout edition or selected version
- **THEN** packaging rejects it instead of renaming an older or different-edition player

#### Scenario: Classic maintenance
- **WHEN** the original branch receives build or publication tooling
- **THEN** its gameplay assets, native ABI, application identity and save data remain independent from Remaster

### Requirement: Product release names
New classic assets SHALL use Ballance-<version>-<platform> and remaster assets SHALL use Reballance-<version>-<platform>. Technical original/remaster edition IDs and tag prefixes SHALL remain unchanged. Published legacy files SHALL remain immutable.

#### Scenario: Package either edition
- **WHEN** a verified current player is packaged
- **THEN** its filename uses its public product name and validation checks the matching edition identity and version

### Requirement: Stable local macOS installations
Local macOS installation SHALL maintain one current application per edition. Stable applications SHALL use `/Applications/Ballance.app` and `/Applications/Reballance.app`; a development version SHALL replace its edition's installed application using a `-dev` filename suffix. Installation SHALL validate identity and copied contents before replacing the installed version, and remove obsolete matching installed application bundles from application folders. After validating a replacement, obsolete matching unpacked builds and retired `.app.disabled` and legacy `.app-backup` copies in both build and application folders SHALL be deleted. Successful stable delivery SHALL remove temporary development applications. Stale matching catalog entries SHALL be removed without resetting unrelated application registrations. Cleanup SHALL preserve saved games, source worktrees and release archives. A failed copy or validation SHALL leave the installed version usable.

#### Scenario: Replace a development player
- **WHEN** a newer verified Remaster application is installed locally
- **THEN** the current Remaster application is replaced, obsolete installed Remaster bundles and build copies are deleted, and the canonical Original and both editions' saves remain intact

#### Scenario: Invalid replacement
- **WHEN** the source identity is wrong or a copied application fails validation
- **THEN** installation stops before replacing the current application or cleaning its alternatives

#### Scenario: Stable application names
- **WHEN** either edition is updated or the old Ballance Original installation is migrated
- **THEN** the application menu exposes one current version per edition, named Ballance and Reballance for stable versions or with a -dev suffix during development, without duplicate copies or temporary staging entries

#### Scenario: Rediscovered diagnostic builds
- **WHEN** macOS registers an old unpacked build again despite its noindex directory
- **THEN** installation removes the obsolete matching bundle and its old catalog entry after validating the current installation

#### Scenario: Legacy backup suffix
- **WHEN** an obsolete matching `.app-backup` bundle remains after a successful installation
- **THEN** cleanup deletes that bundle and preserves unrelated backups and release archives
