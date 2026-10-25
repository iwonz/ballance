## Context

The existing workflow publishes validated release assets without Unity on the hosted runner, but conditionally skips releases lacking Web content. The existing Python preparation/tests already own archive safety. See proposal.md for the delivery requirement.

## Goals / Non-Goals

**Goals:** Mandatory, version-matched Pages rollout with observable live identity; verified 1.0.10 assets for the five current targets.

**Non-goals:** Store signing, iOS, gameplay changes, replacing the build system, or an additional release-management service.

## Decisions

1. Remove the workflow's skip condition instead of adding another publishing system. Missing assets already make `gh release download` fail, and the Python helper will continue to reject incomplete input.
2. Require the release tag as a preparation argument. Match a safe `v<version>` tag to the archive filename and the standard Unity template's productVersion. Add `release.json` only after extraction/content checks pass, including the verified archive digest.
3. After deploy-pages succeeds, fetch release.json with cache-busting and compare the requested tag, retrying for a bounded five minutes. A successful upload alone does not establish which version the public URL serves.
4. Preserve checksum, size, traversal, symlink, and complete-content checks. Extend the existing standard-library test file with missing-asset and version/identity cases; no new dependency is needed.
5. Build all five targets sequentially through the content-aware Unity entry point, moving old outputs into an ignored version-specific backup. Use version 1.0.10 and increment the Android version code. Publish a draft only after all five assets/checksums are attached and source checks pass.

## Risks / Trade-offs

- Pages and Releases are separate GitHub operations: publication is not atomic. Keep the release's delivery status pending until the workflow and public URL are verified; repair/retry a failed deployment before declaring success.
- A future custom Unity template may change the version field: fail closed and adapt its parser/tests in that template's change.
- Existing local builds must remain recoverable: move outputs aside instead of deleting or relabeling older binaries.
- Physical Android/Windows/Linux gameplay and macOS notarization remain outside this release's available evidence. Report those limits explicitly.

## Migration Plan

Validate implementation and built assets locally, record build readiness, synchronize/archive, commit and push. Create v1.0.10 on that source commit, attach all verified assets to a draft, publish, wait for Pages, and check live identity/menu/level behavior. Store hosted rollout evidence in the release notes and Actions run; the archived source record describes checks completed before publication. Manual dispatch can redeploy a previously verified Web release if rollback is explicitly needed.
