## MODIFIED Requirements

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

## ADDED Requirements

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
