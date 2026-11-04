# FR02A executed verification

Fetched baseline: 4e6246006836db88222303bc41bee24b74c90114. Tested C# SHA256: 07e7a7690a0b8b3479e3a891961c833791ae5ec4249d4cba89bfa900dc06d67f. Exact tested source and normalized publication identities are pinned after source publication in FINAL_SEAL.json. No production C# changes occurred after these captures.

| Check | Executed outcome |
|---|---|
| Genuine Unity6000.3.16f1 managed API, external NUnit | 12/12 PASS, zero added warnings versus fresh baseline |
| Genuine API, bundled project NUnit | 12/12 PASS |
| Runtime, Editor, EditMode assembly boundaries | 3/3 PASS |
| Roslyn C#9 syntax | 12 modes, zero errors |
| Focused FR01, FR01E, FR02A and live-folder pure tests | 51/51 PASS,17 new FR02A |
| Fresh fetched baseline full managed NUnit | 2382 passed,47 failed,64 excluded,raw FAILED |
| Final source full managed NUnit | 2399 passed,47 failed,64 excluded,raw FAILED |
| Failure identity/reason comparison | No introduced/resolved failure names or changed reasons |
| L1,L2,L3 original-owner guards | 51/51,65/65,63/63 PASS |
| H3,FR01 source/art guards | 78/78,144/144 PASS |
| FR02A real asset/frozen-owner checks | 185/185 PASS |
| Unity metadata/reference audit | PASS; actual AssetDatabase/native import NOT RUN |
| Independent strict FBX, UV, normals, tangents, bitangents | 39/39 PASS |
| Actual Blender reimport/UV/bounds | 39/39 PASS; original FR01 33/33 also reimported |
| New full-height side walls, all 9 building families x3 LOD | 27/27 PASS |

The genuine .NET harness is not Unity Test Runner. Its47 raw native-call failures remain failures. All preexisting test bytes and exclusion categories remain unchanged. Raw XML and logs are retained as compressed archives, with exact current/baseline JSONs.

Fresh baseline and current H5 source guard both return46/48. Exact unchanged failures: `Original actor/gameplay authority / Characters/HumanoidPose.cs`; `All prior tests and exclusions preserved`. No assertion or old CI stage was removed or weakened. GitHub final-head results and later skipped steps are recorded in the publication seal. A skipped stage is never a pass.

Development corrections: the first footprint algorithm over-reserved angled buildings; it was replaced by exact oriented segment clipping with the same full-width margins. A local garden-leaf origin error was found in actual bounds and corrected before delivery. A Geometry Nodes realization left an unused null material slot; it was explicitly mapped to the original moss material and all39 final exports rechecked. The first DCC invocation tried to load the currently open blend as its own library; the corrected invocation uses the immutable fetched baseline. None of these tool failures was a Unity run. Long full-suite attempts interrupted without XML were not counted; the completed current run lasted380.411109seconds and is the only current full result claimed.

Native project import/shaders, Unity Test Runner, PlayMode, actual saves/devices, collision/camera, screenshots, Profiler and Windows build/launch: NOT RUN. No FPS, frame-time or visual score is invented.
