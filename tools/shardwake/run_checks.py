"""Export the two posed actors and execute the Vale adventure/profile fixture against a compiled Unity-reference assembly."""
import argparse
import os
import subprocess
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
parser = argparse.ArgumentParser(description=__doc__)
for key in ['editor-data','existing-compile','output']:
    parser.add_argument('--'+key, type=Path, required=True)
parser.add_argument('--tests', action='store_true')
args = parser.parse_args()
ed, compiled, out = (p.resolve() for p in (args.editor_data, args.existing_compile, args.output))
out.mkdir(parents=True, exist_ok=True)
bcl = ed/'UnityReferenceAssemblies/unity-4.8-api'
managed = ed/'PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed'
nunit = ed/'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom'
refs = sorted(bcl.glob('*.dll')) + sorted((bcl/'Facades').glob('*.dll')) + sorted(managed.glob('UnityEngine*.dll'))
refs += [compiled/'editor.dll', nunit/'nunit.framework.dll']
response = out/'export.rsp'
exe = out/'ValeExport.exe'
opts = ['-nologo','-nostdlib','-target:exe','-out:'+str(exe)] + ['-r:'+str(p) for p in refs] + [str(ROOT/'tools/shardwake/export_pilot.cs')]
response.write_text('\n'.join('"'+o+'"' for o in opts))
subprocess.run([ed/'NetCoreRuntime/dotnet', ed/'DotNetSdkRoslyn/csc.dll', '@'+str(response)], cwd=ROOT, check=True)
env = os.environ.copy()
env['MONO_PATH'] = os.pathsep.join(map(str, [managed, nunit, ed/'Managed', ed/'Managed/UnityEngine', compiled]))
result = subprocess.run([ed/'MonoBleedingEdge/bin/mono', exe, out]+(['tests'] if args.tests else []), cwd=ROOT, env=env, capture_output=True, text=True)
(out/'pilot-checks.txt').write_text(result.stdout+result.stderr)
print(result.stdout+result.stderr)
result.check_returncode()
