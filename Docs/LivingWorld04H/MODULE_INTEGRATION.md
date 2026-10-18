# 04H integration record

## Ownership

| Change | Existing owner and callers | Persistence and lifetime |
|---|---|---|
| Character geometry | BodyBuilder.Build -> BuildAll -> BuildRenaissance04H; HumanoidRig/BodyFactory retain the existing skin/bone/palette pipeline | Generated from existing Appearance. No new renderer, saved wardrobe field, bone index or GUID replacement |
| Facade tracery | VisualRebirth04AArchitecture.Tier calls RenaissanceFrontage04H on tier zero. CityBuilder.Fantasy04A and DistrictBuilding.AddTier04A already consume Tier | Existing material/chunk/LOD ownership. Tracery04H exposes the same geometry to numerical QA; no collider or interactive placement |
| Movement anticipation | HumanoidPose.Update applies Anticipation04H after Carry, before combat arms/actions | Uses existing local acceleration/yaw and pose. No controller/root hitbox movement or saved state |
| Swept melee | PlayerCombat.UpdateAttack and NpcController.UpdateNpcAttack pass active interval to CombatDirector.Strike | Strike gains optional activeFrom/activeTo/mirror/hitProps arguments; old callers remain source-compatible. MeleeContact04H owns pure approximate trajectory/distance math, no manager or scene objects |
| Combat authority | CombatDirector still checks broadphase, height, arc, cover, allegiance and protections before shared CharacterHealth/DamageInfo | Existing hit ledger survives control loss. Separate player flag allows one prop attempt per strike. Ripostes/area moves retain old behavior |
| Crystal queries | CrystalShots.Query performs bounded initial overlap plus swept query; disc per-target state remains in CrystalShots.Sim | All start colliders are validated before any receiver is returned; capacity fails closed. Query scratch cleared by ResetStatics and session ClearAll; no ownership/resource API change |
| Encounter cover | NpcController.EventCover04H, called from existing ranged fight branch; CoverDirector.FindCoverFor/Release owns shared claims | Only active event Weavers against approved NPC opponents. 3.5-second searches, existing movement/caster/caps. Event stand-down and destruction release claims |
| Event drake | NpcBondedDrake04F.Update -> Think04H, using handler's bounded Opponents list | No CharacterBase/Creature.All/BONDS registration. Each callback revalidates exact event allegiance, life, range and cover. Existing Event.Objects/Retire own cleanup |
| Drake injury/recovery | EventDrakeVitality04G still admits only approved opposing event NPC damage | 90 HP; max 30 per hit retained. Retreat below 35, hold retreat until 52; recovery after 6 quiet seconds only beside handler, max 60 HP, cannot revive. No save block or XP/loot |
| Owned movement | CompanionController.SteerTo tries both tangents after a failed sweep | Every candidate independently swept. Existing trail, floor/ceiling probes, stuck/recall and BondSystem ownership retained |
| Repair activity | New generic Attune step dispatched by QuestSteps; JSON `uh05_04h_01_quiet_circuit` uses existing QuestBook/Runner/Campaign givers | Three seals per node; one success per 1.8-second cycle, 0.9-second readable quiet window. Bad timing loses nothing. Injury resets current node. Each completed node is followed by a checkpoint; interrupted current node restarts, not rewards |
| Companion cooperation | Attune subscribes to existing BondSystem.ExplorationAssist | At most one seal per node; existing assist cooldown/resource authority retained. Subscription and HUD counter released through QuestRun.Cleanups and coroutine finally |
| Rewards/recipe | QuestBook completion reward and ForgeDb rule `QuestDone` | 180 cash, 45 Lumen, 5 shard chunks, 2 coolant salt, once. New recipe ID `brew_stillwater_04h` costs 120 cash, 3 shard chunks, 1 coolant salt for 360 seconds; +12% stamina regeneration, -5% guard cost through existing SkillMods |
| Presentation | PlayerCamera, Fx.Flash and Hud remain owners | Existing saved CameraShake/ReducedEffects settings reused, not rewritten. Existing flash pool and camera hit buffer retained |

NPC caster `CancelWindup()` is additive. Cover hiding and general NPC WeaponController.CancelActions call it; cooldown and volley counters are retained. It prevents an interrupted tell from completing instantly on the next peek.

## Save and asset compatibility

No SaveData, EventSave, GuidanceSave, bond record, fusion record or inventory identifier is renamed, removed or retyped. New quest/recipe IDs are additive strings handled by existing persistence. All earlier shipped quest/event JSON stays byte-identical. Existing Unity metadata and binary assets are preserved. New source/quest files have new unique metadata. Editor version, package pins and default/optional rendering paths remain unchanged.

The repair does not spawn enemies, acquire a world-event slot or set story completion. It is independently offered at Tamsin and can be tracked through the existing optional CONTRACT route. It does not grant a companion, equipment ownership or a free crafted brew.

## Technical and native risks

The swept volume is an authored approximation of the active trajectory, with 24 phase slices and radial rails. It is not the rendered blade's exact surface and does not sweep a moving target's historical capsule. Compare contacts at 30/60/high frame rates and during hitches. Broadphase saturation now refuses the entire strike. Initial crystal overlap queries add physics work; no allocation/frame-time improvement is claimed.

Weaver cover uses the existing raycast cover finder and local movement. Actual route reachability, peek clearance, floor transitions and overlapping claims require native tests. Drake aerial positioning uses the existing grounded/flying controller and ceiling sweeps, not a new flight navigator. Injury may reduce a drake's usefulness for part of a finite encounter by design.

Nested quest coroutine disposal is not assumed: the mission object list and explicit cleanup subscription remain authoritative. Validate abandon/death/load while listening, assisting and reporting. Native JsonUtility save/load remains untested here.
