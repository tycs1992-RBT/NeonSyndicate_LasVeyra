"""Compile and exercise the actual Forge recipe using installed Unity managed references, without starting Unity.

Linux helper. Native Editor import, EditMode/PlayMode and visual testing remain separate.
The independent Python support check needs numpy and runs over the exported actual recipe.
"""
import argparse
import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--editor-data', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--existing-compile', type=Path)
    args = parser.parse_args()
    editor, output = args.editor_data.resolve(), args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    compiled = args.existing_compile.resolve() if args.existing_compile else output / 'compile'
    if not args.existing_compile:
        templates = list((editor / 'Resources/PackageManager/ProjectTemplates/libcache').glob('com.unity.template.3d-cross-platform-*/ScriptAssemblies'))
        if not templates:
            raise RuntimeError('Installed template TestRunner references missing.')
        subprocess.run(['python3', str(ROOT / 'tools/check_unity_classic_wr02g.py'), '--editor-data', str(editor),
                        '--template-assemblies', str(templates[-1]), '--output', str(compiled)], cwd=ROOT, check=True)
    bcl = editor / 'UnityReferenceAssemblies/unity-4.8-api'
    modules = editor / 'PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed'
    nunit = editor / 'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom'
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join(map(str, [modules, nunit, editor / 'Managed', editor / 'Managed/UnityEngine', compiled]))
    for name, assembly, arguments in [('forge_court_wr02i_harness', 'player', [str(ROOT), str(output / 'art')]),
                                      ('forge_court_nunit_wr02i_harness', 'editor', [str(compiled / 'editor.dll')])]:
        exe = output / (name + '.exe')
        refs = sorted(bcl.glob('*.dll')) + sorted((bcl / 'Facades').glob('*.dll')) + sorted(modules.glob('UnityEngine*.dll'))
        response = output / (name + '.rsp')
        lines = ['-nologo', '-nostdlib', '-target:exe', '-langversion:9', '-out:' + str(exe), '-r:' + str(compiled / (assembly + '.dll'))]
        lines += ['-r:' + str(p) for p in refs] + [str(ROOT / ('tools/' + name + '.cs'))]
        response.write_text('\n'.join('"' + line + '"' for line in lines))
        subprocess.run([str(editor / 'NetCoreRuntime/dotnet'), str(editor / 'DotNetSdkRoslyn/csc.dll'), '@' + str(response)], check=True, cwd=ROOT)
        run = subprocess.run([str(editor / 'MonoBleedingEdge/bin/mono'), str(exe)] + arguments, cwd=ROOT, env=env, text=True, capture_output=True)
        (output / (name + '.log')).write_text(run.stdout + run.stderr)
        print(run.stdout + run.stderr, end='')
        run.check_returncode()
    subprocess.run(['python3', str(ROOT / 'tools/verify_forge_court_support_wr02i.py'), str(output / 'art'),
                    '--report', str(output / 'support.json')], cwd=ROOT, check=True)


if __name__ == '__main__':
    main()
