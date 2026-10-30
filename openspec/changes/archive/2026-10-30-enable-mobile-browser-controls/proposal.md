## Why

Ballance cannot be played from a phone browser because its touch controls are excluded from Web builds. Android also needs reliable multi-touch, interruption recovery, and optional calibrated tilt control.

## What Changes

- Enable existing configurable touch layouts on mobile Web and Android, with an analog joystick default for new profiles, camera buttons/swipe gestures and a reachable pause action.
- Reset touch state on release, cancellation, pause, focus loss, orientation changes and layout replacement; keep desktop bindings intact.
- Add opt-in tilt control in mobile settings, calibration, browser permission handling and touch fallback when sensors are unavailable.
- Build and package all five supported targets; publish a new version in this repository and deploy matching GitHub Pages.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `player-input`: Mobile Web controls, reliable touch lifecycle, camera gestures and optional tilt.
- `web-publication`: A responsive touch-safe host with bounded pixel density.

## Impact

Touch controllers, gameplay UI/settings, existing keypad assets and localization, Web template/sensor integration, editor checks, documentation and release version. Native IVP interfaces, saved bindings and edition/storage identity remain stable. No iOS release or unrelated remaster environment qualification is included.
