## Why

The Unity 6.6 player still carries a legacy async scheduler, a manually vendored ZIP DLL, hand-written pooling/storage helpers, and obsolete display APIs. The pool implementations also allow duplicate returns or fail to grow beyond prewarming, while ZIP packing copies whole files into temporary buffers.

## What Changes

- Use Unity's built-in async-operation await support and frame scheduling; remove AsyncAwaitUtil after migrating its coroutine bridge callers.
- Replace the vendored SharpZipLib DLL with the pinned Unity Registry package, simplify archive I/O, preserve CRC checking and legacy package names, and bound decompression memory.
- Delegate pool storage/lifecycle to UnityEngine.Pool, fixing ownership, duplicate return, bounded growth, and teardown while preserving serialized UI components.
- Replace obsolete integer refresh-rate APIs and manual whole-file I/O with supported Unity/.NET APIs.
- Update the vendored editor/debug tools' obsolete Unity API calls narrowly and remove the unsupported VS Code integration package; retain the installed Visual Studio integration and vendor notices.
- Add focused regression checks, rebuild representative scripting backends, update the source map, and record measured code/build differences without inventing performance claims.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `package-management`: Compatible, CRC-checked, bounded archive loading and owned-resource disposal.
- `user-interface`: Safe pooled list reuse and fractional-refresh-rate display settings.

## Impact

Package readers/packer, Unity dependency manifest/lock, startup task consumers, object/scroll pools and their UI callers, display settings, file helpers, editor checks, and documentation. Preserve public package Task contracts, serialized fields/GUIDs, game identifiers, localization, save formats, and native physics. External source mods using removed vendor-specific helper APIs must migrate to the documented Unity APIs; compiled built-in mods are rebuilt with the player. No rendering-pipeline, physics-engine, UI framework, save-format, or input-binding rewrite; iOS remains deferred.
