# CRYSTAL MANOR SAGA / NEON SYNDICATE — LAS VEYRA
# MASTER IMPLEMENTATION DIRECTIVE 04M: THE LIVING CRYSTAL WORLD — DISTRICTS, FACTIONS AND THE FORGE

**Task type:** actual large Unity development sprint for ChatGPT Work or a repository-connected coding agent; **not** a planning-only request, a nonworking prototype, a slide deck or a reinstall.

**Theme:** True inhabitation, finite ecosystem, faction choices, interactable services, meaningful crafting and tangible open-world exploration.

## This update supersedes the earlier generic 04M setup (October 19, 2026)

The owner supplied this 04L Work handoff and explicitly requested **fast consecutive implementation sprints, no repeated verification/research when no discrepancy is reported**:

- **04L final continuation SHA:** `8c6c3804b70898e745479affecdfe5f66d95ad1e`.
- **04L draft PR #7:** `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/7`, targets 04K.
- **Reported delivered in 04L:** the Manor camera shot retained, repaired close-up framing and cinematic cleanup, scene pause/resume with no menu-triggered dialogue skip.
- **Reported tests:** 12 managed API configurations, three assemblies, two source GitHub CI runs, external NUnit 1,389 passes, 46 unchanged native-engine failures. These are **user-reported predecessor facts** and must not be misrepresented as independently rerun against 04M.
- **Native Editor/Windows build:** prior runner blocked by IPC; continue with source features and independently executable regressions.
- **04M goal:** visibly transformed **single-player urban fantasy action RPG**, not competitive pre-match systems, artificial 40-blueprint requirement from another project or a redo of 04L cinematics.

Target deliverables per completed source gate: one coherent player-visible behavior or geometry path, genuine underlying runtime integration, a regression test, an additive Git commit, and status notes. Make the current game meaningfully better even if native playtesting stays restricted.

---

## Exact branch contract

- **Repository:** `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`.
- **Immediate predecessor branch:** `feature/the-veil-remembers-04l` (the 04L branch specified in the prior Work kickoff). **User-reported exact latest 04L HEAD:** `8c6c3804b70898e745479affecdfe5f66d95ad1e`. Treat this as the supplied development baseline. Only inspect branch identity/ancestry if Git actually reports a missing branch, mismatched head or conflicting existing 04M work; do not perform a prolonged redundant preflight audit.
- **New feature branch:** `feature/living-crystal-world-04m`; if already present, inspect parent ancestry and current unmerged work, then continue without reset.
- **Draft PR target:** `feature/the-veil-remembers-04l`. Do not merge to `main` and do not rewrite other open PRs.
- **Unity:** 6000.3.16f1, Built-in default / optional URP, Input System 1.17.0.

**Required continuity:** Work from the reported 04L source HEAD immediately; do not repeatedly fetch status/PR/test reports unless Git errors or an actual inconsistency arises. Read the companion `Crystal_Manor_Saga_MASTER_BLUEPRINT_04K_to_04N.md` and the latest parent `RESUME_NEXT.md`, `START_HERE_*`, module status, test ledger and actual source/scripts. If the Master Bible / visual ZIP exists, treat it as historical visual guidance, not a replacement source project. Before implementing, confirm which requested features truly exist, are partially integrated or are absent.

## Binding engineering instructions

This is an **implementation directive for the actual, already-existing Unity game**, not a request to re-summarize it, generate a concept image, rebuild an empty demo, or return a plan instead of code. Act as a coordinated game director, Unity engineering team, technical art/rig/animation team, narrative/quest team, AI/combat team, content producer, QA and release engineer.

**Source of truth order:** (1) inspected current branch code/serialized content and Unity scene, (2) `Docs/STORY.md`, `Docs/CONTROLS.md`, exact sprint acceptance and handoff docs, (3) preserved master bible/visual-reference pack as creative guidance, (4) this directive. If a historical document contradicts code after 04I (for example city wildlife appearing only at night), current code wins. Do not invent established canon, IDs, assets, game modes, tests, screenshots or capabilities. New lore is optional additive content, not a replacement for the original 12-chapter plot.

**Do not import ideas from another game.** This is Las Veyra's story-driven single-player open-world action RPG with Free Roam, original missions, Underheart, strongholds, territories, guild quests, contracts, crystal creatures and bonded drakes. It is **not** a pre-match PvP class-pick or team-side preparation game. Deepen existing Forge/build/loadout/economy gameplay when relevant; no disconnected match lobby or competitive multiplayer framework.

**Repository:** `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`.

**Engine:** Unity `6000.3.16f1` revision `a56f230f6470`, Built-in renderer default, preserve optional URP compatibility, pinned Input System `1.17.0`. No HDRP migration, no gratuitous package upgrade, no project reset, no Git LFS pointer files treated as hydrated binary assets, no license bypass. This repository is not a Unity HDRP Funhouse project.

**Freeze / protect:** `main`; every earlier sprint branch and draft PR; all original assets, prefabs/scenes/GUIDs/LFS payloads; project settings; existing world seed and geometry; legacy SaveData/EventSave/GuidanceSave/LootSave and saved IDs; original `m00_*`–`m11_*` story and both ending laws; Underheart `uh05_*` content; 04G courier, 04H Quiet Circuit and 04I five Ascension chapters; boss entitlements and unique loot indices 17–22; six owned crystal focus identities, charge/heat and cell receipts; bonded `quartz_guide`, `ash_warden`, `storm_skimmer`, and fused `ventwing`, `kilnback`, `coilrunner`. Never free-grant an owned bond/focus/fusion or duplicate unique rewards. New records require additive defaults, explicit migrations and old-save regression.

**Work protocol:** Use the supplied 04L SHA and branch without unnecessary independent remote lookups. A single `git status` before a write and ordinary branch checkout are sufficient if successful. If the supplied ref is absent, unexpectedly diverges, or a new 04M branch already contains other work, then inspect the specific mismatch and preserve work; never reset or force push. Read `RESUME_NEXT.md`, current `START_HERE_*`, module status and test ledger briefly as handoff context, not an invitation to re-audit every prior sprint. Make real edits in existing owner systems, not shadow managers or duplicate worlds. Save cohesive tested commits after every meaningful gate. Never merge into `main`; create a **draft** PR targeting the immediately preceding feature branch.

**Fast iterative execution:** The owner intends to run approximately ten substantial source-development sprints before a comprehensive manual playtest. Therefore do NOT stop merely because legitimate native Unity licensing/Package Manager IPC is unavailable. Attempt only bounded, legal tool setup; record the exact native blocker once per changed host, continue with actual feature code, managed compilation, offline tests, source CI, and honest acceptance statuses. No fabricated engine QA. Do not leave a sprint as a mere planning package if source writes are available.

**Evidence grades:** `source-authored`, `Roslyn-syntax-checked`, `managed-API-compiled`, `EditMode-passed`, `PlayMode-passed`, `in-game observed`, `save/reload-passed`, `visually compared`, `profiled`, and `Windows-player-launched` are different claims. CI syntax/GUID checks are not Unity native import, PlayMode or measured FPS. Attempt legitimate native verification if available; if the editor/runner cannot initialize, preserve actual error, perform independently verifiable work and mark native items `NOT RUN`. A sprint is not complete merely because its prompt has ended; mark partial modules honestly.

**Default gameplay bar:** First five minutes in Free Roam must show an easily noticed improvement; the player can participate without F2/debug cheats, no unavoidable starter-area swarming, no cinematic that traps control, no inaccessible interior, no event that never ends, no lost companion or duplicate reward. Maintain appropriate creature/AI attack-token limits, cooldowns, mission ownership, ordinary observer safety and graphics presets Low/Medium/High/Ultra. Test keyboard/mouse and Xbox/PlayStation controller action parity.


---

## 24 integrated implementation modules

Each module is an actual build assignment. For every module publish (a) the existing authoritative code/content owner, (b) new source/assets and exact integration path, (c) a normal-control in-game acceptance test, (d) automated regressions where possible, and (e) status COMPLETE/PARTIAL/DEFERRED/BLOCKED with evidence. Make prioritized COMPLETE vertical slices rather than 24 disconnected C# stubs.

### M01 — Fast 04L → 04M continuity and cinematic inheritance

**Implementation:** Begin at user-supplied commit `8c6c3804b70898e745479affecdfe5f66d95ad1e` on `feature/the-veil-remembers-04l`. Preserve 04L authored Manor establishing shot, fixed speaker close-up framing, scene cleanup and **Pause/resume that does not skip dialogue**. Carry forward the reported 1,389 external NUnit passes and 46 engine-dependent failures as the predecessor REPORT, not as a newly rerun baseline. Create `feature/living-crystal-world-04m` from that source without spending the sprint searching history. Investigate branch/HEAD only on real mismatch.

**Concrete acceptance:** Git reports the new 04M branch descends from supplied 04L SHA; all 04L movie/dialogue systems and original story identifiers are unchanged; a source regression checks pause while cinematic dialogue is active. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M02 — One polished contiguous Las Veyra district

**Implementation:** Choose a real walkable corridor anchored near the existing Free Roam arrival, plus one contrasting existing district; improve ground-level facades, crystalline civic identity, usable sidewalks, lamp grammar, storefronts and transport readability. Capture 04L/04M native comparison shots from normal camera settings; prioritise one complete neighborhood over citywide superficial bloom.

**Concrete acceptance:** Player can walk a coherent upgraded Lofts-to-Dispatch corridor without debug mode, collision blocking or missing storefronts; changes are LOD-aware and retain original world coordinates. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M03 — Eight syndicates with visual home signatures

**Implementation:** Respect the saved Ember Court, Quartz Kings, Mirage Lotus, Brass Tower Union, Static Saints, Canal Choir, Glass Jury and Roof Oni faction IDs and turf. Give each existing district recognizable clothing/insignia, encounter behavior, service/front activity and crystal contamination cues without changing `Gangs` IDs or replacing gang/heat logic.

**Concrete acceptance:** Each of the eight existing gang IDs receives a recognizable in-world signature and at least one meaningful nearby behavior/interaction without raising hostility solely because of a cosmetic district change. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M04 — Guild locations with real services

**Implementation:** Deepen Menders clinic care, Forgewrights at Dante forge, Frequency Order at Iris/Veil anchors and Frost Wardens outpost: physically interactable services, faction-specific vendors, quest boards and resident greetings. Reuse the established FactionSystem, CraftingSystem, WorldServices, IInhabitedCity and QuestBook owners.

**Concrete acceptance:** Player enters and uses the same real four guild service owners, with understandable costs/unlock denial messages and no new vendor that bypasses quest/faction restrictions. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M05 — Civilian daily routines and subtle agency

**Implementation:** Use the existing pedestrian/venue schedule, ambient animation and shelter/resume system. Add a few complete work-to-break-to-home cycles and context reactions to an observed drake, patrol or rift without launching a duplicate population simulation. Services remain usable if a scheduled NPC is temporarily hiding.

**Concrete acceptance:** At least three residents demonstrate timed routines and interruptible shelter/resume while the original shop/quest interaction remains accessible after the interruption. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M06 — Reactive reputation with no softlocks

**Implementation:** Integrate faction-standing readouts into existing relationships/quest offers and shop discounts without overriding authored main-story floors such as Ember Court after Ashline Treaty. A player should always have a recovery path from negative standing, while legitimate scripted hostility persists until the relevant resolution.

**Concrete acceptance:** Two factions demonstrate a visible earned change in reception/service/price; authored campaign hostility floors and existing save ranks remain authoritative. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M07 — Territory ownership with visible differences

**Implementation:** When the player captures an existing 04B relay/stronghold, show a bounded NPC, material/lighting or service shift nearby based on saved ownership. Apply through existing TerritorySystem and StrongholdSystem, no new save owner; prevent immediate recapture oscillation or duplicate first-claim rewards.

**Concrete acceptance:** One claimable territory visibly changes patrol, signage or service access and reverts appropriately when control changes; existing claim receipts, ownership and defense windows persist. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M08 — Finite living-world event ecology

**Implementation:** Author bounded events that respond to district, time, weather, saved choices and nearby faction status using WorldEvents templates. Each event must have a readable start, neutral observer handling, player choice, cleanup, cooldown and once-only compensation; existing max-active and local claim/collision budgets remain authoritative.

**Concrete acceptance:** At least three distinct event types execute spawn → interact/fight → resolution → cleanup with correct cooldown, objective exclusion, civilians protected and no repeat payment. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M09 — Crystal ecology reliability in normal Free Roam

**Implementation:** Take 04I/04J CityRift selection/placement and F2 rejection counters all the way through real Unity observation. Cure no-spawn reasons proven by logs, make day/night Hollow/Mite packs visible beyond safe areas, enrich park Jackals, quarry Golems and Grave Shades. Keep preset caps, off-camera natural entrance, objective clearance and return-to-quiet.

**Concrete acceptance:** Normal daytime and nighttime Free Roam can produce eligible Hollow/Mite groups outside quiet areas with bounded F2 rejection metrics; all original spawn caps/mission exclusions remain in effect. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M10 — Horde formations and mini-elite roles

**Implementation:** Add mixed but bounded encounter formations (swarm/flanker/caster/bulwark), one stronger leader and interruptible rally calls. Define explicit active body and attack-token budgets and failure/retreat end states; no 100-monster lag pile or automatic hostility against civilians. Reward only real player contribution.

**Concrete acceptance:** One finite mixed horde demonstrates leader telegraph, flanker/caster/bulwark differentiation, recoverable victory, retreat and consistent reward ownership without unlimited reinforcements. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M11 — Dramatic wilderness and biome routes

**Implementation:** Improve the Frostline, Quartz Badlands, canyon roost and quarry routes as grounded fantasy play spaces. Retain traversable road/vehicle and legitimate creature home ecology; add readable warning signs, gathering nodes, exploration views and at least one discoverable non-forced encounter per area.

**Concrete acceptance:** At least two significant biome routes provide a discoverable playable landmark or hunt with weather-appropriate ecology and safe return via original traversal and road routes. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M12 — Underheart ecology and living physical rooms

**Implementation:** Preserve all 04E Underheart cells, region names, 16-main/12-side quest and boss puzzle logic. Add event-appropriate denizens, heated/cold zones, safe shelter reads and stronger spatial landmarks in existing real corridors. The surface and underground spawn layers must never leak into each other.

**Concrete acceptance:** The same canonical Underheart entrances/doors/quest steps work, with at least two recognizable animated/interactable ecosystem behaviors inside the proper region layer. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M13 — Meaningful building interiors and transitions

**Implementation:** Upgrade at least three existing locations such as Cinder Dispatch, Pressure House, Bondkeeper’s Refuge, Jax garage, Mara clinic or Dante forge. Each has a door you can walk through, NPC/service purpose, interactions, safe camera and the correct exit; improve nav obstruction rather than adding decorative unreachable shells.

**Concrete acceptance:** Three named venues have usable entrances, service/object interactions, unobstructed stairs/doors, safe player and companion route, and correct visible exits. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M14 — Creature habitat behavior without free ownership

**Implementation:** Build a small ecology routine: feed/rest/territory-warning/escape/return for selected existing species and handler drakes. Passive creatures should remain observable without instant attack; injured drake reunion, target ownership and player BONDS must remain separate.

**Concrete acceptance:** At least two existing species demonstrate idle/rest/warning/leaving/returning; calm player behavior does not automatically trigger hostility or transfer animal ownership. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M15 — Traversal, vehicles and crowds work together

**Implementation:** Validate vehicles, motorbikes, flight, glide, drift, passenger/companion follow and fused chassis around real road junctions, doors, roofs and bridges. Tune actual steering and camera from repeatable native evidence, never replace original vehicle physics or corrupt garage ownership.

**Concrete acceptance:** At least one driving route, aerial traverse and owned companion transition survive a world/venue boundary without being clipped, lost, duplicated or deleting vehicle inventory. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M16 — Physical gathering and fair rarity

**Implementation:** Connect creature body-part breaks, rift remnants, caches and territory production to existing MaterialStore and known resource IDs. Add new material IDs only when a complete known acquisition/use/sell path exists; no impossible recipes and no repeatable save/reload duplication.

**Concrete acceptance:** At least four normal gather sources feed existing material IDs with finite depletion/respawn rules; a player can earn ingredients for one genuinely useful Forge recipe without debug grants. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M17 — Forge depth with useful tradeoffs

**Implementation:** Audit actual ForgeDb and CraftingSystem; existing infusions, brews, permanent upgrades and rank/faction unlocks already work at source level. Add a carefully balanced set of 8–12 COMPLETE *Las Veyra* recipes only after checking for duplicate IDs: each needs reachable ingredients, station, unlock, actual applied effect, UI card, consumption, expiration/upgrade stacking and save/reload. This is an RPG Forge, not another game’s pre-match shop.

**Concrete acceptance:** Eight to twelve ADDITIVE complete recipes have unique IDs, attainable input sources, distinct effects and caps, rightful station/unlock, transactional spend, UI and persistent craft/expiration outcomes. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M18 — Gear identity and build tradeoffs

**Implementation:** Make forged armor, shields, crystal foci and weapon cores affect meaningful stats and visible attachments through GearSystem, SkillMods, LootArmory and BuildSystem. Existing ward/heavy/flight/companion roles should have choices, not unlimited additive +damage stacking or paid upgrades that auto-grant without resources.

**Concrete acceptance:** At least two builds present honest tradeoffs rather than universally dominant flat bonuses; normal UI shows actual stats/requirements and refunds or equips cannot duplicate gear. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M19 — Economy and faction exchange

**Implementation:** Improve Forgewrights selling, RefugeTrade bundles, Frost Warden services and merchant prices with source-backed costs, stock/timers and faction standing. Detect profitable purchase/sell loops, duplicate reward claims and bosses-material vending. A player can earn any required ordinary ingredient through its legitimate region and quest route.

**Concrete acceptance:** Purchasing, resale and faction pricing cannot create cash/material arbitrage; stock and receipt handling survive save/reload and do not sell guaranteed-only boss entitlements. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M20 — Guild jobs and discovery loops

**Implementation:** Add at least one complete guild contract variant for each of Menders, Frequency Order and Forgewrights, respecting existing hunt/rift/escort/courier generator and avoid arena overlap. Physical quest clues, normal NPC choices, reachable marker, closure and ledger payment are required; do not manufacture dozens of placeholders.

**Concrete acceptance:** At least three new complete contracts, one each for Menders, Frequency Order and Forgewrights, run through physical objectives and existing QuestBook completion once-only payout. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M21 — Exploration UX and lived-in map

**Implementation:** Keep existing minimap/GPS, discovery runes, bestiary, gear, Journal/PLACES and BONDS pages. Add sensible region-level “what is here” intelligence, track nearest actual station/material source/quest without overriding active main mission GPS. Prompts should be reachable from a controller and show truthful locked/unavailable reasons.

**Concrete acceptance:** A player can track available services, recipes/material sources and relevant discoveries without replacing the currently active mission objective; controller navigation shows readable reasons. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M22 — Weather, day-night and performance tiers

**Implementation:** Connect believable creature habits, traffic, lighting, civilian shelters and emissive readability to existing weather/time. A high-end preset can show richer material detail but no gameplay-essential actor or quest may vanish on Low. Profile actual frame-time/GC, draw calls and shadow costs instead of claiming an abstract FPS target.

**Concrete acceptance:** At Low, Medium, High and Ultra all enemies, interactables and quests remain accessible; any perf numbers must come from actual Unity player/Profiler, not inferred from code. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M23 — Save, life-cycle and multi-zone resilience

**Implementation:** Test death, respawn, abandoned quest, crossing the Veil, loading, old-saves, stronghold claim and standing changes across multiple sessions. Clean event-owned creatures, materials prompts and merchant temporary objects at scene/session boundaries; no new global singleton that leaks into an unrelated world instance.

**Concrete acceptance:** Two reloaded old saves and a fresh save preserve region ownership, recipe unlocks, crafted effects, drake bonds, original 04L cinematic state, and mission event cleanup; no duplicate claims. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

### M24 — Native world acceptance and continuity handoff

**Implementation:** Deliver a reproducible 45–75-minute route: travel through two visually distinct districts, observe a populated service, encounter crystal pack, resolve faction event, enter three real interiors, gather/craft one item, claim or inspect territory, travel Underground and return safely. Publish honest native evidence and draft PR targeting 04L.

**Concrete acceptance:** Deliver the scripted player acceptance walkthrough, authored unit tests/managed matrix/GitHub CI as available, exact changed paths, module ledger, preservation report, draft 04M→04L PR and 04N handoff, clearly marking native steps NOT RUN. Implement automated rule/receipt checks where possible. Report source, native and visual evidence separately.

---


## Practical module-to-source integration map

This section names *candidate existing owners from the current game lineage*, not a request to recreate their classes. Use the checked-out 04L tree to resolve exact partial/class paths.

| Domain | Expected established owners | Real implementation target |
|---|---|---|
| Urban districts and real interiors | `CityLayout`, `DistrictMaterials`, `CityAscensionGeometry04I`, `InhabitedResidents04C`, existing art/venue batch and LOD systems | One contiguous original Lofts/Dispatch/Pressure House walk and three existing usable services |
| Gangs and law | `Gangs`, `FactionDb`, `FactionSystem`, `PedManager`, `NpcController`, `CityCombatRules04F` | Eight distinct gang signatures with ordinary wanted/reputation/hostility intact |
| Territory owners | `TerritorySystem`, `TerritoryBook`, `TerritoryEcology04B`, `WorldEvents` | Control consequences, readable defense, original once-only claim save path |
| Creature ecology | `CreatureDirector.Ecology`, `CreatureLogic`, 04J F2 creature debug counters, `CreatureFightRules` | Real ordinary-city spawns, finite diverse formations, no quiet-area aggression |
| Companions | `CompanionArchetypes`, existing Bond/Companion managers and 04K motion owners | Existing Quartz Drakeling, Ashshell Warden and Stormskimmer assist and travel without ownership changes |
| Crafting/economy | `ForgeDb`, `CraftingSystem`, `MaterialStore`, `GearSystem`, `Economy`, `RefugeTrade04E`, `LootArmory`, `BuildSystem` | Eight to twelve usable recipes with reachable costs and actual working effects; no illegitimate boss-only loot |
| Quests and map | `QuestBook`, `WorldEvents`, `QuestSteps`, `MissionManager`, `CampaignDirector`, existing Journal/Places/Map | Three fully functional distinct guild contracts, correct objectives/receipts, responsive tracking |
| Cinematic continuity | 04L scene/camera/dialogue owner and Pause/resume integration | Never regress authored Manor shot or allow menu input to silently skip scenes |

## Depth-over-count production standards

1. **Front-load a finished playable vertical slice.** In the first meaningful commit add a district improvement that appears in the existing arrival area; don't spend half the sprint on folders or summaries. Art must be actual authored geometry/material/prefab content wired to visible scene owners and correct LOD, not simply filename inventories or visual concept prompts.
2. **Use real player choice.** At least one guild service and one faction response should reflect an earned character decision. Prices, cooldowns, unlocking and denial must be visible. Save state must preserve it.
3. **Build in parallel only for genuinely independent owners.** Avoid simultaneous edits to `Game` initialization, `SaveData`, faction rep, `MaterialStore` transaction handlers or scene bootstrap. Integrate changes at each gate before adding the next.
4. **Prioritize old-game functionality.** A big visual upgrade that breaks the original twelve story missions, M00/M03/M08 crystal waves, 04K Fractured Bell, 04L pause/resume, bonds/fusions or accessible safehouses is not a successful sprint.
5. **No fake native QA.** Automated source/managed tests can prove class shape, deterministic rules and save serialization. Unity Play Mode, physical collisions, screenshot comparison and FPS require an actually working Unity process. If blocked by the same host IPC, record it briefly and continue shipping high-quality code.
6. **No mandatory long audit each sprint.** Use supplied 04L SHA as the start, preserve existing draft PR #7, and create 04M off its legit branch. Only perform deeper Git investigation if an actual mismatch or conflict occurs.
7. **Design for 04N, but do not start it.** Preserve hooks for post-story Mercy/Power consequences, advanced focuses, boss AI and earned drake progression; no unearned endgame item or overwriting the original finale.

## Sequential development gates, with real commits

The executing agent must progress through meaningful checkpoints in the following order. A gate is not done merely because a commit or checklist exists. Native failures are not license to rename source changes as successful PlayMode tests.

### GATE A — Check 04L source and real city baseline

Resolve latest 04L head and actual Unity errors. Capture two district, three venue, Underheart and vehicle baseline scenes.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE B — Polish contiguous districts and interiors

Build detailed normal-camera art in one working neighborhood; open three complete meaningful interiors.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE C — Living NPC, syndicate and guild relationships

Improve resident routines and 8 gang home identities, faction presentation and real services, without double population director.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE D — Dynamic finite creature and territory world

Resolve CityRift no-spawn in Unity, implement bounded horde variety, ecology routines, world events, ownership consequences.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE E — Travel and exploration economy

Tune city driving/flight/companion transitions, physical material sources, faction shops and optional discovery routes.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE F — Forge and quest depth

Add complete earned recipes, sidegrade tradeoffs, physical quest contracts and meaningful cash/material sinks with tests.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE G — World save/performance validation

Run original 12 story + 04E/J/L checks and an independent 45–75-minute integrated route with real Profiler evidence.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE H — Evidence and draft PR

Push 04M source plus UI/asset/license/test evidence; PR 04M → 04L, hand off blockers and 04N hooks.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

## Non-negotiable regression matrix

- **Original campaign:** M00 through M11 using actual mission IDs. Prologue and prologue-skip, early powers, crystal mob waves M00/M03/M08, family Luma protection, final boss anchors, Mercy and Power endings.
- **Underheart and late content:** original 04E 16-main/12-side quests and eight contract grammars where available, acquired BONDS/FUSION, Underheart rooms/bosses, 04G courier, 04H Quiet Circuit, five 04I Ascension chapters, and latest complete 04J/K/L/M work in the current predecessor tree.
- **Systems:** Asterblade/Riftfang/Basalt Crown, all six focus signatures, player/faction/NPC protection, bounded creature spawn/cooldown, drake injuries and handler ownership, gear/crystal UID and boss loot, crafting costs, save/restore from old saves, world transitions, dialogue/mission abort/death/quit, services and enterable interiors.
- **User experience:** normal Free Roam first five minutes, gamepad and mouse/keyboard, readability, accessibility/reduced effects, Low/Medium/High/Ultra parity, daytime/night, original vehicles and optional URP path.
- **Tool truth:** count actual NUnit passes, failures and exclusions; compare names against separately executed parent baseline. Syntax/GUID/LFS checks are not Unity native import. Measure FPS/GC only from running Editor/player. Never invent screenshot, binary build or commercial quality.

## Player acceptance route required for this sprint

Create a step-by-step route in `START_HERE_04M.md` that a nondeveloper can follow from the actual regular Free Roam menu on both a fresh and copied legacy save. Begin with observable art/gameplay improvement at the existing arrival within 60 seconds, follow with meaningful 5-minute encounter/interaction, then a finished longer integrated sequence (mission/event/loot/interior/exit) without debug teleport. Include equipment, a combat/creature contact, real claimed or paid reward, legal abort/retry/save/quit/reload and a calm world afterwards. Reference precise original controls, known world entrance names and any genuinely new optional content IDs. Fail if inaccessible or scripted actors disappear, if companions get lost, or if rewards duplicate.

## Required tracked GitHub handoff files

- `START_HERE_04M.md`
- `Docs/LivingWorld04M/FEATURE_STATUS.md`
- `Docs/LivingWorld04M/DISTRICT_AND_SERVICE_INTEGRATION.md`
- `Docs/LivingWorld04M/FACTIONS_AND_EVENTS.md`
- `Docs/LivingWorld04M/ECOLOGY_AND_HORDES.md`
- `Docs/LivingWorld04M/FORGE_ECONOMY.md`
- `Docs/LivingWorld04M/TEST_LEDGER.md`
- `Docs/LivingWorld04M/VISUAL_AND_PERFORMANCE_EVIDENCE.md`
- `Docs/LivingWorld04M/PRESERVATION_AND_RISKS.md`
- `RESUME_NEXT.md`

All handoff docs must state exact published SHA/tree, predecessor ancestry, module-by-module implementation, what was deferred, reproducible tests and known errors; distinguish visual reference images, offline mesh renders and actual Unity frames. Final response must contain PR URL, verified branch HEAD, new gameplay changes and honest native acceptance, not marketing copy.

## Directive to executing Work agent

**IMPLEMENT NOW** from the verified latest predecessor after its handoff, and keep developing through substantive actual source/art/scene/quest commits. Do not stop at a plan. Prioritize a real cohesive playable slice and fix demonstrated regressions as you go. If the environment cannot run native Unity, say so and ship independently validated source without claiming the milestone fully completed. Never rewrite prior work or merge `main`.
