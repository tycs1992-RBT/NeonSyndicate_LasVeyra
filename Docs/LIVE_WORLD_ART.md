L2 update, 2026-10-25: six original Redspire scatter variants now have a reversible default-off ordinary Free Roam lane. Pause > EXPLORE > Redspire kit: ON, then Route: Redspire art walk, after checking L1 Mirrorwell first. Only open-scatter HoodooBadlands boulders, spires and dead trees change. Source collision, 160 m chunks, all 17 discoveries and revision-10 saves retain their owners. Actual Unity import/gameplay/performance are NOT RUN. See `Docs/LiveWildL2/OWNER_GUIDE_L2.md` and `TEST_LEDGER_L2.md`.

L2 continuation: the Redspire scatter kit is prepared through the same pre-Play/build lifecycle. It is a separate passive mesh catalog used by the existing Wilds chunks, not a second LiveWorld placement owner. See `LIVE_WILDS_L2.md`.

L1 update, 2026-10-25: the original OasisPavilion is integrated at Mirrorwell Oasis in normal revision-10 Free Roam. Pause > EXPLORE > Mirrorwell pavilion routes to its actual accepted entrance. It opts into authored structural collision; the earlier nine pieces below retain their visual-only behavior. `Wild:<name or stable id>`, `collide none|box|mesh` and bounded `clear R` are documented in `Docs/LIVE_WILD_L1.md`. V0 passed the owner's Unity check; L1 native acceptance is pending.

# Live world art: real modeled pieces in the game, and how to add your own

V0 visibility repair: ordinary Play now prepares the live cast before starting; Pause > EXPLORE supplies normal GPS routes and F2 > Live characters retains failures. See `Docs/VISIBLE_REALITY_V0.md` for the five-minute check. Native acceptance remains pending.

This page is for Tyler. No code is needed. It explains what modeled art now stands in the game world, where to walk
to see it, how to export your own structures and props from Blender, where to drop them and how to say where they go.

Status: built and checked offline (layout maths, compile, tests). Nothing on this page was run in Unity yet. The
"Check it in Unity" list at the end is the first thing to do.

## 1. What is in the world now

The live art appears only in the newest world, **Gothic Civic H5 (world revision 10)**. Every piece is visual only: it
has no collision, so it can never block you, a door, a quest or the camera's collision. Each one stands on real
floor, clear of walks, roads, doors, quest places and interaction points (Section 6 lists the rules). Spots below
are from the offline plan for the shipped city; Unity's floor and collider checks may move a piece a little or
skip it, and F2 > Live world says exactly what happened.

**Home court** (leave the Veyra Lofts door and walk north up the court):

- Processional Vault, a carved stone vault bay, on the east side of the court about 60 m north of your door.
- Sanctuary Portal, a gothic portal with an amethyst crystal, further north on the east side, about 77 m up.
- Shardwood Elder, a big multi-root tree, on the west side across from the vault.
- Root Mineral Bank, a low bank of roots round a crystal, at the bend in the middle of the court.

**Walk to the Forge** (from the top of the court, follow the market walk east towards Dante Rook's Forge):

- Shardwood Elder, Root Spring Arch and a second Sanctuary Portal stand on your left, north of the walk, turned to
  face you as you arrive.

**Inside Dante Rook's Forge:**

- The Forge station now has a modeled iron anvil. It is fitted inside the station's original collider, so the station,
  its "Press E" (gamepad Y) use and its collider are exactly as before. Only the old box-shaped anvil pieces are hidden.
- A workbench and a book stand stand against a wall, clear of the walks and sight lines from the door to Dante and to
  the Forge station.

**Further out, along the Hearth Witness route:**

- Sanctuary Portal beside the Dominion Gallery, Processional Vault beside the Castellan's ground (Amethyst Dominion).
- Shardwood Elder by the Azure Shardwood path, Root Mineral Bank beside the Azure shrine, Root Spring Arch at the end
  of the storm cavern route.
- Small root banks beside the route stops where there is room (at most two per stop, eight in all).

Colours follow your canon palette: Gothic Heart stone #665b4e with #c9b98d trims, Amethyst charcoal stone #31373f with
violet crystal #9d67c1, Azure wet stone with turquoise crystal #46c4c8 and moss #135058. The stone uses the city's own
stone textures at the same size as the buildings around it. Only crystals glow, and only a little.

The Sanctuary Stair module is **not** placed on purpose: a stair must really be climbable, with collision and a checked
camera, before it goes in. Its folder says skip.

## 2. Seeing it and comparing before and after

- Press Play, choose the Gothic Civic H5 world, and walk the places above.
- Press **F2** and open the **Live world** tab. It lists every piece that was placed (with its spot) or skipped (with
  the reason). **Hide live art** shows the old look in the same spot so you can compare; **Show live art** brings it
  back. **Place again** removes everything and places it again; in the Editor it re-reads your placement files, so you
  can edit a file, save it and press Place again without stopping Play.
- The pause menu's STATUS page has a row "Live world art (H8S)" with how many pieces were placed.

## 3. Exporting a structure or prop from Blender

1. **Size and direction.** Model in metres (Blender's default scene units). Z is up. Make the side you want people to
   see face Blender's **Front** view (numpad 1). The game turns that side towards the place you name.
2. **Origin at the base.** Put the object's origin at the bottom centre, where it touches the ground
   (3D cursor to the base, then Object > Set Origin > Origin to 3D Cursor).
3. **Apply transforms.** Object > Apply > All Transforms (Ctrl A). Rotation 0, scale 1.
4. **Materials.** Either give each material its own textures (step 6), or name the material after what it is and the
   game paints it with the world's own textures in the right palette: names containing stone, masonry, rock, mineral
   become stone; trim, edge, weathered, carved become lighter trim stone; bark, wood, root become bark; leaf, moss, fern
   become leaves; crystal, amethyst, azure, gem become glowing crystal; iron, metal, bronze become tarnished metal.
5. **Export** with File > Export > FBX (.fbx) and these settings:
   - Limit to: Selected Objects. Object Types: **Mesh** only (no camera, light or armature).
   - Scale 1.00, Apply Scalings: **FBX All**, Forward: **-Z Forward**, Up: **Y Up**, Apply Unit on, Apply Transform on.
   - Geometry: Apply Modifiers on. Animation: Bake Animation off.
   - Name the file after the piece, for example `BellTower.fbx`.
6. **Textures** (optional), PNG files named after the material:
   - `Material_BaseColor.png`: the colour.
   - `Material_Normal.png`: the normal map (the OpenGL style Blender bakes is right).
   - `Material_MetallicSmoothness.png`: metal in the red channel, smoothness (1 minus roughness) in the alpha channel.
     If you only have Roughness and Metallic maps, leave this out for now; the piece then looks like matte stone or wood.
   - `Material_Emission.png`: glow (only for crystals or lit details).
   - `Material` is the material's name in Blender. Endings like `.001` are ignored. If the folder has only one set of
     textures, it is used for every material.
7. **Distance detail** (optional): meshes named `BellTower_LOD0`, `BellTower_LOD1`, `BellTower_LOD2` give Unity a
   lower-detail copy for far away. Without them the piece still disappears when it is tiny on screen.

## 4. Dropping it in

1. In Unity choose **Neon Syndicate > Live World > Make a new drop folder**. It makes
   `Assets/NeonSyndicate/Art/LiveWorld/NewPiece` with a placement file and selects it.
2. Rename the folder after your piece (for example `BellTower`).
3. Drag the exported `.fbx` and its textures into that folder.
4. Open `placement.txt`, write where it goes (Section 5) and delete the `skip` line.
5. Press Play. The game prepares the list by itself when the project opens, when a drop folder changes, before Play
   and before every build. If you want to force it: **Neon Syndicate > Live World > Prepare live world art now**.

The nine shipped pieces are in the same folder (`SanctuaryPortal`, `ShardwoodElder`, `Anvil` and so on). Their
placement files are working examples: change a line and press Play to move them, or add `skip` to switch one off. They
point at their models in other folders with a `model` line, so the art itself is never copied.

## 5. The placement file

One instruction per line. Lines starting with `#` are notes. Words are not case sensitive. Use a dot for decimals.

| Line | What it does |
|---|---|
| `near PLACE` | One copy near a place. Add as many near lines as you want copies. |
| `near PLACE offset 3 0 8` | Prefer a spot 3 m east and 8 m north of the place (two numbers mean east and north; at most 80 m away). |
| `... turn 90` | Turn it 90 degrees clockwise from facing the place. `turn 180` turns its back to the place. |
| `... scale 1.5` | Make it bigger or smaller (0.25 to 4). |
| `... palette amethyst` | For pieces without their own textures: `gothic`, `amethyst` or `azure` colours (normally picked from the place). |
| `route 4 scale 0.8` | Up to 4 copies (at most 8) beside the stops of the Hearth Witness route, two per stop at most. |
| `room forge` | One copy against a wall of a venue room, for example `forge`, `lofts`, `archive`, `clinic`, `inn`, `office`. `room forge 2` places two. |
| `replace forge-anvil` | Give the Forge stations this anvil look, fitted inside the original collider. |
| `model Assets/.../Thing.fbx` | Use a model stored in another folder. |
| `skip why` | Keep the folder but place nothing. |

**Places** you can use: Realm anchors (`Realm:gallery`, `Realm:azure`, `Realm:storm-shrine`, `Realm:hearth-lens`,
`Realm:observation`, `Realm:forge-scar` and the others the quests use), venue points (`Venue:lofts.exit` is your door,
`Venue:forge.exit` is the Forge door), people (`Cast:dante`), and shop or person names (`Dante Rook`). Places that
move or are random (Player, District:...) are refused.

If the spot you asked for is not safe, the game tries other spots 6 to 15 m around the place and takes the first safe
one. If none is safe, it skips that line and says why. It never forces a piece into an unsafe spot.

## 6. The safety rules (why a piece may move or be skipped)

A piece keeps clear of: the story route's stops, legs and walks; every walk path and realm route (with half the
route's width plus a metre); venue doors; every quest place and quest giver in this world; every "Press E" point;
ground openings into the Underheart; building footprints; roads; boss arenas; and the pieces already placed. A piece
taller than 1.5 m keeps another 1.5 m away from walks so the camera does not swing through it. In Unity it also needs
level floor under its corners, no solid object in its base and no trigger volume in its space. Room furnishings keep
the Forge gallery rules: off the walks and sight lines from the door to the giver and the service point, off the walls'
margins and away from the room's furniture.

## 7. What the Console says

Every line starts with `LIVE_WORLD`:

- `LIVE_WORLD placed SanctuaryPortal near Venue:lofts.exit at (18.0, 0.2, 45.0).` A copy is in the world.
- `LIVE_WORLD skipped BellTower: line 4: no safe spot near Realm:gallery (...). The world keeps its original look there.`
  The line was not placed, with the reason. Common reasons: `its model is missing or did not import`, `the model
  carries a Camera; export meshes only`, `the model measures 900 m across; check the export scale`, `inside the
  protected walk path`, `no existing floor near the place's height`, `existing solid geometry occupies its base`,
  `a trigger volume (...) overlaps it`, `there is no place called "..." in this world`.
- `LIVE_WORLD note for BellTower: line 3: turn needs a number of degrees.` A line was partly ignored or corrected.
- `LIVE_WORLD the prepared art list is missing, ...` Open the project in the Editor once (or use Prepare live world art
  now) so the list exists.
- `LIVE_WORLD live world art belongs to Gothic Civic H5 (world revision 10); this session is ..., so nothing was placed.`
- `LIVE_WORLD finished: 14 copies of 9 pieces placed, 2 placement line(s) skipped.` The summary.

In the Editor, preparing prints `[Neon Syndicate] LIVE_WORLD prepared 9 piece folder(s) ...` and a note for every import
setting it corrected (normal maps, colliders turned off, readable meshes).

## 8. Check it in Unity (not run yet)

1. Open the project in Unity 6000.3.16f1 and wait for the import. The Console should show
   `LIVE_WORLD prepared 9 piece folder(s) in Assets/NeonSyndicate/Art/LiveWorld, all ready`.
2. Press Play, start Gothic Civic H5. The Console should end with `LIVE_WORLD finished: ...`.
3. Walk up the home court and to the Forge (Section 1). Look at: the pieces stand on the ground (no floating, not sunk
   too deep), the stone looks like the buildings' stone, crystals glow softly, nothing blocks a walk, door or prompt,
   and the camera does not clip into them in the normal view.
4. In the Forge: the new anvil sits on the old wooden block inside the old station; "Press E" (gamepad Y) still opens
   Dante Rook's Forge from the same distance; the workbench and book stand stand against a wall and do not hide Dante
   from the door.
5. F2 > Live world: read the list, try Hide live art and Show live art, and Place again.
6. If something looks wrong, send the F2 page or the `LIVE_WORLD` Console lines.
