# Visual Rebirth 04A: three strongholds

The existing runtime forts now consume three authored source layouts. This is an optional art replacement on top of the original 02I construction and collision system. Layouts are loaded automatically during each fort's existing rebuild, including after a saved theme or socket changes. The exact same JSON is used by the offline preview renderer.

| Site | Composition | Authored parts | New colliders |
|---|---|---:|---:|
| Hearthspire | Twin ceremonial gates, planted scholar core pavilion, four watch spires, observatory, elevated western cloister and northern scholar balcony, willow garden and moonwell | 177 | 46 |
| Galehook | Rootwoven double entrance, paired monumental inhabited tree silhouettes, sheltered rear spring, overgrown wall arcades, hanging foliage and perimeter gardens | 170 | 0 |
| Sunscar | Heavy asymmetric gate, extraction engine, counterweight tower, suspended gantry, jagged ore yard and fortified wall braces | 109 | 0 |

These counts exclude the original 02I meshes and the additional 04A ornaments generated for whichever six modules the player has saved. Empty sockets receive no socket art. Watchtowers gain low railings, stained glass and mineral finials; conservatories gain modeled root arches and planted borders; bulwarks gain mineral relief and crestwork; flight daises gain a shallow ring and small lamps around the existing landing pad.

The site architecture remains recognizable after a theme change. Saved theme continues to choose the stone, roof, metal, cloth, crystal and foliage palette. Unowned Sunscar keeps the hostile obsidian treatment, using the existing 03A2 style rule; ownership returns color control to the saved theme. No save fields, faction identifiers, socket positions, interaction points or rewards were added or renamed.

## Places to walk

All coordinates below are local to the fort's actual safely selected runtime origin. The original catalog coordinates are candidates, not guaranteed final positions. Use the existing STRONGHOLDS tracking page to find the live site.

- The original challenge point remains `(0, .07, -20)`. Both front gates retain the clear central approach.
- The original core interaction remains `(0, .07, 2.65)`. At Hearthspire the ground-level scholar pavilion surrounds this court, with columns outside the protected central corridor and six socket envelopes.
- From the front approach, walk around the western exterior wall to `(-20, .07, -16)`. Twelve modeled steps rise to a real deck at height `3.07`. A lower landing leads to twelve more steps beginning at `(-20, 3.07, -2)`.
- The upper western promenade at height `6.07` leads north to `(-20, 6.07, 19.8)`, then east to the scholar overlook at `(-6, 6.07, 19.8)`. Every tread and landing has deliberately matching box collision. Upper edge parapets and pavilion columns also collide.
- The eastern garden around `(19, .07, 8)` leads past the cosmetic drake roost to a decorative resource outcrop around `(19, .07, 19)`. The moonwell is near `(19, .07, -19)`.

The core chamber and scholar balcony pavilion are open-sided spaces. Only the explicitly modeled walkway decks and stairs add usable physical landings. Decorative roofs, skyline spires, gantries and branches do not add implied physics surfaces. Existing 02I tower and flight-dais decks retain their original colliders.

## Safety and rendering

All new fort art stays within the original `StrongholdPlacement.PlotHalf = 24` probe. Existing world placement logic is unchanged. Added home colliders are validated against the entrance, central core sightline and all six socket envelopes. Rival compositions introduce zero new colliders, preserving warden spawning at `(+/-5, .2, 5)`, melee sightlines and raid navigation.

Each source layout is bounded to 1,200 parts and 96 colliders; the current largest layout has 177 parts. Meshes and material families use the shared 04A cache. Static parts are batched by material with near and distant LODs; fine flowers, ferns and small lamps are omitted on Low. The common ambience scheduler animates selected rings and vines with shared source meshes. A conservative swept-sphere check keeps animation inside the fort placement probe; border ornaments remain static. The single optional home drake is cosmetic, has no AI/collider/combat state and grants no reward.

The prior 03A2 shell is hidden only after the full new composition is built. Any failure restores that shell and its original materials. The original 02I batch, modules and collisions stay live throughout. `NSLV_04AEnabled` and `NSLV_04AStrongholds` independently permit reverting to the working prior art at the next world or fort rebuild. No original assets are deleted.

## Source and verification

`tools/generate_strongholds_04a.py` authors the three layout JSON files in `Resources/VisualRebirth04A/Layouts`. It is deterministic, preserves existing metadata and can be rerun without a DCC tool. `StrongholdLayout04A` supplies the schema, engine-free safety validation and saved-slot ornament composition. `StrongholdView.Rebirth04A` handles staging, failure recovery and integration with existing forts.

`StrongholdRebirth04ATests` covers exact transformed model vertices in both LODs, entrance clearance, immutable saved slot values across every module rotation, collision exclusions and continuous analytic support along the elevated route. The offline validator checks the same source assets and layouts.

The final offline asset audit passed 1,883 checks. All 456 authored fort parts in both LODs stay inside the 48m placement square, with no actual triangles blocking the reserved low front approach. Every stair and deck collider's center and four inset corners match horizontal triangles in both LODs. The analytic route has continuous support, maximum step rise .25m and a northern overlook at height 6.07m. Collision cross widths were reduced to the source terrace's 75 percent bevel interval so the colliders do not extend beyond visible corners. The first offline rendered review revealed two roofs floating above their support columns; their eaves and rib vaults were lowered to rest on the columns. These are source-asset checks and preview refinements, not proof of Unity playability.

Authored near/far geometry totals are 235,134/64,106 triangles at Hearthspire, 244,814/60,804 at Galehook and 89,172/31,144 at Sunscar. These counts describe the new source composition before the existing base fort, saved socket details and runtime batching. They are not measured draw calls or frame times.

Unity 6000.3.16f1 was unavailable in the cloud task. Unity import, EditMode execution, live walking/camera collision, controller use, original module landings, old-save reload, fort claim, rival raids and frame profiling remain required local checks. In the existing project, run the EditMode tests, enter Free Roam, track Hearthspire, walk the route above, change every socket/module rotation and theme, save/reload, then inspect both optional rival raids. Compare default and Low, and repeat with the 04A stronghold switch disabled to verify fallback.
