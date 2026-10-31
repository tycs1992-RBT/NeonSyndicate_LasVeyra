#!/usr/bin/env python3
"""Hairstyle regions for the H9 Malik groom, in bind-pose Blender metres (front is -Y, up is +Z).

Pure numpy, shared by the card builder (inside Blender) and the skin texture painter (plain Python), so the
painted scalp, sideburn and beard shadow always agree with the geometry cards. Canon: a short undercut with a
swept-back top and a neat short beard on an adult man.

Measured on the selected body: crown z 1.858, forehead y -0.150, occiput y +0.048, ear y -0.060 to -0.030 and
z 1.706 to 1.760, brow z 1.765, nose tip z 1.715, mouth z 1.672 to 1.700, chin bottom (y -0.139, z 1.641),
throat junction (y -0.090, z 1.626).
"""
import numpy as np

HEAD_AXIS_Y = -0.055
EAR_Y, EAR_Z = -0.043, 1.733


def smooth(e0, e1, x):
    t = np.clip((np.asarray(x, dtype=np.float64) - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def azimuth(P):
    """Degrees around the vertical head axis; 0 faces forward (-Y), +90 is the character's left (+X)."""
    P = np.atleast_2d(P)
    return np.degrees(np.arctan2(P[:, 0], -(P[:, 1] - HEAD_AXIS_Y)))


def front_hairline_z(x):
    ax = np.abs(x)
    return 1.833 - 0.004 * (ax / 0.04) ** 2 - 0.006 * smooth(0.025, 0.055, ax)


def ear_clear(P):
    """0 on the ear, 1 elsewhere. Tight in front (sideburns sit against the tragus), a 6 mm margin behind."""
    P = np.atleast_2d(P)
    y, z, ax = P[:, 1], P[:, 2], np.abs(P[:, 0])
    ry = np.where(y < EAR_Y, 0.0175, 0.025)
    e = ((y - EAR_Y) / ry) ** 2 + ((z - EAR_Z) / 0.036) ** 2
    return np.clip(smooth(0.85, 1.15, e) + (ax < 0.050), 0.0, 1.0)


def top_region(P, N=None):
    """1 on the long top of the undercut; fades at the parietal line, the back of the crown and the hairline."""
    P = np.atleast_2d(P)
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    ax = np.abs(x)
    width = 0.052 + 0.006 * smooth(-0.13, -0.06, y) - 0.010 * smooth(0.0, 0.045, y)
    side = 1.0 - smooth(width - 0.006, width + 0.004, ax)
    fh = front_hairline_z(x)
    front = np.where(y < -0.10, smooth(fh - 0.002, fh + 0.004, z), 1.0)
    back = np.where(y > 0.015, smooth(1.806, 1.822, z), 1.0)
    return side * front * back * (z >= 1.80)


def scalp_region(P, N=None):
    """1 wherever hair grows on the scalp (top plus the faded sides and back), with natural hairlines."""
    P = np.atleast_2d(P)
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    fh = front_hairline_z(x)
    forehead = smooth(fh - 0.002, fh + 0.004, z)
    temple_lim = np.interp(y, [-0.10, -0.078], [1.812, 1.776])
    temple = smooth(temple_lim - 0.004, temple_lim + 0.004, z)
    sideburn = smooth(1.711, 1.721, z) * smooth(-0.080, -0.075, y)
    nape_z = np.interp(y, [-0.058, -0.03, 0.02], [1.712, 1.735, 1.695])
    back = smooth(nape_z - 0.004, nape_z + 0.006, z)
    r = np.where(y < -0.10, forehead, np.where(y < -0.078, temple, np.where(y < -0.058, sideburn, back)))
    return r * ear_clear(P) * (z >= 1.66)


_CHEEK = np.array([(0, 1.707), (10, 1.704), (18, 1.700), (30, 1.706), (45, 1.713), (60, 1.720), (72, 1.726),
                   (80, 1.730), (180, 1.730)])


def cheek_line_z(az):
    return np.interp(np.abs(az), _CHEEK[:, 0], _CHEEK[:, 1])


def lip_region(P):
    P = np.atleast_2d(P)
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    return smooth(0.027, 0.021, np.abs(x)) * smooth(1.671, 1.675, z) * smooth(1.700, 1.696, z) * (y < -0.115)


def beard_region(P, N=None):
    """Neat short beard: cheek line, mustache, chin, jaw and a neckline under the jaw; lips and ears excluded."""
    P = np.atleast_2d(P)
    x, y, z = P[:, 0], P[:, 1], P[:, 2]
    az = azimuth(P)
    top = cheek_line_z(az)
    upper = (1.0 - smooth(top - 0.003, top + 0.003, z)) * (1.0 - lip_region(P))
    # Neckline: a tilted surface about 3.4 cm behind the chin, rising to the jaw angle below the ear.
    s = np.clip(np.abs(x) / 0.060, 0.0, 1.0)
    y0 = -0.105 + 0.050 * s ** 1.5
    z0 = 1.630 + 0.035 * s ** 2
    value = -(y - y0) * 0.7071 + (z - z0) * 0.7071
    neck = smooth(-0.002, 0.003, value)
    back = smooth(-0.050, -0.062, y)
    valid = (z <= 1.745) & (z >= 1.595) & (np.abs(az) <= 100)
    return np.clip(upper * neck * back * ear_clear(P), 0.0, 1.0) * valid


def fade_height(P):
    """0 at the bottom of the faded sides, 1 at the undercut line; drives hair length and painted density."""
    P = np.atleast_2d(P)
    return smooth(1.71, 1.815, P[:, 2])
