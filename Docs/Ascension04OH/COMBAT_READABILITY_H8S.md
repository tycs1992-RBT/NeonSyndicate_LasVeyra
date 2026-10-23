# Combat readability H8S

Builder: combat (branch `h8s/combat`). Scope: step 9 of `BUILD_TAKEOVER_H8S.md` ("readable creature tells and finite
encounter pacing", focus readability, the boss audit) and the owner's wish that combat read like a premium fantasy
action RPG. Every change below extends an existing owner through a hook or a partial file. Damage values, rewards,
cooldowns, ranges, phases, budgets and saves are unchanged (no `SaveData` field, no new input, no Settings field).

Status: compiled and tested with the offline reference harness (Unity's managed reference assemblies), not native
Unity. Looks, sound, feel, frame rate and real input are NOT RUN; the checklist at the end says exactly what to try.

## 1. One tell language and honest ground marks

**Friction found in the code**

- The HUD glyphs (creatures, the 04D boss HUD, the Underheart boss HUD and the docs) teach red "!" = guard or parry
  it and orange "!!" = cannot be blocked. The creature ground rings used the same two colours the other way round
  (blockable rings orange, unblockable rings red, golem slams red).
- The ring texture (`UIKit.Ring`) draws its outline at 0.85 of the quad's half size (visible edge about 0.95), but
  the hit tests add a margin beyond the radius they were given: creature rings +0.6 m (`Creature.InRing`), boss rings
  +0.3 m (`CreatureBoss.BurstDamage`), golem slams +0.4 m (`CreatureAbilityKit.Shockwave`). A player standing visibly
  outside a 3 m creature ring by a few decimetres was still hit (the drawn edge sat about 0.75 m inside the real one).
- All-round sweeps (the basilisk's tail, the wyvern's blade tail, the drake's tail crush, the Dune Wyrm's 7 m tail
  lash, the burrower's flail) had no ground mark at all, only the "!".
- Pounces and ground charges re-aimed at the target at the moment of launch, so a sidestep during the wind-up never
  worked; only a dodge roll did.
- Humanoid creature swings (Hollows, wraiths, golem fists, boss melee) showed a "!" with a fixed fill and never "!!";
  people swinging at the player had no warning unless they were the lock-on target.

**What changed**

- `Combat/CombatTellsH8S.cs` (engine-free): the palette (red guard, orange dodge, a hotter orange for the flash before
  an unblockable blow), the ground mark per body shape (ring for areas and all-round sweeps, lane for pounces and
  ground charges, nothing for frontal strikes, breath, shots and air dives), class wind-up floors by difficulty, an
  escape-time rule for areas, true-reach geometry, the lane geometry and the aim lock.
- Wind-up floors (seconds, Story / Balanced / Veteran): frontal strikes and shots 0.42 / 0.36 / 0.32; knockdowns and
  unblockable 0.75 / 0.62 / 0.55; pounces, charges, dives and breath 0.80 / 0.65 / 0.55. Veteran keeps the original
  0.32 and 0.55 exactly. An area blow also never fills faster than reaction (0.45 / 0.35 / 0.28) plus the time to roll
  out of it, counting at most one full dodge roll (4.3 m in 0.64 s): the roll is the answer to an area blow (its
  invulnerable frames carry the player through a deeper one), so a wide area never asks for a long jog. The original
  wind-up is never shortened; active windows and recoveries are untouched. Measured over every shipped attack at body
  heights 0.4 to 5 m: only a few all-round sweeps and small rings get longer, on Balanced and Veteran, by 0.02 to
  0.17 s (the wyvern's blade tail the most, then the drake's tail crush, the glasswing's wing buffet, the furnace
  caster's vent burst and the basilisk's tail sweep), plus the Glass Mite nibble by up to 0.03 s on Balanced;
  everything else, the Dune Wyrm's tail lash included, keeps its timing.
- `Creatures/CreatureTelegraphs.cs` (`GroundRing.ShowHit`, `Follow`): a ring whose edge is drawn exactly at the hit
  radius, with an edge colour (the tell) separate from the fill colour (a theme). `GroundRing.Show` is unchanged for
  every other caller.
- `Creatures/CreatureTelegraphs.LaneH8S.cs` (`GroundLaneH8S`): the creature lane, in the same visual language as the
  04D boss floor lanes (dim strip, bright bar growing over the wind-up, flash at launch) plus edges in the tell
  colour, as wide as the blow's real contact reach, tilted to the slope, pooled, one shared material.
- `Creatures/Creature.TellsH8S.cs` and hooks in `Creature.Attacks.cs`: the timing through
  `CreatureAttackDb.TimingH8S`, rings at true reach in the corrected colours, a ring that follows the body for an
  all-round sweep, a lane for a pounce or ground charge that follows the aim until 70 percent of the wind-up and then
  locks; the commit uses the locked direction (`LockedTravelAimH8S`). Lanes and sweep rings appear only for blows aimed
  at the player (a creature's blow hits only its own target). The orange flash replaces the red one before an
  unblockable blow.
- `Creatures/CreatureAbilities.TellsH8S.cs`: the golem slam ring is orange with its edge at the shockwave's reach and
  its wind-up always leaves time to react and roll out of the ring (the Glacier Titan keeps its original slam timing).
- HUD (`UI/HudCreatures.cs`, `UI/HudCombat.cs`, `AI/NpcController.TellsH8S.cs`): one palette for every glyph, the real
  wind-up progress and severity for humanoid creature swings, the lock-on glyph covers kit slams, and people winding
  up a melee blow on the player within 25 m get the same "!" or "!!".

**Tests**: `CombatTellsH8STests` (OneColourAndMarkPerAnswer, ShapesPickTheRightGroundMark,
TheShippedTablesGetTheMarksTheyNeed, VeteranKeepsTheOriginalFloorsAndEasierDifficultiesGetMore,
TellTimingNeverShortensAndOnlyTouchesTheWindup, EveryAreaBlowLeavesTimeToReactAndRoll, AreasCountAtMostOneFullRoll,
ShippedBigSweepsKeepTheirTimingAndAFastWideSweepIsRaised, EscapeTimeIsOneDodgeThenAJog, MarksAreDrawnAtTheTrueReach,
LanesShowTheRealPath, TheAimLocksBeforeLaunchAndOnlySnapsWithinFiftyDegrees,
OnlyBlowsThatLandAtOneMomentCountForTheCadence).

## 2. Fair pressure: the impact cadence

**Friction found**: the attack tokens already limit how many enemies swing at once (1, 2 or 3 melee by difficulty,
shared with creatures), but nothing spaced out when their blows land. Two token holders starting in the same frame
landed in the same instant behind one "!", and a creature, a humanoid creature and a person could all land together.

**What changed**

- `Combat/ImpactCadenceH8S.cs` (engine-free): a fixed-capacity book of planned landing moments; two blows on the
  player never land closer than 0.55 s (Story), 0.35 s (Balanced) or 0.22 s (Veteran).
- `Combat/CombatCadenceH8S.cs` (runtime, reset from `CombatDirector.ResetStatics`): creatures plan their landing moment
  when a blow starts and give it back when the blow is broken; swings of people and humanoid creatures are read live
  from their wind-ups (`NpcController.MeleeImpactInH8S`), so a staggered swing frees its moment at once.
- Hooks: `Creature.Brain.TryAttack` (before any token is taken), `NpcController.Creature.UpdateCreatureFight`
  (humanoid creatures and boss melee), the ambient golem slam and the 04D encounter warden slam. A blow that would land
  inside the gap waits 0.15 s while its attacker keeps circling or holding its ground. Swarm nibbles, breath, shots,
  calls and scripted boss moves are not held back. Finite encounters stay finite: nothing new is spawned, LocalQuiet,
  maxAlive and the encounter budgets are untouched.

**Tests**: `CombatPacingH8STests` (GapsShrinkWithDifficultyButNeverVanish, TwoBlowsCannotLandInsideTheGap,
ReplanningReleaseAndForgetting, NextFitFindsTheFirstFreeMoment, ABrokenTimeOrAFullBookNeverBreaksTheRule,
LiveSwingsCountToo).

## 3. Hit feedback

**Friction found**: player melee had a hit-stop, but gun, focus, power and blast hits had none, not even on kills; a
kill was confirmed only by a lower-pitched beep; and the damage numbers built a new string for every number on every
frame in OnGUI.

**What changed**

- `Combat/HitWeightRulesH8S.cs` (engine-free) and `Combat/CombatFeedbackH8S.cs`: kills and genuinely heavy single hits
  (a quarter of the target's health, or a critical taking an eighth) from guns, foci, powers and blasts get a short
  impact freeze through `CombatDirector.HitStop`, the existing `GameTime` request owner (0.03 s for a kill, 0.06 s for
  an elite kill, 0.04 s for a heavy hit, 0.025 s for a big critical; at most one every 0.25 s; reduced effects keep 35
  percent like the melee freeze; never `Time.timeScale` directly). Melee and Resonance Bursts keep their own freezes;
  boss and great beast kills keep their death sequences; companion and fused-vehicle hits never freeze.
- A synthesized kill confirmation (a soft thump, a glass crack and a bright crystal chord; a heavier version for
  elites, champions and Shard-Touched creatures) that climbs two semitones per kill in a quick streak. Only the
  player's foes get weight and confirmation (`FoeOfPlayerH8S`, still readable after death), never civilians.
- Damage numbers (`Combat/DamageNumbers.cs`, `Combat/DamageNumberRules.cs`): the killing blow's number pops bigger and
  stays up longer; the text is cached in the book and rebuilt only when the rounded amount changes. The damage
  number setting still turns numbers off (the feedback above runs either way).

**Tests**: `CombatFeedbackH8STests` (MeleeAndBurstsKeepTheirOwnFreeze, OnlyKillsAndHeavyHitsFreezeAndNeverTooOften,
KillTiersAndStreaks, TheKillingBlowStandsOut, NumberTextIsCachedNotRebuiltEveryFrame,
PoiseSaysWhenOneMoreHitStaggersAndHowLongTheOpeningLasts, NewCuesAreCleanSynthesizedClips).

## 4. Boss readability

**Friction found**: the regional guardians already held their swings for 2.5 s and cleared their marks at a phase
change, but the upgraded Foreman, Djinn and Queen did not: `EnterPhase04D` cancelled the current swing yet never set
`CreatureBusyUntil`, so the boss could start a fresh melee swing on the next frame of its own roar, and rings scheduled
just before the change could appear under the player during it. Support golems and mirror copies could swing the
moment they appeared. Phase changes were only a line in the callout stack and a small boss bar label.

**What changed** (`Creatures/BossPhaseRulesH8S.cs`, `Creatures/CreatureBoss.PhaseH8S.cs`, `UI/CombatBannerH8S.cs`)

- Foreman, Djinn, Queen: the boss holds its own swings while it roars (2.4 / 2.0 / 1.6 s by difficulty, always inside
  the original 3 s scripted gap) and rings that were scheduled but never shown are dropped; rings already on the
  floor still burst (they were telegraphed) and the Foreman's support arrival rings (the phase's own event) stay.
- Support golems and mirror copies wait 1.2 / 0.9 / 0.7 s before their first swing (mirages never strike already).
- Every boss (including the Titan and the regional guardians) announces a phase with a "PHASE 2 / 3" banner under the
  world event callout and a deep bell. The Titan's script, balance and timing are otherwise unchanged.
- Boss rings keep their elemental fill with an orange edge at the burst's true reach (all boss rings are unblockable
  except the regional marks, which get a red edge).
- The regional hook lives in `CreatureBoss.Update`, so `CreatureBoss.Regional04OD.cs` keeps its accepted bytes.

**Tests**: `CombatPacingH8STests` (ThePhaseRoarIsSafeAndShorterThanTheScriptedGap, OnlyRingsThatNeverAppearedAreDropped,
TheBannerFadesInHoldsAndFadesOut).

## 5. Crystal focus readability

**Friction found**: the heat bar under the crosshair (drawn by `LootArmory`) was a thin colour-only gradient with no
threshold and no words until a forced vent; there was no warning before an overheat and no cue when a vent ended; the
Mirage Choir lock count was drawn on top of the heat bar; the lock count and the Mirrorwake focus label were built as
new strings every frame.

**What changed** (`Combat/FocusHeatRulesH8S.cs`, `Combat/WeaponController.CrystalCuesH8S.cs`, hooks in
`WeaponController.Crystal.cs` and `WeaponController.CrystalHud.cs`, `CrystalHeat.VentLeftOf`)

- The 04D heat book is only read, never changed (focus balance is untouched). Bands: cool, warm (45 percent), high (75
  percent), critical (the next shot, or the next half second of beam, overheats; a ready charge counts its charged
  heat), venting.
- On the existing heat bar: a white tick at the high mark, the shots left (or beam seconds) to its right, the vent
  countdown while venting, and below it "HEAT HIGH   [R] VENT EARLY" or "NEXT SHOT OVERHEATS   [R] VENT NOW" (the pad
  shows "[D-pad Down]", the reload tap on every pad family). All strings are constants or cached.
- Sounds (synthesized, `Audio/CombatSounds.H8S.cs`): two soft rising ticks when heat runs high, three sharp ticks when
  the next shot overheats, a cool release when a vent ends. One cue per crossing, a 1.5 s guard against repeats, no
  cue for a swapped-in focus's existing heat.
- The Mirage Choir lock count now sits right of the crosshair.

**Tests**: `FocusHeatH8STests` (BandsReadTheHeatWithoutChangingIt, TheNextShotUsesTheRealProfiles,
ShotsAndBeamSecondsLeft, OneCuePerCrossingAndNoneWhenCooling, HudWordsAreCachedConstants).

## 6. Poise and stagger feedback

`Combat/PoiseRulesH8S.cs` and `UI/PoiseHudH8S.cs`: a poise strip at or under a quarter pulses bright with a white end
tick (one more solid hit staggers), and a staggered enemy shows a thin bar shrinking over its punish window, on creature
bars, people's bars, the lock-on bar and both boss bars. Poise and stagger rules are unchanged.

## Debug page

F2 > "Readability H8S" (Order 55): the cadence gap, blows planned and blows that waited a beat, kill confirmations and
feedback freezes, the focus heat band, the last phase banner, and buttons to preview the banner and every new cue.

## Settings respected

Damage numbers (`Settings.DamageNumbers`) still hide numbers; reduced effects (`CrystalCombat.ReducedEffects`) shrink
the new freezes like the melee freeze; new sounds follow the effects volume through `CombatSounds.Play`. Telegraphs are
gameplay information and stay on with reduced effects. No Settings field was added.

## What Tyler should try in Unity (NOT RUN here)

Use F2 > Creatures to spawn, F2 > Readability H8S to watch the counters. Play on Balanced first, then Veteran.

1. Crystal Basilisk: its tail sweep fills a red ring around its body before it swings; step outside the ring edge and
   the tail must miss. Its charge draws a red lane that follows you, then stops turning (about two thirds into the
   wind-up): step sideways out of the lane and the charge must run past you.
2. Neon Jackal pack: each pounce draws a red lane; after it locks, a sidestep avoids the leap. With two or three
   jackals biting, bites should land one after another, never two in the same instant (the "waited a beat" counter
   rises).
3. Dune Wyrm: the eruption marks an orange ring at your feet; the tail lash a big red ring round its body. Stand just
   outside either edge: no hit. Inside: hit.
4. Shard Golem (quarry at night): the slam ring is orange and its edge is where the shockwave stops.
5. A gang or encounter fight: people winding up a melee blow on you show "!" (an elite's unblockable heavy "!!") over
   their heads, not only the locked target.
6. Kill a few jackals with a gun: a short crisp chord per kill, rising through a quick streak, and a tiny freeze on each
   kill; kill a Shard-Touched creature for the heavier chord. Killing a civilian must not play it. Turn damage numbers
   off: the chord still plays, numbers do not. The killing blow's number should pop bigger.
7. Bosses (F2 > Creatures > fight): Foreman at two thirds health shows "PHASE 2 / 3" with a bell, roars without
   swinging for about 2 s, and its support golems pause a moment after clawing up. Queen: copies pause before swinging.
   Djinn and the regional guardians: banner. Titan: banner only, everything else as before. Boss rings show an orange
   edge over their colour; standing just outside the edge is safe.
8. Crystal focus (Shardwake or Prism Thread from the Arsenal or F2 > Crystal weapons): fire steadily and watch the
   tick at three quarters, "HEAT HIGH [R] VENT EARLY" with two ticks, then "NEXT SHOT OVERHEATS" with three, then the
   vent countdown and a soft chime when it can fire again. With a pad the prompt says "[D-pad Down]". Mirage Choir: the
   lock count sits right of the crosshair, clear of the heat bar.
9. Hit any enemy until its poise strip is low: it pulses; stagger it: a shrinking bar shows the opening.
10. Performance: run the perf flythrough before and after; the lanes and rings are pooled and the HUD adds no strings
    per frame, but this was not measured.

## Remaining weaknesses (honest)

- Nothing here was seen, heard or played in Unity; ring and lane brightness, the banner's place on 21:9 or small
  screens, and the cue volumes need a real look and listen.
- Lanes are flat strips tilted to the slope between their ends; on very bumpy ground parts can sink or float.
- People's own melee swings (`NpcController.cs`, not in this builder's area) are read by the cadence but do not wait for
  it themselves; a one-line hook would complete it (see the final report).
- The flash before an elite person's unblockable heavy (`NpcController.cs`) is still red.
- Underheart role signatures and Underheart bosses keep their own documented ring colours.
- The aim lock makes ground pounces and charges avoidable by a sidestep after the lock (intended readability, slightly
  easier); flying dives keep their predictive aim.
