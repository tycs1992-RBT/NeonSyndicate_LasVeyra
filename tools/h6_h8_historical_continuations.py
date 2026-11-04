"""Exact later-owner literals for H6-H8 historical comparisons.

Reviewed changes are the existing melee grip (8fd800ac), generated interiors
(497702a1) and wilds/schema lifecycle (89644e85). Live sources are not modified.
Unknown bytes or partial changes remain visible to the original assertions.
"""
import hashlib
import json
import subprocess
from pathlib import Path
from saved_schema_lineage_l2 import verify as verify_append_only_schema

CONTRACT_SHA256 = '46ab126edcc7e2e9479d1922b02ffb36bc206673b336ed6b7ce73ba172a5c9d6'


def contract():
    raw = Path(__file__).with_suffix('.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != CONTRACT_SHA256:
        raise ValueError('Reviewed H6-H8 continuation literals changed.')
    return json.loads(raw)


def original_view(root, path, current, baseline):
    row = contract().get(baseline, {}).get(path)
    if row is None:
        return current
    before = subprocess.check_output(['git', 'show', baseline + ':' + path], cwd=root)
    if hashlib.sha256(before).hexdigest() != row['before_sha256']:
        return current
    expected = before
    for edit in row['edits']:
        old, new = edit['before'].encode(), edit['after'].encode()
        if not old or expected.count(old) != 1:
            return current
        expected = expected.replace(old, new, 1)
    if path.endswith('/SaveSystem.cs') and not verify_append_only_schema(root):
        return current
    return before if current == expected else current
