## Context

Original starts at d066bfe5 and retains the classic player. Main owns Remaster and the common static site. Separate worktrees own separate Unity caches and outputs.

## Decisions

Use an explicit original descriptor with Ballance / com.MengyuGame.Ballance. The shared builder emits build-identity.json; packaging rejects relabelled builds. Historical releases still use legacy filenames. New releases use original/v<version> and Ballance-Original names.

The shared Pages workflow always checks out main and pins that commit for both preparation and verification. Main owns the pair manifest; a release must already be pinned before publication. Both assets and the combined site size are validated. This branch does not own a second site manifest.

Transfer the shared tooling by one focused cherry-pick, then keep branch-specific documentation and descriptor in a separate commit. Never merge the branches wholesale. Do not change Original product settings, packages, assets, gameplay or physics binaries.

## Validation

Run shared release tests, strict OpenSpec/context checks, workflow syntax, Unity editor compilation and a diff audit excluding runtime changes. No new release or public deployment is part of this maintenance preparation; public dual-player acceptance belongs to the Remaster change on main.
