"""Source guards for the existing grotto presentation. Not a Unity gameplay test."""
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORLD = ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World'


class GrottoRouteContracts(unittest.TestCase):
    def test_grotto_routes_are_existing_and_both_have_their_own_shared_zone(self):
        layout = (WORLD / 'CityLayout.Fractured04OC.cs').read_text()
        plan = (WORLD / 'HeartFR01/HeartPlanFR01.CompositionWR02B.cs').read_text()
        for route in ('grotto-through-route', 'grotto-return'):
            self.assertIn('AddRealmRoute04OB("' + route + '"', layout)
            self.assertIn('route == "' + route + '"', plan)
        self.assertIn('return "Grotto approach"', plan)

    def test_total_budget_and_existing_native_admission_are_retained(self):
        code = (WORLD / 'HeartFR01/HeartPresentationFR01.CompositionWR02B.cs').read_text()
        self.assertIn('totalAdmitted >= HeartPlanFR01.CompositionSoftLimitWR02B', code)
        self.assertIn('totalAdmitted++', code)
        self.assertIn('CompositionClearWR02B(_layout, plan, reserved, out reason)', code)
        self.assertIn('LiveWorldSafetyH8S.Conflict(', code)
        self.assertIn('CompositionGroundWR02B(plan, out reason)', code)


if __name__ == '__main__':
    unittest.main()
