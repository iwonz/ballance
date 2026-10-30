## ADDED Requirements

### Requirement: Responsive mobile host
The published Web player SHALL fit the visible viewport, respect display cutouts and preserve canvas aspect ratio in portrait, landscape and fullscreen. Gameplay touches SHALL not scroll or zoom the page. Mobile rendering SHALL cap device pixel ratio at one by default to avoid multiplying render work on dense screens.

#### Scenario: Phone viewport changes
- **WHEN** the browser viewport changes orientation or enters fullscreen
- **THEN** the player resizes without stretching its content and controls remain inside the usable area
