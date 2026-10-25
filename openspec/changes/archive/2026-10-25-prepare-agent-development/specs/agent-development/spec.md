## Purpose

Keep agent work traceable through the complete OpenSpec lifecycle with English, source-backed, current, and nonduplicated repository context. See the [source map](/docs/architecture.md#agent-development) for implementation and validation boundaries.

## ADDED Requirements

### Requirement: Full change lifecycle
Every repository mutation SHALL be tracked by an OpenSpec change with proposal, applicable delta specs, design, tasks, implementation, verification, synchronization, and archive. Read-only investigation SHALL be allowed without a change. Documentation-only or behavior-preserving changes SHALL explicitly declare skip_specs when no requirement delta is needed.

#### Scenario: Bug fix
- **WHEN** an agent is asked to fix runtime behavior
- **THEN** it inspects the capability and callers, prepares the change artifacts, implements and checks the fix, records evidence, synchronizes the requirements, and archives only completed work

#### Scenario: Documentation correction
- **WHEN** a change affects documentation but no behavioral requirement
- **THEN** it still records proposal, design, tasks, and verification while explaining skip_specs

#### Scenario: Incomplete work
- **WHEN** required implementation or validation is blocked
- **THEN** the change stays active with unchecked tasks and a clear blocker rather than being archived as complete

### Requirement: English maintenance text
Maintained documentation, agent instructions, OpenSpec artifacts, source comments, region labels, and developer-facing help SHALL be English. Localized game resources, player text, stable identifiers/asset paths, proper names, and original legal notices SHALL retain their necessary language and meaning.

#### Scenario: Comment update
- **WHEN** an agent adds or edits a code comment
- **THEN** it writes useful English prose without renaming the associated serialized fields or localization values

### Requirement: Single source of context
Behavior SHALL be owned by capability specifications, implementation navigation by the source map, operating instructions by their focused guides, and historical decisions/evidence by change records. Changes SHALL update affected context in the same delivery and link to shared facts rather than duplicating them.

#### Scenario: Moved source file
- **WHEN** a change moves a documented implementation path
- **THEN** its source-map links and any scoped instructions are updated with the move

### Requirement: Verifiable completion
CI SHALL validate OpenSpec structure and repository context, detect obvious non-English maintenance text, and require a completed change record for delivered repository edits. Verification records SHALL list actual checks, outcomes, and limitations. Passing these checks SHALL NOT be presented as proof of every gameplay scenario.

#### Scenario: Missing change record
- **WHEN** a delivered diff changes repository files without a completed OpenSpec change record in the diff
- **THEN** the change-record check fails

#### Scenario: Untested platform
- **WHEN** only source inspection or packaging was performed for a platform
- **THEN** verification states that limit instead of claiming a device playthrough

### Requirement: Reproducible agent tooling
The repository SHALL provide pinned OpenSpec setup and local workflow skills, including verification and synchronization, without changing the developer's global OpenSpec preferences.

#### Scenario: Fresh agent setup
- **WHEN** the documented setup command runs with the pinned CLI
- **THEN** the required local skills are installed and the existing project configuration is preserved
