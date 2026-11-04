#!/usr/bin/env python3
"""Check the route wiring and preserve the earlier exact owner contracts.

These source checks do not execute Unity or establish the playability of a path.
"""
import json
from pathlib import Path
import re
import subprocess
import unittest

from composition_lineage_wr02b import load_contract, reviewed_view

ROOT = Path(__file__).resolve().parents[1]
START = '5ba215f9941c92fd6a5c7552425ee28fb24b1a77'
ROUTE = 'Assets/NeonSyndicate/Scripts/Runtime/UI/VisibleRealityV0.WorldRewrite.cs'


class RouteIntegrityTests(unittest.TestCase):
    def test_only_the_reviewed_route_contract_changed(self):
        old = json.loads(subprocess.check_output(['git', 'show', START + ':tools/wr02b_owner_hooks.json'], cwd=ROOT))
        new = load_contract()
        self.assertEqual(old['baseline'], new['baseline'])
        self.assertEqual(set(old['paths']), set(new['paths']))
        for path in old['paths']:
            if path != ROUTE:
                self.assertEqual(old['paths'][path], new['paths'][path], path)
        before, after = old['paths'][ROUTE], new['paths'][ROUTE]
        self.assertEqual(before['before_sha256'], after['before_sha256'])
        self.assertEqual(before['edits'][0]['before'], after['edits'][0]['before'])
        self.assertEqual(len(after['edits']), len(before['edits']) + 1)
        original = subprocess.check_output(['git', 'show', new['baseline'] + ':' + ROUTE], cwd=ROOT)
        current = (ROOT / ROUTE).read_bytes()
        self.assertEqual(reviewed_view(ROUTE, original, current), original)
        self.assertEqual(reviewed_view(ROUTE, original, current + b'\n// unexpected edit\n'), current + b'\n// unexpected edit\n')

    def test_circuit_stops_use_existing_realm_tokens_in_order(self):
        source = (ROOT / ROUTE).read_text()
        rows = re.findall(r'DrawRewriteRouteWR01\(rect, ref y, "([^"]+)", "([^"]+)"', source)
        self.assertEqual(len(rows), len(set(title for title, _ in rows)))
        expected = [('Home court', 'Venue:lofts.exit'), ('Forge and return', 'Venue:forge.exit'),
                    ('Archive sanctuary', 'Venue:archive.exit'), ('Stone threshold', 'Realm:threshold'),
                    ('Mineral grotto', 'Realm:grotto'), ('Woodland causeway', 'Realm:azure'),
                    ('Pressure House', 'Venue:pressure.exit')]
        self.assertEqual(rows, expected)
        resolver = (ROOT / 'Assets/NeonSyndicate/Scripts/Runtime/World/CityLayout.Fractured04OC.cs').read_text()
        for token in ('Realm:threshold', 'Realm:grotto', 'Realm:azure'):
            self.assertIn('case "' + token + '"', resolver)
        self.assertIn('Game.Layout.ResolveRealmAnchor04OC(token, out point)', source)
        self.assertIn('CampaignPlacesRuntime04E.TryResolve(token, out point, out yaw, out reason)', source)
        self.assertIn('Game.Gps.SetWaypoint(point)', source)

    def test_route_points_match_existing_quest_data(self):
        quest_dir = ROOT / 'Assets/NeonSyndicate/Resources/Quests'
        stone = json.loads((quest_dir / 'gothic_realm_04ob.json').read_text())['Quests'][0]
        trees = json.loads((quest_dir / 'fractured_wilds_04oc.json').read_text())['Quests'][0]
        self.assertEqual(stone['Id'], 'fg_04ob_stone_not_silence')
        self.assertEqual(trees['Id'], 'fg_04oc_roots_remember')
        self.assertIn(stone['Id'], trees['Requires']['Quests'])
        self.assertIn(6, stone['Requires']['WorldRevisions'])
        self.assertIn(6, trees['Requires']['WorldRevisions'])
        stone_stops = {s.get('At') for p in stone['Phases'] for s in p['Steps']}
        tree_stops = {s.get('At') for p in trees['Phases'] for s in p['Steps']}
        self.assertTrue({'Venue:archive.record2', 'Realm:threshold', 'Realm:grotto', 'Venue:forge.exit'} <= stone_stops)
        self.assertTrue({'Realm:azure', 'Venue:forge.exit'} <= tree_stops)


if __name__ == '__main__':
    unittest.main()
