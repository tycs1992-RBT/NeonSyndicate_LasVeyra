#!/usr/bin/env python3
"""Build official extracted Input System package sources for offline API checks.

UI support is omitted because this project does not install the optional UGUI
package. The independent InputForUI assembly is excluded just as its asmdef
requires. Nothing is downloaded, patched or installed into the Unity project.
"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--unity-references", type=Path, required=True)
    parser.add_argument("--package", type=Path, required=True, help="Extracted com.unity.inputsystem package directory")
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    package = args.package.resolve()
    metadata = json.loads((package / "package.json").read_text())
    if metadata["name"] != "com.unity.inputsystem" or metadata["version"] != "1.17.0":
        raise SystemExit("This project is pinned to official com.unity.inputsystem 1.17.0.")
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))[-1]
    refs = sorted(dotnet.parent.glob("packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll"))
    refs += sorted(args.unity_references.resolve().glob("*.dll"))
    files = sorted(p for p in (package / "InputSystem").rglob("*.cs") if "Plugins/InputForUI" not in p.as_posix())
    symbols = ["UNITY_64", "UNITY_STANDALONE_WIN", "ENABLE_INPUT_SYSTEM", "UNITY_5_3_OR_NEWER",
               "UNITY_2021_3_OR_NEWER", "UNITY_2022_1_OR_NEWER", "UNITY_2022_2_OR_NEWER", "UNITY_2022_3_OR_NEWER",
               "UNITY_2023_1_OR_NEWER", "UNITY_2023_2_OR_NEWER", "UNITY_2023_3_OR_NEWER", "UNITY_6000_0_OR_NEWER",
               "UNITY_6000_1_OR_NEWER", "UNITY_6000_2_OR_NEWER", "UNITY_6000_3_OR_NEWER",
               "UNITY_INPUT_SYSTEM_PLATFORM_SCROLL_DELTA", "UNITY_INPUT_SYSTEM_INPUT_MODULE_SCROLL_DELTA",
               "UNITY_INPUT_SYSTEM_INPUT_MODULE_NAVIGATION_DEVICE_TYPE", "UNITY_INPUT_SYSTEM_SENDPOINTERHOVERTOPARENT",
               "UNITY_INPUT_SYSTEM_PLATFORM_POLLING_FREQUENCY", "UNITY_INPUT_SYSTEM_ENABLE_XR",
               "UNITY_INPUT_SYSTEM_ENABLE_PHYSICS", "UNITY_INPUT_SYSTEM_ENABLE_PHYSICS2D"]
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    options = ["-nologo", "-target:library", "-unsafe", "-langversion:9", "-define:" + ",".join(symbols),
               "-out:" + str(output / "Unity.InputSystem.dll")]
    options += ["-r:" + str(p) for p in refs] + [str(p) for p in files]
    rsp = output / "input.rsp"
    rsp.write_text("\n".join('"' + option + '"' for option in options))
    run = subprocess.run([str(dotnet), str(csc), "@" + str(rsp)], capture_output=True, text=True)
    (output / "input.log").write_text(run.stdout + run.stderr)
    digest = hashlib.sha256()
    for path in files:
        digest.update(path.relative_to(package).as_posix().encode() + b"\0" + path.read_bytes() + b"\0")
    report = {"scope": __doc__.strip(), "source_files": len(files), "source_sha256": digest.hexdigest(),
              "package": {"name": metadata["name"], "version": metadata["version"]}, "symbols": symbols,
              "exit_code": run.returncode, "native_input": "NOT RUN"}
    (output / "input-report.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report, indent=2))
    return run.returncode


if __name__ == "__main__":
    raise SystemExit(main())
