# Native physics changes

Follow the root instructions. This directory contains the C++ bridge; its C# consumers are under `Assets/BallancePhysics`.

- Read both sides of every affected ABI entry, including delegates, layouts, callbacks, and callers. Run `scripts/check-physics-abi.py` after any ABI change.
- Preserve callback rooting and AOT annotations. Check Web static linking and desktop/Android dynamic linking separately.
- Keep native memory, worlds, objects, constraints, and callbacks alive for the required lifetime and release them in the correct order.
- Use the pinned source and toolchains in `scripts/build-physics.py`. Never replace a library with an untracked local build or an unexplained downloaded binary.
- Run the native smoke test on the changed target, then validate the packaged player. A macOS smoke test does not validate Android, Windows, Linux, or Web ABI execution.
- Preserve third-party source/license text. Document target, toolchain, upstream revision, patch changes, and verification in the OpenSpec change when rebuilding a binary.
