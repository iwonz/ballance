# gameplay Specification

## Purpose

Provide the original ball-balancing loop: materials, mechanisms, checkpoints, score, lives, transformation, pause, and completion. See the [source map](/docs/architecture.md#gameplay) for implementation and validation boundaries.

## Requirements

### Requirement: Ball types and movement
Gameplay SHALL provide wood, stone, and paper balls with their configured physics and presentation. Movement SHALL be relative to the camera and enabled only in the appropriate control state.

#### Scenario: Controlled ball
- **WHEN** directional input is held during normal gameplay
- **THEN** the active ball receives movement force relative to the current camera orientation

#### Scenario: Transformation
- **WHEN** a ball enters a transformer for another type
- **THEN** control is temporarily locked, the transformation effect runs, and the target ball becomes controllable

### Requirement: Sector progression and reset
The level SHALL activate mechanisms for the current sector, advance at checkpoints, and use the sector reset point for recovery. Restarting SHALL reset all sectors and initial counters.

#### Scenario: Checkpoint
- **WHEN** the player reaches the next checkpoint
- **THEN** sector activation and the recovery location advance together

#### Scenario: Restart
- **WHEN** the player restarts the current level
- **THEN** the starting ball, lives, points, and first sector are restored

### Requirement: Falling and lives
A controllable ball falling before completion SHALL consume one positive reserve life and respawn at the current sector. With zero reserve lives it SHALL enter the failure flow. Negative life counts SHALL support the existing infinite-life mode. Concurrent fall handling SHALL be guarded.

#### Scenario: Recoverable fall
- **WHEN** a ball falls with at least one reserve life
- **THEN** the reserve count decreases once and the ball respawns at the current sector

#### Scenario: No reserve lives
- **WHEN** the ball falls with zero reserve lives
- **THEN** the failure menu is shown instead of respawning

### Requirement: Points and completion
During active scored play the time-point counter SHALL decrease at its configured half-second cadence. Point and life pickups SHALL update the HUD. Completion SHALL stop normal control and start the configured ending and score sequence once.

#### Scenario: Pickup
- **WHEN** a point pickup is collected
- **THEN** the configured award is added and the point display is refreshed

#### Scenario: Complete a level
- **WHEN** the player activates the level end
- **THEN** the completion sequence and score interface run and repeated completion calls do not restart them

### Requirement: Pause and resume
Pausing SHALL stop physics simulation, ball control, score countdown, and camera movement. Resuming SHALL restore gameplay and close the pause interface.

#### Scenario: Pause round trip
- **WHEN** the player pauses and then resumes an active level
- **THEN** the ball remains in the same gameplay session and control resumes
