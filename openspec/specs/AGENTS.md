# Capability specification maintenance

Follow the root instructions and [OpenSpec workflow](../README.md).

- One capability per directory; preserve existing boundaries unless a change justifies a split.
- Specify observable behavior with stable requirement names, SHALL/MUST, and `#### Scenario:` WHEN/THEN cases. Include meaningful failure behavior.
- Keep implementation inventories and commands in the source map and operating guides; link from Purpose.
- MODIFIED deltas contain the complete requirement and all retained scenarios. Use REMOVED/RENAMED explicitly and preserve unrelated requirements.
- A no-behavior-change task uses `skip_specs: true` in its change metadata with a rationale. It still needs design, tasks, and verification.
- Current specs must not contain proposals, TODO placeholders, or execution logs. Archived changes preserve historical evidence.
- The bootstrap baseline documents code-inspected behavior. It does not prove full level playthroughs or all-device support.
- After synchronization, run strict OpenSpec validation and the repository context checker.
