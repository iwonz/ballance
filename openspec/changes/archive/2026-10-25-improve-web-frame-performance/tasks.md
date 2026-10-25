## 1. Measurement and regression checks

- [x] 1.1 Add an external Web rendered-frame capture with metadata, warm-up, menu and gameplay scenarios; verify it against the existing hardware-accelerated player.
- [x] 1.2 Add the 60 FPS report gate and adversarial self-tests for stalls, low throughput, missing draws, hidden tabs, software rendering, and invalid/incomplete data; run them locally and wire them into hosted CI.

## 2. Web optimization

- [x] 2.1 Pin runtime-speed LTO and enable Wasm 2023 while retaining exception handling; build the complete Web player successfully with Unity 6000.6.7f1.
- [x] 2.3 Rebuild the pinned Web IVP archive with compatible native longjmp/SIMD flags; verify the Web native smoke and unchanged ABI before the player rebuild.
- [x] 2.2 Compare repeated before/after captures at identical resolution, inspect gameplay and corrupt-content failure behavior, and record the measured effect without claiming unsupported platform coverage.

## 3. Documentation and delivery

- [x] 3.1 Document executable performance checks, browser/hardware qualifications, budget tolerances, and remaining coverage limits; validate context and strict OpenSpec.
- [x] 3.2 Review requirements against implementation and checks, write verification.md, and prepare the verified delta for synchronization/archive.
