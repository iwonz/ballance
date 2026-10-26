## Checks

- Exact Unity 6000.6.7f1 PlayerBuild.Build, sequential target builds with graphics backend (no -nographics).
- Python release tools: 17 tests; installer: 20; context checker: 5 self-tests; Web performance checker: 11 self-tests. All passed. JavaScript/shell syntax checks passed.
- Localization: 298 Russian translations across 12 resources; managed/native ABI: 89 delegates. ARM64 physics smoke passed ABI 2302.
- Strict OpenSpec/context validation passed; publication/delivery checks remain pending.
- All five builds (macOS, Windows, Linux, Android and WebGL) completed with exit 0 and all 13 levels. macOS archive round-trip/signature/native smoke passed. Windows/Linux identities and archive level counts/checksums passed.
- Android apksigner v2 verification, zipalign -c -P 16 4, manifest identity/versionCode 4/version 1.0.12, ARM64-only libraries and 13 level assets passed. The packaged IVP differs bytewise due to stripping; comparison of all loadable segments (excluding ELF section-table header metadata) matches the source plugin. All IVP LOAD segments align to 16 KB.

## Results

Ballance 1.0.12 was installed through install-macos.py; the unpacked build copy was
removed after copy/signature verification. Application identity and existing profiles
were preserved. Native player smoke observed the existing unlocked levels, loaded
level 1, skipped the Russian tutorial with Q, moved with Right, fell and respawned
(lives 3 to 2), paused with Escape and returned to the main menu. The player closed
normally. Its log is retained under Builds/checks/release-1.0.12/macOS-player.log.

The verified Web archive was prepared with prepare-pages.py and served locally in Chrome.
Observed Russian menu, level 1 loading, Q tutorial skip, Right movement, fall and
respawn with two lives, Escape pause and return to the main menu. Mouse selection
and held keyboard input both worked. Web SHA-256: e8678bb8f47bd49fb28da88dd23df0a2691a14e8c361af67d09df0f24b7f7918.
Publication and deployed browser verification remain pending.

## Limitations

No physical Android, Windows/Linux runtime or reference-PC test was available.
No all-level completion, new FPS qualification or audible sound certification was
performed. CUA mouse/window capture was intermittent in fullscreen; held keyboard
input successfully exercised the native flow. Unity analytics endpoint connection
errors were observed separately from successful game/content loading.
