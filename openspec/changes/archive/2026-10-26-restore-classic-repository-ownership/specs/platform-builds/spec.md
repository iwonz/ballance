## ADDED Requirements

### Requirement: Independent classic repository
The iwonz/ballance main branch SHALL own only the classic Ballance implementation,
documentation, build tools and releases. Reballance SHALL be independently maintained
in iwonz/reballance. Existing classic product/application identity, package IDs,
GUIDs, saves and native ABI SHALL be preserved. Repository moves SHALL preserve
existing release tags/files and source history without force-pushing main.

#### Scenario: Restore classic ownership
- **WHEN** main resumes classic maintenance
- **THEN** its runtime, content and native plugin files match the preserved classic branch, and Reballance's development history remains available in its own repository

#### Scenario: Build independently
- **WHEN** Ballance is built or released
- **THEN** no Reballance build, mutable cache or publication approval is required

## MODIFIED Requirements

### Requirement: Independent edition identity
New Ballance builds SHALL use the original descriptor, historical Ballance product/application identity, v<version> tags and product-based archive names. Packaging SHALL verify the embedded edition, version, product and application identifier. Existing legacy releases SHALL remain valid and unchanged.

#### Scenario: Incorrect build relabeling
- **WHEN** a build identity does not match the checkout edition or selected version
- **THEN** packaging rejects it instead of renaming an older or different-edition player

#### Scenario: Classic maintenance
- **WHEN** main receives build or publication tooling
- **THEN** its gameplay assets, native ABI, application identity and save data remain independent from Remaster

### Requirement: Product release names
New classic assets SHALL use Ballance-<version>-<platform>. The original edition ID SHALL remain unchanged; legacy original/v tags SHALL remain valid historical refs. Reballance releases belong to their separate repository. Published legacy files SHALL remain immutable.

#### Scenario: Package either edition
- **WHEN** a verified current player is packaged
- **THEN** its filename uses its public product name and validation checks the matching edition identity and version
