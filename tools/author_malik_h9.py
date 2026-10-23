#!/usr/bin/env python3
"""Derive MalikVeyrH9.blend from the owner-selected H8S source. Reproducible: always starts from H8S.

Visual-only art changes. The 53-bone MalikRig (names, hierarchy, rest pose), every socket empty, all 17 actions and
every existing skin influence stay untouched. New groom geometry:
- shells: continuous offset copies of the skin under the beard and the scalp, carrying copies of the skin's own
  complete weights, mapped into the head chart of the groom atlas (left half);
- strand bake: flow-aligned strands for that chart, written to an .npz for make_groom_textures_h9.py;
- top cards: layered swept-back cards for volume, with normals taken from the scalp so they shade as one mass;
- lashes and a wet tear line along the detected lid margins.
All groom pieces use the contract material 'H8R Textured groom'; the tear line uses 'H6 Cornea'.

Run: python tools/author_malik_h9.py -- [--root <repo>] [--output <blend>] [--strands <npz>] [--stages ...]
"""
import argparse, hashlib, json, math, random, sys, bisect
from pathlib import Path
import numpy as np
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.interpolate import poly_3d_calc

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
p.add_argument('--output', type=Path)
p.add_argument('--strands', type=Path)
p.add_argument('--stages', default='groom,eyes,garment')
p.add_argument('--textures', type=Path, help='folder holding the H9 textures (default ProductionH9/Textures)')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else [])
root = a.root.resolve()
SOURCE = root / 'ArtSource/AscensionH8S/MalikVeyrH8S.blend'
SOURCE_SHA = '0180aa40897a6ef631122452d444274c0767b67d2cecfd837f2fa37a6365e969'
OUTPUT = (a.output or root / 'ArtSource/AscensionH9/MalikVeyrH9.blend').resolve()
STRANDS = (a.strands or OUTPUT.with_name('groom-strands-h9.npz')).resolve()
TEX = (a.textures or root / 'Assets/NeonSyndicate/Art/ProductionH9/Textures').resolve()
STAGES = set(s.strip() for s in a.stages.split(',') if s.strip())
if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA:
    raise SystemExit('H8S source fingerprint changed; refusing to derive H9 from an unverified file.')
bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False, use_scripts=False)
rig = bpy.data.objects['MalikRig']
body = bpy.data.objects['MalikBodyH8R']
REPORT = {'source': str(SOURCE.relative_to(root)), 'source_sha256': SOURCE_SHA, 'stages': sorted(STAGES)}
sys.path.insert(0, str(root / 'tools'))
import make_groom_textures_h9 as groomtex  # atlas size and card tile table
import malik_groom_regions_h9 as R          # hairstyle regions shared with the skin painter

# ----------------------------------------------------------------------------------------------------------------
# Bind-pose skin surface with smooth normals and skin UVs (the rig is at rest, mesh coordinates are the bind pose).
# ----------------------------------------------------------------------------------------------------------------
ME = body.data
BV = [v.co.copy() for v in ME.vertices]
BN = [v.normal.copy() for v in ME.vertices]
UVL = ME.uv_layers.active.data
HEAD_POLYS = [pl for pl in ME.polygons if max(BV[i].z for i in pl.vertices) > 1.55]
HEAD_TREE = BVHTree.FromPolygons(BV, [list(pl.vertices) for pl in HEAD_POLYS])


def surface(pt, want_uv=False):
    """Nearest bind-pose skin point, its interpolated normal and (optionally) its skin UV. The UV is None when the
    nearest polygon is not part of a groom shell, so strands stop where no shell geometry can show them."""
    loc, fn, idx, dist = HEAD_TREE.find_nearest(pt)
    if loc is None:
        return (pt.copy(), Vector((0, 0, 1)), None) if want_uv else (pt.copy(), Vector((0, 0, 1)))
    pl = HEAD_POLYS[idx]
    cos = [BV[i] for i in pl.vertices]
    w = poly_3d_calc(cos, loc)
    n = Vector()
    for wi, vi in zip(w, pl.vertices):
        n += BN[vi] * wi
    n = n.normalized() if n.length > 1e-6 else fn
    if not want_uv:
        return loc, n
    if pl.index not in SHELL_POLYS:
        return loc, n, None
    uv = Vector((0.0, 0.0))
    for wi, li in zip(w, pl.loop_indices):
        uv += Vector(UVL[li].uv[:]) * wi
    return loc, n, (uv, pl.index)


def pvec(pt):
    return np.array([[pt.x, pt.y, pt.z]])


def region_at(fn, pt, n=None):
    return float(fn(pvec(pt), pvec(n) if n is not None else np.array([[0.0, 0.0, 1.0]]))[0])


def material(name):
    m = bpy.data.materials.get(name)
    if m is None:
        raise RuntimeError('Missing contract material ' + name)
    return m


def link_head_skin(obj, weights=None):
    """Parent to the rig with an Armature modifier; weights default to 100 percent Head like the H8S groom."""
    obj.parent = rig
    if weights is None:
        g = obj.vertex_groups.new(name='Head')
        g.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    else:
        groups = {}
        for vi, row in enumerate(weights):
            for name, w in row:
                if name not in groups:
                    groups[name] = obj.vertex_groups.new(name=name)
                groups[name].add([vi], w, 'REPLACE')
    m = obj.modifiers.new('Existing head skin', 'ARMATURE')
    m.object = rig


IMAGES = {}


def image(name, non_color):
    if name not in IMAGES:
        im = bpy.data.images.load(str(TEX / name), check_existing=True)
        im.colorspace_settings.name = 'Non-Color' if non_color else 'sRGB'
        IMAGES[name] = im
    return IMAGES[name]


def material_groom():
    """The contract material 'H8R Textured groom' (Groom_Albedo.png alpha cutout 0.3 plus Groom_Normal.png)."""
    m = bpy.data.materials.get('H8R Textured groom') or bpy.data.materials.new('H8R Textured groom')
    m.use_nodes = True
    nt = m.node_tree
    if not any(nd.bl_idname == 'ShaderNodeTexImage' for nd in nt.nodes):
        for nd in list(nt.nodes):
            nt.nodes.remove(nd)
        out = nt.nodes.new('ShaderNodeOutputMaterial')
        bs = nt.nodes.new('ShaderNodeBsdfPrincipled')
        alb = nt.nodes.new('ShaderNodeTexImage')
        alb.image = image('Groom_Albedo.png', False)
        nrm = nt.nodes.new('ShaderNodeTexImage')
        nrm.image = image('Groom_Normal.png', True)
        nm = nt.nodes.new('ShaderNodeNormalMap')
        nm.inputs['Strength'].default_value = 0.65
        cut = nt.nodes.new('ShaderNodeMath')  # alpha cutout at the Unity contract threshold, not blending
        cut.operation = 'GREATER_THAN'
        cut.inputs[1].default_value = 0.3
        nt.links.new(alb.outputs['Color'], bs.inputs['Base Color'])
        nt.links.new(alb.outputs['Alpha'], cut.inputs[0])
        nt.links.new(cut.outputs[0], bs.inputs['Alpha'])
        nt.links.new(nrm.outputs['Color'], nm.inputs['Color'])
        nt.links.new(nm.outputs['Normal'], bs.inputs['Normal'])
        nt.links.new(bs.outputs['BSDF'], out.inputs['Surface'])
        bs.inputs['Roughness'].default_value = 0.66
        bs.inputs['Specular IOR Level'].default_value = 0.3
    m.surface_render_method = 'DITHERED'
    m.use_backface_culling = True
    return m


def set_smooth_normals(obj, normals):
    me = obj.data
    for pl in me.polygons:
        pl.use_smooth = True
    me.normals_split_custom_set_from_vertices([tuple(n) for n in normals])


# ----------------------------------------------------------------------------------------------------------------
# Head chart: the skin's head UV island mapped into the left half of the groom atlas.
# ----------------------------------------------------------------------------------------------------------------
ATLAS = groomtex.SIZE
CHART_PX = (8, 8, ATLAS // 2 - 16, ATLAS - 16)  # x, y, w, h in atlas pixels (y from the top)


SHELL_POLYS = {}


def select_shell_polys():
    """Assign head polygons to the beard or the scalp shell by their strongest region value."""
    P = np.array([v[:] for v in BV]); N = np.array([n[:] for n in BN])
    beard_v = R.beard_region(P, N)
    scalp_v = R.scalp_region(P, N)
    for pl in HEAD_POLYS:
        ids = list(pl.vertices)
        if min(BV[i].z for i in ids) < 1.58:
            continue
        b = max(beard_v[i] for i in ids)
        sc = max(scalp_v[i] for i in ids)
        if max(b, sc) < 0.03:
            continue
        SHELL_POLYS[pl.index] = 'H9 groom beard shell' if b >= sc else 'H9 groom scalp shell'


def uv_islands(poly_ids):
    """Connected components of the given polygons in skin UV space (edges shared with identical UVs)."""
    parent = {i: i for i in poly_ids}

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    edge_owner = {}
    for pi in poly_ids:
        pl = ME.polygons[pi]
        lis = list(pl.loop_indices)
        for k in range(len(lis)):
            l0, l1 = lis[k], lis[(k + 1) % len(lis)]
            v0, v1 = ME.loops[l0].vertex_index, ME.loops[l1].vertex_index
            u0, u1 = tuple(round(c, 6) for c in UVL[l0].uv), tuple(round(c, 6) for c in UVL[l1].uv)
            key = (min(v0, v1), max(v0, v1))
            sig = (u0, u1) if v0 < v1 else (u1, u0)
            if key in edge_owner:
                other, osig = edge_owner[key]
                if osig == sig:
                    ra, rb = find(pi), find(other)
                    if ra != rb:
                        parent[ra] = rb
            else:
                edge_owner[key] = (pi, sig)
    groups = {}
    for pi in poly_ids:
        groups.setdefault(find(pi), []).append(pi)
    return list(groups.values())


def head_chart():
    """Pack every shell UV island into the chart rectangle with one shared scale (uniform texel density)."""
    islands = []
    for polys in uv_islands(sorted(SHELL_POLYS)):
        us = [UVL[li].uv[0] for pi in polys for li in ME.polygons[pi].loop_indices]
        vs = [UVL[li].uv[1] for pi in polys for li in ME.polygons[pi].loop_indices]
        islands.append({'polys': polys, 'u0': min(us), 'u1': max(us), 'v0': min(vs), 'v1': max(vs)})
    islands.sort(key=lambda d: -(d['v1'] - d['v0']) * (d['u1'] - d['u0']))
    x, y, w, h = CHART_PX
    gap = 12
    k = min(w / max(d['u1'] - d['u0'] for d in islands),
            (h - gap * (len(islands) - 1)) / sum(d['v1'] - d['v0'] for d in islands))
    cy = y
    for d in islands:
        d['x'], d['y'], d['k'] = x, cy, k
        cy += (d['v1'] - d['v0']) * k + gap
        for pi in d['polys']:
            POLY_ISLAND[pi] = d
    return {'islands': len(islands), 'k': k, 'sizes': [[round((d['u1'] - d['u0']) * k), round((d['v1'] - d['v0']) * k)] for d in islands]}


POLY_ISLAND = {}
CHART = None


def chart_px(uv, poly_index):
    """Skin UV of a shell polygon to atlas pixel coordinates (x right, y down) inside its packed island."""
    d = POLY_ISLAND[poly_index]
    return (d['x'] + (uv.x - d['u0']) * d['k'], d['y'] + (d['v1'] - uv.y) * d['k'])


def chart_uv(uv, poly_index):
    """Skin UV of a shell polygon to groom atlas UV (Blender convention, v up)."""
    x, y = chart_px(uv, poly_index)
    return (x / ATLAS, 1.0 - y / ATLAS)


# ----------------------------------------------------------------------------------------------------------------
# Flow fields for the hairstyle.
# ----------------------------------------------------------------------------------------------------------------
def hair_flow_top(s, n):
    lat = max(-1.0, min(1.0, s.x / 0.035)) * 0.16
    down = 1.7 * float(R.smooth(0.0, 0.045, s.y))
    return Vector((lat, 1.0, -down))


def hair_flow_side(s, n):
    back = 0.5 if s.y < -0.06 else 0.15
    return Vector((0.0, back, -1.0))


def beard_flow(s, n):
    if s.z > 1.694 and abs(s.x) < 0.03 and s.y < -0.12:  # mustache: outward and down from the philtrum
        return Vector((math.copysign(0.9, s.x if abs(s.x) > 1e-4 else 1.0), -0.1, -0.75))
    if n.z < -0.35:  # under the jaw: back toward the neck
        return Vector((0.0, 0.85, -0.5))
    az = float(R.azimuth(pvec(s))[0])
    chin = float(R.smooth(28, 6, abs(az)))
    return Vector((0.0, -0.12 - 0.2 * chin, -1.0))


def march(root_pt, flow, length, segs, want_uv=False):
    """Walk along the skin following flow(point, normal); returns [(skin point, normal, tangent[, uv])]."""
    res = surface(root_pt, want_uv)
    out = []
    step = length / segs
    for k in range(segs + 1):
        s, n = res[0], res[1]
        f = flow(s, n)
        f = f - n * f.dot(n)
        if f.length < 1e-6:
            f = Vector((0, 0, -1)) - n * (-n.z)
        f.normalize()
        out.append((s.copy(), n.copy(), f.copy()) + ((res[2],) if want_uv else ()))
        if k < segs:
            res = surface(s + f * step + n * 0.002, want_uv)
    return out


def area_sampler(region):
    """Area-uniform samples over head polygons whose region value is positive; returns a draw(rng) function."""
    cents = np.array([sum((BV[i] for i in pl.vertices), Vector()) / len(pl.vertices) for pl in HEAD_POLYS])
    norms = np.array([pl.normal[:] for pl in HEAD_POLYS])
    vals = region(cents, norms)
    tris, acc, total = [], [], 0.0
    for pl, val in zip(HEAD_POLYS, vals):
        if val <= 0.01:
            continue
        cos = [BV[i] for i in pl.vertices]
        for j in range(1, len(cos) - 1):
            ar = (cos[j] - cos[0]).cross(cos[j + 1] - cos[0]).length * 0.5
            if ar > 0:
                total += ar
                acc.append(total)
                tris.append((cos[0], cos[j], cos[j + 1]))

    def draw(rng):
        a0, b0, c0 = tris[bisect.bisect_left(acc, rng.random() * total)]
        u, v = rng.random(), rng.random()
        if u + v > 1:
            u, v = 1 - u, 1 - v
        return a0 + (b0 - a0) * u + (c0 - a0) * v
    return draw, total


def poisson_on_skin(rng, count, region, spacing, attempts=40000):
    draw, total = area_sampler(region)
    pts, tries = [], 0
    while len(pts) < count and tries < attempts:
        tries += 1
        q = draw(rng)
        _, n = surface(q)
        if rng.random() > region_at(region, q, n):
            continue
        if all((q - o).length >= spacing for o in pts):
            pts.append(q)
    return pts


# ----------------------------------------------------------------------------------------------------------------
# Shells: offset copies of the skin under the beard and the scalp, with copies of the skin's complete weights.
# ----------------------------------------------------------------------------------------------------------------
GROUP_NAMES = {g.index: g.name for g in body.vertex_groups}


def skin_weights(vi):
    return [(GROUP_NAMES[g.group], g.weight) for g in ME.vertices[vi].groups if g.weight > 0]


def build_shells(mat):
    P = np.array([v[:] for v in BV]); N = np.array([n[:] for n in BN])
    top_v = R.top_region(P, N)
    shells = {'H9 groom beard shell': [], 'H9 groom scalp shell': []}
    for pl in HEAD_POLYS:
        if pl.index in SHELL_POLYS:
            shells[SHELL_POLYS[pl.index]].append(pl)
    objs = []
    for name, polys in shells.items():
        used = sorted({i for pl in polys for i in pl.vertices})
        remap = {vi: k for k, vi in enumerate(used)}
        verts = []
        for vi in used:
            if 'beard' in name:
                off = 0.0019
            else:
                off = 0.0011 + 0.0016 * float(top_v[vi]) + 0.0004 * float(R.fade_height(P[vi:vi + 1])[0])
            verts.append(tuple(BV[vi] + BN[vi] * off))
        faces = [tuple(remap[i] for i in pl.vertices) for pl in polys]
        me = bpy.data.meshes.new(name)
        me.from_pydata(verts, [], faces)
        uv = me.uv_layers.new(name='UVMap')
        loop_uvs = []
        for pl in polys:
            for li in pl.loop_indices:
                loop_uvs.append(chart_uv(Vector(UVL[li].uv[:]), pl.index))
        for k, d in enumerate(uv.data):
            d.uv = loop_uvs[k]
        me.update()
        obj = bpy.data.objects.new(name, me)
        bpy.context.collection.objects.link(obj)
        me.materials.append(mat)
        link_head_skin(obj, [skin_weights(vi) for vi in used])
        set_smooth_normals(obj, [BN[vi] for vi in used])
        objs.append(obj)
    return objs


# ----------------------------------------------------------------------------------------------------------------
# Strand bake for the head chart (drawn later by make_groom_textures_h9.py).
# ----------------------------------------------------------------------------------------------------------------
def bake_strands(rng):
    rows = []  # (kind, value, width_root, width_tip, [x0, y0, x1, y1, ...])

    def emit(kind, count, region, flow, length_fn, segs, w0, w1, value_fn):
        draw, total = area_sampler(region)
        made = 0
        tries = 0
        while made < count and tries < count * 6:
            tries += 1
            q = draw(rng)
            loc, n = surface(q)
            dens = region_at(region, loc, n)
            if rng.random() > dens:
                continue
            L = length_fn(loc)
            path = march(loc, flow, L, segs, want_uv=True)
            pts = []
            broken = False
            for k, (s, nn, f, uv) in enumerate(path):
                if uv is None:
                    if k == 0:
                        broken = True
                    break  # walked off the shell: the rest could not be seen anyway
                isl = POLY_ISLAND[uv[1]]
                if k == 0:
                    first_island = isl
                elif isl is not first_island:
                    break  # crossed into another UV island: stop the strand at the seam
                x, y = chart_px(uv[0], uv[1])
                if pts and math.hypot(x - pts[-2], y - pts[-1]) > 40:  # crossed a UV seam inside the island
                    break
                pts += [x, y]
            if broken or len(pts) < 4:
                continue
            rows.append((kind, value_fn(loc), w0, w1, pts))
            made += 1
        return made

    top = lambda P_, N_: R.top_region(P_, N_)
    sides = lambda P_, N_: np.clip(R.scalp_region(P_, N_) * (0.18 + 0.82 * R.fade_height(P_) ** 1.2) * (1 - R.top_region(P_, N_)), 0, 1)
    beard = lambda P_, N_: R.beard_region(P_, N_)
    counts = {}
    counts['beard'] = emit('beard', 5200, beard, beard_flow,
                           lambda q: rng.uniform(0.0055, 0.0105), 7, 1.5, 0.45, lambda q: rng.uniform(0.75, 1.0))
    counts['fade'] = emit('fade', 7000, sides, hair_flow_side,
                          lambda q: (0.0022 + 0.0105 * region_at(lambda P_, N_: R.fade_height(P_), q)) * rng.uniform(0.8, 1.2),
                          5, 1.35, 0.45, lambda q: rng.uniform(0.75, 1.0))
    counts['top'] = emit('top', 2400, top, hair_flow_top,
                         lambda q: rng.uniform(0.035, 0.06) * (1.0 - 0.4 * float(R.smooth(-0.01, 0.04, q.y))), 16, 1.6, 0.5,
                         lambda q: rng.uniform(0.8, 1.0))
    REPORT['strands'] = counts
    flat = []
    meta = np.zeros((len(rows), 5), dtype=np.float32)  # kind index, value, w0, w1, point count
    kinds = {'beard': 0, 'fade': 1, 'top': 2}
    for i, (kind, val, w0, w1, pts) in enumerate(rows):
        meta[i] = (kinds[kind], val, w0, w1, len(pts) // 2)
        flat.extend(pts)
    STRANDS.parent.mkdir(parents=True, exist_ok=True)
    np.savez_compressed(STRANDS, meta=meta, points=np.array(flat, dtype=np.float32), size=np.array([ATLAS]))


# ----------------------------------------------------------------------------------------------------------------
# Top cards for volume, shaded with the scalp's normals.
# ----------------------------------------------------------------------------------------------------------------
class CardMesh:
    def __init__(self, name):
        self.name = name
        self.verts, self.faces, self.uvs, self.normals = [], [], [], []

    def add_strip(self, rows, normals, tile):
        """rows: (left, right) points root to tip; the tile maps root v0 to tip v1."""
        u0, v0, u1, v1 = groomtex.tile_uv(tile)
        base = len(self.verts)
        n = len(rows) - 1
        for k, ((l, r), nn) in enumerate(zip(rows, normals)):
            t = k / n
            self.verts += [tuple(l), tuple(r)]
            self.uvs += [(u0, v0 + (v1 - v0) * t), (u1, v0 + (v1 - v0) * t)]
            self.normals += [nn, nn]
        for k in range(n):
            i = base + 2 * k
            self.faces.append((i, i + 1, i + 3, i + 2))

    def build(self, mat):
        me = bpy.data.meshes.new(self.name)
        me.from_pydata(self.verts, [], self.faces)
        uv = me.uv_layers.new(name='UVMap')
        for pl in me.polygons:
            for li in pl.loop_indices:
                uv.data[li].uv = self.uvs[me.loops[li].vertex_index]
        me.update()
        obj = bpy.data.objects.new(self.name, me)
        bpy.context.collection.objects.link(obj)
        me.materials.append(mat)
        link_head_skin(obj)
        set_smooth_normals(obj, self.normals)
        return obj


def card(mesh, root_pt, flow, length, width, segs, lift, tile, taper=0.35, twist=0.0):
    path = march(root_pt, flow, length, segs)
    rows, normals = [], []
    for k, (s, n, f) in enumerate(path):
        t = k / segs
        side = n.cross(f).normalized()
        if twist:
            side = (side * math.cos(twist * t) + n * math.sin(twist * t)).normalized()
        w = width * (1.0 - taper * t) * 0.5
        c = s + n * lift(t)
        rows.append((c - side * w, c + side * w))
        normals.append(n.copy())
    mesh.add_strip(rows, normals, tile)


def build_top_cards(rng, mat):
    top = CardMesh('H9 groom top cards')
    T = lambda P_, N_: R.top_region(P_, N_)
    ntiles = len([k for k in groomtex.TILES if k.startswith('top')])

    def lift_for(q, scale):
        front = float(R.smooth(-0.02, -0.11, q.y))
        peak = (0.0055 + 0.0060 * front) * scale
        return lambda t: 0.0030 + peak * math.sin(math.pi * min(1.0, t * 0.85 + 0.12))
    roots = poisson_on_skin(rng, 46, lambda P_, N_: T(P_, N_) * np.where(P_[:, 1] < 0.02, 1.0, 0.3), 0.0135)
    roots.sort(key=lambda q: -q.y)
    for q in roots:
        L = 0.074 * (1.0 - 0.45 * float(R.smooth(-0.02, 0.04, q.y))) * rng.uniform(0.9, 1.08)
        card(top, q, hair_flow_top, L, rng.uniform(0.017, 0.022), 8, lift_for(q, 1.0), 'top%d' % rng.randrange(ntiles), taper=0.3)
    outer = poisson_on_skin(rng, 30, lambda P_, N_: T(P_, N_) * np.where(P_[:, 1] < 0.0, 1.0, 0.15), 0.016)
    outer.sort(key=lambda q: -q.y)
    for q in outer:
        L = 0.066 * (1.0 - 0.4 * float(R.smooth(-0.03, 0.03, q.y))) * rng.uniform(0.85, 1.05)
        card(top, q, hair_flow_top, L, rng.uniform(0.013, 0.017), 8, lift_for(q, 1.3), 'top%d' % rng.randrange(ntiles),
             taper=0.5, twist=rng.uniform(-0.2, 0.2))
    for i in range(12):  # hairline cards: soften the front edge and lift off the forehead
        x = -0.044 + 0.088 * (i + rng.uniform(-0.3, 0.3)) / 11
        z = float(R.front_hairline_z(x)) + 0.004
        hit = HEAD_TREE.ray_cast(Vector((x, -0.4, z)), Vector((0, 1, 0)), 1.0)
        if hit[0] is not None:
            card(top, hit[0], hair_flow_top, rng.uniform(0.032, 0.042), rng.uniform(0.014, 0.019), 6,
                 lambda t: 0.0012 + 0.0048 * t, 'top%d' % rng.randrange(ntiles), taper=0.45)
    return top.build(mat)


# ----------------------------------------------------------------------------------------------------------------
# Eyes: lid margin detection, lashes and a wet tear line.
# ----------------------------------------------------------------------------------------------------------------
def eye_info(index):
    sc = bpy.data.objects['Sclera %d' % index]
    cs = [v.co for v in sc.data.vertices]
    lo = Vector([min(c[i] for c in cs) for i in range(3)])
    hi = Vector([max(c[i] for c in cs) for i in range(3)])
    return (lo + hi) / 2, (hi.x - lo.x) / 2


def lid_margins(center, radius):
    """Front ray casts over the eye. A ray belongs to the visible opening when it reaches the eyeball sphere before
    the skin, so the margin is the lid edge the eye actually shows. Returns upper and lower margins (skin points
    just outside the opening) ordered by x."""
    cols = []
    for i in range(121):
        x = center.x + (i / 120.0 - 0.5) * 0.040
        hits = []
        for j in range(241):
            z = center.z + (j / 240.0 - 0.5) * 0.026
            loc, nn, idx, d = HEAD_TREE.ray_cast(Vector((x, -0.5, z)), Vector((0, 1, 0)), 1.0)
            disc = radius * radius - (x - center.x) ** 2 - (z - center.z) ** 2
            on_eye = disc > 0 and (loc is None or loc.y > center.y - math.sqrt(disc))
            hits.append((z, loc, on_eye))
        opening = [k for k, (z, loc, e) in enumerate(hits) if e]
        if not opening or not any(abs(k - 120) < 45 for k in opening):
            continue
        lo_i, hi_i = min(opening), max(opening)
        if lo_i == 0 or hi_i == len(hits) - 1:
            continue
        cols.append((x, hits[hi_i + 1][1], hits[lo_i - 1][1]))
    cols.sort(key=lambda c: c[0])
    return [c[1] for c in cols if c[1] is not None], [c[2] for c in cols if c[2] is not None]


def resample(points, count):
    acc = [0.0]
    for p0, p1 in zip(points, points[1:]):
        acc.append(acc[-1] + (p1 - p0).length)
    out = []
    for k in range(count):
        s = acc[-1] * k / (count - 1)
        for i in range(len(acc) - 1):
            if acc[i + 1] >= s:
                t = (s - acc[i]) / max(1e-9, acc[i + 1] - acc[i])
                out.append(points[i].lerp(points[i + 1], t))
                break
    return out


def build_eye_details(mat):
    cornea = material('H6 Cornea')
    lashes = CardMesh('H9 lashes')
    wet = {'verts': [], 'faces': []}
    rows, margins = [], {}
    for index in (0, 1):
        c, r = eye_info(index)
        upper, lower = lid_margins(c, r)
        rows.append({'eye': index, 'upper_samples': len(upper), 'lower_samples': len(lower)})
        if len(upper) < 6 or len(lower) < 6:
            raise RuntimeError('Lid margin detection failed for eye %d' % index)
        margins[str(index)] = {'upper': [list(v) for v in upper], 'lower': [list(v) for v in lower]}
        for kind, line in (('upper', upper), ('lower', lower)):
            full = resample(line, 41)
            pts = full[3:-3] if kind == 'upper' else full[6:-6]  # keep lashes off the corners
            pts = resample(pts, 13 if kind == 'upper' else 9)
            up = Vector((0, 0, 1 if kind == 'upper' else -1))
            fwd = Vector((0, -1, 0))
            strip, normals = [], []
            for k, pt in enumerate(pts):
                t = k / (len(pts) - 1)
                inward = (c - pt).normalized()
                L = (0.0090 if kind == 'upper' else 0.0032) * (0.72 + 0.28 * math.sin(math.pi * t))
                # Root on the lid margin itself, tucked toward the eyeball so lashes grow out of the lid edge.
                base = pt + inward * 0.00045 + up * (-0.00035 if kind == 'upper' else -0.0002)
                tipdir = (fwd * 0.75 + up * 0.66).normalized() if kind == 'upper' else (fwd * 0.8 + up * 0.6).normalized()
                strip.append((base, base + tipdir * L))
                normals.append((fwd * 0.6 + up * 0.4).normalized())
            u0, v0, u1, v1 = groomtex.tile_uv('lash_upper' if kind == 'upper' else 'lash_lower')
            basei = len(lashes.verts)
            nseg = len(strip) - 1
            for k, ((b, tp), nn) in enumerate(zip(strip, normals)):
                uu = u0 + (u1 - u0) * (k / nseg)
                lashes.verts += [tuple(b), tuple(tp)]
                lashes.uvs += [(uu, v0), (uu, v1)]
                lashes.normals += [nn, nn]
            for k in range(nseg):
                i = basei + 2 * k
                e1 = Vector(lashes.verts[i + 2]) - Vector(lashes.verts[i])
                e2 = Vector(lashes.verts[i + 1]) - Vector(lashes.verts[i])
                lashes.faces.append((i, i + 2, i + 3, i + 1) if e1.cross(e2).y < 0 else (i, i + 1, i + 3, i + 2))
            if kind == 'lower':
                ptsw = resample(line, 15)[2:-2]
                basei = len(wet['verts'])
                for pt in ptsw:
                    toward = (c - pt).normalized()
                    on_ball = c + (pt - c).normalized() * (r + 0.00025)
                    wet['verts'] += [tuple(pt + toward * 0.0002 + Vector((0, 0, 0.0002))), tuple(on_ball + Vector((0, 0, 0.0006)))]
                for k in range(len(ptsw) - 1):
                    i = basei + 2 * k
                    e1 = Vector(wet['verts'][i + 2]) - Vector(wet['verts'][i])
                    e2 = Vector(wet['verts'][i + 1]) - Vector(wet['verts'][i])
                    wet['faces'].append((i, i + 2, i + 3, i + 1) if e1.cross(e2).y < 0 else (i, i + 1, i + 3, i + 2))
    objs = [lashes.build(mat)]
    me = bpy.data.meshes.new('H9 tear line')
    me.from_pydata(wet['verts'], [], wet['faces'])
    uv = me.uv_layers.new(name='OpticalUV')
    for d in uv.data:
        d.uv = (0.5, 0.5)
    me.update()
    obj = bpy.data.objects.new('H9 tear line', me)
    bpy.context.collection.objects.link(obj)
    me.materials.append(cornea)
    link_head_skin(obj)
    for pl in me.polygons:
        pl.use_smooth = True
    objs.append(obj)
    REPORT['lid_margins'] = rows
    (STRANDS.parent / 'lid-margins-h9.json').write_text(json.dumps(margins))
    return objs


def conform_brows():
    """Lay the existing brow card onto the brow ridge (it floated up to 4 mm off the skin in H8S)."""
    o = bpy.data.objects['Brows']
    before, after = [], []
    for v in o.data.vertices:
        loc, n = surface(v.co)
        before.append((v.co - loc).length)
        v.co = loc + n * 0.00035
        after.append(0.00035)
    o.data.update()
    REPORT['brows_offset_mm'] = {'before_max': round(max(before) * 1000, 2), 'before_mean': round(sum(before) / len(before) * 1000, 2), 'after': 0.35}


def tune_skin_material():
    """Blender preview of the Unity contract: 'H8R Skin' smoothness 0.28 reads as a fairly rough, satin skin."""
    m = bpy.data.materials['H8R Skin']
    bs = next(nd for nd in m.node_tree.nodes if nd.bl_idname == 'ShaderNodeBsdfPrincipled')
    bs.inputs['Roughness'].default_value = 0.62
    bs.inputs['Specular IOR Level'].default_value = 0.42
    bs.inputs['Subsurface Weight'].default_value = 0.12
    for nd in m.node_tree.nodes:
        if nd.bl_idname == 'ShaderNodeNormalMap':
            nd.inputs['Strength'].default_value = 0.5
    REPORT['skin_material'] = {'roughness': 0.62, 'specular_ior_level': 0.42, 'normal_strength': 0.5}
    sc = bpy.data.materials['H6 Sclera']
    sb = next(nd for nd in sc.node_tree.nodes if nd.bl_idname == 'ShaderNodeBsdfPrincipled')
    sb.inputs['Base Color'].default_value = (0.36, 0.33, 0.29, 1.0)  # natural off-white, not a paper-white stare
    sb.inputs['Roughness'].default_value = 0.35


# ----------------------------------------------------------------------------------------------------------------
# Stages.
# ----------------------------------------------------------------------------------------------------------------
def describe(objs):
    return [{'object': o.name, 'vertices': len(o.data.vertices), 'triangles': sum(len(pl.vertices) - 2 for pl in o.data.polygons)} for o in objs]


def stage_groom():
    global CHART
    for o in list(bpy.data.objects):
        if o.name in ('H8R Surface hair', 'H8R Surface beard'):
            bpy.data.objects.remove(o, do_unlink=True)
    select_shell_polys()
    CHART = head_chart()
    REPORT['head_chart'] = CHART
    mat = material_groom()
    objs = build_shells(mat)
    objs.append(build_top_cards(random.Random(90901), mat))
    bake_strands(random.Random(90902))
    REPORT['groom'] = describe(objs)


def stage_eyes():
    REPORT['eyes'] = describe(build_eye_details(material_groom()))
    conform_brows()
    tune_skin_material()


def stage_textures():
    """Point every file image at the H9 texture folder (the H9 export ships its own copies)."""
    moved = []
    for im in bpy.data.images:
        if im.source != 'FILE' or not im.filepath:
            continue
        name = Path(bpy.path.abspath(im.filepath)).name
        if not (TEX / name).exists():
            raise RuntimeError('H9 texture folder lacks ' + name)
        im.filepath = str(TEX / name)
        moved.append(name)
    REPORT['textures'] = sorted(set(moved))


if 'groom' in STAGES:
    stage_groom()
if 'eyes' in STAGES:
    stage_eyes()
stage_textures()

for i in range(3):
    bpy.ops.outliner.orphans_purge(do_recursive=True)
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT), compress=True, relative_remap=True)
print('H9REPORT ' + json.dumps(REPORT), flush=True)
