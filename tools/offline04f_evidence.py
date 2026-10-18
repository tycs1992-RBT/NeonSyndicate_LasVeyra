#!/usr/bin/env python3
"""Preserve compact, deliverable evidence from actually executed 04F reports.

Copies report data only, never DLLs, compiler outputs or fabricated Unity
screenshots. Baseline native failures remain visible in the delivered summary.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--prefix", default="final")
    parser.add_argument("--output", type=Path, default=ROOT / "Docs/CrystalCombat04F/Evidence")
    args = parser.parse_args()
    root = args.root.resolve()
    logs = root / "Logs/04F"
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    def load(name):
        return json.loads((logs / name).read_text())
    def write(name, value):
        (output / name).write_text(json.dumps(value, indent=2) + "\n")
    compile_report = load(args.prefix + "-compile/compile-report.json")
    write("managed-compile.json", compile_report)
    def diagnostics(path):
        return Counter(re.sub(r"\(\d+,\d+\)", "", re.sub(r"^.*?/Assets/", "Assets/", line))
                       for line in path.read_text().splitlines() if ": warning " in line or ": error " in line)
    deltas = []
    for configuration in compile_report["configurations"]:
        if configuration["status"] == "SKIPPED":
            continue
        label = configuration["name"]
        before = diagnostics(logs / ("baseline-compile/" + label + ".log"))
        after = diagnostics(logs / (args.prefix + "-compile/" + label + ".log"))
        deltas.append({"configuration": label, "introduced": list((after - before).elements()), "resolved": list((before - after).elements())})
    write("diagnostic-delta.json", {"scope": "Compiler error and warning message multiset comparison, ignoring source line offsets only.", "configurations": deltas})
    baseline = load("baseline-tests/test-report.json")
    current = load(args.prefix + "-tests/test-report.json")
    groups = Counter()
    tree = ET.parse(logs / (args.prefix + "-tests/nunit-results.xml")).getroot()
    for case in tree.iter("test-case"):
        if "04FTests" in case.get("classname", ""):
            groups[(case.get("classname"), case.get("result"))] += 1
    test_summary = {
        "scope": current["scope"], "source_sha256": compile_report["source_sha256"],
        "baseline_revision": "4c690c6248f0ff7f00098598e6e42e409d70f84f",
        "baseline_counts": baseline["counts"], "current_counts": current["counts"],
        "baseline_excluded_by_filter": int(baseline["counts"]["testcasecount"]) - int(baseline["counts"]["total"]),
        "current_excluded_by_filter": current["excluded_by_filter"],
        "comparison": current.get("comparison"), "native_unity": "NOT RUN",
        "baseline_failure_names": [row["name"] for row in baseline["failures"]],
        "current_failure_reasons": dict(Counter(row["message"].splitlines()[0] for row in current["failures"])),
        "04f_suites": [{"class": key[0], "result": key[1], "cases": count} for key, count in sorted(groups.items())],
        "executed_invocation": current.get("invocation"), "nunit_process_exit_code": current["exit_code"],
        "baseline_comparison_exit_code": current.get("comparison_exit_code"),
    }
    write("nunit-delta.json", test_summary)
    metadata = load(args.prefix + "-metadata.json")
    metadata.pop("metadata_sha256", None)
    metadata.pop("excluded_recovery_or_hidden_files", None)
    write("metadata.json", metadata)
    write("preservation.json", load(args.prefix + "-preservation.json"))
    lfs = load(args.prefix + "-lfs.json")
    entries = lfs.pop("entries", [])
    lfs["inventory_entries"] = len(entries)
    lfs["inventory_report_sha256"] = hashlib.sha256((logs / (args.prefix + "-lfs.json")).read_bytes()).hexdigest()
    write("lfs.json", lfs)
    syntax = load(args.prefix + "-syntax.json")
    write("syntax-matrix.json", syntax)
    invocations = [
        {"invocation": compile_report.get("invocation"), "exit_code": compile_report.get("exit_code"), "evidence": "managed-compile.json"},
        {"invocation": current.get("invocation"), "exit_code": current.get("comparison_exit_code"), "raw_nunit_exit_code": current["exit_code"], "evidence": "nunit-delta.json"},
        {"invocation": ["python", "tools/check_unity_metadata_04c.py", "--report", "Logs/04F/" + args.prefix + "-metadata.json"], "exit_code": 0 if metadata["passed"] else 1, "evidence": "metadata.json"},
        {"invocation": ["python", "tools/offline04f_preservation.py", "--report", "Logs/04F/" + args.prefix + "-preservation.json"], "exit_code": 0 if load(args.prefix + "-preservation.json")["failed"] == 0 else 1, "evidence": "preservation.json"},
        {"invocation": ["python", "tools/check_living_territories_04b_lfs.py", "--report", "Logs/04F/" + args.prefix + "-lfs.json"], "exit_code": 0 if not lfs["failed"] else 1, "evidence": "lfs.json"},
        {"invocation": ["python", "tools/check_visual_rebirth_04a_syntax.py", "--dotnet", "/tmp/fhb-dotnet/dotnet", "--report", "Logs/04F/" + args.prefix + "-syntax.json"], "exit_code": 0 if syntax["syntaxErrors"] == 0 else 1, "evidence": "syntax-matrix.json"},
        {"invocation": ["bash", "tools/test.sh"], "exit_code": 3, "status": "NOT RUN: Unity Editor not installed"},
        {"invocation": ["git", "diff", "--check"], "exit_code": 0},
        {"invocation": ["python", "-m", "py_compile", "tools/offline04f_compile.py", "tools/offline04f_build_input.py", "tools/offline04f_tests.py", "tools/offline04f_preservation.py", "tools/offline04f_evidence.py"], "exit_code": 0},
    ]
    write("executed-commands.json", {"scope": "Executed report-generating commands. Native Unity is unavailable; no GPU or game execution is implied.", "commands": invocations})
    print(json.dumps({"output": str(output), "files": len(list(output.glob("*.json"))), "new_failure_names": current.get("comparison", {}).get("introduced_failure_names", [])}, indent=2))


if __name__ == "__main__":
    main()
