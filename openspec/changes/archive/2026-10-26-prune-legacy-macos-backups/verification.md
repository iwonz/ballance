## Checks

- Shared installer fix cherry-picked from main commit `5e818a1c` to Original
  commit `52d10c9c`, without importing Remaster runtime code.
- `python3 scripts/test-install-macos.py`: 20 tests passed in both worktrees.
- `python3 scripts/test-release-tools.py`: 18 tests passed in both worktrees.
- `python3 scripts/check-agent-context.py` and `git diff --check`: passed.
- Installer dry run identified only `Builds/checks/Ballance-1.0.8.app-backup`.
  Applying against the verified `/Applications/Ballance.app` 1.0.11 removed it.

## Results

Removed 102,348,863 bytes of obsolete application files. All 1,027 recorded file
hashes for the two installed players, their discovered saved-game files and
application preference files were unchanged. Evidence is retained in the main
worktree under `Builds/checks/legacy-backup-cleanup-{before,result}.json`.
Finder's Applications listing exposed one Ballance and one Reballance-dev.

## Limitations

No Unity/gameplay behavior changed or new binary was built for this installer fix.
The system Apps launcher returned a timeout, so its grid is not accepted as
visually verified. Reballance's independent full gameplay acceptance and release
remain outside this completed maintenance fix.
