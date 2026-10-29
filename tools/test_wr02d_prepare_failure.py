"""Source contracts for first-time preparation failures. Not a Unity import test."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / 'Assets/NeonSyndicate/Scripts/Editor'


class PreparationFailureContracts(unittest.TestCase):
    def test_each_retry_clears_only_its_layer_before_loading_a_catalog(self):
        for name, method, layer in [('HeartSetupFR01.cs', 'public static void Prepare(bool verbose)', 'false'),
                                    ('HeartSetupFR01.LivingStreets.cs', 'public static void PrepareLiving(bool verbose)', 'true')]:
            source = (EDITOR / name).read_text(encoding='utf-8').split(method, 1)[1]
            clear = 'ClearPreparationFailureFR01(' + layer + ');'
            self.assertLess(source.index(clear), source.index('AssetDatabase.LoadAssetAtPath<HeartCatalogFR01>'))
            self.assertIn('SessionState.SetString(PreparationFailureKeyFR01(' + layer + '),e.Message)', source)
            self.assertIn('catalog.Modules=null;catalog.Fingerprint="";catalog.Reason=e.Message', source)

    def test_missing_catalog_uses_failure_only_for_the_two_original_layers(self):
        source = (EDITOR / 'FantasyAcceptanceV1.cs').read_text(encoding='utf-8')
        self.assertIn('if (blockers[i] != prefix + "preparation created no catalog") continue;', source)
        self.assertIn('HeartSetupFR01.PreparationFailureFR01(living)', source)
        self.assertIn('if (!string.IsNullOrEmpty(failure)) blockers[i] = prefix + SafeMessage(failure);', source)
        self.assertIn('RefinePreparationFailures(_receipt.blockers,', source)
        self.assertIn('if (living ? !livingMissing : !heartMissing) continue;', source)

    def test_clear_and_exception_storage_are_separate_for_each_layer(self):
        source = (EDITOR / 'HeartSetupFR01.cs').read_text(encoding='utf-8')
        self.assertIn('(living?"FR02A":"FR01")', source)
        self.assertIn('SessionState.GetString(PreparationFailureKeyFR01(living),"")', source)
        self.assertIn('SessionState.SetString(PreparationFailureKeyFR01(living),"")', source)


if __name__ == '__main__':
    unittest.main()
