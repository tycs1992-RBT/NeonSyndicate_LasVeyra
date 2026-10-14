# Asterfall Promenade: art direction

## Intent

A warm mineral city built around a living crystal infrastructure. Sandstone carries the architecture, bronze provides craft and age, deep teal glazing and ceramic cool the shaded areas, and cyan/amber crystal seams carry the supernatural identity. The goal is a coherent urban-fantasy district that can be judged in motion before extending the style to the world.

## Visual grammar

- Primary forms: massive sandstone buildings, recessed arched bays, stepped cornices, slender fluted pilasters and ceramic domes.
- Secondary forms: bronze diamond lattice, inset glazing, balconies, pendant lanterns, scalloped fabric canopies and crenellated parapets.
- Tertiary forms: modeled stone joints, bevelled metal, dentils, surface-grain normal maps, mineral veins, crystal facets and sparse floating motes.
- Nature: sculptural branching trees with modeled opaque leaves, held in rooftop planters. This is an initial forest vocabulary, not a new forest biome.
- Creatures: pearl and quartz armour follow the existing dragon anatomy. Detail reinforces the jaw, throat, back, tail and wing joints while preserving the original gameplay silhouette and weak-point logic.

## Palette and material behavior

| Role | Direction | Surface |
|---|---|---|
| Sandstone | warm ochre, approximately #A3754D | broad rough mineral grain, normal detail |
| Basalt | blue charcoal, approximately #1D252B | dark rough stone |
| Bronze | aged honey, approximately #82521F | metallic edge definition, fine surface variation |
| Ceramic | deep petrol teal, approximately #1F5254 | glazed, gently mottled |
| Crystal | cyan with restrained amber counterpoints | opaque faceted body, emissive veins and grazing-angle rim |
| Fabric | dusty plum | woven normal detail, low shine |
| Nature | desaturated green over dark bark | opaque leaves to control overdraw |

Color values describe the material direction, not exact final rendered pixels. Tone mapping, weather and existing lighting affect the image. Glass deliberately uses opaque glazing: there are no fake transparent interiors or costly refraction grabs.

## Lighting and presentation

The original 24-hour sky, weather, shadows, post-processing and realm transitions remain authoritative. A geographically blended atmosphere accent adds restrained haze and warm/cool ambient variation around this street. The effect fades out with distance/altitude, bad weather, Frostline and the Veil. Up to four nonshadow facade lights complement the emissive materials, using zero on Low and at most two on Medium.

Do not increase bloom until windows and crystal facets lose their shape. Gameplay should read in clear noon, dusk, night and bad weather, not only in a posed sunset view. Do not add full-screen particles, UI panels, screen distortion or a fixed cinematic camera during normal play.

## Desktop budgets and next review

Targets, not measured performance: 1080p at 60 fps on an agreed desktop test machine. Compare baseline and district using the same seed, time, weather, camera path and graphics preset. Record CPU/GPU frame time, draw calls, triangles, allocations, memory and boot time before accepting a performance claim.

The implemented controls are per-building material merging, two LOD levels, 512px mipmapped maps, released CPU data on combined render meshes, shared materials, no decoration colliders, at most 288 ambient mote particles, and distance/quality-controlled accent lights. The source OBJ meshes remain readable because runtime combining needs their CPU data. Dynamic world generation has no new baked lightmaps, light probes, occlusion bake or authored navigation bake.

A final cinematic pass still needs in-engine look development, higher-end hero sculpt/textures and measured GPU tuning. Do not scale this district across the whole map until the Unity QA gates pass.
