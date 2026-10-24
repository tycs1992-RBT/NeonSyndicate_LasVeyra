#!/usr/bin/env python3
"""Paint the H9 groom atlas: a head chart of flow-aligned strands (left half) and card tiles (right half).

Original procedural artwork (no external source), deterministic for a fixed seed. Writes Groom_Albedo.png
(sRGB RGBA; alpha is strand coverage for a 0.3 cutout) and Groom_Normal.png (linear, OpenGL green-up).
- Left half: the skin's head UV island scaled into the atlas. Strand polylines come from author_malik_h9.py, which
  marches the hairstyle flow on the 3D skin and converts every point to chart pixels (beard, undercut fade, top).
- Right half: card tiles. Strands grow from the root at the bottom edge of a tile to the tip at its top edge, so a
  card maps v=0 to the root and v=1 to the tip.
The tile table is imported by author_malik_h9.py inside Blender, so numpy and Pillow are imported lazily.
"""
import argparse, json, math, random
from pathlib import Path

SIZE = 2048
SS = 3  # supersampling factor
TILES = {}
for i in range(8):
    TILES['top%d' % i] = (1024 + i * 128, 0, 128, 640)
for i in range(8):
    TILES['wisp%d' % i] = (1024 + i * 128, 640, 128, 256)
TILES['lash_upper'] = (1024, 896, 256, 96)
TILES['lash_lower'] = (1024, 992, 256, 64)
TILES['brow0'] = (1280, 896, 256, 112)
TILES['brow1'] = (1280, 1008, 256, 112)
for i in range(4):
    TILES['beard%d' % i] = (1536 + i * 128, 896, 128, 256)
for i in range(8):
    TILES['fade%d' % i] = (1024 + i * 128, 1152, 128, 320)


def tile_uv(name):
    """UV rectangle (u0, v0, u1, v1) of a tile in Blender convention (v up, root at v0)."""
    x, y, w, h = TILES[name]
    return (x / SIZE, 1 - (y + h) / SIZE, (x + w) / SIZE, 1 - y / SIZE)


class Canvas:
    def __init__(self):
        from PIL import Image, ImageDraw
        n = SIZE * SS
        self.alpha = Image.new('L', (n, n), 0)
        self.value = Image.new('L', (n, n), 0)
        self.slope = Image.new('L', (n, n), 128)
        self.da = ImageDraw.Draw(self.alpha)
        self.dv = ImageDraw.Draw(self.value)
        self.ds = ImageDraw.Draw(self.slope)

    def strand(self, pts, w0, w1, value0, value1):
        """One tapered strand through supersampled points from root to tip; the slope buffer rounds it."""
        n = len(pts) - 1
        for k in range(n):
            t = k / max(1, n - 1)
            w = max(1.0, w0 + (w1 - w0) * t)
            v = int(max(0, min(255, value0 + (value1 - value0) * t)))
            (x0, y0), (x1, y1) = pts[k], pts[k + 1]
            dx, dy = x1 - x0, y1 - y0
            L = math.hypot(dx, dy) or 1.0
            px, py = -dy / L, dx / L
            wi = int(round(w))
            self.da.line([(x0, y0), (x1, y1)], fill=255, width=wi)
            self.dv.line([(x0, y0), (x1, y1)], fill=v, width=wi)
            if w >= 3:
                h = w / 3.0
                for s, val in ((-1, 60), (0, 128), (1, 196)):
                    ox, oy = px * s * h, py * s * h
                    self.ds.line([(x0 + ox, y0 + oy), (x1 + ox, y1 + oy)], fill=val, width=max(1, int(round(h))))
            else:
                self.ds.line([(x0, y0), (x1, y1)], fill=128, width=1)


def curve(rng, x0, y0, length, angle, bend, wave, segs=14):
    pts = []
    phase = rng.uniform(0, 6.283)
    for k in range(segs + 1):
        t = k / segs
        ang = angle + bend * t
        pts.append((x0 + math.sin(ang) * length * t + wave * math.sin(phase + t * 7.0) * t, y0 - math.cos(ang) * length * t))
    return pts


def clumped(cv, rng, X, Y, W, H, clumps, per_clump, root_spread, tip_spread, len_lo, len_hi, w_lo, w_hi,
            value_lo, value_hi, wave, segs=14, bend=0.10, root_band=0.06, root_fade=0.0):
    """Strands grouped into clumps: roots overlap (dense base), tips converge (gaps between clumps)."""
    for c in range(clumps):
        u = (c + 0.5) / clumps + rng.uniform(-0.25, 0.25) / clumps
        cx = X + W * (0.12 + 0.76 * u)
        lean = rng.uniform(-0.06, 0.06)
        for i in range(per_clump):
            rx = rng.gauss(cx, W * root_spread)
            if rx < X + W * 0.03 or rx > X + W * 0.97:
                continue
            # roots start at slightly different heights so a card's root edge never reads as a straight line
            ry = Y + H - H * rng.uniform(0.0, root_band) - H * root_fade * rng.random() ** 0.6
            length = min(H * rng.uniform(len_lo, len_hi), ry - Y - 2)
            tx = rng.gauss(cx, W * tip_spread)
            pts = []
            phase = rng.uniform(0, 6.283)
            ang = lean + rng.uniform(-bend, bend)
            for k in range(segs + 1):
                t = k / segs
                x = rx + (tx - rx) * (t ** 1.3) + wave * math.sin(phase + t * 6.0) * t + math.sin(ang) * length * t * 0.3
                pts.append((x, ry - length * t))
            base = rng.uniform(value_lo, value_hi)
            cv.strand(pts, rng.uniform(w_lo, w_hi), w_lo * 0.35, base * 0.8, base * 1.35)


def paint_tile(cv, name, rng, kind):
    x, y, w, h = TILES[name]
    X, Y, W, H = x * SS, y * SS, w * SS, h * SS
    if kind == 'top':
        clumped(cv, rng, X, Y, W, H, rng.randint(5, 7), 27, 0.075, 0.022, 0.62, 0.97, SS * 1.1, SS * 1.6,
                24, 40, SS * 2.0, segs=16, root_fade=0.10)
        for i in range(10):
            rx = X + W * rng.uniform(0.15, 0.85); ry = Y + H * rng.uniform(0.3, 0.75)
            cv.strand(curve(rng, rx, ry, H * rng.uniform(0.12, 0.25), rng.uniform(-0.5, 0.5), rng.uniform(-0.6, 0.6), SS * 3), SS * 1.0, SS * 0.4, 40, 52)
    elif kind == 'wisp':
        for i in range(40):
            rx = X + W * rng.uniform(0.05, 0.95); ry = Y + H - SS * rng.uniform(0, 4)
            cv.strand(curve(rng, rx, ry, H * rng.uniform(0.5, 0.95), rng.uniform(-0.2, 0.2), rng.uniform(-0.5, 0.5), SS * 2.5, segs=10), SS * 1.2, SS * 0.4, 28, 46)
    elif kind == 'fade':
        for i in range(330):
            u = rng.betavariate(1.8, 1.8)
            rx = X + W * (0.03 + 0.94 * u); ry = Y + H - H * rng.uniform(0.0, 0.5)
            base = rng.uniform(22, 36)
            cv.strand(curve(rng, rx, ry, H * rng.uniform(0.18, 0.42), rng.uniform(-0.22, 0.22), rng.uniform(-0.3, 0.3), SS * 1.0, segs=6), SS * rng.uniform(1.2, 1.7), SS * 0.45, base * 0.8, base * 1.25)
    elif kind == 'beard':
        clumped(cv, rng, X, Y, W, H, rng.randint(6, 8), 26, 0.06, 0.025, 0.45, 0.92, SS * 1.3, SS * 1.9,
                18, 34, SS * 3.0, segs=10, bend=0.35, root_band=0.18)
    elif kind in ('lash_upper', 'lash_lower'):
        count = 96 if kind == 'lash_upper' else 34
        for i in range(count):
            u = (i + rng.uniform(-0.35, 0.35)) / count
            rx = X + W * (0.03 + 0.94 * u); ry = Y + H - SS * 2
            mid = math.sin(math.pi * min(1, max(0, u)))
            length = H * (0.45 + 0.5 * mid) * rng.uniform(0.8, 1.0) * (1.0 if kind == 'lash_upper' else 0.75)
            lean = (u - 0.5) * 0.9
            wroot = SS * rng.uniform(1.7, 2.3) if kind == 'lash_upper' else SS * rng.uniform(1.2, 1.6)
            cv.strand(curve(rng, rx, ry, length, lean, rng.uniform(0.25, 0.45) * (1 if lean >= 0 else -1), SS * 0.6, segs=8), wroot, SS * 0.35, 14, 20)
    elif kind == 'brow':
        for i in range(300):
            u = rng.random()
            rx = X + W * (0.02 + 0.96 * u); ry = Y + H * rng.uniform(0.35, 0.95)
            lean = 1.15 + 0.35 * (1 - u) + rng.uniform(-0.2, 0.2)
            cv.strand(curve(rng, rx, ry, H * rng.uniform(0.25, 0.55), lean, rng.uniform(-0.3, 0.1), SS * 0.5, segs=6), SS * rng.uniform(1.2, 1.7), SS * 0.45, 20, 30)


def paint_chart(cv, strands, rng):
    """Draw the baked head-chart strands: beard, undercut fade and the top base layer."""
    import numpy as np
    d = np.load(strands)
    meta, pts = d['meta'], d['points']
    if int(d['size'][0]) != SIZE:
        raise SystemExit('Strand bake was made for a different atlas size.')
    order = rng.sample(range(len(meta)), len(meta))  # interleave kinds so no layer always wins
    offsets = np.concatenate([[0], np.cumsum(meta[:, 4].astype(np.int64) * 2)])
    counts = {0: 0, 1: 0, 2: 0}
    for i in order:
        kind, val, w0, w1, n = meta[i]
        xy = pts[offsets[i]:offsets[i + 1]].reshape(-1, 2) * SS
        base = (22 + 14 * rng.random()) * float(val)
        if kind == 0 and rng.random() < 0.012:
            base = rng.uniform(85, 110)  # sparse grey hairs in an adult beard
        cv.strand([tuple(p) for p in xy], w0 * SS, w1 * SS, base * 0.8, base * 1.3)
        counts[int(kind)] += 1
    return {'beard': counts[0], 'fade': counts[1], 'top': counts[2]}


def finish(cv, out):
    import numpy as np
    from PIL import Image, ImageFilter
    A = np.asarray(cv.alpha, dtype=np.float32) / 255.0
    a = A.reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))
    v = (np.asarray(cv.value, dtype=np.float32) / 255.0 * A).reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))
    s = ((np.asarray(cv.slope, dtype=np.float32) - 128.0) / 128.0 * A).reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))
    del A
    cov = np.maximum(a, 1e-4)
    v = v / cov
    s = s / cov
    filled = a > 0.02
    # Edge padding: spread colour into transparent texels so mips never bleed black fringes.
    vv = np.where(filled, v, 0.0)
    ww = filled.astype(np.float32)
    for r in (1, 2, 4, 8, 16, 32):
        bv = np.asarray(Image.fromarray((vv * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r)), dtype=np.float32) / 255.0
        bw = np.asarray(Image.fromarray((ww * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r)), dtype=np.float32) / 255.0
        grow = (ww < 0.5) & (bw > 0.02)
        vv = np.where(grow, bv / np.maximum(bw, 1e-4), vv)
        ww = np.where(grow, 1.0, ww)
    v = np.clip(np.where(filled, v, vv), 0, 1)
    s = np.where(filled, s, 0.0)
    base = np.array([0.070, 0.052, 0.042])  # near-black espresso at the root (sRGB)
    tip = np.array([0.205, 0.160, 0.128])   # warmer and lighter toward the tip
    t = np.clip((v - 0.07) / 0.17, 0, 1)[..., None]
    rgb = base * (1 - t) + tip * t
    grey = np.clip((v - 0.33) / 0.1, 0, 1)[..., None]
    rgb = rgb * (1 - grey) + np.array([0.48, 0.46, 0.44]) * grey
    rgba = np.concatenate([rgb, a[..., None]], axis=-1)
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA').save(out / 'Groom_Albedo.png', optimize=True)
    nx = np.clip(s * 0.55, -0.75, 0.75)
    ny = np.where(filled, 0.08, 0.0)
    nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0.05, 1))
    nrm = np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, nz * 0.5 + 0.5], axis=-1)
    Image.fromarray((nrm * 255 + 0.5).astype(np.uint8), 'RGB').save(out / 'Groom_Normal.png', optimize=True)
    return float(a.mean())


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--out', type=Path, required=True)
    p.add_argument('--strands', type=Path, required=True, help='groom-strands-h9.npz from author_malik_h9.py')
    p.add_argument('--seed', type=int, default=90123)
    a = p.parse_args()
    a.out.mkdir(parents=True, exist_ok=True)
    rng = random.Random(a.seed)
    cv = Canvas()
    for name in TILES:
        paint_tile(cv, name, rng, ''.join(ch for ch in name if not ch.isdigit()))
    counts = paint_chart(cv, a.strands, rng)
    coverage = finish(cv, a.out)
    print(json.dumps({'size': SIZE, 'coverage': round(coverage, 4), 'chart_strands': counts}))


if __name__ == '__main__':
    main()
