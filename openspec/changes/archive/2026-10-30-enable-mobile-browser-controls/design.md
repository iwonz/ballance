## Context

See proposal.md. Existing Unity UI keypads and player force/camera APIs already provide the core controls. Compile-time mobile gates exclude Web. The directional controller polls global touches and lacks cancellation ownership; the joystick depends on ScrollRect callbacks. Existing saved layout IDs and prefab references must remain compatible.

## Goals / Non-Goals

Deliver playable mobile Web and Android through existing UI, with optional tilt and explicit controls. Preserve native physics, desktop bindings and save identity. Do not add an input framework, iOS distribution, or claim physical-device ergonomics from browser emulation.

## Decisions

- Detect mobile/touch capability at runtime. Use the existing left joystick as the new-profile default and retain all registered layouts. Route mobile settings using the same capability check.
- Use EventSystem pointer ownership for joystick/directional controls and reset on interruption. A free-area swipe handler ignores UI-originating touches; buttons remain the primary discoverable camera controls.
- Add tilt as a settings layout backed by the existing joystick prefab. Keep touch override, a recenter action, localized instructions and sensor status. Native Android uses Input System gravity/acceleration; Web uses a small browser bridge with a direct user-gesture permission action, freshness checks and screen rotation mapping. No permission is requested until selected.
- Bound analog input, apply a dead zone and time-based smoothing; release input on pause, focus loss, rotation, destruction and replacement. Only active gameplay writes force.
- Fit keypad roots to safe areas. Supply a responsive host with touch-action disabled on canvas, fullscreen from a user action and bounded mobile DPR. Keep existing public URLs and save directories.
- Extend virtual input/editor assertions and browser bridge self-tests. Build Web/Android and smoke touch, multitouch, settings, pause/resume, rotation and sensor fallback in an isolated browser. Use available physical devices if connected; record unavailable hardware checks explicitly.

## Risks / Trade-offs

Sensor/browser support varies; zero force and visible touch fallback are required. Phone orientation and real-device comfort need device testing. Lower mobile pixel density trades some sharpness for reduced GPU/memory work. No hardware FPS guarantee follows from build success.

## Migration Plan

Build all five targets with the pinned Unity editor, package and verify each archive, commit source and its completed record, then publish a new immutable release and deploy its matching Web asset. Existing tags/assets are not overwritten. Rollback uses an explicit prior release tag. Preserve unrelated working-tree files.

Browser acceptance exposed the legacy layout cards swallowing scroll drags through EventTrigger. Use ordinary Unity Buttons for card selection so the parent ScrollRect owns swipes; separate thumbnail and label bounds in both editions. Keep saved layout IDs unchanged.

Desktop browser verification found Unity creates a virtual Touchscreen even without touch hardware. Web capability detection therefore uses `navigator.maxTouchPoints`; native players retain the existing Input System/mobile-platform check. Verify both desktop keyboard settings and phone controls. This Web-only correction does not change compiled native code paths.

The user clarified that budget and midrange Android phones are the primary targets, without a fixed device/browser. Keep the bounded mobile render density and conservative mobile defaults; real low-end memory, thermal and sensor comfort remain device-validation limits rather than desktop-emulation claims.

An early-pause browser check exposed a delayed spawn EventStart reopening the mobile overlay above the pause menu. The overlay must consult the current playing state when that delayed event arrives. Verify early pause remains navigable, then rebuild all targets because this callback is shared with native players.
