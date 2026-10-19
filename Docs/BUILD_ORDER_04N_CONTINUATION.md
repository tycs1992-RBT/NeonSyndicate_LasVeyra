# Master Build Order: 04N Continuation
# Crystal Resonance and the Living World Vertical Slice

Status: EXECUTION DIRECTIVE for the existing, active 04N implementation. This is not a request to restart 04N or open 04O.
Reference source snapshot: 802b66cd31e80f148a462b9ad61eda476a1c25a0. Owner must fetch the live head again at execution time.
Existing gameplay branch: feature/crystal-kingdom-awakens-04n, originally from 04M f9655c2c4373b6ef6efffee21f49f364e70ea765.
Current documentation proposal: docs/04n-resonance-bible-blueprint-20261019. Do not overwrite the gameplay branch with this docs branch or use stale 04M as a new base.

## A. Your immediate objective

Ship one convincing end-to-end normal-player experience which PROVES the systems interact, rather than broadly labeling 24 separate modules complete.

The slice starts with Malik entering a visually polished portion of the existing Las Veyra Core / Cinder Dispatch / Sunken Row route, uses genuine source-authored combat and the earned BONDS system in a clear caster-led creature fight, leads to a legitimate guild/Forge consequence, and includes one contiguous real Underheart transition. Finish with save/reload and native visual/profile evidence if the environment supports it.

Target first: three actual distinct companion Synergy Arts integrated with the existing gun-to-crystal-to-Burst system, not dozens of names that share one generic particle pulse.

## B. Source state you MUST preserve

Source implementation already exists for:
- Distinct city structures and hero tailoring, commit 46ccd630.
- Caster role and CityRift eligibility changes, commit ecd98d0f.
- ResonanceChains04N, ResonanceSystem.Chains04N, BondSystem.Resonance04N, real chain patterns, three partner art names, prompt options and milestones, commit 802b66cd.

Do not regenerate these owners or redo Gate A/B as if nothing exists. Read source and tests once, identify actual remaining functional gaps, then continue from current live 04N HEAD. The current root RESUME_NEXT.md was stale and still described 04M at time of docs preparation; update that handoff correctly on the gameplay branch after implementation.

04M trusted reference tests: 1416 external passes, 46 known engine dependent failures, 58 native exclusions, 27 new cases passing, 12 compile configs. These are not fresh 04N test results.

## C. Priority work, in strict dependency order

### 04N-C1: reconcile actual live source and tests

- Confirm branch is feature/crystal-kingdom-awakens-04n, check latest HEAD and any live work, existing remote changes and any existing PR.
- Read CLAUDE.md and affected combat/companion/city owners. Never change a protected story ID or legacy save field.
- Run the available fast compile and source test matrix on the exact current 04N source once if facilities exist. Record new tests and failure names separately from 04M.
- Check whether 04N existing four chain recipes, three companion arts, controller prompts, known preset configurations and Unity metadata have regression coverage.
- If native Editor available, attempt one licensed boot/import and capture actual first error. If same known IPC blocker persists on the same environment, record NATIVE_NOT_RUN and continue source work. Do not loop on licence setup or bypass.

Output: current source status, issue list, minimal tests, commit checkpoint. No speculative rebuild.

### 04N-C2: finish Crystal Resonance end to end

- Preserve real original Burst. Chain recognition consumes confirmed actual hits with unique action serial, not raw button commands.
- Exercise Gun/Gun/Melee, Melee/Gun/Melee, Gun/Gun/Focus/Gun and Counter/Melee/Focus sequences with current recognizer. Test shared prefixes, wrong order, timeout, duplicate receipts and target-local lifetime cap.
- Make ready prompts stable, readable and contextual, and verify no collisions with existing Act/Pad bindings, Pause, Veil shift and menus. Update Docs/CONTROLS.md if a real binding changes.
- Complete eligibility, limited cost expenditure, camera/assist reservation and once-only impact/cleanup.
- Test three original owned archetypes: quartz_guide, ash_warden, storm_skimmer. No wild-creature auto unlocks and no free focus grants.
- Test both manual and automatic confirmation, generous timing, miss fallback, target death, LOS/range loss, damage interruption, Dodge, Pause and load.
- Ensure generated art effects and actual ability mechanics differ for Cataclysm Wing, Aegis Breaker and Prismatic Snare.
- Integrate Focus effects only when legitimately owned. Preserve original weapon focus, BONDS and damage owners.

P0 acceptance: normal play can legitimately make a ready chain and activate each eligible owned archetype art; every failure restores control and neither duplicates damage nor corrupts saved state. Source-only validation must be labelled appropriately if Unity is blocked.

### 04N-C3: make combat visually and physically believable

- Align shot tracer, gun muzzle, weapon grip, melee active contact and actual applied damage.
- Ensure target guard/armor/poise and weak point behavior remain compatible with existing Resonance Burst.
- Add clear, distinct wind-up and impact posing; actual weapon/companion pose alignment; target reactions, restrained hit emphasis, audible impacts and short recovery.
- Produce at least three authored action timelines or procedural sequences with unique camera framing and attack identity, rather than three names over identical hit effects.
- Preserve 04L camera lease/Pause/skip lifecycle, avoid clipping through doors, provide fallback camera when no safe space.
- Do not add untested physics sweeps or change hit envelopes indiscriminately; use actual test cases and native diagnosis.

P0 acceptance: one real melee/ranged/caster exchange looks coherent in captured Unity footage, or remains clearly flagged as native blocked with source-level integrity proven.

### 04N-C4: crystal creatures and drakes in a real combat encounter

- Finish the existing caster as a real visible opponent using the current CreatureDirector and CityRift admission.
- Confirm line of sight, grounded placement, range and actual telegraphed caster attack. Preserve finite budgets, starter-region safety, contribution, retreat and no-repeat payout.
- Give one normal mixed-horde route real opposing roles and fair attack arbitration.
- Use existing owned drake combat agent and canonical bond/recall. Improve independent target selection, hurt/retreat/return and corridor collision only where needed.
- Test an encounter with and without a companion. Make sure both are winnable on welcoming default difficulty.

P0 acceptance: one complete fight from normal Free Roam with hostiles and drake/companion, valid damage, player control, visible consequence and clean teardown.

### 04N-C5: one premium surface route, one premium Underheart route

- Pick one contiguous existing surface corridor through Las Veyra Core, Dispatch and/or Sunken Row. Keep entry points, nav surfaces, roads, stairways, clinic/guild host, and real NPC routines accessible.
- Finish shape language and hero geometry: facade silhouette, doors, canopies, street layers, convincing materials, coherent lighting, effects only after geometry. Compare to source 04N CityKingdomGeometry04N and existing art bible.
- Refine actual Malik/companion silhouette and animation where player camera sees them. Focus on hands, shoulders, body balance, weapons, legs and close-up facial proportions.
- Upgrade ONE real Underheart traversal sequence in the Gardens / Workshop while preserving the original boss/cell logic.
- Add collision only to intended traversal surfaces, preserve low LOD and Built-in/URP implementations.
- If a dedicated modeled/skinned art asset pipeline is available with lawful provenance, integrate real import and rigging, not only offline OBJ previews; otherwise produce the strongest credible procedural improvement and mark remaining AAA-scale art as pending.

P1 acceptance: player can traverse the corridor and Underheart path without hitting invisible obstruction, and two real, matched Unity camera frames show an obvious qualitative difference. No native capture means visual acceptance remains NOT RUN.

### 04N-C6: demonstrate connected RPG consequence

- Choose a legitimate WorldEvents / QuestBook event that touches the caster/creature conflict.
- Use real existing receipt/cooldown, valid materials and existing guild/Forge host. No new fake menu.
- Complete a board job or finite recovery, actually earn the item/material, pay a Forge cost, apply an observable gameplay/gear treatment, then play a follow-up encounter in which it matters.
- Save, restart, reload, confirm ownership/treatment and exhausted receipt. Test full inventory and abandon/retry.
- Do not add inventory, merchant or reward managers. Preserve economics and earlier scripts.

P1 acceptance: at least one directly player-visible chain of world -> guild -> crafting -> combat -> progression -> save, with no duplicate reward or arbitrage.

### 04N-C7: compatibility and release truth

- Retest original M00-M11, two First Laws, 04G courier, 04H Quiet Circuit, 04I chapters, 04K Fractured Bell, 04L Veil investigation and 04M routes to the extent environment permits.
- Cover role-specific Guild/Forgewrights, original drakes and fusion, saved focus ownership, original camera and changing district/Veil/Underheart.
- Avoid inconsistent multiplayer shortcuts: document a transport-neutral reservation/receipt contract, but do not build a network lobby/replication stack into 04N.
- Run 12 managed API configs, three assembly boundaries, current external NUnit, metadata/source preservation and LFS checks. Compare actual newly failing names, not raw totals alone.
- On a licensed Windows Unity host: import, EditMode and PlayMode, real play, save/reload, 12 representative screenshots, profile, build and launch the Windows development binary.
- If native cannot run, preserve the exact current-source test evidence and label individual acceptance rows NOT RUN. Never rename source passes as native passes.

Output: one honest, recoverable draft PR on 04N targeting 04M, complete handoff and remaining work.

## D. Gate checkpoints, not independent fake milestones

1. N-C1 SOURCE_RECONCILED: exact commit, previous functionality and test result recorded.
2. N-C2 RESONANCE_INTEGRATED: full four existing patterns and three companion actions have real path and source tests.
3. N-C3 COMBAT_PRESENTED: actual meaningful 3-art presentation and hit/camera integration.
4. N-C4 CREATURE_ROUTE: visible finite caster encounter and companion behavior.
5. N-C5 WORLD_SLICE: attractive safe player route to meaningful interior/Underheart.
6. N-C6 RPG_LOOP: guild, materials, Forge, progression and receipts form a complete activity.
7. N-C7 SOURCE_QA: compilers, tests and preservation manifest.
8. N-C8 NATIVE_ACCEPTANCE or NATIVE_BLOCKED: full evidence or exact reason missing.
9. N-Z HANDOFF: updated docs, source SHA, new tests, and draft PR.

Prefer first proven integrated routes over more broad optional features. If one gate cannot be native verified due to host limits, continue independent source implementation and keep the disposition honest.

## E. Release gate matrix

Each feature has SPECIFIED, SOURCE_IMPLEMENTED, MANAGED_TESTED, NATIVE_IMPORTED, NATIVE_PLAYTESTED, VISUAL_ACCEPTED, PERFORMANCE_ACCEPTED, WINDOWS_BUILD_VERIFIED as separate fields.

Highest-value tests:
- Recorded semantic trace determinism, accepted tokens only, event serial idempotency, wrong order, timeout and reset.
- Existing Burst + new chain entire loop with charge/cost/mark/prime/detonation.
- Three owned partner types versus invalid companion, dead partner, lost target and range/layer/LOS.
- Only-one-commit damage and spend, cancellation after charge, pause, load and scene transition.
- Prompt and HUD parity for keyboard, supported controllers and remapped binding labels.
- Caster LOS/cooldown/interrupt, horde threat admission, body budget and no duplicate rewards.
- Bond milestones, focus entitlement and older 04M save.
- Material depletion and guild/Forge transaction once-only behavior.
- Clean Game.ResetStatics without duplicate HUD overlays/subscriptions or stale target IDs.
- Built-in + optional URP and three input settings for both Editor and Player.
- Source CI and, separately, native Unity/Windows reproducibility.

## F. Evidence and files

Update/create on the gameplay branch:
- START_HERE_04N.md
- RESUME_NEXT.md (current 04M text must be superseded without erasing history)
- Docs/CrystalKingdom04N/FEATURE_STATUS.md
- Docs/CrystalKingdom04N/TEST_LEDGER.md
- Docs/CrystalKingdom04N/GATE_RECEIPTS.md
- Docs/CrystalKingdom04N/VISUAL_EVIDENCE.md
- Docs/CrystalKingdom04N/CRYSTAL_RESONANCE.md
- Docs/CrystalKingdom04N/INTEGRATION_GUIDE.md
- Docs/CrystalKingdom04N/NATIVE_ACCEPTANCE.md

For each module mark source owner, actual player entry, source test, native evidence, incomplete assets, evidence paths and honest status. Record exact commit SHA per checkpoint. Store captured files and logs instead of asserting screenshots were taken.

Keep source changes focused, do not merge into main, do not rewrite earlier sprint branches, and no placeholder screenshots or invented profiler metrics.

## G. Stop condition and following sprint

04N is production-accepted only after three genuinely distinct real Synergy Arts, one surface/caster/drake/Underheart route, one connected Forge progression, old saves, original mission invariants, real captured native gameplay/performance and Windows build pass.

If native remains blocked, deliver source checkpoints with PARTIAL and explicit blocked gates rather than declaring a commercial-quality finish.

Only after the honest 04N handoff should 04O be scoped. Provisional future 04O direction: full character/creature animation production, larger polished districts, expanded unique partner moves and actual network feasibility prototype if hardware and native QA permit.
