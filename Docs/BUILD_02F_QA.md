# Build 02F verification record

## Baseline and scope

The working reconstruction used the supplied 01C project plus the shipped 02D files and the approved 02E update. The online art branch was read and remained at 44a4ef4 during the initial access check; the approved 02E changes are local package changes, not assumed to exist online. The installer validates the actual local 02E source hashes before writing.

## Executed here

- Actual OBJ file validation: finite coordinates, normals and UVs, in-range triangle indices, nonzero areas, winding/normal agreement and lower-detail triangle reductions for all 28 model files.
- Actual PNG header, dimensions and texture importer checks for all 16 maps.
- Actual JSON composition validation: valid model/material references, positive transforms, conservative declared bounds and bounded animation rates.
- Lexical source checks, automatic integration checks, cache ownership checks and independent roof/path arithmetic. These are not a C# parser or compiler.
- Source comparison showing that the two modified existing files contain only the intended build/reset hook additions relative to 02E.
- The prior 02E offline source-contract suite, without its unavailable historical-baseline option.
- Executed the portable asset-authoring recipe in a separate project fixture. All 94 generated asset and metadata files matched the shipped versions byte-for-byte.
- Software asset rendering of the shipped OBJ files and composition JSON. The three preview images were inspected; framing was adjusted to include the tallest landmark.
- Python tests of package integrity, installer decisions, idempotence, canonical text hashes and rollback policy. They do not execute Windows PowerShell.

Results: 609 new asset/source assertions passed, the prior 02E source suite passed 60 checks, and all 31 Python installer-policy tests passed. Machine-readable reports accompany this file. Test counts reflect individual assertions about files and rules, not separate game playtests or a quality score.

## Not executed here

Unity Editor compilation, native EditMode tests, actual OBJ import inside Unity, live gameplay, frame-time measurements, memory profiling, Windows PowerShell execution, multiplayer checks and game review scoring were NOT performed. No claim of passing those gates is made.

The new `CrystalCity02FTests.cs` contains 12 Editor test methods covering catalog data, readable models, cheaper LODs, shared resource lifetime, malformed inputs, protected roof types, deterministic selection, roof scale, garden path exclusion, night/rotation math, budgets and map-marker cleanup. They are provided for Unity execution, not reported as passing.

## Local playtest priorities

1. Import with Unity 6000.3.16f1. Stop on a red compile error and preserve its full first entry.
2. Start Free Story. Check for `Crystal City 02F | Ready.` and inspect its art-failure count.
3. Open the full map, find a `*` landmark marker and inspect the structure from the adjacent street. Check the home/clinic crest as an immediate near-ground indicator.
4. Check the three palettes and a fountain or planted box. Camera visibility and pedestrian/vehicle routes must remain clear.
5. Verify rings move on Medium/High/Ultra, freeze when gameplay is paused and stay stationary on Low.
6. At night, inspect the local accent without a fullscreen color cast. Verify leaving the area disables distant accent lights.
7. Drive, fight, interact and load a save. Existing controls and objectives should be unchanged.
8. Return to the title, restart play, and repeat with domain reload on/off. No duplicate landmark roots or stale map markers should remain.
9. Compare the same location on 02E and 02F using the same graphics preset, weather, clock and camera. Record CPU/GPU frame time, load time, peak memory and draw calls in the Unity Profiler.

## Known limits and risks

The landmarks are above roof level and require looking upward. The new full-map markers and coordinate logs help locate them. Layouts without eligible roofs or park pockets legitimately receive fewer decorations. Current clearance logic cannot predict future dynamic event placement. Decorative meshes have no physical collision, so future flight gameplay will need an explicit collision and traversal design before these become playable spaces.

The importer metadata follows the project's existing OBJ import pattern but still needs native Editor validation. The offline preview uses an inspection renderer with different lighting and materials from Unity; it is not an in-game before/after comparison.

The package does not delete or ignore `_Recovery` files. Git does not automatically ignore those files merely because another version-control product does.
