#!/usr/bin/env python3
"""Paint the H9 groom card atlas: clumped, tapered strands with alpha cutout and a matching tangent normal map.

Original procedural artwork (no external source). Deterministic for a fixed seed. Writes Groom_Albedo.png
(sRGB RGBA, alpha is coverage for a 0.3 cutout) and Groom_Normal.png (linear, OpenGL green-up) and prints the
tile table used by the card builder. Strands grow from the root at the bottom edge of each tile to the tip at
the top edge, so a card maps v=0 to the root and v=1 to the tip.
"""
import argparse, json, math, random
from pathlib import Path

SIZE = 1024
SS = 4  # supersampling factor
# Tile rectangles in image pixels (x, y, w, h) with y measured from the top of the image.
TILES = {}
for i in range(6):
    TILES['top%d' % i] = (i * 128, 0, 128, 640)
for i in range(4):
    TILES['fade%d' % i] = (768 + (i % 2) * 128, (i // 2) * 320, 128, 320)
for i in range(6):
    TILES['beard%d' % i] = (i * 128, 640, 128, 256)
TILES['lash_upper'] = (768, 640, 256, 96)
TILES['lash_lower'] = (768, 736, 256, 64)
for i in range(6):
    TILES['wisp%d' % i] = (i * 128, 896, 128, 128)
TILES['brow0'] = (768, 800, 256, 112)
TILES['brow1'] = (768, 912, 256, 112)


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

    def strand(self, pts, w0, w1, value0, value1, alpha0=255, alpha1=255):
        """Draw one tapered strand given supersampled points from root to tip."""
        n = len(pts) - 1
        for k in range(n):
            t = k / max(1, n - 1)
            w = max(1.0, w0 + (w1 - w0) * t)
            a = int(alpha0 + (alpha1 - alpha0) * t)
            v = int(max(0, min(255, value0 + (value1 - value0) * t)))
            (x0, y0), (x1, y1) = pts[k], pts[k + 1]
            # Across-strand offsets carry a signed slope so the normal map rounds each strand.
            dx, dy = x1 - x0, y1 - y0
            L = math.hypot(dx, dy) or 1.0
            px, py = -dy / L, dx / L
            self.da.line([(x0, y0), (x1, y1)], fill=a, width=int(round(w)))
            self.dv.line([(x0, y0), (x1, y1)], fill=v, width=int(round(w)))
            if w >= 3:
                h = w / 3.0
                for s, val in ((-1, 70), (0, 128), (1, 186)):
                    ox, oy = px * s * h, py * s * h
                    self.ds.line([(x0 + ox, y0 + oy), (x1 + ox, y1 + oy)], fill=val, width=max(1, int(round(h))))
            else:
                self.ds.line([(x0, y0), (x1, y1)], fill=128, width=1)


def curve(rng, x0, y0, length, angle, bend, wave, clump_x, clump_pull, segs=14):
    """Strand centerline in tile-local supersampled pixels; y decreases toward the tip."""
    pts = []
    phase = rng.uniform(0, 6.283)
    for k in range(segs + 1):
        t = k / segs
        ang = angle + bend * t
        x = x0 + math.sin(ang) * length * t + wave * math.sin(phase + t * 7.0) * t
        y = y0 - math.cos(ang) * length * t
        pull = clump_pull * (t ** 1.6)
        x = x + (clump_x - x) * pull
        pts.append((x, y))
    return pts


def clumped(cv, rng, X, Y, W, H, clumps, per_clump, root_spread, tip_spread, len_lo, len_hi, w_lo, w_hi,
            value_lo, value_hi, wave, segs=14, bend=0.10, root_band=0.06):
    """Strands grouped into clumps: roots spread and overlap (dense base), tips converge (gaps between clumps)."""
    centers = []
    for c in range(clumps):
        u = (c + 0.5) / clumps + rng.uniform(-0.25, 0.25) / clumps
        centers.append(X + W * (0.10 + 0.80 * u))
    for cx in centers:
        lean = rng.uniform(-0.06, 0.06)
        for i in range(per_clump):
            rx = rng.gauss(cx, W * root_spread)
            if rx < X + W * 0.02 or rx > X + W * 0.98:
                continue
            ry = Y + H - H * rng.uniform(0.0, root_band)
            length = H * rng.uniform(len_lo, len_hi)
            tx = rng.gauss(cx, W * tip_spread)
            pts = []
            phase = rng.uniform(0, 6.283)
            ang = lean + rng.uniform(-bend, bend)
            for k in range(segs + 1):
                t = k / segs
                x = rx + (tx - rx) * (t ** 1.3) + wave * math.sin(phase + t * 6.0) * t + math.sin(ang) * length * t * 0.3
                y = ry - length * t
                pts.append((x, y))
            base = rng.uniform(value_lo, value_hi)
            cv.strand(pts, rng.uniform(w_lo, w_hi), w_lo * 0.35, base * 0.8, base * 1.35)
    return centers


def paint_tile(cv, name, rng, kind):
    x, y, w, h = TILES[name]
    X, Y, W, H = x * SS, y * SS, w * SS, h * SS
    if kind == 'top':
        clumped(cv, rng, X, Y, W, H, rng.randint(5, 7), 30, 0.075, 0.020, 0.62, 0.98, SS * 1.2, SS * 1.8,
                24, 40, SS * 2.0, segs=16)
        for i in range(10):  # loose flyaways break the clump outline
            rx = X + W * rng.uniform(0.15, 0.85); ry = Y + H * rng.uniform(0.3, 0.75)
            pts = curve(rng, rx, ry, H * rng.uniform(0.12, 0.25), rng.uniform(-0.5, 0.5), rng.uniform(-0.6, 0.6), SS * 3, rx, 0)
            cv.strand(pts, SS * 1.0, SS * 0.4, 40, 52)
    elif kind == 'fade':
        for i in range(330):
            u = rng.betavariate(1.8, 1.8)
            rx = X + W * (0.03 + 0.94 * u)
            ry = Y + H - H * rng.uniform(0.0, 0.5)
            length = H * rng.uniform(0.18, 0.42)
            pts = curve(rng, rx, ry, length, rng.uniform(-0.22, 0.22), rng.uniform(-0.3, 0.3), SS * 1.0, rx, 0, segs=6)
            base = rng.uniform(22, 36)
            cv.strand(pts, SS * rng.uniform(1.2, 1.7), SS * 0.45, base * 0.8, base * 1.25)
    elif kind == 'beard':
        clumped(cv, rng, X, Y, W, H, rng.randint(6, 8), 26, 0.06, 0.025, 0.45, 0.92, SS * 1.3, SS * 1.9,
                18, 34, SS * 3.0, segs=10, bend=0.35, root_band=0.18)
        for i in range(4):  # sparse grey hairs for an adult beard
            rx = X + W * rng.uniform(0.1, 0.9); ry = Y + H * rng.uniform(0.8, 0.98)
            pts = curve(rng, rx, ry, H * rng.uniform(0.4, 0.75), rng.uniform(-0.25, 0.25), rng.uniform(-0.4, 0.4), SS * 2.5, rx, 0, segs=8)
            cv.strand(pts, SS * 1.3, SS * 0.45, 95, 115)
    elif kind in ('lash_upper', 'lash_lower'):
        count = 64 if kind == 'lash_upper' else 40
        for i in range(count):
            u = (i + rng.uniform(-0.35, 0.35)) / count
            rx = X + W * (0.03 + 0.94 * u)
            ry = Y + H - SS * 2
            mid = math.sin(math.pi * min(1, max(0, u)))
            length = H * (0.45 + 0.5 * mid) * rng.uniform(0.8, 1.0) * (1.0 if kind == 'lash_upper' else 0.75)
            lean = (u - 0.5) * 0.9
            pts = curve(rng, rx, ry, length, lean, rng.uniform(0.25, 0.45) * (1 if lean >= 0 else -1), SS * 0.6, rx, 0, segs=8)
            cv.strand(pts, SS * rng.uniform(1.6, 2.2), SS * 0.35, 14, 20)
    elif kind == 'wisp':
        for i in range(40):
            rx = X + W * rng.uniform(0.05, 0.95); ry = Y + H - SS * rng.uniform(0, 4)
            pts = curve(rng, rx, ry, H * rng.uniform(0.5, 0.95), rng.uniform(-0.2, 0.2), rng.uniform(-0.5, 0.5), SS * 2.5, rx, 0, segs=10)
            cv.strand(pts, SS * 1.2, SS * 0.4, 28, 46)
    elif kind == 'brow':
        for i in range(300):
            u = rng.random()
            rx = X + W * (0.02 + 0.96 * u)
            ry = Y + H * rng.uniform(0.35, 0.95)
            lean = 1.15 + 0.35 * (1 - u) + rng.uniform(-0.2, 0.2)
            length = H * rng.uniform(0.25, 0.55)
            pts = curve(rng, rx, ry, length, lean, rng.uniform(-0.3, 0.1), SS * 0.5, rx, 0, segs=6)
            cv.strand(pts, SS * rng.uniform(1.2, 1.7), SS * 0.45, 20, 30)


def finish(cv, out):
    import numpy as np
    from PIL import Image, ImageFilter
    n = SIZE * SS
    A = np.asarray(cv.alpha, dtype=np.float32) / 255.0
    Vv = np.asarray(cv.value, dtype=np.float32) / 255.0
    S = (np.asarray(cv.slope, dtype=np.float32) - 128.0) / 128.0

    def down(img):
        return img.reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))
    a = down(A)
    prem_v = down(Vv * A)
    prem_s = down(S * A)
    cov = np.maximum(a, 1e-4)
    v = prem_v / cov
    s = prem_s / cov
    # Edge padding: spread colour from covered texels into transparent ones so mips never bleed black.
    filled = a > 0.02
    vv = np.where(filled, v, 0.0)
    ww = filled.astype(np.float32)
    for r in (1, 2, 4, 8, 16, 32):
        img_v = Image.fromarray((vv * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))
        img_w = Image.fromarray((ww * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(r))
        bv = np.asarray(img_v, dtype=np.float32) / 255.0
        bw = np.asarray(img_w, dtype=np.float32) / 255.0
        grow = (ww < 0.5) & (bw > 0.02)
        vv = np.where(grow, bv / np.maximum(bw, 1e-4) * 1.0, vv)
        ww = np.where(grow, 1.0, ww)
    v = np.where(filled, v, vv)
    s = np.where(filled, s, 0.0)
    # Root-to-tip value lives in v (0..1 strand value, about 0.08 to 0.25). Map through a dark brown ramp.
    v = np.clip(v, 0, 1)
    base = np.array([0.070, 0.052, 0.042])  # sRGB near-black espresso at the root
    tip = np.array([0.205, 0.160, 0.128])    # warmer, lighter toward the tip
    t = np.clip((v - 0.07) / 0.17, 0, 1)[..., None]
    rgb = base * (1 - t) + tip * t
    # Grey beard hairs keep their own lighter, desaturated value.
    grey = np.clip((v - 0.33) / 0.1, 0, 1)[..., None]
    rgb = rgb * (1 - grey) + np.array([0.48, 0.46, 0.44]) * grey
    rgba = np.concatenate([rgb, a[..., None]], axis=-1)
    Image.fromarray((np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGBA').save(out / 'Groom_Albedo.png', optimize=True)
    # Tangent-space normal: across-strand roundness in X, a slight lift toward the tip in Y.
    nx = np.clip(s * 0.55, -0.75, 0.75)
    ny = np.where(filled, 0.12, 0.0)
    nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0.05, 1))
    nrm = np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, nz * 0.5 + 0.5], axis=-1)
    Image.fromarray((nrm * 255 + 0.5).astype(np.uint8), 'RGB').save(out / 'Groom_Normal.png', optimize=True)
    return float(a.mean())


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--out', type=Path, required=True)
    p.add_argument('--seed', type=int, default=90123)
    a = p.parse_args()
    a.out.mkdir(parents=True, exist_ok=True)
    rng = random.Random(a.seed)
    cv = Canvas()
    for name in TILES:
        kind = ''.join(ch for ch in name if not ch.isdigit())
        paint_tile(cv, name, rng, kind)
    coverage = finish(cv, a.out)
    print(json.dumps({'size': SIZE, 'coverage': round(coverage, 4), 'tiles': {k: tile_uv(k) for k in TILES}}))


if __name__ == '__main__':
    main()
