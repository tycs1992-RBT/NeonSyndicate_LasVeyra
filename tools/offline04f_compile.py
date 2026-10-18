#!/usr/bin/env python3
"""Compile the game against genuine supplied Unity metadata without starting Unity.

This is managed source/API verification only. The reference directory must come
from the exact editor release. No source edits, fake engine APIs or API aliases
are applied. Optional package/editor dependencies enable additional matrix rows.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--unity-references", type=Path, required=True)
    parser.add_argument("--nunit", type=Path, required=True)
    parser.add_argument("--input-system", type=Path)
    parser.add_argument("--urp-references", type=Path)
    parser.add_argument("--editor-references", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    root = args.root.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))[-1]
    refs = sorted(dotnet.parent.glob("packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll"))
    unity = sorted(args.unity_references.resolve().glob("*.dll"))
    if not any(p.name == "UnityEngine.CoreModule.dll" for p in unity):
        raise SystemExit("Supply genuine UnityEngine modules, including CoreModule.")
    paths = {
        name: sorted((root / prefix).rglob("*.cs"))
        for name, prefix in {
            "runtime": "Assets/NeonSyndicate/Scripts/Runtime",
            "editor": "Assets/NeonSyndicate/Scripts/Editor",
            "tests": "Assets/NeonSyndicate/Tests/EditMode",
        }.items()
    }
    # Compile one immutable source snapshot across every matrix row. Coordinated
    # gameplay streams can continue editing without making row results disagree.
    digest = hashlib.sha256()
    source_root = output / "sources"
    for group, originals in paths.items():
        captured = []
        for path in originals:
            relative = path.relative_to(root)
            content = path.read_bytes()
            digest.update(relative.as_posix().encode() + b"\0" + content + b"\0")
            target = source_root / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(content)
            captured.append(target)
        paths[group] = captured
    cases = []
    for editor in (False, True):
        for input_mode in ("classic", "both", "input-system-only"):
            for urp in (False, True):
                label = ("editor" if editor else "player") + "-" + input_mode + "-" + ("urp" if urp else "builtin")
                missing = []
                if editor and not args.editor_references:
                    missing.append("Genuine matching UnityEditor assemblies unavailable")
                if input_mode != "classic" and not args.input_system:
                    missing.append("Compiled Unity.InputSystem 1.17.0 assembly unavailable")
                if urp and not args.urp_references:
                    missing.append("Compiled matching optional URP/Core package assemblies unavailable")
                if missing:
                    cases.append({"name": label, "status": "SKIPPED", "reasons": missing})
                    continue
                symbols = ["UNITY_STANDALONE_WIN", "UNITY_64", "NS_INPUT_SYSTEM"]
                if input_mode != "input-system-only":
                    symbols.append("ENABLE_LEGACY_INPUT_MANAGER")
                if input_mode != "classic":
                    symbols.append("ENABLE_INPUT_SYSTEM")
                if urp:
                    symbols.append("NS_URP")
                if editor:
                    symbols += ["UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_INCLUDE_TESTS"]
                files = paths["runtime"] + paths["tests"] + (paths["editor"] if editor else [])
                dependencies = refs + unity + [args.nunit.resolve()]
                if args.input_system:
                    dependencies.append(args.input_system.resolve())
                if urp:
                    dependencies += sorted(args.urp_references.resolve().glob("*.dll"))
                if editor:
                    dependencies += sorted(args.editor_references.resolve().glob("*.dll"))
                assembly = output / (label + ".dll")
                options = ["-nologo", "-target:library", "-langversion:9", "-define:" + ",".join(symbols),
                           "-pathmap:" + str(source_root) + "=" + str(root), "-out:" + str(assembly)]
                options += ["-r:" + str(p) for p in dependencies] + [str(p) for p in files]
                rsp = output / (label + ".rsp")
                rsp.write_text("\n".join('"' + option + '"' for option in options))
                run = subprocess.run([str(dotnet), str(csc), "@" + str(rsp)], capture_output=True, text=True)
                log = run.stdout + run.stderr
                (output / (label + ".log")).write_text(log)
                errors = [re.sub(r"\(\d+,\d+\)", "", line.replace(str(source_root) + "/", "").replace(str(root) + "/", "")) for line in log.splitlines() if ": error " in line]
                cases.append({"name": label, "status": "PASS" if run.returncode == 0 else "FAIL", "exit_code": run.returncode,
                              "source_files": len(files), "errors": errors, "warnings": len(re.findall(": warning ", log)), "assembly": str(assembly)})
    report = {"scope": "Genuine Unity metadata managed API compile only, with .NET 8 BCL references. Not Unity import, Editor compile, native execution or Windows build.",
              "invocation": [sys.executable] + sys.argv, "exit_code": 1 if any(row["status"] == "FAIL" for row in cases) else 0,
              "project_unity_version": (root / "ProjectSettings/ProjectVersion.txt").read_text().strip(),
              "source_sha256": digest.hexdigest(), "reference_sha256": {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in unity},
              "configurations": cases, "passed": sum(row["status"] == "PASS" for row in cases),
              "failed": sum(row["status"] == "FAIL" for row in cases), "skipped": sum(row["status"] == "SKIPPED" for row in cases),
              "native_unity": "NOT RUN"}
    (output / "compile-report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in ("scope", "passed", "failed", "skipped", "source_sha256")}, indent=2))
    for row in cases:
        if row["status"] == "FAIL":
            print("\n".join(row["errors"]))
    return 1 if report["failed"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
