## Context

main currently contains Reballance; original f4acc137 contains maintained Ballance
1.0.11. Both were pushed. The new public iwonz/reballance repository is empty.

## Decisions

Clone the complete Reballance main history independently, update its ownership and
push/verify it first. Restore tracked classic files on old main from f4acc137 in a
new commit, preserving uncommitted .DS_Store work. Keep the previous original tip
under archive/original-before-repository-split before deleting the redundant remote
branch. Existing v1.0.11 and original/v1.0.11 remain d066bfe5; no force-push.

Each repository owns main, v<version> releases and Pages. Remove paired-site tooling
from classic Ballance. Use the existing single-archive validator and deployment
workflow, with product validation for new versions and compatibility for historical
archives without build-identity.json. Preserve safe extraction, size, checksums and
no-overwrite behavior. Ballance stays at /ballance/ and keeps its native identity.

Restore classic code/assets/plugins exactly; retain screenshot/credits improvements
but present only Ballance in its README. Link Reballance as a separate project.
Move Reballance's caches and build evidence to its own clone; use the old classic
worktree cache for Ballance. Do not touch installed applications or user profiles.

## Validation

Compare Assets, Packages, ProjectSettings and BallancePhysics against f4acc137;
verify identity and tag SHAs. Run release tests, localization, ABI, context/OpenSpec,
workflow syntax and actual legacy Web extraction. Verify both remotes, original
branch archival and unchanged public release identity. No new gameplay/performance
claim is warranted for a repository/publication-only migration.
