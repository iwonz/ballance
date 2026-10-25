## ADDED Requirements

### Requirement: Reproducible Web frame performance gate
Performance tooling SHALL measure rendered game frames, retain raw timing evidence, and report average FPS, tail frame intervals, stalls, and browser/rendering conditions for warmed-up menu and gameplay scenarios. It SHALL fail a documented 60 FPS budget on a qualified reference profile when sustained throughput or frame pacing regresses. It SHALL NOT interpret browser animation callbacks without game rendering as game FPS.

#### Scenario: Slow or frozen renderer
- **WHEN** a capture contains sustained 30 FPS, long stalls, or no game-rendered frames
- **THEN** the performance check fails even if browser animation callbacks continue

#### Scenario: Unqualified measurement
- **WHEN** the tab is hidden, the renderer is software-based, GPU timings are unavailable or disjoint, or the capture is incomplete
- **THEN** the check refuses a passing hardware performance verdict

#### Scenario: Reproducible comparison
- **WHEN** before/after builds are measured
- **THEN** evidence identifies the browser, GPU, render resolution, scenario, warm-up and capture duration, and distinguishes hardware performance results from hosted checker self-tests
