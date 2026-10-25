## Purpose

Preserve player settings, local high scores, level completion state, and package choices across compatible launches. See the [source map](/docs/architecture.md#player-persistence) for implementation and validation boundaries.

## ADDED Requirements

### Requirement: Namespaced settings
Settings SHALL use package-prefixed keys and preserve their existing stored types, including integer key bindings and string boolean values. Reset-to-default SHALL clear PlayerPrefs and notify registered settings consumers.

#### Scenario: Package setting isolation
- **WHEN** two packages save a setting with the same local key
- **THEN** their package-prefixed keys remain separate

### Requirement: Local high scores and completion state
The game SHALL load existing high-score and level-pass files from persistent storage or use defaults when those files are absent. It SHALL preserve the existing JSON format and order inserted scores from highest to lowest.

#### Scenario: Fresh installation
- **WHEN** neither score nor pass-state file exists
- **THEN** default scores and an empty completion-state map are used

#### Scenario: New score
- **WHEN** a score is recorded for a level
- **THEN** it is inserted into that level's ranking and the level is marked completed

### Requirement: Persistence on normal quit
The normal quit lifecycle SHALL save high scores and level-pass state. Package enabled/trusted state and no-package mode SHALL use the existing package-status XML in persistent storage.

#### Scenario: Save and relaunch
- **WHEN** valid player progress is saved through normal quit and the same files are loaded on relaunch
- **THEN** the stored scores and completion state are restored

### Requirement: Separate writable and shipped content
Runtime paths SHALL distinguish shipped core/package/level content from writable persistent data. Web and mobile built-in resources SHALL be addressable from StreamingAssets without rewriting their location into a local filesystem path.

#### Scenario: Browser subpath
- **WHEN** the player is hosted below a repository URL prefix
- **THEN** built-in resource requests preserve that prefix while player data uses the runtime's persistent storage
