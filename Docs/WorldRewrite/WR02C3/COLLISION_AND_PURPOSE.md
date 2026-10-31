# Collision and purpose: bounded Forge change

| Item | Source owner and intended use | Current evidence and remaining check |
| --- | --- | --- |
| Forge room floor and three walls | `VenueView04C`, from `InhabitedCity04CComposition`, owns the four original solid boxes and separate BoxColliders. `HeartPlanFR01.ForgeWR02C3` partitions their exact existing visual volumes. | The candidate creates no collider and does not extend the box envelope. Ground contact, scale, camera, fit and play remain unverified. The public opening and anvil are unchanged in source. |
| Forge upper structure and roof | Existing H3/H5 and 04C owners; not changed by this work. | Roof contact and safe landing are not certified. Do not infer support from the wall treatment. |
| Anvil and Dante | Existing `ForgeStations`, `VenueView04C` and `LiveWorldArtH8S.Forge` owners. The existing service anchor in `forge.json` is local `(-4.5, 0.012, 8.2)`; the staff anchor is `(5.3, 0.012, 10)`. | Source shows a real intended work area and existing interaction. Neither service has been played or observed here. |
| Archive sanctuary | Existing 04C/H5 room and lectern, with optional WR01/WR02-A visual crown and original physical witnesses. | Crown upper surfaces may not match the existing physical owner; no flying or landing conclusion is established. Its invalid candidate must retain the WR01 fallback. |
| Two giant buildings | Unknown. They may belong to different original, independent preview, fallback, service or skyline owners. | The screenshot does not identify their renderer, collider or purpose. Do not label a structure or add collision from the image alone. |

## If a reported structure needs new physical geometry

After identifying its exact object and existing physics, first try to fit or reject its isolated visual candidate using the current owner and restore path. If that cannot yield truthful surfaces, prepare an object-specific request for owner authorization before adding geometry. The request must include local and world bounds, collider shape and dimensions, layers, wall thickness and doorway clearance, roof and landing policy, and the exact visual/physical owner. Map saved player and actor positions, existing NPC and companion routes, vehicle corridors and quest markers. Specify activation, streaming and rollback, and a test matrix for normal and fast walking, camera, earned flight from both sides, roof landing, preview OFF/ON, teardown, save/reload, services and regressions. A generic collider on a shared city chunk or proposed arch is not authorized.

The closest-point warning is a separate unresolved observation. Reproduce it with the full Unity Console stack trace before naming a call path or linking it to placement or collision.
