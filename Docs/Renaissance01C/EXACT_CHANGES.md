# Visual Renaissance 01C: exact changes

Foundation: completed Visual Renaissance 01B, itself based on the complete Master Build 01 project. All listed changes are implemented in the delivered source/assets. They have not been compiled or playtested in Unity here.

## Difficulty and incoming combat

The project already had Story, Balanced and Veteran. This revision extends those existing choices instead of adding an incompatible difficulty stack or save field. `Core/Settings.cs` defaults fresh settings to Story; existing preferences remain. `UI/MenuSystem.cs` explains the welcoming default and response differences in the existing settings panel.

| Story setting | Before 01C | 01C |
| --- | ---: | ---: |
| Incoming damage multiplier | 0.40 | 0.35 |
| Enemy spread multiplier, higher is less accurate | 1.9 | 2.2 |
| Initial aimed-shot reaction | 1.4 s | 1.8 s |
| Enemy windup multiplier | 1.30 | 1.45 |
| Ranged admission | Separate NPC and creature pools | One combined pool, Story cap 1 |
| Shots without a ranged token | NPCs could fire broad suppression | Disabled on Story |

`Combat/CombatDirector.cs` uses new engine-free `CombatPressureRules.cs`. NPC sniper and pinning paths now respect ranged admission. Balanced and Veteran retain their existing damage, reaction and spread numbers. Their ranged pools also become shared. The melee limit remains separate: a Story encounter can still have one melee and one ranged attacker. Deployed turrets and already-flying projectiles are not counted as new ranged-token applicants.

The player damage multiplier is still applied at the existing player damage hook. `CharacterHealth`, general damage definitions and gang reputation/story floors are unchanged; there is no second damage multiplier added to them.

## Police spawning and wanted levels

Audit found overlapping dispatch routes, uncapped patrol recruitment and a foot-only count that missed seated crews. `AI/PoliceManager.cs` now gives automatic cruisers, recruited patrols, foot squads, roadblocks and police bikes one shared car/officer budget. It counts managed living officers even while seated or temporarily distant.

| Automatic response | Before | Story in 01C | Balanced in 01C | Veteran in 01C |
| --- | --- | --- | --- | --- |
| Main dispatch interval | 2 s | 5 s | 3.5 s | 2 s |
| First dispatch delay | No deliberate initial grace | 5 s | 3 s | 1.5 s |
| Car caps, stars 1 through 5 | 2 / 3 / 4 / 5 / 6 | 1 / 1 / 2 / 2 / 3 | 1 / 2 / 3 / 4 / 5 | 2 / 3 / 4 / 5 / 6 |
| Combined officer caps, stars 1 through 5 | Foot target plus separate crews | 2 / 3 / 4 / 6 / 8 | 3 / 5 / 7 / 9 / 11 | 5 / 8 / 11 / 14 / 17 |
| Crime heat multiplier | 1 | 0.65 | 0.85 | 1 |
| Evasion progress multiplier | 1 | 1.35 | 1.15 | 1 |

New `AI/PoliceResponseRules.cs` owns the pure budget values. `Vehicles/TrafficManager.Bikes.cs` routes automatic police bikes through the same budget and first-response gate. Explicit authored `SpawnPursuitCar` calls and mission `SetStars` remain available.

`AI/WantedSystem.cs` rechecks delayed witness calls against pause, suppression, mission crime policy and the existing creature-fight exemption. Creature practice does not grant immunity for attacking civilians or police. Serious crimes still count.

## Peaceful starter exploration

`AI/StarterSafety.cs` resolves the existing layout rather than moving the player. `StarterSafetyRules.cs` separates policy inputs for four protected regions:

- Veyra Lofts: 140 m horizontal radius.
- Mara Solane Street Clinic: 120 m horizontal radius.
- Crystal Manor: 100 m horizontal radius.
- Existing promenade: x22 to x238, z-85 to z85.

The quiet policy has no five-minute expiry. Actual local aggression suspends it for 60 seconds; unrelated zones stay quiet. Aiming alone and traffic noise do not count as choosing combat. New/load sessions reset transient provocation, not saved reputation.

`NpcController.StarterSafety.cs` and narrow NPC integration changes stop ambient gang heat-only acquisition and unprovoked humanoid wildlife acquisition. Direct retaliation and explicitly scripted actors remain. Non-humanoid ambient creatures use the same policy in `Creature.Brain.cs`. `Threats.cs` distinguishes ordinary violence from optional practice noise, including finishing effects.

Additional director fixes close paths that gang tuning alone would miss:

- `Gameplay/WorldEvents.cs` checks both player and placement. Unsolicited random encounters in protected areas retire before their runner advances, without a reward or failure-history entry. `Events/EventInstance.cs` records an actual manual interaction separately from proximity; manually accepted and forced events remain.
- `Gameplay/LootWorld.Guards.cs` prevents automatic chest-crew aggression in protected areas. `LootWorld.Chests.cs` offers an explicit guard challenge through the existing interact prompt when a key is missing. Normal key opening, bounty state and rewards remain. An automatically started escort may quietly end on return to safety; walking near it is not treated as explicit acceptance.
- `CreatureDirector.Bosses.cs` suppresses automatic arena entry fights while the starter location is quiet. Explicit boss-start and mission/quest boss-spawn APIs are unchanged.

## Cars and motorcycles

`Vehicle.cs`, `Vehicle.Bike.cs` and additive pure helpers in `BikeMath.cs` change player-driven handling. Existing AI branches, vehicle definitions, top speeds, boosts, handbrake input and traffic routes remain.

| Source calculation | Before | 01C |
| --- | ---: | ---: |
| Normal car steering at 5 m/s | 26.67 degrees | 38.08 degrees |
| Bike steering at 5 m/s | 26.12 degrees | 36.40 degrees |
| Assisted yaw grip budget | Car uncapped geometric request; bike full grip | Car 88%; bike 82% |
| Visible player bike bank limit | 38 degrees | 30 degrees |
| Bike visible bank recovery | Original smoothing | Faster bounded recovery, up to 120 degrees/s |

The bike receives bounded lateral support during a measured turn, plus slide correction. Low-speed bank fades toward upright. Car and bike yaw assistance require ground support; neither gains a stationary pivot or unlimited cornering grip. These are source calculations, not measured corner radii or proven driving feel. Full equations and test scenarios are in `VEHICLE_AUDIT.md`.

## Magical habitat and interaction

`Creatures/StarterHabitat.cs` registers with the existing world reset system. `GameRoot.cs` creates it after the creature director and starts a deferred session after player/save placement. It waits for free exploration, so the original prologue and skip-to-clinic transition remain intact.

The habitat requests three existing creature species within 62 m of the actual free-exploration start: Neon Jackal, Crystal Basilisk and Rime Griffin, with a smaller Frost Stalker fallback if a clear griffin roost cannot fit. Placement rejects occupied roads, buildings, service prompts and blocked sightlines. It logs a shortfall rather than forcing creatures through geometry; a live placement check remains required.

Existing interact prompts let the player observe residents, update saved bestiary knowledge and trigger an acknowledgement using the current pose solver. One optional, bounded practice jackal reuses the normal combat and telegraph systems. Practice rewards and creature-part farming are disabled. Both sides of the practice encounter have scoped nonlethal damage handling; finishing hits, scaled burn damage and cancellation are covered in the habitat source review. Ordinary combat is unchanged. Session/load/despawn cleanup is owned by the existing director and world-system lifecycle. It is not a replacement companion, quest or combat system.

## Fantasy architecture and visibility

`ArtDistrict/DistrictArchitecture.cs` retains Asterfall and selects up to four existing frontages around Veyra Lofts plus two around the clinic. The selection uses actual generated POIs, so the new architecture is visible near normal starts. Custom lots and helipads remain excluded.

The facade composition changes from many small floor-window rows to fewer tall pointed registers, full-height mineral supports, embedded crystal fins and larger faceted crowns. Opaque building envelopes and original collider construction remain. Nearby existing road strips receive the established mineral-paving material. `DistrictAtmosphere.cs` extends the same bounded cool-light/fog treatment to the actual home/clinic anchors.

The source kit retains all 23 old model names/GUIDs. Four source meshes are refined and one 144-triangle arch LOD is added: 24 modules, 9,422 unique source triangles. The source-placement comparison for one representative 24 m by 24 m facade plus backing/crown changes from 142 to 29 placed parts, 35,856 to 7,482 near triangles and 2,656 to 2,284 far triangles. Those are reconstruction counts, not complete district counts or an FPS measurement.

01B's material library, crystal shader and pearl/cyan drake surface remain in the project. The visual report shows reference targets and actual source comparisons, not fabricated Unity screenshots.

## Integration and remaining work

`Core/Game.cs` identifies revision 01C and registers new resets. Existing input bindings, save schema, mission scripts, quest/event template data, rendering packages and vehicle roster remain. No external Unity dependency was introduced.

New regression test sources cover police budgets, ambient encounter consent, starter safety, mixed ranged pressure, vehicle handling mathematics and bounded starter architecture selection. They require execution in Unity. Offline evidence and the exact baseline file list are in the QA report and preservation manifest.

Production hero creature sculpts/rigs/clips, authored NPCs, Manor interiors/evolving rooms, full forest and realm art, physical crystal transmission and a complete authored spell suite are still needed. See the preserved production asset brief. This revision does not claim to reach the final cinematic references or pass the requested live playtest goals.

## Exact original-file modification list

Compared by SHA-256 with the completed 01B checkpoint. All other 682 original files are byte-identical. No original files were deleted. The preservation report records the hashes and all newly added files.

- `Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame.obj`
- `Assets/NeonSyndicate/Resources/ArtDistrict/Models/crystal_shard.obj`
- `Assets/NeonSyndicate/Resources/ArtDistrict/Models/dome.obj`
- `Assets/NeonSyndicate/Resources/ArtDistrict/Models/mesh_inventory.json`
- `Assets/NeonSyndicate/Resources/ArtDistrict/Models/pilaster.obj`
- `Assets/NeonSyndicate/Scripts/Runtime/AI/NpcController.Creature.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/AI/NpcController.Tactics.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/AI/NpcController.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/AI/PoliceManager.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/AI/Threats.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/AI/WantedSystem.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict/DistrictArchitecture.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/ArtDistrict/DistrictAtmosphere.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Combat/CombatDirector.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Core/Game.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Core/GameRoot.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Core/Settings.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Creatures/Creature.Attacks.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Creatures/Creature.Brain.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Creatures/Creature.Motion.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Creatures/Creature.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Creatures/CreatureDirector.Bosses.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Creatures/CreatureDirector.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Events/EventInstance.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Gameplay/LootWorld.Chests.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Gameplay/LootWorld.Guards.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Gameplay/WorldEvents.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/UI/MenuSystem.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Vehicles/BikeMath.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Vehicles/TrafficManager.Bikes.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Vehicles/Vehicle.Bike.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/Vehicles/Vehicle.cs`
- `Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs`
- `START_HERE_ART_DEMO.md`
- `tools/generate_district_meshes.py`
