"""The ravine under Three Arch Bridge, carved by water: stepped rock shelves with distinct strata, broken lips,
soil pockets and moss in the sheltered backs, a dark wet band at the waterline, shoreline boulders, and a river
surface that is deeper in the middle, paler in the shallows and streaked with foam behind the piers and rocks.

All of it is decoration over the unchanged terrain collider (no colliders here), so walking, the crossing and the
fall-back checkpoint behave exactly as before. Never run on scene load or during Play.
"""
import math
import random
import numpy as np
from assets import Mesh, rgb
import land
from vale_art import HARD, SOIL, lump

WATER_Y = -8.4
STRATA = ['b49b7b', 'a68b6f', 'c3a985', '8f8279', '9d8068', 'baa68d', '7f756f', 'aa9278']
MOSS = np.array(rgb('5f7a46'), float)
SOIL_TONE = np.array(rgb('6f5640'), float)
WET = np.array(rgb('3b4448'), float)
NEAR = (4.0, 37.0)    # near wall: the surface falls as z grows (the ravine bites back toward the shelf near x 0)
FAR = (37.0, 62.0)    # far wall: the surface rises as z grows


def wall_z(x, y, span, falling):
    """The z where the terrain surface at x reaches height y on one wall (binary search on the collider)."""
    lo, hi = span
    for _ in range(26):
        mid = (lo + hi) / 2
        above = land.surface(x, mid) > y
        if above == falling:
            lo = mid
        else:
            hi = mid
    return (lo + hi) / 2


def _noise(x, seed):
    return (math.sin(x * .37 + seed) + .6 * math.sin(x * .91 + seed * 1.7) + .35 * math.sin(x * 2.3 + seed * .3)) / 1.95


def _tone(hex_tone, y, shade=1., moss=0., soil=0.):
    c = np.array(rgb(hex_tone), float) * shade
    c = c * (1 - moss) + MOSS * moss
    c = c * (1 - soil) + SOIL_TONE * soil
    wet = min(1., max(0., (WATER_Y + 1.25 - y) / 1.0))
    c = c * (1 - .7 * wet) + WET * .7 * wet
    return '%02x%02x%02x' % tuple(int(max(0, min(255, v))) for v in c)


def strata(x0=-58., x1=66., step=1.4):
    """Stepped shelves on both walls. Returns (mesh, soil pockets for ground cover as (x, z, radius, clumps))."""
    m = Mesh()
    m.code = HARD
    rng = random.Random(3307)
    pockets = []
    for span, falling, seed in [(NEAR, True, 1.3), (FAR, False, 4.1)]:
        out = np.array([0, 0, 1. if falling else -1.])
        top = max(land.surface(x, span[0] if falling else span[1]) for x in range(-60, 70, 5)) - .25
        levels = []
        y = WATER_Y - .9
        while y < top - .6:
            levels.append(y)
            y += rng.uniform(1.25, 1.85)
        xs = np.arange(x0, x1 + 1e-6, step)
        for k, y in enumerate(levels):
            layer = STRATA[(k * 3 + int(seed * 2)) % len(STRATA)]
            thick = (levels[k] - levels[k - 1]) * 1.08 if k else 1.3
            rows = []
            for i, x in enumerate(xs):
                rim = min(land.surface(x, span[0] if falling else span[1]), land.surface(x, span[0] + 6 if falling else span[1] - 6))
                if y > rim - .5:
                    rows.append(None)  # this shelf would stand above the local rim
                    continue
                n = _noise(x, seed + k * 2.1)
                lip = max(0., .35 + .55 * n + rng.uniform(-.12, .12))
                if rng.random() < .07:
                    lip = .02  # broken off: the shelf drops back to the wall here
                yy = y + .22 * _noise(x * 1.7, seed + k) + rng.uniform(-.05, .05)
                z_top = wall_z(x, yy, span, falling)
                z_bot = wall_z(x, yy - thick, span, falling)
                slope = abs(land.surface(x, z_top - .8) - land.surface(x, z_top + .8)) / 1.6
                if slope < .55:
                    rows.append(None)  # a gentle meadow slope, not the carved wall
                    continue
                front_top = np.array([x + rng.uniform(-.2, .2), yy, z_top]) + out * lip
                front_bot = np.array([x, yy - thick, z_bot]) + out * (.12 + .25 * max(0, n))
                back = np.array([x, yy + .05, z_top]) - out * .7
                under = front_bot - out * .45 + np.array([0, .25, 0])
                rows.append((front_top, front_bot, back, under, lip, n))
            for (a, b) in zip(rows, rows[1:]):
                if a is None or b is None:
                    continue
                ft0, fb0, bk0, un0, lip0, n0 = a
                ft1, fb1, bk1, un1, lip1, n1 = b
                ymid = (ft0[1] + ft1[1]) / 2
                shade = .92 + .1 * n0
                # Steep face: the strata band, mossy in the sheltered lower half under a deep lip.
                moss = .35 if (lip0 > .55 and ymid > WATER_Y + .6 and rng.random() < .5) else 0.
                face_mid = (ft0 + ft1 + fb0 + fb1) / 4
                upper = (ft0 + ft1) / 2 * .5 + face_mid * .5
                m.quad(ft0, ft1, (ft1 + fb1) / 2, (ft0 + fb0) / 2, _tone(layer, ymid, shade), out + np.array([0, .25, 0]))
                m.quad((ft0 + fb0) / 2, (ft1 + fb1) / 2, fb1, fb0, _tone(layer, ymid - thick / 2, shade * .86, moss), out)
                # Shelf top: paler, sometimes a pocket of soil that plants have found.
                pocket = ymid > WATER_Y + 1.2 and rng.random() < .16
                m.quad(bk0, bk1, ft1, ft0, _tone(layer, ymid, 1.12, .12 if not pocket else 0, .65 if pocket else 0),
                       np.array([0, 1., 0]))
                if pocket:
                    c = (ft0 + ft1 + bk0 + bk1) / 4
                    pockets.append((float(c[0]), float(c[2]), .55, rng.randrange(2, 5)))
                # Undercut below the lip, dark.
                if lip0 > .3:
                    m.quad(fb0, fb1, un1, un0, _tone(layer, ymid - thick, .55, .2), np.array([0, -1., 0]) + out * .3)
            del rows
    return m, pockets


def shore_rocks():
    """Boulders along both waterlines; their lower halves are dark and wet."""
    m = Mesh()
    m.code = HARD
    rng = random.Random(8812)
    for k in range(34):
        far = k % 2 == 1
        x = rng.uniform(-50, 60)
        if -19 < x < 19:
            continue  # the stretch through the first arch uses the fractured family (vale_rocks.bank_stretch)
        z = wall_z(x, WATER_Y + rng.uniform(-.3, .4), FAR if far else NEAR, not far)
        z += rng.uniform(-1.2, .8) * (-1 if far else 1)
        r = rng.uniform(.5, 1.5)
        y = WATER_Y + r * rng.uniform(-.1, .35)
        before = len(m.v)
        lump(m, (x, y, z), (r * rng.uniform(1.1, 1.7), r * .75, r), ['7d746c', '8a8076', '6f6963', '93887b'],
             rng.randrange(10 ** 6), 1, .3, squash=-.2)
        # Wet lower part: darken every vertex near or below the waterline.
        for i in range(before, len(m.v)):
            key = m.v[i]
            wet = min(1., max(0., (WATER_Y + .35 - key[1]) / .5))
            col = np.array(key[6:9], float) * (1 - .55 * wet) + WET * .55 * wet
            m.v[i] = key[:6] + tuple(int(v) for v in col) + key[9:]
    return m


def river(piers, rocks=()):
    """The river surface: deeper teal mid-channel, paler shallows, small ripples, and foam wakes downstream (+x)
    of each pier and rock."""
    m = Mesh()
    m.code = SOIL
    rng = random.Random(120)
    deep, mid, shallow = (np.array(rgb(h), float) for h in ('2f6a72', '3f828a', '6fa7a2'))
    xs = np.arange(-70, 71, 2.)
    zs = np.arange(29, 53.01, 1.)
    def depth(x, z):
        c = (land.surface(x, z) - WATER_Y)
        return c
    def colour(x, z):
        d = -depth(x, z)  # metres below the water surface (positive in the channel)
        t = min(1., max(0., d / 3.5))
        c = shallow * (1 - t) + mid * t if t < .6 else mid * (1 - (t - .6) / .4) + deep * ((t - .6) / .4)
        streak = .5 + .5 * math.sin(z * 2.1 + x * .15 + math.sin(x * .3) * 1.5)
        c = c * (.94 + .08 * streak)
        return '%02x%02x%02x' % tuple(int(max(0, min(255, v))) for v in c)
    def point(x, z):
        return (x, WATER_Y + .025 * math.sin(x * .9 + z * 1.7) + .02 * math.cos(z * 2.9 - x * .4), z)
    for i in range(len(xs) - 1):
        for j in range(len(zs) - 1):
            a, b, c, d = (xs[i], zs[j]), (xs[i + 1], zs[j]), (xs[i + 1], zs[j + 1]), (xs[i], zs[j + 1])
            ta, tb, tc, td = (colour(*p) for p in (a, b, c, d))
            m.shaded(point(*a), point(*d), point(*c), [ta, td, tc], (0, 1, 0))
            m.shaded(point(*a), point(*c), point(*b), [ta, tc, tb], (0, 1, 0))
    # Foam: V wakes behind piers and rocks, and loose flecks along the current.
    foam = ['d8ece6', 'c4e0da', 'e8f4ef']
    # Calm water in the lee of each pier and rock: a darker, smoother patch just downstream, and a pale pillow where
    # the current piles against the upstream face.
    calm = ['2a5f68', '2d636b']
    for px, pz, size in list(piers) + list(rocks):
        y = WATER_Y + .035
        ring = [(px + size * .55 + math.cos(a) * size * (1.6 if math.cos(a) > 0 else .5), y,
                 pz + math.sin(a) * size * .55) for a in np.linspace(0, math.tau, 13)[:-1]]
        c = (px + size * 1.1, y, pz)
        for k in range(12):
            m.tri(c, ring[k], ring[(k + 1) % 12], calm[k % 2], (0, 1, 0))
        for k in range(5):
            a0 = math.pi * (.62 + .76 * k / 5); a1 = math.pi * (.62 + .76 * (k + 1) / 5)
            r0, r1 = size * .5, size * .9
            p0 = (px + math.cos(a0) * r0, y + .02, pz + math.sin(a0) * r0)
            p1 = (px + math.cos(a1) * r0, y + .02, pz + math.sin(a1) * r0)
            q0 = (px + math.cos(a0) * r1, y + .02, pz + math.sin(a0) * r1)
            q1 = (px + math.cos(a1) * r1, y + .02, pz + math.sin(a1) * r1)
            m.quad(p0, p1, q1, q0, foam[k % 3], (0, 1, 0))
    for px, pz, size in list(piers) + list(rocks):
        for side in (-1, 1):
            for k in range(7):
                t = k / 6
                x = px + size * .6 + t * size * 5.5
                z = pz + side * (size * .4 + t * size * 1.6) + rng.uniform(-.15, .15)
                l = rng.uniform(.6, 1.4) * (1 - .5 * t) * size
                y = WATER_Y + .06
                m.tri((x, y, z), (x + l, y, z + side * .12 * l), (x + .3 * l, y, z + side * .22), foam[k % 3], (0, 1, 0))
    for k in range(70):
        x = rng.uniform(-60, 60)
        z = rng.uniform(32, 50)
        l = rng.uniform(.6, 2.6)
        y = WATER_Y + .05
        m.tri((x, y, z), (x + l, y, z + .08), (x + .2 * l, y, z + .16), foam[k % 3], (0, 1, 0))
    return m
