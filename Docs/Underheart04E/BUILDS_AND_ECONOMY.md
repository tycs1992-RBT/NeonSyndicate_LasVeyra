# Builds, the ward and the 04E economy

This page covers everything the BUILDS stream added for "Underheart and Bonds": the BUILD overview page, the three
build suggestions and their starter kits, the starter ward (the handheld Lattice Shield, called the Buckler), the four
ward aspects, the MoveSpeed gear affix, the Fracture Lattice focus module, the two loadouts, the reward ledger with its
32 rewards, the fitting stations and ledger terminals, and the economy sheet that ties early costs to guaranteed early
income.

Nothing here has been run inside Unity yet. The code compiles in all twelve configurations and the pure rules pass
their offline tests; how it looks and feels must be checked in a native session (the checklist is at the end).

## 1. Where the player meets it

- **BUILD page**: press **K** (or open the pause menu and pick the BUILD tab; on a controller LB and RB switch tabs).
  It is pause tab Order 4, just before GADGETS. Five sub tabs:
  - **OVERVIEW**: the crystal focus in hand and the four loot slots (FOCUS, FRACTURE and "in hand" tags), the six gear
    slots with a compare against the same slot and an Equip button for better owned pieces, the ward and its aspect,
    both gadget slots, the active companion, the personal chassis and fusion form, a stats preview (health, armor,
    resistances, movement speed, parry window, gadget power) computed by the same helpers the game uses, the element
    and set lines, the next shrine upgrade with its real Lumen cost or the reason it is blocked, and a schematic
    paper-doll with a ward silhouette per aspect.
  - **SUGGESTIONS**: the three build cards (Wardkeeper, Conduit, Riftstrider) with Preview (anywhere), Apply (only at
    a fitting station) and Revert.
  - **WARD**: aspect buttons, the Fracture Lattice fit buttons, the Mender's Halo toggle, the heat buffer and Coolant
    Draught time left.
  - **REWARDS (n)**: every pending reward with its offers side by side (compare against the worn piece, worth in Lumen
    and in cash), Choose and Claim, and the claimed history.
  - **LOADOUTS**: save and equip loadout 1; loadout 2 unlocks with a reward.
- **HUD**: a ward panel bottom right (state, capacity bar while raised, READY with the key name, BROKEN or RECOVERING
  seconds, Void Screen window, the Ember Mantle heat buffer or the Volt Lattice charge). A small chip says when rewards
  are waiting (out of combat only). A banner announces each reward as it is earned and as it is claimed.
- **Pause map**: F marks fitting stations and L marks ledger terminals. With the Survey notes reward, R marks rest
  points, S marks shortcut gates in the Underheart regions already discovered, and E marks the optional elite site.
- **F2 debug page "Builds 04E"** (Order 56): state lines, a reward picker with Grant and Grant and claim (debug
  tokens, saved like real ones), unlock the ward, unlock every aspect, place a fitting station here, open the ledger, and
  the ward's live debug line.

## 2. Stations

| Station | Where | What it does |
|---|---|---|
| Dispatch fitting worktable (`dispatch_worktable`) | Cinder Dispatch, venue anchor `fitting` (registered when VENUES builds the room) | Opens BUILD > SUGGESTIONS; Apply works here |
| Refuge fitting station (`refuge_fitting`) | Underheart anchor `refuge.fitting` (registered once `Game.Underheart.Ready`) | Same as above |
| Home reward ledger (`home_ledger`) | Veyra Lofts, beside the desk anchor, local offset (3.4, 0, 0.2) | Opens BUILD > REWARDS; the home trophy case stands next to it |
| Refuge reward ledger (`refuge_ledger`) | Underheart anchor `refuge.ledger` | Opens BUILD > REWARDS |

Fitting refuses (with the reason on screen) while dead, in a vehicle, in combat or with the ward raised. Stations are
built from code: a mini Buckler emblem spinning over the worktable, a ledger plinth with a screen, a crystal reader and
a status lamp that pulses while rewards are pending. Other streams can add stations with
`RegisterFittingStation(id, pos, parent)` and `RegisterLedgerTerminal(id, pos, parent)`.

## 3. Starter grants

- **Starter ward** (`uh_ward_starter`, from the first quest at Cinder Dispatch): unlocks the handheld Lattice Shield
  (Buckler) right away, whatever the player's level, and puts it in a free gadget slot. Below level 11 (the Buckler's
  normal unlock level) it is the **starter variant** at 80% capacity; from level 11 it is the full Buckler. Only the
  Buckler opens early: the Palisade and every other gadget keep their level gates. A banner explains the unlock once.
- **Starter kits**: the first time each suggestion is applied at a fitting station, its kit is granted and claimed
  through the ledger with the token `kit:<preset>`, once per save, ever. Kits are modest (item power 1 to 3, one Rare and
  one Uncommon piece). A kit piece is **skipped** when the player already owns a piece in that slot with an equal or
  better gear score and an equal or better build match, so a kit never duplicates something you beat.

| Kit | Pieces | Extra |
|---|---|---|
| Wardkeeper | Rare Shell (health, armor, bullet resistance), Uncommon Gauntlets (guard cost, parry window) | |
| Conduit | Rare Implant (crystal energy, power damage), Uncommon Visor (energy, critical chance) | 4 Coolant Salt |
| Riftstrider | Rare Striders (movement speed, dodge invulnerability, stamina), Uncommon Visor (parry window, critical chance) | |

Trying all three suggestions gives all three kits once each. That is intended: they are starter tier and fill
different slots.

## 4. The three build suggestions

| Suggestion | Key slots | Focus it prefers | Gadgets | Suggested aspects | Honest tradeoff |
|---|---|---|---|---|---|
| Wardkeeper | Shell, Gauntlets, Visor | Shardwake, then Mirrorwake | Buckler, Ripple | Ember Mantle, Frostglass | Forgiving, not the highest sustained damage; the ward breaks and must recover |
| Conduit | Implant, Core, Visor | Prism Thread, then Shardwake | Buckler, Gadfly, Ripple | Volt Lattice, Ember Mantle | Sustained crystal pressure, shorter defensive uptime |
| Riftstrider | Striders, Gauntlets, Implant | Gyre Crescent, then Shardwake | Buckler, Ripple, Balm | Void Screen, Frostglass | Mobility, less sustained protection, no automatic flight |

Rules (pure, in `BuildPresetRules04E`, tested):

- **Preview** works anywhere; **Apply** only at a fitting station.
- Apply first saves the current setup, so **Revert** puts everything back for free.
- Only **owned** items move. Nothing is bought, crafted or created by Apply (the kit is a separate, once-only grant).
- Only the suggestion's **key slots** change. An empty key slot takes the best-matching owned piece. A worn piece is
  replaced only by a piece with an **equal or higher gear score** whose build match is clearly higher (more than 0.1).
  So Apply never silently downgrades.
- The focus: the preferred crystal focus already in a loot slot is selected; otherwise an owned focus goes into a free
  loot slot; if no slot is free the page says so instead of dropping anything (`LootArmory.Equip` refuses rather than
  drop a focus or a favourite).
- Gadgets: the first wanted gadget may replace a gadget the suggestion does not want (slot 2 first); the others only
  fill empty slots; locked gadgets are never slotted.
- Aspect: the first suggested aspect the player has unlocked becomes active.
- `PresetChosen` (which the campaign's Build step waits for) fires after the menu closes.

## 5. The ward (handheld Lattice Shield)

Press the ward's gadget key to raise it (1 or 2 on the keyboard, hold D-pad left or right on a pad; the key table is in
`Docs/CONTROLS.md`). It stays up for 12 seconds or until it breaks, then recovers on the Buckler's normal 28 second
cooldown (gadget mods and gear shorten it as usual).

**Capacity** comes from the Buckler's shield points: 260 times the gadget level scale (+2% per level), times the
slot's gadget power bonus (capped at +60%), times 1.25 with the Kiln Crown, times 0.8 for the starter variant below
level 11.

| Level | Starter variant | Full Buckler, no bonus | With +60% power and the Kiln Crown |
|---|---|---|---|
| 1 | 208 | 260 | 520 |
| 10 | 245 | 306 | 614 |
| 11 | (full from here) | 312 | 624 |
| 20 | | 359 | 718 |
| 30 | | 411 | 822 |
| 50 | | 515 | 1030 |

**Interception** runs in the shared player damage path through `PlayerDamageHooks` (key `ward04e`, order 100, after
dodge, guard and parry, before gear and skill resistances):

- Bullets, melee, power hits and directed fire are blocked when the attacker is within **70 degrees** of the ward's
  facing on either side; outside that, nothing is blocked.
- Explosions are blocked by **angle share**: everything within 40 degrees, nothing past 115 degrees, a straight line
  between; a blast with no direction counts as half.
- Falls, vehicle hits and unblockable attacks are never intercepted. Hazard heat that rises from the floor is not
  "directed fire" and belongs to the Ember Mantle instead.
- **Beams** arrive as damage ticks, so the ward drains by time (the same total at any tick rate; tested).
- The ward absorbs up to its remaining capacity; the rest of the hit lands. When capacity runs out it **breaks**
  (state BROKEN, then RECOVERING on the cooldown). A hit fully absorbed also loses its knockdown.
- **No double counting**: bullets that physically stop on the ward's face (layer 15) are charged to the same capacity
  pool; the face ignores explosions, which the hook alone takes by angle. Friendly fire (player or companion team) is
  never charged.
- **No reflection loops**: the ward never sends damage back as a projectile. The Volt Lattice discharge is a separate
  limited hit with its own cooldown, and Frostglass deflection simply cancels the hit.

## 6. Ward aspects (optional, reward-unlocked, one active)

Switching aspects is free, but only out of combat and with the ward lowered. The first aspect unlocked becomes active.

| Aspect | Reward | What it does | Limit or cost | Look |
|---|---|---|---|---|
| Ember Mantle | `uh_matron_aspect` (Cinder Matron) | A heat buffer (80 at level 1, +2 per level, capped at 160) absorbs 60% of each heat hit; refills at 8 per second after 2.5 s without heat; smothers burning (rate 3, costs 6 buffer each); fire costs the raised ward half capacity | Never full immunity: at least 25% of heat always lands | Ember scale plates, glowing seams, vent stacks |
| Frostglass | `uh_frostglass_aspect` | The first 0.6 s after raising the ward deflect frontal direct hits at no capacity cost; parry window +50 ms | The parry bonus stays inside the usual 0.12 s cap | Faceted glass diamond with icicles |
| Volt Lattice | `uh_volt_aspect` | Every 90 damage blocked from enemies charges one discharge into up to 3 foes within 9 m in front, line of sight checked (24 damage at level 1, +2% per level) | 4 s cooldown; extra charge is not banked; refunds nothing | Open grid with coils and arc pylons |
| Void Screen | `uh_void_aspect` | When the ward breaks: a 1.2 s repositioning window (+30% movement speed) and the breaking hit plus the next 0.35 s pass through you | +6 s on the ward's cooldown, every break | Hollow ring, ink disk, orbiting fragments |

Cosmetic extras: the **Kiln Crown** (from the Regent pinnacle choice) adds molten spikes and +25% capacity for good;
**Mender's Halo** (`uh_ward_visual`) adds a halo ring and can be toggled on the WARD tab.

The **Coolant Draught** brew (section 9) also feeds the same heat path: 30% less heat while it lasts, combined with
the Ember Mantle in one calculation, still with the 25% floor.

## 7. MoveSpeed affix

`AffixType.MoveSpeed` was appended at value 18 (before `Count`), so old saves read exactly as before (frozen by
`Builds04ETests.AffixTypeValuesAreAppendOnly`). It rolls only on **Striders** (2% to 9% per piece depending on quality).
The total from gear is capped at **12%**; the Void Screen window adds 30%; the combined on-foot multiplier is capped at
1.35. It applies to on-foot movement through one line in `PlayerController` (`GearSystem.MoveSpeedMultiplier`).
Vehicles, travel powers and fusion are not affected.

## 8. Fracture Lattice (Vaultbreaker reward)

A module fitted to **one** crystal focus at a time (fit or refit for free on BUILD > WARD, anywhere out of combat; at
claim time it goes on the focus in hand or the first owned focus; it is saved by the focus's item uid and checked once
after loading). In the player's hands only, a direct bullet hit from
that focus that deals at least 1 damage to a hostile target splits **2 fracture shards** (3 from a Legendary focus)
into the nearest other hostile targets within 7 m that are in line of sight, each dealing 40% of the hit. With no other
target in reach it re-strikes the same target for 25%, at most every 0.7 s. Shards are plain hits: they never trigger
more shards, never trigger the module again, and the module triggers at most every 0.35 s. NPCs never use it.

## 9. Loadouts and recipes

- **Loadouts**: loadout 1 is always there; loadout 2 unlocks with `uh_loadout_slot2`. A loadout stores the six gear
  uids, the four loot slot uids, the two gadgets, the aspect, the gun in hand and the suggestion. Equipping one works
  out of combat, uses only items still owned and reports how many are missing.
- **Coolant Draught** (`brew_coolant_draught`, taught by `uh_recipe_coolant` or known after `uh05_06_pressure`): costs
  $90, 3 Coolant Salt and 1 Ash Resin; brews in the Forge; lasts 8 minutes: 30% less heat (read by the ward hook) and
  6% less damage overall.
- **Lantern Oil** (`brew_lantern_oil`, taught by `uh_utility_recipe` or known after `uh05_s03_shelter`): costs $80,
  1 Ember Crystal and 2 Coolant Salt; lasts 10 minutes: +10% creature loot rolls and 15% faster stamina recovery.

## 10. The reward ledger

Every 04E reward goes through one ledger (`Game.Builds.Ledger`, saved in `SaveData.Builds.Rewards`).

- **Once per token.** `Grant(rewardId, token, source)` stores an entitlement the first time a token is seen and
  ignores every repeat. A token is the identity of the moment that earned the reward: a quest claim step, a boss first
  win, a kit. Repeated callbacks, reloads, leaving the arena or reopening the reward screen can never grant twice.
- **No rerolls.** At grant time the ledger stores a seed (FNV-1a of the token), the item power, the loot level and the
  build suggestion. Offers are rebuilt from those stored values only, so a pending choice shows the same items forever.
- **Commit or rollback.** Claiming runs one transaction: check room and prerequisites for every part first, then
  apply the reversible parts (gear, guns, materials, mods), then the external unlocks (recipes, aspects, flags, garage,
  bonds). If any part fails, every applied part is undone in reverse and the entitlement stays pending with the reason.
  The entry is marked claimed only after everything landed.
- **Never lost to a full inventory.** A reward that cannot be delivered stays pending on BUILD > REWARDS (claimable
  anywhere) and at both ledger terminals. Boss rewards are stored even when the inventory is full.
- **Protection.** Reward guns arrive marked as favourites (they cannot be sold or scrapped by accident). Claim is
  always a deliberate button press; choice rewards never auto-pick.
- **Choice rewards stay pending** even through `GrantAndClaim`, with a message telling the player where to choose.

### What other streams call

```
Game.Builds.Ledger.Grant(rewardId, token, source)                    // store the entitlement only
Game.Builds.Ledger.GrantAndClaim(rewardId, token, source, out msg)   // store and try to deliver now
Game.Builds.Ledger.TryClaim(token, choiceIndex, out msg)
Game.Builds.Ledger.Has / IsPending / IsClaimed / PendingCount / Entries, event Changed
```

Token patterns: `quest:<questId>` (or `quest:<questId>:<rewardId>` when one quest gives several), `kit:<preset>`,
`boss04e:<bossId>` from `UnderBossIds.Token(id)`, with `:gear` and `:bond` suffixes for the second boss rewards, and
`debug:...` for the F2 page. Boss first wins (BOSSES grants these itself; quest Claim steps reuse the same tokens, so
nothing is granted twice):

| Boss | Token | Reward |
|---|---|---|
| Valve Guardian | `boss04e:valve_guardian` | `uh_descent_piece` |
| Cinder Matron | `boss04e:cinder_matron` | `uh_matron_aspect` |
| Cinder Matron | `boss04e:cinder_matron:gear` | `uh_matron_gear_choice` |
| Vaultbreaker | `boss04e:vaultbreaker` | `uh_vault_module` |
| Kiln Regent | `boss04e:kiln_regent` | `uh_regent_pinnacle` |
| Kiln Regent | `boss04e:kiln_regent:bond` | `uh_bond_awaken` |

## 11. The reward catalog (all 32 ids)

"Worth" is what the card shows: gear and gadget mods salvage for **Lumen** (`GearSystem.SalvageValue`,
`GadgetRules.ModSalvageValue`); loot guns and materials sell for **cash** (`LootEconomy.SellValue`, a Forgewright's
material price). Unlocks, recipes, aspects, trims and bond ranks cannot be sold and are worth nothing. "Unique" means
there is only ever one, guarded by its token.

| Id | What | Power band | Source | Upgrade path |
|---|---|---|---|---|
| uh_ward_starter | Buckler unlock | Early, 80% until level 11 | A Knock Beneath Home | Gadget mods, Kiln Crown, aspects |
| uh_kit_wardkeeper | Rare Shell, Uncommon Gauntlets | Early, power 1 to 3 | First Wardkeeper fitting | Shrine (Lumen) |
| uh_kit_conduit | Rare Implant, Uncommon Visor, 4 Coolant Salt | Early, power 1 to 3 | First Conduit fitting | Shrine |
| uh_kit_riftstrider | Rare Striders, Uncommon Visor | Early, power 1 to 3 | First Riftstrider fitting | Shrine |
| uh_gear_choice_early | Choose 1 of 2 Rare pieces (defensive or offensive for the build), each with 2 Ember Crystal and 2 Coolant Salt | Early, Rare, power 2+ | Sparks in the Walls | Shrine; salvage the outgrown one |
| uh_descent_piece | The build's signature slot with 3 guaranteed build affixes | Rare (Epic from power 6), power +1 | Valve Guardian (unique) | Shrine |
| uh_coupler_part | Coupler key item, 2 Regulator Cog, 1 Ember Crystal | Key item | The First Descent | Never consumed or sold |
| uh_fusion_license | Fusion entitlement and the Ventwing form | Signature unlock | Two Hearts, One Chassis (unique) | New forms with new bonds |
| uh_trim_choice | Choose 1 of 3 trims: ember ribs, quartz crest, canal chrome | Cosmetic | Two Hearts, One Chassis | More from Jax |
| uh_augment_choice | Choose 1 of 3 Rare gadget mods (Quickcharge, Overclock, Longcell) | Mid, Rare mod | Glass Under Pressure | Socket in GADGETS |
| uh_recipe_coolant | Coolant Draught recipe and 3 Coolant Salt | Utility | Glass Under Pressure | Rebrew from materials |
| uh_matron_aspect | Ember Mantle | Signature aspect | Cinder Matron (unique) | Free switching |
| uh_matron_gear_choice | Epic Ember Shell (power +2) or a Legendary crystal caster (loot level +2), each with 1 Matron Ember | Mid to high | Cinder Matron (unique) | Shrine or ARMORY |
| uh_vault_module | Fracture Lattice and 1 Vaultbreaker Core | Signature module | Vaultbreaker (unique) | Refit free |
| uh_loadout_slot2 | Second loadout | Utility | What We Choose to Keep | |
| uh_regent_pinnacle | Mythic Ember Shell (power +3), the Mythic rifle Skyline Requiem (loot level +3), or the Kiln Crown; each with 1 Regent Heart | Pinnacle, Mythic | Kiln Regent (unique) | Shrine and ARMORY |
| uh_bond_awaken | One bonded companion to Awakened (final rank and evolution) | Signature bond | Kiln Regent (unique) | Ranks never drop |
| uh_frostglass_aspect | Frostglass | Aspect | Campaign data decides | Free switching |
| uh_volt_aspect | Volt Lattice | Aspect | Campaign data decides | Free switching |
| uh_void_aspect | Void Screen | Aspect | Campaign data decides | Free switching |
| uh_trim_second | Choose 1 of 2 trims: Jax's stripes, coupler glow | Cosmetic | Jax's Impossible Garage | |
| uh_handling_sidegrade | Choose canal grip or vent lift | Sidegrade, not more power | Jax's Impossible Garage | Swap at the bay |
| uh_chassis_cruiser | Cruiser hover bike chassis | Second vehicle class | Jax's Impossible Garage | Garage |
| uh_care_kit | 5 Coolant Salt, 2 Ash Resin, 2 Ember Crystal | Consumables | Mara's Quiet Work | Spent on bonds and brews |
| uh_ward_visual | Mender's Halo | Cosmetic | Mara's Quiet Work | |
| uh_map_knowledge | Survey notes on the pause map | Map knowledge | Iris's Missing Maps | |
| uh_elite_choice | Legendary build piece, Legendary Weapon Core (power +2) or a Legendary loot gun (loot level +2); each with 1 Pyre Seed-Core | High, Legendary | Iris's Missing Maps (elite site) | Shrine and ARMORY |
| uh_home_display | Lit trophy case at home | Home change | What We Choose to Keep | Fills as you win |
| uh_utility_recipe | Lantern Oil recipe, 1 Ember Crystal, 2 Coolant Salt | Utility | The Home We Keep | Rebrew from materials |
| uh_bond_storm_evolve | Stormskimmer evolution stage 1 | Cosmetic evolution | A Road for the Living | Never drops |
| uh_bond_ash_evolve | Ashshell Warden evolution stage 1 | Cosmetic evolution | Gardens That Burn | Never drops |
| uh_bond_drake_evolve | Quartz Drakeling evolution stage 1 | Cosmetic evolution | What We Choose to Keep | Never drops |

Typical worth (from the formulas): a Rare power 2 piece salvages for 152 Lumen, an Epic power 5 for 320, a Legendary
power 7 for 542, a Mythic power 8 for 818. A Rare gadget mod salvages for 115 Lumen. A Legendary gun sells for $1,600
at level 1 (+4% per level), a Mythic for $4,000. Boss materials sell for $1,500 (Matron Ember, Vaultbreaker Core) and
$2,000 (Regent Heart), so the premium underground drops are visibly worth more.

Trim ids for FUSION (`PersonalGarage.UnlockTrim`): `trim_ember_ribs`, `trim_quartz_crest`, `trim_canal_chrome`,
`trim_jax_stripes`, `trim_coupler_glow`, and the handling profiles `handling_canal_grip`, `handling_vent_lift`. Chassis
`FusionIds.Cruiser`, form `FusionIds.Ventwing`. BONDS: rank rewards call `RaiseRank` up to `BondIds.RankAwakened` and
check `RankOf`; evolution rewards set the evolution stage to the higher of the current and the reward value.

## 12. Economy sheet

Currencies stay separate: **cash** (vendors, Forge brew fees, material and gun sales), **Lumen** (shrine upgrades, from
kills, quests, discovery and gear or mod salvage), **materials** (the eight 04E materials plus the existing 30), and
unlocks (aspects, trims, bond ranks, recipes), which cost nothing and cannot be sold. No second currency was added.

### Required early costs against guaranteed early income (Act I and the start of Act II)

| Step | Required cost | Guaranteed income before or at that step |
|---|---|---|
| Choose a build and raise a ward | Nothing | Starter ward and one starter kit (2 pieces, Conduit also 4 Coolant Salt) |
| Fight through the service level | Nothing | Early gear choice: 1 Rare piece, 2 Ember Crystal, 2 Coolant Salt |
| The First Descent | Nothing | Descent piece (Rare or Epic), coupler key item, 2 Regulator Cog, 1 Ember Crystal |
| First fusion at Jax's bay | The coupler key item only (no material cost in this sandbox) | Fusion entitlement, Ventwing form, a trim |
| First Coolant Draught (optional) | $90, 3 Coolant Salt, 1 Ash Resin | 3 Coolant Salt with the recipe (4 more for Conduit); Ash Resin drops from Ash Burrowers (80%, 1 to 2) and Kiln Crabs (40%) |

The mandatory first fusion depends only on a guaranteed quest reward (the coupler from the descent). It never waits on a
rare drop. If FUSION adds a calibration material cost, the coupler reward's 2 Regulator Cog and 1 Ember Crystal are the
intended budget; Regulator Cogs also drop from Extraction Sentinels (70%, 1 to 2) and Repair Constructs (50%).

### Optional costs against repeatable content

- **Shrine upgrades** (existing rule, five steps, +12% affix values per step): cost
  `(60 + 90 x rarity) x (step) x (1 + 0.08 x power)` Lumen, rarity 0 for Common to 5 for Mythic.

  | Piece | Step 1 | Step 2 | Step 3 | Step 4 | Step 5 | All five |
  |---|---|---|---|---|---|---|
  | Uncommon, power 2 (kit) | 174 | 348 | 522 | 696 | 870 | 2,610 |
  | Rare, power 2 (kit, early choice) | 278 | 557 | 835 | 1,114 | 1,392 | 4,176 |
  | Rare, power 4 (descent) | 317 | 634 | 950 | 1,267 | 1,584 | 4,752 |
  | Epic, power 5 (Matron Shell) | 462 | 924 | 1,386 | 1,848 | 2,310 | 6,930 |
  | Legendary, power 7 (elite choice) | 655 | 1,310 | 1,966 | 2,621 | 3,276 | 9,828 |
  | Mythic, power 8 (Regent Shell) | 836 | 1,673 | 2,509 | 3,346 | 4,182 | 12,546 |

  The BUILD page shows the cheapest next step for worn gear with its real cost, or why it is blocked (not enough Lumen,
  already at step 5).
- **Brews**: Coolant Draught ($90 + 3 Coolant Salt + 1 Ash Resin) and Lantern Oil ($80 + 1 Ember Crystal + 2 Coolant
  Salt) are refilled from repeatable Underheart creatures: Coolant Salt from Furnace Casters (60%, 1 to 2) and Repair
  Constructs (30%), Ember Crystal from Ember Scavengers (45%), Glasswing Ambushers (50%, 1 to 2), Furnace Casters (40%)
  and a little from Cinder Moths, Ash Resin from Ash Burrowers and Kiln Crabs (drop rates as in today's
  `CreatureSpecies` entries, which ECOLOGY may retune). The care kit adds 5 Coolant Salt,
  2 Ash Resin and 2 Ember Crystal once.
- **Free by design**: switching aspects, reverting a suggestion, refitting the Fracture Lattice, switching loadouts.
- **Bond upgrades** and fusion costs are defined by the BONDS and FUSION streams with previewed costs from these same
  material ids.

## 13. Anti-duplication rules (all tested)

1. One entitlement per token; repeats are ignored and return the existing entry.
2. Offers are rebuilt only from stored values; no rerolls.
3. A claim is marked only after every part landed; any failure restores the pending entitlement.
4. Kits are once per suggestion per save (token `kit:<preset>`) and skip pieces already covered.
5. Suggestions move only owned items, only in key slots, never to a lower gear score.
6. Seeded reward gear uses its own random stream (`GearSystem.GenerateSeeded`), so it does not disturb drops and
   rebuilds the same item from the same seed.
7. Aspect unlocks and flags are idempotent; recipe grants check `IsGranted`; bond ranks and evolutions never go down.
8. Gear and gadget stacks have caps (move speed 12% and 1.35 total, parry bonus 0.12 s, ward power +60%, heat floor 25%).

## 14. Save data

- `SaveData.Builds` (`BuildSave`) appended `PreviousLoadout`, `KeyItems`, `Unlocks`, `FractureFocusUid` and
  `WardVisualOn` (true by default) after the frozen foundation fields.
- `RewardEntrySave` appended `Power` (1), `Level` (1), `Preset` ("") and `Delivered` ("").
- Both are in the `Builds04ETests` freeze lists, as is the appended `AffixType.MoveSpeed` value.
- Loading never re-announces the starter ward and never strips a slotted Buckler: the ward entitlement is restored
  before the gadget slots are checked.

## 15. Hooks outside the BUILDS files

- `Gameplay/ForgeDb.cs`: two appended brew recipes, `brew_coolant_draught` and `brew_lantern_oil` (the recipe rewards
  need real Forge recipes).
- `Characters/PlayerController.cs`: one line after the combat and travel speed scales,
  `if (Game.Gear != null) speed *= Game.Gear.MoveSpeedMultiplier;` (applies MoveSpeed and the Void Screen window on
  foot).

## 16. What a native Unity session must check

1. The ward panel at the bottom right (it sits at 340 px above the bottom in 1080p space): does it overlap the weapon,
   gadget or companion panels at 16:9 and ultrawide?
2. Raise the Buckler with each aspect: the plain ward, Ember Mantle plates and vents, Frostglass facets and icicles,
   the Volt grid and coils, the Void ring and orbiting fragments, plus the Kiln Crown and the Mender's Halo. Does the
   panel fade as capacity drops?
3. Stand in front of a rifle squad with the ward up: bullets should stop on the face and the bar should drop once per
   hit (not twice). Then a grenade beside you: the blocked share should depend on the angle.
4. A beam enemy (Prism style) on the ward: steady drain, then a break, then the RECOVERING timer.
5. Volt Lattice discharge and Void Screen dash: timing and readability.
6. Striders with MoveSpeed: does on-foot speed feel right, and do animations keep up at +12% and in the Void window?
7. Fracture Lattice on a focus: shards should jump only to visible nearby enemies.
8. BUILD page layout at 1920x1080 and at ultrawide; the schematic paper-doll; every tab with a controller.
9. Fitting emblem and ledger plinth at the Lofts desk (offset 3.4 m to the side) and, once VENUES and UNDERWORLD are
   merged, at the dispatch worktable and the refuge anchors: placement, collision, prompts.
10. Claim a gear reward with a full gear inventory: it must stay pending with a clear message, then claim after
    salvaging. Save, reload: the Buckler stays slotted, pending rewards keep the same offers, claimed ones stay claimed.

## 17. Honestly unfinished

- In this sandbox the garage and bonds are stubs, so trim, chassis, fusion, form and bond rewards stay **pending**
  (honestly, with a message) until the FUSION and BONDS streams are merged; their delivery code calls the frozen
  interfaces and checks the result.
- The dispatch and refuge stations appear only once VENUES (`fitting` anchor) and UNDERWORLD (`refuge.fitting`,
  `refuge.ledger`) provide their anchors. The home ledger at the Lofts works now.
- The BUILD page draws a schematic paper-doll, not a 3D preview of the character.
- CAMPAIGN and BOSSES must call `Grant` or `GrantAndClaim` with the tokens above; the ledger cannot know a quest or
  boss happened on its own (it does light trophies from `UnderBosses.EverDefeated`).
- Where the three optional aspects (Frostglass, Volt Lattice, Void Screen) are earned is campaign data.
