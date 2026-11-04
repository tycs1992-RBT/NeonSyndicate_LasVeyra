"""Matched player-height architecture cameras and isolated service silhouettes. No native rendering."""
import argparse
import subprocess
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
CAMERAS = {
    'home': ('Home approach', (-6, 1.82, -14), (0, 6, -41)),
    'court': ('Court, looking to Dante', (10, 1.82, 61), (75, 7.5, 106)),
    'workshop': ('Forge market junction', (74, 1.82, 78), (76, 7, 105)),
    'seat': ('Supplemental flight view, looking into the seating recess', (-1.2, 11.8, -28.8), (-4.45, 11.55, -34.5)),
    'bore': ('Supplemental flight view, looking through the exhaust rim', (86, 21.0, 102), (80.1, 11.8, 109)),
    'return': ('Shardwood return, looking toward the settlement', (-76, 1.82, 30), (-5, 6, 66)),
}
SIZE = (768, 432)
INK = (28, 41, 43)
PAPER = (236, 236, 227)
FONT = '/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'


def silhouette(path):
    # Same perspective, scale and local camera for both buildings and both revisions.
    eye, target = np.array([-14., 1.67, -35.]), np.array([0., 7., 10.])
    front = target - eye
    front /= np.linalg.norm(front)
    right = np.cross([0, 1, 0], front)
    right /= np.linalg.norm(right)
    up = np.cross(front, right)
    vertices, faces = [], []
    with path.open() as stream:
        for line in stream:
            fields = line.split()
            if fields[:1] == ['v']:
                vertices.append([float(x) for x in fields[1:4]])
            elif fields[:1] == ['f']:
                faces.append([int(x) - 1 for x in fields[1:4]])
    points = np.asarray(vertices) - eye
    depth = points @ front
    assert np.all(depth > 0)
    w, h = SIZE
    focal = h * .5 / np.tan(.38)
    projected = np.column_stack((w/2 + focal*(points @ right)/depth,
                                 h/2 - focal*(points @ up)/depth))
    image = Image.new('RGB', SIZE, (221, 227, 223))
    draw = ImageDraw.Draw(image)
    for face in faces:
        draw.polygon([tuple(x) for x in projected[face]], fill=INK)
    return image


def sheet(path, title, rows, footer):
    w, h = SIZE
    image = Image.new('RGB', (2*w, 92+len(rows)*(h+37)+42), PAPER)
    draw = ImageDraw.Draw(image)
    large, small = [ImageFont.truetype(FONT, pts) for pts in (25, 17)]
    draw.text((19, 12), title, fill=INK, font=large)
    draw.text((19, 58), 'BEFORE   /   bb700625', fill=INK, font=small)
    draw.text((w+19, 58), 'AFTER   /   Lofts terrace and Dante exhaust crown', fill=INK, font=small)
    for i, (label, left, right) in enumerate(rows):
        top = 92 + i*(h+37)
        draw.text((19, top+5), label, fill=INK, font=small)
        image.paste(left, (0, top+34))
        image.paste(right, (w, top+34))
    draw.text((19, image.height-32), footer, fill=INK, font=small)
    image.quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE).save(path, optimize=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ['before', 'after', 'output', 'working']:
        parser.add_argument(name, type=Path)
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    args.working.mkdir(parents=True, exist_ok=True)
    textures = args.working / 'albedo'
    textures.mkdir(exist_ok=True)
    res = ROOT/'Assets/NeonSyndicate/Resources'
    files = sorted((res/'AscensionH2/Textures').glob('*_Albedo.png'))
    files += [res/'AscensionH3/Textures/masonry_Albedo.jpg', res/'AscensionH3/Textures/basalt_Albedo.jpg',
              res/'AscensionH5/Textures/iron_Albedo.jpg']
    for file in files:
        Image.open(file).convert('RGB').resize((256, 256), Image.Resampling.LANCZOS).save(textures/(file.stem.split('_Albedo')[0]+'.ppm'))
    raster = args.working/'raster'
    subprocess.run(['c++', '-O2', '-std=c++17', ROOT/'tools/render_science_circuit.cpp', '-o', raster], check=True)
    rows = {}
    for name, (label, eye, look) in CAMERAS.items():
        pair = []
        for side, folder in [('before', args.before), ('after', args.after)]:
            path = args.working/(side+'_'+name+'.ppm')
            subprocess.run([raster, folder/'circuit.bin', textures, path, *map(str, (*eye, *look))], check=True)
            pair.append(Image.open(path).resize(SIZE, Image.Resampling.LANCZOS))
        rows[name] = (label, *pair)
    footer = 'Software camera. Exported meshes, retained rooms and sampled albedo. Approximate daylight and ground. No emission, native shadows or actors.'
    for key, names in [('landmarks', ['home', 'workshop']), ('sightlines', ['court', 'return']),
                       ('upper_surfaces', ['seat', 'bore'])]:
        sheet(args.output/('lofts_forge_'+key+'.png'), 'LAS VEYRA   /   '+key.replace('_', ' ').upper(), [rows[name] for name in names], footer)
    flat = [(label, *[silhouette(folder/(name+'_after.obj')) for folder in [args.before, args.after]])
            for name, label in [('lofts', 'Lofts'), ('forge', 'Forge')]]
    sheet(args.output/'lofts_forge_silhouettes.png', 'SERVICE PROFILES   /   FLAT SILHOUETTES', flat,
          'Isolated exported service meshes, identical local perspective across all four panels. No texture, light or glow. Separate room owner omitted.')


if __name__ == '__main__':
    main()
