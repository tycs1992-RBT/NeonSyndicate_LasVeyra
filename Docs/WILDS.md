L3 continuation, 2026-10-25: original Redspire ground PBR source and a separate default-off EXPLORE ground setting are implemented on the original base floor and wild chunks. Existing L2 art, original collision, 17 discoveries and revision 10 are preserved. L1/L2 native acceptance stays open; L3 native import/play/performance are NOT RUN. See Docs/LIVE_GROUND_L3.md and Docs/LiveGroundL3/VERIFICATION.json. Earlier evidence below keeps its own source version.

L2 update, 2026-10-25: six original Redspire scatter variants now have a reversible default-off ordinary Free Roam lane. Pause > EXPLORE > Redspire kit: ON, then Route: Redspire art walk, after checking L1 Mirrorwell first. Only open-scatter HoodooBadlands boulders, spires and dead trees change. Source collision, 160 m chunks, all 17 discoveries and revision-10 saves retain their owners. Actual Unity import/gameplay/performance are NOT RUN. See `Docs/LiveWildL2/OWNER_GUIDE_L2.md` and `TEST_LEDGER_L2.md`.

L2 continuation: Pause > EXPLORE > Redspire kit: OFF enables the original modeled Redspire candidates. Route: Redspire art walk supplies an ordinary GPS destination. Only Redspire open scatter changes; named-site pieces, collision and discovery stay with their original owners. See `LIVE_WILDS_L2.md` for the native check and limits.

L1 update, 2026-10-25: Mirrorwell Oasis now has an original imported, solid stone pavilion through the existing LiveWorld owner. The five lands, 17 site IDs and discovery rewards below are unchanged. Pause > EXPLORE > Mirrorwell pavilion supplies its entrance route. V0 passed the owner's check; L1 native checks are in `Docs/LIVE_WILD_L1.md`.

# The wilds: five lands and seventeen places to find

V0 visibility repair: ordinary Play now prepares the live cast before starting; Pause > EXPLORE supplies normal GPS routes and F2 > Live characters retains failures. See `Docs/VISIBLE_REALITY_V0.md` for the five-minute check. Native acceptance remains pending.

Owner direction 2026-10-24: "I want the game map to be filled out more ... with better areas to explore". Before this, the
land between the city and the mountains (about 6 square kilometres) was flat sand with a few cactus, two mesas and ten small
sites. Now it is five lands with their own ground colour, rolling rises, landforms and scatter, and seventeen named places to
find, all generated from the city seed like everything else.

Nothing here was run in Unity by the build session (it has no Unity editor). The code compiles, the offline tests pass, and
the exact geometry the game builds was rendered offline in Blender (simple colours, no game lighting); how it looks with the
game's materials, light, fog and frame rate is yours to judge (section 4).

## 1. The lands

| Land | Where | What you see |
|---|---|---|
| Redspire Badlands | west | red earth, layered buttes, hoodoo spires with dark cap rocks, rock arches, boulder fields, dead trees |
| Whispering Dune Sea | south | wind dunes (gentle on one side, steep on the other), bleached ribs and skulls, dry grass |
| Glasslight Barrens | east | dark glass sand, quartz crystals and clusters that glow softly, basalt buttes |
| Saltbrush Steppe | north, and the band between the city and the beltway | dry turf, junipers, shrubs, rock outcrops |
| Rimewood | the Frostline's snow | ice spires, snow boulders, frozen dead trees (the pines and the lake stay as they were) |

The ground itself rolls: about 300 gentle rises (50 to 130 m across, 2 to 8 m high) that you can walk and drive over, and
everything standing on one sits on its surface. Coloured ground patches give each land its own floor.

## 2. The seventeen places

| Land | Places |
|---|---|
| Redspire Badlands | Hollowwind Maze (a maze of rock spires with two arches), Sunken Temple of Vey (a gate, obelisks, columns and walls), Circle of Nine Suns (standing stones round a glowing obelisk), Redwatch Tower (a lookout tower with a camp) |
| Whispering Dune Sea | Ribs of the Old Titan (a giant skeleton), Wreck of the Dawnstrider (a crashed skyship and its debris), Mirrorwell Oasis (a pond ringed with palms, reeds, a pier and ruins), Caravan Rest (nomad tents round a fire) |
| Glasslight Barrens | The Singing Grove (giant crystals round an obelisk), Breathing Flats (salt flats with geyser cones), Shardwatch Obelisks (five glowing obelisks) |
| Saltbrush Steppe | Emberfall Village (ruined adobe huts, walls, a well and a tower), Old Wind Relay (wind turbines and a relay hut), Saltroad Camp (nomad tents) |
| Rimewood | Frostmaw Cave (a snow butte with a dark cave gate and ice spires), The Icebound Barge (a frozen ship), Steamveil Springs (warm pools, a hut, dead trees) |

- On the map and the minimap every place shows as a grey "?" until found. Walking, riding or flying into it shows
  "Discovered: name (land, n of 17)", pays $300 and 120 Lumen, and the marker turns gold with the name.
- Finding all seventeen pays $6,000 and a skill point, once. Found places are saved (save block `Wilds`; old saves start
  with none found).
- The pause map and the minimap show the lands (tinted ground) and the bigger landforms.
- F2 > Wilds: the build summary and every place with a "Go there" button.

- The mountain ring around the map is now rugged faceted ridges (a main peak and two shoulders each, ragged snow caps on
  the northern ones) instead of smooth half spheres, from the same rock maths. Golden shots that see the horizon change.

## 3. How it works (for the curious)

- `WildsLayout` (pure, tested) plans everything from the city layout: the lands, the places (spread at least 230 m apart,
  beyond the beltway), the rises, the landforms and an even scatter on a 12 m grid. A blocked grid keeps every piece off the
  roads, the story sites, the desert lots, mesas, mountains, stunt jumps, points of interest, collectible, creature and
  pickup spots, the frozen lake, and whatever the game placed before: the forts and their courts, the gang territories and
  habitats, and the loot chests.
- `WildsSites` arranges each named place; `WildsShapes` builds every piece from code (faceted rocks, stacked buttes and
  spires, dunes, crystals, trees, palms, bones, ruins, tents, wrecks, obelisks, turbines, huts).
- `WildsBuilder` cuts the map into 160 m chunks: one mesh for the landforms and places (seen from far, with shadows), one
  for the small scatter (hidden at a distance, no shadows) and one collision mesh. All of it is on the World layer, so
  people, creatures, cars and the camera treat it like ground and rock.
- `WildsSystem` (Game.Wilds) builds it last at start-up, keeps the found places, the markers and the rewards; `WildsMap`
  paints the map.
- Size on the default city: about 18,500 pieces, 294 chunks, 3.5 million vertices in total (only the nearby chunks draw).
- The offline preview renders came from the real code run outside Unity.

## 4. What to check in Unity

1. Start Free Roam and open the map: the lands should show as coloured areas with grey "?" markers. The Console says
   `Wilds: 17 places, ... built in N ms`. Send that line if it is missing or says anything else.
2. F2 > Wilds > Go there on a few places (Mirrorwell Oasis, Hollowwind Maze, The Singing Grove, Ribs of the Old Titan,
   Frostmaw Cave): look at the shapes and materials, walk in to see the "Discovered" message, check the marker turns gold.
3. Drive a car or a bike across the dunes and rises: wheels should follow the ground, nothing should launch you.
4. Fly over the wilds at night: the crystals glow, the obelisk runes and campfires are lit.
5. Frame rate: stand on a rise and look across a land; compare with the city. If it drops, tell me the place and the
   graphics preset.
6. Things that might sit badly: a rock half inside a fort wall, a tree on a story marker, a piece floating at the edge of a
   rise. Send a screenshot and the F2 > Wilds line.

## 5. Current limits, honestly

- The shapes are low-poly and made from code with the city's materials; they are the stage for later art (an imported
  rock or tree set can replace pieces one kind at a time).
- The land rises are gentle mounds, not cliffs or valleys; nothing is carved below the old ground.
- Places are to look at and find; no new quests, enemies or creatures were added there yet (taming and creature dens are
  the natural next step).
- Story missions and events that pick spots at run time in the desert may now find a rock or a rise there; tell me where
  if one looks wrong.
