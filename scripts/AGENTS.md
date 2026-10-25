# Build and validation scripts

Follow the root instructions and [validation guide](../docs/validation.md).

- Prefer the Python standard library and existing helpers. Do not add a package manager to the Unity project for small scripts.
- Validate paths, archive members, checksums, expected content, and command exit status at input boundaries. Preserve no-overwrite behavior for existing outputs.
- Keep build, packaging, validation, and publication responsibilities separate. A successful build is not a gameplay test or a store submission.
- Add a small self-test for new nontrivial logic, including its failure cases. Run the affected tests and shell syntax checks.
- Keep local commands and CI steps consistent. Tool-version changes must update the setup script, workflow, and OpenSpec guide together.
- Build logs and temporary outputs belong in ignored paths. Use `docs/build-and-release.md` for operator instructions rather than duplicating them in comments.
