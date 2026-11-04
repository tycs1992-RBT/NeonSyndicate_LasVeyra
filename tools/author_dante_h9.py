#!/usr/bin/env python3
"""Derive DanteRookH9.blend from the H8S Dante study. Reproducible: always starts from DanteRookH8S.blend.

1. Rest refit toward the procedural Dante gameplay rig (CastRoster04E look, exported to dante_gameplay_rig.json by
   the integration owner): pelvis, spine, chest, neck and head heights, shoulder height, upper arm and forearm
   lengths, palm size, hip, knee and ankle heights. The mesh follows through per-bone affine maps blended with the
   existing complete weights (stretch along the bone for spine and limbs, rigid head so the face is untouched, uniform
   hand scale, vertical compression below the ankle so the sole stays on the ground). All 65 bones keep their
   names, parents and weights.
2. Sockets re-placed on the correct side of the body (H8S had them rotated 180 degrees about the vertical axis).
3. Art: closed boots without sculpted toes, short sleeves (canon), a hanging leather apron fitted to the body with a
   neck strap, a tool belt with pouches and a hammer loop, near-black brows (H8S brows were dark red), groom shading.
Run: python tools/author_dante_h9.py -- [--root <repo>] [--output <blend>] [--rig-json <file>] [--stages ...]
"""
import argparse, hashlib, json, math, sys
from pathlib import Path
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.interpolate import poly_3d_calc

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
p.add_argument('--output', type=Path)
p.add_argument('--report', type=Path, help='also write the stage report JSON here')
p.add_argument('--rig-json', type=Path, default=Path('/home/claude/work/dante_gameplay_rig.json'))
p.add_argument('--stages', default='refit,sockets,boots,sleeves,apron,belt,groom,body')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
root = a.root.resolve()
SOURCE = root / 'ArtSource/AscensionH8S/DanteRookH8S.blend'
SOURCE_SHA = 'f763a694c36fd6d88eb099f69d5d97a88261908fef97b4f983c3c31ab367013a'
OUTPUT = (a.output or root / 'ArtSource/AscensionH9/DanteRookH9.blend').resolve()
STAGES = set(s.strip() for s in a.stages.split(',') if s.strip())
if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA:
    raise SystemExit('H8S Dante fingerprint changed; refusing to derive H9 from an unverified file.')
bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False, use_scripts=False)
rig = bpy.data.objects['DanteRig']
assert len(rig.data.bones) == 65
REPORT = {'source': str(SOURCE.relative_to(root)), 'source_sha256': SOURCE_SHA, 'stages': sorted(STAGES)}
MESHES = [o for o in bpy.data.objects if o.type == 'MESH']


def rest_pose():
    if rig.animation_data:
        rig.animation_data.action = None
    for b in rig.pose.bones:
        b.rotation_mode = 'QUATERNION'; b.rotation_quaternion = (1, 0, 0, 0); b.location = (0, 0, 0); b.scale = (1, 1, 1)
    bpy.context.view_layer.update()


rest_pose()
OLD = {b.name: (b.head_local.copy(), b.tail_local.copy()) for b in rig.data.bones}


# ----------------------------------------------------------------------------------------------------------------
# 1. Rest refit toward the gameplay rig.
# ----------------------------------------------------------------------------------------------------------------
def gameplay_targets():
    """Gameplay joint positions in Blender axes: Blender = (-x_unity, -z_unity, y_unity)."""
    d = json.loads(a.rig_json.read_text())
    out = {}
    for b in d['bones']:
        out[b['name']] = Vector((-b['x'], -b['z'], b['y']))
    return out


def stretch_map(h0, t0, h1, t1):
    """Affine map taking segment h0-t0 to h1-t1: stretch along the bone only, then rotate and translate."""
    u0 = (t0 - h0).normalized()
    u1 = (t1 - h1).normalized()
    s = (t1 - h1).length / max(1e-9, (t0 - h0).length)
    S = Matrix.Identity(3) + (s - 1.0) * Matrix(((u0.x * u0.x, u0.x * u0.y, u0.x * u0.z),
                                                  (u0.y * u0.x, u0.y * u0.y, u0.y * u0.z),
                                                  (u0.z * u0.x, u0.z * u0.y, u0.z * u0.z)))
    Rm = u0.rotation_difference(u1).to_matrix()
    M = Rm @ S
    return M, h1 - M @ h0


def similarity_map(c0, c1, k):
    M = Matrix.Identity(3) * k
    return M, c1 - M @ c0


def plan_refit():
    G = gameplay_targets()
    new = {}
    maps = {}
    O = OLD
    # spine chain: heights from the gameplay rig, front/back offsets kept
    chain = [('pelvis', 'Hips'), ('spine_01', 'Spine'), ('spine_03', 'Chest'), ('neck_01', 'Neck'), ('Head', 'Head')]
    heads = {}
    for bone, gname in chain:
        h = O[bone][0].copy(); h.z = G[gname].z; heads[bone] = h
    f = (O['spine_02'][0].z - O['spine_01'][0].z) / (O['spine_03'][0].z - O['spine_01'][0].z)
    h2 = O['spine_02'][0].copy(); h2.z = heads['spine_01'].z + f * (heads['spine_03'].z - heads['spine_01'].z); heads['spine_02'] = h2
    new['pelvis'] = (heads['pelvis'], O['pelvis'][1] + (heads['pelvis'] - O['pelvis'][0]))
    new['spine_01'] = (heads['spine_01'], heads['spine_02'])
    new['spine_02'] = (heads['spine_02'], heads['spine_03'])
    gap = O['neck_01'][0].z - O['spine_03'][1].z
    t3 = O['spine_03'][1].copy(); t3.z = heads['neck_01'].z - gap
    new['spine_03'] = (heads['spine_03'], t3)
    new['neck_01'] = (heads['neck_01'], heads['Head'])
    new['Head'] = (heads['Head'], heads['Head'] + (O['Head'][1] - O['Head'][0]))
    new['root'] = O['root']
    for side, sg in (('l', 'L'), ('r', 'R')):
        sh = O['upperarm_' + side][0].copy(); sh.z = G['UpperArm.' + sg].z; sh.x = G['UpperArm.' + sg].x
        d_sh = sh - O['upperarm_' + side][0]
        new['clavicle_' + side] = (O['clavicle_' + side][0] + d_sh, O['clavicle_' + side][1] + d_sh)
        L_up = (G['UpperArm.' + sg] - G['Forearm.' + sg]).length
        L_lo = (G['Forearm.' + sg] - G['Hand.' + sg]).length
        u_up = (O['lowerarm_' + side][0] - O['upperarm_' + side][0]).normalized()
        u_lo = (O['hand_' + side][0] - O['lowerarm_' + side][0]).normalized()
        el = sh + u_up * L_up
        wr = el + u_lo * L_lo
        new['upperarm_' + side] = (sh, el)
        new['lowerarm_' + side] = (el, wr)
        palm_old = (O['middle_01_' + side][0] - O['hand_' + side][0]).length
        palm_new = (G['Middle.1.' + sg] - G['Hand.' + sg]).length
        k = palm_new / palm_old
        maps['hand_k_' + side] = k
        for b in rig.data.bones:
            if b.name == 'hand_' + side or b.parent_recursive and any(pb.name == 'hand_' + side for pb in b.parent_recursive):
                h0, t0 = O[b.name]
                new[b.name] = (wr + (h0 - O['hand_' + side][0]) * k, wr + (t0 - O['hand_' + side][0]) * k)
        # legs: hip, knee and ankle heights; the ankle height also sets the foot compression
        hp = O['thigh_' + side][0].copy(); hp.z = G['Thigh.' + sg].z; hp.x = G['Thigh.' + sg].x
        kn = hp + (O['calf_' + side][0] - O['thigh_' + side][0]).normalized() * (G['Thigh.' + sg] - G['Shin.' + sg]).length
        dc = (O['foot_' + side][0] - O['calf_' + side][0]).normalized()
        an_z = G['Foot.' + sg].z
        an = kn + dc * ((kn.z - an_z) / max(1e-6, -dc.z))
        new['thigh_' + side] = (hp, kn)
        new['calf_' + side] = (kn, an)
        sf = an.z / O['foot_' + side][0].z  # keep the sole on the ground: compress below the ankle
        maps['foot_sz_' + side] = sf
        for name in ('foot_' + side, 'ball_' + side, 'ball_leaf_' + side):
            h0, t0 = O[name]
            def fmap(p0, a0=O['foot_' + side][0], a1=an, s=sf):
                q = p0 - a0
                return Vector((a1.x + q.x, a1.y + q.y, a1.z + q.z * s))
            new[name] = (fmap(h0), fmap(t0))
    missing = [b.name for b in rig.data.bones if b.name not in new]
    if missing:
        raise RuntimeError('Refit plan misses bones: %s' % missing)
    # Per-bone affine maps for the mesh.
    for b in rig.data.bones:
        n = b.name
        h0, t0 = O[n]
        h1, t1 = new[n]
        if n == 'Head' or n == 'root' or n == 'pelvis' or n.startswith('clavicle_'):
            maps[n] = (Matrix.Identity(3), h1 - h0)  # rigid: the face and pelvis keep their shape
        elif n.startswith(('foot_', 'ball_')):
            side = n[-1]
            sf = maps['foot_sz_' + side]
            M = Matrix(((1, 0, 0), (0, 1, 0), (0, 0, sf)))
            a0 = O['foot_' + side][0]; a1 = new['foot_' + side][0]
            maps[n] = (M, a1 - M @ a0)
        elif n == 'hand_l' or n == 'hand_r' or any(n.startswith(f) for f in ('index_', 'middle_', 'ring_', 'pinky_', 'thumb_')):
            side = n[-1]
            maps[n] = similarity_map(O['hand_' + side][0], new['hand_' + side][0], maps['hand_k_' + side])
        else:
            maps[n] = stretch_map(h0, t0, h1, t1)
    return new, maps


def apply_refit():
    new, maps = plan_refit()
    for o in MESHES:
        names = {g.index: g.name for g in o.vertex_groups}
        for v in o.data.vertices:
            gs = [(names[g.group], g.weight) for g in v.groups if g.weight > 0 and names[g.group] in maps]
            tot = sum(w for _, w in gs)
            if tot <= 0:
                continue
            acc = Vector()
            for n, w in gs:
                M, t = maps[n]
                acc += (M @ v.co + t) * (w / tot)
            v.co = acc
        o.data.update()
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    eb = rig.data.edit_bones
    rolls = {b.name: b.roll for b in eb}
    for b in eb:
        h1, t1 = new[b.name]
        b.head = h1
        b.tail = t1
        b.roll = rolls[b.name]
    bpy.ops.object.mode_set(mode='OBJECT')
    rest_pose()
    REPORT['refit'] = {n: {'head': [round(c, 4) for c in rig.data.bones[n].head_local], 'old_head': [round(c, 4) for c in OLD[n][0]]}
                       for n in ('pelvis', 'spine_01', 'spine_03', 'neck_01', 'Head', 'upperarm_l', 'lowerarm_l', 'hand_l',
                                 'thigh_l', 'calf_l', 'foot_l', 'ball_l')}
    REPORT['refit_scales'] = {k: round(v, 4) for k, v in maps.items() if isinstance(v, float)}


# ----------------------------------------------------------------------------------------------------------------
# 2. Sockets on the correct side (H8S placed them at each bone head rotated 180 degrees about the vertical axis).
# ----------------------------------------------------------------------------------------------------------------
def place_sockets():
    bpy.context.view_layer.update()
    bones = rig.data.bones
    def head(n):
        return rig.matrix_world @ bones[n].head_local
    hr = head('hand_r')
    palm_r = hr + (rig.matrix_world.to_3x3() @ (bones['hand_r'].tail_local - bones['hand_r'].head_local)) * 0.5
    front = Vector((0, -1, 0))
    places = {'socket_hand_r': hr, 'socket_hand_l': head('hand_l'),
              'socket_foregrip': hr + front * 0.25, 'socket_muzzle': hr + front * 0.55,
              'socket_blade_hilt': palm_r, 'socket_blade_tip': palm_r,
              'socket_head': head('Head'), 'socket_chest': head('spine_03'),
              'socket_fx': head('spine_03'), 'socket_synergy': head('spine_03')}
    rows = {}
    for name, pos in places.items():
        o = bpy.data.objects[name]
        o.matrix_world = Matrix.Translation(pos)
        rows[name] = {'parent_bone': o.parent_bone, 'world': [round(c, 4) for c in pos]}
    rf = bpy.data.objects['review_forward']
    rf.matrix_world = Matrix.Translation(Vector((0, -0.4, head('Head').z)))
    REPORT['sockets'] = rows


# ----------------------------------------------------------------------------------------------------------------
# 3. Art stages (run on the refit bind pose).
# ----------------------------------------------------------------------------------------------------------------
def weight_rows(obj):
    names = {g.index: g.name for g in obj.vertex_groups}
    return [{names[g.group]: g.weight for g in v.groups if g.weight > 0} for v in obj.data.vertices]


def normalized(row, cap=8):
    items = sorted(((k, v) for k, v in row.items() if v > 1e-4), key=lambda kv: -kv[1])[:cap]
    tot = sum(v for k, v in items)
    return [(k, v / tot) for k, v in items]


def set_weights(obj, rows):
    for g in list(obj.vertex_groups):
        obj.vertex_groups.remove(g)
    groups = {}
    for vi, row in enumerate(rows):
        for name, w in row:
            if name not in groups:
                groups[name] = obj.vertex_groups.new(name=name)
            groups[name].add([vi], w, 'REPLACE')


class SurfaceWeights:
    """Nearest point on skinned meshes: position, smooth normal and barycentric blend of the weights."""
    def __init__(self, objs):
        self.v, self.n, self.polys, self.w = [], [], [], []
        for o in objs:
            base = len(self.v)
            self.v += [x.co.copy() for x in o.data.vertices]
            self.n += [x.normal.copy() for x in o.data.vertices]
            self.w += weight_rows(o)
            self.polys += [[base + i for i in pl.vertices] for pl in o.data.polygons]
        self.tree = BVHTree.FromPolygons(self.v, self.polys)

    def query(self, pt):
        loc, fn, idx, d = self.tree.find_nearest(pt)
        ids = self.polys[idx]
        ws = poly_3d_calc([self.v[i] for i in ids], loc)
        n = Vector(); row = {}
        for wi, vi in zip(ws, ids):
            n += self.n[vi] * wi
            for k, val in self.w[vi].items():
                row[k] = row.get(k, 0.0) + val * wi
        return loc, (n.normalized() if n.length > 1e-6 else fn), row, d


def new_skinned(name, verts, faces, uvs, mat, rows):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    uv = me.uv_layers.new(name='UVMap')
    for pl in me.polygons:
        pl.use_smooth = True
        for li in pl.loop_indices:
            uv.data[li].uv = uvs[me.loops[li].vertex_index]
    me.validate(clean_customdata=False)
    me.update()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    me.materials.append(mat)
    obj.parent = rig
    set_weights(obj, rows)
    m = obj.modifiers.new('Original skin owner', 'ARMATURE')
    m.object = rig
    obj['candidate_source'] = 'h9-dante-fitted'
    return obj


def body():
    return bpy.data.objects['CourtDefender Human_Male']


def stage_boots():
    """Replace sculpted toes with a closed toe box, keep a flat sole, enclose the foot."""
    boots = bpy.data.objects['Closed work boots']
    ank = {s: rig.data.bones['foot_' + s].head_local.copy() for s in ('l', 'r')}
    bm = bmesh.new(); bm.from_mesh(boots.data); bm.verts.ensure_lookup_table()
    def in_box(v):
        a0 = ank['l' if v.co.x > 0 else 'r']
        return v.co.z < a0.z + 0.012 and v.co.y < a0.y - 0.035
    region = [v for v in bm.verts if in_box(v)]
    sole = [v for v in region if v.co.z < 0.006]
    upper = [v for v in region if v.co.z >= 0.006]
    for it in range(30):
        bmesh.ops.smooth_vert(bm, verts=upper, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        bmesh.ops.smooth_vert(bm, verts=sole, factor=0.5, use_axis_x=True, use_axis_y=True, use_axis_z=False)
    bm.normal_update()
    sw = SurfaceWeights([body()])
    pushed = 0
    for v in upper:
        loc, n, w, d = sw.query(v.co)
        depth = (v.co - loc).dot(n)
        if depth < 0.004:
            v.co = loc + n * 0.004 + Vector((0, 0, 0))
            pushed += 1
    for v in sole:
        v.co.z = min(v.co.z, 0.0)
    bm.to_mesh(boots.data); bm.free(); boots.data.update()
    REPORT['boots'] = {'toe_box_vertices': len(region), 'pushed_outside_foot': pushed}


def stage_sleeves():
    """Canon short sleeves: the shirt ends a little above mid upper arm; the cut edge gets a turned hem."""
    shirt = bpy.data.objects['Fitted work shirt']
    bm = bmesh.new(); bm.from_mesh(shirt.data); bm.verts.ensure_lookup_table()
    doomed = []
    for side, sgn in (('l', 1), ('r', -1)):
        h = rig.data.bones['upperarm_' + side].head_local
        t = rig.data.bones['upperarm_' + side].tail_local
        ax = (t - h)
        L = ax.length
        ax.normalize()
        for v in bm.verts:
            if v.co.x * sgn <= 0.1:
                continue
            if (v.co - h).dot(ax) > 0.52 * L:
                doomed.append(v)
    before = len(bm.verts)
    bmesh.ops.delete(bm, geom=list(set(doomed)), context='VERTS')
    edge = [e for e in bm.edges if e.is_boundary and abs(sum(v.co.x for v in e.verts) / 2) > 0.2]
    ret = bmesh.ops.extrude_edge_only(bm, edges=edge)
    newv = [x for x in ret['geom'] if isinstance(x, bmesh.types.BMVert)]
    for v in newv:
        side = 'l' if v.co.x > 0 else 'r'
        h = rig.data.bones['upperarm_' + side].head_local
        t = rig.data.bones['upperarm_' + side].tail_local
        ax = (t - h).normalized()
        c = h + ax * (v.co - h).dot(ax)
        out = (v.co - c); out -= ax * out.dot(ax)
        v.co = v.co - out.normalized() * 0.003 - ax * 0.006
    bm.to_mesh(shirt.data); bm.free(); shirt.data.update()
    # new hem vertices copy the weights of their source edge loop
    rows = weight_rows(shirt)
    for i, r in enumerate(rows):
        if not r:
            rows[i] = None
    tree_src = [(v.co.copy(), rows[v.index]) for v in shirt.data.vertices if rows[v.index]]
    from mathutils.kdtree import KDTree
    kd = KDTree(len(tree_src))
    for k, (c, r) in enumerate(tree_src):
        kd.insert(c, k)
    kd.balance()
    final = []
    for v in shirt.data.vertices:
        r = rows[v.index]
        if r is None:
            r = tree_src[kd.find(v.co)[1]][1]
        final.append(normalized(r))
    set_weights(shirt, final)
    REPORT['sleeves'] = {'shirt_vertices_before': before, 'shirt_vertices_after': len(shirt.data.vertices)}


def front_surface(trees, x, z, ymax=0.4):
    best = None
    for t in trees:
        loc, n, i, d = t.ray_cast(Vector((x, -1.0, z)), Vector((0, 1, 0)), 2.0)
        if loc is not None and (best is None or loc.y < best):
            best = loc.y
    return best


def stage_apron():
    """A hanging leather forge apron: bib on the chest, drape from the most forward point above, neck strap."""
    old = bpy.data.objects.get('Fitted forge apron')
    mat = old.data.materials[0] if old else bpy.data.materials['Dante worn forge apron']
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    under = [bpy.data.objects[n] for n in ('Fitted work shirt', 'Fitted work trousers', 'CourtDefender Human_Male')]
    trees = [BVHTree.FromPolygons([v.co for v in o.data.vertices], [list(pl.vertices) for pl in o.data.polygons]) for o in under]
    sw = SurfaceWeights(under)
    z_top, z_waist, z_bot = 1.405, 1.06, 0.585
    NX, NZ = 13, 22
    zs = [z_top - (z_top - z_bot) * j / (NZ - 1) for j in range(NZ)]
    def half_width(z):
        if z >= 1.18:
            return 0.115 + (0.15 - 0.115) * (z_top - z) / (z_top - 1.18)
        if z >= z_waist:
            return 0.15 + (0.215 - 0.15) * (1.18 - z) / (1.18 - z_waist)
        return 0.215 + 0.02 * (z_waist - z) / (z_waist - z_bot)
    grid = []
    for j, z in enumerate(zs):
        row = []
        hw = half_width(z)
        for i in range(NX):
            u = i / (NX - 1)
            x = -hw + 2 * hw * u
            ys = []
            for k in range(0, 9):
                zz = min(z_top, z + 0.03 * k)
                y = front_surface(trees, x, zz)
                if y is not None:
                    ys.append(y)
            y = min(ys) if ys else -0.15
            row.append(Vector((x, y - 0.012, z)))
        grid.append(row)
    # wrap the side edges slightly back around the body and smooth the drape
    for it in range(6):
        for j in range(1, NZ - 1):
            for i in range(1, NX - 1):
                q = grid[j][i]
                avg = (grid[j - 1][i].y + grid[j + 1][i].y + grid[j][i - 1].y + grid[j][i + 1].y) / 4
                q.y = min(q.y, 0.5 * q.y + 0.5 * avg)
    for j in range(NZ):
        for i in (0, NX - 1):
            grid[j][i].y += 0.018
        for i in (1, NX - 2):
            grid[j][i].y += 0.006
    verts, faces, uvs, rows = [], [], [], []
    def add(pt, uv):
        verts.append(tuple(pt)); uvs.append(uv)
        loc, n, w, d = sw.query(pt)
        if pt.z < z_waist:
            f = 0.45
            w = {k: v * (1 - f) for k, v in w.items()}
            w['pelvis'] = w.get('pelvis', 0.0) + f
        rows.append(normalized(w))
        return len(verts) - 1
    front = [[add(grid[j][i], (i / (NX - 1), 1 - j / (NZ - 1))) for i in range(NX)] for j in range(NZ)]
    back = [[add(grid[j][i] + Vector((0, 0.004, 0)), (i / (NX - 1), 1 - j / (NZ - 1))) for i in range(NX)] for j in range(NZ)]
    for j in range(NZ - 1):
        for i in range(NX - 1):
            faces.append((front[j][i], front[j][i + 1], front[j + 1][i + 1], front[j + 1][i]))
            faces.append((back[j][i], back[j + 1][i], back[j + 1][i + 1], back[j][i + 1]))
    rim = [front[0][i] for i in range(NX)] + [front[j][NX - 1] for j in range(1, NZ)] + \
          [front[NZ - 1][i] for i in range(NX - 2, -1, -1)] + [front[j][0] for j in range(NZ - 2, 0, -1)]
    rimb = [back[0][i] for i in range(NX)] + [back[j][NX - 1] for j in range(1, NZ)] + \
           [back[NZ - 1][i] for i in range(NX - 2, -1, -1)] + [back[j][0] for j in range(NZ - 2, 0, -1)]
    for k in range(len(rim)):
        a0, b0 = rim[k], rim[(k + 1) % len(rim)]
        c0, d0 = rimb[(k + 1) % len(rim)], rimb[k]
        faces.append((a0, d0, c0, b0))
    apron = new_skinned('Fitted forge apron', verts, faces, uvs, mat, rows)
    # neck strap: from the bib corners up and around the back of the neck
    neck = rig.data.bones['neck_01'].head_local
    strap_v, strap_f, strap_uv, strap_w = [], [], [], []
    pts = []
    for k in range(25):
        t = k / 24.0
        ang = math.pi * (0.5 + t)
        rr = 0.075
        cx = math.cos(ang) * 0.072
        cy = neck.y + math.sin(ang) * rr * (1.0 if math.sin(ang) > 0 else 0.0) + 0.01
        pts.append(Vector((cx, cy, neck.z - 0.035 + 0.01 * math.sin(math.pi * t))))
    left = Vector((grid[0][1].x, grid[0][1].y + 0.002, z_top - 0.005))
    right = Vector((grid[0][NX - 2].x, grid[0][NX - 2].y + 0.002, z_top - 0.005))
    path = [right] + [right.lerp(pts[0], f) for f in (0.33, 0.66)] + pts + [pts[-1].lerp(left, f) for f in (0.33, 0.66)] + [left]
    for k, pt in enumerate(path):
        loc, n, w, d = sw.query(pt)
        q = loc + n * 0.004
        t = (path[min(k + 1, len(path) - 1)] - path[max(k - 1, 0)]).normalized()
        side = n.cross(t).normalized()
        for s in (-1, 1):
            strap_v.append(tuple(q + side * 0.011 * s)); strap_uv.append((k / len(path), 0.5 + 0.5 * s)); strap_w.append(normalized(w))
    for k in range(len(path) - 1):
        a0 = 2 * k
        strap_f.append((a0, a0 + 2, a0 + 3, a0 + 1))
    strap = new_skinned('Forge apron neck strap', strap_v, strap_f, strap_uv, mat, strap_w)
    REPORT['apron'] = {'apron_vertices': len(verts), 'strap_vertices': len(strap_v)}


def box(center, axes, size):
    """Corners of an oriented box (axes: right, up, out) for pouch geometry."""
    r, u, o = axes
    sx, sy, sz = size
    pts = []
    for dz in (-1, 1):
        for dy in (-1, 1):
            for dx in (-1, 1):
                pts.append(center + r * dx * sx / 2 + u * dy * sy / 2 + o * (dz * sz / 2 + sz / 2))
    faces = [(0, 2, 3, 1), (4, 5, 7, 6), (0, 1, 5, 4), (2, 6, 7, 3), (0, 4, 6, 2), (1, 3, 7, 5)]
    return pts, faces


def stage_belt():
    """Tool belt over shirt, trousers and apron at the waist, with two pouches and a hammer loop on the right hip."""
    leather = bpy.data.materials['H6 Worn leather']
    under = [bpy.data.objects[n] for n in ('Fitted forge apron', 'Fitted work shirt', 'Fitted work trousers', 'CourtDefender Human_Male')]
    trees = [BVHTree.FromPolygons([v.co for v in o.data.vertices], [list(pl.vertices) for pl in o.data.polygons]) for o in under]
    sw = SurfaceWeights([bpy.data.objects['Fitted work trousers'], bpy.data.objects['CourtDefender Human_Male']])
    zc = rig.data.bones['pelvis'].head_local.z + 0.045
    centre = Vector((0.0, 0.02, zc))
    N = 48
    verts, faces, uvs, rows = [], [], [], []
    ring_out = []
    for k in range(N):
        ang = 2 * math.pi * k / N
        d = Vector((math.sin(ang), -math.cos(ang), 0.0))
        col = []
        for dz in (-0.022, 0.022):
            c = centre + Vector((0, 0, dz))
            r_far = None
            for t in trees:
                loc, n, i, dist = t.ray_cast(c + d * 0.4, -d, 0.4)
                if loc is not None:
                    rr = (loc - c).length
                    r_far = rr if r_far is None else max(r_far, rr)
            r_far = (r_far or 0.16) + 0.004
            for thick in (0.0, 0.006):
                pt = c + d * (r_far + thick)
                verts.append(tuple(pt)); uvs.append((k / N * 4, 0.5 + dz * 10 + thick * 20))
                loc, n, w, dd = sw.query(pt)
                w = {kk: v * 0.4 for kk, v in w.items()}; w['pelvis'] = w.get('pelvis', 0) + 0.6
                rows.append(normalized(w))
                col.append(len(verts) - 1)
        ring_out.append(col)  # [bottom inner, bottom outer, top inner, top outer]
    for k in range(N):
        a, b = ring_out[k], ring_out[(k + 1) % N]
        faces.append((a[1], b[1], b[3], a[3]))  # outer face
        faces.append((a[0], a[2], b[2], b[0]))  # inner face
        faces.append((a[2], a[3], b[3], b[2]))  # top edge
        faces.append((a[0], b[0], b[1], a[1]))  # bottom edge
    # pouches and hammer loop
    for side_ang, size in ((0.62, (0.075, 0.09, 0.035)), (-0.62, (0.065, 0.08, 0.03))):
        k = int(round(side_ang / (2 * math.pi) * N)) % N
        p_out = Vector(verts[ring_out[k][3]])
        d = Vector((math.sin(2 * math.pi * k / N), -math.cos(2 * math.pi * k / N), 0.0))
        right = Vector((0, 0, 1)).cross(d).normalized()
        c = p_out + Vector((0, 0, -0.06))
        pts, fs = box(c, (right, Vector((0, 0, 1)), d), size)
        base = len(verts)
        for pt in pts:
            verts.append(tuple(pt)); uvs.append((0.5, 0.5))
            loc, n, w, dd = sw.query(pt)
            w = {kk: v * 0.4 for kk, v in w.items()}; w['pelvis'] = w.get('pelvis', 0) + 0.6
            rows.append(normalized(w))
        faces += [tuple(base + i for i in f) for f in fs]
    k = int(round(-1.25 / (2 * math.pi) * N)) % N
    d = Vector((math.sin(2 * math.pi * k / N), -math.cos(2 * math.pi * k / N), 0.0))
    hub = Vector(verts[ring_out[k][3]]) + d * 0.012 + Vector((0, 0, -0.03))
    right = Vector((0, 0, 1)).cross(d).normalized()
    base = len(verts)
    M = 12
    for m in range(M):
        ang = 2 * math.pi * m / M
        for rr in (0.016, 0.022):
            pt = hub + right * math.cos(ang) * rr + d * math.sin(ang) * rr
            verts.append(tuple(pt)); uvs.append((m / M, rr * 20))
            loc, n, w, dd = sw.query(pt)
            w = {kk: v * 0.4 for kk, v in w.items()}; w['pelvis'] = w.get('pelvis', 0) + 0.6
            rows.append(normalized(w))
    for m in range(M):
        a0, a1 = base + 2 * m, base + 2 * m + 1
        b0, b1 = base + 2 * ((m + 1) % M), base + 2 * ((m + 1) % M) + 1
        faces.append((a0, b0, b1, a1))
        faces.append((a1, b1, b0, a0))
    belt = new_skinned('Forge tool belt', verts, faces, uvs, leather, rows)
    REPORT['belt'] = {'vertices': len(verts)}


def flow_unwrap(obj, yc, u_repeat=3.0, v_period=0.2):
    """Cylindrical flow unwrap around the head axis: u = azimuth (tiled, seam at the back made continuous), v = height,
    so the seamless strand texture runs down the hanging hair and the beard."""
    me = obj.data
    uv = me.uv_layers.active.data
    for pl in me.polygons:
        us = []
        for li in pl.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            az = math.atan2(co.x, -(co.y - yc))
            us.append(az / (2 * math.pi) * u_repeat)
        if max(us) - min(us) > u_repeat * 0.5:  # face straddles the back seam: keep it on one side
            us = [u + u_repeat if u < 0 else u for u in us]
        for li, u in zip(pl.loop_indices, us):
            co = me.vertices[me.loops[li].vertex_index].co
            uv[li].uv = (u, co.z / v_period)


def stage_groom():
    """Brows were dark red in H8S; canon hair palette 0 is near-black. Hair and beard get a tiled strand texture."""
    brow = bpy.data.materials['MI_Hair_1']
    bs = next(nd for nd in brow.node_tree.nodes if nd.bl_idname == 'ShaderNodeBsdfPrincipled')
    bs.inputs['Base Color'].default_value = (0.016, 0.012, 0.010, 1.0)
    hair = bpy.data.materials['Dante dark iron hair']
    nt = hair.node_tree
    hs = next(nd for nd in nt.nodes if nd.bl_idname == 'ShaderNodeBsdfPrincipled')
    npc = root / 'Assets/NeonSyndicate/Art/ProductionH9/NPC'
    alb = nt.nodes.new('ShaderNodeTexImage')
    alb.image = bpy.data.images.load(str(npc / 'DanteHair_Albedo.png'), check_existing=True)
    nrm = nt.nodes.new('ShaderNodeTexImage')
    nrm.image = bpy.data.images.load(str(npc / 'DanteHair_Normal.png'), check_existing=True)
    nrm.image.colorspace_settings.name = 'Non-Color'
    nm = nt.nodes.new('ShaderNodeNormalMap'); nm.inputs['Strength'].default_value = 0.7
    nt.links.new(alb.outputs['Color'], hs.inputs['Base Color'])
    nt.links.new(nrm.outputs['Color'], nm.inputs['Color'])
    nt.links.new(nm.outputs['Normal'], hs.inputs['Normal'])
    hs.inputs['Roughness'].default_value = 0.5
    hs.inputs['Specular IOR Level'].default_value = 0.4
    yc = rig.data.bones['Head'].head_local.y
    for name in ('Hair_Long', 'Hair_Beard'):
        flow_unwrap(bpy.data.objects[name], yc)
    REPORT['groom'] = {'brow_colour': 'near-black (canon hair palette 0)',
                       'hair_and_beard': 'cylindrical flow unwrap with seamless strand textures DanteHair_Albedo.png and DanteHair_Normal.png'}


def stage_hidden_body(min_depth=0.0012, min_edge=0.035):
    """Skin fully covered by shirt, trousers and boots (well away from openings) is removed, as on Malik. The bare
    skin foot inside the closed boot is always removed, so sculpted toes can never show through the toe box."""
    from mathutils.kdtree import KDTree
    b = body()
    cover = [bpy.data.objects[n] for n in ('Fitted work shirt', 'Fitted work trousers', 'Closed work boots')]
    shells = []
    for o in cover:
        verts = [v.co.copy() for v in o.data.vertices]
        polys = [list(pl.vertices) for pl in o.data.polygons]
        norms = [pl.normal.copy() for pl in o.data.polygons]
        bm = bmesh.new(); bm.from_mesh(o.data)
        edge = [v.co.copy() for e in bm.edges if e.is_boundary for v in e.verts]
        bm.free()
        kd = KDTree(max(1, len(edge)))
        for k, c in enumerate(edge):
            kd.insert(c, k)
        kd.balance()
        shells.append((BVHTree.FromPolygons(verts, polys), norms, kd, len(edge)))
    names = {g.index: g.name for g in b.vertex_groups}
    keep = ('Head', 'neck_01', 'hand_', 'thumb_', 'index_', 'middle_', 'ring_', 'pinky_', 'lowerarm_')
    hidden = [False] * len(b.data.vertices)
    for v in b.data.vertices:
        gs = [g for g in v.groups if g.weight > 0]
        dom = names[max(gs, key=lambda g: g.weight).group] if gs else ''
        if dom.startswith(('foot_', 'ball_')) and v.co.z < 0.07:
            hidden[v.index] = True
            continue
        if dom.startswith(keep):
            continue
        for tree, norms, kd, ne in shells:
            loc, n, idx, d = tree.find_nearest(v.co)
            if loc is None or d > 0.09:
                continue
            if -(v.co - loc).dot(norms[idx]) >= min_depth and (ne == 0 or kd.find(loc)[2] >= min_edge):
                hidden[v.index] = True
                break
    bm = bmesh.new(); bm.from_mesh(b.data)
    doomed = [f for f in bm.faces if all(hidden[v.index] for v in f.verts)]
    before = len(bm.verts)
    bmesh.ops.delete(bm, geom=doomed, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    bm.to_mesh(b.data); bm.free(); b.data.update()
    REPORT['hidden_body'] = {'vertices_before': before, 'vertices_after': len(b.data.vertices)}


if 'refit' in STAGES:
    apply_refit()
if 'sockets' in STAGES:
    place_sockets()
if 'boots' in STAGES:
    stage_boots()
if 'sleeves' in STAGES:
    stage_sleeves()
if 'apron' in STAGES:
    stage_apron()
if 'belt' in STAGES:
    stage_belt()
if 'groom' in STAGES:
    stage_groom()
if 'body' in STAGES:
    stage_hidden_body()

def stage_textures():
    """Dante-specific maps live in ProductionH9/NPC, the shared cloth and leather maps in ProductionH9/Textures."""
    npc = root / 'Assets/NeonSyndicate/Art/ProductionH9/NPC'
    shared = root / 'Assets/NeonSyndicate/Art/ProductionH9/Textures'
    moved = {}
    for im in bpy.data.images:
        if im.source != 'FILE' or not im.filepath:
            continue
        name = Path(bpy.path.abspath(im.filepath)).name
        folder = npc if (npc / name).exists() or name.startswith(('Dante', 'T_')) else shared
        im.filepath = str(folder / name)
        moved[name] = str((folder / name).relative_to(root))
    REPORT['textures'] = moved


def drop_unused_slots():
    """Eyebrow and eye meshes carried a second, unused material slot; one skin per material needs clean slots."""
    dropped = {}
    for o in [o for o in bpy.data.objects if o.type == 'MESH']:
        me = o.data
        used = sorted({pl.material_index for pl in me.polygons})
        if len(used) == len(me.materials):
            continue
        keep = [me.materials[i] for i in used]
        remap = {old: new for new, old in enumerate(used)}
        idx = [remap[pl.material_index] for pl in me.polygons]
        me.materials.clear()
        for m in keep:
            me.materials.append(m)
        for pl, i in zip(me.polygons, idx):
            pl.material_index = i
        dropped[o.name] = [m.name for m in keep]
    REPORT['material_slots'] = dropped


drop_unused_slots()
stage_textures()
for i in range(3):
    bpy.ops.outliner.orphans_purge(do_recursive=True)
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT), compress=True, relative_remap=True)
print('D9REPORT ' + json.dumps(REPORT), flush=True)
if a.report:
    a.report.write_text(json.dumps(REPORT, indent=1) + '\n')
