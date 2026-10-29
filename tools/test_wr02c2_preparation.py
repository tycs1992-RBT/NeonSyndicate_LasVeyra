#!/usr/bin/env python3
"""Cross-check the existing foundry and Gothic Heart preparation contract.

These source and payload checks do not import assets or run Unity.
"""
import hashlib
import json
from pathlib import Path
import re
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[1]
EDITOR = ROOT / 'Assets/NeonSyndicate/Scripts/Editor'
FOUNDRY = EDITOR / 'HeartSetupFR01.FoundryWR02A.cs'


class PreparationContractTests(unittest.TestCase):
    def test_foundry_loads_materials_from_the_actual_foundation_directory(self):
        foundation = (EDITOR / 'HeartSetupFR01.cs').read_text()
        foundry = FOUNDRY.read_text()
        self.assertRegex(foundation, r'Prepared="Assets/NeonSyndicate/Resources/HeartPreparedFR01"')
        self.assertIn('string path=Prepared+"/"+name+".mat"', foundation)
        self.assertIn('AssetDatabase.LoadAssetAtPath<Material>(Prepared + "/" + name + ".mat")', foundry)
        self.assertNotIn('Assets/NeonSyndicate/Art/HeartFR01/Materials/', foundry)
        self.assertIn('shared.shader.isSupported', foundry)
        for required in ('shared.mainTexture', 'shared.GetTexture("_BumpMap")', 'shared.GetTexture("_MetallicGlossMap")'):
            self.assertIn(required, foundry)

    def test_normal_preparation_reaches_foundation_before_foundry_and_review(self):
        setup = (EDITOR / 'HeartSetupFR01.WorldRewrite.cs').read_text()
        menu = re.search(r'public static void MenuRewrite\(\)\s*\{([^}]+)\}', setup).group(1)
        calls = ['Prepare(true)', 'PrepareRewrite(true)', 'PrepareFoundryWR02A(true)', 'PrepareCompositionWR02B(true)']
        indexes = [menu.index(call) for call in calls]
        self.assertEqual(indexes, sorted(indexes))
        acceptance = (EDITOR / 'FantasyAcceptanceV1.cs').read_text()
        self.assertIn('HeartSetupFR01.PrepareFoundryWR02A(true)', acceptance)
        self.assertIn('HeartSetupFR01.PrepareCompositionWR02B(true)', acceptance)

    def test_existing_archive_payloads_and_material_sources_are_intact(self):
        manifest = json.loads((ROOT / 'Assets/NeonSyndicate/Art/FoundryWR02A/manifest.json').read_text())
        self.assertEqual(manifest['license'], 'CC0-1.0')
        self.assertEqual({row['name'] for row in manifest['modules']}, {'WRArchiveCrown', 'CrystalBasiliskCandidate'})
        for name in ('WRArchiveCrown', 'CrystalBasiliskCandidate'):
            self.assertEqual({row['lod'] for row in manifest['modules'] if row['name'] == name}, {0, 1, 2})
        for row in manifest['modules'] + manifest['textures']:
            path = ROOT / row['path']
            self.assertTrue(path.is_file(), row['path'])
            self.assertEqual(path.stat().st_size, row['bytes'], row['path'])
            self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), row['sha256'], row['path'])

    def test_required_district_source_receipts_are_hydrated(self):
        for directory, count in (('HeartFR01', 33), ('HeartFR02A', 39)):
            receipt = json.loads((ROOT / 'Assets/NeonSyndicate/Art' / directory / 'manifest.json').read_text())
            self.assertEqual(len(receipt['modules']), count, directory)
            self.assertEqual(len(receipt['textures']), 15, directory)
            for row in receipt['modules'] + receipt['textures']:
                path = ROOT / row['path']
                self.assertTrue(path.is_file(), row['path'])
                self.assertEqual(path.stat().st_size, row['bytes'], row['path'])
                self.assertEqual(hashlib.sha256(path.read_bytes()).hexdigest(), row['sha256'], row['path'])

    def test_original_creature_stays_review_only(self):
        runtime = (ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs').read_text()
        self.assertIn('proof.creatureActivationAllowed = false;', runtime)
        editor = (EDITOR / 'HeartSetupFR01.CompositionWR02B.cs').read_text()
        self.assertIn('Identity = "creature:" + BasiliskSkinRulesWR02B.SpeciesId, Enabled = false', editor)

    def test_broken_foundry_crown_is_checked_before_selection(self):
        runtime = (ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.FoundryWR02A.cs').read_text()
        method = runtime.split('private HeartCatalogFR01 CivicCatalogFoundryWR02A', 1)[1].split('private void FillFoundryProofWR02A', 1)[0]
        selection = method.index('return _foundryCatalogWR02A;')
        for check in ('Fingerprint.Length == 64', 'RewriteArtBoundsWR01(', '!Passive(prefab)', '!ValidPreparedModelV1(prefab, out reason)'):
            self.assertLess(method.index(check), selection, check)
        self.assertIn('WR02A crown refused:', method[selection:])
        self.assertIn('return _rewriteCatalog;', method[selection:])

    def test_f2_keeps_preparation_failure_instead_of_masking_it(self):
        proof = (ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/HeartFR01/HeartPresentationFR01.ProofV1.cs').read_text()
        summary = (ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/QA/FantasyProofV1.cs').read_text()
        self.assertIn('if (!proof.fr01.prepared || !proof.fr02a.prepared)', summary)
        self.assertIn('name + " prepared catalog missing.', proof)
        self.assertIn('name + " prepared catalog incomplete:', proof)
        self.assertIn('module.Id + " LOD" + lod', proof)
        for layer in ('fr01', 'fr02a'):
            self.assertIn('(proof.' + layer + '.prepared || proof.' + layer + '.firstRefusal.Length == 0)', proof)

    def test_preservation_replay_allows_only_the_complete_repair(self):
        from wr02c2_preparation_lineage import BASE, CONTRACTS, original_view
        for target, (_, _, repairs) in CONTRACTS.items():
            with self.subTest(target=target):
                original = subprocess.check_output(['git', 'show', BASE + ':' + target], cwd=ROOT)
                current = (ROOT / target).read_bytes()
                replayed = original
                for before, after in repairs:
                    self.assertEqual(replayed.count(before), 1)
                    replayed = replayed.replace(before, after, 1)
                self.assertEqual(current, replayed)
                self.assertEqual(original_view(ROOT, target, current), original)
                mutations = [current + b'\n// unexpected edit\n'] + [current.replace(after, before, 1) for before, after in repairs]
                for changed in mutations:
                    self.assertNotEqual(changed, current)
                    self.assertEqual(original_view(ROOT, target, changed), changed)
                self.assertEqual(original_view(ROOT, 'another-file.cs', current), current)


if __name__ == '__main__':
    unittest.main()
