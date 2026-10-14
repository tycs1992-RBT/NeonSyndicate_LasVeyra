# The combat loop: Resonance, armour plates, breakable parts, damage numbers

Phase 6 makes guns and crystal powers feed each other, gives human enemies armour you shoot off piece by piece, gives
every creature parts you can break, and puts floating damage numbers over everything you hit. Nothing here has been run
in Unity yet: the code compiles in all twelve configurations and the rules are tested offline, but how it looks, sounds
and feels needs your eyes (checklist at the end).

## The Resonance loop

Three steps that keep repeating in a fight:

1. **Guns charge powers.** Gun hits on hostiles refill the crystal bar (the bar pulses light blue when a gain lands).
   The charge is spread over the gun's fire rate, so a fast SMG and a slow sniper rifle charge about the same per
   second of good aim. Criticals, weak points and headshots charge more, kills a little extra, melee hits a little.
2. **Powers prime enemies.** Every offensive power leaves a short mark on what it hits: a glowing ring at the feet and
   a crystal shell around the body, in the mark's colour, with the mark's name and a shrinking timer over the head.
3. **Guns detonate primed enemies.** The first gun hit on a marked foe sets off a **Resonance Burst**: a much bigger
   hit, a splash on the foes around it, a full magazine for the gun that did it, a gold RESONANCE BURST marker at the
   crosshair, a big gold damage number, a crystal boom and a short hit-stop. The mark is used up.

### The marks

| Mark | Left by | Colour | What its burst does around the target |
|---|---|---|---|
| Kindled | Ember Chain, Sunbolt | orange | burning crystal: foes within 4.5 m take 35% of the burst as fire and catch fire; the target burns too |
| Stormlit | Static Lance, Roc Call | violet blue | lightning chains on to 3 more foes within 9 m, 40% each, with a stagger push |
| Unmoored | Brass Gravity Well | brass | a shockwave: foes within 5 m take 30% and are thrown down; the target is lifted |
| Glassbound | Glass Sight (the 4 nearest hostiles in front of you within 25 m) | cyan | the crystal shell shatters: foes within 4 m take 45% and lose poise (frost stagger) |

Quartz Ward, Mirage Step, Canal Mend, Ghost Frequency and Drive Sigil are not attacks and leave no mark.

### The numbers (all in `Gameplay/ResonanceRules.cs`)

| What | Value |
|---|---|
| Energy per second of continuous body hits, any gun | 4.5 |
| Criticals, weak points, headshots | x1.8 |
| Gun kill bonus (the normal takedown energy comes on top) | +2 |
| Melee hit | 2.5 |
| Most energy from hits in any one second | 9 |
| Mark lasts | 6 s (bosses and great beasts 4.5 s) |
| Burst damage on the detonating hit | x2.5 (bosses and great beasts x1.75) |
| Burst on a breakable creature part | double part damage |
| Splash never uses more than | 500 of the burst hit |
| Magazine refill | fills the magazine from the reserve; a crystal caster vents all its heat |

Example: Ember Chain costs 30. Six to seven seconds of steady body hits with any gun pay for it; headshots get there in
about four. Only gun rounds detonate a mark (hitscan guns and loot guns, ricochets included); melee, rockets, grenades,
explosive-round splash and powers do not, so a mark waits for your gun. A shotgun detonates with its first pellet.

### Hints

Three first-time hints show at most three times each, 25 s apart, and are remembered across sessions (PlayerPrefs):
gun hits charge your energy, a marked foe can be burst, and what each mark does. F2 > Combat loop > Show the hints again.

### For other systems

- `ResonanceSystem.BurstFired` (static event: target, mark) fires after every burst. Gear sets or gadgets can listen
  (for example a gadget cooldown cut on a burst). It is cleared by ResetStatics; subscribe again in your Init.
- `ResonanceSystem.Prime(character, mark)` and `PrimeAround(centre, radius, max, mark, frontOnly)` mark foes from any
  new power or gadget.
- `ResonanceSystem.StateOf(character)` reads a mark.

## Armour plates

Human enemies wear visible procedural armour: a chest plate, a helmet and shoulder guards, dark plates with a glowing
trim (blue police, deep blue SWAT, orange gang, red mission crews, cyan Shard-Touched elites, purple champions). One
armour pool is split between the pieces they wear (chest 50%, helmet 25%, each shoulder 12.5%, normalised).

- **Bullets strip the plate they hit first.** An intact plate stops the round; only the rest of a hit that breaks it
  reaches health. Legs are never armoured, so leg shots go straight to health.
- **Helmets blunt headshots.** A headshot on a helmet does only x1.25 (into the helmet) until the helmet is shot off;
  then the full headshot multiplier applies.
- **Explosions** spread over every plate (the plates take 75% between them, the rest reaches health). **Melee** is
  half stopped. **Fire and crystal powers pass straight through** (powers are the answer to heavy armour).
- **Lightning (Volt) rounds** do x1.5 damage to armour.
- **A plate whose share runs out flies off** the body with sparks, crystal glints and a crack ("Helmet off", "Armor
  broken" callouts). Bullets on intact plates spark like metal instead of drawing blood.
- **The name plates** show the armour pool as a blue bar just over the red health bar (enemy bars, the lock-on bar
  and the champion plates).

| Who | Pieces | Pool (Balanced) |
|---|---|---|
| Police | chest, helmet | 60 |
| SWAT (police heavies) | chest, helmet, shoulders | 160 |
| Mission enemies | chest, helmet | 70 |
| Gang members | 35% wear a vest (12% a helmet too) | 45 |
| Shard-Touched elite | everything | double |
| Champion | everything | triple (Armored affix: half again) |

Story x0.7, Veteran x1.25. Civilians, companions and clerks wear nothing; creatures never wear plates. Shielded
champions refill the plates that are still on. Numbers in `Combat/ArmorRules.cs`; the component is
`Combat/ArmorPlates.cs`; NPCs dress in `AI/NpcController.Armor.cs` (`RefreshArmor`, and `EquipArmor(loadout)` for other
systems, for example enemy roles that want a heavier set).

## Breakable creature parts

Every species has at least one breakable part and the bosses two or three. Each part has its own health (a share of
the creature's), carries glowing crystal shards on its bones, and breaking it changes the fight. A break throws the
crystal off with a shard burst and a crack, staggers the creature, drops the part's material at once and writes the
part into the bestiary. Each broken part adds 20% (up to 60%) chance of an extra loot-gun roll when the creature dies.

| Species | Part | Breaking it |
|---|---|---|
| Glass Mite | Glass Carapace | halves frontal damage until it cracks, then full damage from the front |
| Neon Jackal | Neon Fangs | bite reach and damage halved |
| Shard Golem | Core Plate | the chest core lies bare: criticals x1.5 harder |
| Hollow Shade | Drain Tendrils | half reach and damage, no more energy drain |
| Hollow | Blink Crystal | it can no longer blink to your blind side |
| Mirror Wraith | Glass Mantle | its frontal glass guard is gone for good |
| Crystal Basilisk | Crown Gem, Tail Blade | the crown gaze stops; tail and bites lose half reach and damage |
| Violet Wyvern | Wing Membrane, Fire Gland | it crashes and never takes off again; no more violet fire |
| Frost Stalker | Rime Claws | claw and pounce reach and damage halved |
| Rime Griffin | Flight Feathers, Talons | brought down for good; talons halved |
| Snow Hollow | Rime Claws | half reach and damage, no more stamina chill |
| Glacier Titan (boss) | Frost Crown, Knee Plates, Core Plate | takes 25% more damage; 70% speed; bared core |
| Dune Wyrm | Throat Plate, Ring Plates | bared throat core; 70% speed |
| Quartz Drake | Chest Plates, Wing Membranes, Ember Gland | bared heart crystal; grounded for good; no ember breath |
| Sun Griffin | Flight Feathers, Sun Beak | grounded for good; beak and talons halved |
| Sand Golem | Chest Plate, Sand Fists | bared core; no more thrown sand |
| Mirage Djinn (boss) | Sand Crown, Storm Bracers | takes 25% more damage; blows halved |
| Shard Golem Foreman (boss) | Foreman Crown, Slam Gauntlets, Core Plate | 25% more damage; blows halved; bared core |
| Mirror Wraith Queen (boss) | Glass Mantle, Mirror Crown | her own guard stays cracked; 25% more damage |

How part damage works: the part under the hit takes the hit's damage, bullets in full, melee x1.2, crystal powers
x0.8, explosions x0.6, fire x0.5; a Resonance Burst doubles it. Torn wings and a snapped tail tip also shrink on the
body itself. The **Third Eye** marks intact parts with a cyan ring, their name and a part health bar (on scanned
creatures, and on any creature within 35 m while the Third Eye is open). The **BESTIARY** lists a species' parts once
you have broken them ("Breakable parts"); old saves load with none known (`CreatureSave.PartsBroken`, append-only).

Data in `Creatures/CreatureParts.cs` (`CreaturePartsDb`, keyed by species id, plus `CreaturePartRules` and the pure
`CreaturePartBook`); the live component is `Creatures/CreaturePartsRuntime.cs` (`CreatureParts`); fight effects in
`Creatures/Creature.Parts.cs` and `NpcController.OnCreaturePartBroken`.

## Floating damage numbers

Every hit you land shows a number over the target: **white** for a plain hit, **yellow and bigger** for criticals,
weak points and headshots, **blue** for damage soaked by armour, the **element's colour** for elemental rounds and
crystal powers (fire orange, lightning blue, frost pale blue, void violet), **big gold with a "!"** for a Resonance
Burst. Hits from fast guns on the same target merge into one climbing number for 0.35 s. Turn them off in
**Pause > Settings > Damage numbers** (default on). Drawn through HudOverlays (`Combat/DamageNumbers.cs`, rules in
`Combat/DamageNumberRules.cs`).

## Debug page (F2, tab "Combat loop", Order 49)

Live counts (bursts, energy from hits, marked foes, crystal), buttons to mark every foe nearby with each mark, fill
crystal energy, show the hints again, strip all armour nearby, break a part on the nearest creature, spawn an armoured
squad (two mission enemies, a Shard-Touched elite and a champion) and toggle the damage numbers, plus the rules in
one paragraph.

## Controls

No new buttons. Fire is still left mouse or RT, powers are still B or LB + RT, the Third Eye is still Caps Lock / T or
LB + RB. F1 help has a line about the loop.

## What to test in Unity

1. F2 > Combat loop > Spawn armoured squad. Check the plates sit on the chest, head and shoulders of each body (not
   floating, not buried), follow the animation and the ragdoll, and the trim colours read.
2. Shoot a plate: metal sparks, a crack, blue numbers, the blue bar over the red one draining. Keep shooting until it
   flies off (sparks, a louder crack, "Armor broken"). Headshot a helmeted enemy (small yellow number into blue), shoot
   the helmet off ("Helmet off"), headshot again (big yellow number). Shoot legs: health only.
3. Gun hits: the crystal bar should pulse light blue as it refills. Try an SMG and the Long Watch: both should fill the
   bar at a similar pace.
4. Unlock powers (F2 Progress or cheats), cast Ember Chain at the squad: orange rings and shells, "KINDLED" labels with
   timers. Shoot one: RESONANCE BURST marker, big gold number, fire on its neighbours, magazine full. Repeat with Static
   Lance (lightning chains), Gravity Well (shockwave), Glass Sight (shells shatter).
5. Creatures: F2 Creatures, spawn a Frost Stalker, a Violet Wyvern and a Glass Mite swarm. Hold the Third Eye on them:
   cyan part markers with names and bars. Break the stalker's claws (its swipes should fall short and hurt less), the
   wyvern's wing (it crashes and never takes off again), shoot mites from the front then the back. Check the bestiary
   page lists the broken parts and the material toasts appear.
6. Bosses: the Titan, Foreman, Djinn and Queen each have two or three parts; check the shards sit on the crown, chest,
   arms or knees and that breaking the Core Plate makes core criticals much bigger.
7. Pause > Settings > Damage numbers off: no numbers. On again.
8. Watch the frame rate in a big fight with many armoured enemies and a mite swarm (plates are a few small meshes per
   enemy, mites carry one shard each).

## Files

New: `Gameplay/ResonanceRules.cs`, `Combat/ResonanceSystem.cs`, `Combat/ResonanceSystem.Hud.cs`,
`Combat/WeaponController.Resonance.cs`, `Audio/CombatSounds.cs`, `Combat/ArmorRules.cs`, `Combat/ArmorPlates.cs`,
`AI/NpcController.Armor.cs`, `Combat/DamageNumberRules.cs`, `Combat/DamageNumbers.cs`, `Creatures/CreatureParts.cs`,
`Creatures/CreaturePartsRuntime.cs`, `Creatures/Creature.Parts.cs`; tests `ResonanceTests`, `ArmorTests`,
`CreaturePartsTests`.
