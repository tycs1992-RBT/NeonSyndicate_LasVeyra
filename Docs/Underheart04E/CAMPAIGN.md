# Underheart and Bonds: the campaign (04E)

The 04E campaign is sixteen main quests in four acts, twelve side quests in four arcs of three, and eight repeatable
contract grammars posted on the Lantern Refuge board. It is all **data** in `Assets/NeonSyndicate/Resources/Quests`
(`underheart_act1.json` to `underheart_act4.json`, `underheart_side.json`, `underheart_contracts.json`), run by the same
quest runtime as the guild arcs, plus eighteen new step types for the 04E systems (builds, rewards, bonds, fusion,
skirmishes, the underground bosses and choices).

Nothing on this page has been run inside Unity. The data passes the strict reader and the validator offline, and the
rules are covered by EditMode tests; how it looks, feels and plays must be checked in the editor (see the end).

## How a campaign quest plays

- **Who gives it.** A campaign quest's `Giver` is a cast member (`Cast:nessa`, `Cast:jax`, ...) with a `Presenter` (the
  name the offer card shows). The cast system (GUIDANCE) spawns them as real people at their posts; you walk up and talk.
  Two finale quests are given by a place instead (`Under:crown.staging`, "The allied crews"; `Under:crown.regent_gate`,
  "The Regent's gate"). Starting any campaign quest reserves its cast giver (`Game.Campaign.Reserve`) so the person does not
  wander off mid-quest; cleanup releases them, and a retry reserves again.
- **Where it happens.** Every place is written out as a token: `Venue:<venue>.<anchor>` (a room in an inhabited building,
  for example `Venue:dispatch.fitting`), `Under:<anchor>` (a named point in the Underheart, for example
  `Under:canals.valve_room`), `Cast:<id>` (a person), `Arsenal:case` and `Arsenal:lane` (the real 04D resonance case and
  practice lane at Veyra Lofts). Token targets are exact points: the runtime never probes the ground from above, so an
  objective inside a room or 40 m underground lands on its floor, not on the roof or the street above.
- **When a place does not exist yet.** A step waits up to 10 s for its room, anchor or person to exist, then fails the quest
  with the plain reason ("The Underheart is not built yet", "Cinder Dispatch is not open in this city"). Before you even
  start, the journal and offer card show that reason as the lock (`CampaignBlocker`), so a quest whose world piece is
  missing never strands you halfway.
- **Story gates.** In story mode each act waits for story progress: Act I after story mission 1, Act II after mission 4,
  Act III after mission 7, Act IV after mission 9. "Two Hearts, One Chassis" opens after mission 2 (Jax is met in chapter
  2, so the first fusion stays early). Free Roam passes every story gate; the quest chain itself always applies.
- **Checkpoints.** Long chapters mark phases as checkpoints. Entering one commits `questId:phaseId` and asks for an
  autosave at that moment (`CheckpointSafeNow` is true just for that ask; a mission otherwise never autosaves). If you fail,
  quit or load, the quest **resumes at its last checkpoint**: Retry, the giver, the journal (`CanResume`, `ResumeQuest`)
  and a glowing **R** resume point at the phase's `ResumeAt` place (press E there) all start from that phase, so earlier
  steps and rewards never repeat. Finishing the quest clears its checkpoint.
- **Rewards never pay twice.** Campaign quests pay through `Claim` steps (a durable once-per-token entitlement in the
  BUILDS ledger) and the quest `Reward` on completion, never through `Give`. A Claim never blocks: a full inventory or a
  choice reward stays pending and the HUD says where to claim it (ARSENAL, the case, the fitting station). Boss rewards use
  the boss's own token (`boss04e:<boss>`), so the boss's victory grant and the quest's Claim are one and the same. If the
  ledger is not running yet, the grant is queued in the save and retried.
- **Choices last.** `Decide` records a choice once (`Game.Quests.Choice(key)`); a resumed or replayed quest keeps the first
  answer, except approach choices marked `Reask` (`constructs`, `descent_window`), which are asked again on every attempt
  so a player who failed one way can try the other. `Branch` jumps by condition, which is how later quests remember earlier choices and how the story's ending
  (Mercy or Power) and Free Roam change lines. Idempotent fights: a cleared skirmish room, a defeated boss, a silenced
  mechanism, a rescued companion and an opened shortcut are all skipped on a resume.
- **Failing.** Dying, being arrested or becoming wanted fails a campaign quest (the wanted message says checkpoints are
  kept). Losing a boss fight or a skirmish fails it too. Retry (Enter or hold A within 12 s) resumes at the checkpoint.

## The quests

Kind, act, giver, prerequisites and the story gate (story missions passed; Free Roam ignores it). Difficulty is 1 to 5.

| Id | Title | Kind / Act | Giver | Requires | Story | Region | Diff |
|---|---|---|---|---|---|---|---|
| `uh05_01_knock` | A Knock Beneath Home | Main I | Nessa Vale (`Cast:nessa`) | none | 1 | surface | 1 |
| `uh05_02_sparks` | Sparks in the Walls | Main I | Tamsin Reyes (`Cast:maintainer`) | 01 | 1 | service_level | 1 |
| `uh05_03_bond` | A Creature Is Not a Battery | Main I | Odile Marsh (`Cast:caretaker`) | 02 | 1 | service_level | 2 |
| `uh05_04_descent` | The First Descent | Main I | Corin Pell (`Cast:dispatcher`) | 03 | 1 | ember_canals | 2 |
| `uh05_05_chassis` | Two Hearts, One Chassis | Main II | Jax Calder (`Cast:jax`) | 04 | 2 | surface | 2 |
| `uh05_06_pressure` | Glass Under Pressure | Main II | Dante Rook (`Cast:dante`) | 05 | 4 | ember_canals | 3 |
| `uh05_07_gardens` | Gardens That Burn | Main II | Bram Achterberg (`Cast:gardens_keeper`) | 06 | 4 | pyre_gardens | 3 |
| `uh05_08_matron` | The Cinder Matron | Main II | Bram Achterberg (`Cast:gardens_keeper`) | 07 | 4 | pyre_gardens | 4 |
| `uh05_09_ledger` | Names on the Ledger | Main III | Iris Moon (`Cast:iris`) | 08 | 7 | surface | 3 |
| `uh05_10_road` | A Road for the Living | Main III | June Pyre (`Cast:june`) | 09 | 7 | crucible_workshops | 3 |
| `uh05_11_vaultbreaker` | The Vaultbreaker | Main III | Dante Rook (`Cast:dante`) | 10 | 7 | crucible_workshops | 4 |
| `uh05_12_keep` | What We Choose to Keep | Main III | Mara Solane (`Cast:mara`) | 11 | 7 | surface | 2 |
| `uh05_13_crown` | Down to the Crown | Main IV | Ion Solane (`Cast:ion`) | 12 | 9 | ash_reliquary | 4 |
| `uh05_14_choir` | Break the Extraction Choir | Main IV | The allied crews (`Under:crown.staging`) | 13 | 9 | foundry_crown | 4 |
| `uh05_15_regent` | The Kiln Regent | Main IV | The Regent's gate (`Under:crown.regent_gate`) | 14 | 9 | foundry_crown | 5 |
| `uh05_16_answers` | A City That Answers | Main IV | Nessa Vale (`Cast:nessa`) | 15 | 9 | surface | 1 |
| `uh05_s01_overheat` | The Overheated Room | Side (Nessa) | Nessa Vale | 02 | 1 | surface | 1 |
| `uh05_s02_instrument` | The Lofts' Music Box | Side (Nessa) | Nessa Vale | s01 | 1 | surface | 1 |
| `uh05_s03_shelter` | A Warm Corner | Side (Nessa) | Nessa Vale | s02, 04 | 1 | lantern_refuge | 2 |
| `uh05_s04_mismatch` | Bond and Chassis | Side (Jax) | Jax Calder | 05 | 2 | surface | 2 |
| `uh05_s05_profile` | A Second Profile | Side (Jax) | Jax Calder | s04 | 2 | surface | 2 |
| `uh05_s06_coupler` | The Rare Coupler | Side (Jax) | Jax Calder | s05, 10 | 7 | crucible_workshops | 3 |
| `uh05_s07_supply` | Cold Comfort | Side (Mara) | Mara Solane | 04 | 1 | ember_canals | 2 |
| `uh05_s08_habitat` | Walking Home | Side (Mara) | Mara Solane | s07, 07 | 4 | pyre_gardens | 2 |
| `uh05_s09_treatment` | Quiet Hands | Side (Mara) | Mara Solane | s08 | 4 | surface | 1 |
| `uh05_s10_shortcut` | The Lift Glyph | Side (Iris) | Iris Moon | 09 | 7 | lantern_refuge | 2 |
| `uh05_s11_drone` | The Beeping Ledge | Side (Iris) | Iris Moon | s10, 10 | 7 | pyre_gardens | 2 |
| `uh05_s12_entries` | Two Entries, One Truth | Side (Iris) | Iris Moon | s11, 13 | 9 | ash_reliquary | 4 |

Main quests use Order 1 to 16; side quests Order 101 to 112. No main quest ever needs a side quest. Every quest also fails
on a wanted level (see above).

## Act I: the Pressure House and the first descent

### 01 A Knock Beneath Home (`uh05_01_knock`)
- **Actors:** Nessa Vale (giver), Corin Pell at Cinder Dispatch, Ion Solane on the call screen.
- **Rooms:** Cinder Dispatch (ground floor, across Veyra Avenue from the Lofts): `Venue:dispatch.visitor1`,
  `.ion_screen`, `.receiver_a`, `.receiver_b`, `.fitting`; the 04D case and lane (`Arsenal:case`, `Arsenal:lane`); the
  Pressure House door (`Venue:pressure.exit`).
- **Flow:** a continuity Branch picks Nessa's opening (Mercy, Power, or before the ending); cross the avenue, talk to
  Corin, Ion over the screen; inspect both receivers (flag `dispatch_receivers_read`); **checkpoint** `fitting` (resume at
  `Venue:dispatch.visitor1`): choose a build at the fitting worktable (`Build`) and claim the starter ward; an optional
  practice (`Equip` at `Arsenal:case` with the practice-lane calibration), or "I KNOW IT" skips straight on; the warm
  conduit lights (`Conduit`) along the sidewalk to the Pressure House door, and dims when you arrive.
- **Rewards:** `uh_ward_starter`; $400, 60 Lumen.

### 02 Sparks in the Walls (`uh05_02_sparks`)
- **Actors:** Tamsin Reyes (giver, speaks for Ion).
- **Rooms:** Pressure House workroom (`Venue:pressure.fuse_cabinet`, `.nest`, `.vent_valve`, `.stair_top`), then the
  service level 6 m below (`Under:service.stair_bottom`, `.juveniles`, `.construct_room`, `.service_panel`, `.vent`).
- **Flow:** inspect the fuse cabinet and the nest, divert the vent; **Decide `juveniles`** (shelter the hatchlings in
  Tamsin's crate, or guide them to the stair top); **checkpoint** `descend` (resume `Venue:pressure.stair_top`): down the
  real stairwell; if you chose guide, walk them to their home vent; **Decide `constructs`**: fight one bounded group
  (`Skirmish`, two extraction sentinels and a repair construct, no reinforcements, room `service.construct_room`) with the
  ward-direction hint, or bypass at the service panel; **checkpoint** `room` (resume `Under:service.stair_bottom`): quiet
  the vent (flag `pressure_repaired`); back up to Tamsin.
- **Rewards:** `uh_gear_choice_early`; $600, 80 Lumen, ember crystal x3, coolant salt x2.

### 03 A Creature Is Not a Battery (`uh05_03_bond`)
- **Actors:** Odile Marsh (giver), the quartz drakeling.
- **Rooms:** Bondkeeper's Refuge (`Venue:bondkeeper.care_station`, `.training`); the clamp hollow
  (`Under:service.clamp_hollow`, clamps `clamp_a`, `clamp_b`, `clamp_c`).
- **Flow:** Odile's briefing at the care station; **checkpoints** `hollow` (resume `Venue:pressure.stair_top`) and
  `rescue` (resume `Under:service.stair_bottom`): the bonds rescue (`Rescue`, bond `quartz_guide`: three clamps, keep your
  distance while it flares, tune it calm, name it); bring it home to the care station (`Care`), rank Bonded
  (`BondRank` 1) and the companion controls note; a first route-reveal assist in the training yard (`Assist`, the hand
  tool works too).
- **Rewards:** $500, 80 Lumen, ember crystal x2 (the companion is the reward).

### 04 The First Descent (`uh05_04_descent`)
- **Actors:** Corin Pell (giver), Ilse Varro at Lantern Refuge, the Valve Guardian.
- **Places:** the service tunnel (`Under:service.tunnel_top`), Lantern Refuge (`Under:refuge.entry`, `.rest`,
  `.exit_canals`, `.sample_drop`), the Ember Canals (`Under:canals.entry`, `.assist_route`, `.crossing`, `.valve_room`,
  `.pressure_valve`, `.gate`, `.sample_vent`).
- **Flow:** **checkpoint** `tunnel` (resume `Venue:pressure.stair_top`): down to the refuge; **checkpoint** `refuge`
  (resume `Under:refuge.rest`): meet Ilse, the rest note; **checkpoint** `canals` (resume `Under:refuge.exit_canals`): the
  companion reveals the cooldown route (`Assist`, tool alternative), cross to the valve room; **checkpoint** `guardian`
  (resume `Under:canals.entry`): the Valve Guardian (`UBoss`); **checkpoint** `valve` (resume `Under:canals.rest`): claim
  the descent piece, release the valve, open the return gate (`Shortcut` `sc_canals_gate`), take a cooled sample; bring it
  to the refuge, the drakeling reaches Attuned (`BondRank` 2), claim the coupler part.
- **Rewards:** `uh_descent_piece` (boss token `boss04e:valve_guardian`), `uh_coupler_part`; $1,200, 150 Lumen, ember
  crystal x4, regulator cog x1. Opens the Lantern Refuge board and the refuge, canals and service regions for contracts.

## Act II: chassis, canals, gardens and the Matron

### 05 Two Hearts, One Chassis (`uh05_05_chassis`)
- **Actors:** Jax Calder (giver).
- **Rooms:** Jax's garage bay (`Venue:garage.bay`, `.service`), a short course outside.
- **Flow:** park your own bike in the bay (`Park`; a borrowed vehicle gets the reason); **checkpoint** `calibrate`
  (resume `Venue:garage.service`): fit and calibrate the coupler, **Decide `fusion_profile`** (Updraft or Vent Brake); the
  first fusion parked in the bay (`Fuse`, form `ventwing`), claim the fusion licence; **checkpoint** `course` (resume
  `Venue:garage.bay`): Jax's handling course (`Course` `jax_handling`, one ability use); a safe separation (`Fuse`
  "separate"), claim the trim choice.
- **Rewards:** `uh_fusion_license`, `uh_trim_choice`; $1,500, 160 Lumen.

### 06 Glass Under Pressure (`uh05_06_pressure`)
- **Actors:** Dante Rook (giver), the canal salvage crew.
- **Places:** Dante's forge (`Venue:forge.service`, `.activity`); `Under:canals.actuator`, `.transit`, `.salvage_crew`.
- **Flow:** a recipe Choice that only offers what you can afford: Frost Salts (2 coolant salt) or Ember Glass (3 ember
  crystal), spent at the forge, or the always-free loaner rig; tune the sample, claim the coolant recipe; **checkpoint**
  `canals` (resume `Under:refuge.exit_canals`): fit the actuator; **checkpoint** `window` (resume `Under:canals.rest`):
  clear the transit loop (`Skirmish`, three ember scavengers, room `canals.transit`); meet the salvage crew (flag
  `canals_salvage_crew`), claim the augment choice.
- **Rewards:** `uh_recipe_coolant`, `uh_augment_choice`; $1,800, 200 Lumen, Forgewrights +4.

### 07 Gardens That Burn (`uh05_07_gardens`)
- **Actors:** Bram Achterberg (giver), the ashshell warden.
- **Places:** `Under:gardens.study_a`, `.study_b`, `.study_c`, `.cool_route_a`, `.cool_route_b`, `.seed_core`,
  `.boundary`, `.basilisk`, `.rescue_nest`.
- **Flow:** study three residents; **checkpoint** `route` (resume `Under:gardens.rest`): **Decide `cooling_route`**
  (pools, long and cool, or the root bridge, short and hot with a ward hint); lift the seed core, claim the Frostglass
  aspect; **checkpoint** `boundary` (resume `Under:gardens.rest`): hold the refuge boundary against one announced intrusion
  (`Skirmish`, an ash burrower and three scavengers, two announced reinforcements); rescue the ashshell warden (`Rescue`
  `ash_warden`); an optional Choice to free a crushed nest (flag `gardens_nest`, thanks only).
- **Rewards:** `uh_frostglass_aspect`; $2,000, 220 Lumen, seed core x1. Opens the gardens for contracts.

### 08 The Cinder Matron (`uh05_08_matron`)
- **Actors:** Bram Achterberg (giver), the Cinder Matron.
- **Places:** `Under:gardens.lure_a`, `.lure_b`, `.lure_c`, `.matron_gate`, `.matron_arena`.
- **Flow:** **checkpoint** `lures` (resume `Under:gardens.rest`): tear out the three extraction lures; cross the gate;
  **checkpoint** `fight` (resume `Under:gardens.matron_gate`): the Cinder Matron (`UBoss`, three phases with the cooled
  plates); claim the Matron aspect and gear choice; **Decide `matron_fate`** (contain her in the old cooling pen, or
  relocate her to the deep habitat; the card names what each does), then a line that says what changes from then on
  (see "Choices and their effects").
- **Rewards:** `uh_matron_aspect`, `uh_matron_gear_choice` (tokens `boss04e:cinder_matron` and
  `boss04e:cinder_matron:gear`); $3,000, 300 Lumen, matron ember x1.

## Act III: the ledger, the road and the keeping

### 09 Names on the Ledger (`uh05_09_ledger`)
- **Actors:** Iris Moon (giver), Ansel Whit at the Resonance Office.
- **Rooms:** Resonance Archive (`Venue:archive.record1` to `record3`, `.service`); the Resonance Office (`Venue:office.visitor1`,
  `.testimony`, `.back_room`, `.terminal`).
- **Flow:** compare three records; **checkpoint** `office` (resume `Venue:office.exit`): Ansel at the counter;
  **Decide `ledger_route`**: `testimony` sits you with Ansel at the bench (`Venue:office.testimony`), `terminal` sends you
  through the back room to pull the mandate from the terminal (`Venue:office.back_room`, `.terminal`). Both are nonlethal,
  as the spec asks (no fight on either). Back at the archive a Branch on the choice gives Iris's reaction (`proof_witness`:
  a witness on the record; `proof_copy`: a clean copy, nobody hurt), then both lines join at `credentials` (flag
  `ledger_credentials`).
- **Rewards:** $2,400, 240 Lumen, Frequency Order +6.

### 10 A Road for the Living (`uh05_10_road`)
- **Actors:** June Pyre (giver), her convoy, the stormskimmer.
- **Places:** `Under:workshops.entry`, `.convoy_start`, `.convoy_obstacle`, `.convoy_end`, `.storm_relay`, `.hoist`.
- **Flow:** **checkpoint** `corridor` (resume `Under:canals.rest`): clear the corridor (`Skirmish`, three scavengers and a
  glasswing); test it: a fused crossing (only offered once fusion is unlocked; `Course` `canal_crossing`), any vehicle, or
  on foot with an `Assist`; **checkpoint** `convoy` (resume `Under:workshops.rest`): protect the stopped convoy
  (`Skirmish`, a furnace caster and three scavengers, two announced reinforcements), clear the beam; rescue the stormskimmer
  at the relay (`Rescue` `storm_skimmer`); open the workshops hoist (`Shortcut` `sc_workshops_hoist`).
- **Rewards:** `uh_volt_aspect`; $3,200, 320 Lumen, regulator cog x2. Opens the workshops for contracts.

### 11 The Vaultbreaker (`uh05_11_vaultbreaker`)
- **Actors:** Dante Rook (giver), the Vaultbreaker.
- **Places:** `Under:workshops.records`, `.vault_gate`, `.vault_arena`, `.craft_point`.
- **Flow:** **checkpoint** `workshops` (resume `Under:workshops.rest`): read its work orders; **checkpoint** `fight`
  (resume `Under:workshops.vault_gate`): the Vaultbreaker (`UBoss`); claim the vault module, take the regulator, fit it at
  the craft point.
- **Rewards:** `uh_vault_module` (token `boss04e:vaultbreaker`); $4,000, 400 Lumen, vault core x1.

### 12 What We Choose to Keep (`uh05_12_keep`)
- **Actors:** Mara Solane (giver), your companion, Nessa at home.
- **Rooms:** `Venue:pressure.fuse_cabinet`; Cinder Diner (`Venue:inn.service`, `.activity`) or the Archive
  (`Venue:archive.service`, `.activity`); `Venue:bondkeeper.training`, `.care_station`; `Venue:lofts.desk`.
- **Flow:** restore the civilian conduit at Tamsin's cabinet (flag `civic_conduit_restored`); **Decide `civic_first`**
  (the diner's cold room or the archive's preservation vault; the other waits for chapter 16); **checkpoint** `care`
  (resume `Venue:bondkeeper.exit`): a care moment, the drakeling's evolution; home to the Lofts desk, the home display and
  the second loadout slot; a continuity Branch for Nessa's line.
- **Rewards:** `uh_bond_drake_evolve`, `uh_home_display`, `uh_loadout_slot2`; $2,500, 300 Lumen, Menders +6.

## Act IV: the Crown

### 13 Down to the Crown (`uh05_13_crown`)
- **Actors:** Ion Solane (giver, in person at the observatory).
- **Places:** `Venue:observatory.instrument1` to `instrument3`; `Under:reliquary.entry`, `.platform_route`, `.columns`,
  `.elite_detour`, `.revelation`, `.exit_crown`; `Under:crown.staging`.
- **Flow:** calibrate three instruments; **checkpoint** `window` (resume `Under:workshops.rest`): **Decide
  `descent_window`** (`Reask`: a retry asks again), whose text discloses both trade-offs and the detour reward; the router
  phase `reliquary` Branches on the choice:
  - **EARLY** (`dark_route`, phase `Window: early`): the reliquary's heat columns run at half heat, but its lamps are out
    (practical lights at about a fifth, ambient light and fog at 40%). Ion's radio line and a banner say so; the
    platform `Assist` takes 20 s by hand in the dark, so the companion's route sense (U) is the quick way; then the heat
    columns. No detour: the route goes straight to the revelation.
  - **LATE** (`lit_route`, `Window: late`): the heat columns run at 1.5 times their heat, but the columns are lit (the
    lit column bands and four column lights, lamps a little brighter, ambient light at 120%); the lit platform line takes
    6 s by hand; then **checkpoint** `detour` (resume `Under:reliquary.rest`): a Branch skips straight on once the
    detour is done (flag `crown_detour_done`), otherwise the `offer` Choice discloses the elite detour (an elite
    sentinel and two glasswings, reward the Void Screen aspect, once) or PRESS ON. The `elite` phase is a finite
    `Skirmish` (room `reliquary.elite_detour`, cleared for the run), then the Claim and the flag.
  - Both routes join at the revelation (continuity Branch: Mercy, Power, before the ending) and go on to the Crown's
    staging refuge. Every phase from `reliquary` on carries a `Window` (`choice` on the shared ones), so the heat and light
    hold until the quest ends; nothing is saved, so finishing, failing, abandoning or loading drops the window at once.
- **The heat columns** (`reliquary_heat_w`, `_c`, `_e` in `reliquary.archive`): three breathing grates across the
  colonnade aisle (12 heat per second, 5 s cycle, 55% breathing, staggered into a ripple) with walkable gaps and cool lanes
  behind the column rows, and a blue cooling vent (`reliquary_cool_0`) before them. Hazard heat rises from below, so the
  plain ward does not block it; the **Ember Mantle** aspect (guaranteed from the Matron) soaks 60% from its buffer and the
  cooling vent stops the grates hurting for 6 s. Offline worst cases with no aspect and the grace second ignored: a
  whole late breath about 83 of the base 200 health, walking straight across about 67; a jog across never burns.
- **Rewards:** `uh_void_aspect` (only on the late window's optional detour); $3,500, 400 Lumen. Opens the reliquary for
  contracts and the extraction trial grammar.

### 14 Break the Extraction Choir (`uh05_14_choir`)
- **Actors:** the allied crews (giver place), garden crews and city help chosen by your earlier choices.
- **Places:** `Under:crown.staging`, `.valve_mech`, `.reflector_mech`, `.escort_start`, `.regent_gate`.
- **Flow:** the muster: a Branch on `matron_fate` (the garden crews speak differently) and one on `civic_first` (the diner
  or the archive sends help); **checkpoint** `hub` (resume `Under:crown.staging`): a Choice offers only the mechanisms not
  yet silenced (`!flag:mech_...`): defend the valve, turn the reflectors, escort the maintenance construct (`Mechanism`;
  a loss restarts that mechanism, not the quest); each one sets `mech_crown_<id>` and returns to the hub checkpoint; when
  all three are silent the Branch goes to the finale and the Regent's gate opens.
- **Rewards:** $4,000, 450 Lumen, regulator cog x2, coolant salt x4, ember crystal x6.

### 15 The Kiln Regent (`uh05_15_regent`)
- **Actors:** the Regent's gate (giver place), the Kiln Regent.
- **Places:** `Under:crown.regent_arena`, `.lift`.
- **Flow:** the warning (the opening forces a safe separation; nothing is taken); **checkpoint** `fight` (resume
  `Under:crown.staging`): the Kiln Regent (`UBoss`); claim the pinnacle and the awakening; **Decide `regent_end`** (contain
  it as a custodian under a new first order, or shut it down; the card names what each does to the deep); a continuity
  Branch for the reflection; open the Crown lift home (`Shortcut` `sc_crown_lift`).
- **Rewards:** `uh_regent_pinnacle`, `uh_bond_awaken` (tokens `boss04e:kiln_regent` and `boss04e:kiln_regent:bond`);
  $6,000, 800 Lumen, regent heart x1.

### 16 A City That Answers (`uh05_16_answers`)
- **Actors:** Nessa Vale (giver), Iris (if the archive waited), Odile.
- **Rooms:** `Venue:lofts.desk`; the service that waited (`Venue:archive.activity` or `Venue:inn.activity`);
  `Venue:bondkeeper.perch`.
- **Flow:** a five-way Branch for Nessa (and Luma's mention): story ended in Mercy, in Power, ended without a law, Free
  Roam, or before the ending; install the receiver at the desk; a Branch on `civic_first` sends you to the other service;
  your companion at the perch with a Branch on `regent_end` (Odile's line, then a line naming the Crown's state, Ilse's
  new stall stock and the board's new work); the ending (flag `endgame_board`): the board now posts the endgame tier.
- **Rewards:** $5,000, 600 Lumen, Menders, Frequency Order and Forgewrights +4 each.

## Side arcs

| Arc | Quest | Beat | Places | Claims |
|---|---|---|---|---|
| Nessa | s01 The Overheated Room | A baking room in the Lofts: radiator, companion finds the hot spot (`Assist`), reroute the branch at Tamsin's cabinet | `Venue:lofts.bed`, `.visitor1`, `Venue:pressure.fuse_cabinet` | none |
| | s02 The Lofts' Music Box | Check the freight depot's ledger, recover the instrument, set it on the lobby desk | `Venue:depot.service`, `.record1`, `.retrieve`, `Venue:lofts.desk` | `uh_utility_recipe` |
| | s03 A Warm Corner | Heater from the Lofts, blankets from Odile, a warm corner in Lantern Refuge (flag `home_shelter`); checkpoint `refuge` at `Under:refuge.rest` | `Venue:lofts.desk`, `Venue:bondkeeper.habitat`, `Under:refuge.residents` | none |
| Jax | s04 Bond and Chassis | Park in the bay, read the coupler diagnostics, find the mismatch (`Assist`) without stripping a part | `Venue:garage.bay`, `.service` | `uh_trim_second` |
| | s05 A Second Profile | Fuse in the bay, ride the second course using the ability twice, separate; checkpoint `course` at `Venue:garage.bay` | `Venue:garage.bay` | `uh_handling_sidegrade` |
| | s06 The Rare Coupler | A bounded store-room fight (two repair constructs and a sentinel), pry the coupler loose, bring it to Jax; checkpoint `room` at `Under:workshops.rest` | `Under:workshops.coupler_room`, `Venue:garage.bay` | `uh_chassis_cruiser` |
| Mara | s07 Cold Comfort | Gather four coolant salts in a cool canal pocket, hand them to the clinic; checkpoint `pocket` at `Under:canals.rest` | `Under:canals.cool_pocket`, `Venue:clinic.service`, `.record1` | `uh_care_kit` |
| | s08 Walking Home | Carry a lost kiln crab from Odile's habitat to the gardens habitat; no fighting; checkpoint `habitat` at `Under:gardens.rest` | `Venue:bondkeeper.habitat`, `Under:gardens.habitat` | `uh_bond_ash_evolve` |
| | s09 Quiet Hands | Read a burned canal worker's chart, prepare a poultice, treat him with your companion close | `Venue:clinic.service`, `.visitor1`, `.record1`, `.activity` | `uh_ward_visual` |
| Iris | s10 The Lift Glyph | An archive clue, the old refuge lift, trace the glyph (`Shortcut` `sc_refuge_lift`); checkpoint `lift` at `Under:refuge.rest` | `Venue:archive.record1`, `Under:refuge.lift` | `uh_map_knowledge` |
| | s11 The Beeping Ledge | Find Iris's survey drone over the gardens vista by navigation (`Assist`), bring it home; checkpoint `search` at `Under:gardens.rest` | `Under:gardens.entry`, `.vista`, `Venue:archive.service` | `uh_bond_storm_evolve` |
| | s12 Two Entries, One Truth | Compare two entries, judge the forgery (a wrong pick costs a second look, never the quest), reach the sealed cache (elite `Skirmish`); checkpoint `right` at `Under:reliquary.rest` | `Venue:archive.record2`, `Venue:office.records`, `Under:reliquary.elite_detour` | `uh_elite_choice` |

Side quest pay: s01 $500 / 60 Lumen / coolant salt x2; s02 $700 / 80; s03 $900 / 120 / ash resin x3; s04 $1,200 / 120;
s05 $1,400 / 140; s06 $2,500 / 250; s07 $900 / 100; s08 $1,100 / 120; s09 $1,000 / 100 / Menders +4; s10 $1,200 / 150;
s11 $1,400 / 160; s12 $2,500 / 300 / Frequency Order +4.

## Choices and their effects

All keys and values are fixed in `QuestIds04E.ChoiceKeys` and `ChoiceValues`; the validator rejects anything else. Read a
choice with `Game.Quests.Choice(key)` ("" until made).

| Key | Asked in | Values | What it changes |
|---|---|---|---|
| `juveniles` | 02 | shelter, guide | Shelter: the hatchlings stay in Tamsin's crate. Guide: an extra phase walks them to their home vent below. The 04E residents read it too |
| `constructs` | 02 | fight, bypass | Fight: one bounded skirmish. Bypass: the service panel freezes them. `Reask` after a failed attempt |
| `fusion_profile` | 05 | updraft, ventbrake | The Ventwing handling profile: read by the garage (`PersonalGarage.ActiveProfile`) until Jax swaps it |
| `cooling_route` | 07 | a, b | The pool route or the hot root bridge to the seed core |
| `matron_fate` | 08 | contain, relocate | 08: the aftermath phase. 14: the garden crews' line at the muster. From then on (code, read from the stored choice): the Underheart residents (contain: her wandering hatchlings sleep at her nest, the gardens habitat crab is calm; relocate: her nest is empty, the habitat crab still guards, her brood sleeps in the reliquary rest room and the reliquary moths thin out), Bram's greeting, and a board variant (contain: Bram's "Pen feed" runs to the gardens caretaker; relocate: "Matron's stray" rescues in the reliquary or at her old nest) |
| `ledger_route` | 09 | testimony, terminal | The testimony bench or the back-room terminal (both nonlethal), and Iris's reaction before both join at the credentials |
| `civic_first` | 12 | diner, archive | 12: which service gets power first. 14: who sends help. 16: you power the other one |
| `descent_window` | 13 | early, late | Early: cool heat columns, lamps out, slow hand search, no detour. Late: hot heat columns, lit columns, quick hand search, the optional elite detour. `Reask` after a failed attempt |
| `regent_end` | 15 | contain, shutdown | 15: the ending phase. 16: what Odile says at the perch. From then on (code): the deep habitat (contain: moths on the Crown's furnace balcony, young hatchlings in the staging refuge; shutdown: the Crown empties, one moth per reliquary hall), the Lantern Refuge (Ilse's greeting and her board line; her trade stall: warm-grown ember crystal and a seed-core, or cold-run coolant salt and regulator cogs) and a board variant (contain: "Custodian lens" work in the reliquary gallery; shutdown: "Hand valve shift" in the Crown's valve chamber) |

Every key changes play (a Decide that routes to phases, or a Branch that reads it); `ChoiceConsequences04ETests` fails if a
key is ever left recorded with nothing reading it (`fusion_profile` is read by code, so it is the one listed exception).
`matron_fate` and `regent_end` are also read by code for the rest of the game, and the same test fails unless each of
their answers changes the Underheart's residents and posts its own board variant.

**The later consequences in code.** Nothing new is saved: everything reads `Game.Quests.Choice(key)`, which the campaign
save block already keeps, so a reload shows the same world.

- *Residents* (`Creatures/Underheart/ChoiceEcology04E.cs`, applied by `UnderheartEcology.Residents.cs`): the full table
  is in ECOLOGY.md section 2b. Regions stay at or under 6 residents; a changed slot leaves off camera and a new one
  settles out of view.
- *The cast* (`Gameplay/Guidance04E/OutcomeLines04E.cs`): Ilse Varro's later greeting follows `regent_end`, Bram
  Achterberg's follows `matron_fate`; first-meeting lines and everyone else stay as authored.
- *The Lantern Refuge stall* (`RefugeTrade04E.cs`, `CampaignDirector.Refuge.cs`): once `regent_end` is decided, talking
  to Ilse (her prompt says "and trade at the Lantern Refuge stall") opens a pausing card that sells one bundle per pick
  for cash through the existing Economy and MaterialStore. Contain: 3 ember crystal ($320) and 1 Pyre seed-core ($390).
  Shutdown: 5 coolant salt ($180) and 2 regulator cogs ($270). Every bundle costs more per piece than a Forgewright pays
  for it, nothing sold is a boss material, and the stock is unlimited (it is a shop, priced above the sell value).
- *The board* (`CampaignContracts04E.Variants`, see the board section): one variant per answer.

**The descent window in code.** A campaign phase may set `Window` (`early`, `late`, or `choice` for the stored
`descent_window`; the validator checks the values and that `choice` follows an earlier Decide). `UnderheartWorld.Window`
reads the running quest's phase four times a second (no save field, no switch), and only inside the Ash Reliquary:
breathing heat and lava scale (`UnderheartWindowRules.HeatScale`, read by the hazard code and `HazardAt`, so companions
and creature spawns see the same heat), practical lights scale from their built intensity, the atmosphere's
`UndergroundLight` scales the ambient light and fog colour, vent rings and steam read hotter or calmer (never dimmer than
85%, so the boundary shows in the dark), and the late window shows the lit column bands and lets the four column lights
into the light budget. F2 > Underheart can force early, late or none, or hand it back to the quest.

**Continuity Branches** (story ending and Free Roam): 01 (Nessa's opening), 12 (Nessa at home), 13 (the revelation),
15 (the reflection) and 16 (Nessa and the five-way homecoming) each branch on `story_done&law=Mercy`,
`story_done&law=Power` and `else` (16 also on `story_done` and `freeroam`).

**Flags** set by the campaign (read with `Game.Quests.Flag(name)`): `dispatch_receivers_read` (01), `pressure_repaired`
(02), `canals_salvage_crew` (06), `gardens_nest` (07, optional), `ledger_credentials` (09), `civic_conduit_restored`
(12), `crown_detour_done` (13, the late detour finished), `endgame_board` (16), `home_shelter` (s03),
`mech_crown_valve`, `mech_crown_reflectors`, `mech_crown_escort` (14).
Other streams may read them for visuals (the Pressure House sparks, the refuge's warm corner, the lit civic conduit).

## Checkpoints

| Quest | Checkpoint phases (resume place) |
|---|---|
| 01 | fitting (`Venue:dispatch.visitor1`) |
| 02 | descend (`Venue:pressure.stair_top`), room (`Under:service.stair_bottom`) |
| 03 | hollow (`Venue:pressure.stair_top`), rescue (`Under:service.stair_bottom`) |
| 04 | tunnel (`Venue:pressure.stair_top`), refuge (`Under:refuge.rest`), canals (`Under:refuge.exit_canals`), guardian (`Under:canals.entry`), valve (`Under:canals.rest`) |
| 05 | calibrate (`Venue:garage.service`), course (`Venue:garage.bay`) |
| 06 | canals (`Under:refuge.exit_canals`), window (`Under:canals.rest`) |
| 07 | route (`Under:gardens.rest`), boundary (`Under:gardens.rest`) |
| 08 | lures (`Under:gardens.rest`), fight (`Under:gardens.matron_gate`) |
| 09 | office (`Venue:office.exit`) |
| 10 | corridor (`Under:canals.rest`), convoy (`Under:workshops.rest`) |
| 11 | workshops (`Under:workshops.rest`), fight (`Under:workshops.vault_gate`) |
| 12 | care (`Venue:bondkeeper.exit`) |
| 13 | window (`Under:workshops.rest`, asks the window again), detour (`Under:reliquary.rest`, late window only) |
| 14 | hub (`Under:crown.staging`), returned to after each mechanism |
| 15 | fight (`Under:crown.staging`) |
| s03, s05, s06, s07, s08, s10, s11, s12 | one each, listed in the side arc table |

Rules (the validator enforces them): a checkpoint phase needs a `ResumeAt` that is a place or token (never a person,
`Player` or `Previous`); a Collect that spends materials never sits in a checkpoint phase; campaign quests never use
`Give` or `GiveGun`; every Claim reward is listed in the quest's `Claims` and the other way round.

## Reward totals

| | Cash | Lumen | Materials | Reputation | Claims |
|---|---|---|---|---|---|
| Main (16) | $41,600 | 4,760 | ember crystal 15, coolant salt 6, regulator cog 5, seed core 1, matron ember 1, vault core 1, regent heart 1 | Forgewrights 8, Frequency Order 10, Menders 10 | 19 (one optional: `uh_void_aspect`, late window only) |
| Side (12) | $15,300 | 1,700 | coolant salt 2, ash resin 3 | Menders 4, Frequency Order 4 | 10 |

Every one of the 32 `RewardIds04E` is granted exactly once in play: 29 by quest Claims (above) and the three starter
kits (`uh_kit_wardkeeper`, `uh_kit_conduit`, `uh_kit_riftstrider`) by the fitting station's own once-only build choice.
06 spends 2 coolant salt or 3 ember crystal unless you take the free loaner rig. No quest reward is near the validator
limits ($60,000, 10,000 Lumen per quest).

## The Lantern Refuge board (contracts)

The board stands at `Under:refuge.map_board` (an E prompt and a **C** blip). It opens after The First Descent, posts three
jobs (four in the endgame tier after A City That Answers) and reposts every 20 minutes of active play; a finished job is
replaced. Jobs are made from eight grammars in `underheart_contracts.json` (`QuestIds04E.Contracts`). Each job is a
generated campaign quest (`contract_underheart_<serial>`) run by the same runtime, deterministic per serial (saved in
`CampaignSave.BoardSerials`), sent only to regions the story has opened, with a readable risk (Low to Extreme) and pay.

| Id | Grammar | Risk | Regions | Opens | What the job is |
|---|---|---|---|---|---|
| `uhc_salvage` | Salvage | 2 | canals, workshops, reliquary | 04 | Reach the site, drive off three scavengers, strip three crates, report |
| `uhc_elite_hunt` | EliteHunt | 4 | gardens, workshops, reliquary, crown | 07 | An elite sentinel with an escort; nothing else comes |
| `uhc_valve_defense` | ValveDefense | 3 | canals, workshops, crown | 04 | Start a valve cycle, hold it (two announced reinforcements), lock it |
| `uhc_rescue` | CreatureRescue | 1 | canals, gardens, reliquary | 04 | Find a trapped resident (companion assist or by hand), free it |
| `uhc_lens` | LensCalibration | 1 | canals, gardens, reliquary | 04 | Align three lens facets |
| `uhc_courier` | ShortCourier | 2 | canals, gardens, workshops | 04 | A cold sealed case from the board to a site in 5 minutes |
| `uhc_surveyor` | SurveyorSearch | 2 | gardens, workshops, reliquary | 07 | Follow the trail (assist or chalk marks), find the surveyor, report |
| `uhc_trial` | ExtractionTrial | 5 | workshops, reliquary, crown | 13 | A declared roster plus two announced reinforcements in 4 minutes |

Pay: base cash and Lumen scaled by risk (+25% per level), loyalty (+4% per job done, capped at ten) and the endgame tier
(x1.3, risk +1, one more of each roster entry and material); always $100 to $12,000 and 10 to 2,500 Lumen, materials at
most 10, never a boss material, no guild reputation. "Opens" is the quest whose region (or `After`) the grammar needs.

**Outcome variants.** Once a decision is made, one grammar per answer is bent (`CampaignContracts04E.Variants`; the board
context carries the stored choices, `CampaignBoardContext.Choice`). The variant is posted 2.5 times as often as its
grammar, goes to the outcome's places whenever they are open (a subset of the grammar's own destinations) and has its
own title and brief; its steps, roster, risk and pay are the grammar's, so it is exactly as valid and bounded. All eight
grammars stay on the board either way, and the validator (which knows no choices) sees the authored jobs.

| Decision | Grammar bent | Posted as | Goes to |
|---|---|---|---|
| `matron_fate` contain | `uhc_courier` | Pen feed: a cold feed case for the Matron's pen crew | `Under:gardens.caretaker` |
| `matron_fate` relocate | `uhc_rescue` | Matron's stray: free one of her strays | `Under:reliquary.safe_route` (once the reliquary is open) or `Under:gardens.rescue_nest` |
| `regent_end` contain | `uhc_lens` | Custodian lens: keep the custodian reading the living | `Under:reliquary.window` |
| `regent_end` shutdown | `uhc_valve_defense` | Hand valve shift: guard a crew turning valves by hand | `Under:crown.valve_mech` (once the Crown is open) |

Ilse's line at the top of the endgame board follows `regent_end` too ("The Crown hums steady under its custodian" or
"The Crown runs cold now"). The variant rules live in code because a contract template has no field for a choice and
the shipped template count is frozen at eight; a `When` field on `ContractDef` (with the validator and the freeze lists)
would let them move into `underheart_contracts.json`.

## Code map

| File | What it holds |
|---|---|
| `Gameplay/Quests/QuestData.cs` | Appended fields: `QuestFile.Campaigns`, `CampaignDef`, `RosterEntry`; QuestDef `Kind`, `Campaign`, `Act`, `Difficulty`, `Region`, `Floor`, `Entrance`, `Claims`; QuestPhase `Checkpoint`, `ResumeAt`, `Window`; the 04E step fields; ContractDef `Regions`, `Risk`, `After`, `Roster`, `Reinforce` |
| `Gameplay/Quests/CampaignRules04E.cs` | Pure: place tokens, regions, the condition grammar, checkpoints, choices, claim tokens, reward labels, phase windows (`ResolveWindow`) |
| `World/Underheart/UnderheartWindowRules.cs`, `UnderheartWorld.Window.cs` | Pure window numbers (heat, light, ambient, column lights, exposure damage); the runtime that reads the running phase and applies them |
| `Tests/EditMode/ChoiceConsequences04ETests.cs` | The descent window, the heat columns, the detour, the ledger route and the every-choice-is-read guard |
| `Gameplay/Quests/CampaignContracts04E.cs` | Pure: board eligibility, pay, the eight grammars as quest data, the outcome variants and Ilse's board line |
| `Creatures/Underheart/ChoiceEcology04E.cs` | Pure: the residents in force for `matron_fate` and `regent_end` |
| `Gameplay/Guidance04E/OutcomeLines04E.cs`, `RefugeTrade04E.cs`, `CampaignDirector.Refuge.cs` | Pure greetings by decision; the pure refuge stall stock and prices; Ilse's stall card at runtime |
| `Tests/EditMode/ChoiceOutcomes04ETests.cs` | The choice ecology, the greetings, the stall, the board variants and the boss Resonance rules |
| `Gameplay/Quests/CampaignPlacesRuntime04E.cs` | Token to live point (venues, the Underheart, cast, the arsenal) |
| `Gameplay/Quests/QuestSteps.Underheart*.cs` | The 18 step runtimes and the exact-point helpers |
| `Gameplay/Quests/QuestBook.Campaign04E.cs` | `Game.Quests` campaign API: choices, flags, checkpoints, resume, reservation, the save block |
| `Gameplay/Quests/QuestBook.Contracts04E.cs`, `QuestBook.Debug04E.cs` | The refuge board; the F2 "Campaign" tab (Order 63) |
| `Gameplay/Quests/QuestValidator.Campaign04E.cs` | The campaign validation rules |
| `Tests/EditMode/Campaign04ETests.cs` | 34 offline tests over the shipped files and the rules |

`Game.Quests` campaign API: `Choice(key)`, `SetChoice`, `Flag(name)`, `SetFlag`, `CheckpointSafeNow`, `IsCampaign(q)`,
`CheckpointOf(id)`, `CanResume(id)`, `ResumeLabel(id)`, `ResumeQuest(id)`, `OfferText`, the `CampaignChanged` event,
and the board: `CampaignBoardPlaced`, `CampaignBoardPosition`, `CampaignBoardOpen`, `CampaignBoardOffers`. The save block is
`CampaignSave` (Version, Checkpoints, Choices, Flags, PendingGrants, BoardSerials, BoardNextSerial, ContractsDone,
BoardPlaySeconds), frozen by `Campaign04ETests`. The module reports "campaign_04e" Loaded, or Partial with the missing
systems named.

**F2 > Campaign:** clear choices and flags, clear checkpoints, repost the board, check the campaign data, and per quest
Start (teleport and start, ignoring requirements), Complete, Reset and Clear checkpoint.

## What to check in Unity

Nothing here has been played. In this order:

1. **Tests**: Test Runner > EditMode: Campaign04ETests, QuestDataTests, QuestFixTests green. F2 > Campaign > Check campaign
   data: 0 errors.
2. **The first route, 01 to 05, in story mode after mission 1**: Nessa's offer at the Lofts; Corin and Ion's screen inside
   Cinder Dispatch (are markers on the floor, not the roof?); the build at the fitting worktable and the starter ward
   claim; the practice at the real 04D case and lane, and "I KNOW IT"; the conduit glow to the Pressure House door;
   Tamsin's workroom, the real stairwell down, both `constructs` paths; the drakeling rescue and care; the tunnel to Lantern
   Refuge, the canals assist, the Valve Guardian, the gate, the sample; Jax's bay, the first fusion, the course, the
   separation.
3. **Checkpoints**: quit mid 04 after the guardian; reload: the journal offers RESUME, the **R** point stands at the canals
   rest, and the quest starts at the valve with the descent piece not granted twice. Die in a skirmish and Retry.
4. **The board**: after 04, the board's C blip in Lantern Refuge, three jobs, a job finished and replaced.
5. **Locks**: a campaign quest whose room or region is missing must say why in the journal instead of starting.
6. **Choices later**: make `civic_first` and `matron_fate` choices and see 14 and 16 change lines. After 08, check the
   residents for your `matron_fate` answer (ECOLOGY.md 2b), Bram's greeting and a "Pen feed" or "Matron's stray" job on
   the board (repost it from F2 > Campaign if none shows). After 15, check Ilse's greeting, her stall card (buy one
   bundle: the cash drops and the material arrives; with too little cash nothing is taken), the Crown's residents, the
   endgame board line and a "Custodian lens" or "Hand valve shift" job. Save and reload: everything stays.
7. **The descent window** (F2 > Underheart > Force early / Force late lets you look before Act IV): in the Ash Reliquary,
   early should read clearly darker with the heat-column rings and the blue vent still easy to see, and late should show
   the lit gold column bands and brighter halls. Stand in a breathing grate in each window and read the HUD heat row (about
   6 per second early, 18 late, 12 with no window), then step out and check the gaps between the grates are walkable. In
   uh05_13 pick LATE, take and win the detour, fail on purpose afterwards: the retry must skip the detour and not grant the
   Void Screen twice. Pick EARLY: no detour is offered. Fail before the detour: the window menu comes back.

## Review fixes (9315fe4)

- The `constructs` Decide in `uh05_02_sparks` has `"Reask": true`, so a failed fight asks again; `juveniles` still stands once made.
- A Flag step `pressure_vent_diverted` follows the vent valve; the Pressure House sparks stop when it is set.
- The `uh05_04_descent` canals phase runs entry, coolant catwalk, hot-vent assist, valve room (no backtracking).
- Leaving a boss arena resets the fight (up to 12 resets) instead of failing the quest; death still fails it.
- The ash warden and storm skimmer are Bonded right after their rescues and reach Attuned after the Matron and at the `uh05_12` care moment.
