# WR02-D continuation: supported dwelling forms and Archive crown

Starting fetched/published HEAD: `70e5277e3de0c969bc97d534d9965653af189129`.
Published implementation: `42d4f0eeb981f08f8c59fb0c75eb54defc93f5b1`. Its tree matches the tested local checkpoint `390b3f71` exactly. The following documentation commit is the delivery head recorded in PR #12. Shell push had no credentials; the connected GitHub service published this normal forward commit with an expected-head check, without force. The local checkpoint remains on `codex/wr02j-local-20261031`. This is a continuation of the original game and of the prior Forge exterior, not a new world or a completed Gothic Heart.

## What is actually different

The new presentation removes the generic envelope-fitted FR01 house models from two precisely identified small lots. It reveals their original, physically supported architectural shapes instead: a raised vaulted hall on six mineral supports with a real stair, and an asymmetric mineral-backed dwelling with a low vault, ledge and stair. Stone, carved trim, slate and selective crystal follow those structural shapes at metre-scale UVs. These are whole-building presentation changes from the prior imported house envelopes, not new props in front of those envelopes. They do not remove either unidentified giant building.

The Archive now first stages a fitted crown built from its actual seven nave roof sections, fourteen aisle roof sections and two supported mineral spires. This takes priority over the larger WR02A/WR01 decorative upper crown when valid. It exposes the existing long nave and paired low galleries instead of adding another unsupported upper building volume. It does not create new towers or enlarge the original H5 roof profile. The previously delivered Forge exterior, stone circuit and nature transition remain integrated and unchanged.

These are source-implemented differences. The amount visible from an ordinary camera, their composition with the rest of the city and their artistic quality are NOT VERIFIED. The untransformed dominant skyline is still a major open task. Neither this work nor the earlier Forge relief establishes the owner's requested complete fantasy district.

## Exact owners and access

| Visual group | Location and intended viewpoint | Original physical/gameplay owner | Replacement and fallback |
| --- | --- | --- | --- |
| Raised dwelling, seed 961727, family 4 | Footprint x=94..116, z=-26..0, height 12 m. East of the home court; approach the existing south stair from ground level. | `CityBuilder.Gothic04OB.BuildGothicBuilding04OB` calls `GothicRealmGeometry04OB.Family`; its `04O-B exact compound solid` uses the same stone/trim/crystal triangles. Non-service house lot, no POI, no receipt roof. | Three original source appends are isolated before shared batching. WR02J partitions those exact triangles by finish. The generic HeartHouseB is not staged for this lot. Any missing material, changed frame or incomplete capture retains every original append. |
| Mineral-backed dwelling, seed 961728, family 5 | Footprint x=102..124, z=145..171, height 13 m. North-east of the Forge; view from its southern approach. | Same CityBuilder/Families owner, its own original compound MeshCollider. Non-service house lot, no POI or receipt roof. | Same exact-source transaction; replaces the previous HeartHouseA envelope. The original low hall, asymmetric rock back, ledge and six-step stair remain visible. |
| Archive roof and spires, venue archive / seed 479130 | Existing Archive sanctuary approach, then the original entrance. Look at the long roof and lower side galleries from the street, not just from above. | `GothicCivicH5.Plan`, rendered by `VenueView04C.GothicHeartShellH3`; `H3 fixed-asset service shell / archive` retains all its low-mesh collision. | Candidate uses the same 23 low-mesh source placements and local transforms. Existing Heart transaction hides only the isolated crown. Preparation/staging refusal continues through the unchanged WR02A/WR01 selector. Preview OFF or lost dependencies restores the original H5 crown. |

The two dwellings have original open architectural space; they are unfurnished background dwellings, not new playable homes, quest locations or services. A door-like surface has not been placed over their real openings. Their walls, stairs, floors, vaults and ledges retain their original source geometry. Source equivalence is not a native contact or safe-landing result.

The Archive remains the original service interior, lectern, research/contract interactions and story destination. The Forge remains Dante's original crafting location with the existing anvil and room. The Lofts entrance and home ownership are unchanged. No NPC, companion, quest marker, reward, GPS priority, receipt roof, route, actor position, save schema or save bank is moved or replaced. `Stone, Not Silence` and `Where the Trees Ring` retain their actual prerequisites and reward owners. No flight, quest or progress is granted.

## Integration, art and physical truth

Editable integration source is `HeartPlanFR01.SupportedDistrictWR02J.cs`, `HeartPresentationFR01.SupportedDistrictWR02J.cs` and `VenueView04C.SupportedArchiveWR02J.cs`. It reuses the original repository's H2 mesh bytes, original procedural Gothic families and prepared FR01 Masonry/CarvedTrim/SlateTimber/AmethystBronze materials. No new external assets, paid generation, FBX files or Blender files were produced. Existing attribution, source assets and GUIDs are unchanged; new scripts have unique sidecars.

Dwelling vertices remain in original world metres and their existing parent frame. Archive parts use the original local service pivot, yaw and scale. The dwelling uses the original current-quality geometry (8/16 vault subdivisions); the Archive keeps its complete low physical silhouette in both near/far batches. Neither changes the physical owner. No world collider is added, removed, scaled, recooked or disabled by this code. Runtime roots are passive and inactive until validation; they contain no gameplay controller. Original collider objects remain outside both visual roots.

Tests compare the complete triangle multisets, including duplicate faces and winding, with the actual original source, at both dwelling quality levels and with the shipped Archive mesh bytes. Every tested source triangle is retained once, with no candidate displacement. Shader, albedo, normal and metallic maps remain required. These tests do not replace Unity checks of backfaces, contact, slopes, camera clipping, landing or streaming.

The new Archive key is separate from the inherited fallback key. A failed new candidate therefore cannot consume the key and prevent the original selector from restoring an available crown. The recent WR02-H foundry fallback and route foliage logic are byte-identical to the starting HEAD.

## Loading, Forge refusal and the two giants

The original first FR01/FR02A failure on the owner's computer is still UNKNOWN. This session verified hydrated source payloads and receipts, and preserved all preparation validation. No successful native catalog build or native Fantasy World load is claimed. Preparation code was not changed in this checkpoint because no new causal defect was established.

The earlier Forge optional-art refusal is still not disambiguated between Sanctuary Portal, Shardwood Elder or another placement. The existing settled-bounds underground quest protection passes its 13 source harness checks and is unchanged. Actual admission at `Venue:forge.exit` is NOT RUN. No protected quest or camera margin was weakened.

Two actual owner Game View captures were recovered and inspected: `image(20261029-202550).png` and `Screenshot 2026-10-29 173349(1).png`. One shows Crystal Spine / Las Veyra Core, flight altitude 55 ft, a Lofts door lead about 15 m away and the Cinder Dispatch frontage below broad pale/dark masses. The other shows Crystal Spine / Old Silver, flight altitude 130 ft and a Lofts lead about 365 m away. Both display the preparation refusal. Neither shows a selected wall, full hierarchy path or collider. The Inspector is empty. They do not establish which generating owner is responsible. The three WR01 receipt-frame candidates remain untouched and are not assumed to be the two buildings. No giant was removed and no matching collision was changed.

The second capture displays Physics.ClosestPoint's collider-type warning, without a call stack. It was not reproduced and is not attributed to the loading failure, Forge refusal or large structures.

## Fresh checks

Raw results and source identities are in `WR02J_EVIDENCE/`. Both baseline and changed compile rows were run with managed files extracted from the official exact-version Unity archive. The existing environment still refuses AF_UNIX socket binding with EPERM, has no configured display and no `/dev/dri`. Unity startup was not retried. Only managed compiler/runtime components were used; this is not a restored, licensed, working Editor installation or a native test run.

| Check | Actual result |
| --- | --- |
| New pure NUnit frame/geometry cases, directly invoked outside Unity | 12 passed, 0 failed |
| New source/ownership/lineage contracts | 5/5 |
| Prior Forge pure NUnit / geometry regression | 8/8; 211,726 assertions; 162,610 independent support samples, 0 unmatched |
| Unity 6000.3.16f1 managed API compilation | Player 1,071 sources: 0 errors, 20 warnings; combined runtime/editor/tests 1,335 sources: 0 errors, 67 warnings. Baseline 1,068/1,330 sources and the same 20/67 warning counts. |
| C# 9 syntax | 12 configurations, 0 errors |
| Older Unity 2021-reference comparison | 48 baseline / 48 changed diagnostics, 0 introduced. Not Unity 6 compilation. |
| WR01 / WR02 / WR02-A / WR02-B | 59/59, 57/57, 69/69, 162/162 |
| FR01 / FR02A / Fantasy proof | 144/144, 185/185, 33/33 |
| New and prior Forge source contracts | 5/5 each |
| C2 preparation / preparation-failure / optional-tree-cache | 8/8, 3/3, 3/3 |
| Archive fallback / foliage zone lineage / route integrity | 2/2, 1/1, 3/3 |
| Exterior / WR02-G / grotto / composition lineage | 3/3, 3/3, 2/2, 4/4 |
| Surface / foundry lineage / WR01 checker regressions | 3/3, 3/3, 28/28 |
| Connected route / street union / quest placement / Forge anchors | 85,504 / 48,950 / 13 / 488 checks passed |
| Metadata / LFS | 2,822 sidecars; zero invalid/unresolved; 13/13 regressions. 2,382 tracked LFS paths hydrated; fsck OK. |
| H5 | 46/48; same two baseline failures, detailed below |
| Full managed NUnit / full API matrices / assembly-boundary checks | NOT RUN; only the stated focused cases and two managed compile rows ran |
| Four new native transaction fixture cases | Authored and compiled; NOT RUN |
| Unity import, native compile, EditMode, PlayMode, Game View, walking, flight, collision, camera, companion, controller, saves, performance, Windows build | NOT RUN |

The player compile uses Linux player API references with Windows symbols. The editor row combines assemblies and does not prove Unity assembly boundaries, Package Manager resolution or import. Native fixture cases cover preview switching, parent streaming, lost normal maps and untouched collision, but have not executed.

H5 still fails `Original actor/gameplay authority / Characters/HumanoidPose.cs` and `All prior tests and exclusions preserved`. Inspection traces these to inherited SwordHand/melee-grip changes and the earlier SaveDataTests additions for Interiors/Wilds, respectively. Both files are unchanged here. Neither assertion was weakened and unrelated owner work was not reverted. The no-new-source preservation registries were extended only with exact new-file hashes and exact whole-file hook transformations; unknown or partial mutations remain rejected.

An early managed compile attempt ran the player row while the official archive was still extracting, then stopped because the template TestRunner DLLs were not present yet. After extraction, baseline and changed rows both passed. Unsupported `--mono/--csc` arguments to three older .NET harness launchers were corrected by compiling their unchanged generated projects directly with Roslyn; their final results are above. No production validation was bypassed. Remote CI outcomes are recorded in the latest PR checkpoint after publication; local checks are not remote CI.

## Reproduce the available checks

From the repository root, with EDITOR_DATA pointing at the official Editor/Data directory:

```
python3 tools/check_unity_classic_wr02g.py --editor-data "$EDITOR_DATA" --template-assemblies "$EDITOR_DATA/Resources/PackageManager/ProjectTemplates/libcache/com.unity.template.3d-cross-platform-17.0.14/ScriptAssemblies" --output /tmp/wr02j-compile
python3 tools/run_supported_district_wr02j.py --editor-data "$EDITOR_DATA" --existing-compile /tmp/wr02j-compile --output /tmp/wr02j-tests
python3 tools/run_forge_court_wr02i.py --editor-data "$EDITOR_DATA" --existing-compile /tmp/wr02j-compile --output /tmp/wr02j-forge
python3 tools/test_supported_district_wr02j.py
python3 tools/test_forge_court_wr02i.py
python3 tools/check_world_rewrite_wr01.py --report /tmp/wr01.json
python3 tools/check_surface_wr02.py --report /tmp/wr02.json
python3 tools/check_foundry_wr02a.py --report /tmp/wr02a.json
python3 tools/check_kingdom_wr02b.py --report /tmp/wr02b.json
python3 tools/validate_heart_fr01.py --report /tmp/fr01.json
python3 tools/check_heart_fr02a.py --report /tmp/fr02a.json
python3 tools/check_fantasy_proof_v1.py --report /tmp/proof.json
python3 tools/check_unity_metadata_04c.py --report /tmp/metadata.json
python3 tools/check_real_malik_h5.py --report /tmp/h5.json
python3 tools/test_kingdom_street_wr02g.py --mono "$EDITOR_DATA/MonoBleedingEdge/bin/mono" --csc "$EDITOR_DATA/MonoBleedingEdge/lib/mono/4.5/csc.exe"
python3 tools/test_street_wr02d.py --dotnet dotnet
python3 tools/test_safety_wr02d.py --dotnet dotnet
python3 tools/test_forge_wr02c3.py --dotnet dotnet
git lfs fsck
git diff --check
```

## One owner walkthrough and the first unfinished gate

1. Close Play and make a dated copy of the existing game save folder before testing. With the current project company/product settings, the expected Windows folder is `%USERPROFILE%\AppData\LocalLow\Neon Syndicate\Neon Syndicate Las Veyra`; use the existing game's actual save location if your installation differs. Copy all existing revision-specific banks and backups, including `lasveyra_civic_h5_save_*.json`; do not rename one bank into another. Fetch/Pull this branch in GitHub Desktop and open its existing project in Unity 6000.3.16f1. Do not use a new project or upgrade packages.
2. Choose **Neon Syndicate > Acceptance > Verify Fantasy World**. Wait for preparation/import. Success means the actual FR01 and FR02A rows pass and the current checkout stamp is present. If it fails, send the FIRST failed row and the first related Console error, with its stack expanded. This is the first missing native gate; the generic yellow Game View message is insufficient to identify an asset.
3. Start **PLAY FANTASY WORLD** using the existing workflow. From the Lofts, inspect the court, the small raised hall to the east, the Archive sanctuary approach and lectern, then the Forge, Dante and anvil. Continue past the Forge toward the mineral-backed dwelling, then return via the existing threshold/grotto route when legitimately available. The dwellings have no new service. Do not teleport, grant flight or bypass a prerequisite to demonstrate the route.
4. At the same ground camera, compare Fantasy preview ON/OFF. Look for the raised stair/open undercroft instead of the generic enclosed house, the smaller low vault against rock north-east of Forge, and the Archive's long roof with paired lower galleries and mineral spires. Check doors, floor contact, stairs, return route and companion access. If flight is already earned, check walls and roof contact without assuming a landing is safe. Record the actual F2 checkout and layer states. Run the `SupportedDistrict` EditMode fixtures in Test Runner if available; they do not alter saves.
5. For EACH giant, pause Play and select its visible wall in Scene view. Send one capture with the full selected Hierarchy path and Inspector Transform/MeshFilter/MeshRenderer, plus the object carrying its Collider and F2 player position. If the renderer is a combined chunk, include its mesh name and selected bounds; do not delete the chunk. Also capture the first FR01/FR02A refusal and the expanded ClosestPoint stack if it appears. These are the missing evidence items, not requests to approve every small change.

Rollback: preview OFF restores the retained original visuals in the current lifetime. Stop Play to end the temporary session. Keep the save backup until native checks pass. For a source rollback, revert the new forward commits in GitHub Desktop on top of current work; do not reset the branch or restore old save files over current progress. The fallback remains present, so a failed candidate does not require deleting a service or reverting a save.

SOURCE IMPLEMENTED: yes. ART PRODUCED: new procedural integration/material-partition recipe using existing geometry, no new Blender asset. ART IMPORTED IN UNITY: NOT RUN. NATIVE PLAYTESTED: NOT RUN. VISIBLE DIFFERENCE CONFIRMED: NOT RUN. GAMEPLAY VERIFIED: NOT RUN. OWNER ACCEPTED: not given. BLOCKED: native IPC/display/licensing environment, missing first preparation refusal and selected giant-building owners. UNFINISHED: giant removal, demonstrated successful Fantasy World loading, broader dominant skyline/composition, furnished home purposes and all native route/contact acceptance.
