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
