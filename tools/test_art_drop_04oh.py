"""Executable read-only intake regressions; no Unity import or asset approval."""
import hashlib,json,tempfile,unittest,zipfile
from pathlib import Path
import inspect_art_drop_04oh as intake
class ArtDropTests(unittest.TestCase):
 def test_reference_images_never_become_models(self):
  self.assertEqual(intake.kind('supplied_REFERENCES/concept.png'),'REFERENCE_ONLY');self.assertEqual(intake.kind('hero.png'),'image_not_proof_of_pbr_or_rig')
 def test_model_extensions_require_rig_validation(self):
  self.assertEqual(intake.kind('Hero.FBX'),'model_interchange_requires_rig_validation')
 def test_scripts_and_unitypackages_require_review(self):
  for ext in ['cs','dll','exe','unitypackage']:self.assertEqual(intake.kind('Candidate.'+ext),'EXECUTABLE_OR_IMPORT_PACKAGE_REVIEW_REQUIRED')
 def test_archive_traversal_and_drive_paths_are_refused_without_extraction(self):
  with tempfile.TemporaryDirectory() as folder:
   p=Path(folder)/'drop.zip'
   with zipfile.ZipFile(p,'w')as z:
    for name in ['../outside.fbx','C:\\private.fbx','/absolute.fbx','normal/hero.png']:z.writestr(name,b'not a model')
   report=intake.inspect(p);self.assertEqual(len(report['unsafe_paths']),3);self.assertEqual(len(report['files']),1);self.assertFalse((Path(folder)/'normal').exists())
 def test_hashes_describe_actual_bytes_without_approving_rights(self):
  with tempfile.TemporaryDirectory()as folder:
   p=Path(folder)/'hero.fbx';data=b'; FBX test only';p.write_bytes(data);report=intake.inspect(folder)
   self.assertEqual(report['files'][0]['sha256'],hashlib.sha256(data).hexdigest());self.assertIn('UNKNOWN',report['rights_status']);self.assertEqual(p.read_bytes(),data)
 def test_symlink_is_not_followed(self):
  with tempfile.TemporaryDirectory()as folder:
   source=Path(folder)/'source';source.mkdir();(Path(folder)/'private').write_text('private');(source/'linked').symlink_to(Path(folder)/'private')
   report=intake.inspect(source);self.assertEqual(report['files'],[]);self.assertEqual(report['unsafe_paths'],['linked'])
if __name__=='__main__':unittest.main()
