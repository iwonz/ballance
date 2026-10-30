# player-input Specification

## Purpose

Support configurable desktop controls and touch input while preserving the legacy key settings used by gameplay on Android. See the [source map](/docs/architecture.md#player-input) for implementation and validation boundaries.

## Requirements

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
The mobile settings interface SHALL allow selecting and arranging the existing touch controls, including directional and joystick input, and persist the chosen layout. Android and touch-capable mobile Web players SHALL expose movement, camera rotation/elevation and pause without a keyboard. New profiles SHALL default to an analog joystick; saved layouts SHALL remain available.

#### Scenario: Touch direction
- **WHEN** a player presses and releases a touch direction control
- **THEN** the corresponding ball movement state starts and stops

#### Scenario: Simultaneous camera and movement
- **WHEN** one finger moves the ball and another operates a camera control
- **THEN** each control tracks its own pointer without stealing the movement finger

#### Scenario: Interrupted input
- **WHEN** a touch is cancelled, controls are replaced, the game pauses, focus is lost or the screen rotates
- **THEN** pending movement and camera holds are released and stale gestures cannot resume movement

#### Scenario: Browse control layouts by touch
- **WHEN** a player swipes the mobile layout list and selects a control card
- **THEN** the list scrolls without selecting during the drag, labels remain readable, and a tap applies the selected layout

#### Scenario: Desktop browser without touch
- **WHEN** a desktop browser reports no touch points
- **THEN** it retains keyboard settings and does not show the mobile keypad merely because Unity allocated a virtual touch device

### Requirement: Camera and UI input separation
Camera manipulation SHALL respect the existing GUI focus guards. Gameplay SHALL support camera rotation/elevation separately from ball movement, including suppressing movement during the camera-rotation modifier.

#### Scenario: Camera rotation modifier
- **WHEN** the player holds the camera rotation modifier and directional keys
- **THEN** the camera rotates without applying the corresponding ball movement force

### Requirement: Optional calibrated tilt
Mobile control settings SHALL offer opt-in tilt steering using available orientation/gravity sensors, with a neutral calibration, dead zone and bounded smoothed force. A touch joystick SHALL remain usable and take priority while held. Sensor denial, absence or stale data SHALL yield no sensor force and a visible fallback message. Tilt SHALL not silently request permission on startup.

#### Scenario: Enable and calibrate
- **WHEN** the player selects tilt and grants any required permission while holding the phone comfortably
- **THEN** the current position becomes neutral and subsequent tilts steer relative to the current screen orientation

#### Scenario: Recenter and rotate
- **WHEN** the player recalibrates or rotates the display
- **THEN** the previous calibration is discarded before further tilt force is applied

#### Scenario: Sensor unavailable
- **WHEN** permission is denied or no fresh sensor sample arrives
- **THEN** tilt contributes zero force and the player can continue using touch controls

### Requirement: Touch camera gestures
The mobile gameplay surface SHALL support horizontal swipes to rotate the camera without consuming touches that began on controls or menus. Explicit camera buttons SHALL remain available.

#### Scenario: Camera swipe
- **WHEN** a player swipes horizontally on the free gameplay area
- **THEN** the camera turns once in that direction without changing joystick ownership

### Requirement: Permission-aware touch priority
Interactive tutorial, menu and dialog controls SHALL receive touches before the gameplay swipe surface. Movement controls SHALL be hidden when movement is forbidden; camera actions SHALL be shown only when permitted. Opening a page SHALL release gameplay holds and prevent delayed spawn or layout replacement from reactivating gameplay input over that page.

#### Scenario: Continue above gestures
- **WHEN** a player taps Continue while the keypad exists
- **THEN** the current tutorial step advances once and the swipe surface does not consume the tap

#### Scenario: Camera-only stage
- **WHEN** the tutorial permits the camera but locks movement
- **THEN** the joystick is hidden and available camera controls still operate

#### Scenario: Menu during held input
- **WHEN** a player opens a menu while holding movement or while the ball is appearing
- **THEN** all holds stop and remain stopped through layout changes until the appropriate gameplay state is restored
