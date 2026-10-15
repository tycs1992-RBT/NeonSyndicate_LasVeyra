# Crystal Strongholds 02I: implementation and asset record

## Concrete scope

Three automatically planned 32 m square forts: claimable Hearthspire Keep, Galehook Redoubt and Sunscar Bastion. They use checked flat ground outside the existing highway ring, rather than demolishing existing city blocks. The planner tests up to 169 deterministic candidates per site and can refuse all of them. It rejects nearby roads, mission locations, desert lots, walk nodes, parked vehicles, collectibles, existing interactables, steep ground and overlapping world collision. This is not a guarantee that every future dynamic event avoids the fort.

Each fort has a foundation, low perimeter cover, a large open Gothic gate, banners, crystal corner spires, a central core, decorative rotating orbit and six construction sockets. There is one distance/night-gated point light per fort, no shadows from those lights, and none enabled on Low graphics. Static model pieces are grouped by shared material into near/far batches. Physics is deliberately separate from cosmetic detail: the foundation, major walls, supports and landing decks use simple static colliders; every carved ornament is not individually collidable.

The runtime loads binary CMH1 geometry through the already working 02H decoder. The OBJ files are editable source copies and do not need their importer Read/Write flags repaired. No 02F/H asset cache or decoder was changed.

## Runtime files

- StrongholdData.cs: serializable catalog/save data and immutable IDs.
- StrongholdBook.cs: engine-free ownership, construction, transient raid, unique echo and progression rules.
- StrongholdPlacement.cs: deterministic candidate list, layout clearance and native Unity probes.
- StrongholdAssets.cs: instance-owned model/material cache, validated binary geometry.
- StrongholdView.cs: automatic fort assembly, batched geometry, landing/collision surfaces, world interaction and map markers.
- StrongholdSystem.cs: startup, guard encounters, capture, save lifecycle and cancellation.
- StrongholdSystem.UI.cs: existing pause-page/HUD integration and six-socket construction workflow.

Existing source touched: Game.cs (registry, reset and visible version), GameRoot.cs (boot hook), SaveSystem.cs (one appended field), PlayerController.Travel.cs (two bounded flight target multipliers) and SaveDataTests.cs (appended freeze contract). No other existing source or existing asset is replaced.

## Authored assets

13 original model designs with near/far variants (26 OBJ authoring files and 26 CMH1 runtime copies): foundation, gate, gate trim, watchtower, wall, flight dais, conservatory trellis, podium, heart crystal, spire, banner, rune inlay, foliage canopy. Four new 512x512 maps provide masonry albedo/normal/mask and woven heraldry albedo. Existing 02F material roles provide the shared metallic, leaf and crystal shaders; new stone and banner surfaces clone these materials. No additional external art or font files are distributed.

The supplied generator uses the earlier project's mathematical Mesh builder. Source models and textures were authored for this sprint, not copied from another game. Earlier asset provenance and licenses are unchanged. This is not a legal license audit or a promise of exclusive copyright in machine-assisted output.

## Ownership and capture boundaries

The catalog has exact stable IDs: hearthspire, galehook, sunscar. Local owner identity is local_malik or an NPC identity. Construction has six positions, five module states and four yaw positions per fort. Three themes change appearance, not combat statistics. Starter-kit edits are free. No gold/material deductions or external reward callback is introduced.

Only one raid can be active. It starts explicitly at a rival challenge sigil and registers exactly two defender death callbacks. Foreign/duplicate death notifications are rejected; a missing defender is not counted as dead. NPC wardens use NoRewards and Persistent options. A failed or unready spawn, leaving the encounter, player death, realm/story/vehicle change or timeout cancels the fight and removes its tagged bodies.

After both actual death notifications, capture needs five safe seconds within 2.8 m of the core and clear line of sight. Every increment is capped at 0.1 seconds. Paused frames never advance the rule. Successful capture sets permanent ownership; infusion occurs later at the owned home core, once per rival. A save/load cycle cannot replay a completed echo reward. Repeated requests do not raise the cap. This does NOT provide tamper resistance against a user editing a local save.

## Flight tuning

The existing speed, steering, banking, touchdown, energy/skill logic, camera, collision sweep and altitude clamp are unchanged. While not aiming, the target flight vector is multiplied by 1.00, 1.04 or 1.08 based on infused echoes. The separate ascend/descend input uses 1.00, 1.03 or 1.06. Pitch-based flight has the regular movement multiplier. No flight unlock is granted and no invincibility is added.

## Persistence

SaveData.Strongholds is append-only, with a versioned block and stable site IDs. Themes/modules/rotations and unique echoes are normalized on load. The existing WorldSystems registry owns capture, restore and reset. Source caches are instance-owned and disposed with the system; Game.ResetStatics clears its registry reference. Active fights and partial capture timers are intentionally transient.

## Performance estimate, not measurement

Only three sites and one active two-creature encounter are allowed. Shared decoded meshes and cached materials avoid per-frame generation. Static geometry has near/far groups and a 0.008 relative screen-height cull. Default LOD0 fort geometry is counted from source in the asset report, not profiled in an Editor. Collision remains active while the fort is culled so the player can still land safely. Object counts, draw calls, CPU/GPU frame time and loading time must be measured locally before raising these budgets.

## Asset authorship workflow

Do not edit the generated mesh files then rerun the generator without backups. The current binary geometry is authoritative for runtime, so editing only an OBJ will not automatically change the game. A later authoring pipeline should export both source and validated runtime geometry together and remove duplicate authoring assets from shipping builds where appropriate.
