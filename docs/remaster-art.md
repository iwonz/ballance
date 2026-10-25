# Remaster material sources

The current sources are development assets. The active
[implementation change](../openspec/changes/build-original-and-remaster-editions/proposal.md)
owns completion and verification. They are not evidence that the reference art
has been reproduced in the running game.

Five base-material and four covering albedo textures were generated with the built-in imagegen tool on 2026-10-25.
Each returned PNG is **1254 × 1254**, despite the requested 2048 × 2048 size. The
files were copied without resampling or image editing into
[Remaster resources](../Assets/Game/Remaster/Resources/Remaster).

| Asset | SHA-256 |
| --- | --- |
| Sandstone.png | ffb8e9d4807fae9bed890d133e1100883e2121b9ea621ae8a07a85895cae8990 |
| Oak.png | e3e26d2ec6d1344fd0c70910d9c60e3f359d28aa21e4a902bcbc614fb0af7d78 |
| Granite.png | 3ece6c463391e3cd153da979dc22092ba32c938c13888e5d964232ea997ca69b |
| Paper.png | daa28f5ad5cf26e575d2c0d564b63e0d41b0620ab70cb640f277554e16c4734a |
| Concrete.png | 49933944e9b71f391fe1b7a203baf5c06d73b45b8ccccd9fbc0abbe4590f91e9 |

## Exact prompts

### Sandstone

Use case: photorealistic-natural. Asset type: seamless square PBR base-color texture for a real-time Unity game, Ballance Remaster. Create ONE 2048 by 2048 tileable albedo image of pale warm beige fine-grained sandstone, matching the light airy natural stone tracks in the supplied Ballance material references. Orthographic top-down flat surface fills every pixel. Dense subtle pores, mineral specks and gentle broad color variation, restrained realism, neutral even diffuse illumination, no directional light, no ambient shadow, no highlights, no cracks that suggest deep geometry. Seamless repeat on left/right and top/bottom edges, uniform texel density, no border, text, ball, scene, perspective, props, captions or watermark. This is a technical texture source, not a scene render.

### Oak

Create ONE seamless square 2048x2048 PBR base-color texture for Ballance Remaster. Warm honey-brown natural oak wood, longitudinal fine grain running vertically, subtle growth rings and small restrained knots. Continuous wood surface, not separate boards; no gaps, nails, bevels or construction. Flat orthographic evenly lit technical albedo scan with zero directional lighting, shadows or specular highlights. Fine realistic but clean detail, warm subdued colors and moderate contrast, suitable for both a wooden ball and suspended wooden walkways. All four edges tile seamlessly. Full bleed, no text, borders, props, perspective or watermark.

### Granite

Create ONE seamless square 2048x2048 PBR base-color texture for the dense stone ball in Ballance Remaster. Dark gray natural granite with fine charcoal, gray and muted warm mineral grains and occasional tiny light flecks; matte stone, not metallic. Orthographic flat evenly lit technical texture scan. No highlights, gradients, ambient shadows, large cracks or deep holes. Subtle rich natural microdetail with calm contrast so the ball remains readable during rotation. Seamless repeat on all four edges, full bleed, no scene, sphere, text, border or watermark.

### Paper

Use case: photorealistic-natural. Asset type: seamless PBR base-color texture for Ballance Remaster's crumpled paper ball. Create one square tileable ivory paper albedo, full bleed. Warm off-white handmade cellulose fibers, tiny fine fibers and subtle mottling, restrained realistic detail. Flat orthographic material scan under perfectly even neutral diffuse illumination. Color information only: no baked crease shadows, no highlights, no directional light, no vignette, no gradients. This will be wrapped over an independently crumpled 3D mesh; do not paint large folds or polygons into the image. All four edges must tile seamlessly. No sphere, scene, props, text, border or watermark. Aim for 2048 by 2048 pixels.

### Concrete

Use case: photorealistic-natural. Asset type: seamless square PBR base-color texture for Ballance Remaster's concrete mechanism bases. Create one tileable 2048x2048 albedo scan of pale warm gray cast concrete, realistic tiny dark pores, fine sand aggregate and small mineral flecks, occasional extremely subtle mottling. Calm low contrast, clear distinction from beige sandstone. Orthographic flat surface fills every pixel under perfectly uniform neutral diffuse light. Color only, no directional light, shadows, highlights, large cracks, seams, panels, edges, bolts, stains or perspective. All four edges must tile seamlessly. No object, scene, text, caption, border or watermark.

## Integration and acceptance

RemasterMaterialAuthoring.Apply assigns the shared Built-in Standard surface
shader to explicit material families while retaining material GUIDs and authored
layout textures. It does not modify mesh vertices or collision geometry. Inspect
repeat seams, scale, highlights, ball rotation and each material under all presets
before accepting the assets. `RemasterReliefAuthoring.Generate` now creates explicit material-specific normal, height and roughness data; see the relief pipeline below. Glass uses Built-in Standard transparency; rubber, brass and concrete have explicit material assignments. Full material acceptance remains outstanding. `RemasterBallVisual` refines only the three player render meshes; paper keeps the imported mesh as its explicit native convex source. Wood and stone retain their original native sphere radii. Runtime silhouette changes still require player inspection.

The landing's weather contact sheet is the user-supplied concept reference and
is labelled as such. Archive images come from the repository's DemoImages.
Replace or supplement them with verified Remaster captures as implementation
reaches acceptance; never relabel a concept as an in-game screenshot.

## Interface font

Noto Sans CJK SC Regular is bundled unchanged from the [official Noto CJK repository](https://github.com/notofonts/noto-cjk), revision `f8d157532fbfaeda587e826d4cd5b21a49186f7c`, `Sans/OTF/SimplifiedChinese/NotoSansCJKsc-Regular.otf`. It covers Latin, Cyrillic and both maintained Chinese localizations without relying on operating-system fonts. The matching SIL Open Font License is in `Assets/Resources/ThirdPartAllLincnse/NotoSansCJK.LICENSE.txt` and appears in the in-game credits. SHA-256: `2c76254f6fc379fddfce0a7e84fb5385bb135d3e399294f6eeb6680d0365b74b`.

## Side foliage source

Fern.png was generated with imagegen on 2026-10-25, unchanged RGBA 1024 × 1536.
Unity imports it with alpha coverage mipmaps and a 0.55 cutout threshold; the
source has soft partial-alpha edges. It is used on three crossed fronds outside
platform walls, with no colliders. Placement remains a draft pending level review.

SHA-256: `781552b4dc58aee68322b97f6e33bd8a39c05db2f558cdc212be88edfdc3f4a6`.

Prompt: Use case: photorealistic-natural. Asset type: alpha-cutout foliage texture for side vegetation on floating platforms in a Unity game. One single lush natural tropical fern frond, upright, viewed directly from the front, entire frond fully visible with a slim stem reaching the bottom center, many delicate alternating pointed leaflets, fresh medium and dark green hues with subtle vein detail. Realistic botanical shape, isolated on genuinely transparent background. Frond fills a tall portrait composition with modest transparent padding on every side. Soft neutral uniform illumination, no cast shadow, no ground, no pot, no room, no text, no border, no glow, no scenery. Intended for layered crossed foliage cards, with lighting applied by a 3D shader. Preserve crisp natural leaflet edges and transparent gaps.

## Weather covering textures

Generated with the built-in imagegen tool on 2026-10-25. Files are copied unchanged.
The shader maps detail within existing contact coverage; these maps do not change region boundaries.

### Snow

- File: `Assets/Game/Remaster/Resources/Remaster/Snow.png`
- Source: `/Users/iwonz/.codex/generated_images/01a172ec-7ce9-7372-b9f1-2aeb73d82ab8/exec-32ee3701-164f-4863-bbbc-0f66315c22bf.png`
- Actual dimensions: 1254 × 1254
- SHA-256: `d7afff5a317005589a6ec636a70d4176327edd62bca9236cb90baf10c5830734`

Prompt:

> Use case: photorealistic-natural. Asset type: seamless square PBR base-color texture for the snow covering on stone platforms in Ballance Remaster. Generate only the texture, orthographic directly overhead, edge-to-edge surface, no object or scene. Fine clean compacted alpine snow with irregular tiny powder grains and subtle natural off-white / cool white variation. Uniform scale, approximately one square meter, detailed but restrained. Flat neutral diffuse illumination, no directional shadows, no baked highlights, no vignette, no perspective, no footprints, no borders, no text, no watermark. Seamless repeating edges on both axes. Requested resolution 2048x2048.

### Ice

- File: `Assets/Game/Remaster/Resources/Remaster/Ice.png`
- Source: `/Users/iwonz/.codex/generated_images/01a172ec-7ce9-7372-b9f1-2aeb73d82ab8/exec-9b126a4a-b8b1-469e-8a14-8d17e8a5c822.png`
- Actual dimensions: 1254 × 1254
- SHA-256: `37d7cf84688e7a32225d43f2bcbc7c63601a01bb4063c4ae1014ba4f0fcce387`

Prompt:

> Use case: photorealistic-natural. Asset type: seamless square PBR base-color texture for shallow ice patches on stone platforms in Ballance Remaster. Generate only the texture, orthographic directly overhead, edge-to-edge surface, no object or scene. Pale blue-grey translucent frozen water with delicate irregular hairline fracture networks, tiny trapped frost bubbles and subtle milky depth variation. Uniform scale, approximately one square meter. Flat neutral diffuse illumination, no reflected scenery, no specular highlights, no directional shadow, no vignette, no perspective, no borders, no text, no watermark. Seamless repeating edges on both axes. Requested resolution 2048x2048.

### Sand

- File: `Assets/Game/Remaster/Resources/Remaster/Sand.png`
- Source: `/Users/iwonz/.codex/generated_images/01a172ec-7ce9-7372-b9f1-2aeb73d82ab8/exec-fc6c7f72-f6b2-47d1-85fe-ae1b0a53a4d3.png`
- Actual dimensions: 1254 × 1254
- SHA-256: `076e103961ebf982d0bb50913e4a7916d741fdadb5881db69f102a3ed7e76123`

Prompt:

> Use case: photorealistic-natural. Asset type: seamless square PBR base-color texture for fine sand deposits on stone platforms in Ballance Remaster. Generate only the texture, orthographic directly overhead, edge-to-edge surface, no object or scene. Natural fine golden-beige desert sand with a little darker mineral grain, restrained irregular shallow ripple variation, detailed tiny grains, approximately one square meter. Dry warm sandstone palette without strong orange. Flat neutral diffuse illumination, no directional shadows, no baked highlights, no dunes or landscape, no footprints, no vignette, no perspective, no borders, no text, no watermark. Seamless repeating edges on both axes. Requested resolution 2048x2048.

### Moss

- File: `Assets/Game/Remaster/Resources/Remaster/Moss.png`
- Source: `/Users/iwonz/.codex/generated_images/01a172ec-7ce9-7372-b9f1-2aeb73d82ab8/exec-969a5a39-87e0-4ad3-9006-417432d9ac7b.png`
- Actual dimensions: 1254 × 1254
- SHA-256: `afb1fae8c43daa9a30b225ff1c158be572683ad7230db4e0f068106529966d81`

Prompt:

> Use case: photorealistic-natural. Asset type: seamless square PBR base-color texture for low tropical moss covering stone platforms in Ballance Remaster. Generate only the texture, orthographic directly overhead, edge-to-edge surface, no object or scene. Dense fine short moss with natural olive and muted forest-green tiny leaflets, small irregular earthy gaps, varied botanical texture at approximately one square meter. Low groundcover, no tall grass or large leaves. Flat neutral diffuse illumination, no directional shadows, no baked highlights, no glossy reflections, no vignette, no perspective, no borders, no text, no watermark. Seamless repeating edges on both axes. Requested resolution 2048x2048.

## Remaster title and cloud reveal

Generated with imagegen on 2026-10-25 and copied unchanged. These are interface illustrations, not evidence of in-game graphical quality. `RemasterIntro` reveals the title with two mirrored drifting cloud layers; `RemasterAbout` reuses the title with its original aspect ratio.

| Asset | Actual size | SHA-256 |
| --- | --- | --- |
| TitleArtwork.png | 1672 × 941 RGB | a7d8ca367f13e53593568ee13ffacc41aa9269898727a46194f1b5f5f9e90ee5 |
| IntroCloud.png | 1536 × 1024 RGBA | e7e57024f4f2e9c09fd10cb9af265f90b5a408369591dfb7b54b2be7197788ba |

### Title prompt

Create a premium game title illustration for a fan-made Ballance Remaster startup screen. Wide 16:9 landscape, ideally 2560x1440 or larger. A breathtaking serene world suspended above deep layers of ivory cumulus clouds, pale blue atmosphere, subtle warm late-morning sunlight. In the lower third, a highly detailed wooden ball made of curved honey oak panels rests on a narrow weathered cream sandstone track, with a wooden bridge and elegant twin steel rails curving toward a small floating round platform and a tiny lantern on the right. Absolutely no buildings, no city, no characters. Strong cinematic depth, refined physically plausible material detail, tasteful atmospheric haze. Composition must have a clear quiet central upper region for the title. Render the exact title text 'BALLANCE' in large refined widely spaced ivory-white uppercase letters with a restrained dark shadow for readability, and 'REMASTER' directly beneath in smaller widely spaced uppercase, perfectly legible and centered. The typography is elegant contemporary game branding, not a neon science fiction logo. No additional words, studio names, logos, credits, watermarks or UI. The outer left and right edges have softly lit clouds that frame the scene; title and ball remain within the central 70% so the artwork can fit varying screens. This is standalone title-screen artwork, not a screenshot or mockup: no monitor frame or interface.

### Cloud prompt

A single premium soft volumetric ivory-white cloud bank for an animated game intro foreground overlay. Wide horizontal fluffy cloud mass, luminous subtle warm edges, cool pale blue shaded interior, many softly feathered translucent wisps around every edge. Viewed face-on as if floating among clouds in bright daylight. The cloud occupies the middle and lower portion with a natural irregular billowing silhouette. Isolated on an actual fully transparent background with smoothly feathered alpha edges; no sky, ground, horizon, text, logos, black or white rectangular backdrop. Suitable for slowly drifting sideways to reveal the game title behind it. Photorealistic atmospheric cloud, calm elegant cinematic softness. Landscape 1536x1024.

## Reproducible relief pipeline

`Ballance2.Editor.RemasterReliefAuthoring.Generate` uses the unchanged imagegen albedos plus deterministic periodic material patterns to author 1024 × 1024 packed relief maps. RG stores tangent-space normal XY, B stores height, and A stores roughness. They are imported as linear data with repeat wrapping, mipmaps and anisotropic filtering. The surface shader reconstructs normal Z explicitly; these packed maps must not use Unity's normal-map importer, which would reinterpret their other channels. Height drives view-dependent parallax, and roughness drives the Standard BRDF. Coverage normals are transformed from the same local support coordinates used by the physics regions.

These are synthetic art-authored maps, not scanned depth. The authoring pass checks exact edge continuity and non-flat height variation. Moving-light and oblique-angle previews in `Builds/checks/remaster-art` still need in-game comparison for texel scale, seams, strength and readability. No generated map changes collision geometry.

The same entry point creates a deterministic 128³ density volume with mipmaps. Billow centers and radii vary inside the source. `LayeredSky` distorts this volume with continuous world-space cloud groups, variable deck height and independently rotated detail, removing the short repeated rows. Sampling footprints select filtered density levels for distant rays. The renderer integrates density-dependent extinction, directional shadow samples and separate ambient/sun lighting. Lower quality simplifies detail and lighting and lowers the sky capture resolution; weather timing and physics are unchanged. Stock skies A–M, including menu day/night, share this renderer while retaining their authored palettes.

`RemasterSkyRenderer` captures only the sky into two bounded cubemaps (128 pixels per face at Low, 256 at Medium/High). After initialization, it updates one face per frame from a fixed weather/position snapshot and blends completed captures over 0.18 seconds. Camera rotation samples the cached sky immediately; movement, weather and quality changes request refreshed captures. It restores the source sky after each render and releases its camera, materials and textures on disable. UI and track rendering keep their independent resolution. Reflections and explicit custom skies retain their own paths. Compare direct and cached renders and measure frame timing in a player; editor previews alone do not establish performance.

Weather coverings use deterministic local patches with irregular, feathered boundaries shared by the surface shader and native contact sampler. The contact/mesh normal selects their projection plane even on thick or rotating supports. Texture detail does not move that physical boundary. Inspect the covering previews on sandstone and actual levels; draft placements still need all-level playthrough acceptance.

`RemasterArtChecks.RenderClouds` regenerates the volume and captures seven presets, all stock palettes, multiple headings, a motion sample and a camera translation by the old texture period. The translated image must differ from the original; inspect the images as well because this assertion alone cannot detect every repetitive composition. The generated title artwork remains an artistic target, not a substitute for runtime visual acceptance or performance measurements.

## Reballance title rename

The final product name is Reballance. The built-in imagegen editor replaced the former two-line title in `Assets/Game/Remaster/Resources/Remaster/TitleArtwork.png`, retaining its GUID and scene composition. The output is copied unchanged into the project. Historical prompts above describe the earlier authoring pass.

Prompt:

> Use case: text-localization. Edit target: supplied game title illustration. Replace ONLY the two-line text 'BALLANCE REMASTER' with one centered elegant word 'REBALLANCE', spelled R E B A L L A N C E. Retain the refined widely spaced ivory-white serif lettering and subtle shadow, centered in the quiet upper blue sky. No subtitle, no other text. Preserve the exact wooden ball, stone track, rails, lantern, camera, soft dense cloud scenery, lighting, colors and wide 16:9 composition. This is the final startup artwork for the game Reballance. Keep the full image, do not crop or redesign the scene.

SHA-256: `6553c0b7fe02fc7c3011e3f397f5f95c366cb91320d791b355ac8c0f7c8edd45`.

## Interface icons and flags

`Assets/Game/Remaster/Resources/Remaster/InterfaceIcons.png` was generated with the
built-in imagegen tool on 2026-10-25. Source artifact:
`exec-1e4bcfb9-d29d-4cc5-906b-c23914ab1ddc.png`. The image is a transparent 4x4 atlas;
`RemasterIcons` slices its original cells without editing the generated pixels.
It supplies package/modification icons, generic packages, score/lives and controls.

Mode: new generation with transparent background. Requested 2048 square; returned
1280 x 1280. No CLI fallback or external icon package was used.

Exact prompt:

> Use case: logo-brand. Asset type: ONE production UI icon atlas for the Unity game Reballance, transparent PNG, square 2048 x 2048 if possible. Create exactly sixteen icons in a mathematically regular 4 by 4 grid, equal square cells, no grid lines. Every icon is centered in its cell and occupies the same 60 percent of cell width/height, leaving wide empty transparent margins. Unified premium minimal 2D line-icon style: thick smooth rounded strokes, pale mint #B8DECF with restrained ivory highlights, a very thin dark teal outer keyline for contrast over both pale clouds and dark panels. Flat crisp vector-like silhouette; no 3D rendering, no photorealism, no drop shadows, no glow, no square tiles, no backplates, no backgrounds, no letters/numbers/captions. Meaning must read at 24–48 pixels. Top row left to right: (1) segmented spherical ball, game core; (2) fluffy cloud, sky resources; (3) loudspeaker with two sound arcs; (4) two joined music notes. Second row left to right: (5) heart containing an infinity symbol, unlimited lives modification; (6) circular clockwise restart arrow; (7) two right-pointing chevrons, speed modification; (8) a simple faceted diamond gemstone, score. Third row left to right: (9) simple heart outline, lives; (10) single right-pointing chevron; (11) plus sign; (12) minus sign. Bottom row left to right: (13) checkmark; (14) simple six-tooth settings gear; (15) isometric package cube; (16) X cross. Each icon completely separate, no overlaps across cells. All 16 icons must look like one carefully designed consistent family. Genuine transparent alpha background.

The four language flags under `Resources/Remaster/Flags` are original small raster
drawings made with deterministic geometric code, preserving recognizable national
colors and symbols. They are separate sprites so emoji font availability cannot
change language selection. No external flag assets or license dependency was added.

The touch-control pass edited the atlas with imagegen, replacing the unused gear
cell with a matching eye for camera/look controls. Updated source:
`exec-13a591cb-7382-4a83-9945-b1cb84572451.png`; the original metadata/GUID is retained.

Edit prompt:

> Edit this transparent Reballance 4 by 4 UI icon atlas. Make exactly one substitution: in the bottom row, second column, replace the six-tooth gear icon with a simple open eye icon (almond outline and circular pupil), representing camera/look controls. Match the existing pale mint and ivory rounded stroke family and thin dark teal keyline, the exact optical size, centered placement, and transparent background. Keep the other fifteen icons completely unchanged in their exact cells and positions, including color, shapes and crispness. Do not resize, crop, add text, add shadows, add a backdrop, or shift any icons. Preserve the square 1280 x 1280 atlas and exact equal 4x4 cell grid. The replacement eye must be clearly readable at small UI sizes.
