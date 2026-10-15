#!/usr/bin/env python3
"""Supplementary Roslyn diagnostic comparison using genuine, older Unity references.

This does NOT compile with pinned Unity 6000.3.16f1 and does not execute tests.
Provide an existing .NET SDK, UnityEngine 2021.3 module references and NUnit 3.
The only adaptation is a PhysicsMaterial alias in scratch copies of Vehicle.cs.
All other Unity 6 API mismatches remain errors and must match the baseline.
No repository source is changed and no artificial Unity API stubs are used.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
BASE = "8af8559fa646672d94f2c27ea04e3e2e0a1728cf"


def diagnostics(log):
    return Counter(re.sub(r"\(\d+,\d+\)", "", re.sub(r"^.*?/Assets/", "Assets/", line))
                   for line in log.splitlines() if ": error " in line)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--unity-references", type=Path, required=True)
    parser.add_argument("--nunit", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--baseline", default=BASE)
    args = parser.parse_args()
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))[-1]
    refs = sorted(dotnet.parent.glob("packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll"))
    refs += sorted(args.unity_references.glob("*.dll")) + [args.nunit.resolve()]
    if not any(p.name == "UnityEngine.CoreModule.dll" for p in refs):
        raise SystemExit("A genuine UnityEngine.CoreModule reference is required.")
    args.output.mkdir(parents=True, exist_ok=True)
    paths = subprocess.check_output(["git", "ls-tree", "-r", "--name-only", args.baseline], cwd=ROOT, text=True).splitlines()
    prefixes = ("Assets/NeonSyndicate/Scripts/Runtime/", "Assets/NeonSyndicate/Tests/EditMode/")
    paths = [p for p in paths if p.endswith(".cs") and p.startswith(prefixes)]
    results = {}
    source_digest = hashlib.sha256()
    for stage in ("baseline", "working"):
        files = paths if stage == "baseline" else [p.relative_to(ROOT).as_posix() for prefix in prefixes for p in (ROOT / prefix).rglob("*.cs")]
        sources = []
        for relative in files:
            target = args.output / stage / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            text = (subprocess.check_output(["git", "show", args.baseline + ":" + relative], cwd=ROOT).decode("utf-8-sig")
                    if stage == "baseline" else (ROOT / relative).read_text(encoding="utf-8-sig"))
            if stage == "working":
                source_digest.update(relative.encode() + b"\0" + text.encode() + b"\0")
            if relative.endswith("/Vehicles/Vehicle.cs"):
                text = "using PhysicsMaterial = UnityEngine.PhysicMaterial;\n" + text
            target.write_text(text)
            sources.append(target.resolve())
        arguments = ["-nologo", "-target:library", "-langversion:9", "-out:" + str((args.output / (stage + ".dll")).resolve())]
        arguments += ["-r:" + str(p.resolve()) for p in refs] + [str(p) for p in sources]
        response = args.output / (stage + ".rsp")
        response.write_text("\n".join('"' + argument + '"' for argument in arguments))
        run = subprocess.run([str(dotnet), str(csc), "@" + str(response.resolve())], capture_output=True, text=True)
        log = run.stdout + run.stderr
        (args.output / (stage + ".log")).write_text(log)
        results[stage] = {"source_files": len(files), "exit_code": run.returncode, "errors": diagnostics(log)}
    introduced = results["working"]["errors"] - results["baseline"]["errors"]
    tool_failures = [stage for stage in results if results[stage]["exit_code"] not in (0, 1) or
                     (results[stage]["exit_code"] != 0 and not results[stage]["errors"])]
    report = {"scope": "Real Roslyn C# 9 compilation diagnostic comparison against genuine UnityEngine 2021.3 references, classic input/Built-in only. NUnit source compiled, not executed. NOT a pinned Unity compile.",
              "baseline": args.baseline, "scratch_adaptation": "Vehicle.cs only: alias PhysicsMaterial to UnityEngine.PhysicMaterial in both copies.",
              "baseline_sources": results["baseline"]["source_files"], "working_sources": results["working"]["source_files"],
              "working_source_sha256": source_digest.hexdigest(), "compiler_process_failures": tool_failures,
              "baseline_errors": sum(results["baseline"]["errors"].values()), "working_errors": sum(results["working"]["errors"].values()),
              "introduced_errors": list(introduced.elements()), "unity_compile": "NOT RUN", "unity_editmode": "NOT RUN"}
    (args.output / "diagnostic_comparison.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return 1 if introduced or tool_failures else 0


if __name__ == "__main__":
    sys.exit(main())
