# Visual Renaissance 01C: QA report

Build scope: the complete 01B project plus the 01C playtest corrections. No replacement world or empty scene was created. This report separates offline evidence from Unity acceptance. Final source and packaging evidence accompanies this file.

**Release status: implementation ready for a Unity review session, not playtest-certified.** Unity Editor, a C# semantic compiler, a shader compiler and a graphics-device session were unavailable in this environment. No acceptance goal below has been claimed as passed.

## Final offline results

| Check actually performed | Result |
| --- | --- |
| Exact comparison with completed 01B | 717 original files checked; 682 byte-identical, 35 reviewed modifications, no deletions or changes outside the reviewed scope |
| New Unity assets | 15 new assets and their 15 metadata files explicitly reviewed; no unexpected additions or missing new metadata |
| Model, texture and import metadata structure | 24 OBJ models, 32 textures and 97 unique GUIDs checked; zero reported errors or warnings |
| C# grammar under conditional compilation | All 12 configurations passed grammar checks; 342 editor source files and 297 player source files per configuration; not semantic compilation |
| Vehicle equations | Source-expression steering/yaw/lean checks passed at 25, 50 and 100 Hz; not Unity physics |
| Practice burn cap | 20 float32 arithmetic cases retained positive health; not runtime combat |
| Live Unity acceptance | All seven goals below remain NOT RUN |

## User acceptance status

| Requested goal | Status | Implemented support | Required live test |
| --- | --- | --- | --- |
| Peaceful starting district for at least five minutes unless combat is initiated | NOT RUN | Persistent welcome-area policy; ambient gang and both creature-brain gates; random event, chest-guard and automatic Manor-boss gates | On Story, walk for five minutes from both Free Roam and post-prologue starts without attacking or accepting a challenge. Repeat after save/load. Record attacks, wanted state and console output. |
| One manageable starter encounter without police immediately joining | NOT RUN | Optional single-jackal practice, reduced damage/health, shared combat pressure limits, local practice-noise exemption and slower budgeted police response | Observe the sanctuary, deliberately start practice, exercise melee/ranged/burn attacks and finish or cancel. Confirm no unsolicited police/gang recruitment and no kill/loot reward. Also damage an ordinary actor to confirm real crimes still count. |
| Controlled sharp turns at low speeds | NOT RUN | Increased low-speed steering lock, faster steering response, grip-bounded yaw and grounded assistance | Test compact car and a heavier vehicle at walking speed and 5/10 m/s, forward/reverse, keyboard and gamepad; compare the same corners with 01B. Check braking, handbrake and collisions. |
| Motorcycles turn and recover from leaning | NOT RUN | Bounded corner support, low-speed upright bias, reduced visible bank, quicker return and physical roll recovery | Test repeated left/right turns, releasing input mid-turn, reverse, stop/start, bumps and collision recovery. Check rider pose and camera in both viewpoints. |
| Several magical creatures found and interacted with within two minutes | NOT RUN | Requests three calm residents within 62 m of the actual free-exploration start; sanctuary blip; existing observation/bestiary interaction | Start a two-minute timer when free exploration begins. Find and observe three loaded residents. Check default and alternate seeds, fallback species, occupied paths and body-build failure handling. |
| Upgraded district visibly differs from original city | NOT RUN IN UNITY | Larger pointed arches, full-height mineral piers, embedded crystal fins and crowns on existing starter frontages; retained 01B materials and crystal shading | Take matching 01B/01C screenshots at home, clinic and promenade with the same time/weather/FOV/quality. Judge silhouette, surface depth, exposure and creature visibility. |
| Compiles and runs without new critical errors | NOT RUN | Source grammar, asset structure and exact baseline-preservation checks supplied separately | Import in Unity 6000.3.16f1, run EditMode tests, enter Play Mode and build the desktop player. Inspect console, shader/import logs and session/reset behavior. |

The two-minute habitat target begins after the preserved authored prologue has ended. Existing saved difficulty preferences remain; explicitly select Story when testing the welcoming balance. Protection does not remove authored mission enemies, erase wanted levels or override a manually accepted challenge. Deliberate aggression temporarily opens a local 60-second response window.

## Offline evidence and its limits

- `VALIDATION_SUMMARY_01C.json`: final validation status and explicit NOT_RUN runtime categories.
- `STRICT_PRESERVATION_01C.json` and `MASTER01B_CHANGES.diff`: exact comparison with the completed 01B checkpoint, with a reviewed list of allowed original-file changes. This establishes file preservation, not unchanged runtime behavior.
- `CSHARP_SYNTAX_12_CONFIGS_01C.json`: tree-sitter grammar checks after evaluating Editor/Windows, Built-in/URP and classic/both/new input conditionals. This cannot establish C# member/type resolution or Unity compilation.
- `OFFLINE_ASSET_VALIDATION_01C.json`: OBJ geometry, texture decoding, import metadata, GUID uniqueness and manifest checks. Unity importer behavior and GPU shader results still require Unity.
- `VEHICLE_NUMERICAL_CHECKS.json`: source-expression mathematical evaluation at 25, 50 and 100 Hz. This checks steering/yaw bounds and lean convergence; it does not simulate WheelColliders, Unity physics, contacts or driving feel.
- `ARCHITECTURE_SOURCE_QA.json` and `FRONTAGE_SOURCE_COMPARISON.json` in `Docs/ArtDistrict01C`: source geometry and placement reconstruction. The example has fewer repeated parts and triangles, but total scene draw calls and frame time have not been measured.
- `VISUAL_REPORT_FILE_CHECKS.json`: embedded report images decode and dimensions match. The report was not rendered in a browser here. Reference images and source reconstructions are clearly labeled; there are no invented Unity screenshots.
- Police, combat, ambient-director, vehicle and habitat audits identify exact source paths, implemented behavior and review limitations. Their earlier per-file snapshots are supplemented by the final complete-project checks.

## Regression checks still required in Unity

1. Run all existing EditMode tests, including save schema, quest/event data, bodies/poses and graphics math, plus new StarterSafetyTests, CombatPressureTests, PoliceResponseTests, AmbientEncounterTests, PlayerVehicleHandlingTests and ArtDistrictStarterSelectionTests.
2. Compile all twelve supported configurations: Editor and Windows player; classic input, Both and Input System only; Built-in and the existing optional URP path. Begin with the shipped Built-in pipeline. No new package is required by 01C.
3. Replay prologue, prologue skip, a combat mission, an authored pursuit and a manually accepted event. Check mission contacts, rewards, progression, wanted escalation/evasion and saves. Test automatic chest guards and manual challenges separately.
4. At every difficulty, sustain a pursuit with car crews and bikes, move more than 260 m away and return. Verify the shared officer cap, first-response delay, seated/distant crew accounting, pause and delayed witness calls.
5. Exercise sanctuary observation, practice start/finish/cancel, area damage, high damage multipliers, burn, a mission starting, timeout, leaving the area, body-load failure and return to title. Repeat without domain reload. Confirm no duplicate blips, lingering attacks, stale interactables or reward farming.
6. Inspect imported models, normal maps, crystal/drake shaders and LOD transitions at home, clinic and promenade. Check first-person/third-person camera clearance, doors, cover and collision alignment. Review HUD, prompts, bright emissions and spell telegraph visibility at 1080p and other target aspect ratios.
7. Profile the existing performance route and densest starter view with creatures present on target desktop hardware. Record CPU/GPU frame time, memory, draw calls, shadow cost and frame spikes under driving/combat. No FPS target is certified by this package.

## Remaining visual and production limits

The project includes implemented source meshes, textures, shaders and runtime placements. It still needs artist-authored hero humanoids/creatures, polished animation clips, a production facade/surface kit, detailed forest and realm assets, Manor room variants and an authored spell family. The sanctuary reuses existing species and rigs. Observation is a bestiary interaction, not a new ownership/breeding/companion system. Crystal rendering is not physical transmission or a full volumetric-lighting solution.

See `REMAINING_PRODUCTION_ASSETS.md` and `VISUAL_COMPARISON.html` for the prioritized handoff. The final cinematic art direction remains a production target pending real Unity visual review.
