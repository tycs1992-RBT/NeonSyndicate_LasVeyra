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
