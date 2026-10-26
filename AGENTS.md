# Agent instructions

Read this file first. Follow more specific `AGENTS.md` files in directories you edit.

## Start with context

1. Inspect `git status` and preserve work that is not yours.
2. Read [the OpenSpec workflow](openspec/README.md); inspect active changes with `openspec list`.
3. Use [the source map](docs/architecture.md) to find the relevant capability specs and implementation. Read callers, assets/configuration, and checks before editing.
4. Use [validation guidance](docs/validation.md) to select checks for the actual change.

Do not read generated `Library`, `Temp`, `Builds`, or logs as architecture documentation. Use them only to investigate a specific build or runtime result. Prefer `rg` for searches. Use RTK as a shell-command prefix when available in the working environment; it is not a repository build dependency.

## Required development process

Every repository change, including fixes, refactors, documentation, dependencies, and CI, uses the **full OpenSpec lifecycle** described in `openspec/README.md`. Read-only exploration does not require a change. Reuse a matching active change; do not create competing proposals for the same work.

Complete proposal, relevant delta specs, design, and tasks before implementation. Record verification, synchronize specs, and archive only when the work is actually complete. Never mark a task complete for a command that was not run. Do not claim that hosted CI tests Unity gameplay. Existing user authorization remains valid; OpenSpec is not an additional permission ceremony.

When a request changes scope, update the same change's artifacts before continuing. Leave an incomplete change active with clear remaining tasks and evidence. Do not archive merely to make CI pass.

## Language and context ownership

- Write all maintained documentation, OpenSpec artifacts, code comments, region labels, commit/PR descriptions, and developer-facing help in **English**. Chat replies may use the user's language.
- Preserve localized game resources, player-entered text, proper names/asset paths, and original third-party legal notices. English maintenance text does not mean removing Russian or Chinese gameplay localization.
- `openspec/specs/` owns behavioral requirements. `docs/architecture.md` owns navigation and implementation boundaries. Other `docs/` files own operating procedures. Archived changes own historical decisions and verification evidence.
- Update affected specs and documentation in the same change as code. Link to the owner instead of copying lists, instructions, or status claims. Do not create a second `openspec/docs` documentation tree.
- Put future ideas and discovered defects in a clearly scoped change or the source map's known gaps; never describe them as completed capabilities.

## Unity and native code invariants

- Use the exact editor in `ProjectSettings/ProjectVersion.txt`; build through the documented content-aware entry point.
- Preserve `.meta` files and GUIDs when moving assets. Never rename serialized fields, class/namespace names, prefab object names, JSON/XML keys, package IDs, or event strings without auditing their consumers and planning migration.
- Keep editor-only APIs behind editor boundaries. Check Mono and IL2CPP conditional code when changing shared runtime APIs.
- Ball gameplay uses native IVP through `Assets/BallancePhysics`, not Unity Rigidbody physics. Unity colliders still serve trigger and presentation paths.
- Pair native creation with cleanup; keep C/C# calling conventions, struct layouts, static AOT callbacks, and delegate lifetimes consistent. Follow [native instructions](BallancePhysics/AGENTS.md).
- `Assets/Plugins` mostly contains third-party code. Keep vendor changes narrow, retain licenses, and explain local integration changes. `AndroidInput.cs` is project-owned.
- Run local macOS players through `scripts/install-macos.py`; use the canonical latest Original and Remaster applications. Do not register additional diagnostic app copies. See the build guide for installation and cleanup.
- Do not commit generated players, caches, logs, credentials, signing keys, or machine-specific configuration. Do not rewrite native binaries for a documentation change.
- iOS work is deferred. Windows/Linux are x86-64, macOS and Android release targets are ARM64; check the build guide before changing platform support.

## Finish a change

Run the relevant checks, review the diff for unintended behavior or asset churn, and update the verification record with outcomes and limits. Report what changed, why, what was checked, and anything still unverified. Commit/push/publish when the user requests it; preparing repository context does not require a new binary release.

New public releases are unified Ballance and Reballance distributions from main. This branch supplies verified classic artifacts and source tags to that process; do not publish separate edition releases. The unified release must include both games and the complete Pages site with checksums, then deploy that exact site. Attach verified assets before publishing, wait for the Pages workflow, and verify the public release identity and browser behavior before reporting delivery complete. A missing Web asset or failed Pages deployment is a failed release delivery, not an optional follow-up. Follow [the release guide](docs/build-and-release.md#github-pages).
