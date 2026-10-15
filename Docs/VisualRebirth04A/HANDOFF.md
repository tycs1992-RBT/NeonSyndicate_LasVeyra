# Visual Rebirth 04A implementation handoff

A substantial original art and world-generation pass is implemented on `feature/visual-rebirth-04a`. It starts at the live `art/crystal-fantasy-block-proof` tip **8af8559fa646672d94f2c27ea04e3e2e0a1728cf**. The existing Unity project, editor pin, saves, flight controls, crystal progression, fort ownership and construction interfaces are retained.

**This is reviewable implementation, not a production-readiness claim.** Unity 6000.3.16f1 is unavailable in the cloud machine. Native Unity compilation, import, gameplay, real in-game screenshots and performance measurements are **NOT RUN**. The images are actual Blender renders of shipping geometry and layouts, labeled offline previews. Supplementary compiler diagnostics and geometric checks do not replace the Windows acceptance run.

[Feature branch](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/tree/feature/visual-rebirth-04a) | [Review against the art branch](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/compare/art/crystal-fantasy-block-proof...feature/visual-rebirth-04a?expand=1)

Implementation commits: [original source kit](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/commit/f4ab885), [world integration](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/commit/f4d0dd1), [render-driven structure and city refinement](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/commit/1fe6505), and [completed sanctuary planting](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/commit/c671570). The final evidence commit contains this handoff, reproducible checks and labeled previews. No commit is merged into `main`.

The delivery changes 759 files against that base: 748 additions, 11 modifications and zero deletions. This includes 326 metadata files, 18 new Unity C# files and ten narrowly edited existing C# integration hosts. The complete path list is in the delivery inventory below; most added files are art payloads, their metadata or review evidence.

## What is implemented

| Area | Delivered behavior |
|---|---|
| Original kit | 47 distinct masters, 94 high/low mesh variants, matching editable OBJ sources, deterministic generator, import metadata and bounds/hash inventory |
| Surface library | 12 authored families, 48 albedo/normal/mask/emission textures and 56 palette/material variants, native Built-in/URP Lit material selection |
| Hearthspire | New layered gate, stone arcades, courtyard pavilion, observatory, planted edges, two stairs and a connected 3m/6m promenade. Six original construction sockets and original physical decks retained |
| Galehook | Living mineral garden fort with monumental trunk/canopy forms, root arches, hanging vegetation and a distinct green/stone silhouette |
| Sunscar | Fractured basalt fort, scorched-iron extraction machinery, angular towers, suspended mineral components and an aligned gantry. Unowned visual identity remains distinct; ownership still uses saved themes |
| City architecture | Four regional styles and five deterministic variations per archetype, deep arches, layered wall surfaces, shop thresholds, perimeter towers/spires and lower-cost distant architecture. Existing collision and roof equipment retained; helipad roof ornaments excluded |
| Neighborhood courts | Up to three separately surveyed Hearthspire plots: Rootwater Sanctuary, Lantern Court and Fracture Survey Garden. Up to one scenic court by each rival. Nine-metre cross paths, vegetation, fountain/workshop/ore scenery and navigation markers |
| Population and ambience | Bounded decorative drake roosts using existing creature art, pollen, moving rings/vines and nighttime accents. Existing interactive habitat, pedestrian and creature systems continue unchanged |
| QA and fallback | Independent editor switches, runtime coverage logging, real Unity look-test capture command, import/geometry/route tests and retained previous-art fallback |

The surrounding courts are scenic, not new capture sites or resource rewards. Their actual placement must pass the original fort survey for roads, POIs, pickups, slope and occupied space. A failed survey logs an exclusion and leaves the existing world intact. New rival art introduces no raid collision. No additional hostile spawn system, faction gameplay, social AI, save block or quest is introduced.

## Where to look

The fort coordinates below are the existing preferred anchors, not a promise that the live safety survey chooses the first candidate. The STRONGHOLDS page, existing K/R markers and runtime log identify actual placement.

| Destination | Preferred world anchor | Inspection route |
|---|---|---|
| Veyra Lofts arrival | approximately (-9.5, 0.15, -65.73) for seed 61724 | Inspect nearby transformed facades at street level, then fly above their perimeter roof silhouettes |
| Hearthspire Keep | (1120, 0, -740) | South gate, unchanged crystal core, west stairs, 3m walk, second stair, 6m rear overlook and open pavilion; inspect all six sockets |
| Galehook Redoubt | (-1180, 0, -710) | Rooted approach, side gardens and aerial canopy profile; optional wardens retain the old arena |
| Sunscar Bastion | (1190, 0, 450) | Angular entry, extraction rig and rear gantry; compare its dark stone and iron with the other forts |
| Surrounding scenic courts | Surveyed offsets about 78 to 166m around Hearthspire, 156 to 244m around rivals | G markers and `Visual Rebirth 04A` log lines provide exact coordinates; gardens have broad cross paths |

Visitor route: Hearthspire gate and overlook, the nearest G-marked sanctuary and its decorative drake roost, Lantern Court, a district landmark toward the existing city, then a Fracture Survey Garden. Existing discovery shrine rewards remain controlled by 02G. The route through the live terrain is not playtested here.

## Coverage and budgets

The actual unchanged `CityLayout` generator and new planner were executed for seed 61724 through an explicitly documented managed math adapter. This produced 181 blocks and 1,323 buildings: **1,319 eligible**, four custom-authored exclusions, and 24 protected helipad roofs. The runtime logs `VISUAL04A_COVERAGE` rows by district and archetype, including successful detailed/ordinary treatment, fallback, exclusions and variant masks. Planned eligibility does not prove that all art loaded in Unity.

The actual ordinary city architecture source produced **4,331,028 triangles** for those eligible structures in the offline extraction, with a maximum of 14,646 on one large three-tier building. All 1,319 buildings passed finite-vertex, triangle-area, smooth-normal, facade-envelope and central roof-landing checks. The 558 planned ordinary trees use 757,672 replacement triangles in total. These figures exclude higher-detail selected buildings, roads, retained rooftop equipment and other existing game content. They are geometry budgets, not frame-rate measurements. Source mesh library totals are 81,568 triangles across all high masters and 23,206 across all low masters, with 10,059,056 bytes of runtime mesh payloads. These shared sources are reused rather than recreated for each object.

Fort layouts contain 456 base decorative parts in total, plus ornaments selected from the existing saved construction modules. Hearthspire adds 46 deliberate route colliders; rivals add zero. Final batches are grouped by material, split before exceeding 180,000 vertices, and release output CPU mesh data. Sources remain readable for combination and the small set of matching static garden colliders. Low retains cheap architectural silhouettes and static ornaments; added pollen and local lights are disabled. Ambience has one scheduler and fixed caps.

The runtime marker is `Crystal Manor Saga / Visual Rebirth 04A`. The console reports actual fort/court installation and source/batch counts. The Unity capture manifest records available draw calls, triangles, memory and frame samples when run locally. Cloud startup time, game memory, real draw calls and frame rates are not measured.

## Evidence and known limits

Read [QA_REPORT.md](QA_REPORT.md) for the complete evidence and [Previews/README.md](Previews/README.md) for exact rendering provenance. The machine-readable audit and supplemental compiler logs are in `Validation/`. The art bible is [ART_BIBLE.md](ART_BIBLE.md), route details are in [BUILD_04A_STRONGHOLDS.md](../BUILD_04A_STRONGHOLDS.md), and the archetype-by-archetype city account is [CITY_DISTRIBUTION.md](CITY_DISTRIBUTION.md).

| Offline inspection | Source render |
|---|---|
| Matched Hearthspire baseline and result | [Before](Previews/hearthspire_before.png), [after](Previews/hearthspire_flight.png) |
| Entry, raised garden and night | [Entry](Previews/hearthspire_ground.png), [garden](Previews/hearthspire_garden.png), [night](Previews/hearthspire_night.png) |
| Rival faction silhouettes | [Galehook](Previews/galehook_flight.png), [Sunscar](Previews/sunscar_flight.png) |
| Surrounding sanctuary | [Ground](Previews/sanctuary_ground.png), [flight](Previews/sanctuary_flight.png) |
| Actual city parcel samples | [Spawn](Previews/city_spawn.png), [verdant](Previews/city_verdant.png), [dawnstone](Previews/city_dawnstone.png), [obsidian](Previews/city_obsidian.png) |
| Full generated city architecture | [Overview](Previews/city_all.png), [closer flight](Previews/city_all_flight.png); buildings only, with no native world rendering |
| Original model catalog | [Kit](Previews/kit_sheet.png), [tree species](Previews/tree_lineup.png) |

There are 19 labeled offline images and a packed, editable [Blender gallery](Previews/visual_rebirth_04a_gallery.blend). The gallery is inspection source outside `Assets`, not a replacement Unity scene. [DELIVERY_INVENTORY.json](DELIVERY_INVENTORY.json) records exact file-change counts and the delivered asset categories.

- The asset/spatial audit passes 1,889 checks: actual triangle geometry, normals, UVs, high/low correspondence, metadata, texture import settings, original source/assets/GUID preservation, entrance clearance, route continuity, four-corner vault support and visible-surface/collider alignment.
- The supplementary C# 9 compiler comparison checks 391 runtime/test source files against genuine older Unity 2021.3 reference assemblies. It finds zero introduced errors relative to the base, which already has 35 API-version errors with those older references. This produces no successful Unity 6 build and executes no NUnit tests.
- A separate Roslyn syntax scan passes twelve input/renderer/editor-player preprocessor configurations, including the Editor menus. This is syntax parsing, not twelve successful Unity compilations or type checks.
- Existing 02E, 02F, 02G and 02I offline audits pass. The old 03A2 audit passes 73/74; its frozen exact-source expectation predates the new optional fort hooks. The new preservation audit explicitly checks those hooks and recovers the unchanged original host content.
- All 296 current Unity/delivery LFS objects pass integrity checks. The repository-wide cache check was incomplete for 87 older, unrelated archive/browser/preview objects that were not downloaded; their tracked pointers are unchanged.
- Native Unity import/compile, all EditMode tests, all twelve input/renderer configurations, old-save load, walking/driving/flying/landing, controller use, pause/resume, repeated Play sessions, discovery rewards, fort claim/customization and rival raids remain unrun.
- Default/Low/High runtime culling, frame rate and final lighting are unverified. There are no real in-game before/after images in this delivery.

Rendered review led to source corrections: pavilion ribs now meet their four corner supports without hanging piers; canopy geometry is fuller; Sunscar uses scorched iron; its rear gantry spans the actual supports; stepped terrace collision follows the clipped deck; garden benches and roots clear their paths. Ordinary city buildings received a second pass with smaller glazing, carved facade structure and broader perimeter roof masses after the first actual source renders showed weak proportions. These are refinements to delivered source, not retouched concept pictures.

## Open on Windows

1. In GitHub Desktop, fetch origin and select `feature/visual-rebirth-04a`. Keep the project on Unity **6000.3.16f1**. Git LFS must finish downloading the models and textures. No Unity package or render-pipeline change is required.
2. Open the existing project, allow import, and inspect the Console. Stop on the first new red error. Do not delete your saves or rebuild the project from scratch.
3. With the Editor closed, run `powershell -ExecutionPolicy Bypass -File tools\test.ps1` for the complete EditMode suite. The new 04A tests are included. Then reopen and follow the gameplay matrix in the QA report using an existing save and a new Free Roam session.
4. In Play Mode, use **Neon Syndicate > QA > Capture Visual Rebirth 04A Looks**. It captures up to eight views in five looks: day, golden hour, dusk, night and rain. The existing QA safety/cancel/restore flow is reused. Run the performance flythrough as well.
5. Repeat on Low/default/High and both supported render paths you actually use. Inspect flight arrival, shadow transitions, canopy culling, camera clearance and physical steps. Capture a matching baseline on the original art branch if needed.

To reproduce cloud checks: `python tools/check_visual_rebirth_04a.py --report path/to/audit.json` requires NumPy and Pillow. Geometry, surface and layout regeneration scripts live in `tools/generate_*04a*.py`. Regeneration preserves existing metadata GUIDs. See the preview README for Blender reproduction and the supplemental compiler script for its explicit older-reference prerequisites.

The exact city-seed coverage and geometry checks can be repeated with `python tools/run_city_coverage_04a.py --dotnet /path/to/dotnet --geometry`. This uses the unchanged city generator and shipping architecture source with the disclosed managed math adapter. Reports are [planned_city_coverage.json](Reports/planned_city_coverage.json) and [city_geometry_counts.json](Reports/city_geometry_counts.json). Optional `--whole-city /external/scratch/path` exports the full ordinary city geometry for offline inspection; keep that large intermediate outside the Unity project.

## Safe rollback and remaining work

Use **Neon Syndicate > Graphics > Visual Rebirth 04A** to disable the whole layer or its architecture, strongholds, gardens or atmosphere features. Stop and restart Play Mode to rebuild geometry. These are local art settings, not save-game schema changes. The previous 02D/02E and 03A2 art remains available; required-resource failure falls back to it. Switching back to the original art branch restores the original source without rewriting saves.

Next acceptance work, in order: run the pinned Unity import/compile and complete tests; fix any first red error; test the full saved-game and control matrix; capture actual matched in-game views; profile and tune culling/LOD/shadows against the target Windows GPU; then expand populated social spaces and interactive resource/relay behavior if desired. New scenic resource and marker art should not be described as completed gameplay features.

No merge to `main` has been made. Native HTTPS Git push works, while the GitHub API returned Forbidden, so a PR was not automatically created. The review link above opens the feature comparison against the intended art branch.

## Asset provenance

All new models, textures, layouts and material palettes were authored for this project using deterministic source scripts. No external art, commercial pack, reference-image copy or licensed third-party model was used. Blender, Python, NumPy and Pillow are production tools; their logos or content are not embedded as game art. Existing project assets and GUIDs remain intact. No build output, `Library`, recovery scene, browser dependency directory or local credential is part of the delivery commits.
