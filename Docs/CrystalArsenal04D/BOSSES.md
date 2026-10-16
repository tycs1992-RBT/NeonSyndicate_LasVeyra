# 04D bosses: the Foreman, the Djinn and the Queen

Status: implemented in source, compiled offline in the twelve configurations, pure rules covered by EditMode tests.
**Native Unity play: NOT RUN.** Nothing on this page has been seen on screen yet; timings, readability and feel must be
checked in Unity (see "How to test it in Unity" below).

## What changed and what did not

- The Shard Golem Foreman, the Mirage Djinn and the Mirror Wraith Queen now run an upgraded fight inside the existing
  boss owner (`CreatureBoss`, one boss, one fight owner). Each fight teaches the verb of the crystal focus its first win
  grants: the Foreman's gravity orb teaches Sunderheart, the Djinn's guided shard choir teaches the Mirage Choir, the
  Queen's reflected beams teach Mirrorwake (`BossProfile04D.SignatureUnique` equals `CrystalFocusDb.SignatureFor`).
- Unchanged on purpose: the Glacier Titan (it has no 04D profile and runs the original script line for line), every
  arena rule in `CreatureDirector.Bosses.cs` (finding arenas, mission and event quiet, stepping out and back in, the
  Djinn's night rule, the Queen's Veil rule, leaving 75 m past the edge, the defeat record, the gear drop and the 04D
  reward hand-off in `OnBossDied`), `SpawnBoss` for quests (a quest Foreman, Djinn or Queen gets the upgraded fight), and
  the debug "fight a boss" buttons.
- The original Foreman, Djinn and Queen moves are kept: they are the fillers between signature moves and the fallback
  if a profile were ever missing.

## The shared beat

Every signature move runs the same readable beat (`CreatureBoss.Fight04D.cs`):

1. **Telegraph**: the HUD names the move and its counter above the boss bar and fills an orange bar; an orange "!!"
   pulses over the boss; the body glows; the ground (or the air) shows where it lands. A stagger in this window breaks
   the move (its telegraph vanishes harmlessly, "Interrupted: ... broken").
2. **Commit**: the aim is fixed from here on. Nothing re-aims at a moving player after this point.
3. **Active**: projectiles fly, rings and lanes burst.
4. **Recovery window**: the boss stands still and open, takes `CoreBonus` times the damage (Foreman 1.6, Djinn 1.35,
   Queen 1.5), the boss bar says so, a gold bar shrinks above it and a ring marks the core on screen.

The signature comes at least every other attack; fillers are the original ring moves. Between moves there is a pause
of 2.4 s in phase one, shorter later (`BossTiming04D.MoveGap`, never under 1.2 s).

### Timings per difficulty (seconds, from `BossTiming04D`)

| Move | Story wind-up | Balanced | Veteran | Story window | Balanced | Veteran |
|---|---|---|---|---|---|---|
| Foreman gravity orb | 1.43 | 1.10 | 0.94 | 2.00 | 1.60 | 1.28 |
| Djinn shard choir | 1.30 | 1.00 | 0.85 | 1.63 | 1.30 | 1.04 |
| Djinn lane shift | 1.04 | 0.80 | 0.70 | none | none | none |
| Queen reflected beams | 1.50 (+0.6 aim) | 1.15 (+0.5 aim) | 0.98 (+0.4 aim) | 2.13 | 1.70 | 1.36 |

Balanced and Veteran wind-ups stay in 0.7 to 1.2 s and windows in 1 to 2 s; Story is longer on both (tests enforce it).
The original filler moves keep their original timings (they scale with `CombatDirector.EnemyWindupScale`).

## Shard Golem Foreman (quarry floor)

- **Gravity orb (signature).** A dense violet orb gathers over its raised hands for the wind-up, then is thrown slowly
  at where the player stood (at the player in the air when they fly), stopped short of the first wall on the way. A
  rupture ring (3.5 m) and rupture lanes appear on the floor at once and fill until the rupture, which is never sooner
  than the wind-up. The orb hangs, pulls once (a bounded tug of about half a metre through the player's own movement:
  nothing through a wall, nothing on a driver, nothing outside 6 m), then ruptures. In the air a filling disc marks the
  rupture and it bursts as a 3.5 m sphere that needs a clear line.
- **Lanes.** While the hazard plates hold: three lanes (four on Veteran) radiate 12 m from the orb. Once the plates
  break: three lanes fan 28 degrees apart straight from the Foreman's body through the target ("its ruptures now cut
  straight from its body"). Every layout is checked for a reachable safe spot (off the ring and every lane, within
  4.5 m of the player) and turned, then thinned, until one exists (`BossLanes04D.FindSafeSpot`).
- **Exposed core.** After each discharge the core is open for the window above.
- **Plates.** Unchanged rule: hazard plates halve damage until its poise breaks once; Volt powers still stun the crown
  (a stun breaks a gathering orb). At one third health the plates shatter for good.
- **One support arrival.** At two thirds health it reserves its one batch and marks up to two rings; two quarry golems
  (Shard Golems, 70 percent health, 80 percent damage) claw up when the rings burst. Nothing else ever arrives (the old
  per-phase Glass Mite swarms are gone from the upgraded fight).
- **Fillers.** Crystal rain, the widening ring slam at close range, and the arcing shard throw against a flyer.

## Mirage Djinn (Mirage Flats, night only)

- **Shard choir (signature).** Four, five or six glowing shards (Story, Balanced, Veteran) gather in a fan round the
  real body: the real casting tell. Meanwhile two harmless shimmering sand pillars copy the tell elsewhere (decoys are
  never ground rings, because rings always hurt). At the commit the shards leave 0.12 s apart, fanned round the line to
  the player, at 14 m/s. Each steers toward the player at 45, 65 or 85 degrees per second, only for its first 0.7, 0.9
  or 1.1 s, and only while it can see the player: one break of sight ends its steering for good, and it always bursts
  on walls and cars. At most 1, 2 or 3 shards of a volley may hurt. Crossing the volley's lane or breaking sight beats
  it; then the Djinn is spent for the window.
- **Lane shifts.** Every few moves, and at two thirds health, it shimmers at a legal new spot (inside 85 percent of the
  arena, 10 to 28 m from the player, at least 8 m from where it stood, on ground at about its height, room for its
  body, a clear line to the player, not inside a building) and steps there.
- **Illusion phase (once, bounded).** At one third health its one batch becomes at most two mirages wearing its face
  for 16, 13 or 11 s. They cast no shadow, glow with the real tell, never strike and pop at a touch. The real Djinn
  shifts lanes among them. A mirage's death is never the boss's: only the registered body reaches the director's
  defeat record (unchanged `OnBossDied` and mirage rules).
- **Filler.** The original sandstorm rings.

## Mirror Wraith Queen (Crystal Manor, inside the Veil)

- **Focus points.** She raises three mirror focus points (four on Veteran) round the hall, 3 m up, in open air and in
  her sight: glass facets round a bright prism with their own health (4 percent of hers, at least 90). Each is a
  shootable sphere on the gadget shield layer: shots and blasts stop on it, bodies and cars pass. The HUD marks each
  with a ring, "FOCUS" and a health bar.
- **Reflected beams (signature).** One beam in phase one, two after, each off an intact focus she can see (straight at
  the player when none stands). For a short aim window the faint lines follow the player at a limited turn rate, then
  they lock: lanes mark both legs on the floor and fill over the wind-up, and the beam fires exactly along the locked
  lines (each leg stopped by the first wall, reaching far enough for a flyer). Shattering a focus breaks the beam through
  it; breaking every beam of a pattern makes it fizzle and opens her core at once. A second beam is dropped if together
  they would leave no reachable safe spot.
- **Core opening.** After every committed pattern her core opens for the window, and her own glass guard is down.
- **One mirror change.** At two thirds health the old focus points shatter and a new set rises, turned half a step.
- **Copies.** At one third health her one batch is at most two mirror copies (the original copies that share her guard).
- **Filler.** The original glass shatter rings. The Veil rule is the director's and unchanged: leaving the Veil ends the
  fight.

## Fairness rules shared by all three

- Phases open at two thirds and one third health, each exactly once (`BossPhaseLatch04D`): a burst across both enters
  phase three directly and skips phase two's event explicitly (logged); pause, simultaneous collider hits and healing
  cannot reopen a phase.
- Supports: at most two alive at once and one finite batch, reserved before anything is scheduled
  (`BossAddBudget04D`). A support's death never opens a new batch.
- No beam or orb is re-aimed after its commit. Shards turn at a limited rate for a short window and only with sight.
- Every boss can reach a flying player (orb, choir, beams, the shard throw) and nothing disables flight. Ground rings
  only reach 3 m up, so a player above 3.2 m always faces an air-capable move.
- Every boss hit on the player is capped like any creature hit (`CreatureBrainLogic.CapHit`) and shares the ring
  respite (0.6 s after a hit, further bursts pass harmlessly), so nothing chains into a stagger lock.
- Damage taken is `BossProfiles04D.DamageTakenScale`: the Foreman's plates halve hits until broken (then 1.15, as
  before), and the window multiplies by `CoreBonus`.

## Lifecycle and cleanup

Everything a fight owns is counted in `BossOwnedLedger04D` and released on every way out through one cleanup
(`CreatureBoss.Cleanup04D`), which logs one line: the reason, and how many rings, lanes, beams, shards, decoys, orbs,
focus points and supports it released, with the phases entered and skipped and the supports admitted.

| Way out | How it reaches the cleanup | Reason logged | Victory? |
|---|---|---|---|
| Boss dies | the boss script sees the dead body | BossDied | only this, and only if the director sees the player's blow |
| A support dies | the support list prunes it (not a cleanup); its ledger entry is released; no new batch | n/a | no |
| Player leaves the arena | director `EndFight`, the boss body is destroyed | PlayerLeft | no |
| Veil closes (Queen) | director `EndFight` | VeilExit | no |
| Player dies or is busted | director `EndFight` | PlayerDied | no |
| Title screen | world reset destroys the boss; the player is gone | Title | no |
| Load or new game | world reset destroys the boss; the player is placed again | Load | no |
| Anything else (quest despawn, debug) | the boss body is destroyed | Removed | no |

After the cleanup the budget and ledger are closed, so a golem arrival ring still filling when the boss died or the
player left can never produce a golem. Supports and mirages the fight raised are removed with it. The defeat record and
the reward stay with the director (`OnBossDied`), which only a real player kill reaches.

## Debug (F2, tab "Creatures")

While an upgraded boss is being fought, under "End boss fight":

- **Boss: burst past a phase**: sets the boss's health below one third in one step (never lethal, never the player's
  hit, so it can never grant a reward). Watch the log: one phase entered, one skipped.
- **Boss: open its window**: opens the recovery window now.
- **Boss: signature move now**: starts the signature move.
- A status line: phase (entered, skipped), the beat and move, supports alive / budget / batches left / admitted, live
  lanes, shards and focus points, the illusion, and whether the player counts as airborne.

## How to test it in Unity

1. Play, press F2 (the debug menu is keyboard only; its buttons also take the mouse), open "Creatures", press "Shard Golem Foreman" under
   "Fight a boss in its arena". Watch the orb gather, the ring and lanes fill, the pull, the rupture, then the
   "CORE EXPOSED" window. Shoot the chest core in the window. Break the plates (stagger it) and check the lanes now fan
   from its body. At two thirds health two golems should rise from marked rings, once.
2. Fight it once on foot and once flying (Flight: double-tap Jump in the air, pad A; F2 > Travel unlocks it): in the air the orb comes at you with a
   filling disc and the shard throw arcs up; neither disables flight.
3. Press "Boss: burst past a phase" at full health: the log shows one phase entered and one skipped, no second batch.
4. Mirage Djinn button (it sets the night): look for the shard fan on the real body and the two shimmering pillars.
   Strafe across the volley or put a rock between you and it: the shards stop steering. At one third health check the
   two mirages (no shadow) and that killing a mirage does not end the fight.
5. Mirror Wraith Queen button (it opens the Veil): find the "FOCUS" markers, shoot one during a pattern and see that
   beam break; let a pattern fire and see the core open. At two thirds health the mirrors turn once.
6. Lifecycle: leave the arena mid-fight, die mid-fight, close the Veil mid-Queen, go to the title and load a save mid
   fight. Each time the log's "Boss 04D: ... cleanup (Reason)" line should appear once and no lanes, shards, orbs,
   pillars or focus points should stay behind.

Keys and pad: no new input. Fight with the existing weapons, Flight and dodge on keyboard or pad as usual; F2 is the
existing debug menu (keyboard).

## Files

| File | What |
|---|---|
| `Creatures/BossProfiles04D.cs` | Pure rules: profiles, move chooser, timings, phase latch, support budget, ledger, lanes and safe spots, shard steering, the Queen's aim lock, pull, cleanup classification; the reset hub |
| `Creatures/CreatureBoss.cs` | The original boss script (now partial), with the hooks into the 04D fight; the Titan path unchanged |
| `Creatures/CreatureBoss.Fight04D.cs` | The shared beat, phases, damage scale, supports, the HUD, cleanup, debug |
| `Creatures/CreatureBoss.Foreman04D.cs` | Gravity orb, rupture lanes, plate pattern change, the golem arrival |
| `Creatures/CreatureBoss.Djinn04D.cs` | Shard choir, decoy pillars, lane shifts, the illusion phase |
| `Creatures/CreatureBoss.Queen04D.cs` | Focus points, reflected beams, the mirror change, the copies |
| `Creatures/CreatureBoss.Lanes04D.cs` | Pooled lane, beam and shard visuals (one shared additive material, property block tints) |
| `Creatures/CreatureDirector.Debug.cs` | The three boss debug buttons and the status line |
| `Tests/EditMode/BossProfiles04DTests.cs` | 39 tests of the pure rules |

## Known limits

- Not run in Unity: telegraph readability, lane visuals on slopes, the orb's look, shard tracking feel, focus placement
  in the real Crystal Manor hall and the support golems' arrival hitch are unverified.
- The quarry golems are not ordered ahead on a worker thread (the prewarm list lives in `CreatureDirector.Bosses.cs`,
  which this pass may not touch), so their two bodies build on the main thread when they arrive. Adding
  `OrderCopies("shard_golem", 0, 2);` to the Foreman's `PrewarmAdds` case would remove that hitch.
- The window bonus applies to every hit on the boss (the core still crits on top); it is not limited to core hits.
- Focus points rely on the gadget shield layer colliding with nothing; if the gadget system failed to boot, a focus
  sphere 3 m up could bump a flying player.
