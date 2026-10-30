## 1. Input and lifecycle

- [x] 1.1 Fix UI raycast order and permission-aware controls; verify real prefab Continue hit and joystick release/cancel/menu paths.
- [x] 1.2 Separate user pause snapshots and deferred spawn completion; verify exact state restoration and restart/exit cancellation.
- [x] 1.3 Unify tutorial transitions and cleanup; verify Continue sequence, repeated taps, skip and later hints without stale callbacks.

## 2. Presentation

- [x] 2.1 Add safe responsive tutorial text scrolling and fixed touch targets, compact menu and localized touch instructions; inspect both orientations and preserve this game's theme.
- [x] 2.2 Update source map and validation guidance; run localization and repository checks.

## 3. Verification and delivery

- [x] 3.1 Run Unity 6000.6.7f1 input and prefab integration checks and touch-only Chromium/WebKit flows, including pause, settings, layout changes, restart, fall/recovery and exit; record outcomes and physical-device limits.
- [x] 3.2 Update release version/code, build all five supported targets and verify archives/checksums/identity plus available Android validation.
- [x] 3.3 Record verification, review changes, synchronize specs and archive this completed fix; run final strict/context checks.
