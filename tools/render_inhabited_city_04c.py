#!/usr/bin/env python3
"""Render shipped, connected 04C room source geometry. Native gameplay is not demonstrated."""
import argparse
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
from offline_source_scene import ROOT, RESOURCES, SourceScene, label

OUT = ROOT / 'Docs/InhabitedCity04C/Previews'


def render(args):
    import bpy
    source = SourceScene(); manifest_path = args.output / 'preview_manifest.json'
    manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else {'scope': 'OFFLINE SOURCE PREVIEW, NOT UNITY GAMEPLAY', 'views': {}}
    for name in args.views.split(','):
        venue, view = name.split('_', 1); path = args.source / (venue + '.json'); data = json.loads(path.read_text())
        source.clear(); binding = data['binding']; parent = source.transform({'position': binding['origin'], 'euler': [0, binding['yaw'], 0]})
        before = view == 'before'; night = 'night' in view
        parts = data['baseline'] if before else data['parts']; total = 0
        for part in parts:
            if not before and (part.get('low', False) or part.get('progress', 0) > args.owned_relays): continue
            if 'file' in part: mesh = args.source / part['file']
            else:
                family = {'04A': 'VisualRebirth04A', '04B': 'LivingTerritories04B', '04C': 'InhabitedCity04C'}[part['source']]
                mesh = RESOURCES / family / 'Meshes' / (part['model'] + '_high.bytes')
            source.add(part, mesh, None if before else parent); total += 1
        if view in ('before', 'after', 'threshold', 'threshold_night'):
            camera = [0, 1.68, -3.8]; target = [0, 1.68, 10]; lens = 28
        elif 'stairs' in view:
            camera = [-3.8, 1.68, 3.2]; target = [-5.5, 3.1, 11.8]; lens = 28
        elif 'upper' in view:
            camera = [-3.8, 5.88, 14.7]; target = [3.3, 5.88, 7.5]; lens = 26
        else:
            camera = [5.7, 1.68, 1.7]; target = [-2.1, 1.65, 11.6]; lens = 28

        def world(p):
            a = math.radians(binding['yaw']); origin = binding['origin']
            return [origin[0] + math.cos(a) * p[0] + math.sin(a) * p[2], origin[1] + p[1], origin[2] - math.sin(a) * p[0] + math.cos(a) * p[2]]
        scene = source.setup(world(camera), world(target), args.width, args.height, args.samples, night, lens)
        if not before:
            for practical in data['expanded']['lights'][:3]: source.practical(practical, parent)
        scene.render.filepath = str(args.output / (name + '.png')); bpy.ops.render.render(write_still=True)
        manifest['views'][name] = {'file': name + '.png', 'venue': venue, 'view': view, 'baseline': before,
            'camera_local': camera, 'target_local': target, 'camera_world': world(camera), 'target_world': world(target),
            'lens_mm': lens, 'eye_height': camera[1], 'samples': args.samples, 'resolution': [args.width, args.height],
            'source': str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path), 'source_sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
            'source_parts': total, 'binding': binding, 'static_pose_only': True,
            'source_connection': 'Single physical room and original upper building at the actual source binding. No detached room or removed roof. Native traversal NOT RUN.'}
        manifest['geometry_and_texture_sha256'] = dict(manifest.get('geometry_and_texture_sha256', {}), **source.hashes)
        manifest['render_source_sha256'] = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path(__file__).resolve(), ROOT / 'tools/offline_source_scene.py', ROOT / 'tools/render_visual_rebirth_04a.py']}
        manifest['source_export'] = json.loads((args.source / 'source_provenance.json').read_text())
        manifest['lod_scope'] = 'Near venue source meshes use intended High detail; original preserved upper uses detailed Tier/Crown. Baseline ordinary city uses original ordinary budget.'
        manifest['lighting_scope'] = 'Exact practical light positions/colors; Blender intensity scale90 and sunlight/exposure approximate Unity. Native shadows, weather exclusion, materials and performance NOT VERIFIED.'
        manifest['unity'] = 'NOT RUN'; manifest['renderer'] = bpy.app.version_string + ' CPU Cycles, no denoising'
        manifest_path.write_text(json.dumps(manifest, indent=2) + '\n')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--views', default='lofts_threshold,lofts_interior,lofts_stairs,lofts_upper,clinic_interior,forge_interior')
    parser.add_argument('--source', type=Path, default=OUT / 'SourceGeometry'); parser.add_argument('--output', type=Path, default=OUT)
    parser.add_argument('--width', type=int, default=1440); parser.add_argument('--height', type=int, default=900); parser.add_argument('--samples', type=int, default=48)
    parser.add_argument('--owned-relays', type=int, default=0); parser.add_argument('--threads', type=int, default=4)
    parser.add_argument('--inside-blender', action='store_true'); parser.add_argument('--label-only', action='store_true')
    args = parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else None); args.output.mkdir(parents=True, exist_ok=True)
    if args.inside_blender: render(args); return
    if not args.label_only:
        subprocess.run(['blender', '-b', '--threads', str(args.threads), '--python', str(Path(__file__).resolve()), '--', '--inside-blender', '--views', args.views,
                        '--source', str(args.source), '--output', str(args.output), '--width', str(args.width), '--height', str(args.height), '--samples', str(args.samples), '--owned-relays', str(args.owned_relays)], check=True)
    for name in args.views.split(','):
        label(args.output / (name + '.png'), name.replace('_', ' ').title(), 'Actual room and binding source. Intended near LOD. Approximate offline lighting/materials; native entry and collision unverified.')


if __name__ == '__main__': main()
