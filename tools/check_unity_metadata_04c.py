#!/usr/bin/env python3
"""Audit actual Unity asset metadata and serialized GUID references without Unity.

Recovery scenes are inventoried separately and excluded from delivery checks.
Native AssetDatabase/import and compiled source inclusion require the Editor.
"""
import argparse
from collections import defaultdict
import hashlib
import json
from pathlib import Path
import re
import tempfile

ROOT = Path(__file__).resolve().parents[1]
GUID = re.compile(r"[0-9a-fA-F]{32}\Z")
BUILTIN = {"0" * 32, "0000000000000000e000000000000000", "0000000000000000f000000000000000"}
TEXT_TYPES = {".meta", ".asset", ".unity", ".prefab", ".mat", ".asmdef", ".asmref", ".json", ".controller", ".anim", ".playable"}
REFERENCE = re.compile(r"\bguid:\s*([^\s,}\]]+)|\bGUID:([a-zA-Z0-9]+)")


def valid_guid(value):
    return isinstance(value, str) and GUID.fullmatch(value) is not None


def excluded(path):
    return "_Recovery" in path.parts or any(part.startswith(".") for part in path.parts)


def audit(root):
    assets = root / "Assets"
    owners = defaultdict(list)
    invalid, missing, orphan, excluded_paths = [], [], [], []
    inventory = {}
    for path in sorted(assets.rglob("*")):
        relative = path.relative_to(root)
        if excluded(relative):
            if path.is_file(): excluded_paths.append(str(relative))
            continue
        if path.suffix != ".meta":
            if not Path(str(path) + ".meta").is_file(): missing.append(str(relative))
            continue
        text = path.read_text(encoding="utf-8-sig")
        values = re.findall(r"^guid:\s*(\S+)\s*$", text, re.M)
        inventory[str(relative)] = hashlib.sha256(path.read_bytes()).hexdigest()
        if len(values) != 1 or not valid_guid(values[0]):
            invalid.append({"path": str(relative), "values": values, "lengths": [len(value) for value in values]})
        for value in values: owners[value.lower()].append(str(relative))
        if not Path(str(path)[:-5]).exists(): orphan.append(str(relative))
    duplicates = {value: paths for value, paths in owners.items() if len(paths) > 1}
    local, builtin = 0, 0
    invalid_refs, unresolved = [], []
    for folder in ("Assets", "Packages", "ProjectSettings"):
        for path in sorted((root / folder).rglob("*")):
            if not path.is_file() or excluded(path.relative_to(root)) or path.suffix not in TEXT_TYPES: continue
            try: text = path.read_text(encoding="utf-8-sig")
            except UnicodeDecodeError: continue
            for line_number, line in enumerate(text.splitlines(), 1):
                if path.suffix == ".meta" and line.startswith("guid:"): continue
                for match in REFERENCE.finditer(line):
                    value = (match.group(1) or match.group(2)).lower()
                    row = {"path": str(path.relative_to(root)), "line": line_number, "guid": value}
                    if not valid_guid(value): invalid_refs.append(row)
                    elif value in owners: local += 1
                    elif value in BUILTIN: builtin += 1
                    else: unresolved.append(row)
    classes = []
    for path in sorted(assets.rglob("*.cs")):
        if excluded(path.relative_to(root)): continue
        if re.search(r"\bclass\s+TerritoryPlacement\b", path.read_text(encoding="utf-8-sig")):
            classes.append(str(path.relative_to(root)))
    return {"scope": __doc__.strip(), "metadata_count": len(inventory), "invalid_metadata": invalid,
        "duplicate_guids": duplicates, "missing_sidecars": missing, "orphan_sidecars": orphan,
        "serialized_references": {"resolved_local": local, "builtin": builtin, "invalid": invalid_refs, "unresolved": unresolved},
        "territory_placement_sources": classes, "excluded_recovery_or_hidden_files": excluded_paths,
        "metadata_sha256": inventory, "native_import": "NOT RUN", "native_assembly_inclusion": "NOT RUN"}


def regressions():
    cases = []
    def check(name, result): cases.append({"name": name, "passed": bool(result)})
    check("Reject reported 33-character runtime GUID", not valid_guid("180937b2c4b745e4bb42615b272b6be5e"))
    check("Reject 33-character placement-test GUID", not valid_guid("18b37ff5a738427e9f634318eb79508f1"))
    check("Reject 33-character builtin shader reference", not valid_guid("0000000000000000f0000000000000000"))
    check("Accept canonical builtin shader reference", valid_guid("0000000000000000f000000000000000"))
    check("Reject truncated GUID", not valid_guid("a" * 31))
    check("Reject nonhex GUID", not valid_guid("g" * 32))
    check("Reject otherwise-valid prefix with trailing text", not valid_guid("a" * 32 + "suffix"))
    check("Accept uppercase hex", valid_guid("ABCDEF0123456789" * 2))
    with tempfile.TemporaryDirectory(prefix="04c-meta-regression-") as directory:
        root = Path(directory); assets = root / "Assets"; assets.mkdir()
        (assets / "A.cs").write_text("class A {}")
        (assets / "A.cs.meta").write_text("fileFormatVersion: 2\nguid: " + "a" * 32 + "\n")
        (assets / "B.cs").write_text("class B {}")
        (assets / "B.cs.meta").write_text("fileFormatVersion: 2\nguid: " + "a" * 32 + "\n")
        (assets / "Missing.cs").write_text("class Missing {}")
        (assets / "Orphan.cs.meta").write_text("fileFormatVersion: 2\nguid: " + "b" * 32 + "\n")
        (assets / "Bad.mat").write_text("m_Shader: {fileID: 46, guid: 0000000000000000f0000000000000000, type: 0}\n")
        (assets / "Broken.mat").write_text("m_Shader: {fileID: 46, guid: " + "c" * 32 + ", type: 0}\n")
        report = audit(root)
        check("Audit detects duplicate ownership", "a" * 32 in report["duplicate_guids"])
        check("Audit detects missing sidecar", "Assets/Missing.cs" in report["missing_sidecars"])
        check("Audit detects orphan sidecar", "Assets/Orphan.cs.meta" in report["orphan_sidecars"])
        check("Audit rejects malformed serialized reference", len(report["serialized_references"]["invalid"]) == 1)
        check("Audit reports unresolved serialized reference", len(report["serialized_references"]["unresolved"]) == 1)
    return cases


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    report = audit(args.root.resolve()); report["regressions"] = regressions()
    report["passed"] = not any(report[key] for key in ("invalid_metadata", "duplicate_guids", "missing_sidecars", "orphan_sidecars"))
    report["passed"] &= not report["serialized_references"]["invalid"] and not report["serialized_references"]["unresolved"]
    report["passed"] &= len(report["territory_placement_sources"]) == 1 and all(case["passed"] for case in report["regressions"])
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"passed": report["passed"], "metadata_count": report["metadata_count"],
        "invalid_metadata": len(report["invalid_metadata"]), "invalid_references": len(report["serialized_references"]["invalid"]),
        "unresolved_references": len(report["serialized_references"]["unresolved"]), "regressions_passed": sum(case["passed"] for case in report["regressions"]),
        "native_import": "NOT RUN"}, indent=2))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
