"""Review historical melee-grip and appended schema-test changes, independently of district art.

Source commits: 8fd800ac (melee grip), 562b9825 (grade), 89644e85 (schema).
No live actor, schema or test file is changed by this module.
"""
import hashlib
import json
import subprocess
from pathlib import Path
from saved_schema_lineage_l2 import verify as verify_append_only_schema

BASE = 'ab5887779a9f3e6ed0775721d56237e247d7b81b'
CONTRACT_SHA256 = '10feb98d8f31257eac44a5d69de0602a553746291e2e73a08fc67ed0ff10cca9'


def original_view(root, path, current):
    raw = Path(__file__).with_suffix('.json').read_bytes()
    if hashlib.sha256(raw).hexdigest() != CONTRACT_SHA256:
        raise ValueError('Historical H5 continuation literals changed.')
    data = json.loads(raw)
    if data['base'] != BASE:
        raise ValueError('Historical H5 baseline changed.')
    row = data['paths'].get(path)
    if row is None:
        return current
    before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root)
    if hashlib.sha256(before).hexdigest() != row['before_sha256']:
        return current
    expected = before
    for edit in row['edits']:
        old, new = edit['before'].encode(), edit['after'].encode()
        if not old or expected.count(old) != 1:
            return current
        expected = expected.replace(old, new, 1)
    return before if current == expected else current


def inherited_tests_preserved(root):
    paths = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', BASE, '--',
        'Assets/NeonSyndicate/Tests/EditMode'], cwd=root, text=True).splitlines()
    for path in paths:
        file = root / path
        if not file.is_file():
            return False
        before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=root)
        if original_view(root, path, file.read_bytes()) != before:
            return False
    # The two appended test entries must still describe the exact previously shipped append-only schema.
    return verify_append_only_schema(root)
