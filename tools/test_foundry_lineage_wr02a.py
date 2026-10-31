import json,subprocess,tempfile,unittest
from pathlib import Path
from foundry_lineage_wr02a import BASELINE,load_contract,reviewed_view
ROOT=Path(__file__).resolve().parents[1]
class FoundryHookTests(unittest.TestCase):
 def test_every_complete_transformation_and_extra_edit(self):
  contract=load_contract();self.assertEqual(len(contract['paths']),9)
  for path,row in contract['paths'].items():
   with self.subTest(path=path):
    before=subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT);after=subprocess.check_output(['git','show','38ec685deafa3cf6f20a013f7c9556d8e02897f6:'+path],cwd=ROOT);self.assertEqual(reviewed_view(path,before,after),before)
    altered=after+b'\n// unauthorized extra edit\n';self.assertEqual(reviewed_view(path,before,altered),altered)
    partial=after.replace(row['edits'][0]['after'].encode(),row['edits'][0]['before'].encode(),1);self.assertNotEqual(partial,after);self.assertEqual(reviewed_view(path,before,partial),partial)
 def test_unlisted_owner_and_modified_original_not_exempt(self):
  self.assertEqual(reviewed_view('Characters/CharacterBase.cs',b'before',b'after'),b'after')
  contract=load_contract();path=next(iter(contract['paths']));after=subprocess.check_output(['git','show','38ec685deafa3cf6f20a013f7c9556d8e02897f6:'+path],cwd=ROOT);self.assertEqual(reviewed_view(path,b'wrong original',after),after)
 def test_tampered_contract_fails_closed(self):
  with tempfile.TemporaryDirectory()as temp:
   p=Path(temp)/'contract.json';p.write_text('{}')
   with self.assertRaises(ValueError):load_contract(p)
if __name__=='__main__':unittest.main()
