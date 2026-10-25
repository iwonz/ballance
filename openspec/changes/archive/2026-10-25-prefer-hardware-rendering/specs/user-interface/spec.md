## ADDED Requirements

### Requirement: Reported rendering device
Graphics settings SHALL display the graphics device and API reported by the running player without a misleading CPU/GPU toggle. A recognized software renderer SHALL produce localized guidance for enabling hardware acceleration or selecting a performance GPU outside the game. Unavailable device information SHALL be reported as unknown, not as verified hardware acceleration.

#### Scenario: Hardware device information
- **WHEN** the player opens graphics settings
- **THEN** the page shows the reported graphics device and API without modifying saved graphics preferences or polling device information every frame

#### Scenario: Software fallback or unavailable information
- **WHEN** the reported renderer matches a known software fallback or no device name is available
- **THEN** settings show the corresponding warning or unknown state without preventing the player from continuing
