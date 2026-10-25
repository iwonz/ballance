# web-publication Specification

## Purpose

Publish a verified Web release to GitHub Pages and load game content correctly from a repository subpath. See the [source map](/docs/architecture.md#web-publication) for implementation and validation boundaries.

## Requirements

### Requirement: Static hosting compatibility
The Web player SHALL use gzip with decompression fallback, single-threaded WebAssembly physics, and relative content paths so GitHub Pages does not require custom compression or cross-origin-isolation headers.

#### Scenario: Repository Pages URL
- **WHEN** the game is opened at a repository subpath such as /ballance/
- **THEN** its loader, Wasm, core packages, and level bundles resolve under that deployment

### Requirement: Verified release extraction
Pages preparation SHALL accept exactly one Web archive with a matching SHA-256 sidecar, require an empty destination, reject links/special files and unsafe extraction paths, enforce the uncompressed size limit, and validate required game content. Its archive filename and embedded Web player version SHALL match the requested release tag. Successful preparation SHALL write a public release identity containing the tag, version, archive name, and archive checksum.

#### Scenario: Corrupt archive
- **WHEN** the archive checksum differs from its sidecar
- **THEN** publication fails before extraction or deployment

#### Scenario: Unsafe archive member
- **WHEN** an archive contains a link, special file, or a path escaping the destination
- **THEN** preparation rejects it

#### Scenario: Wrong player version
- **WHEN** the requested release tag differs from the archive name or embedded Web player version
- **THEN** preparation fails instead of deploying that player under the requested identity

### Requirement: Release-triggered deployment
Every published release SHALL trigger mandatory preparation of the shared site from the two explicitly pinned Web archive/checksum entries in main. The triggering release SHALL match a pin. Missing, invalid or unready entries SHALL fail publication. Manual dispatch SHALL rebuild the current pair, allowing rollback by restoring only one manifest entry. Deployment SHALL be serialized and the combined uncompressed site SHALL not exceed 1,000,000,000 bytes.

#### Scenario: Publish Web release
- **WHEN** an Original release has verified assets and its pin is updated in main
- **THEN** publication deploys that Original and the unchanged pinned Remaster from the same main checkout

#### Scenario: Missing Web asset
- **WHEN** either pinned release lacks its required Web archive or checksum
- **THEN** preparation fails before a partial site is deployed

#### Scenario: Redeploy older version
- **WHEN** one pin is restored to a verified older release and Pages is manually dispatched
- **THEN** the other edition retains its pinned version and checksum

### Requirement: Deployment permissions and browser verification
Pages setup SHALL use the GitHub Actions source and permit the intended release tags. Deployment SHALL pin the main source commit used for preparation and use it again for verification. The workflow SHALL verify both public release identities within a bounded wait. Release acceptance SHALL include HTTP-served menu, loading and gameplay checks for both editions.

#### Scenario: Release readiness
- **WHEN** a release is declared delivered
- **THEN** evidence identifies the served URL, both identities and actual menu, level-loading and control checks

#### Scenario: Stale deployed site
- **WHEN** either public edition does not match its manifest identity
- **THEN** verification fails instead of reporting success
