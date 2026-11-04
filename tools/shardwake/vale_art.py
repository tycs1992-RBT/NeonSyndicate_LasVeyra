"""Second art pass for the Shardwake Vale opening. Original designs, pure numpy geometry, authored once offline.

Surface codes ride in the vertex colour alpha and are read by ValePalette.shader:
  HARD 255 stone and metal (grain channel R), WOOD 225 and LEAF 190 (channel G), SOIL 140 (channel B),
  LAMP 18 and SEAM 10 partly self-lit (warm panes, the Crown's seam), SKY 0 fully self-lit and outside the haze.
"""
import math
import random
import numpy as np
from PIL import Image
from assets import ART, Mesh, meta, guid, rgb
import land

HARD, WOOD, LEAF, SOIL, LAMP, SEAM, SKY = 255, 225, 190, 140, 18, 10, 0
GRAIN = ART/'Materials/SurfaceGrain.png'


# ------------------------------------------------------------------------------------------------ helpers
def icosphere(sub):
    t = (1 + 5 ** .5) / 2
    v = [(-1, t, 0), (1, t, 0), (-1, -t, 0), (1, -t, 0), (0, -1, t), (0, 1, t), (0, -1, -t), (0, 1, -t),
         (t, 0, -1), (t, 0, 1), (-t, 0, -1), (-t, 0, 1)]
    f = [(0, 11, 5), (0, 5, 1), (0, 1, 7), (0, 7, 10), (0, 10, 11), (1, 5, 9), (5, 11, 4), (11, 10, 2), (10, 7, 6),
         (7, 1, 8), (3, 9, 4), (3, 4, 2), (3, 2, 6), (3, 6, 8), (3, 8, 9), (4, 9, 5), (2, 4, 11), (6, 2, 10),
         (8, 6, 7), (9, 8, 1)]
    v = [np.array(p, float) / np.linalg.norm(p) for p in v]
    for _ in range(sub):
        cache = {}
        nf = []

        def mid(a, b):
            k = (min(a, b), max(a, b))
            if k not in cache:
                p = v[a] + v[b]
                v.append(p / np.linalg.norm(p))
                cache[k] = len(v) - 1
            return cache[k]
        for a, b, c in f:
            ab, bc, ca = mid(a, b), mid(b, c), mid(c, a)
            nf += [(a, ab, ca), (b, bc, ab), (c, ca, bc), (ab, bc, ca)]
        f = nf
    return v, f


def lump(m, centre, radii, tones, seed, sub=1, jitter=.2, squash=None, code=None):
    """A faceted, irregular lump. Tones run dark (underside) to light (top)."""
    rng = random.Random(seed)
    v, f = icosphere(sub)
    c = np.array(centre, float)
    r = np.array(radii, float)
    pts = []
    for p in v:
        k = 1 + rng.uniform(-jitter, jitter)
        q = p * r * k
        if squash is not None and q[1] < squash * r[1]:
            q[1] = squash * r[1] + (q[1] - squash * r[1]) * .25
        pts.append(c + q)
    old = m.code
    if code is not None:
        m.code = code
    for a, b, cc in f:
        pa, pb, pc = pts[a], pts[b], pts[cc]
        cen = (pa + pb + pc) / 3
        h = (cen - c)[1] / max(r[1], 1e-6)
        k = int(np.clip((h * .5 + .5) * len(tones) + rng.uniform(-.7, .7), 0, len(tones) - 1))
        m.tri(pa, pb, pc, tones[k], cen - c)
    m.code = old


def taper(m, a, b, ra, rb, tone, sides=6, twist=0.):
    """A tapered faceted limb from a to b (roots, trunks and branches)."""
    a, b = np.array(a, float), np.array(b, float)
    axis = b - a
    axis /= np.linalg.norm(axis)
    side = np.cross(axis, [0, 1, 0])
    if np.linalg.norm(side) < .05:
        side = np.cross(axis, [1, 0, 0])
    side /= np.linalg.norm(side)
    up = np.cross(side, axis)
    ring = lambda p, r, t: [p + r * (math.cos(t + i * math.tau / sides) * side + math.sin(t + i * math.tau / sides) * up)
                            for i in range(sides)]
    A, B = ring(a, ra, 0), ring(b, rb, twist)
    for i in range(sides):
        j = (i + 1) % sides
        m.quad(A[i], A[j], B[j], B[i], tone, (A[i] + A[j]) / 2 - a)
    for i in range(1, sides - 1):
        m.tri(B[0], B[i], B[i + 1], tone, axis)


def prism(m, rings, tones, cap_bottom=True):
    """Stacked polygons (same count) joined by broad faces: crystal slabs, piers and blocks."""
    rings = [[np.array(p, float) for p in r] for r in rings]
    for k in range(len(rings) - 1):
        A, B = rings[k], rings[k + 1]
        n = len(A)
        ca, cb = sum(A) / n, sum(B) / n
        for i in range(n):
            j = (i + 1) % n
            mid = (A[i] + A[j] + B[i] + B[j]) / 4
            axis_point = ca + (cb - ca) * .5
            out = mid - axis_point
            out[1] *= .2
            tone = tones[k][i % len(tones[k])] if isinstance(tones[k], (list, tuple)) else tones[k]
            m.quad(A[i], A[j], B[j], B[i], tone, out)
    if cap_bottom:
        A = rings[0]
        for i in range(1, len(A) - 1):
            m.tri(A[0], A[i], A[i + 1], tones[0][0] if isinstance(tones[0], (list, tuple)) else tones[0], (0, -1, 0))
    top = rings[-1]
    if np.linalg.norm(top[0] - top[len(top) // 2]) > 1e-3:
        t = tones[-1][0] if isinstance(tones[-1], (list, tuple)) else tones[-1]
        for i in range(1, len(top) - 1):
            m.tri(top[0], top[i], top[i + 1], t, (0, 1, 0))


def polygon(cx, cy, cz, radii, angle=0., lean=(0, 0)):
    n = len(radii)
    return [(cx + lean[0] + r * math.cos(angle + i * math.tau / n), cy, cz + lean[1] + r * math.sin(angle + i * math.tau / n))
            for i, r in enumerate(radii)]


# ------------------------------------------------------------------------------------------------ canopy trees
LEAVES = ['35593c', '3f6a43', '4b7a4a', '598a51', '6b9a57', '86ab62']
BARK = ['5a3f30', '6c4b37', '7d5a40']


def canopy_tree(variant):
    """Irregular broadleaf shapes for the trees nearest the player: four distinct silhouettes."""
    m = Mesh()
    rng = random.Random(2201 + variant)
    h, spread, lean, twin = [(8.6, 1.35, .25, False), (10.4, .95, -.15, False), (7.6, 1.15, 1.15, False),
                             (9.2, 1.2, -.4, True)][variant]
    m.code = WOOD
    trunks = [(0., 0.)] if not twin else [(-.55, .1), (.6, -.2)]
    tops = []
    for k, (tx, tz) in enumerate(trunks):
        lx = lean + (k - .5) * 1.2 * twin
        mid = (tx + lx * .45, h * .45, tz + rng.uniform(-.3, .3))
        top = (tx + lx, h * .70, tz + rng.uniform(-.4, .4))
        taper(m, (tx, -.35, tz), mid, .42 if not twin else .31, .30 if not twin else .22, BARK[1], 7, .3)
        taper(m, mid, top, .30 if not twin else .22, .16, BARK[2], 7, .6)
        tops.append(np.array(top))
        # Root flare: buttress roots that dive into the ground.
        for i in range(5):
            a = i * math.tau / 5 + rng.uniform(-.3, .3) + variant
            out = np.array((tx + math.cos(a) * 1.25, -.28, tz + math.sin(a) * 1.25))
            taper(m, (tx + math.cos(a) * .18, .55, tz + math.sin(a) * .18), out, .20, .06, BARK[0], 5)
    m.code = LEAF
    centre = sum(tops) / len(tops)
    blobs = 8 if variant != 1 else 7
    for i in range(blobs):
        a = i * math.tau / blobs + rng.uniform(-.35, .35)
        ring = rng.uniform(1.2, 2.6) * spread
        y = centre[1] + rng.uniform(.4, 2.9) * (1.15 if variant == 1 else .9)
        p = centre + np.array((math.cos(a) * ring, y - centre[1], math.sin(a) * ring))
        r = rng.uniform(1.35, 2.05) * (spread if variant != 1 else 1.0)
        # Branch to each lump so the canopy hangs off visible limbs, with gaps that let sunlight through.
        m.code = WOOD
        taper(m, centre + np.array((0, -.6, 0)), p + np.array((0, -.5 * r, 0)), .13, .05, BARK[2], 5)
        m.code = LEAF
        lump(m, p, (r * 1.08, r * .78, r), LEAVES, 900 + variant * 31 + i, 1, .24, squash=-.45)
    lump(m, centre + np.array((0, 2.9 if variant != 1 else 4.0, 0)), (2.2 * spread, 1.5, 2.0 * spread), LEAVES[2:],
         950 + variant, 1, .22, squash=-.5)
    return m


# ------------------------------------------------------------------------------------------------ path edge
EDGE_ROCK = ['8a8478', '9b927f', 'a69a82', '7c7a73']
COVER = ['4f7442', '5d8048', '6f8c4f', '7f9555']
MOSS = ['6c7f45', '7a8b4c', '889957']


def _trail(points):
    detail = []
    for a, b in zip(points, points[1:]):
        count = math.ceil(math.dist(a, b) / .8)
        detail.extend([tuple(np.array(a) + (np.array(b) - a) * (i / count)) for i in range(count)])
    return detail + [points[-1]]


NEAR_TRAIL = [(-7, -16), (-7, -9), (-8.5, -4), (-7.65, 1.55), (-8, 5), (-5, 8), (0, 10), (5, 14), (10, 19), (13.7, 23.6)]
FAR_TRAIL = [(-3.7, 56.4), (-3.9, 59), (-4, 63), (-10, 67), (-14, 71), (-15, 75), (-12, 79), (-5, 82), (2, 86), (7, 91),
             (11, 96), (12, 102)]


def path_edge():
    """Exposed roots, ground cover, moss and half-buried rock that break up both edges of the footpath."""
    m = Mesh()
    rng = random.Random(4417)
    for trail, width, density in [(NEAR_TRAIL, 3.7, 1.0), (FAR_TRAIL, 3.9, .55)]:
        pts = np.array(_trail(trail), float)
        for i in range(1, len(pts) - 1):
            d = pts[i + 1] - pts[i - 1]
            d /= np.linalg.norm(d)
            side = np.array((d[1], -d[0]))
            for s in (-1, 1):
                if rng.random() > .78 * density:
                    continue
                kind = rng.random()
                off = width / 2 + rng.uniform(-.35, .9)
                x, z = pts[i] + side * s * off + d * rng.uniform(-.3, .3)
                y = land.surface(x, z)
                if kind < .34:
                    r = rng.uniform(.16, .52)
                    m.code = HARD
                    lump(m, (x, y + r * .15, z), (r * rng.uniform(1, 1.5), r * .62, r), EDGE_ROCK, rng.randrange(10 ** 6), 0,
                         .28, squash=-.35)
                elif kind < .72:
                    m.code = LEAF
                    for j in range(rng.randrange(5, 9)):
                        a = rng.random() * math.tau
                        base = np.array((x + rng.uniform(-.25, .25), y - .02, z + rng.uniform(-.25, .25)))
                        tip = base + np.array((math.cos(a) * .26, rng.uniform(.28, .55), math.sin(a) * .26))
                        n = np.array((-math.sin(a), 0, math.cos(a))) * .06
                        tone = COVER[rng.randrange(len(COVER))]
                        m.tri(base - n, base + n, tip, tone, (0, 1, 0))
                        m.tri(base + n, base - n, tip, tone, (0, -1, 0))
                else:
                    # Moss mat: an irregular flat patch that creeps over the paved edge.
                    m.code = SOIL
                    inward = -s * rng.uniform(.0, .45)
                    cx, cz = np.array((x, z)) + side * inward
                    ring = []
                    for k in range(7):
                        a = k * math.tau / 7
                        rr = rng.uniform(.35, .8)
                        px, pz = cx + math.cos(a) * rr, cz + math.sin(a) * rr * .7
                        ring.append(np.array((px, land.surface(px, pz) + .045, pz)))
                    c = np.array((cx, land.surface(cx, cz) + .05, cz))
                    tone = MOSS[rng.randrange(len(MOSS))]
                    for k in range(7):
                        m.tri(c, ring[k], ring[(k + 1) % 7], tone, (0, 1, 0))
    # Two old stumps beside the near trail. Their exposed roots run out across the path edge.
    m.code = WOOD
    for sx, sz, h, toward in [(-3.9, -10.4, .72, -1), (-11.2, -3.4, .95, 1)]:
        y = land.surface(sx, sz)
        taper(m, (sx, y - .3, sz), (sx + .05, y + h, sz + .04), .55, .44, BARK[1], 8, .2)
        m.code = HARD
        lump(m, (sx + .05, y + h + .02, sz + .04), (.46, .08, .44), ['a88a62', 'b99c72'], 77 + int(sx), 0, .05)
        m.code = WOOD
        for k in range(6):
            a = k * math.tau / 6 + .4
            reach = 1.6 if math.cos(a) * toward > .3 else 1.0
            ex, ez = sx + math.cos(a) * reach * 1.4, sz + math.sin(a) * reach
            mid = ((sx + ex) / 2, land.surface((sx + ex) / 2, (sz + ez) / 2) + .12, (sz + ez) / 2)
            taper(m, (sx + math.cos(a) * .35, y + .2, sz + math.sin(a) * .35), mid, .13, .085, BARK[0], 5)
            taper(m, mid, (ex, land.surface(ex, ez) - .12, ez), .085, .03, BARK[0], 5)
    return m


# ------------------------------------------------------------------------------------------------ the bridge
STONE = ['cdb894', 'c3ac86', 'd6c3a0', 'b9a07c', 'c8b38e']
WORN = ['a8916f', '9e8a6a', '978a6c', '8e8b6b']
VAULT = ['6a5a4e', '5f5146', '72614f']


def bridge2():
    """Three Arch Bridge with barrel vaults, protruding arch rings, coursed and weathered masonry and river piers.
    The deck surface, curbs and caps are the exact walking geometry of the first pass."""
    m = Mesh()
    rng = random.Random(1717)
    deck_y = land.deck_y
    n = 32
    m.code = HARD
    for i in range(n):
        a, b = -16 + i, -15 + i
        y, yy = deck_y(a), deck_y(b)
        for x, x1 in [(-3.25, -2.62), (-2.62, 0), (0, 2.62), (2.62, 3.25)]:
            m.quad((x, y, a), (x, yy, b), (x1, yy, b), (x1, y, a),
                   ('edge' if abs(x) == 3.25 or x == 2.62 else ['d0bda0', 'c7b89d', 'cdbda5'][i % 3]), (0, 1, 0))
        m.quad((-3.25, y - .55, a), (3.25, y - .55, a), (3.25, yy - .55, b), (-3.25, yy - .55, b), 'shade', (0, -1, 0))
        for side in (-1, 1):
            x = side * 3.25
            # Deck fascia in two courses of blocks with weathered tones.
            for c0, c1 in [(0, .27), (.27, .55)]:
                tone = rng.choice(STONE if c0 == 0 else STONE[1:] + WORN[:1])
                m.quad((x, y - c0, a), (x, yy - c0, b), (x, yy - c1, b), (x, y - c1, a), tone, (side, 0, 0))
            m.beam((side * 3.04, y + .28, a), (side * 3.04, yy + .28, b), .43, .55, 'stone')
            m.beam((side * 3.04, y + .60, a), (side * 3.04, yy + .60, b), .52, .13, 'edge')

    def underside(p):
        return -5.65 + 5.8 * (math.sin(math.pi * p) ** .67)

    for arch, (a, b) in enumerate([(-16, -6), (-6, 6), (6, 16)]):
        seg = 16
        for i in range(seg):
            t, u = i / seg, (i + 1) / seg
            z, zz = a + t * (b - a), a + u * (b - a)
            v, w = underside(t), underside(u)
            top_v, top_w = deck_y(z) - .44, deck_y(zz) - .44
            for side in (-1, 1):
                x = side * 2.59
                # Spandrel walls in three courses; lower courses darker and greener with damp.
                for k in range(3):
                    f0, f1 = k / 3, (k + 1) / 3
                    lo = (v + (top_v - v) * f0, w + (top_w - w) * f0)
                    hi = (v + (top_v - v) * f1, w + (top_w - w) * f1)
                    for dx, face in [(-.46, -1), (.46, 1)]:
                        tone = rng.choice(WORN if k == 0 else STONE)
                        m.quad((x + dx, lo[0], z), (x + dx, lo[1], zz), (x + dx, hi[1], zz), (x + dx, hi[0], z), tone,
                               (face, 0, 0))
                # Protruding ring stones (voussoirs) on the outer face, alternating long and short.
                ox = side * (3.05 + .13)
                depth = .62 if i % 2 == 0 else .48
                tone = rng.choice(['dccaa7', 'cfbb95', 'c4ae88']) if i not in (seg // 2 - 1, seg // 2) else 'e2d2b0'
                m.quad((ox, v, z), (ox, w, zz), (ox, w + depth, zz), (ox, v + depth, z), tone, (side, 0, 0))
                m.quad((side * 3.05, v, z), (ox, v, z), (ox, w, zz), (side * 3.05, w, zz), 'shade', (0, -1, 0))
                m.quad((side * 3.05, v + depth, z), (side * 3.05, w + depth, zz), (ox, w + depth, zz), (ox, v + depth, z),
                       tone, (0, 1, 0))
            # Barrel vault between the ribs: the arch has real depth and a shadowed ceiling.
            for k, (x0, x1) in enumerate([(-2.13, -.71), (-.71, .71), (.71, 2.13)]):
                m.quad((x0, v, z), (x1, v, z), (x1, w, zz), (x0, w, zz), VAULT[(i + k) % 3], (0, -1, 0))
            m.quad((-3.05, v, z), (-2.13, v, z), (-2.13, w, zz), (-3.05, w, zz), 'shade', (0, -1, 0))
            m.quad((2.13, v, z), (3.05, v, z), (3.05, w, zz), (2.13, w, zz), 'shade', (0, -1, 0))
        for side in (-1, 1):
            x = side * 2.58
            m.rings([(x, -13, a, 1.13, 1.5), (x, -5, a, .84, 1.06), (x, deck_y(a) - .36, a, .76, .81)],
                    ['stone', 'shade', 'sand', 'stone', 'shade', 'stone'], .3)
    # River piers with pointed cutwaters under the two inner springings.
    for z in (-6, 6):
        rings = []
        for y, s in [(-13, 1.08), (-8.9, 1.0), (-5.35, .96)]:
            rings.append([(-4.45 * s, y, z), (-3.15, y, z - 1.05 * s), (3.15, y, z - 1.05 * s), (4.45 * s, y, z),
                          (3.15, y, z + 1.05 * s), (-3.15, y, z + 1.05 * s)])
        prism(m, rings, [WORN, ['b39d7a', 'a8916f', 'bba684'], ['c8b38e']])
    for z in [-15.55, -6, 6, 15.55]:
        for side in (-1, 1):
            x = side * 3.03
            y = deck_y(z)
            m.rings([(x, y, z, .48, .50), (x, y + .82, z, .39, .43), (x, y + 1.08, z, .44, .46), (x, y + 1.23, z, .32, .33)],
                    ['stone', 'sand', 'edge', 'stone', 'sand', 'edge'], .5)
    return m


# ------------------------------------------------------------------------------------------------ the Split Crown
ORE_DARK = ['3f3250', '4a3a5e', '54426a']
ORE_MID = ['6a5385', '765e92', '7f669c']
ORE_LIGHT = ['9c83ba', 'a88fc6', 'b49cd1']
INNER = ['33283f', '3b2e49']


def crown2():
    """A bold split monolith: two leaning crystal slabs with broad faces around a narrow cleft and a restrained
    luminous seam. The stone seat, compression collar and copper struts of the first pass stay in place."""
    m = Mesh()
    rng = random.Random(6061)
    m.code = HARD
    # Left half: the taller slab, leaning west.  Right half: shorter, leaning east.
    halves = [
        dict(c=(-2.15, .2), radii=[3.1, 2.7, 2.2, 2.9, 3.2], ang=.35, h=[-.6, 6.4, 13.8, 21.8], lean=-4.8, inner=4),
        dict(c=(3.0, .1), radii=[2.6, 2.3, 2.5, 2.1, 2.4], ang=3.6, h=[-.6, 5.4, 11.4, 17.2], lean=4.0, inner=4),
    ]
    for half in halves:
        cx, cz = half['c']
        rings = []
        shrink = [1, .9, .68, .2]
        for k, y in enumerate(half['h']):
            lean = half['lean'] * (max(0., y) / half['h'][-1]) ** 1.25
            rad = [r * shrink[k] for r in half['radii']]
            rings.append(polygon(cx, y, cz, rad, half['ang'], (lean, 0)))
        tones = [ORE_DARK, ORE_MID, ORE_LIGHT, ORE_LIGHT]
        tones = [[t[(i + k) % 3] for i in range(5)] for k, t in enumerate(tones)]
        # The faces looking into the cleft are darker.
        for k in range(len(tones)):
            tones[k][half['inner']] = INNER[k % 2]
        prism(m, rings, tones)
    # Restrained luminous seam: a thin blade of light deep in the cleft, brighter low and fading toward the tips.
    m.code = SEAM
    pts = []
    for k in range(11):
        y = .4 + k * 1.3
        x = .42 + .08 * k + .03 * math.sin(k * 1.7)
        w = max(.05, .34 - .029 * k)
        pts.append((x, y, w))
    for (x0, y0, w0), (x1, y1, w1) in zip(pts, pts[1:]):
        tone = 'efe1ff' if y0 < 5 else 'd9c4f6'
        for zc in (.18, .22):
            a, b = np.array((x0 - w0, y0, zc)), np.array((x0 + w0, y0, zc))
            c, d = np.array((x1 + w1, y1, zc)), np.array((x1 - w1, y1, zc))
            m.quad(a, b, c, d, tone, (0, 0, -1 if zc < .2 else 1))
    # Faceted satellites at the base, each with broad faces, leaning away from the monolith.
    m.code = HARD
    for x, z, h, lean, r in [(-4.3, 2.6, 4.4, -1.1, .95), (4.2, 2.2, 3.4, 1.0, .8), (-.6, -2.4, 4.9, -.3, .9),
                             (1.6, 2.7, 2.6, .6, .6)]:
        rings = [polygon(x, -.4, z, [r, r * .85, r * .95, r * .8], rng.random() * 3),
                 polygon(x, h * .55, z, [r * .72, r * .6, r * .7, r * .55], rng.random() * 3, (lean * .45, 0)),
                 polygon(x, h, z, [.05, .04, .05, .04], 1, (lean, 0))]
        prism(m, rings, [ORE_DARK, ORE_MID, ORE_LIGHT])
    # Broad stone seat and broken compression collar hold the cleft under load (first-pass story pieces).
    m.rings([(.38, -.07, .45, 2.24, 1.95), (.38, .58, .45, 2.10, 1.85), (.38, .93, .45, 1.81, 1.53)],
            ['basalt', 'ore', 'shade', 'basalt', '745d77', 'ore', 'shade', 'basalt'], .23)
    for side in [-1, 1]:
        m.beam((side * 1.41, .69, -.64), (side * 1.00 + .23, 3.35, -.71), .29, .32, 'basalt')
        m.beam((side * 1.02 + .23, 3.33, -.73), (side * .62 + .24, 3.58, -.80), .29, .24, 'copper')
    return m


# ------------------------------------------------------------------------------------------------ the three collars
def collar(variant):
    """Regulator stands with three distinct collars: ember fins (1), patinated plates (2), twin twisted rings (3)."""
    m = Mesh()
    m.code = HARD
    m.rings([(0, -.23, 0, 1.04, .93), (0, .21, 0, .88, .8), (0, .68, 0, .60, .58), (0, .79, 0, .68, .64)],
            ['stone', 'edge', 'shade', 'stone', 'brass', 'stone'])
    if variant == 0:
        m.rings([(0, .79, 0, .62, .6), (0, 1.02, 0, .62, .6)], ['brass'] * 9)
        for i in range(3):
            a = i * math.tau / 3 + .5
            p = np.array((math.cos(a) * .5, .85, math.sin(a) * .5))
            m.beam(p, p * np.array((1.25, 1, 1.25)) + np.array((0, .85, 0)), .07, .2, 'f2ad56')
        m.code = LAMP
        m.rings([(0, 1.02, 0, .5, .48), (0, 1.07, 0, .5, .48)], ['ffbb60'] * 9)
    elif variant == 1:
        m.code = HARD
        for i in range(4):
            a = i * math.tau / 4 + .2
            c = np.array((math.cos(a) * .58, 1.05, math.sin(a) * .58))
            m.beam(c - np.array((0, .3, 0)), c + np.array((0, .38, 0)), .34, .07, 'b56b43' if i % 2 else '4b7974')
        m.rings([(0, .82, 0, .63, .6), (0, .95, 0, .63, .6)], ['4b7974'] * 8)
    else:
        for tilt in (-1, 1):
            ring = []
            for i in range(13):
                a = i * math.tau / 12
                ring.append(np.array((math.cos(a) * .6, .98 + tilt * .14 * math.sin(a), math.sin(a) * .6)))
            for p, q in zip(ring, ring[1:]):
                m.beam(p, q, .09, .09, 'a27fa9' if tilt > 0 else 'c08a52')
    for s in [-1, 1]:
        m.code = HARD
        m.beam((s * .48, .7, .02), (s * .55, 1.57, .12), .13, .14, 'copper')
    return m


# ------------------------------------------------------------------------------------------------ ridges and sky
RIDGES = [  # z, base height, amplitude, palette (near to far: warm sage, sage-blue, blue-grey, pale with snow)
    (128, 9, 12, ['8f9a66', '98a06c', '879566', '8d9a70']),
    (152, 13, 18, ['8f9d8c', '869a8f', '96a798', '8b9b94']),
    (186, 19, 27, ['8d9bb2', '8392ab', '9aa6bb', '9087ad']),
    (228, 27, 42, ['a6b1c6', 'b0b9cc', '9fa8c2', 'a59cc2']),
]


def ridge2(band):
    m = Mesh()
    m.code = SOIL
    rng = random.Random(4300 + band)
    z0, base, amp, palette = RIDGES[band]
    cols = 41
    rows = []
    for i in range(cols):
        x = -205 + i * 10.25
        s = base + amp * (.55 + .45 * math.sin(i * .41 + band * 1.3)) * (.75 + .25 * math.cos(i * 1.37 + band))
        # Keep a deep, wide notch on the spawn's line of sight through the Split Crown, so its tips meet sky
        # and the ranges frame it from both sides instead of towering behind it.
        notch = -7.49 + 19.49 * (z0 + 18.49) / 122.49
        s -= (amp * .95) * math.exp(-((x - notch) / (22 + band * 9)) ** 2)
        s += rng.uniform(-.12, .12) * amp
        s = max(base * .6, s)
        rows.append([(x, -6, z0 - 16), (x + rng.uniform(-2, 2), s * .45, z0 - 7),
                     (x + rng.uniform(-3, 3), s * .82, z0 - 1), (x + rng.uniform(-3, 3), s, z0 + 4)])
    for a, b in zip(rows, rows[1:]):
        for k in range(3):
            tone = palette[rng.randrange(4)]
            if band == 3 and k == 2 and b[k + 1][1] > base + amp * .75:
                tone = 'e6e9ef'  # snow on the highest far crests
            m.quad(a[k], b[k], b[k + 1], a[k + 1], tone, (0, .35, -1))
    return m


def sky_dome(to_sun):
    """A painted sky: warm pale horizon on the sun side, cool blue overhead. Self-lit, outside the haze."""
    m = Mesh()
    m.code = SKY
    centre = np.array((0., -40., 60.))
    radius = 420
    sun_az = math.atan2(to_sun[2], to_sun[0])
    rows, cols = 26, 48
    step, span = (math.pi / 2 + .08) / rows, math.tau / cols
    def point(i, j):
        el = -.08 + (i / rows) * (math.pi / 2 + .08)
        az = j * math.tau / cols
        return centre + radius * np.array((math.cos(el) * math.cos(az), math.sin(el), math.cos(el) * math.sin(az))), el, az
    def tone(el, az):
        warm = max(0., math.cos(az - sun_az)) ** 2
        h = max(0., el) / (math.pi / 2)
        horizon = np.array(rgb('d9dcd8')) * (1 - warm) + np.array(rgb('eedcc0')) * warm
        top = np.array(rgb('6f9cc8'))
        c = horizon * (1 - h ** .55) + top * h ** .55
        return '%02x%02x%02x' % tuple(int(x) for x in np.clip(c, 0, 255))
    for i in range(rows):
        for j in range(cols):
            p, el, az = point(i, j)
            q, _, _ = point(i + 1, j)
            r, _, _ = point(i + 1, j + 1)
            s, _, _ = point(i, j + 1)
            mid = (p + q + r + s) / 4
            t = [tone(e, z) for e, z in ((el, az), (el + step, az), (el + step, az + span), (el, az + span))]
            m.shaded(p, s, r, t[:1] + t[3:] + t[2:3], centre - mid)
            m.shaded(p, r, q, [t[0], t[2], t[1]], centre - mid)
    return m


# ------------------------------------------------------------------------------------------------ surface grain tile
def grain_texture(size=256):
    """One tiling grain tile: R stone grain with fine cracks, G leaf clusters, B soil speckle. Mean 0.5."""
    rng = np.random.default_rng(812)
    f = np.fft.fftfreq(size)
    fx, fy = np.meshgrid(f, f)
    radius = np.sqrt(fx ** 2 + fy ** 2) + 1e-6

    def field(beta, lo=0., hi=1.):
        noise = rng.normal(size=(size, size))
        spec = np.fft.fft2(noise) / radius ** beta
        spec[(radius < lo) | (radius > hi)] = 0
        out = np.real(np.fft.ifft2(spec))
        return (out - out.mean()) / (out.std() + 1e-9)

    stone = .6 * field(1.2) + .4 * field(.4)
    cracks = np.abs(field(1.6))
    stone -= 1.4 * np.clip(.18 - cracks, 0, None) / .18
    leaf = .7 * field(.9, .02, .25) + .3 * field(.2)
    soil = .5 * field(.3) + .5 * field(1.4)
    channels = []
    for ch, spread in [(stone, .14), (leaf, .16), (soil, .12)]:
        channels.append(np.clip(.5 + ch * spread, 0, 1))
    img = (np.stack(channels, -1) * 255 + .5).astype(np.uint8)
    Image.fromarray(img, 'RGB').save(GRAIN, optimize=True)
    dest = GRAIN.parent / (GRAIN.name + '.meta')
    if not dest.exists():
        dest.write_text('fileFormatVersion: 2\nguid: ' + guid(GRAIN) + '\nTextureImporter:\n  externalObjects: {}\n'
                        '  serializedVersion: 13\n  mipmaps:\n    mipMapMode: 0\n    enableMipMap: 1\n    sRGBTexture: 0\n'
                        '  bumpmap:\n    convertToNormalMap: 0\n    externalNormalMap: 0\n  isReadable: 0\n  textureType: 0\n'
                        '  textureShape: 1\n  maxTextureSize: 256\n  textureSettings:\n    serializedVersion: 2\n'
                        '    filterMode: 2\n    aniso: 4\n    mipBias: 0\n    wrapU: 0\n    wrapV: 0\n    wrapW: 0\n'
                        '  alphaSource: 0\n  alphaIsTransparency: 0\n  userData: Shardwake Vale surface grain\n'
                        '  assetBundleName:\n  assetBundleVariant:\n')
    return GRAIN
