## Why

The game already renders through the GPU, but graphics settings do not identify the active renderer, and release builds do not explicitly restore Unity's automatic API selection or Web high-performance GPU preference. Players need useful diagnostics when browser/driver policy selects a software renderer, without a misleading CPU/GPU switch.

## What Changes

- Explicitly apply Unity's automatic graphics API selection for supported release targets and its high-performance Web GPU hint during content-aware builds.
- Show the reported graphics device and API in graphics settings, with localized advice for recognized software renderers.
- Extend Web performance evidence to record and validate the actual context's power preference, retaining hardware and frame-budget checks.
- Document browser/OS GPU selection limits and the difference between GPU rendering and CPU gameplay/physics.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `platform-builds`: reproducible automatic graphics selection and Web performance preference.
- `user-interface`: truthful graphics-device information and software-rendering advice.
- `developer-tools`: GPU preference included in qualified Web performance evidence.

## Impact

PlayerBuild, graphics settings prefab and UI component, core language resources, editor checks, browser performance scripts/fixtures, and operating documentation. Preserve saved graphics preferences, visuals, IVP simulation, API compatibility, and existing GUIDs. No CPU renderer toggle, GPU physics rewrite, rendering-pipeline migration, forced WebGPU/DX12/Vulkan switch, new dependency, or automatic release publication.
