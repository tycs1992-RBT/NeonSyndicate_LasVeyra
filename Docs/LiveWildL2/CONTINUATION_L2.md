# L2: Redspire implementation in progress

Fetched exact branch HEAD: a8f21d507876b36630f4d0421c2e55401e36ab15, feature/04oh-ascension-art-foundation. PR12 is open/draft on feature/04o-shattered-metropolis-reborn. No newer remote work was present at intake. PR11 is untouched.

V0 passed the owner's reported ordinary-play check. L1 is source delivered but native pavilion import, stairs, collision and camera acceptance remain pending. No Unity Editor executable is installed in this host. Native import, Test Runner, PlayMode, actual saves, screenshots, profiling and Windows launch are NOT RUN. L2 will remain an explicit owner opt-in in ordinary Free Roam; procedural fallback stays normal until selected.

## Narrow ownership map

| Owner | L2 responsibility |
|---|---|
| WildsLayout, WildsSites, WildsTypes, WildsShapes | Original immutable plan, 17 discoveries, seeds, procedural visual and collision shapes; retain exact bytes. |
| WildsBuilder and WildsChunkL1 | Match only Redspire open scatter, omit its old visual, batch imported geometry by chunk/kind/LOD; retain original collision and clear leases. |
| New LiveWilds registry/rules and existing LiveWorld editor preparation | Validate original assets, deterministic variants, native mesh/material preparation and build inclusion. |
| WildsSystem partial and VisibleRealityV0 | Existing EXPLORE/GPS route, explicit visual preference, metrics and temporary comparison lifecycle. |
| Original physics, creatures, companions, quest/save/reward owners | No authority transfer or new saved data. |

Original source uses 160 m chunks built at boot, not streamed per frame. Existing chunk batches and collision are rebuilt by scoped L1 leases. L2 must honor those exclusions and leave an existing collider active during appearance-only changes. No per-piece prefab roots or Update components are planned.

The inherited frontier guard was reproduced before edits: 14/15, solely 'Append-only saved schema untouched'. Its raw prefix comparison rejects the already shipped append-only Interiors and Wilds blocks. The source diff contains those four comment/field lines and later bank-name extensions, with no L2 save change. A narrow historical proof plus mutation regressions is required before correcting that stale guard; a generic exemption is forbidden.

Art direction: two eroded sandstone boulder silhouettes, two layered hoodoos and two twisted dead-tree silhouettes, original editable Blender source and shared 1K rock/bark material maps. Real source geometry and labeled comparison evidence are required. These are not native screenshots or performance proof.

## Runtime checkpoint

The three-family lane is source implemented in ordinary Free Roam. Before Play/build, the existing LiveWorld preparation now validates the original-art receipt, importer, UVs, metre bounds and shared materials and creates an ignored Resources/LiveWildsPrepared catalog. Missing/invalid art retains procedural output. The kit is OFF until the owner chooses Redspire kit in Pause > EXPLORE. No default world, renderer or saved progression is changed.

Qualifying pieces keep original WildsShapes collision. Their old visible output is omitted only after the authored variant is admitted. Imported output is combined into three LOD meshes per kind per existing 160 m chunk, with no per-piece object or Update. Appearance toggles detach and retain the exact enabled original collider mesh while rebuilding visual ownership; scoped L1 exclusions remain authoritative. Legacy comparison never changes the preference and restores before Capture, Restore, ResetAll, title/loading, teardown and QA.

Actual default unreserved source plan: 18,522 pieces, 294 chunks; 965 eligible Redspire boulders, 327 spires, 169 dead trees. Runtime reserved areas can change those counts. A pure source selector finds a mixed-family viewpoint near Hollowwind Maze or Redwatch Tower; the real runtime plan, L1 exclusions, ground and capsule check decide whether GPS may mark it. No teleport, reward or discovery is granted.

Smoke compilation: player and Editor classic/Built-in pass. All 21 new L2 pure tests pass. Twelve-mode matrix, complete regressions and final native checklist remain in progress. Exact current C# checksum: 998d0619a8883b470acd99decf69330da2d9200dbaa030c11014e69c362d7459. Native import/gameplay/saves/screenshots/performance/build remain NOT RUN.
