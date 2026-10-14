#!/usr/bin/env python3
"""Offline delivery checks. This does not compile or run Unity.

Usage:
  python tools/validate_art_district.py --baseline path/to/original.zip \
      --report Logs/art_district_validation.json --diff Logs/art_district_changes.diff

Only Python standard library and Pillow are required. Exit 0 means the checks
performed here passed; it does not mean Unity import, play or rendering passed.
"""
from __future__ import annotations

import argparse
from collections import Counter
import difflib
import hashlib
import json
import math
from pathlib import Path, PurePosixPath
import re
import sys
import zipfile

try:
    from PIL import Image
except ImportError:
    Image = None

PROTECTED_PREFIXES = (
    "Assets/NeonSyndicate/Resources/Quests/",
    "Assets/NeonSyndicate/Resources/Events/",
    "Assets/NeonSyndicate/Scripts/Runtime/Gameplay/",
    "Assets/NeonSyndicate/Scripts/Runtime/AI/",
    "Assets/NeonSyndicate/Scripts/Runtime/Vehicles/",
    "Assets/NeonSyndicate/Scripts/Runtime/UI/",
)
PROTECTED_NAMES = {
    "Docs/CONTROLS.md", "ProjectSettings/ProjectVersion.txt",
    "Packages/manifest.json", "Assets/NeonSyndicate/Scripts/Runtime/Core/GInput.cs",
    "Assets/NeonSyndicate/Scripts/Runtime/Core/Pad.cs",
    "Assets/NeonSyndicate/Scripts/Runtime/Core/Act.cs",
    "Assets/NeonSyndicate/Scripts/Runtime/Core/Keys.cs",
    "Assets/NeonSyndicate/Scripts/Runtime/Core/Layers.cs",
}
# The reviewed integration points are visual hooks, creature appearance and project notes.
# Any other original change is surfaced for review. Protected gameplay files still fail.
EXPECTED_VISUAL_CHANGES = {
    "Assets/NeonSyndicate/Scripts/Runtime/" + name for name in (
        "World/CityBuilder.cs", "World/Atmosphere.cs", "Core/Game.cs", "Core/GameRoot.cs",
        "Creatures/CreatureLooks.cs", "Creatures/CreaturePose.Flight.cs", "Creatures/CreatureTypes.cs",
        "Creatures/CreaturePose.Legged.cs", "Creatures/CreatureBuilder.Winged.cs",
    )
} | {"README.md", "CLAUDE.md"}
IGNORED_DIRS = {"Library", "Temp", "Obj", "Logs", ".git", "__pycache__", "Builds"}


class Checks:
    def __init__(self):
        self.errors = []
        self.warnings = []
        self.results = {}

    def error(self, path, message):
        self.errors.append({"path": str(path), "message": message})

    def warn(self, path, message):
        self.warnings.append({"path": str(path), "message": message})

    def finish(self):
        return {
            "validation_scope": "Offline files, geometry, metadata and baseline preservation only",
            "unity_import_compile_play_render_performance": "NOT RUN",
            "status": "FAIL" if self.errors else "PASS_OFFLINE",
            "error_count": len(self.errors), "warning_count": len(self.warnings),
            "errors": self.errors, "warnings": self.warnings, **self.results,
        }


def sha256(data):
    return hashlib.sha256(data).hexdigest()


def meta_value(text, name):
    match = re.search(r"^\s*" + re.escape(name) + r":\s*([^\r\n#]+)", text, re.MULTILINE)
    return match.group(1).strip() if match else None


def is_protected(name):
    if name in PROTECTED_NAMES or any(name.startswith(p) for p in PROTECTED_PREFIXES):
        return True
    # Movement, camera and city topology are expressly outside the visual pass.
    leaf = PurePosixPath(name).name
    return (name.endswith(".cs") and (
        leaf.startswith("PlayerController") or leaf.startswith("CityLayout") or
        leaf.startswith("PlayerCamera") or leaf in {"SaveSystem.cs", "WorldSystems.cs"}))


def inspect_obj(path, root, checks):
    rel = path.relative_to(root).as_posix()
    positions, uvs, normals = [], [], []
    materials, faces = set(), []
    libraries = []
    try:
        with path.open(encoding="utf-8-sig") as stream:
            for line_no, raw in enumerate(stream, 1):
                line = raw.partition("#")[0].strip()
                if not line:
                    continue
                parts = line.split()
                kind, values = parts[0], parts[1:]
                if kind in {"v", "vt", "vn"}:
                    size = 2 if kind == "vt" else 3
                    if len(values) < size:
                        checks.error(rel, f"Line {line_no}: {kind} needs {size} components")
                        continue
                    numbers = tuple(float(s) for s in values[:size])
                    if not all(math.isfinite(x) for x in numbers):
                        checks.error(rel, f"Line {line_no}: nonfinite {kind}")
                    {"v": positions, "vt": uvs, "vn": normals}[kind].append(numbers)
                elif kind == "f":
                    # Resolve negative OBJ references at the point of declaration.
                    counts = (len(positions), len(uvs), len(normals))
                    face = []
                    for token in values:
                        refs = token.split("/")
                        result = []
                        for column in range(3):
                            if column >= len(refs) or not refs[column]:
                                result.append(None)
                            else:
                                value = int(refs[column])
                                result.append(value - 1 if value > 0 else counts[column] + value)
                        face.append(tuple(result))
                    faces.append((line_no, face))
                elif kind == "usemtl":
                    materials.add(" ".join(values))
                elif kind == "mtllib":
                    libraries.extend(values)
    except (OSError, ValueError) as error:
        checks.error(rel, f"Unreadable OBJ: {error}")
        return {"path": rel}

    if not positions or not faces:
        checks.error(rel, "Mesh has no vertices or faces")
    if not uvs:
        checks.error(rel, "Mesh has no UV coordinates")
    if not normals:
        checks.error(rel, "Mesh has no normals")
    triangles, invalid_faces, degenerate, missing_attributes, backward = 0, 0, 0, 0, 0
    minimum_area = None
    for line_no, face in faces:
        invalid = len(face) < 3
        for refs in face:
            for value, count in zip(refs, (len(positions), len(uvs), len(normals))):
                if value is not None and not 0 <= value < count:
                    invalid = True
            if refs[0] is None:
                invalid = True
            if refs[1] is None or refs[2] is None:
                missing_attributes += 1
        if invalid:
            invalid_faces += 1
            continue
        for i in range(1, len(face) - 1):
            triangles += 1
            a, b, c = (positions[face[j][0]] for j in (0, i, i + 1))
            ab = tuple(y - x for x, y in zip(a, b))
            ac = tuple(y - x for x, y in zip(a, c))
            cross = (ab[1]*ac[2]-ab[2]*ac[1], ab[2]*ac[0]-ab[0]*ac[2], ab[0]*ac[1]-ab[1]*ac[0])
            magnitude = math.sqrt(sum(x*x for x in cross))
            area = magnitude / 2
            minimum_area = area if minimum_area is None else min(minimum_area, area)
            if area <= 1e-12 or not math.isfinite(area):
                degenerate += 1
            elif face[0][2] is not None:
                normal = normals[face[0][2]]
                agreement = sum(x*y for x, y in zip(cross, normal)) / magnitude
                if agreement < -0.05:
                    backward += 1
    if invalid_faces:
        checks.error(rel, f"{invalid_faces} faces contain invalid indices or fewer than three vertices")
    if missing_attributes:
        checks.error(rel, f"{missing_attributes} face corners are missing UV or normal indices")
    if degenerate:
        checks.error(rel, f"{degenerate} triangles have zero/nonfinite area")
    zero_normals = sum(sum(x*x for x in normal) < 1e-12 for normal in normals)
    nonunit_normals = sum(abs(math.sqrt(sum(x*x for x in normal)) - 1) > 0.03 for normal in normals)
    if zero_normals:
        checks.error(rel, f"{zero_normals} zero-length normals")
    if nonunit_normals:
        checks.warn(rel, f"{nonunit_normals} normals differ from unit length by more than 3 percent")
    if backward:
        checks.warn(rel, f"{backward}/{triangles} triangles have winding opposite their first corner normal; inspect backface culling in Unity")
    if triangles > 100000:
        checks.warn(rel, f"{triangles} triangles in one module; verify LOD and desktop budget")
    for library in libraries:
        mtl = path.parent / library
        if not mtl.is_file():
            checks.warn(rel, f"Referenced MTL absent ({library}); requires intentional runtime material assignment")
        else:
            defined = set(re.findall(r"^newmtl\s+(.+)$", mtl.read_text(encoding="utf-8"), re.MULTILINE))
            if materials - defined:
                checks.error(rel, f"MTL lacks used materials: {sorted(materials-defined)}")
    bounds = None
    if positions:
        bounds = [[min(v[i] for v in positions) for i in range(3)], [max(v[i] for v in positions) for i in range(3)]]
    return {"path": rel, "vertices": len(positions), "uvs": len(uvs), "normals": len(normals),
            "triangles": triangles, "materials": sorted(materials), "bounds": bounds,
            "minimum_triangle_area": minimum_area, "sha256": sha256(path.read_bytes())}


def inspect_meta(path, root, checks):
    rel = path.relative_to(root).as_posix()
    meta = Path(str(path) + ".meta")
    if not meta.is_file():
        checks.error(rel, "Missing .meta file; imported GUID and asset settings are not reproducible")
        return None
    text = meta.read_text(encoding="utf-8")
    guid = meta_value(text, "guid")
    if not guid or not re.fullmatch(r"[a-fA-F0-9]{32}", guid):
        checks.error(rel, "Meta GUID is missing or malformed")
    if path.suffix.lower() == ".obj":
        if "ModelImporter:" not in text:
            checks.error(rel, "OBJ metadata is not ModelImporter metadata")
        if meta_value(text, "isReadable") != "1":
            checks.error(rel, "District OBJ must retain read access for runtime CombineMeshes")
        if meta_value(text, "globalScale") not in {"1", "1.0"}:
            checks.error(rel, "District module must import at globalScale 1 metre units")
        if meta_value(text, "addColliders") == "1":
            checks.error(rel, "Decorative module importer must not add gameplay colliders")
    return text


def inspect_texture(path, root, checks, maximum):
    rel = path.relative_to(root).as_posix()
    text = inspect_meta(path, root, checks)
    result = {"path": rel, "sha256": sha256(path.read_bytes())}
    if Image is None:
        checks.error(rel, "Pillow is required to inspect image data")
        return result
    try:
        with Image.open(path) as img:
            img.load()
            width, height = img.size
            result.update({"width": width, "height": height, "mode": img.mode})
            if width > maximum or height > maximum:
                checks.error(rel, f"Texture exceeds {maximum}px budget: {width}x{height}")
            if width & (width - 1) or height & (height - 1):
                checks.warn(rel, "Texture dimensions are not powers of two")
            if "normal" in path.stem.lower() and img.mode not in {"RGB", "RGBA"}:
                checks.error(rel, "Normal map must have RGB color channels")
            if path.stem.lower().endswith("_mask") and img.mode != "RGBA":
                checks.error(rel, "Packed surface mask needs RGBA including smoothness alpha")
    except (OSError, ValueError) as error:
        checks.error(rel, f"Unreadable texture: {error}")
    if text:
        keys = ("textureType", "sRGBTexture", "enableMipMap", "isReadable", "maxTextureSize", "textureCompression", "filterMode", "aniso")
        result["import"] = {key: meta_value(text, key) for key in keys}
        if "TextureImporter:" not in text:
            checks.error(rel, "Image metadata is not TextureImporter metadata")
        stem = path.stem.lower()
        if stem.endswith("_normal"):
            if meta_value(text, "textureType") != "1":
                checks.error(rel, "Normal texture requires textureType: 1")
            if meta_value(text, "sRGBTexture") != "0":
                checks.error(rel, "Normal texture must import as linear")
        if stem.endswith(("_mask", "_emission")) and meta_value(text, "sRGBTexture") != "0":
            checks.error(rel, "Surface masks/emission data must import as linear for this material contract")
        if stem.endswith("_albedo") and meta_value(text, "sRGBTexture") != "1":
            checks.error(rel, "Albedo must import as sRGB")
        if meta_value(text, "isReadable") == "1":
            checks.warn(rel, "Texture retains CPU readback copy; confirm runtime need")
        if meta_value(text, "enableMipMap") != "1":
            checks.warn(rel, "No mipmaps specified; inspect distant surface shimmer")
    return result


def inspect_metadata_guids(root, checks):
    seen = {}
    for path in sorted((root / "Assets").rglob("*.meta")):
        guid = meta_value(path.read_text(encoding="utf-8"), "guid")
        if guid:
            if guid in seen:
                checks.error(path.relative_to(root), f"GUID duplicates {seen[guid]}")
            seen[guid] = path.relative_to(root).as_posix()
    return len(seen)


def compare_baseline(root, baseline, checks, allowed, diff_path):
    changed, deleted, unchanged, diffs = [], [], 0, []
    existing = set()
    with zipfile.ZipFile(baseline) as archive:
        corrupt = archive.testzip()
        if corrupt:
            checks.error(baseline, f"Source ZIP CRC failure: {corrupt}")
        members = [i for i in archive.infolist() if not i.is_dir()]
        # Find the Unity project root using the version file rather than assuming ZIP folder names.
        versions = [i.filename for i in members if i.filename.endswith("ProjectSettings/ProjectVersion.txt")]
        if len(versions) != 1:
            checks.error(baseline, "Expected exactly one Unity project root in source ZIP")
            return {}
        prefix = versions[0][:-len("ProjectSettings/ProjectVersion.txt")]
        for member in members:
            if not member.filename.startswith(prefix):
                continue
            name = member.filename[len(prefix):]
            if not name:
                continue
            existing.add(name)
            local = root / name
            if not local.is_file():
                deleted.append(name)
                checks.error(name, "Original file missing from updated project")
                continue
            before = archive.read(member)
            after = local.read_bytes()
            if sha256(before) == sha256(after):
                unchanged += 1
                continue
            protected = is_protected(name)
            explicitly_allowed = name in allowed
            change = {"path": name, "before_sha256": sha256(before), "after_sha256": sha256(after),
                      "protected": protected, "explicitly_allowed": explicitly_allowed}
            changed.append(change)
            if protected and not explicitly_allowed:
                checks.error(name, "Protected gameplay/control/content original was modified")
            elif not explicitly_allowed:
                checks.warn(name, "Modified original needs human review; no explicit --allow-modified entry")
            try:
                old_text, new_text = before.decode("utf-8-sig"), after.decode("utf-8-sig")
                diffs.extend(difflib.unified_diff(old_text.splitlines(True), new_text.splitlines(True), fromfile="baseline/"+name, tofile="updated/"+name))
            except UnicodeDecodeError:
                diffs.append(f"Binary file changed: {name}\n")
    added = []
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        rel = path.relative_to(root)
        if any(part in IGNORED_DIRS for part in rel.parts):
            continue
        if rel.as_posix() not in existing:
            added.append(rel.as_posix())
    if diff_path:
        diff_path.parent.mkdir(parents=True, exist_ok=True)
        diff_path.write_text("".join(diffs), encoding="utf-8")
    return {"archive": str(baseline), "original_count": len(existing), "unchanged_count": unchanged,
            "modified": changed, "deleted": deleted, "added": sorted(added),
            "protected_original_count": sum(is_protected(name) for name in existing),
            "protected_originals_unchanged": not any(c["protected"] for c in changed) and not any(is_protected(name) for name in deleted),
            "diff_file": str(diff_path) if diff_path else None}


def inspect_manifests(root, checks):
    results = []
    for path in sorted(root.glob("Assets/**/ArtDistrict/**/*.json")):
        rel = path.relative_to(root).as_posix()
        try:
            content = json.loads(path.read_text(encoding="utf-8"))
        except (OSError, ValueError) as error:
            checks.error(rel, f"Invalid JSON: {error}")
            continue
        verified = 0
        entries = content.get("maps", content.get("assets", [])) if isinstance(content, dict) else []
        if isinstance(entries, list):
            for entry in entries:
                if not isinstance(entry, dict) or "sha256" not in entry:
                    continue
                if "file" in entry:
                    asset = path.parent / entry["file"]
                elif "resource" in entry:
                    asset = root / "Assets/NeonSyndicate/Resources" / (entry["resource"] + ".obj")
                else:
                    continue
                if not asset.is_file():
                    checks.error(rel, f"Manifest asset missing: {asset.name}")
                    continue
                if sha256(asset.read_bytes()) != entry["sha256"]:
                    checks.error(rel, f"Manifest SHA256 mismatch: {asset.name}")
                else:
                    verified += 1
        if path.name == "mesh_inventory.json" and content.get("asset_count") != len(entries):
            checks.error(rel, "Inventory asset_count does not match its asset records")
        results.append({"path": rel, "root_type": type(content).__name__, "verified_asset_hashes": verified})
    return results


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--baseline", type=Path, help="Original supplied ZIP for full preservation comparison")
    parser.add_argument("--allow-modified", action="append", default=[], metavar="PROJECT_RELATIVE_PATH",
                        help="Explicitly reviewed original file change; repeat per file")
    parser.add_argument("--report", type=Path, help="Write detailed JSON report")
    parser.add_argument("--diff", type=Path, help="Write unified diff of every modified original text file")
    parser.add_argument("--max-texture-size", type=int, default=2048)
    args = parser.parse_args(argv)
    root = args.project.resolve()
    checks = Checks()
    if not (root / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("--project must identify the Unity project root")
    models = sorted((root / "Assets").rglob("*.obj"))
    textures = sorted(p for p in (root / "Assets").rglob("*") if p.suffix.lower() in {".png", ".tga", ".jpg", ".jpeg"})
    if not models:
        checks.error("Assets", "No imported OBJ modules found in demonstration delivery")
    if not textures:
        checks.error("Assets", "No imported material textures found in demonstration delivery")
    checks.results["models"] = []
    for path in models:
        checks.results["models"].append(inspect_obj(path, root, checks))
        inspect_meta(path, root, checks)
    checks.results["textures"] = [inspect_texture(path, root, checks, args.max_texture_size) for path in textures]
    checks.results["unique_asset_guids"] = inspect_metadata_guids(root, checks)
    checks.results["manifests"] = inspect_manifests(root, checks)
    if args.baseline:
        checks.results["baseline"] = compare_baseline(root, args.baseline, checks, EXPECTED_VISUAL_CHANGES | set(args.allow_modified), args.diff)
    else:
        checks.warn("baseline", "No baseline ZIP supplied; original gameplay preservation not checked")
    result = checks.finish()
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(result["status"] + ": " + str(len(models)) + " OBJ models, " + str(len(textures)) +
          " textures, " + str(len(checks.errors)) + " errors, " + str(len(checks.warnings)) + " warnings")
    for severity, items in (("ERROR", checks.errors), ("WARNING", checks.warnings)):
        for item in items[:40]:
            print(f"{severity}: {item['path']}: {item['message']}")
        if len(items) > 40:
            print(f"{severity}: {len(items)-40} additional entries in JSON report")
    if args.report:
        print("Report: " + str(args.report))
    return 1 if checks.errors else 0


if __name__ == "__main__":
    sys.exit(main())
