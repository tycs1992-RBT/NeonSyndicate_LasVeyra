# The loot world (Phase 5)

Chests, hidden satchels, Chest Keys, guard crews and a gunsmith quest arc, all feeding the Phase 4 loot guns. The idea
is a loop: the city is full of glowing chests worth a detour, the best ones are locked or guarded, the keys come from
fighting, and the hidden satchels reward the travel powers. Nothing here has been run in Unity yet: it compiles in all
twelve configurations and the placement, loot tables and quest data pass the offline checks, but how it looks and feels
needs your eyes (checklist at the end).

## Chests

140 chests stand across the map, placed once from the city layout and the seed (the same spots every game):

| Region | Common | Sealed | Veil Vaults |
|---|---|---|---|
| The city (parks, empty lots, courts, back yards) | 38 | 8 | 12 (only inside the Inner World) |
| The Quartz Badlands | 36 | 12 | 0 |
| The Frostline | 26 | 8 | 0 |

Chests never stand on roads or sidewalks, inside or against buildings, on the ridge, in a boss arena or inside a named
site (the motel, the airstrip, the outpost and so on). Downtown (Las Veyra Core, Mirage Strip, Sunken Row) is built
too densely for chests on the ground; its rooftops hold satchels instead.

- **Common chest**: a wood-and-glass pod. Walk up and press **E** (Y / Triangle on a controller). 1 or 2 guns (35%
  chance of a second), cash and one or two crafting materials, now and then a weapon core.
- **Sealed chest**: a black chest with brass bands and a glowing lock. It needs a **Chest Key**, and a guard crew
  appears when you come within 65 m (see below). 2 guns with better odds, more cash, materials, a 50% weapon core.
- **Veil Vault**: a violet crystal chest that only exists inside the Inner World (shift through the Veil to see it).
  2 or 3 guns, every one Epic or better, a Prism Core, Veil materials. Opening it wakes a pack of Hollows (and from
  level 12 an elite Mirror Wraith, the Vault Warden).

Guns come out of the chest as normal loot drops in their rarity beams (take with E, hold to equip, like any drop). They
count toward the pity rule. Item level: the player's level, +1 for sealed chests, +2 for vaults.

**Level appropriate odds.** Higher player levels move weight from Common and Uncommon up to Rare, Epic and Legendary
(up to level 41); Veteran favours Epic and better as it does for enemy drops.

| Chest, level 1 | Common | Uncommon | Rare | Epic | Legendary | Mythic |
|---|---|---|---|---|---|---|
| Common chest | 42% | 38% | 15.5% | 4% | 0.5% | 0 |
| Sealed chest | 0 | 14% | 52% | 27.5% | 6% | 0.5% |
| Veil Vault | 0 | 0 | 0 | 72% | 24.5% | 3.5% |

**Finding them.** A chest glows (a pulsing light, a halo on the ground and a soft column of light in its colour: teal
common, amber sealed, violet vault) and hums, so you can hear one before you see it. The minimap shows chests within
60 m. Once you have been within 60 m of a chest it is **discovered** and stays on the full map (+ common, K sealed,
V vault, grey once opened).

**Respawn.** An opened chest refills two in-game days later (96 real minutes at the normal clock). Opened chests and
when they were opened are saved.

## Chest Keys

Keys open sealed chests (one key each). Where they come from:

- the **crew boss** of a sealed chest always drops one, and every other crew member has a 25% chance (35% on Veteran);
- **champions** anywhere drop one 60% of the time (75% on Veteran) when you kill them;
- one satchel in three holds a key;
- **gun shops** sell them: Gun shop > Loot guns, the Chest Key row ($2,500 plus $100 a level, shop discounts apply).

A dropped key is a small spinning golden key in a light beam with a **k** on the minimap: walk over it to take it. It
fades after three minutes.

## Guard crews and the bounty board

Every sealed chest has a guard crew that steps out when you come within 65 m. The crew scales with your level: 3 guards
at level 1 and one more every 12 levels (at most 7), health up 3.5% a level (2.7 times at level 50), better guns as you
level. The leader is a Shard-Touched elite (the Crew Boss) before level 8 and a champion with affixes from level 8 on.
They hold their ground until they see you within 30 m or get shot. Crews leave when you are 230 m away (and come back
next time unless they were all killed), and never appear during missions.

**The bounty board.** Each in-game day three sealed chests are posted as bounties (a flashing red **B** on the map, even
before you find them). A bounty crew has one more guard, 25% more health and is always led by a champion. Killing the
whole crew pays $1,500 plus $120 a level. The Collector reads the board to you on the phone, and the F2 page lists it.

## Hidden satchels and the Collector

40 satchels hide in places the travel powers reach: 24 on flat rooftops (12 to 70 m up), 8 on the setback ledges of
the stepped towers in Glass Heights (47 to 100 m up, so Flight helps) and 8 down in the canal under the bridges. Each
holds a weapon core (every tenth a Prism Core) or, one in three, a Chest Key. Satchels never come back.

The **Collector** is a phone contact (after the first story mission). Call after finding 10, 25 and 40 satchels for a
guaranteed **Rare**, **Epic** and **Legendary** gun (into the backpack, or at your feet when it is full). Calling before
that tells you how many you have and where to look.

## The Third Eye loot sense

While the Third Eye is open (bullet time) a small LOOT panel under the top of the screen shows the nearest unopened
chest on your side of the Veil (kind, distance, compass direction, whether you have a key) and, within 60 m, the
nearest satchel ("above you" or "below you" when it is on a roof or under a bridge).

## The ARSENAL tab

The backpack header carries the tally: chests opened, satchels found, Chest Keys held, bounties open.

## The Gunsmith arc (8 quests)

Data in `Resources/Quests/loot_gunsmith.json`. Dante Rook's gunsmith **Oren Vasque** (Forgewright look) works at the
Gun Vault in the Ash Underpass and wants to know who seeds the city with chests. The arc belongs to the Forgewrights
(Orders 6 to 13) and opens after "The First Mythic": the shipped-data tests require each guild arc to be one chain in
Order, so it follows the Forgewrights' own five quests. F2 > Quests > Start runs any of them at once.

| Quest | Beat | Reward step |
|---|---|---|
| Wood and Glass | Jackals in the Old Silver park, strip the hinges from three broken pods | Rare pistol (GiveGun) |
| Maker's Marks | Read six makers' stamps at Battery Yard Scrap, hold off the mites | Rare gauntlets (Give gear) |
| Champion Steel | Read and break the Sand Golem Rustjaw at the Dust Devil Trailer Park | Epic shotgun (GiveGun) |
| Under Seal | Escort the courier Pell Varga and a key mould to Jax Calder Customs | Epic striders (Give gear) |
| The Prism Seam | Read and break the Crystal Basilisk Glasshide at the quarry mouth | Epic crystal caster (GiveGun) |
| Frost Temper | Snow Hollows east of the Frost Wardens Outpost, bring back frost essence | Epic shell (Give gear) |
| Cast Behind the Veil | Cross the Veil in the Glass Heights park, take the casting shards from the Hollows | Epic gun (GiveGun) |
| Masterwork | Hold the Gun Vault with two apprentices through three Hollow waves | **Skyline Requiem**, the Mythic unique (GiveGun) |

The new **GiveGun** quest step hands over a loot gun: `Rarity` (default Rare), `Archetype` (Pistol, HandCannon, Smg,
AssaultRifle, Shotgun, Sniper, Launcher, CrystalCaster; default any) or `Unique` (a unique's name, which sets rarity and
type itself). The validator checks every name and rejects a Rarity or Archetype that contradicts the unique. Unique is a
name rather than an index so the data still reads right if the unique list grows (an index of 0 also could not be told
apart from "not set" by the validator).

## Debug page

F2 > **Loot world** (after Loot and Travel): reveal every chest on the map, give 5 Chest Keys, respawn every chest, go
to the nearest unopened chest, go to the nearest satchel (straight onto the roof or into the canal), mark 10 satchels
found (then call the Collector), post a new bounty board, drop a Chest Key in front of you, call the Collector. The top
lines count chests by kind, open, discovered and opened in total, satchels found, keys, bounties and the game hour; the
bottom lists today's bounties with distance and direction.

## Saves

`SaveData.LootWorld` (`LootWorldSave`): opened chests with the game hour each was opened, discovered chests, found
satchels, crews down this cycle, keys, chests opened, Collector rewards claimed, the bounty board's day and posted
chests, bounties done. Old saves load with an empty block. Chests are saved by their index in the fixed placement, so
the placement must not change once shipped (or the saved chests land on different spots).

## Files

`Gameplay/LootWorldLayout.cs` (pure: chest and satchel spots), `LootWorldRules.cs` (pure: loot tables, respawn, guards,
bounties, keys, the Collector, the save block), `LootWorld.cs` (the system, streaming, saves), `LootWorld.Chests.cs`
(models, opening, loot), `LootWorld.Guards.cs` (crews, bounties, vault packs, key drops), `LootWorld.Caches.cs`
(satchels, the Collector), `LootWorld.Ui.cs` (Third Eye sense, ARSENAL tally, gun shop key row, F2 page),
`Quests/QuestSteps.Loot.cs` (GiveGun). Hooks: `Game.LootWorld`, the "Loot world" boot step, one line each in
`LootArmory.Ui` (tally), `LootArmory.Shop` (key row) and `Phone` (the Collector). Tests: `LootWorldTests.cs`.

## What to test in Unity

1. F2 > Loot world > Reveal, then open the map: K, + and V marks in the parks, the Badlands and the Frostline.
   "Go to the nearest chest": does the pod sit on the ground (not floating, not sunk), does it glow and hum, is the
   prompt right, does the lid swing open and the guns pop out in beams in front of it?
2. Walk around a park at night with the HUD on: is a chest findable by its light and hum from 40 to 60 m? Is the glow
   too much or too little? Does the minimap show it within 60 m?
3. A sealed chest without a key: the crew steps out, the boss is an elite or a champion, they drop keys, then unseal.
   With F2 > Give 5 keys, try a posted bounty (red B): one more guard, a champion boss, the BOUNTY CLAIMED banner.
4. Shift into the Inner World near a V: the vault appears, opens, the Hollows wake. Shift out: the vault and its pack go.
5. Hold the Third Eye open: the LOOT panel at the top (does it collide with anything else on the HUD?).
6. "Go to the nearest satchel" on a rooftop, a ledge and under a bridge: can you stand there, take it, and get back
   down? Mark 10 found, then Phone > The Collector: a Rare gun in the backpack.
7. Gun shop > Loot guns: the Chest Key row and its price.
8. Save, quit, load: opened chests stay open, keys and satchels kept, the bounty board the same.
9. The Gunsmith arc: F2 > Quests > Start on "Wood and Glass", then "Masterwork" (the unique banner at the end).
10. Golden shots will change wherever a chest stands in view (they glow): expected, not a regression.

Known limits: the opening is a lid swing and a flash, no animation of the player; chest guards are generic enemies (no
syndicate colours); nothing stops an NPC car from parking on a back-yard chest; the placement is checked against the
layout's buildings and roads, not the real colliders, so a few spots may need moving after a look in the game.
