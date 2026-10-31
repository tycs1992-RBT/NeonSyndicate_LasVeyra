"""Exact implementation and ownership contracts. Native scene results remain a separate gate."""
import hashlib
import subprocess
import unittest
from pathlib import Path
from wr02j_lineage import BASE, EDITS, ADDITIONS, original_view

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = 'Assets/NeonSyndicate/Scripts/Runtime/'


class SupportedDistrictContracts(unittest.TestCase):
    def test_complete_hooks_and_unknown_mutations(self):
        for path, hashes in EDITS.items():
            current = (ROOT / path).read_bytes()
            self.assertEqual(hashlib.sha256(current).hexdigest(), hashes[1], path)
            before = subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT)
            self.assertEqual(original_view(ROOT, path, current), before)
            for bad in [current[:-1], current + b'\n// unrelated edit\n']:
                self.assertEqual(original_view(ROOT, path, bad), bad)

    def test_art_sources_and_metadata_are_pinned(self):
        for path, digest in ADDITIONS.items():
            self.assertEqual(hashlib.sha256((ROOT / path).read_bytes()).hexdigest(), digest, path)
            self.assertTrue((ROOT / (path + '.meta')).is_file())

    def test_original_physics_layout_and_gameplay_owners_are_unchanged(self):
        paths = ['World/CityBuilder.Gothic04OB.cs', 'World/CityLayout.Gothic04OB.cs',
                 'World/GothicRealmGeometry04OB.cs', 'World/GothicCivicH5.cs', 'World/GothicHeartH3.cs',
                 'ArtInhabitedCity04C/VenueView04C.cs', 'ArtInhabitedCity04C/VenueView04C.GothicHeartH3.cs',
                 'Gameplay/InhabitedCity/InhabitedCityBindings04C.cs', 'Gameplay/SaveSystem.cs',
                 'Gameplay/Quests/QuestRunner.cs', 'Characters/PlayerController.cs',
                 'World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs',
                 'World/HeartFR01/HeartPlanFR01.CompositionWR02B.cs',
                 'World/HeartFR01/HeartPresentationFR01.KingdomStreetWR02G.cs']
        for path in paths:
            path = RUNTIME + path
            self.assertEqual((ROOT / path).read_bytes(), subprocess.check_output(['git', 'show', BASE + ':' + path], cwd=ROOT), path)
        self.assertEqual(subprocess.check_output(['git', 'diff', BASE, '--name-only', '--',
            'Assets/NeonSyndicate/Resources', 'Assets/NeonSyndicate/Art', 'ArtSource', 'Packages', 'ProjectSettings'], cwd=ROOT, text=True), '')

    def test_no_new_physical_or_gameplay_owner_and_no_geometry_displacement(self):
        text = '\n'.join((ROOT / p).read_text() for p in ADDITIONS if '/Runtime/' in p)
        for token in ['AddComponent<', 'Physics.', 'SaveSystem.', 'PlayerPrefs.', 'new CityLayout', 'new QuestDef', 'RewardLedger']:
            self.assertNotIn(token, text)
        plan = (ROOT / (RUNTIME + 'World/HeartFR01/HeartPlanFR01.SupportedDistrictWR02J.cs')).read_text()
        self.assertIn('mesh.TriangleSmooth(0, va, vb, vc', plan)
        self.assertIn('AscensionArtH2.Append(target, mesh, part.Position, part.Yaw, part.Scale)', plan)

    def test_failure_preserves_captured_and_uncaptured_source(self):
        source = (ROOT / (RUNTIME + 'World/HeartFR01/HeartPresentationFR01.SupportedDistrictWR02J.cs')).read_text()
        self.assertIn('return retained;', source)
        self.assertIn('capture.Failed || capture.Appends != 3', source)
        self.assertIn('!SupportedParcelWR02J(building)', source)
        self.assertIn('capture.Candidate.SetActive(false)', source)
        self.assertIn('capture.Original.SetActive(true)', source)
        for token in ['_catalog == null || !_catalog.Ready', 'material.mainTexture == null',
                      'GetTexture("_BumpMap") == null', 'GetTexture("_MetallicGlossMap") == null']:
            self.assertIn(token, source)
        archive = (ROOT / (RUNTIME + 'ArtInhabitedCity04C/VenueView04C.SupportedArchiveWR02J.cs')).read_text()
        self.assertIn('"/supported-wr02j"', archive, 'Refused new candidate must not reserve the old fallback key.')
        self.assertIn('return false;', archive)


if __name__ == '__main__':
    unittest.main()
