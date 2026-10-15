# Build 02E: Audit and verification

## Source basis

Development baseline: art/crystal-fantasy-block-proof at 44a4ef4fa808a6feab69dc80cb24d184594f06ae. The live branch and relevant source blobs were read through GitHub. Local originals of CityBuilder.cs, CityDressing.cs and DistrictArchitecture.cs were verified against their Git blob IDs before editing. Other sources used for API inspection came from the previously attached 01C archive plus the verified 02D changed-file archive.

No claim is made that a full new checkout or a full native build ran in this environment.

## Executed checks

Command:

python tools/check_crystal_city_02e.py --baseline <prepared-02D-source-copy> --output <report-path>

65 checks passed, zero failed. The command performed:

- Delimiter checks on the seven changed/added C# files, metadata presence/GUID format checks, and unsupported-attribute checks.
- Verification of the automatic city-builder integration calls, both original and themed paths, the no-quota building policy and the static cache cleanup hook.
- Exact comparison of the argument text of existing AddBoxCollider and AddCapsule calls in CityBuilder.cs and CityDressing.cs against the prepared baseline. These are source-level invariants, not live collider snapshots.
- Checks that the new visual module adds no colliders, rigidbodies, GameObjects, per-decoration Update methods, camera modifications, time changes or new package APIs.
- Independent Python arithmetic over 392 coverage inputs and 630 window triangles, checking finite, nondegenerate, outward-facing reference geometry. This does not execute the actual C# methods.
- Comparison of other files in the prepared source copy for unrelated edits.

The exact result and source SHA-256 hashes are in BUILD_02E_OFFLINE_REPORT.json. These checks are not a C# compiler, parser or Unity test runner. No test score or review percentage is assigned.

## Included, not executed

CrystalCity02ETests contains six Editor tests for no-quota coverage, helipad roof protection, invalid/custom exclusions, facade-cost bounds, ordinary generated-layout coverage, and transformed shared-mesh data. These tests require the project's actual Unity Test Framework. No package or assembly-definition change was made to support them.

Unity Editor compilation, all native EditMode tests, Free Story gameplay, shaders, screenshots, frame time, memory and startup performance have NOT been measured here. The installer uses Windows PowerShell; its source and manifest were reviewed and its file-policy logic was mirrored in Python tests, but the PowerShell executable is unavailable here.

## First local acceptance pass

- Let the pinned Editor compile. Stop on the first new red Console error.
- Start Free Story and inspect the coverage line beginning Crystal City 02E.
- Return to the formerly orange or white apartment towers. Verify their old rectangular facade sheet has been replaced with the new stone and pointed-window exterior.
- Look at ordinary street trees, lamps, a planter and a fountain. Verify their new forms are present and sensible in scale.
- Walk/sprint, switch cameras, enter and drive a car, take sharp corners, exit and interact with an existing quest.
- Check that helipad access and landing markings remain usable.
- Load an existing save and enter Play Mode a second time; check for duplicate state, missing meshes or material errors.
- Compare the same route before/after at the same quality setting with the Profiler. Report frame time, startup time, memory and active rendering counts rather than assuming a target FPS.

## Known costs and limitations

Lightweight facades are merged into the existing 160m material chunks, avoiding a new renderer per window or building. Each tier limits each face to four bays and two registers. Shared source arrays are cached once rather than repeatedly reading Mesh.vertices for every street tree. These are performance design choices, not measured guarantees.

There are more vertices and material groups than in 02D, and distant lightweight detail uses the original chunk culling rather than independent window LODs. If memory or frame time regresses, reduce per-face ornament or add chunk LODs before increasing the detailed-landmark budget.

New roof details and wall bevels are visual-only. Existing collision calls are preserved, but visual clipping and contact feel still need a playtest. The old 02C non-colliding pitched gables on ordinary buildings were replaced by smaller perimeter ornaments that leave rooftop centres open. Custom-built sites, terrain and dynamic props remain outside this replacement path.
