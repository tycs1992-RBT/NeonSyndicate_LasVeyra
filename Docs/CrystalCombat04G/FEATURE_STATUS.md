# 04G implementation status

Continue branch `feature/crystal-combat-living-city-04g` from the published `2f8491681d7b3e45e72ab8e3a250c2c41bc568d3` checkpoint. The preserved 04F ancestor is `747624335141a18031091f62737c73fe7f8969b7`. PR #2 remains a draft targeting 04F. Nothing is merged into main.

## Completion categories

- Fully implemented and independently verified: pure timing/health/drop rules, strict quest data, generated mesh/weight budgets, preserved save declarations and IDs, actual LFS payloads and metadata.
- Implemented but requiring native Unity playtesting: the integrated combat, companion movement, drake injury, courier, character poses and resident behavior paths in the table below.
- Partially implemented: precise blade contact, local navigation, independent event-drake ground combat and the earlier disclosed loot/flash limitations.
- Deferred or blocked: full sculpted asset/animation production, native acceptance, screenshots, Windows build and performance profiling, as detailed below.

## Implemented in the game

| System | Change in the actual runtime | Verification boundary |
|---|---|---|
| Crystal melee | Three-cut blade chain and rapid paired cuts now finish with a full recovery; admitted attacks alone advance the chain. Daggers have unique pursuit and roll cuts. Maul pursuit and roll blows retain a slow committed windup. Held heavy buffers include the maximum charge time. | Pure timing tests and source compilation; playfeel needs Unity |
| Defense | 0.18-second dodge request retention, weapon-specific legal defensive recovery, guard recovery, full stamina admission, capped roll invulnerability, and flagged crystal-lance deflection. A weapon swap clears an old strike. | Pure boundaries verified; input, physics and feel need Unity |
| Hit and casting feedback | Whooshes begin at the strike rather than the start of a held charge; dagger and maul pitch differ. Player and NPC focus windups feed a supporting-hand casting pose. Existing six crystal signatures, charge, heat and saved ownership remain. | Compiled source; sound and pose synchronization need Unity |
| Projectile safety | Every saturated shared crystal sweep quenches. NPC pooled lances validate direction and origin, retain explicit ownership and exclude their own emitting drake body. Existing bounded reuse and session cleanup are preserved. | Source/API checked; collider ordering and pooling lifetime need Unity |
| Bonded movement | Actual movement steps and hops sweep for obstacles, arrivals slow near the formation, sharp turns slow the body, and backing into a companion prompts it to recover space. Idle alert and quiet head checks use the existing pose and audio system. | Pure motion rules verified; stairs, ceilings and stuck recovery need Unity |
| Bond progression | Existing Harmonized rank and purchased tuning reduce support recovery by up to 12 percent. BONDS displays the actual cooldown. No new bond IDs or saved fields. | Pure progression bounds verified; old-save and fusion route needs Unity |
| Event drakes | Separate transient 90 HP receiver; opposing event NPC bolts alone can injure it, each hit capped at 30. Windups interrupt, low health slows assistance, the handler repositions to protect it, and a downed drake cannot satisfy rescue/reunion. Health is visible in event status. No bond registration, XP or loot. | Pure health rules verified; autonomous aiming and rescue need Unity |
| Encounter combinations | The cache event includes a heavy Sentinel and Weaver within the existing fighter budget. Other events retain blade/duelist and Weaver roles, observer protection, finite endings and saved reward receipts. | Compiled source; normal encounter placement needs Unity |
| New courier activity | A Light Carried Home: real Dispatch pickup, carried visible reserve and supporting-arm pose, recoverable drop on injury/vehicle/warp, Pressure House fitting interaction, report and physical exit. Existing quest checkpoints and once-only completion rewards own persistence. | Data validator, schema and drop rules verified; complete route needs Unity |
| Guidance and reactivity | CONTRACT guidance includes the courier; the map target follows the parcel/destination. Corin and Tamsin acknowledge the completed delivery in idle greetings when another quest offer does not take precedence. The existing campaign, companion lead and giver state stay authoritative. | Data/pure tests; map, prompts and controller focus need Unity |
| Resident routines | Existing venue residents shelter briefly from nearby violence and resume their original station route. Services do not depend on an actor, and no second population director is added. | Compiled source; crowd response needs Unity |
| Character geometry | Closed weighted collar/strap/buckles for coat wearers, broad Sentinel mantle/greaves, split Weaver coat, asymmetric duelist mantle/forearm lames, hero facial folds, and crystal-enforcement helmets. Existing rig indices, fingers, single body mesh and low detail budgets remain. | Generated mesh statistics, skin/geometry tests; visual difference needs native screenshots |

## Explicit partials and deferred production

This is a source playtest candidate. It is not a shipping-quality or fully playtested release. No professional imported/sculpted hero, facial blendshapes or new animation clips were delivered. Geometry and animation work use the existing authored C# mesh and pose systems. Exact weapon-surface contact remains the existing timed cone/LOS strike system. Companion navigation remains breadcrumb and local steering, not a new global pathfinder. Event drakes are damageable by opposing event bolts, but do not have a complete independent target-selection/ground-melee system. Legacy trigger-changing loot traits remain dormant as disclosed in 04F. Global flash reduction and full enforcement investigate/warn behavior remain partial. Long-term game length, cinematic production, broad new creature evolution branches and additional full interiors are deferred.

## Preservation

No existing quest/event JSON, binary asset, metadata GUID, package or project setting is modified. The new courier JSON is additive. SaveData, EventSave and GuidanceSave field declarations, WeaponId order, unique catalog, companion archetypes and existing reward authorities remain. No additional save migration is required. The carried parcel is temporary mission presentation, not a persistent inventory item or a rigidbody throwing simulation. A carry-phase save restarts the parcel at Dispatch; the delivered report checkpoint avoids repeating the carry.

## Native limitation

Unity 6000.3.16f1 is installed and was launched in batch mode. Licensing initialization failed with LicenseClient IPC errors before import/test execution; no result XML was produced. Therefore Unity import, Free Roam, Windows build, rendering, screenshots, input devices, physics, save/reload and performance are blocked/unvalidated. External managed compilation is a separate result. See TEST_LEDGER.md for executed counts and all 15 acceptance items.
