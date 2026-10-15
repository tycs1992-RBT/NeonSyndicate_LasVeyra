#!/usr/bin/env python3
"""Audit reproducible offline evidence, without claiming Unity acceptance."""
import argparse
import hashlib
import json
from pathlib import Path
import struct

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
PREVIEWS = ROOT / 'Docs/LivingTerritories04B/Previews'
RESOURCES = ROOT / 'Assets/NeonSyndicate/Resources'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--previews', type=Path, default=PREVIEWS)
    parser.add_argument('--city-dir', type=Path, default=Path('/tmp/vr04a-world'))
    args = parser.parse_args()
    manifest = json.loads((args.previews / 'preview_manifest.json').read_text())
    checks = []; failures = []; images = {}; inputs = {}

    def check(name, condition):
        checks.append(name)
        if not condition: failures.append(name)

    def digest(path):
        value = hashlib.sha256(path.read_bytes()).hexdigest()
        inputs[str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path)] = value
        return value

    def same_hash(name, path, expected):
        check(name, path.exists() and digest(path) == expected)

    check('Explicit offline scope', manifest['unity_screenshots'] is False)
    renderer = ROOT / 'tools/render_living_territories_04b.py'
    same_hash('Final annotation renderer', renderer, manifest['annotation_renderer_sha256'])
    script = renderer.read_text()
    render_function = script[script.index('def render(args):'):script.index('\ndef prepare_actors')]
    if 'capture_render_function_sha256' in manifest:
        check('Scene generation function unchanged by footer refinement', hashlib.sha256(render_function.encode()).hexdigest() == manifest['capture_render_function_sha256'])
    check('Inspection LOD disclosed', manifest['actor_lod_scope']['inspection_detail'] == 2)
    for name, expected in manifest['actor_lod_scope']['runtime_source_sha256'].items():
        same_hash('Runtime LOD reference:' + name, ROOT / name, expected)
    same_hash('Runtime surface manifest', RESOURCES / 'VisualRebirth04A/material_manifest.json', manifest['material_manifest_sha256'])
    for field in ('geometry_sha256', 'texture_sha256'):
        for name, expected in manifest[field].items():
            path = ROOT / name if '/' in name else args.city_dir / name
            same_hash(field + ':' + name, path, expected)
    for name, expected in manifest['composition_source_sha256'].items():
        same_hash('Composition:' + name, ROOT / 'Assets/NeonSyndicate/Scripts/Runtime' / name, expected)

    actor_dir = args.previews / 'ActorSource'
    provenance = json.loads((actor_dir / 'source_provenance.json').read_text())
    check('Actor provenance captured in manifest', provenance == manifest['actor_source'])
    same_hash('Actor requests', actor_dir / 'requests.json', provenance['requests_sha256'])
    for name, expected in provenance['source_sha256'].items():
        same_hash('Actor source:' + name, ROOT / name, expected)
    actors = json.loads((actor_dir / 'actors.json').read_text())['actors']
    by_id = {a['request']['id']: a for a in actors}
    for actor in actors:
        path = actor_dir / actor['file']; raw = path.read_bytes(); digest(path)
        count = struct.unpack_from('<i', raw, 4)[0]
        valid = raw[:4] == b'CMH1' and count % 3 == 0 and len(raw) == 8 + count * 32
        check('Actor binary contract:' + actor['file'], valid)
        if valid:
            data = np.frombuffer(raw, dtype='<f4', offset=8).reshape(count, 8)
            check('Actor finite vertices:' + actor['file'], bool(np.isfinite(data).all()))
            check('Actor triangle count:' + actor['file'], count // 3 == actor['triangles'])
    for path in sorted(actor_dir.iterdir()):
        if path.is_file(): digest(path)

    required = ['hearth_court_before', 'hearth_court_after', 'hearth_court_night',
                'relay_amethyst_unowned', 'relay_amethyst_owned', 'relay_amethyst_fortified',
                'relay_amethyst_staff', 'relay_amethyst_flight', 'relay_verdant_ground',
                'relay_obsidian_ground', 'relay_obsidian_night', 'habitat_sanctuary_ground',
                'habitat_sanctuary_detail', 'habitat_sanctuary_flight', 'habitat_canopy_flight',
                'habitat_rift_ground', 'habitat_transition_ground', 'city_flight_before', 'city_flight_after']

    def layout_sources(node):
        if isinstance(node, list):
            for item in node: layout_sources(item)
        elif isinstance(node, dict):
            if 'path' in node and 'sha256' in node:
                same_hash('Layout:' + node['path'], ROOT / node['path'], node['sha256'])
            for value in node.values():
                if isinstance(value, (dict, list)): layout_sources(value)

    for name in required:
        check('Required view:' + name, name in manifest['views'])
        if name not in manifest['views']: continue
        view = manifest['views'][name]; path = args.previews / view['file']
        check('Image exists:' + name, path.exists())
        if not path.exists(): continue
        with Image.open(path) as image:
            check('Resolution:' + name, list(image.size) == view['resolution'])
            check('Burned-in footer background:' + name, image.convert('RGB').getpixel((0, image.height - 1)) == (8, 16, 25))
        images[name] = {'sha256': digest(path), 'size': path.stat().st_size, 'resolution': view['resolution']}
        layout_sources(view['layout'])
        for actor in view['actors']:
            check('Actual actor request:' + name + ':' + actor['id'], actor['request'] == by_id[actor['id']]['request'])
        if name.startswith('city_flight'):
            same_hash('City export:' + name, args.city_dir / 'city_all.json', view['layout']['city_export_sha256'])
            if name.endswith('after'):
                same_hash('Provisional placement report', ROOT / 'Docs/LivingTerritories04B/Validation/placement_candidates.json', view['layout']['placement_report_sha256'])
        elif not name.startswith('hearth_court'):
            layout = json.loads((ROOT / view['layout']['path']).read_text())
            selected = [n for n in layout['nodes'] if n.get('tier', 0) <= view['tier']
                        and (not n.get('owned', False) or view['owned'])
                        and n.get('variant', 0) in (0, abs(view['variant']) % 3 + 1)
                        and (not n.get('resource', False) or not view['resource_claimed'])]
            check('Exact state-filtered part count:' + name, len(selected) + 2 == view['parts'])

    for before, after in [('hearth_court_before', 'hearth_court_after'), ('city_flight_before', 'city_flight_after'),
                          ('relay_amethyst_unowned', 'relay_amethyst_owned'), ('relay_amethyst_unowned', 'relay_amethyst_fortified')]:
        if before not in manifest['views'] or after not in manifest['views']: continue
        for field in ['camera_unity', 'target_unity', 'mood', 'resolution', 'samples']:
            check('Matched ' + field + ':' + before + '/' + after, manifest['views'][before][field] == manifest['views'][after][field])
    for path in sorted((RESOURCES / 'LivingTerritories04B/Habitats').glob('*.json')): digest(path)
    digest(RESOURCES / 'LivingTerritories04B/territories_catalog.json')
    digest(ROOT / 'tools/render_visual_rebirth_04a.py')
    digest(Path(__file__).resolve())
    report = {'scope': 'Offline image/source integrity only. Unity, native placement, live AI and gameplay NOT RUN.',
              'status': 'PASS' if not failures else 'FAIL', 'checks': len(checks), 'failures': failures,
              'views': len(images), 'actors_exported': len(actors), 'actor_triangles': sum(a['triangles'] for a in actors),
              'images': images, 'input_sha256': inputs}
    (args.previews / 'preview_validation.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: report[k] for k in ('scope', 'status', 'checks', 'failures', 'views', 'actors_exported', 'actor_triangles')}, indent=2))
    return 1 if failures else 0


if __name__ == '__main__':
    raise SystemExit(main())
