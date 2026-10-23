#!/usr/bin/env python3
"""Paint the H9 skin layers onto the owner-selected CC0 skin texture (plain Python: numpy, scipy, Pillow).

Inputs: the H8S Skin_Albedo.png and Skin_Normal.png and a UV position map from bake_uv_position_h9.py. Layers:
1. Mild low-frequency tone evening on head and neck (keeps pores and wrinkles, removes blotches).
2. Painted hair base under the groom: full density under the swept-back top, an undercut fade on the sides and
   back, sideburns, and a neat short beard with jittered soft edges, using the same regions as the groom cards.
3. Slightly deeper, warmer lips; a darker lash line along the lid margins when margins are supplied.
4. Normal map: skin detail kept, hair regions given fine hair grain instead of bare-scalp pores.
Deterministic for a fixed seed.
"""
import argparse, json, sys
from pathlib import Path
import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
import malik_groom_regions_h9 as R


def srgb_to_lin(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def masked_blur(img, mask, sigma):
    num = ndimage.gaussian_filter(img * mask[..., None], (sigma, sigma, 0))
    den = ndimage.gaussian_filter(mask.astype(np.float64), sigma)[..., None]
    return num / np.maximum(den, 1e-6)


def main():
    p = argparse.ArgumentParser()
    p.add_argument('--src', type=Path, required=True, help='folder with the H8S Skin_Albedo.png and Skin_Normal.png')
    p.add_argument('--posmap', type=Path, required=True)
    p.add_argument('--out', type=Path, required=True)
    p.add_argument('--seed', type=int, default=90777)
    a = p.parse_args()
    rng = np.random.default_rng(a.seed)
    a.out.mkdir(parents=True, exist_ok=True)
    src = Image.open(a.src / 'Skin_Albedo.png')
    alb = np.asarray(src.convert('RGB'), dtype=np.float64) / 255.0
    alpha = np.asarray(src)[..., 3] if src.mode == 'RGBA' else None
    nrm_img = np.asarray(Image.open(a.src / 'Skin_Normal.png').convert('RGB'), dtype=np.float64) / 255.0
    d = np.load(a.posmap)
    P, N, cov = d['pos'].astype(np.float64), d['nrm'].astype(np.float64), d['cov'] > 0
    S = alb.shape[0]
    flatP = P.reshape(-1, 3)
    flatN = N.reshape(-1, 3)
    idx = np.flatnonzero(cov.ravel())
    def field(fn):
        out = np.zeros(S * S)
        out[idx] = fn(flatP[idx], flatN[idx])
        return out.reshape(S, S)
    scalp = field(R.scalp_region)
    top = field(R.top_region)
    beard = field(R.beard_region)
    fade = field(lambda P_, N_: R.fade_height(P_))
    lips = field(lambda P_, N_: R.lip_region(P_))
    z = np.zeros((S, S)); z.reshape(-1)[idx] = flatP[idx, 2]
    y = np.zeros((S, S)); y.reshape(-1)[idx] = flatP[idx, 1]
    lin = srgb_to_lin(alb)
    stats = {}

    # 1. Tone evening on head and neck: compress low-frequency variation toward the cheek tone.
    head = cov & (z > 1.40)
    blend_w = (R.smooth(1.40, 1.48, z) * cov)
    low = masked_blur(lin, head, 22.0)
    cheek = head & (z > 1.69) & (z < 1.745) & (y < -0.10) & (lips < 0.1) & (beard < 0.5) & (scalp < 0.1)
    target = np.median(lin[cheek], axis=0)
    stats['cheek_target_srgb'] = [round(float(c), 4) for c in lin_to_srgb(target)]
    ratio = (target[None, None, :] / np.maximum(low, 1e-4)) ** 0.5
    ratio = np.clip(ratio, 0.75, 1.35)
    lin = lin * (1 - blend_w[..., None]) + lin * ratio * blend_w[..., None]

    # 2. Painted hair base. Fine speckle noise thresholded by density gives a buzzed, strand-like grain.
    grain = ndimage.gaussian_filter(rng.random((S, S)), 0.7)
    # Rank transform to a uniform distribution so a density of 0.4 really covers 40 percent of texels.
    order = np.argsort(grain, axis=None)
    ranks = np.empty(S * S); ranks[order] = np.linspace(0.0, 1.0, S * S)
    grain = ranks.reshape(S, S)
    lowj = ndimage.gaussian_filter(rng.random((S, S)), 6.0)
    lowj = (lowj - lowj.mean()) / (lowj.std() + 1e-9)
    hair_lin = srgb_to_lin(np.array([0.105, 0.078, 0.064]))
    shade = 0.82 + 0.36 * ndimage.gaussian_filter(rng.random((S, S)), 1.2)
    side_density = scalp * (0.10 + 0.86 * fade ** 1.3) * (1 - top) + top
    xx = np.zeros((S, S)); xx.reshape(-1)[idx] = flatP[idx, 0]
    mouth = (np.abs(xx) < 0.03) & (z > 1.66) & (z < 1.71) & (y > -0.125)  # mouth cavity behind the lips
    beard = np.where(mouth, 0.0, beard)
    beard_density = np.clip(beard + 0.10 * lowj * (beard > 0.05) * (beard < 0.95), 0, 1) * 0.92
    density = np.maximum(np.clip(side_density + 0.10 * lowj * (side_density > 0.05) * (side_density < 0.95), 0, 1),
                         beard_density)
    coverage = R.smooth(-0.12, 0.12, density - grain) * R.smooth(0.0, 0.25, density)
    under = 1.0 - 0.35 * np.clip(density * 1.4, 0, 1)  # skin between hairs reads darker in dense areas
    hair_col = hair_lin[None, None, :] * shade[..., None]
    lin = lin * under[..., None]
    lin = lin * (1 - coverage[..., None]) + hair_col * coverage[..., None]
    stats['scalp_texels'] = int((scalp > 0.5).sum())
    stats['beard_texels'] = int((beard > 0.5).sum())

    # 3. Lips: slightly deeper and warmer than the grey-pink source.
    lips_w = lips[..., None] * 0.8
    lin = lin * (1 - lips_w) + lin * np.array([0.80, 0.62, 0.60])[None, None, :] * lips_w

    out = lin_to_srgb(lin)
    rgb = (np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8)
    if alpha is not None:
        img = Image.fromarray(np.dstack([rgb, alpha]), 'RGBA')
    else:
        img = Image.fromarray(rgb, 'RGB')
    img.save(a.out / 'Skin_Albedo.png', optimize=True)

    # 4. Normal map: hair regions get fine grain instead of scalp pores; elsewhere unchanged.
    n = nrm_img * 2 - 1
    hair_w = np.clip(np.maximum(side_density, beard_density) * 1.2, 0, 1)[..., None]
    gx = ndimage.sobel(grain, axis=1) * 0.9
    gy = ndimage.sobel(grain, axis=0) * 0.9
    hair_n = np.dstack([-gx, gy, np.ones_like(gx)])
    hair_n /= np.linalg.norm(hair_n, axis=-1, keepdims=True)
    flat = np.array([0.0, 0.0, 1.0])
    soft = n * 0.45 + flat * 0.55
    blend = soft * (1 - hair_w) + hair_n * hair_w
    n_out = n * (1 - hair_w) + blend * hair_w
    n_out /= np.maximum(np.linalg.norm(n_out, axis=-1, keepdims=True), 1e-6)
    Image.fromarray(((n_out * 0.5 + 0.5) * 255 + 0.5).astype(np.uint8), 'RGB').save(a.out / 'Skin_Normal.png', optimize=True)
    print(json.dumps(stats))


if __name__ == '__main__':
    main()
