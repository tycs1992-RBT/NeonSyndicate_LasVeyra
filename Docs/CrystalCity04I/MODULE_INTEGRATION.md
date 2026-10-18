# 04I module integration

| Existing owner | Change/callers | Persistence/lifetime |
|---|---|---|
| BodyBuilder / HumanoidRig / BodyFactory | BuildAscension04I plus head surface planes; original one-skin rig/palette streams | Existing Appearance, bone indices, budgets and worker ownership |
| CastCostumeBuilder04E | ServiceTorso04I for Tablet/Wrench service profiles, existing Dress attachments | Existing costume material/OwnedMeshes lifecycle; no saved wardrobe field |
| CompanionBodyFactory / CompanionSculpt | Anatomy04I shared by owned and event body builds | Existing archetypes/evolution/rig; bounds expanded; no new ownership |
| HumanoidPose | Small heavy recovery settle through actual attack state | Original legal windup/active/recovery and hitboxes retained |
| MeleeContact04H / CombatDirector | Additive Contact04I witness API; hilt/start/approach and blade corridor validation | Same DamageInfo, protections, armor/poise, hit ledger and resource authority |
| StuckMeter / CompanionController | Speed-based obstruction detection, cached narrow formation choice, continuous target and per-step sweep | Original breadcrumbs, recall/off-camera safety and BondSystem commands |
| NpcBondedDrake / NpcCityActor | Injured-handler interception, threat priority and stood-down target protection | Handler/event ownership only, existing windup/cooldown/damage budget |
| Vehicle / BikeMath | PlayerSteerIntent04I calls bounded pure CityDrive04I before original steering smoothing | Vehicle-local drift recovery; no saved tune/control or traffic change |
| FusionFormGeometry | AscensionFairing04I in existing body mesh | Original forms/chassis, parts, collision and fusion records |
| VenueView04C / art batches | Ascension04I produces four venue near/far material additions | Same bindings, shells, collision, services, light/LOD budgets |
| NpcController.Residence04C | Existing resident inspection and owned-drake head-look cues | Existing local routine and shelter timers, no population replacement |
| QuestBook / QuestCatalog / QuestSteps | Five data chapters; additive generic CityEvent step and strict whitelist/schema | Existing completion/checkpoint/flags/rewards; no save class change |
| WorldEvents | TryQuestCity04I / ReleaseQuestCity04I scoped to exact active QuestRun; normal Spawn(force=false) | Existing event actor/placement/cooldown/receipt owners; quest-only payout and explicit cleanup |
| ForgeDb / CraftingSystem / SkillMods | Two paid chapter-gated timed treatments | Existing transaction, modifier and brew save owners; Stillwater unchanged |
| Guidance / ModuleStatus | Eligible chapter tracking, final greetings,04I source label | Existing map/journal/giver ownership and saved settings |

Game.ResetStatics calls CityAscensionGeometry04I, CityDrive04I and CityIncidentRules04I resets. Helpers have no mutable session statics. New event lease fields are instance-only and clear through quest cleanup or existing EndAll/ResetAll. There is no second combat, companion, quest, event, population or save manager.

The source review checked admission, ownership, reward authority, cleanup, input parity, rig/material streams, geometry bounds and old identifiers. Native event/report ordering, actual physics, prompt competition, pose deformation and saves remain required. PRESERVATION_AND_RISKS.md records practical limits.
