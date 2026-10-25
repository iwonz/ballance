## ADDED Requirements

### Requirement: Web GPU preference evidence
Qualified Web performance reports SHALL include the running WebGL context's power preference. A missing or non-performance preference SHALL fail the documented reference-profile qualification, independently of average FPS. A high-performance preference alone SHALL NOT be accepted as proof of hardware rendering.

#### Scenario: Power preference regression
- **WHEN** an otherwise fast report omits the context preference or reports a default/low-power preference
- **THEN** the performance checker refuses a passing reference-profile verdict
