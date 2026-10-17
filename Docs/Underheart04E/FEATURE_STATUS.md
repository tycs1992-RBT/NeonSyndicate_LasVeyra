# Underheart and Bonds 04E: feature status

Tested revision: `9315fe4` on `feature/underheart-bonds-04e` (base `0dbfc0b`, the 04D head). "Offline" means the
twelve-configuration compile against assemblies built from the Unity 6000.3.16f1 reference source plus the NUnit suite
under .NET 8. **Native Unity: NOT RUN for every row.** Nothing below is proven to look, feel or play right.

Legend: **Implemented** = source exists, is wired into the normal game and compiles; pure rules have passing offline
tests. **Partial** = works but a named piece is missing. **Authored only** = data or text exists, nothing reads it.
**Not started / cut** = named plainly.

## Implemented

| Feature | Source | Entry path in play | Remaining defect or risk |
|---|---|---|---|
| Named quest-givers with posts, overhead state, dialogue | `Gameplay/Guidance04E/CampaignDirector*.cs`, `GuidanceRules04E.cs`, `CastIds04E` | Free Roam: Nessa Vale near the Lofts door; giver marker, map and journal share `GuidanceRules04E` | Posts are checked for floor and room at runtime; untested in Unity |
| Lead for a new start and for an old save (dismissible, no teleport) | `CampaignDirector.Session.cs` | Free Roam start or load: session chip, lead line, Track | |
| Map and floor guidance (entrance, stairs, underground) | `UI/MapOverlays.cs`, `CampaignDirector.Map.cs`, `WorldBounds` | Track a target: route via doors and stair; underground targets never point through a ceiling | Route hint visuals unverified |
| Four new interiors | `ArtInhabitedCity04C/ArrivalScene04E*.cs`, venue layouts and `venues.json` | Cinder Dispatch, Pressure House, Bondkeeper's Refuge, Jax's garage bay | Clearance checked offline (12 venues bind on seed 61724) |
| Early build choice and starter ward | `Gameplay/Builds/*`, `UI/BuildPage.cs`, `GadgetSystem.Ward.cs` | `uh05_01` fitting worktable, then K (pad: pause > BUILD) | Starter ward now survives a load (fixed in `9315fe4`) |
| Reward ledger (once per token, pending claims) | `RewardCatalog04E.cs`, `RewardDelivery04E.cs`, `CampaignRules04E.cs` | Banner, rewards chip, BUILD > REWARDS | Unknown ids are refused and stay pending |
| The Underheart: six region variants, stair and vehicle portal, cells, shortcuts, hazards | `World/Underheart/*`, `WorldApertures`, `UnderheartPlanner` | Pressure House stair (on foot); portal for vehicles when planned | Lighting, fog and audio feel unverified; light budget not measured. Beside an active boss fight the region keeps one practical light fewer (7, or 4 on Low; `UnderheartLightBudget.cs`) |
| Kill planes, ground and camera for negative depth | `WorldBounds`, edits in player, NPC, creature, vehicle, ragdoll, projectile, camera | Automatic underground | |
| Bonded companions (3) with follow, wait, recall, assists, care, ranks, rescue, naming | `Gameplay/Bonds/*`, `Creatures/Companion*`, `UI/BondsPage.cs` | `uh05_03_bond` rescue; N and U; pause > BONDS | Companion movement in tight spaces unverified |
| Creature-vehicle fusion (Ventwing, Kilnback, Coilrunner) on the owned chassis | `Vehicles/Fusion/*`, `Gameplay/Garage/*` | `uh05_05_chassis` at Jax's bay; hold N in your own bike | Handling feel unverified |
| Companion and fused damage on bosses, their lures and furnace vents | `UnderBossFight.Assist.cs`, `UnderheartBosses.TryAssistTarget`, `UnderBossFight.Lock.cs` | Any 04E boss fight | Companion strikes on the body land at the boss's lock point while you are locked on to the boss (the lock point sits on the exposed part the damage rules score highest right now, for example a weak point or an open core), otherwise at the body's centre; the body's hit volume nearest that point takes the hit. Fused area hits strike the body's hit volume nearest the blast centre. Lures and furnace vents are hit directly (companion: aimed within 3 m; fused: inside the area). So a weak point takes companion damage only through lock-on, and fused damage only when it is the nearest volume. Added in `9315fe4`, lock point from the lock-on pass |
| Underground ecology (9 species), elites, residents | `Creatures/Underheart/*`, `CreatureSpecies` appended ids | Underheart regions | |
| Bestiary labels for the 9 underground species (region "The Underheart", where found, day and night, residents) | `Creatures/Underheart/UnderheartBestiary04E.cs`, `UI/BestiaryPage.cs` | Pause > BESTIARY | Cross-checked offline against the shipped rosters; page layout unverified in Unity |
| Four bosses (Valve Guardian, Cinder Matron, Vaultbreaker, Kiln Regent) with first-win tokens | `Creatures/UnderBosses/*` | `uh05_04`, `uh05_08`, `uh05_11`, `uh05_15` | Attack readability unverified |
| Lock-on, pad aim assist and damage numbers on the 4 bosses (body and weak points; companion and fused hits) | `UBossLockProxy04E.cs`, `UBossLockRules04E.cs`, `UnderBossFight.Lock.cs`, hooks in `PlayerCombat` and `PlayerCamera`, `DamageNumbers.ReportAt` | Any 04E boss fight | Reticle placement and feel unverified in Unity |
| Boss arena light budget (one shadowless key light per fight; the Regent's furnaces emissive only) | `BossLightBudget04E.cs`, `UnderBossFight.Lights.cs` | Kiln Regent arena | Counted in source (Regent 5 created, 3 on, now 1); frame cost not profiled |
| Sixteen main quests, twelve side quests, eight contract grammars | `Resources/Quests/underheart_*.json`, `QuestSteps.Underheart*.cs` | Journal; Lantern Refuge board | All pass `QuestValidator` offline |
| Campaign choices that change play (all nine keys) | `underheart_act1..4.json`, `QuestPhase.Window`, `UnderheartWorld.Window.cs`, `UnderheartWindowRules.cs` | `descent_window` in `uh05_13`: early (cool heat columns, lamps out, no detour) or late (hot, lit columns, optional elite detour); `ledger_route` in `uh05_09`: bench or terminal, Iris reacts; F2 > Underheart forces a window | Reliquary darkness, lit bands and heat feel unverified in Unity; offline tests guard that every key is read |
| Later consequences of `matron_fate` and `regent_end` (read from the stored choice, no new save field) | `Creatures/Underheart/ChoiceEcology04E.cs`, `UnderheartEcology.Residents.cs`, `Gameplay/Guidance04E/OutcomeLines04E.cs`, `RefugeTrade04E.cs`, `CampaignDirector.Refuge.cs`, `CampaignContracts04E.Variants` | After 08: the Matron's nest fills (and the gardens habitat crab calms) or stays empty (and her brood sleeps in the reliquary rest room), Bram's greeting, a "Pen feed" or "Matron's stray" job. After 15: the Crown and reliquary residents, Ilse's greeting and board line, her trade stall (Ilse, after the finale), a "Custodian lens" or "Hand valve shift" job | Residents only (the deep's heat and lights are unchanged); never seen in Unity; `ChoiceOutcomes04ETests` and `ChoiceConsequences04ETests` cover the rules |
| Resonance marks and bursts on the 4 bosses | `Gameplay/ResonanceSystem.UnderBoss04E.cs`, `Creatures/UnderBosses/UnderBossFight.Resonance.cs`, hooks in `Combat/ResonanceSystem*.cs` | Glass Sight, Sunbolt, Static Lance or the drakeling's Glass Lance marks a boss (4.5 s); the next gun round bursts at x1.75, splashes its adds, refills the magazine; gun hits on a boss charge crystal energy | Ember Chain, Roc Call and Gravity Well also reach and mark the boss through its lock stand-in (`Abilities.FindHostiles`, never pulled or knocked down); ring, shell and burst feel unverified in Unity |
| Checkpoints and underground load safety | `QuestBook.Campaign04E.cs`, `GameRoot.cs` | Save anywhere; load resumes at an anchor or a disclosed fallback | Loaded underground saves no longer replace the safehouse (fixed). A rest point is the resume point for saves made underground only; a save made on the surface stores the surface safehouse (`SafehouseRules04E.SavePosition`, appended `SaveData.HasSurfaceSafehouse` and `SurfaceSafeX/Y/Z`). A checkpoint autosave whose file was not written asks once more a few seconds later. Untested in Unity |
| Source and loaded-module check | `Core/BuildInfo04E.cs`, `Core/ModuleStatus04E.cs`, `CampaignDirector.Status.cs`, `Editor/BuildInfo04EEditor.cs` | Pause > STATUS; session chip; `UNDERHEART_04E` log lines | Revision text comes from the repo; no fabricated hash |
| HUD layout for every 04E panel, plus the moved older pieces (third callout vs heat row, 04D reward card, district name on foot) | `Gameplay/Guidance04E/HudLayout04E.cs`, `UI/Hud.cs`, `UI/HudCombat.cs`, `CrystalArsenal.Hud.cs` | Gameplay HUD | No overlap at 16:9, 16:10, 21:9 offline; HUD scale up to 1.25 only shares space with short-lived cards; at 1.3 to 1.5 the 04E panels stay apart but the older HUD overlaps itself; unverified in Unity |
| Surface world events while underground (held, then resumed or quietly expired) | `Gameplay/Events/WorldEvents.Underground04E.cs`, `UndergroundEventRules04E.cs` | Any running street event when you go below | Untested in Unity |
| Synthesized audio cues and loops | `Audio/Underheart04ESounds*.cs` | Lava, lift, UI track, companion refuse | Mix unverified |

## Partial

- **Choices**: every key changes play, and `matron_fate` and `regent_end` now change the ecology, the refuge and the
  board from then on (Implemented above). What they do not change: the deep's heat, hazards and lights (UNDERWORLD
  code), and the Matron herself is not shown after her fight (her pen or range reads through her brood and residents).
- **Pressure House sparks** stop when the vent is diverted; other ambient effects keep running.
- **HUD scale 1.3 to 1.5**: the 04E panels stay on screen and apart, but the fixed-size older HUD overlaps itself and
  meets them (the boss slot reaches the money block at 1.5); companion messages and the rewards chip hide there.

## Authored only

- None now (the bestiary tier labels for the underground species moved to Implemented).

## Not started or cut

- **Vent lift handling sidegrade**: cut; the grip tune is the single offer.
- **Finger bones** (`HandPose.Apply` is still a stub).
- **Networking**: none. Everything is local single-player PvE (see `NETWORK_BOUNDARY.md`).
- **Matched before/after arrival views, normal-play footage, native profiling**: need Unity on the owner's machine.
