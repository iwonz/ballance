## ADDED Requirements

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
