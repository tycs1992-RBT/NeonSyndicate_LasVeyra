# CRYSTAL MANOR SAGA / NEON SYNDICATE — LAS VEYRA
# MASTER IMPLEMENTATION DIRECTIVE 04L: THE CINEMATIC SAGA — THE VEIL REMEMBERS

**Task type:** actual large Unity development sprint for ChatGPT Work or a repository-connected coding agent; **not** a planning-only request, a nonworking prototype, a slide deck or a reinstall.

**Theme:** Narrative performance, physical cinematics, original twelve-chapter storytelling and a coherent optional mystery.

## Exact branch contract

- **Repository:** `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`.
- **Immediate predecessor branch:** `feature/renaissance-in-motion-04k`. **At execution time** resolve that branch's latest legitimate commit SHA and verify actual completion/status. It may have advanced after this file was authored. Never use the historic 04J or initial 04K SHA as a substitute for the actual immediate parent.
- **New feature branch:** `feature/cinematic-saga-04l`; if already present, inspect parent ancestry and current unmerged work, then continue without reset.
- **Draft PR target:** `feature/renaissance-in-motion-04k`. Do not merge to `main` and do not rewrite other open PRs.
- **Unity:** 6000.3.16f1, Built-in default / optional URP, Input System 1.17.0.

**Required continuity:** Read the companion `Crystal_Manor_Saga_MASTER_BLUEPRINT_04K_to_04N.md` and the latest parent `RESUME_NEXT.md`, `START_HERE_*`, module status, test ledger and actual source/scripts. If the Master Bible / visual ZIP exists, treat it as historical visual guidance, not a replacement source project. Before implementing, confirm which requested features truly exist, are partially integrated or are absent.

## Binding engineering instructions

This is an **implementation directive for the actual, already-existing Unity game**, not a request to re-summarize it, generate a concept image, rebuild an empty demo, or return a plan instead of code. Act as a coordinated game director, Unity engineering team, technical art/rig/animation team, narrative/quest team, AI/combat team, content producer, QA and release engineer.

**Source of truth order:** (1) inspected current branch code/serialized content and Unity scene, (2) `Docs/STORY.md`, `Docs/CONTROLS.md`, exact sprint acceptance and handoff docs, (3) preserved master bible/visual-reference pack as creative guidance, (4) this directive. If a historical document contradicts code after 04I (for example city wildlife appearing only at night), current code wins. Do not invent established canon, IDs, assets, game modes, tests, screenshots or capabilities. New lore is optional additive content, not a replacement for the original 12-chapter plot.

**Do not import ideas from another game.** This is Las Veyra's story-driven single-player open-world action RPG with Free Roam, original missions, Underheart, strongholds, territories, guild quests, contracts, crystal creatures and bonded drakes. It is **not** a pre-match PvP class-pick or team-side preparation game. Deepen existing Forge/build/loadout/economy gameplay when relevant; no disconnected match lobby or competitive multiplayer framework.

**Repository:** `https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra`.

**Engine:** Unity `6000.3.16f1` revision `a56f230f6470`, Built-in renderer default, preserve optional URP compatibility, pinned Input System `1.17.0`. No HDRP migration, no gratuitous package upgrade, no project reset, no Git LFS pointer files treated as hydrated binary assets, no license bypass. This repository is not a Unity HDRP Funhouse project.

**Freeze / protect:** `main`; every earlier sprint branch and draft PR; all original assets, prefabs/scenes/GUIDs/LFS payloads; project settings; existing world seed and geometry; legacy SaveData/EventSave/GuidanceSave/LootSave and saved IDs; original `m00_*`–`m11_*` story and both ending laws; Underheart `uh05_*` content; 04G courier, 04H Quiet Circuit and 04I five Ascension chapters; boss entitlements and unique loot indices 17–22; six owned crystal focus identities, charge/heat and cell receipts; bonded `quartz_guide`, `ash_warden`, `storm_skimmer`, and fused `ventwing`, `kilnback`, `coilrunner`. Never free-grant an owned bond/focus/fusion or duplicate unique rewards. New records require additive defaults, explicit migrations and old-save regression.

**Work protocol:** Read `RESUME_NEXT.md`, current `START_HERE_*`, module status and test ledger first. Verify exact remote HEAD and ancestry, read/write authorization, working-tree cleanliness, Git LFS hydration, pinned editor and package names. If a requested next branch already exists, inspect its history and continue its legitimate head; never reset or force push. Make real edits in existing owner systems, not shadow managers or duplicate worlds. Save cohesive tested commits after every meaningful gate. Never merge into `main`; create a **draft** PR targeting the immediately preceding feature branch.

**Evidence grades:** `source-authored`, `Roslyn-syntax-checked`, `managed-API-compiled`, `EditMode-passed`, `PlayMode-passed`, `in-game observed`, `save/reload-passed`, `visually compared`, `profiled`, and `Windows-player-launched` are different claims. CI syntax/GUID checks are not Unity native import, PlayMode or measured FPS. Attempt legitimate native verification if available; if the editor/runner cannot initialize, preserve actual error, perform independently verifiable work and mark native items `NOT RUN`. A sprint is not complete merely because its prompt has ended; mark partial modules honestly.

**Default gameplay bar:** First five minutes in Free Roam must show an easily noticed improvement; the player can participate without F2/debug cheats, no unavoidable starter-area swarming, no cinematic that traps control, no inaccessible interior, no event that never ends, no lost companion or duplicate reward. Maintain appropriate creature/AI attack-token limits, cooldowns, mission ownership, ordinary observer safety and graphics presets Low/Medium/High/Ultra. Test keyboard/mouse and Xbox/PlayStation controller action parity.


---

## 24 integrated implementation modules

Each module is an actual build assignment. For every module publish (a) the existing authoritative code/content owner, (b) new source/assets and exact integration path, (c) a normal-control in-game acceptance test, (d) automated regressions where possible, and (e) status COMPLETE/PARTIAL/DEFERRED/BLOCKED with evidence. Make prioritized COMPLETE vertical slices rather than 24 disconnected C# stubs.

### L01 — Current 04K and Unity baseline recovery

**Implementation:** Discover the current 04K head, exact commits, K source modules, available logs, LFS and any native acceptance. Audit failing C# imports/PlayMode first; never assume the 04K motion-art checkpoint is finished. Record an immutable source manifest and first reliable reproduced issue before expanding story polish.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L02 — Repeatable real in-game visual evidence

**Implementation:** Create camera/scene capture recipes for Veyra Lofts, Crystal Manor ruins, Pressure House, Cinder Dispatch, Underheart and a normal combat street. Reproduce the same seed 61724, lighting, resolution and presets before/after; capture true Unity frames where possible, distinguishing imported assets, procedural meshes, concepts and rendered gameplay.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L03 — One authoritative cinematic-sequence owner

**Implementation:** Extend existing MissionManager, QuestBook and PlayerCamera rather than installing a second cutscene system. Support scheduled character beats, camera cuts, HUD/subtitle cues, timed actions, skip/cancel, mission checkpoint, camera restoration and failure cleanup. Build one complete vertical slice before distributing to 12 chapters.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L04 — Dialogue and interruptions

**Implementation:** Tie mission dialogue to the canonical actor/giver and actual world position, gaze and activity. A skip, pause, death, load, door transition or disconnecting controller must never leave cinematic state, blocked controls, stopped animation, duplicate dialogue or a lingering HUD prompt.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L05 — Readable film language for a player camera

**Implementation:** Build a small reusable vocabulary of over-shoulder, reveal, close, two-shot, tracking and pullback moves with obstruction-aware collision and near/far clip discipline. Respect manually controlled Free Roam camera and existing shake/reduced-effects settings; never force nausea-inducing motion or clip through geometry.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L06 — Malik identity and physical performance

**Implementation:** Build on actual 04K hero geometry, 04I weighted coat and existing BodyBuilder/HumanoidRig. Improve face/eyes, readable brows and jaw, hand/finger grips, grounding, body weight, stance changes, coat response and emotional idle poses using legitimate art/rig assets when toolchain allows. Check real third-person camera at gameplay distance, not just close-up mesh counts.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L07 — Luma Veyr noncombatant storytelling

**Implementation:** Preserve Luma as Malik’s young civilian daughter. Add appropriate safe fear/hope/curiosity performances, crystal-response gaze, lantern/hand gestures and family scene positioning without combat AI, adult animation or dangerous forced pathing. Rescue/escort mission failure and vehicle pillion remain intact.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L08 — Canonical supporting cast performances

**Implementation:** Give Mara, Ion, Jax, Dante, Nico, June, Iris and Valez distinct in-world silhouettes, idle language, service interactions and repeatable gestures. Preserve their existing mission and guild offers; do not replace original contacts with invented concept art characters.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L09 — Shard Warden foreshadowing

**Implementation:** Make the final steward’s presence visible through distant shard echoes, signal anomalies and selective villain performance, without spoiling the late encounter or spawning it before its original mission. The Warden’s motivations and final battle stay consistent with the Heart, Luma, Third Eye and both lawful endings.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L10 — Prologue M00: original story in living crystal world

**Implementation:** Polish Malik/Luma Manor approach, Third Eye awakening, hunter/Warden/Weaver combat, summoned crystal pack, hover-bike blockade and Mara clinic arrival. Use existing tags and mission checkpoints, make optional beasts visible and lootable, ensure the bike/Luma escape remains completable and no scene/AI failure deadlocks progression.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L11 — Chapters M01–M03: city relationships

**Implementation:** Improve Heat Mirrors, Ghost Lanes and False Kings with actual cars, NPC performances, ghost-light environmental reads, controlled chases and a clear crystal Alpha after the king. Keep story mission IDs, power unlock timings, reward amounts and faction heat authority; make the optional monster response legible without blocking a timed chase.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L12 — Chapters M04–M06: tears, betrayal and signal market

**Implementation:** Revisit Flooded Grid ambulance/canal rescue, June’s Ashline ambush and Iris’s Nocturne Market drive as one midgame story arc. Add specific environmental crystal leaks, readable gang/faction roles and recoverable objective states. Protect escorts and original fail conditions; do not convert cutscenes to text-only task summaries.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L13 — Chapters M07–M09: exposure and pursuit

**Implementation:** Upgrade Glass Saints investigation, the three Signal War transmitters and Crownless Mile truck betrayal with player-readable objectives, physical VFX, normal existing enemy spawns and crystal tears. Keep Signal War Hollows and Focus Weaver guards optional where originally designed; no infinite reinforcements or faction authority bypass.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L14 — Chapters M10–M11: family stakes and final law

**Implementation:** Improve the Long Switch van rescue, Shard Warden three-anchor boss scene and first-law choice. Preserve Luma protection, original Warden health/damage flow, Mercy vs Power game states and original ending text. Refuse new cinematics that automatically decide or silently skip the player’s ending choice.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L15 — Narrative consequence and replay continuity

**Implementation:** Add additive post-mission acknowledgments from the right existing characters and environment: who knows what at each step, which rift has closed and whether a contact has been rescued. Use a single progress-derived state lookup, not unsaved duplicate flags or new permanent story claims. Old saves after chapters 1, 6, 10 and 12 must resume consistently.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L16 — Human locomotion performance and blending

**Implementation:** Introduce real authored clips/animation data if production tools permit, or documented substantive procedural pose updates when not. Cover idle/walk/run/sprint, starts/stops, pivoting, clambering, combat approach, dodge, injury, interaction and seated/riding. No sliding feet, broken IK, twisted joints or disabled existing controls.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L17 — Combat contact and enemy performance

**Implementation:** Align Asterblade, Riftfang Pair and Basalt Crown active frames and impact reads with real weapon grip, sweep checks and wall collision. Every crystal Warden/Sentinel/Weaver/Rift Duelist needs different telegraph geometry and behavior at 8–20 m. Preserve one-hit-per-strike, poise, stamina, charge, legal cancels and fair parry.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L18 — Creature introductions and boss-scale camera

**Implementation:** Give Glass Mites, Hollow, Shade, Jackal, Golem and at least one winged creature meaningful entry/exit/motion silhouettes, readable attack build-up and hit reactions. Existing behavior and encounter budgets win over cinematics; avoid camera takeover for every ambient enemy. Document close/mid/far render budgets.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L19 — Bonded drake interaction scenes

**Implementation:** Stage one complete earned-companion interaction with Quartz Guide, Ashshell Warden or Stormskimmer through real BONDS ownership, exploration/combat assist, care and rescue. Honor existing `quartz_guide`, `ash_warden`, `storm_skimmer` IDs and fusion lifecycle; no automatically awarded creature or duplicate owned roster.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L20 — Meaningful cinematic world environments

**Implementation:** Produce art assets tied to actual world geometry: fractured Manor wall, resonance beacon, city ghost lights, clinic warmth, Ashline industrial heat, market stalls and Underheart thresholds. Existing walkable roads, cover and doors must stay reachable. Do not claim 2D vision art is a game-ready textured/rigged asset.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L21 — Voice, soundscape and musical motifs

**Implementation:** Assign distinct audio identities to Malik/Luma/Third Eye/Warden, four guilds, ambient crystal foes, major locations and story revelations. Integrate actual clean sound assets or clearly identify existing synthesis as interim; avoid unsupported claims of recorded professional VO. Ensure subtitles still convey all plot beats with audio off.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L22 — Accessibility, subtitles and UI readability

**Implementation:** Build subtitle timing, adjustable text panels, color-independent critical telegraphs, reduced flash/shake, skip/replay dialogue and controller navigation into existing HUD/menus. Keep camera shake toggles and dialogue controls consistent with `Docs/CONTROLS.md`. A player never loses navigation or an item for skipping a scene.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L23 — One optional cinematic quest bridge

**Implementation:** Create one finished 20–40-minute optional “Veil memory” investigation, using a real named giver and existing QuestBook/Underheart/venue owners, not an alternate main story. It should feature a clue, enterable investigation location, fair combat/drake interaction, return and once-only modest earned reward. Before naming it canon, validate the actor, location and new IDs against existing content.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

### L24 — Integration, native QA and cinematic handoff

**Implementation:** Run original 12 story identifiers and critical checkpoint routes, 04G/04H/04I quests, crystal loot pickup, BONDS/fusion, legacy saves, GUI/controller and profile tests. Publish recorded before/after clips only if actually captured in Unity. Supply module-by-module status, first unresolved defect and draft PR base to 04K.

**Acceptance:** Record exact affected gameplay route, physical player action, stable save/event/reward behavior, regression test(s), and native verification status. Do not call this module complete based only on data declarations or source CI.

---

## Sequential development gates, with real commits

The executing agent must progress through meaningful checkpoints in the following order. A gate is not done merely because a commit or checklist exists. Native failures are not license to rename source changes as successful PlayMode tests.

### GATE A — Repository handoff and native recovery

Prove latest 04K build and resolve first genuine engine blocker. Capture immutable baseline and protected save sample.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE B — Cinematic foundation and hero acting

Implement authoritative sequence player, camera and actor performances; deliver one actual re-playable in-world scene.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE C — First third of original campaign

Improve M00–M03 and prologue crystal enemies with real shot, animation and interactive acceptance; retain story rewards.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE D — Midgame scenes and creature performance

Complete M04–M08 mission polish, crystal leaks, physical assists and boss-scale acting.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE E — Final story and First Law

Polish M09–M11 and both Mercy/Power endings without changed canon, gated power entitlements or unearned credits.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE F — Optional Veil memory story + audio/UX

Finish a compact new physical optional investigation, score/cues, subtitle, skip and accessibility.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE G — Full regression and actual on-device review

Try all twelve canonical story IDs, endgame law paths, old saves, owned drakes, original routes and input devices.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

### GATE H — Evidence and draft PR

Push source/art evidence and playtest route; PR 04L → 04K, deliver exact SHA and 04M ready handoff.

**Gate receipt:** a source commit with module and affected scene/quest owner, test run identifiers, before/after evidence if available and a clear next-blocker note. If an Editor cannot start, checkpoint independently tested source and mark native acceptance pending.

## Non-negotiable regression matrix

- **Original campaign:** M00 through M11 using actual mission IDs. Prologue and prologue-skip, early powers, crystal mob waves M00/M03/M08, family Luma protection, final boss anchors, Mercy and Power endings.
- **Underheart and late content:** original 04E 16-main/12-side quests and eight contract grammars where available, acquired BONDS/FUSION, Underheart rooms/bosses, 04G courier, 04H Quiet Circuit, five 04I Ascension chapters, and latest complete 04J/K/L/M work in the current predecessor tree.
- **Systems:** Asterblade/Riftfang/Basalt Crown, all six focus signatures, player/faction/NPC protection, bounded creature spawn/cooldown, drake injuries and handler ownership, gear/crystal UID and boss loot, crafting costs, save/restore from old saves, world transitions, dialogue/mission abort/death/quit, services and enterable interiors.
- **User experience:** normal Free Roam first five minutes, gamepad and mouse/keyboard, readability, accessibility/reduced effects, Low/Medium/High/Ultra parity, daytime/night, original vehicles and optional URP path.
- **Tool truth:** count actual NUnit passes, failures and exclusions; compare names against separately executed parent baseline. Syntax/GUID/LFS checks are not Unity native import. Measure FPS/GC only from running Editor/player. Never invent screenshot, binary build or commercial quality.

## Player acceptance route required for this sprint

Create a step-by-step route in `START_HERE_04L.md` that a nondeveloper can follow from the actual regular Free Roam menu on both a fresh and copied legacy save. Begin with observable art/gameplay improvement at the existing arrival within 60 seconds, follow with meaningful 5-minute encounter/interaction, then a finished longer integrated sequence (mission/event/loot/interior/exit) without debug teleport. Include equipment, a combat/creature contact, real claimed or paid reward, legal abort/retry/save/quit/reload and a calm world afterwards. Reference precise original controls, known world entrance names and any genuinely new optional content IDs. Fail if inaccessible or scripted actors disappear, if companions get lost, or if rewards duplicate.

## Required tracked GitHub handoff files

- `START_HERE_04L.md`
- `Docs/CinematicSaga04L/FEATURE_STATUS.md`
- `Docs/CinematicSaga04L/SHOT_LIST_AND_ACTING.md`
- `Docs/CinematicSaga04L/ART_ANIMATION_PROVENANCE.md`
- `Docs/CinematicSaga04L/STORY_CONTINUITY.md`
- `Docs/CinematicSaga04L/TEST_LEDGER.md`
- `Docs/CinematicSaga04L/VISUAL_EVIDENCE.md`
- `Docs/CinematicSaga04L/PRESERVATION_AND_RISKS.md`
- `RESUME_NEXT.md`

All handoff docs must state exact published SHA/tree, predecessor ancestry, module-by-module implementation, what was deferred, reproducible tests and known errors; distinguish visual reference images, offline mesh renders and actual Unity frames. Final response must contain PR URL, verified branch HEAD, new gameplay changes and honest native acceptance, not marketing copy.

## Directive to executing Work agent

**IMPLEMENT NOW** from the verified latest predecessor after its handoff, and keep developing through substantive actual source/art/scene/quest commits. Do not stop at a plan. Prioritize a real cohesive playable slice and fix demonstrated regressions as you go. If the environment cannot run native Unity, say so and ship independently validated source without claiming the milestone fully completed. Never rewrite prior work or merge `main`.
