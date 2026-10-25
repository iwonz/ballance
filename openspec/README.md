# OpenSpec workflow

All repository development follows this workflow, including bug fixes, documentation,
CI, dependencies, and refactoring. Read-only investigation is the only activity that
does not need a change record. The [root instructions](../AGENTS.md) govern agents.

## Tooling

Use **OpenSpec 1.11.0**, Node **20.19.0+**, and the built-in `spec-driven` schema.
Install the CLI if necessary, then regenerate the checked-in shared skills:

```sh
npm install -g @fission-ai/openspec@1.11.0
bash scripts/setup-openspec.sh
```

Setup uses a temporary custom profile and preserves global preferences and the
project configuration. It installs ten skills under `.agents/skills`, including
verify and sync. Reload the agent session after regenerating skills if necessary.
Do not run a bare `openspec update` with an unrelated global profile: it can remove
workflow skills required here. Do not create `openspec/AGENTS.md`; OpenSpec treats
that legacy location as removable during initialization. Spec-specific rules live
in [specs/AGENTS.md](specs/AGENTS.md).

Skills are conveniences; the lifecycle also works through the CLI below. This
repository requires a concise design and verification record even where the
upstream default workflow makes them optional. It does not add a separate approval
step for work the user has already authorized.

## Complete lifecycle

1. **Explore.** Read the relevant [source map](../docs/architecture.md), current specs,
   callers, data/assets, and tests. Run `openspec list` and reuse a matching active
   change. Establish scope and observable acceptance conditions.
2. **Propose.** Run `openspec new change <verb-noun-name>`. Use
   `openspec instructions proposal --change <name>` to write `proposal.md` with
   motivation, capabilities, impact, and explicit non-goals where useful.
3. **Specify and design.** Use `openspec instructions specs --change <name>` and
   `openspec instructions design --change <name>`. Write full delta requirements
   and scenarios for affected capabilities, then `design.md` with the chosen
   approach, compatibility concerns, and checks. Keep the design short for a small
   edit. If no behavior changes, set `skip_specs: true` in the change's
   `.openspec.yaml` and explain why in the proposal; do not manufacture a delta.
4. **Plan tasks.** Use `openspec instructions tasks --change <name>`. Write checkboxes
   with concrete acceptance checks, including documentation and verification work.
   Run `openspec validate <name> --strict --no-interactive` before implementation.
5. **Apply.** Run `openspec instructions apply --change <name> --json`, read its
   context files, and implement pending tasks. Mark only finished tasks complete.
   Update the artifacts when scope or evidence changes; do not leave an abandoned
   approach described as the final design.
6. **Verify.** Use the `openspec-verify-change` skill and the
   [validation guide](../docs/validation.md). Write `verification.md` with `## Checks`,
   `## Results`, and `## Limitations`. Include commands and outcomes, requirement
   coverage, manual observations, untested behavior, and unresolved issues.
   Resolve discrepancies before declaring completion.
7. **Synchronize and archive.** Review the final delta against the current specs.
   With all tasks complete and evidence recorded, run
   `openspec archive <name> --yes`. This CLI command validates, merges deltas, and
   archives the change. For a no-behavior-change record, its explicit `skip_specs`
   metadata is sufficient. Do not use `--no-validate` or archive unfinished work.
   If the sync skill already applied the deltas, verify equality and use
   `--skip-specs` only to avoid reapplying that verified merge.
8. **Check the final delivery.** Run strict validation and context checks again,
   review the diff, and deliver the implementation together with its archived
   record. Commit/push/publish according to the user's request.

The generated `propose` or `ff` skill can prepare all planning artifacts in one
pass; it does not remove the verify/sync/archive requirements. An interruption
leaves the change active with clear unchecked tasks and current evidence. A later
agent resumes that change instead of starting from chat history.

## Files and ownership

| Location | Owns |
| --- | --- |
| `specs/<capability>/spec.md` | Current behavior, requirement names, and scenarios |
| `changes/<name>/` | Proposed delta and work in progress |
| `changes/archive/<date>-<name>/` | Completed intent, design, tasks, delta, and verification |
| `config.yaml` | Short project context and artifact/operation guidance |
| `../docs/architecture.md` | Source navigation, integration boundaries, known gaps |
| `../docs/validation.md` | Check selection and executable commands |

Keep requirements about behavior, not method inventories. Add capabilities only
when they have a distinct responsibility; extend an existing capability otherwise.
Never copy operating guides into specs or create a parallel `openspec/docs` tree.
Archived records are historical; update current specs/guides rather than rewriting
old outcomes when behavior changes.

## Enforcement and limits

```sh
openspec validate --all --strict --no-interactive
python3 scripts/check-agent-context.py
python3 scripts/test-agent-context.py
# Check a completed delivery against its base commit:
python3 scripts/check-agent-context.py --base <base-commit>
```

The hosted context job checks links, current spec coverage, workflow files,
maintenance language, and archived artifact completeness. Its delivery check
requires the diff to include a completed change record for repository edits.
An in-progress branch can therefore have a failing delivery gate until verification
and archive are complete; do not falsify completion to satisfy it.

These checks cannot prove English fluency, semantic consistency, or gameplay
correctness. Review requirement coverage, changed callers, and runtime evidence.
Original license text, localized game values, and stable asset names are not
maintenance prose. Keep any new checker exemption narrow and explain it in the
same OpenSpec change.

Upstream references: [workflow documentation](https://github.com/Fission-AI/OpenSpec/blob/main/docs/workflows.md),
[CLI documentation](https://github.com/Fission-AI/OpenSpec/blob/main/docs/cli.md).
Use the pinned CLI's `--help` and artifact instructions when upstream documentation
describes a later version.
