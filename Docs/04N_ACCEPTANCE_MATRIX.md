# 04N Test, Acceptance and Risk Matrix

Document status: planned verification protocol. Native execution not certified by creation of this file.
Audit snapshot: 802b66cd31e80f148a462b9ad61eda476a1c25a0, 2026-10-19.

## 1. Verification status taxonomy

SPECIFIED: design exists.
SOURCE_IMPLEMENTED: source integrated with the real owner and a normal-player route.
MANAGED_TESTED: tests executed against exact source SHA and outcome recorded.
NATIVE_IMPORTED: Unity 6000.3.16f1 imported real project on a licensed supported host.
NATIVE_PLAYTESTED: real gameplay interaction played and log captured.
VISUAL_ACCEPTED: matched screenshots/video accepted after inspection.
PERFORMANCE_ACCEPTED: measured CPU, GPU, memory and frame spikes against stated target PC.
WINDOWS_BUILD_VERIFIED: actual Windows development player built and launched.

A feature can be SOURCE_IMPLEMENTED while the other stages remain NOT RUN. A single boolean "complete" is disallowed.

## 2. Source baseline

Reported 04M final external NUnit: 1416 passes / 46 engine-only failures / 58 exclusions; 27 new 04M case passes; 12 managed API configurations and 3 assembly boundaries passing. The raw NUnit report itself remained Failed. Treat these as historic 04M evidence, not a current 04N success. Obtain actual 04N test results and compare failing test names/reasons.

Observed 04N source checkpoints show authored game code and 04N EditMode tests, but no independent licensed Unity native acceptance result has been established here.

## 3. Mandatory cases

| Group | Exact acceptance |
|---|---|
| Controls | Keyboard/mouse and supported pad produce equivalent semantic actions; bindings show actual device labels |
| Chain | Four current recipes match only valid order; wrong order and timeout fail |
| Receipts | One gun shot / pellet burst / melee attack produces at most one qualifying chain receipt |
| Target | Civilian/protected targets and invalid/dead targets grant no chain credit |
| Meter | Original Resonance Burst mark/prime/detonate and new chain cost work in one fight |
| Resource | No negative meter, no spending twice, no invalid companion art for free |
| Bond | Three owned archetypes have actual eligible assist; unowned, inactive or busy partner rejected |
| Focus | Earned focus/behavior check; no forged item or stale UID grants free focus |
| Camera | Blocked anchor, collision, Pause, cutscene, death, load and Veil transition restore camera/control |
| Prompt | Default, generous, automatic and miss paths resolve safely |
| Milestones | Original CompanionRecord milestones do not duplicate or reset after reload |
| Enemy | Caster ranged LOS, target damage, interrupt, cooldown, finite body budget and retreat work |
| Ecology | Day/night CityRift rejected-placement reasons are inspectable and supported ground is used |
| Drake | Legitimately owned drake can navigate, assist, fight, take permitted damage and recover |
| City | One corridor has real revised geometry without collision/entrance regression |
| Underheart | Normal player can enter, perform an interaction and exit, even with bonded companion |
| Economy | Contract/material/Forge transactions charge/grant once, preserve receipts and save state |
| Legacy | Two copied pre-04N saves retain original story law, progress, equipment, BONDS and Forge |
| Source build | 12 defined managed compile configurations and 3 assemblies pass current source |
| Unity | Native EditMode, PlayMode, scene entry and player save/restore pass |
| Graphics | Real 12-shot capture proof exists for both visual and animation assessment |
| Performance | Profiler trace contains CPU/GPU and frame budget for city, Underheart and horde |
| Release | Windows development binary built and launched with a normal play route |
| Future co-op | Only authority contracts exist; no current multiplayer success claim |

## 4. Deterministic trace suite

Recorded token examples:
- Valid: Gun -> Gun -> Melee, expected twin_fang ready.
- Valid: Melee -> Gun -> Melee, expected riftstorm ready.
- Valid: Gun -> Gun -> Focus -> Gun, expected prism_volley ready with earned focus.
- Valid: Counter -> Melee -> Focus, expected phantom_reversal ready.
- Invalid: Gun -> Melee -> Gun, must not falsely match twin_fang.
- Timeout: break gap beyond 3.2 seconds; old sequence expires.
- Ready expiry: no accept past real ready timestamp.
- Duplicate: same serial on multiple pellets, enemy parts or splash never grants duplicate credit.
- Protected target: damage or buttons on protected civilian cannot create ready move.
- Overbudget: exceed target-local two-chain life cap; cannot farm by retry.
- Burst integration: original mark and charged crystal resource remain consistent.

Test exact function behavior and player event hookup separately. Unit tests proving the pure recognizer are not proof that gameplay producers emit tokens.

## 5. Full normal-player route

1. New Free Roam, original arrival. Confirm world, player, camera, services and prompts initialize.
2. Traverse enhanced facade route into existing Dispatch / Sunken Row clinic; distinguish building identity and talk to service host.
3. Obtain or load a legitimately earned companion through original BONDS route.
4. Outside starter protection, encounter mixed normal creature group including the caster; verify read, attack, dodge and caster interrupt.
5. Use real gun and melee damage to complete each eligible chain; perform three companion-specific Arts across valid earned partner states.
6. Test missed timing, unavailable target, blocked sight, player stagger and Pause recovery.
7. Travel a real Underheart path, use a legitimate source or puzzle and leave.
8. Complete one real guild contract or event, carry earned materials to Forge, pay an actual treatment and verify effect.
9. Save, exit application, reload, verify bond, focus, mastery, item, quest and original story progress.
10. Repeat critical route from two copied pre-04N saves. Verify original M00-M11, First Law and preserved older side quests as practical.
11. Capture matched native images and performance.
12. Build and launch Windows player, repeat smoke test outside Editor.

No debug teleport, artificial entitlements or fabricated quest completion may count as normal-player acceptance.

## 6. Minimum evidence per gate

For every published gate add:
- Commit SHA, changed source and owned entry points.
- Test invocation and exact raw result link/artifact.
- New/fixed/excluded case names.
- Preservation and metadata/LFS report.
- Player-route steps with success/failure.
- Native status separately from managed tests.
- Screenshot and profiler file paths if really captured.
- Known defects, incomplete visuals, unresolved host blockers.
- Next responsible action, not invented acceptance.

## 7. Licensing and host constraints

Previous licensed Editor execution on a restricted host failed during licensing/UPM IPC and lacked actual Windows player validation. Do not retry the same unchanged bad host endlessly or bypass licenses. Source implementation can proceed when native is unavailable. Gate native results remain NOT RUN until a working supported machine is used.

## 8. Top risks and action

Critical: no native runtime feedback. Mitigate by explicit blocked status, separate source continuation and first real licensed host acceptance.
Critical: overexpansion into network gameplay. Mitigate by transport-neutral single-player contracts only.
High: contradictory saved progress. Mitigate through append-only SaveData, existing owner, real old-save fixtures.
High: multiple resource systems. Mitigate by keeping original Burst and existing CrystalCombat charge authority.
High: art promises unsupported by game assets. Mitigate via real rig/material/animation import or improved generated geometry plus actual visual gate.
High: camera and companion collision. Mitigate via line/layer/volume validation, timeouts and fallback presentation.
High: loss of player agency. Mitigate by semantic chains, optional prompts and normal combat viability.
High: performance during crowded fights. Mitigate with finite threat tokens, AI/animation LOD and measured budgets.

## 9. Final criteria

The phrase "04N complete" is reserved for an integrated normal-player route with three distinct synergy Arts, convincing contact, caster/drake fight, improved surface and Underheart visual/gameplay, legitimate guild-to-Forge progress, save compatibility, correct keyboard/controller response, and real native Unity and Windows QA. Otherwise state SOURCE_PARTIAL and exactly which acceptance rows remain open.
