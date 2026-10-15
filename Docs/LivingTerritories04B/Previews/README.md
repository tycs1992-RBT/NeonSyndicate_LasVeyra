# Offline source visual evidence

These are Blender renders of actual repository geometry and authored runtime layout data. They are **not Unity screenshots**. Every final image states that limitation in its footer. No live AI, gameplay, collision, import, frame rate or renderer parity is demonstrated here.

The 19 views use 1440x900 output and 48 CPU Cycles samples. Ground views inspect the actual local layout and static body poses; flight views inspect composition and placement candidates.

| Review | Images |
| --- | --- |
| Matched Hearthspire court | [Before](hearth_court_before.png), [after](hearth_court_after.png), [night](hearth_court_night.png) |
| Amethyst states and composition | [Unowned](relay_amethyst_unowned.png), [owned](relay_amethyst_owned.png), [fortified](relay_amethyst_fortified.png), [flight](relay_amethyst_flight.png) |
| Other regional hubs | [Verdant](relay_verdant_ground.png), [Obsidian day](relay_obsidian_ground.png), [Obsidian night](relay_obsidian_night.png) |
| Actual existing actor source | [Staff detail](relay_amethyst_staff.png), [quartz drake detail](habitat_sanctuary_detail.png) |
| Sanctuary composition | [Ground](habitat_sanctuary_ground.png), [flight](habitat_sanctuary_flight.png) |
| Other habitat compositions | [Canopy](habitat_canopy_flight.png), [rift](habitat_rift_ground.png), [transition](habitat_transition_ground.png) |
| Actual city with provisional candidates | [Before](city_flight_before.png), [after](city_flight_after.png) |

## What is represented

The relay and habitat compositions read the exact `Resources/LivingTerritories04B/Layouts/*.json` files used by the runtime. Both 04A and 04B meshes are decoded directly from their CMH1 payloads, retaining their UVs and normals. The renderer follows ownership, fortification and deterministic variant filters. Paving dimensions, elevations and UVs match `TerritoryArt04BView.BuildPaving`.

The actor exporter executes the existing `CreatureBuilder`, `CreaturePose`, `BodyBuilder` and `HumanoidPose` C# sources. Explicit arithmetic adapters replace basic vector/data types, and a recorder skins the actual bone weights against a static solved pose. This is source geometry extraction, not a Unity compile test or simulation of the engine. It does not replace the real creatures in the game with static display meshes.

Habitat species, locations, orientation and seeded looks come from the shipping habitat JSON and the exact `TerritoryEcologyRules04B.Seed` helper. Hub staff use the exact `StaffLook`, `StaffOffset` and `StaffYaw` helpers and actual catalog IDs. The review uses a fixed two-second idle or flying pose. It cannot predict the runtime's random aerial orbit phase, creature motion, population scheduling, grounded spawn adjustment or visibility. No new imported animation clips are implied.

**Actor LOD difference:** new staff and wildlife are extracted at source builder detail 2 for inspection. The runtime uses ambient detail 1 for staff and ordinary wildlife on Medium/High/Ultra, detail 0 on Low, and always detail 0 for glass mites. The Quartz Drake GreatBeast uses detail 2 on High/Ultra, matching this inspection body. The original 04A decorative drake remains detail 1. Several inspection bodies therefore contain more geometry than their runtime ambient versions. These are real source meshes at a disclosed LOD, not proof of native visual parity; requests and the manifest record this distinction.

Character and creature palette values come from `CharacterPaletteData`, with the actual `CrystalDrakeSurface` palette overrides for the quartz drake. Blender's material response approximates the game shader. The game's procedural fabric/scale detail shader, eye effects and other renderer-specific effects are not reproduced.

## Matched comparisons

`hearth_court_before` and `hearth_court_after` use the same 04A sanctuary court, camera, lighting, resolution and exposure. The original cosmetic quartz drake remains present in both, at its source position and seed. The after view adds the actual home ledger composition, one steward, the care resident and a stated example of three owned-relay crystals. This depicts a source ledger state, not a captured gameplay result.

The home display uses the runtime's proposed footer offset `(0, 0, -28)` relative to the existing court. **The native mini-survey has not run.** The runtime may skip this display if the surrounding road, terrain or interaction clearances fail. The source view does not establish a live world location for Hearthspire's court.

The Amethyst unowned, owned and fortified views use one camera. They show the actual state-filtered nodes, including three ownership crystals and the two finite tiers of added cover. They do not prove the capture encounter or save state works in Unity.

The city-flight pair uses the actual 04A architecture export from `CityLayout` seed 61724. The after view adds the new layouts at the first candidates passing the actual source-only placement exclusions. Candidate coordinates come from `Validation/placement_candidates.json`. **Native ground, Physics, existing installed court locations and actual live placement remain unverified.** Roads, terrain, live actors and the capped high-detail city landmark layer are omitted from these city views. The camera intentionally frames the western city edge and its new territory candidates, rather than presenting a conceptual replacement city.

## Review and refinement

Early source renders exposed sparse understory and bare soil between paths. The territory layouts gained worked ground quadrants, layered fern/reed planting and mineral-water accents while retaining their approach and actor clearances. The Amethyst conversation space gained supported arcades, columns, roof ribs and an actual roof. Freestanding relay banners gained grounded masts. The home ownership crystals initially intersected the arch; their row moved forward and now fills from the center outward. Inherited portal shoulder caps had a visible support gap; the affected 04B layouts gained existing columns reaching their exact undersides. The canopy's suspended ring is intentional magical flight art. Review cameras were adjusted where foreground trunks hid the habitat or the home steward fell outside the frame.

The plain ground, directional light, moonlight, exposure and camera are a review rig retained from 04A. Night hub accents approximate the existing bounded runtime light position and color, with a documented offline intensity. Neither Blender's lighting nor its render time is a Unity performance result. Manifest triangle totals describe each offline scene, including review ground and static actor copies; they are not a native world budget. Sampling noise remains visible because this Blender build has no OpenImageDenoise.

## Reproduce

Requirements: Blender 4.x, Python with Pillow and NumPy, and a .NET 8 SDK. The recorder uses direct Roslyn compilation and requires no package restore. The complete set includes city views, so create their temporary source export before `--views all`. From the repository root:

```sh
python3 tools/render_living_territories_04b.py --export-actors --dotnet /path/to/dotnet
python3 tools/run_city_coverage_04a.py --dotnet /path/to/dotnet --geometry --whole-city /tmp/vr04a-world
python3 tools/render_living_territories_04b.py --views all --city-dir /tmp/vr04a-world --samples 48 --width 1440 --height 900
python3 tools/validate_living_territories_04b_previews.py --city-dir /tmp/vr04a-world
```

The actor export records source hashes, request parameters, bone transforms, palettes and source mesh snapshots under `ActorSource`. It does not write any Unity scene, save or gameplay resource. The generated actor snapshots are review artifacts; the C# builders remain their authoritative editable sources.

To regenerate just the city pair after the source export exists:

```sh
python3 tools/render_living_territories_04b.py --views city_flight_before,city_flight_after --city-dir /tmp/vr04a-world --samples 48 --width 1440 --height 900
```

Refresh the provisional candidate report before rendering a changed placement implementation:

```sh
python3 tools/run_living_territories_04b_placement.py --dotnet /path/to/dotnet --catalog Assets/NeonSyndicate/Resources/LivingTerritories04B/territories_catalog.json --report Docs/LivingTerritories04B/Validation/placement_candidates.json
```

Use a local temporary directory on Windows. `--output-dir` selects a scratch review directory; `--threads` controls bounded CPU workers; `--owned-count` selects the illustrated home ledger count. The default is three owned relays, not a claim about an actual save. `--resource-claimed` removes the one-time resource overlay using the same node flag as the runtime. Individual views can be selected as a comma-separated list. `--label-only` refreshes the explicit offline footers without changing scene pixels above the footer; the final material disclaimer was applied this way, with the unchanged scene-generation function hash retained.

`preview_manifest.json` records each camera, state, actor request, layout hash, source geometry hashes and material dependencies. `preview_validation.json` checks those hashes, all required images, the 26 exported actors, exact state-filtered node counts, matched cameras and footer backgrounds. This is evidence integrity, not an engine test. Native Unity acceptance remains the required next step in [04B_QA.md](../../../04B_QA.md).
