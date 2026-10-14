# Additional ambient-encounter bypass fixes

Scope: `Gameplay/WorldEvents.cs`, `Gameplay/Events/EventInstance.cs`, `Gameplay/LootWorld.Guards.cs`, `Gameplay/LootWorld.Chests.cs`, new pure `Gameplay/AmbientEncounterRules.cs`, and `Tests/EditMode/AmbientEncounterTests.cs`. All changes build on the shared StarterSafety policy. Save fields, quest files, mission scripts, reward formulas and chest key costs are unchanged.

## Causes

The random world-event director begins trying after 50 seconds and uses persistent actors. The sealed-chest director automatically creates armed persistent crews inside 65 metres. These are independent of pedestrian and creature ambient target-acquisition rules. Blocking only ordinary NPC aggro therefore left both attack paths active.

`EventInstance.Engaged` means the player came within 45 metres. It is not manual acceptance. `EndAll(true)` marks unfinished events Missed and can add their missed outcome to history; `EndAll(false)` still removes every event, including chosen ones. Neither method was used as a blanket starter-area fix.

## World events

Automatic event spawning now checks the player and every candidate location. A protected player cannot receive a random event just outside the boundary, and an outside player cannot place an automatic event inside a protected area. Force/debug spawns retain their explicit bypass.

The central `EventInstance.AddInteract` callback now sets runtime `PlayerAccepted` only for a manual use. Automatic pickups, proximity and the distance-based Engaged flag do not set acceptance. This uses the existing interact input, including its gamepad mapping.

When a running automatic event reaches protected player or focus coordinates, the director quietly retires it unless it was forced or manually accepted. Retirement marks the instance Missed internally, cleans its owned actors, props and blips, removes it from the live lists, and skips its runner. It does not call Report, add a failed/missed history entry, emit EventCompleted, or grant a reward. Skipping the runner also prevents removed enemies from being mistaken for defeated enemies. Already finished events retain their normal report and reward flow.

Manually accepted talk, goods, relight and sabotage interactions remain active. The current ambient escort begins automatically when the player walks within six metres, so it can quietly end on returning to a protected area. The implementation does not mistake this proximity behavior for consent. Story missions and quests use their separate runtimes and are untouched. Ordinary local provocation temporarily opens the shared area's ambient response under StarterSafety; an unaccepted automatic activity can retire again when protection resumes. Event templates and reward schemas are unchanged.

## Sealed chests

Automatic guard crews require both player and chest placement to permit ambient aggression. A previously spawned unsolicited crew is removed on protected arrival without invoking its kill callback, paying a bounty, setting CrewDown, or marking BountyPaid. The system retains daily bounty assignments and the chest's unopened state, so the fight remains available later.

A protected sealed chest without a key offers `Press E to challenge the guard crew for a Chest Key (combat)`. The existing prompt translation maps this to the gamepad use button. Only the use callback starts the crew; proximity alone never chooses combat. Runtime chest-id tracking keeps the chosen crew from disappearing when protection is checked. Direct player damage to an existing crew also records the combat choice. Existing distance cleanup and mission cleanup remain in effect, and session restore/reset clears invitations through the existing DespawnCrew path.

A player who already has a key gets the original normal opening action and key cost. The challenge does not consume a key, open the chest, pay the bounty or grant loot. The existing leader key drop and bounty completion logic still provide those outcomes after the fight. Vault guardians remain tied to the existing explicit vault-opening action. No immunity to police consequences was added for attacking human crews or bystanders.

## Verification and limits

All six changed or new C# files passed grammar parsing, with no syntax errors or prohibited dash characters. Five engine-free NUnit cases cover safety on both sides of a boundary, explicit requests, proximity versus consent, preserved key opening, and blocked/redundant guard challenges. Tests were added but not executed because Unity and a C# compiler are unavailable. Evidence: `AMBIENT_STATIC_CHECKS.json`.

Required live checks: remain in each protected start beyond the 50-second event delay; walk into and out of protection near an unopened sealed chest; approach an automatic hostile event without attacking and return to safety; confirm it ends without reward or a failure entry; manually accept an activity outside safety and return; force a debug event inside safety; challenge a protected chest with no key using keyboard and gamepad; open it normally with a key; verify an existing bounty still pays once, and repeat after loading a save.
