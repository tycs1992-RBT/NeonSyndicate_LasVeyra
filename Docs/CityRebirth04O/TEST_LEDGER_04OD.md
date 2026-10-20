# 04O-D executed verification

Parent: 00f65a4158892f52c806ce153fe14e22e0fc79a5. First source recovery checkpoint: f12eb6e1fcd187d01982fbe0577fc613503367e6. Final tested source and exact results are filled by the release evidence step below.

Early focused runs found blocked regional branches, an unsupported direct court route on seed 1, missing boss parts/loot/arena definitions and a validator test built with incomplete world references. These were fixed in actual source. No genuine failure was converted to NeedsEngine. The raw pre-fix full reports are retained in Evidence04OD.

The historical assertions for highest known revision, allowed quest layouts and treatment catalog size were explicitly updated for additive revision 4 and four new recipes. Original bank filenames, earlier geography and the first ten recipe definitions continue to be tested. The source byte guard for BondSystem.Resonance04N is explicitly scoped to the two earned regional variants; no other unexpected old-owner byte differences are accepted.

Native Unity import/EditMode/PlayMode, real player-save behavior, actual controller, rendered visuals, profiling and Windows build: NOT RUN.


## Final executed result

Tested source commit: `3aa60ef9b301732c98b071ae54bebc9efbe662ec` on `feature/04o-shattered-metropolis-reborn`.

C# snapshot SHA-256: `f6113de19748d6349bb72f702afc529305826e02dacd5e0ae7e6461c962d25bc` (815 runtime, 11 editor, 126 test files; 952 total).

| Check | Actual result |
| --- | --- |
| Genuine Unity managed API matrix, C# 9 | 12/12 PASS: editor/player x classic/both/input-only x Built-in/URP |
| Runtime / Editor / EditMode assembly boundaries | 3/3 PASS |
| Roslyn syntax configurations | 12/12 PASS |
| Full external NUnit | 1692 discovered, 1634 executed; 1588 PASS, 46 FAIL, 58 unchanged NeedsEngine exclusions |
| Raw outcome | Failed / raw exit 46. Comparison wrapper exits 0 because no failure was introduced. |
| Failure comparison | Same 46 names AND identical failure messages as 04OC. Native ECall failures remain failures. |
| New 04OD cases | 22/22 executed and passed inside the final full suite |
| Original-owner source guards | 14/14 current guards pass; raw inherited byte guards retain explicit scope exceptions |
| Unity metadata | 1772 sidecars, zero missing/invalid/unresolved references; 13 auditor regression cases pass |
| Git LFS / whitespace | git lfs fsck OK; git diff --check clean |
| Regional geometry | Actual integrated export: 5752 vertices / 2688 triangles, zero degenerate faces or normal mismatches. Counts prove geometry existence only. |
| Revision-3 geometry preservation | World solids, terrain and forest source export SHA-256 values match the 04OC baseline |
| Multi-seed source routes | 61724, 1, 2016, 77231, 92017 pass service/receipt preservation, connected branches and clearance assertions |
| Native acceptance | NOT RUN for every native category |

One assembly-boundary attempt was started before the matrix report existed and could not write its report. It was rerun after matrix completion and all three assemblies passed. This was a tooling dependency error, not a reclassified game test.

Reproduce from the exact source using the invocations in Evidence04OD/api-matrix.json, assemblies.json, external-tests.json and syntax-matrix.json. These retain actual executable/reference paths from this host. The raw final NUnit XML is retained compressed in Evidence/04od-final-nunit.xml.gz; the full API-reference report is in Evidence/04od-api-matrix-full.json.gz.

Run `python3 tools/check_crystal_realms_04od.py --report <path>` for the focused source guards, `python3 tools/check_unity_metadata_04c.py --report <path>` for metadata, and `python3 tools/measure_crystal_realms_04od.py --root . --dotnet <dotnet> --unity-references <Unity player Managed directory> --assembly <compiled player-classic-builtin.dll> --output <directory>` for topology and mesh exports. Those tools do not launch Unity.
