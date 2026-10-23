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
p.add_argument('--stages', default='groom,eyes,garment,body')
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
    """File image in the H9 texture folder. The groom atlas is painted after this script (it needs the strand bake),
    so a missing file gets a path-only datablock that resolves once make_groom_textures_h9.py has run."""
    if name not in IMAGES:
        path = TEX / name
        if path.exists():
            im = bpy.data.images.load(str(path), check_existing=True)
        else:
            im = bpy.data.images.new(name, 4, 4, alpha=True)
            im.source = 'FILE'
            im.filepath = str(path)
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
        self.layer, self.index = [], []
        self.cards = 0
        self.current_layer = 0

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
            self.layer += [self.current_layer] * 2
            self.index += [self.cards] * 2
        self.cards += 1
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
        if self.layer:
            # Card layer (0 inner, 1 outer, 2 hairline) and card number let the exporter thin cards per LOD.
            la = me.attributes.new('h9_card_layer', 'INT', 'POINT')
            la.data.foreach_set('value', self.layer)
            ia = me.attributes.new('h9_card_index', 'INT', 'POINT')
            ia.data.foreach_set('value', self.index)
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
    top.current_layer = 1
    outer = poisson_on_skin(rng, 30, lambda P_, N_: T(P_, N_) * np.where(P_[:, 1] < 0.0, 1.0, 0.15), 0.016)
    outer.sort(key=lambda q: -q.y)
    for q in outer:
        L = 0.066 * (1.0 - 0.4 * float(R.smooth(-0.03, 0.03, q.y))) * rng.uniform(0.85, 1.05)
        card(top, q, hair_flow_top, L, rng.uniform(0.013, 0.017), 8, lift_for(q, 1.3), 'top%d' % rng.randrange(ntiles),
             taper=0.5, twist=rng.uniform(-0.2, 0.2))
    top.current_layer = 2
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
    """Exactly count points spaced evenly along a polyline."""
    acc = [0.0]
    for p0, p1 in zip(points, points[1:]):
        acc.append(acc[-1] + (p1 - p0).length)
    out = []
    for k in range(count):
        s = min(acc[-1], acc[-1] * k / (count - 1))
        for i in range(len(acc) - 1):
            if acc[i + 1] >= s - 1e-12:
                t = (s - acc[i]) / max(1e-9, acc[i + 1] - acc[i])
                out.append(points[i].lerp(points[i + 1], max(0.0, min(1.0, t))))
                break
        else:
            out.append(points[-1].copy())
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
# Garment: fitted rolled collar, shaped lapels, conformed armour, trims and belt, decimated coat with body weights.
# ----------------------------------------------------------------------------------------------------------------
def mesh_arrays(obj):
    return [v.co.copy() for v in obj.data.vertices], [list(pl.vertices) for pl in obj.data.polygons]


def weight_rows(obj):
    names = {g.index: g.name for g in obj.vertex_groups}
    return [{names[g.group]: g.weight for g in v.groups if g.weight > 0} for v in obj.data.vertices]


class SurfaceWeights:
    """Nearest-point lookup on a skinned mesh: position, smooth normal and barycentric blend of its weights."""
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
        n = Vector()
        row = {}
        for wi, vi in zip(ws, ids):
            n += self.n[vi] * wi
            for k, val in self.w[vi].items():
                row[k] = row.get(k, 0.0) + val * wi
        return loc, (n.normalized() if n.length > 1e-6 else fn), row, d


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


def new_skinned(name, verts, faces, uvs, mat, rows, smooth=True):
    me = bpy.data.meshes.new(name)
    me.from_pydata(verts, [], faces)
    uv = me.uv_layers.new(name='UVMap')
    for pl in me.polygons:
        pl.use_smooth = smooth
        for li in pl.loop_indices:
            uv.data[li].uv = uvs[me.loops[li].vertex_index]
    me.validate(clean_customdata=False)
    me.update()
    obj = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(obj)
    me.materials.append(mat)
    obj.parent = rig
    set_weights(obj, rows)
    m = obj.modifiers.new('Complete fitted skin', 'ARMATURE')
    m.object = rig
    obj['candidate_source'] = 'h9-fitted-garment'
    return obj


def ray_radius(tree, center, direction, rmax=0.2):
    """Distance from an axis point to a mesh along a horizontal direction (first hit from inside)."""
    loc, n, idx, d = tree.ray_cast(center, direction, rmax)
    return d if loc is not None else None


def build_collar(coat_sw, body_sw):
    coat = bpy.data.objects['H7 tailored connected coat']
    bm = bmesh.new(); bm.from_mesh(coat.data)
    rim = [v.co.copy() for e in bm.edges if e.is_boundary for v in e.verts if v.co.z > 1.58]
    bm.free()
    cx, cy = 0.0, sum(c.y for c in rim) / len(rim)
    z_open = sum(c.z for c in rim) / len(rim)

    def rim_radius(theta):
        d = Vector((math.sin(theta), -math.cos(theta), 0))
        best, bd = None, 1e9
        for c in rim:
            v = Vector((c.x - cx, c.y - cy, 0))
            if v.length < 1e-6:
                continue
            ang = v.normalized().dot(d)
            if ang > 0.995 and (1 - ang) < bd:
                bd, best = 1 - ang, v.length
        if best is None:
            best = max(Vector((c.x - cx, c.y - cy, 0)).length * max(0.0, Vector((c.x - cx, c.y - cy, 0)).normalized().dot(d)) for c in rim)
        return best
    body_tree = HEAD_TREE_FULL
    span0, span1 = math.radians(38), math.radians(322)
    cols = 64
    prof = []  # per column: list of (point) for the closed profile
    for k in range(cols + 1):
        th = span0 + (span1 - span0) * k / cols
        d = Vector((math.sin(th), -math.cos(th), 0))
        back = (1 - math.cos(th)) / 2  # 0 front, 1 back
        z_top = 1.647 + 0.021 * back
        end = min(k, cols - k) / 6.0
        taper = min(1.0, end)
        z_top = z_open + (z_top - z_open) * (0.35 + 0.65 * math.sin(min(1.0, taper) * math.pi / 2))
        r_neck = ray_radius(body_tree, Vector((cx, cy, z_top - 0.008)), d) or 0.06
        r_t = r_neck + 0.010 + 0.003 * (1 - back)
        r_b = rim_radius(th) + 0.004
        z_b = z_open - 0.014
        pts = []
        ring = [(r_b, z_b), ((r_b + r_t) / 2 + 0.002, (z_b + z_top) / 2), (r_t + 0.002, z_top - 0.007),
                (r_t + 0.0045, z_top - 0.001), (r_t + 0.002, z_top + 0.0025), (r_t - 0.0015, z_top + 0.001),
                (r_t - 0.0035, z_top - 0.005), (r_t - 0.003, (z_b + z_top) / 2 + 0.004), (r_b - 0.006, z_b + 0.004)]
        for rr, zz in ring:
            pts.append(Vector((cx, cy, 0)) + d * rr + Vector((0, 0, zz)))
        prof.append(pts)
    verts, faces, uvs, rows = [], [], [], []
    npf = len(prof[0])
    for k, pts in enumerate(prof):
        acc = 0.0
        for j, pt in enumerate(pts):
            if j:
                acc += (pt - pts[j - 1]).length
            verts.append(tuple(pt))
            uvs.append((k / cols * 2.4, acc / 0.2))
            h = max(0.0, min(1.0, (pt.z - (z_open - 0.014)) / 0.05))
            _, _, cw, _ = coat_sw.query(pt)
            _, _, bw, _ = body_sw.query(pt)
            blend = h ** 1.4
            row = {}
            for kk, vv in cw.items():
                row[kk] = row.get(kk, 0) + vv * (1 - blend)
            for kk, vv in bw.items():
                row[kk] = row.get(kk, 0) + vv * blend
            rows.append(normalized(row))
    for k in range(cols):
        for j in range(npf - 1):
            a0 = k * npf + j
            b0 = (k + 1) * npf + j
            faces.append((a0, b0, b0 + 1, a0 + 1))
    # end caps close the collar at its front ends
    for k in (0, cols):
        ids = [k * npf + j for j in range(npf)]
        faces.append(tuple(ids if k == cols else list(reversed(ids))))
    obj = new_skinned('H9 fitted rolled collar', verts, faces, uvs, material('H6 Worn leather'), rows)
    return obj


def build_lapels(coat_sw, coat_tree):
    objs = []
    for sgn in (1.0, -1.0):
        # Narrow pointed revers: from the collar end out to a peak below the collarbone, then down to the break.
        outer = [(0.049, 1.611), (0.068, 1.594), (0.093, 1.566), (0.103, 1.556), (0.092, 1.538), (0.072, 1.512), (0.050, 1.487), (0.037, 1.476)]
        inner = [(0.045, 1.605), (0.041, 1.565), (0.038, 1.520), (0.035, 1.479)]
        def curve_pts(c, n):
            vs = [Vector((sgn * x, 0, z)) for x, z in c]
            return resample(vs, n)
        N, M = 12, 5
        oc, ic = curve_pts(outer, N), curve_pts(inner, N)
        grid = []
        for i in range(N):
            row = []
            for j in range(M):
                t = j / (M - 1)
                q = ic[i].lerp(oc[i], t)
                hit = coat_tree.ray_cast(Vector((q.x, -1.0, q.z)), Vector((0, 1, 0)), 2.0)
                if hit[0] is None:
                    raise RuntimeError('Lapel outline left the coat surface')
                loc, n, row_w, d = coat_sw.query(hit[0])
                roll = 0.0035 * (1 - t) ** 2  # the fold line rolls up off the chest
                row.append((loc + n * (0.0035 + roll), n, row_w))
            grid.append(row)
        verts, faces, uvs, rows = [], [], [], []
        for i in range(N):
            for j in range(M):
                pt, n, w = grid[i][j]
                verts.append(tuple(pt)); uvs.append((j / (M - 1) * 0.4, i / (N - 1) * 0.9)); rows.append(normalized(w))
        front = lambda i, j: i * M + j
        for i in range(N - 1):
            for j in range(M - 1):
                a0, b0, c0, d0 = front(i, j), front(i, j + 1), front(i + 1, j + 1), front(i + 1, j)
                faces.append((a0, d0, c0, b0) if sgn > 0 else (a0, b0, c0, d0))
        # thickness: a 3 mm wall along the outer edge and the bottom so the lapel reads as folded leather
        base = len(verts)
        rim_ids = [front(i, M - 1) for i in range(N)] + [front(N - 1, j) for j in range(M - 2, -1, -1)]
        for vi in rim_ids:
            pt, n, w = grid[vi // M][vi % M]
            verts.append(tuple(pt - n * 0.003)); uvs.append(uvs[vi]); rows.append(normalized(w))
        for k in range(len(rim_ids) - 1):
            a0, b0 = rim_ids[k], rim_ids[k + 1]
            c0, d0 = base + k + 1, base + k
            faces.append((a0, b0, c0, d0) if sgn > 0 else (a0, d0, c0, b0))
        objs.append(new_skinned('H9 shaped lapel %s' % ('l' if sgn > 0 else 'r'), verts, faces, uvs, material('H6 Worn leather'), rows))
    return objs


def conform(obj, sw, offset, max_thickness=None, rigid=False):
    """Lay an accessory onto the garment surface, keep its outline, compress its depth, copy surface weights."""
    me = obj.data
    qs = [sw.query(v.co) for v in me.vertices]
    ds = [(v.co - q[0]).dot(q[1]) for v, q in zip(me.vertices, qs)]
    dmin, dmax = min(ds), max(ds)
    before = (round(dmin * 1000, 1), round(dmax * 1000, 1))
    if rigid:
        c = sum((v.co for v in me.vertices), Vector()) / len(me.vertices)
        loc, n, w, d = sw.query(c)
        shift = n * (offset - dmin)
        for v in me.vertices:
            v.co += shift
        rows = [normalized(w)] * len(me.vertices)
    else:
        k = 1.0 if max_thickness is None else min(1.0, max_thickness / max(1e-6, dmax - dmin))
        rows = []
        for v, q, dd in zip(me.vertices, qs, ds):
            v.co = q[0] + q[1] * (offset + (dd - dmin) * k)
            rows.append(normalized(q[2]))
    me.update()
    set_weights(obj, rows)
    return {'object': obj.name, 'depth_before_mm': before, 'offset_mm': round(offset * 1000, 1)}


def decimate(obj, ratio, unsubdivide=0):
    """Budget reduction. The remeshed cloth is a regular quad grid, so 'unsubdivide' keeps clean quads and
    exact original vertices (and weights); collapse is the fallback for irregular meshes."""
    before = len(obj.data.vertices)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    m = obj.modifiers.new('H9 budget', 'DECIMATE')
    if unsubdivide:
        m.decimate_type = 'UNSUBDIV'
        m.iterations = unsubdivide
    else:
        m.ratio = ratio
        m.use_symmetry = False
        m.use_collapse_triangulate = False
    source = obj.copy()
    source.data = obj.data.copy()
    source.modifiers.clear()
    bpy.context.collection.objects.link(source)
    bpy.ops.object.modifier_move_to_index(modifier=m.name, index=0)
    bpy.ops.object.modifier_apply(modifier=m.name)
    rows = [normalized(r) for r in weight_rows(obj)]
    set_weights(obj, rows)
    # Shade the reduced cloth with the original surface: transfer the high-resolution normals as custom normals.
    for pl in obj.data.polygons:
        pl.use_smooth = True
    dt = obj.modifiers.new('H9 original surface normals', 'DATA_TRANSFER')
    dt.object = source
    dt.use_loop_data = True
    dt.data_types_loops = {'CUSTOM_NORMAL'}
    dt.loop_mapping = 'POLYINTERP_NEAREST'
    bpy.ops.object.modifier_move_to_index(modifier=dt.name, index=0)
    bpy.ops.object.modifier_apply(modifier=dt.name)
    bpy.data.objects.remove(source, do_unlink=True)
    return {'object': obj.name, 'vertices_before': before, 'vertices_after': len(obj.data.vertices)}


def body_weight_blend(obj, body_sw, near=0.025, far=0.06):
    """Garment vertices close to the skin take the skin's own weights, so cloth and body bend together."""
    changed = 0
    own = weight_rows(obj)
    rows = []
    for v, mine in zip(obj.data.vertices, own):
        loc, n, bw, d = body_sw.query(v.co)
        f = 1.0 if d <= near else max(0.0, 1.0 - (d - near) / (far - near))
        if f <= 0:
            rows.append(normalized(mine)); continue
        row = {k: val * (1 - f) for k, val in mine.items()}
        for k, val in bw.items():
            row[k] = row.get(k, 0.0) + val * f
        rows.append(normalized(row))
        changed += 1
    set_weights(obj, rows)
    return changed


def fit_boots_over_trousers(clearance=0.005):
    """Riding boots are worn over the trousers. H8S boot shafts pinched to an hourglass at mid-shin and flared through
    the trouser leg; each shaft ring above the trouser hem is rebuilt on the trouser wall plus clearance (rays from
    the ring centre along each vertex's own direction, widest wall within 2 cm), the ankle ring eases into it, and the
    shaft takes the trouser weights so boot and trousers bend together."""
    boots = bpy.data.objects['H8R fitted riding boots']
    trousers = bpy.data.objects['H8R fitted leather trousers']
    tverts = [v.co.copy() for v in trousers.data.vertices]
    ttree = BVHTree.FromPolygons(tverts, [list(pl.vertices) for pl in trousers.data.polygons])
    hem = min(v.z for v in tverts)
    tsw = SurfaceWeights([trousers])
    rings = {}
    for v in boots.data.vertices:
        rings.setdefault(('l' if v.co.x > 0 else 'r', round(v.co.z, 2)), []).append(v)
    centres = {k: sum((v.co for v in vs), Vector()) / len(vs) for k, vs in rings.items()}

    def radius_profile(key):
        c = centres[key]
        return sorted((math.atan2(v.co.y - c.y, v.co.x - c.x), (Vector((v.co.x - c.x, v.co.y - c.y, 0))).length) for v in rings[key])

    def radius_at(profile, ang):
        best = min(profile, key=lambda pr: abs(math.atan2(math.sin(pr[0] - ang), math.cos(pr[0] - ang))))
        return best[1]
    rebuilt = {}
    for key, vs in rings.items():
        side, z = key
        if z <= hem:
            continue
        c = centres[key]
        for v in vs:
            d = Vector((v.co.x - c.x, v.co.y - c.y, 0.0))
            if d.length < 1e-7:
                d = Vector((1, 0, 0))
            d.normalize()
            wall = None
            for dz in (-0.02, 0.0, 0.02):
                zz = max(hem + 0.002, v.co.z + dz)
                loc, n, idx, dist = ttree.ray_cast(Vector((c.x, c.y, zz)), d, 0.15)
                if loc is not None:
                    wall = dist if wall is None else max(wall, dist)
            if wall is not None:
                v.co = Vector((c.x, c.y, v.co.z)) + d * (wall + clearance)
        rebuilt[key] = True
    for key, vs in rings.items():
        side, z = key
        if key in rebuilt or z <= hem - 0.05:
            continue
        above = sorted(k for k in rebuilt if k[0] == side and k[1] > z)
        if not above:
            continue
        prof = radius_profile(above[0])
        c = centres[key]
        for v in vs:
            d = Vector((v.co.x - c.x, v.co.y - c.y, 0.0))
            if d.length < 1e-7:
                continue
            ang = math.atan2(d.y, d.x)
            r = max(d.length, 0.5 * (d.length + radius_at(prof, ang)))
            v.co = Vector((c.x, c.y, v.co.z)) + d.normalized() * r
    own = weight_rows(boots)
    rows = []
    for v, mine in zip(boots.data.vertices, own):
        f = float(R.smooth(hem - 0.03, hem + 0.04, v.co.z))
        if f <= 0:
            rows.append(normalized(mine)); continue
        _, _, tw, _ = tsw.query(v.co)
        row = {kk: val * (1 - f) for kk, val in mine.items()}
        for kk, val in tw.items():
            row[kk] = row.get(kk, 0.0) + val * f
        rows.append(normalized(row))
    boots.data.update()
    set_weights(boots, rows)
    return {'trouser_hem_z': round(hem, 3), 'rebuilt_rings': sorted('%s %.2f' % k for k in rebuilt)}


def stage_garment():
    global HEAD_TREE_FULL
    HEAD_TREE_FULL = BVHTree.FromPolygons(BV, [list(pl.vertices) for pl in ME.polygons])
    coat = bpy.data.objects['H7 tailored connected coat']
    panels = [bpy.data.objects['H7 split walking panel l'], bpy.data.objects['H7 split walking panel r']]
    body_sw = SurfaceWeights([body])
    rep = {}
    rep['coat_decimate'] = decimate(coat, 0.26)
    rep['panel_decimate'] = [decimate(o, 0.30) for o in panels]
    rep['coat_body_weighted_vertices'] = body_weight_blend(coat, body_sw)
    coat_sw = SurfaceWeights([coat])
    coat_tree = coat_sw.tree
    garment_sw = SurfaceWeights([coat] + panels)
    for o in list(bpy.data.objects):
        if o.name in ('H8S continuous rolled collar', 'H7 folded lapel 1', 'H7 folded lapel -1'):
            bpy.data.objects.remove(o, do_unlink=True)
    collar = build_collar(coat_sw, body_sw)
    lapels = build_lapels(coat_sw, coat_tree)
    rep['new'] = describe([collar] + lapels)
    rep['conform'] = []
    for name in ('H7 contoured chest plate 1', 'H7 contoured chest plate -1'):
        rep['conform'].append(conform(bpy.data.objects[name], coat_sw, 0.003, max_thickness=0.009))
    for name in ('H7 front armor edge 1', 'H7 front armor edge -1'):
        rep['conform'].append(conform(bpy.data.objects[name], coat_sw, 0.0045, max_thickness=0.004))
    for name in ('H7 side tailoring seam 1', 'H7 side tailoring seam -1'):
        rep['conform'].append(conform(bpy.data.objects[name], garment_sw, 0.0012, max_thickness=0.0025))
    rep['conform'].append(conform(bpy.data.objects['H7 anchored belt'], garment_sw, 0.0025, max_thickness=0.007))
    rep['boots'] = fit_boots_over_trousers()
    for o in bpy.data.objects:
        if o.name.startswith('Bronze coat fastener') or o.name == 'Restrained amethyst seal':
            rep['conform'].append(conform(o, coat_sw, 0.0008, rigid=True))
    REPORT['garment'] = rep


# ----------------------------------------------------------------------------------------------------------------
# Hidden body removal: skin fully covered by clothing (well away from every opening) is deleted, so it can never
# poke through cloth in a pose and costs nothing. Head, neck, throat, hands and wrists are always kept.
# ----------------------------------------------------------------------------------------------------------------
COVER = ['H7 tailored connected coat', 'H8R fitted leather trousers', 'H8R fitted riding boots',
         'Forearm fitting l', 'Forearm fitting r']


def stage_hidden_body(min_depth=0.004, min_edge=0.035):
    from mathutils.kdtree import KDTree
    shells = []
    for name in COVER:
        o = bpy.data.objects.get(name)
        if o is None:
            continue
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
        shells.append((name, BVHTree.FromPolygons(verts, polys), norms, kd, len(edge)))
    names = {g.index: g.name for g in body.vertex_groups}
    keep_bones = ('Head', 'neck_01', 'hand_', 'thumb_', 'index_', 'middle_', 'ring_', 'pinky_')
    hidden = [False] * len(ME.vertices)
    for v in ME.vertices:
        if v.co.z > 1.585:
            continue
        gs = [g for g in v.groups if g.weight > 0]
        if gs and names[max(gs, key=lambda g: g.weight).group].startswith(keep_bones):
            continue
        for name, tree, norms, kd, ne in shells:
            loc, n, idx, d = tree.find_nearest(v.co)
            if loc is None or d > 0.09:
                continue
            depth = -(v.co - loc).dot(norms[idx])
            if depth >= min_depth and (ne == 0 or kd.find(loc)[2] >= min_edge):
                hidden[v.index] = True
                break
    bm = bmesh.new(); bm.from_mesh(ME)
    bm.verts.ensure_lookup_table()
    doomed = [f for f in bm.faces if all(hidden[v.index] for v in f.verts)]
    before_v, before_f = len(bm.verts), len(bm.faces)
    bmesh.ops.delete(bm, geom=doomed, context='FACES')
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
    bm.to_mesh(ME); bm.free()
    ME.update()
    REPORT['hidden_body'] = {'vertices_before': before_v, 'vertices_after': len(ME.vertices),
                             'faces_before': before_f, 'faces_after': len(ME.polygons),
                             'rule': 'all face vertices at least %.0f mm inside clothing and %.0f mm from any garment opening' % (min_depth * 1000, min_edge * 1000)}


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
    for o in objs:
        o['h9_lod_policy'] = 'shell'
    cards = build_top_cards(random.Random(90901), mat)
    cards['h9_lod_policy'] = 'cards'
    objs.append(cards)
    bake_strands(random.Random(90902))
    REPORT['groom'] = describe(objs)


def stage_eyes():
    objs = build_eye_details(material_groom())
    for o in objs:
        o['h9_lod_policy'] = 'drop_lod2'
    REPORT['eyes'] = describe(objs)
    conform_brows()
    tune_skin_material()


def stage_textures():
    """Point every file image at the H9 texture folder (the H9 export ships its own copies)."""
    moved = []
    for im in bpy.data.images:
        if im.source != 'FILE' or not im.filepath:
            continue
        name = Path(bpy.path.abspath(im.filepath)).name
        im.filepath = str(TEX / name)
        moved.append(name)
    REPORT['textures'] = sorted(set(moved))


if 'groom' in STAGES:
    stage_groom()
if 'eyes' in STAGES:
    stage_eyes()
HEAD_TREE_FULL = None
if 'garment' in STAGES:
    stage_garment()
if 'body' in STAGES:
    stage_hidden_body()
stage_textures()

for i in range(3):
    bpy.ops.outliner.orphans_purge(do_recursive=True)
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT), compress=True, relative_remap=True)
print('H9REPORT ' + json.dumps(REPORT), flush=True)
