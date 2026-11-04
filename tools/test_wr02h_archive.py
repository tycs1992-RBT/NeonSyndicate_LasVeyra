"""Source contracts only. Native model and frame checks require Unity."""
from pathlib import Path
import subprocess
import unittest
from wr02h_archive_lineage import BASE, TARGET, original_view

ROOT = Path(__file__).resolve().parents[1]

class ArchiveFallbackTests(unittest.TestCase):
    def test_foundry_actual_frame_is_probed_before_selection(self):
        source = (ROOT / TARGET).read_text()
        selection = source.split('private HeartCatalogFR01 CivicCatalogFoundryWR02A', 1)[1].split('private bool ProbeFoundryFrameWR02H', 1)[0]
        self.assertLess(selection.index('ProbeFoundryFrameWR02H(plan, out reason)'), selection.index('return _foundryCatalogWR02A;'))
        self.assertIn('return _rewriteCatalog;', selection)
        probe = source.split('private bool ProbeFoundryFrameWR02H', 1)[1].split('private void FillFoundryProofWR02A', 1)[0]
        for required in ('Model(plan.Family', 'CrownFramesWR01(probe, probe.transform', 'finally', 'Destroy(probe)'):
            self.assertIn(required, probe)

    def test_preservation_requires_the_exact_complete_change(self):
        current = (ROOT / TARGET).read_bytes()
        before = subprocess.check_output(['git', 'show', BASE + ':' + TARGET], cwd=ROOT)
        self.assertEqual(original_view(ROOT, TARGET, current), before)
        for altered in (current + b'\n// unrelated\n', current.replace(b'if (valid) valid = ProbeFoundryFrameWR02H(plan, out reason);', b'', 1)):
            self.assertEqual(original_view(ROOT, TARGET, altered), altered)
        self.assertEqual(original_view(ROOT, 'unrelated.cs', current), current)

if __name__ == '__main__':
    unittest.main()
