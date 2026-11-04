"""Fractured rock: a small family of broken blocks with broad faces, chipped corners, layered edges and a darker wet
base, for the riverbank seen through the first arch and the bedrock shoulder on the far meadow. Each rock is the
convex hull of a cut box (flat broad faces), optionally stacked in two or three offset layers like bedded stone.
Never run on scene load or during Play.
"""
import math
import random
import numpy as np
from scipy.spatial import ConvexHull
from assets import Mesh, rgb

WET = np.array(rgb('3a4044'), float)
FAMILY = [['9a8f80', 'a89c8a', '8c8275', 'b3a690'], ['8e877d', '9d958a', '7f7a72', 'aaa194'],
          ['a3917a', 'b09e85', '958571', 'bba88e']]


def _block(rng, size, chip):
    """Corner points of a box with every corner chipped into three points, so hull faces include chamfers."""
    sx, sy, sz = size
    pts = []
    for cx in (-1, 1):
        for cy in (-1, 1):
            for cz in (-1, 1):
                corner = np.array((cx * sx, cy * sy, cz * sz)) * rng.uniform(.85, 1.08)
                for axis in range(3):
                    p = corner.copy()
                    p[axis] -= np.sign(p[axis]) * chip * rng.uniform(.4, 1.3) * size[axis]
                    pts.append(p)
    # A few extra cuts so faces are irregular.
    for k in range(4):
        d = np.array([rng.gauss(0, 1) for _ in range(3)]); d /= np.linalg.norm(d)
        pts.append(d * np.array(size) * rng.uniform(.75, .95))
    return np.array(pts)


def fractured(m, centre, size, yaw, layers, tones, rng, water_y=None, tilt=0.):
    """One fractured rock (one to three bedded layers) added to mesh m, world-space centre at the base."""
    centre = np.array(centre, float)
    c, s = math.cos(yaw), math.sin(yaw)
    rot = np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
    ct, st = math.cos(tilt), math.sin(tilt)
    rot = rot @ np.array([[1, 0, 0], [0, ct, -st], [0, st, ct]])
    y = 0.
    for layer in range(layers):
        h = size[1] / layers * rng.uniform(.8, 1.2)
        shrink = 1 - .14 * layer
        sz = (size[0] * shrink * rng.uniform(.9, 1.05), h / 2, size[2] * shrink * rng.uniform(.9, 1.05))
        pts = _block(rng, sz, rng.uniform(.12, .3))
        offset = np.array((rng.uniform(-.12, .12) * size[0], y + h / 2, rng.uniform(-.12, .12) * size[2]))
        pts = (pts + offset) @ rot.T + centre
        hull = ConvexHull(pts)
        mid = pts.mean(0)
        tone_set = tones
        for k, simplex in enumerate(hull.simplices):
            a, b, cc = pts[simplex]
            n = hull.equations[k][:3]
            # Broad faces keep one tone per hull plane; top faces paler, undersides darker.
            plane = tuple(np.round(hull.equations[k], 3))
            tone = np.array(rgb(tone_set[hash(plane) % len(tone_set)]), float)
            tone *= 1.08 if n[1] > .6 else .78 if n[1] < -.3 else 1.
            if water_y is not None:
                wet = min(1., max(0., (water_y + .45 - (a[1] + b[1] + cc[1]) / 3) / .55))
                tone = tone * (1 - .65 * wet) + WET * .65 * wet
            hexc = '%02x%02x%02x' % tuple(int(max(0, min(255, v))) for v in tone)
            m.tri(a, b, cc, hexc, (a + b + cc) / 3 - mid)
        y += h * .92
    return m


def bank_stretch():
    """The riverbank seen through the first arch (both waterlines, x -18 to 18): rocks set into the bank and rocks
    breaking the waterline. Returns (mesh, [(x, z, size)] of the rocks in the current, for the water's response)."""
    import vale_ravine as rv
    m = Mesh()
    m.code = 255
    rng = random.Random(2468)
    breaking = []
    for k in range(26):
        far = k % 3 != 0
        x = -17 + 34 * (k + rng.random() * .6) / 26
        span = rv.FAR if far else rv.NEAR
        tones = FAMILY[k % 3]
        if k % 4 == 1:
            # In the current: base below the surface, top a little above it.
            z = rv.wall_z(x, rv.WATER_Y - .6, span, not far) + (-1 if far else 1) * rng.uniform(.6, 1.6)
            size = (rng.uniform(.7, 1.3), rng.uniform(1.2, 1.7), rng.uniform(.6, 1.1))
            fractured(m, (x, rv.WATER_Y - .9, z), size, rng.random() * 3, 2, tones, rng, rv.WATER_Y, rng.uniform(-.12, .12))
            breaking.append((x, z, max(size[0], size[2])))
        else:
            # Set into the bank: half buried in the wall at or just above the waterline.
            y = rv.WATER_Y + rng.uniform(-.2, 1.6)
            z = rv.wall_z(x, y, span, not far)
            size = (rng.uniform(.8, 1.8), rng.uniform(.9, 1.8), rng.uniform(.7, 1.4))
            fractured(m, (x, y - size[1] * .45, z), size, rng.random() * 3, rng.choice((1, 2, 3)), tones, rng, rv.WATER_Y,
                      rng.uniform(-.25, .25))
    return m, breaking


def shoulder_outcrop():
    """Exposed bedrock on the steep face of the far meadow's shoulder (bedded, tilted slabs set into the slope)."""
    import land
    m = Mesh()
    m.code = 255
    rng = random.Random(1357)
    for k in range(16):
        x = rng.uniform(-6.5, 8.5)
        # Walk down the face from the shoulder top to where it is steep.
        z = 74 - rng.uniform(2.2, 5.5)
        y = land.surface(x, z)
        size = (rng.uniform(1.2, 2.6), rng.uniform(1.0, 2.2), rng.uniform(.9, 1.8))
        fractured(m, (x, y - size[1] * .5, z), size, rng.uniform(-.4, .4), rng.choice((2, 3)), FAMILY[k % 3], rng,
                  None, rng.uniform(-.35, -.1))
    return m


def talus(variant):
    """The 'layered shelf' crags (solid, same mesh for drawing and collision): three bedded fractured blocks in a
    row, about the footprint of the earlier rounded crags so their placement and collision footprint barely change."""
    m = Mesh()
    m.code = 255
    rng = random.Random(1301 + variant)
    for i in range(3):
        x = (i - 1) * 1.4
        z = rng.uniform(-.45, .45)
        h = rng.uniform(.9, 1.7)
        fractured(m, (x, -.35, z), (1.25, h + .35, 1.0), rng.uniform(-.3, .3), rng.choice((2, 3)), FAMILY[(i + variant) % 3],
                  rng, None, rng.uniform(-.15, .15))
    return m
