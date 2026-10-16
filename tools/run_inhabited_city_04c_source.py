#!/usr/bin/env python3
"""Execute actual 04C ledger/binding/service logic and NUnit tests on .NET 8.

CityLayout uses the disclosed existing managed math adapter. No Unity view,
physics, JsonUtility, transition or gameplay execution is represented here.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "Assets/NeonSyndicate/Scripts/Runtime"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--nunit", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args(); dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))[-1]
    refs = sorted(dotnet.parent.glob("packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll")) + [args.nunit.resolve()]
    hashes = {}
    def read(path):
        hashes[path.relative_to(ROOT).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
        return path.read_text()
    with tempfile.TemporaryDirectory(prefix="04c-source-tests-") as directory:
        scratch = Path(directory); sources = []
        for path in sorted((RUNTIME / "World").glob("CityLayout*.cs")):
            source = read(path)
            if path.name == "CityLayout.Frost.cs":
                end = source.index("    public static class SnowMesher")
                source = source[:source.rfind("    /// <summary>", 0, end)] + "}\n"
            target = scratch / path.name; target.write_text(source); sources.append(target)
        source = read(RUNTIME / "Core/Util.cs")
        target = scratch / "UtilPure.cs"; target.write_text(source[:source.index("    public static class Log")] + "}\n"); sources.append(target)
        source = read(RUNTIME / "Core/Mats.cs"); start = source.index("    public enum FacadeStyle"); end = source.index("    /// <summary>", start)
        target = scratch / "Enums.cs"; target.write_text("namespace NeonSyndicate {\n" + source[start:end] + "}\n"); sources.append(target)
        paths = [RUNTIME / name for name in ["World/CityTypes.cs", "Gameplay/Quests/QuestJson.cs", "Gameplay/ServiceUseGate04C.cs",
            "Gameplay/InhabitedCity/InhabitedCityData04C.cs", "Gameplay/InhabitedCity/InhabitedCityBook.cs",
            "Gameplay/InhabitedCity/InhabitedCityBindings04C.cs", "Gameplay/InhabitedCity/QuietSignal04C.cs"]]
        paths += [ROOT / "Assets/NeonSyndicate/Tests/EditMode" / (name + ".cs") for name in ["InhabitedCityBook04CTests", "InhabitedCityBindings04CTests", "ServiceUseGate04CTests", "UnityMetadata04CTests"]]
        paths += [ROOT / name for name in ["tools/visual_rebirth_04a_harness/MathAdapter.cs", "tools/inhabited_city_04c_harness/SourceHost.cs", "tools/inhabited_city_04c_harness/SourceProgram.cs"]]
        for path in paths: read(path); sources.append(path)
        dll = scratch / "SourceTests.dll"; rsp = scratch / "compile.rsp"
        options = ["-nologo", "-target:exe", "-langversion:9", "-out:" + str(dll)] + ["-r:" + str(path) for path in refs] + [str(path) for path in sources]
        rsp.write_text("\n".join('"' + option + '"' for option in options))
        subprocess.run([str(dotnet), str(csc), "@" + str(rsp)], check=True)
        shutil.copyfile(args.nunit, scratch / args.nunit.name)
        dll.with_suffix(".runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {"tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": "8.0.0"}}}))
        run = subprocess.run([str(dotnet), str(dll)], cwd=ROOT, text=True, capture_output=True)
        if run.returncode not in (0, 1): raise RuntimeError(run.stderr)
        report = json.loads(run.stdout); report["source_sha256"] = hashes
        game_root = read(RUNTIME / "Core/GameRoot.cs")
        spawn = re.search(r"public static Vector3 SpawnOutside\(Poi poi\)\s*\{([^}]+)\}", game_root)
        if spawn is None or "poi.Position + f * 3.5f" not in spawn.group(1) or "poi.Facing.normalized : Vector3.forward" not in spawn.group(1):
            raise RuntimeError("GameRoot arrival contract changed; update and review the disclosed planar projection.")
        report["arrival_projection"] = {"source": "Assets/NeonSyndicate/Scripts/Runtime/Core/GameRoot.cs: SpawnOutside and HomeSpawn",
            "method": spawn.group(0), "scope": "The harness computes the exact source X/Z arrival formula. Native FindGround and actual player spawn are not executed."}
        report["catalog_sha256"] = hashlib.sha256((ROOT / "Assets/NeonSyndicate/Resources/InhabitedCity04C/venues.json").read_bytes()).hexdigest()
        report["extractions"] = ["CityLayout.Frost excludes only unrelated SnowMesher renderer.", "Util.cs exact GRand/MathUtil region before Log.", "Mats.cs exact FacadeStyle enum."]
        args.report.parent.mkdir(parents=True, exist_ok=True); args.report.write_text(json.dumps(report, indent=2) + "\n")
        print(json.dumps({"tests": len(report["tests"]), "failures": report["failures"], "failed_cases": [case["name"] + str(case["arguments"]) for case in report["tests"] if not case["passed"]],
            "seeds": [{"seed": row["seed"], "sourceSelected": row["sourceSelected"]} for row in report["seeds"]], "nativeUnity": "NOT RUN"}, indent=2))
        return run.returncode


if __name__ == "__main__": raise SystemExit(main())
