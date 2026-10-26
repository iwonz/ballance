## ADDED Requirements

### Requirement: Edition-specific build identity
Release tooling SHALL distinguish original and remaster, validate tag/edition/version
agreement and name new assets Ballance-<version>-<platform> or Reballance-<version>-<platform>. It SHALL preserve
support for historical unqualified release assets. Each edition SHALL build content
and native code from its own checkout and preserve no-overwrite output checks.

#### Scenario: Mismatched tag
- **WHEN** a Remaster archive is supplied for an Original release
- **THEN** preparation rejects the mismatch before publication

#### Scenario: Historical release
- **WHEN** v1.0.11 is selected as Original
- **THEN** its existing Ballance-1.0.11 assets remain usable without renaming or rebuilding

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
