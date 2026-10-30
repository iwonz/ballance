## 1. Mobile input

- [x] 1.1 Enable mobile Web settings/keypads and default joystick; verify runtime detection, touch scrolling/selection and saved layout compatibility.
- [x] 1.2 Fix pointer ownership/release, add camera swipes, pause and safe-area handling; verify virtual pointers and browser touch/lifecycle flows.
- [x] 1.3 Add calibrated opt-in tilt, localized status/recenter and touch override; verify mapping, dead zone, denial, stale data and cleanup tests.

## 2. Web presentation

- [x] 2.1 Make the host responsive and touch-safe with bounded mobile DPR; verify portrait/landscape/fullscreen dimensions and browser interaction.

## 3. Validation and release preparation

- [x] 3.1 Update architecture/validation guidance and verification.md with actual results and physical-device limitations; run strict specs, context, localization and relevant editor/script checks.
- [x] 3.2 Build and package Web, Android, macOS, Windows and Linux for the new version; verify archive identities/checksums and available Web gameplay and native startup/physics smoke, recording unavailable device/runtime checks.
- [x] 3.3 Review the diff, retained GUIDs and requirement coverage; confirm the implementation and verification record are ready for spec synchronization and archive.

After all tasks pass, synchronize and archive with the CLI, then run strict delivery/context checks. Publication follows archive: commit/push source, create an immutable annotated version tag, upload and inspect all five platform assets/checksums plus legal notices, publish, wait for matching Pages deployment, then verify public identity and gameplay. Record final public URLs and any unverified hardware in the delivery report.
