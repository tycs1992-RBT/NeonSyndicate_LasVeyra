# City interiors: 28 places you can walk into

Owner choice 2026-10-24 (blueprint item: enterable interiors). Together with the twelve venues of the inhabited city
(Veyra Lofts, the clinic, the Forge, Cinder Diner, Jax Calder Customs and the rest) the city now has the blueprint's forty
enterable interiors.

Nothing here was run in Unity by the build session (it has no Unity editor). The code compiles, the offline tests pass,
and the rules were checked on the default city; how it looks and feels is yours to judge (section 4).

## 1. What you get

- Twenty-eight ordinary buildings in seven districts have a street door: a dark door with a glowing frame in the
  shop's colour, a lit sign with its name and a lamp. The map and the minimap show them as service markers with an "I".
- Walk up and press **E** (pad **Y**): the screen fades and you are inside. The door behind you leads back out (E or Y
  again). You cannot go in while wanted, in a fight, in a vehicle or in the Veil; the prompt says why.
- Eight kinds of place, each with its own room, furniture, lights, a keeper behind the counter and a few customers
  (ordinary townspeople, so the live crowd bodies dress them):

| Kind | Inside | The counter |
|---|---|---|
| Noodle bar | a long counter with stools, a steaming pot, two tables, hanging lanterns | a bowl of ember broth, $15, restores about half your health |
| Tea house | low tables with cushions, paper screens, a tea counter | a pot of glasswater tea, $10, restores a quarter |
| Gym | a boxing ring, punching bags, benches, a mirror wall | a sparring round, $25, a little health |
| Boutique | clothing racks, mannequins, a mirror | the keeper says a line |
| Pawnshop | display cases of trinkets, a caged counter, junk shelves | the keeper says a line |
| Bookshop | rows of shelves, a reading table with a lamp | the keeper says a line |
| Chapel | pews, an aisle runner, an altar with candles, a lit window | the keeper says a line |
| Arcade | two rows of game cabinets with lit screens, a prize counter | the keeper says a line |

- The first time you walk into a place the HUD says "Found: name (n of 28 places)". Visiting all of them pays $2,500
  once. The visits are saved (save block `Interiors`; old saves start with none visited).

## 2. The places in the default city

| Name | Kind | District |
|---|---|---|
| Ember Broth Counter | noodle bar | Las Veyra Core |
| Second Light Pawn | pawnshop | Las Veyra Core |
| The Coin Furnace | arcade | Las Veyra Core |
| The Ninth Lamp Chapel | chapel | Las Veyra Core |
| Spine and Ink | bookshop | Las Veyra Core |
| Dog-Ear Books | bookshop | Mirage Strip |
| The Quiet Kettle | tea house | Mirage Strip |
| The Resonance Ring | gym | Mirage Strip |
| Last Ledger Loans | pawnshop | Mirage Strip |
| Thread and Prism | boutique | Old Silver |
| Shrine of Still Water | chapel | Old Silver |
| House of Lumen Silk | boutique | Old Silver |
| The Steam Lantern | noodle bar | Old Silver |
| The Brass Hem | boutique | Old Silver |
| Pixelvault Arcade | arcade | Brass Projects |
| Odd Shard Exchange | pawnshop | Brass Projects |
| Ironlung Gym | gym | Sunken Row |
| Ninefold Noodles | noodle bar | Sunken Row |
| Neon Ladder Arcade | arcade | Sunken Row |
| Lanternpage Books | bookshop | Sunken Row |
| Glasswater Tea Rooms | tea house | Grave Frequency |
| The Margin Reader | bookshop | Grave Frequency |
| Hollowfist Boxing | gym | Grave Frequency |
| Copperjaw Pawn | pawnshop | Grave Frequency |
| Chapel of the Low Bell | chapel | Skyrail Orbit |
| Veilcut Tailors | boutique | Skyrail Orbit |
| Amberleaf House | tea house | Skyrail Orbit |
| Cinder Bowl | noodle bar | Skyrail Orbit |

A door whose building turns out to have no flat wall where the door should go (an arcade, a setback, a reshaped
gothic building) is left out at start rather than floating; the Console says how many doors were placed. F2 >
Interiors lists every place with a "Go to the door" button.

## 3. How it works (for the curious)

- **Which buildings**: `InteriorRules.Select` (pure, tested) takes ordinary one-tier buildings that are not a venue and
  have no story point of interest, finds the side whose door opens onto the public sidewalk, keeps doors 45 m apart and
  9 m from any story door, and lets the districts take turns so the places spread over the city. The same city seed
  always gives the same places, names and ids (`int-` plus the building seed), so saves stay valid.
- **The rooms**: `InteriorRooms.Plan` (pure, tested) builds each room from boxes and cylinders: a closed shell with the
  door in the front wall, the kind's furniture from a seeded random, ceiling lamps, the keeper's and customers' spots.
  The tests check that nothing pokes through a wall or the ceiling and that nothing solid blocks the doorway.
- **Where the rooms are**: each room is built 1,200 m straight above its own building (far above the 450 m flight
  ceiling) only while you are in it, and removed when you leave. The map, the minimap, quest distances and the save all
  keep pointing at the right street; a save made inside resumes on the sidewalk outside the door. Anything that moves
  you away (death, a mission start, loading) closes the room quietly.
- **Indoors**: inside a room the rain, snow and dust stop and fog thins (the same shelter the venues use), the camera
  stays close, the world region is Interior and the Veil will not open.
- Code: `Gameplay/Interiors/InteriorRules.cs`, `InteriorRooms.cs` and `CityInteriors.cs` (with `.Room` and `.Ui`);
  tests: `Tests/EditMode/InteriorRulesTests.cs`.

## 4. What to check in Unity

1. Start Free Roam, open the map and find an "I" marker near you (or F2 > Interiors > Go to the door).
2. The door: does it sit flat on the facade, at street level, with the sign readable from the sidewalk? Any door
   floating, buried or inside a wall: send a screenshot and the name.
3. Press E (pad Y): fade, then the room. Walk around: furniture, lights, the keeper and customers standing (not
   falling or stuck in furniture), no rain indoors.
4. At a noodle bar, tea house or gym counter, buy the item when hurt: money goes down, health goes up.
5. Leave by the door: you should stand on the sidewalk facing the street.
6. Save inside a room, load the save: you should resume outside its door.
7. Try to go in while wanted or in a car: the prompt should say why not.
8. Frame rate inside a room (it is a small scene) and walking the street with the doors around.

## 5. Current limits, honestly

- The rooms are built from simple boxes and cylinders with the city's materials; they read as places, not as finished
  art. They are the stage for later art passes.
- Keepers and customers stand idle; nobody sits on the stools yet.
- Only food, tea and the gym sell something; the other counters give a line. Boutique outfits, pawning and books are
  natural next steps.
- A companion following you stays on the street while you are inside.
- Glass Heights and the Ash Underpass have no interior on the default city (no building there passed the door rules).
