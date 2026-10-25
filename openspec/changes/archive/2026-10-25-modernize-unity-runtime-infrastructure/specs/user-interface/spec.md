## ADDED Requirements

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
