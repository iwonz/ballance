## 1. Release preparation

- [x] 1.1 Set version 1.0.11 and Android version code 3, preserving old outputs and verifying the source diff.
- [x] 1.2 Rebuild all five supported players and content with Unity 6000.6.7f1; require successful build logs and complete target outputs.
- [x] 1.3 Package five versioned assets and checksums; verify embedded versions, content, architectures, macOS signing/round-trip, and Android signature/alignment.

## 2. Readiness verification

- [x] 2.1 Prepare and serve the exact Web release under the repository subpath; verify menu, graphics information, level loading, controls, and the hardware frame budget.
- [x] 2.2 Record checks and limits, prepare accurate release notes, review unchanged behavioral specs, pass strict/context validation, and archive source readiness before tagging.

After the source/readiness commit, mandatory delivery continues with draft asset
upload, publication, successful Pages deployment, public release identity, and live
browser verification. Record these external outcomes in the release notes and
linked Actions runs before reporting completion.
