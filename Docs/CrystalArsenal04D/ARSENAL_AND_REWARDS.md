# 04D Arsenal and rewards

Status: **source-validated, native testing pending**. The code compiles in all twelve configurations and its pure rules
pass the offline EditMode tests. Nothing here has been run inside Unity yet: placement, visuals, input feel and the
reward flow need the in-editor checks at the end of this page.

## What the player gets

1. **The resonance case** stands beside the Veyra Lofts arrival (map letter **A**, gold, flashing while the starter is
   unclaimed). Press **E** (pad **Y**) at the case to open the offer screen.
2. **Compartment 1: Shardwake**, the starter crystal focus. The choices are explicit:
   - **Equip to slot N**: the first empty loot slot.
   - **Replace slot 1 to 4**: the slot's gun moves to the backpack. Refused (with the reason) when the backpack is full,
     so the replaced gun would have nowhere to go, and refused for a favourite.
   - **Send to backpack**.
   - **Not now**: nothing changes; the offer stays in the case.
   - When every loot slot and the backpack are full, opening the case turns the starter into one **pending claim** so it
     can never be lost.
3. **The practice lane** near the case: three crystal target posts in front of a real backstop wall. While you stand in
   the lane holding a crystal focus, shots are not reported as crimes (`CrystalPractice.SuppressNoise`). Targets flash
   and count hits and award nothing (no loot, kills, faction, Lumen, energy or heat refill: they are not characters).
   Hitting all three with any crystal focus completes **calibration** and opens **compartment 2: Prism Thread** (same
   offer screen). A panel under the crosshair explains leading shots, beam heat and the mark-to-burst combo.
4. **The contract board** (last row of the case screen):
   - **Resonance Breach**: START asks the encounters system (`ContractAvailable`, `StartContract`) and shows its status
     line and refusal reason exactly as it reports them. First victory grants **Gyre Crescent** through
     `GrantReward("contract:resonance_breach", 19, "Resonance Breach")`.
   - **Track the Foreman**: reveals its quarry arena and sets a GPS waypoint. One line explains that the Djinn needs
     night and the Queen needs the Veil, and the three signature states are listed.
5. **Boss signatures**: the first real victory over the Foreman, Djinn and Queen grants Sunderheart, Mirage Choir and
   Mirrorwake (token `boss:<bossId>`). After the fight clears (no boss, no summons left, a 2.5 s beat after the banner)
   a non-pausing card shows what the focus does and where it went, with **Press E (pad Y) to equip or compare it now**
   when nothing else would take that press; otherwise it points to ARSENAL. A matching focus trophy hovers where the boss
   fell (pressing E there opens the same screen). The trophy is a celebration only: the record is the entitlement.
6. **Retroactive claims**: loading a save whose boss defeat ledger already lists one of the three bosses gives one
   pending claim marked "earlier victory" per boss without a record. The defeat ledger is never touched.

## Where rewards wait

A reward that cannot go into the backpack is a **pending claim**. It is shown, with claim buttons, in:

- **ARSENAL** (pause menu): the "CRYSTAL CLAIMS" strip above the card ("Reward waiting: free a slot or collect at the
  case or the Forge"). VIEW shows the focus card and the same choices as the case.
- **The resonance case**: one row per pending reward.
- **The Forge ARMORY tab**: "CRYSTAL REWARDS WAITING" with COLLECT (backpack, else a free loot slot).

A pending claim never expires, is never dropped on the ground, and never replaces equipment. Open offers (the starter,
Prism Thread) are taken at the case; if the case could not be placed in this city they are taken in ARSENAL instead, and
if the lane could not be placed Prism Thread is offered without calibration.

## Durable entitlements

One record per token in `SaveData.CrystalArsenal.Entitlements` (`CrystalEntitlementSave`):
`Token, Unique, Level, Seed, Status (0 Pending, 1 Granted), Uid, Source, Retro`.

- The **seed** is a hash of the token, so a pending reward always rebuilds the same gun; the **level** is resolved when
  the record is created (boss rewards use the ordinary boss drop level, the others the normal level).
- A grant creates the record, then inserts the gun into the backpack only if there is room and records its uid in the
  same frame, so any save snapshot holds both or neither.
- A token is granted once per save. Replaying a boss (including the debug re-fight) or a duplicate death callback finds
  the record and grants nothing.
- A Granted record stays Granted forever: selling or scrapping the focus never re-mints it.
- **Reconcile on restore** (the first frame after a load, after the armory and the boss ledger are restored): empty
  records are dropped, duplicate tokens keep the strongest record, a known token's unique is repaired, a Granted record
  without a uid becomes Pending (nothing proves the insertion), a Pending record whose uid is an owned gun of the same
  unique is promoted, any other stale pending uid is cleared. A retro claim adopts an unowned copy already in the armory
  (for example a debug copy) instead of minting a second one.
- Durability is as good as the save that follows: the record is in memory until a save succeeds. The save writer's
  verified transaction and `SaveSystem.LastError` report failures; a deliberately reloaded older save can revert a grant
  (this is single-player, there is no rollback protection and none is claimed).

## Item cards and the Forge

- Crystal focus cards (ARSENAL, the case, the Forge preview) show: sustained damage per second over a full heat cycle
  (`CrystalFocusDb.SustainedDps`, compared on the same basis for any gun), the hit pattern or the beam's damage **per
  second** (never the 0.1 s tick), fire rate (not for the beam), shots or beam seconds before an overheat, vent time,
  range, how it fires, the heat line, its limitation and its source ("Foreman signature", "Resonance case", or
  "unrecorded" for a debug copy).
- Selling (gun shop) or scrapping (ARSENAL, Forge) any crystal focus asks twice and says "Granted once: it will not be
  granted again."
- Forge: infuse (element), raise level and ascend keep the behavior (it comes from the unique index). A focus takes no
  traits, so ascension raises its power only, and the screen says so before payment. Any result that would change the
  behavior or carry a trait is refused before payment (`CrystalForgeRules.KeepsBehavior`).
- The F2 Loot page's "every unique" button now spawns only the 17 legacy uniques; foci come from F2 > Crystal arsenal.

## Placement audit (logged)

Done once per boot, the first time the player is within 95 m of the Veyra Lofts arrival in play (so streamed props and
parked cars exist). Case candidates go along the Lofts frontage, 4.5 to 16 m beside the door and 1.4 to 3 m off the
facade, nearest first; then rings around the arrival. Each candidate must: stay 4 m from the Lofts door and 3.5 m from
every other door, keep clear of every service prompt, sit off roads and outside buildings, stand on level ground near the
arrival's height, stay 1.3 m from pedestrian paths, be clear in a physics sphere against World, Debris and Vehicles, and
be visible from the arrival. The lane footprint (about 12 by 7 m) is checked the same way over its whole area, plus a
physics box, level ground under every corner, target and the wall, no pedestrian node inside and no path crossing it. The
chosen spots (or the reason for a failure) are written to the log and shown on the F2 page.

## Debug: F2 > Crystal arsenal (Order 53)

Grant each focus through the real path (debug tokens), fill the backpack, fill the empty loot slots, reset the offers
and calibration, complete calibration, force a retro claim per boss, remove boss records (the defeat ledger and guns stay),
run reconcile now, GPS to the case, open the case screen, queue a test reward card, and the record list with each uid's
presence in the armory.

## How to test in Unity

Use a test save slot. Keys: **E** interact (pad **Y**), **Esc** or **Backspace** closes the case screen (pad **B** or
**Start**), mouse or D-pad and **Enter** (pad **A**) on its buttons.

1. Free roam: the hint names the case; walk to the gold A, press E, equip Shardwake to a free slot. Check the case's
   first display focus disappears and the marker stops flashing.
2. Stand on the lane's blue mark, hit all three posts with Shardwake. Check the lane panel, flashes, the gold ring per
   calibrated post, no police reaction, then CALIBRATION COMPLETE and compartment 2.
3. F2 > Crystal arsenal: Fill empty loot slots, Fill the backpack, Reset offers, then open the case: Shardwake becomes a
   pending claim; free one backpack space in ARSENAL and claim it from the CRYSTAL CLAIMS strip.
4. Contract board: START Resonance Breach (needs the encounters pass), win it, see the reward card, save, reload.
5. Track the Foreman, defeat it: the card appears after the fight clears, the trophy hovers where it fell; with a full
   backpack the card says "Reward waiting"; save and reload, then claim it in ARSENAL, at the case and at the Forge.
6. Re-fight the Foreman from F2 > Creatures: no second Sunderheart.
7. Load a pre-04D save that already beat a boss: one retroactive claim per beaten boss.
8. Forge ARMORY: infuse, raise level and ascend a focus; check the notes, that it still fires its own way afterwards, and
   that SCRAP asks twice. Sell one at a gun shop: it asks twice.

Not run here: placement on the real city geometry, visuals of the case and lane (art pass bodies), every input path in
the editor, frame cost. The log line "Crystal arsenal: Resonance case at ..." reports where the audit put things.
