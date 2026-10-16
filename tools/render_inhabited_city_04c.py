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
    provenance = json.loads((args.source / 'source_provenance.json').read_text())
    if manifest.get('source_export') != provenance:
        manifest = {'scope': 'OFFLINE SOURCE PREVIEW, NOT UNITY GAMEPLAY', 'views': {}}
    actor_dir = OUT / 'ActorSource'
    for name in args.views.split(','):
        venue, view = name.split('_', 1)
        neighborhood = venue == 'neighborhood'
        path = args.source / (('lofts' if neighborhood else venue) + '.json'); data = json.loads(path.read_text())
        source.clear(); binding = data['binding']; parent = source.transform({'position': binding['origin'], 'euler': [0, binding['yaw'], 0]})
        before = view == 'before'; night = 'night' in view
        total = 0
        def draw_room(room, old, people=False):
            nonlocal total
            origin = source.transform({'position':room['binding']['origin'],'euler':[0,room['binding']['yaw'],0]})
            for part in room['baseline'] if old else room['parts']:
                if not old and (part.get('low', False) or part.get('progress', 0) > args.owned_relays): continue
                if 'file' in part: mesh = args.source / part['file']
                else:
                    family = {'04A':'VisualRebirth04A','04B':'LivingTerritories04B','04C':'InhabitedCity04C'}[part['source']]
                    mesh = RESOURCES / family / 'Meshes' / (part['model']+'_high.bytes')
                source.add(part,mesh,None if old else origin); total += 1
            if not old:
                for text in room['expanded']['labels']: source.physical_label(text,origin)
                if people and room['id'] != 'depot' and (actor_dir/'actors.json').exists():
                    anchors={a['id']:a for a in room['expanded']['anchors']}
                    for slot, identifier in [(0,'staff'),(1,'visitor1')]:
                        a=anchors[identifier]; source.person(actor_dir/(room['id']+'_'+str(slot)+'.bytes'),a['position'],a['yaw'],origin,actor_dir)
        draw_room(data,before,not neighborhood)
        if venue in ('lofts','inn','archive','observatory') or neighborhood:
            context_path=args.source/'neighborhood_context.json'; context=json.loads(context_path.read_text()); source.hash(context_path)
            for part in context['parts']: source.add(part,args.source/part['file']); total += 1
            for other in ('lofts','inn','archive','observatory'):
                if other == data['id']: continue
                other_path=args.source/(other+'.json'); source.hash(other_path)
                draw_room(json.loads(other_path.read_text()),before)
        if view in ('before', 'after', 'frontage', 'frontage_night'):
            camera = [0, 1.68, -12]; target = [0, 4.1, 1]; lens = 28
        elif view in ('threshold', 'threshold_night'):
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
        camera_world, target_world = world(camera), world(target)
        if neighborhood:
            camera_world, target_world = [-3,1.83,-5],[-40,4,-42]
        scene = source.setup(camera_world, target_world, args.width, args.height, args.samples, night, lens)
        if not before:
            for practical in data['expanded']['lights'][:3]: source.practical(practical, parent)
        scene.render.filepath = str(args.output / (name + '.png')); bpy.ops.render.render(write_still=True)
        manifest['views'][name] = {'file': name + '.png', 'venue': venue, 'view': view, 'baseline': before,
            'camera_local': None if neighborhood else camera, 'target_local': None if neighborhood else target, 'camera_world': camera_world, 'target_world': target_world,
            'lens_mm': lens, 'eye_height': camera[1], 'samples': args.samples, 'resolution': [args.width, args.height],
            'source': str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else str(path), 'source_sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
            'source_parts': total, 'binding': binding, 'static_pose_only': True,
            'source_connection': 'Single physical room and original upper building at the actual source binding. No detached room or removed roof. Native traversal NOT RUN.'}
        manifest['geometry_and_texture_sha256'] = dict(manifest.get('geometry_and_texture_sha256', {}), **source.hashes)
        manifest['render_source_sha256'] = {str(p.relative_to(ROOT)): hashlib.sha256(p.read_bytes()).hexdigest() for p in [Path(__file__).resolve(), ROOT / 'tools/offline_source_scene.py', ROOT / 'tools/render_visual_rebirth_04a.py']}
        manifest['source_export'] = provenance
        manifest['residents'] = 'Actual 04C Look/Seed plus existing standard body and idle-pose solvers, statically placed at authored first stations. No live AI or admission proof. Runtime Forge anvil not included in room source preview.'
        manifest['typography'] = 'Authored text and transforms, approximate offline font metrics. Native TextMesh is unverified.'
        if (actor_dir/'source_provenance.json').exists():
            source.hash(actor_dir/'actors.json'); source.hash(actor_dir/'source_provenance.json')
        manifest['geometry_and_texture_sha256'].update(source.hashes)
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
    path=args.output/'preview_manifest.json'; manifest=json.loads(path.read_text())
    for name in args.views.split(','): manifest['views'][name]['artifact_sha256']=hashlib.sha256((args.output/(name+'.png')).read_bytes()).hexdigest()
    path.write_text(json.dumps(manifest,indent=2)+'\n')


if __name__ == '__main__': main()
