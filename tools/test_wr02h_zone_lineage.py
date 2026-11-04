"""Keep the route change bounded and preserve the exact original planner."""
from pathlib import Path
import subprocess
import unittest
from wr02h_route_lineage import BASE, TARGET, original_view

ROOT = Path(__file__).resolve().parents[1]
class RouteLineageTests(unittest.TestCase):
    def test_exact_change_and_mutations(self):
        current = (ROOT / TARGET).read_bytes()
        before = subprocess.check_output(['git', 'show', BASE + ':' + TARGET], cwd=ROOT)
        self.assertEqual(original_view(ROOT, TARGET, current), before)
        for changed in (current + b'\n// unrelated\n', current.replace(b'InterleaveCompositionZonesWR02H(result)', b'result', 1)):
            self.assertEqual(original_view(ROOT, TARGET, changed), changed)
        self.assertEqual(original_view(ROOT, 'unrelated.cs', current), current)

if __name__ == '__main__':
    unittest.main()
