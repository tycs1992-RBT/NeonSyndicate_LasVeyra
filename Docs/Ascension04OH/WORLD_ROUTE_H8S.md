# H8S world: Forge gallery furnishings and the revision-10 route (VISUAL PREVIEW draft)

Status: source integration with offline checks only. Nothing here has been run in Unity. Every number below marked
"offline" comes from the offline reference harness (Unity's managed reference assemblies and the project's own pure
layout maths), not from native Unity. Owner appearance approval is NOT GIVEN.

Two explicit Editor reviews make the delivered H8S furnishings and one coherent route visible in the real game. Both are
VISUAL PREVIEW overlays: they add no collider, change no floor, navigation, quest, reward, save or default world, and
remove everything they added (and restore the four hidden Forge renderers) on End, on a failure part-way through setup,
when Play mode stops, when the window closes and before an assembly reload.

| Menu (Neon Syndicate > 04OH) | What it does |
|---|---|
| H8S Forge gallery furnishings review | Edit mode: checks the 11 prop receipt hashes and prepares texture import settings. Play mode: fits the Anvil inside the original Forge collider and places the Workbench and BookStand in the Forge room. |
| H8S End Forge gallery furnishings review | Restores the original Forge art and removes every preview object and material. |
| H8S Revision 10 route review | Play mode in Gothic Civic H5 (revision 10): resolves the seven stops, measures the legs, logs the time estimate, draws the route and places the H8R modules and accents that pass the clearance rule. |
| H8S End Revision 10 route review | Removes every route preview object and material. |

## 1. The route

One ordered route through the existing revision-10 world, in the Hearth Witness quest's own order
(`Resources/Quests/ascension_h3_hearth_witness.json`, id `fg_04oh_h3_hearth_witness`, unchanged). The plan is the pure
class `Runtime/World/WorldRouteH8S.cs`; the test `StopsFollowTheHearthWitnessQuestAndItsExistingTokens` proves the last
five tokens equal the quest's step places in order and that every token passes the existing validators.

| # | Stop | Place token | Resolver (existing owner) | Purpose | Time at stop (estimate) | Protected disc |
|---|---|---|---|---|---|---|
| 1 | Home and court | `Venue:lofts.exit` | `CampaignPlacesRuntime04E.TryResolve` (the live `QuestPlaces.TokenResolver`) | Start at the Veyra Lofts door, cross the home court | 0 s | 4 m |
| 2 | Forge and gallery | `Cast:dante` (door `Venue:forge.exit`) | `CampaignPlacesRuntime04E.TryResolve`: Dante's live actor, else his post | Accept The Hearth Witness from the original giver beside the Forge and its gallery furnishings | 30 s | 4 m (indoors, no dressing) |
| 3 | Amethyst investigation: hearth lens | `Realm:hearth-lens` | `CityLayout.ResolveRealmAnchor04OC` (as `QuestPlaces.Resolve`) | Home-side stair, supported reading porch, read the impression | 15 s | 4.5 m |
| 4 | Amethyst investigation: oath witness | `Realm:oath-witness` | `CityLayout.ResolveRealmAnchor04OC` | Compare the witness, clear two glass mites one at a time (maxAlive 1) | 50 s | 5 m |
| 5 | Azure shrine | `Realm:storm-shrine` | `CityLayout.ResolveRealmAnchor04OC` | Settle the one neon jackal | 30 s | 9 m |
| 6 | Root spring | `Realm:storm-spring` | `CityLayout.ResolveRealmAnchor04OC` | Grounded lower cavern route, paired mineral survey | 15 s | 4.5 m |
| 7 | Safe Forge return | `Venue:forge.exit` | `CampaignPlacesRuntime04E.TryResolve` | Return to the Forge door, Dante closes the survey | 20 s | 8 m |

Each protected disc is the largest quest trigger radius at that place plus 2 m, at least 4 m (checked against the quest
data by the same test). Legs into and out of the indoor giver stop pass through its door (`Venue:forge.exit`), because
the layout's walk graph starts outdoors.

### Offline resolution, seed 61724, revision 10 (offline reference harness)

`WorldRouteH8STests.RevisionTenRouteResolvesThroughTheRealAnchorsOffline` generates the real default city, applies
revision 10, resolves Realm anchors through `ResolveRealmAnchor04OC`, venue anchors through the binding origin, yaw and
the shipped layout anchor (what `VenueView04C.WorldPoint` returns live) and `Cast:dante` at his post (the Forge staff
anchor, because no actor exists offline). Legs follow `CityLayout.FindWalkRoute04O`.

| Stop | Offline point |
|---|---|
| home | (0.0, 0.2, -32.0) |
| forge (Dante's post) | (80.3, 0.2, 110.0) |
| hearth lens | (-10.0, 0.6, -44.0) |
| oath witness | (-302.8, 0.2, 75.1) |
| storm shrine | (-270.0, 0.2, 176.0) |
| storm spring | (-379.9, 0.2, 162.3) |
| Forge return | (75.0, 0.2, 98.0) |

| Leg | Straight | Followed on the walk graph |
|---|---|---|
| home to forge | 163.1 m | 200.1 m (11 points) |
| forge to hearth | 178.5 m | 222.9 m (14 points) |
| hearth to witness | 316.1 m | 492.2 m (19 points) |
| witness to shrine | 106.2 m | 385.2 m (12 points) |
| shrine to spring | 110.7 m | 174.8 m (10 points) |
| spring to return | 459.4 m | 603.4 m (21 points) |
| total | 1334 m | 2079 m |

### Time estimate against the 4 to 8 minute target

Speeds come from the controller's own constants in `TravelMath`: walk 1.55 m/s, default run 4.4 m/s, sprint 8.2 m/s,
Surge top speed 30 m/s plus a 1.2 s ramp per leg. Time at stops totals 2 min 40 s (estimates in the table above).

| Travel | Moving | Plus stops | Against 4 to 8 min |
|---|---|---|---|
| Walk | 22 min 21 s | 25 min 1 s | outside |
| Default run (the segment estimate) | 7 min 52 s | 10 min 32 s | outside |
| Sprint (ignores stamina) | 4 min 13 s | 6 min 53 s | inside, only if stamina allows |
| Surge (lower bound, ignores turns) | 1 min 16 s | 3 min 56 s | just under |

Honest reading: the route the existing quest defines is long. On foot at the default run it is about 10.5 minutes,
above the target; it reaches the window only with sprinting or travel powers. The quest file must not be edited, so this
sprint cannot shorten it. A measured play time in Unity (stopwatch, per travel mode) is still required; these are
estimates.

## 2. What is physical and what is VISUAL PREVIEW

| Physical, existing and untouched (authoritative) | VISUAL PREVIEW, Editor-only, reversible, IgnoreRaycast, no collider, rigidbody or script |
|---|---|
| The Forge station `Forge (Dante Rook's forge)`, its BoxCollider, its Interactable (E or gamepad Y), faction and combat conditions, the Forge screen | The fitted Anvil visual inside that collider |
| Forge room floors, walls, furniture colliders, the single gothic shell MeshCollider, venue keepouts and NPC posts | Workbench and BookStand on the Forge room floor |
| Revision-10 floors, stairs, porch, cavern and realm colliders, the walk graph | Gold route lines (red where a leg had no walk graph route) and stop pins |
| Quest runtime, step radii, creature spawns and territories, rewards, saves and banks 0 to 10 | Up to four H8R modules and up to eight accents (`RootMineralBank` at 0.7 scale) |

While the Forge review runs, four original primitive renderers are hidden: `Waist`, `Face`, `Horn` and `Glow` (their
exact `enabled` values are recorded first and restored). `Block` (the wooden stump the new anvil rests on), `Bowl`,
`Embers` and `Ember Glow` stay visible. No layer, collider or component of the original changes.

One persistent side effect: the first review run sets texture import flags on four delivered textures and Unity writes
them into their `.meta` files: `Metal_Normal.png` and `Furniture_Normal.png` become Normal map, and
`Metal_MetallicSmoothness.png` and `Furniture_MetallicSmoothness.png` turn sRGB off (linear). Base colour stays sRGB. GUIDs
do not change. These are the intended import settings; keep them.

## 3. The overlay clearance rule and its results

Rule (`WorldRouteH8S.OverlayConflict`, plan view, any height): a decorative overlay's bounds keep out of a disc of each
stop's protected radius and out of a 3 m corridor around every straight leg between consecutive stops and around every
segment of the followed walk polylines. The review also requires existing floor under the overlay (downward ray against
the World layer, not steeper than about 37 degrees), not inside a building footprint (`CityLayout.IsInsideBuilding`) and
the lower 2 m of its inner footprint free of existing solid geometry (`Physics.CheckBox`). An arch over a path is refused
by design.

Offline clearance plan (source-mesh footprints; the live review re-measures the imported bounds and adds the floor,
building and collider checks), `H8RModulesAndAccentsAgainstTheRealRouteCorridorOffline`:

| H8R module | Anchor | At its H8R offset | First plan alternative that passes |
|---|---|---|---|
| SanctuaryPortal | `Realm:gallery` | refused: walk corridor hearth to witness | (4, 0, -4) |
| ShardwoodElder | `Realm:azure` | refused: straight leg spring to return | (3, 0, -5) |
| RootMineralBank | `Realm:storm-shrine` | refused: inside the jackal fight disc (stop shrine) | (-10, 0, 6) |
| RootSpringArch | `Realm:storm-cavern` | passes | not needed |

Accent slots passing the plan rule: 24 (4 per outdoor stop); the review keeps at most 2 per stop and 8 in all, none in
the Forge. SanctuaryStair is still not placed (it needs climbable geometry and native grounding and camera proof).

Palette: the H8R stone, weathered edge, bark and leaf values, with crystals tinted by region (amethyst for home and the
investigation, ember amber at the Forge, azure in Shardwood), restrained emission at 0.12.

## 4. The Forge furnishings

Assets: `Assets/NeonSyndicate/Art/ProductionH8S/Props` (Anvil, Workbench, BookStand FBX; Metal and Furniture base colour,
normal, ORM and derived MetallicSmoothness). Before trusting the packed maps they were checked with Python and PIL: for
both sets, MetallicSmoothness red equals ORM blue and alpha equals 255 minus ORM green with a maximum difference of 0,
green and blue are empty, all maps are 2048 square, and the hashes match `candidate.json`. Materials are runtime-only
through the original Lit owner (`Mats.StandardShader`, `RenderPath.IsURP` picks Built-in Standard or URP Lit property
names). Occlusion is not bound: both pipelines read occlusion from green, which holds roughness in the delivered ORM.

### Anvil fit (offline numbers from the delivered source mesh; the review recomputes from the imported mesh)

Delivered Anvil source size 1.0816 by 0.5560 by 0.4016 m (Unity x, y, z), pivot at the feet centre, tapering horn on
the long side. The review refuses if the imported size differs by more than 3 percent (native scale check). The original
station collider: centre (-0.2, 0.45, 0.05), size (1.5, 0.9, 0.6); the original block top is at 0.5 m.

`ForgeGalleryFitH8S.Fit` chooses the largest uniform scale up to 1 (never above native, refused below 0.5) at which every
rendered corner keeps a 2 cm margin from every face while the anvil rests on the block, centred within 12 cm:

| Result | Value |
|---|---|
| Scale | 0.6834 (binding limit: height above the block, (0.9 - 0.02 - 0.5) / 0.556) |
| Turn | 180 degrees, horn to +x like the original anvil horn |
| Pivot (station space) | (0.000, 0.498, 0.000) |
| Fitted size | 0.739 by 0.380 by 0.274 m |
| Clearances to the collider faces | left 0.622, right 0.139, bottom 0.500, top 0.020, back 0.113, front 0.213 m |
| Kept art | clear of Bowl and Embers; touches only the Block it rests on |

The earlier proposal (scale 0.7 at height 0.5) is inside the raw box by only about 9 mm at the top and fails the 2 cm
margin (`ProposedScaleSevenTenthsAtHalfAMetreDoesNotKeepTheMargin`). After placing, the review re-measures every
rendered corner in the collider's space and refuses (hiding nothing) if any corner breaks the margin.

### Gallery placement rules (Workbench 2.02 by 0.89 by 1.02 m, BookStand 0.51 by 1.45 by 0.51 m)

In the live Forge room (`Venue:forge`, the room extents `VenueView04C.Contains` uses) a spot must: back onto a wall,
face into the room, keep 0.25 m from the walls; stay 0.3 m clear of every venue keepout (public threshold, principal
corridor, staff and visitor routes); stay out of every active Interactable's radius plus 0.3 m (the Forge station 2.2 m,
Dante's talk 3 m indoors, any quest prop); keep 1 m from every venue anchor except the door; keep 0.75 m from the walks
from the entrance to the giver and to the station; not cut the sight lines from the entrance eye (1.6 m) to the giver's
head, chest and knees or to the station; keep 0.5 m from the station collider and 0.1 m from every existing collider
footprint (building-sized combined colliders such as the gothic shell are left to the exact check below); then pass the
physical probes: five downward rays find the level room floor within 6 cm, and `Physics.CheckBox` over the whole volume
finds no existing collider on any layer except IgnoreRaycast.

Offline plan (forge.json keepouts and anchors, without furniture colliders): Workbench against the left wall at
room-local (-6.94, 10.91), turned 90 degrees; BookStand against the left wall at (-7.19, 7.15), turned 90 degrees; 279
and 428 rule-passing candidates. In Unity the real colliders (for example the tool rack near (-6.5, 6.2)) can move them.

## 5. What Tyler must check in Unity (all NOT RUN here)

Forge gallery:

1. Open the project and let it import. Run Neon Syndicate > 04OH > H8S Forge gallery furnishings review once in Edit
   mode. Expect "the 11 receipt hashes match" (and, the first time, four "Texture import setting corrected" lines).
   If it reports a hash mismatch, pull Git LFS files and retry.
2. Press Play, Title > Gothic Civic H5, go inside Dante Rook's Crystal Forge. Run the menu again and read the report
   (scale near 0.683, top clearance near 0.020 m).
3. Select `Forge (Dante Rook's forge)` in the Hierarchy and look at the Scene view from front, back, both sides and
   above: no part of the new anvil leaves the green collider box. Check the new anvil sits on the wooden block.
4. Walk up to the anvil: the "Press E to use Dante Rook's forge" prompt must appear at the same distance as before. Press
   E (keyboard) and Y (gamepad): the Forge screen must open. Faction refusal and combat lockout must behave as before.
5. Walk around the workbench and book stand with the camera close: the camera must not bump, zoom or clip because of
   them (they have no collision, so walking through them is expected in a preview).
6. Stand at the door: Dante must stay visible and the walk to him clear.
7. Neon Syndicate > 04OH > H8S End Forge gallery furnishings review: the original box anvil returns. Repeat, then stop
   Play mode in the middle, and once trigger a script reload in the middle: no `VISUAL PREVIEW` object may remain.

Route:

1. Play, Title > Gothic Civic H5. Run Neon Syndicate > 04OH > H8S Revision 10 route review. Expect 7 of 7 stops
   resolved, six gold legs, the estimate lines, and the module and accent lines (copy the `H8S_ROUTE` Console lines).
2. If The Hearth Witness is legitimately available, accept it from Dante and walk the route. At each pin the quest step
   must fire (GoTo radius 2.5 or 6 m, Interact, Kill counts 2 mites one at a time and 1 jackal). Never reset receipts
   or grant progress for the test.
3. Check that no overlay hides a path, stair, quest prop, creature, NPC or the door, and that the camera never collides
   with an overlay. Note any overlay that floats or sinks.
4. Time the segment from Dante to the Forge return with a stopwatch: once at the default run, once sprinting, once with
   Surge. Record against the 4 to 8 minute target.
5. Neon Syndicate > 04OH > H8S End Revision 10 route review: everything disappears.

## 6. NOT RUN and known weaknesses

NOT RUN: native import of the three FBX files and the textures (axes, scale, materials), Unity Console compile, EditMode
tests inside Unity, both review windows, the anvil inside the collider in a real scene, E and Y interaction, camera,
walking the route, quest triggers, timing, the URP path, frame rate, Windows build. The 12 compile configurations and the
EditMode tests were run in the offline reference harness only.

Weaknesses: the route estimate is above 8 minutes at the default run; the fitted anvil is two thirds of the pack's
native size (it has to rest on the original 0.5 m block inside a 0.9 m collider); ambient occlusion from the ORM is not
bound; the normal maps' green-channel convention is unverified until seen in Unity; the furnishings are walk-through
previews with no collision; the offline gallery plan ignores furniture colliders (the live review handles them); the
straight-leg corridor is conservative (the 459 m spring to return line crosses the city), which pushes some modules off
their H8R offsets; the H8R module materials are flat colours, not textured.

## 7. Files

| File | Purpose |
|---|---|
| `Assets/NeonSyndicate/Scripts/Runtime/World/ForgeGalleryFitH8S.cs` | Engine-free fit, quarter turns, box clearance, overlap depth, 3D slab sight test, delivered source sizes |
| `Assets/NeonSyndicate/Scripts/Runtime/World/GalleryPlanH8S.cs` | Engine-free plan rules and ranked wall-backed furnishing spots |
| `Assets/NeonSyndicate/Scripts/Runtime/World/WorldRouteH8S.cs` | Engine-free route stops, door-routed legs, time estimate, clearance rule, dressing slots |
| `Assets/NeonSyndicate/Scripts/Editor/ForgeGalleryReviewH8S.cs` | Forge gallery VISUAL PREVIEW window and menus |
| `Assets/NeonSyndicate/Scripts/Editor/WorldRouteReviewH8S.cs` | Revision-10 route VISUAL PREVIEW window and menus |
| `Assets/NeonSyndicate/Tests/EditMode/ForgeGalleryFitH8STests.cs` | 10 tests: fit, margins, refusals, slab test, plan distances, conflicts, pivot on footprint, real Forge room spots |
| `Assets/NeonSyndicate/Tests/EditMode/WorldRouteH8STests.cs` | 6 tests: quest order and tokens, estimate, clearance rule, dressing slots, real revision-10 route and clearance plan |
