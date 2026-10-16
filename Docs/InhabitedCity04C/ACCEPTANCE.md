# 04C native acceptance record

Status: **NOT RUN**. Unity `6000.3.16f1` is unavailable in this cloud environment. This is the reproducible Windows test itinerary, not a completed test report. Source evidence is listed separately in `Validation/`; offline images are labeled in `Previews/`.

## Prepare without discarding local work

1. Preserve the locally working `TerritoryPlacement04B.cs` and its `.meta`, plus other edits, outside the Unity project or through a reviewed local commit/stash. Read [the repair reconciliation](BASELINE_AND_METADATA.md#reconcile-the-owners-working-windows-repair) before switching. There must be exactly one `NeonSyndicate.TerritoryPlacement` class after reconciliation.
2. Fetch and switch to `feature/inhabited-city-04c` after resolving local changes. Run `git lfs pull` for the branch. Do not restore an old renamed duplicate beside the repaired canonical script.
3. Back up the existing save directory. Use a test copy for acceptance, retaining one pre-04C save and one new 04C save. Do not clear saves or PlayerPrefs merely to make a result pass.
4. Open the unchanged project in Unity `6000.3.16f1`. Let import finish. Capture actual compile/import errors before changing caches. Run the project EditMode tests, including `UnityMetadata04CTests`, `InhabitedCity*04CTests`, `QuietSignal04CTests`, `InhabitedResidents04CTests` and `ServiceUseGate04CTests`.

## First real route

In Free Story, retain the ordinary Veyra Lofts arrival. Enter its open lobby, inspect the furnished ground floor, walk the stairs into the apartment, use the bed or desk's existing safehouse menu, cancel and return to the street. Confirm the exit remains physically open.

Use PLACES to track the **exterior** of Mara's clinic. Enter reception, use actual treatment, inspect the supply area and leave. Track Dante's forge, enter its workroom, inspect a real recipe with insufficient materials, cancel, then perform a permitted transaction when supplies allow. Confirm one cost and one output. These canonical destinations are distant; the source route estimate is about 644 m home-to-clinic and 1,574 m clinic-to-forge, not a compact single-block circuit.

Return to the arrival neighborhood for Cinder Diner, the archive and adjacent observatory. Buy the existing meal, ask for directions, read all three records, walk the observatory stairs, and inspect the upper instrument room/open city view. Use the QuestBook offers to follow The Quiet Signal. At the depot, test the peaceful bypass and a separate opt-in combat attempt. At Jax Calder Customs, drive through the open bay, buy/cancel the existing garage service and reverse out without teleporting the vehicle.

## Cases to record

| Area | Required native observation | Status |
| --- | --- | --- |
| Import and compilation | All assets hydrated/imported; one placement type; runtime and Editor assemblies compile under the pinned Editor. | NOT RUN |
| First route | Continuous home entry, apartment stair, clinic and forge service use, exit to normal play. | NOT RUN |
| All eight venues | Walk every principal passage and exit on Low/default/High; verify furniture collision and headroom. | NOT RUN |
| Repeated entry | At least 20 entry/exit cycles; compare player/listener/menu/NPC/light counts and memory before/after. | NOT RUN |
| Interruption | Rapid threshold reversal, double input, pause, menu cancellation, death, ragdoll, arrest, title and reload. | NOT RUN |
| Fallback | Temporarily unavailable source in a disposable checkout leaves the original parcel/service; dependent chapter gives a readable unavailable reason. | NOT RUN |
| Service integrity | Clinic free/paid eligibility, one meal charge, recipe costs/output, home rest/save and garage repair/customization unchanged. | NOT RUN |
| Room context | Other floor/wall cannot activate service; land before foot interaction; pursuit and outdoor cooldowns continue; Veil refusal spends no energy. | NOT RUN |
| Residents | Correct floors, no doorway/stair/service obstruction, bounded admission, streaming cleanup, crime/damage behavior retained. | NOT RUN |
| Weather/camera | Day/night/rain; nearby work lights; indoor precipitation suppression and outdoor restoration; no camera/ceiling exposure. | NOT RUN |
| Chapter | Both investigation choices, free calibration and real crafting, peaceful and finite combat depot routes, five completions, exactly-once rewards. | NOT RUN |
| Encounter failure | Leave, die, abort, fail a body build or destroy a body unexpectedly; only tagged depot actors are cleaned and no false defeat is awarded. | NOT RUN |
| Save policy | Save shop/dialogue/stairs and before/after reward; reload at verified exterior; blocked candidates fall back safely; missing/null fields normalize. | NOT RUN |
| Feature switches | Apply disabled interiors/residents/ambience at next boot; save progress retained; no removed occupied floor. | NOT RUN |
| Preservation | Nine relay placement accounting, capture/fortification, 02I raids/echoes/sockets, 02G discovery, flight, driving, combat, original quests and Veil. | NOT RUN |
| Build/configurations | Windows player, resource loading outside Editor, supported render/input combinations, controller at target resolution. | NOT RUN |

## Render and performance evidence

The supplied native QA menu can capture an actual camera tour and passive current-place moment. A camera tour is not entry/exit or service proof. Record an uninterrupted normal-play walk-in > function > walk-out sequence as well. Keep original captures, settings, hardware, source revision and logs together.

Compare 04B and 04C at the same seed, save, hardware, resolution, graphics preset, render path, time and weather. Record startup, route entry, repeated-entry memory, frame-time distribution, draw/triangle cost and a populated room. No native FPS, startup improvement, memory stability, 20-40 minute chapter duration, or visual acceptance has been measured here.

Source render changes should be checked again in Unity; Blender lighting, the source camera and CPU render times are not runtime performance or native appearance evidence.
