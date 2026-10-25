# BallancePhysics

This is the unity wrapper interface of the IVP physics engine. The CSharp interface is under the directory `/assets/ballancephysics`.

For Unity 6000.6.7f1, use `python3 scripts/build-physics.py WebGL` or
`python3 scripts/build-physics.py Android` from the repository root. These builds
pin the IVP revision, apply `source-physics.patch`, and use Unity's toolchains.
See [build and release instructions](../docs/build-and-release.md).
Run `python3 scripts/check-physics-abi.py` after changing the native API.

Compile: you need to clone the source code of [ivp physic engine](https://github.com/nillerusr/source-physics), and then compile the project using Visual Studio 2019+.

---

这个是 IVP 物理引擎的Unity包装接口，C#端的接口在 `/Assets/BallancePhysics` 目录下。

编译：你需要先克隆 [IVP 物理引擎](https://github.com/nillerusr/source-physics), 然后使用 Visual Studio 2019+ 编译此项目。
