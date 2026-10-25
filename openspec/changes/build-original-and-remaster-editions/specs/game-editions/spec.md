## Purpose

Preserve the classic recreation while allowing an independently maintained Remaster
and explicit, reproducible selection of published editions.

## ADDED Requirements

### Requirement: Immutable Original baseline
Original SHALL preserve v1.0.11 at d066bfe5 with an annotated original/v1.0.11 alias.
Its maintenance branch SHALL retain classic presentation and gameplay. README SHALL
distinguish the Unity recreation from Cyparade's commercial original.

#### Scenario: Inspect the preserved game
- **WHEN** a maintainer resolves the original/v1.0.11 tag
- **THEN** it resolves to the same commit as v1.0.11 without changing existing release files

### Requirement: Independent edition maintenance
Original and Remaster SHALL have separate development branches, Unity worktrees,
release version sequences and application/storage identities. Main SHALL own Remaster
and site sources. Shared fixes SHALL be transferred selectively and verified in the
receiving branch without merging complete edition histories.

#### Scenario: Remaster update
- **WHEN** a Remaster build or release changes
- **THEN** Original's code, published binary and player data remain unchanged

#### Scenario: Independent installation
- **WHEN** both editions are installed on a supported native platform
- **THEN** they coexist without replacing one another or sharing mutable player preferences

### Requirement: Player-facing edition names
The classic edition SHALL be named Ballance and the remastered edition SHALL be named Reballance in application menus, game branding, site labels and maintained documentation. Technical branch/tag names, package IDs and storage identities SHALL remain stable.

#### Scenario: Identify either edition
- **WHEN** a player installs, launches or selects an edition on the shared site
- **THEN** its title is Ballance or Reballance, with no Original or Native suffix added to the classic product name
