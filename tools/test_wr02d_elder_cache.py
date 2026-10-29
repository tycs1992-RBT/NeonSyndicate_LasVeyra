"""Check the wiring of FR01's optional reused tree dependency. Not a Unity test."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / 'Assets/NeonSyndicate/Scripts/Editor'


class OptionalElderCache(unittest.TestCase):
    def test_lookup_and_saved_stamp_use_the_same_complete_recipe(self):
        source = (EDITOR / 'HeartSetupFR01.cs').read_text(encoding='utf-8')
        call = 'PreparationStamp("FR01 material dependencies 3",json,receipt,OptionalElderStampFR01())'
        self.assertEqual(source.count(call), 2)
        self.assertIn('catalog.Fingerprint==fingerprint && catalog.Ready && PreparedSurfacesReady(catalog)', source)
        self.assertIn('AssetDatabase.GetAssetDependencyHash(path).ToString():"MISSING"', source)

    def test_dependency_is_the_same_original_asset_that_is_loaded(self):
        source = (EDITOR / 'HeartSetupFR01.cs').read_text(encoding='utf-8')
        match = re.search(r'ElderSourceFR01="([^"]+)"', source)
        self.assertIsNotNone(match)
        self.assertTrue((ROOT / match.group(1)).is_file())
        self.assertIn('AssetDatabase.LoadAssetAtPath<GameObject>(ElderSourceFR01)', source)
        self.assertIn('OptionalSourceStampFR01(ElderSourceFR01)', source)

    def test_living_streets_depends_on_the_foundation_fingerprint(self):
        source = (EDITOR / 'HeartSetupFR01.LivingStreets.cs').read_text(encoding='utf-8')
        self.assertEqual(source.count('PreparationStamp("FR02A material dependencies 2",json,receipt,foundation.Fingerprint)'), 2)


if __name__ == '__main__':
    unittest.main()
