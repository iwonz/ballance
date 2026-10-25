## Purpose

Provide package-based language resources and persistent language selection without requiring fonts installed on the player machine. See the [source map](/docs/architecture.md#localization) for implementation and validation boundaries.

## ADDED Requirements

### Requirement: Language selection
The game SHALL offer English, Chinese, and Russian interface selection. It SHALL use the saved system-package language setting, falling back to the operating system language on first launch, and save a changed preference before requesting restart.

#### Scenario: Russian first launch
- **WHEN** no preference exists and the system language is Russian
- **THEN** the Russian interface resources are selected

#### Scenario: Language change
- **WHEN** the player selects another language
- **THEN** the preference is saved immediately and a restart prompt is shown if the current language differs

### Requirement: Resource fallback
Language loading SHALL load English values first, then overlay nonempty values from the selected language. Missing lookup keys SHALL return the explicit fallback or the existing missing-key diagnostic.

#### Scenario: Partial translation
- **WHEN** a selected language omits a key present in English
- **THEN** the English value remains available

### Requirement: Translation compatibility
Translations SHALL preserve required formatting placeholders and resource identifiers. Built-in prefab labels and package metadata SHALL resolve through the language resources.

#### Scenario: Placeholder mismatch
- **WHEN** a maintained translation changes the required formatting placeholders
- **THEN** the localization check fails

### Requirement: Bundled Cyrillic glyphs
The bundled UI fonts SHALL provide the Cyrillic glyphs used by Russian translations through the configured fallback, independent of locally installed fonts.

#### Scenario: Russian menu rendering
- **WHEN** Russian menu and tutorial text is rendered on a supported player
- **THEN** its Cyrillic glyphs are available from bundled fonts
