# Renaissance 01C: ambient aggression and combat-pressure audit

Implementation is present in source. Unity compilation, a five-minute peaceful play session and starter encounter difficulty have not been tested in Unity. The new pure-policy NUnit tests are acceptance tests to run, not reported executed results.

## Findings in the 01B foundation

- Ambient gangs acquired the player from global gang heat above 0.6, within 38 metres and line of sight. Gang heat can come from saved standing and permanent story floors, so an otherwise peaceful visit could become a fight without a new local action.
- Gang threat handling treated nearby player aiming and gun/violence noise as reasons to fight. Actual attacks already triggered direct retaliation and nearby gang alerts.
- Humanoid wildlife uses `NpcController.Creature.cs`, separately from the non-humanoid creature brain. It could acquire targets through sight, territorial warning escalation or noise without a welcome-area check.
- The existing player damage path already applies `CombatDirector.PlayerDamageTaken` through `PlayerController` into `CharacterHealth.DamageScale`. Scaling damage again in the health component would double the intended change.
- NPC aimed fire and creature ranged attacks had separate admission budgets. Story allowed one NPC shooter and one creature ranged attacker at the same time. Marksman shots did not request the normal shot token, and tokenless NPC suppression still fired potentially damaging rounds.
- Explicit mission, quest and event actors use persistence, tags, hostile options or direct `Attack` calls. Blanket changes to those methods would break authored encounters.

## Implemented welcome-area policy

`StarterSafety` resolves the existing generated layout and caches real POI/site positions. It does not relocate the player or modify city generation.

| Area | Bounds |
|---|---|
| Veyra Lofts | 140 metre horizontal radius around the generated `Veyra Lofts` POI |
| Clinic | 120 metre horizontal radius around `Mara Solane Street Clinic` |
| Crystal Manor | 100 metre horizontal radius around the existing `CrystalManor` site |
| Asterfall Promenade | x 22 to 238, z -85 to 85 |

Unprovoked ambient acquisition is quiet indefinitely inside those areas; this is not a five-minute timer. Each area has its own 60-second game-time provocation cooldown. Deliberate player gunfire, explosions, violence, fire or power noise open only the containing local area. Aiming and vehicle crash noise alone do not. If two areas overlap, an active provocation cooldown takes precedence, so moving within the overlap cannot erase it. New/load session reset clears these runtime timers without editing saved gang heat or reputation.

Ambient gang heat alone no longer marks the player as a target in a quiet area. Existing heat-only pursuit is also stopped when it enters the protected area, keeping the hostility query and actual firing state consistent. Directly provoked gangs still retaliate and alert their nearby crew. Explicit `Hostile` state targeting the player is retained, so this is not a way to cancel an attack the player started. Authored persistent/tagged actors bypass the ambient-gang policy.

Humanoid ecology is quiet only when it is ambient, not persistent, not mission-tagged, not explicitly hostile and not provoked. It stops warning/acquisition and heat-free pursuit in a quiet area. Authored monsters and directly provoked wildlife continue to fight. The non-humanoid brain uses the same runtime safety API through the separate habitat integration.

Opt-in practice noise reads `StarterHabitat.SuppressPracticeNoise`. That query includes the active trainer, final-hit callbacks and the habitat's short local finish grace. Generic practice sounds do not wake ambient crews. Actual attacks on ordinary NPCs and police crime handling still call `NotifyPlayerAggression`, overriding this noise exemption. The noise exception neither grants invulnerability nor clears wanted state.

The public `Attack` method remains authoritative. Autonomous world-event/bounty spawning must be gated before creating persistent actors; their implementation is covered by the separate starter-flow changes. The safety API must not be bypassed simply because a mission is active.

## Implemented welcoming difficulty tuning

The existing `DifficultyLevel` values and saved preference identity remain unchanged. The Story setting has gentler pressure; Balanced and Veteran retain their former values for these settings.

| Setting | Story in 01B | Story in 01C | Balanced | Veteran |
|---|---:|---:|---:|---:|
| Incoming damage multiplier | 0.40 | 0.35 | 0.62 | 1.00 |
| Enemy gun spread multiplier | 1.90 | 2.20 | 1.35 | 1.00 |
| First-shot base reaction time | 1.40 s | 1.80 s | 0.95 s | 0.60 s |
| Enemy windup multiplier | 1.30 | 1.45 | 1.00 | 0.85 |
| Shared melee attacker cap | 1 | 1 | 2 | 3 |
| Shared aimed ranged cap | Separate NPC/creature pools | 1 combined | 2 combined | 4 combined |

The existing single player-damage scaling path is retained. Health, armor, dodge, guard, parry, burning, healing, lethal handling, loot and story damage hooks were not rewritten. Reaction times still include the original random variation and warning-miss phase.

NPCs and non-humanoid creatures now request slots from one shared ranged admission budget. The diagnostic `ActiveShooters` count includes both. Existing NPC token handoff can consider waiting creatures. Marksman telegraphs reserve a token and release it when cancelled or fired. Tactical pinning requests a token as well. In Story, NPCs without a token wait instead of firing wide suppression that could still hit the player. Balanced and Veteran retain their old broad suppression behavior when an aimed token is unavailable.

The ranged cap controls attack admission, not all damage sources. Already-fired projectiles, explosions, humanoid creature abilities and engineer-deployed turrets can still exist independently; those established systems were not comprehensively rewritten. Scripted engineer encounters retain their turret behavior. The starter practice encounter does not create an engineer turret.

## Exact files owned by this pass

| Path below `Assets/NeonSyndicate/` | Change |
|---|---|
| `Scripts/Runtime/AI/StarterSafety.cs` | New layout-resolved runtime zones, independent provocation timers, session/static reset and public safety API |
| `Scripts/Runtime/AI/StarterSafetyRules.cs` | New engine-free bounds, cooldown, ambient gang/wildlife and threat policy |
| `Scripts/Runtime/AI/NpcController.StarterSafety.cs` | New focused partial for ambient versus authored actor checks, heat-only pursuit cleanup and practice-noise handling |
| `Scripts/Runtime/AI/NpcController.cs` | Gang hostility query, direct ordinary-victim provocation, quiet aim handling, stale pursuit cleanup and Story suppression gate |
| `Scripts/Runtime/AI/NpcController.Creature.cs` | Humanoid ecology perception, warning/acquisition and noise gates |
| `Scripts/Runtime/AI/Threats.cs` | Deliberate player-noise provocation hook, excluding aim/crash and explicit practice noise |
| `Scripts/Runtime/Combat/CombatPressureRules.cs` | New pure tuning and shared ranged admission policy |
| `Scripts/Runtime/Combat/CombatDirector.cs` | Story tuning delegation, combined ranged budgets/count and reset chain |
| `Scripts/Runtime/AI/NpcController.Tactics.cs` | Marksman token reservation/release and Story tactical suppression gate |
| `Tests/EditMode/StarterSafetyTests.cs` | Radius/boundary, indefinite quiet, cooldown, scripted/provoked exceptions, practice-noise and humanoid ecology policy tests |
| `Tests/EditMode/CombatPressureTests.cs` | Story/legacy difficulty checks, mixed-species capacity and invalid-count tests |

New source/test files include stable Unity metadata. Root integration adds `StarterSafety.ResetStatics()` to `Game.ResetStatics()` and `ResetSession()` to the existing world reset path. `CombatDirector.ResetStatics()` resets the pure combat policy; `StarterSafety.ResetStatics()` resets its pure policy. No new runtime package is required.

`Gangs.cs`, `CharacterHealth.cs` and `Damage.cs` were checked byte-for-byte against 01B and remain unchanged by this work. Gang story floors, heat arrays and the save schema remain intact.

## Checks performed and remaining acceptance

Eleven owned C# files parsed without syntax errors using the C# tree-sitter parser. New metadata and required reset references were inspected. Source-preservation checks confirmed the three core files above are unchanged. `COMBAT_SOURCE_CHECKS.json` records these checks and source hashes. Parser success does not prove C# semantic compilation, Unity API compatibility or executed behavior. No C# compiler or Unity runtime was available for this pass.

Required Unity acceptance:

1. Clean import, Console review, complete EditMode suite including `StarterSafetyTests` and `CombatPressureTests`, then a Windows player build. Repeat required renderer/input configurations.
2. Start at each actual entry point with a fresh save and with a later save containing high gang heat. Spend at least five minutes walking, looking and aiming without attacking. Confirm zero unsolicited ambient attacks, visible wildlife, readable prompts and no automatic hostile world event or bounty crew intrusion.
3. Attack an ordinary gang member, civilian or police actor. Confirm immediate appropriate retaliation, crime behavior and local cooldown; a quiet distant refuge should not be globally unprotected by that action. Reload/new-game reset should clear session timers without erasing saved reputation.
4. Start practice, use normal weapons/powers through the winning hit, and stop or leave practice. Confirm ambient crews remain quiet during final-shot and projectile-noise callbacks. Attack an ordinary victim during practice and confirm the exemption does not suppress retaliation or crime handling.
5. Walk toward an unprovoked humanoid territorial creature in a quiet area, then deliberately attack it. Verify no automatic territorial escalation before the attack, but normal retaliation afterward. Repeat outside the quiet area and with authored mission monsters.
6. Test one NPC shooter together with one ranged creature in Story, then Balanced/Veteran. Inspect actual shot token counts and firing behavior, including a marksman, pinning fire, token release on death/stagger/despawn and mid-session difficulty changes. The count should not be mistaken for a universal concurrent damage cap.
7. Complete a representative authored mission and event encounter, save/load, die/respawn and repeatedly enter Play Mode with domain reload disabled. Verify that quiet-area rules do not block scripted enemies, leak timers or preserve stale attack slots.

The five-minute quiet start and manageable fight are intended outcomes pending these tests, not observed results.
