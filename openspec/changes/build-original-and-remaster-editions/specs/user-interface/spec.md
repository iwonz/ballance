## ADDED Requirements

### Requirement: Remaster presentation and guidance
All Remaster menus, dialogs, loading states, score/package pages, HUD, pause and ending
screens SHALL use a coherent readable responsive presentation while retaining their
existing functions and languages. Tutorials SHALL be contextual, skippable and
replayable, and show current input bindings and appropriate touch guidance.

#### Scenario: Remaster branding
- **WHEN** Remaster starts or displays its About, records, loading or package screens
- **THEN** the visible experience uses Reballance branding and current project links without the upstream IMENGYU/studio promotional logos or links
- **AND** the cloud-reveal illustration is newly generated Reballance artwork, while source copyright and third-party license notices retain their legal text

#### Scenario: Complete control styling
- **WHEN** a player visits records, audio, controls, language, packages or confirmation dialogs
- **THEN** their controls and illustrations share the new visual system with readable text, clear focus and adequate touch targets

#### Scenario: Consistent menu alignment
- **WHEN** the main menu or a settings page is opened
- **THEN** its primary content is centered horizontally at portrait and landscape sizes

#### Scenario: Language icons
- **WHEN** language selection is opened
- **THEN** each supported language has a flag icon before its readable name, using image assets independent of emoji font support

#### Scenario: Consistent interface icons
- **WHEN** HUD, modifications, package details or menu controls are visible
- **THEN** their icons share one newly generated visual family, including score and lives, without mixing legacy logos and font symbols

#### Scenario: Rebound tutorial controls
- **WHEN** movement bindings change and the tutorial is replayed
- **THEN** its prompts show the current bindings and can be dismissed without blocking progression

#### Scenario: Web restart or exit
- **WHEN** Remaster settings require a restart or the player exits through the game menu
- **THEN** pending saves are synchronized before the browser reloads Remaster or returns to the landing
- **AND** synchronization failure leaves a visible error instead of silently navigating away

### Requirement: Independent rendering and environment settings
Remaster SHALL separate weather selection from Low/Medium/High graphics quality and
individual cloud, shadow, AA and effect controls. It SHALL offer Auto, Full HD, QHD and
4K, plus 50/67/75/100 percent 3D render scale, with actual pixel dimensions displayed.
Default quality SHALL be Medium and Auto SHALL cap 3D rendering at Full HD. Layout
SHALL preserve aspect ratio, safe areas and UI readability across viewport sizes.

#### Scenario: High DPI browser resize
- **WHEN** the Web viewport changes size or enters fullscreen
- **THEN** UI fits the visible area and the 3D target respects the selected budget without stretching

#### Scenario: Weather change during a run
- **WHEN** a different preset is selected during a level
- **THEN** it applies smoothly without a practice-run warning or any change to score, timer or record logic

### Requirement: Unobtrusive gameplay counters
Reballance SHALL show score and lives as compact readable corner counters without solid rectangular panels. The HUD SHALL omit the centered environment-preset label, retain score/life updates and show no practice-run warning.

#### Scenario: Play with a fixed preset
- **WHEN** a level is running in Moscow or another preset
- **THEN** score and lives remain readable over bright clouds and dark surfaces while no preset name occupies the top center
