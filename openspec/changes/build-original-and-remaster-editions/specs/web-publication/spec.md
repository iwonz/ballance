## MODIFIED Requirements

### Requirement: Verified release extraction
Pages preparation SHALL validate each pinned edition's Web archive and matching
SHA-256 sidecar, require an empty destination, reject unsafe paths, links and special
files, validate complete content and tag/archive/player version agreement, and enforce
a combined published-site limit of 1,000,000,000 bytes. Successful preparation SHALL
write edition-specific identities and an aggregate manifest with tags, versions,
archive names and verified checksums. Legacy v1.0.11 SHALL remain a valid Original input.

#### Scenario: Corrupt archive
- **WHEN** either archive checksum differs from its pinned value or sidecar
- **THEN** publication fails before deploying either edition

#### Scenario: Unsafe archive member
- **WHEN** an archive contains a link, special file or escaping path
- **THEN** preparation rejects it

#### Scenario: Wrong player version
- **WHEN** a release tag differs from the archive or embedded player identity
- **THEN** preparation fails rather than relabeling that player

#### Scenario: Combined size overflow
- **WHEN** both players plus landing exceed the site size budget
- **THEN** preparation fails even if each player alone fits

### Requirement: Release-triggered deployment
Every published edition release SHALL trigger mandatory preparation and atomic Pages
deployment of the two explicitly pinned editions and landing. Main SHALL own the
deployment implementation and release manifest. Missing assets or an event tag not
pinned in the manifest SHALL fail visibly. Manual dispatch SHALL allow restoring a
previously verified edition pair. Delivery SHALL remain incomplete until deployment succeeds.

#### Scenario: Publish Web release
- **WHEN** a pinned edition release is published with its archive and checksum
- **THEN** the workflow validates both editions and deploys the site without a Unity license

#### Scenario: Missing Web asset
- **WHEN** either pinned release lacks its required Web asset or checksum
- **THEN** the workflow fails and leaves the previously deployed site intact

#### Scenario: Redeploy older version
- **WHEN** one manifest entry is changed to a verified older release and deployed
- **THEN** that edition rolls back while the other retains its pinned identity

### Requirement: Deployment permissions and browser verification
Pages SHALL use GitHub Actions with intended release refs allowed. Serialized deployment
SHALL verify both public identities within a bounded wait and fail on stale identity.
Acceptance SHALL include HTTP-served menu, level and control checks for both editions.

#### Scenario: Release readiness
- **WHEN** a release is declared delivered
- **THEN** verification identifies both public URLs and actual gameplay checks

#### Scenario: Stale deployed site
- **WHEN** either public identity differs from the requested manifest after deployment
- **THEN** verification fails rather than reporting success

## ADDED Requirements

### Requirement: Reballance player header
The Web player SHALL expose About and Full screen controls without a full-width
divider below its header. Keyboard focus SHALL remain visible on those controls.

#### Scenario: Game canvas gains focus
- **WHEN** the loaded game receives keyboard focus
- **THEN** no focus outline appears as a line beneath the header, and Tab navigation still highlights the header controls

### Requirement: Bilingual game landing
The root SHALL show a responsive Russian/English landing explaining Ballance history,
Original, Remaster, materials, presets, downloads and attribution. Browser language
SHALL choose the initial language with an explicit switch available. Header play
links SHALL lead to working Original and Remaster players without loading Unity on
the landing. Reduced-motion and keyboard navigation SHALL be supported.

#### Scenario: First visit
- **WHEN** a Russian-language browser opens the root
- **THEN** it sees the Russian landing and two play choices without downloading a player

### Requirement: Stable Original browser storage
Original SHALL launch at original.html in the former root page directory with its
resources under original/. Remaster SHALL launch under remaster/. Hosting changes
SHALL preserve Original's saved profile and all requests SHALL retain the repository prefix.

#### Scenario: Returning Original player
- **WHEN** a browser with an existing root-hosted v1.0.11 save opens original.html
- **THEN** its saved data is available and no Remaster data is imported
