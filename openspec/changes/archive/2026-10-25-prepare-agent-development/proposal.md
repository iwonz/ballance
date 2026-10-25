## Why

The Unity 6.6 game has no durable specification baseline or repository instructions for agents. Its mixed-language comments and outdated guides make it hard to find the right code, distinguish supported behavior from legacy features, and keep future changes consistent.

## What Changes

- Initialize OpenSpec 1.11.0 with the complete planning, implementation, verification, synchronization, and archive workflow.
- Describe the existing implementation as capability specifications backed by source paths and explicit verification limits. This is a baseline, not a claim that new gameplay is being implemented.
- Add concise repository and scoped agent instructions, a source map, and reproducible validation instructions.
- Translate maintained documentation, source comments, development annotations/diagnostics, editor labels, and authoring-template placeholders into English. Preserve localized game content, identifiers, serialized asset references, and original legal notices.
- Add CI checks for specification validity, context consistency, English maintenance text, and change records.

## Capabilities

### New Capabilities

- `agent-development`: Required OpenSpec lifecycle, language policy, context ownership, and checks.
- `runtime-lifecycle`: Startup, service registration, logical scenes, events, and shutdown.
- `package-management`: Package metadata, resources, dependencies, trust, built-in mods, and backend limits.
- `level-loading`: Built-in and custom level loading, native level format, preview, and legacy NMO limits.
- `gameplay`: Ball materials, sector progression, lives, score, pause, death, and completion.
- `physics-runtime`: Native IVP ABI, world/object ownership, simulation, collisions, and constraints.
- `player-input`: Configurable keys, camera input, Android adapter, and touch controls.
- `user-interface`: Menu/page lifecycle, dialogs, settings, HUD, and audio presentation.
- `localization`: Language resources, selection, fallbacks, placeholders, and fonts.
- `player-persistence`: Settings, high scores, package state, and writable paths.
- `developer-tools`: Debug console, editor packaging, level authoring, and diagnostic tools.
- `platform-builds`: Unity version, target configuration, content packaging, signing, and release archives.
- `web-publication`: Release-based GitHub Pages deployment and browser-compatible content delivery.

### Modified Capabilities

None. This repository has no previous OpenSpec specifications.

## Impact

The change affects repository instructions, documentation, comments across first-party C#/C++/shader sources, OpenSpec metadata and generated agent skills, validation scripts, and hosted CI. It must not alter gameplay, public identifiers, localization values, save formats, native binaries, or existing releases. iOS remains deferred. Existing unrelated defects found during exploration will be recorded with source evidence, not silently fixed or presented as intended requirements.
