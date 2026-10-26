## Context

See proposal.md. The discovered bundle is a complete Ballance 1.0.8 application
under the ignored build tree, while the installed classic application is 1.0.11.

## Decisions

Extend the existing retired-suffix check by one exact alternative. Reuse the
validated replacement and matching-identity cleanup path; do not add a general
backup-directory remover. Transfer the tested shared code with cherry-pick.

## Risks and checks

Deletion is restricted by executable metadata, bundle identifier, version and
running-process checks. Test recognized and unrelated backups, normal discovery
exclusion and removal after validation. Run installer/release tests, strict
OpenSpec and repository context checks. No Unity rebuild is needed for this
Python-only maintenance fix; no release is implied.
