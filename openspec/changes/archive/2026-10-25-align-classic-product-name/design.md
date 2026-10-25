## Approach

Apply the small shared naming changes from the main checkout to release identity, archive selection and corresponding tests. Keep the original branch independent and leave its game code, native library, identifiers and immutable v1.0.11 baseline intact. Historical tags retain their existing names.

## Validation

Run release-tool self-tests, strict OpenSpec validation and repository context checks. Review the diff to confirm it contains only naming, tooling and lifecycle records.
