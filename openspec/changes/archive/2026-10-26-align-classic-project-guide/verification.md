## Checks

- `openspec validate --all --strict --no-interactive`: 14 items passed.
- `python3 scripts/check-agent-context.py`: passed.
- Relative Markdown link check for README, AGENTS and the changed release/validation guides: passed.
- Compared the exact upstream Cyparade notice with HEAD: unchanged.
- `git diff --name-only`: only documentation files changed; no runtime, assets, native libraries or build outputs.

## Results

Both tasks are complete. The classic README identifies original as Ballance and main as Reballance/shared publication, links the upstream fork first in Credits, shows actual version-labelled classic captures from the shared gallery and preserves the rights notice. Release instructions direct maintainers to a complete unified distribution on main rather than an independent classic release. No behavioral spec delta is needed for this documentation-only change.

## Limitations

No players were rebuilt or released for this documentation change. Main's full Reballance acceptance remains unfinished and its publication barrier is unchanged. External gallery links become available when the corresponding authorized main commit is pushed; no generated screenshot is presented as gameplay.
