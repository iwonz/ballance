# physics-runtime Specification

## Purpose

Provide the native IVP simulation contract used by gameplay, including matching ABI, object ownership, collision callbacks, and constraints. See the [source map](/docs/architecture.md#physics-runtime) for implementation and validation boundaries.

## Requirements

### Requirement: Compatible native API
The managed wrapper and shipped native plugin SHALL agree on API version 2302, function signatures, layouts, and calling conventions. Web SHALL link its native implementation statically; desktop and Android SHALL use the appropriate native library.

#### Scenario: Native smoke check
- **WHEN** the shipped library is loaded by the smoke test
- **THEN** API version validation, vector readback, world simulation, collision, and constraint cleanup succeed

### Requirement: Physics world simulation
A created world SHALL simulate enabled objects using its configured gravity, step rate, and time factor, then expose updated transforms and collision events to gameplay. Disabling simulation SHALL stop world advancement.

#### Scenario: Gravity and collision
- **WHEN** a dynamic ball is simulated above a static floor
- **THEN** it falls under gravity and is constrained by the floor collision

### Requirement: Body and constraint lifecycle
The wrapper SHALL support creation and removal of static/dynamic bodies, collision filtering, phantoms, impulses, constant forces, and the exposed constraint types. Native callbacks SHALL remain valid for their owning objects' lifetimes.

#### Scenario: Constraint removal
- **WHEN** a created fixed constraint is destroyed
- **THEN** its managed/native ownership is released without invoking a collected callback

### Requirement: Platform-compatible native artifacts
Shipped native artifacts SHALL match their target CPU and toolchain. Android artifacts SHALL satisfy the build pipeline's 16 KB segment alignment check; Web physics SHALL run without worker threads.

#### Scenario: Rebuild Web physics
- **WHEN** the pinned Web physics build succeeds
- **THEN** its Wasm smoke test passes before the checked-in library is replaced
