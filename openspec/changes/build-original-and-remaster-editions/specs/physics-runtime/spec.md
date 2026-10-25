## MODIFIED Requirements

### Requirement: Compatible native API
The managed wrapper and shipped native plugin SHALL agree on edition-specific API
version, signatures, layouts and calling conventions: Original retains 2302 and
Remaster uses 2304. Web SHALL link statically; desktop and Android SHALL use their
appropriate native library. An incompatible plugin SHALL fail before table dispatch.

#### Scenario: Native smoke check
- **WHEN** the shipped library is loaded by the smoke test
- **THEN** API validation, vector readback, world simulation, collision and constraint cleanup succeed

#### Scenario: Wrong edition plugin
- **WHEN** a Remaster player loads an Original-only native API
- **THEN** initialization reports incompatibility without calling missing functions

## ADDED Requirements

### Requirement: Dynamic localized contact profiles
Remaster SHALL update material-pair and local surface contact properties without
recreating bodies, altering baseline geometry, or losing constraint/velocity state.
Existing persistent contacts SHALL reflect gradual changes. Dry profiles SHALL retain
baseline behavior. Native state SHALL be released with its owning body/world.

#### Scenario: Wetting while supported
- **WHEN** a ball remains supported while the surface gradually becomes wet
- **THEN** existing contacts update consistently without a teleport or artificial impulse

#### Scenario: Repeated level lifecycle
- **WHEN** weather-enabled levels are repeatedly loaded and unloaded
- **THEN** surface regions, callbacks and force ownership are cleaned up with their worlds

#### Scenario: Quit while a ball is supported
- **WHEN** the application shuts down with active friction contacts
- **THEN** registered worlds and bodies are destroyed before the native temporary-vector pool, and later component cleanup does not use released native state
