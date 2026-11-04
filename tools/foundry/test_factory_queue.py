import copy,unittest
from factory_queue import release_gate
class QueueTests(unittest.TestCase):
 def test_native_and_owner_acceptance_for_exact_source_required(self):
  assets=[{'assetId':'a','input_sha256':'exact-a'},{'assetId':'b','input_sha256':'exact-b'}]
  with self.assertRaises(ValueError):release_gate({},assets)
  good={'assets':{x['assetId']:{'ownerAccepted':True,'nativeImported':True,'nativeMotionOrCollisionAccepted':True,'ordinaryGameplayAccepted':True,'inputSha256':x['input_sha256']}for x in assets}}
  self.assertTrue(release_gate(good,assets))
  for flag in ['ownerAccepted','nativeImported','nativeMotionOrCollisionAccepted','ordinaryGameplayAccepted']:
   bad=copy.deepcopy(good);bad['assets']['b'][flag]=False
   with self.assertRaises(ValueError):release_gate(bad,assets)
  bad=copy.deepcopy(good);bad['assets']['a']['inputSha256']='other'
  with self.assertRaises(ValueError):release_gate(bad,assets)
if __name__=='__main__':unittest.main()
