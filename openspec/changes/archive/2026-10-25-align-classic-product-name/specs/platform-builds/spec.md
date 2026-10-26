## ADDED Requirements

### Requirement: Product release names
New classic assets SHALL use Ballance-<version>-<platform> and remaster assets SHALL use Reballance-<version>-<platform>. Technical original/remaster edition IDs and tag prefixes SHALL remain unchanged. Published legacy files SHALL remain immutable.

#### Scenario: Package either edition
- **WHEN** a verified current player is packaged
- **THEN** its filename uses its public product name and validation checks the matching edition identity and version

## MODIFIED Requirements

### Requirement: Independent edition identity
New Original builds SHALL use the original descriptor, historical Ballance product/application identity, original/v<version> tags and product-based archive names. Packaging SHALL verify the embedded edition, version, product and application identifier. Existing legacy releases SHALL remain valid and unchanged.

#### Scenario: Incorrect build relabeling
- **WHEN** a build identity does not match the checkout edition or selected version
- **THEN** packaging rejects it instead of renaming an older or different-edition player

#### Scenario: Classic maintenance
- **WHEN** the original branch receives build or publication tooling
- **THEN** its gameplay assets, native ABI, application identity and save data remain independent from Remaster
