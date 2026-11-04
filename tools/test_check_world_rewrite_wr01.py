#!/usr/bin/env python3
"""Regressions for real inherited payload preservation and narrow new-art authorization."""
import hashlib
import subprocess
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from check_world_rewrite_wr01 import (BASELINE, ROOT, UNDERHEART_BUILDER, PayloadIdentity,
                                      addition_allowed, baseline_path_inventory, check_frozen_tree, compare_payload,
                                      pointer_identity, underheart_render_hook_bytes,
                                      verify_underheart_render_hooks)


def lfs_pointer(payload):
    return ('version https://git-lfs.github.com/spec/v1\n'
            'oid sha256:' + hashlib.sha256(payload).hexdigest() + '\nsize '
            + str(len(payload)) + '\n').encode('ascii')


class PayloadTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.path = Path(self.folder.name) / 'asset.fbx'
        self.payload = b'original authored asset\x00\xff'
        self.identity = pointer_identity(lfs_pointer(self.payload))

    def tearDown(self):
        self.folder.cleanup()

    def test_hydration_is_not_an_inherited_edit(self):
        self.path.write_bytes(self.payload)
        self.assertEqual('preserved', compare_payload(self.identity, self.path))

    def test_identical_pointer_is_unhydrated_not_payload_success(self):
        self.path.write_bytes(lfs_pointer(self.payload))
        self.assertEqual('unhydrated', compare_payload(self.identity, self.path))

    def test_same_size_payload_mutation_fails(self):
        self.path.write_bytes(self.payload[:-1] + b'x')
        self.assertEqual('changed', compare_payload(self.identity, self.path))

    def test_pointer_oid_mutation_fails(self):
        self.path.write_bytes(lfs_pointer(b'changed authored asset\x00\xff'))
        self.assertEqual('changed', compare_payload(self.identity, self.path))

    def test_pointer_size_mutation_fails(self):
        self.path.write_bytes(lfs_pointer(self.payload).replace(
            ('size ' + str(len(self.payload))).encode(), b'size 999'))
        self.assertEqual('changed', compare_payload(self.identity, self.path))

    def test_missing_asset_fails(self):
        self.assertEqual('missing', compare_payload(self.identity, self.path))

    def test_symlink_is_not_accepted_as_owner_asset(self):
        target = self.path.with_suffix('.target')
        target.write_bytes(self.payload)
        self.path.symlink_to(target)
        self.assertEqual('missing', compare_payload(self.identity, self.path))

    def test_ordinary_git_binary_uses_payload_sha256(self):
        self.path.write_bytes(self.payload)
        identity = PayloadIdentity(hashlib.sha256(self.payload).hexdigest(), len(self.payload))
        self.assertEqual('preserved', compare_payload(identity, self.path))

    def test_malformed_or_embedded_pointer_is_not_lfs_identity(self):
        self.assertIsNone(pointer_identity(b'prefix\n' + lfs_pointer(self.payload)))
        self.assertIsNone(pointer_identity(lfs_pointer(self.payload) + b'extra'))
        self.assertIsNone(pointer_identity(lfs_pointer(self.payload).replace(b'sha256:', b'sha1:')))

    def test_allowed_prefix_does_not_authorize_neighbor_or_metadata(self):
        allowed = ('Assets/Art/HeartWR01/', 'Assets/Art/HeartWR01.meta')
        self.assertTrue(addition_allowed('Assets/Art/HeartWR01/Crown.fbx', allowed))
        self.assertTrue(addition_allowed('Assets/Art/HeartWR01.meta', allowed))
        self.assertFalse(addition_allowed('Assets/Art/HeartWR01Other/Crown.fbx', allowed))
        self.assertFalse(addition_allowed('Assets/Art/HeartWR01.meta.changed', allowed))
        self.assertFalse(addition_allowed('Assets/Art/Owner/Character.mat', allowed))


class GitTreeTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.root = Path(self.folder.name)
        self.asset = 'Assets/Art/Inherited/Owner.fbx'
        self.meta = self.asset + '.meta'
        self.payload = b'original hydrated geometry\x00\x01'
        for path, content in [(self.asset, lfs_pointer(self.payload)),
                              (self.meta, b'guid: 99999999999999999999999999999999\n')]:
            target = self.root / path
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
        self.command('init', '-q')
        self.command('add', '.')
        self.command('-c', 'user.name=Guard Fixture', '-c', 'user.email=fixture@example.invalid',
                     'commit', '-q', '-m', 'Fixture baseline')
        self.baseline = self.command('rev-parse', 'HEAD').decode().strip()
        (self.root / self.asset).write_bytes(self.payload)

    def command(self, *args):
        return subprocess.check_output(['git', *args], cwd=self.root, stderr=subprocess.DEVNULL)

    def tearDown(self):
        self.folder.cleanup()

    def check(self):
        return check_frozen_tree(self.root, self.baseline, 'Assets/Art',
                                 ('Assets/Art/HeartWR01/', 'Assets/Art/HeartWR01.meta'))

    def test_real_git_pointer_baseline_and_hydrated_worktree_pass(self):
        result = self.check()
        self.assertTrue(result['passed'])
        self.assertTrue(result['hydrated'])
        self.assertEqual(2, result['preserved'])

    def test_inventory_matches_historical_object_existence_for_files_trees_and_missing_paths(self):
        candidates = [self.asset, self.meta, 'Assets', 'Assets/Art', 'Assets/Art/Inherited',
                      'Assets/Art/New.fbx', 'Packages', 'ProjectSettings']
        inventory = baseline_path_inventory(self.root, self.baseline, ('Assets', 'Packages', 'ProjectSettings'))
        expected = [path for path in candidates if subprocess.run(
            ['git', 'cat-file', '-e', self.baseline + ':' + path], cwd=self.root,
            capture_output=True).returncode == 0]
        self.assertEqual(expected, [path for path in candidates if path in inventory])

    def test_untracked_authorized_new_art_passes(self):
        target = self.root / 'Assets/Art/HeartWR01/Crown.fbx'
        target.parent.mkdir(parents=True)
        target.write_bytes(b'new original geometry')
        result = self.check()
        self.assertTrue(result['passed'])
        self.assertEqual(['Assets/Art/HeartWR01/Crown.fbx'], result['allowed_additions'])

    def test_untracked_unrelated_owner_art_fails(self):
        target = self.root / 'Assets/Art/Inherited/Generated.mat'
        target.write_bytes(b'accidental import')
        result = self.check()
        self.assertFalse(result['passed'])
        self.assertEqual(['Assets/Art/Inherited/Generated.mat'], result['unexpected_additions'])

    def test_staged_unrelated_owner_art_also_fails(self):
        target = self.root / 'Assets/Art/Inherited/Generated.mat'
        target.write_bytes(b'accidental import')
        self.command('add', 'Assets/Art/Inherited/Generated.mat')
        result = self.check()
        self.assertFalse(result['passed'])
        self.assertEqual(['Assets/Art/Inherited/Generated.mat'], result['unexpected_additions'])

    def test_inherited_deletion_fails_even_with_authorized_addition(self):
        (self.root / self.asset).unlink()
        result = self.check()
        self.assertFalse(result['passed'])
        self.assertEqual([self.asset], result['missing'])

    def test_inherited_guid_mutation_fails(self):
        (self.root / self.meta).write_bytes(b'guid: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\n')
        result = self.check()
        self.assertFalse(result['passed'])
        self.assertEqual([self.meta], result['changed'])

    def test_identical_pointer_reports_hydration_failure_separately(self):
        (self.root / self.asset).write_bytes(lfs_pointer(self.payload))
        result = self.check()
        self.assertTrue(result['passed'])
        self.assertFalse(result['hydrated'])
        self.assertEqual([self.asset], result['unhydrated'])

    def test_new_authorized_test_does_not_allow_inherited_test_mutation(self):
        inherited = self.root / 'Assets/Tests/ExistingTests.cs'
        inherited.parent.mkdir(parents=True)
        inherited.write_text('Assert.AreEqual(10, worldRevision);\n')
        self.command('add', 'Assets/Tests/ExistingTests.cs')
        self.command('-c', 'user.name=Guard Fixture', '-c', 'user.email=fixture@example.invalid',
                     'commit', '-q', '-m', 'Existing test fixture')
        baseline = self.command('rev-parse', 'HEAD').decode().strip()
        inherited.write_text('Assert.Pass();\n')
        (inherited.parent / 'WorldRewriteWR01Tests.cs').write_text('new test source\n')
        result = check_frozen_tree(self.root, baseline, 'Assets/Tests',
                                 ('Assets/Tests/WorldRewriteWR01Tests.cs',))
        self.assertFalse(result['passed'])
        self.assertEqual(['Assets/Tests/ExistingTests.cs'], result['changed'])
        self.assertEqual(['Assets/Tests/WorldRewriteWR01Tests.cs'], result['allowed_additions'])


class UnderheartLiteralHookTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.original = subprocess.check_output(
            ['git', 'show', BASELINE + ':' + UNDERHEART_BUILDER], cwd=ROOT)
        cls.transformed = underheart_render_hook_bytes(cls.original)

    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.root = Path(self.folder.name)
        self.source = self.root / UNDERHEART_BUILDER
        self.source.parent.mkdir(parents=True)

    def tearDown(self):
        self.folder.cleanup()

    def verify(self, content):
        self.source.write_bytes(content)
        with patch('check_world_rewrite_wr01.git', return_value=self.original) as frozen_git:
            result = verify_underheart_render_hooks(self.root)
        frozen_git.assert_called_once_with(self.root, 'show', BASELINE + ':' + UNDERHEART_BUILDER)
        self.assertEqual(BASELINE, result['source_commit'])
        return result

    def test_transform_matches_the_independently_reviewed_complete_source_digest(self):
        self.assertEqual('2dedc443551bd61ec375bec00b1b11964db016ba162b97e5691df0f36a911e54',
                         hashlib.sha256(self.transformed).hexdigest())

    def test_complete_exact_render_hooks_pass(self):
        result = self.verify(self.transformed)
        self.assertTrue(result['passed'])
        self.assertFalse(result['unchanged'])

    def test_unchanged_immutable_source_passes(self):
        result = self.verify(self.original)
        self.assertTrue(result['passed'])
        self.assertTrue(result['unchanged'])

    def test_collider_owner_mutation_is_rejected(self):
        before = b'            BoxCollider bc = go.AddComponent<BoxCollider>();'
        self.assertEqual(1, self.transformed.count(before))
        changed = self.transformed.replace(before, before + b'\n            bc.enabled = false;')
        self.assertFalse(self.verify(changed)['passed'])

    def test_unrelated_builder_code_mutation_is_rejected(self):
        self.assertEqual(1, self.transformed.count(b'                    CellsBuilt++;'))
        changed = self.transformed.replace(b'                    CellsBuilt++;', b'                    CellsBuilt += 100;')
        self.assertFalse(self.verify(changed)['passed'])

    def test_removed_render_hook_is_rejected(self):
        changed = self.transformed.replace(b'            PrepareUnderheartRewrite();\n', b'')
        self.assertFalse(self.verify(changed)['passed'])

    def test_duplicated_render_hook_is_rejected(self):
        hook = b'            PrepareUnderheartRewrite();\n'
        self.assertFalse(self.verify(self.transformed.replace(hook, hook + hook))['passed'])

    def test_whitespace_outside_hooks_is_also_frozen(self):
        self.assertFalse(self.verify(self.transformed + b'\n')['passed'])

    def test_missing_or_duplicated_baseline_context_cannot_be_reinterpreted(self):
        original = self.original.replace(b'            FlushAll();', b'            FlushEveryBucket();')
        with self.assertRaises(ValueError):
            underheart_render_hook_bytes(original)
        original = self.original + b'\n            string key = _group + "|" + m.GetInstanceID();\n'
        with self.assertRaises(ValueError):
            underheart_render_hook_bytes(original)


if __name__ == '__main__':
    unittest.main()
