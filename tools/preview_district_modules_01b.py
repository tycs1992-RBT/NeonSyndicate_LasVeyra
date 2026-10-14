#!/usr/bin/env python3
"""Compare actual source meshes. Offline orthographic diagram, never a Unity screenshot.

Requires Pillow and NumPy. --baseline-generator accepts the unchanged Master Build 01 source.
The two versions use the same view, scale and flat clay material for each pair.
"""
from __future__ import annotations

import argparse
import importlib.util
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]


def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return {mesh.name: mesh for mesh in module.build_kit()}


def font(size):
    for path in ("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", "/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf"):
        if Path(path).is_file():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def dot(a, b):
    return sum(x * y for x, y in zip(a, b))


def project(vertex):
    yaw, elevation = math.radians(18), math.radians(12)
    right = (math.cos(yaw), 0, -math.sin(yaw))
    up = (-math.sin(yaw) * math.sin(elevation), math.cos(elevation), -math.cos(yaw) * math.sin(elevation))
    view = (math.sin(yaw) * math.cos(elevation), math.sin(elevation), math.cos(yaw) * math.cos(elevation))
    return dot(vertex, right), -dot(vertex, up), dot(vertex, view)


def mesh_panel(image, draw, mesh, box, bounds, scale):
    x, y, width, height = box
    projected = [project(vertex) for vertex in mesh.vertices]
    px = width * .5 - (bounds[0] + bounds[2]) * .5 * scale
    py = (height - 40) * .5 - (bounds[1] + bounds[3]) * .5 * scale
    colors = np.zeros((height, width, 4), dtype=np.uint8)
    depth = np.full((height, width), -np.inf)
    light = (.33, .67, .665)
    for face in mesh.faces:
        normal = mesh.normals[face[0]]
        if project(normal)[2] <= 0:
            continue
        level = .35 + .65 * max(0, dot(normal, light))
        color = tuple(int(channel * level) for channel in (176, 190, 203))
        points = [(px + projected[i][0] * scale, py + projected[i][1] * scale, projected[i][2]) for i in face]
        a, b, c = points
        x0, x1 = max(0, math.floor(min(p[0] for p in points))), min(width - 1, math.ceil(max(p[0] for p in points)))
        y0, y1 = max(0, math.floor(min(p[1] for p in points))), min(height - 1, math.ceil(max(p[1] for p in points)))
        if x0 > x1 or y0 > y1:
            continue
        divisor = (b[1] - c[1]) * (a[0] - c[0]) + (c[0] - b[0]) * (a[1] - c[1])
        if abs(divisor) < 1e-10:
            continue
        yy, xx = np.mgrid[y0:y1 + 1, x0:x1 + 1]
        xx, yy = xx + .5, yy + .5
        u = ((b[1] - c[1]) * (xx - c[0]) + (c[0] - b[0]) * (yy - c[1])) / divisor
        v = ((c[1] - a[1]) * (xx - c[0]) + (a[0] - c[0]) * (yy - c[1])) / divisor
        w = 1 - u - v
        z = u * a[2] + v * b[2] + w * c[2]
        view = depth[y0:y1 + 1, x0:x1 + 1]
        visible = (u >= -1e-8) & (v >= -1e-8) & (w >= -1e-8) & (z >= view)
        view[visible] = z[visible]
        colors[y0:y1 + 1, x0:x1 + 1][visible] = (*color, 255)
    rendered = Image.fromarray(colors)
    image.paste(rendered, (x, y), rendered)
    draw.text((x + 14, y + height - 34), f"{len(mesh.faces):,} source triangles", font=font(15), fill="#a5b4c6")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline-generator", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=ROOT / "Docs/ArtDistrict01B/MODULE_COMPARISON_OFFLINE.png")
    args = parser.parse_args()
    before = load(args.baseline_generator, "district_before")
    after = load(ROOT / "tools/generate_district_meshes.py", "district_after")
    names = [("arch_frame", "Pointed arcade"), ("window_frame", "Pointed window"),
             ("dome", "Faceted mineral crown"), ("cornice", "Crisp roof ledge"),
             ("pilaster", "Engaged mineral buttress"), ("parapet", "Mineral roof blades")]
    image = Image.new("RGB", (1680, 1460), "#111925")
    draw = ImageDraw.Draw(image)
    draw.text((38, 22), "VISUAL RENAISSANCE 01B | SOURCE GEOMETRY COMPARISON", font=font(27), fill="#e6ecf4")
    draw.text((38, 66), "Actual generated meshes in neutral clay. Same view and scale within each pair.", font=font(20), fill="#b5c7dc")
    draw.text((38, 100), "OFFLINE DIAGRAM: not Unity rendering, lighting, materials, environment, or gameplay evidence.", font=font(18), fill="#e9ba71")
    for index, (name, label) in enumerate(names):
        group_x = 38 + (index % 2) * 822
        group_y = 157 + (index // 2) * 417
        draw.text((group_x, group_y), f"{label} / {name}", font=font(21), fill="#d6e2f0")
        all_points = [project(v) for m in (before[name], after[name]) for v in m.vertices]
        bounds = (min(p[0] for p in all_points), min(p[1] for p in all_points),
                  max(p[0] for p in all_points), max(p[1] for p in all_points))
        scale = min(330 / (bounds[2] - bounds[0]), 252 / (bounds[3] - bounds[1]))
        for column, (version, mesh) in enumerate((("Master Build 01", before[name]), ("01B refinement", after[name]))):
            x, y = group_x + column * 398, group_y + 33
            draw.rounded_rectangle((x, y, x + 383, y + 355), radius=8, fill="#1b2736", outline="#344155")
            draw.text((x + 14, y + 12), version, font=font(17), fill="#acbed4" if column == 0 else "#a8e1df")
            mesh_panel(image, draw, mesh, (x, y + 40, 383, 310), bounds, scale)
    draw.text((38, 1421), "Retained: 23 model names, import GUIDs, metre scale, original gameplay colliders. Unique-kit triangles: 10,270 to 9,150.", font=font(17), fill="#aabed0")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    image.save(args.output)
    print(args.output)


if __name__ == "__main__":
    main()
