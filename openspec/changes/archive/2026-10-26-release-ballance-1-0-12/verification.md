## Checks

- Exact Unity 6000.6.7f1 PlayerBuild.Build, sequential target builds with graphics backend (no -nographics).
- Python release tools: 17 tests; installer: 20; context checker: 5 self-tests; Web performance checker: 11 self-tests. All passed. JavaScript/shell syntax checks passed.
- Localization: 298 Russian translations across 12 resources; managed/native ABI: 89 delegates. ARM64 physics smoke passed ABI 2302.
- Final archive validation passed: 13 strict capability specs, context delivery check against e66c535d, and git diff --check. The release record has no behavioral delta to synchronize.
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
Release v1.0.12 was published from source commit 2fccfb14. All 12 GitHub assets
(five players, five SHA-256 sidecars, LICENSE and CREDITS.md) matched local sizes
and API SHA-256 digests before publication. Pages run 40338128701 completed
successfully; public release.json reports v1.0.12 and the expected Web checksum.
All eight public player/identity/Build files were fetched and byte-hash matched
against the prepared release archive. After intermittent CUA window/focus failures,
the public Chrome smoke completed: Russian menu and level selection, level 1,
tutorial dismissal, held Right movement, fall and respawn (three lives to two),
Escape pause, exit confirmation and visible return to the main menu.

The first cold public load entered the menu before core.assets.skys was ready,
showing a plain blue background. The console reported that missing package;
reloading restored the sky, and subsequent gameplay and menu return succeeded.
This unresolved startup ordering issue is recorded in the source map and release
notes. FMOD Web warnings and blocked Unity analytics requests were also observed;
the run is not a claim of a warning-free browser console or certified audio.

A filesystem audit of /Applications, user Applications, Desktop and Downloads
found exactly the two current matching bundles: Ballance 1.0.12 and Reballance 1.0.0.
No obsolete development or .app.disabled bundle remained. Finder's Applications
list included both canonical apps. The separate system Apps search view has not
been visually verified. Profiles and release archives were preserved.

## Limitations

No physical Android, Windows/Linux runtime or reference-PC test was available.
No all-level completion, new FPS qualification or audible sound certification was
performed. CUA mouse/window capture was intermittent in fullscreen; held keyboard
input successfully exercised the native flow. Unity analytics endpoint connection
errors were observed separately from successful game/content loading.
