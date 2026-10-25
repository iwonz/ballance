## ADDED Requirements

### Requirement: Performance-oriented graphics selection
Content-aware release builds SHALL explicitly use the engine's automatic graphics API selection for the supported release targets and request the high-performance GPU preference for Web. This preference SHALL NOT be described as a guarantee that the browser or operating system selects a particular GPU.

#### Scenario: Rebuild after local graphics overrides
- **WHEN** a supported release target is built after local graphics settings were changed
- **THEN** the build restores automatic graphics API selection and, for Web, the high-performance preference
