## ADDED Requirements

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
