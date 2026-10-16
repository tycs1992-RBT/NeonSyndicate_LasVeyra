# Start here: Crystal Arsenal 04D

**Status: source-validated, native testing pending.** The code compiles offline in all twelve configurations and
730 offline tests pass, but nothing in this update has been run inside Unity yet. Treat the first session as a
playtest of a candidate, not a finished release.

Branch: `feature/crystal-arsenal-04d` (not merged into main). Unity 6000.3.16f1, same project, same saves.

## What changed for the player

- **Six crystal weapons ("foci") that are not guns.** Shardwake fires traveling crystal shards and a charged
  three-shard fan. Prism Thread is a real continuous beam. Gyre Crescent throws a disc that banks off walls and comes
  back (press fire again to recall it). Sunderheart launches a slow gravity orb that pulls small foes in, then ruptures.
  Mirage Choir locks up to three foes while you aim, then fires guided shards. Mirrorwake charges a precision pulse,
  can plant a mirror focus on a wall, and bends the next pulse off it to a foe.
- **A way to get them in normal play.** A glowing resonance case stands beside Veyra Lofts (gold marker on the map).
  It offers Shardwake, a three-target practice lane unlocks Prism Thread, and its contract board starts a short fight
  and points you at the Foreman.
- **Fights that end.** The Resonance Breach is a finite skirmish (four foes with readable roles, one announced
  reinforcement). When you win, the area stays quiet for two minutes of play and until you leave.
- **Bosses that teach and pay.** The Foreman, the Djinn and the Queen have new attack patterns built around their
  weapon's idea, with clear warnings and openings. Your first win against each one guarantees its signature weapon:
  Sunderheart, Mirage Choir and Mirrorwake. If your backpack is full, the reward waits as a claim in ARSENAL, at the
  case or at the Forge. It is never dropped and never lost.
- **Safer saves.** A save is written to a temporary file, checked, then swapped in with the previous save kept as a
  backup. A broken save loads its backup automatically.

The disc weapon is called Gyre Crescent. The planned name was blocked by the project's commercial-name filter.

## One short play route (Free Story)

1. Title screen: **Free Roam (skip story)**. You arrive outside Veyra Lofts.
2. Follow the gold **A** on the map to the resonance case beside the Lofts. Press **E** (pad **Y**) and equip
   Shardwake into a free slot (or send it to the backpack and equip it from ARSENAL).
3. Stand on the blue mark in the practice lane and hit all three crystal posts with shards. The case's second
   compartment opens: claim Prism Thread the same way. Try holding fire for the beam.
4. Open the case again, choose the contract board and press **START** on Resonance Breach. Follow the marker (about
   220 m away in the default city), win the fight, and stay a moment to feel the quiet afterward. First win: Gyre
   Crescent.
5. On the board, press **TRACK** for the Foreman. Win at the quarry. The reward card offers Sunderheart: press **E**
   (pad **Y**) or equip it from ARSENAL, then fire an orb into a group of foes.
6. Save in a slot, load it, and check Sunderheart is still in your armory and still fires orbs.

Optional: the Djinn only appears at night on the Mirage Flats; the Queen only inside the Veil at Crystal Manor.

## Controls

The foci use your existing fire, aim and reload buttons. Nothing new to learn on the pad. Full table:
`Docs/CONTROLS.md` (section "Crystal foci (04D)").

## What to look at first (needs your eyes in Unity)

- Weapon look in the hand and in first person, muzzle and hand alignment, and beam readability in daylight.
- Whether the case, the practice lane and the skirmish site landed in sensible places (the log prints
  "Crystal arsenal: Resonance case at ..." and an `ENC04D` line for the skirmish site).
- Boss telegraph timing and how fair the fights feel on foot and in flight.
- Frame rate in a busy fight with several foci.

## Where the details live

- `Docs/CrystalArsenal04D/CHECKPOINT.md`: exact status of every weapon, boss and system.
- `RESUME_NEXT.md`: the continuation state for the next development session.
- `Docs/CrystalArsenal04D/WEAPONS.md`, `ARSENAL_AND_REWARDS.md`, `ENCOUNTERS.md`, `BOSSES.md`, `ART_AND_AUDIO.md`:
  per-system design, numbers and Unity test steps.
- `Docs/CrystalArsenal04D/FEATURE_STATUS.json`, `TEST_LEDGER.json`, `DECISION_LOG.md`, `KNOWN_ISSUES.md`.
