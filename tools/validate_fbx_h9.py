#!/usr/bin/env python3
"""Re-import exported H9 FBX files into an empty Blender scene and check the skinning contract (source-only proof).

Checks per file: one armature with the expected bone names, parents and rest heads (compared with the source rig),
every gameplay semantic bone present (47 for Malik through the RigLayout to candidate-bone mapping), the socket
empties and review_forward, every mesh vertex weighted to 1 to 8 armature bones with weights summing to 1, the
material name per skin and the skin count. Results are printed and optionally merged into a stats JSON.
Usage: python tools/validate_fbx_h9.py -- --source <blend> --rig <name> --fbx <a.fbx> [<b.fbx> ...]
       [--stats <export-stats.json>] [--materials <comma list>] [--semantic] [--skins N]
"""
import argparse, json, sys
from pathlib import Path
import bpy

p = argparse.ArgumentParser()
p.add_argument('--source', required=True)
p.add_argument('--rig', required=True)
p.add_argument('--fbx', nargs='+', required=True)
p.add_argument('--stats')
p.add_argument('--materials', default='')
p.add_argument('--semantic', action='store_true')
p.add_argument('--skins', type=int, default=0, help='exact skinned mesh count per file (0 = do not check)')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
SEMANTIC = ['pelvis', 'spine_01', 'spine_03', 'neck_01', 'Head', 'upperarm_l', 'lowerarm_l', 'hand_l', 'upperarm_r',
            'lowerarm_r', 'hand_r', 'thigh_l', 'calf_l', 'foot_l', 'thigh_r', 'calf_r', 'foot_r']
for side in ('l', 'r'):
    for finger in ('index', 'middle', 'ring', 'pinky', 'thumb'):
        for j in (1, 2, 3):
            SEMANTIC.append('%s_0%d_%s' % (finger, j, side))
SOCKETS = ['socket_' + r for r in ('hand_r', 'hand_l', 'foregrip', 'muzzle', 'blade_hilt', 'blade_tip', 'head', 'chest', 'fx', 'synergy')]

bpy.ops.wm.open_mainfile(filepath=a.source, load_ui=False, use_scripts=False)
src = bpy.data.objects[a.rig]
ref = {b.name: (b.parent.name if b.parent else None, src.matrix_world @ b.head_local) for b in src.data.bones}
contract = set(x for x in a.materials.split(',') if x)
results = []
for path in a.fbx:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path, use_anim=False, axis_forward='-Z', axis_up='Y', automatic_bone_orientation=False)
    errors = []
    arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
    if len(arms) != 1:
        errors.append('expected one armature, found %d' % len(arms))
        results.append({'file': path, 'errors': errors}); continue
    arm = arms[0]
    names = {b.name for b in arm.data.bones}
    if names != set(ref):
        errors.append('bone names differ: missing %s extra %s' % (sorted(set(ref) - names)[:6], sorted(names - set(ref))[:6]))
    worst_head = 0.0
    for b in arm.data.bones:
        if b.name not in ref:
            continue
        parent = b.parent.name if b.parent else None
        if parent != ref[b.name][0]:
            errors.append('parent of %s is %s, expected %s' % (b.name, parent, ref[b.name][0]))
        worst_head = max(worst_head, ((arm.matrix_world @ b.head_local) - ref[b.name][1]).length)
    if worst_head > 0.001:
        errors.append('rest head moved by %.4f m' % worst_head)
    if a.semantic:
        missing = [n for n in SEMANTIC if n not in names]
        if missing:
            errors.append('missing semantic bones %s' % missing)
    empties = {o.name for o in bpy.data.objects if o.type == 'EMPTY'}
    lost = [s for s in SOCKETS + ['review_forward'] if s not in empties]
    if lost:
        errors.append('missing sockets %s' % lost)
    meshes = [o for o in bpy.data.objects if o.type == 'MESH']
    skins, verts, tris, unweighted, worst_sum, max_inf, mats = 0, 0, 0, 0, 0.0, 0, []
    for o in meshes:
        me = o.data
        if not any(m.type == 'ARMATURE' and m.object == arm for m in o.modifiers):
            errors.append(o.name + ' is not skinned to the armature'); continue
        skins += 1
        verts += len(me.vertices)
        tris += sum(len(pl.vertices) - 2 for pl in me.polygons)
        gname = {g.index: g.name for g in o.vertex_groups}
        for v in me.vertices:
            ws = [g.weight for g in v.groups if g.weight > 0 and gname.get(g.group) in names]
            if not ws:
                unweighted += 1; continue
            worst_sum = max(worst_sum, abs(sum(ws) - 1.0))
            max_inf = max(max_inf, len(ws))
        for m in me.materials:
            mats.append(m.name.split('.')[0] if m else None)
    if unweighted:
        errors.append('%d vertices without bone weight' % unweighted)
    if worst_sum > 0.002:
        errors.append('weights do not sum to 1 (worst %.4f)' % worst_sum)
    if max_inf > 8:
        errors.append('more than 8 influences (%d)' % max_inf)
    if a.skins and skins != a.skins:
        errors.append('expected %d skins, found %d' % (a.skins, skins))
    if contract and set(mats) != contract:
        errors.append('materials differ from contract: %s' % sorted(set(mats) ^ contract))
    row = {'file': Path(path).name, 'bones': len(names), 'semantic_present': sum(n in names for n in SEMANTIC) if a.semantic else None,
           'sockets_present': len(SOCKETS) - len([s for s in SOCKETS if s not in empties]), 'review_forward': 'review_forward' in empties,
           'skins': skins, 'vertices': verts, 'triangles': tris, 'max_influences': max_inf,
           'worst_weight_sum_error': round(worst_sum, 6), 'unweighted_vertices': unweighted,
           'worst_rest_head_offset_m': round(worst_head, 6), 'materials': sorted(set(mats)), 'errors': errors}
    results.append(row)
    print(json.dumps(row), flush=True)
if a.stats:
    sp = Path(a.stats)
    stats = json.loads(sp.read_text()) if sp.exists() else {}
    stats['validation'] = {'method': 'Re-import of each exported FBX into an empty Blender 5.2 scene (source tool, not Unity)',
                           'native_unity': 'NOT RUN', 'files': results, 'passed': all(not r['errors'] for r in results)}
    sp.write_text(json.dumps(stats, indent=2) + '\n')
print('VALIDATION', 'PASSED' if all(not r.get('errors') for r in results) else 'FAILED', flush=True)
