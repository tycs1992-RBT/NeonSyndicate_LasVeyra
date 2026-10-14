# Renaissance 01C independent acceptance review

Status: completed source review and final offline validation. No Unity playtest result is claimed.

This review compares the 01C Unity project against the exact 01B project. It examines source paths that could undermine the requested peaceful starter experience, optional training, visible fantasy creatures, readable starter architecture, and manageable police and combat pressure. The independent review prompted targeted implementation corrections. The final reviewer applied one scoped ability-cancellation call in the new humanoid ambient-safety branch.

No Unity Editor or GPU was available. Source review, geometry validation and C# grammar checks do not establish Unity compilation, correct imports, working input, visual quality, frame rate, or five minutes of successful play.

## Findings raised during implementation

| Finding | Source path | Review disposition |
| --- | --- | --- |
| Managed officers outside a 240 m radius stopped consuming the shared budget although they could return to the pursuit. | `AI/PoliceManager.cs`, `LivingOfficers` | Corrected by the police author. All managed living officers now consume the budget regardless of distance. |
| Automatic police bikes called the public mission spawn method, bypassing initial dispatch delay and shared budgets. | `Vehicles/TrafficManager.Bikes.cs` and `AI/PoliceManager.cs` | Corrected by the police author. The automatic caller now uses `TrySpawnAutomaticUnit`, guarded by live wanted state, dispatch readiness, car cap and crew budget. |
| Random world events could create persistent hostile people and creatures inside welcome areas; an ambient gang gate alone does not cover them. | `Gameplay/WorldEvents.cs`, `Gameplay/Events/EventInstance.cs`, `Gameplay/Events/EventKinds.World.cs` | Corrected and independently source-reviewed. Player and candidate positions gate automatic spawning. Unaccepted automatic events retire before runner advancement when safety applies. Explicit manual acceptance and forced events remain distinct. |
| Sealed chest crews automatically spawned persistent enemy NPCs near the player; guard perception could attack without an accepted challenge. | `Gameplay/LootWorld.Guards.cs` and `AI/NpcController.cs` | Corrected and independently source-reviewed. Both player and chest positions gate automatic crews; uninvited crews retire safely. An existing manual interaction offers a clearly labeled challenge when the protected player has no key. Key-opening behavior is unchanged. |
| The Crystal Manor boss arena could start automatically inside a protected Manor area in the Inner World. | `Creatures/CreatureDirector.Bosses.cs` | Corrected and independently source-reviewed. Automatic arena eligibility checks welcome protection; public manual and quest boss methods retain their prior conditions. |
| Humanoid ambient creatures use the NPC creature brain, so a gate only in `Creature.Brain` does not protect against both creature implementations. | `AI/NpcController.Creature.cs` | Corrected and independently source-reviewed. Quiet ambient creature policy now guards humanoid thinking, perception and creature-noise reaction; unprovoked fighting creatures return home when protection applies. |
| The final hitscan strike could defeat a training target before gunshot threats were reported. An immediate `!IsDead` training flag then classified that same shot as ordinary aggression. | `Creatures/StarterHabitat.cs`, `Combat/WeaponController.cs`, `AI/Threats.cs` | Corrected and independently source-reviewed. Practice uses nonlethal completion; `SuppressPracticeNoise` survives the final-hit callbacks and two seconds of local trailing grace. Ordinary-victim damage still overrides protection. |
| Sanctuary observation assigned an unscaled deadline to highlight fields compared against scaled game time, causing excessive highlight duration after pauses. | `Creatures/StarterHabitat.cs`, `Gameplay/Compendium.cs`, `Creatures/CreaturePartsRuntime.cs` | Corrected and independently source-reviewed. Observation now sets the highlight deadline using the same scaled game clock as both existing readers. |
| Pending humanoid Blink and Slam abilities survived the new quiet-ambient return-home path; Blink could dereference its cleared target. | `AI/NpcController.Creature.cs`, `Creatures/CreatureAbilities.cs` | Corrected and independently source-reviewed. The new branch cancels `CreatureKit` before clearing the target and returning home. |
| Damage-scaled burn ticks bypassed the practice target's final lethal callback. | `Creatures/Creature.StarterHabitat.cs`, `Characters/CharacterHealth.cs` | Corrected and independently source-reviewed. The practice filter accounts for the later damage multiplier; the shared health implementation remains unchanged. |
| The trainer could defeat the player lethally, or leave a pending strike/telegraph during deferred destruction. | `Creatures/Creature.StarterHabitat.cs`, `Creatures/Creature.Attacks.cs`, `Creatures/StarterHabitat.cs` | Corrected and independently source-reviewed. Only the practice jackal's synchronous damage call uses temporary final lethal protection; cleanup cancels strikes and releases warning effects before deactivation. |
| A winning area hit could reach calm spectators after the live-training flag changed. | `Creatures/StarterHabitat.cs`, `Creatures/Creature.cs` | Corrected and independently source-reviewed. Allocated-trainer lifetime plus two seconds of local finish grace covers practice collateral; ordinary people keep their normal damage/crime response. |

The event `Engaged` field originally meant only that the player came within 45 m. It did not prove an accepted interaction or deliberate combat. The fix introduces `PlayerAccepted` for actual manual event interactions and keeps forced events in a separate director set. Proximity alone no longer bypasses safety cleanup.

## Preservation boundaries examined

- `WantedSystem.SetStars` remains available for authored mission consequences.
- Public `PoliceManager.SpawnPursuitCar` retains its requested mission crew. Automatic dispatch uses the separately budgeted path.
- Queued witness calls pause outside active play and are cancelled during wanted suppression or mission crime suppression.
- Direct mission, quest and explicit challenge actions retain their authority. Welcome areas suppress unsolicited acquisition rather than making the player invulnerable.
- Architecture decorates selected existing frontage and retains the established spawn positions, topology, road collision and interaction access.
- The starter habitat uses the existing creature director, body/pose pipeline, shared interaction controls and bestiary. It does not add a replacement combat implementation.
- New state should reset through existing static/session lifecycle hooks; existing save schema and shipped content identifiers remain protected by strict baseline comparison.

## Required Unity acceptance checks

| Check | Procedure and acceptance condition |
| --- | --- |
| Import and console | Open with the project Unity version, let model/texture import finish, then enter Play Mode. No new compilation errors, missing mesh failures, pink surfaces or repeating exceptions. |
| Peaceful starter | Start fresh Free Roam and the post-prologue route. Walk and look around each welcome area for five minutes without firing, damaging or accepting a challenge. No unsolicited hostile gang, creature, chest crew or random event attack. |
| Boundary return | Walk near an automatically generated event outside safety without interacting or attacking, then return. Proximity alone must not make an unsolicited fight follow into the refuge. |
| Explicit consequences | Damage an ordinary actor and verify retaliation and wanted behavior still work. Start a story encounter with explicit wanted stars and check its authored pursuit. |
| Optional practice | Find the sanctuary within two minutes; observe each available creature; deliberately start and finish practice using both melee and a ranged finishing hit. No police or ambient gang recruitment from practice noise, no training kill rewards, and stop/walk-away paths work. |
| Police budgets | At each difficulty, sustain a pursuit, deploy car crews, invite a bike response, drive more than 260 m away and return. Automatic crews remain within the shared budget and the initial delay is respected. |
| Driving | On keyboard and gamepad, check car acceleration, braking, reverse steering, tight and fast turns, collision recovery and camera stability. Repeat for a hover bike including lean recovery. |
| Fantasy frontage | From unchanged home and clinic starts, confirm the selected detailed facade is visible; inspect arch/window depth, crystal emission, surface shading and roof silhouette in both camera modes. Doors, paths, mission prompts and HUD remain readable. |
| Performance | Run the established performance route and compare frame-time summaries on the target desktop, then stand at the densest starter facade with sanctuary creatures present. No frame-rate result is claimed by this source review. |
| Lifecycle | Repeat New Game, Load and return to title; also test Enter Play Mode without domain reload. No duplicate sanctuary, training targets, stale protection cooldowns or stale police timers. |

## Reproducible offline validation

Run from the 01C project directory:

```text
python tools/validate_renaissance_01c.py --baseline path/to/exact/01B/project-or.zip
```

The tool creates asset, exact preservation, unified diff, twelve-configuration C# grammar and summary reports in this directory. The C# pass evaluates editor/player, Built-in/URP, and classic/both/new input conditional branches. It is grammar analysis only, not a C# compiler, Unity test runner or shader compiler.

Final execution completed after the source freeze against `CrystalManorSaga_VisualRenaissance_01B_checkpoint.zip`:

- Asset structure: 24 OBJ models and 32 textures; 97 unique Unity metadata GUIDs; zero reported errors and zero warnings.
- Exact preservation: 717 original files compared, 682 byte-identical and 35 modified within the explicit reviewed allowlist. No original files deleted and no changes outside that list.
- Added Unity assets: 15 implementation/test/model assets and their 15 metadata files, all within the explicit added-assets allowlist; none missing metadata. Their SHA-256 hashes are recorded.
- C# grammar: all twelve preprocessor configurations passed tree-sitter grammar checks with no reported errors. This did not resolve Unity API types or compile C#.
- Validator summary: `PASS_OFFLINE`. Evidence is in `OFFLINE_ASSET_VALIDATION_01C.json`, `STRICT_PRESERVATION_01C.json`, `CSHARP_SYNTAX_12_CONFIGS_01C.json` and `VALIDATION_SUMMARY_01C.json`.

Test files supplied by the implementation authors have not been executed in Unity. Import, shader compilation, semantic C# compilation, actual controls, frame rate, rendered appearance and all seven requested gameplay acceptance outcomes remain **NOT RUN**.
