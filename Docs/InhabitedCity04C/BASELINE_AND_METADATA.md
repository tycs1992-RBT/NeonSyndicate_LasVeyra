# 04C baseline and import repair

The inspected project root is `/workspace/NeonSyndicate_LasVeyra`. The verified starting commit is `c49144150cfeb2f105f628f7f9d6ed85c1ccb218` on `feature/living-territories-04b`; 04C branches from that commit. The user's Windows project and regenerated local metadata were not accessible. Their reported Play Mode session establishes local relay installation for that session, not a native acceptance result produced here.

## Confirmed defects and isolated corrections

The full regular-asset audit found two malformed metadata GUIDs:

| Asset sidecar | Invalid baseline value | Repaired unique value |
| --- | --- | --- |
| `Assets/NeonSyndicate/Scripts/Runtime/Gameplay/Territories/TerritoryPlacement.cs.meta` | `180937b2c4b745e4bb42615b272b6be5e` | `c6a3378a91b84d35903bbac8cb5a6990` |
| `Assets/NeonSyndicate/Tests/EditMode/TerritoryPlacementTests.cs.meta` | `18b37ff5a738427e9f634318eb79508f1` | `1f4dcc5b5a1e476b8596a48ae4667540` |

Both old values contain 33 hexadecimal characters. Both replacements contain exactly 32 and have no duplicate owner. The old values occurred only in their own sidecars, so no serialized asset reference needed to be redirected. The runtime script remains at its existing repository path, with exactly one `NeonSyndicate.TerritoryPlacement` class. Its source and placement safety checks are unchanged.

The reference audit also found **56 invalid shader references**, one in every `Resources/VisualRebirth04A/Materials/*.mat`. Each used `0000000000000000f0000000000000000` (33 characters). These references now use `0000000000000000f000000000000000` (32), matching the canonical built-in reference already serialized in this project's other materials and settings. The intended Standard shader file ID remains 46. The same literal in `tools/generate_visual_rebirth_04a_surfaces.py` is corrected so regeneration cannot reintroduce it.

This repair changes neither the materials' own GUIDs nor their texture references, colors, surface parameters, or names. The complete 59-file repair list, before/after hashes and values are in [metadata_repairs.json](Validation/metadata_repairs.json): two sidecars, 56 material shader references and one generator literal. Existing 04A preview and validation files remain historical evidence for their recorded revision; they have not been rewritten to imply a new native render. The material manifest describes unchanged texture surfaces and does not contain material-file hashes.

Invalid GUID formatting is verified. The historical Windows compiler/import error and the visual effect of the malformed material references have **not** been reproduced in Unity here. The earlier offline audit checked uniqueness but missed exact GUID length; its pass count did not establish import success.

## Executed verification

| Check | Result |
| --- | --- |
| Before repair | 1,115 regular metadata files; two invalid GUIDs, 56 invalid serialized references. No duplicates or unresolved well-formed references. |
| After repair and new regression test | 1,116 regular metadata files; zero invalid GUIDs, duplicates, missing sidecars or orphan sidecars. |
| Reference resolution | 388 local references and 110 built-in references resolved; zero invalid or unresolved references. |
| Existing valid metadata | All 1,113 previously valid regular sidecars remain byte-identical. |
| Placement type | Exactly one source declaration; original runtime source unchanged. |
| Metadata regression tool | 13/13 cases passed, including both 33-character script values, the malformed shader value, malformed suffix/nonhex/short input, duplicates, missing/orphan sidecars and unresolved references. |
| Supplemental C# comparison | 418 baseline versus 419 working runtime/test sources; 35 older-Unity API diagnostics in both; zero introduced diagnostics. This is not a pinned Unity build. |
| Native test attempt | `tools/test.sh` exits 3 with `TESTS NOT RUN`, Unity 6000.3.16f1 unavailable. |

Evidence: [before](Validation/metadata_before.json), [after](Validation/metadata_after.json), [preservation](Validation/metadata_preservation.json), [compiler comparison](Validation/diagnostic_comparison.json), [environment](Validation/environment.json), [native attempt](Validation/unity_test_attempt.txt).

The regular audit scans `Assets` sidecars plus serialized GUID references in `Assets`, `Packages` and `ProjectSettings`. Exact whole-value matching prevents accepting a legal 32-character prefix followed by an extra character. It identifies unresolved external/package references as failures rather than pretending they resolve without their package assets.

`Assets/_Recovery` contains inherited recovery scenes, including `0 (1).unity` without a sidecar. Those files are explicitly inventoried as excluded recovery content, not counted as valid project content or modified by this repair. They were already tracked in the baseline. No cache, recovery scene, external backup, browser build output or `tools/__pycache__` is added by this work.

## Native regression coverage, not yet executed

`UnityMetadata04CTests.cs` adds format regression cases and three Editor checks:

1. The actual project metadata is complete, unique and exactly 32 hexadecimal characters, excluding recovery content.
2. `TerritoryPlacement.cs` is an imported `MonoScript`, its AssetDatabase GUID maps back to the same path, and the actual runtime compilation source input contains the type exactly once.
3. All 56 authored 04A materials import with the intended Standard shader, rather than a missing/error shader.

The native checks require the pinned Editor. The older-reference comparison does not run AssetDatabase or compile Editor-only test branches. Native import, native runtime/Editor compilation, EditMode execution, gameplay, real screenshots and performance remain **NOT RUN**.

Reproduce the source audit from the repository root:

```bash
python3 tools/check_unity_metadata_04c.py --report Docs/InhabitedCity04C/Validation/metadata_current.json
```

## Reconcile the owner's working Windows repair

The delivered branch keeps the canonical repository filename `TerritoryPlacement.cs` and its class name, and supplies a valid new GUID. It does not assume that the unavailable Windows GUID equals either new cloud value.

Before switching branches, preserve the locally working script/meta pair and any original same-class pair in a backup **outside** the Unity project. Keep any other local edits through the user's usual reviewed commit or stash workflow. Never discard the working copy to make checkout succeed.

Use the delivered single script/meta pair after the branch switch. Do not copy `TerritoryPlacement04B.cs` back alongside `TerritoryPlacement.cs`, and do not blindly reapply an old stash containing the rename over the repaired pair. That would reintroduce two definitions or overwrite the verified metadata. If the local GUID must be retained because local scenes reference it, reconcile that one pair and its actual references as a reviewed follow-up using the preserved backup. No local reference mapping has been invented here.

The Editor import test and actual runtime source-input test provide a concrete check after reconciliation. No full cache deletion, new project, manual prefab placement or handwritten gameplay script is required by this repair.
