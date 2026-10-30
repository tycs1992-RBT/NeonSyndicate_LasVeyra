"""Forge source integration/preservation contracts. These are not Unity tests."""
import hashlib
import subprocess
import unittest
from pathlib import Path
from wr02i_lineage import BASE, EDITS, ADDITIONS, original_view

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = 'Assets/NeonSyndicate/Scripts/Runtime/'


class ForgeCourtSourceTests(unittest.TestCase):
    def test_only_complete_pinned_hooks_are_normalized(self):
        for path, pair in EDITS.items():
            with self.subTest(path=path):
                current = (ROOT / path).read_bytes()
                before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT)
                self.assertEqual(hashlib.sha256(current).hexdigest(), pair[1])
                self.assertEqual(original_view(ROOT, path, current), before)
                mutated = current + b'\n// arbitrary extra owner edit\n'
                self.assertEqual(original_view(ROOT, path, mutated), mutated)
                self.assertEqual(original_view(ROOT, path, current[:-1]), current[:-1])

    def test_complete_art_and_tests_are_pinned(self):
        for path, digest in ADDITIONS.items():
            self.assertEqual(hashlib.sha256((ROOT / path).read_bytes()).hexdigest(), digest, path)
            self.assertTrue((ROOT / (path + '.meta')).is_file())

    def test_entire_physics_and_gameplay_source_owners_are_unchanged(self):
        for tail in ['World/GothicHeartH3.cs', 'World/GothicCivicH5.cs',
                     'ArtInhabitedCity04C/VenueView04C.cs', 'Gameplay/InhabitedCity/InhabitedCityBindings04C.cs',
                     'Gameplay/SaveSystem.cs', 'Gameplay/Quests/QuestRunner.cs', 'Characters/PlayerController.cs',
                     'World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs',
                     'World/HeartFR01/HeartPlanFR01.CompositionWR02B.cs']:
            path = RUNTIME + tail
            self.assertEqual((ROOT / path).read_bytes(), subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT), path)
        path = RUNTIME + 'ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs'
        current = (ROOT / path).read_text()
        before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT, text=True)
        self.assertEqual(current.replace('            if (!StageForgeCourtWR02I(binding, structure, high, low))\n', ''), before)

    def test_same_passive_transaction_preserves_fallback_and_material_validation(self):
        source = (ROOT / (RUNTIME + 'ArtInhabitedCity04C/VenueView04C.ForgeCourtWR02I.cs')).read_text()
        self.assertIn('model => AscensionArtH2.Source(model, true)', source)
        self.assertIn('candidate.Transform.gameObject.SetActive(false)', source)
        self.assertIn('heart.RegisterRewriteVisual(ForgeCourtIdWR02I, "ForgeCourtWR02I"', source)
        self.assertIn('_forgeCourtOriginalWR02I.SetActive(true)', source)
        for token in ['AddComponent<', 'SaveSystem.', 'Physics.', 'PlayerPrefs.', 'new QuestDef', 'new CityLayout']:
            self.assertNotIn(token, source)
        for channel in ['material.mainTexture == null', 'GetTexture("_BumpMap") == null', 'GetTexture("_MetallicGlossMap") == null']:
            self.assertIn(channel, source)

    def test_original_assets_routes_services_and_guids_are_byte_identical(self):
        paths = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', BASE, '--',
            'Assets/NeonSyndicate/Resources/Quests', 'Assets/NeonSyndicate/Resources/InhabitedCity04C',
            'Assets/NeonSyndicate/Resources/AscensionH2/Meshes', 'Packages', 'ProjectSettings'], cwd=ROOT, text=True).splitlines()
        # Compare Git object identities; LFS payload hydration is independently checked by git lfs fsck.
        changed = subprocess.check_output(['git', 'diff', BASE, '--name-only', '--'] + paths, cwd=ROOT, text=True)
        self.assertEqual(changed, '')


if __name__ == '__main__':
    unittest.main()
