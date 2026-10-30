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

### Requirement: Reported rendering device
Graphics settings SHALL display the graphics device and API reported by the running player without a misleading CPU/GPU toggle. A recognized software renderer SHALL produce localized guidance for enabling hardware acceleration or selecting a performance GPU outside the game. Unavailable device information SHALL be reported as unknown, not as verified hardware acceleration.

#### Scenario: Hardware device information
- **WHEN** the player opens graphics settings
- **THEN** the page shows the reported graphics device and API without modifying saved graphics preferences or polling device information every frame

#### Scenario: Software fallback or unavailable information
- **WHEN** the reported renderer matches a known software fallback or no device name is available
- **THEN** settings show the corresponding warning or unknown state without preventing the player from continuing

### Requirement: Responsive touch guidance
Touch tutorial guidance SHALL use original panels, fonts and tutorial arrows. Portrait guidance SHALL sit above the control zone; landscape guidance SHALL sit between control zones. Long text SHALL scroll inside the card while action buttons remain fixed. Touch targets SHALL be at least 48 CSS pixels on Web and 48 dp on Android, respecting safe areas and viewport changes. Touch instructions SHALL describe touch, swipes and camera buttons instead of keyboard-only actions.

#### Scenario: Narrow or short viewport
- **WHEN** the viewport rotates or browser bars change its available height
- **THEN** the card remains readable and Continue and Skip remain reachable without overlapping movement controls

### Requirement: Compact gameplay menu
Mobile gameplay SHALL expose a compact menu icon in a reserved upper-right area without covering the HUD, including during tutorials. It SHALL replace the large pause label and permanent generic input hint. The menu SHALL expose resume, settings, restart and exit.

#### Scenario: Menu from tutorial
- **WHEN** a player taps the menu icon on any tutorial stage
- **THEN** the menu opens and can be closed by touch to return to that same stage

#### Scenario: Original menu touch access
- **WHEN** a player selects a level or opens the original settings menu on a narrow phone
- **THEN** existing themed buttons have 48-pixel touch targets and long lists can scroll without changing identifiers or selection behavior
