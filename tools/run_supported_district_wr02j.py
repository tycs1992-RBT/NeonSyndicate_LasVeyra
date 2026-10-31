"""Run twelve pure NUnit cases against the actual compiled district geometry, outside the Unity Editor.

Requires the official Editor's managed compiler/runtime and a successful check_unity_classic_wr02g.py
combined editor reference compilation. Does not execute native transaction tests or simulate physics.
"""
import argparse
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--editor-data', type=Path, required=True)
    parser.add_argument('--existing-compile', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    editor, compiled, out = (p.resolve() for p in (args.editor_data, args.existing_compile, args.output))
    out.mkdir(parents=True, exist_ok=True)
    bcl = editor / 'UnityReferenceAssemblies/unity-4.8-api'
    modules = editor / 'PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed'
    nunit = editor / 'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom'
    exe = out / 'supported-district.exe'
    refs = sorted(bcl.glob('*.dll')) + sorted((bcl / 'Facades').glob('*.dll'))
    response = out / 'harness.rsp'
    response.write_text('\n'.join('"' + line + '"' for line in
        ['-nologo', '-nostdlib', '-target:exe', '-langversion:9', '-out:' + str(exe)] +
        ['-r:' + str(p) for p in refs] + [str(ROOT / 'tools/supported_district_nunit_wr02j_harness.cs')]))
    subprocess.run([str(editor / 'NetCoreRuntime/dotnet'), str(editor / 'DotNetSdkRoslyn/csc.dll'),
                    '@' + str(response)], check=True, cwd=ROOT)
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join(map(str, [modules, nunit, editor / 'Managed', editor / 'Managed/UnityEngine', compiled]))
    run = subprocess.run([str(editor / 'MonoBleedingEdge/bin/mono'), str(exe), str(compiled / 'editor.dll')],
                         cwd=ROOT, env=env, text=True, capture_output=True)
    (out / 'pure-tests.log').write_text(run.stdout + run.stderr)
    print(run.stdout + run.stderr, end='')
    return run.returncode


if __name__ == '__main__':
    raise SystemExit(main())
