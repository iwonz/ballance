## Why

The verified v1.0.11 recreation needs a stable Original edition while a separately
maintained Remaster introduces modern presentation and meaningful weather. A single
player, save namespace, and Pages deployment currently couple these products.

## What Changes

- Preserve d066bfe5 as original/v1.0.11 and maintain an original branch; main owns
  Remaster and the shared site. Transfer fixes selectively, without branch merges.
- Keep independent edition versions, application identities, saves and build trees; publish both games together in one versioned distribution release.
- Modernize Remaster materials, skies, sound, all menu/HUD/tutorial flows and scalable
  rendering while preserving level topology and the calibrated dry gameplay baseline.
- Replace Remaster startup/About promotional branding with its own generated cloud-reveal title artwork; redesign records and every remaining menu control while preserving source license notices.
- Add seven environment presets, authored profiles for all thirteen built-in levels,
  gradual runtime switching, shared visual/contact state, checkpoint restoration and
  environment-independent records. Extend native IVP rather than substituting Unity physics.
- Publish a bilingual static landing and both verified Web players atomically from
  pinned source tags and platform checksums, preserving Original browser storage and independent rollback. Include the exact two-player site archive in the same release.
- Rewrite README with branch ownership, run/development instructions, real screenshot galleries, upstream-first Credits and the unchanged upstream rights notice for both editions.
- Validate all five existing release platforms, the 91 level/preset combinations,
  frame-rate independence and actual hardware performance before claiming completion.

## Capabilities

### New Capabilities
- `game-editions`: Immutable Original baseline and independent maintenance/release identities.
- `remaster-environment`: Authored environment presets, surface state, weather physics and presentation.

### Modified Capabilities
- `user-interface`: Modern responsive Remaster UI, tutorials and resolution/quality controls.
- `player-persistence`: Edition isolation and shared Reballance records with preservation of earlier development scores.
- `physics-runtime`: Dynamic per-contact surface profiles and reproducible native builds.
- `platform-builds`: Edition-aware identity and release asset validation.
- `web-publication`: Bilingual landing and atomic publication of two pinned players.

## Impact

Touches gameplay/level lifecycle, IVP C++ and managed ABI, material/shader and prefab
assets, settings/localization, high scores, player building/packaging, scripts and CI,
README and operating guides. Unity stays at 6000.6.7f1 with Built-in rendering; no paid
dependency or mandatory new rendering package. Existing GUIDs, serialized identifiers,
package IDs, legacy release files and user work remain intact. Original presentation
is unchanged. iOS, new abilities, deep deformable terrain, and automatic cross-edition
save import are outside this change. Hardware and all-level acceptance are required
work, not inferred from a successful build or historical tests.
