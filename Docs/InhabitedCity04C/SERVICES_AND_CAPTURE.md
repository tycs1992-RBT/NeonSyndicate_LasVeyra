# Existing services and native evidence

04C moves the original service interaction after its exact venue shell is available. It does not create a second wallet, shop, Forge inventory, recipe list or treatment transaction. If the venue cannot be built, the original exterior service path remains. Turning off 04C applies at the next city load. Disabling the active manager does not remove the in-place shell or make service checks depend on its update loop.

| Venue | Original owner and action | Existing cost and behavior | Exterior interaction |
|---|---|---|---|
| Veyra Lofts | `WorldServices`, `MarkSafehouse`, `MenuSystem.OpenSafehouse` | Rest/save/load/wardrobe; no payment. Wanted gate remains. Sleep advances six hours, heals and refills energy. | Same interaction moved to the ground-floor service anchor. Upstairs bed and desk presentation points invoke that exact original callback. Original map icon remains outside. |
| Mara Solane Street Clinic | `WorldServices.Treat` | Free in Free Roam or after story progress 1; otherwise $100. Fully heals and clears burning. Already healthy and not burning costs nothing. | Same interaction and dynamic price prompt moved inside. Menders guild contact is separate and unchanged. |
| Dante Rook's forge | `CraftingSystem.Station`, `UseStation`, `ForgeScreen` | Original materials, recipe costs, rank refusal, discounts and transactions. Combat/wanted restrictions remain. | Exactly one station chooses the available indoor anchor or original exterior placement. The map icon stays at Dante's original entrance. |
| Cinder Diner | `WorldServices.Eat` | $40, restores 80 health. Original payment rules remain, including paying when already healthy. | Same food interaction moved inside. No second clerk or robbery system is introduced. |
| Jax Calder Customs | `WorldServices.AddDriveIn`, `MenuSystem.DrawGarage` | Base prices: repair $300; engine $5,000/$12,000/$25,000; armor $4,000/$10,000/$20,000; tires $15,000; three nitro charges $3,000; paint $400. Original shop discounts apply. | Same vehicle-only automatic trigger moved into the open bay after its original mission condition is assigned. Original on-foot hint remains at the entrance. Original POI, road approach and delivery triggers are unchanged. |

The old safehouse prompt advertised a stash, but the existing menu has no item-transfer storage. The prompt now says save, sleep and wardrobe. No stash is claimed or added.

Foot service range retains its source radius and uses a 1.4 m height limit plus the venue's room, grounded-player and line-of-sight checks. The added bed and desk points use separate upstairs anchors and a 1.35 m radius; they cannot answer a player downstairs. Garage retains its 4.2 m drive-in radius, 3 m height tolerance and original mission rule. Original callbacks recheck availability immediately before invoking the transaction. The shared instance guard rejects reentrant calls; the original Interactable cooldown still owns repeated input. The guard does not replace existing economy rollback behavior.

Four production-logic NUnit tests ran through .NET 8 without Unity: stale access causes no payment/output, reentrant callbacks cannot spend twice, a failed source callback does not permanently lock the service, and missing context cannot invoke a transaction. They all passed. Existing Forge payment and discount tests remain authoritative. This is offline logic verification, not a native service-playthrough result.

## Controls and navigation

Pause > PLACES lists real bindings and availability, gives service descriptions and tracks exterior entrances. All buttons use the existing keyboard/controller menu navigation. Local service and chapter prompts use E or the gamepad context button. Garage servicing remains automatic while driving into the bay. Physical request hosts use the original QuestBook offer/accept/cancel menu and disappear while a quest is running: the lofts host offers stages 1 and 2, the forge offers stage 3, the archive desk offers stage 4 and the observatory offers stage 5. They reuse existing assistants and do not add copies of named story characters. No action on PLACES grants rewards or performs a remote service.

The small neighborhood note appears near Veyra Lofts until dismissed on PLACES. Its dismissed state is saved. Inside a venue, the HUD names the place and gives distance to the open street doorway. Upstairs, follow the stairs back to that doorway; this distance is horizontal, not a generated indoor route.

## Native screenshot tools, not yet executed in the cloud

With the existing project running in Unity, use:

- `Neon Syndicate > QA > Capture Inhabited City 04C Camera Tour`: actual exterior, room and stair/landing views at noon, night and 14:00 rain. The tool refuses active quests, encounters and pursuits before setting the global QA flag. It parks the player and controls the camera. Esc cancels and the existing QA session restores its state. Matched entrance cameras remain available when 04C was disabled at boot for a baseline comparison.
- `Neon Syndicate > QA > Capture Current Place Moment`: the actual current player frame, including an open shop/menu. It does not move the player/camera, change time, invoke a service, or set the global QA flag that can suppress encounters.
- `Neon Syndicate > QA > Open QA Output Folder`: dated folders under the existing persistent-data `GoldenShots` directory.

Tour output includes actual bindings, service and entrance coordinates, source building identity, live geometry counters, occupancy, resident counts, frame-time percentiles, memory and available render counters. Unsupported counters are -1. Counts come from the running Unity scene; they are not predetermined acceptance targets. Frame timings include frame caps and capture overhead. No performance improvement is claimed by writing this tool.

A camera tour does not demonstrate walking through a door or using a service. For that evidence, record one continuous Game view video: approach Veyra Lofts, walk through the open doorway, open/cancel the existing safehouse menu, go upstairs, use the actual bed and desk prompts, then return to the street. Repeat real treatment, Forge and garage purchase/cancel checks, keeping a backed-up test save. Current Place Moment screenshots can document the real state at checkpoints but are not a substitute for the continuous sequence.

Repeat entry and exit twenty times at one venue, watching resident/light/object counts in passive moment reports. Test the same path on Low/default/High, including the actual stairs, jumping and roof camera collision. Save upstairs and reload to the verified exterior fallback, then test with 04C disabled at the next boot. For transaction checks, record money/materials/health before and after success, insufficient funds, full health and cancellation; treatment must retain original free eligibility, and a cancelled Forge/garage menu must not spend. No report written by the capture tool automatically passes these acceptance checks.

Native import/compile, Game view appearance, actual collision, service transactions, controller traversal, repeated-entry memory and measured performance remain NOT RUN until exercised in the pinned Unity editor or Windows player. The source camera coordinates are review targets, not validated player paths.
