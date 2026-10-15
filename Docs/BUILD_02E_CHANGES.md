# Build 02E: Change manifest

## Existing files edited

- Scripts/Runtime/World/CityBuilder.cs: add source-coverage accounting; route non-landmark normal buildings through the lightweight fantasy exterior; retain original tier collider calls and helipad roof props; match intersection and sidewalk material treatment; remove old uncollided gable/spire accent method.
- Scripts/Runtime/World/CityDressing.cs: automatic street-tree and lamp replacement paths, planter/fountain and palm additions, and scoped material changes to canal, monorail, signs and station structures. Existing colliders, breakable registration, signal logic and labels remain.
- Scripts/Runtime/ArtDistrict/DistrictArchitecture.cs: add cleanup for 02E-owned cached materials and source arrays. Detailed-building selection and its original budgets remain unchanged.

## New files

- Scripts/Runtime/ArtDistrict/CrystalCity02ECoverage.cs: pure eligibility and cost limits. No random or geographical gate on ordinary buildings.
- Scripts/Runtime/ArtDistrict/CrystalCity02EAssets.cs: shared muted stone/glass palettes and transformed mesh stamping into existing material chunks.
- Scripts/Runtime/World/CityBuilder.Fantasy02E.cs: lightweight facades, roof details, street-tree, lamp and prop geometry, plus a completed-build coverage log.
- Tests/EditMode/CrystalCity02ETests.cs: local Unity tests, not run here.
- Matching metadata for each new C# file.
- tools/check_crystal_city_02e.py and release/QA documents.

## Assets and provenance

The pointed-window outline and bevels, integration code and placement rules were authored in this pass. Tree leaf, crystal, crown and lantern geometry reuse the original shared 02D runtime kit. Stone and normal maps reuse the existing district resource textures; no third-party art, font files or new asset-store package is redistributed. These remain procedural/runtime assets, not new imported production character models or scanned material packs.

## Preservation

No edits were made to player input, vehicle controllers, AI/combat, save schema, quests, source layout generation, rendering pipeline selection, Editor version, package manifest or assembly definitions. Existing .meta files are not replaced in this patch. Unity recovery files are not included or deleted. This release has not been pushed to the remote repository by the assistant.
