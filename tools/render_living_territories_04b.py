#!/usr/bin/env python3
"""Render actual 04B source layouts and existing actor builders in Blender.

Offline static source evidence only. No Unity, live AI, collision or performance claims.
The mesh/material review helpers retain the 04A rig to allow matched comparisons.
"""
import argparse
import hashlib
import json
import math
import os
from pathlib import Path
import struct
import subprocess
import sys
import tempfile
sys.path.insert(0, str(Path(__file__).resolve().parent))
import render_visual_rebirth_04a as prior

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/NeonSyndicate/Resources/VisualRebirth04A'
LIVING = ROOT / 'Assets/NeonSyndicate/Resources/LivingTerritories04B'
OLD = ROOT / 'Assets/NeonSyndicate/Resources/CrystalStrongholds02I'
OUT = ROOT / 'Docs/LivingTerritories04B/Previews'
CACHE = Path(tempfile.gettempdir()) / 'visual-rebirth-04a-preview'
resolve_material = prior.resolve_material

HABITATS = {'habitat_sanctuary':'neutral_sanctuary', 'habitat_canopy':'canopy_observation',
            'habitat_rift':'rift_nesting', 'habitat_transition':'transitional_roaming','home_care':'home_care'}


def preview_manifest():
    p = OUT / 'preview_manifest.json'
    return json.loads(p.read_text()) if p.exists() else {'format':'04B offline source visual evidence', 'unity_screenshots':False, 'views':{}}


def render(args):
    import bpy
    from mathutils import Matrix, Vector
    names = args.views.split(',')
    manifest = preview_manifest()
    model_cache = {}
    material_cache = {}
    hashes = {}
    material_manifest = json.loads((ART / 'material_manifest.json').read_text()) if (ART / 'material_manifest.json').exists() else {}
    C = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))
    CI = C.inverted()

    def transform(part):
        pos = part.get('position', [0, 0, 0]); rot = part.get('euler', [0, 0, 0]); scl = part.get('scale', [1, 1, 1])
        r = [math.radians(x) for x in rot]
        return C @ Matrix.Translation(Vector(pos)) @ Matrix.Rotation(r[1], 4, 'Y') @ Matrix.Rotation(r[0], 4, 'X') @ Matrix.Rotation(r[2], 4, 'Z') @ Matrix.Diagonal((*scl, 1)) @ CI

    def point(v):
        return (v[0], -v[2], v[1])

    def mesh(path):
        key = str(path)
        if key in model_cache:
            return model_cache[key]
        import numpy as np
        raw = path.read_bytes()
        count = struct.unpack_from('<i', raw, 4)[0]
        if raw[:4] != b'CMH1' or count % 3 or len(raw) != 8 + count * 32:
            raise ValueError('Invalid runtime mesh: ' + str(path))
        data = np.frombuffer(raw, dtype='<f4', offset=8).reshape(count, 8)
        if not np.isfinite(data).all():
            raise ValueError('Non-finite runtime vertex: ' + str(path))
        coords = np.stack((data[:, 0], -data[:, 2], data[:, 1]), axis=1)
        normals = np.stack((data[:, 3], -data[:, 5], data[:, 4]), axis=1)
        m = bpy.data.meshes.new(path.stem)
        m.vertices.add(count); m.vertices.foreach_set('co', coords.ravel())
        m.loops.add(count); m.loops.foreach_set('vertex_index', np.arange(count, dtype=np.int32))
        m.polygons.add(count // 3)
        m.polygons.foreach_set('loop_start', np.arange(0, count, 3, dtype=np.int32))
        m.polygons.foreach_set('loop_total', np.full(count // 3, 3, dtype=np.int32))
        m.update()
        uv = m.uv_layers.new(name='UVMap'); uv.data.foreach_set('uv', data[:, 6:8].ravel())
        # Original hard-edge and smooth normals are retained, including crystal facets.
        m.normals_split_custom_set_from_vertices(normals)
        model_cache[key] = m
        hashes[str(path.relative_to(ROOT)) if path.is_relative_to(ROOT) else path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
        return m

    def old_tint(style, role):
        table = {
            'stone': [(0.59, .63, .76), (.86, .79, .61), (.48, .56, .48), (.21, .23, .27)],
            'trim': [(.91, .88, .77), (.91, .88, .77), (.72, .76, .57), (.43, .36, .31)],
            'gilt': [(.84, .70, .38), (.84, .70, .38), (.66, .69, .37), (.67, .38, .17)],
            'bark': [(.32, .25, .19)] * 4,
            'leaf': [(.21, .46, .34), (.37, .49, .24), (.21, .46, .34), (.23, .28, .24)],
            'cloth': [(.30, .20, .49), (.58, .36, .14), (.13, .39, .29), (.38, .12, .09)],
            'crystal': [(.47, .57, 1), (1, .73, .34), (.19, .8, .56), (1, .32, .07)]}
        return table.get(role, table['crystal'])[style]

    def material(style, role, baseline=False):
        key = (style, role, baseline)
        if key in material_cache:
            return material_cache[key]
        spec = resolve_material(material_manifest, style, role) if not baseline else {}
        if baseline:
            family = 'stone' if role in ('stone', 'trim') else 'cloth' if role == 'cloth' else 'leaf' if role in ('leaf', 'bark') else 'mineral'
            spec = {'maps': {'albedo': str(CACHE / 'baseline/maps' / (family + '.png'))}, 'normalStrength': .42}
            if family == 'stone':
                spec['maps']['normal'] = str(CACHE / 'baseline/maps/normal.png')
        tint = spec.get('tint', old_tint(style, role))
        rough = spec.get('roughness', .44 if role in ('gilt', 'metal') else .75 if role in ('stone', 'trim') else .64)
        metallic = spec.get('metallic', .7 if role in ('gilt', 'metal') else 0)
        m = bpy.data.materials.new(('03A2 ' if baseline else '04A ') + str(style) + '/' + role)
        m.use_nodes = True
        nodes = m.node_tree.nodes; links = m.node_tree.links
        bsdf = nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (*tint[:3], 1)
        bsdf.inputs['Metallic'].default_value = metallic
        bsdf.inputs['Roughness'].default_value = rough
        maps = spec.get('maps', {})
        for channel, socket in [('albedo', 'Base Color'), ('roughness', 'Roughness')]:
            file = maps.get(channel)
            if not file:
                continue
            path = ART / file
            if not path.exists():
                raise FileNotFoundError(path)
            image = bpy.data.images.load(str(path), check_existing=True)
            image.colorspace_settings.name = 'sRGB' if channel == 'albedo' else 'Non-Color'
            tex = nodes.new('ShaderNodeTexImage'); tex.image = image
            if channel == 'albedo':
                mul = nodes.new('ShaderNodeMixRGB'); mul.blend_type = 'MULTIPLY'; mul.inputs[0].default_value = 1
                mul.inputs[2].default_value = (*tint[:3], 1)
                links.new(tex.outputs['Color'], mul.inputs[1]); links.new(mul.outputs['Color'], bsdf.inputs[socket])
            else:
                links.new(tex.outputs['Color'], bsdf.inputs[socket])
        if maps.get('normal'):
            tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(ART / maps['normal']), check_existing=True); tex.image.colorspace_settings.name = 'Non-Color'
            norm = nodes.new('ShaderNodeNormalMap'); norm.inputs['Strength'].default_value = spec.get('normalStrength', .55)
            links.new(tex.outputs['Color'], norm.inputs['Color']); links.new(norm.outputs['Normal'], bsdf.inputs['Normal'])
        if maps.get('mask'):
            tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(ART / maps['mask']), check_existing=True); tex.image.colorspace_settings.name = 'Non-Color'
            separate = nodes.new('ShaderNodeSeparateColor'); links.new(tex.outputs['Color'], separate.inputs['Color'])
            links.new(separate.outputs['Red'], bsdf.inputs['Metallic'])
            inverse = nodes.new('ShaderNodeMath'); inverse.operation = 'SUBTRACT'; inverse.inputs[0].default_value = 1
            links.new(tex.outputs['Alpha'], inverse.inputs[1]); links.new(inverse.outputs[0], bsdf.inputs['Roughness'])
        emission = spec.get('emission', .20 if role in ('crystal', 'bloom') else 0)
        if isinstance(emission, list):
            emit_tint = emission; emission = 1
        else:
            emit_tint = tint
        if emission:
            bsdf.inputs['Emission Color'].default_value = (*emit_tint[:3], 1)
            bsdf.inputs['Emission Strength'].default_value = emission
            if maps.get('emission'):
                tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(ART / maps['emission']), check_existing=True)
                tex.image.colorspace_settings.name = 'sRGB'
                mul = nodes.new('ShaderNodeMixRGB'); mul.blend_type = 'MULTIPLY'; mul.inputs[0].default_value = 1; mul.inputs[2].default_value = (*emit_tint[:3], 1)
                links.new(tex.outputs['Color'], mul.inputs[1]); links.new(mul.outputs['Color'], bsdf.inputs['Emission Color'])
        material_cache[key] = m
        return m

    def add_part(part, style, source='04A', baseline=False):
        name = part['model']
        if source == '02I':
            path = OLD / 'Geometry' / (name + '.bytes')
        elif source == '04B':
            path = LIVING / 'Meshes' / (name + '_' + part.get('lod','high') + '.bytes')
        elif source == 'ACTOR':
            path = Path(args.actor_dir) / (name + '.bytes')
        elif source == 'CITY':
            path = Path(args.city_dir) / (name + '.bytes')
        elif source == '03A2':
            path = CACHE / 'baseline' / (name + '.bytes')
        else:
            path = ART / 'Meshes' / (name + '_' + part.get('lod','high') + '.bytes')
        ob = bpy.data.objects.new(source + '/' + name, mesh(path))
        bpy.context.collection.objects.link(ob)
        ob.matrix_world = transform(part)
        ob.data.materials.clear() if not ob.data.materials else None
        ob.data.materials.append(material(style, part['role'], baseline)) if not ob.data.materials else None
        ob.material_slots[0].link = 'OBJECT'
        ob.material_slots[0].material = material(style, part['role'], baseline)
        return ob

    def setup(camera_pos, target, mood):
        scene = bpy.context.scene
        scene.render.engine = 'CYCLES'
        scene.cycles.samples = args.samples
        scene.cycles.use_adaptive_sampling = True
        scene.cycles.adaptive_threshold = .04
        scene.cycles.adaptive_min_samples = min(16, args.samples)
        scene.cycles.use_denoising = False
        scene.cycles.max_bounces = 5
        scene.cycles.diffuse_bounces = 3
        scene.cycles.glossy_bounces = 3
        scene.render.resolution_x = args.width; scene.render.resolution_y = args.height; scene.render.resolution_percentage = 100
        scene.render.image_settings.file_format = 'PNG'
        scene.view_settings.view_transform = 'AgX'
        scene.view_settings.look = 'AgX - Medium High Contrast'
        scene.view_settings.exposure = .5 if mood == 'day' else 1.1
        world = bpy.data.worlds.new('Offline review world')
        scene.world = world; world.use_nodes = True
        bg = world.node_tree.nodes.get('Background')
        bg.inputs['Color'].default_value = (.58, .69, .81, 1) if mood == 'day' else (.1, .16, .28, 1)
        bg.inputs['Strength'].default_value = .55 if mood == 'day' else .35
        bpy.ops.object.light_add(type='SUN', location=(0, 0, 40))
        sun = bpy.context.object; sun.name = 'Offline review directional light'
        sun.rotation_euler = (math.radians(30), math.radians(-20), math.radians(-35))
        sun.data.energy = 2.1 if mood == 'day' else .65
        sun.data.angle = math.radians(9 if mood == 'day' else 14)
        sun.data.color = (1, .85, .68) if mood == 'day' else (.52, .68, 1)
        bpy.ops.mesh.primitive_plane_add(size=2000, location=(0, 0, -.035))
        ground = bpy.context.object; ground.name = 'Review ground plane, not game terrain'
        mat = bpy.data.materials.get('Review floor') or bpy.data.materials.new('Review floor')
        mat.use_nodes = True; p = mat.node_tree.nodes.get('Principled BSDF'); p.inputs['Base Color'].default_value = (.155, .17, .16, 1); p.inputs['Roughness'].default_value = .95
        ground.data.materials.append(mat)
        bpy.ops.object.camera_add(location=point(camera_pos))
        camera = bpy.context.object
        direction = Vector(point(target)) - camera.location
        camera.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
        camera.data.lens = 42
        camera.data.clip_end = 2500
        scene.camera = camera
        return scene

    def clear():
        for ob in list(bpy.data.objects):
            bpy.data.objects.remove(ob, do_unlink=True)

    def fit_source_camera(scene, target_unity, padding=1.15):
        # Review cameras must show the complete real source silhouette. Do not
        # include the artificial review ground in the framing calculation.
        bpy.context.view_layer.update()
        camera = scene.camera
        target = Vector(point(target_unity))
        outward = (camera.location - target).normalized()
        right = camera.rotation_euler.to_matrix() @ Vector((1, 0, 0))
        up = camera.rotation_euler.to_matrix() @ Vector((0, 1, 0))
        frame = camera.data.view_frame(scene=scene)
        tan_x = max(abs(v.x / v.z) for v in frame)
        tan_y = max(abs(v.y / v.z) for v in frame)
        distance = (camera.location - target).length
        for ob in scene.objects:
            if ob.type != 'MESH' or not ob.name.startswith('CITY/'):
                continue
            for corner in ob.bound_box:
                delta = ob.matrix_world @ Vector(corner) - target
                distance = max(distance,
                    abs(delta.dot(right)) * padding / tan_x + delta.dot(outward),
                    abs(delta.dot(up)) * padding / tan_y + delta.dot(outward))
        camera.location = target + outward * distance
        x, y, z = camera.location
        return (x, z, -y)

    def cross_floor(style, origin=(0, 0, 0)):
        for x0, z0, x1, z1, y, u, v in [(-23, -4.5, 23, 4.5, .012, 10, 2), (-4.5, -23, 4.5, 23, .013, 2, 10)]:
            m = bpy.data.meshes.new('Actual runtime paving cross')
            points = [(x0,y,z0),(x0,y,z1),(x1,y,z1),(x1,y,z0)]
            m.from_pydata([point([v[i] + origin[i] for i in range(3)]) for v in points], [], [(0,1,2,3)])
            m.update(); uv = m.uv_layers.new(name='UVMap')
            for i, entry in enumerate(uv.data): entry.uv = [(0,0),(0,v),(u,v),(u,0)][i]
            ob = bpy.data.objects.new('Actual runtime paving cross', m); bpy.context.collection.objects.link(ob)
            m.materials.append(material(style, 'paving'))

    def layout(site, tier=0, owned=False, origin=(0, 0, 0), variant=None):
        path = LIVING / 'Layouts' / (site + '.json')
        data = json.loads(path.read_text()); count = 0
        variant = args.variant if variant is None else variant
        for part in data['nodes']:
            if part.get('tier', 0) > tier or (part.get('owned', False) and not owned): continue
            if part.get('variant',0) not in (0,abs(variant) % 3 + 1): continue
            if part.get('resource',False) and args.resource_claimed:continue
            placed = dict(part); placed['position'] = [part['position'][i] + origin[i] for i in range(3)]
            style=1 if part['role']=='cloth' and variant%3==1 and data['style']==0 else data['style']
            add_part(placed, style, part.get('source', '04A')); count += 1
        cross_floor(data['style'], origin)
        return data, count + 2, {'path':str(path.relative_to(ROOT)), 'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}

    def actor_material(palette):
        key = ('actor', palette)
        if key in material_cache: return material_cache[key]
        m = bpy.data.materials.new('Actual actor palette / ' + palette); m.use_nodes = True
        nodes=m.node_tree.nodes; links=m.node_tree.links; bsdf=nodes.get('Principled BSDF')
        texs = {}
        for channel in ['albedo', 'properties', 'emission']:
            path = Path(args.actor_dir) / (palette + '_' + channel + '.png')
            tex = nodes.new('ShaderNodeTexImage'); tex.image = bpy.data.images.load(str(path), check_existing=True)
            tex.interpolation = 'Closest'; tex.image.colorspace_settings.name = 'Non-Color' if channel == 'properties' else 'sRGB'
            texs[channel] = tex
        links.new(texs['albedo'].outputs['Color'], bsdf.inputs['Base Color'])
        separate = nodes.new('ShaderNodeSeparateColor'); links.new(texs['properties'].outputs['Color'], separate.inputs['Color'])
        links.new(separate.outputs['Red'], bsdf.inputs['Metallic'])
        inverse = nodes.new('ShaderNodeMath'); inverse.operation='SUBTRACT'; inverse.inputs[0].default_value=1
        links.new(texs['properties'].outputs['Alpha'],inverse.inputs[1]); links.new(inverse.outputs[0],bsdf.inputs['Roughness'])
        links.new(texs['emission'].outputs['Color'],bsdf.inputs['Emission Color']); bsdf.inputs['Emission Strength'].default_value=1
        material_cache[key]=m; return m

    def populate(site, origin=(0, 0, 0), night=False):
        staging_path = Path(args.actor_dir) / 'staging.json'
        if not staging_path.exists(): return []
        staging=json.loads(staging_path.read_text()); actors=json.loads((Path(args.actor_dir)/'actors.json').read_text())['actors']
        by_id={a['request']['id']:a for a in actors}; used=[]
        for part in staging.get(site, []):
            if part.get('night',False) != night and part.get('conditional',False): continue
            actor=by_id[part['model']]; placed=dict(part)
            placed['position']=[part['position'][i]+origin[i] for i in range(3)]
            ob=bpy.data.objects.new('Static source pose / '+part['model'],mesh(Path(args.actor_dir)/actor['file']))
            bpy.context.collection.objects.link(ob); ob.matrix_world=transform(placed)
            if not ob.data.materials:ob.data.materials.append(actor_material(actor['palette']))
            ob.material_slots[0].link='OBJECT';ob.material_slots[0].material=actor_material(actor['palette'])
            used.append({'id':part['model'],'request':actor['request'],'position':placed['position'],'euler':placed.get('euler',[0,0,0])})
        return used

    def home(after, progress, night=False):
        path=ART/'Gardens/sanctuary.json';data=json.loads(path.read_text());count=0
        sources=[{'path':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}]
        for part in data['nodes']:add_part(part,2);count+=1
        cross_floor(2);count+=2
        actors=populate('home_baseline',night=night)
        if after:
            path=LIVING/'Props/home_ledger.json';display=json.loads(path.read_text())
            sources.append({'path':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
            for node in display['nodes']:
                if node.get('progress',0)>progress:continue
                placed=dict(node);placed['position']=[node['position'][0],node['position'][1],node['position'][2]-28]
                if node.get('progress',0)>0:placed['lod']='low'
                add_part(placed,display['style'],node['source']);count+=1
            actors+=populate('home_care',night=night)+populate('home_staff',night=night)
        return count,sources,actors

    def city(after):
        path=Path(args.city_dir)/'city_all.json';data=json.loads(path.read_text())
        for part in data['parts']:add_part(part,part['style'],'CITY')
        source={'city_export_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'city_seed':61724,
                'city_scope':data,'placements':[],'placement_status':'Source exclusions only. Native terrain, Physics and installed court positions NOT VERIFIED.'}
        count=len(data['parts'])
        if after:
            report_path=ROOT/'Docs/LivingTerritories04B/Validation/placement_candidates.json'
            report=json.loads(report_path.read_text());candidates=next(s['sites'] for s in report['seeds'] if s['seed']==61724)
            catalog=json.loads((LIVING/'territories_catalog.json').read_text());definitions={s['id']:s for s in catalog['sites']}
            by_habitat={value:key for key,value in HABITATS.items()}
            source['placement_report_sha256']=hashlib.sha256(report_path.read_bytes()).hexdigest()
            for candidate in candidates:
                at=candidate['provisionalSourceCandidate']
                if at is None:continue
                site=candidate['id'];definition=definitions.get(site)
                layout_id=definition['layout'] if definition else by_habitat[site]
                _,n,layout_source=layout(layout_id,origin=at,variant=definition['variant'] if definition else 0)
                count+=n
                source['placements'].append({'id':site,'position':at,'layout':layout_source,'status':candidate['status']})
        return count,source

    for name in names:
        clear(); night=name.endswith('_night'); mood='night' if night else 'day'
        suffixes=['_fortified','_unowned','_owned','_flight','_ground','_night','_detail','_staff']
        site=name
        for suffix in suffixes:
            if site.endswith(suffix):site=site[:-len(suffix)];break
        tier=2 if name.endswith('_fortified') else 0;owned=name.endswith(('_fortified','_owned'))
        actors=[]
        if name.startswith('hearth_court'):
            count,source,actors=home('before' not in name,args.owned_count,night)
            camera=(7,4.5,-49);target=(0,3,-14)
        elif name.startswith('city_flight'):
            count,source=city('after' in name)
            camera=(-1720,380,-650);target=(-1240,50,10)
        else:
            data,count,source=layout(site,tier,owned)
            actors=populate(site,night=night)
        if name.startswith(('hearth_court','city_flight')):pass
        elif name.endswith('_flight'):
            camera=(46,36,-55);target=(0,5,2)
        elif name.endswith('_detail'):
            camera=(8,2.2,0);target=(0,1.7,10)
        elif name.endswith('_staff'):
            camera=(14,2.2,-11);target=(7,1.1,-2)
        elif site.startswith('habitat'):
            camera=(-6,3.2,-28);target=(0,3.2,8)
        else:
            camera=(1,3.4,-31);target=(0,4.5,5)
        scene=setup(camera,target,mood)
        if night and site.startswith('relay_'):
            bpy.ops.object.light_add(type='POINT',location=point((0,4,10)))
            light=bpy.context.object;light.name='Existing bounded hub accent, offline intensity approximation'
            light.data.energy=120;light.data.shadow_soft_size=1
            light.data.color={'relay_amethyst':(.68,.45,1),'relay_verdant':(.4,.85,.58),'relay_obsidian':(1,.42,.22)}[site]
        if name.startswith('city_flight'):
            scene.camera.data.clip_end=14000
            bpy.data.objects['Review ground plane, not game terrain'].scale*=8
        scene.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
        manifest['views'][name]={'file':name+'.png','layout':source,'site':site,'tier':tier,'owned':owned,'variant':args.variant,'resource_claimed':args.resource_claimed,
            'parts':count,'actors':actors,'camera_unity':camera,'target_unity':target,'mood':mood,
            'samples':args.samples,'resolution':[args.width,args.height],
            'source_triangles':sum(len(ob.data.loops)-2*len(ob.data.polygons) for ob in scene.objects if ob.type=='MESH'),
            'owned_relays_in_home_example':args.owned_count if name.startswith('hearth_court') and 'before' not in name else None,
            'pose_scope':'Static frames from actual source rigs, not live AI or gameplay.'}
        manifest['geometry_sha256']=dict(manifest.get('geometry_sha256',{}),**hashes)
        manifest['material_manifest_sha256']=hashlib.sha256((ART/'material_manifest.json').read_bytes()).hexdigest()
        manifest['texture_sha256']={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted((ART/'Textures').glob('*.png'))}
        actor_provenance=Path(args.actor_dir)/'source_provenance.json'
        if actor_provenance.exists():manifest['actor_source']=json.loads(actor_provenance.read_text())
        manifest['blender_version']=bpy.app.version_string;manifest['renderer']='CPU Cycles, no denoising'
        manifest['renderer_sha256']=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
        relevant=['ArtTerritories04B/TerritoryArt04BView.cs','ArtTerritories04B/TerritoryHome04BDisplay.cs',
                  'Gameplay/Territories/TerritorySystem.Home.cs','ArtRebirth04A/VisualRebirth04ANeighborhoods.cs']
        manifest['composition_source_sha256']={p:hashlib.sha256((ROOT/'Assets/NeonSyndicate/Scripts/Runtime'/p).read_bytes()).hexdigest() for p in relevant}
        manifest['review_rig']={'lens_mm':42,'day_sun':2.1,'day_world':.55,'night_sun':.65,'night_world':.35,'exposure_day':.5,'exposure_night':1.1,'view':'AgX Medium High Contrast'}
        manifest['limitations']=['Offline source renders, not Unity screenshots.','Static actor poses do not demonstrate AI or live animation.','Plain review ground and lighting; no Unity shader, collision, streaming or performance verification.']
        (OUT/'preview_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


def prepare_actors(actor_dir, dotnet):
    """Use actual habitat slot data and exact pure seeded appearance factories."""
    import export_actors_04b
    from PIL import Image
    import numpy as np
    actor_dir.mkdir(parents=True,exist_ok=True);requests=[];staging={}
    for site,habitat in HABITATS.items():
        path=LIVING/'Habitats'/(habitat+'.json');data=json.loads(path.read_text());staging[site]=[]
        for index,slot in enumerate(data['slots']):
            for night in ([False,True] if slot['day']!=slot['night'] else [False]):
                species=slot['night' if night else 'day'];identifier=site+'_'+str(index)+('_night' if night else '')
                requests.append({'id':identifier,'species':species,'siteId':data['id'],'slotIndex':index,'detail':2,'flying':slot['aerial']})
                staging[site].append({'model':identifier,'position':slot['position'],'euler':[0,slot['yaw'],0],
                    'night':night,'conditional':slot['day']!=slot['night']})
    catalog=json.loads((LIVING/'territories_catalog.json').read_text())
    for site in catalog['sites']:
        if not site['hub']:continue
        for index in range(3):
            requests.append({'id':site['id']+'_staff_'+str(index),'kind':'human','region':site['region'],
                'siteId':site['id'],'slotIndex':index+40,'staffIndex':index,'detail':2})
    requests.append({'id':'home_decorative_drake','species':'quartz_drake','seed':61724,'detail':1})
    requests.append({'id':'home_steward','kind':'human','region':'amethyst','siteId':'hearthspire_caretaker','slotIndex':40,'staffIndex':0,'hearth':True,'detail':2})
    request_path=actor_dir/'requests.json';request_path.write_text(json.dumps(requests,indent=2)+'\n')
    export_actors_04b.export(request_path,actor_dir,dotnet.resolve())
    actors=json.loads((actor_dir/'actors.json').read_text())['actors']
    garden=json.loads((ART/'Gardens/sanctuary.json').read_text())
    staging['home_baseline']=[{'model':'home_decorative_drake','position':garden['drake'],'euler':[0,95,0]}]
    home_layout=json.loads((LIVING/'Props/home_ledger.json').read_text());offset=home_layout['staff'][0]['position']
    home_actor=next(a for a in actors if a['request']['id']=='home_steward')
    staging['home_staff']=[{'model':'home_steward','position':[offset[0],offset[1],offset[2]-28],'euler':[0,home_actor['staffYaw'],0]}]
    for site in catalog['sites']:
        if not site['hub']:continue
        staging[site['layout']]=[]
        for actor in actors:
            request=actor['request']
            if request['kind']=='human' and request['siteId']==site['id']:
                staging[site['layout']].append({'model':request['id'],'position':actor['staffOffset'],'euler':[0,actor['staffYaw'],0]})
    (actor_dir/'staging.json').write_text(json.dumps(staging,indent=2)+'\n')
    for palette in ('character','quartz_regalia'):
        data=json.loads((actor_dir/(palette+'_palette.json')).read_text())
        for channel,values in data.items():
            pixels=np.rint(np.clip(np.array(values).reshape(16,16,4),0,1)*255).astype(np.uint8)
            Image.fromarray(pixels[::-1]).save(actor_dir/(palette+'_'+channel+'.png'))


def main():
    global OUT
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--views',default='relay_amethyst_ground,habitat_sanctuary_ground')
    parser.add_argument('--samples',type=int,default=32);parser.add_argument('--width',type=int,default=1280);parser.add_argument('--height',type=int,default=800)
    parser.add_argument('--inside-blender',action='store_true');parser.add_argument('--export-actors',action='store_true')
    parser.add_argument('--label-only',action='store_true',help='Refresh explicit offline annotations without rerendering geometry.')
    parser.add_argument('--actor-dir',type=Path,default=OUT/'ActorSource');parser.add_argument('--city-dir',type=Path,default=Path('/tmp/vr04a-world'))
    parser.add_argument('--output-dir',type=Path,default=OUT);parser.add_argument('--threads',type=int,default=min(4,os.cpu_count() or 1))
    parser.add_argument('--dotnet',type=Path,default=ROOT.parent/'.tools/dotnet/dotnet')
    parser.add_argument('--variant',type=int,default=0)
    parser.add_argument('--owned-count',type=int,default=3)
    parser.add_argument('--resource-claimed',action='store_true')
    args=parser.parse_args(sys.argv[sys.argv.index('--')+1:] if '--' in sys.argv else None)
    OUT=args.output_dir;OUT.mkdir(parents=True,exist_ok=True)
    if args.export_actors:prepare_actors(args.actor_dir,args.dotnet);return
    if args.views=='all':
        args.views=','.join(['hearth_court_before','hearth_court_after','hearth_court_night',
            'relay_amethyst_unowned','relay_amethyst_owned','relay_amethyst_fortified','relay_amethyst_staff','relay_amethyst_flight',
            'relay_verdant_ground','relay_obsidian_ground','relay_obsidian_night',
            'habitat_sanctuary_ground','habitat_sanctuary_detail','habitat_sanctuary_flight',
            'habitat_canopy_flight','habitat_rift_ground','habitat_transition_ground','city_flight_before','city_flight_after'])
    if args.inside_blender:render(args);return
    command=['blender','-b','--threads',str(args.threads),'--python',str(Path(__file__).resolve()),'--','--inside-blender',
        '--views',args.views,'--samples',str(args.samples),'--width',str(args.width),'--height',str(args.height),
        '--output-dir',str(OUT),'--actor-dir',str(args.actor_dir),'--city-dir',str(args.city_dir),'--variant',str(args.variant),
        '--owned-count',str(args.owned_count)]
    if args.resource_claimed:command.append('--resource-claimed')
    if not args.label_only:subprocess.run(command,check=True)
    for name in args.views.split(','):
        detail='Source geometry; approximate materials, native skin omitted. Static poses. Placement provisional; Unity unverified.'
        if name.startswith('city_flight'):
            detail='Actual city geometry, approximate materials. 04B candidates provisional; native terrain and placement NOT VERIFIED.'
        elif name.startswith('hearth_court'):
            detail='Matched source court; approximate materials, native skin omitted. Static poses. Footer placement needs native survey.'
        prior.label_image(OUT/(name+'.png'),name.replace('_',' ').title(),detail)
    manifest=preview_manifest()
    for name in args.views.split(','):
        manifest['views'][name].setdefault('capture_renderer_sha256',manifest['renderer_sha256'])
    manifest['actor_lod_scope']={
        'inspection_detail':2,
        'exception':'The existing 04A decorative quartz drake uses detail 1.',
        'runtime_difference':'Staff and ordinary wildlife use ambient detail 1 on Medium/High/Ultra and 0 on Low; glass mites always use 0. Quartz Drake GreatBeast uses 2 on High/Ultra. Inspection meshes therefore exceed several runtime ambient LODs.',
        'claim':'Actual source builders at a disclosed inspection LOD, not runtime mesh or appearance parity.',
        'runtime_source_sha256':{p:hashlib.sha256((ROOT/p).read_bytes()).hexdigest() for p in [
            'Assets/NeonSyndicate/Scripts/Runtime/Characters/BodyFactory.cs',
            'Assets/NeonSyndicate/Scripts/Runtime/Creatures/CreatureDirector.Bodies.cs']}}
    manifest['annotation_renderer_sha256']=hashlib.sha256(Path(__file__).read_bytes()).hexdigest()
    (OUT/'preview_manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__=='__main__':main()
