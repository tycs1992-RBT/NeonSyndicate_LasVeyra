# 04OG physical world topology

New terrain is an explicitly selected **Living Frontier 04O-G**, revision 6, bank `lasveyra_frontier04og_save_N.json`. Revision 5 remains **Unbound 04O-E**. Revisions 0-5, default boot, their bank names, original service origins, mission owners and saved coordinates remain available. A running older layout refuses conversion. No automatic migration.

The original home, courtyard, guild, Archive, Forge, threshold, grotto and regional boss routes remain the foundation. The new branches use the original city generator, spatial hashes, route graph, mesh chunks and matching MeshCollider triangles, rather than a separate scene.

## Physical branches

| Route | Actual authored span length, seed 61724 | Geometry and interaction |
|---|---:|---|
| Oath access | 72.000 m | Ground approach outside the ramp footprint |
| Oath loop | 120.498 m | Two 32 m horizontal ramps rising 4 m, a 28 m elevated vaulted gallery, a 28 m lower return, piers and parapets |
| Oath witness | 16.000 m | Open-ended defense passage, wall recesses and a physical record interaction |
| Storm access | 96.000 m | Existing grotto approach to the frontier basin |
| Storm loop | 120.498 m | Supported elevated route and lower banked path; faceted rock banks and crystal roots |
| Storm cavern | 32.000 m | Irregular mineral roof, rock walls, sheltered spring and a clear junction |
| Storm vein | 14.000 m | Side bore and physical vein assay; return through the same junction |

These are emitted route lengths, not simulated travel times. The raised banks create elevation above the original surface; this is not excavation through the old global ground plane. The floor remains surface-side. No Underheart gate or story restriction is opened.

## Measured graph routes from arrival

Actual source execution, seed 61724, arrival `(0, 0.15, -27.25)`:

| Destination | Revision 5 | Revision 6 |
|---|---:|---:|
| Castellan court | 246.143 m | 246.143 m |
| Sovereign court | 379.432 m | 379.389 m |
| New elevated oath seal | unavailable | 472.393 m |
| New witness passage | unavailable | 470.143 m |
| New storm crossing | unavailable | 547.375 m |
| New spring junction | unavailable | 491.167 m |
| New side vein | unavailable | 505.167 m |

The 0.043 m Sovereign graph difference is a new revision-six connection, not a compression claim. No speed or map-distance multiplier changed. Full anchor coordinates and all twelve service measurements are in `Evidence04OG/world-manifest.json`.

## Source collision evidence and limits

Five seeds: 61724, 1, 2016, 77231, 92017. Tests generate revisions 5 and 6 independently, preserve the ordered old route prefix (some old IDs repeat), resolve every new quest anchor, require a home-to-anchor graph route, verify slopes below 0.18, check actual upward floor triangles on three walking lanes and test body-height lines at 0.45, 1.2 and 1.9 m against all new combined collision triangles.

Development tests caught and fixed an access crossing a rising ramp, gallery end columns blocking a turn, witness walls blocking an approach and roots crossing the cavern junction. They are not exclusions. The combined new-geometry clearance tests now pass.

These tests do not execute PhysX, CharacterController steps, imported scene geometry, companion motors, vehicles, flight or camera collision. Those checks remain NOT RUN. Arbitrary seeds beyond the five tested seeds remain unverified; bounded admission rejects an invalid branch rather than moving old saved geography.

## Continuation compatibility and navigation

Continuation source `138451d52c66fc9f3d3d67d9767ff204561e5fb1` changes no terrain, collision, route, anchor, service coordinate or saved schema from delivery `0d2f4e4`. Revision 6 remains the same isolated bank. The original MAP now offers five frontier lead buttons using the real anchor positions, including the 4.15 m elevated seal and crossing. GPS lead selection does not flatten those positions. Native menu/controller, navigation-arrow and companion traversal acceptance still requires the Windows session.
