## Context

The Original branch already uses the public title Ballance. Main has a tested generic installer; a focused cherry-pick keeps both branches independent while sharing this fix.

## Decisions

Import commit 507fca6f without merging branches. Select canonical app filenames from the validated edition/version; development filenames and display labels end in -dev. Verify source signature, copied-file hashes and target identity before replacement. Remove only matching obsolete bundles and stale LaunchServices entries, including disabled copies. Preserve newer builds, archives and profiles, and reject running copies or downgrades. The installer is an explicit post-packaging step, not a side effect of building on CI.

## Risks / Trade-offs

Cleanup deletes unpacked older players as explicitly requested; the verified archive remains available. macOS launcher presentation depends on OS indexing, so record catalog/filesystem checks separately from visible UI inspection. No gameplay or native-library migration is needed.
