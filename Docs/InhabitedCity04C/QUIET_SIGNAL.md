# The Quiet Signal, implemented chapter

The chapter is five nonrepeatable JSON quests in `Resources/Quests/quiet_signal_04c.json`. It uses the existing QuestBook, journal, MissionManager, ScriptRunner, choice phases, quest rewards and saved Completed/Active fields. One generic `Venue` step resolves authored interior points without projecting them onto outdoor ground or feeding indoor coordinates to the road GPS. The separate inhabited-city ledger stores finite physical-action proofs and archive reading history; it does not own a second quest engine, wallet or inventory.

| Existing quest stage | Implemented actions | Existing completion reward |
| --- | --- | --- |
| `quiet_signal_01_familiar_door` | Speak to Nessa Vale in Veyra Lofts, inspect the damaged receiver | $150, 2 Shard Chunks, 2 Hollow Essence |
| `quiet_signal_02_supply_and_repair` | Read the canonical Mara Solane clinic manifest, identify its intact supply crate, use Dante's repair bench | $90, +2 Menders reputation, +2 Forgewrights reputation |
| `quiet_signal_03_read_the_city` | Choose three separate archive records or a courier conversation at Cinder Diner; both explain the depot and alignment | $80, +2 Frequency Order reputation |
| `quiet_signal_04_missing_piece` | Choose a five-second noncombat isolation or explicitly start one Story / two higher-difficulty Glass Mites; retrieve the relay afterward | $120, +2 Frequency Order reputation |
| `quiet_signal_05_signal_restored` | Select split crescent at the lens, low band at the tuner, hold focus for five seconds, install the receiver at home | $180, +4 Frequency Order, +2 Menders, +2 Forgewrights reputation |

Nessa offers the first two stages at home. After the repair, the workshop assistant offers stage three at the forge. The archive desk assistant offers the stage-four recovery contract at the archive. The Survey attendant offers the final calibration at the observatory. This keeps the route moving without mandatory home returns between every stage. The journal resolves each offer waypoint to its actual venue entrance, including the two civic venues without an original POI. The existing journal can restart an interrupted chapter. Completed chapter quests cannot be forced into a reward replay through the debug start entry point. A completed physical action remains recorded after failure or save/load; the existing quest system still controls stage completion and payout. Installing the receiver is an action proof; the final home display must use `QuestBook.IsDone(quiet_signal_05_signal_restored)` so installation alone cannot claim final success.

The clinic is the existing Mara Solane Street Clinic in Sunken Row. The forge is Dante Rook's existing service in Old Silver. The chapter explicitly describes the longer cross-city travel; these contacts have not been cloned into the home neighborhood. The three archive records and the diner courier's directions are also readable outside the chapter. Reading history is separate from action completion and grants no money or progression.

The repair bench can use the actual `brew_warding_salve` recipe through CraftingSystem: normal affordability, discounts, recipe knowledge, timed brew effect and saved craft count apply. Previous successful crafting qualifies. Hostile Forgewright reputation refuses a new purchase. The alternative loaned tool needs four seconds beside the station and spends nothing, so using the one-time supply allowance elsewhere cannot strand the story. The borrowed tool grants no permanent combat or travel benefit.

Depot combat begins only after the existing chapter Choice selected its combat phase and the player activates the physical terminal inside. Both authored spawn positions, actual floor, capsule clearance and the full existing creature body budget are checked before allocating actors. Pending creature bodies count toward the budget. Bodies use NoRewards and have no reinforcement loop; health death callbacks, never despawn counts, prove success. Exit, mission release, death, arrest, pursuit, loading or title cleanup retires the attempt. Leaving the room bounds, a failed body load, or the 180-second limit cancels it without action completion. The peaceful route remains available by restarting this chapter. Native combat containment and feel still require Unity testing.

The `Venue` step exposes the current venue/action on QuestRun; only that exact matching action can complete. Each activation rechecks room occupancy, floor height, proximity, line of sight, grounded movement and the active quest. Choice callbacks recheck these conditions after the menu closes. Calibration progress stops during pause, cancels on movement or damage, and never advances at the title screen. Missing venue replacements lock the dependent chapter with a reason; a venue lost during a running step fails cleanly instead of leaving an endless objective.

## Offline evidence and native checks still required

`tools/run_inhabited_city_04c_quests.py` executes the actual strict QuestJson reader, complete source-extracted QuestValidator quest/step methods and their dependencies, and the actual QuestLogic phase-graph methods. The selected chapter has no contracts; the harness deliberately throws if contract validation is reached. Catalog references are extracted from the checked-in source. It also executes `QuietSignal04CTests` with NUnit on .NET 8. The report records every input source and data hash. This is offline validation, not native Unity execution.

Run in Unity on Windows before accepting the chapter:

1. Start from an existing save and from a fresh game. Existing progression and the four original guild arcs must remain intact. The new chapter must not open the Frequency Order contract board early.
2. Enter the lofts and accept the first chapter. Complete each stage through the actual journal and its physical venue request stand; confirm one payout per stage.
3. Fail, abandon, save/load and retry before and after every physical action. The action proof can persist, but the supply allowance and final rewards must not duplicate.
4. Try both investigation branches and both depot branches on separate saves. Verify all required rooms and floors are reachable and the exterior GPS returns after exiting.
5. At the repair bench, test enough materials, insufficient materials, previous salve crafting, hostile Forgewright reputation and the free loaned-tool route. Inspect the actual timed brew effect.
6. Read all three archive records and use the diner directions with no active chapter. Save/load and verify the reading history and all four interactions remain available.
7. Verify keyboard and controller prompts, menu options, dialogue advance/skip, and upper-floor interaction height. Do not complete an action from outside a wall or another floor.
8. Interrupt each steady calibration with pause, movement, damage and save/load. Pausing must neither advance nor reset it; other interruptions must not complete it.
9. Test depot actor loading failure, an occupied spawn point, global creature budget exhaustion, retreat, death, pursuit, abandonment, title and load. No actor disappearance may count as a kill; no loot, materials, abilities, bestiary kill count or extra wallet reward may be granted.
10. During an unrelated mission, 04B capture, 02I raid or world event, the chapter must refuse to start. During this chapter those systems must refuse to supersede it.
11. Confirm the final home display appears only after final quest completion and survives save/load. Disable the 04C feature at the supported city-boot boundary and confirm original services remain available.

No native Unity quest playthrough, physics test, rendered gameplay screenshot, controller test or frame-time measurement has been performed in this cloud environment.
