# Build 02E: City-wide fantasy exteriors

## Why some buildings stayed modern

02D only replaced the full facade of buildings admitted by DistrictArchitecture.Contains. That selection retained a graphics-dependent landmark cap and left other buildings on the original facade-texture path. 02E keeps that expensive detail selection, but gives the remaining ordinary buildings their own lower-cost fantasy exterior. Raising the heavy-detail cap is not required.

## This delivery is a local patch, not a published GitHub commit

The source was developed against art/crystal-fantasy-block-proof at 44a4ef4fa808a6feab69dc80cb24d184594f06ae. Current tools can read the repository but do not expose write actions. This release has NOT been committed or pushed online by the assistant. Fetching alone will not download 02E.

## Apply without assembling anything in Unity

1. Close the Unity Editor, but GitHub Desktop and Unity Hub may remain open.
2. Extract the Build02E update ZIP into its own folder. Double-click APPLY_02E.cmd.
3. The installer suggests C:\games\NeonSyndicate_LasVeyra. Confirm the correct existing project. It checks the expected art branch, verifies source hashes, backs up touched files outside the project, then applies the patch. It does not install software, delete scenes, change Editor versions or push to GitHub.
4. Open that same project in Unity 6000.3.16f1, wait for compilation, press Play and choose Free Story Mode.

No Art menu step, manual prefab placement, replacement project, save reset, or scene setup is required. Do not copy only one C# file; these changes are one integrated patch.

If source validation fails, the installer stops before changing anything. This protects newer work and local edits. Retain the message rather than forcing the patch.

## What is covered

- Every ordinary building that passes through the main city building generator gets either the existing detailed landmark facade or a lightweight fantasy shell with bevelled pointed windows, mineral stone, cornices and corner detail. There is no random, distance or graphics-preset gate on the lightweight building coverage.
- Ordinary generated houses, towers, industrial buildings, outskirts buildings and the ordinary desert-site buildings are included. Structurally invalid/sub-3m footprints and lots explicitly marked CustomBuilt are excluded.
- Helipad building walls are themed, but the landing deck, markings and collision remain. New roof ornaments are specifically forbidden on helipads.
- City street trees replace the green sphere canopies with branches and folded leaves using the shared low-detail mesh kit. Original trunk colliders remain.
- City streetlamps get mineral stems and crystal lantern heads at the existing lamp locations. No new Light components are added.
- City planters receive ferns and crystal flowers. Fountains receive small crystal ornaments over their existing basins. Palm trees retain their trunks and fronds, with crystal flowers at their crowns.
- Benches, planters, monorail supports and track structures, canal walls and railings, sign backings and selected station structures use the shared fantasy materials. Signs keep their existing readable names. Road markings and traffic signals retain their functions.
- Inner-city intersection surfaces now match the existing mineral road strips; city sidewalks use stone paving.

## What is NOT a completed replacement

This is not a claim that every object in every realm is now a bespoke model. Hand-built snow/outpost/realm geometry remains protected. Vehicles, characters, enemies, creature models, road traffic logic, breakable street objects and gameplay UI were not rewritten. Some structures have material changes rather than new geometry. Palms are decorated, not remodelled. High-detail landmark selection still follows 02D's cap. Missing resources can intentionally fall back to old visuals and are logged.

## Verify the update

After world generation, search the Unity Console for:

Crystal City 02E | Eligible buildings

The line reports detailed, lightweight, original-fallback and protected counts, plus changed tree/lamp counts. It is a coverage diagnostic, not proof of rendering quality. Expected eligible accounting is detailed + lightweight + original fallback. Any original fallback or art exception needs investigation.

The orange apartment towers seen beside the purple landmarks in the previous screenshot should now use the new ordinary-building exterior, provided they are generated ordinary buildings rather than protected custom assets. Judge coverage from those formerly modern towers and nearby street trees, not only from the already-detailed Gothic building.

## Tests and rollback

65 Python source and reference-arithmetic checks were executed successfully. Unity compilation, native tests, visual appearance, frame rate and memory remain untested here. Read Docs/BUILD_02E_QA.md for details.

The installer prints a backup folder. It includes RESTORE_02E.cmd. Close Unity before restoring; restore refuses to overwrite post-install edits and moves newly added files into the backup rather than silently discarding them. No Git history is rewritten.

After a successful playtest, review the listed source changes in GitHub Desktop and commit/push 02E on the art branch. Leave unrelated Assets/_Recovery files out of the commit. There is no requirement to merge into main to test this build.
