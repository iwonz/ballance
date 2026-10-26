## Checks

- Reballance remote main verified at 8bfec078132648bfaa4141ddaafa2d413a7078b6 before restoring classic main.
- `git diff original -- Assets Packages ProjectSettings BallancePhysics`: empty; classic runtime/content/identity/native files match f4acc137 exactly.
- `python3 scripts/test-release-tools.py`: 17 passed, including historical v1.0.11 and required new build identities.
- `python3 scripts/test-install-macos.py`: 20 passed (fixtures only).
- `python3 scripts/test-agent-context.py`: 5 passed.
- Localization: passed, 298 Russian translations across 12 resources.
- Physics ABI: passed, 89 delegates.
- Strict OpenSpec: 14 passed. Agent context and actionlint 1.7.12: passed.
- Actual Ballance 1.0.11 Web archive extracted and validated with its original SHA-256 sidecar.

## Results

Ballance runtime files are restored without edits from the maintained classic branch.
README has its own four-screen gallery, development/release instructions and Credits.
The upstream rights notice is retained verbatim. New release validation accepts only
Ballance; existing v1.0.8 through v1.0.11 remain compatible. Pages no longer depends
on Reballance. No historical release files or native saves were changed.

Independent local projects are /Users/iwonz/Desktop/Ballance and
/Users/iwonz/Desktop/Reballance. Remaster Library/build evidence moved to Reballance;
classic Library moved from its old worktree to Ballance. No mutable cache is shared.
The old worktree contains only tracked classic files and ignored Logs/__pycache__.

## Limitations

No new game binary/version or public Reballance release is claimed. Reballance's
unfinished gameplay acceptance remains in its own active change. This migration
changes source ownership and publication tooling, not classic runtime behavior.
Final remote refs and public identity are verified below. The same-version Pages redeploy subsequently succeeded; final evidence follows below.

### Remote and source verification

- iwonz/ballance main: 25b31108f3faeb61592e19e4436540af5e27e95c; hosted Build checks
  [40322686767](https://github.com/iwonz/ballance/actions/runs/40322686767) passed.
- iwonz/reballance main: 8bfec078132648bfaa4141ddaafa2d413a7078b6; hosted Build checks
  [40321995098](https://github.com/iwonz/reballance/actions/runs/40321995098) passed.
  These jobs do not certify gameplay.
- Both remote branch inventories contain only main. Original's final f4acc137 commit
  is preserved by the annotated archive/original-before-repository-split tag. The
  redundant remote/local branch and clean worktree were removed after verification;
  its diagnostic logs were moved to Builds/checks/original-worktree-logs.
- Existing v1.0.11 and original/v1.0.11 still peel to d066bfe5. The public release
  retains its original date and all ten archives/checksum files. Public release.json
  remains v1.0.11 / SHA-256 3557b247f90e6b3877a5c94ad409da016c669c1b9ca95bb1845208f203b15610.
- Restored classic files contain pre-existing trailing spaces. They are intentionally
  unchanged; the migration diff against f4acc137 is checked separately for new issues.

### Requirement coverage

Independent classic ownership and identity: byte comparison plus both remote refs.
Product naming and historical compatibility: release-tool round trips and rejection
fixtures. Release-triggered deployment: single-product workflow, explicit-tag rollback
and actual legacy archive extraction. Stable classic Web location: unchanged URL,
archive checksum and public release identity; no saved-profile migration is performed.
README, Credits and local cache separation match the design. No unresolved migration
implementation gap remains; unrelated Reballance acceptance stays outside this record.

The same-version deployment [40323185651](https://github.com/iwonz/ballance/actions/runs/40323185651)
was dispatched with v1.0.11. At this checkpoint GitHub reports queued with no jobs or
pending deployment approvals. It does not replace the already verified public site
until it succeeds. No new release delivery is claimed by this migration.

### Final delivery verification

- Same-version Pages [40323185651](https://github.com/iwonz/ballance/actions/runs/40323185651)
  completed successfully: prepare and deploy passed, including the public tag and
  SHA-256 check. A fresh HTTP read after deployment still reports the exact original
  v1.0.11 archive hash. No Reballance artifact participated in this deployment.
- OpenSpec archive synchronized the two capability deltas: 13 baseline specs passed
  strict validation. `check-agent-context.py --base 430e3143` passed for the complete
  migration. `git diff --check f4acc137` passed excluding the preserved user .DS_Store.
- The verbatim upstream notice appears in both READMEs and Credits; LICENSE bytes
  in both projects match the classic source. Classic runtime comparison is still empty.
- Installed applications remain Ballance.app 1.0.11 (com.MengyuGame.Ballance) and
  Reballance-dev.app 1.0.0-dev.11 (com.iwonz.BallanceRemaster). No installer was run.
- Reballance's final evidence commit is d61ea01c. Its subsequent CI failure is solely
  the completed-change-record gate for its still-active implementation; native/script
  checks pass. The full remaster change was deliberately not falsely archived.

No fresh public browser gameplay session or launcher screenshot was captured during
this repository move. Ballance's deployed player bytes and save directory are
unchanged; earlier runtime acceptance remains historical evidence. New Reballance
releases/Pages remain withheld pending the separately tracked gameplay acceptance.
