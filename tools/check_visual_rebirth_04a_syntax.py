#!/usr/bin/env python3
"""Run real Roslyn syntax parsing across the twelve required preprocessor setups.

No Unity/API references are needed for syntax parsing. This checks active C# 9
syntax only; type resolution, compilation, native execution and platform support
remain separate requirements. Requires a .NET 8 SDK with Roslyn assemblies.
"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
PROGRAM = r'''
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class Program
{
    public static int Main(string[] args)
    {
        string root = args[0];
        string[] runtime = Directory.GetFiles(Path.Combine(root, "Assets/NeonSyndicate/Scripts/Runtime"), "*.cs", SearchOption.AllDirectories);
        string[] editor = Directory.GetFiles(Path.Combine(root, "Assets/NeonSyndicate/Scripts/Editor"), "*.cs", SearchOption.AllDirectories);
        string[] tests = Directory.GetFiles(Path.Combine(root, "Assets/NeonSyndicate/Tests/EditMode"), "*.cs", SearchOption.AllDirectories);
        List<object> configurations = new List<object>();
        int errors = 0;
        foreach (bool isEditor in new[] { false, true })
        foreach (string input in new[] { "classic", "both", "input-system-only" })
        foreach (bool urp in new[] { false, true })
        {
            List<string> symbols = new List<string> { "UNITY_STANDALONE_WIN", "NS_INPUT_SYSTEM" };
            if (isEditor) symbols.AddRange(new[] { "UNITY_EDITOR", "UNITY_EDITOR_WIN", "UNITY_INCLUDE_TESTS" });
            if (input != "input-system-only") symbols.Add("ENABLE_LEGACY_INPUT_MANAGER");
            if (input != "classic") symbols.Add("ENABLE_INPUT_SYSTEM");
            if (urp) symbols.Add("NS_URP");
            string[] paths = isEditor ? runtime.Concat(editor).Concat(tests).OrderBy(p => p).ToArray() : runtime;
            CSharpParseOptions options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: symbols);
            List<string> diagnostics = new List<string>();
            foreach (string path in paths)
            {
                SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path.Substring(root.Length + 1));
                foreach (Diagnostic diagnostic in tree.GetDiagnostics())
                    if (diagnostic.Severity == DiagnosticSeverity.Error) diagnostics.Add(diagnostic.ToString());
            }
            errors += diagnostics.Count;
            configurations.Add(new { target = isEditor ? "editor-windows" : "player-windows", input,
                pipeline = urp ? "URP" : "Built-in", files = paths.Length, symbols, errors = diagnostics });
        }
        Console.Write(JsonSerializer.Serialize(new { scope = "Real Roslyn C# 9 syntax parsing only. No type resolution, Unity import, compilation or execution.",
            configurations = configurations, syntaxErrors = errors }, new JsonSerializerOptions { WriteIndented = true }));
        return errors == 0 ? 0 : 1;
    }
}
'''


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    dotnet = args.dotnet.resolve()
    csc = sorted(dotnet.parent.glob("sdk/*/Roslyn/bincore/csc.dll"))[-1]
    refs = sorted(dotnet.parent.glob("packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll"))
    roslyn = [csc.parent / name for name in ("Microsoft.CodeAnalysis.dll", "Microsoft.CodeAnalysis.CSharp.dll")]
    with tempfile.TemporaryDirectory(prefix="04a-roslyn-syntax-") as directory:
        scratch = Path(directory)
        source, assembly = scratch / "Program.cs", scratch / "Syntax.dll"
        source.write_text(PROGRAM)
        response = scratch / "compile.rsp"
        arguments = ["-nologo", "-target:exe", "-langversion:9", "-out:" + str(assembly)]
        arguments += ["-r:" + str(path) for path in refs + roslyn] + [str(source)]
        response.write_text("\n".join('"' + argument + '"' for argument in arguments))
        subprocess.run([str(dotnet), str(csc), "@" + str(response)], check=True)
        for path in roslyn:
            shutil.copyfile(path, scratch / path.name)
        (scratch / "Syntax.runtimeconfig.json").write_text(json.dumps({"runtimeOptions": {
            "tfm": "net8.0", "framework": {"name": "Microsoft.NETCore.App", "version": "8.0.0"}}}))
        run = subprocess.run([str(dotnet), str(assembly), str(ROOT)], capture_output=True, text=True)
        if run.returncode not in (0, 1):
            raise RuntimeError(run.stderr)
        report = json.loads(run.stdout)
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2) + "\n")
        print(json.dumps({"configurations": len(report["configurations"]), "syntaxErrors": report["syntaxErrors"], "scope": report["scope"]}, indent=2))
        return run.returncode


if __name__ == "__main__":
    raise SystemExit(main())
