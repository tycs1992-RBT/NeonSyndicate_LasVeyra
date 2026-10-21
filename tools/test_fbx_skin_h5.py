"""Execute deterministic actual-file and hostile-input regressions for the H5 skin derivative."""
import hashlib,json,math,struct,unittest
from pathlib import Path
from fbx_skin_h5 import Document,derive
ROOT=Path(__file__).resolve().parents[1]
class SkinTests(unittest.TestCase):
 def setUp(self):self.source=(ROOT/'Assets/NeonSyndicate/Art/CandidatesH3/Models/MaleBase.fbx').read_bytes()
 def test_all_sources_roundtrip_without_changing_a_byte(self):
  for p in (ROOT/'Assets/NeonSyndicate/Art/CandidatesH3/Models').glob('*.fbx'):self.assertEqual(Document(p.read_bytes()).encode(),p.read_bytes())
 def test_committed_derivatives_are_deterministic_and_preserve_every_influence(self):
  m=json.loads((ROOT/'Assets/NeonSyndicate/Art/CandidatesH5/skin-lineage.json').read_text())
  for r in m['models']:
   raw=(ROOT/r['source']).read_bytes();self.assertEqual(hashlib.sha256(raw).hexdigest(),r['original_sha256']);data,proof=derive(raw,8)
   self.assertEqual(data,(ROOT/r['output']).read_bytes());self.assertTrue(proof['non_skin_properties_unchanged'])
   self.assertTrue(all(q['pruned_influences']==0 and q['maximum_discarded_normalized_mass']==0 for q in proof['meshes']))
 def test_negative_or_nan_weights_are_rejected(self):
  for invalid in [-.1,float('nan')]:
   d=Document(self.source);objects=next(n for n in d.nodes if n.name=='Objects')
   w=next(q for n in objects.children for q in n.children if q.name=='Weights'and q.props[0].value);w.props[0].value[0]=invalid;w.props[0].changed=True
   with self.assertRaises(ValueError):derive(d.encode(),8)
 def test_zero_total_vertex_is_not_given_a_fabricated_bone(self):
  d=Document(self.source);objects=next(n for n in d.nodes if n.name=='Objects')
  for n in objects.children:
   arrays={q.name:q.props[0]for q in n.children if q.name in ['Indexes','Weights']}
   if set(arrays)!=set(['Indexes','Weights']):continue
   for i,v in enumerate(arrays['Indexes'].value):
    if v==0:arrays['Weights'].value[i]=0;arrays['Weights'].changed=True
  with self.assertRaises(ValueError):derive(d.encode(),8)
 def test_truncated_or_unknown_binary_is_rejected(self):
  for data in [b'',b'not a model',self.source[:70]]:
   with self.assertRaises((ValueError,struct.error,IndexError)):derive(data,8)
 def test_unreviewed_pruning_limit_is_rejected(self):
  with self.assertRaises(ValueError):derive(self.source,2)
if __name__=='__main__':unittest.main()
