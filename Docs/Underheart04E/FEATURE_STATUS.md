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
| The Underheart: six region variants, stair and vehicle portal, cells, shortcuts, hazards | `World/Underheart/*`, `WorldApertures`, `UnderheartPlanner` | Pressure House stair (on foot); portal for vehicles when planned | Lighting, fog and audio feel unverified; light budget not measured |
| Kill planes, ground and camera for negative depth | `WorldBounds`, edits in player, NPC, creature, vehicle, ragdoll, projectile, camera | Automatic underground | |
| Bonded companions (3) with follow, wait, recall, assists, care, ranks, rescue, naming | `Gameplay/Bonds/*`, `Creatures/Companion*`, `UI/BondsPage.cs` | `uh05_03_bond` rescue; N and U; pause > BONDS | Companion movement in tight spaces unverified |
| Creature-vehicle fusion (Ventwing, Kilnback, Coilrunner) on the owned chassis | `Vehicles/Fusion/*`, `Gameplay/Garage/*` | `uh05_05_chassis` at Jax's bay; hold N in your own bike | Handling feel unverified |
| Companion and fused damage on bosses and their weak points | `UnderBossFight.Assist.cs`, `UnderheartBosses.TryAssistTarget` | Any 04E boss fight | Added in `9315fe4` |
| Underground ecology (9 species), elites, residents | `Creatures/Underheart/*`, `CreatureSpecies` appended ids | Underheart regions | |
| Four bosses (Valve Guardian, Cinder Matron, Vaultbreaker, Kiln Regent) with first-win tokens | `Creatures/UnderBosses/*` | `uh05_04`, `uh05_08`, `uh05_11`, `uh05_15` | Attack readability unverified |
| Sixteen main quests, twelve side quests, eight contract grammars | `Resources/Quests/underheart_*.json`, `QuestSteps.Underheart*.cs` | Journal; Lantern Refuge board | All pass `QuestValidator` offline |
| Checkpoints and underground load safety | `QuestBook.Campaign04E.cs`, `GameRoot.cs` | Save anywhere; load resumes at an anchor or a disclosed fallback | Loaded underground saves no longer replace the safehouse (fixed) |
| Source and loaded-module check | `Core/BuildInfo04E.cs`, `Core/ModuleStatus04E.cs`, `CampaignDirector.Status.cs`, `Editor/BuildInfo04EEditor.cs` | Pause > STATUS; session chip; `UNDERHEART_04E` log lines | Revision text comes from the repo; no fabricated hash |
| HUD layout for every 04E panel | `Gameplay/Guidance04E/HudLayout04E.cs` | Gameplay HUD | Tested for no overlap at 16:9, 16:10, 21:9 offline; some older HUD overlaps remain |
| Synthesized audio cues and loops | `Audio/Underheart04ESounds*.cs` | Lava, lift, UI track, companion refuse | Mix unverified |

## Partial

- **Choices**: `juveniles`, `constructs` (re-asks after a failed fight) and `fusion_profile` change play. `descent_window`
  (Act IV) and `ledger_route` (Act III) are recorded but nothing branches on them yet.
- **Pressure House sparks** stop when the vent is diverted; other ambient effects keep running.
- **HUD**: older overlaps (a third combat callout against the heat row, the 04D reward card against the rewards chip,
  the district name popup) and HUD scale above 1 are not covered.
- **World events**: none start underground; one already running when you descend keeps running.
- **Boss lock-on and damage numbers** for 04E bosses: not checked against the existing lock-on and number systems.

## Authored only

- Bestiary tier labels for the underground species: listed as an open item by the ecology stream; not verified either way.

## Not started or cut

- **Vent lift handling sidegrade**: cut; the grip tune is the single offer.
- **Finger bones** (`HandPose.Apply` is still a stub).
- **Networking**: none. Everything is local single-player PvE (see `NETWORK_BOUNDARY.md`).
- **Matched before/after arrival views, normal-play footage, native profiling**: need Unity on the owner's machine.
