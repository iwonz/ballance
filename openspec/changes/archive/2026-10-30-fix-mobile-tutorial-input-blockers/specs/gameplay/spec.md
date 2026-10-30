## MODIFIED Requirements

### Requirement: Pause and resume
Pausing SHALL stop physics simulation, ball control, score countdown, and camera movement. Resuming SHALL close the pause interface and restore the pre-pause control permissions, simulation, countdown and tutorial state. User pause SHALL be distinct from a tutorial's own simulation stop. Pending transitions SHALL be suspended while the user menu is open and cancelled on restart or exit.

#### Scenario: Pause round trip
- **WHEN** the player pauses and then resumes an active level
- **THEN** the ball remains in the same gameplay session and control resumes

#### Scenario: Pause a tutorial or spawn
- **WHEN** the player opens and closes the menu during a tutorial hint or ball appearance
- **THEN** the same tutorial step and previous permissions remain, without a spontaneous transition or unlocked movement

## ADDED Requirements

### Requirement: Single tutorial progression
Tutorial buttons, configured keyboard input and gamepad actions SHALL use one current-step transition. Repeated input SHALL NOT advance more than one step per accepted action. Restart and exit SHALL remove previous listeners and pending callbacks. Continue and Skip SHALL be touch-accessible on each applicable step, including later hints.

#### Scenario: Complete introductory guidance
- **WHEN** a player uses Continue without skipping or a keyboard
- **THEN** the camera, overhead view and movement stages can be completed in order and the ball receives movement force

#### Scenario: Restart after pending input
- **WHEN** a player restarts or exits after advancing a hint
- **THEN** old callbacks cannot change or hide the new tutorial and listeners do not accumulate

#### Scenario: Skip a hint
- **WHEN** a player skips an introductory or later hint
- **THEN** guidance closes and normal gameplay is restored without leftover holds or cards
