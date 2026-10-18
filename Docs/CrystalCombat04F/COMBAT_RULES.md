# Crystal combat rules 04F

Owners: `WeaponController` owns player focus execution; `PlayerCombat` owns melee phase/combo/stamina; `CrystalCombat04F` owns player Resonance and physical recharge; `CrystalHeat` continues to own focus heat by UID; `NpcCrystal04FCaster` owns each NPC's local volley/windup budget. Existing CharacterHealth/DamageInfo, factions, Forge, gear and reward authorities remain in use.

## Live tuning

| Rule | Value |
|---|---:|
| Resonance capacity / default for a missing old-save block | 100 |
| Basic shard / charged fan | 4 / 10 |
| Sustained beam | 1.2 per admitted 0.1-second tick |
| Disc | 12, recall has no extra cost |
| Orb / concentrated orb | 16 / 22 |
| Guided volley | 15 |
| Mirror pulse | 18 |
| Meaningful melee / riposte hit | 8 / 14 |
| Minimum harvest interval per victim | 0.3 seconds |
| Maximum melee harvest per victim life | 32 |
| Qualified victory | 12, once per life |
| Remembered victim lives per session | 512, fail closed rather than evict paid lives |
| Finite reserve cells | 3, each up to 30 once per saved token |
| Safe workstation | Refill to 100, 2-second action cooldown |
| Asterblade / talons / maul base damage | 32 / 23 / 48 before move/gear modifiers |
| Mouse heavy threshold | 0.18 seconds; quick release is light |
| Wide maul heavy recovery | 6 seconds; attempts during it use committed ordinary hammerfall |
| Talon guard cost | x1.5 |
| Light / heavy / riposte hitstop | 0.035 / 0.065 / 0.09 seconds |
| Reduced impact option | x0.35 hitstop and camera-shake scale |

Full individual move anticipation, active/recovery, stamina, poise, arc, lunge and charge values remain authored in `Combat/MeleeMoves.cs`. Ranged charge never gates melee, guards, dodges or existing gadgets. A refused heavy does not consume the maul cooldown. A buffered heavy retains hold intent. The native feel of these values is untested.

A shot first validates gameplay state, legal muzzle, resource availability, protected target, query capacity and actual effect/projectile admission. Successful projectile launch or an admitted beam tick is paid once. Menus, focus loss, deaths and weapon swaps cancel pending actions. A beam uses its existing bounded grid and discards hitches; no frame-rate-dependent burst payout is added. Native paths still require testing.

Harvest requires health/armor actually removed from an already-hostile unprotected character. A civilian attacked into hostility does not qualify for the first hit and protected roles never qualify. Practice targets, props, companions, refused damage and duplicate death callbacks cannot mint charge. Victim receipts persist for the current actor lifetime/session; reloading does not preserve a street actor's health/lifetime. Finite found-cell receipts do persist in the charge save block.

## Compatibility mapping

No saved WeaponId was reordered, removed or replaced. This is a live definition/presentation policy. Existing ownership and the equipped ID stay intact on load. Dormant legacy ammo values remain in their old arrays; they do not constitute a second magic resource.

| Saved fixed ID | Live counterpart / behavior |
|---|---|
| VectorSidearm | Wayfarer Shard Focus / traveling shard and fan |
| BrassHammer | Sunspike Lens / charged mirror pulse |
| CoilBurst, MicroSpray | Stormseed Focus, Quartz Bud / shards |
| GlassShot | Waystone Crescent / returning disc |
| RiotDrum | Choir Reliquary / guided volley |
| ArcCarbine, CinderLMG | Threadweaver Lens, Ember Conduit / continuous beam |
| LongWatch | Horizon Mirror / charged precision pulse |
| PulseLauncher, Hammerhead | Gravity Heart, Rupture Heart / gravity orb |
| Buzzsaw | Prism Loom / beam |
| VoltKatana, Switchblade, CrystalMaul | Asterblade, Riftfang Pair, Basalt Crown |
| SluggerBat, TireIron | Geode Staff, Forge Hook |

Ordinary loot keeps its seed, UID, maker, rarity, element, level, parts and unique record. Its archetype selects a crystal form. Runtime definitions normalize damage to that form's timing; current item cards read those live definitions. Saved generation and the original numeric catalog are unchanged. Trigger-changing legacy traits are dormant to avoid double fire; this limitation is disclosed on current generic focus cards. Element/handling/damage modifiers still feed the converted definition. Six 04D signatures retain their own exact behaviors and indices 17 through 22, with their original tokens and first-win ownership.

Old saves receive no companion, quest completion or replacement gear. They start with a full reservoir if its block is absent. Starter blade calibration is additive and requires an explicit safe cradle interaction on an old save. New Free Roam begins with the kit selected on Asterblade.

## Four-preset incremental budgets

These are code caps, not measured FPS promises. Existing city, creature, FX and global CombatDirector budgets still apply.

| New or reused 04F budget | Low | Medium | High | Ultra |
|---|---:|---:|---:|---:|
| Concurrent autonomous city encounter | 1 | 1 | 1 | 1 |
| Fighter cap per side | 2 | 3 | 3 | 3 |
| Shipped templates' actual fighters per side | 2 | 2 | 2 | 2 |
| Event-owned drakeling | 1 | 1 | 1 | 1 |
| NPC crystal bolt bodies | 12 | 24 | 24 | 24 |
| Existing player crystal projectile pool | 32 | 64 | 64 | 64 |
| New event/hero point lights | 0 | 0 | 0 | 0 |
| Event drake thinking cadence | 0.35 s | 0.35 s | 0.35 s | 0.35 s |
| Crowd human live transforms | 17 | 17 | 17 | 17 |
| Detailed human rig transforms | 47 | 47 | 47 | 47 |

Detailed human rigs are used only when the existing body detail budget requests them; low-detail crowds keep mittens and their old transform count. NPC bolts are capped transient objects, not pooled; pooling and actual profiler evidence are required before increasing their count. Existing global light caps remain 4/10/18/28. Existing creature/FX streaming is preserved. Measure pathfinding, GC, skinning, particles and frame-time percentiles in a real city fight and Underheart session before tuning higher.
