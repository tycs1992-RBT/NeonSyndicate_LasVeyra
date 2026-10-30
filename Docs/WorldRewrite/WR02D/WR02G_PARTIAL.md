# WR02-G: connected stone route and service heraldry

Starting head: `911c141fda7e982063c27cf07cf8e3b8d9ec1c55`. This is a partial source and procedural art delivery, not a completed kingdom or a native playtest. The owner's giant buildings remain unidentified and unchanged. The actual FR01 or FR02A preparation failure on the owner's computer remains unknown. No saves, story, gameplay services, quests, world revisions or default renderers were changed.

## Route and actual owners

The existing Heart preview stages a new staggered flagstone treatment over the existing home court, civic crossing, Archive approach, Forge market, home-to-Dispatch walk, Dispatch-to-clinic walk, Stone threshold and grotto approach/return. The marks at the court, Archive approach and Forge crossing use the existing prepared masonry and carved-trim materials. The western paving leaves irregular gaps to show the older underlying supported road. This is procedural surface geometry, not a new route graph, collision, cave, building or quest. The existing `CityBuilder.Gothic04OB.BuildRealmLandscape04OB` still owns the real flat route surfaces and their BoxColliders. Player, camera, companions and GPS retain their original owners.

The route union subtracts crossings before laying stones, cuts registered ground openings with a 0.15 m margin, limits the stone plan to 18,000 pieces and combines meshes by 48 m area for frustum culling. In the engine-free fixture from the checked-in revision-10 routes, the plan produced 5,474 stones, 11,241 triangles and 12 culling areas. These are source measurements, not an FPS result or evidence of appearance. Actual ground, cameras, opening contact and interaction paths require Unity review.

The previous WR02-D street is built first and stays available. The new candidate is inactive during construction, requires both existing mapped materials, and is shown only after validation. If the new candidate fails, the older street is restored only when its uncut surface is clear of the registered openings; otherwise the overlays stay off and the original underlying road remains. If the set of openings changes, the overlays are switched off. Ending the fantasy session restores the prior independent appearance choices. No world collider or proposed physical arch was added.

## Actual service façades

The existing `VenueView04C` exterior masonry candidate now carries two soft cloth markers per service: a diamond for the Lofts, an open-book motif for the Archive and an anvil for the Forge. They are 1.3 m wide and 3.15 m tall, generated from editable C# and original procedural textures, with two mesh detail levels. No paid assets or generation were used. The Lofts and Forge fabric is positioned at the existing H3 front piers; Archive fabric is at the H5 outer walls. The placement refuses altered service widths or depths. The cloth is explicitly non-solid and has no door, roof, landing or interaction. It does not replace a service or claim a new interior.

The banners share the existing `ExteriorMasonryWR02D` transaction. Its original H3/H5 masonry mesh, service collision, entrances and room remain separate. The original visual is retained if the candidate or required material is invalid or when its transaction is deselected; streaming follows the existing venue owner. Generated textures and materials are released with the venue owner. The real Archive lectern and the original Forge, Dante and anvil/crafting remain under their existing gameplay owners; no interaction has been tested here. These markers and the new ground treatment do not reshape the giant skyline masses.

## Independent safety fixes

The automatic fantasy-preview waypoint no longer replaces an existing player waypoint, mission destination, tracked lead, running mission or quest. Explicit route buttons still use the original GPS. The WR02-B foliage placement now repeats the existing protected-volume check after native grounding changes a candidate's height; a conflicting settled piece is refused. Neither fix identifies the owner's earlier Forge placement or Physics.ClosestPoint warning. No warning or catalog validation was suppressed.

## Evidence and owner test

Unity 6000.3.16f1 is installed in the cloud, but the previously reproduced licensing and Package Manager IPC restrictions prevented native import. Native import, Unity compile, Unity EditMode/PlayMode, real Game View, gameplay, flight, collision, save/load, controller, performance and artistic acceptance are NOT RUN. Blender is unavailable. A managed API comparison using the installed Unity 6 references is separate from native import and must not be described as a playtest.

Back up existing saves, update the feature branch without overwriting local changes, and open Unity 6000.3.16f1. Stop Play and choose `Neon Syndicate > Acceptance > Verify Fantasy World > 1. Verify preparation and source identity`. If it fails, capture its first blocker and the first related Console error; do not bypass it. If it succeeds, resume the existing revision-10 game and select the Fantasy preview. Walk from the Lofts through the court to the Archive and Forge, then back west toward the Stone threshold and grotto. Compare preview ON/OFF, inspect the paving and the three service markers, and check the real doors, lectern, Dante, anvil, camera and companion. Test earned flight only if already available. Stop and report any obstruction.

For each giant building, pause while it is visible, select its visible object in Scene view and capture the full Hierarchy and Inspector including name, Transform, renderer and collider if present, plus F2 player position. Separately capture the full Console stack trace of any Physics.ClosestPoint warning. The screenshots previously supplied do not identify either object or collider. Do not remove a shared chunk.

Preview OFF is the first rollback. To undo this source delivery use normal revert commits after protecting local work and backing up saves; do not reset the shared branch or overwrite save banks. Final-head CI must be checked separately.
