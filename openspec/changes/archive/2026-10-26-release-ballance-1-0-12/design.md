## Approach

Use Unity 6000.6.7f1 and PlayerBuild.Build, the existing content-aware entry point.
Run one Unity build at a time to bound memory/disk pressure. Use the graphics backend
because the prior -nographics invocation crashed on shutdown; never count a failed
build exit as success. Package through existing validators without overwriting files.

Keep v1.0.11 immutable. Build v1.0.12 from current main with the independent original
identity and increment Android versionCode for upgrades. Preserve completed packaged
artifacts/checksums while retiring only identified obsolete generated outputs.

Verify script/localization/ABI/native checks, Unity compilation, all five identities,
archive content and checksums. Exercise local Mac/Web menu, level/control/recovery and
return where computer control is available. Record unavailable device checks honestly.
Prepare notes listing exactly what changed and what was untested. Attach assets to a
draft before publication. Wait for Pages and verify public identity and browser paths.
