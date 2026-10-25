## Why

Web frame pacing has no repeatable performance check, and players report low FPS. Release builds retain the legacy Web exception compilation profile; measurements must distinguish rendering work from browser refresh limits and software rendering.

## What Changes

- Establish repeatable, warmed-up Web menu and gameplay measurements on a visible hardware-accelerated browser, with actual rendered frame intervals and tail latency.
- Optimize demonstrated Web overhead while preserving exception handling, visual quality, and IVP simulation behavior.
- Add an executable 60 FPS regression gate, negative fixtures, and documented hardware/profile requirements. Hosted CI validates the gate; hardware measurements remain separate.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `developer-tools`: reproducible frame performance measurements and honest regression verdicts.
- `platform-builds`: performance-oriented Web release compilation with retained error handling.

## Impact

Web build configuration and the pinned native IVP archive/toolchain flags, browser performance scripts and their self-tests, CI, validation/build documentation, and relevant specs. Compare identical render resolution and scenarios before/after; no automatic quality reduction, physics timestep changes, new rendering pipeline, iOS work, or unsupported universal hardware guarantee.
