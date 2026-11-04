"""Mutate actual current FBX inputs; a standalone source parser is never native Unity acceptance."""
import argparse,shutil,subprocess,tempfile,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2];ART=ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A/EnvironmentWR02B';INSPECTOR=None
class EnvironmentInspectorTests(unittest.TestCase):
 def run_file(self,path,cwd=ROOT):return subprocess.run([str(INSPECTOR),str(path)],cwd=cwd,capture_output=True,text=True)
 def test_actual_current_asset_then_truncated_payload(self):
  source=ART/'AzureElder/AzureElder_LOD0.fbx';self.assertEqual(self.run_file(source).returncode,0)
  with tempfile.TemporaryDirectory()as d:
   bad=Path(d)/'truncated.fbx';bad.write_bytes(source.read_bytes()[:64]);self.assertNotEqual(self.run_file(bad).returncode,0)
 def test_arch_cannot_impersonate_a_unit_crown(self):
  with tempfile.TemporaryDirectory()as d:
   bad=Path(d)/'SanctuaryCedar_LOD0.fbx';shutil.copyfile(ART/'EscarpmentArch/EscarpmentArch_LOD0.fbx',bad);self.assertNotEqual(self.run_file(bad).returncode,0)
 def test_skinned_animated_creature_is_not_passive_environment(self):
  self.assertNotEqual(self.run_file(ROOT/'Assets/NeonSyndicate/Art/FoundryWR02A/CrystalBasiliskCandidate/CrystalBasiliskCandidate_LOD0.fbx').returncode,0)
 def test_unknown_original_cliff_is_rejected(self):
  with tempfile.TemporaryDirectory()as d:
   bad=Path(d)/'ErodedCliff99999_LOD0.fbx';shutil.copyfile(ART/'ErodedCliff61724/ErodedCliff61724_LOD0.fbx',bad);self.assertNotEqual(self.run_file(bad).returncode,0)
 def test_unrelated_physical_surface_cannot_certify_this_cliff(self):
  source=ART/'ErodedCliff61724/ErodedCliff61724_LOD0.fbx';self.assertEqual(self.run_file(source).returncode,0)
  with tempfile.TemporaryDirectory()as d:
   root=Path(d);folder=root/'ArtSource/HeartWR02';folder.mkdir(parents=True);(folder/'Cliff61724.source.obj').write_text('v 0 0 0\nv 1 0 0\nv 0 0 1\nf 1 2 3\n');self.assertNotEqual(self.run_file(source,root).returncode,0)
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--inspector',type=Path,required=True);a=p.parse_args();INSPECTOR=a.inspector.resolve();unittest.main(argv=[__file__])
