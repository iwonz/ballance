## ADDED Requirements

### Requirement: Edition storage isolation
Remaster SHALL use independent settings, scores, progression and package-choice storage
without automatically importing Original data. Resetting Remaster SHALL NOT erase
Original data. Relocating Original's Web assets SHALL preserve its existing save identity.

#### Scenario: Reset one edition
- **WHEN** Remaster settings or progress are reset and Original is relaunched
- **THEN** Original's previously saved data remains available and unchanged

### Requirement: Environment-independent records
Reballance SHALL use one record table per level across all environments. Selecting
or changing an environment SHALL NOT change score calculation, timers, record
eligibility or progression. There SHALL be no practice-run classification.
Earlier preset/rules score files SHALL be merged into the shared table, preserving
distinct entries and original files without importing classic Ballance records.

#### Scenario: Mixed-weather completion
- **WHEN** a level is completed after a manual weather change
- **THEN** its score qualifies for the same shared records and progression as any other completion

#### Scenario: Update an existing Reballance profile
- **WHEN** earlier development record files exist and the shared table is absent
- **THEN** their distinct scores are combined in score order, unlocks remain available and source files remain unchanged
- **AND** subsequent loads use the shared table without repeatedly importing old scores

#### Scenario: Malformed legacy scores
- **WHEN** a legacy score file cannot be parsed or has invalid records
- **THEN** migration fails visibly without replacing any source file or writing a partial shared table

### Requirement: Product rename preserves existing remaster data
Reballance SHALL preserve its pre-rename remaster application identifier, preferences, file storage and Web URL storage identity. Renaming the product SHALL not import classic Ballance data.

#### Scenario: Existing development installation
- **WHEN** a player updates the earlier remaster application to Reballance
- **THEN** that edition retains its settings and progress while Ballance data remains separate
