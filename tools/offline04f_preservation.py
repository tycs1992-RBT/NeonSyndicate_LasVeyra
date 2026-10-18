#!/usr/bin/env python3
"""Compare protected source, save declarations and shipped payloads to the specified preserved baseline.

This complements runtime tests. It checks actual hydrated bytes and declarations,
not Unity import, save JsonUtility behavior, scene traversal or game performance.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[1]
BASE = "4c690c6248f0ff7f00098598e6e42e409d70f84f"


def block(source, marker):
    start = source.index(marker)
    opening = source.index("{", start)
    index, depth = opening + 1, 1
    while depth:
        depth += (source[index] == "{") - (source[index] == "}")
        index += 1
    return source[opening + 1:index - 1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--baseline", default=BASE)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    def git(*command):
        return subprocess.check_output(["git", *command], cwd=root)
    paths = git("ls-tree", "-r", "--name-only", args.baseline).decode().splitlines()
    checks = []
    def check(name, result, detail):
        checks.append({"name": name, "passed": bool(result), "detail": detail})
    protected = []
    for name in paths:
        if name.startswith(("Packages/", "ProjectSettings/", "Assets/NeonSyndicate/Resources/")):
            protected.append(name)
        elif name.startswith("Assets/") and Path(name).suffix in (".meta", ".asmdef", ".asmref", ".prefab", ".unity", ".mat", ".shader", ".obj", ".fbx", ".bytes", ".png", ".jpg", ".wav", ".anim", ".controller"):
            protected.append(name)
    changed = []
    for name in protected:
        old = git("show", args.baseline + ":" + name)
        path = root / name
        if not path.is_file():
            changed.append(name)
            continue
        current = path.read_bytes()
        if old.startswith(b"version https://git-lfs.github.com/spec/v1"):
            expected = re.search(rb"oid sha256:([a-f0-9]{64})", old).group(1).decode()
            same = hashlib.sha256(current).hexdigest() == expected
        else:
            same = old == current
        if not same:
            changed.append(name)
    check("All pre-existing assets, metadata, quest/event JSON, settings and package state preserved", not changed,
          {"files": len(protected), "changed": changed})
    runtime = "Assets/NeonSyndicate/Scripts/Runtime/"
    old_save = git("show", args.baseline + ":" + runtime + "Gameplay/SaveSystem.cs").decode("utf-8-sig")
    new_save = (root / runtime / "Gameplay/SaveSystem.cs").read_text(encoding="utf-8-sig")
    for label, filename, marker in (
        ("SaveData", "Gameplay/SaveSystem.cs", "public sealed class SaveData"),
        ("EventSave", "Gameplay/WorldEvents.cs", "public sealed class EventSave"),
        ("GuidanceSave", "Gameplay/Guidance04E/CampaignDirector.cs", "public sealed class GuidanceSave"),
    ):
        old = old_save if label == "SaveData" else git("show", args.baseline + ":" + runtime + filename).decode("utf-8-sig")
        new = new_save if label == "SaveData" else (root / runtime / filename).read_text(encoding="utf-8-sig")
        pattern = r"\bpublic\s+(?!sealed\b|static\b|class\b)([^;{}]+;)"
        before = re.findall(pattern, block(old, marker))
        after = re.findall(pattern, block(new, marker))
        missing = [field for field in before if field not in after]
        check(label + " declarations remain append-only", not missing,
              {"baseline_fields": len(before), "current_fields": len(after), "missing_or_changed": missing,
               "appended": [field for field in after if field not in before]})
    for filename, marker in (("Combat/WeaponData.cs", "public enum WeaponId"),):
        old = git("show", args.baseline + ":" + runtime + filename).decode("utf-8-sig")
        new = (root / runtime / filename).read_text(encoding="utf-8-sig")
        def entries(source):
            clean = re.sub(r"//[^\n]*", "", block(source, marker))
            return [part.strip() for part in clean.split(",") if part.strip()]
        check("Stable saved WeaponId entries and order preserved", entries(old) == entries(new), entries(new))
    name = runtime + "Gameplay/LootTypes.cs"
    old = git("show", args.baseline + ":" + name).decode("utf-8-sig")
    new = (root / name).read_text(encoding="utf-8-sig")
    check("Stable saved unique catalog remains exact", block(old, "public static class LootUniques") == block(new, "public static class LootUniques"), name)
    report = {"scope": __doc__.strip(), "baseline": args.baseline, "checks": checks,
              "passed": sum(row["passed"] for row in checks), "failed": sum(not row["passed"] for row in checks), "native_unity": "NOT RUN"}
    args.report.parent.mkdir(parents=True, exist_ok=True)
    args.report.write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return 1 if report["failed"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
