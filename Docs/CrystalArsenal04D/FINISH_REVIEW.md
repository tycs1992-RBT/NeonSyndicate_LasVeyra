# 04D finishing review: save recovery and next-session handoff

## Scope and readiness

Review base: `9e884340226c9e06b9a2f843c9e1628ef921a011` on
`feature/crystal-arsenal-04d`. This follow-up keeps the six implemented weapons,
arsenal acquisition, finite encounter, boss signatures, art and prior review fixes.
It does not restart 04D or add another gameplay system.

Readiness remains **native testing pending**. The source review found and patched
one additional save-recovery defect. The added regression cases have NOT been
executed here. The original handoff's 12-configuration compile and 730 passing
tests are historical results from the prior agent, not rerun results for this
follow-up. Do not add the new tests to that historical passing count.

## Finding: saving after recovery destroys the previous valid backup

The existing `ReadValidated` correctly rejects an invalid main file and reads its
valid `.bak`. It does not replace the invalid main file. The previous `Install`
then unconditionally supplied `.bak` to `File.Replace`, or copied the main onto
`.bak` on the portable path. The next save could therefore replace the good
recovery snapshot with the invalid primary it had just rejected.

Discriminating regression sequence:

1. A valid snapshot A is in `.bak`; the main file contains rejected text.
2. ReadValidated returns A with fromBackup=true.
3. Save a valid new snapshot B.
4. The backup must still contain A, byte for byte, not the rejected text.
5. Corrupt the main again in this temporary fixture; recovery must still return A.
6. After another healthy save C, ordinary rotation should put B in the backup.

This is a source-traced defect, not a claim to have reproduced a Unity failure or
inspected the owner's actual saves. Do not perform intentional corruption on the
owner's save directory. The NUnit fixture creates disposable temporary files.

## Patch

The same supplied validation predicate now gates rotation of the previous main
file. A valid readable main becomes the next backup as before. An invalid or
unreadable main never replaces an existing backup. The atomic path passes a null
backup destination when rotation would be unsafe; the portable fallback follows
the same rule and keeps the verified temporary file if installation fails.

The fallback is extracted into a private method so tests exercise the real
production fallback explicitly, rather than depending on the test filesystem
rejecting File.Replace. No Unity object, save schema, serialized field, weapon
ID, public transaction result value or item-reward rule is changed. Existing valid
metadata is untouched. Null-validator behavior remains compatible: readable text
is accepted because the caller supplied no rejection policy.

Reference: Microsoft System.IO.File.Replace documentation states that a null
backup argument creates no backup, and an existing backup destination is replaced
with the old main file. This patch relies on that documented distinction.

The existing single-writer/main-thread assumption remains. This is not a new
cross-process save lock or protection against arbitrary disk damage. The
portable copy fallback is not an atomic filesystem transaction.

## Added regression fixture

`Assets/NeonSyndicate/Tests/EditMode/SaveRecovery04DTests.cs` defines 18 cases:
seven scenarios run once through public Write and once through the actual private
portable fallback, plus four failure-path cases.

The scenarios cover recovery and resave, a second corruption, normal rotation
after recovery, healthy primary rotation, absent primary, invalid primary
without a backup, exception-throwing validation of the old primary, unchanged
null-validator behavior, rejected new data, throwing validation of new data and
blocked installation retaining the recovery backup and verified temporary file.

Status: **AUTHORED, NOT RUN**. No Unity Editor, .NET SDK, Mono compiler or NUnit
runtime is installed in this review environment, and attempted tool bootstrap
could not download dependencies. Source/diff checks are not a replacement for C#
compilation or execution. Run this fixture and CrystalFoundation04DTests in the
pinned Unity Editor, or in the existing real offline C# test harness, before
calling the save-recovery regression passed.

## Preserve the delivered implementation

This review changes only SaveFileTransaction.cs, adds its focused regression
fixture and metadata, and updates handoff documentation. Do not revise the crystal
weapon balance, reward indices or combat behavior on speculation.

The original reference files remain authoritative for the delivered gameplay:

- `START_HERE_04D.md`: normal acquisition and combat route.
- `Docs/CrystalArsenal04D/CHECKPOINT.md`: per-system implementation state.
- `Docs/CrystalArsenal04D/KNOWN_ISSUES.md`: native visual/physics acceptance items.
- `Docs/CrystalArsenal04D/DECISION_LOG.md`: stable design decisions.

## Next session, in order

1. Verify the current remote head and preserve local edits and pre-04D saves.
   Continue this branch. Do not switch to an older master, reorder uniques,
   recreate the project, replace all GUIDs, or merge main.
2. Run the pinned Unity 6000.3.16f1 import/compile and EditMode tests. Run the new
   SaveRecovery04DTests and existing CrystalFoundation04DTests first. Preserve the
   exact tested commit, logs and failure output. If no native Editor is available,
   report that limitation instead of claiming a native build.
3. In Free Roam, find the gold A by Veyra Lofts, claim/equip Shardwake, calibrate
   three posts, try Prism Thread, start and finish Resonance Breach, equip Gyre
   Crescent, then defeat the Foreman and claim Sunderheart. Save to a test slot,
   reload it, and confirm the actual weapon and its entitlement persist.
4. Test the remaining boss/weapon paths, inventory-full pending claims, beam and
   shard wall occlusion, pause/charge cancellation, aftermath quiet and old-save
   restoration. Capture actual player footage before changing visual tuning.
5. Polish from that evidence: readable daylight beams, correct muzzle/hand
   alignment, boss warning visibility, controlled impact sound/shake, and
   frame-time measurements in a busy fight. Keep signed-off mechanics intact.

## Owner update workflow

Close Unity, use the existing repository and the existing 04D branch, fetch and
pull without discarding local changes. No new ZIP installer or Unity project is
needed. Do not merge main. The native play route is still the gold A at Veyra
Lofts, not a separate debug scene. The user's earlier Windows metadata repair must
remain reconciled to exactly one TerritoryPlacement type.
