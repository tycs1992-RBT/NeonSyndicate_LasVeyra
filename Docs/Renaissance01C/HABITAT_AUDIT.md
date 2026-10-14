# Starter habitat audit, Visual Renaissance 01C

## Implemented

This pass adds real, interactive creatures through the existing `CreatureDirector`. They use the existing species,
procedural skinned meshes, animation solver, health, damage feedback and input system. It does not replace the world,
move a story spawn, add a separate pet simulation, or create a new save format.

### Entry and findability

- `GameRoot` initializes `StarterHabitat` after the creature director. `Init` registers it with `WorldSystems`.
- After the player and save/new-game state are placed, `BeginSession` requests the habitat.
- Placement waits for 0.75 seconds of active free exploration with no active mission. It samples the player's
  position at that point. This preserves the prologue and its authored teleport, including Start After the Prologue.
- Free Roam can populate promptly. New Story waits until its active prologue ends; the two-minute discovery target
  should be measured from free exploration, not through the authored prologue cinematic.
- Candidate sites are within 62 metres of this anchor. Existing walk nodes and nearby ground are checked against
  road/building footprints, ground height, solid props, sight lines and service/mission interaction clearance.
- One cyan `C` blip labelled `Sanctuary` uses the existing map and minimap. A brief existing notification explains
  the interaction. No objective panel, full-screen tutorial or new input binding is added.

The runtime requests three residents: Neon Jackal, Crystal Basilisk and Rime Griffin. The griffin requires a wider
clear area; a Frost Stalker is used when a wide roost cannot be found. All remain on their existing scale and rig.
If a seed has fewer than three safe sites, the code logs the shortfall and preserves occupied roads and entrances.
Actual three-creature placement and visibility have not been validated in Unity for this build.

### Observation and calm behavior

- Residents are ground-level, calm until actually damaged, with goals confined to 1.25 metres around each home.
- Merely seeing, aiming near or walking beside them does not start their original territorial/aggressive logic.
- The existing nearest `Interactable` action presents `Press E to observe ...`; `GInput.Prompt` supplies the existing
  gamepad wording. Interaction waits for the actual mesh and rig to be ready.
- Observation uses the existing head-look pose for eight seconds, highlights weak points for 20 game-time seconds,
  and calls `Compendium.MarkScanned(speciesId, true)`. This unlocks the actual bestiary entry and its combat information.
- The existing first-species scan award is paid at most once per saved progression. Repeated observation does not
  grant repeated scan currency. Scan state already persists through `SaveData.Creatures`.
- Residents retain their normal creature team and collision volumes. Deliberate damage outside practice can provoke
  their existing retaliation. New resident instances use 25% base damage and 70% base health.
- During an allocated practice fight and its two-second local finish grace, calm residents ignore collateral damage.
  This covers an explosion processed after the winning hit as well as damage while sparring. Ordinary people are not
  protected by this creature-only filter.

Observation is not permanent pet ownership, feeding, breeding, following, mounting or a relationship progression
system. Those systems have not been claimed or added.

### Optional encounter

An existing world marker and interaction offer a single Neon Jackal practice encounter. Starting is disabled while
there is an active mission, existing combat, a wanted level, player death or QA capture. Cancellation remains
available for an allocated practice creature.

- A new trainer uses 65% of normal jackal health (45.5 before any other system) and 35% of normal base damage (4.2
  before its attack multiplier and the existing difficulty/hit-cap rules).
- The spawn search requires clear, level ground at least five metres from the player and five metres from residents.
  If this space is unavailable, the encounter does not start and the existing notification asks the player to move.
- The original enemy brain, attack selection, telegraphs, collision and dodge/parry loop remain active.
- The winning hit is nonlethal. `CharacterHealth.OnLethal` applies after damage multipliers and leaves one health.
  The creature burn filter includes `Health.DamageScale` before dividing the remaining-health allowance back into
  the incoming damage. This closes the burn-tick bypass when a gadget has increased the target damage multiplier.
  The habitat then removes the defeated trainer. Shared `CharacterHealth.cs` remains byte-identical to 01B.
- The practice jackal's bite and pounce temporarily install the same final lethal-hit protection on the player for
  that synchronous damage call only. The original callback is restored in `finally`; dodge and armor still apply.
  A would-be lethal practice hit leaves at least one health and ends the trial with feedback. This does not heal the
  player, protect against unrelated attackers or hazards, or add general invulnerability. The current jackal has no
  fire damage, breath or projectile attack; other practice species would need separate path coverage.
- Cancellation explicitly clears the target, cancels the pending strike and its telegraph, releases attack tokens,
  and deactivates the trainer before the director's deferred `Destroy`. It cannot receive a later frame update while
  waiting for destruction. Entering a vehicle also blocks a pending trainer hit from damaging that vehicle.
- No kill count, quest kill event, loot, parts, gear, experience or kill-triggered weapon refill is awarded.
  Ordinary on-hit combat feedback and resonance remain available for practicing the existing combat loop.
- Practice ends on either participant's nonlethal defeat, pressing the same interaction, leaving 38 metres from its marker, player death,
  a mission starting, return to title, QA capture, a 150-second timeout, or a 20-second failed body-build timeout.
- A 12-second cooldown prevents immediate repetition. The spawn cap is three residents plus one trainer.

The existing creature gunfire exemption remains in use. `SuppressPracticeNoise` remains true through the final
hit callback and for two seconds within 45 metres of the marker after cleanup. The starter-safety threat hooks use
this to avoid ambient gang escalation from the finishing shot. Actual damage to ordinary people still invokes the
normal crime/provocation path. This system does not clear wanted levels or make crimes immune to police response.

### Lifecycle and performance

- Each resident has one existing skinned mesh and its usual original species detail level. No new art asset is loaded.
- Resident bodies are queued by the existing worker-thread builder and materialized through its main-thread budget.
- The local habitat unloads past 125 metres and reloads below 85 metres. It does not follow normal walking or move
  the anchor after its initial free-exploration placement.
- A total body-build failure retries at most three times. Successful loads reset that failure counter; ordinary
  leave/return visits are not restricted to three visits.
- Tags `starter01c_habitat` and `starter01c_training` support exact director cleanup. Resident interactions are
  attached to their creatures and unregister through the existing `OnDisable` lifecycle. The practice interaction,
  marker and map blip are explicitly removed on unload/reset.
- `WorldSystems.ResetAll` clears pending placement, bodies, prompts, marker, blip and temporary practice state.
  `Game.ResetStatics` clears `StarterHabitat.Instance` for no-domain-reload entry.
- Existing save fields and all species IDs are unchanged. Runtime-only spawn options are `CalmUntilProvoked`,
  `NoRewards` and `PracticeTarget`.

## Files and integration points

| File | Scope |
| --- | --- |
| `Scripts/Runtime/Creatures/StarterHabitat.cs` | Placement, discovery interaction, optional encounter and lifecycle |
| `Scripts/Runtime/Creatures/Creature.StarterHabitat.cs` | Calm wander, observation look and nonlethal practice finish |
| `Scripts/Runtime/Creatures/CreatureDirector.cs` | Runtime spawn-option copying and no-reward death exclusion |
| `Scripts/Runtime/Creatures/Creature.cs` | Calm targeting, practice lethal/filter hooks, optional parts exclusion |
| `Scripts/Runtime/Creatures/Creature.Brain.cs` | Calm sanctuary and ambient starter-safety acquisition gates |
| `Scripts/Runtime/Creatures/Creature.Motion.cs` | Existing pose input receives temporary observation look |
| `Scripts/Runtime/Creatures/Creature.Attacks.cs` | Practice-only bite/pounce player protection and occupied-vehicle guard |
| `Scripts/Runtime/Core/GameRoot.cs` | Boot and session entry, integrated by the core pass |
| `Scripts/Runtime/Core/Game.cs` | Static reset, integrated by the core pass |

Paths in the table are relative to `Assets/NeonSyndicate`.

Public integration API: `StarterHabitat.Instance`, `Init(CityLayout)`, `BeginSession(Vector3)`, `ResetStatics()`,
`TrainingActive`, `SuppressPracticeNoise`, and `IsStarterResident(CharacterBase)`.

## Validation performed

- Read the actual boot, New Game, Free Roam, prologue skip, interaction, creature spawn/brain, health damage,
  burn, death/reward, bestiary persistence, body-job, map-blip and despawn paths.
- Parsed seven habitat-related changed/new creature C# files with the C# tree-sitter grammar without syntax errors.
- Transcribed the scaled burn-cap arithmetic using IEEE float32 and checked 20 combinations of low/full health and
  damage scale; all retained positive health. This is a numerical/source check, not execution of C# or Unity.
  Exact inputs and source hashes are in `HABITAT_SOURCE_CHECKS.json`.
- Reviewed the post-filter damage path and used the existing final `OnLethal` hook rather than relying on an early
  damage cap that later resonance multipliers could bypass.
- Independent review caught and corrected the final-shot threat race, weak-point highlight time-base mismatch,
  damage-scaled burn bypass, practice damage to the player, and pending-strike cancellation at cleanup. The highlight
  uses `Time.time`, matching current bestiary expiry; its visual pulse separately uses unscaled time.
- Verified source references for the boot/session/static-reset call sites. This is source inspection, not execution.

## Unity validation still required

1. Compile the supported Built-in and URP editor/player/input configurations. Static parsing is not C# type checking.
2. Run Free Roam and Start After the Prologue from clean and existing saves. Confirm three bodies are ready and
   visible near the actual start, the cyan C is useful, and each is interactable within two minutes of free exploration.
3. Walk peacefully for five minutes at each starter area. Check ambient nonhumanoid and humanoid creatures, gangs,
   police, world events and nearby hostile camps together. Other 01C systems own the non-habitat threat changes.
4. Inspect all generated points at the default seed and several alternate seeds. Check sidewalks, roads, slopes,
   doors, mission contacts, collision, griffin folded wings and the smaller-species fallback.
5. Test keyboard/mouse and gamepad observation, repeated observation, pause/slow-motion highlight duration, save/load,
   and confirmation that a repeated scan does not pay again.
6. Start one practice fight; verify warning visibility, manageable damage, no collateral resident aggression, no
   police pile-on, and a nonlethal finish with melee, a large critical, resonance burst, explosion and burning.
   Apply a damage-amplifying gadget mark before burning. Lose a trial at low health and confirm the final point is
   protected, the callback is restored, and unrelated enemy or environmental damage still behaves normally.
7. Shoot an ordinary civilian, gang member and officer during practice; their normal crime/retaliation must still occur.
8. Test cancellation, leaving, losing, body-build failure, timeout, mission activation, title return, repeated sessions,
   no-domain-reload twice, and unload/reload without duplicate blips or stale interactables.
9. Confirm first/third-person camera clearance, UI readability, animation grounding and frame-time cost on desktop.

No Unity Editor, graphics-device render, play session, two-minute timing, five-minute safety run, profiler capture,
shader compilation or desktop build was executed in this environment. The runtime habitat is implemented; these
acceptance checks remain open.
