# District and service integration

Branch `feature/living-crystal-world-04m`; [draft PR #8](https://github.com/tycs1992-RBT/NeonSyndicate_LasVeyra/pull/8) targets supplied 04L `8c6c3804b70898e745479affecdfe5f66d95ad1e`. Tested source `b38a0e649e9e3c8f8df49aa28b8519dcac8e7cb5`, tree `8e3b4d6773bb25a53a031ff7e8543e2d29e79ca2`, C# snapshot `7e894569f4925304477ea4fe17e9403ff8a3aa79f9dab836d1267f9def67e4ef`. Later handoff/evidence commits retain this tested C# snapshot; resolve delivery HEAD from PR #8 or git. **Production sprint PARTIAL. Native Unity, gameplay/save/render/profile/Windows acceptance NOT RUN.**

Runtime prefix: Assets/NeonSyndicate/Scripts/Runtime/.

- ArtRebirth04A/VisualRebirth04AArchitecture.Living04M.cs selects the actual Lofts→Pressure House corridor (75m lateral radius) from existing positions, plus the Mara clinic neighborhood in Sunken Row within120m. It is called only on original Tier0. No city rebuilding or coordinate move.
- ArtInhabitedCity04C/CityLivingGeometry04M.cs emits masonry piers, canopies, transoms and canal wave grammar into existing shared materials/batches. VenueView04C.Ascension04I appends receiver/heat-exchanger/feeding-niche relief to the three real rooms. Equipment remains outside |x|7.2m and all original solid/door/stair/anchor geometry is retained.
- New typical18×18m frontage: arrival near3360/far1056 vertices, canal near3744/far1248. Room additions near384–768/far192–240. These are measured mesh budgets, not frame-performance measurements.
- AI/NpcController.Residence04C.cs consumes LivingWorldRules04M shifts. Work alternates the existing checked stations; break/rest use one station; storms shelter locally. No new population director or housing claim. Three seed offsets are tested, full home commutes not implemented.
- Gameplay/FactionSystem.cs keeps Menders donations and Order offerings usable at negative standing; the original healing/attunement gates still decide service. Existing Dante/Outpost station owners apply all new recipe gates.

Native review: walk Lofts→Dispatch→Pressure→Refuge, then visit Mara clinic, Dante and Frost outpost. Check storefront seams, head clearance, camera, residents, doors, services and exits. No native observation is claimed.
