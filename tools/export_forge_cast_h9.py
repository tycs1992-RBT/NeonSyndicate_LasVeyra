#!/usr/bin/env python3
"""Export the H9 Dante Rook study with complete skin weights and three actual LODs.

Derived from export_forge_cast_h8s.py (which stays untouched). Differences: the source is DanteRookH9.blend (refit
toward the gameplay rig, sockets on the correct side, new apron, tool belt and boots); the textures are copied from
the H8S NPC folder instead of re-reading the CC0 archive; objects sharing a material are joined into one skin so a
LOD has at most 9 skins; LOD distance decimation is unchanged (1.0, 0.6, 0.3 above 180 polygons).
"""
import bpy, argparse, hashlib, json, shutil
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
a = p.parse_args()
root = a.root.resolve()
source = root / 'ArtSource/AscensionH9/DanteRookH9.blend'
out = root / 'Assets/NeonSyndicate/Art/ProductionH9/NPC'
out.mkdir(parents=True, exist_ok=True)
h8s = root / 'Assets/NeonSyndicate/Art/ProductionH8S/NPC'
for name in ('DanteAlbedo.png', 'T_Eye_Brown.png', 'T_Eye_Normal.png', 'T_Superhero_Male_Normal.png'):
    shutil.copyfile(h8s / name, out / name)
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False, use_scripts=False)
rig = bpy.data.objects['DanteRig']
assert len(rig.data.bones) == 65
if rig.animation_data:
    rig.animation_data_clear()
for b in rig.pose.bones:
    b.rotation_mode = 'QUATERNION'; b.rotation_quaternion = (1, 0, 0, 0); b.location = (0, 0, 0); b.scale = (1, 1, 1)
bones = set(rig.data.bones.keys())
originals = [o for o in bpy.data.objects if o.type == 'MESH']


def active(o):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o


def complete(o):
    for v in o.data.vertices:
        rows = [(g.group, g.weight) for g in v.groups if o.vertex_groups[g.group].name in bones and g.weight > 0]
        total = sum(w for _, w in rows)
        if not rows or len(rows) > 8:
            raise ValueError('Invalid complete LOD skin on ' + o.name + ' vertex ' + str(v.index))
        for i, w in rows:
            o.vertex_groups[i].add([v.index], w / total, 'REPLACE')


files, stats, rows = [], [], []
for level, ratio in [(0, 1), (1, .6), (2, .3)]:
    copies = []
    for original in originals:
        o = original.copy(); o.data = original.data.copy(); bpy.context.collection.objects.link(o)
        o.name = 'LOD%d_%s' % (level, original.name); active(o)
        for m in list(o.modifiers):
            o.modifiers.remove(m)
        if ratio < 1 and len(o.data.polygons) > 180:
            m = o.modifiers.new('Distance simplification', 'DECIMATE'); m.ratio = ratio; bpy.ops.object.modifier_apply(modifier=m.name)
        complete(o)
        copies.append(o)
    # one skin per material
    by_mat = {}
    for o in copies:
        if len(o.data.materials) != 1:
            raise ValueError('Unbatched source material ' + o.name)
        by_mat.setdefault(o.data.materials[0].name, []).append(o)
    skins = []
    for mat, objs in sorted(by_mat.items()):
        bpy.ops.object.select_all(action='DESELECT')
        for o in objs:
            o.select_set(True)
        bpy.context.view_layer.objects.active = objs[0]
        if len(objs) > 1:
            bpy.ops.object.join()
        o = bpy.context.object
        o.name = 'LOD%d_%s' % (level, mat)
        active(o)
        m = o.modifiers.new('Explicit tangent-safe triangles', 'TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=m.name)
        m = o.modifiers.new('Complete original skin', 'ARMATURE'); m.object = rig
        skins.append(o)
        rows.append({'lod': level, 'name': o.name, 'material': mat, 'vertices': len(o.data.vertices), 'triangles': len(o.data.polygons)})
    if len(skins) > 9:
        raise ValueError('More than 9 skins in LOD %d' % level)
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
    for o in skins:
        o.select_set(True)
    for o in bpy.data.objects:
        if o.type == 'EMPTY':
            o.select_set(True)
    bpy.context.view_layer.objects.active = rig
    f = out / ('DanteRookH9' + ('' if level == 0 else '_LOD' + str(level)) + '.fbx')
    bpy.ops.export_scene.fbx(filepath=str(f), use_selection=True, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False,
                             bake_anim=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             path_mode='RELATIVE')
    files.append(f)
    stats.append({'lod': level, 'vertices': sum(len(o.data.vertices) for o in skins), 'triangles': sum(len(o.data.polygons) for o in skins), 'skins': len(skins)})
    for o in skins:
        bpy.data.objects.remove(o, do_unlink=True)
h8s_stats = json.loads((h8s / 'candidate.json').read_text()).get('lods', [])
rest = {b.name: [round(c, 4) for c in b.head_local] for b in rig.data.bones}
(out / 'export-stats.json').write_text(json.dumps({'bones': 65, 'lod_summary': stats, 'lods': rows, 'h8s_lods': h8s_stats,
                                                    'rest_heads_blender_metres': rest, 'native_unity': 'NOT RUN',
                                                    'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest()}, indent=2) + '\n')
print(json.dumps({'lods': stats, 'h8s': h8s_stats}), flush=True)
