#!/usr/bin/env python3
"""Paint Dante's H9 hair textures: seamless vertical strands (tile in u and v), opaque, with a matching normal map.

Original procedural artwork. Dante's long hair and beard are solid sculpted meshes; author_dante_h9.py unwraps them
cylindrically around the head axis (u = azimuth, v = height, both tiled) so these strands run down the hanging hair
and the beard. Albedo is near-black with root-to-tip-like value banding and sparse grey hairs; the normal map
rounds each strand. Writes DanteHair_Albedo.png (sRGB) and DanteHair_Normal.png (linear, OpenGL green-up).
"""
import argparse, json, math, random
from pathlib import Path
import numpy as np
from PIL import Image

W = H = 1024


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--out', type=Path, required=True)
    p.add_argument('--seed', type=int, default=91234)
    a = p.parse_args()
    rng = random.Random(a.seed)
    nprng = np.random.default_rng(a.seed)
    height = np.zeros((H, W), dtype=np.float64)
    value = np.zeros((H, W), dtype=np.float64)
    weight = np.zeros((H, W), dtype=np.float64)
    ys = np.arange(H)
    clumps = 18
    centers = [(c + rng.uniform(-0.3, 0.3)) / clumps * W for c in range(clumps)]
    for c in centers:
        spread = rng.uniform(10, 18)
        sway = rng.uniform(4, 12)
        k = rng.choice((1, 2))
        phase = rng.uniform(0, 2 * math.pi)
        for s in range(44):
            x0 = c + rng.gauss(0, spread)
            amp = sway + rng.uniform(-2, 2)
            ph = phase + rng.uniform(-0.4, 0.4)
            width = rng.uniform(1.1, 2.2)
            base = rng.uniform(0.10, 0.22)
            if rng.random() < 0.015:
                base = rng.uniform(0.32, 0.42)  # sparse grey hairs
            xs = x0 + amp * np.sin(2 * math.pi * k * ys / H + ph)  # periodic in y: seamless vertically
            band = base * (0.85 + 0.3 * np.sin(2 * math.pi * ys / H + rng.uniform(0, 6.28)))
            for dx in range(-4, 5):
                cols = np.round(xs + dx).astype(int) % W  # wraps in x: seamless horizontally
                dist = np.abs((xs + dx) - np.round(xs + dx) + 0.0)
                prof = np.clip(1.0 - (np.abs(dx) / width) ** 2, 0, 1)
                if not prof.any():
                    continue
                height[ys, cols] = np.maximum(height[ys, cols], prof)
                value[ys, cols] += band * prof
                weight[ys, cols] += prof
    v = np.where(weight > 0, value / np.maximum(weight, 1e-6), 0.08)
    cover = np.clip(height, 0, 1)
    v = v * cover + 0.06 * (1 - cover)  # gaps between strands read as deeper, darker hair
    grain = nprng.random((H, W)) * 0.02
    v = np.clip(v + grain, 0, 1)
    base = np.array([0.055, 0.042, 0.034]); light = np.array([0.30, 0.24, 0.19])
    rgb = base + (light - base) * np.clip((v - 0.06) / 0.25, 0, 1)[..., None]
    grey = np.clip((v - 0.3) / 0.12, 0, 1)[..., None]
    rgb = rgb * (1 - grey) + np.array([0.40, 0.385, 0.37]) * grey
    a.out.mkdir(parents=True, exist_ok=True)
    Image.fromarray((np.clip(rgb, 0, 1) * 255 + 0.5).astype(np.uint8), 'RGB').save(a.out / 'DanteHair_Albedo.png', optimize=True)
    hgt = height
    gx = (np.roll(hgt, -1, axis=1) - np.roll(hgt, 1, axis=1)) * 0.5
    gy = (np.roll(hgt, -1, axis=0) - np.roll(hgt, 1, axis=0)) * 0.5
    n = np.dstack([-gx * 1.6, gy * 0.4, np.ones_like(gx)])
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    Image.fromarray(((n * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8), 'RGB').save(a.out / 'DanteHair_Normal.png', optimize=True)
    print(json.dumps({'size': [W, H], 'coverage': round(float(cover.mean()), 3)}))


if __name__ == '__main__':
    main()
