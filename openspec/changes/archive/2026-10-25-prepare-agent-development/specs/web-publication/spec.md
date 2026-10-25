## Purpose

Publish a verified Web release to GitHub Pages and load game content correctly from a repository subpath. See the [source map](/docs/architecture.md#web-publication) for implementation and validation boundaries.

## ADDED Requirements

### Requirement: Static hosting compatibility
The Web player SHALL use gzip with decompression fallback, single-threaded WebAssembly physics, and relative content paths so GitHub Pages does not require custom compression or cross-origin-isolation headers.

#### Scenario: Repository Pages URL
- **WHEN** the game is opened at a repository subpath such as /ballance/
- **THEN** its loader, Wasm, core packages, and level bundles resolve under that deployment

### Requirement: Verified release extraction
Pages preparation SHALL accept exactly one Web archive with a matching SHA-256 sidecar, require an empty destination, reject links/special files and unsafe extraction paths, enforce the uncompressed size limit, and validate required game content.

#### Scenario: Corrupt archive
- **WHEN** the archive checksum differs from its sidecar
- **THEN** publication fails before extraction or deployment

#### Scenario: Unsafe archive member
- **WHEN** an archive contains a link, special file, or a path escaping the destination
- **THEN** preparation rejects it

### Requirement: Release-triggered deployment
Publishing a release with the expected Web asset SHALL trigger the Pages workflow. A release without that asset SHALL leave the site unchanged. Manual dispatch SHALL allow redeploying an existing release tag.

#### Scenario: Publish Web release
- **WHEN** a release is published with its Web archive and checksum
- **THEN** the hosted workflow downloads, validates, and deploys those files without requiring a Unity license

#### Scenario: Redeploy older version
- **WHEN** the workflow is manually dispatched with a valid older release tag
- **THEN** that release's verified Web content becomes the deployed site

### Requirement: Deployment permissions and browser verification
Pages setup SHALL use the GitHub Actions source and permit the intended release tags in its deployment environment. Release verification SHALL include an HTTP-served menu and level gameplay check; opening index.html as a local file SHALL NOT count as browser verification.

#### Scenario: Release readiness
- **WHEN** a Web release is declared verified
- **THEN** its record identifies the served URL and actual menu, level-loading, and control checks performed
