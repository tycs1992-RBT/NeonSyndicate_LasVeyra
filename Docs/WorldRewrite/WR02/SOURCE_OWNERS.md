# WR02 source ownership

Starting source: 99072f180f11da7edf168602b6fc603d5eb71daf. Tested implementation commit: 251a3d8f10fd84bae27b338391574cfdaa3ac8af. All fourteen integration edits are frozen as complete literal transformations in tools/wr02_owner_hooks.json. tools/surface_lineage_wr02.py pins that contract's hash. A changed original, partial hook or extra edit fails. New tests demonstrate that behavior. The new WR02 guard independently freezes every inherited C# owner, test, asset and metadata payload from the starting source outside those exact hooks.

- Assets/NeonSyndicate/Scripts/Editor/FantasyAcceptanceV1.Capture.cs
- Assets/NeonSyndicate/Scripts/Editor/FantasyAcceptanceV1.Source.cs
- Assets/NeonSyndicate/Scripts/Editor/FantasyAcceptanceV1.cs
- Assets/NeonSyndicate/Scripts/Editor/HeartSetupFR01.WorldRewrite.cs
- Assets/NeonSyndicate/Scripts/Editor/LiveWorldArtSetupH8S.cs
- Assets/NeonSyndicate/Scripts/Runtime/QA/FantasyProofV1.cs
- Assets/NeonSyndicate/Scripts/Runtime/UI/VisibleRealityV0.HeartFR01.cs
- Assets/NeonSyndicate/Scripts/Runtime/UI/VisibleRealityV0.WorldRewrite.cs
- Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.Fractured04OC.cs
- Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.Gothic04OB.cs
- Assets/NeonSyndicate/Scripts/Runtime/World/CityBuilder.cs
- Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartCatalogFR01.cs
- Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.ProofV1.cs
- Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.cs

New implementation owners are CityBuilder.SurfaceWR02.cs, HeartPlanFR01.SurfaceWR02.cs, HeartPresentationFR01.SurfaceWR02.cs and HeartSetupFR01.SurfaceWR02.cs. Two new test files cover pure policy/fit/proof and native batching/fallback contracts. The three native cases remain NOT RUN. Source policy is not a collision or visual acceptance result.

CityBuilder.Get captures only audited eligible callers before shared material merging. Natural meshes are captured only at their existing physical-source emitters. Per-chunk inactive candidates validate all meshes, shared maps, roots and required renderer children before activation. Original renderer bundles hide only after activation. Failure or lost runtime components restores the whole matching bundle. Original collider creation and root ownership remain unchanged. One bounded review in the existing visibility owner replaces per-object updates. Sources remain grouped by 160 metre append-origin chunks, like the existing CityBuilder; WildsBuilder's five-region streaming is unchanged.

CityDressing.cs, CityDressing.Frost.cs, all layouts, WildsBuilder, quests, save schemas, cast assets, combat, controllers, NPCs, discovery IDs and all prior tests are byte-preserved. There is no new world manager, gameplay authority, actor population, collision or permanent default.
