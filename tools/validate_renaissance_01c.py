#!/usr/bin/env python3
"""Reproduce the 01C offline asset, source-preservation and C# grammar checks.

Usage:
  python tools/validate_renaissance_01c.py --baseline path/to/01B/project-or.zip

Pillow is required for texture inspection. Optional tree-sitter and
tree-sitter-c-sharp enable the twelve conditional-source grammar checks.
This tool does not compile C#, import Unity assets, execute Unity tests,
exercise controls, render the game, or measure runtime performance.
"""
from pathlib import Path
import argparse
import difflib
import hashlib
import json
import sys
import zipfile

import validate_art_district as assets
from validate_renaissance_01b import syntax_checks


def approved_original_changes():
    """Explicit reviewed change surface for the 01C user-acceptance pass."""
    runtime = (
        "AI/NpcController.cs", "AI/NpcController.Creature.cs", "AI/NpcController.Tactics.cs",
        "AI/PoliceManager.cs", "AI/Threats.cs", "AI/WantedSystem.cs",
        "ArtDistrict/DistrictArchitecture.cs", "ArtDistrict/DistrictAtmosphere.cs",
        "Combat/CombatDirector.cs", "Core/Game.cs", "Core/GameRoot.cs", "Core/Settings.cs",
        "Creatures/Creature.cs", "Creatures/Creature.Attacks.cs", "Creatures/Creature.Brain.cs", "Creatures/Creature.Motion.cs",
        "Creatures/CreatureDirector.cs", "Creatures/CreatureDirector.Bosses.cs",
        "Gameplay/WorldEvents.cs", "Gameplay/Events/EventInstance.cs",
        "Gameplay/LootWorld.Guards.cs", "Gameplay/LootWorld.Chests.cs",
        "UI/MenuSystem.cs", "Vehicles/BikeMath.cs", "Vehicles/TrafficManager.Bikes.cs",
        "Vehicles/Vehicle.cs", "Vehicles/Vehicle.Bike.cs", "World/CityBuilder.cs",
    )
    approved = {"Assets/NeonSyndicate/Scripts/Runtime/" + name for name in runtime}
    approved.update(("tools/generate_district_meshes.py", "START_HERE_ART_DEMO.md",
                     "Assets/NeonSyndicate/Resources/ArtDistrict/Models/mesh_inventory.json"))
    for name in ("arch_frame", "pilaster", "dome", "crystal_shard"):
        approved.add("Assets/NeonSyndicate/Resources/ArtDistrict/Models/" + name + ".obj")
    return approved


def approved_added_assets():
    """Only these new Unity assets belong to the reviewed 01C implementation."""
    runtime = (
        "AI/NpcController.StarterSafety.cs", "AI/PoliceResponseRules.cs",
        "AI/StarterSafety.cs", "AI/StarterSafetyRules.cs", "Combat/CombatPressureRules.cs",
        "Creatures/Creature.StarterHabitat.cs", "Creatures/StarterHabitat.cs",
        "Gameplay/AmbientEncounterRules.cs",
    )
    tests = ("AmbientEncounterTests.cs", "ArtDistrictStarterSelectionTests.cs", "CombatPressureTests.cs",
             "PlayerVehicleHandlingTests.cs", "PoliceResponseTests.cs", "StarterSafetyTests.cs")
    approved = {"Assets/NeonSyndicate/Scripts/Runtime/" + name for name in runtime}
    approved.update("Assets/NeonSyndicate/Tests/EditMode/" + name for name in tests)
    approved.add("Assets/NeonSyndicate/Resources/ArtDistrict/Models/arch_frame_lod.obj")
    approved.update(name + ".meta" for name in tuple(approved))
    return approved


def digest(content):
    return hashlib.sha256(content).hexdigest()


def included(relative):
    return not any(part in assets.IGNORED_DIRS for part in relative.parts)


def baseline_files(path):
    """Yield original files from the exact baseline directory or a project ZIP."""
    if path.is_dir():
        if not (path / "ProjectSettings/ProjectVersion.txt").is_file():
            raise ValueError("Baseline directory must be the Unity project root")
        for file in sorted(path.rglob("*")):
            relative = file.relative_to(path)
            if file.is_file() and included(relative):
                yield relative.as_posix(), file.read_bytes()
        return
    with zipfile.ZipFile(path) as archive:
        versions = [name for name in archive.namelist() if name.endswith("ProjectSettings/ProjectVersion.txt")]
        if len(versions) != 1:
            raise ValueError("Baseline ZIP must contain exactly one Unity project")
        prefix = versions[0][:-len("ProjectSettings/ProjectVersion.txt")]
        for entry in sorted(archive.infolist(), key=lambda e: e.filename):
            if entry.is_dir() or not entry.filename.startswith(prefix):
                continue
            relative = Path(entry.filename[len(prefix):])
            if included(relative):
                yield relative.as_posix(), archive.read(entry)


def strict_preservation(root, baseline, approved, diff_path):
    changed, deleted, unexpected, differences = [], [], [], []
    unchanged, originals = 0, set()
    for name, old in baseline_files(baseline):
        originals.add(name)
        current = root / name
        if not current.is_file():
            deleted.append(name)
            continue
        new = current.read_bytes()
        if new == old:
            unchanged += 1
            continue
        changed.append({"path": name, "before_sha256": digest(old), "after_sha256": digest(new),
                        "explicitly_approved_path": name in approved})
        if name not in approved:
            unexpected.append(name)
        try:
            differences.extend(difflib.unified_diff(old.decode("utf-8-sig").splitlines(True),
                                                   new.decode("utf-8-sig").splitlines(True),
                                                   fromfile="01B/" + name, tofile="01C/" + name))
        except UnicodeDecodeError:
            differences.append("Binary file changed: " + name + "\n")
    added, added_assets, unexpected_assets, missing_meta = [], [], [], []
    allowed_assets = approved_added_assets()
    for path in root.rglob("*"):
        relative = path.relative_to(root)
        if path.is_file() and included(relative) and relative.as_posix() not in originals:
            name = relative.as_posix()
            added.append(name)
            if relative.parts[0] == "Assets":
                added_assets.append({"path": name, "sha256": digest(path.read_bytes()), "bytes": path.stat().st_size})
                if name not in allowed_assets:
                    unexpected_assets.append(name)
                if path.suffix != ".meta" and not Path(str(path) + ".meta").is_file():
                    missing_meta.append(name)
    diff_path.write_text("".join(differences), encoding="utf-8")
    return {"scope": "Exact file comparison against the explicit 01B baseline; no runtime behavior claim",
            "baseline": str(baseline), "original_files": len(originals), "unchanged": unchanged,
            "modified_count": len(changed), "modified": changed, "deleted": deleted,
            "outside_approved_scope": unexpected, "added": sorted(added),
            "added_unity_assets": sorted(added_assets, key=lambda entry: entry["path"]),
            "unexpected_new_assets": sorted(unexpected_assets), "new_assets_missing_meta": sorted(missing_meta),
            "allowed_paths": sorted(approved), "allowed_new_assets": sorted(allowed_assets),
            "diff_file": diff_path.name,
            "status": "PASS_PRESERVATION" if not deleted and not unexpected and not unexpected_assets and not missing_meta else "FAIL"}


def inspect_assets(root):
    checks = assets.Checks()
    models = sorted((root / "Assets").rglob("*.obj"))
    textures = sorted(p for p in (root / "Assets").rglob("*") if p.suffix.lower() in {".png", ".tga", ".jpg", ".jpeg"})
    if not models or not textures:
        checks.error("Assets", "Expected imported model and texture assets")
    checks.results["models"] = []
    for path in models:
        checks.results["models"].append(assets.inspect_obj(path, root, checks))
        assets.inspect_meta(path, root, checks)
    checks.results["textures"] = [assets.inspect_texture(p, root, checks, 2048) for p in textures]
    checks.results["unique_asset_guids"] = assets.inspect_metadata_guids(root, checks)
    checks.results["manifests"] = assets.inspect_manifests(root, checks)
    return checks.finish()


def save(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--baseline", type=Path, required=True)
    parser.add_argument("--report-dir", type=Path)
    args = parser.parse_args()
    root, baseline = args.project.resolve(), args.baseline.resolve()
    if not (root / "ProjectSettings/ProjectVersion.txt").is_file():
        parser.error("--project must identify the Unity project root")
    output = args.report_dir.resolve() if args.report_dir else root / "Docs/Renaissance01C"
    output.mkdir(parents=True, exist_ok=True)
    inspection = inspect_assets(root)
    save(output / "OFFLINE_ASSET_VALIDATION_01C.json", inspection)
    print(inspection["status"], "assets", inspection["error_count"], "errors", flush=True)
    preservation = strict_preservation(root, baseline, approved_original_changes(), output / "MASTER01B_CHANGES.diff")
    save(output / "STRICT_PRESERVATION_01C.json", preservation)
    print(preservation["status"], "unchanged", preservation["unchanged"], "modified", preservation["modified_count"], flush=True)
    if preservation["outside_approved_scope"] or preservation["deleted"]:
        print("Outside reviewed paths:", preservation["outside_approved_scope"], "Deleted:", preservation["deleted"], flush=True)
    if preservation["unexpected_new_assets"] or preservation["new_assets_missing_meta"]:
        print("Unexpected new assets:", preservation["unexpected_new_assets"], "Missing metadata:", preservation["new_assets_missing_meta"], flush=True)
    syntax = syntax_checks(root)
    save(output / "CSHARP_SYNTAX_12_CONFIGS_01C.json", syntax)
    print(syntax["status"], "grammar checks only; NOT C# compilation", flush=True)
    summary = {
        "validation_scope": "Offline asset structure, exact baseline preservation and C# grammar in 12 preprocessor configurations",
        "assets": inspection["status"], "preservation": preservation["status"], "syntax": syntax["status"],
        "unity_import": "NOT_RUN", "unity_compilation": "NOT_RUN", "unity_test_runner": "NOT_RUN",
        "runtime_gameplay": "NOT_RUN", "visual_comparison_in_unity": "NOT_RUN", "desktop_performance": "NOT_RUN",
    }
    failure = inspection["status"] == "FAIL" or preservation["status"] == "FAIL" or syntax["status"] == "FAIL"
    summary["status"] = "FAIL" if failure else ("PASS_OFFLINE_WITH_SYNTAX_NOT_RUN" if syntax["status"] == "NOT_RUN" else "PASS_OFFLINE")
    save(output / "VALIDATION_SUMMARY_01C.json", summary)
    return int(failure)


if __name__ == "__main__":
    sys.exit(main())
