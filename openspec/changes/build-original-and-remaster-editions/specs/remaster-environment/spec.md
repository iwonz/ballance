## Purpose

Provide readable modern materials and authored weather conditions with consistent
visual, physical and audio behavior throughout the thirteen built-in levels.

## ADDED Requirements

### Requirement: Seven authored presets
Remaster SHALL offer Standard (default), Standard + Weather, Moscow, London, Sahara,
Hong Kong and California. Standard SHALL retain each level's identity and dry gameplay.
Standard + Weather SHALL use authored level/sector sequences. Other presets SHALL
apply their chosen theme throughout all built-in levels. Unknown custom levels without
an environment profile SHALL retain authored surroundings and baseline physics.

#### Scenario: Choose a fixed biome
- **WHEN** Moscow is selected and any built-in level is loaded
- **THEN** winter presentation and authored snow/ice regions are active in that level

#### Scenario: Unknown custom content
- **WHEN** a custom level has no environment profile
- **THEN** it loads without applying unvalidated weather forces or altering its authored sky

### Requirement: Consistent surface state
Weather SHALL share one resolved surface state across graphics, contacts and
audio. Wetness, snow, ice, sand and vegetation SHALL have separately calibrated
effects for ball/surface material combinations. Fog alone SHALL NOT change friction.
Coverage SHALL be local and visibly match contact regions, including moving supports.
Wind SHALL use bounded authored forces and the simulation clock.

#### Scenario: Dry and icy parts of one route
- **WHEN** a ball moves from visible dry stone into a visible ice region
- **THEN** only the covered contacts adopt the ice profile and the dry region remains baseline

#### Scenario: Loose covering
- **WHEN** a ball rolls through snow or sand
- **THEN** rolling resistance differs from ice sliding and does not act as airborne drag

### Requirement: Continuous and recoverable weather
Manual preset changes SHALL apply during play with an eight-second game-time blend
from the current resolved state. Pause SHALL stop advancement. Repeat changes SHALL
not jump to the previous blend endpoint. Checkpoints SHALL restore coherent weather
and mechanisms; manual changes SHALL rebase checkpoint weather to the selected preset.
Restart SHALL reset the selected preset's initial scenario.

#### Scenario: Interrupted transition
- **WHEN** a player changes from rain toward snow and selects sun before the transition ends
- **THEN** the new blend starts at the current state without abrupt contact or lighting changes

#### Scenario: Pause and recovery
- **WHEN** weather is paused and the player later resumes or respawns
- **THEN** pause duration adds no weather time and respawn restores the checkpoint's weather state

### Requirement: Readable scalable presentation
Remaster SHALL improve materials, silhouettes, skies, lighting, contact sound and
restrained effects without changing baseline collision geometry or obscuring required
path edges. Quality settings SHALL NOT change weather physics or necessary fog range.
Paper SHALL remain functional in rain. No new movement abilities SHALL be introduced.

#### Scenario: Lower quality
- **WHEN** a player reduces cloud and particle quality
- **THEN** visual cost decreases while the same wind, contacts and required visibility remain

### Requirement: Full environment acceptance
Final acceptance SHALL cover all thirteen levels in all seven presets, three ball
materials, checkpoints and moving mechanisms, frame-rate-independent input and real
performance measurements. Concept art or a test slice SHALL NOT count as completed
level-wide implementation or runtime evidence.

#### Scenario: Release qualification
- **WHEN** the first complete Remaster is reported delivered
- **THEN** verification includes the 91 level/preset results and identifies tested hardware and limitations

#### Scenario: Directed runtime integration
- **WHEN** automated checks directly select sectors or create ball types
- **THEN** they run in an isolated profile and retain their results as integration evidence, without counting them as full level completions

### Requirement: Volumetric clouds and coherent material relief
Remaster clouds SHALL have soft, dense, enveloping volume, layered depth and internal light response consistent with the generated title illustration. Snow, rain and vegetation SHALL have visible motion and material response appropriate to their environment. Base materials and coverings SHALL use coherent albedo, normal, height and roughness data, authored through image generation, reproducible code and Unity's material/shader capabilities. Relief SHALL preserve the calibrated collision geometry and readable route edges.

#### Scenario: Material under moving light
- **WHEN** the camera or light changes across stone, wood, metal, paper, snow or sand
- **THEN** relief and highlights respond to the view and lighting rather than remaining painted into the albedo

#### Scenario: Living environment
- **WHEN** a player observes a cloudy, rainy, snowy or tropical preset in motion
- **THEN** clouds show soft three-dimensional density and lighting, precipitation moves at plausible scale, and foliage has restrained motion without blocking the route

#### Scenario: Refreshed stock skies
- **WHEN** the menu or any built-in level displays a stock sky
- **THEN** it uses the modern textured cloud presentation, retains its day/night palette, and shows no visible wave-like sampling bands

#### Scenario: Legacy cloud plane
- **WHEN** a built-in environment loads, restarts or changes cloud quality
- **THEN** its old scrolling plane cannot cover the modern sky, and texture animation does not modify shared source materials

#### Scenario: Cloud-deck clearance
- **WHEN** the player reaches a low platform on a built-in level
- **THEN** the lower cloud volume remains below the playable route so the camera sees billow shapes instead of being immersed in diffuse cloud density

#### Scenario: Irregular cloud distribution
- **WHEN** the player rotates or travels above the cloud deck
- **THEN** cloud groups vary in height, size and spacing without recognizable copied rows or a repeating horizon pattern; motion remains continuous

### Requirement: Organic physical covering footprints
Local snow, ice, sand and vegetation coverings SHALL have varied irregular feathered footprints rather than repeated rectangular strips. Rendering and native surface sampling SHALL evaluate the same coverage boundary in the physical object's local coordinates. Static surface bindings SHALL not be re-uploaded each frame when unchanged.

#### Scenario: Moscow platform
- **WHEN** the ball traverses a visible ice patch on a stationary or moving platform
- **THEN** the patch has a natural boundary and the contact state blends at the same boundary shown by the material

### Requirement: No weather footprint trail
Weather SHALL NOT create dark footprint decals or a trailing shadow behind the ball.
The ball's normal lighting shadow SHALL remain independent of that removed effect.

#### Scenario: Change environment while rolling
- **WHEN** the player changes environment and continues moving
- **THEN** no weather footprint trail is created behind the ball
