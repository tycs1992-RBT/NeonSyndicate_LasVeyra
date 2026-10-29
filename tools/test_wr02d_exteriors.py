"""Bounded source checks for service exterior presentation. Unity remains required."""
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASE = '8bd2b1d54fdc7c541f4af1888f283149f85a7bb6'
RUNTIME = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime'


class ExteriorChecks(unittest.TestCase):
    def test_original_geometry_is_used_for_both_roots(self):
        source = (RUNTIME / 'ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs').read_text()
        self.assertIn('original.PartMeshes(near, far, old, Vector3.zero, Vector3.one, Quaternion.identity)', source)
        self.assertIn('candidate.PartMeshes(near, far, stone, Vector3.zero, Vector3.one, Quaternion.identity)', source)
        self.assertIn('candidate.Transform.gameObject.SetActive(false)', source)
        self.assertIn('heart.RegisterRewriteVisual(id, "ExteriorMasonryWR02D"', source)
        self.assertIn('_exteriorOriginalWR02D.SetActive(true)', source)

    def test_physical_shell_is_unchanged(self):
        path = 'Assets/NeonSyndicate/Scripts/Runtime/ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs'
        original = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT, text=True)
        actual = (ROOT / path).read_text()
        for line in original.splitlines():
            if 'collision' in line.lower() or 'Collider' in line:
                self.assertIn(line, actual)
        for name in ('Gameplay/SaveSystem.cs', 'Gameplay/Quests/QuestRunner.cs', 'Characters/PlayerController.cs'):
            path = 'Assets/NeonSyndicate/Scripts/Runtime/' + name
            before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT)
            self.assertEqual((ROOT / path).read_bytes(), before)

    def test_scope_and_material_gate(self):
        source = (RUNTIME / 'ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs').read_text()
        self.assertIn('role != "masonry"', source)
        for venue in ('lofts', 'archive', 'forge'):
            self.assertIn('_layout.id != "' + venue + '"', source)
        self.assertIn('ForgeSurfaceMaterialWR02C3("Masonry")', source)
        self.assertNotIn('AddComponent<BoxCollider>', source)
        self.assertNotIn('AddComponent<MeshCollider>', source)


if __name__ == '__main__':
    unittest.main()
