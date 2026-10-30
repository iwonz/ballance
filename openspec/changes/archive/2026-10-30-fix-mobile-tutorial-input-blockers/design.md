## Context

See proposal.md. Keypad views currently sit above tutorial views, and a full-screen raycast catches Continue. Legacy tutorial callbacks and key listeners outlive their stage. Resume currently restarts control instead of restoring the previous state.

## Goals / Non-Goals

Keep original serialized fields, prefab GUIDs, saved layout identifiers and public PauseLevel(bool)/ResumeLevel(bool) signatures. Use native IVP unchanged. Do not redesign Ballance, close unrelated remaster work or add native iOS.

## Decisions

- Put the keypad at the bottom of the gameplay view layer, with a dedicated gesture surface below interactive descendants. Derive availability from ball permissions and current page; release before disabling. Keep the menu available independently of movement.
- Snapshot user pause state separately from tutorial stops. Defer spawn completion and fall/recovery timers while user-paused, invalidate it on restart/quit, and restore the snapshot without starting a new run.
- Use one tutorial stage handler with owned key subscriptions, a session generation and pause-aware delays. Preserve each game's arrows and style rather than sharing remaster presentation.
- Adapt the existing tutorial prefab with a safe-area layout, scrollable text and fixed action row. Compute minimum targets from CSS viewport/native density instead of assuming Canvas units equal physical UI units.
- Preserve authored left/right screen anchors independently of grid child alignment. Refit all saved layouts and size settings within viewport bounds; separate center-layout camera actions from movement visibility. Reserve the actual control height below portrait tutorial cards.
- Enlarge original main/level/settings menu buttons to 48 logical pixels and scroll long level lists, retaining their assets and direct child paths.
- Apply logical touch sizing and scrolling to shared settings/confirmation menus while retaining each edition's existing theme and child lookup paths. WebKit touch-event support plus a coarse pointer also enables mobile input when maxTouchPoints is zero.
- Size the existing mobile control-settings list and reverse/size actions in logical units; use side-by-side list/options in landscape and preserve all settings bindings.
- Expand the existing common menu host within the mobile safe area before fitting buttons; preserve panel artwork and give wrapped labels the full button height. Include the real host in menu regression fixtures.
- Fit landscape tutorial width between the actual control zones; stack its actions when the remaining center column is too narrow for readable labels.
- Reserve the upper-center HUD gap for a concise unavailable-sensor message, above tutorial cards. The browser sensor-permission dialog blocks underlying page/game touches until dismissed.
- Test real prefab hierarchy and EventSystem raycasts, then touch-only Chromium and WebKit flows before freezing all release builds. Direct pointer callbacks alone cannot catch the reported defect.

## Risks / Trade-offs

- Existing pause callers and spawn callbacks can overlap: audit all callers and cover menu during spawn and later hints.
- Browser emulation cannot establish physical touch/sensor ergonomics: record real-device limits separately.
- Original and remaster presentation can drift: inspect both portrait and landscape independently.

## Migration Plan

No saved-data migration. Publish verified assets under new immutable tags, deploy matching Pages, and retain old releases for rollback. Record checks and limitations before spec synchronization/archive; publish only after all target archives are verified.

Directed real-level editor checks exercise the bound tutorial buttons and authored
later-hint callbacks in an isolated profile. They verify pause snapshots and
cancellation separately from browser touch routing and full-level playthroughs.
