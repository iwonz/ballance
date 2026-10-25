# level-loading Specification

## Purpose

Load built-in or compatible custom levels, validate their content, support preview, and release level resources on exit. See the [source map](/docs/architecture.md#level-loading) for implementation and validation boundaries.

## Requirements

### Requirement: Native level content
The native level loader SHALL load a target-compatible bundle containing Level.json and Level.prefab, read its metadata, and build configured floors, mechanisms, sectors, spawn points, lighting, and music.

#### Scenario: Built-in level
- **WHEN** a player selects an available built-in level
- **THEN** the corresponding level bundle is loaded and its configured starting ball, lives, points, and sector are used

#### Scenario: Invalid content
- **WHEN** the bundle is missing, unreadable, or lacks Level.json or Level.prefab
- **THEN** loading reports an error instead of entering a completed playable state

### Requirement: Dependencies and configuration validation
Level loading SHALL check required package minimum versions and mandatory level configuration before starting gameplay.

#### Scenario: Missing dependency
- **WHEN** a level requires an unavailable or outdated package
- **THEN** the loader reports the dependency problem

### Requirement: Level preview
The game SHALL provide preview for a level that permits it, with a free camera and preview mechanism lifecycle, rather than starting the scored gameplay session.

#### Scenario: Preview allowed
- **WHEN** a player previews a compatible level with allowPreview enabled
- **THEN** the preview camera and preview interface are shown

#### Scenario: Preview prohibited
- **WHEN** preview is requested for a level that disallows it
- **THEN** loading reports that preview is not allowed

### Requirement: Level cleanup
Exiting or replacing a level SHALL unload its bundle and level-owned objects and clear its mechanism/resource collections before returning to the menu or starting another level.

#### Scenario: Return to menu
- **WHEN** the player confirms leaving a loaded level
- **THEN** level unload events run and the menu becomes available again

### Requirement: Legacy NMO availability
The menu SHALL hide NMO loading outside Windows and when the required legacy loader directory is absent. The supported release set SHALL NOT claim NMO support from the retained Windows 32-bit Virtools importer source alone.

#### Scenario: Current Windows x64 release
- **WHEN** the shipped player has no legacy Virtools loader directory
- **THEN** the NMO menu entry is hidden
