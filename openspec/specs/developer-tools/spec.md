# developer-tools Specification

## Purpose

Support the existing debug console, level/package authoring, and validation tools without promising an unfinished standalone level editor. See the [source map](/docs/architecture.md#developer-tools) for implementation and validation boundaries.

## Requirements

### Requirement: Debug mode and console
The game SHALL expose its existing debug-mode activation, console, and command registration with argument-count checks. Debug commands SHALL remain distinct from ordinary player UI controls.

#### Scenario: Console use
- **WHEN** debug mode is active and the console key is pressed
- **THEN** the developer can enter registered commands and see command output

#### Scenario: Unlock built-in levels
- **WHEN** the developer runs the existing highscore open-all command
- **THEN** the built-in level pass-state entries are marked available

### Requirement: Editor content authoring
Unity editor tools SHALL package configured modules and levels for the selected build target, with metadata and compatible AssetBundles. The content-aware player build SHALL use these tools rather than relying on the default player build alone.

#### Scenario: Package a level
- **WHEN** a valid level definition is packaged for a target
- **THEN** the output contains the level metadata, prefab, and referenced assets for that target

### Requirement: Extensible level mechanisms
The level authoring/runtime interface SHALL support registered mechanism prefabs, custom loading stages, and lifecycle events for activation, reset, preview, and unload.

#### Scenario: Custom mechanism
- **WHEN** a registered mechanism is referenced by a valid level configuration
- **THEN** the loader instantiates it and connects it to the sector lifecycle

### Requirement: Honest diagnostic coverage
Development documentation SHALL distinguish editor checks, native smoke checks, packaging checks, browser smoke tests, and real-device gameplay. Retained legacy tools SHALL NOT be described as validated current release features without evidence.

#### Scenario: Packaging-only platform verification
- **WHEN** a platform build and archive pass but gameplay was not run on that platform
- **THEN** the verification record explicitly states that runtime gameplay remains untested

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

### Requirement: Web GPU preference evidence
Qualified Web performance reports SHALL include the running WebGL context's power preference. A missing or non-performance preference SHALL fail the documented reference-profile qualification, independently of average FPS. A high-performance preference alone SHALL NOT be accepted as proof of hardware rendering.

#### Scenario: Power preference regression
- **WHEN** an otherwise fast report omits the context preference or reports a default/low-power preference
- **THEN** the performance checker refuses a passing reference-profile verdict
