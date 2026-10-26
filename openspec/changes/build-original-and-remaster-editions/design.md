## Context

See proposal.md for motivation. Baseline d066bfe5 is already published as v1.0.11.
Unity 6000.6.7f1 uses Built-in shaders, uGUI, custom content bundles and native IVP.
Floor collision meshes currently come from render MeshFilters. Friction is supplied
at body creation. The pinned IVP exposes a material manager with contact positions
and recompute_friction for existing contacts. Pages currently extracts one player
at its root, and Web persistentDataPath depends on the page's directory URL.

## Goals / Non-Goals

Separate independently maintained editions without copying two projects into Assets
or sharing mutable Unity caches. Preserve serialized contracts and dry collision
geometry. A completed test slice is an intermediate milestone, not the final scope.
No engine/pipeline migration, mandatory paid assets, automatic cross-edition import,
new ball abilities, deep terrain simulation or iOS delivery.

## Decisions

### Editions and delivery

Create the annotated original/v1.0.11 alias and original branch at d066bfe5. Use an
ignored worktree for Original and keep main for Remaster/site; no full branch merges.
Original retains product and application IDs. Remaster uses Reballance and
com.iwonz.BallanceRemaster, independently numbered development builds until a release is verified.
An edition descriptor is read by build/packaging tooling; no runtime edition toggle.
Edition source tags are <edition>/v<version>, assets <Product>-<version>-<platform>.
Public distributions use release/v<version> on main and contain both independently versioned games.
Legacy v1.0.11 remains downloadable with its original files. Do not reuse binaries
under a new version. Release identity includes edition, tag, version and SHA-256.

### Environment and contact state

Use stable enum values standard, weather, moscow, london, sahara, hongkong, california.
LevelEnvironmentProfile data is separate from Level.json and owns sector sequences,
intensity limits and surface regions for thirteen built-in levels. Unknown custom
levels keep authored surroundings and dry behavior. SurfaceState contains wetness,
snow, ice, sand and vegetation cover; native profiles, shaders and sound consume
the same resolved state. Local regions attach to collider transforms, including moving
mechanisms. Preserve original collider source meshes when improving render silhouettes.

Coverage footprints stay in each body's local coordinates. Draft support footprints
use the two broad collision-bound axes, including bridges stored vertically. The
upward-face mask uses the world contact/render normal on both native and shader
paths; it must not assume that every mechanism's local Y axis is its walkable face.
Authoring and playthrough tuning remain required before these draft footprints ship.

One level-owned controller advances with simulation time, pauses with gameplay and
cleans up materials, particles, forces, audio and event subscriptions on unload.
Eight-second smooth transitions restart from the current blend when interrupted.
Sector checkpoints capture weather time/state; changing preset rebases the weather
checkpoint for that sector. Respawn restores that state, restart resets it. Author
weather sequences rather than roll random hazards, and retain sufficient fog visibility.

Native IVP material-manager contact lookup resolves ball material, floor material and
local coverage. Append ABI functions for profiles, body material/regions, state updates
and contact refresh; bump Remaster ABI to 2304 and keep Original 2302. Update persistent
contacts without destroying bodies. Resolve native contact work without a managed
callback for every solver contact. Contact results retain the existing support body ID for compatible native consumers. Rolling resistance is distinct from friction and
only acts while supported; wind uses the fixed simulation loop and existing force API.
All five target plugins must be reproducibly rebuilt and smoke-tested before release.

### Persistence and records

Use separate application identities and a Reballance settings namespace. Preserve
classic Ballance storage. The user's latest instruction replaces preset/rules score
partitions and practice-run eligibility with one `Remaster/Highscores.json` table.
Weather selection cannot affect scoring, timers, records or unlock logic. On first
load without that shared file, validate and merge earlier `Highscores-r*-*.json`
files, collapse identical name/score/date entries and sort by score. Atomically save
only after every source validates; preserve source files. Once the shared file exists,
use it without reimporting old entries. Keep the existing JSON payload and unlock file.
Remove the practice flag, warnings and record-preset heading, including localization
strings and fixtures. Score arithmetic and gameplay timer code remain unchanged.

### Presentation and rendering

Reuse Standard PBR lighting and a shared surface overlay shader. Source tileable
textures through imagegen when existing assets are inadequate; inspect repeatability,
map interpretation and scale. Keep roots/collision meshes and add detailed visual
children when needed. Local particle volumes, cloud layers and side vegetation have
quality-dependent density; physics and fog gameplay range do not depend on quality.
Use existing uGUI pages and lifecycle, upgrading their visual system rather than
replacing navigation. Include all dialogs, packages, scores, HUD and tutorial states.
Keep all localizations and bind tutorial prompts to actual input mappings.

Separate display/UI size from 3D render resolution. Offer Auto/1080p/1440p/2160p and
50/67/75/100% scale; cap default Auto to 1080p and default to medium quality. Preserve
aspect ratio, safe areas and readable UI. Show actual pixel dimensions. Use existing
graphics settings for independent shadows/AA and add clouds/effects controls.

A geometry-free presentation camera keeps a screen render target active while scene
cameras render into the resolution-controlled texture. The display and game UI use
overlay canvases; native Metal must be exercised as well as WebGL when this changes.
Scene targets remain bound across the entire frame, including the GPU multisample
resolve. Resize rebinds every owned camera before releasing the old texture.

Shutdown destroys registered IVP worlds and bodies before releasing the native
temporary-vector pool, because contact deletion invokes callbacks during cleanup.
Component teardown also tolerates already-destroyed command, sound and event
services; unloading a live level still unregisters its owned handlers normally.

### Site and Pages

Vanilla HTML/CSS/JS in site, RU/EN with browser-language initial selection. Use real
game captures for claims, labelled concepts only for direction. Header links to
original.html and remaster/. Original's HTML remains at the repository root directory;
its assets move under original/ with only host paths rewritten, preserving storage.
Do not preload either Unity instance on the landing. Keep original release.json at
original/release.json, remaster at remaster/release.json and an aggregate root manifest.

site/releases.json schema 2 pins one distribution tag and both source tags with SHA-256 hashes for WebGL, Windows, Linux, macOS and Android. Null release/remaster pins remain a deliberate publication barrier until gameplay acceptance. The local release preparer verifies all ten packaged artifacts and sidecars, assembles the existing two-player site, and stages one flat upload directory with the players, site archive/checksum, manifest, license, credits and release notes linking both source tags. Reuse an unchanged edition's bytes rather than renaming or rebuilding it.

The Pages workflow selects the approved distribution from main, then checks out that immutable release tag. It downloads the one release's assets, verifies the manifest and all platform hashes, reconstructs the site from its pinned Web inputs and source, and compares the attached site file-for-file before deployment. The attached site is the deployed artifact. Reject unsafe extraction and total size above 1,000,000,000 bytes. Serialize deployment and verify the distribution and both edition identities by HTTP. Update or roll back one game through a new complete distribution while preserving the other pin. Never resolve a game's version through latest.

Inject small localized navigation links into both hosted player pages, outside Unity; retain keyboard focus and stable Original storage paths. Keep the classic footer reachable on narrow/mobile screens by sizing the Web canvas container around it; desktop game presentation remains unchanged. README owns the introductory guide and version-labelled screenshot galleries. CREDITS.md inventories attribution and carries the exact upstream notice; release assets and the site include it and LICENSE. GPL code rights and separately licensed game data are described separately; branding does not grant new rights over inherited content.

## Risks / Trade-offs

- Native weather changes can break original puzzles: calibrate on the test course,
  retain dry measurements and complete all 91 level/preset combinations.
- New art is expensive in Web memory/frame time: reuse material families and limit
  texture/particle budgets; measure each weather case and combined site size.
- Two branches duplicate maintenance: small explicit cherry-picks preserve isolation.
- Hardware is not guaranteed available: report missing devices honestly and leave
  acceptance tasks open; build success is not gameplay/performance certification.
- Existing baseline has persistence and package-trust gaps: do not erase user data
  or claim unrelated defects are solved by this change.

## Migration Plan

Preserve Original refs first. Implement and validate locally without replacing live
v1.0.11. Keep the distribution manifest unready until Remaster gameplay acceptance is complete.
Prepare one draft distribution with both verified five-platform artifact sets, publish, wait for
Pages, then exercise both public games. Never overwrite the immutable baseline.

## Validation

Use docs/validation.md plus environment/checkpoint/scoring tests, edition isolation,
malformed profile/archive rejection, 30/60/120/144 FPS input replay, all thirteen levels
in seven presets and physical input/audio checks. Primary performance target is RTX
3060 12 GB, i5-12400, 16 GB RAM, Windows 11, High, 1920x1080 at 60 FPS using the documented
frame/GPU tolerances. Add QHD/4K measurements and retain Apple M3 regression captures.
Record screenshots, routes, cold loading, p95/p99, 1% low method, CPU/GPU and memory.

## Web lifecycle completion

The historical Web player stops rendering after an in-game restart/exit because
Application.Quit does not reload its browser host. Remaster's shared quit entry
uses a Web-only JavaScript bridge: wait for Unity's automatic persistence queue,
finish an explicit save sync, then notify the host to quit the instance and reload
or return to the parent landing. Failure is visible and must not navigate away.
The bridge is pinned to the exact editor's IDBFS implementation and tested with
delayed-success/failure fixtures and actual language restart in the player.

## Covering texture refinement

Snow, ice, sand and moss use separate generated repeating albedo sources with
material-specific smoothness variation. Their UVs follow the physical support's
local dominant plane, including the vertically stored folding bridge. This changes
detail inside the shared coverage weights without changing collision geometry.
The final organic region boundaries and HUD treatment are described below.

Fixed presets also remap the authored sky's luminance into a shared theme palette,
so California remains bright blue and Moscow cool on warm sunset levels. Standard
keeps the authored palette with refreshed cloud volumes. A premultiplied RGB/weight vector blends with the same weather
clock and checkpoint state, preserving continuous interrupted transitions.

## Remaster branding and remaining interface refresh

The user's follow-up removes upstream IMENGYU/studio promotional branding and links
from the Remaster player, including the initial loading logo and About screens.
Retain source copyright and third-party license notices. Replace the cloud-reveal
illustration with generated Reballance artwork and record its provenance.
Give records a dedicated layout; finish the remaining sliders, toggles, selectors,
dialogs and package views. Render tests must use the same page containers as runtime,
including narrow common pages, rather than stretching every prefab to the screen.

## Material relief and living atmosphere

The generated Remaster title is the user's direction for soft, dense cloud volumes in the running game. Flat cloud images alone are insufficient acceptance. Built-in shaders will combine view-dependent density and light transport with layered motion, within separately measured quality budgets. The default dry track and collision meshes remain unchanged.

Imagegen supplies material color/detail sources; reproducible authoring code produces explicitly synthetic, material-specific height, normal and roughness maps. These are art-authored relief, not measured physical scans or depth inferred as ground truth from a photograph. Unity imports the normal maps correctly and the shared shader consumes height/roughness alongside albedo, including weather coverings. Close-up rotation, moving light, repeat seams and oblique viewing must be inspected. Rain, snow and foliage require motion/lighting review as well as static captures.

## Local macOS installation hygiene

The user requested exactly one current Original and Remaster application on the Mac. Use canonical `/Applications/Ballance.app` and `/Applications/Reballance.app` paths. A separate standard-library installer stages and validates the replacement before swapping it, rejects unknown bundle identifiers and downgrades, and then removes matching duplicate installed applications from application folders. It unregisters build/archive copies without deleting them; automatic approval review rejected deletion inside archive/build directories. Saves and release archives are excluded. Future native test runs use these canonical installations rather than registering additional diagnostic copies.

Spotlight rediscovered preserved app bundles after unregistration. Keep the local generated tree in `Builds.noindex` with a `Builds` directory symlink for existing tooling. The installer performs this reversible move only after validating an installation, refuses conflicting paths and unregisters the copies. This excludes Spotlight indexing but did not prevent LaunchServices rediscovery; it is not sufficient on its own. CI still uses a normal `Builds` directory.

Stock menu skies C and M must use the same volumetric renderer as built-in gameplay skies A–M. Their authored palettes, including night, remain distinct. Explicit custom sky textures continue through the original custom-sky path. Wave-like integration bands are an acceptance failure, not an intentional style.

The user identified copied cloud rows in the first volume. Break the short texture period with a continuous world-space macro field that varies grouping, deck height and domain distortion, plus independently rotated billow detail. Keep deterministic simulation-time drift and shared menu/gameplay rendering. Inspect multiple headings and translated cameras; a render translated by the old texture period must no longer reproduce the same image. This visual change must not alter weather physics.

The follow-up asks for a denser deck: increase its vertical thickness and fill the largest gaps while preserving irregular billow silhouettes and internal shading. Density remains a visual property and must not obscure the playable route or change weather forces.

Reducing ray integration steps exposed bands. Preserve smooth integration in the cached cubemap described below. The sky-only camera renders no UI or geometry and restores the authored sky after rendering. Reflections and custom skies retain their own paths.

The native medium-quality menu remained too expensive after the first optimization. Precompute the smooth macro grouping field in a small linear 2D texture instead of evaluating lattice hashes inside every ray/light sample. Its approximately 48 km world period is beyond the bounded 8.4 km ray range; independently transformed density/detail sampling breaks the former short repeated pattern. Preserve continuous simulation-time movement and check the same translated/headed views after regeneration.

The latest naming request uses Ballance for the classic macOS application, retaining its original bundle ID and signed contents. Installation migrates the earlier Ballance Original path through the same verified replacement and duplicate cleanup. Validate the application launcher as well as Spotlight; an unregistered historical LaunchServices record alone is not evidence of a visible duplicate.

Use Ballance and Reballance as the public product names throughout current documentation and the RU/EN landing. Keep original/remaster technical IDs, release tag prefixes, and existing archive names compatible; historical verification records retain the names actually tested.

The final public remaster name is Reballance. Update the generated title artwork, runtime text, all four localizations, native product names, Web host and RU/EN site together. Keep the existing com.iwonz.BallanceRemaster application identifier, remaster technical IDs and preference keys for compatibility. Desktop file storage must continue to resolve the existing pre-rename data directory; a product display-name change must not strand existing profiles. The installer migrates the former app path and removes only matching installed bundles. New Remaster archives use Reballance while published archive names remain immutable.

## Gameplay HUD and covering feedback

The user rejected the solid score/life blocks and the centered preset label. Use compact corner typography, small line icons and text contrast shadows, retain score feedback, and remove the gameplay preset heading. Existing score/life data and serialized callbacks remain unchanged.

The user also reported straight artificial covering stripes and a visible FPS regression in the Moscow preset. Replace rectangular region masks with irregular feathered elliptical footprints, evaluated consistently in native contacts and the surface shader using the contact/mesh normal to choose the local projection plane, even on thick supports. Vary deterministic local placement and aspect ratios to avoid copied strips, including moving supports. Preserve the ABI layout; record storage remains independent of these physical footprints. Avoid uploading unchanged static surface bindings each frame and sampling inactive covering textures. Measure the sky and reflection contribution before selecting further rendering changes; preserve the dense cloud direction and gameplay physics when reducing rendering cost.

The sampled dev.6 native player spends substantial main-thread time waiting for Metal presentation; this is not a GPU timing measurement but supports investigating rendering cost. Replace the per-frame screen-space cloud raymarch with a camera-position cubemap cache. Populate it once, then render one face per frame into a second buffer with a fixed weather snapshot and blend completed cubes. Camera rotation samples the cached sky immediately; position and weather changes request new captures. Reuse buffers and retain full cloud density/lighting rather than reducing geometric simulation. Validate directions, seams, camera translation, pause and transitions; measure native frame timing after the change.

The first cubemap-cache Web candidate still showed approximately 16 FPS on Apple M3 at Medium with the previous native player also running. Treat that as a failed diagnostic observation, not a qualified idle-machine benchmark. The atmospheric shader now derives its palette from the front texture and ignores each old face color, so its six identical passes are redundant. Collapse it to one directional pass and compare rendered headings and direct/cached output before rebuilding; retain the serialized texture properties for asset compatibility.

Native acceptance exposed a clipped localized return-to-menu warning. The existing
dialog reserves only one line. Give its message enough height for the complete
warning and check preferred text height in every existing language/resolution fixture;
retain the serialized buttons and event bindings.

The user requested removing the line below the Web player's About/Full screen
header. It is the canvas focus outline, not a header border. Restrict the accent
focus ring to links/buttons and suppress the canvas outline, retaining keyboard
input and visible control focus. Verify the loaded player and Tab navigation.
Keep Tab's browser default action by stopping its propagation before Unity installs
its global keyboard handlers; game input must not trap focus away from host controls.

## Centered menus and language flags

Center the main menu panel and its title, description and existing buttons using the
same 460-unit content width as common settings pages, clamped to the available width.
Keep level selection centered and its heading centered. Add four small original flag
sprites before the existing language labels (Russia, United Kingdom, China, Taiwan),
retaining language names, button identities, localization and keyboard selection.
Inspect these pages in all four language/resolution fixtures and in the player.

The user rejected the dark weather footprint trail behind the ball. Remove its
controller integration, pooled decal implementation and unused shader; preserve
normal mesh shadows and native contact ABI. Remove obsolete track-only fixtures.

Generate one transparent interface icon atlas with imagegen and record provenance.
Use its consistent mint/ivory line family for built-in package rows/details, generic
package fallback, HUD score/lives, arrows, refresh and toggle marks. Reuse/rotate the
same sprites for directional controls; preserve text labels and interactions. Keep
language flags as recognizable country images rather than symbolic UI glyphs.

## Repeated application discovery and release gate

The user reproduced duplicate apps after the noindex/unregister workaround. After
validating a canonical installation, rename matching unpacked build bundles from
`.app` to `.app.disabled`, unregister their previous paths and remove stale matching
LaunchServices registrations (including obsolete translocation paths). Choose an unused numbered suffix for name
conflicts, preserve every bundle byte and release archive, and do not reset the
global app database. Package before installing; a retired diagnostic can be restored
by renaming it deliberately. Verify both application catalogs and the visible launcher.

The user explicitly authorized meaningful commits, tags, pushes, both releases and
Pages publication, then required Reballance publication to wait for full gameplay
acceptance. Do not publish it as a prerelease workaround. Keep outstanding 91-run
and performance evidence explicit; unavailable physical Android/reference-PC tests
remain marked unrun as already agreed. The two-game Pages launch waits for eligible
working builds of both editions in one unified distribution release.

The interface icon inventory also includes dynamically instantiated touch keypads.
Use the same atlas for directional/camera buttons and the joystick thumb, retaining
existing hit areas, action bindings and pressed-direction visibility. Apply the theme
at keypad creation and render all five built-in layouts in editor fixtures; physical
Android input remains a separate unavailable check.

The user subsequently explicitly requested deletion of obsolete `.app.disabled`
contents to reclaim disk space, superseding the earlier preservation workaround.
After validating the installed copy, remove matching non-current bundles under
the known build roots, including retired suffixes; preserve newer work in progress
and reject active copies. Development macOS output/installation filenames use
`-dev.app`; product IDs and storage paths remain unchanged. The legacy macOS shell
entry point delegates to the same edition-aware build and packaging commands,
instead of overriding the application name. Cleanup includes disabled copies in
application folders and checks running executables through real paths as well as
the Builds symlink. Stable delivery replaces
the development installation and removes its temporary copies. Release archives
remain separate packaging evidence until publication checks complete.

## Runtime integration evidence

Add an editor-only play-mode runner using the normal entry scene, native IVP and
level loader. Give it a temporary preference namespace and file root before game
startup, retain per-level/preset results, and stop on runtime exceptions or timeout.
Exercise pause, interrupted weather transitions, checkpoint restore, restart, ball
creation and level teardown. Direct sector/ball setup is classified as integration
evidence; it must not be reported as an unaided playthrough or satisfy the 91 full
completion requirement. Player performance is measured separately with no runner
or simultaneous build. The stale macOS output-name assertion must accept the
development suffix while retaining stable-name validation.

### Legacy cloud overlay removal

Runtime acceptance exposed the old `FinalSkyLayer` / `FinalSkyVoterx` plane still
covering the new sky. After a validated built-in environment attaches, destroy
that presentation-only plane and clear its loader reference so settings, restart,
preview and completion callbacks cannot re-enable it. Unknown custom levels keep
their authored plane. TextureScroller uses a renderer property block so its
remaining custom-level use cannot mutate a shared material or persist play-mode
animation into source assets. Verify both behaviors in editor/runtime checks and
inspect a rebuilt player before claiming the cloud direction is accepted.

### Play-mode UI component creation

The runtime check reproduced a missing HUD CanvasGroup hidden by edit-mode
render fixtures. Replace null-coalescing GetComponent/AddComponent calls in the
Remaster UI with TryGetComponent, which handles Unity's missing-component wrappers.
Apply the same correction to HUD contrast, panel backgrounds and About layout.
Require real HUD visibility/contrast components in the visual fixture and exercise
HUD update/pause/resume during play mode.

### Legacy unpacked backup cleanup

The scoped inventory found one remaining old `Ballance-1.0.8.app-backup` bundle.
Treat the exact `.app-backup` suffix as retired alongside `.app.disabled`; keep
the existing bundle identity/version, process and replacement validation checks.
Do not broaden matching to arbitrary backup directories or archives. Cherry-pick
the shared installer correction to Original and verify it independently.


### Cloud-deck clearance follow-up

Runtime captures still show oversized blurred cloud shading under low platforms.
The current deck's possible upper extent is roughly 390 world units above its
base, while its base is placed only 300 below the lowest floor. Inspect a lower
deck placement against the same gameplay views before accepting the cloud visuals.
Keep camera visibility above the volume, calibrate the existing light-tap distance
to the billow size without additional samples, retain the cached render budget, and
compare direct/cached renders, headings and translated positions. This is a visual
placement correction; collision geometry and weather physics remain unchanged.

The packaged development-10 player still has oversized smooth billows. Compare a
smaller base billow scale and coherent multi-scale density detail in the existing
volume texture before increasing runtime sample counts or cache dimensions. Retain
the development-10 captures as the comparison; reject variants that reintroduce
wave bands, repeated rows or excessive cost. Audit the first tutorial greeting as
part of the same public-name check in all four languages.

Smaller density, altered vertical envelopes and alternative lighting did not
resolve the stretched forms. The retained correction reduces the macro domain
distortion from 2.6 to 0.55 noise-space units: the former displacement folded and
stretched otherwise round billows. Expand the density volume's world-space repeat
period while preserving billow size by doubling its cell count and halving texture
frequency; retain the 128-cubed texture dimensions, light samples and
cache dimensions. The distance-bounded march below reduces excess primary samples. Inspect direct/cached silhouettes and all
presets, retain the translated-camera check against repeated rows, then compare
the result in rebuilt players. Final reference-art acceptance remains separate.


### Diagnostic console material compatibility

The actual Web settings-reset test opens the shipped diagnostic console. Its
legacy UI blur shader lacks the `_MainTex` property expected by uGUI, causing a
material error and recursive graphic-rebuild diagnostics. Add the standard
per-renderer texture property without changing the blur algorithm, then recheck
console rendering and `/s reset` in the isolated browser profile. Retain its GUID.

### Powered foreground frame follow-up

Development 10 still misses the frame budget on Apple M3 after charging and a
foreground capture. Its shader always allocates 240 primary samples even for a
short ray crossing the cloud layer vertically. Compare a distance-bounded march
with 0.08/0.05/0.03 density-space step lengths at Low/Medium/High, clamped to 32–240
samples. Keep density, extinction per unit length, light probes, cache dimensions
and weather/physics unchanged. Retain fixed-step screenshots for comparison at
all presets, headings and both repeat periods. Reject visible banding or silhouette
loss, and measure the rebuilt player separately; editor render time is not FPS.
