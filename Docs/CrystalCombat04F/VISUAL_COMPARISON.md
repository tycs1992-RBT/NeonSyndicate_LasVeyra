# Crystal Combat and Living City 04F: visual comparison

## Evidence status

This document maps inspected reference designs to source changes in the existing playable game. **No Unity screenshots, GPU render, native Play Mode, animation playtest or measured performance capture was produced in this environment.** Source compilation and pure geometry tests are separate evidence, not visual acceptance.

The source baseline is `4c690c6248f0ff7f00098598e6e42e409d70f84f` on `feature/underheart-bonds-04e`. The supplied `14_EXISTING_GAME_SCREENSHOTS` images are labelled older 04B or city-before examples. They cannot establish a matching 04E before frame. No new image is presented as a before/after render.

## Reference review and canon

Inspected the contact sheet covering all 14 numbered categories and all three full master boards, plus the individual hero portrait, Warden concept, hatchling growth sheet, melee arsenal and armor/outfit sheet. Boards are directional artwork. Malik Veyr, Luma Veyr, the established recurring cast, Quartz Drakeling, Ashshell Warden, Stormskimmer and existing fusion forms remain authoritative. No speculative board character or elemental drake replaces a saved identity. No collage was cut into a game asset.

| Reference category | Observed design and resulting decision | 04F source or retained system |
| --- | --- | --- |
| Heroes and cast | Defined brow, almond eyelids, narrow mouth seam, layered lapels, dark textile with small hard trim. Preserve Malik's current skin, hair, beard and wardrobe. Luma stays a young civilian. | `Characters/BodyBuilder.Crystal04F.cs` adds lower lid/crease geometry, nostril recesses, philtrum ridges, mouth seam, closed lapels and coat panels. Existing `Appearance` remains unchanged. |
| Crystal enforcement | Segmented shoulders, breastplate planes, raised neck protection, articulated hip armor and a small central crystal. Armor reads through its mass and silhouette rather than glow alone. | Same file adds closed six-sided lames, tassets, split breastplate, gorget and inset. Existing `SuitStyle`, `ShellStyle`, `ArmorColor` and visor fields drive variation. |
| Drayke companions and growth | A broad juvenile skull leads into narrow snout, swept cheek/horn forms, large wing structure, distinct neck/throat plating. Larger stages retain the same animal. | `Creatures/CreatureBuilder.Crystal04F.cs` adds curved cheek casques, orbital/jaw/nasal plates and weighted neck sails to the existing Quartz body. Existing `CompanionLooks` and `CompanionSculpt` still own saved growth and throat lantern. |
| Fusion vehicles | Chassis identity remains visible inside swept mineral fins, wings and structural braces. | Existing 04E Ventwing, Kilnback and Coilrunner geometry, rig and animations preserved. No replacement chassis authored in this art pass. |
| Crystal weapons | Thick faceted blades, metal structural spine, tapered point, swept quillons, winding and a small power inset. Ranged silhouettes are clearly separate from ordinary firearms. | `Combat/WeaponModels.Crystal04F.cs` authors Asterblade and one Riftfang talon as closed sectional geometry. `WeaponModels.Create` uses the existing six moving 04D focus models for converted ranged definitions. |
| Armor and customization | Layered cloth and hard sections have real overlap and edge thickness. Identity comes from morphology and garment choices. | Modular additions share the current skin renderer and palette. Existing saved garment, shell, helmet and color choices are retained. |
| Factions and city life | Counterpart silhouettes and handler/drake pairs make faction activity readable in the street. | Role appearance selection remains with the city AI integration. Shared armor geometry responds to its existing appearance fields. |
| Enemies and bosses | Heavy orbital crests, protected surfaces and visible weak-point hierarchy. | Existing boss shapes and hit volumes retained. Quartz facial planes added without moving brow weak points or collision. No new boss model is claimed. |
| City environments | Tall crystal architecture, inhabited service frontage and distinct districts. | 04A to 04E architecture, streaming, doors, floor layouts and graphics remain intact. This art pass does not claim a new city asset set. |
| Interiors and underground | Warm public rooms contrast with cool mineral stations and dangerous thermal caverns. | Existing Cinder Dispatch, Pressure House and Underheart spaces remain intact; gameplay tasks use their existing physical spaces. |
| Gameplay and missions | Clear blade arcs, opposing stances and a supporting creature need to read at gameplay distance. | Actual blade geometry, articulated grips and finite NPC encounter integration. Combat/native camera readability still needs the checks below. |
| UI and menus | Small crystal symbols support readable names and objectives rather than replacing text. | Existing menus and cast guidance preserved; no art-only menu mockup is offered as gameplay. |
| Lighting and mood | Material and silhouette need to remain readable in both daylight and night, with restrained emission. | Blade cutting facets use narrow crystal inserts with emission 1.8. No added character lights or bloom-only workaround. |
| Existing gameplay shots | Older supplied screenshots provide context but do not establish 04E camera/time/settings provenance. | Require new controlled 04E/04F target screenshots before judging native improvement. |

## Actual deformation and rendering changes

`RigLayout` preserves indices 0 through 16 and appends three joints for each of ten digits. Detailed human meshes weight vertices to all 30 new joints. `HandPose.Apply` is implemented and receives the existing animator's smoothed grips: relaxed, open, fist, handle, trigger, support and handlebar. The pure digit solver mirrors the hands, lets the index extend independently and limits distal curl for a larger grip. Crowd bodies and legacy humanoid monsters keep their original 17 live transform budget; crowd mitten geometry remains unchanged.

New human tailoring joins the existing one-material, one-renderer skinned mesh with proper normals, tangents, palette/detail coordinates and bone weights. New Quartz anatomy joins its existing skin and bones; collision, weak points and root movement are unchanged. Its quiet pose adds a bounded head/neck scent gesture and tail lift/sway, suppressed by alert, hurt, attacks and death so combat tells retain priority. The existing flight solver still owns wing stroke and folding.

No new binary or purchased asset dependency is introduced. These are original code-authored surfaces and riggable modular profiles, not imported offline concept art. They improve selected geometry; they do not yet achieve the reference portraits' detailed textured hair, production facial deformation or hand-authored animation clips. Native quality remains **unverified**.

## Budgets and checks

- Detail 0: unchanged mitten geometry, no added tailoring/face/Quartz anatomy; 17 live human transforms.
- Detail 1: articulated fingers and limited face/tailoring detail; existing standard body vertex ceiling remains 8,200.
- Detail 2: full hand/nail, face and armor detail; existing hero body vertex ceiling remains 13,500.
- Every detailed human still has one skin renderer and one shared character material. New body detail adds no scene objects, colliders or lights.
- Every blade is cached per existing weapon id. A paired talon uses two cached instances; the blade geometry itself contains one talon.
- Standard Quartz companion detail remains selected by the established `CompanionLooks.DetailFor` policy. The additional anatomy scales with the body and uses its existing LOD choice.
- Pure tests verify body geometry budgets, all digit weights, mirrored curls, independent index, grip-radius stop, blade thickness/taper and valid normals. They do not verify grip clearance against every weapon, native skin bounds, shader appearance or frame time.

## Required target screenshots and native acceptance

Use Unity `6000.3.16f1`, the same existing save slot copied before testing, seed `61724`, identical graphics preset, clear weather, fixed camera and clock. Capture the unmodified 04E baseline and 04F branch separately. Record the exact commit of each run. Use actual game cameras; do not compare a special offline render to gameplay.

1. Free Roam arrival: daylight 12:00, normal third-person distance, hero + street + nearby cast. Confirm changed loadout and visible coat silhouette.
2. Hero face: same camera roughly 1.2 metres in front, HUD hidden, daylight and one warm interior. Confirm eyelids are not occluding irises and the mouth/nose additions do not float or z-fight.
3. Hero hands: right sword grip, both talons, ranged focus, open casting hand, guard, reload vent, ladder/phone and vehicle handlebar. Inspect each at normal speed and pause. Confirm no digit inversion, hand clipping or detached weapons.
4. Enforcement at 8 to 12 metres: blade patrol, shield role, caster and counterpart. Confirm armor lames read and do not intersect the face, upper arms or doorways.
5. Bonded Quartz Drakeling: same saved juvenile and evolved stage, idle, walk, run, attack, hurt, summon and fusion transitions. Confirm cheeks preserve expressive eyes, jaw opens cleanly, neck sails deform with the neck and no duplicate animal appears.
6. Weapon profiles in daylight: sword and talon side/front views. Confirm closed thickness, point taper, grip placement and visible metal structure without bloom.
7. Repeat the arrival and one finite faction conflict on Low, Medium, High and Ultra. Record frame-time percentiles, draw calls, skin bone counts and peak active actors on the owner's hardware. No FPS is predicted here.
8. Repeat nighttime 22:00, Pressure House interior, Underheart fire cavern and flight/vehicle scenes. Check for magenta materials, culling, overexposure and regression to earlier art.

Until these frames and checks exist, completion is **source implemented, native art/animation acceptance pending**. Imported hand-authored hero models, dedicated facial blendshapes, dense texture work, new custom animation clips and environment-wide art replacement remain future production work.
