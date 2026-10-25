## Context

Ballance is a Unity 6000.6.7f1 project with a C# service/package framework, external AssetBundles, a native IVP physics bridge, and platform-specific packaging. Runtime code and tooling contain substantial inherited Chinese comments; the main release guide is in Russian. The baseline is commit `52a1835f` after release 1.0.9.

## Goals / Non-Goals

**Goals:** Give agents a small, navigable, source-backed context; require the full OpenSpec lifecycle for future repository edits; establish English maintenance text; verify the rules in CI.

**Non-goals:** Gameplay changes, renaming serialized C# types or assets, translating player language resources, modifying third-party legal notices, upgrading Unity or native libraries, enabling iOS, or republishing unchanged binaries.

## Decisions

1. Use the installed OpenSpec 1.11.0 CLI and its `spec-driven` schema. Keep repository-local skills and a reproducible setup command; do not change the user's global OpenSpec profile.
2. Split specifications by observable capability rather than by source file. Each specification links its implementation and validation evidence. State platform limits and known gaps separately from requirements.
3. Keep behavioral truth in `openspec/specs`, architecture/navigation in `docs`, process rules in `AGENTS.md` and the OpenSpec guide, and historical evidence in archived changes. Cross-link instead of copying these facts.
4. Require proposal, delta specs (or an explicit no-behavior-change marker), design, tasks, implementation, verification, synchronization, and archive. Read-only exploration needs no change. Existing user authorization is sufficient; the workflow does not introduce a separate approval ritual.
5. Translate comments in place using lexical comment boundaries. Translate region labels and developer-facing annotations, console help, diagnostics, and editor labels through a separately reviewed literal map. Compare source outside these text boundaries, allowing trailing whitespace cleanup on edited lines. Do not rename code symbols, data keys, event names, paths, or localized player text to make a language check pass.
6. Add small standard-library checks and self-tests. Use OpenSpec's own parser for spec validation. CI can check artifacts and obvious non-English maintenance text; semantic consistency and translation quality still require review.

## Risks / Trade-offs

- A broad comment translation can damage syntax or erase technical meaning. Preserve comment structure and compare non-comment source against the baseline; review technical descriptions and run existing checks and a Unity compile.
- Historical comments can disagree with implementation. Use the implementation as evidence and record defects as gaps rather than turning them into promised behavior.
- Agent files can become redundant. Keep one owner for each fact and validate local links; instructions require updates within the same change when behavior or paths change.
- A procedural CI gate cannot prove every requirement is implemented. Verification records must distinguish executable checks, manual inspection, and untested platform behavior.

## Migration Plan

Bootstrap this change through the same lifecycle it introduces. Audit and map the implementation; establish the baseline delta specs; translate maintenance text; install agent instructions and checks; run validation; record evidence; let OpenSpec merge the baseline and archive this change. Existing release assets and Pages are unaffected.

## Open Questions

None requiring user input. iOS stays deferred, and all repository maintenance prose is English. Chat replies may follow the user's language.
