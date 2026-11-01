"""Project only explicit science-preview hooks for historical preservation checks.

The historical digests stay unchanged. A partial hook, extra edit or changed baseline
is never normalized. Live physical replacement has separate behavioral contracts.
"""
import hashlib
import json
import subprocess
from pathlib import Path

BASE = '2c1169f22cc3a6352a03a83815601911b18282d8'
CONTRACT_SHA256 = '21cc4b750436f1758fd1eb19c0e5d414de4a2d670239ba3140d4dc9171aa0b96'
PREFIX = 'Assets/NeonSyndicate/'
ADDITIONS = frozenset(PREFIX + p for p in (
    'Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.ScienceDistrict.cs',
    'Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceDistrict.cs',
    'Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceArchive.cs',
    'Scripts/Runtime/World/HeartFR01/HeartPlanFR01.ScienceClearance.cs',
    'Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.ScienceDistrict.cs',
    'Scripts/Runtime/UI/VisibleRealityV0.StructureProbe.cs',
    'Tests/EditMode/ScienceDistrictTests.cs',
))


def contract():
    raw = Path(__file__).with_name('science_district_hooks.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != CONTRACT_SHA256:
        raise ValueError('Reviewed science hook literals changed.')
    data = json.loads(raw)
    if data['base'] != BASE:
        raise ValueError('Science hook baseline changed.')
    return data


def original_view(root, path, current):
    row = contract()['paths'].get(path)
    if row is None:
        return current
    try:
        before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root, stderr=subprocess.DEVNULL)
    except subprocess.CalledProcessError:
        return current
    if hashlib.sha256(before).hexdigest() != row['before_sha256']:
        return current
    expected = before
    for edit in row['edits']:
        old, new = edit['before'].encode(), edit['after'].encode()
        if not old or expected.count(old) != 1:
            return current
        expected = expected.replace(old, new, 1)
    return before if current == expected else current
