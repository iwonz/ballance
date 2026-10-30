## Why

The mobile keypad intercepts tutorial buttons after the first Continue, leaving movement locked and the visible pause button inert. Players need a complete touch-only tutorial and a reversible menu at every stage.

## What Changes

- Give interactive UI priority over swipes and display only permitted controls.
- Make tutorial transitions single-owner, cancel stale work, and preserve tutorial state across user pause.
- Adapt tutorial cards and touch targets to safe portrait and landscape viewports; replace the large pause label with a compact menu.
- Preserve Ballance's existing visual identity, localization keys, layouts, saves and serialized assets.
- Verify real prefab raycasts and touch-only browser flows, then release 1.0.14 (Android code 6) for all five targets and deploy matching Pages.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `player-input`: UI touch priority and permission-aware control visibility.
- `gameplay`: exact pause restoration and single tutorial progression.
- `user-interface`: responsive touch tutorial and accessible compact game menu.

## Impact

MobileControls, keypad creation, TutorialController, GamePlayManager, tutorial UI, localized resources, editor integration checks, version settings and validation guides. Native IVP and native iOS are unchanged. Historical archives remain untouched; unrelated active work remains open.
