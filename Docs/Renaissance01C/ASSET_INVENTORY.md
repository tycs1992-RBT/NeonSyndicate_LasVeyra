# Visual Renaissance 01C asset inventory

This is the current inventory and delta from the completed 01B foundation. Earlier inventories remain in `Docs/ArtDistrict` and `Docs/ArtDistrict01B`. Source-generated artwork is supplied; production hero sculpts and authored animation packs are not.

| Group | Current delivered state | 01C change |
| --- | --- | --- |
| Modular architecture | 24 OBJ modules in `Assets/NeonSyndicate/Resources/ArtDistrict/Models`, 9,422 unique source triangles | Revised `arch_frame`, `pilaster`, `dome` and `crystal_shard`; added 144-triangle `arch_frame_lod`. All 23 prior model names and GUIDs retained; 19 prior models unchanged. |
| Architecture placement | Existing Asterfall plus bounded selections near existing home and clinic POIs | Larger pointed registers, mineral supports/fins and crowns; maximum four home and two clinic frontages; original building envelopes/collision path retained. |
| Texture library | 32 PNG maps in `Resources/ArtDistrict/Textures`: ten albedo/normal/mask sets, crystal pattern and mote | Retained 01B maps and importer settings. No new reference-derived game textures. |
| Materials and shaders | Existing stone/basalt/bronze/cyan/amethyst/amber library, opaque crystal shader and mote shader | Retained 01B library; mineral paving applied to nearby existing starter road strips. |
| Local atmosphere | Existing bounded district atmosphere and practical light rig | Same treatment follows resolved home/clinic anchors as well as the original promenade. |
| Drake art | Existing QuartzRegalia model/rig/pose solver and 01B shared pearl/cyan surface shaders | Retained. No new hero sculpt, rig or authored animation clips in 01C. |
| Habitat creatures | Existing Neon Jackal, Crystal Basilisk and Rime Griffin; smaller Frost Stalker placement fallback | New calm placement, observation/bestiary interaction and optional practice encounter use the existing director, body jobs, rigs, poses and combat. Three residents plus one temporary trainer at most. |
| Gameplay assets | Existing scene setup, city generator, roads, vehicle roster, story/quest/event data and input system | Preserved foundation with scoped source changes listed in `EXACT_CHANGES.md`. No new external Unity package. |
| Comparison evidence | Twelve official reference images remain documented by the 01B provenance manifest; four full reference images appear in the 01C report | New source-placement facade comparison. These are documentation, not in-game assets or Unity-rendered evidence. |

The architecture generation and offline preview scripts are included under `tools`. Runtime-generated material and creature sources remain under `Assets/NeonSyndicate/Scripts/Runtime`. No paid or licensed production art package is claimed as included. See `REMAINING_PRODUCTION_ASSETS.md` for the commissioning brief and `QA_REPORT.md` for unperformed Unity checks.
