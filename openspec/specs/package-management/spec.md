# package-management Specification

## Purpose

Load game resources and optional modules with metadata, dependency versions, lifecycle hooks, and explicit scripting-backend limits. See the [source map](/docs/architecture.md#package-management) for implementation and validation boundaries.

## Requirements

### Requirement: Package discovery and metadata
The game SHALL expose built-in packages and discovered local packages with their metadata, enabled state, and resources. Package identifiers and resource identifiers SHALL remain stable across compatible changes.

#### Scenario: Built-in modules
- **WHEN** the package manager scans a shipped player
- **THEN** it can list the built-in sound, music, sky, infinite-life, self-kill, and speed-ball packages

### Requirement: Dependency-aware loading and unloading
The loader SHALL load dependencies before their dependent package, reject insufficient dependency versions, and report load failure. It SHALL protect system/non-unloadable packages and defer ordinary unloading while references remain unless immediate unloading was requested.

#### Scenario: Dependency too old
- **WHEN** a package requires a minimum dependency version above the loaded version
- **THEN** that package reports load failure

#### Scenario: Referenced package
- **WHEN** an ordinary unload is requested while another loaded package depends on it
- **THEN** unloading waits until its dependency reference count reaches zero

### Requirement: Built-in code and external mod limits
Built-in mod code SHALL be available in every shipped player without loading its legacy DLL. External C# DLL loading SHALL be limited to compatible Mono desktop players; IL2CPP players SHALL report that dynamic C# loading is unsupported.

#### Scenario: Built-in speed mod on Web
- **WHEN** the speed-ball package is enabled in a Web player
- **THEN** its compiled entry point can provide its speed setting without loading an external DLL

#### Scenario: External DLL on IL2CPP
- **WHEN** an Android or Web package requests external C# code loading
- **THEN** the loader reports that the current backend cannot load that DLL

### Requirement: Package management controls
The package interface SHALL expose package information, enable/disable controls, stored trust state, and settings supplied by loaded packages. These controls SHALL NOT be represented as a security sandbox for arbitrary mod code.

#### Scenario: Package setting
- **WHEN** a loaded built-in mod registers its settings UI
- **THEN** the package manager exposes controls backed by that package's settings namespace
