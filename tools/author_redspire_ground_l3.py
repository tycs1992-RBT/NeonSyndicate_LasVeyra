#!/usr/bin/env python3
"""Original periodic sediment and gravel PBR authoring. No external art inputs.
Run with Python 3, numpy, scipy and Pillow from the repository root.
"""
import hashlib
import json
from pathlib import Path
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, map_coordinates
from scipy.spatial import cKDTree

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Assets/NeonSyndicate/Art/LiveGround/Redspire'
SIZE = 1024


def periodic_noise(rng, cells):
    small = rng.random((cells, cells))
    y, x = np.mgrid[:SIZE, :SIZE] * cells / SIZE
    return map_coordinates(small, [y, x], order=3, mode='grid-wrap')


def save(path, values):
    # Sampling includes a duplicate boundary texel on both axes. Adjacent repeat
    # tiles meet exactly even at the smallest mip, without painting over seams.
    values = np.concatenate([values, values[:1]], axis=0)
    values = np.concatenate([values, values[:, :1]], axis=1)
    image = Image.fromarray(np.uint8(np.clip(values, 0, 1) * 255))
    image = image.resize((SIZE, SIZE), Image.Resampling.BILINEAR)
    a = np.array(image)
    if path.name.endswith('_Normal.png'):
        n = a.astype(np.float64) / 255 * 2 - 1
        n /= np.maximum(np.linalg.norm(n, axis=-1, keepdims=True), .0001)
        a = np.uint8(np.clip(n * .5 + .5, 0, 1) * 255)
    a[-1, :] = a[0, :]
    a[:, -1] = a[:, 0]
    Image.fromarray(a).save(path)


def family(name, seed, stony):
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[:SIZE, :SIZE] / SIZE
    macro = periodic_noise(rng, 6)
    medium = periodic_noise(rng, 30)
    fine = periodic_noise(rng, 180)
    grain = rng.random((SIZE, SIZE))
    # Periodic cellular plates, with dry fissures following cell boundaries.
    seeds = rng.random((135 if not stony else 260, 2))
    pts = np.stack([x.ravel(), y.ravel()], axis=-1)
    distance, ids = cKDTree(seeds, boxsize=1).query(pts, k=2)
    gap = (distance[:, 1] - distance[:, 0]).reshape(SIZE, SIZE)
    cracks = np.exp(-gap * (1100 if not stony else 1800))
    strata = np.sin((y * 24 + (macro - .5) * 1.6) * 2 * np.pi)
    height = .045 * medium + .012 * fine + .003 * grain
    height -= cracks * (.038 if not stony else .012)
    height += strata * (.008 if not stony else .003)
    stone = np.zeros((SIZE, SIZE))
    stone_tint = np.zeros((SIZE, SIZE))
    # Real raised angular chips in the height field, not printed dark dots.
    for _ in range(155 if not stony else 630):
        cx, cy = rng.random(2)
        radius = rng.uniform(.0025, .014 if not stony else .026)
        dx = ((x - cx + .5) % 1 - .5) / radius
        dy = ((y - cy + .5) % 1 - .5) / radius
        angle = rng.uniform(0, np.pi)
        u = dx * np.cos(angle) + dy * np.sin(angle)
        v = (-dx * np.sin(angle) + dy * np.cos(angle)) * rng.uniform(1.1, 2.0)
        contour = np.maximum(abs(u) * .86 + abs(v) * .44, abs(v) * .85)
        chip = np.clip((1 - contour) * 5, 0, 1)
        stone = np.maximum(stone, chip)
        stone_tint += chip * rng.uniform(-.09, .12)
        height += chip * np.clip(1 - contour * .6, 0, 1) * rng.uniform(.018, .075)
    base = np.array([.46, .315, .222] if not stony else [.54, .433, .32])
    tone = (macro - .5) * .08 + (medium - .5) * .035 + (fine - .5) * .018
    tone += (grain - .5) * .025 + stone_tint
    tone -= cracks * (.095 if not stony else .028)
    color = base + tone[:, :, None]
    color += stone[:, :, None] * np.array([.045, .05, .055])
    color += strata[:, :, None] * np.array([.012, .008, .003])
    # Height represents metres over one four-metre repeat. +Y tangent normal.
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * SIZE / 8
    dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * SIZE / 8
    normal = np.stack([-dx, -dy, np.ones_like(dx)], axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    roughness = np.clip(.89 - stone * .18 + (fine - .5) * .06, .6, .96)
    packed = np.zeros((SIZE, SIZE, 4))
    packed[:, :, 3] = 1 - roughness
    save(OUT / (name + '_BaseColor.png'), color)
    save(OUT / (name + '_Normal.png'), normal * .5 + .5)
    save(OUT / (name + '_MetallicSmoothness.png'), packed)
    # Editable height source is outside Unity. No runtime displacement is claimed.
    save(ROOT / 'ArtSource/RedspireL3' / (name + '_Height.png'), height / .18)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    (ROOT / 'ArtSource/RedspireL3').mkdir(parents=True, exist_ok=True)
    family('RedspireWeatheredEarth', 61724, False)
    family('RedspireStonySand', 61725, True)
    files = []
    for p in sorted(OUT.glob('*.png')):
        files.append({'path': p.relative_to(ROOT).as_posix(), 'bytes': p.stat().st_size,
                      'sha256': hashlib.sha256(p.read_bytes()).hexdigest()})
    receipt = {'schema': 1, 'license': 'CC0-1.0', 'external_inputs': [],
               'source': 'tools/author_redspire_ground_l3.py', 'size': SIZE,
               'tile_metres': 4, 'normal': 'tangent +Y',
               'packed': 'RGB metallic zero, A smoothness (1 minus roughness)', 'files': files}
    (OUT / 'manifest.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'families': 2, 'maps': len(files), 'size': SIZE}))


if __name__ == '__main__':
    main()
