# Quests, contracts and world events

Phase 3 adds guild quest arcs, repeatable contracts and random world events. All three are **data**: JSON files read at
runtime by one generic quest runtime and one event director. Writing a new quest means writing a JSON file, not code.
Everything is checked by a validator (an EditMode test, a button in the F2 debug menu and an offline tool) before it ships.

This page covers how it works, the file format with an annotated example, how to add a quest, what ships, and what to
test first in Unity.

## How it works

### Quests (Game.Quests, QuestBook)

- **Givers.** Each guild has one giver, standing beside its contact: Iris Moon (Frequency Order), Dante Rook
  (Forgewrights), Mara Solane (Menders) and Captain Rhosyn Hale inside the Frost Wardens Outpost. While a giver has work, a
  glowing marker stands there and the map shows a **Q** in the guild's colour. Walk up and press **E** (Y on a
  controller) to hear the offer, then choose ACCEPT or NOT NOW. A new quest becoming available is announced once with a
  short HUD note.
- **Unlocking.** Every arc opens after the first story mission is passed and the Third Eye is yours. The Frost Wardens also
  want you to have reached their outpost in the Frostline once. Later quests need the previous quest of the arc, and the
  last quest of each arc also needs some reputation with the guild (always reachable from the arc's own rewards).
- **Running.** An accepted quest runs as a mission through MissionManager, exactly like story missions: one at a time,
  HUD objective, GPS target, markers and blips, dialogue, cinematics, the fail screen with **Retry** (Enter or hold A
  within 12 s; retry restarts the quest from its giver). No quest can start during another mission, a cutscene, a menu
  or while you are wanted.
- **Tracker.** While a quest runs, a line under the HUD objective shows the guild, the quest title and the phase or
  "step 3 of 9". The count follows the route the quest takes: the longest way on from the current phase, recounted
  whenever a phase starts, so a Choice's other branch is never counted and the last step reads "9 of 9".
- **Ending.** Passing shows QUEST COMPLETE and pays the reward: cash through Economy, Lumen through Progression,
  materials, reputation, and skill points. `QuestCompleted(questId)` is raised for guild quests (never for contracts).
  Failing shows QUEST FAILED; after the retry window the quest stays accepted and can be restarted from the giver or
  the journal ("Last attempt" remembers why; a failed contract never overwrites it). **Abandon** in the JOURNAL stops
  the running quest or contract at once, with no retry prompt; abandoning a contract keeps the accepted guild quest.
- **Skill points.** Rewards go to the skill trees through `Game.Skills.Grant(int)`. Points earned while no skill tree runs
  are banked as `UnclaimedSkillPoints` (saved, `SkillPointsPending(total)` is raised, the journal shows them) and are
  handed to the skill trees automatically as soon as they run: a frame after a save is loaded, or within a second.
  `Game.Quests.TakeUnclaimedSkillPoints()` still hands them to any other caller.
- **Saving.** Saves keep completed quests, visited sites, banked skill points, the contract boards and the recent world
  events. Saves happen outside missions, so a quest is never saved halfway: an accepted, unfinished quest restarts from
  its beginning at its giver (marked "[restart]" in the journal).

### Contracts (the guild boards)

Next to each giver stands a contract board (a **C** on the map once the guild's arc is open). Read it with **E** (Y) to
see three posted jobs and take one. Jobs are generated from templates: a **Hunt** (a species and a hunting ground in its
habitat), a **Rift** to close in the Outer City, an **Escort** to a destination, or a **Courier** run (with a time limit
on some). Pay scales with the danger of the quarry (health, damage, tier and count), the trip length and how many
contracts you have done for that guild, always between $50 and $12,000. A finished job is replaced, and the boards post
fresh jobs every in-game day. Contracts run on the same step runtime as quests.

No contract sends you into a boss arena: hunting grounds and destinations stay at least 30 m outside every arena
(`QuestPlaces.BossArenas`, the same arenas the creature director uses; the quarry's hunts ring the quarry outside the
Foreman's floor), and a destination inside an arena is passed over for the next one in the template's list. A courier
ambush waits at the halfway point and only hunts you at once if you are within 60 m of it when it appears; otherwise it
fights when it notices you.

### World events (Game.Events, WorldEvents)

- The director picks a template every 75 to 135 seconds (the first one about 50 s into a session), weighted by
  `Weight`, among the templates allowed by the district (City, the Badlands, the Frostline, or inside the Inner World), the
  hour, the weather, story progress, the Third Eye and each template's cooldown (10 minutes by default).
- The event appears 80 to 160 m from you on its anchor (a sidewalk, a parking lot, the canal, the snow, the sand, a named
  site...), with a small callout at the top of the screen and an **E** on the map. It waits dormant until you come
  close, then plays out. At most two run at once. No anchor point is ever inside a boss arena or 30 m around it; a site
  that is an arena (the quarry) offers points on the ground ringing the arena instead of its centre.
- Events never start, and running ones end quietly, during story missions, quests, side activities (taxi, vigilante and
  paramedic jobs are fine), cutscenes, cinematics and menus, while you are wanted, or with the phone open. Events respect
  the Veil: an Inner World event fades if you leave the Inner World, and the other way round, and its creatures go with
  it at once.
- Ignored events fade after 4 minutes; events you leave far behind clean themselves up. Distances are measured from what
  the event is about now: an Escort follows the person you are walking home, who stays with the city when you get her
  there (no sudden vanishing). Rewards are cash, Lumen, materials and reputation. `EventCompleted(eventId)` is raised on
  success.
- Pack species (Glass Mites, Neon Jackals, Hollow Shades and other non-humanoid pack hunters) spawn as one pack through
  `Game.Creatures.SpawnGroup` (shared pack behaviour, one shared swarm body for mites); other creatures arrive one a
  frame. The same holds for quest steps.
- The JOURNAL lists events happening now (with a waypoint button) and the last 12 that happened.

### The JOURNAL and the debug tabs

- **Pause menu > JOURNAL** (LB and RB switch pause tabs, the D-pad or mouse picks buttons, B goes back): ACTIVE (the
  running or accepted quest and its objective, waypoint, abandon), AVAILABLE (every open quest plus the next locked one of
  each arc, with "Set waypoint"), COMPLETED (by arc, with reputation and contracts done), CONTRACTS (each board's
  posted jobs and a waypoint to the board) and WORLD EVENTS.
- **F2 debug menu > Quests**: start, complete or reset any quest, abort the running quest, reset all quests, mark every
  site visited (opens the Frost Wardens), post new contracts, and **Check quest data** (runs the full validator in game
  and prints the result in the Console).
- **F2 debug menu > Events**: switch the director on or off, end all events, spawn a random event now, reset cooldowns,
  cross the Veil, and **Fire** any template 20 to 90 m from you (district rules ignored, the Veil crossed for you if the
  template lives on the other side). Each row says why the template could or could not appear right now.

## Files

| Path | What it holds |
|---|---|
| `Assets/NeonSyndicate/Resources/Quests/*.json` | Guild givers, quests and contract templates (any file may hold any of them) |
| `Assets/NeonSyndicate/Resources/Events/*.json` | World event templates |
| `Scripts/Runtime/Gameplay/QuestBook.cs` (+ `Quests/QuestBook.Givers.cs`, `Quests/QuestBook.Ui.cs`) | Loading, unlocking, starting, rewards, saves; givers and boards; the journal, tracker and Quests debug tab |
| `Scripts/Runtime/Gameplay/Quests/QuestData.cs` | The data classes and the defaults (`QuestDefaults`) |
| `Quests/QuestRunner.cs`, `QuestSteps*.cs` | The phase runner and one coroutine per step type |
| `Quests/QuestCatalog.cs` | Every allowed name and which fields each step type and event kind reads |
| `Quests/QuestPlaces.cs`, `QuestWorld.cs`, `QuestProps.cs`, `RiftTear.cs` | Place words to map points, spawning and rewards, set dressing, the rift |
| `Quests/QuestLogic.cs`, `QuestValidator.cs`, `QuestJson.cs` | Pure rules, the validator and the strict engine-free JSON reader |
| `Scripts/Runtime/Gameplay/WorldEvents.cs` (+ `Events/*.cs`) | The director, the event kinds, the selector and the callout and Events debug tab |
| `Tests/EditMode/Quest*Tests.cs`, `WorldEventTests.cs` | Data, logic, selector and save tests |

## The file format

Files are JSON read with `JsonUtility` in the game: public fields only, names are case sensitive, no comments, no
trailing commas. **JsonUtility silently ignores misspelled fields**, which is why the validator reads every file a second
time with its own strict reader and reports unknown fields ("did you mean ..."). A number left out (or 0) means "use the
default" from `QuestDefaults`; text left out means "use a sensible default" (objectives, prompts).

### Annotated example

The JSON itself cannot hold comments, so the notes are below the example.

```json
{
  "Format": 1,
  "Givers": [
    { "Guild": "menders", "At": "Mara Solane", "Name": "Mara Solane", "Board": "Clinic Notice Board",
      "Greeting": "Jobs for people who don't faint at blood." }
  ],
  "Quests": [
    {
      "Id": "md_example",
      "Title": "Cold Box",
      "Arc": "menders",
      "Order": 9,
      "Summary": "Fetch the cold box from the trailer park before the jackals do.",
      "Done": "The medicine made it home.",
      "Requires": { "Quests": [ "md_medicine_run" ] },
      "Phases": [
        {
          "Id": "fetch",
          "Title": "The trailer park",
          "Steps": [
            { "Type": "Talk", "Lines": [ { "Speaker": "Mara Solane", "Text": "Bring it back cold." } ] },
            { "Type": "GoTo", "At": "TrailerPark", "Radius": 40 },
            { "Type": "Kill", "Species": "neon_jackal", "Count": 4 },
            { "Type": "Interact", "Prop": "Crate", "OffsetX": 3, "Prompt": "Press E to take the cold box" }
          ]
        },
        {
          "Id": "home",
          "Title": "Back to the clinic",
          "Steps": [
            { "Type": "GoTo", "At": "Mara Solane Street Clinic", "Radius": 8 },
            { "Type": "Choice", "Text": "Keep a dose for yourself?", "Options": [
              { "Label": "Hand it all over", "Phase": "END" }, { "Label": "Keep one", "Phase": "greedy" } ] }
          ]
        },
        {
          "Id": "greedy",
          "Next": "END",
          "Steps": [ { "Type": "AddRep", "Faction": "menders", "Amount": -3 }, { "Type": "Give", "Lumen": 50 } ]
        }
      ],
      "FailIf": [ { "Type": "TimeLimit", "Phases": [ "home" ], "Seconds": 300, "Message": "The medicine spoiled." } ],
      "Reward": { "Cash": 1500, "Lumen": 120, "Rep": [ { "Faction": "menders", "Amount": 5 } ] }
    }
  ]
}
```

- **Givers**: one per guild. `At` is a POI name or a site key; with no offset the giver stands 4.5 m beside the POI's door
  so story markers on the POI stay clear. `Board` titles the contract board, `Greeting` is said when it is read.
- **Id** is lower case with underscores and is what saves remember: never rename a shipped id. **Arc** is the guild's
  faction id and **Order** its place in the arc (sorting). **Giver** (left out here) can name another POI or site.
- **Requires**: `Quests` (ids), `Story` (story missions passed; free roam counts as all), `ThirdEye`, `InnerWorld`,
  `Visited` (a site key reached once), `Faction` plus `Rep` (minimum reputation).
- **Phases** run in file order. `Next` jumps to another phase or to `END`. A **Choice** step ends its phase and jumps to
  the phase of the chosen option (or `END`). The validator rejects phases that can never be reached and loops that can
  never finish.
- **Places** (`At`, and `To` for a second place): a POI name ("Mara Solane Street Clinic"), a site key ("TrailerPark"),
  `Giver`, `Player`, `Previous` (the default: the place of the step before), `District:SunkenRow` (a sidewalk in that city
  district) or `World` (the map origin, so the offsets are map coordinates). `OffsetX` and `OffsetZ` are metres along
  the world axes (+X east, +Z north).
- **Veil** on a step: `Inner` or `Outer`. The player is asked to shift first (the guild carries them over if the Veil is
  still closed to them, and tops up the crystal bar if it is too low). `HoldVeil` stops the energy drain during the step.
- **FailIf**: `AllyDies`, `TimeLimit` (whole quest, or the listed phases), `LeaveArea` (`At`, `Radius`, grace `Seconds`),
  `LeaveVeil` (grace `Seconds`), `Wanted` (`Stars`), `Hour` (`HourMin` to `HourMax`, wrapping past midnight) and
  `Vehicle` (must stay on foot). Dying and being arrested fail every quest anyway.
- **Reward**: `Cash`, `Lumen`, `SkillPoints`, `Materials` (`Id`, `Count`) and `Rep` (`Faction`, `Amount`).

### Step types

Every step may set `Objective` (the HUD text), `Veil` and `HoldVeil`. Bold fields are required.

| Type | What happens | Fields |
|---|---|---|
| GoTo | Reach a place (on foot if `OnFoot`) | At, OffsetX/Z, Radius (6), OnFoot |
| Talk | Plays dialogue. With `Look` a person spawns there first and you press E to talk | **Lines**, At, Look, Name, Prompt, Prop |
| Interact | Press E at one spot or `Count` spots around it (props light up for Brazier, Campfire, Shrine; the pickups Cache, Satchel, Crystal, Mote and Feather vanish once used) | At, Prop, Count, Radius, Prompt, Text, Name, Lines |
| Kill | Kill `Count` of a species near a place, at most `MaxAlive` at once (a pack species arrives as packs). A creature lost far away is removed and replaced; a spawn that fails is tried again | **Species**, Count, MaxAlive (6), Radius (14), Elite, HealthScale, DamageScale, Name |
| Boss | Travel to the arena (with no At: the creature director's own arena), then the boss's own fight from `Game.Creatures.SpawnBoss`: phases, ring attacks, its summons, the boss bar and banners. AddSpecies and AddCount (adds at two thirds and one third health) are used only by the stand-in while no creature director runs. A quest that ends with the boss standing ends its fight and clears its summons | **Species**, At (default: the boss's arena), AddSpecies, AddCount, Name, HealthScale, DamageScale, Lines |
| Collect | Hold or gather materials; `Caches` scatters glowing caches; `Spend` hands them over | **Material**, **Count**, Have, Spend, Caches, Prop, Radius |
| Survive | Last `Seconds`, with optional waves of a species (the timer holds while the Veil pauses the step) | **Seconds**, Species, Count, Waves, Elite |
| Defend | Hold a zone through waves; `Allies` people to protect cower inside, and every third attacker goes for them | **Species**, Count (4), Waves (3), Seconds (pause), Allies, Look, Name, Radius (18), Elite |
| Escort | Meet a person, then bring them to `To`; ambushes on the way | **To**, **Name**, Look, Health, Pacifist, Species, Count, Waves, ToRadius, Lines |
| Hunt | Read `Clues` tracks from `At` toward `To`, then kill the quarry there | **Species**, **To**, Clues (3), Name, Elite, HealthScale, DamageScale, Lines |
| CloseRift | Rifts (`Count`) spit creatures until their crystal core is destroyed | Count, Species (hollow), AddSpecies, Seconds (spawn interval), MaxAlive, Health (650) |
| Scan | Read a creature with the Third Eye (in view, within 32 m). A Kill of the same species right after fights that same creature. Inside the Inner World (where nothing refills the crystal bar) the bar is topped up whenever it is too low for one reading | **Species**, Seconds, Name, Elite, HealthScale |
| Shift | Cross the Veil (`Inner` true or false); `Force` does it at once. Its Objective is shown while it waits | Inner, Force |
| Wait | Pause | **Seconds** |
| Spawn, Ally, Prop | Instant: creatures (hunting you at once only within 60 m, else they fight when they notice you), a following ally, set dressing | Species / **Name** / **Prop**, At, Count, ... |
| SetTime, SetWeather | Instant: clock hour, weather name (Clear, Cloudy, Rain, Storm, Fog, Sandstorm) | Hour / **Weather** |
| Give, AddRep, Message | Instant: materials, cash, Lumen or a gear piece (`Gear` slot and `Rarity`, rolled by the gear system); reputation; a note or a banner | Material, Count, Cash, Lumen, Gear, Rarity / **Faction**, **Amount** / **Text**, Banner, Name, Seconds |
| GiveGun | Instant: a loot gun into the backpack (at the player's feet when it is full), rolled at the player's level as a champion drop. `Unique` (a unique's name, for example "Skyline Requiem") sets rarity and type itself | Rarity (Rare), Archetype (Pistol, HandCannon, Smg, AssaultRifle, Shotgun, Sniper, Launcher, CrystalCaster; any), Unique |
| Cinematic | Camera at `At` (raised by `Height`) looking at `To`, with optional lines | At, To, Height (4), Seconds (4), Lines |
| Choice | A pausing choice of 2 to 4 options, each leading to a phase or END | **Text**, **Options**, Name |

Names the data may use: species ids (CreatureSpeciesDb), material ids (MaterialDb), faction ids (FactionDb), site keys
and POI names of the default city. Looks: Civilian, Patient, Warden, Mender, Forgewright, Seer, Preacher, Prospector,
Biker, Pilgrim, Echo, Courier, Hunter, Worker, Mara, Ion, Luma, Iris, Dante. Props: Crate, Cache, Satchel, Wreck, Boat,
BusStop, Brazier, Shrine, Jammer, Crystal, Feather, Mote, Board, Altar, Anvil, Scaffold, Cones, Campfire, Tracks, Soapbox,
Bike, Stall, Totem, Forge. Bosses only appear in Boss steps, and never in contracts or world events. A Boss step with no
At fights at the boss's arena: Titan Hollow, the quarry floor, Crystal Manor (inside the Inner World) or the Mirage Flats
deep in the south-east Badlands (the Mirage Djinn; the Starlite Motel is the nearest named place, which is what
`QuestCatalog.BossArena` names for it). Keep other fights and scans of hand-written quests outside the arenas
(QuestFixTests checks the shipped ones): a found arena starts its boss fight when you walk in outside a mission.

### Contract templates

`Id`, `Guild`, `Kind` (Hunt, Rift, Escort, Courier), `Title` and `Brief` (with `{species}`, `{count}`, `{place}` and
`{dest}`), `Weight`, `MinRep` (reputation before the board posts it), `Species` (the quarry, the rift's creatures, or
the ambush on an escort or courier run), `CountMin` and `CountMax`, `Destinations` (POI names or site keys),
`Look` and `Name` (the escorted person), `Seconds` (courier time limit), base `Cash`, `Lumen`, `Rep` and `Materials`.

### World event templates

Common fields: `Id`, `Title`, `Kind`, `Callout`, `Objective` (status line), `Success`, `Failure`, `Weight`, `Districts`
(district names, `City`, `Wild` or `Any`; empty means the city), `Veil` (empty or `Outer`, `Inner`, `Any`), `HourMin`
and `HourMax` (wrapping past midnight), `Weathers`, `Story`, `ThirdEye`, `Cooldown` (600), `Duration` (240), `Anchor`
(Sidewalk, Road, Parking, Park, Plaza, Lot, Canal, Lamp, Desert, Snow, Any or `Site:Key`), `Lines` and `Reward`.

| Kind | What happens | Extra fields |
|---|---|---|
| Rescue | Bystanders attacked by creatures or a crew; save them before they fall | **Civilians**, Species, Count, Look, FactionA, Guards, Prop, Seconds, Elite, HealthScale |
| Loot | Goods guarded by creatures (take them after) or a crew (rob them) | Species, Count, FactionA, Guards, Prop (Boat floats in the canal), Points, Elite |
| Talk | A character with something to say | **Look**, **Name**, **Lines**, Prop |
| Brawl | Two syndicate crews fight; hit one to back the other (reputation swings by Amount) | **FactionA**, **FactionB**, Count, Amount, Seconds |
| Relight | Light `Points` braziers around a shrine before the time runs out | **Points**, Seconds, Species, Count, Prop |
| Swarm | A pack to clear | **Species**, Count, Species2, Count2, Elite, HealthScale |
| Rift | A rift to close | **Species**, Species2, Health, Count, Seconds |
| Escort | Someone to walk home (default: the nearest clinic or hospital) | **Look**, **Name**, To, Species, Count, Health |
| Rising | A beast rises from the ground among bystanders | **Species**, Elite, HealthScale, Civilians, Look, Seconds, Prop |
| Pickup | Grab an item before the time runs out, a creature returns or a crew gets there | Prop, Seconds, Species, Count, FactionA, Guards |
| Sighting | A dangerous beast to avoid, or hunt for the bounty | **Species**, Elite, HealthScale, Prop |
| Impostor | A Mirror Wraith hides in a crowd; the Third Eye reveals it | **Civilians**, **Species**, Look, Elite |
| Sabotage | Destroy a device or pull its wires, guarded by a crew | Prop, Health, FactionA, Guards, Species, Count |
| Storm | Gather falling Lumen motes | **Points**, Seconds, Species, Count |
| Holdout | Hold a spot for `Seconds` through waves | **Seconds**, **Species**, Count, Civilians, Look, Points, Prop |

## Adding a quest as data

1. Pick the guild (its `Arc`) and the next `Order`. Write the quest in that guild's file, or a new file in
   `Resources/Quests` (file names do not matter; every `.json` there is loaded).
2. Set `Requires.Quests` to the quest it follows. Give it phases of steps from the table above. Use places from the city
   (the map shows POI names; the site keys are StarliteMotel, Airstrip, Canyon, Quarry, TrailerPark, CrystalManor,
   Blockade, FrostLake, WardenOutpost, TitanHollow).
3. Check it: in Unity open Window > General > Test Runner and run **QuestDataTests**, or in the game open F2 > Quests >
   Check quest data. The messages say the file, the line and what is wrong ("unknown species 'holow' (did you mean
   'hollow'?)").
4. Try it: F2 > Quests > Start teleports you to the giver and starts it, ignoring its requirements.

Rules the validator enforces: no em or en dashes in any text, plain ASCII is recommended, objectives up to 110
characters, titles up to 42, dialogue lines up to 260; every field must be read by its step type; numbers in range;
rewards within limits (quests: $60,000, 10,000 Lumen, 3 skill points, 40 reputation per faction; events: $10,000, 3,000
Lumen, 1 skill point, 15 reputation).

AI sessions without Unity also run an offline checker (a small dotnet program kept outside the project) that reads every
file with System.Text.Json in strict mode and with QuestJson, checks the two agree, runs the validator, and adds
whole-city checks: fixed places not inside buildings or mountains, 40 generated jobs per contract template, and every
event template pickable and placeable somewhere.

## What ships

### Guild arcs (19 quests)

| Arc | Quest | Beat |
|---|---|---|
| Frost Wardens (Captain Rhosyn Hale, Frost Wardens Outpost) | The Hunting Trial | Read a Snow Hollow with the Third Eye, kill three in the snowfields |
| | The Griffin of Rimeglass | Snow Hollows raid the griffin's nest at Rimeglass Lake; spare Old Whitecrest or take its heart |
| | Night Raid | Hold the palisade at night with wounded Wardens, then burn the Snow Hollow den |
| | Tracks in the Rime | Track the Frost Stalker Greyhush across the snow (four clues), then kill it |
| | The Titan Wakes | Boss: the Glacier Titan in Titan Hollow, with a Warden ally |
| Frequency Order (Iris Moon) | First Walk Behind the Veil | Cross the Veil in a park, gather lumen motes, read and destroy Hollow Shades |
| | Three Tears in the Static | Close three rifts: the Static Saints Tavern, the Signal Hill Radio Shack, the cemetery |
| | Shades of Grave Frequency | Tune the old transmitter at night, destroy the shades it calls; smash it or leave it for the Saints |
| | The Mirror Duel | Duel your own reflection in the Inner World outside Veyra Lofts |
| | The Queen of Glass | Boss: the Mirror Wraith Queen in the Inner World's Crystal Manor |
| Forgewrights (Dante Rook) | Raw Materials | Scavenge mite dust and shard at Battery Yard Scrap Co., survive the swarm |
| | Infestation at the Kiln Yard | Light the work lamps, crush the mite swarm, hold the yard |
| | Old Slab | Read and break the Shard Golem Old Slab at the mouth of the quarry (clear of the Foreman's floor) |
| | The Foreman's Last Shift | Boss: the Shard Golem Foreman in the quarry |
| | The First Mythic | Bring the binding materials, hold the hammer while the core sings, the first mythic forging |
| Menders (Mara Solane) | Medicine Run | Fetch the cold box from the Dust Devil Trailer Park, drive it back in five minutes |
| | Outbreak at the Clinic | Defend the clinic and its patients from Hollows, close the tear by the canal |
| | The Night Patient | Walk a bitten Static Saint through Grave Frequency at night, on foot |
| | Heart of Sand | Find the prospector at the Starlite Motel, break the Dune Sleeper, bring home its core |

"The First Mythic" ends with a Mythic Core gear piece rolled by the gear system (`GearSystem.Generate` at the player's
current power; a full bag takes it anyway) and raises `QuestCompleted("fg_mythic_forging")`. The Forge is another
system: it can also tie a recipe to that id (`ForgeDb.RegisterQuestUnlock`).

### The Gunsmith arc (8 quests, Phase 5)

`loot_gunsmith.json`: Dante Rook's gunsmith Oren Vasque at the Gun Vault in the Ash Underpass (each quest names its own
Giver place). Forgewrights arc, Orders 6 to 13, so it opens after "The First Mythic" (each guild arc is one chain in
Order). Every quest ends with a GiveGun or a gear Give; the last, "Masterwork", hands over the Skyline Requiem. Details
and the beat list: `Docs/LOOTWORLD.md`.

### Contracts (19 templates)

Frost Wardens: stalker bounty, Snow Hollow cull, rogue griffin (20 reputation), scout escort, supply run. Frequency
Order: rift closing, mirror rift (25 reputation), shade hunt, beasts behind the Veil (15 reputation), tuning fork
delivery. Forgewrights: golem salvage, mite pest control, Quartz Drake trophy (30 reputation), hot parts courier,
apprentice escort. Menders: patient escort, medicine courier, jackal cull, dream tear.

### World events (46 templates)

City (21): Hollows at the Bus Stop, Crashed Courier (jackal guards), Preacher of the Third Eye, Recruitment Brawl, Mites in
the Parking Lot, Rift Incursion, The Wrong Neighbour (a Mirror Wraith imitating a civilian), Static Saints Jammer, Canal
Choir Smugglers (a boat in the canal), Jackals in the Park, Shades Over the Graves, Golem in the Lot, A Lost Echo, Mender
Under Siege, Falling Crystal (Roof Oni race you for it), Lotus Payroll Van, Courthouse Scuffle, Rivals on the Embankment,
Lanterns Out on the Row, The Fortune Teller, Night Market Panic.

Frostline (9): The Frozen Shrine (relight it), Wounded Warden (escort her to the outpost), Griffin Feather, Frost Stalker
Sighting, Snow Hollow Pack, Stranded Campers, Buried Supply Cache, Warden Camp Under Siege, The Ice Pilgrim.

Badlands (9): Sand Golem at the Trailer Park, Dune Wyrm Warning, Crashed Supply Truck, Sun Griffin Feather, Roadside
Brawl, The Desert Prophet, Drake on the Rim, Sandstorm Shelter (only in sandstorms), Quarry Mite Bloom.

Inner World (7): Lumen Storm, A Wound in the Mirror, Basilisk in the Glass Streets, Wyvern Dive, An Echo That Remembers,
Lost in the Veil, Falling Glass Star.

## What Tyler should test first

Nothing here has been run inside Unity yet: it compiles in all twelve configurations and the data, rules and selection
pass the offline checks, but how it plays has to be seen in the game.

1. **Run the tests.** Test Runner > EditMode: QuestDataTests, QuestLogicTests, WorldEventTests and QuestSaveTests should
   be green, including the two NeedsEngine tests in QuestSaveTests (they prove JsonUtility reads the data exactly like the
   validator). Then F2 > Quests > Check quest data should say "0 errors, 0 warnings".
2. **The first quest end to end.** Free Roam (or a save after the first story mission). Walk to Mara Solane's clinic:
   is the Q marker beside the door, does E (and Y on a controller) offer "Medicine Run", does the tracker line sit under
   the objective, do the trailer park, the jackals, the crate and the timer work, and does QUEST COMPLETE pay out?
3. **Fail and retry.** Let the timer run out: QUEST FAILED, then Retry within 12 s. Then fail again, wait, and restart it
   from the giver and from the JOURNAL.
4. **The JOURNAL with a controller only**: all five tabs, Set waypoint, the contract list, paging.
5. **A contract** from Mara's board (E on the board): take one, finish it, check the board posts a new job.
6. **World events.** Walk around the city for five minutes: a callout should appear, an E on the map, and the event
   should play out when you get close. Then F2 > Events > Fire a few of each kind (Brawl, Loot with the Boat, Impostor,
   Storm inside the Inner World) and check they clean up when you drive away. Start a story mission next to a running
   event: the event must vanish.
7. **The Veil quests**: "First Walk Behind the Veil" (the shift prompts, the motes, the LeaveVeil fail) and "The Mirror
   Duel".
8. **A boss**: F2 > Quests > Start on "The Titan Wakes" (or any boss quest). The boss bar and its phase label should
   show, the boss should use its ring attacks and summon its own help (no extra adds from the quest), and there should be
   one banner when it appears and one when it falls. Then fail the quest mid-fight (die, or abandon it from the JOURNAL):
   the boss and every summon must leave.
9. **Wounded Warden** (Frostline event): walk her all the way to the outpost; the event must not vanish on the way, and
   she should stay standing at the outpost afterwards instead of disappearing.
10. **A courier contract with an ambush** ("Tuning fork for..."): the shades should wait halfway, not run to the board.
11. **"Old Slab"** and **"Quarry Mite Bloom"** now happen at the mouth and around the outside of the quarry, never on the
   Foreman's floor: check the spots look sensible and nothing spawns inside the rock walls.

Known limits:

- **Creatures.** Quests and events spawn creatures through `Game.Creatures` (`Spawn`, `SpawnGroup` for packs,
  `SpawnBoss` for Boss steps). Only while the creature director cannot spawn does a humanoid stand-in with the species'
  name, health and damage take a creature's place (logged once as a warning). Tagged creatures are cleaned up
  (`DespawnTag`) after quests and events; a boss also has its fight ended and its summons cleared.
- Givers are a marker and a prompt, not a person standing there (story contacts are spawned by the story missions).
- Brawls use the syndicates' street gangs, so hitting a crew member also raises gang heat and possibly your wanted level.
- Positions were checked against building footprints and mountains, not against the real colliders: a few spots
  (Defend zones, the den, the Starlite Motel dunes) should be eyeballed.
