#!/usr/bin/env python3
"""Export the H9 Malik derivative and its fitted wardrobe; complete weights and visual-only authority preserved.

Derived from export_selected_malik_h8s.py (which stays untouched). Same socket rebuild, same complete-weight policy
(1 to 8 influences, normalized, no zero-weight vertex), same material batching, same axes. H9 changes:
- per-object LOD policy from the authoring script ('h9_lod_policy'): groom shells keep LOD1 and halve at LOD2;
  top cards drop their outer layer at LOD1 and keep half the inner layer at LOD2; lashes and the tear line are
  dropped at LOD2; everything else follows the H8S distance decimation (0.65 / 0.34 above 180 polygons);
- every LOD must keep the same 12 contract material batches (asserted);
- export-stats.json records per-batch and per-LOD totals and the H8S comparison.
The candidate receipt is sealed separately by seal_malik_h9.py after the motion export and validation.
"""
import bpy, bmesh, argparse, hashlib, json
from pathlib import Path
from mathutils import Vector, Matrix

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
a = p.parse_args()
root = a.root.resolve()
source = root / 'ArtSource/AscensionH9/MalikVeyrH9.blend'
out = root / 'Assets/NeonSyndicate/Art/ProductionH9'
CONTRACT = {'H6 Aged bronze', 'H6 Brows', 'H6 Cornea', 'H6 Worn leather', 'H6 Blackened steel', 'H6 Charcoal wool',
            'H6 Restrained amethyst', 'H8R Textured groom', 'H6 Eyes', 'H8R Skin', 'H6 Pupils', 'H6 Sclera'}
bpy.ops.wm.open_mainfile(filepath=str(source), load_ui=False, use_scripts=False)
rig = bpy.data.objects['MalikRig']
rig.animation_data_clear()
for b in rig.pose.bones:
    b.rotation_mode = 'QUATERNION'; b.rotation_quaternion = (1, 0, 0, 0); b.location = (0, 0, 0); b.scale = (1, 1, 1)
for o in list(bpy.data.objects):
    if o.type not in {'MESH', 'ARMATURE'}:
        bpy.data.objects.remove(o, do_unlink=True)
# Visual review locators are rebuilt on the selected bind, without moving the gameplay weapon (as H8S).
roles = {'hand_r': 'hand_r', 'hand_l': 'hand_l', 'foregrip': 'hand_l', 'muzzle': 'hand_r', 'blade_hilt': 'hand_r',
         'blade_tip': 'hand_r', 'head': 'Head', 'chest': 'spine_03', 'fx': 'spine_03', 'synergy': 'spine_03'}
for role, bone in roles.items():
    o = bpy.data.objects.new('socket_' + role, None); bpy.context.collection.objects.link(o)
    o.parent = rig; o.parent_type = 'BONE'; o.parent_bone = bone; bpy.context.view_layer.update()
    o.matrix_world = Matrix.Translation(rig.data.bones[bone].head_local)
o = bpy.data.objects.new('review_forward', None); bpy.context.collection.objects.link(o); o.location = (0, -.4, 1.7); o.parent = rig
originals = [o for o in bpy.data.objects if o.type == 'MESH']
bones = set(rig.data.bones.keys())


def active(o):
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active = o


def complete_weights(obj, label):
    for v in obj.data.vertices:
        rows = [(g.group, g.weight) for g in v.groups if obj.vertex_groups[g.group].name in bones and g.weight > 0]
        total = sum(w for _, w in rows)
        if not rows or len(rows) > 8:
            raise ValueError('Complete weight policy cannot export ' + label + ' ' + obj.name + ' vertex ' + str(v.index))
        for i, w in rows:
            obj.vertex_groups[i].add([v.index], w / total, 'REPLACE')


for obj in originals:
    for m in list(obj.modifiers):
        if m.type != 'ARMATURE':
            active(obj); bpy.ops.object.modifier_apply(modifier=m.name)
    if not obj.data.uv_layers:
        active(obj); bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
        bpy.ops.uv.smart_project(island_margin=.015); bpy.ops.object.mode_set(mode='OBJECT')
    complete_weights(obj, 'LOD0')


def thin_cards(obj, level):
    me = obj.data
    layer = me.attributes.get('h9_card_layer'); index = me.attributes.get('h9_card_index')
    if layer is None or index is None:
        raise ValueError('Card LOD policy needs h9_card_layer and h9_card_index on ' + obj.name)
    lay = [d.value for d in layer.data]; idx = [d.value for d in index.data]
    bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
    doomed = []
    for v in bm.verts:
        L, I = lay[v.index], idx[v.index]
        if L == 1 or (level == 2 and (L == 2 or I % 2 == 1)):
            doomed.append(v)
    bmesh.ops.delete(bm, geom=doomed, context='VERTS'); bm.to_mesh(me); bm.free()


lod_rows = []
for level in [1, 2]:
    for original in originals:
        policy = original.get('h9_lod_policy', '')
        if policy == 'drop_lod2' and level == 2:
            continue
        obj = original.copy(); obj.data = original.data.copy(); bpy.context.collection.objects.link(obj)
        obj.name = 'LOD' + str(level) + '_' + original.name; active(obj)
        for m in list(obj.modifiers):
            obj.modifiers.remove(m)
        ratio = None
        if policy == 'cards':
            thin_cards(obj, level)
        elif policy == 'shell':
            ratio = None if level == 1 else 0.5
        elif policy == 'drop_lod2':
            ratio = None
        elif len(obj.data.polygons) > 180:
            ratio = .65 if level == 1 else .34
        if ratio is not None:
            mod = obj.modifiers.new('Selected anatomy distance LOD', 'DECIMATE'); mod.ratio = ratio
            bpy.ops.object.modifier_apply(modifier=mod.name)
        complete_weights(obj, 'LOD' + str(level))
        mod = obj.modifiers.new('Complete selected skin', 'ARMATURE'); mod.object = rig
for obj in originals:
    obj.name = 'LOD0_' + obj.name
batches = {}
for o in [o for o in bpy.data.objects if o.type == 'MESH']:
    if len(o.data.materials) != 1:
        raise ValueError('Unbatched source material ' + o.name)
    batches.setdefault((o.name.split('_')[0], o.data.materials[0].name), []).append(o)
rows = []
for (level, material), objects in sorted(batches.items()):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    o = bpy.context.object; o.name = level + '_' + material; active(o)
    m = o.modifiers.new('Explicit tangent-safe triangles', 'TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=m.name)
    rows.append({'lod': level, 'name': o.name, 'vertices': len(o.data.vertices), 'triangles': len(o.data.polygons), 'material': material})
for level in ('LOD0', 'LOD1', 'LOD2'):
    mats = {r['material'] for r in rows if r['lod'] == level}
    if mats != CONTRACT:
        raise ValueError(level + ' material batches differ from the 12-name contract: ' + str(sorted(mats ^ CONTRACT)))
files = []
(out / 'Models').mkdir(parents=True, exist_ok=True)
for level in range(3):
    f = out / 'Models' / ('MalikVeyrH9' + ('' if level == 0 else '_LOD' + str(level)) + '.fbx')
    for o in bpy.data.objects:
        o.select_set(o.type in {'ARMATURE', 'EMPTY'} or o.type == 'MESH' and o.name.startswith('LOD' + str(level) + '_'))
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=str(f), use_selection=True, object_types={'MESH', 'ARMATURE', 'EMPTY'}, add_leaf_bones=False,
                             bake_anim=False, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                             path_mode='RELATIVE', mesh_smooth_type='FACE')
    files.append(f)
totals = {}
for level in ('LOD0', 'LOD1', 'LOD2'):
    rs = [r for r in rows if r['lod'] == level]
    totals[level] = {'vertices': sum(r['vertices'] for r in rs), 'triangles': sum(r['triangles'] for r in rs), 'batches': len(rs)}
h8s = json.loads((root / 'Assets/NeonSyndicate/Art/ProductionH8S/Models/export-stats.json').read_text())
before = {}
for level in ('LOD0', 'LOD1', 'LOD2'):
    rs = [r for r in h8s['lods'] if r['lod'] == level]
    before[level] = {'vertices': sum(r['vertices'] for r in rs), 'triangles': sum(r['triangles'] for r in rs), 'batches': len(rs)}
stats = {'bones': len(rig.data.bones), 'lods': rows, 'totals': totals, 'h8s_totals': before,
         'native_unity': 'NOT RUN', 'source_sha256': hashlib.sha256(source.read_bytes()).hexdigest(),
         'skinned_material_batches_per_lod': 12, 'material_names': sorted(CONTRACT)}
(out / 'Models/export-stats.json').write_text(json.dumps(stats, indent=2) + '\n')
print(json.dumps({'totals': totals, 'h8s_totals': before}), flush=True)
