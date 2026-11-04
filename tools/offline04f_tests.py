#!/usr/bin/env python3
"""Execute the existing NUnit suite on .NET, with genuine Unity managed modules.

NeedsEngine tests are excluded. Other tests that reach native Unity calls can
still fail; retain the XML and compare baseline/current rather than hiding them.
This is not Unity Test Runner, native scene execution, rendering or playtesting.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET


def isolate_previous_results(output):
    """Keep previous evidence recoverable without letting a killed runner reuse its old XML."""
    previous = [output / name for name in ("nunit-results.xml", "test-report.json", "nunit.log") if (output / name).exists()]
    if previous:
        archive = output / "previous-results" / uuid.uuid4().hex
        archive.mkdir(parents=True)
        for path in previous:
            shutil.move(str(path), str(archive / path.name))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, required=True)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--unity-references", type=Path, required=True)
    parser.add_argument("--nunit", type=Path, required=True)
    parser.add_argument("--nunitlite", type=Path, required=True)
    parser.add_argument("--assembly", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--where", default="cat != NeedsEngine")
    parser.add_argument("--baseline-report", type=Path, help="Compare current failed test identities with an executed baseline report")
    args = parser.parse_args()
    root = args.root.resolve()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    isolate_previous_results(output)
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))[-1]
    refs = sorted(dotnet.parent.glob("packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll"))
    runner = output / "OfflineNUnit.cs"
    runner.write_text('using System.Reflection; using NUnitLite; public static class OfflineNUnit { public static int Main(string[] args) { return new AutoRun(Assembly.LoadFrom(@"' + str(args.assembly.resolve()).replace('"', '""') + '")).Execute(args); } }')
    exe = output / "OfflineNUnit.dll"
    options = ["-nologo", "-target:exe", "-langversion:9", "-out:" + str(exe)]
    options += ["-r:" + str(p) for p in refs + [args.nunit.resolve(), args.nunitlite.resolve()]] + [str(runner)]
    rsp = output / "runner.rsp"
    rsp.write_text("\n".join('"' + option + '"' for option in options))
    subprocess.run([str(dotnet), str(csc), "@" + str(rsp)], check=True)
    for path in list(args.unity_references.resolve().glob("*.dll")) + [args.nunit.resolve(), args.nunitlite.resolve(), args.assembly.resolve()]:
        target = output / path.name
        if path != target:
            shutil.copyfile(path, target)
    exe.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {"tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": "8.0.0"}}}))
    xml = output / "nunit-results.xml"
    command = [str(dotnet), str(exe), "--workers=0", "--noheader", "--where=" + args.where, "--result=" + str(xml), "--work=" + str(root)]
    run = subprocess.run(command, cwd=root, capture_output=True, text=True)
    (output / "nunit.log").write_text(run.stdout + run.stderr)
    if not xml.is_file():
        (output / "test-report.json").write_text(json.dumps({"scope": __doc__.strip(), "status": "BLOCKED_NO_NEW_XML", "exit_code": run.returncode, "counts": None, "command": command, "native_unity": "NOT RUN"}, indent=2) + "\n")
        print(run.stdout + run.stderr)
        raise SystemExit("NUnit produced no XML. First failure must be inspected in nunit.log.")
    tree = ET.parse(xml).getroot()
    failed = []
    for case in tree.iter("test-case"):
        if case.get("result") != "Failed":
            continue
        failed.append({"name": case.get("fullname"), "message": case.findtext("failure/message", default=""),
                       "stack_trace": case.findtext("failure/stack-trace", default="")})
    report = {"scope": __doc__.strip(), "invocation": [sys.executable] + sys.argv, "command": command, "exit_code": run.returncode, "counts": tree.attrib,
              "excluded_by_filter": int(tree.get("testcasecount", "0")) - int(tree.get("total", "0")),
              "failures": failed, "native_unity": "NOT RUN"}
    introduced = failed
    if args.baseline_report:
        baseline = json.loads(args.baseline_report.read_text())
        old = {row["name"] for row in baseline["failures"]}
        old_reasons = {row["name"]: row["message"] for row in baseline["failures"]}
        current = {row["name"] for row in failed}
        changed = [row["name"] for row in failed if row["name"] in old and row["message"] != old_reasons[row["name"]]]
        introduced = [row for row in failed if row["name"] not in old or row["name"] in changed]
        report["comparison"] = {"baseline_report": str(args.baseline_report.resolve()),
                                "introduced_failure_names": sorted(current - old), "changed_failure_reasons": sorted(changed),
                                "resolved_failure_names": sorted(old - current)}
    report["comparison_exit_code"] = 0 if not introduced else 1
    (output / "test-report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps({"counts": tree.attrib, "failed_cases": [row["name"] for row in failed], "native_unity": "NOT RUN"}, indent=2))
    return 0 if not introduced else 1


if __name__ == "__main__":
    raise SystemExit(main())
