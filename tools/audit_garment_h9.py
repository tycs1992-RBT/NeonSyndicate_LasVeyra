#!/usr/bin/env python3
"""Measure garment fit on the real rig across all 17 actions (source-only evidence, not a Unity result).

For each sampled frame the skinned meshes are evaluated through the existing Armature modifiers. Reported:
- coat: body vertices that sit inside the coat at rest but poke out through it in the pose (count, worst depth);
- collar: smallest clearance between collar and neck skin (negative means the collar cuts into the neck) and the
  widest gap to the neck at the collar's upper edge;
- skirt: thigh and knee vertices that pass through the walking panels.
Usage: python tools/audit_garment_h9.py -- --blend <file> --out <report.json> [--frames 5]
"""
import argparse, json, math, sys
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

p = argparse.ArgumentParser()
p.add_argument('--blend', required=True)
p.add_argument('--out', required=True)
p.add_argument('--frames', type=int, default=5)
p.add_argument('--coat', default='H7 tailored connected coat')
p.add_argument('--collar', default='H8S continuous rolled collar')
p.add_argument('--panels', default='H7 split walking panel l,H7 split walking panel r')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:])
bpy.ops.wm.open_mainfile(filepath=a.blend, load_ui=False, use_scripts=False)
scene = bpy.context.scene
rig = bpy.data.objects['MalikRig']
body = bpy.data.objects['MalikBodyH8R']
coat = bpy.data.objects.get(a.coat)
collar = bpy.data.objects.get(a.collar)
panels = [bpy.data.objects[n] for n in a.panels.split(',') if bpy.data.objects.get(n)]


def evaluated(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = ev.to_mesh()
    verts = [obj.matrix_world @ v.co for v in me.vertices]
    polys = [list(pl.vertices) for pl in me.polygons]
    normals = [(obj.matrix_world.to_3x3() @ pl.normal).normalized() for pl in me.polygons]
    ev.to_mesh_clear()
    return verts, polys, normals


def rest_pose():
    if rig.animation_data:
        rig.animation_data.action = None
    for b in rig.pose.bones:
        b.rotation_mode = 'QUATERNION'; b.rotation_quaternion = (1, 0, 0, 0); b.location = (0, 0, 0); b.scale = (1, 1, 1)
    bpy.context.view_layer.update()


HAND = ('hand_', 'thumb_', 'index_', 'middle_', 'ring_', 'pinky_')


def dominant_bone(i):
    names = {g.index: g.name for g in body.vertex_groups}
    gs = [g for g in body.data.vertices[i].groups if g.weight > 0]
    return names[max(gs, key=lambda g: g.weight).group] if gs else ''


def inside_set(shell_obj, zlo, zhi, max_d):
    """Body vertices clearly inside the garment at rest: at least 2 mm behind its surface, not a hand vertex, and
    attached at least 30 mm away from any open garment edge (cuffs, neck hole, hem, slits)."""
    import bmesh
    bv, _, _ = evaluated(body)
    sv, sp, sn = evaluated(shell_obj)
    tree = BVHTree.FromPolygons(sv, sp)
    bm = bmesh.new(); bm.from_mesh(shell_obj.data)
    edge_pts = [shell_obj.matrix_world @ v.co for e in bm.edges if e.is_boundary for v in e.verts]
    bm.free()
    from mathutils.kdtree import KDTree
    kd = KDTree(max(1, len(edge_pts)))
    for k, c in enumerate(edge_pts):
        kd.insert(c, k)
    kd.balance()
    out = []
    for i, v in enumerate(bv):
        if not (zlo < v.z < zhi) or dominant_bone(i).startswith(HAND):
            continue
        loc, n, idx, d = tree.find_nearest(v)
        if loc is None or d > max_d:
            continue
        if (v - loc).dot(sn[idx]) < -0.002 and (not edge_pts or kd.find(loc)[2] > 0.03):
            out.append(i)
    return out


ATTACH = {}


def attach(shell_obj, covered):
    """Remember, at rest, the garment surface point each covered body vertex sits behind (polygon + weights)."""
    from mathutils.interpolate import poly_3d_calc
    bv, _, _ = evaluated(body)
    sv, sp, sn = evaluated(shell_obj)
    tree = BVHTree.FromPolygons(sv, sp)
    rows = []
    for i in covered:
        loc, n, idx, d = tree.find_nearest(bv[i])
        rows.append((i, idx, poly_3d_calc([sv[k] for k in sp[idx]], loc)))
    ATTACH[shell_obj.name] = rows


def penetrations(shell_obj, covered):
    """Count covered body vertices that end up more than 1 mm outside their own attached garment point."""
    if shell_obj.name not in ATTACH:
        attach(shell_obj, covered)
    bv, _, _ = evaluated(body)
    sv, sp, sn = evaluated(shell_obj)
    count, worst = 0, 0.0
    for i, idx, ws in ATTACH[shell_obj.name]:
        pt = Vector()
        for w, k in zip(ws, sp[idx]):
            pt += sv[k] * w
        depth = (bv[i] - pt).dot(sn[idx])
        if depth > 0.001:
            count += 1
            worst = max(worst, depth)
    return count, worst


def collar_clearance():
    if collar is None:
        return None
    bv, bp, _ = evaluated(body)
    cv, cp, cn = evaluated(collar)
    neck = BVHTree.FromPolygons(bv, bp)
    mins, gaps = [], []
    top = max(v.z for v in cv)
    bottom = min(v.z for v in cv)
    for v in cv:
        if v.z < bottom + 0.022:  # the base is tucked inside the coat neckline and cannot be seen
            continue
        loc, n, idx, d = neck.find_nearest(v)
        if loc is None:
            continue
        mins.append(d if (v - loc).dot(n) >= 0 else -d)
        if v.z > top - 0.006:
            gaps.append(d)
    return round(min(mins) * 1000, 2), round(max(gaps) * 1000, 2) if gaps else None


rest_pose()
covered = inside_set(coat, 0.95, 1.58, 0.08) if coat else []
if coat:
    attach(coat, covered)
thighs = []
if panels:
    bv, _, _ = evaluated(body)
    for pnl in panels:
        thighs.append(inside_set(pnl, 0.35, 1.0, 0.10))
        attach(pnl, thighs[-1])
report = {'blend': a.blend, 'covered_coat_vertices': len(covered), 'rest': {}, 'actions': {}}
c, w = penetrations(coat, covered) if coat else (0, 0)
report['rest'] = {'coat_pokes': c, 'coat_worst_mm': round(w * 1000, 1), 'collar_min_mm_and_top_gap_mm': collar_clearance()}
for action in sorted(bpy.data.actions, key=lambda x: x.name):
    if not action.name.startswith(('H7_', 'H8_')):
        continue
    rig.animation_data_create()
    rig.animation_data.action = action
    if action.slots:
        rig.animation_data.action_slot = action.slots[0]
    f0, f1 = map(int, action.frame_range)
    rows = []
    for k in range(a.frames):
        f = round(f0 + (f1 - f0) * k / max(1, a.frames - 1))
        scene.frame_set(f)
        bpy.context.view_layer.update()
        c, w = penetrations(coat, covered) if coat else (0, 0)
        sk = [penetrations(pnl, t) for pnl, t in zip(panels, thighs)]
        rows.append({'frame': f, 'coat_pokes': c, 'coat_worst_mm': round(w * 1000, 1),
                     'skirt_pokes': sum(x[0] for x in sk), 'skirt_worst_mm': round(max([x[1] for x in sk] + [0]) * 1000, 1),
                     'collar_min_mm_and_top_gap_mm': collar_clearance()})
    report['actions'][action.name] = rows
    print(action.name, json.dumps(rows), flush=True)
json.dump(report, open(a.out, 'w'), indent=1)
