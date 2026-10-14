#!/usr/bin/env python3
"""Reproduce Visual Renaissance 01B material data maps. Requires Python, numpy and Pillow.

No downloads, image models, photographs or licensed source textures are used.
Albedo is sRGB. Tangent normals and packed metal/AO/rough/smooth maps are linear.
"""
from pathlib import Path
import hashlib
import json
import uuid
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/NeonSyndicate/Resources/ArtDistrict/Textures"
SIZE = 512
U, V = np.meshgrid(np.arange(SIZE) / SIZE, np.arange(SIZE) / SIZE)
TAU = np.pi * 2


def noise(seed, low=1, high=32, count=35):
    rng = np.random.default_rng(seed)
    value = np.zeros((SIZE, SIZE))
    strength = 0
    for _ in range(count):
        x, y = rng.integers(low, high + 1, 2)
        x *= rng.choice((-1, 1))
        y *= rng.choice((-1, 1))
        phase = rng.uniform(0, TAU)
        weight = 1 / np.sqrt(x * x + y * y)
        value += np.sin(TAU * (x * U + y * V) + phase) * weight
        strength += weight
    return value / max(np.std(value) * 3, .001)


def periodic_distance(p):
    return np.abs((p + .5) % 1 - .5)


def normal(height, strength):
    dx = (np.roll(height, -1, 1) - np.roll(height, 1, 1)) * strength
    dy = (np.roll(height, -1, 0) - np.roll(height, 1, 0)) * strength
    z = np.ones_like(dx)
    norm = np.sqrt(dx * dx + dy * dy + 1)
    return np.stack((-dx / norm, dy / norm, z / norm), -1) * .5 + .5


def meta(path, is_normal=False, linear=False, alpha=False):
    # Existing importer settings and GUIDs belong to the project, never replace them during an art iteration.
    if path.with_name(path.name + ".meta").exists():
        return
    identifier = uuid.uuid5(uuid.NAMESPACE_URL, "crystal-manor/art-district/" + path.name).hex
    path.with_name(path.name + ".meta").write_text(
        "fileFormatVersion: 2\n"
        f"guid: {identifier}\n"
        "TextureImporter:\n"
        "  internalIDToNameTable: []\n"
        "  externalObjects: {}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 1\n"
        f"    sRGBTexture: {0 if linear or is_normal else 1}\n"
        "    borderMipMap: 0\n"
        "    mipMapsPreserveCoverage: 0\n"
        "  bumpmap:\n"
        "    convertToNormalMap: 0\n"
        "    externalNormalMap: 0\n"
        "    heightScale: 0.25\n"
        "    normalMapFilter: 0\n"
        "    flipGreenChannel: 0\n"
        "  isReadable: 0\n"
        "  streamingMipmaps: 0\n"
        "  textureFormat: 1\n"
        f"  maxTextureSize: {SIZE}\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 2\n"
        "    aniso: 4\n"
        "    mipBias: 0\n"
        "    wrapU: 0\n"
        "    wrapV: 0\n"
        "    wrapW: 0\n"
        "  nPOTScale: 0\n"
        "  lightmap: 0\n"
        "  compressionQuality: 50\n"
        "  textureCompression: 1\n"
        f"  textureType: {1 if is_normal else 0}\n"
        "  textureShape: 1\n"
        f"  alphaSource: {1 if alpha else 0}\n"
        "  alphaIsTransparency: 0\n"
        "  platformSettings: []\n"
        "  userData: Original deterministic periodic surface data, seed recipe in tools/generate_district_textures.py\n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n", encoding="utf-8")


def save(name, values, **options):
    path = OUT / (name + ".png")
    data = np.clip(np.rint(values * 255), 0, 255).astype(np.uint8)
    pending = path.with_suffix(".pending")
    Image.fromarray(data).save(pending, format="PNG", optimize=True)
    with Image.open(pending) as check:
        check.load()
    pending.replace(path)
    meta(path, **options)
    return {"file": path.name, "width": SIZE, "height": SIZE,
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def surface(name, base, height, variation, smooth, metallic=0, strength=4, stain=None, cavity=None):
    rgb = np.array(base)[None, None, :] * (1 + variation[..., None])
    if stain is not None:
        mask, color = stain
        rgb = rgb * (1 - mask[..., None]) + np.array(color)[None, None, :] * mask[..., None]
    # Cavity darkening stays in occlusion; lighting still determines the broad surface value.
    if cavity is None:
        cavity = np.zeros_like(height)
    ao = np.clip(.98 - cavity * .30 + height * .025, .65, 1)
    sm = np.clip(smooth + variation * .15, .04, .94)
    mask = np.stack((np.broadcast_to(metallic, height.shape), ao, 1 - sm, sm), -1)
    records.extend([save(name + "_Albedo", rgb),
                    save(name + "_Normal", normal(height, strength), is_normal=True),
                    save(name + "_Mask", mask, linear=True, alpha=True)])


OUT.mkdir(parents=True, exist_ok=True)
records = []
n = noise(2711)
fine = noise(4219, 24, 112, 65)

# Pale dressed limestone/sandstone, four offset courses, with quiet mineral grain.
row = np.floor(V * 4)
joint_x = periodic_distance(U * 2 + (row % 2) * .5)
joint_y = periodic_distance(V * 4)
mortar = np.maximum(np.exp(-(joint_x / .009) ** 2), np.exp(-(joint_y / .018) ** 2))
height = .10 * n + .028 * fine - .32 * mortar
surface("sandstone", (.70, .66, .57), height, n * .065 + fine * .012 - mortar * .11,
        .31 - mortar * .14 + n * .035, strength=7, cavity=mortar)

# Dark volcanic stone, occasional tiny vesicles and slow wave-like mineral seams.
vesicle = np.maximum(0, fine - .49) ** 2
vein = np.exp(-(np.sin(TAU * (3 * U + 2 * V)) + n * .35) ** 2 / .006)
polished = np.clip(.5 + noise(877, 1, 9, 18) * .7, 0, 1)
surface("basalt", (.075, .105, .15), n * .13 - vesicle * .28 + vein * .035,
        n * .11 - vesicle * .15 + vein * .11, .34 + polished * .35 - vesicle * .17,
        strength=7, cavity=np.clip(vesicle * 2, 0, 1))

# Brushed bronze, mild oxidation islands and incised repeating guilloche lines.
patina = np.clip((noise(218) - .08) * .6, 0, .55)
engrave = np.exp(-(np.sin(TAU * (8 * U + 2 * V)) * np.sin(TAU * (8 * U - 2 * V))) ** 2 / .002)
surface("bronze", (.47, .36, .23), .021 * fine - engrave * .11, fine * .025 - engrave * .11,
        .67 - patina * .47 - engrave * .14, .9 * (1 - patina * 1.5), 4,
        (patina, (.07, .19, .18)), cavity=engrave)

# Teal fired ceramic, recessed tile grout and slight uneven kiln glaze.
grout = np.maximum(np.exp(-(periodic_distance(U * 4) / .016) ** 2),
                   np.exp(-(periodic_distance(V * 4) / .016) ** 2))
kiln = np.clip(.5 + noise(727, 1, 6, 12) * .6, 0, 1)
surface("ceramic", (.09, .24, .28), n * .025 - grout * .22, n * .065 - grout * .24,
        .69 + kiln * .13 - grout * .47, .025 * (1 - grout), 6, cavity=grout)

# Opaque cobalt glazing for privacy panels. No translucent sorting or refraction.
surface("glass", (.055, .105, .17), n * .008, n * .035, .87 + n * .025, .18, 2)

# Repeating stylized broadleaf vein relief on opaque modeled leaves.
leaf_vein = np.exp(-(periodic_distance(U * 4 + np.abs(np.sin(V * TAU * 2))) / .026) ** 2)
surface("foliage", (.10, .255, .19), n * .055 + leaf_vein * .11, n * .12 + leaf_vein * .10,
        .32 + n * .045, 0, 4)

# Fantasy cedar bark ridges, periodic around the trunk and along its length.
bark = np.sin(TAU * U * 12 + np.sin(TAU * V * 2) * .7)
moss = np.clip((noise(551, 1, 11, 20) - .15) * .36, 0, .36)
surface("bark", (.15, .18, .175), bark * .19 + n * .17,
        bark * .11 + n * .13, .24 - moss * .2, 0, 9,
        (moss, (.12, .25, .16)), cavity=np.clip(-bark, 0, 1) * .5)

# Woven awning cloth. Fine relief stays moderate to reduce distance shimmer.
weave = np.sin(TAU * U * 96) * np.sin(TAU * V * 96)
stripe = .5 + .5 * np.sin(TAU * U * 8)
surface("fabric", (.29, .19, .35), weave * .04 + n * .012,
        weave * .02 + stripe * .07 + n * .025, .15, 0, 2)

# Sculpted overlapping diamond scales, used only by the hero demonstration dragon.
scale_row = np.floor(V * 16)
scale_column = np.floor(U * 12 + (scale_row % 2) * .5) % 12
cell = np.mod(np.sin(scale_column * 127.1 + scale_row * 311.7) * 43758.5453, 1)
sx = ((U * 12 + (scale_row % 2) * .5) % 1) - .5 + (cell - .5) * .06
sy = (V * 16) % 1
scale = np.clip(1 - np.sqrt((sx / (.55 + cell * .04)) ** 2 + ((sy - .30) / (.77 + cell * .08)) ** 2), 0, 1)
groove = 1 - np.clip(scale * 5, 0, 1)
surface("dragonHide", (.62, .69, .73), scale * .26 + fine * .010,
        scale * .08 + n * .035 + (cell - .5) * .10, .58 + scale * .16 - groove * .19,
        .31 + scale * .22, 5, (groove * .73, (.065, .105, .135)), cavity=groove)

# Leather wing membrane with long branching-like periodic veins.
wing_vein = np.exp(-(np.sin(TAU * U * 5 + np.sin(TAU * V) * .8)) ** 2 / .012)
surface("dragonWing", (.32, .28, .40), n * .025 + wing_vein * .09,
        n * .12 + wing_vein * .13, .36 + n * .06 - wing_vein * .06, .045, 5)

# Packed crystal mineral data, no photographic pixels. R fine fractures, G cloudy inclusions, B growth strata.
# Separate channels allow two inexpensive samples to suggest internal layers without transmission or refraction.
cracks = np.exp(-(np.sin(TAU * (5 * U + 3 * V)) + .2 * np.sin(TAU * V * 7)) ** 2 / .0045)
cracks += np.exp(-(np.sin(TAU * (2 * U - 6 * V))) ** 2 / .0025) * .30
inclusions = np.clip(.54 + noise(491, 1, 13, 22) * .24, .12, .93)
strata = .5 + .5 * np.sin(TAU * (2 * U + 5 * V) + n * .5)
mineral = np.stack((np.clip(.025 + cracks * .88, 0, 1), inclusions, strata * .7 + inclusions * .3), -1)
records.append(save("crystal_Emission", mineral, linear=True))

# Small analytic glow shape for sparse world-space motes, not a screen effect.
r = np.sqrt((U * 2 - 1) ** 2 + (V * 2 - 1) ** 2)
alpha = np.clip(1 - r, 0, 1) ** 3
records.append(save("mote", np.stack((np.ones_like(r), np.ones_like(r), np.ones_like(r), alpha), -1), alpha=True))

(OUT / "texture_manifest.json").write_text(json.dumps({
    "generator": "tools/generate_district_textures.py", "revision": "Visual Renaissance 01B", "seeded": True,
    "source": "Original analytic periodic surface data. No third-party source images.",
    "packing": "Albedo: sRGB; Normal: tangent, linear, Unity NormalMap importer; Mask: R metal, G AO, B roughness, A smoothness. crystal_Emission: linear R fractures, G inclusions, B growth strata.",
    "note": "World surface maps repeat. Mote sprite clamps visually through zero-alpha edges.",
    "maps": records}, indent=2) + "\n", encoding="utf-8")
print(f"Wrote {len(records)} reproducible {SIZE} x {SIZE} maps to {OUT}")
