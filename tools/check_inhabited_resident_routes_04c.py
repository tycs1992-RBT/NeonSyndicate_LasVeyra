#!/usr/bin/env python3
"""Conservative resident clearance checks against exact exported production room geometry.

The export executes Composition.Expand and actual mesh sources. These numerical
checks do not import Unity assets or prove live capsule movement and animation.
"""
import argparse
import hashlib
import json
from pathlib import Path
import numpy as np
from check_visual_rebirth_04a import decode, intersects_box, transform_vertices, visual_surface_at

ROOT = Path(__file__).resolve().parents[1]
RESOURCE = ROOT / 'Assets/NeonSyndicate/Resources'
PACKAGES = {'04A': 'VisualRebirth04A', '04B': 'LivingTerritories04B', '04C': 'InhabitedCity04C'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', type=Path, default=ROOT / 'Docs/InhabitedCity04C/Previews/SourceGeometry')
    parser.add_argument('--output', type=Path, default=ROOT / 'Docs/InhabitedCity04C/Validation/resident_routes.json')
    args = parser.parse_args()
    results, hashes, venues = [], {}, []
    files = sorted(args.source.glob('*.json'))
    for path in files:
        data = json.loads(path.read_text())
        if 'expanded' not in data or data['id'] == 'depot':
            continue
        layout, venue = data['expanded'], data['id']
        venues.append(venue)
        hashes[str(path.relative_to(ROOT))] = hashlib.sha256(path.read_bytes()).hexdigest()
        authored_path = RESOURCE / 'InhabitedCity04C/Layouts' / (venue + '.json')
        authored = json.loads(authored_path.read_text())
        hashes[str(authored_path.relative_to(ROOT))] = hashlib.sha256(authored_path.read_bytes()).hexdigest()
        results.append({'venue': venue, 'check': 'exported resident anchors match current authored layout',
                        'passed': layout['anchors'] == authored['anchors']})
        anchors = {anchor['id']: np.array(anchor['position'], dtype=float) for anchor in layout['anchors']}
        for lod in ('high', 'low'):
            geometry = []
            for part in data['parts']:
                if 'file' in part:
                    mesh_path = path.parent / part['file']
                else:
                    mesh_path = RESOURCE / PACKAGES[part['source']] / 'Meshes' / (part['model'] + '_' + lod + '.bytes')
                hashes[str(mesh_path.relative_to(ROOT))] = hashlib.sha256(mesh_path.read_bytes()).hexdigest()
                points = transform_vertices(decode(mesh_path)[1][:, :3], part)
                geometry.append((part, points.reshape(-1, 3, 3)))
            floor = [triangles for part, triangles in geometry if part['nodeId'] == 'interior_floor']
            for station in ('staff', 'visitor1', 'visitor2'):
                first = anchors[station]
                second = anchors.get(station + '_next', first)
                low = np.minimum(first, second) + [-.4, .06, -.4]
                high = np.maximum(first, second) + [.4, 2.1, .4]
                blockers = [part['nodeId'] for part, triangles in geometry if intersects_box(triangles, low, high)]
                results.append({'venue': venue, 'lod': lod, 'station': station, 'check': 'conservative complete route body envelope clear of actual visual triangles', 'passed': not blockers, 'blockers': blockers})
                support = all(any(visual_surface_at(triangles, point[0], point[1], point[2]) for triangles in floor)
                    for point in np.linspace(first, second, 9))
                results.append({'venue': venue, 'lod': lod, 'station': station, 'check': 'nine route points supported by actual floor triangles', 'passed': support})
                safe = all(np.linalg.norm(point - anchors[protected]) >= 1.3
                    for point in np.linspace(first, second, 9) for protected in ('exit', 'service', 'activity'))
                results.append({'venue': venue, 'lod': lod, 'station': station, 'check': 'route remains outside interaction clearance', 'passed': safe})
    expected = [row['Layout'] for row in json.loads((RESOURCE / 'InhabitedCity04C/venues.json').read_text())['Venues'] if row['Kind'] != 'depot']
    missing = sorted(set(expected) - set(venues))
    report = {'scope': __doc__.strip(), 'venues': venues, 'missing_exports': missing,
              'passed': sum(row['passed'] for row in results), 'failed': [row for row in results if not row['passed']],
              'source_sha256': hashes, 'results': results, 'native_collision_and_ai': 'NOT RUN'}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({key: value for key, value in report.items() if key not in ('results', 'source_sha256')}, indent=2))
    return 1 if report['failed'] or missing or not results else 0


if __name__ == '__main__':
    raise SystemExit(main())
