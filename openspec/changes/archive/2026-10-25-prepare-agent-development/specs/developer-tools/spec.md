## Purpose

Support the existing debug console, level/package authoring, and validation tools without promising an unfinished standalone level editor. See the [source map](/docs/architecture.md#developer-tools) for implementation and validation boundaries.

## ADDED Requirements

### Requirement: Debug mode and console
The game SHALL expose its existing debug-mode activation, console, and command registration with argument-count checks. Debug commands SHALL remain distinct from ordinary player UI controls.

#### Scenario: Console use
- **WHEN** debug mode is active and the console key is pressed
- **THEN** the developer can enter registered commands and see command output

#### Scenario: Unlock built-in levels
- **WHEN** the developer runs the existing highscore open-all command
- **THEN** the built-in level pass-state entries are marked available

### Requirement: Editor content authoring
Unity editor tools SHALL package configured modules and levels for the selected build target, with metadata and compatible AssetBundles. The content-aware player build SHALL use these tools rather than relying on the default player build alone.

#### Scenario: Package a level
- **WHEN** a valid level definition is packaged for a target
- **THEN** the output contains the level metadata, prefab, and referenced assets for that target

### Requirement: Extensible level mechanisms
The level authoring/runtime interface SHALL support registered mechanism prefabs, custom loading stages, and lifecycle events for activation, reset, preview, and unload.

#### Scenario: Custom mechanism
- **WHEN** a registered mechanism is referenced by a valid level configuration
- **THEN** the loader instantiates it and connects it to the sector lifecycle

### Requirement: Honest diagnostic coverage
Development documentation SHALL distinguish editor checks, native smoke checks, packaging checks, browser smoke tests, and real-device gameplay. Retained legacy tools SHALL NOT be described as validated current release features without evidence.

#### Scenario: Packaging-only platform verification
- **WHEN** a platform build and archive pass but gameplay was not run on that platform
- **THEN** the verification record explicitly states that runtime gameplay remains untested
