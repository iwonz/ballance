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
Hosted CI, final remote refs and public identity still need final verification below.
