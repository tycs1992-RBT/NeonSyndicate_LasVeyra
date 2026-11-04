#!/usr/bin/env python3
"""Compile classic-input source against installed Unity 6 managed references, without launching Unity.

The editor row combines runtime, editor and tests into one assembly, so assembly boundaries and
native import remain unverified. The player row uses Linux player API references with Windows symbols.
Neither row executes Unity or tests, validates packages, or represents a Windows build.
"""
import argparse
import hashlib
import json
import subprocess
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument('--editor-data', type=Path, required=True)
    parser.add_argument('--template-assemblies', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root, editor, template, out = (p.resolve() for p in (args.root, args.editor_data, args.template_assemblies, args.output))
    out.mkdir(parents=True, exist_ok=True)
    bcl = editor / 'UnityReferenceAssemblies/unity-4.8-api'
    runtime = editor / 'NetCoreRuntime/dotnet'
    compiler = editor / 'DotNetSdkRoslyn/csc.dll'
    basic = sorted(bcl.glob('*.dll')) + sorted((bcl / 'Facades').glob('*.dll'))
    rows = []
    for kind in ('player', 'editor'):
        if kind == 'player':
            refs = basic + sorted((editor / 'PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed').glob('UnityEngine*.dll'))
            symbols = 'UNITY_STANDALONE_WIN,UNITY_64,ENABLE_LEGACY_INPUT_MANAGER'
            folders = ['Scripts/Runtime']
        else:
            refs = basic + sorted((editor / 'Managed/UnityEngine').glob('Unity*.dll'))
            refs += sorted(p for p in (editor / 'Managed').glob('Unity*.dll') if p.name not in ('UnityEngine.dll', 'UnityEditor.dll'))
            refs += [editor / 'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom/nunit.framework.dll',
                     template / 'UnityEngine.TestRunner.dll', template / 'UnityEditor.TestRunner.dll']
            symbols = 'UNITY_EDITOR,UNITY_EDITOR_WIN,UNITY_STANDALONE_WIN,UNITY_64,ENABLE_LEGACY_INPUT_MANAGER,UNITY_INCLUDE_TESTS'
            folders = ['Scripts/Runtime', 'Scripts/Editor', 'Tests/EditMode']
        required = [runtime, compiler, bcl / 'mscorlib.dll'] + refs
        if not refs or any(not item.is_file() for item in required) or not any(p.name == 'UnityEngine.CoreModule.dll' for p in refs):
            raise SystemExit('Unity managed compiler or required references are unavailable for ' + kind)
        sources = sorted(p for folder in folders for p in (root / 'Assets/NeonSyndicate' / folder).rglob('*.cs'))
        if not sources:
            raise SystemExit('No original game source found.')
        source_hash = hashlib.sha256()
        for source in sources:
            source_hash.update(source.relative_to(root).as_posix().encode() + b'\0' + source.read_bytes() + b'\0')
        options = ['-nologo', '-nostdlib', '-target:library', '-langversion:9', '-define:' + symbols, '-out:' + str(out / (kind + '.dll'))]
        options += ['-r:' + str(p) for p in refs] + [str(p) for p in sources]
        response = out / (kind + '.rsp')
        response.write_text('\n'.join('"' + option + '"' for option in options))
        run = subprocess.run([str(runtime), str(compiler), '@' + str(response)], capture_output=True, text=True)
        log = run.stdout + run.stderr
        (out / (kind + '.log')).write_text(log)
        errors = [line for line in log.splitlines() if ': error ' in line]
        warnings = [line for line in log.splitlines() if ': warning ' in line]
        row = {'row': kind, 'exit_code': run.returncode, 'sources': len(sources), 'source_sha256': source_hash.hexdigest(),
               'errors': len(errors), 'warnings': len(warnings), 'error_messages': errors}
        rows.append(row)
        print(kind, 'exit', run.returncode, 'sources', len(sources), 'errors', len(errors), 'warnings', len(warnings), flush=True)
        if errors:
            print('\n'.join(errors[:10]), flush=True)
    (out / 'report.json').write_text(json.dumps({'scope': __doc__, 'rows': rows}, indent=2) + '\n')
    return 0 if all(row['exit_code'] == 0 for row in rows) else 1


if __name__ == '__main__':
    raise SystemExit(main())
