# WR02-D continuation: Dante's fitted stone and slate workcourt

Starting published head: `a910c2ae86f1f3de4aee575af88fb38db5b816b1`.
Implementation checkpoint: `ec99c491776ebf9a1f0c924f1af889259b453c93`.
The following documentation commit is the delivery head, recorded in PR #12. The source tree is identical to local checkpoint 09e99bd7f54cf747ceeb00af494d13e0d7530cb1. Shell push lacked credentials, so the authenticated GitHub connector published normal forward commits; all blob and tree hashes were checked. PR remains draft, unmerged, with its original base. Local predecessor work was preserved on `codex/kingdom-continuation`; a clean forward branch started from the fetched published head. No history was reset.

This is an integrated Forge exterior art change, not completion of the Gothic Heart or the city skyline. Native appearance, contact and artistic acceptance remain open. Neither reported giant building has been identified or removed. This Forge is a deliberately selected known owner, not a guess about the screenshots.

## The actual location and change

The existing Forge is the 30 by 30 metre service parcel, seed 584924, source footprint x=60..90, z=100..130. `CityLayout.Gothic04OB` already connects it to the home and Archive circuit via the eight metre wide `forge-market` route, ending at (75, 96). These are source coordinates, not a claim of a completed walk.

`VenueView04C.GothicHeartShellH3` generates its original exterior from `GothicHeartH3.Structure` and `ServicePlan`. Its entire **Forge-only exterior visual group** now has an inactive replacement candidate. It includes the existing deep entrance, two open side galleries, the vaulted roof, offset roof crest, buttresses and rear court wall. The artist-authored C# recipe rebuilds these surfaces as chamfered cut-stone courses, raised slate courses at the front roof bays, contrasting stone arch dressings, metal plates on the actual suspended smoke hood, and pointed blind-arcade patterns on the rear wall. These are generated meshes in the actual game, not an unused art catalog.

The original overall roof profile and structural dimensions remain. This does **not** lower the wider skyline, create a new tower, add a room or achieve a wholly new building silhouette. Medium and higher graphics use raised near detail; Low and the distant LOD retain the complete support mesh and material grouping. Whether the change is noticeable or attractive from the player camera is NOT RUN. A source-geometry inspection was used while developing the mesh; it is not Unity evidence.

The new work joins the existing WR02-G stone street at the Forge's real entrance and the prior WR02-C3 stone workroom inside. Existing side galleries remain open. The current Archive foundry fallback and distributed route foliage are byte-identical to the starting checkpoint. No new street, route shortcut, quest, reward, teleport or flight grant was added.

## Art, support and restoration

| Changed visual | Original physical owner | Candidate and support policy | Restoration |
| --- | --- | --- | --- |
| Forge entrance and stone galleries | `VenueView04C` / `H3 fixed-asset service shell / forge` | Same original low collision triangles, re-cut surface relief no more than 0.022 m from a supporting face | Exact original H3 exterior retained beside the candidate |
| Existing pitched roofs and furnace hood | Same existing H3 MeshCollider | All roof triangles and gaps retained at both LODs; slate courses and metal plate relief, no new landing surface | Same existing Heart preview transaction |
| Rear court wall | Same existing H3 MeshCollider | Blind stone pattern with continuous backing; no extra entrance | Same original group restored |
| Forge room, floor, door, anvil and staff | Existing room BoxColliders, `InhabitedCitySystem`, canonical service/actor owners | Unchanged; existing WR02-C3 workroom remains independent | Existing lifetime unchanged |

The complete original collision emission is byte-identical. No new or changed world collider exists in this delivery. A pure geometric support check covers exported near vertices and triangle centres against the actual H3 collision triangles; it does not prove Unity collider cooking, flight, wall contact, landing or camera behavior. Inherited gaps and collision behavior are not certified as correct.

On a bad frame, missing source, missing map, unsupported shader or geometry-budget failure, this candidate is discarded and the prior WR02-D exterior path runs. If a completed candidate loses validation during activation, the Heart transaction restores the original H3 exterior. Preview OFF and comparison restore that same exact group. Both visual roots remain under the same venue parent, so its existing streaming ownership is retained. No shared city chunk is hidden.

Editable original art recipe: `Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ForgeCourtWR02I.cs`. Integration: `Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ForgeCourtWR02I.cs`. The new procedural artwork is original, dedicated to this project under CC0-1.0. It reuses existing H2 low geometry and slate/iron/amethyst materials, and existing prepared FR01 Masonry and CarvedTrim maps. Existing source assets, rights notices and GUIDs are unchanged. No new external asset, paid generation, download, FBX, Blender file or catalog is represented as produced. Blender is unavailable here.

## Real gameplay owners preserved

Dante, the existing anvil and the full original Forge service remain where they were. Local `staff` stays (5.3, .012, 10), and `service` stays (-4.5, .012, 8.2). Existing crafting inventory, recipes, reputation/discount checks and calibration activity remain under their original owners. Archive research records and its contract desk are unchanged. There are no new interactions in the relief artwork.

`fg_04ob_stone_not_silence` still belongs to Dante at the Forge; `fg_04oc_roots_remember` still requires its completion. Their JSON includes revision 6, and the existing `QuestLogic.WorldMatches` compatibility rule admits that requirement in revision 10. Actual offer/step completion is NOT RUN. The existing threshold, grotto and woodland route selections remain; mission/GPS priority is unchanged.

## What is still unknown

- Neither giant structure's selected renderer or collider is available. No guessed structure was removed. For each, pause Play, select the visible wall in Scene view, then capture the full Hierarchy path, Inspector with Transform and MeshFilter/Renderer, and the object carrying its collider. Include the F2 checkout/player position. If selection lands on a combined chunk, include its mesh name and a Scene view showing the selected bounds; do not delete that chunk.
- The owner's first actual FR01/FR02A preparation refusal is still unknown. Source assets and current receipts pass the available checks; that is not successful Unity preparation. Send the first failed row from Verify Fantasy World plus the first related Console error.
- Forge-area live-world art admission is unverified. The older screenshot did not disambiguate Sanctuary Portal from Shardwood Elder. The separate placement/protection fix is retained; this exterior does not relocate either optional piece or weaken the protected underground quest volume.
- The Physics.ClosestPoint warning has no captured stack trace and was not reproduced. No link to the buildings or loading failure is claimed.
- The major skyline, home court and Archive architectural transformation remains unfinished. This checkpoint's scope is the Forge exterior. Further work can continue on known owners, with native evidence needed to judge player-view results.

## Executed checks and limitations

Raw evidence is in `WR02I_EVIDENCE/`. The installed Editor binary is 6000.3.16f1; the project requests that exact version. A lightweight Unix socket bind still fails with EPERM. No display variable or `/dev/dri` is available, and no license file was found in the two standard paths checked. The same blocked Unity startup was not repeated. No credential or license contents were read.

| Check | Actual result |
| --- | --- |
| New geometry harness, real compiled recipe and shipped H2 meshes | 211,726 assertions passed; 20,956 original support triangles retained, 95,378 near triangles; counts are budget evidence, not artistic acceptance |
| Independent physical-surface correspondence | 162,610 unique vertices/face centres checked, 0 unmatched, maximum measured distance 0.0220016074 m including float roundoff |
| New pure NUnit assertion cases invoked directly outside Unity | 8 passed; not a Unity Test Runner run |
| New source/owner/lineage tests | 5/5 passed |
| Managed Unity 6 reference compilation | Classic player: 1,068 sources, 0 errors, 20 warnings; combined runtime/editor/tests: 1,330 sources, 0 errors, 67 warnings, same warning counts as baseline |
| C# 9 syntax only, bundled Roslyn | 12 configurations, 0 errors; no type/package/native claims |
| WR01 / WR02 / WR02-A / WR02-B | 54/54, 57/57, 69/69, 157/157 |
| FR01 / FR02A / Fantasy proof | 144/144, 185/185, 33/33 |
| Preparation / Archive fallback / zone lineage | 8/8, 2/2, 1/1 |
| Forge contracts / exterior / WR02-G contracts / composition lineage | 5/5, 3/3, 3/3, 4/4 |
| Preparation failure / grotto / route integrity / optional tree cache | 3/3, 2/2, 3/3, 3/3 |
| Surface and foundry lineage | 3/3 each |
| Existing connected street harness | 85,504 checks passed, 485 polygons and 12 culling chunks |
| Metadata and LFS | 2,817 sidecars; 0 invalid or unresolved; 13/13 metadata regressions; `git lfs fsck OK` |
| H5 guard | 46/48, same two inherited failures; assertions retained |
| Full managed NUnit, native Unity compile/import, EditMode, PlayMode | NOT RUN |
| Native Game View, walk, flight, collision, camera, companions, controller, saves, performance, Windows build | NOT RUN |

The reference player compile uses Linux Unity API references with Windows defines. The editor row combines assemblies, so it does not validate Unity assembly boundaries or installed package resolution. Other native compile configurations are NOT RUN. The three new NeedsEngine transaction tests are authored but NOT RUN.

The H5 failures were examined: the older normalized `HumanoidPose.cs` differs at the inherited `SwordHand`/melee-grip changes, and the prior-test guard still finds `SaveDataTests.cs` changed against its old baseline. Neither file changed here. They were not reverted or exempted to obtain a green result.

During development, the candidate initially exceeded its geometry budget; detail was reduced rather than relaxing the cap. An independent check then found eight slightly noncoplanar merged-face samples outside the relief limit; face merging was tightened and the check passed. Two continuation-fixture failures were corrected by keeping new native test fixtures separate from the old no-collider source registry and replaying only the three exact new visual hooks before inherited hash assertions. Mutation tests still reject arbitrary or partial edits. An inherited optional-tree source test expected the old cache predicate; it now requires the existing stronger receipt-completeness gate as well as material validation. Production preparation code did not change.

## Reproducible commands

Run from this project root. These Linux commands do not start Unity:

```
python3 tools/run_forge_court_wr02i.py --editor-data /opt/unity-6000.3.16f1/Editor/Data --output /tmp/wr02i-check
python3 tools/test_forge_court_wr02i.py
python3 Docs/WorldRewrite/WR02D/WR02I_EVIDENCE/syntax-driver.py
python3 tools/check_world_rewrite_wr01.py --report /tmp/wr01.json
python3 tools/check_surface_wr02.py --report /tmp/wr02.json
python3 tools/check_foundry_wr02a.py --report /tmp/wr02a.json
python3 tools/check_kingdom_wr02b.py --report /tmp/wr02b.json
python3 tools/validate_heart_fr01.py --report /tmp/fr01.json
python3 tools/check_heart_fr02a.py --report /tmp/fr02a.json
python3 tools/check_fantasy_proof_v1.py --report /tmp/proof.json
python3 tools/check_unity_metadata_04c.py --report /tmp/metadata.json
python3 tools/check_real_malik_h5.py --report /tmp/h5.json
python3 tools/test_kingdom_street_wr02g.py --mono /opt/unity-6000.3.16f1/Editor/Data/MonoBleedingEdge/bin/mono --csc /opt/unity-6000.3.16f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe
git lfs fsck
git diff --check
```

The five Forge source checks also run in the existing route CI workflow. Check final-head Actions for actual CI outcomes; local results are not a substitute.

## One owner walkthrough

1. Close Play Mode, back up the entire existing save folder outside the project, and preserve any local edits. In GitHub Desktop select the existing feature branch, Fetch and Pull. Use Unity 6000.3.16f1 and let import finish. Do not run two Editors on this checkout.
2. Open **Neon Syndicate > Acceptance > Verify Fantasy World**, then **1. Verify preparation and source identity**. If it fails, keep the first failed row and Console error; do not clear or bypass the refusal. The first native gate is successful preparation/import.
3. If preparation succeeds, start the existing **PLAY FANTASY WORLD** session. Walk from the Lofts along the existing court/Archive route, then follow the Forge route to Dante. On Medium or higher, compare the main stone arch, plated hood, layered front slate and side work galleries with preview OFF/ON from the same normal ground camera. Confirm the original entrance, anvil service, Dante dialogue and return path. Check the Archive lectern on the same circuit. Use the existing threshold and grotto route selections without granting progression.
4. With legitimately available flight, check wall/roof contact around the Forge and any identified giant structures. Check companion/camera clearance, preview restoration, walking away and returning, then a backed-up save reload. Use **Window > General > Test Runner > EditMode** to run `ForgeCourtWR02ITests`, `ForgeCourtNativeWR02ITests`, and the existing transaction tests. These are all local Editor tests; no password or subscription change is needed.

Preview OFF is the immediate visual rollback. Code rollback is a normal revert of implementation commit `ec99c491776ebf9a1f0c924f1af889259b453c93`, after protecting any newer work and saves; do not reset shared history. This delivery changes no save schema, save bank, default renderer or world revision.

SOURCE IMPLEMENTED: yes. ART PRODUCED: original procedural mesh recipe, integrated. ART IMPORTED IN UNITY: NOT RUN. NATIVE PLAYTESTED: NOT RUN. VISIBLE DIFFERENCE CONFIRMED: NOT RUN. GAMEPLAY VERIFIED: NOT RUN. OWNER ACCEPTED: not given. BLOCKED: native Unity IPC/license/display gates, exact owner screenshot identities and first preparation error.
