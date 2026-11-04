"""Dante's workshop up close: fitted stone columns, a worn timber frame that visibly carries the swept roof, flagstones
on the apron, banked embers in the kiln mouth and their warm light on the stones in front of it.

These are drawing-only overlays in the workshop's local frame. The original hall, apron and roof keep their colliders
(the overlays sit a few centimetres outside them), so walking, talking to Dante and the furnace clamp are unchanged.
Never run on scene load or during Play.
"""
import math
import random
import numpy as np
from assets import Mesh, rgb
from vale_art import HARD, LAMP, prism, polygon, lump

COLUMNS = [(-4.4, -4.0), (-.5, -4.0), (3.55, -4.0), (3.55, .95), (3.55, 4.0)]
STONE = ['cbb692', 'c0aa86', 'd5c29e', 'b9a27e', 'c6b08c', 'ad977a']
TIMBER = ['7a5a40', '6c4f39', '86654a', '5f4634']
KILN = np.array((0., .9, 1.35))
WARM = np.array(rgb('ff9a48'), float)
FLOOR_Y = .24


def eave(x, z):
    """The swept roof's underside height function from land.workshop (kept identical)."""
    y = (6.16 + (x + 1.7) * .46 if x < -1.7 else 6.16 - (x + 1.7) * .245)
    return y + .29 * math.cos(z * .47) + .085 * (z + .8)


def _bake_warm(m, start, radius=4.6, strength=.55):
    """Kiln light on nearby surfaces: blend toward warm by distance to the kiln mouth and by facing it."""
    for i in range(start, len(m.v)):
        key = m.v[i]
        p = np.array(key[:3]); n = np.array(key[3:6])
        d = np.linalg.norm(p - KILN)
        if d > radius:
            continue
        facing = max(.25, float(np.dot(n, (KILN - p) / max(d, 1e-6))))
        k = strength * (1 - d / radius) ** 2 * facing
        col = np.array(key[6:9], float) * (1 - k) + WARM * k
        m.v[i] = key[:6] + tuple(int(min(255, v)) for v in col) + key[9:]


def columns():
    """Drum-built stone columns with recessed joints, a square plinth, a chamfered capital and a timber bolster."""
    m = Mesh()
    m.code = HARD
    rng = random.Random(4471)
    for cx, cz in COLUMNS:
        # Plinth.
        prism(m, [polygon(cx, .1, cz, [.86] * 4, math.pi / 4), polygon(cx, .38, cz, [.82] * 4, math.pi / 4),
                  polygon(cx, .44, cz, [.74] * 4, math.pi / 4)], [['b39d7b'], ['c2ac88'], ['c9b48f']])
        y = .44
        while y < 3.55:
            h = rng.uniform(.36, .52)
            top = min(3.6, y + h)
            def radius(yy):
                return .66 - (.66 - .45) * (yy - .44) / (3.6 - .44)
            tone = STONE[rng.randrange(len(STONE))]
            dark = '%02x%02x%02x' % tuple(int(v * .82) for v in rgb(tone))
            ang = rng.random() * .4
            r0, r1 = radius(y), radius(top)
            prism(m, [polygon(cx, y + .015, cz, [r0 - .035] * 8, ang), polygon(cx, y + .05, cz, [r0] * 8, ang),
                      polygon(cx, top - .05, cz, [r1] * 8, ang), polygon(cx, top - .015, cz, [r1 - .035] * 8, ang)],
                  [[dark], [tone], [tone], [dark]], cap_bottom=False)
            y = top
        # Capital: a chamfered block, then a timber bolster under the plate beam.
        prism(m, [polygon(cx, 3.58, cz, [.5] * 8, .2), polygon(cx, 3.72, cz, [.66] * 8, .2),
                  polygon(cx, 3.98, cz, [.7] * 8, .2), polygon(cx, 4.04, cz, [.62] * 8, .2)],
              [['b9a27e'], ['cbb692'], ['d5c29e'], ['c0aa86']])
        m.code = 225
        m.beam((cx - .55, 4.16, cz), (cx + .55, 4.16, cz), .26, .5, TIMBER[0])
        m.code = HARD
    _bake_warm(m, 0, 5.2, .35)
    return m


def timber():
    """Plate beams on the column heads, knee braces, rafters under both roof slopes and purlins: the roof's frame."""
    m = Mesh()
    m.code = 225
    rng = random.Random(9031)
    def tone():
        return TIMBER[rng.randrange(len(TIMBER))]
    # Plate beam along the open east side and the back row, resting on the bolsters.
    m.beam((3.55, 4.42, -4.75), (3.55, 4.42, 4.75), .34, .44, TIMBER[1])
    m.beam((-4.95, 4.42, -4.0), (3.9, 4.42, -4.0), .34, .44, TIMBER[1])
    # Knee braces from each front column into the plate beam.
    for cx, cz in COLUMNS[2:]:
        for s in (-1, 1):
            if abs(cz + s * .95) > 4.7:
                continue
            m.beam((cx, 3.5, cz + s * .1), (cx, 4.25, cz + s * .95), .16, .16, tone())
    # Rafters every 0.8 m, from the ridge to past the eave on the east slope and down the west slope.
    for k in range(14):
        z = -5.2 + k * .8
        a = np.array((-1.7, eave(-1.7, z) - .42, z)); b = np.array((5.3, eave(5.3, z) - .42, z))
        m.beam(a, b, .14, .22, tone())
        c = np.array((-5.6, eave(-5.6, z) - .42, z))
        m.beam(c, a, .14, .22, tone())
    # Purlins across the rafters.
    for x in (.4, 2.4, 4.4, -3.6):
        m.beam((x, eave(x, -5.3) - .58, -5.3), (x, eave(x, 5.3) - .58, 5.3), .16, .16, TIMBER[3])
    return m


def flagstones():
    """Fitted, slightly raised flagstones over the apron with mortar gaps, warmed by the kiln in front of it."""
    m = Mesh()
    m.code = HARD
    rng = random.Random(6610)
    cell = .72
    xs = np.arange(-4.4, 6.3, cell); zs = np.arange(-6.1, 6.1, cell)
    jit = {(i, j): (rng.uniform(-.2, .2) * cell, rng.uniform(-.2, .2) * cell)
           for i in range(len(xs) + 1) for j in range(len(zs) + 1)}
    def corner(i, j):
        jx, jz = jit[(i, j)]
        return np.array((-4.4 + i * cell + jx, 0., -6.1 + j * cell + jz))
    for i in range(len(xs)):
        for j in range(len(zs)):
            pts = [corner(i, j), corner(i + 1, j), corner(i + 1, j + 1), corner(i, j + 1)]
            c = sum(pts) / 4
            if np.hypot(c[0], c[2]) > 6.15 or (c[0] < -4.2):
                continue
            if rng.random() < .25 and i + 1 < len(xs):
                pass  # long stones: leave as cut, the jitter already breaks the grid
            inset = [c + (p - c) * .9 for p in pts]
            top = [p + np.array((0, FLOOR_Y + .03 + rng.uniform(-.006, .006), 0)) for p in inset]
            low = [c + (p - c) * .96 + np.array((0, FLOOR_Y + .002, 0)) for p in pts]
            tone = STONE[rng.randrange(len(STONE))]
            if rng.random() < .2:
                tone = '%02x%02x%02x' % tuple(int(v * .86) for v in rgb(tone))
            m.quad(*top, tone, (0, 1, 0))
            for k in range(4):
                a, b = top[k], top[(k + 1) % 4]
                la, lb = low[k], low[(k + 1) % 4]
                m.quad(a, b, lb, la, 'a08d70', (a + b) / 2 - c)
    _bake_warm(m, 0, 4.8, .6)
    return m


def embers():
    """Banked embers in the kiln mouth: a low bed of partly self-lit coals."""
    m = Mesh()
    m.code = LAMP
    rng = random.Random(77)
    for k in range(14):
        x = rng.uniform(-.75, .75); z = rng.uniform(1.25, 1.95)
        r = rng.uniform(.07, .14)
        lump(m, (x, .64 + r * .4, z), (r * 1.3, r * .7, r), ['ff8a3a', 'f26a2a', 'ffb060', 'c4482a'], rng.randrange(10 ** 6), 0, .25,
             code=LAMP)
    return m


def pebbles(spots):
    """Small stones in loose groups beside the first steps of the path (world space)."""
    import land
    m = Mesh()
    m.code = HARD
    rng = random.Random(2290)
    for x, z, radius, count in spots:
        for k in range(count):
            a = rng.random() * math.tau; r = radius * math.sqrt(rng.random())
            px, pz = x + math.cos(a) * r, z + math.sin(a) * r
            s = rng.uniform(.06, .2)
            lump(m, (px, land.surface(px, pz) + s * .2, pz), (s * rng.uniform(1, 1.6), s * .6, s),
                 ['9a9182', '8a8478', 'a8a090', '7d786f'], rng.randrange(10 ** 6), 0, .3, squash=-.3)
    return m
