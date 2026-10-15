# Build 02G implementation and integration

## Scope

Three ground-level exploration activities connect the 02F landmark art to a complete player-facing loop. The locations are automatically sought near the existing Aster Crown Observatory, Dawnspire Atrium and Moonroot Sky Garden selections. The new shrines do not turn decorative floating architecture into landable platforms.

## Files and responsibilities

- DiscoveryLogic.cs: engine-free catalog validation, authored IDs, puzzle state, reward credit ledger and detached save snapshots. No UnityEngine dependency.
- DiscoveryPlacement.cs: bounded candidate generation and native ground/clearance/approach probes. It reads the existing layout and never changes it.
- DiscoverySiteView.cs: low, collision-free pedestal compositions using the already included 02F OBJ assets, floating response glyphs, contextual text, audio and existing Interactable/Blip instances.
- CityDiscoverySystem.cs: session lifecycle, asynchronous placement, existing-input integration, safe-use checks, progress updates and rewards through MaterialStore and Progression.
- CityDiscoverySystem.UI.cs: a DISCOVERY pause page and a compact local-only hint. It uses UIKit and its gamepad navigation. It does not open a modal or pause gameplay automatically.
- discovery_catalog.json: authored clues, three distinct found-family memories, rune orders, and capped existing-material rewards.
- Audio/chime_0.wav through chime_3.wav: original synthesized mono PCM chimes, with attack and tail fades. Not third-party audio.

## Existing files changed, narrowly

GameRoot.cs receives one guarded Step after world events. SaveData receives one appended Discovery block in SaveSystem.cs. The existing save-schema freeze list adds that field. Docs/CONTROLS.md adds an explanation of the reused controls. No save field is removed or renamed. No existing save-write routine is rewritten.

Game.Root starts world resets and save restores through WorldSystems. The new component owns its state without adding a global singleton. On teardown it removes its interaction objects, markers, page and registry entry; the HUD delegate checks destroyed-object state. Existing world shutdown clears the HUD registry.

## Reward design

Each solved shrine has two credit bits: Lumen and the authored crafting material. A bit is reserved before calling the existing inventory API, so re-entrant interaction or event-driven saves do not intentionally duplicate a credit. A failed credit clears its own reservation. Successful credits are not paid again when another credit is pending.

The runtime adapter checks space and integer capacity before adding. It inspects the before/after count if an existing feedback callback throws after the credit landed. Rewards are synchronous; this is a local single-player ledger, not a secure multiplayer trade or server-authoritative economy. Local saves are still editable by their owner.

All three site credit masks must be complete before a single Prism Core is awarded. Full inventory leaves it pending at any core. Reverting to an older save intentionally restores that save's entire inventory and activity snapshot; this is not cross-save anti-cheat.

## Placement and budgets

Three stations maximum, each with one core and three rune interaction points. At most 80 nearby walk-node candidates and 160 park/plaza candidates are examined per region, with up to four rotations. Native probes yield after every six candidate positions. Geometry and markers are created only when a complete four-point site passes validation.

Each point is rejected inside a building, on a road, near a service/POI, pickup or parking position, on steep ground, at an incompatible height, or inside a world collider. Paths between the four nodes and an approach to the walk graph require clear line tests. This is not full NavMesh path validation; complex scene geometry and visual-only decoration still require local review.

Three sites, twelve Interactable components and three spatial AudioSources are the maximum. Existing source mesh/material caches are reused. Pedestal parts are batched by material and LOD through the existing 02F composer. Animated response glyphs stay separate; labels are shown only within 18 metres. Proximity discovery runs four times per second. No new camera, AudioListener, Rigidbody, physical collider, NavMesh or package was added.

## Preservation

02D/02E/02F environment scripts, shaders, models, texture files and metadata remain byte-identical to the reconstructed 02F baseline. Roads, traffic, police, combat, player movement, vehicle handling, creatures and existing quest definitions are not edited. No recovery scene is deleted or committed by the installer.

## Authorship

New code, three memories, inscriptions, activity data and four chimes were authored for this update. Visual compositions reuse the already included project art, with no downloaded art or fonts. This is not a new rights review of older assets.

## Limits

This is a three-activity gameplay slice, not the full story, a restored Manor room, multiplayer, creature trading, new combat powers or a complete game. Existing enemy aggression remains controlled by the prior difficulty systems. No update can guarantee a review score. Native testing is still needed.
