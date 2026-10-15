#!/usr/bin/env python3
"""Validate committed 04A geometry, textures, metadata and gameplay preservation.

This reads actual authored data. It is not a Unity import, compiler, playtest,
physics simulation or performance benchmark. Requires numpy and Pillow.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import subprocess
import sys

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/NeonSyndicate/Resources/VisualRebirth04A"
BASE = "8af8559fa646672d94f2c27ea04e3e2e0a1728cf"
RUNTIME = "Assets/NeonSyndicate/Scripts/Runtime/"
ROLES = {"stone", "trim", "roof", "metal", "gilt", "bark", "leaf", "cloth",
         "crystal", "water", "glass", "paving", "soil", "flower"}


class Audit:
    def __init__(self):
        self.checks = []
        self.meshes = {}
        self.layouts = {}
        self.warnings = []

    def check(self, name, result, detail=None):
        row = {"name": name, "passed": bool(result)}
        if detail is not None:
            row["detail"] = detail
        self.checks.append(row)
        return bool(result)


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT)


def decode(path):
    raw = path.read_bytes()
    if len(raw) < 8 or raw[:4] != b"CMH1":
        raise ValueError("Missing CMH1 header (possibly an unresolved LFS pointer)")
    count = struct.unpack("<i", raw[4:8])[0]
    if count < 3 or count > 300000 or count % 3 or len(raw) != 8 + count * 32:
        raise ValueError("Invalid triangle count or payload length")
    return raw, np.frombuffer(raw, dtype="<f4", offset=8).reshape(count, 8)


def obj_vertices(path):
    positions, normals, uvs, triangles = [], [], [], []
    for line in path.read_text(encoding="utf-8").splitlines():
        items = line.split()
        if not items:
            continue
        if items[0] == "v":
            positions.append([float(n) for n in items[1:4]])
        elif items[0] == "vn":
            normals.append([float(n) for n in items[1:4]])
        elif items[0] == "vt":
            uvs.append([float(n) for n in items[1:3]])
        elif items[0] == "f":
            if len(items) != 4:
                raise ValueError("OBJ must contain triangulated faces")
            triangles.extend([[int(n) - 1 for n in item.split("/")] for item in items[1:]])
    if not triangles:
        raise ValueError("OBJ has no triangles (possibly an unresolved LFS pointer)")
    data = []
    for face in triangles:
        if len(face) != 3 or min(face) < 0:
            raise ValueError("OBJ requires positive position/UV/normal indices")
        data.append(positions[face[0]] + normals[face[2]] + uvs[face[1]])
    return np.asarray(data)


def validate_geometry(audit):
    paths = sorted((ART / "Meshes").glob("*.bytes"))
    audit.check("CMH1 geometry assets exist", len(paths) >= 60, len(paths))
    for path in paths:
        name = path.stem
        try:
            raw, data = decode(path)
            audit.check(name + ": CMH1 length and triangle format", True)
            audit.check(name + ": finite vertex attributes", np.isfinite(data).all())
            normals = data[:, 3:6]
            audit.check(name + ": unit normals", np.allclose(np.linalg.norm(normals, axis=1), 1, atol=2e-4))
            tri = data[:, :3].reshape(-1, 3, 3).astype(np.float64)
            cross = np.cross(tri[:, 1] - tri[:, 0], tri[:, 2] - tri[:, 0])
            area = np.linalg.norm(cross, axis=1)
            audit.check(name + ": no degenerate triangles", np.all(area > 1e-9), float(area.min()))
            dots = np.sum(cross * normals[::3], axis=1)
            audit.check(name + ": winding agrees with normals", np.all(dots > 0), float(dots.min()))
            lo, hi = data[:, :3].min(0), data[:, :3].max(0)
            audit.check(name + ": nonempty bounded geometry", np.all(hi > lo) and np.abs(data[:, :3]).max() < 100)
            audit.check(name + ": usable bounded UVs", np.abs(data[:, 6:8]).max() < 100 and np.ptp(data[:, 6]) > .01 and np.ptp(data[:, 7]) > .01)
            obj = ART / "Models" / (name + ".obj")
            if audit.check(name + ": editable OBJ exists", obj.is_file()):
                expanded = obj_vertices(obj)
                audit.check(name + ": OBJ and runtime binary agree", expanded.shape == data.shape and np.allclose(expanded, data, atol=3e-5))
            audit.meshes[name] = {"vertices": len(data), "triangles": len(data) // 3,
                                  "bounds_min": lo.tolist(), "bounds_max": hi.tolist(),
                                  "sha256": hashlib.sha256(raw).hexdigest()}
        except (ValueError, OSError, IndexError) as exc:
            audit.check(name + ": valid authored geometry", False, str(exc))
    for name, model in audit.meshes.items():
        if name.endswith("_low"):
            continue
        low = audit.meshes.get(name.removesuffix("_high") + "_low")
        if audit.check(name + ": distant prototype exists", low is not None):
            audit.check(name + ": distant triangle cost does not increase", low["triangles"] <= model["triangles"])
    for lod in ("high", "low"):
        vault_file = ART / "Meshes" / ("rib_vault_" + lod + ".bytes")
        if vault_file.is_file():
            vertices = decode(vault_file)[1][:, :3]
            center = vertices[(np.abs(vertices[:, 0]) < .75) & (np.abs(vertices[:, 2]) < .75)]
            audit.check("rib_vault_" + lod + ": no hanging central posts", len(center) > 0 and np.min(center[:, 1]) > 2)
            distances = [float(np.linalg.norm(vertices - [x, 0, z], axis=1).min()) for x in (-2.9, 2.9) for z in (-2.9, 2.9)]
            audit.check("rib_vault_" + lod + ": all four column-top endpoints are supported", max(distances) < .22, distances)
    inventory_path = ART / "Models/model_inventory.json"
    if audit.check("authored geometry inventory exists", inventory_path.is_file()):
        inventory = json.loads(inventory_path.read_text())
        rows = inventory["models"]
        audit.check("inventory names exactly match shipped geometry", {row["variant"] for row in rows} == set(audit.meshes) and len(rows) == len(audit.meshes))
        audit.check("inventory prototype counts match actual files", inventory["variants"] == len(audit.meshes) and inventory["unique_prototypes"] == sum(name.endswith("_high") for name in audit.meshes))
        for row in rows:
            actual = audit.meshes.get(row["variant"])
            if actual is None:
                continue
            name = row["variant"]
            audit.check(name + ": manifest hash and counts match actual data", all(row[key] == actual[key] for key in ("sha256", "vertices", "triangles")))
            audit.check(name + ": manifest bounds match actual data", np.allclose(row["bounds_min"], actual["bounds_min"], atol=1e-5) and np.allclose(row["bounds_max"], actual["bounds_max"], atol=1e-5))
            audit.check(name + ": source paths resolve to audited files", (ROOT / row["binary"]).resolve() == (ART / "Meshes" / (name + ".bytes")).resolve() and (ROOT / row["source"]).is_file())


def validate_surfaces(audit):
    manifest = ART / "material_manifest.json"
    if not audit.check("material manifest exists", manifest.exists()):
        return
    surfaces = json.loads(manifest.read_text())["surfaces"]
    pairs = [(s["style"], s["role"]) for s in surfaces]
    audit.check("four palettes have all material roles without duplicates", len(pairs) == len(set(pairs)) and set(pairs) == {(style, role) for style in range(4) for role in ROLES})
    for row in surfaces:
        name = str(row["style"]) + "/" + row["role"]
        audit.check(name + ": physical scalar bounds", all(0 <= row[k] <= 1 for k in ("metallic", "smoothness")))
        audit.check(name + ": finite color and emission", all(np.isfinite(row[k]).all() and min(row[k]) >= 0 and max(row[k]) <= 1 for k in ("tint", "emission")))
    textures = sorted((ART / "Textures").glob("*.png"))
    audit.check("material surface textures exist", len(textures) >= 30, len(textures))
    for path in textures:
        try:
            with Image.open(path) as image:
                image.load()
                audit.check(path.name + ": valid power-of-two surface", image.width == image.height and image.width in (256, 512, 1024, 2048) and image.mode in ("RGB", "RGBA"))
                pixels = np.asarray(image)
                if "albedo" in path.stem.lower() or "normal" in path.stem.lower():
                    audit.check(path.name + ": nonflat surface information", np.ptp(pixels[:, :, :3]) > 0)
            meta = Path(str(path) + ".meta").read_text()
            if "_normal" in path.stem.lower() or "_mask" in path.stem.lower():
                audit.check(path.name + ": linear data texture", re.search(r"sRGBTexture:\s*0\b", meta) is not None)
            if "_normal" in path.stem.lower():
                audit.check(path.name + ": normal-map import semantics", re.search(r"textureType:\s*1\b", meta) is not None)
        except (OSError, ValueError) as exc:
            audit.check(path.name + ": readable texture and importer", False, str(exc))


def validate_metadata(audit):
    guids, duplicates, missing = {}, [], []
    for path in (ROOT / "Assets").rglob("*.meta"):
        match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(encoding="utf-8-sig"), re.M)
        if not match:
            if "04A" in str(path):
                missing.append(path.relative_to(ROOT).as_posix())
            continue
        guid = match.group(1)
        if guid in guids:
            duplicates.append([guids[guid], path.relative_to(ROOT).as_posix()])
        guids[guid] = path.relative_to(ROOT).as_posix()
    audit.check("all asset GUIDs are unique", not duplicates, duplicates)
    audit.check("new metadata has valid GUIDs", not missing, missing)
    for path in (ART / "Materials").glob("*.mat"):
        references = set(re.findall(r"guid: ([0-9a-f]{32})", path.read_text()))
        unresolved = sorted(guid for guid in references if guid not in guids and not guid.startswith("0000000000000000"))
        audit.check(path.name + ": material texture references resolve", not unresolved, unresolved)
    for root in [ART, ROOT / (RUNTIME + "VisualRebirth04A"), ROOT / (RUNTIME + "ArtRebirth04A")]:
        if root.exists():
            for path in [root, *root.rglob("*")]:
                if path.suffix == ".meta":
                    continue
                audit.check(path.relative_to(ROOT).as_posix() + ": metadata exists", Path(str(path) + ".meta").is_file())


def validate_preservation(audit, baseline):
    tracked = git("ls-tree", "-r", "--name-only", baseline).decode().splitlines()
    changed = set(git("diff", "--name-only", baseline, "--", "Assets", "Packages", "ProjectSettings").decode().splitlines())
    # The sole existing Gameplay file allowed to change hosts the optional art
    # layer and retains the original physical foundation/deck/socket construction.
    visual_host = RUNTIME + "Gameplay/Strongholds/StrongholdView.cs"
    prefixes = [RUNTIME + s for s in ("Characters/", "AI/", "Combat/", "Vehicles/", "Creatures/", "Gameplay/")]
    protected = [p for p in tracked if p != visual_host and (any(p.startswith(q) for q in prefixes) or p.startswith("Assets/NeonSyndicate/Resources/Quests/") or p.startswith("Assets/NeonSyndicate/Resources/Events/") or p.startswith("Packages/") or p.startswith("ProjectSettings/") or p in [RUNTIME + "Core/GInput.cs", RUNTIME + "Core/Pad.cs", RUNTIME + "Core/MenuPages.cs", RUNTIME + "Core/GameTime.cs"])]
    mutated = sorted(changed.intersection(protected))
    audit.check("baseline gameplay, saves, input, quest IDs, packages and editor pin preserved", not mutated, {"files": len(protected), "changed": mutated})
    visual_hooks = {RUNTIME + path for path in ("ArtDistrict/DistrictArchitecture.cs", "Core/Game.cs", "Core/GameRoot.cs",
        "Gameplay/Strongholds/StrongholdView.cs", "QA/GoldenShots.cs", "QA/QaTools.cs", "World/Atmosphere.cs",
        "World/CityBuilder.Fantasy02E.cs", "World/CityBuilder.cs")}
    existing_runtime = {p for p in tracked if p.startswith(RUNTIME) and p.endswith(".cs")}
    unexpected_runtime = sorted((changed & existing_runtime) - visual_hooks)
    audit.check("existing runtime edits limited to nine reviewed art and QA integration hosts", not unexpected_runtime, unexpected_runtime)
    current_fort = (ROOT / visual_host).read_text()
    reverse = current_fort.replace("A real traversable fort with six sockets, a core and a challenge point. Optional 04A routes use separate bounded collision.",
        "A real traversable fort with six socket positions, a core and a challenge point. No physics in decorations.")
    reverse = reverse.replace("                VisualRebirth04AQa.RegisterStronghold(Definition.id,Root.transform);\n", "")
    reverse = reverse.replace("                TryDecorateRebirth04A(staged.transform,state);\n", "")
    reverse = reverse.replace('                    if(_rebirth04AActive)_label.text+="\\nVISUAL REBIRTH 04A";\n                    else if(_citadelActive)',
                              '                    if(_citadelActive)')
    audit.check("removing four optional fort art edits recovers exact baseline gameplay/collision host", reverse == git("show", baseline + ":" + visual_host).decode())
    old_meta = [p for p in tracked if p.startswith("Assets/") and p.endswith(".meta")]
    audit.check("all original Unity metadata retained byte-for-byte", not changed.intersection(old_meta), sorted(changed.intersection(old_meta)))
    old_art = [p for p in tracked if p.startswith("Assets/") and Path(p).suffix in (".obj", ".bytes", ".png", ".wav", ".shader", ".mat", ".prefab", ".unity")]
    audit.check("all original model, texture, sound, shader, prefab and scene assets preserved", not changed.intersection(old_art), sorted(changed.intersection(old_art)))
    unresolved = []
    for p in tracked:
        path = ROOT / p
        if p.startswith("Assets/") and path.is_file() and path.stat().st_size < 200 and path.read_bytes().startswith(b"version https://git-lfs.github.com/spec/v1"):
            unresolved.append(p)
    audit.check("baseline Assets LFS data hydrated", not unresolved, unresolved)


def transform_vertices(vertices, part):
    """Unity Quaternion.Euler applies Z, then X, then Y; use the same order."""
    x, y, z = np.radians(part["euler"])
    cx, sx, cy, sy, cz, sz = np.cos(x), np.sin(x), np.cos(y), np.sin(y), np.cos(z), np.sin(z)
    rotation = (np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]]) @
                np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]]) @
                np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]]))
    return (vertices * part["scale"]) @ rotation.T + part["position"]


def intersects_box(triangles, lower, upper):
    """Exact triangle-box surface intersection using convex polygon clipping."""
    candidates = triangles[(triangles.max(1) > lower).all(1) & (triangles.min(1) < upper).all(1)]
    for triangle in candidates:
        polygon = list(triangle)
        for axis in range(3):
            for boundary, sign in ((lower[axis], 1), (upper[axis], -1)):
                clipped = []
                for index, first in enumerate(polygon):
                    second = polygon[(index + 1) % len(polygon)]
                    a, b = (first[axis] - boundary) * sign, (second[axis] - boundary) * sign
                    if a >= 0:
                        clipped.append(first)
                    if (a >= 0) != (b >= 0):
                        clipped.append(first + (second - first) * (a / (a - b)))
                polygon = clipped
                if not polygon:
                    break
            if not polygon:
                break
        if polygon:
            return True
    return False


def support_height(collisions, x, z):
    tops = []
    for box in collisions:
        at, size = np.asarray(box["position"]), np.asarray(box["size"])
        if abs(x - at[0]) <= size[0] * .5 + 1e-5 and abs(z - at[2]) <= size[2] * .5 + 1e-5:
            tops.append(float(at[1] + size[1] * .5))
    return max(tops) if tops else None


def visual_surface_at(triangles, x, y, z):
    """Does a horizontal visual triangle actually cover a physical tread sample?"""
    selected = triangles[np.max(np.abs(triangles[:, :, 1] - y), axis=1) < .002]
    if len(selected) == 0:
        return False
    a = selected[:, 0][:, [0, 2]]
    b = selected[:, 1][:, [0, 2]] - a
    c = selected[:, 2][:, [0, 2]] - a
    q = np.asarray([x, z]) - a
    det = b[:, 0] * c[:, 1] - b[:, 1] * c[:, 0]
    valid = np.abs(det) > 1e-10
    u = np.divide(q[:, 0] * c[:, 1] - q[:, 1] * c[:, 0], det, out=np.full_like(det, -10), where=valid)
    v = np.divide(b[:, 0] * q[:, 1] - b[:, 1] * q[:, 0], det, out=np.full_like(det, -10), where=valid)
    return bool(np.any(valid & (u >= -.001) & (v >= -.001) & (u + v <= 1.001)))


def validate_layouts(audit):
    cache = {}
    for name in audit.meshes:
        cache[name] = decode(ART / "Meshes" / (name + ".bytes"))[1][:, :3]
    for kind, part_key in (("Layouts", "parts"), ("Gardens", "nodes")):
        files = sorted((ART / kind).glob("*.json"))
        audit.check(kind + ": three exact authored compositions", len(files) == 3, len(files))
        for path in files:
            layout = json.loads(path.read_text())
            name = kind + "/" + path.stem
            parts = layout[part_key]
            triangles_by_lod = {}
            for lod in ("high", "low"):
                all_triangles, bounds, gate_hits, path_hits, invalid = [], [], [], [], []
                importer_bounds = []
                for index, part in enumerate(parts):
                    key = part["model"] + "_" + lod
                    if key not in cache or part["role"] not in ROLES:
                        invalid.append([index, key, part["role"]])
                        continue
                    transformed = transform_vertices(cache[key], part)
                    tri = transformed.reshape(-1, 3, 3)
                    all_triangles.append(tri)
                    low, high = transformed.min(0), transformed.max(0)
                    if not (np.isfinite(transformed).all() and np.abs(transformed[:, [0, 2]]).max() < 24 and low[1] >= -.3 and high[1] <= 40):
                        bounds.append([index, key, low.tolist(), high.tolist()])
                    if kind == "Layouts" and intersects_box(tri, np.array([-2.75, .35, -23.5]), np.array([2.75, 2.2, -11])):
                        gate_hits.append([index, key])
                    if kind == "Gardens" and (intersects_box(tri, np.array([-4.5, .15, -23]), np.array([4.5, 2.2, 23])) or
                                               intersects_box(tri, np.array([-23, .15, -4.5]), np.array([23, 2.2, 4.5]))):
                        path_hits.append([index, key])
                    if kind == "Gardens":
                        source_lo, source_hi = cache[key].min(0), cache[key].max(0)
                        corners = np.asarray([[source_hi[axis] if mask & (1 << axis) else source_lo[axis] for axis in range(3)] for mask in range(8)])
                        corners = transform_vertices(corners, part)
                        if not (np.abs(corners[:, [0, 2]]).max() <= 23.8 and corners[:, 1].min() >= -.2 and corners[:, 1].max() <= 32):
                            importer_bounds.append([index, key, corners.min(0).tolist(), corners.max(0).tolist()])
                audit.check(name + "/" + lod + ": all model and material references exist", not invalid, invalid)
                audit.check(name + "/" + lod + ": actual transformed geometry stays in surveyed plot", not bounds, bounds)
                if kind == "Layouts":
                    audit.check(name + "/" + lod + ": original entrance has no intersecting art triangles", not gate_hits, gate_hits)
                else:
                    audit.check(name + "/" + lod + ": nine-metre ground paths clear of art triangles", not path_hits, path_hits)
                    audit.check(name + "/" + lod + ": conservative runtime bounds validation accepts all models", not importer_bounds, importer_bounds)
                triangles_by_lod[lod] = np.concatenate(all_triangles) if all_triangles else np.zeros((0, 3, 3))
            audit.layouts[name] = {"parts": len(parts), "colliders": len(layout.get("collisions", [])),
                                   "high_triangles": len(triangles_by_lod["high"]), "low_triangles": len(triangles_by_lod["low"])}
            if kind != "Layouts":
                audit.layouts[name]["solid_nodes"] = sum(p["solid"] for p in parts)
                audit.layouts[name]["low_visible_nodes"] = sum(p["solid"] or p["far"] for p in parts)
                continue
            collisions = layout["collisions"]
            if path.stem != "hearthspire":
                audit.check(name + ": no new rival raid collision", not collisions)
                continue
            violations = []
            for box in collisions:
                at, size = np.asarray(box["position"]), np.asarray(box["size"])
                low, high = at - size * .5, at + size * .5
                for label, center, extent in [("gate", [0, -17.5], [3.1, 6.5]), ("core", [0, 5], [6.2, 6.75])]:
                    if np.all(np.abs(at[[0, 2]] - center) < size[[0, 2]] * .5 + extent):
                        violations.append(box["name"] + ": " + label)
                for slot in range(6):
                    center = [-11 if slot % 2 == 0 else 11, -8 + (slot // 2) * 8]
                    if np.all(np.abs(at[[0, 2]] - center) < size[[0, 2]] * .5 + 3.2):
                        violations.append(box["name"] + ": socket " + str(slot))
                if not (np.all(size > 0) and max(abs(low[[0, 2]]).max(), abs(high[[0, 2]]).max()) < 24 and low[1] >= -.02):
                    violations.append(box["name"] + ": plot")
            audit.check(name + ": physical route preserves gate, core and six socket envelopes", not violations, violations)
            samples = [(-20., float(z)) for z in np.arange(-15.95, 18.051, .1)]
            samples += [(float(x), 19.8) for x in np.arange(-20, -5.999, .1)]
            heights = [support_height(collisions, x, z) for x, z in samples]
            audit.check(name + ": elevated route has continuous physical support", all(h is not None for h in heights))
            if all(h is not None for h in heights):
                rises = np.abs(np.diff(heights))
                audit.check(name + ": route stair rises never exceed 0.25m", rises.max() <= .25001, float(rises.max()))
                audit.check(name + ": route reaches six-metre overlook", abs(heights[-1] - 6.07) < .001)
            for lod, triangles in triangles_by_lod.items():
                unmatched = []
                for box in collisions:
                    if any(word in box["name"] for word in ("column", "parapet")):
                        continue
                    at, size = np.asarray(box["position"]), np.asarray(box["size"])
                    top = at[1] + size[1] * .5
                    for dx, dz in ((0, 0), (-.49, -.49), (-.49, .49), (.49, -.49), (.49, .49)):
                        if not visual_surface_at(triangles, at[0] + size[0] * dx, top, at[2] + size[2] * dz):
                            unmatched.append([box["name"], dx, dz])
                audit.check(name + "/" + lod + ": route collision tops match visible modeled surfaces", not unmatched, unmatched)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--baseline", default=BASE)
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    audit = Audit()
    validate_geometry(audit)
    validate_layouts(audit)
    validate_surfaces(audit)
    validate_metadata(audit)
    validate_preservation(audit, args.baseline)
    result = {"scope": "Actual authored geometry, texture/import data, GUID and baseline file preservation audit. Not Unity or C# execution.",
              "baseline": args.baseline, "checks": len(audit.checks), "passed": sum(c["passed"] for c in audit.checks),
              "failed": [c for c in audit.checks if not c["passed"]], "warnings": audit.warnings,
              "unique_masters": sum(n.endswith("_high") for n in audit.meshes),
              "mesh_prototypes": len(audit.meshes), "mesh_inventory": audit.meshes,
              "texture_maps": len(list((ART / "Textures").glob("*.png"))),
              "material_variants": len(list((ART / "Materials").glob("*.mat"))),
              "surface_families": len({row["family"] for row in json.loads((ART / "material_manifest.json").read_text())["surfaces"]}),
              "authored_layouts": audit.layouts,
              "unity_compile": "NOT RUN", "unity_editmode": "NOT RUN", "unity_gameplay": "NOT RUN", "performance": "NOT MEASURED"}
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps({k: v for k, v in result.items() if k != "mesh_inventory"}, indent=2))
    return 1 if result["failed"] else 0


if __name__ == "__main__":
    sys.exit(main())
