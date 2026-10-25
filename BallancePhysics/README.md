# BallancePhysics

This directory contains the C++ bridge to the IVP physics engine. Its managed
wrapper is in [Assets/BallancePhysics](../Assets/BallancePhysics). Read
[AGENTS.md](AGENTS.md) and the [physics source map](../docs/architecture.md#physics-runtime)
before changing the native API.

For reproducible Web or Android rebuilds, run from the repository root:

```sh
python3 scripts/build-physics.py WebGL
python3 scripts/build-physics.py Android
python3 scripts/check-physics-abi.py
```

The script pins [upstream IVP](https://github.com/nillerusr/source-physics), applies
[source-physics.patch](source-physics.patch), and uses Unity's matching toolchains.
See [build and release instructions](../docs/build-and-release.md) and
[validation](../docs/validation.md#native-physics). The retained Visual Studio project
is a legacy Windows bridge build path; do not assume it recreates every shipped
platform library without a target-specific toolchain and validation.
