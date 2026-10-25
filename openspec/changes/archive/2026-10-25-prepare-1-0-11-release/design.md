## Context

See proposal.md. Source through 73a32e48 has passed hosted checks and a hardware Web capture. The last public release is v1.0.10. Existing build, packaging, and Pages tools already implement the required release contract; no architectural change is needed.

## Goals / Non-Goals

Deliver fresh 1.0.11 binaries and matching Web publication. Preserve saves, build provenance, and existing outputs. No iOS build, store submission, new signing identity, or unrelated gameplay changes.

## Decisions

- Change only player version and Android version code in source configuration. Rebuild all five target players and external content with Unity 6000.6.7f1; do not relabel older binaries.
- Run Unity targets sequentially because they share the project/import state. Move existing target outputs to an ignored archive before building. Use existing packagers and validate checksums, package/level content, embedded versions, and native architectures.
- Verify macOS archive signing/round-trip and Android APK signature/alignment. Inspect the release Web player under the repository subpath and run the hardware frame gate on the exact release player.
- Archive source/build readiness before committing/tagging, following the established release workflow. Publish a draft only after all ten files (five players plus checksums) are attached. Publication, Pages completion, public release identity, and live gameplay are mandatory subsequent delivery checks; record their outcome in release notes and Actions links, without pretending they ran before the tag existed.

## Risks / Trade-offs

- A passing compile is not device gameplay evidence: disclose unavailable Windows/Linux/Android runtime coverage and keep current signing limits explicit.
- A stale Web deployment is a failed delivery: verify the public release.json and exercise the public player after the workflow succeeds.
- Target switching can serialize incidental editor settings: review the final diff and retain only intended release metadata and required build configuration.

## Migration Plan

Publish v1.0.11 from the verified source commit, then wait for its Pages deployment. Existing v1.0.10 assets remain available; a rollback can redeploy their verified Web archive with the existing manual workflow. No save migration is introduced.
