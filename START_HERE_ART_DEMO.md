# GideoVamesAI: Crystal Manor Saga

**Current revision: [Visual Renaissance 01C](START_HERE_01C.md).** Read that guide and its QA report first. The material below is the retained 01B art-review guide.

## Visual Renaissance 01B, Asterfall Promenade refinement

This is the complete Master Build 01 Unity source project with a bounded art-direction refinement based on the 12 supplied references. It is not a compiled game executable. Unity import, compilation, rendering and playtesting have not been performed in this environment. No claim of an in-engine visual match is made.

1. Extract this ZIP into a new folder. Keep your previous project as a comparison copy.
2. In Unity Hub, add the folder containing `Assets`, `Packages` and `ProjectSettings`.
3. Open it with **Unity 6000.3.16f1**, the version recorded in the supplied project. Let packages and assets finish importing. Internet access may be needed for Unity packages.
4. The existing automatic setup creates/opens the game scene. The new automatic import hook retains district material variants for the active renderer. Keep the default Built-in pipeline for your first comparison.
5. Press Play and choose **Free Roam**. While on foot with no active mission, use **Neon Syndicate > Art District > Visit Promenade in Play Mode**.
6. Explore both sides of Core Avenue, approximately x22 to x238 around z0. Use your existing camera and movement controls. The review menu does not add or replace any control binding.
7. Use **Visit Rooftop Drake in Play Mode** to inspect the cosmetic Quartz Drake. It is an art exhibit, not a new tameable companion.

For daytime, dusk and night comparisons, use the game's existing F2 atmosphere controls. The art system does not force the clock or weather. Repeat the view in first person and third person, with the HUD visible.

If the new models do not appear, inspect Console errors and use **Art District > Repair Import Settings**, then re-enter Play Mode. Missing/unreadable kit assets or an art construction exception should fall back to the original building visuals. New art remains separate from the existing building collider path. Helipads are excluded.

**Editable assets:** OBJ source models and texture maps are included. Outside Play Mode, **Art District > Create Editable Kit Library** creates artist-facing material and prefab copies. Existing artist copies are preserved. The running game uses the Resources kit and `DistrictMaterials`; editing a copied material does not automatically change runtime art. Update the Resources source or material factory to integrate an approved change.

## Read these reports

- [01B visual comparison, all 12 references and actual source mesh comparisons](Docs/ArtDistrict01B/VISUAL_COMPARISON.html)
- [01B implemented changes](Docs/ArtDistrict01B/CHANGELOG.md)
- [01B QA and tests still required in Unity](Docs/ArtDistrict01B/QA_REPORT.md)
- [01B remaining production-quality 3D assets](Docs/ArtDistrict01B/REMAINING_PRODUCTION_ASSETS.md)
- [01B asset inventory](Docs/ArtDistrict01B/ASSET_INVENTORY.md)

The reports below are the preserved Master Build 01 record, not the latest 01B results:

- [Asset inventory and remaining art](Docs/ArtDistrict/ASSET_INVENTORY.md)
- [Changelog](Docs/ArtDistrict/CHANGELOG.md)
- [QA report and Unity acceptance checklist](Docs/ArtDistrict/QA_REPORT.md)
- [Art direction and performance rules](Docs/ArtDistrict/ART_DIRECTION.md)
- [Baseline renderer audit](Docs/ArtDistrict/BASELINE_AUDIT.md)
- [Material and effect technical notes](Docs/ART_DISTRICT_MATERIALS_AND_FX.md)

`MODULAR_KIT_OFFLINE.png` is an inspection render of the actual OBJ geometry, clearly labeled **not a Unity screenshot**. Historical screenshots elsewhere in the supplied project are not evidence of this release's appearance.

The wider city, complete Manor interiors/evolving rooms, forest biomes, large dimensional realms and new production humanoid art are outside this first district delivery. See the inventory for concrete next assets.
