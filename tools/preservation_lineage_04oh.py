"""Exact inherited owner versions, not permission for future edits or native acceptance."""
import hashlib
import subprocess
from pathlib import Path


REVIEW_ANCHOR = '6d5ca2cafa7dbff4f482aea3a0b57915df798a17'
PREFIX = 'Assets/NeonSyndicate/'
# These nine versions were inspected against 721c1238 during the owner's L1 CI investigation.
# Future edits must get their own review, not a broad OWNERS exemption.
REVIEWED = {
    PREFIX + 'Scripts/Runtime/AI/PedManager.cs': (
        'dd05470ad9bbb2435bcfef9aadb0996477f91d07',
        '4d1d8303816f4e19e9994d876514e3274c18bc45f9e87a9e035a6488ac4cb9d6',
        'Existing family/elder spawning, original NPC owner and child safety.'),
    PREFIX + 'Scripts/Runtime/Characters/Appearance.cs': (
        'dd05470ad9bbb2435bcfef9aadb0996477f91d07',
        '8abcbe7c538d1a7dbdb07f9e216d7abe0621945b5c2381f346ffc3f11a7a1656',
        'Existing age field, child/elder appearance factories and equality.'),
    PREFIX + 'Scripts/Runtime/Characters/PlayerCamera.cs': (
        '497702a14fb6dae0cc461f8547947f041f7d4ce6',
        '58724e171a2cea6429563e76373c03dade03c2393c70f37fd19f44ed89c9c095',
        'Existing generated-interior camera boom bound.'),
    PREFIX + 'Scripts/Runtime/Characters/WeaponGrip.Table.cs': (
        '8fd800ac9897cbe693270fb20b418ec369d40a7d',
        'def06589b34396acb76dfa3d1af1b9725fe8739cd9c9c091d64f2a5c9e066308',
        'Existing melee hand grip; no weapon damage change.'),
    PREFIX + 'Scripts/Runtime/Core/MeshKit.cs': (
        '89644e85aa9b3270d2755ceab5f12a1ebe88e49c',
        '6b6b66f8d3ee5c17c215d3e04e98f5c0bab379807a3e5a4ac0f0710b5d724306',
        'Existing read-only triangle accessor used by wild geometry.'),
    PREFIX + 'Scripts/Runtime/Core/WorldBounds.cs': (
        '497702a14fb6dae0cc461f8547947f041f7d4ce6',
        '9006533a07a48b7e871e700b4d8665da0e5e53bf84c069bd57e356411f148ccd',
        'Existing generated-interior region classification.'),
    PREFIX + 'Scripts/Runtime/World/CityDressing.cs': (
        'b47f7b33466251fd183f455cc7cc57a5d57a7e2c',
        '609cc24008d7e9a4d8140087607d673cd56039f4a7c4743c3ed32e641cfae462',
        'Existing rugged mountain silhouettes at the original mountain specs.'),
    PREFIX + 'Scripts/Runtime/World/InnerWorld.cs': (
        '497702a14fb6dae0cc461f8547947f041f7d4ce6',
        'fb92d15a3131959e127ee2dc8274760b9a7d4d679a389ce6d2de2886759593f9',
        'Existing refusal to open the Veil inside generated interiors.'),
    PREFIX + 'Tests/EditMode/SaveDataTests.cs': (
        '89644e85aa9b3270d2755ceab5f12a1ebe88e49c',
        '545816b39d74ce31d45fd4aaa7673280ca501644264aa120ecfc2e8000ba117d',
        'Existing freeze list appends Interiors and Wilds; no previous field removed.'),
}


def verify_reviewed_paths(root, unexpected, records=None, anchor=REVIEW_ANCHOR):
    """Unknown paths and any mismatch fail. Pinned files are checked even after a revert to the old parent."""
    root = Path(root)
    records = REVIEWED if records is None else records
    rows = []
    for path in sorted(set(unexpected) | set(records)):
        row = {'path': path, 'passed': False, 'reason': ''}
        if path not in records:
            row['reason'] = 'Existing file changed outside an authorized owner or exact reviewed version.'
            rows.append(row)
            continue
        commit, expected, purpose = records[path]
        row.update({'source_commit': commit, 'expected_sha256': expected, 'purpose': purpose})
        lineage = subprocess.run(['git', 'merge-base', '--is-ancestor', commit, anchor],
                                 cwd=root, capture_output=True)
        if lineage.returncode != 0:
            row['reason'] = 'Recorded source commit is missing or is not an ancestor of the fixed review anchor.'
            rows.append(row)
            continue
        blob = subprocess.run(['git', 'show', commit + ':' + path], cwd=root, capture_output=True)
        if blob.returncode != 0 or hashlib.sha256(blob.stdout).hexdigest() != expected:
            row['reason'] = 'Recorded digest does not match the immutable source commit blob.'
            rows.append(row)
            continue
        try:
            actual = hashlib.sha256((root / path).read_bytes()).hexdigest()
        except OSError:
            row['reason'] = 'Reviewed owner file is missing or unreadable.'
            rows.append(row)
            continue
        row['actual_sha256'] = actual
        row['passed'] = actual == expected
        row['reason'] = 'Exact reviewed inherited version.' if row['passed'] else 'Reviewed owner changed again; inspect the new diff before updating its proof.'
        rows.append(row)
    return rows
