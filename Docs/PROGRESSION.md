# Progression: factions, the Forge and the skill trees

Three systems that make the hours you put in show: **reputation** with every faction of Las Veyra, the **Forge**
that turns creature materials into gear and upgrades, and the **Body, Mind and Shard skill trees**. All three save
with the game, work with mouse, keyboard and controller, and switch themselves off quietly if something they need
is missing (no creatures yet, no quests yet: nothing breaks, those parts simply stay idle).

They meet in one place: every bonus from a learned skill, a Forge upgrade, an active brew or a guild perk is added
into `SkillMods` (Gameplay/SkillMods.cs), and the game reads those numbers wherever damage, stamina, energy, prices
and so on are worked out.

| Where in the game | What |
|---|---|
| Pause menu > SKILLS | The three skill trees: learn skills with your points |
| Pause menu > FACTIONS | Every faction, your standing, what it gives you now and at the next rank |
| Resonance Shrine menu > SKILLS | The same trees, plus RESET ALL TREES for Lumen |
| Dante Rook, Old Silver (orange **A** on the map) | The Forgewrights' full forge, with SALVAGE |
| Frost Wardens Outpost, the Frostline (blue **A** on the map) | A full forge, with the Wardens' shop |
| Beside every Resonance Shrine | A small shrine forge: brews and infusions |
| Mara Solane's corner, Sunken Row (a green cross sign) | The Menders: healing and donations |
| Iris Moon, Glass Heights (violet **~** on the map) | The Frequency Order: Veil Attunement and rift shard offerings |

The two guild contacts stand on the left of their POI's door (like Dante Rook's forge), 4 m from every other prompt
(2.2 m on a crowded corner) and 4 m from the spot where the guild's quest giver stands (to the right of the door), so
two "Press E" prompts do not overlap. Only if no such spot exists does a contact fall back to the old placement, and
the log says so.
| HUD, top left | Reputation toasts ("+3 Frost Wardens (24) killed a Frost Stalker") and active brews with time left |
| F2 > Progress | Debug buttons for all of it (see the end) |
| Pause menu > GEAR, HUD bottom right, F2 > Gear | Phase 6 builds: set bonuses, gear score and the city tier (section 8) |

Forges and guild contacts cannot be used during a fight or while you are wanted.

---

## 1. Factions and reputation

Reputation runs from **-100 to +100** for each faction. Everyone starts at 0.

### The factions

| Faction | Kind | Home turf | Rival | Contact |
|---|---|---|---|---|
| Ember Court | Syndicate (gang 0) | Ash Underpass | Brass Tower Union | |
| Quartz Kings | Syndicate (gang 1) | Quartz Badlands | Glass Jury | |
| Mirage Lotus | Syndicate (gang 2) | Mirage Strip | Solane Holdings | |
| Brass Tower Union | Syndicate (gang 3) | Brass Projects | Ember Court | |
| Static Saints | Syndicate (gang 4) | Grave Frequency | Hollow Cult | |
| Canal Choir | Syndicate (gang 5) | Sunken Row | Roof Oni | |
| Glass Jury | Syndicate (gang 6) | Glass Heights | Quartz Kings | |
| Roof Oni | Syndicate (gang 7) | Skyrail Orbit | Canal Choir | |
| Frequency Order | Guild | Grave Frequency | Hollow Cult | Iris Moon |
| Menders | Guild | Sunken Row | | Mara Solane |
| Forgewrights | Guild | Old Silver | | Dante Rook |
| Frost Wardens | Guild | The Frostline | Hollow Cult | Frost Wardens Outpost |
| Solane Holdings | Corporate | Glass Heights | Mirage Lotus | Ion Solane |
| Hollow Cult | Cult | Grave Frequency | Frequency Order | |

### Ranks

| Rank | Reputation | Colour on the FACTIONS page |
|---|---|---|
| Hunted | -100 to -60 | red |
| Hostile | -59 to -20 | orange |
| Neutral | -19 to 19 | grey |
| Friendly | 20 to 49 | green |
| Trusted | 50 to 79 | blue |
| Family | 80 to 100 | gold |

A value exactly on a boundary belongs to the rank further from 0: -60 is Hunted, -20 Hostile, 20 Friendly, 50
Trusted, 80 Family.

### How reputation moves

- **Every gain costs the rival.** When a faction gains, its rival loses half of that gain. Losses do not spread.
- **Silver Tongue** (Mind skill) makes every gain 25% bigger. The rival's loss is worked out from the gain before
  the bonus.
- **Fractions are kept.** Half points and skill bonuses build up until they make a whole point, and the leftover is
  saved, so nothing is lost to rounding.
- **Killing a gang member** costs that syndicate 2 (an elite 5; Silver Tongue halves it), and the gang's rival
  syndicate gains 1. Because of the rule above, that gain costs the victim's gang another half point. Mirage Lotus and
  Static Saints have non-gang rivals, so their kills give nobody a gain. Two kinds of kill cost nothing at all:
  - **Scripted fights**: mission enemies, rampage targets, story guards, quest foes and world-event crews (anyone the
    game spawned for a job: `Persistent` or carrying a `MissionTag`). A rampage or a story mission no longer leaves a
    gang Hostile or Hunted.
  - **Self-defense in a war**: once a gang's heat is past the attack-on-sight line (0.6) and either its standing or the
    story holds it there (Hostile, Hunted, the Ember Court after the treaty) or it has stayed there for 10 seconds,
    further kills are self-defense. So a fight you start costs you for its first 10 seconds, and a gang that already
    attacks you on sight can never push you deeper by fighting back.
- **Old grudges fade.** Every in-game hour (two real minutes), every syndicate you are below 0 with comes closer to 0
  by 10/24 of a point plus 4% of what is left, so deep grudges fade faster: at least 10 points a day. From Hostile -25
  you are Neutral again after about 8 real minutes, from -59 after about 42, from Hunted -100 after about 64. Sleeping
  at a safehouse counts its hours. The fade is quiet; a toast shows only when it changes your rank. Guilds, the
  company and the cult do not forget.
- **Creature kills** (automatic, through the creature runtime's Killed event):

| Creatures | Faction | Normal | Great beast | Boss |
|---|---|---|---|---|
| Frostline creatures (Frost Stalker, Rime Griffin, Snow Hollow, the Glacier Titan) | Frost Wardens | +1 | +4 | +10 |
| Inner World creatures (Hollow, Mirror Wraith, Crystal Basilisk, Violet Wyvern, the Mirror Wraith Queen) | Frequency Order | +1 | +4 | +10 |
| Any creature whose id contains "hollow" (Hollow Shade, Hollow, Snow Hollow) | Hollow Cult | -1 | -3 | -5 |
| Badlands great beasts and bosses (Dune Wyrm, Quartz Drake, the Mirage Djinn) | Quartz Kings | 0 | +3 | +5 |
| Outer City bosses (the Shard Golem Foreman) | Forgewrights | 0 | +2 | +5 |

  The Frost Wardens and the Frequency Order are both rivals of the Hollow Cult, so their gains cost the cult too.

- **The Forge**: forging gear gives the Forgewrights +1 (Common, Uncommon), +2 (Rare, Epic) or +4 (Legendary,
  Mythic); an infusion +1; an upgrade rank +2; selling materials +1 for every $500. Forging at the outpost also
  gives the Frost Wardens +1, and so does buying their gear.
- **Contacts**: a $500 donation to the Menders gives +3. Offering a Rift Shard to the Frequency Order gives +3.
- **Quests and missions** call `Game.Factions.AddRep(factionId, amount, reason)`; the reason shows in the toast.

### What standing does

**Syndicates (the eight gangs)**

| Rank | What happens |
|---|---|
| Hunted | Gang heat never drops below 0.9: members attack on sight and it never cools below that |
| Hostile | Gang heat never drops below 0.65 (members attack on sight above 0.6) |
| Neutral | Normal: members leave you alone unless provoked |
| Friendly | Offences raise their heat by half as much, heat cools 2 times as fast, 5% off at shops on their turf |
| Trusted | Offences count 0.35, heat cools 3 times as fast, 10% off |
| Family | Offences count 0.25, heat cools 4 times as fast, 15% off |

At Friendly or better, a member who turns on you over a minor offence (a shot fired nearby, a shove, a stray aim)
calms down after about two seconds, unless you actually hurt them in the last 8 seconds or the whole gang's heat is
already over 0.6. Mission guards, rampage and event crews and quest foes never calm down this way, so scripted fights
do not flicker. After the Law of Power ending ("every crew in Las Veyra was paying tribute") Hostile and Hunted no
longer force the heat up.

Gang heat keeps cooling (and bodies keep being cleared) while you are inside the Veil.

**Solane Holdings** gives the same 5, 10 and 15% shop discount in Glass Heights.

**Guilds**

| Guild | Hostile or worse | Neutral | Friendly | Trusted | Family |
|---|---|---|---|---|---|
| Forgewrights | Dante's forge refuses you; other forges make Common gear only | Gear up to Rare; infusions, brews, first Tonic Flask and Energy Cell | Gear up to Epic; 5% off Forge fees; second Energy Cell; Focus Draught | Gear up to Legendary; 10% off fees; materials sell for 10% more; second Tonic Flask; third Energy Cell | Mythic gear (with a boss heart); 15% off fees; materials sell for 20% more |
| Menders | Mara's people will not treat you | Healing at Mara's corner costs $150 | Free healing and tonic refill | +1 Shard Tonic charge | Shard Tonics heal 10% more |
| Frost Wardens | The outpost forge is closed | Outpost forge, Frost Lining rank 1 | The Wardens' shop (Rare Frostline Warden pieces, Frost Ward Tea), 5% off it, Frost Lining rank 2 | Epic pieces, 10% off, Frost Lining rank 3 | Legendary pieces, 15% off, +10% frost resistance |
| Frequency Order | They will not speak to you; Veil Attunement stops working | Offer rift shards to earn trust | They teach **Veil Attunement**: the Veil drains 15% less and shifting costs 20% less | The Veil drains another 10% less | Shifting costs another 10% less |

**Hollow Cult**: Friendly +8% crystal power damage, Trusted +15%, Family +25% and the Veil drains 15% less. It has
no shops and no contact; quests are the way in.

### Shop discounts

Gun shops (weapons, ammo, body armor), Jax Calder Customs (mods and paint) and clothing stores take the best discount
of a syndicate or Solane Holdings that calls the shop's district home, combined with **Haggler** (4% per rank):
each discount applies to what the other leaves, and the total is capped at 50%. **City Royalty** doubles faction
discounts (up to 40%). The shop header shows the line, for example "Ember Court turf: 10% off (Trusted)   Haggler:
8% off   (total 17% off)". The Forge uses the Forgewrights discount instead, and the Wardens' shop uses the Frost
Wardens discount; materials are never discounted, only cash.

### For quest and mission authors

- `Game.Factions.Rep(id)`, `RankOf(id)` (a `FactionRank`), `RepExact(id)` (with the fraction).
- `Game.Factions.AddRep(id, amount, reason)` (whole points, as before) and `AddRepExact(id, amount, reason)`.
- `event Action<string, int, int> RepChanged` (faction id, old, new): raised for every whole-number change,
  including the rival's. A listener that throws is logged and does not stop the others.
- `HasPerk("veil_attunement")`, `FactionDiscount(id)`, `ShopDiscountAt(district)`, `SetRep(id, value)` (debug).
- The rules are in `FactionRules` (engine-free and tested): ranks, perks, heat numbers, kill rules.

---

## 2. The Forge

### Stations

| Station | Where | Tabs |
|---|---|---|
| Dante Rook's forge | Beside Dante Rook in Old Silver | GEAR, INFUSE, BREWS, UPGRADES, SALVAGE |
| The Frost Wardens' forge | Inside the Frost Wardens Outpost | GEAR, INFUSE, BREWS, UPGRADES, WARDENS |
| Shrine forge (a smaller anvil) | Beside every Resonance Shrine | INFUSE, BREWS |

Each station is an anvil with a glow, placed when the boot finishes in the first free spot near its target that is
off the road, outside buildings and at least 2.2 m from any other prompt. If no spot is free the station is skipped
and the log says so. Using one opens the Forge screen, which pauses the game like a shop. The header shows your cash,
your Forgewrights rank, the best rarity you can forge and your fee discount.

### Gear: design a piece

Pick a **slot**, a **rarity**, optionally a **set** and an **element** (and a **boss heart** for Mythic). The piece is
made by the same code as gear drops (`GearSystem.GenerateCrafted`), with affixes rolled for its rarity at your
current gear power plus one, then the set and element you chose. It goes into your inventory (48 pieces at most);
equip it on the GEAR tab.

Rules (the same as for drops): sets come only on Rare or better Visors, Shells, Gauntlets and Striders; elements only
on Weapon Cores (which must have one) and on Epic or better pieces; Mythic needs a boss heart. The best rarity
depends on your Forgewrights rank (Neutral: Rare, Friendly: Epic, Trusted: Legendary, Family: Mythic).

**Cost by rarity** (frame materials depend on the slot, see below):

| Rarity | Materials | Fee |
|---|---|---|
| Common | 3 common frame + 2 Mite Dust | $150 |
| Uncommon | 5 common frame + 3 Mite Dust | $300 |
| Rare | 6 common frame + 2 uncommon frame | $700 |
| Epic | 8 common frame + 4 uncommon frame | $1,500 |
| Legendary | 6 uncommon frame + 2 rare frame | $3,500 |
| Mythic | 8 uncommon frame + 3 rare frame + 1 boss heart | $8,000 |

**Frame materials** (common, uncommon, rare):

| Slot | Common | Uncommon | Rare |
|---|---|---|---|
| Visor | Glass Chitin | Neon Hide | Basilisk Crown Gem |
| Shell | Shard Chunk | Basilisk Scale | Drake Plate |
| Gauntlets | Jackal Fang | Wyrm Scale | Golem Core |
| Striders | Rime Pelt | Wyvern Membrane | Griffin Heart |
| Implant | Mite Dust | Void Thread | Mirror Glass |
| Weapon Core | Shard Chunk | Sand Core | Golem Core |

**Element**: Common and Uncommon pieces add 2 of the element's common essence, Rare and Epic 2 of its uncommon
essence, Legendary and Mythic 1 rare plus 2 uncommon.

| Element | Common essence | Uncommon | Rare |
|---|---|---|---|
| Volt | Glass Chitin | Neon Hide | Golem Core |
| Ember | (1 Wyrm Scale instead of 2, there is no common one) | Sun Feather | Ember Gland |
| Frost | Frost Essence | Frost Fang | Glacier Shard |
| Void | Hollow Essence | Void Thread | Mirror Glass |

**Set**: Neon Revenant takes Neon Hide, Frostline Warden takes Rime Feather, Rift Walker takes Rift Shard: 2 of them
on Rare and Epic pieces, 4 on Legendary and Mythic.

**Boss hearts** for Mythic: Titan Heart, Quartz Heart, Mirage Heart, Foreman Core, Queen's Mirror.

### Infusions (any station, needs Forgewrights Neutral)

An infusion re-tunes your **equipped Weapon Core**, and the Core's element is the element of every melee weapon you
swing (`GearSystem.WeaponElement`). With **Prismatic Infusion** (Shard skill) each infusion also adds +10% elemental
trigger chance to the Core, up to 30% in total (`ForgeMath.PrismaticProc`). A Core that already rolled more (high
rarity Cores can roll up to about 42%) keeps its roll, and the message shows a bonus only when the chance went up.

| Infusion | Cost | Effect of the element |
|---|---|---|
| Volt Infusion | $400, 4 Glass Chitin, 1 Neon Hide | Melee hits can arc lightning into another enemy |
| Ember Infusion | $400, 2 Wyrm Scale, 1 Sun Feather | Melee hits can set enemies burning |
| Frost Infusion | $400, 4 Frost Essence, 1 Frost Fang | Melee hits can shatter an enemy's poise |
| Void Infusion | $400, 4 Hollow Essence, 1 Void Thread | Melee hits can drain health back to you |

### Brews (any station)

Timed effects. Crafting a brew that is already running starts its timer again; different brews stack. They count
down in game time, show top left on the HUD and are saved with the time left.

| Brew | Lasts | Cost | Effect | Needs |
|---|---|---|---|---|
| Hunter's Oil | 6 min | $120, 3 Jackal Fang, 2 Mite Dust | +20% damage to creatures, +10% chance on creature loot rolls | |
| Warding Salve | 4 min | $150, 2 Shard Chunk, 2 Hollow Essence | Take 15% less damage | |
| Frost Ward Tea | 8 min | $100, 2 Rime Pelt, 2 Frost Essence | Take 25% less damage deep in the Frostline | (also sold by the Wardens for $350) |
| Mender's Draught | 10 min | $120, 3 Mite Dust, 1 Frost Essence, 1 Shard Chunk | Shard Tonics heal 25% more | |
| Veil Incense | 5 min | $200, 1 Shade Essence, 1 Void Thread | The Veil drains 35% less | The Third Eye |
| Focus Draught | 3 min | $200, 2 Glass Chitin, 1 Rift Shard | Crystal powers cost 20% less and deal 10% more | Forgewrights Friendly |

### Upgrades (full forges only, permanent)

Each rank needs the rank shown with the gating faction.

| Upgrade | Per rank | Rank 1 | Rank 2 | Rank 3 |
|---|---|---|---|---|
| Tonic Flask | +1 Shard Tonic charge (the new one comes filled) | Forgewrights Neutral: $1,500, 6 Shard Chunk, 2 Shade Essence | Trusted: $4,000, 1 Golem Core, 3 Rift Shard | |
| Energy Cell | +15 max crystal energy | Neutral: $1,200, 6 Glass Chitin, 2 Neon Hide | Friendly: $2,500, 4 Neon Hide, 1 Golem Core | Trusted: $5,000, 2 Golem Core, 1 Basilisk Crown Gem |
| Frost Lining (outpost only) | 10% less damage deep in the Frostline | Frost Wardens Neutral: $900, 4 Rime Pelt, 1 Frost Fang | Friendly: $2,000, 2 Rime Feather, 2 Frost Fang | Trusted: $4,500, 1 Glacier Shard, 3 Rime Feather |
| Veil Anchor (needs the Third Eye) | The Veil drains 12% less | Neutral: $1,500, 3 Void Thread, 2 Shade Essence | Friendly: $3,000, 1 Mirror Glass, 3 Void Thread | Trusted: $6,000, 2 Mirror Glass, 1 Basilisk Crown Gem |

Frost resistance from every source (Frost Lining, Frost Ward Tea, Frost Skin, Wardens Family) adds up, is capped at
60%, and scales with how deep in the Frostline you are (none at the edge, full about 150 m in).

### Salvage (Dante Rook's forge)

Sell materials for cash: each pays its listed value, plus 15% per rank of **Fence** (Mind skill) and 10% at
Forgewrights Trusted or 20% at Family. Selling also earns Forgewrights reputation (+1 per $500).

### The Frost Wardens' shop (WARDENS tab at the outpost, Frost Wardens Friendly or better)

Frostline Warden set pieces for Visor, Shell, Gauntlets or Striders: Rare for $2,400 at Friendly, Epic for $5,200
at Trusted, Legendary for $11,000 at Family (Epic and Legendary pieces also carry the Frost element; the gear rules
keep elements off Rare armour). Frost Ward Tea for $350. Prices take the Frost Wardens discount combined with
Haggler.

### Unlocks are data

A recipe is known when every one of its rules holds (or when it was granted directly). Rule kinds
(`UnlockKind`): `ForgeRank` (Forgewrights rank), `FactionRank` (any faction's rank), `ThirdEye`, `StoryFlag` (a
`MissionManager.Flags` entry), `QuestDone` (a quest id), `StoryProgress` (story missions passed). Nothing is hard-coded
to a quest: quest authors call

- `ForgeDb.RegisterQuestUnlock(questId, recipeId)` (or `Game.Crafting.RegisterQuestUnlock`, which also catches up if
  the quest is already done): finishing that quest teaches the recipe for good, with a notice. Loading a save catches
  up on quests finished in it.
- `Game.Crafting.Grant(recipeId, announce)` teaches a recipe directly.

Other public calls: `Game.Crafting.Knows(recipe)`, `LockReason(recipe)`, `RecipeProblem(recipe, station)`,
`Craft(recipe, station, out message)`, `GearProblem(order, station)`, `ForgeGear(order, station, out message)`,
`Sell(materialId, count, out message)`, `BuyWarden(slot, out message)`, `BuyWardenTea(out message)`, `UpgradeRank`,
`BuffLeft`, `OpenForge(station, title)`. All prices and rules are in `ForgeMath` (engine-free and tested). Payment is
all or nothing: if anything is missing, nothing is taken.

---

## 3. The skill trees

### Points, tiers and links

- One skill point for every level after the first (leveling up at a shrine says "+1 skill point"), plus any granted
  by quests through `Game.Skills.Grant(points)`. Points are always worked out again from your level, so they can
  never drift.
- Each tree has 6 tiers of 5 skills. A skill needs the skill above it in the same column, and its tier needs points
  already spent in the earlier tiers of that tree: tier 2 needs 2, tier 3 needs 5, tier 4 needs 9, tier 5 needs 14,
  tier 6 needs 20.
- Skills with several ranks cost 1 point per rank. Single-rank skills cost 1 point in tier 2, 2 points in tiers 3
  to 5 and 3 points in tier 6. Filling a tree completely takes 73 points (Body), 73 (Mind) or 70 (Shard), 216 for
  all three, so choices matter for a long time.
- **Learning**: click a skill (or press A on it) to select it and read what it does; click or press it again, or
  use LEARN, to learn the next rank. It works at once.
- **Resetting**: at any Resonance Shrine (shrine menu > SKILLS > RESET ALL TREES, press twice to confirm) every point
  comes back for Lumen: (250 + 50 times your level), plus half of that again for each earlier reset, up to four times
  the base. For example 750 Lumen at level 10, 3,500 at level 30 after two resets.

### How bonuses add up

For each stat, every addition is summed first and then every multiplier is applied, and the result is kept within
sane limits (for example dodge and guard costs never below 30%, damage taken never below 50%, crit chance at most
60%, discounts at most 50%, frost resistance at most 60%). Skills, Forge upgrades, active brews and guild perks all
feed the same numbers. A hit is critical at most once: the skill critical roll is skipped when gear already made the
hit critical. Skill critical hits deal 1.5 times damage (2 with Telling Blow) and show "Critical".

### Every node

Signature skills (in bold) change how you play; the rest are numbers.

### Body: melee, stamina, health, dodging and the guard

| Tier | Lane | Skill | Ranks | Points per rank | Signature | Needs | What it does |
|---|---|---|---|---|---|---|---|
| 1 | 1 | Heavy Hands | 3 | 1 |  | nothing | +4% melee damage per rank. |
| 1 | 2 | Deep Lungs | 3 | 1 |  | nothing | +6 max stamina per rank. |
| 1 | 3 | Thick Skin | 3 | 1 |  | nothing | +15 max health per rank. |
| 1 | 4 | Light Step | 3 | 1 |  | nothing | Dodge rolls cost 6% less stamina per rank. |
| 1 | 5 | Steady Guard | 3 | 1 |  | nothing | Guarding costs 6% less stamina per rank. |
| 2 | 1 | Brawler | 2 | 1 |  | Heavy Hands | +6% melee damage per rank. |
| 2 | 2 | Second Breath | 3 | 1 |  | Deep Lungs | Stamina refills 8% faster per rank. |
| 2 | 3 | Field Medic | 2 | 1 |  | Thick Skin | Shard Tonics heal 8% more per rank. |
| 2 | 4 | Quick Feet | 2 | 1 |  | Light Step | Sprint 3% faster per rank. |
| 2 | 5 | Quick Reflexes | 2 | 1 |  | Steady Guard | The parry window lasts 20 ms longer per rank. |
| 3 | 1 | Brick Fists | 3 | 1 |  | Brawler | +5% melee damage per rank. |
| 3 | 2 | Long Haul | 2 | 1 |  | Second Breath | +8 max stamina per rank. |
| 3 | 3 | Hardened | 3 | 1 |  | Field Medic | Take 3% less damage per rank. |
| 3 | 4 | Ghost Roll | 2 | 1 |  | Quick Feet | Dodge rolls stay untouchable 25 ms longer per rank. |
| 3 | 5 | Counter Cut | 2 | 1 |  | Quick Reflexes | Ripostes deal 15% more damage per rank. |
| 4 | 1 | Savage | 2 | 1 |  | Brick Fists | +6% melee damage per rank. |
| 4 | 2 | **Adrenal Surge** | 1 | 2 | yes | Long Haul | Every takedown, people or creatures, refills 20 stamina on the spot. |
| 4 | 3 | **Second Wind** | 1 | 2 | yes | Hardened | Once per fight, dropping below 20% health restores all your stamina and 15% of your health. |
| 4 | 4 | **Momentum** | 1 | 2 | yes | Ghost Roll | A perfect dodge makes your melee hits deal 30% more damage for 2.5 seconds. |
| 4 | 5 | Bulwark | 2 | 1 |  | Counter Cut | Guarding costs 8% less stamina per rank. |
| 5 | 1 | Crushing Blows | 2 | 1 |  | Savage | +5% melee damage per rank. |
| 5 | 2 | Tireless | 2 | 1 |  | Adrenal Surge | Stamina refills 10% faster per rank. |
| 5 | 3 | Vital Reserve | 1 | 2 |  | Second Wind | +1 Shard Tonic charge. |
| 5 | 4 | Gale Runner | 1 | 2 |  | Momentum | Sprint 4% faster and dodge rolls cost 8% less stamina. |
| 5 | 5 | Perfect Guard | 1 | 2 |  | Bulwark | The parry window lasts 30 ms longer. |
| 6 | 1 | **Berserker** | 1 | 3 | yes | Crushing Blows | While you are below 40% health your melee hits deal 25% more damage. |
| 6 | 2 | Unyielding | 1 | 3 |  | Tireless | +20 max stamina and guarding costs 10% less stamina. |
| 6 | 3 | Iron Body | 1 | 3 |  | Vital Reserve | +40 max health and take 5% less damage. |
| 6 | 4 | **Weightless Roll** | 1 | 3 | yes | Gale Runner | For 3 seconds after a perfect dodge, dodge rolls cost no stamina at all. |
| 6 | 5 | **Killing Riposte** | 1 | 3 | yes | Perfect Guard | Ripostes deal 50% more damage. |

### Mind: guns, critical hits, stealth, loot, money and driving

| Tier | Lane | Skill | Ranks | Points per rank | Signature | Needs | What it does |
|---|---|---|---|---|---|---|---|
| 1 | 1 | Trigger Discipline | 3 | 1 |  | nothing | +4% gun damage per rank. |
| 1 | 2 | Keen Eye | 3 | 1 |  | nothing | +2% critical hit chance per rank, for melee and guns. |
| 1 | 3 | Soft Tread | 3 | 1 |  | nothing | Creatures notice you from 6% closer per rank. |
| 1 | 4 | Street Smarts | 3 | 1 |  | nothing | +4% cash from jobs, pickups and rewards per rank. |
| 1 | 5 | Road Feel | 3 | 1 |  | nothing | +3% tire grip in vehicles you drive per rank. |
| 2 | 1 | Marksman | 2 | 1 |  | Trigger Discipline | +5% gun damage per rank. |
| 2 | 2 | Weak Spots | 2 | 1 |  | Keen Eye | +3% critical hit chance per rank. |
| 2 | 3 | Scavenger | 3 | 1 |  | Soft Tread | +5% chance on every creature loot roll per rank. |
| 2 | 4 | Haggler | 3 | 1 |  | Street Smarts | 4% off at shops and the Forge per rank. |
| 2 | 5 | Nitro Tuning | 2 | 1 |  | Road Feel | Nitro pushes 8% harder per rank. |
| 3 | 1 | Gunsmith | 2 | 1 |  | Marksman | +5% gun damage per rank. |
| 3 | 2 | **Telling Blow** | 1 | 2 | yes | Weak Spots | Critical hits deal double damage instead of one and a half times. |
| 3 | 3 | Tracker | 2 | 1 |  | Scavenger | Creatures notice you from 8% closer per rank. |
| 3 | 4 | Lumen Siphon | 3 | 1 |  | Haggler | +5% Lumen from defeated enemies per rank. |
| 3 | 5 | Corner Carver | 2 | 1 |  | Nitro Tuning | +4% tire grip per rank. |
| 4 | 1 | Close Range | 2 | 1 |  | Gunsmith | +10% gun damage within 8 m per rank. |
| 4 | 2 | Lethal Precision | 2 | 1 |  | Telling Blow | +4% critical hit chance per rank. |
| 4 | 3 | **Beast Hunter** | 1 | 2 | yes | Tracker | +15% damage to creatures and +15% chance on every creature loot roll. |
| 4 | 4 | **Silver Tongue** | 1 | 2 | yes | Lumen Siphon | Reputation gains are 25% bigger, and killing gang members costs only half the reputation. |
| 4 | 5 | **Getaway Driver** | 1 | 2 | yes | Corner Carver | While you drive, a nitro charge refills every 90 seconds (up to three). |
| 5 | 1 | Long Shot | 2 | 1 |  | Close Range | +10% gun damage beyond 30 m per rank. |
| 5 | 2 | Cutthroat | 1 | 2 |  | Lethal Precision | +5% critical hit chance. |
| 5 | 3 | **Ghost Step** | 1 | 2 | yes | Beast Hunter | While crouched, creatures notice you from only half as far. |
| 5 | 4 | Fence | 2 | 1 |  | Silver Tongue | Forgewrights pay 15% more for your materials per rank. |
| 5 | 5 | Road Veteran | 1 | 2 |  | Getaway Driver | +5% tire grip and nitro pushes 10% harder. |
| 6 | 1 | **Still Point** | 1 | 3 | yes | Long Shot | While the Third Eye is open, your guns deal 25% more damage. |
| 6 | 2 | Cold Read | 1 | 3 |  | Cutthroat | +8% critical hit chance. |
| 6 | 3 | Predator's Patience | 1 | 3 |  | Ghost Step | +10% damage to creatures, and creatures notice you from 10% closer. |
| 6 | 4 | **City Royalty** | 1 | 3 | yes | Fence | Faction shop discounts are doubled, and you earn 10% more cash. |
| 6 | 5 | Phantom Driver | 1 | 3 |  | Road Veteran | +6% tire grip and nitro pushes 15% harder. |

### Shard: crystal powers, energy, the Veil, elements and creature hunting

| Tier | Lane | Skill | Ranks | Points per rank | Signature | Needs | What it does |
|---|---|---|---|---|---|---|---|
| 1 | 1 | Focused Light | 3 | 1 |  | nothing | +5% crystal power damage per rank. |
| 1 | 2 | Deep Well | 3 | 1 |  | nothing | +8 max crystal energy per rank. |
| 1 | 3 | Veil Sense | 3 | 1 |  | nothing | The Veil drains 8% less energy per rank. |
| 1 | 4 | Elemental Affinity | 3 | 1 |  | nothing | +4% chance per rank for your weapon's element to trigger on a hit. |
| 1 | 5 | Hunter's Resonance | 3 | 1 |  | nothing | +3% damage to creatures per rank. |
| 2 | 1 | Efficient Casting | 3 | 1 |  | Focused Light | Crystal powers cost 5% less energy per rank. |
| 2 | 2 | Swift Recharge | 3 | 1 |  | Deep Well | Crystal energy refills 10% faster per rank. |
| 2 | 3 | Thin Veil | 2 | 1 |  | Veil Sense | Shifting into the Veil costs 10% less energy per rank. |
| 2 | 4 | Ember Ward | 1 | 1 |  | Elemental Affinity | Fire on you burns out twice as fast. |
| 2 | 5 | Soul Harvest | 1 | 1 |  | Hunter's Resonance | Every creature you kill restores 6 crystal energy. |
| 3 | 1 | Overcharge | 2 | 1 |  | Efficient Casting | +6% crystal power damage per rank. |
| 3 | 2 | Conduit | 2 | 1 |  | Swift Recharge | Every takedown restores 3 more crystal energy per rank. |
| 3 | 3 | **Veil Walker** | 1 | 2 | yes | Thin Veil | The Veil drains half as much energy, and shifting into it costs 40% less. |
| 3 | 4 | Frost Skin | 2 | 1 |  | Ember Ward | Take 8% less damage deep in the Frostline per rank. |
| 3 | 5 | Crystal Hunter | 2 | 1 |  | Soul Harvest | +4% damage to creatures per rank. |
| 4 | 1 | Power Surge | 2 | 1 |  | Overcharge | +6% crystal power damage per rank. |
| 4 | 2 | **Shard Echo** | 1 | 2 | yes | Conduit | Perfect dodges refund 15 crystal energy. |
| 4 | 3 | Deep Veil | 2 | 1 |  | Veil Walker | The Veil drains 10% less energy per rank. |
| 4 | 4 | **Storm Conduit** | 1 | 2 | yes | Frost Skin | Melee takedowns with an elemental weapon restore 10 crystal energy. |
| 4 | 5 | **Apex Resonance** | 1 | 2 | yes | Crystal Hunter | Every creature you kill heals you for 6% of your max health. |
| 5 | 1 | **Prism Mind** | 1 | 2 | yes | Power Surge | While the Third Eye is open, crystal powers cost 25% less energy. |
| 5 | 2 | Reservoir | 2 | 1 |  | Shard Echo | +12 max crystal energy per rank. |
| 5 | 3 | Veilborn | 1 | 2 |  | Deep Veil | The Veil drains 10% less energy and shifting costs 10% less. |
| 5 | 4 | **Prismatic Infusion** | 1 | 2 | yes | Storm Conduit | Forge infusions also add +10% elemental trigger chance to your Weapon Core (up to +30%). |
| 5 | 5 | Titan Slayer | 1 | 2 |  | Apex Resonance | +6% damage to creatures. |
| 6 | 1 | **Radiant Overload** | 1 | 3 | yes | Prism Mind | While your crystal energy is nearly full, crystal powers deal 30% more damage. |
| 6 | 2 | Infinite Well | 1 | 3 |  | Reservoir | +25 max crystal energy, and energy refills 15% faster. |
| 6 | 3 | **Veil Sovereign** | 1 | 3 | yes | Veilborn | Inside the Veil you take 15% less damage and your crystal powers deal 15% more. |
| 6 | 4 | Elemental Mastery | 1 | 3 |  | Prismatic Infusion | +10% chance for your weapon's element to trigger on a hit. |
| 6 | 5 | Beastbane | 1 | 3 |  | Titan Slayer | +8% damage to creatures. |

Body has 6 signature skills, Mind 7 and Shard 8. Their timing details: Second Wind recharges when the fight ends;
Momentum, Weightless Roll and Shard Echo start on a perfect dodge (the "Perfect dodge" slow motion when a hit lands
early in a roll); "nearly full" energy for Radiant Overload means 95% or more; every body counts once for takedown
skills even when a humanoid creature is reported both as a person and as a creature.

### Where each bonus is applied (`SkillMods`)

| SkillMods field | Comes from | Applied in |
|---|---|---|
| MeleeDamage (changes with Berserker, Momentum), CritChance, CritDamage, RiposteDamage | Body melee skills, crit skills, Telling Blow, Counter Cut, Killing Riposte | PlayerCombat.DamageMultiplier, the multiplier PlayerCombat passes to CombatDirector.Strike (SkillMods.MeleeRoll) |
| GunDamage (Still Point), GunPointBlank, GunLongShot, CritChance, CritDamage | Mind gun and crit skills | WeaponController bullet damage for the player (ProgressionHooks.GunMultiplier) |
| PowerDamage (Radiant Overload, Veil Sovereign) | Shard power skills, Focus Draught, Hollow Cult | Abilities.PowerScale (every damaging power, Sunbolt included) |
| PowerCost (Prism Mind), EnergyRegen, KillEnergy | Efficient Casting, Focus Draught; Swift Recharge, Infinite Well; Conduit | Abilities.Cast, energy refill, Abilities.OnKill |
| EnergyMaxBonus, HealthMaxBonus, StaminaMaxBonus | Energy skills and Energy Cell; Thick Skin, Iron Body; stamina skills | Progression.ApplyToPlayer (max health is `Progression.MaxHealthFor`: 200, Vigor, gear, skills and the paramedic milestone's +50) |
| VeilDrain, VeilShiftCost | Veil skills, Veil Anchor, Veil Incense, Veil Attunement, Frequency Order, Hollow Cult | InnerWorld drain and PlayerShift (`InnerWorld.ShiftCost`, `DrainNow`), and the HUD's VEIL tag and hold ring |
| StaminaRegen, DodgeCost (Weightless Roll), DodgeIFrames, GuardCost, ParryWindowBonus | Body skills | PlayerCombat |
| DamageTaken (Veil Sovereign), FrostResist | Hardened, Iron Body, Warding Salve; Frost Skin, Frost Lining, Frost Ward Tea, Wardens Family | PlayerCombat incoming damage (SkillMods.IncomingScale) |
| TonicChargesBonus, TonicHeal | Vital Reserve, Tonic Flask, Menders Trusted; Field Medic, Mender's Draught, Menders Family | PlayerCombat.MaxTonics and the tonic heal |
| MoveSpeed | Quick Feet, Gale Runner | PlayerController sprint speed |
| ElementProc | Elemental Affinity, Elemental Mastery | Gear.Recalculate (added to the gear's elemental proc chance) |
| VehicleGrip, VehicleBoost | Driving skills | Vehicle grip and nitro, only while the player drives |
| LumenGain | Lumen Siphon | Progression.OnEnemyKilled (people); creature Lumen needs the creature runtime to apply it (see section 7) |
| CashGain | Street Smarts, City Royalty | Economy.Earn (not the casino, cheats or salvage) |
| Discount, SalvageBonus | Haggler; Fence | Shop prices (MenuSystem), Forge fees, salvage prices |
| RepGain | Silver Tongue | FactionSystem gains |
| CreatureDamage, MaterialFind, CreatureSense (Ghost Step) | Creature skills, Hunter's Oil | The creature runtime (computed here, applied by the creature worker) |

---

## 4. Saves

Each system writes its own block inside the save, and only appended fields (never renamed, retyped or removed):

| Block | Fields added by this work | What they hold |
|---|---|---|
| `FactionSave` (`Ids`, `Rep` existed) | `Carry`, `Perks` | The fraction of a point per faction; perks a guild taught (Veil Attunement) |
| `CraftSave` (`Known`, `CraftedIds`, `CraftedCounts` existed) | `BuffIds`, `BuffTimes` | Active brews and their seconds left. Upgrade ranks are the crafted counts |
| `SkillSave` (`Learned`, `Points` existed) | `Ranks`, `Granted`, `Respecs` | The rank of each learned skill, points granted by quests, resets made |

Old saves load cleanly: no reputation, nothing crafted, no skills, and the points your level has earned waiting to
be spent. If a save holds skills that are no longer valid (for example after a later update changes a tree), those
skills are dropped and their points come back; if a save holds more spent points than it should, every point is
refunded with a notice. Tonic charges above the normal maximum are put back after the skill and Forge bonuses are
known, whatever order the systems load in, and so are health and crystal energy: a save made at 155 of 310 health and
130 of 160 energy loads at exactly that (before, health was rescaled and energy capped at 100). The freeze lists in
`ProgressionTests` fail if a field is renamed.

---

## 5. Debug tools (F2 > Progress)

+5 skill points, a free tree reset, +5,000 Lumen, +20 of every material, know every recipe, +$20,000, open each kind
of Forge directly, and a faction picker with -20, -5, +5, +20 and Set 0 (the + buttons go through the normal rules,
so the rival loses half; Set 0 touches only that faction). Below that, a live summary of every SkillMods number.

---

## 6. What Tyler should test first

Nothing here has been run in Unity yet, so this is the order that finds the most in the least time:

1. **Run the EditMode tests in Unity** (tools/test.ps1 or Window > General > Test Runner). Three tests only run inside
   Unity (they use JsonUtility): `SaveDataTests.JsonRoundTripKeepsEveryField`, `SaveDataTests.OldSavesLoadWithDefaults`
   and `ProgressionTests.SaveBlocksSurviveJson`.
2. **SKILLS page.** New game, Pause > SKILLS. Are the three columns, links and texts readable (at 1920x1080 and in a
   small window)? At 16:10 the pause tabs shrink their labels to fit one row; at 5:4 or 4:3 (for example 1280x1024)
   they wrap onto two rows and the page moves down to make room, and the SKILLS page tightens its margins and shrinks
   node names that would not fit (the full name is always in the details panel). With F2 > Progress > +5 skill
   points, learn Heavy Hands with the mouse, then with a controller (D-pad to a skill, A to select, A again to learn).
   LB and RB should reach SKILLS and FACTIONS like the other tabs.
3. **Dante Rook's forge.** Map: orange A in Old Silver. Does the anvil stand beside Dante (not in a wall, not on the
   road, not on his mission marker)? Use it: the game pauses; Esc, Backspace and B close it without opening the pause
   menu and without a dodge roll; LB and RB switch tabs. With F2 > Progress > +20 of every material, forge a Rare
   Shell and a Volt Weapon Core, equip both on the GEAR tab, then use INFUSE to re-tune the Core to Ember and check
   that melee hits now burn. The green or red status line under each tab should sit below the FORGE IT, INFUSE and
   BREW buttons, never on top of them.
4. **Brews and upgrades over a save.** Craft Warding Salve (the timer shows top left) and a Tonic Flask (tonics go up
   by one, already filled). Save, quit to the title, load: the brew is still running and the extra tonic is still
   there.
5. **Gangs.** F2 > Progress: set Ember Court to -20 (Hostile) and walk into the Ash Underpass: they should attack on
   sight, and killing them in that fight shows no reputation toast (self-defense). Set +20 (Friendly): a gun shop there
   shows "Ember Court turf: 5% off (Friendly)" with lower prices, and a member who turns on you over a shove calms down.
   Kill one in a fresh fight: the toast shows the loss. Play an Ember Court rampage: no reputation change at all.
   Set a gang to -25 and play on for about 8 real minutes: it is Neutral again (a toast says so).
6. **Contacts.** Mara Solane's corner in Sunken Row (green cross sign): healing costs $150 at Neutral, free at
   Friendly. Iris Moon in Glass Heights (violet ~): Veil Attunement at Friendly. Each contact's prompt should stand on
   the left of the door and never overlap the guild's quest giver prompt on the right.
7. **The outpost and the shrines.** The Frost Wardens' forge inside the outpost and the WARDENS tab at Friendly; a
   few shrine forges (are any inside a wall or a fence?).
8. **Feel.** Critical hit numbers with Keen Eye; Second Wind at low health; Momentum after a perfect dodge; Getaway Driver
   in a car with nitro; sprint speed with Quick Feet. These are first-pass numbers and are easy to change in
   `SkillDb.cs`.
9. **Old save.** Load a save from before this work: no errors, points equal to level minus one.

---

## 7. What was checked, and what was not

Checked here, without Unity:

- The project compiles with no errors and no warnings in all twelve configurations (the six normal ones and the six
  with the URP path), with `check_at.sh` and `check_urp.sh`.
- The EditMode tests compile as their asmdef describes and pass offline: 68 passed, 0 failed, 3 skipped because they
  need the engine. `ProgressionTests` adds 15 tests: rank bands, the rival share and the carry, kill rules, perk text
  for every faction and rank, gear prices against the gear rules, all-or-nothing payment, unlock rules and quest
  unlocks, discount stacking, the tree shape (90 nodes, 30 per tree, every link inside its own tree and pointing up,
  at least four signatures per tree), points, links and tiers, modifiers from the learned set, save round trips and
  validation, and freeze lists for the three save blocks.
- The offline logic harness (a sandbox copy of LogicTests) passes all 6,563 checks: the 2,637 original ones and 3,926
  new progression checks.
- `SaveDataTests` now fills and compares every save block generically, so it keeps working as other systems add
  blocks; this was checked offline with a stand-in JSON serializer (the real JsonUtility run is Unity only).
- The review fixes are covered by `ProgressionFixTests` (8 tests: which gang kills cost reputation and that normal
  play cannot spiral into Hunted, the grudge fade times, Prismatic Infusion never lowering a Core, the Veil's real cost
  and drain, the paramedic bonus in max health, pause tabs fitting at every aspect ratio, and the golden views keeping
  their order). With them the offline run is 136 passed, 0 failed, 5 skipped, and LogicTests passes all 16,040 checks.

Not checked: anything on screen (layout, readability, prop placement), anything that needs real input or a real pad,
game feel and balance, frame rate, and the real JsonUtility round trip. The creature and quest runtimes were stubs in
this sandbox, so creature reputation, the creature skills (Beast Hunter, Soul Harvest, Apex Resonance, Ghost Step,
Predator's Patience) and quest unlocks were only checked as pure logic, not in play. Materials only come from those
runtimes (or the debug page), so the Forge cannot be used for real until they are merged.

### What the creature runtime is expected to do

The skill trees only compute `SkillMods.CreatureDamage`, `MaterialFind` and `CreatureSense`; the creature runtime
applies them (damage to creatures, loot rolls, how far creatures notice you). Conduit's extra energy is added in
`Abilities.OnKill`, so the creature runtime should call that for creatures the player kills. Lumen Siphon is applied
here to the Lumen people drop; creature Lumen is worked out by the creature runtime, which should multiply it by
`SkillMods.LumenGain`.

### Tuning

- Reputation: `FactionRules` (ranks, discounts, heat numbers, kill amounts, guild perk numbers; `AttackOnSightHeat` and
  `WarGraceSeconds` decide which kills are self-defense; the grudge fade is `DailyForgiveness`, the steady points per
  day, and `HourlyForgiveShare`, the share of the grudge that fades each hour).
- The Forge: `ForgeDb` (recipes, costs, unlocks, brew lengths) and `ForgeMath` (gear frames, essences, fees).
- Skills: `SkillDb` (every node, its ranks, costs and numbers; `TierPoints`), `SkillBook.RespecCost`, and the
  signature numbers in `SkillTree` (for example Second Wind's 20% and 15%).

---

## 8. BUILDS: build sets, gear score and the city tier (Phase 6)

Phase 6 turns gear into builds around guns, gadgets and armor. Three pieces work together: six new **build sets**,
one **gear score** for the whole loadout, and the **city tier** that the gear score sets. Gadgets themselves are in
`Docs/GADGETS.md`. Every number below lives in one pure class each (`GearBuildRules`, `GearScoreRules`,
`GadgetRules`) and is tested offline (`GearBuildTests`, `GadgetTests`).

### Build sets

The three classic melee sets keep their index (0 to 2) and their 2 and 4 piece bonuses. The six build sets were
appended after them (indexes 3 to 8 of `GearSystem.SetNames`, so old saves keep their pieces) and also have a
**3 piece** bonus. Like the classic sets they come on Rare or better Visors, Shells, Gauntlets and Striders, so a set
is worn 2, 3 or 4 pieces at a time and two sets can be mixed 2 + 2. Set pieces now roll any of the nine sets (two
thirds of set drops are build sets). The Forge still crafts only the three classic sets; build sets are found.

| Set | 2 pieces | 3 pieces | 4 pieces |
|---|---|---|---|
| Longsight Lattice (precision guns) | +12% gun damage beyond 25 m | Gun hits have an 8% chance of a gear critical (x1.6) | Each gear critical cuts both gadget cooldowns by 0.4 s (at most every 0.25 s) |
| Stoneward Choir (armor) | Kills restore 15 armor | Armor regenerates 4 a second after 4 s without being hit | Take 15% less damage while armor is above half |
| Gearwright's Wake (gadgets) | Gadget cooldowns 15% shorter | Gadgets hit, heal and shield 25% harder and last 20% longer | Kills while a gadget is out cut both cooldowns by 2 s |
| Resonant Circuit (powers) | Kills with a gun in hand restore 6 crystal energy | +12% crystal power damage | Each Resonance Burst cuts both gadget cooldowns by 3 s |
| Alley Saint (close range) | +12% gun damage within 10 m | Kills heal 6% of max health | Kills within 10 m refill the magazine of the gun in hand from its reserve |
| Vanta Shroud (scanning) | Scanner marks last 50% longer | Marked enemies take 10% more damage | Killing a marked enemy sends a free Ripple pulse (20 m, at most every 3 s) |

Where they act: gun bonuses and the gear critical in `ProgressionHooks.GunMultiplier` (every player bullet, loot guns
included, through `GearBuilds.GunHitMultiplier`); the damage cut in `GearSystem.ArmorMultiplier`; power damage in
`GearSystem.PowerMultiplier`; kill effects, armor regeneration and cooldown cuts in `GadgetSystem` (people killed by
the player through `ProgressionHooks.OnEnemyKilled`, creatures through the creature director's `Killed` event).
Resonant Circuit's 4 piece needs the combat loop's Resonance Burst to call `Game.Gadgets.OnResonanceBurst()` (see
GADGETS.md, "Merge notes").

The GEAR tab shows the gear score next to EQUIPPED, the city tier under the character totals, every set worn as
"(n/4)" and each **active** bonus on its own green line. A set piece's card lists its 2, 3 and 4 piece bonuses, green
for the ones you already wear.

### Gear score

One number for the loadout: every equipped gear piece (six slots) and loot gun (four slots) adds its **item level x
rarity factor**, rounded.

| | Rule |
|---|---|
| Gear piece item level | power x 3 + upgrade x 2 (1 to 60) |
| Loot gun item level | gun level + ascensions x 2 (1 to 60) |
| Rarity factor | Common 1.0, Uncommon 1.1, Rare 1.25, Epic 1.45, Legendary 1.7, Mythic 2.0 |

The starter kit scores about 24, a mid game Epic loadout about 340, a full Legendary end game loadout about 900
(the most is 1,200). The score shows in the bottom-right HUD corner (under the gadget slots), on the GEAR tab, on a
gear piece's card ("Score") and on F2 > Gear with a slot by slot breakdown.

### City tier

| Tier | Name | Gear score | Enemy level | Enemy health | Enemy damage | Gun drop item level | Rarity bump |
|---|---|---|---|---|---|---|---|
| 1 | Quiet Streets | 0 | +0 | x1.00 | x1.00 | +0 | 0% |
| 2 | Neon Heat | 100 | +2 | x1.12 | x1.06 | +1 | 6% |
| 3 | Shard Season | 250 | +4 | x1.28 | x1.14 | +2 | 12% |
| 4 | Veil Fever | 450 | +7 | x1.50 | x1.25 | +4 | 18% |
| 5 | Crown of Glass | 700 | +10 | x1.80 | x1.38 | +6 | 25% |

- **Enemies**: each fighter (gang, police, SWAT, hostiles, humanoid creatures, wild creatures and bosses) is scaled
  once, the first time the game sees it (`GearScore.ScaleNewEnemies`, twice a second), so changing gear never changes
  a fight already under way. People get the health and damage multipliers (their gun and melee damage); creatures get
  the health multiplier (their attack numbers are private to the creature runtime). "Enemy level" is the player's
  level plus the tier's offset; it is shown on the GEAR tab and F2 > Gear.
- **Drops**: the tier's item level bonus is added to the drop source's bonus and passed to the generator's existing
  level input (`GearScoreRules.LootItemLevel` calls `LootGenerator.ItemLevelFor`; the generator itself is untouched).
  The rarity bump lifts a rolled gun (before the pity rule), gear drop or gadget mod one step, never to Mythic. Gear
  drops also gain half the item level bonus as power.
- Gadget damage and healing grow with the player's level (x1 at level 1, about x2 at level 50) to keep up with tier 5
  health.
- The tier is worked out from the score every time; nothing is saved. F2 > Gear can force a tier for testing.

### Saves

`SaveData.Gadgets` (`GadgetSave`) was appended and added to the `SaveDataTests` freeze list, with its own freeze lists
for `GadgetSave` and `GadgetModSave`. Build sets need no new save field (a piece already stores its set index; an
unknown index loads as "no set"). Gear score and the city tier are not saved.
