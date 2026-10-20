# Crystal Manor Saga, Master Technical Blueprint 04N

Status: specification and next implementation contract. Based on the feature/crystal-kingdom-awakens-04n source snapshot 802b66cd31e80f148a462b9ad61eda476a1c25a0. Do not treat proposals as implemented features.

## 1. Integration first, parallel frameworks never

Preserve these current owners and conventions:
- Unity 6000.3.16f1. Runtime boot: GameRoot, Game and WorldSystems. Runtime uses the NeonSyndicate assembly and existing Editor/EditMode boundaries.
- Input: Act, GInput, Pad and Keys. Controller identifiers must follow the active device, and GInput.GameplayBlocked must remain respected.
- Combat authority: PlayerCombat, WeaponController, CombatDirector, Damage and CrystalCombat04F.
- Existing gun-to-mark-to-Burst system: ResonanceSystem and ResonanceRules. New chains attach to that owner, not a competing damage manager.
- 04N chain implementation: ResonanceChains04N, ResonanceSystem.Chains04N, BondSystem.Resonance04N and the added tests.
- Owned companions: BondSystem, CompanionRecord, state, rank, milestone tokens, assist cooldown and actual body.
- Human/creature enemies: CreatureDirector, Creature, CreatureLogic, NPCs, territory, WorldEvents and current encounter admission budget.
- Visuals: current BodyBuilder, HumanoidPose, HumanoidAnimator, CityLayout, CityBuilder, near/far batches, meshes, existing camera and UI hooks.
- Quests/economy: existing JSON validators, MaterialStore, CraftingSystem, ForgeDb, SkillMods, FactionSystem and established transaction receipts.
- Save: append-only SaveData with JsonUtility plus registered IWorldSystem Capture/Restore. No unexplained schema migration number.
- Art: existing Built-in materials/shaders and optional URP are separate compile targets. Shader and LFS safety remain mandatory.

Repository development rules in CLAUDE.md govern everything. These include 12 compile configurations, controller parity, ResetStatics, save-field freeze checks, guarded optional dependencies, engine-free tests for pure rules, strict authored content IDs and no commercial references in committed project documents.

## 2. Current 04N source truth

Three already published 04N source checkpoints:
- 46ccd630: distinct district structure additions and character tailoring.
- ecd98d0f: finite caster and CityRift ground eligibility improvements.
- 802b66cd: chain recipes, earned companion arts, UI prompts and bond mastery source changes.

Four currently defined chains in ResonanceChains04N: twin_fang (Gun, Gun, Melee); riftstorm (Melee, Gun, Melee); prism_volley (Gun, Gun, Focus, Gun); phantom_reversal (Counter, Melee, Focus).

Current chain contract:
- Valid nonzero action serial, hostile, unprotected, real positive damage and target instance.
- Duplicate action receipts blocked, capped at 8192 unique serials per reset to prevent recycled credit.
- Ordered suffix matching, 3.2 second maximum gap, 5 second ready window.
- Target-local limit of two accepted chains per life and bounded target map.
- Three known companion archetypes: quartz_guide, ash_warden, storm_skimmer.
- Existing CrystalCombat resource spends 40 for ordinary art, 55 for eligible Eclipse.
- Source HUD/menu use UIKit, Pad and original BONDS records.
- Synergy prompt window currently .95 seconds by default and 1.3 seconds in generous mode.
- Actual resolution code validates target, hostility, layer, sight, range, character state and companion availability.

These facts are source inspection, not a native Unity acceptance report. Audit real reset, save/load and charge behavior before declaring the existing implementation fully playable.

## 3. Proposed interaction contracts

Combat action event pipeline:
  Player input -> accepted action -> authoritative gameplay contact/result -> action serial receipt -> semantic token -> chain candidate -> eligibility and cost check -> atomic art reservation -> present animation/camera -> impact via existing damage owner -> release controls and owners.

Do not mint a token on raw button input alone. A gun shot that missed or was cancelled cannot be a RangedHit token. A defense token requires its actual correct defense result. Melee tokens require valid contact where the recipe says Hit. Ensure firearm pellets/projectile forks cannot create duplicate credit.

Keep the current enum and source implementation working. The future semantic vocabulary may include Gun, Melee, Heavy, Counter, Focus, Dodge, Launcher, Aerial, Environment, CompanionAssist and DrakeAssist, but add new members only with actual producers, tests and recipes. Do not refactor working code merely to match a conceptual class diagram.

Use stable token receipt plus sequence ID, target ID, actor ID, confirmed-action timestamp and optional focus entitlement. For future network transport, convert local Unity instance IDs at the boundary to durable session entity IDs; do not serialize GetInstanceID as a network identity.

## 4. A small concrete initial catalog

Already source authored:
- Twin Fang: Gun -> Gun -> Melee.
- Riftstorm Waltz: Melee -> Gun -> Melee.
- Prism Volley: Gun -> Gun -> Focus -> Gun.
- Phantom Reversal: Counter -> Melee -> Focus.
- Companion archetype finisher presentation: Cataclysm Wing (quartz_guide), Aegis Breaker (ash_warden), Prismatic Snare (storm_skimmer).
- Eclipse Convergence: requires legitimately earned harmonized bond, mastery tokens and earned focus.

Next required visual/playable acceptance: Choose at least three fully functioning distinctive art outcomes from these source paths. They must play real differentiated pose/action/VFX/camera beats and have visible hits, not merely different labels and numbers. An optional fourth is appropriate only after first three pass.

Future proposed examples, not implemented: a grappling drake aerial transfer, a defensive ally rescue chain, a synced ranged/mystic attack, environmental chain variants and human player cooperative attacks.

## 5. Atomic finisher lifecycle

1. Read source-ready chain and actual target.
2. Verify companion actually owned, active, visible, alive/eligible, correct rank, distance and cooldown.
3. Verify target hostile, in layer, line of sight, within range, and free of protected/mission-critical exclusions.
4. Validate focus entitlement and energy. Do not award unearned focus via temporary load or art animation.
5. Reserve input/actor/action before spending. A failure before commit must not charge or block the player.
6. Commit resource and ready-chain at one clearly defined success point. If an expected recoverable pre-impact failure occurs, use a documented consistent partial refund or fallback policy. Do not double-commit.
7. Start a bounded assist behavior and prompt. No global scene time freeze in future multiplayer.
8. Fire gameplay impact only from authorized movement/action callback with a one-shot receipt.
9. On target death, interrupted player, lost owner, loss of line, Pause, load, changing Veil state or exit, cancel/finish exactly once.
10. Always restore normal camera, movement, companion and HUD ownership, clear pending callbacks and release reservations.

Unit tests must include double activation, target death before impact, blocked LOS, out-of-range body, expired prompt, automatic confirmation, manual confirmation, cost failure, interrupted movement, Pause freeze, scene transition, old save and reset.

## 6. Existing Burst and new Chain resource balance

The original Resonance Burst loop recharges crystal energy through valid gun hits and primes targets from offensive powers; shooting a marked target detonates the Burst. The new 04N Synergy Arts spend through CrystalCombat. Keep resource flow in one documented diagram and test an entire cycle:
  Gun hit -> legitimate crystal charge -> crystal power primes target -> gun detonation -> chain recognized through eligible hits -> bonded companion art spends resource -> normal HUD/state restored.

Avoid a second visible meter until the existing distinction between crystal energy, charge, focus heat and chain-ready state has been formally resolved. Source says an art costs 40 (or 55 Eclipse); treat those as existing code values to test rather than automatic final balance targets.

Provisional balancing, not source truth:
- Finishers do not instantly delete normal bosses.
- Anti-spam and target-local cooldowns block charge farming.
- Confirmed timing improves the move; missed confirmation still safely resolves a weaker move.
- Stronger results require earned companion, valid target and legitimate cost.
- Crystal visuals never conceal an incoming enemy telegraph.

## 7. Companion authority and saved progression

Use existing CompanionRecord.Milestones and BondSystem to keep mastery persistent if sufficient. The existing 04N code credits Fang and Volley milestone sequences and gates Eclipse by Harmonized rank and an earned focus. Do not add a second trust/rank/save ledger unless a concrete case requires it.

Future optional Trust, Tactics and Resonance tree presentation is a design model and must map into genuine BondSystem progression, rather than overwriting the present ranks. A new save field is an append-only migration task and requires tests from representative old 04M saves.

Companion movement/animation: use current CompanionProfile/Body goal, CreatureAction, combat target, layer/LOS and synced timing. Pair-specific anchors are a proposed presentation enhancement: player, companion, victim, camera-safe positions with collision and corridor checks. If safe placement fails, play a shorter anchored attack with no teleport.

## 8. Collision and animation contract

Source-contact remains the authority for damage; IK aligns visuals after or alongside gameplay contact. Upgrade weapon sweeps only when tests demonstrate they outperform existing hit envelopes and do not break animation diversity.

Combat attack -> active contact window -> sweep/filter/guard -> damage/poise -> knockback/stagger -> hit reaction/sound/VFX/camera. Use one authoritative damage receipt and a limited per-target contact map. No contact-generated permanent environmental hazard without cleanup.

For paired arts, build actual hand/weapon sockets, travel phase, anticipation, simultaneous beats, impact and recovery. Native review checks weapon clipping, camera obstructions, companion collisions, dialogue/menu interruptions and player control.

No hard cut to a fixed camera if the route crosses a wall. A fallback over-shoulder angle must be available and camera ownership from prior cinematic sprints retained.

## 9. Creature role, CityRift and horde contracts

Existing ecology and territory owners govern spawn, claim, eligibility and rewards. The 04N caster source adds an original ranged attack role with interruption; confirm its real visibility, combat response, range and cooldown in native play.

A horde has two budgets: total visible admitted actors and maximum simultaneous high-pressure attackers. Each enemy has a tactical role, timeout, retreat, damage and reward authority. Preserve original starter quiet zone, shared tokens, event contribution gate, finite no-reward bodies and duplicate-loot controls.

Diagnose actual CityRift misses using the existing F2 placement rejection ledger and a normal daytime/night route. Do not simply override safe placement rules. A new source-level ground eligibility guard alone is not proof of native spawn success.

## 10. Living city, Forge and Underheart contracts

Continue the 04M sources, contracts, faction reputation, clinic, dispatch, forge treatments, stock logic and actual service hosts. Deliver one chain:
  real mixed-creature event -> finite claimed resource -> existing guild board job -> valid materials/cash transaction -> tangible Forge build change -> evolved encounter with a partner -> persisted reward.

Underheart must retain its established cells, bosses, entrances, environment layers and exit. Deliver one newly dressed or mechanically enhanced route and one normal-player objective. Do not silently overwrite the authored optional 04L Veil investigation or the entire 12-mission campaign.

## 11. Art pipeline and performance contract

Current code-generated geometry and rigging are shipped source truth. Fidelity gaps should be closed by adding tested production assets or truly higher-quality runtime geometry that actually render in the current pipeline. Any external DCC production path requires project governance, original/licensed asset provenance, retained import files, stable GUIDs and no accidental LFS pointer-only assets.

Asset stages: visual target -> blockout -> detailed mesh and UV -> textures and material response -> rig/socket/clip -> LOD and collision -> Unity import/build -> animation integration -> screenshot, GPU/CPU budget and playtest.

Prioritize: Malik silhouette and deformation; one polished supporting companion; one caster and one signature creature; two contiguous city streets; one Underheart entrance/room; three fully authored synergy attacks. Do not spend the sprint producing dozens of new model definitions without running them in the existing game.

Performance goal is provisionally 60 FPS for representative Windows test hardware; actual target hardware is unknown. Record frame time, spikes, draw/mesh counts, main-thread allocations, animation costs, visible AI count, particle load and save size. Low presets must preserve readable silhouettes and gameplay prompts.

## 12. UI and accessibility contracts

Preserve existing GInput/Act/Pad. 04N source currently offers U / LB plus Up for an art and a follow-up assist confirmation. All mappings must be verified for conflicts with existing pad focus cycling, Veil shift, interaction and menu input. Keys must appear in Docs/CONTROLS.md when changed.

Four HUD states: hidden/normal, building combo, synergy ready, brief optional confirmation, then clear. The real partner, target, cost and prompt appear. Auto confirm and generous window settings must persist and be visible without hiding core combat. A missed window must never break the game.

Colorblind-readable shapes, captions, reduced camera shake, reduced flashes, font scaling and accessible timing are mandatory acceptance targets, not just requested settings.

## 13. Save and lifecycle testing

CLAUDE.md says SaveData is append-only and the project uses JsonUtility, not a generic arbitrary schema. Use the existing version field convention and stable original fields. Test with two copied real pre-04N saves and a fresh save, including active bond, learned focus, spent treatments, guild reputation, Forge recipe, law, quest flags, mounted state and a pending finisher.

Transient combo history, runtime target instance IDs, camera leases and prompt windows are never resurrected across load; persistent mastery and earned art unlocks are. New Game / Enter Play Mode without domain reload must clear any new static state through Game.ResetStatics.

## 14. Honest verification ladder and evidence

SPECIFIED -> SOURCE_IMPLEMENTED -> MANAGED_TESTED -> NATIVE_IMPORTED -> NATIVE_PLAYTESTED -> VISUAL_ACCEPTED -> PERFORMANCE_ACCEPTED -> WINDOWS_BUILD_VERIFIED.

04M external reference: 1416 pass, 46 unchanged native engine-call failures, 58 exclusions and 27 new case passes. Do not confuse this with a fresh 04N run. Run real current-source tests and reconcile exact failing test names and reasons.

Native issues reported previously: Editor licensing/UPM IPC failure on a restricted host. If still blocked on the same host, do not keep attempting bypass; implement source work independently and record NATIVE_NOT_RUN. Native acceptance must occur on a licensed supported editor with real save/reload, screenshots and Windows build.

## 15. Future networking boundary

Current production remains offline single-player. Reserve only a transport-neutral future command contract with player, companion, target, sequence, expiry, resource cost, expected state, predicted visual start and authoritative impact receipt.

Later actual cooperative mode requires session and lobby, real identity/auth, server action authority, rewinding or latency tolerance, disconnect and reconnect policy, ownership and reward reconciliation, server-side encounter validation, two-to-four-user cameras and tests. Do not insert fake network sync into current BONDS.

## 16. Build and handoff

The executable next order is Docs/BUILD_ORDER_04N_CONTINUATION.md. All new work starts from the real current 04N source branch and includes explicit first-pass vertical slice, regression matrix and native evidence gate.

The implementation agent must publish code, assets, test traces, documented owner integrations and source commits, not only upgraded design files. Never merge main or previous draft PRs as an undocumented convenience.
