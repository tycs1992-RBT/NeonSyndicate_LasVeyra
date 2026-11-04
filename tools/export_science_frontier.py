"""Export the actual compiled district and frontier meshes. No Unity rendering or physics."""
import argparse
import os
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--editor-data',type=Path,required=True)
p.add_argument('--existing-compile',type=Path,required=True)
p.add_argument('--output',type=Path,required=True)
p.add_argument('--circuit',action='store_true',help='Also export a bounded route and textured mesh survey for the circuit review.')
a=p.parse_args()
e,c,o=(v.resolve() for v in (a.editor_data,a.existing_compile,a.output));o.mkdir(parents=True,exist_ok=True)
b=e/'UnityReferenceAssemblies/unity-4.8-api'
m=e/'PlaybackEngines/LinuxStandaloneSupport/Variations/mono/Managed'
n=e/'Resources/PackageManager/BuiltInPackages/com.unity.ext.nunit/net40/unity-custom'
refs=list(b.glob('*.dll'))+list((b/'Facades').glob('*.dll'))+list(m.glob('UnityEngine*.dll'))+[c/'editor.dll',n/'nunit.framework.dll']
options=['-nologo','-nostdlib','-target:exe','-out:'+str(o/'Export.exe')]+['-r:'+str(v) for v in refs]+[str(ROOT/'tools/export_science_district.cs')]
if a.circuit: options.append(str(ROOT/'tools/export_science_circuit_context.cs'))
(o/'export.rsp').write_text('\n'.join('"'+v+'"' for v in options))
subprocess.run([str(e/'NetCoreRuntime/dotnet'),str(e/'DotNetSdkRoslyn/csc.dll'),'@'+str(o/'export.rsp')],cwd=ROOT,check=True)
env=os.environ.copy();env['MONO_PATH']=os.pathsep.join(map(str,[m,n,e/'Managed',e/'Managed/UnityEngine',c]))
run=subprocess.run([str(e/'MonoBleedingEdge/bin/mono'),str(o/'Export.exe'),str(o)],cwd=ROOT,env=env,text=True,capture_output=True)
(o/'mesh-counts.txt').write_text(run.stdout+run.stderr);print(run.stdout+run.stderr);run.check_returncode()
