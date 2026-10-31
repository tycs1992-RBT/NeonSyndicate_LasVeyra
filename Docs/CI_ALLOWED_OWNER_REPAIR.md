# Allowed-owner preservation repair, L1 gate check

Fetched head: 6d5ca2cafa7dbff4f482aea3a0b57915df798a17. PR12 stays draft and unmerged.
V0 owner visual acceptance is not evidenced. The last owner screenshot in this thread shows a pull blocked by local
metadata; it does not show Unity gameplay. L1 pavilion, new placement grammar and world expansion are gated by the
owner's explicit precondition. This delivery changes only preservation tooling and its regression tests.

## Ownership map before changes

| File | Scoped change | Authority |
|---|---|---|
| tools/check_ascension_04oh.py | Keep every existing assertion; explain exact inherited exceptions using immutable history | Existing original-owner guard |
| tools/preservation_lineage_04oh.py | Nine exact existing file versions, pinned to prior commits and SHA256 | Pure validation only |
| tools/test_preservation_lineage_04oh.py | Reproduce modifications, deletions, hash forgery and unsupported paths | Guard regression tests |
| .github/workflows/04oh-art-proof.yml | Run guard regressions before the existing preservation step | Existing CI job |

No C#, asset, save, renderer or generated project file is changed. No gameplay or content ID is introduced.

## Diagnosis

The old assertion compares existing files against gameplay parent 721c1238. Nine files changed in later commits without
being accounted for in that assertion. Their entire diff is 96 insertions and 11 deletions: child/elder appearances and
family pedestrians (dd05470), generated-interior camera/region/Veil checks (497702a), melee grip (8fd800a), a read-only
mesh triangle accessor and appended Interiors/Wilds save freeze fields (89644e8), and the mountain silhouette (b47f7b3).
Those source changes already exist at fetched V0 head. This repair does not make or undo them.

## Narrow correction

The nine files are not added to the unrestricted OWNERS list. The validator accepts only their exact recorded bytes,
requires SHA256 agreement with the recorded Git blob, and requires each source commit to be an ancestor of the fixed
V0 delivery. Missing history, a deleted file, an unreviewed path, a new byte change, a forged digest or a future commit
fails. All original assertions remain. Each history record is separately reported so future failures name the file.
Existing guard failures elsewhere remain separately visible; correcting this assertion does not mean the whole legacy
workflow is green. Native Unity acceptance is independent of CI and remains NOT RUN / OWNER NOT CONFIRMED.

## Executed checks and remaining gate

The original guard reproduced 16/17 before this repair and passes 26/26 afterwards. Ten Python regression tests pass, including a changed/deleted/reverted case for every one of the nine pinned paths. Metadata passes with 2,446 sidecars; 6 art-drop and 7 skin tests pass; LFS fsck, Python compilation and diff check pass. The game C# digest remains `eb635b200d08fc5819f7cb8350d5edc0d0b1030c8ed19b1361312a2cbc738969`, identical to tested V0. No runtime compile or native play claim is added.

The next legacy CI guard still fails `Append-only saved schema untouched` (14/15). Its script and every source/asset input are unchanged from fetched V0. Other H5/H6/H7/H8/H8S guard failures exactly match the recorded V0 failures. This narrow correction intentionally does not relabel those tests green. Raw before/after checks and the comparison are under `Docs/CIAllowedOwnerRepair/`.

## Exact owner action

After the update is pulled, open Unity 6000.3.16f1, wait for imports and live-character preparation, choose Free Roam, and follow Pause > EXPLORE. Check Malik, Dante and the Forge service, nearby fitted people, one live-art structure, the creature sanctuary and the wild destination. Return the first failed step with its Console error and F2 > Live characters, or explicitly report that the V0 route passed. The last pull-error screenshot does not establish a visual pass or a gameplay failure. Until that evidence arrives, no pavilion, collision/clearing expansion or L1 acceptance is claimed.
