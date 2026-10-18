# Ascension episode and world integration

The connected optional route is `Resources/Quests/crystal_city_04i.json`: five chapters,45 steps and14 restart checkpoints. They are additive Side quests in the existing Underheart campaign, not replacements for its sixteen main/twelve original side quests. No duration was measured;30 to60 minutes remains a playtest target.

| Chapter ID | Physical entry and gameplay | Completion / reward |
|---|---|---|
| uh05_04i_01_ledger | Corin at Dispatch; receiver reference, Archive records/Iris, rhythmic calibration, vulnerable carried sample to Pressure House, reports | Dispatch exit;140 cash,35 Lumen,3 shards,1 salt |
| uh05_04i_02_passage | Corin/Dispatch door explicitly requests existing handler/drake incident; mediation or protection, physical reunion and report collection; Refuge treatment record/training relay | Return report/exit;160 cash,45 Lumen,4 shards,1 salt |
| uh05_04i_03_habitat | Tamsin; real Pressure House stair, Lantern Refuge relay, Pyre Gardens habitat/vista; manual or owned-companion cooperation | Surface report/exit;170 cash,55 Lumen,5 shards,1 salt |
| uh05_04i_04_recovery | Tamsin; Crucible Workshops coupler court; one elite extraction sentinel/two glasswings, zero reinforcements, arena arbitration; recoverable on-foot courier return | Pressure House exit;190 cash,65 Lumen,6 shards,2 salt; Edge Oil recipe known |
| uh05_04i_05_ascension | Corin; both receivers, Pressure House seal, Dante forge sample/manual or companion test, final report | Dispatch exit;200 cash,70 Lumen,7 shards,2 salt; Cityward recipe known |

Every chapter after the first requires the previous completed ID. Track the eligible chapter in JOURNAL to prioritize it at a shared giver. Existing map/giver/journal/door and underground routing owners remain authoritative. Original rescue and fusion prerequisites remain independent. Every companion-assisted task has a manual route; neither an owned companion nor the NPC event drake is granted for free.

## Event authority

The generic CityEvent step is whitelisted to the passage chapter and the actual existing `ev_04f_street_drake` template. WorldEvents owns the request. It requires the exact currently running QuestRun/MissionContext, on-foot surface/Outer state, no wanted stars, an available encounter budget, and no territory encounter/raid. Normal template district/story/weather/time/cooldown checks and safe street placement still run with force=false. A recent ordinary handler incident can impose its normal420-second cooldown. Wait in gameplay, move away from unsafe door/giver courts, or abandon and return later; no cooldown bypass is added.

A scoped lease lets only this requested incident run while the owning quest is active. Ambient auto selection is suppressed during the lease; unrelated story missions are never exempted from blocking. Menu/time ownership stays unchanged. The original finite setup/fight/reunion/claim states, actor/projectile/drake budgets and passive observer protections remain. This is an integrated requested variant of an existing category, not a newly invented faction simulation.

The requested event supplies no separate material/Lumen payout: QuestOwned04I uses an empty event reward, and ordinary chapter completion owns payment. `ascension_handler_passage_04i` records successful reunion/claim through existing quest flags. Retrying after that receipt does not rerun the incident. Recent-event reporting is retained before cleanup. Original ambient event receipts/payouts remain unchanged. Failure, death, abandon or session reset releases the lease and its tagged actors/props/drake immediately through existing cleanup owners.

## Progression and recovery

Ascendant Edge Oil (`brew_ascendant_edge_04i`): chapter4 completion unlock;160 cash,4 shard chunks,2 coolant salt,1 ember crystal;480seconds, melee damage+8%, guard cost-4%.

Cityward Infusion (`brew_cityward_04i`): chapter5 completion unlock;140 cash,3 glass chitin,2 coolant salt;480seconds, maximum stamina+10, incoming damage-4%.

Both use the existing Forge all-or-nothing payment, modifier and brew persistence systems. Neither is given for free. Stillwater Tonic's ID/cost/effects remain unchanged. No new inventory unique, bond ID, chassis ID or save declaration is added.

Each checkpoint resumes at an existing exact venue/Underheart anchor. A partly solved current rhythm node restarts; completed earlier phases remain committed. Courier interruption recovers the physical dropped parcel; reload during carrying restarts that phase at its source. Finite guardians use the existing ecology arena lease and no new permanent room-clear flag. The original campaign mechanisms and room completion remain independent. Completion rewards use the existing once-only quest IDs. Native death/abandon/reload, event cooldown restoration and reward acceptance still require the Windows route.
