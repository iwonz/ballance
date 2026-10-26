## MODIFIED Requirements

### Requirement: Release-triggered deployment
Every published Ballance release in iwonz/ballance SHALL trigger mandatory Pages preparation and deployment of its matching Web archive and checksum. A missing Web asset SHALL fail the workflow rather than silently skip publication. Release delivery SHALL remain incomplete until its Pages workflow succeeds. Manual dispatch SHALL allow redeploying an existing release tag. Only Ballance assets SHALL be required; Reballance releases and Pages SHALL be independent.

#### Scenario: Publish Web release
- **WHEN** a release is published with its Web archive and checksum
- **THEN** the hosted workflow downloads, validates, and deploys those files without requiring a Unity license

#### Scenario: Missing Web asset
- **WHEN** a release is published without its required Web archive or checksum
- **THEN** the Pages workflow fails visibly and that release is not reported as completely delivered

#### Scenario: Redeploy older version
- **WHEN** the workflow is manually dispatched with a valid older release tag
- **THEN** that release's verified Web content becomes the deployed site

## ADDED Requirements

### Requirement: Stable classic Web location
Ballance SHALL remain hosted at /ballance/ with its player page in the existing root
directory, preserving the URL-derived Web save location. Reballance SHALL NOT be
bundled into this site. Existing published archive bytes SHALL remain unchanged.

#### Scenario: Return after repository separation
- **WHEN** a returning player opens the existing Ballance URL
- **THEN** the same classic release and Web data directory remain available without loading Reballance
