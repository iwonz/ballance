## ADDED Requirements

### Requirement: Compatible bounded archive reads
Package loading SHALL read existing `.ballance` ZIP metadata, language resources, logos, bundles, and Mono code entries without changing their identifiers. Legacy leading slashes on root metadata SHALL remain accepted. Entry reads SHALL reject checksum or length mismatches and entries larger than 512 MiB before allocating their output, release owned streams on success and failure, and avoid worker-thread dependence on Web.

#### Scenario: Legacy package
- **WHEN** a valid package contains `/PackageDef.xml` and the existing bundle entry path
- **THEN** metadata and resources load with the same package identity

#### Scenario: Invalid or oversized entry
- **WHEN** a selected entry is truncated, has an invalid CRC, or exceeds the uncompressed entry limit
- **THEN** loading reports failure and closes the archive without accepting partial data

#### Scenario: Large Web resource
- **WHEN** a valid bundle requires multiple read chunks in a Web player
- **THEN** reading cooperatively yields between chunks and completes without background threads
