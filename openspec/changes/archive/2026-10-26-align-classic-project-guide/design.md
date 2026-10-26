## Context

The original branch owns classic Ballance maintenance. Main owns Reballance and the shared site/release workflow. Copying the whole main branch would violate this boundary.

## Decisions

Make a narrow documentation commit on original. Keep local classic build/development guides, link the shared project gallery and release procedure on main, and retain the upstream notice verbatim. Do not import Reballance code, assets or caches.

## Validation

Check relative links, notice preservation and the absence of runtime changes. Run strict OpenSpec and agent-context checks, record verification, archive this documentation change and push the branch.
