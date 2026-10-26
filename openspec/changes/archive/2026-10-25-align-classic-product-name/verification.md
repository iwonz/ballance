## Checks

- `python3 scripts/test-release-tools.py`: 17 tests passed.
- `python3 scripts/check-agent-context.py`: passed for 13 capabilities.
- `openspec validate --all --strict --no-interactive`: 14 items passed.
- Reviewed the classic worktree diff: only README/release guide, product-name validators, archive selection, tests and OpenSpec records changed.

## Results

The classic public title is Ballance. New archive names use Ballance or Reballance while technical edition IDs and published v1.0.11 files remain unchanged. The original descriptor still uses Ballance / com.MengyuGame.Ballance.

## Limitations

No new Original player build or gameplay run was required for these text and shared-tooling changes; the editor validator changes only the accepted name for the other edition. No release was published.
