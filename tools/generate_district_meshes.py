#!/usr/bin/env python3
"""Generate the original Crystal Arcade modular art kit, Visual Renaissance 01C.

One metre is one source unit. Facades face +Z; free-standing props grow up +Y.
Every OBJ has explicit per-face normals, UVs, triangles and a readable importer.
Geometry is intentionally economical, with layered profiles instead of textures
carrying all of the silhouette. Run from any directory with Python 3.9 or newer.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/NeonSyndicate/Resources/ArtDistrict/Models"
TAU = math.tau


def sub(a, b):
    return tuple(a[i] - b[i] for i in range(3))


def add(a, b):
    return tuple(a[i] + b[i] for i in range(3))


def mul(a, s):
    return tuple(v * s for v in a)


def dot(a, b):
    return sum(a[i] * b[i] for i in range(3))


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def norm(a):
    length = math.sqrt(dot(a, a))
    return mul(a, 1 / length) if length > 1e-12 else (0, 1, 0)


class Mesh:
    def __init__(self, name, material, description):
        self.name, self.material, self.description = name, material, description
        self.vertices, self.normals, self.uvs, self.faces = [], [], [], []

    def face(self, points, outward=None, uvs=None):
        """One simple face, flat shaded; triangulate concave profiles by ear clipping."""
        points = list(points)
        # Newell's normal remains stable for concave outlines and collinear starts.
        nx = ny = nz = 0.0
        for i, p in enumerate(points):
            q = points[(i + 1) % len(points)]
            nx += (p[1] - q[1]) * (p[2] + q[2])
            ny += (p[2] - q[2]) * (p[0] + q[0])
            nz += (p[0] - q[0]) * (p[1] + q[1])
        n = norm((nx, ny, nz))
        if outward is not None and dot(n, outward) < 0:
            points.reverse()
            if uvs is not None:
                uvs = list(reversed(uvs))
            n = mul(n, -1)
        axis = max(range(3), key=lambda a: abs(n[a]))
        plane = [(1, 2), (0, 2), (0, 1)][axis]
        flat = [(p[plane[0]], p[plane[1]]) for p in points]
        if uvs is None:
            # Metric planar UVs, no zero-area triangles. Tiling materials use metres.
            uvs = [(p[plane[0]], p[plane[1]]) for p in points]
        start = len(self.vertices)
        self.vertices.extend(points)
        self.normals.extend([n] * len(points))
        self.uvs.extend(uvs)
        def area(a, b, c):
            return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
        orientation = 1 if sum(flat[i][0] * flat[(i + 1) % len(flat)][1] - flat[(i + 1) % len(flat)][0] * flat[i][1] for i in range(len(flat))) > 0 else -1
        remaining = list(range(len(points)))
        while len(remaining) > 3:
            ear = False
            for j, b in enumerate(remaining):
                a, c = remaining[j - 1], remaining[(j + 1) % len(remaining)]
                if area(flat[a], flat[b], flat[c]) * orientation <= 1e-12:
                    continue
                inside = False
                for p in remaining:
                    if p in (a, b, c):
                        continue
                    if min(area(flat[a], flat[b], flat[p]) * orientation,
                           area(flat[b], flat[c], flat[p]) * orientation,
                           area(flat[c], flat[a], flat[p]) * orientation) >= -1e-12:
                        inside = True
                        break
                if inside:
                    continue
                self.faces.append((start + a, start + b, start + c))
                del remaining[j]
                ear = True
                break
            if not ear:
                # Remove a collinear outline vertex, never emit a degenerate face.
                for j, b in enumerate(remaining):
                    a, c = remaining[j - 1], remaining[(j + 1) % len(remaining)]
                    if abs(area(flat[a], flat[b], flat[c])) <= 1e-12:
                        del remaining[j]
                        ear = True
                        break
            if not ear:
                raise ValueError(self.name + ": non-simple polygon")
        if len(remaining) == 3:
            a, b, c = remaining
            if abs(area(flat[a], flat[b], flat[c])) > 1e-12:
                self.faces.append((start + a, start + b, start + c))

    def box(self, center, size):
        x, y, z = center
        a, b, c = (s / 2 for s in size)
        p = [(x - a, y - b, z - c), (x + a, y - b, z - c),
             (x + a, y + b, z - c), (x - a, y + b, z - c),
             (x - a, y - b, z + c), (x + a, y - b, z + c),
             (x + a, y + b, z + c), (x - a, y + b, z + c)]
        for indices, n in [((0, 1, 2, 3), (0, 0, -1)), ((4, 5, 6, 7), (0, 0, 1)),
                           ((0, 4, 7, 3), (-1, 0, 0)), ((1, 5, 6, 2), (1, 0, 0)),
                           ((0, 1, 5, 4), (0, -1, 0)), ((3, 2, 6, 7), (0, 1, 0))]:
            self.face([p[i] for i in indices], n)

    def prism(self, polygon, back, front):
        """Extrude a counterclockwise XY profile to a watertight Z prism."""
        self.face([(x, y, back) for x, y in polygon], (0, 0, -1))
        self.face([(x, y, front) for x, y in polygon], (0, 0, 1))
        for i, (x, y) in enumerate(polygon):
            xx, yy = polygon[(i + 1) % len(polygon)]
            self.face([(x, y, back), (xx, yy, back), (xx, yy, front), (x, y, front)], (yy - y, x - xx, 0))

    def rod(self, start, end, radius, sides=6, end_radius=None):
        direction = norm(sub(end, start))
        ref = (0, 1, 0) if abs(direction[1]) < 0.93 else (1, 0, 0)
        a, b = norm(cross(direction, ref)), norm(cross(direction, norm(cross(direction, ref))))
        end_radius = radius if end_radius is None else end_radius
        rings = []
        for center, r in [(start, radius), (end, end_radius)]:
            rings.append([add(center, add(mul(a, r * math.cos(i * TAU / sides)), mul(b, r * math.sin(i * TAU / sides)))) for i in range(sides)])
        self.face(rings[0], mul(direction, -1))
        self.face(rings[1], direction)
        for i in range(sides):
            j = (i + 1) % sides
            self.face([rings[0][i], rings[0][j], rings[1][j], rings[1][i]],
                      add(sub(rings[0][i], start), sub(rings[0][j], start)))

    def lathe(self, profile, sides=24, center=(0, 0, 0), phase=0, smooth=False):
        """Closed radial shell from ordered (radius, height) cross section."""
        cx, cy, cz = center
        for j in range(len(profile) - 1):
            r1, y1 = profile[j]
            r2, y2 = profile[j + 1]
            for i in range(sides):
                a, b = phase + i * TAU / sides, phase + (i + 1) * TAU / sides
                p = [(cx + r1 * math.cos(a), cy + y1, cz + r1 * math.sin(a)),
                     (cx + r1 * math.cos(b), cy + y1, cz + r1 * math.sin(b)),
                     (cx + r2 * math.cos(b), cy + y2, cz + r2 * math.sin(b)),
                     (cx + r2 * math.cos(a), cy + y2, cz + r2 * math.sin(a))]
                out = (math.cos((a + b) / 2) * (y2 - y1), r1 - r2, math.sin((a + b) / 2) * (y2 - y1))
                # At a pole emit a real triangle instead of a degenerate quad.
                if r1 < 1e-8:
                    p = [p[0], p[2], p[3]]
                elif r2 < 1e-8:
                    p = [p[0], p[1], p[2]]
                start = len(self.vertices)
                self.face(p, out)
                if smooth:
                    for k in range(start, len(self.vertices)):
                        vx, vy, vz = self.vertices[k]
                        length = math.hypot(vx - cx, vz - cz)
                        if length > 1e-8:
                            self.normals[k] = norm(((vx - cx) * (y2 - y1) / length,
                                                   r1 - r2, (vz - cz) * (y2 - y1) / length))
        r, y = profile[0]
        if r > 1e-8:
            self.face([(cx + r * math.cos(phase + i * TAU / sides), cy + y, cz + r * math.sin(phase + i * TAU / sides)) for i in range(sides)], (0, -1, 0))
        r, y = profile[-1]
        if r > 1e-8:
            self.face([(cx + r * math.cos(phase + i * TAU / sides), cy + y, cz + r * math.sin(phase + i * TAU / sides)) for i in range(sides)], (0, 1, 0))

    def arch(self, inner, outer, spring, back, front, segments=24, gap=0):
        """Separate radial voussoirs, joined only when gap is zero."""
        for i in range(segments):
            a, b = i * math.pi / segments + gap, (i + 1) * math.pi / segments - gap
            self.prism([(inner * math.cos(a), spring + inner * math.sin(a)),
                        (outer * math.cos(a), spring + outer * math.sin(a)),
                        (outer * math.cos(b), spring + outer * math.sin(b)),
                        (inner * math.cos(b), spring + inner * math.sin(b))], back, front)

    def pointed_arch(self, inner, outer, spring, inner_rise, outer_rise,
                     back, front, segments=10):
        """Two curved lancets sharing a sharp apex, using economical flat stone ribs."""
        for side in (-1, 1):
            for i in range(segments):
                a, b = i / segments, (i + 1) / segments
                polygon = [lancet(inner, spring, inner_rise, a, side),
                           lancet(outer, spring, outer_rise, a, side),
                           lancet(outer, spring, outer_rise, b, side),
                           lancet(inner, spring, inner_rise, b, side)]
                # Mirroring changes profile winding. Preserve outward reveal normals on the left rib.
                if side < 0:
                    polygon.reverse()
                self.prism(polygon, back, front)

    def ellipse(self, center, radii, sides=16, bands=8):
        """Smooth oblate canopy core. Opaque foliage avoids alpha overdraw."""
        cx, cy, cz = center
        rx, ry, rz = radii
        rings = []
        for j in range(bands + 1):
            p = -math.pi / 2 + j * math.pi / bands
            rings.append([(cx + rx * math.cos(p) * math.cos(i * TAU / sides),
                           cy + ry * math.sin(p),
                           cz + rz * math.cos(p) * math.sin(i * TAU / sides)) for i in range(sides)])
        for j in range(bands):
            for i in range(sides):
                ni = (i + 1) % sides
                p = [rings[j][i], rings[j][ni], rings[j + 1][ni], rings[j + 1][i]]
                if j == 0:
                    p = [p[0], p[2], p[3]]
                if j == bands - 1:
                    p = [p[0], p[1], p[2]]
                out = sub(tuple(sum(v[k] for v in p) / len(p) for k in range(3)), center)
                start = len(self.vertices)
                self.face(p, out)
                for k in range(start, len(self.vertices)):
                    vx, vy, vz = self.vertices[k]
                    self.normals[k] = norm(((vx - cx) / (rx * rx), (vy - cy) / (ry * ry), (vz - cz) / (rz * rz)))

    def leaf(self, base, tip, width):
        """Double-sided folded lanceolate leaf, twelve triangles, no alpha material."""
        axis = sub(tip, base)
        side = mul(norm(cross(axis, (0, 1, 0))), width)
        outline = [base, add(add(base, mul(axis, .30)), side),
                   add(add(base, mul(axis, .65)), mul(side, .78)), tip,
                   sub(add(base, mul(axis, .65)), mul(side, .78)), sub(add(base, mul(axis, .30)), side)]
        center = add(add(base, mul(axis, .48)), (0, .025, 0))
        for i in range(6):
            p = [outline[i], outline[(i + 1) % 6], center]
            self.face(p, (0, 1, 0))
            self.face(p, (0, -1, 0))

    def crystal(self, base, radius, height, lean=(0, 0), phase=0):
        x, y, z = base
        sides = 6
        rings = []
        for t, factor in [(0, 0.78), (0.14, 1.0), (0.75, 0.90)]:
            rings.append([(x + lean[0] * t + radius * factor * math.cos(phase + i * TAU / sides),
                           y + height * t,
                           z + lean[1] * t + radius * factor * math.sin(phase + i * TAU / sides)) for i in range(sides)])
        self.face(rings[0], (0, -1, 0))
        for j in range(2):
            for i in range(sides):
                ni = (i + 1) % sides
                self.face([rings[j][i], rings[j][ni], rings[j + 1][ni], rings[j + 1][i]],
                          (math.cos(phase + (i + .5) * TAU / sides), 0, math.sin(phase + (i + .5) * TAU / sides)))
        tip = (x + lean[0], y + height, z + lean[1])
        for i in range(sides):
            ni = (i + 1) % sides
            self.face([rings[-1][i], rings[-1][ni], tip],
                      (math.cos(phase + (i + .5) * TAU / sides), .4, math.sin(phase + (i + .5) * TAU / sides)))

    def export(self, folder):
        folder.mkdir(parents=True, exist_ok=True)
        path = folder / (self.name + ".obj")
        lines = ["# Original Crystal Arcade kit for GideoVamesAI: Crystal Manor Saga",
                 "# Units: metres. +Y up. Facade exterior: +Z.",
                 "# Generated by tools/generate_district_meshes.py", "o " + self.name]
        lines += ["v %.6f %.6f %.6f" % p for p in self.vertices]
        lines += ["vt %.6f %.6f" % p for p in self.uvs]
        lines += ["vn %.6f %.6f %.6f" % p for p in self.normals]
        lines += ["s off", "usemtl " + self.material]
        lines += ["f " + " ".join("%d/%d/%d" % (i + 1, i + 1, i + 1) for i in f) for f in self.faces]
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        guid = hashlib.md5(("CrystalArcadeKit/" + self.name).encode()).hexdigest()
        path.with_suffix(".obj.meta").write_text("""fileFormatVersion: 2
guid: %s
ModelImporter:
  serializedVersion: 22200
  internalIDToNameTable: []
  externalObjects: {}
  materials:
    materialImportMode: 0
  animations:
    legacyGenerateAnimations: 4
  meshes:
    globalScale: 1
    meshCompression: 0
    addColliders: 0
    useSRGBMaterialColor: 1
    importVisibility: 1
    importBlendShapes: 0
    importCameras: 0
    importLights: 0
    fileIdsGeneration: 2
    swapUVChannels: 0
    generateSecondaryUV: 0
    useFileUnits: 1
    keepQuads: 0
    weldVertices: 1
    bakeAxisConversion: 1
    preserveHierarchy: 0
    indexFormat: 0
    isReadable: 1
  tangentSpace:
    normalSmoothAngle: 60
    normalImportMode: 0
    tangentImportMode: 3
    normalCalculationMode: 4
    normalSmoothingSource: 0
  importAnimation: 0
  humanoidOversampling: 1
  avatarSetup: 0
  animationType: 0
  userData: Original mesh; one source unit is one metre; runtime assigns shared materials.
  assetBundleName:
  assetBundleVariant:
""" % guid, encoding="utf-8")
        mins = [min(v[i] for v in self.vertices) for i in range(3)]
        maxs = [max(v[i] for v in self.vertices) for i in range(3)]
        return {"name": self.name, "resource": "ArtDistrict/Models/" + self.name,
                "material": self.material, "description": self.description,
                "triangles": len(self.faces), "source_vertices": len(self.vertices),
                "bounds_min": [round(v, 4) for v in mins],
                "bounds_max": [round(v, 4) for v in maxs],
                "dimensions_metres": [round(maxs[i] - mins[i], 4) for i in range(3)],
                "pivot": "Base centre unless mounting note specifies paired origin",
                "colliders": "None; runtime retains original world colliders",
                "sha256": hashlib.sha256(path.read_bytes()).hexdigest()}


def lancet(radius, spring, rise, t, side=1):
    """Quadratic shoulder meeting a pointed apex. Used by frames and their exact panes."""
    s = 1 - t
    return (side * radius * (s * s + 1.46 * s * t), spring + rise * (1.28 * s * t + t * t))


def lancet_pane(radius, spring, rise, sill=0, segments=10):
    """Counterclockwise window outline, leaving one vertex at the apex."""
    return [(-radius, sill), (radius, sill)] + [lancet(radius, spring, rise, i / segments) for i in range(segments + 1)] + [lancet(radius, spring, rise, i / segments, -1) for i in range(segments - 1, -1, -1)]


def build_kit():
    kit = []
    def mesh(name, material, desc):
        m = Mesh(name, material, desc)
        kit.append(m)
        return m

    m = mesh("block", "sandstone", "Unit backing volume. Scale to wall or glass panel dimensions.")
    m.box((0, .5, 0), (1, 1, 1))

    m = mesh("arch_frame", "sandstone", "01C monumental pointed arcade with deeper bevelled jambs and a faceted keystone. Original paired pane and four-metre source apex; runtime spans multiple storeys.")
    for side in (-1, 1):
        for row in range(6):
            m.box((side * 1.34, (row + .5) * 2.1 / 6, 0), (.32, 2.1 / 6 - .014, .28))
        m.box((side * 1.35, .08, .015), (.36, .16, .28))
        m.box((side * 1.34, 2.06, .02), (.36, .08, .30))
        m.box((side * 1.195, 1.05, .145), (.055, 2.1, .075))
        m.box((side * 1.475, 1.05, .13), (.05, 2.1, .05))
    m.pointed_arch(1.18, 1.50, 2.1, 1.61, 1.9, -.14, .14)
    m.pointed_arch(1.178, 1.235, 2.1, 1.609, 1.661, .14, .195)
    m.pointed_arch(1.45, 1.50, 2.1, 1.855, 1.9, .14, .17)
    # The keystone is opaque cut mineral, not another glow strip. Its broad facets hold up at distance.
    m.prism([(-.16, 3.78), (0, 3.62), (.16, 3.78), (0, 4.0)], .15, .26)
    for side in (-1, 1):
        x = side * 1.34
        m.prism([(x - .11, .28), (x + .11, .28), (x + .085, 1.89),
                 (x, 2.07), (x - .085, 1.89)], .145, .245)

    m = mesh("arch_frame_lod", "sandstone", "01C economical far-LOD arcade. Matches the hero frame outer and inner apex, jamb spacing and four-metre height; reduced curved ribs.")
    for side in (-1, 1):
        m.box((side * 1.34, 1.05, 0), (.32, 2.1, .28))
    m.pointed_arch(1.18, 1.50, 2.1, 1.61, 1.9, -.14, .14, segments=5)

    m = mesh("arch_infill", "glass", "01B pointed pane mathematically paired with arch_frame; opaque recessed glazing, no rectangular spill above arch.")
    m.prism(lancet_pane(1.178, 2.1, 1.608), -.09, -.05)

    m = mesh("window_frame", "bronze", "01B pointed bronze window with stepped lancet mouldings, central mullion and an angular crown. Same mounting origin.")
    for x in (-.95, .95):
        m.box((x, .80, 0), (.10, 1.6, .22))
        m.box((x, .80, .135), (.035, 1.58, .045))
    m.pointed_arch(.90, 1.0, 1.6, 1.05, 1.18, -.11, .11)
    m.pointed_arch(.919, .958, 1.6, 1.075, 1.125, .11, .16)
    m.box((0, .065, .015), (2.04, .13, .24))
    m.box((0, .17, .05), (1.95, .05, .15))
    m.box((0, 1.42, .025), (.04, 2.58, .095))
    m.box((0, 1.05, 0), (1.82, .045, .08))
    for x in (-.95, .95):
        for y in (.35, 1.40):
            m.prism([(x, y - .065), (x + .07, y), (x, y + .065), (x - .07, y)], .10, .14)
    m.prism([(-.07, 2.70), (0, 2.60), (.07, 2.70), (0, 2.80)], .11, .17)

    m = mesh("window_infill", "glass", "01B pointed pane paired exactly with window_frame inner lancet; now used by district upper storeys.")
    m.prism(lancet_pane(.899, 1.6, 1.049, .14), -.08, -.05)

    m = mesh("lattice_panel", "bronze", "Open geometric diamond screen with nested eight-point rosettes; actual open spaces.")
    for x in (-.85, .85):
        m.box((x, 1.15, 0), (.04, 2.3, .045))
    for y in (.02, 2.28):
        m.box((0, y, 0), (1.7, .04, .045))
    for row in range(3):
        for col in range(2):
            x, y = -.42 + col * .84, .40 + row * .75
            for angle_offset, radius in [(0, .36), (math.pi / 4, .255)]:
                p = [(x + radius * math.cos(angle_offset + i * math.pi / 2),
                      y + radius * math.sin(angle_offset + i * math.pi / 2), 0) for i in range(4)]
                for i in range(4):
                    m.rod(p[i], p[(i + 1) % 4], .019, 4)
            m.rod((x, y - .36, 0), (x, y + .36, 0), .012, 4)
            m.rod((x - .36, y, 0), (x + .36, y, 0), .012, 4)

    m = mesh("balcony", "bronze", "Solid projecting deck, rounded rails, turned posts, central fan and open diamond balusters. Mount above y3.")
    m.box((0, .075, .2), (3, .15, 1.2))
    m.box((0, .16, .2), (2.94, .045, 1.17))
    for y, r in [(.27, .028), (1.05, .045)]:
        m.rod((-1.44, y, .76), (1.44, y, .76), r, 8)
        for x in (-1.44, 1.44):
            m.rod((x, y, -.35), (x, y, .76), r, 8)
    for x in [-1.44, -.96, -.48, 0, .48, .96, 1.44]:
        m.rod((x, .18, .76), (x, 1.06, .76), .029, 6)
        m.lathe([(.033, .38), (.058, .41), (.034, .46)], 8, (x, 0, .76))
        m.lathe([(.034, .82), (.058, .85), (.033, .88)], 8, (x, 0, .76))
    for x in [-1.2, -.72, -.24, .24, .72, 1.2]:
        p = [(x, .37, .76), (x + .17, .65, .76), (x, .93, .76), (x - .17, .65, .76)]
        for i in range(4):
            m.rod(p[i], p[(i + 1) % 4], .016, 4)
    for x in (-1.44, 1.44):
        for z in (-.35, .2):
            m.rod((x, .18, z), (x, 1.06, z), .035, 6)

    m = mesh("cornice", "sandstone", "01B crisp stepped roof ledge with three inset mineral joints; broad light-catching planes replace tiny dentil repetition.")
    for y, h, z, depth in [(.045, .09, -.03, .12), (.14, .10, .015, .21),
                             (.275, .15, .06, .30), (.405, .07, .11, .35)]:
        m.box((0, y, z), (4, h, depth))
    for x in (-1.33, 0, 1.33):
        m.prism([(x - .06, .03), (x + .06, .03), (x + .055, .19), (x, .25), (x - .055, .19)], .115, .18)

    m = mesh("pilaster", "sandstone", "01C blade-shaped mineral buttress with a broad faceted ridge, shouldered base and pointed crest. Original four-metre mounting span.")
    for y, h, width, depth in [(.065, .13, .65, .28), (.17, .08, .56, .24), (.25, .08, .48, .21)]:
        m.box((0, y, 0), (width, h, depth))
    # Five broad cut-mineral planes replace the narrow rounded column silhouette.
    rings = []
    for y, radius in [(.29, .24), (.38, .24), (3.42, .20), (3.53, .16)]:
        ring = []
        for i in range(7):
            a = i * math.pi / 6
            ring.append((radius * math.cos(a), y, radius * 1.42 * math.sin(a)))
        rings.append(ring)
    for j in range(3):
        for i in range(6):
            mid = (i + .5) * math.pi / 6
            m.face([rings[j][i], rings[j][i + 1], rings[j + 1][i + 1], rings[j + 1][i]], (math.cos(mid), 0, math.sin(mid)))
    m.face(rings[0], (0, -1, 0))
    m.face(rings[-1], (0, 1, 0))
    m.face([rings[0][0], rings[0][-1], rings[-1][-1], rings[-1][0]], (0, 0, -1))
    m.prism([(-.28, 3.50), (.28, 3.50), (.22, 3.77), (0, 4.0), (-.22, 3.77)], -.12, .24)
    m.prism([(-.11, 3.44), (.11, 3.44), (.11, 3.75), (0, 3.99), (-.11, 3.75)], .24, .36)

    m = mesh("parapet", "sandstone", "01B five tapering mineral blades with alternating height and a continuous coping; original four-metre roof bay.")
    m.box((0, .21, 0), (4, .42, .22))
    m.box((0, .07, .012), (4, .14, .29))
    for i in range(5):
        x = -1.6 + i * .8
        tip = 1.0 if i % 2 == 0 else .83
        m.prism([(x - .17, .38), (x + .17, .38), (x + .14, .64),
                 (x, tip), (x - .14, .64)], -.11, .11)
    m.box((0, .43, .04), (4, .07, .29))

    m = mesh("dome", "ceramic", "01C split mineral crown with three unequal pointed blades and a low octagonal seat. Original resource name, metre scale and roof mount retained.")
    m.lathe([(1, 0), (1, .06), (.94, .10), (.94, .16), (.72, .27), (.61, .29)], 8)
    m.crystal((0, .16, 0), .31, .92, (.04, 0), .52)
    m.crystal((-.49, .12, 0), .25, .62, (-.11, -.05), .23)
    m.crystal((.48, .12, .07), .24, .69, (.13, .04), .78)

    m = mesh("crystal_cluster", "crystal", "Seven asymmetrical six-sided crystal prisms with bevelled bases and pointed terminations.")
    for base, radius, height, lean, phase in [((0, 0, 0), .33, 3, (.12, -.1), .3),
              ((-.49, 0, -.04), .25, 2.1, (-.22, .08), .8), ((.45, 0, -.20), .24, 1.72, (.30, -.13), 1.2),
              ((.17, 0, .39), .26, 1.52, (.12, .28), .5), ((-.35, 0, .35), .22, 1.14, (-.26, .2), .1),
              ((-.18, 0, -.42), .24, 2.27, (-.12, -.15), 1.0), ((.54, 0, .28), .18, .89, (.20, .13), .2)]:
        m.crystal(base, radius, height, lean, phase)

    m = mesh("crystal_shard", "crystal", "01C structural crystal blade with subsidiary cut facets. Mounted into full-height facade piers, never loose on a road.")
    m.crystal((0, 0, 0), .20, 1.0, (.025, 0), .3)
    m.crystal((-.13, .09, -.03), .078, .65, (-.05, -.015), .1)
    m.crystal((.13, .13, .025), .065, .54, (.035, .01), .4)

    m = mesh("lantern", "bronze", "Six-sided pierced bronze lantern cage, domed crown and suspension ring. Place lantern_core at same origin.")
    m.lathe([(.14, 0), (.18, .08), (.25, .15), (.22, .20), (.22, .24)], 12)
    m.lathe([(.22, .93), (.27, .99), (.23, 1.04), (.19, 1.12), (.13, 1.18), (.08, 1.23), (0, 1.29)], 12)
    for i in range(6):
        a = i * TAU / 6
        x, z = .21 * math.cos(a), .21 * math.sin(a)
        m.rod((x, .22, z), (x, .97, z), .017, 6)
    # Hanging ring in XY plane.
    for i in range(12):
        a, b = i * TAU / 12, (i + 1) * TAU / 12
        m.rod((.075 * math.cos(a), 1.38 + .105 * math.sin(a), 0),
              (.075 * math.cos(b), 1.38 + .105 * math.sin(b), 0), .014, 5)

    m = mesh("lantern_core", "crystalWarm", "Emissive six-sided inset for lantern, paired origin. Use no realtime point light by default.")
    m.lathe([(.13, .25), (.17, .32), (.17, .85), (.11, .94)], 6)

    m = mesh("planter", "ceramic", "Octagonal elongated planter with stepped foot, flared body and thick rolled lip; open top.")
    # Elliptical lathe to keep the vessel watertight with a visible inner bowl.
    m.lathe([(.72, 0), (.79, .08), (.78, .13), (.90, .50), (1, .53), (1, .63),
             (.95, .65), (.85, .61), (.84, .54), (.71, .18), (0, .18)], 12)
    m.vertices = [(x, y, z * .47) for x, y, z in m.vertices]
    m.normals = [norm((x, y, z / .47)) for x, y, z in m.normals]

    m = mesh("tree_trunk", "bark", "Sculptural six-limbed desert tree with tapered curved boughs and exposed buttress roots. Pair tree_canopy at same origin.")
    for i in range(5):
        a = i * TAU / 5
        m.rod((0, .30, 0), (.62 * math.cos(a), .035, .62 * math.sin(a)), .13, 7, .035)
    m.rod((0, .02, 0), (.10, 1.2, -.05), .24, 10, .17)
    m.rod((.10, 1.2, -.05), (-.14, 2.2, .04), .17, 10, .13)
    m.rod((-.14, 2.2, .04), (.10, 3.2, .08), .13, 9, .065)
    for i in range(6):
        a = .35 + i * TAU / 6
        y = 1.7 + .22 * (i % 3)
        start = (-.03, y, .02)
        mid = (.64 * math.cos(a), y + .55, .64 * math.sin(a))
        tip = (1.35 * math.cos(a), y + 1.08, 1.35 * math.sin(a))
        m.rod(start, mid, .10, 8, .063)
        m.rod(mid, tip, .063, 7, .018)
        fork = (1.15 * math.cos(a + .42), y + 1.28, 1.15 * math.sin(a + .42))
        m.rod(mid, fork, .038, 6, .009)

    m = mesh("tree_canopy", "foliage", "Layered umbrella canopy with 88 folded lanceolate leaves around smooth opaque crown cores. No alpha overdraw. Pair origin with tree_trunk.")
    m.ellipse((.07, 3.25, .04), (1.18, .46, 1.14), 14, 6)
    for j in range(16):
        a = j * TAU / 16
        m.leaf((.07 + .60 * math.cos(a), 3.66, .04 + .57 * math.sin(a)),
               (.07 + 1.30 * math.cos(a), 3.35, .04 + 1.26 * math.sin(a)), .13)
    for i in range(6):
        a = .35 + i * TAU / 6
        y = 2.78 + .22 * (i % 3)
        cx, cz = 1.17 * math.cos(a), 1.17 * math.sin(a)
        m.ellipse((cx, y, cz), (.79, .32, .78), 12, 5)
        for j in range(12):
            b = a + j * TAU / 12
            m.leaf((cx + .34 * math.cos(b), y + .30, cz + .34 * math.sin(b)),
                   (cx + .95 * math.cos(b), y + .04, cz + .93 * math.sin(b)), .105)

    m = mesh("tree_canopy_lod", "foliage", "Low-detail single-volume canopy alternative for distant trees, same tree origin.")
    m.ellipse((0, 3.10, 0), (1.95, .55, 1.9), 10, 4)

    m = mesh("canopy", "fabric", "Draped textile awning with radial seams and scalloped front valance. Wall z0, front z1.5; mount above y3.")
    nx, nz = 20, 8
    def canopy_point(ix, iz):
        x, z = -1.5 + 3 * ix / nx, 1.5 * iz / nz
        y = .42 * (1 - z / 1.5) - .11 * math.sin(math.pi * z / 1.5)
        y += .022 * math.cos(ix * math.pi) * math.sin(math.pi * z / 1.5)
        return (x, y + .22, z)
    for ix in range(nx):
        for iz in range(nz):
            p = [canopy_point(ix, iz), canopy_point(ix + 1, iz), canopy_point(ix + 1, iz + 1), canopy_point(ix, iz + 1)]
            m.face(p, (0, 1, 0))
            m.face([(x, y - .012, z) for x, y, z in p], (0, -1, 0))
    for ix in range(30):
        x1, x2 = -1.5 + ix * .10, -1.5 + (ix + 1) * .10
        low1, low2 = .035 + .07 * math.cos(ix * TAU / 6), .035 + .07 * math.cos((ix + 1) * TAU / 6)
        p = [(x1, low1, 1.5), (x2, low2, 1.5), (x2, .22, 1.5), (x1, .22, 1.5)]
        m.face(p, (0, 0, 1))
        m.face([(x, y, z - .012) for x, y, z in p], (0, 0, -1))
    # Base pivot: move smallest scallop tip to y0.
    y_min = min(v[1] for v in m.vertices)
    m.vertices = [(x, y - y_min, z) for x, y, z in m.vertices]

    m = mesh("canopy_bracket", "bronze", "Two swept support arms paired with canopy origin, plus front rail.")
    for x in (-1.44, 1.44):
        m.rod((x, .52, 0), (x, .08, 1.48), .027, 6)
        m.rod((x, .02, 0), (x, .08, 1.48), .022, 6)
    m.rod((-1.5, .25, 1.49), (1.5, .25, 1.49), .025, 8)

    m = mesh("obelisk", "basalt", "Chamfered civic plinth with recessed tiers, tapering faceted body and crystal mounting seat.")
    m.lathe([(.62, 0), (.65, .10), (.65, .18), (.54, .23), (.54, .34), (.45, .40),
             (.36, 1.85), (.43, 1.91), (.43, 2.01), (.30, 2.08)], 8, phase=math.pi / 8)

    m = mesh("rosette", "bronze", "Eight-point star medallion for facade crests. Geometric invented motif.")
    for i in range(8):
        a = i * TAU / 8
        p = [(0, .50), (.50 * math.cos(a - .09), .50 + .50 * math.sin(a - .09)),
             (.60 * math.cos(a), .50 + .50 * math.sin(a)),
             (.50 * math.cos(a + .09), .50 + .50 * math.sin(a + .09))]
        # Separate convex kite petals, no self-intersection.
        m.prism(p, -.04, .04)
    m.rod((0, .50, -.05), (0, .50, .06), .15, 12)

    return kit


def validate(mesh):
    """Fail on non-finite data, missing channels or degenerates before shipping."""
    if not mesh.faces:
        raise ValueError(mesh.name + ": empty mesh")
    if len(mesh.vertices) != len(mesh.normals) or len(mesh.vertices) != len(mesh.uvs):
        raise ValueError(mesh.name + ": vertex channel mismatch")
    for value in mesh.vertices + mesh.normals + mesh.uvs:
        if not all(math.isfinite(v) for v in value):
            raise ValueError(mesh.name + ": non-finite value")
    for face in mesh.faces:
        a, b, c = [mesh.vertices[i] for i in face]
        n = cross(sub(b, a), sub(c, a))
        if dot(n, n) < 1e-16:
            raise ValueError(mesh.name + ": degenerate triangle")
        if dot(n, mesh.normals[face[0]]) <= 0:
            raise ValueError(mesh.name + ": inconsistent normal")
    if len(mesh.faces) > 3000:
        raise ValueError(mesh.name + ": unexpectedly heavy mesh")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=OUT)
    args = parser.parse_args()
    records = []
    for mesh in build_kit():
        validate(mesh)
        records.append(mesh.export(args.output))
    inventory = {"collection": "Crystal Arcade original modular kit", "version": "01C",
                 "authorship": "Original geometry authored for this project; no third-party geometry or textures.",
                 "units": "metres", "up": "+Y", "facade_front": "+Z", "static_geometry": True,
                 "uvs": "Metric planar projections, explicit per-face UV0", "normals": "Explicit normals, smooth foliage cores, crisp mineral crowns, architecture and crystal edges",
                 "model_import": "Readable, material import disabled, unit scale, no generated colliders",
                 "runtime_materials": "Supplied by DistrictMaterials, one shared material per module.",
                 "validation": "Finite vertices; matching channels; no degenerate triangles; matching normal winding; triangle cap.",
                 "unity_verification": "Pending. No Unity Editor or GPU was available during authoring.",
                 "asset_count": len(records), "total_triangles": sum(m["triangles"] for m in records), "assets": records}
    (args.output / "mesh_inventory.json").write_text(json.dumps(inventory, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"output": str(args.output), "asset_count": len(records),
                      "total_triangles": inventory["total_triangles"],
                      "validation": "PASS", "triangles": {m["name"]: m["triangles"] for m in records}}, indent=2))


if __name__ == "__main__":
    main()
