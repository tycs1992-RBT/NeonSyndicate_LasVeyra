#!/usr/bin/env python3
"""Derive MalikVeyrH9.blend from the owner-selected H8S source. Reproducible: always starts from H8S.

Visual-only art changes. The 53-bone MalikRig (names, hierarchy, rest pose), every socket empty, all 17 actions and
every existing skin influence are left untouched. New groom, lash and tear-line geometry is weighted 100 percent to
the existing Head bone, exactly like the H8S strand groom it replaces. Garment edits keep their complete weights.

Run: python tools/author_malik_h9.py [--root <repo>] [--output <blend>] [--stages groom,face,garment,skin]
"""
import argparse, hashlib, json, math, random, sys
from pathlib import Path
import bpy, bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from mathutils.interpolate import poly_3d_calc

p = argparse.ArgumentParser()
p.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
p.add_argument('--output', type=Path)
p.add_argument('--stages', default='groom,face,garment,skin')
p.add_argument('--textures', type=Path, help='folder holding the H9 textures (default ProductionH9/Textures)')
a = p.parse_args(sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else sys.argv[1:])
root = a.root.resolve()
SOURCE = root / 'ArtSource/AscensionH8S/MalikVeyrH8S.blend'
SOURCE_SHA = '0180aa40897a6ef631122452d444274c0767b67d2cecfd837f2fa37a6365e969'
OUTPUT = (a.output or root / 'ArtSource/AscensionH9/MalikVeyrH9.blend').resolve()
TEX = (a.textures or root / 'Assets/NeonSyndicate/Art/ProductionH9/Textures').resolve()
STAGES = set(s.strip() for s in a.stages.split(',') if s.strip())
if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA:
    raise SystemExit('H8S source fingerprint changed; refusing to derive H9 from an unverified file.')
bpy.ops.wm.open_mainfile(filepath=str(SOURCE), load_ui=False, use_scripts=False)
rig = bpy.data.objects['MalikRig']
body = bpy.data.objects['MalikBodyH8R']
REPORT = {'source': str(SOURCE.relative_to(root)), 'source_sha256': SOURCE_SHA, 'stages': sorted(STAGES)}
sys.path.insert(0, str(root / 'tools'))
import make_groom_textures_h9 as groomtex  # tile table shared with the texture painter

# ----------------------------------------------------------------------------------------------------------------
# Surface helpers on the bind-pose body (the armature is at rest, so mesh coordinates are the bind pose).
# ----------------------------------------------------------------------------------------------------------------
BV = [v.co.copy() for v in body.data.vertices]
BN = [v.normal.copy() for v in body.data.vertices]
HEAD_POLYS = [pl for pl in body.data.polygons if max(BV[i].z for i in pl.vertices) > 1.55]
HEAD_TREE = BVHTree.FromPolygons(BV, [list(pl.vertices) for pl in HEAD_POLYS])


def surface(pt):
    """Nearest bind-pose skin point and its smooth (interpolated) normal."""
    loc, fn, idx, dist = HEAD_TREE.find_nearest(pt)
    if loc is None:
        return pt.copy(), Vector((0, 0, 1))
    pl = HEAD_POLYS[idx]
    cos = [BV[i] for i in pl.vertices]
    w = poly_3d_calc(cos, loc)
    n = Vector()
    for wi, vi in zip(w, pl.vertices):
        n += BN[vi] * wi
    if n.length < 1e-6:
        n = fn
    return loc, n.normalized()


def smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def azimuth(pt):
    """Degrees around the vertical head axis; 0 faces forward (-Y), +90 is the character's left (+X)."""
    return math.degrees(math.atan2(pt.x, -(pt.y + 0.03)))


def head_bone_weight(obj):
    g = obj.vertex_groups.new(name='Head')
    g.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    obj.parent = rig
    m = obj.modifiers.new('Existing head skin', 'ARMATURE')
    m.object = rig


def material(name):
    m = bpy.data.materials.get(name)
    if m is None:
        raise RuntimeError('Missing contract material ' + name)
    return m


# ----------------------------------------------------------------------------------------------------------------
# Hair card construction.
# ----------------------------------------------------------------------------------------------------------------
class CardMesh:
    def __init__(self, name):
        self.name = name
        self.verts, self.faces, self.uvs = [], [], []

    def add_strip(self, rows, tile, flip=False):
        """rows: list of (left, right) points root to tip. Adds quads with the tile mapped root v0 to tip v1."""
        u0, v0, u1, v1 = groomtex.tile_uv(tile)
        if flip:
            u0, u1 = u1, u0
        base = len(self.verts)
        n = len(rows) - 1
        for k, (l, r) in enumerate(rows):
            t = k / n
            self.verts += [tuple(l), tuple(r)]
            self.uvs += [(u0, v0 + (v1 - v0) * t), (u1, v0 + (v1 - v0) * t)]
        for k in range(n):
            i = base + 2 * k
            self.faces.append((i, i + 1, i + 3, i + 2))

    def build(self, mat):
        me = bpy.data.meshes.new(self.name)
        me.from_pydata(self.verts, [], self.faces)
        uv = me.uv_layers.new(name='UVMap')
        for pl in me.polygons:
            pl.use_smooth = True
            for li in pl.loop_indices:
                uv.data[li].uv = self.uvs[me.loops[li].vertex_index]
        me.validate(clean_customdata=False)
        me.update()
        obj = bpy.data.objects.new(self.name, me)
        bpy.context.collection.objects.link(obj)
        me.materials.append(mat)
        head_bone_weight(obj)
        return obj


def march(root_pt, flow, length, segs):
    """Walk along the skin from root_pt following flow(point, normal); returns [(skin point, normal, tangent)]."""
    s, n = surface(root_pt)
    out = []
    step = length / segs
    for k in range(segs + 1):
        f = flow(s, n)
        f = (f - n * f.dot(n))
        if f.length < 1e-6:
            f = Vector((0, 0, -1)) - n * (-n.z)
        f.normalize()
        out.append((s.copy(), n.copy(), f.copy()))
        if k < segs:
            s, n = surface(s + f * step + n * 0.002)
    return out


def card(mesh, root_pt, flow, length, width, segs, lift, tile, taper=0.35, twist=0.0, rng=None, flip=False, push=None):
    path = march(root_pt, flow, length, segs)
    rows = []
    for k, (s, n, f) in enumerate(path):
        t = k / segs
        h = lift(t)
        side = n.cross(f).normalized()
        if twist:
            side = (side * math.cos(twist * t) + n * math.sin(twist * t)).normalized()
        w = width * (1.0 - taper * t) * 0.5
        c = s + n * h
        if push is not None:
            c = c + push(t)
        rows.append((c - side * w, c + side * w))
    mesh.add_strip(rows, tile, flip)
    return path


def poisson_on_skin(rng, count, region, spacing, attempts=40000, seed_pts=None):
    """Area-uniform dart throwing over head polygons weighted by region(P, N) (vectorised numpy regions)."""
    import numpy as np
    cents = np.array([sum((BV[i] for i in pl.vertices), Vector()) / len(pl.vertices) for pl in HEAD_POLYS])
    norms = np.array([pl.normal[:] for pl in HEAD_POLYS])
    vals = region(cents, norms)
    tris, acc, total = [], [], 0.0
    for pl, val in zip(HEAD_POLYS, vals):
        if val <= 0.02:
            continue
        cos = [BV[i] for i in pl.vertices]
        for j in range(1, len(cos) - 1):
            ar = (cos[j] - cos[0]).cross(cos[j + 1] - cos[0]).length * 0.5 * float(val)
            if ar > 0:
                total += ar
                acc.append(total)
                tris.append((cos[0], cos[j], cos[j + 1]))
    import bisect
    pts = list(seed_pts or [])
    tries = 0
    while len(pts) < count and tries < attempts:
        tries += 1
        a0, b0, c0 = tris[bisect.bisect_left(acc, rng.random() * total)]
        u, v = rng.random(), rng.random()
        if u + v > 1:
            u, v = 1 - u, 1 - v
        q = a0 + (b0 - a0) * u + (c0 - a0) * v
        if all((q - o).length >= spacing for o in pts):
            pts.append(q)
    return pts[len(seed_pts or []):]


# ----------------------------------------------------------------------------------------------------------------
# Hairstyle: neat undercut (long swept-back top, short faded sides and back) and a neat short beard.
# Regions live in malik_groom_regions_h9 so the painted skin and the cards always agree.
# ----------------------------------------------------------------------------------------------------------------
import numpy as np
import malik_groom_regions_h9 as R


def region_value(fn, pt, nrm=None):
    P = np.array([[pt.x, pt.y, pt.z]])
    Nn = np.array([[nrm.x, nrm.y, nrm.z]]) if nrm is not None else np.array([[0.0, 0.0, 1.0]])
    return float(fn(P, Nn)[0])


def hair_flow_top(s, n):
    lat = max(-1.0, min(1.0, s.x / 0.035)) * 0.16
    down = 1.7 * float(R.smooth(0.0, 0.045, s.y))
    return Vector((lat, 1.0, -down))


def hair_flow_side(s, n):
    back = 0.55 if s.y < -0.06 else 0.2
    return Vector((0.0, back, -1.0))


def beard_flow(s, n):
    az = float(R.azimuth(np.array([[s.x, s.y, s.z]]))[0])
    if s.z > 1.694 and abs(s.x) < 0.03 and s.y < -0.12:  # mustache: outward and down from the philtrum
        return Vector((math.copysign(0.9, s.x if abs(s.x) > 1e-4 else 1.0), -0.1, -0.7))
    if n.z < -0.3:  # under the jaw: back toward the neck
        return Vector((0.0, 0.8, -0.6))
    chin = float(R.smooth(30, 8, abs(az)))
    return Vector((0.0, 0.25 * (1 - chin) - 0.25 * chin, -1.0))


def build_groom(rng):
    mat = material_groom()
    top = CardMesh('H9 groom top')
    fade = CardMesh('H9 groom fade')
    beard = CardMesh('H9 groom beard')
    T = lambda P, N: R.top_region(P, N)

    def lift_for(q, scale):
        front = float(R.smooth(-0.02, -0.11, q.y))  # more volume at the front of the swept-back top
        peak = (0.0065 + 0.0065 * front) * scale
        return lambda t: 0.0022 + peak * math.sin(math.pi * min(1.0, t * 0.85 + 0.12))

    # Long top: an inner dense layer and an outer clumped layer, both swept back.
    roots = poisson_on_skin(rng, 55, lambda P, N: T(P, N) * np.where(P[:, 1] < 0.02, 1.0, 0.35), 0.0135)
    roots.sort(key=lambda q: -q.y)  # back first, so the front cards overlap them
    for q in roots:
        L = 0.078 * (1.0 - 0.45 * float(R.smooth(-0.02, 0.04, q.y))) * rng.uniform(0.9, 1.08)
        card(top, q, hair_flow_top, L, rng.uniform(0.021, 0.027), 7, lift_for(q, 1.0), 'top%d' % rng.randrange(6), taper=0.25)
    outer = poisson_on_skin(rng, 42, lambda P, N: T(P, N) * np.where(P[:, 1] < 0.0, 1.0, 0.15), 0.0155)
    outer.sort(key=lambda q: -q.y)
    for q in outer:
        L = 0.07 * (1.0 - 0.4 * float(R.smooth(-0.03, 0.03, q.y))) * rng.uniform(0.85, 1.05)
        card(top, q, hair_flow_top, L, rng.uniform(0.015, 0.02), 7, lift_for(q, 1.35), 'top%d' % rng.randrange(6),
             taper=0.45, twist=rng.uniform(-0.25, 0.25))
    # Front hairline: short cards that soften the edge and lift off the forehead.
    for i in range(13):
        x = -0.046 + 0.092 * (i + rng.uniform(-0.3, 0.3)) / 12
        z = float(R.front_hairline_z(x)) + 0.0035
        hit = HEAD_TREE.ray_cast(Vector((x, -0.4, z)), Vector((0, 1, 0)), 1.0)
        if hit[0] is None:
            continue
        card(top, hit[0], hair_flow_top, rng.uniform(0.032, 0.042), rng.uniform(0.016, 0.022), 5,
             lambda t: 0.0012 + 0.008 * t, 'top%d' % rng.randrange(6), taper=0.4)
    # Faded sides and back: short flat cards whose length grows toward the undercut line.
    side_roots = poisson_on_skin(rng, 140, lambda P, N: R.scalp_region(P, N) * (1.0 - 0.9 * R.top_region(P, N)), 0.0105)
    for q in side_roots:
        hh = float(R.fade_height(np.array([[q.x, q.y, q.z]]))[0])
        L = (0.010 + 0.015 * hh) * rng.uniform(0.85, 1.15)
        card(fade, q, hair_flow_side, L, rng.uniform(0.016, 0.022), 3,
             lambda t, hh=hh: 0.0007 + (0.0008 + 0.0022 * hh) * t, 'fade%d' % rng.randrange(4), taper=0.2)
    # Neat short beard: a flat base layer and a slightly lifted outer layer for volume and a soft jaw edge.
    b_roots = poisson_on_skin(rng, 175, lambda P, N: R.beard_region(P, N), 0.0085)
    for q in b_roots:
        card(beard, q, beard_flow, rng.uniform(0.010, 0.014), rng.uniform(0.012, 0.016), 3,
             lambda t: 0.0006 + 0.0016 * t, 'beard%d' % rng.randrange(6), taper=0.25)
    b_outer = poisson_on_skin(rng, 110, lambda P, N: R.beard_region(P, N) ** 2, 0.0105)
    for q in b_outer:
        card(beard, q, beard_flow, rng.uniform(0.012, 0.016), rng.uniform(0.010, 0.014), 3,
             lambda t: 0.0014 + 0.003 * t, 'beard%d' % rng.randrange(6), taper=0.35, twist=rng.uniform(-0.3, 0.3))
    return [top.build(mat), fade.build(mat), beard.build(mat)]


def material_groom():
    """The contract material 'H8R Textured groom' (Groom_Albedo.png alpha cutout plus Groom_Normal.png)."""
    m = bpy.data.materials.get('H8R Textured groom') or bpy.data.materials.new('H8R Textured groom')
    m.use_nodes = True
    nt = m.node_tree
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
    # Alpha cutout at the Unity contract threshold (0.3), not a blended transparency.
    cut = nt.nodes.new('ShaderNodeMath')
    cut.operation = 'GREATER_THAN'
    cut.inputs[1].default_value = 0.3
    nt.links.new(alb.outputs['Color'], bs.inputs['Base Color'])
    nt.links.new(alb.outputs['Alpha'], cut.inputs[0])
    nt.links.new(cut.outputs[0], bs.inputs['Alpha'])
    nt.links.new(nrm.outputs['Color'], nm.inputs['Color'])
    nt.links.new(nm.outputs['Normal'], bs.inputs['Normal'])
    nt.links.new(bs.outputs['BSDF'], out.inputs['Surface'])
    bs.inputs['Roughness'].default_value = 0.62
    bs.inputs['Specular IOR Level'].default_value = 0.35
    m.surface_render_method = 'DITHERED'
    m.use_backface_culling = True
    return m


IMAGES = {}


def image(name, non_color):
    if name in IMAGES:
        return IMAGES[name]
    path = TEX / name
    im = bpy.data.images.load(str(path), check_existing=True)
    im.colorspace_settings.name = 'Non-Color' if non_color else 'sRGB'
    IMAGES[name] = im
    return im


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
    """Front ray casts over the eye: columns where the ray enters the socket mark the opening. Returns the upper and
    lower margin as lists of skin points from the inner corner to the outer corner (by x)."""
    cols = []
    xs = [center.x + (i / 80.0 - 0.5) * 0.040 for i in range(81)]
    deep = center.y - radius * 0.55
    for x in xs:
        hits = []
        for j in range(161):
            z = center.z + (j / 160.0 - 0.5) * 0.026
            loc, nn, idx, d = HEAD_TREE.ray_cast(Vector((x, -0.5, z)), Vector((0, 1, 0)), 1.0)
            hits.append((z, loc))
        opening = [i for i, (z, loc) in enumerate(hits) if loc is None or loc.y > deep]
        if not opening:
            continue
        mid = len(hits) // 2
        if not any(abs(i - mid) < 30 for i in opening):
            continue
        lo_i, hi_i = min(opening), max(opening)
        if lo_i == 0 or hi_i == len(hits) - 1:
            continue
        cols.append((x, hits[hi_i + 1][1], hits[lo_i - 1][1]))
    cols.sort(key=lambda c: c[0])
    upper = [c[1] for c in cols if c[1] is not None]
    lower = [c[2] for c in cols if c[2] is not None]
    return upper, lower


def resample(points, count):
    if len(points) < 2:
        return points
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


def build_eye_details():
    groom = material_groom()
    cornea = material('H6 Cornea')
    lashes = CardMesh('H9 lashes')
    wet = CardMesh('H9 tear line')
    rows = []
    for index in (0, 1):
        c, r = eye_info(index)
        upper, lower = lid_margins(c, r)
        rows.append({'eye': index, 'upper_samples': len(upper), 'lower_samples': len(lower)})
        if len(upper) < 6 or len(lower) < 6:
            raise RuntimeError('Lid margin detection failed for eye %d' % index)
        sign = 1.0 if c.x > 0 else -1.0
        for kind, line in (('upper', upper), ('lower', lower)):
            pts = resample(line, 14)
            if kind == 'upper':
                pts = pts[1:-1]
            strip = []
            for k, pt in enumerate(pts):
                t = k / (len(pts) - 1)
                out = (pt - c).normalized()
                up = Vector((0, 0, 1 if kind == 'upper' else -1))
                fwd = Vector((0, -1, 0))
                mid = math.sin(math.pi * t)
                L = (0.0085 if kind == 'upper' else 0.0045) * (0.55 + 0.45 * mid)
                base = pt + out * 0.0004 + fwd * 0.0003
                tip = base + (fwd * 0.55 + up * 0.85).normalized() * L
                strip.append((base, tip))
            # Lash strip: one quad row along the lid, root edge on the margin, tip edge outward.
            u0, v0, u1, v1 = groomtex.tile_uv('lash_upper' if kind == 'upper' else 'lash_lower')
            basei = len(lashes.verts)
            nseg = len(strip) - 1
            for k, (b, tp) in enumerate(strip):
                uu = u0 + (u1 - u0) * (k / nseg)
                lashes.verts += [tuple(b), tuple(tp)]
                lashes.uvs += [(uu, v0), (uu, v1)]
            for k in range(nseg):
                i = basei + 2 * k
                if (sign > 0) == (kind == 'upper'):
                    lashes.faces.append((i, i + 2, i + 3, i + 1))
                else:
                    lashes.faces.append((i, i + 1, i + 3, i + 2))
            if kind == 'lower':
                # Wet line: a narrow strip bridging the lower lid margin and the eyeball.
                pts = resample(line, 12)[1:-1]
                basei = len(wet.verts)
                for k, pt in enumerate(pts):
                    toward = (c - pt).normalized()
                    on_ball = c + (pt - c).normalized() * (r + 0.0002)
                    wet.verts += [tuple(pt + toward * 0.0002 + Vector((0, 0, 0.0002))), tuple(on_ball + Vector((0, 0, 0.0006)))]
                    wet.uvs += [(0.5, 0.5), (0.5, 0.5)]
                for k in range(len(pts) - 1):
                    i = basei + 2 * k
                    if sign > 0:
                        wet.faces.append((i, i + 2, i + 3, i + 1))
                    else:
                        wet.faces.append((i, i + 1, i + 3, i + 2))
    objs = [lashes.build(groom), wet.build(cornea)]
    REPORT['lid_margins'] = rows
    return objs


def stage_groom():
    for o in list(bpy.data.objects):
        if o.name in ('H8R Surface hair', 'H8R Surface beard'):
            bpy.data.objects.remove(o, do_unlink=True)
    rng = random.Random(90901)
    objs = build_groom(rng) + build_eye_details()
    REPORT['groom'] = [{'object': o.name, 'vertices': len(o.data.vertices), 'triangles': sum(len(pl.vertices) - 2 for pl in o.data.polygons)} for o in objs]


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
        im.reload()
        moved.append(name)
    REPORT['textures'] = sorted(set(moved))


if 'groom' in STAGES:
    stage_groom()
stage_textures()

# ----------------------------------------------------------------------------------------------------------------
# Save.
# ----------------------------------------------------------------------------------------------------------------
for i in range(3):
    bpy.ops.outliner.orphans_purge(do_recursive=True)
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT), compress=True, relative_remap=True)
print('H9REPORT ' + json.dumps(REPORT), flush=True)
