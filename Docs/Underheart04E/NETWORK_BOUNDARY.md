# Underheart and Bonds 04E: network boundary

**Design only. Nothing in this game is networked.** Every system in 04E is local, single-player PvE on one machine. No
bot, AI companion or test here is network validation.

## What is local today

Everything: the player, companions, fusion, the reward ledger, quests, the Underheart, bosses and saves.

## Contracts kept ready for later co-op

These already behave like commands with one authority, which is what a future host would own:

| Area | Command or record | Authority today |
|---|---|---|
| Rewards | once-per-token entitlements (`boss04e:<id>`, `quest:<qid>:<reward>`, `kit:<preset>`) | the local ledger in the save |
| Bonds | summon, recall, assist, rescue, care, rank (one active companion per owner) | `BondSystem` |
| Fusion | fuse and separate requests on an owned chassis uid (`ChassisUid`) | `FusionSystem` state machine |
| Damage | every hit carries a `DamageOrigin` (player, companion, fused vehicle) | `PlayerDamageHooks` and the shared pipeline |
| Encounters | arena claims through the encounter director, quiet zones through `LocalQuiet` | the local director |

## Next verifiable co-op milestone (not started)

Two independent clients on one host: both see the same companion and fused bike, a boss kill pays each player once,
damage totals agree, and a client that disconnects and rejoins keeps its bond and pending rewards. Until that runs with
real clients, no co-op or PvPvE claim is made.
