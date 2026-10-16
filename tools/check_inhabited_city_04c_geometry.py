#!/usr/bin/env python3
"""Audit actual 04C mesh payloads and source-resolved room geometry.

Numerical source evidence only. This does not import assets, execute Unity
colliders, walk the player controller, or measure native rendering performance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import numpy as np
from PIL import Image
from check_visual_rebirth_04a import Audit, decode, obj_vertices, transform_vertices, intersects_box, visual_surface_at

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/NeonSyndicate/Resources/InhabitedCity04C"
SOURCE_ROOTS = {"04A": ROOT / "Assets/NeonSyndicate/Resources/VisualRebirth04A", "04B": ROOT / "Assets/NeonSyndicate/Resources/LivingTerritories04B", "04C": ART}


def box_vertices(size):
    corners = np.asarray([[x, y, z] for x in (-.5, .5) for y in (-.5, .5) for z in (-.5, .5)]) * size
    indices = [0, 1, 3, 0, 3, 2, 4, 6, 7, 4, 7, 5, 0, 4, 5, 0, 5, 1,
        2, 3, 7, 2, 7, 6, 0, 2, 6, 0, 6, 4, 1, 5, 7, 1, 7, 3]
    return corners[indices]


def occupied(entries, lower, upper):
    blocked = []
    for entry in entries:
        if not ((entry["max"] > lower).all() and (entry["min"] < upper).all()): continue
        hit = intersects_box(entry["triangles"], lower, upper)
        if not hit and entry.get("box") is not None:
            # Surface intersection alone misses a probe entirely enclosed by a solid box.
            node = entry["box"]; center = (lower + upper) * .5
            basis = transform_vertices(np.eye(3), {"position": [0, 0, 0], "scale": [1, 1, 1], "euler": node["euler"]})
            local = (center - node["position"]) @ basis.T
            hit = (np.abs(local) < np.asarray(node["size"]) * .5).all()
        if hit: blocked.append(entry["id"])
    return blocked


def geometry_entry(identifier, points, box=None):
    return {"id": identifier, "triangles": points.reshape(-1, 3, 3), "min": points.min(0), "max": points.max(0), "box": box}


def audit_rooms(audit, source_dir):
    provenance = json.loads((source_dir / "source_provenance.json").read_text())
    stale = []
    for name, digest in provenance["source_sha256"].items():
        path = ROOT / name
        if path.exists() and hashlib.sha256(path.read_bytes()).hexdigest() != digest: stale.append(name)
    audit.check("Resolved room source snapshot matches its recorded inputs", not stale, stale)
    action_source = (ROOT / "Assets/NeonSyndicate/Scripts/Runtime/Gameplay/InhabitedCity/QuietSignal04C.cs").read_text()
    actions = re.findall(r'"([a-z0-9_:]+)"', re.search(r'string\[\] Actions\s*=\s*\{([^}]+)', action_source).group(1))
    anchors = re.findall(r'"([a-z0-9_]+)"', re.search(r'string\[\] Anchors\s*=\s*\{([^}]+)', action_source).group(1))
    if len(actions) != len(anchors): raise ValueError("QuietSignal source action/anchor mapping changed.")
    cache = {}; rooms = []
    def mesh(source, model, lod):
        key = source, model, lod
        if key not in cache: cache[key] = decode(SOURCE_ROOTS[source] / "Meshes" / (model + "_" + lod + ".bytes"))[1][:, :3]
        return cache[key]
    for path in sorted(source_dir.glob("*.json")):
        data = json.loads(path.read_text())
        if not isinstance(data, dict) or "expanded" not in data: continue
        expanded = data["expanded"]; name = data["id"]; parts = data["parts"]; collisions = []
        for node in data["collisions"]:
            if node["kind"] == "box":
                local = box_vertices(np.asarray(node["size"])); transform = {"position": node["position"], "scale": [1, 1, 1], "euler": node["euler"]}
                collisions.append(geometry_entry(node["nodeId"], transform_vertices(local, transform), node))
            else:
                collisions.append(geometry_entry(node["nodeId"], transform_vertices(mesh(node["source"], node["model"], "low"), node)))
        all_collision = np.concatenate([entry["triangles"] for entry in collisions])
        visual = {}
        for lod in ("high", "low"):
            entries = []
            for part in parts:
                if part["kind"] == "upper04A" and bool(part["low"]) != (lod == "low"): continue
                if part["kind"] == "mesh": points = mesh(part["source"], part["model"], lod)
                else: points = decode(source_dir / part["file"])[1][:, :3]
                entries.append(geometry_entry(part["nodeId"], transform_vertices(points, part)))
            visual[lod] = entries
        for clear in expanded.get("keepouts") or []:
            if clear["id"] == "stairs_and_headroom": continue
            lower = np.asarray(clear["min"], dtype=float); upper = np.asarray(clear["max"], dtype=float); lower[1] += .15
            blocked = occupied(collisions, lower, upper)
            audit.check(name + ": " + clear["id"] + " clears actual collider geometry", not blocked, blocked)
            for lod, entries in visual.items():
                blocked = occupied(entries, lower, upper)
                audit.check(name + "/" + lod + ": " + clear["id"] + " clears visible source triangles", not blocked, blocked)
        used = {"staff", "staff_next", "visitor1", "visitor1_next", "visitor2", "visitor2_next", "exit"}
        if name in {"lofts", "clinic", "forge", "inn", "garage"}: used.add("service")
        if name == "lofts": used.update({"bed", "desk"})
        if name == "depot": used.update({"defense1", "defense2"})
        used.update(anchor for action, anchor in zip(actions, anchors) if action.split(":")[0] == name)
        available = {anchor["id"]: anchor for anchor in expanded["anchors"]}
        audit.check(name + ": required interaction/resident anchors exist", used.issubset(available), sorted(used - set(available)))
        for identifier in sorted(used & set(available)):
            point = np.asarray(available[identifier]["position"], dtype=float)
            blocked = occupied(collisions, point + [-.42, .15, -.42], point + [.42, 2.1, .42])
            audit.check(name + ": " + identifier + " has standing collider clearance", not blocked, blocked)
            if identifier != "exit":
                supported = [visual_surface_at(all_collision, point[0] + dx, point[1], point[2] + dz) for dx, dz in ((0, 0), (-.25, 0), (.25, 0), (0, -.25), (0, .25))]
                audit.check(name + ": " + identifier + " has actual floor support", all(supported), supported)
        for identifier in ("staff", "visitor1", "visitor2"):
            if identifier not in available or identifier + "_next" not in available: continue
            first = np.asarray(available[identifier]["position"], dtype=float); last = np.asarray(available[identifier + "_next"]["position"], dtype=float)
            lower = np.minimum(first, last) + [-.42, .15, -.42]; upper = np.maximum(first, last) + [.42, 2.1, .42]
            blocked = occupied(collisions, lower, upper)
            audit.check(name + ": " + identifier + " routine segment clears actual colliders", not blocked, blocked)
        if expanded["twoStoreys"]:
            steps = sorted([node for node in expanded["nodes"] if node["id"].startswith("stair_tread_")], key=lambda node: int(node["id"].rsplit("_", 1)[1]))
            audit.check(name + ": staircase has 24 authored treads", len(steps) == 24)
            failures = []; prior_top = 0.; prior_end = 6.
            for node in steps:
                point = np.asarray(node["position"], dtype=float); size = np.asarray(node["scale"], dtype=float); top = point[1] + size[1] * .5
                support = all(visual_surface_at(all_collision, point[0] + dx, top, point[2]) for dx in (-.6, 0, .6))
                lower = point + [-.4, 0, -.3]; lower[1] = top + .43
                upper = point + [.4, 0, .3]; upper[1] = top + 2.1
                blocked = occupied(collisions, lower, upper)
                if not support or blocked or abs(top - prior_top - .175) > .002 or abs(point[2] - size[2] * .5 - prior_end) > .002:
                    failures.append({"step": node["id"], "support": support, "blocked": blocked, "rise": top - prior_top})
                prior_top, prior_end = top, point[2] + size[2] * .5
            audit.check(name + ": all treads support the route with clear headroom and continuous rises", not failures, failures)
            point = np.asarray([-5.7, 4.2, 14.3]); blocked = occupied(collisions, point + [-.4, .15, -.4], point + [.4, 2.1, .4])
            audit.check(name + ": upper landing has supported standing space", visual_surface_at(all_collision, *point) and not blocked, blocked)
        # A preserved upper volume must never extend down through a playable floor.
        upper = [node for node in data["collisions"] if node["nodeId"] == "preserved_upper_architecture"]
        audit.check(name + ": original solid body starts above occupied rooms", len(upper) == 1 and abs(upper[0]["position"][1] - upper[0]["size"][1] * .5 - expanded["occupiedHeight"]) < .002)
        rooms.append({"id": name, "source_nodes": len(expanded["nodes"]), "colliders": len(collisions), "checked_anchors": sorted(used), "unused_authored_anchors": sorted(set(available) - used)})
    return rooms


def audit_assets(audit):
    manifest = json.loads((ART / "asset_manifest.json").read_text())
    rows = manifest["models"]
    audit.check("04C manifest version", manifest["version"] == "04C")
    audit.check("Model inventory uniquely describes every CMH1 payload", len({(row["name"], row["lod"]) for row in rows}) == len(rows)
        and {Path(row["binary"]).name for row in rows} == {path.name for path in (ART / "Meshes").glob("*.bytes")})
    meshes = {}
    for row in rows:
        label = row["name"] + "/" + row["lod"]
        raw, data = decode(ROOT / row["binary"])
        points = data[:, :3]; normals = data[:, 3:6]; triangles = points.reshape(-1, 3, 3).astype(np.float64)
        cross = np.cross(triangles[:, 1] - triangles[:, 0], triangles[:, 2] - triangles[:, 0]); areas = np.linalg.norm(cross, axis=1)
        audit.check(label + ": finite source attributes", np.isfinite(data).all())
        audit.check(label + ": unit authored normals", np.allclose(np.linalg.norm(normals, axis=1), 1, atol=2e-4))
        audit.check(label + ": no degenerate triangles", np.all(areas > 1e-9), float(areas.min()))
        audit.check(label + ": consistent triangle winding", np.all(np.sum(cross * normals[::3], axis=1) > 0))
        audit.check(label + ": usable UVs", np.abs(data[:, 6:]).max() < 100 and np.ptp(data[:, 6]) > .01 and np.ptp(data[:, 7]) > .01)
        editable = obj_vertices(ROOT / row["source"])
        audit.check(label + ": editable OBJ equals runtime payload", editable.shape == data.shape and np.allclose(editable, data, atol=3e-5))
        audit.check(label + ": manifest hash and counts match", hashlib.sha256(raw).hexdigest() == row["sha256"] and row["vertices"] == len(data) and row["triangles"] == len(triangles))
        audit.check(label + ": manifest bounds match", np.allclose(points.min(0), row["bounds_min"], atol=1e-5) and np.allclose(points.max(0), row["bounds_max"], atol=1e-5))
        meshes[(row["name"], row["lod"])] = data
    for name in sorted({row["name"] for row in rows}):
        high, low = meshes[(name, "high")], meshes[(name, "low")]
        shared_coarse = name in {"bookcase", "record_stack"}
        audit.check(name + (": already-coarse structural topology shared at both LODs" if shared_coarse else ": lower detail mesh reduces triangles"),
            len(low) == len(high) if shared_coarse else 0 < len(low) < len(high))
        audit.check(name + ": LOD retains support and overall bounds", np.allclose(high[:, :3].min(0), low[:, :3].min(0), atol=.13)
            and np.allclose(high[:, :3].max(0), low[:, :3].max(0), atol=.13))
    texture = np.asarray(Image.open(ART / "Textures/Timber_Albedo.png").convert("RGB"))
    audit.check("Timber albedo is a real varied texture", texture.shape[0] >= 128 and texture.shape[1] >= 128 and float(texture.std()) > 5)
    audit.check("Overall low-detail library reduces source triangles", sum(len(data) for (name, lod), data in meshes.items() if lod == "low")
        < sum(len(data) for (name, lod), data in meshes.items() if lod == "high"))
    return {"masters": len({row["name"] for row in rows}), "variants": len(rows),
        "high_library_triangles": sum(row["triangles"] for row in rows if row["lod"] == "high"),
        "low_library_triangles": sum(row["triangles"] for row in rows if row["lod"] == "low")}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--report", type=Path, required=True)
    parser.add_argument("--resolved-dir", type=Path)
    args = parser.parse_args()
    audit = Audit(); inventory = audit_assets(audit)
    rooms = audit_rooms(audit, args.resolved_dir) if args.resolved_dir else "PENDING SOURCE EXPORT"
    report = {"scope": __doc__.strip(), "checks": len(audit.checks), "passed": sum(row["passed"] for row in audit.checks),
        "failed": [row for row in audit.checks if not row["passed"]], "details": audit.checks,
        "inventory": inventory, "rooms": rooms, "native_import": "NOT RUN", "native_collision": "NOT RUN"}
    args.report.parent.mkdir(parents=True, exist_ok=True); args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: value for key, value in report.items() if key != "details"}, indent=2))
    return 0 if not report["failed"] else 1


if __name__ == "__main__": raise SystemExit(main())
