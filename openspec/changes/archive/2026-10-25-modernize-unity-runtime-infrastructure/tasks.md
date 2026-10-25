## 1. Native async and archive infrastructure

- [x] 1.1 Capture baseline source/build sizes, pin the Unity ZIP package, remove unused vendor assets after reference/GUID checks, and verify dependency resolution and compilation.
- [x] 1.2 Replace archive helpers/packer and readers with streamed writes, indexed CRC-checked bounded reads, and deterministic disposal; regression checks cover legacy names, corruption, limits, and round-trip content.
- [x] 1.3 Migrate async-operation awaits and startup task consumers, remove AsyncAwaitUtil, and verify startup failure/success semantics plus Web frame-yielding load behavior.

## 2. Pools and supported APIs

- [x] 2.1 Replace custom pool storage with UnityEngine.Pool and fix all UI callers; regression checks cover distinct instances, growth/capacity, repeated clear, duplicate/foreign return, inactive embedded templates, and disposal.
- [x] 2.2 Use ratio-based display settings and System.IO file helpers, update obsolete vendor API calls, and remove deprecated VS Code integration; verify fractional refresh formatting, legacy keys, file round trips, short headers, and a clean obsolete-API compilation check.

## 3. Integration and documentation

- [x] 3.1 Run editor regression checks and content-aware macOS, Web, Android, Windows, and Linux builds; inspect compilation logs and exercise Web menu, settings, package UI, level controls, and teardown.
- [x] 3.2 Update architecture/validation and dependency migration guidance; record source/size measurements and actual outcomes/limits in verification.md, then pass repository checks and strict OpenSpec validation.
- [x] 3.3 Review all changed callers/assets, requirement coverage, and the final diff; prepare verified deltas for synchronization and archival through the required finish workflow.
