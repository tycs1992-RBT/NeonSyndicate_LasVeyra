"""Lifecycle and integration contracts; these are not rendered or physics tests."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'Assets/NeonSyndicate/Scripts/Runtime'
HEART = ROOT / 'World/HeartFR01'


class KingdomIntegration(unittest.TestCase):
    def test_settled_vegetation_must_pass_the_existing_protection_again(self):
        source = (HEART / 'HeartPresentationFR01.CompositionWR02B.cs').read_text()
        block = source.split('if (!CompositionGroundWR02B(plan, out reason))', 1)[1].split('record.admitted = true;', 1)[0]
        self.assertIn('record.position = plan.position;', block)
        self.assertIn('LiveWorldSafetyH8S.Conflict(', block)
        self.assertIn('plan.position.y + plan.size.y', block)
        self.assertIn('if (reason != null)', block)
        self.assertIn('continue;', block)

    def test_street_uses_the_existing_root_and_falls_back_without_colliders(self):
        street = (HEART / 'HeartPresentationFR01.StreetWR02D.cs').read_text()
        detail = (HEART / 'HeartPresentationFR01.KingdomStreetWR02G.cs').read_text()
        self.assertLess(street.index('StageKingdomStreetWR02G(root.transform'), street.index('candidate.SetActive(false);\n                }'))
        self.assertIn('KingdomOpeningsUnchangedWR02G()', street)
        self.assertIn('CaptureStreetFallbackWR02G(pieces)', street)
        self.assertIn('if (!_legacyStreetClearWR02G)', street)
        self.assertIn('_streetCandidateWR02D.SetActive(true)', street)
        self.assertIn('WorldApertures.All', detail)
        self.assertIn('GetComponentsInChildren<Collider>(true).Length != 0', detail)
        self.assertNotIn('AddComponent<BoxCollider>', detail)
        self.assertNotIn('AddComponent<MeshCollider>', detail)

    def test_service_marks_join_the_existing_candidate_not_gameplay_or_collision(self):
        exterior = (ROOT / 'ArtInhabitedCity04C/VenueView04C.ForgeWR02C3.cs').read_text()
        art = (ROOT / 'ArtInhabitedCity04C/VenueView04C.HeraldryWR02G.cs').read_text()
        self.assertIn('candidate.Finish(); candidate.Transform.gameObject.SetActive(false);\n                StageHeraldryWR02G(candidate.Transform);', exterior)
        self.assertIn('ReleaseHeraldryWR02G();', exterior)
        self.assertNotIn('AddComponent<BoxCollider>', art)
        self.assertNotIn('AddComponent<MeshCollider>', art)
        self.assertIn('Layers.IgnoreRaycast', art)
        self.assertIn('ArtInhabitedCity04C.Release(_heraldryTextureWR02G)', art)


if __name__ == '__main__':
    unittest.main()
