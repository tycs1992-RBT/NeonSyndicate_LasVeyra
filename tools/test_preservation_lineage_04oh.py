"""Regression tests for the exact historical-owner correction. No Unity execution."""
import hashlib
import importlib.util
import subprocess
import tempfile
import unittest
from pathlib import Path

from preservation_lineage_04oh import PREFIX, REVIEWED, verify_reviewed_paths


class ReviewedOwnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(prefix='owner-proof-')
        self.root = Path(self.temp.name)
        self.git('init', '-q')
        self.git('config', 'user.name', 'Guard regression')
        self.git('config', 'user.email', 'guard@example.invalid')
        self.paths = list(REVIEWED)
        for path in self.paths:
            file = self.root / path
            file.parent.mkdir(parents=True, exist_ok=True)
            file.write_bytes(('original ' + path + '\n').encode())
        self.git('add', '.')
        self.git('commit', '-qm', 'old parent')
        self.parent = self.git('rev-parse', 'HEAD').strip()
        self.contents = {}
        for path in self.paths:
            self.contents[path] = ('reviewed ' + path + '\n').encode()
            (self.root / path).write_bytes(self.contents[path])
        self.git('add', '.')
        self.git('commit', '-qm', 'existing inherited changes')
        self.anchor = self.git('rev-parse', 'HEAD').strip()
        self.records = {p: (self.anchor, hashlib.sha256(self.contents[p]).hexdigest(), 'fixture') for p in self.paths}

    def tearDown(self):
        self.temp.cleanup()

    def git(self, *args):
        return subprocess.check_output(['git', *args], cwd=self.root, stderr=subprocess.STDOUT, text=True)

    def rows(self, unexpected=None, records=None):
        return verify_reviewed_paths(self.root, self.paths if unexpected is None else unexpected,
                                     self.records if records is None else records, self.anchor)

    def test_exact_inherited_versions_pass_with_individual_proof(self):
        rows = self.rows()
        self.assertEqual(len(rows), 9)
        self.assertTrue(all(r['passed'] for r in rows))
        self.assertTrue(all(r['actual_sha256'] == r['expected_sha256'] for r in rows))

    def test_new_edit_to_each_of_nine_owners_still_fails(self):
        for path in self.paths:
            with self.subTest(path=path):
                file = self.root / path
                file.write_bytes(self.contents[path] + b'new unreviewed edit\n')
                rejected = [r for r in self.rows() if not r['passed']]
                self.assertEqual([r['path'] for r in rejected], [path])
                file.write_bytes(self.contents[path])

    def test_removing_or_reverting_an_existing_owner_still_fails(self):
        for path in self.paths:
            with self.subTest(path=path):
                file = self.root / path
                file.unlink()
                self.assertFalse(next(r for r in self.rows() if r['path'] == path)['passed'])
                # A revert disappears from the old-parent diff. The proof must still check the pinned file.
                file.write_bytes(('original ' + path + '\n').encode())
                self.assertFalse(next(r for r in self.rows(unexpected=[]) if r['path'] == path)['passed'])
                file.write_bytes(self.contents[path])

    def test_unknown_existing_path_is_rejected(self):
        path = PREFIX + 'Scripts/Runtime/Gameplay/SaveSystem.cs'
        rows = self.rows(unexpected=[path])
        self.assertFalse(next(r for r in rows if r['path'] == path)['passed'])

    def test_changing_digest_to_match_new_bytes_does_not_forge_history(self):
        path = self.paths[0]
        changed = self.contents[path] + b'reward writer\n'
        (self.root / path).write_bytes(changed)
        records = dict(self.records)
        records[path] = (self.anchor, hashlib.sha256(changed).hexdigest(), 'forged fixture')
        row = next(r for r in self.rows(records=records) if r['path'] == path)
        self.assertFalse(row['passed'])
        self.assertIn('immutable', row['reason'])

    def test_missing_source_commit_is_rejected(self):
        path = self.paths[0]
        records = dict(self.records)
        records[path] = ('f' * 40, self.records[path][1], 'missing')
        self.assertFalse(next(r for r in self.rows(records=records) if r['path'] == path)['passed'])

    def test_newer_commit_cannot_be_used_as_its_own_approval(self):
        path = self.paths[0]
        changed = self.contents[path] + b'future edit\n'
        (self.root / path).write_bytes(changed)
        self.git('add', '.')
        self.git('commit', '-qm', 'not part of the fixed review')
        future = self.git('rev-parse', 'HEAD').strip()
        records = dict(self.records)
        records[path] = (future, hashlib.sha256(changed).hexdigest(), 'future')
        row = next(r for r in self.rows(records=records) if r['path'] == path)
        self.assertFalse(row['passed'])
        self.assertIn('ancestor', row['reason'])

    def test_missing_file_in_the_recorded_commit_is_rejected(self):
        path = 'Assets/not-in-history.cs'
        (self.root / path).write_bytes(b'not in history')
        records = dict(self.records)
        records[path] = (self.anchor, hashlib.sha256(b'not in history').hexdigest(), 'missing blob')
        self.assertFalse(next(r for r in self.rows(records=records) if r['path'] == path)['passed'])


class RealHistoryTests(unittest.TestCase):
    def test_all_nine_real_versions_match_the_existing_history(self):
        root = Path(__file__).resolve().parents[1]
        rows = verify_reviewed_paths(root, REVIEWED)
        self.assertEqual(len(rows), 9)
        self.assertEqual([r for r in rows if not r['passed']], [])

    def test_records_do_not_become_unrestricted_owner_exemptions(self):
        path = Path(__file__).with_name('check_ascension_04oh.py')
        spec = importlib.util.spec_from_file_location('owner_guard', path)
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        self.assertTrue(set(REVIEWED).isdisjoint({PREFIX + p for p in module.OWNERS}))


if __name__ == '__main__':
    unittest.main()
