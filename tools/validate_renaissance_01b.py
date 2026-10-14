#!/usr/bin/env python3
"""Reproduce the 01B offline asset, source-preservation and C# syntax checks.

  python tools/validate_renaissance_01b.py --baseline CrystalManorSaga_Asterfall_ArtDemo.zip

Requires Pillow. Optional tree-sitter and tree-sitter-c-sharp enable syntax checks.
No result from this script constitutes Unity compilation, rendering or play testing.
"""
from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess
import sys
import zipfile


def approved_original_changes():
    approved = {"START_HERE_ART_DEMO.md", "tools/generate_district_meshes.py", "tools/generate_district_textures.py",
                "Assets/NeonSyndicate/Scripts/Editor/DistrictArtEditor.cs",
                "Assets/NeonSyndicate/Resources/ArtDistrict/Shaders/DistrictCrystal.shader",
                "Assets/NeonSyndicate/Resources/ArtDistrict/Models/mesh_inventory.json",
                "Assets/NeonSyndicate/Resources/ArtDistrict/Textures/texture_manifest.json",
                "Assets/NeonSyndicate/Resources/ArtDistrict/Textures/crystal_Emission.png"}
    for name in ("arch_frame", "arch_infill", "cornice", "dome", "parapet", "pilaster", "window_frame", "window_infill"):
        approved.add("Assets/NeonSyndicate/Resources/ArtDistrict/Models/" + name + ".obj")
    for name in ("sandstone", "basalt", "bronze", "ceramic", "glass", "foliage", "bark", "fabric", "dragonHide", "dragonWing"):
        for suffix in ("Albedo", "Normal", "Mask"):
            approved.add("Assets/NeonSyndicate/Resources/ArtDistrict/Textures/" + name + "_" + suffix + ".png")
    for name in ("ArtDistrict/DistrictArchitecture.cs", "ArtDistrict/DistrictAtmosphere.cs", "ArtDistrict/DistrictCrystalFx.cs",
                 "ArtDistrict/DistrictMaterials.cs", "Core/Game.cs", "Creatures/CreatureRig.cs", "World/CityBuilder.cs"):
        approved.add("Assets/NeonSyndicate/Scripts/Runtime/" + name)
    return approved


def save(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def strict_preservation(root, baseline, approved):
    modified, deleted, unexpected = [], [], []
    unchanged = 0
    with zipfile.ZipFile(baseline) as archive:
        versions = [p for p in archive.namelist() if p.endswith("ProjectSettings/ProjectVersion.txt")]
        if len(versions) != 1:
            raise ValueError("Baseline must contain exactly one Unity project")
        prefix = versions[0][:-len("ProjectSettings/ProjectVersion.txt")]
        for member in archive.infolist():
            if member.is_dir() or not member.filename.startswith(prefix):
                continue
            relative = member.filename[len(prefix):]
            path = root / relative
            if not path.is_file():
                deleted.append(relative)
                continue
            old, new = archive.read(member), path.read_bytes()
            if old == new:
                unchanged += 1
                continue
            modified.append({"path": relative, "before_sha256": hashlib.sha256(old).hexdigest(),
                             "after_sha256": hashlib.sha256(new).hexdigest()})
            if relative not in approved:
                unexpected.append(relative)
    return {"check": "Exact Master 01 comparison against an explicit 01B allowlist", "master_zip": baseline.name,
            "unchanged": unchanged, "modified_count": len(modified), "modified": modified, "deleted": deleted,
            "outside_approved_scope": unexpected, "allowed_paths": sorted(approved),
            "status": "PASS_PRESERVATION" if not deleted and not unexpected else "FAIL"}


def evaluate(expression, defines):
    expression = expression.split("//")[0].strip()
    if not re.fullmatch(r"[A-Za-z0-9_\s!&|()=]+", expression):
        raise ValueError("Unsupported preprocessor expression: " + expression)
    expression = re.sub(r"\b[A-Za-z_][A-Za-z0-9_]*\b",
                        lambda m: "True" if m[0] == "true" or m[0] in defines else "False", expression)
    expression = expression.replace("&&", " and ").replace("||", " or ")
    expression = re.sub(r"!(?!=)", " not ", expression)
    return bool(eval(expression, {"__builtins__": {}}, {}))


def preprocess(source, initial):
    defines, stack, active, result = set(initial), [], True, []
    for line in source.splitlines(keepends=True):
        directive = re.match(r"^\s*#(if|elif|else|endif|define|undef)\b(.*)", line)
        if not directive:
            result.append(line if active else "\n")
            continue
        kind, value = directive.groups()
        value = value.strip()
        if kind == "if":
            test = evaluate(value, defines)
            stack.append([active, test])
            active = active and test
        elif kind == "elif":
            parent, chosen = stack[-1]
            test = evaluate(value, defines)
            active = parent and not chosen and test
            stack[-1][1] = chosen or test
        elif kind == "else":
            parent, chosen = stack[-1]
            active = parent and not chosen
            stack[-1][1] = True
        elif kind == "endif":
            active = stack.pop()[0]
        elif active and kind == "define":
            defines.add(value)
        elif active and kind == "undef":
            defines.discard(value)
        result.append("\n")
    if stack:
        raise ValueError("Unclosed preprocessor block")
    return "".join(result)


def syntax_checks(root):
    try:
        from tree_sitter import Language, Parser
        import tree_sitter_c_sharp
    except ImportError:
        return {"status": "NOT_RUN", "reason": "Optional tree-sitter and tree-sitter-c-sharp are not installed"}
    parser = Parser(Language(tree_sitter_c_sharp.language()))
    files = sorted((root / "Assets").rglob("*.cs"))

    def parse_errors(data):
        todo, errors = [parser.parse(data).root_node], []
        while todo:
            node = todo.pop()
            if node.type == "ERROR" or node.is_missing:
                errors.append({"type": node.type, "line": node.start_point.row + 1, "column": node.start_point.column + 1})
            todo.extend(node.children)
        return errors

    variants = []
    for editor in (False, True):
        for urp in (False, True):
            for mode in ("classic", "both", "input-system-only"):
                defines = {"UNITY_6000_3_OR_NEWER", "UNITY_STANDALONE", "UNITY_STANDALONE_WIN", "NS_INPUT_SYSTEM"}
                if editor:
                    defines.update(("UNITY_EDITOR", "UNITY_EDITOR_WIN"))
                if urp:
                    defines.add("NS_URP")
                if mode != "classic":
                    defines.add("ENABLE_INPUT_SYSTEM")
                if mode != "input-system-only":
                    defines.add("ENABLE_LEGACY_INPUT_MANAGER")
                chosen = [p for p in files if editor or ("Editor" not in p.parts and "Tests" not in p.parts)]
                failures = []
                for path in chosen:
                    errors = parse_errors(preprocess(path.read_text(encoding="utf-8"), defines).encode())
                    if errors:
                        failures.append({"path": path.relative_to(root).as_posix(), "errors": errors})
                variants.append({"target": "editor" if editor else "Windows player", "rendering": "URP" if urp else "Built-in",
                                 "input": mode, "defines": sorted(defines), "files": len(chosen), "errors": failures})
    raw = []
    for path in files:
        errors = parse_errors(path.read_bytes())
        if errors:
            raw.append({"path": path.relative_to(root).as_posix(), "errors": errors})
    return {"scope": "Tree-sitter grammar after preprocessor evaluation; NOT C# compilation or Unity validation",
            "all_source_files": len(files), "raw_parse_errors": raw, "variants": variants, "configuration_count": 12,
            "status": "FAIL" if raw or any(v["errors"] for v in variants) else "PASS_SYNTAX_ONLY"}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--baseline", required=True, type=Path)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--report-dir", type=Path)
    args = parser.parse_args()
    root, baseline = args.project.resolve(), args.baseline.resolve()
    output = args.report_dir.resolve() if args.report_dir else root / "Docs/ArtDistrict01B"
    output.mkdir(parents=True, exist_ok=True)
    approved = approved_original_changes()
    command = [sys.executable, str(root / "tools/validate_art_district.py"), "--project", str(root), "--baseline", str(baseline),
               "--report", str(output / "OFFLINE_VALIDATION_01B.json"), "--diff", str(output / "MASTER01_CHANGES.diff")]
    for name in sorted(approved):
        command.extend(("--allow-modified", name))
    asset_result = subprocess.run(command, check=False)
    preservation = strict_preservation(root, baseline, approved)
    save(output / "STRICT_PRESERVATION_01B.json", preservation)
    print(preservation["status"], "unchanged", preservation["unchanged"], "modified", preservation["modified_count"], flush=True)
    if preservation["outside_approved_scope"] or preservation["deleted"]:
        print("Unexpected changes:", preservation["outside_approved_scope"], "Deleted:", preservation["deleted"], flush=True)
    syntax = syntax_checks(root)
    save(output / "CSHARP_SYNTAX_12_CONFIGS_01B.json", syntax)
    print(syntax["status"], "(syntax only, not Unity compilation)", flush=True)
    return int(asset_result.returncode != 0 or preservation["status"] == "FAIL" or syntax["status"] == "FAIL")


if __name__ == "__main__":
    sys.exit(main())
