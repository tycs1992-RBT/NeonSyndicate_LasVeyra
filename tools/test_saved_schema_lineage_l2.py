"""Mutation regressions for the narrowly reviewed saved prefix, independent of Unity."""
import subprocess,unittest
from pathlib import Path
import saved_schema_lineage_l2 as policy
ROOT=Path(__file__).resolve().parents[1]
class SavedPrefixTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.old=policy.prefix(subprocess.check_output(['git','show',policy.BASE+':'+policy.PATH],cwd=ROOT))
        cls.new=policy.prefix(subprocess.check_output(['git','show',policy.REVIEWED+':'+policy.PATH],cwd=ROOT))
    def test_exact_published_appends_and_history_pass(self):
        self.assertTrue(policy.exact_append_only(self.old,self.new,self.new));self.assertTrue(policy.verify(ROOT))
    def test_prefix_before_the_two_blocks_is_exactly_preserved(self):
        normalized=self.new
        for block in policy.APPENDS:normalized=normalized.replace(block,b'')
        self.assertEqual(normalized,self.old)
    def test_old_field_removal_rename_retype_and_initializer_changes_fail(self):
        mutations=[(b'public int Version',b'public long Version'),(b'public int Money',b'public int Cash'),(b'public int Money;',b''),(b'public int Version = 1',b'public int Version = 99')]
        for old,new in mutations:
            with self.subTest(old=old):
                self.assertIn(old,self.new);self.assertFalse(policy.exact_append_only(self.old,self.new,self.new.replace(old,new)))
    def test_future_append_is_not_implicitly_approved(self):
        changed=self.new.replace(policy.APPENDS[1],policy.APPENDS[1]+b'        public int NewReward;\n');self.assertFalse(policy.exact_append_only(self.old,self.new,changed))
    def test_missing_reordered_or_duplicate_reviewed_block_fails(self):
        a,b=policy.APPENDS
        for changed in [self.new.replace(a,b''),self.new.replace(a,a+a),self.new.replace(a,b'').replace(b,b+a)]:self.assertFalse(policy.exact_append_only(self.old,self.new,changed))
    def test_forging_new_reviewed_bytes_cannot_bypass_fixed_digest(self):
        changed=self.new+b'// unreviewed\n';self.assertFalse(policy.exact_append_only(self.old,changed,changed))
    def test_old_parent_revert_is_not_a_valid_current_schema(self):self.assertFalse(policy.exact_append_only(self.old,self.new,self.old))
if __name__=='__main__':unittest.main()
