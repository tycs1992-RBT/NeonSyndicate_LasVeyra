"""Actual Blender admission counterexamples. No network or native Unity execution."""
import importlib.util,json,struct,sys,tempfile,unittest
from pathlib import Path
import bpy
sys.path.insert(0,str(Path(__file__).resolve().parent))
import process_asset as pipeline
class IntakeTests(unittest.TestCase):
 def setUp(self):bpy.ops.wm.read_factory_settings(use_empty=True)
 def obj(self,uv=True):
  data=bpy.data.meshes.new('Fixture');data.from_pydata([(0,0,0),(1,0,0),(0,1,0)],[],[(0,1,2)]);data.update();obj=bpy.data.objects.new('Fixture',data);bpy.context.collection.objects.link(obj)
  if uv:
   layer=data.uv_layers.new()
   for i,point in enumerate([(0,0),(1,0),(0,1)]):layer.data[i].uv=point
  return obj
 def test_valid_static_fixture_is_inspected(self):self.assertEqual(pipeline.inspect([self.obj()],False)['triangles'],1)
 def test_nonfinite_geometry_rejected(self):
  obj=self.obj();obj.data.vertices[0].co.x=float('nan')
  with self.assertRaises(ValueError):pipeline.bounds([obj])
 def test_missing_uv_rejected(self):
  with self.assertRaises(ValueError):pipeline.inspect([self.obj(False)],False)
 def test_unweighted_creature_rejected(self):
  with self.assertRaises(ValueError):pipeline.inspect([self.obj()],True)
 def test_external_glb_resource_rejected(self):
  body=json.dumps({'asset':{'version':'2.0'},'images':[{'uri':'https://unapproved.example/image.png'}]}).encode();body+=b' '*((-len(body))%4);raw=struct.pack('<III',0x46546c67,2,20+len(body))+struct.pack('<II',len(body),0x4e4f534a)+body
  with tempfile.TemporaryDirectory()as temp:
   p=Path(temp)/'external.glb';p.write_bytes(raw)
   with self.assertRaises(ValueError):pipeline.inspect_glb(p)
 def test_malformed_header_rejected(self):
  with tempfile.TemporaryDirectory()as temp:
   p=Path(temp)/'bad.glb';p.write_bytes(b'glTFbad')
   with self.assertRaises(ValueError):pipeline.inspect_glb(p)
if __name__=='__main__':
 result=unittest.TextTestRunner().run(unittest.defaultTestLoader.loadTestsFromTestCase(IntakeTests));assert result.wasSuccessful()
