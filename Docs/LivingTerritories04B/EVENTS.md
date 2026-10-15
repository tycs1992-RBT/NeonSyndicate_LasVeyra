# Living territory event integration

These are six authored JSON events run by the existing WorldEvents director and ScriptRunner. Two shared activity kinds reuse EventInstance interaction, actor, marker, outcome and cleanup services. They are never eligible for random ambient selection, including the force-spawn debug button. A physical territory terminal supplies the surveyed court and a ledger reward transaction.

| Stable ID | Implemented loop | Authored reward, once globally per template |
| --- | --- | --- |
| ev_04b_blossom_gather | Use three visible authored flowers, remain within 3 m for 3 seconds each, then report at the terminal. Leaving or taking damage interrupts tending. | 40 Lumen, 3 shard_chunk, +2 menders |
| ev_04b_courier_aid | A real companion-role courier waits in an existing cower pose. Gather three supply caches with 2-second channels, give them to the courier, then report. Harm to the courier fails the activity. | 45 Lumen, 3 glass_chitin, +2 menders |
| ev_04b_canopy_observation | A real calm-until-provoked Neon Jackal is spawned through CreatureDirector. Keep it in view, within 30 m, with clear world line of sight for 8 continuous seconds. Harm fails the activity. Observation adds a legitimate seen entry through Compendium.MarkSeen; report for the bounded event reward. Existing Third Eye scanning still works independently. | 35 Lumen, 4 mite_dust, +2 menders |
| ev_04b_ancient_resonance | Use three numbered mineral sigils in order, no more than 20 seconds between notes. A wrong note or timeout resets the sequence. Report the completed chord. | 50 Lumen, 3 shard_chunk, +2 frequency_order |
| ev_04b_relay_defense | Owned relays only. Explicit 3-second warning, one finite Neon Jackal assault, no reinforcements. Defeat threats, hold within 8 m without damage, then manually confirm. | 70 Lumen, 4 glass_chitin, +2 forgewrights |
| ev_04b_rift_flare | Explicit 3-second warning, one finite Hollow Shade group. Seal three visible fracture conduits with interrupted-on-damage 3-second channels, defeat threats and manually confirm before 240 seconds. | 80 Lumen, 2 rift_shard, +2 frequency_order |

The three territorial cultures are not new reputation accounts. The table uses existing guild IDs whose definitions were inspected in Factions.cs. Combined maximum template rewards are 320 Lumen, 6 shard_chunk, 7 glass_chitin, 4 mite_dust and 2 rift_shard, with +6 Menders, +4 Frequency Order and +2 Forgewrights reputation. The territory ledger callback owns global claim keys, not these visual site IDs.

## Runtime boundaries

`TryStartTerritoryEvent(eventId, siteId, safeCenter, owned, grantOnce, out reason, fortificationTier = 0)` requires a grounded player on foot within 18 m of the physical court, outside any mission, pursuit, Veil, QA capture, existing stronghold raid or territorial capture encounter. The callback is `Func<QuestReward, bool>` and fully claims AND grants the reward transaction; EventInstance never grants it again. The existing ambient director cannot add a random event during a territory activity or capture.

One requested territory activity can run at a time. Points are within 16 m of the surveyed center. A native support/headroom/slope probe must succeed before actors appear, and does not redirect to city sidewalks when obstructed. If placement or the creature budget fails, the event cancels without rewarding partial content. Existing 04A meshes and shared materials provide visible flowers, sigils and fracture clusters; courier supplies use the existing satchel prop. Props add no colliders.

All events end on departure beyond 90 m in 3D, 240-second lifetime, death, arrest, wanted pursuit, mission start, title return, load or Veil crossing. Pause freezes runner time and cooldowns. Creature tags, props, interactions, companion and markers belong to the EventInstance and are removed together. Death callbacks are explicitly unsubscribed. A missing or unready hostile body is a canceled encounter, never a defeated enemy. Body creation must finish within 18 seconds. No event creates gangs, police, persistent followers or bespoke humanoid animation.

| Defense tuning | Story | Balanced | Veteran |
| --- | --- | --- | --- |
| Maximum simultaneous threats | 2 | 3 | 4 |
| Low graphics maximum | 2 | 2 | 2 |
| Health scale before fortification | 0.75 | 1 | 1 |
| Damage scale before existing difficulty rules | 0.65 | 1 | 1 |

A global weighted body budget rejects new event actors above the existing preset's ambient body cap plus four. Every event body uses NoRewards, so restarting an activity cannot mint kill loot, parts or progression. Only the idempotent terminal transaction grants template rewards. Native body caps and interaction pacing remain Windows acceptance items.

| Owned defense fortification | 0 | 1 | 2 |
| --- | --- | --- | --- |
| Assault health multiplier | 1 | 0.9 | 0.8 |
| Post-clear hold | 20 s | 16 s | 12 s |

Fortification affects only this explicitly requested defense. It adds no player-stat or flight multiplier and never changes original 02I echoes. The maximum health reduction is 20 percent. The territory system owns upgrade costs, saved tiers and visible changes.

## Save and tests

EventSave appends parallel `TerritoryCooldownKeys` and `TerritoryCooldownSeconds` lists. Keys are `siteId:eventId`. Success rests for 600 seconds of actual play; cancel/failure rests for 30 seconds. An active event is not restored; saving it stores a 30-second retry cooldown. Missing/null old fields load as no cooldowns. NaN, infinity, negative and excessive saved values are rejected or clamped. Existing event IDs, seen counts, journal and other world-system saves remain intact. Global reward claims live only in the new territory ledger.

`TerritoryEventTests.cs` covers actual budget, resonance progression, observation interruption/pause, cancellation, fortification caps, corrupt cooldown values and the six shipped data definitions. `QuestSaveTests.cs` adds the append-only freeze fields and native JsonUtility round-trip coverage. The real existing QuestValidator checks the JSON schema and IDs. The reproducible offline runner `tools/run_living_territories_04b_events.py` executed 20 real NUnit cases successfully and validated all six new templates with zero errors or warnings, with 52 unique shipped event IDs. Its source-hashed report is `Validation/events_source_validation.json`. These tests do not prove native physics placement, live interaction, character appearance, compilation in Unity 6 or performance. Those remain explicitly NOT RUN until Unity 6000.3.16f1 is available.

The observation is a ground-level canopy-edge creature study, not an aerial flight reconnaissance mission. The aid courier uses existing cower/idle animation and does not perform a new medical animation. Rift stabilization uses authored fracture props plus a finite creature group, not the older unbounded RiftTear spawner. These boundaries keep the six loops truthful and safe.
