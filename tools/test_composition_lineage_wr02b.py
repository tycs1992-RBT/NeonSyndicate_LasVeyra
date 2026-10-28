"""Exact replay mutations: none of the new visual hooks grants a general source exemption."""
import subprocess,tempfile,unittest
from pathlib import Path
from composition_lineage_wr02b import BASELINE,load_contract,reviewed_view
ROOT=Path(__file__).resolve().parents[1]
class CompositionHookTests(unittest.TestCase):
 def test_each_exact_hook_then_every_partial_or_extra_edit(self):
  contract=load_contract();self.assertEqual(len(contract['paths']),11)
  for path,row in contract['paths'].items():
   with self.subTest(path=path):
    original=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT);current=(ROOT/path).read_bytes();self.assertEqual(reviewed_view(path,original,current),original)
    mutation=current+b'\n// unauthorized source change\n';self.assertEqual(reviewed_view(path,original,mutation),mutation)
    for edit in row['edits']:
     partial=current.replace(edit['after'].encode(),edit['before'].encode(),1);self.assertNotEqual(partial,current);self.assertEqual(reviewed_view(path,original,partial),partial)
    self.assertEqual(reviewed_view(path,original+b'x',current),current)
 def test_old_h7_h8_importer_validation_remains_exact(self):
  from h7_owner_hooks import restore_h6_visual
  from h8_owner_hooks import restore_owner_h8
  path='Assets/NeonSyndicate/Scripts/Runtime/Characters/ImportedVisual04OD.cs';before=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT);current=(ROOT/path).read_bytes()
  self.assertEqual(restore_h6_visual(current.decode()),restore_h6_visual(before.decode()))
  self.assertEqual(restore_owner_h8(path,current),restore_owner_h8(path,before))
  altered=current+b'\n// unauthorized importer mutation\n'
  self.assertNotEqual(restore_owner_h8(path,altered),restore_owner_h8(path,before))
  self.assertNotEqual(restore_h6_visual(altered.decode()),restore_h6_visual(before.decode()))
 def test_unknown_owner_remains_visible(self):
  self.assertEqual(reviewed_view('Gameplay/QuestBook.cs',b'original',b'changed'),b'changed')
 def test_tampered_contract_is_rejected(self):
  with tempfile.TemporaryDirectory()as d:
   p=Path(d)/'contract.json';p.write_text('{}')
   with self.assertRaises(ValueError):load_contract(p)
if __name__=='__main__':unittest.main()
