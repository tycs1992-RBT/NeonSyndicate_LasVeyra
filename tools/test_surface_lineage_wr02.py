"""Mutation checks: WR02's exact hook replay is not a filename exemption."""
import copy,hashlib,json,subprocess,tempfile,unittest
from pathlib import Path
from surface_lineage_wr02 import BASELINE,load_contract,reviewed_view
ROOT=Path(__file__).resolve().parents[1]
class HookContractTests(unittest.TestCase):
 def test_every_complete_hook_and_every_extra_edit(self):
  contract=load_contract();self.assertEqual(len(contract['paths']),14)
  for path,row in contract['paths'].items():
   with self.subTest(path=path):
    before=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT);after=subprocess.check_output(['git','show','251a3d8f10fd84bae27b338391574cfdaa3ac8af:'+path],cwd=ROOT)
    self.assertEqual(reviewed_view(path,before,after),before)
    self.assertEqual(reviewed_view(path,before,after+b'\n// unauthorized mutation\n'),after+b'\n// unauthorized mutation\n')
    self.assertEqual(reviewed_view(path,before+b'x',after),after)
    edit=row['edits'][0];partial=after.replace(edit['after'].encode(),edit['before'].encode(),1)
    self.assertEqual(reviewed_view(path,before,partial),partial)
 def test_unlisted_paths_remain_exact(self):
  for path in ['Characters/HumanoidPose.cs','Gameplay/SaveSystem.cs','existing.mat.meta','arbitrary.cs']:
   self.assertEqual(reviewed_view(path,b'before',b'after'),b'after')
 def test_tampered_contract_refused(self):
  with tempfile.TemporaryDirectory() as temp:
   path=Path(temp)/'contract.json';path.write_text(json.dumps({'baseline':BASELINE,'paths':{}}))
   with self.assertRaises(ValueError):load_contract(path)
if __name__=='__main__':unittest.main()
