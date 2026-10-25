## Context

See proposal.md. The checked-in Web preference is already HighPerformance and the previous hardware capture used the Apple M3 GPU. The content-aware builder does not enforce that preference or automatic API selection. Graphics UI uses the existing prefab/page framework and localized core resources. The project uses the Built-in Render Pipeline and native IVP simulation.

## Goals / Non-Goals

Make the intended GPU selection policy reproducible and the actual reported renderer visible. Preserve quality, user preferences, hardware fallbacks, and simulation behavior. This change does not claim a measured FPS gain or override browser/OS GPU policy.

## Decisions

- Apply Unity's automatic API selection in the existing build configuration, plus the standard Web HighPerformance hint. Do not maintain a guessed cross-device ranking of Direct3D, Vulkan, Metal, and OpenGL. The preference is a hint, not a driver override.
- Keep WebGL 2 as the current automatic Web backend. WebGPU can offer useful features, but needs separate shader/readback compatibility and comparative performance work; newer API names do not prove higher FPS. The current player also exposes a screenshot path affected by WebGPU's synchronous readback limitations.
- Add a small read-only graphics settings component that refreshes on page activation, showing SystemInfo's device/API. Display known software fallback advice and an explicit unknown label for missing device information. No saved setting, per-frame query, generic diagnostic framework, or new dependency is needed.
- Keep localized player text in the existing four-language core resource. Disable rich-text interpretation for driver-reported names and wrap long names in a bounded settings row.
- Extend the existing external capture/checker with WebGL context powerPreference. Preserve renderer checks and GPU timings: the hint does not prove that the requested GPU was selected. Bump the report schema so older raw evidence is not silently treated as fully qualified.

## Risks / Trade-offs

- Browsers may ignore the preference or conceal the device name → label information as reported, keep fallback playable, and document OS/browser settings.
- Software-name detection cannot identify every driver → only warn on recognized signatures; never label an unrecognized name as verified GPU acceleration.
- Longer localized names/advice can overflow UI → test layout with real and synthetic device strings and inspect the rebuilt Web settings page.
- A single-GPU reference machine cannot demonstrate discrete-GPU switching → validate the requested context and record the hardware limit explicitly.

## Migration Plan

No save migration. Rebuild content so the settings prefab and player component stay aligned. Existing source builds remain WebGL 2; no API compatibility expansion is introduced. Revert code/prefab together if needed. Source delivery does not automatically publish a binary release.
