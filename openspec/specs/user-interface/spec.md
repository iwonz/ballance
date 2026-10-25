# user-interface Specification

## Purpose

Present menu navigation, dialogs, settings, gameplay status, and audio feedback using the existing page and control framework. See the [source map](/docs/architecture.md#user-interface) for implementation and validation boundaries.

## Requirements

### Requirement: Page navigation
The interface SHALL register named pages, navigate with options, maintain back navigation, and expose page show/hide callbacks. Requests for an unregistered page SHALL fail with a reported error.

#### Scenario: Settings navigation
- **WHEN** the player opens settings from the menu and returns
- **THEN** the appropriate settings page is shown and back navigation restores the previous page

#### Scenario: Missing page
- **WHEN** navigation requests an unregistered page name
- **THEN** the UI reports the missing registration rather than silently substituting another page

### Requirement: Player-facing screens
The game SHALL expose level selection, custom-level/preview entry, high scores, settings, package management, credits, and licenses, plus gameplay pause, failure, and completion screens.

#### Scenario: Pause screen
- **WHEN** gameplay is paused with its UI enabled
- **THEN** the player can resume, restart, or leave through the existing menu controls

### Requirement: Gameplay feedback
The HUD SHALL show points and reserve lives and refresh them on gameplay events. Global loading/error displays and confirmation dialogs SHALL provide the existing feedback and confirmation actions.

#### Scenario: Reserve life changes
- **WHEN** a recoverable fall consumes a life
- **THEN** the HUD updates the displayed reserve lives during recovery

### Requirement: Audio categories and themes
The sound system SHALL expose separate UI, main, and background volume controls, route sounds to their mixers, and provide level music/ambient themes and ball collision/rolling sounds.

#### Scenario: Volume adjustment
- **WHEN** audio settings are applied
- **THEN** the matching mixer volumes are updated for their sound categories

#### Scenario: Level sound lifecycle
- **WHEN** a level is loaded and later unloaded
- **THEN** its music and ball sound controllers are initialized and their registered sound resources are released on teardown

### Requirement: Owned reusable list items
Pooled UI lists SHALL reuse distinct owned objects, grow from their initial capacity to their configured total limit, and reject duplicate or foreign returns without corrupting availability. Clearing a list SHALL release only rented items. Embedded row templates SHALL remain inactive and SHALL NOT be treated as rented items. Disposing a pool SHALL release its owned active and inactive objects; scroll pools SHALL destroy excess recycled items instead of leaving orphaned hidden objects.

#### Scenario: Repeated refresh
- **WHEN** a package or custom-level list is cleared repeatedly and populated again
- **THEN** simultaneously visible rows use distinct objects and no object is returned twice

#### Scenario: Capacity boundary
- **WHEN** more rows than the prewarm count are acquired
- **THEN** the pool grows up to its configured limit and reports exhaustion without dereferencing a missing row

#### Scenario: Screen teardown
- **WHEN** a pooled screen is destroyed
- **THEN** its owned pool objects are cleaned up without destroying another pool's objects

#### Scenario: Embedded row template
- **WHEN** a page keeps a row template under its list root
- **THEN** the template stays hidden while acquired clones become visible, including after repeated refresh

### Requirement: Precise display refresh settings
Graphics settings SHALL display fractional refresh rates and apply the selected resolution using its refresh-rate ratio and an explicit full-screen mode. Existing saved setting keys and resolution selection indices SHALL remain compatible.

#### Scenario: Fractional display mode
- **WHEN** a display reports a rate such as 60000/1001 Hz
- **THEN** the UI does not truncate it to an integer and applying the mode retains that ratio
