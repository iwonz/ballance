## Checks

- Cherry-picked main commit 507fca6f as 1010208e, without a branch merge.
- `python3 scripts/test-install-macos.py`: 19 tests passed, including disabled-copy removal, copied-file/signature failure, edition isolation, downgrade rejection and running real-path detection.
- `python3 scripts/test-release-tools.py`: 18 tests passed, including development bundle and display names.
- `bash -n scripts/build.sh scripts/build-macos.sh`: passed.
- `python3 scripts/check-agent-context.py`: passed, 13 capabilities.
- `openspec validate maintain-local-macos-installations --strict --no-interactive`: passed before implementation.
- Unity 6000.6.7f1 batch import/compile exited 0 with no compiler errors; log `/tmp/ballance-original-installation-compile.log`.
- Reviewed the source change: no classic gameplay assets, native plugins, application ID or save format changed.

## Results

Both branches use the shared safe installation and development-name rules. On this machine, scoped filesystem and LaunchServices checks found only Ballance 1.0.11 and Reballance-dev 1.0.0-dev.9; disabled build bundles had been deleted after verified replacement. Both installed signatures passed, and all 295 Reballance saved-file hashes were unchanged after its update. The classic app was not rebuilt or replaced by this tooling-only change.

## Limitations

Native UI automation could not capture the launcher, so visible OS indexing was not certified. No new binary release, Pages publication or gameplay acceptance is claimed. The broader Remaster change remains active independently.
