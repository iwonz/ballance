## Context

See proposal.md. The current release build explicitly disables Wasm 2023 and enables full exception stacks. Its code optimization mode is not pinned. Web defaults to High quality and browser-driven refresh; Retina doubles the canvas dimensions. IVP runs through the existing fixed-update scheduler. No simulation or quality changes are needed to establish performance evidence.

## Goals / Non-Goals

Establish a repeatable hardware frame budget and reduce compilation overhead. Preserve visuals, input, save data, and native simulation timing. This is a Web regression baseline, not certification of every level, browser, mobile device, or desktop player.

## Decisions

- Enable Unity's Wasm 2023 native exceptions/SIMD and runtime-speed LTO, retaining full exception handling. Unity documents low exception overhead with Wasm 2023. Disabling exceptions would break existing error boundaries; changing physics or reducing quality would confound the comparison.
- Rebuild the pinned IVP archive with Wasm-native longjmp support. The original archive contains emulated `emscripten_longjmp` references in convex-builder objects, which fail to link with the Wasm 2023 player. Keep C++ exceptions disabled as before; only align the longjmp target and enable SIMD. Run the same native smoke with matching flags before replacing the Web archive. No ABI or simulation changes are involved.
- Measure externally in a real browser, avoiding a permanently running in-game profiler. Count animation callbacks only when the Unity canvas submits WebGL draw calls; collect raw intervals, callback CPU duration, draw counts, and asynchronous WebGL GPU timer queries. Require valid GPU timings so a fast CPU submitting work to an overloaded GPU cannot pass. This measures render submission cadence and GPU work, not physical presentation latency.
- Use a fresh browser profile at the same viewport/device scale, a visible hardware renderer, at least 10 seconds of warm-up and 30 seconds of capture per scenario. Record actual canvas pixels, GPU, browser, build identity, and visibility. Exercise the animated menu and level 01 with live physics; keep loading outside the capture.
- Calibrate idle browser animation cadence before loading Unity; a browser refreshing below 59 Hz cannot qualify as a 60 FPS reference environment.
- The 60 FPS gate permits browser clock jitter: average at least 59 FPS, p95 at most 20 ms, p99 at most 25 ms, no more than 0.1% intervals above 33.34 ms, and no interval above 100 ms. GPU p95 must fit within 16.67 ms, with p99 at most 20 ms and the same stall fraction/maximum limits. These tolerances are explicit, not a claim that every frame always takes at most 16.67 ms. Higher-refresh displays can exceed 60 without a cap.
- Use small browser automation and a standard-library report checker with adversarial fixtures. Hosted CI checks measurement/gate logic; it does not claim GPU gameplay coverage. Hardware captures are retained outside source control, with summarized evidence in verification.md.

## Risks / Trade-offs

- Browser/OS scheduling noise → warm-up, visible foreground tab, idle machine, repeat runs and retained raw data.
- Instrumentation overhead → only draw submissions and animation callbacks; same instrumentation before/after; no screenshot/log analysis during capture.
- Native Wasm feature compatibility → document browser requirements and run the rebuilt player including native IVP and a corrupt-content failure.
- A start-area fixture misses later mechanisms → explicitly scope evidence and document repeating captures in heavier levels; do not generalize to all thirteen levels.

## Migration Plan

Build a separate Web output, compare against c28ece4 on the same hardware, run error-path and gameplay smoke, then deliver source and verified specs. Roll back the Web build settings, native archive, and native compiler flags together to restore the previous compilation profile without save migration. Publishing a new release remains coupled to matching Pages deployment under the existing release workflow.
