import unittest
from policy import collider_candidates
class ColliderPolicyTests(unittest.TestCase):
 def test_no_physics_is_the_default_authority_contract(self):
  self.assertEqual(collider_candidates({'colliderPolicy':'ORIGINAL_AUTHORITY_ONLY'},[[0,0,0],[10,10,10]]),[])
  with self.assertRaises(ValueError):collider_candidates({'colliderPolicy':'ORIGINAL_AUTHORITY_ONLY','colliderCandidateBoxes':[{}]},[[0,0,0],[10,10,10]])
 def test_candidates_need_finite_dimensions_known_owner_and_portal_clearance(self):
  b={'colliderPolicy':'EXPLICIT_CANDIDATES_ONLY','colliderCandidateBoxes':[{'center':[2,2,2],'size':[1,1,1],'sourceOwner':'test original owner'}]};bounds=[[0,0,0],[10,10,10]]
  result=collider_candidates(b,bounds);self.assertFalse(result[0]['nativeAccepted']);self.assertFalse(result[0]['enabledInUnity'])
  for field,value in [('center',[float('nan'),2,2]),('size',[1,-1,1]),('center',[20,2,2]),('sourceOwner','')]:
   bad={'colliderPolicy':b['colliderPolicy'],'colliderCandidateBoxes':[dict(b['colliderCandidateBoxes'][0],**{field:value})]}
   with self.assertRaises(ValueError):collider_candidates(bad,bounds)
  b['requiredPortalClearances']=[{'min':[1,1,1],'max':[3,3,3]}]
  with self.assertRaises(ValueError):collider_candidates(b,bounds)
if __name__=='__main__':unittest.main()
