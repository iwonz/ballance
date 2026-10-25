# runtime-lifecycle Specification

## Purpose

Start the game through its service and package framework, move between logical scenes, and shut down owned resources. See the [source map](/docs/architecture.md#runtime-lifecycle) for implementation and validation boundaries.

## Requirements

### Requirement: Ordered startup
The game SHALL initialize settings and language resources before loading the core package, configured system packages, and enabled user packages. It SHALL enter the configured initial logical scene only after startup succeeds.

#### Scenario: Normal startup
- **WHEN** the player launches with valid core and required system packages
- **THEN** the intro and menu become available after package initialization

#### Scenario: Required package failure
- **WHEN** the core or a required system package cannot load or is below its required version
- **THEN** startup stops and reports the failure rather than starting a playable level

### Requirement: Logical scene transitions
The runtime SHALL support the configured logical scenes and notify subscribers when leaving and entering a scene. It SHALL reject an unregistered scene name.

#### Scenario: Enter gameplay from the menu
- **WHEN** a valid level start request is made from the menu
- **THEN** the runtime enters the Level logical scene and initializes its gameplay objects before loading content

#### Scenario: Unknown scene
- **WHEN** code requests a logical scene absent from the configuration
- **THEN** the transition fails without selecting that name

### Requirement: Package-scoped event integration
The runtime SHALL expose registered global events and single-handler notifications for package integration, with removable handlers and per-component event emitters.

#### Scenario: Package observes level loading
- **WHEN** a loaded package registers for a level lifecycle event
- **THEN** its handler receives the event when that lifecycle step occurs

### Requirement: Shutdown notifications
The normal quit path SHALL notify interested packages before releasing services and the native physics system. Desktop restart SHALL attempt to relaunch the executable; this is not a browser reload guarantee.

#### Scenario: Normal quit
- **WHEN** the player confirms quitting through the game
- **THEN** before-quit handlers run so persistence services can save their state before shutdown
