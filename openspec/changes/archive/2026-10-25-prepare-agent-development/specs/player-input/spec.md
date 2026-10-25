## Purpose

Support configurable desktop controls and touch input while preserving the legacy key settings used by gameplay on Android. See the [source map](/docs/architecture.md#player-input) for implementation and validation boundaries.

## ADDED Requirements

### Requirement: Configurable controls
The game SHALL retain key bindings for ball movement and camera controls and expose them through the control settings. Input dispatch SHALL support press/release listeners used by gameplay and UI.

#### Scenario: Changed binding
- **WHEN** a movement key setting is changed and applied
- **THEN** the ball controller listens to the selected binding

### Requirement: Android compatibility input
Android SHALL use the Input System backend while retaining saved KeyCode bindings, mouse-axis scaling, and the existing touch control interfaces. Missing devices SHALL yield inactive input rather than requiring physical peripherals.

#### Scenario: Keyboard mapping
- **WHEN** a supported saved KeyCode is pressed on an Android keyboard device
- **THEN** its mapped input state is available to the legacy control callers

#### Scenario: Touch release
- **WHEN** the primary touch is released
- **THEN** the pointer compatibility path exposes the release to UI consumers

### Requirement: Mobile control layout
The mobile settings interface SHALL allow selecting and arranging the existing touch controls, including directional and joystick input, and persist the chosen layout.

#### Scenario: Touch direction
- **WHEN** a player presses and releases a touch direction control
- **THEN** the corresponding ball movement state starts and stops

### Requirement: Camera and UI input separation
Camera manipulation SHALL respect the existing GUI focus guards. Gameplay SHALL support camera rotation/elevation separately from ball movement, including suppressing movement during the camera-rotation modifier.

#### Scenario: Camera rotation modifier
- **WHEN** the player holds the camera rotation modifier and directional keys
- **THEN** the camera rotates without applying the corresponding ball movement force
