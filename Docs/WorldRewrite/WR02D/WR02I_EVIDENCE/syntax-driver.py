import sys,subprocess,json,shutil
from pathlib import Path
sys.path.insert(0,'tools');from check_visual_rebirth_04a_syntax import PROGRAM
editor=Path('/opt/unity-6000.3.16f1/Editor/Data');out=Path('/tmp/wr02i/syntax');out.mkdir(parents=True,exist_ok=True)
source=out/'Syntax.cs';source.write_text(PROGRAM)
refs=list((editor/'NetCoreRuntime/shared/Microsoft.NETCore.App/6.0.21').glob('*.dll'))
for name in ['Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll']:
 shutil.copyfile(editor/'DotNetSdkRoslyn'/name,out/name);refs.append(out/name)
(out/'Syntax.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net6.0','framework':{'name':'Microsoft.NETCore.App','version':'6.0.21'}}}))
rsp=out/'compile.rsp';rsp.write_text('\n'.join(['-nologo','-target:exe','-langversion:9','-out:'+str(out/'Syntax.dll')]+['-r:'+str(p) for p in refs]+[str(source)]))
subprocess.run([str(editor/'NetCoreRuntime/dotnet'),str(editor/'DotNetSdkRoslyn/csc.dll'),'@'+str(rsp)],check=True)
r=subprocess.run([str(editor/'NetCoreRuntime/dotnet'),str(out/'Syntax.dll'),str(Path.cwd())],capture_output=True,text=True)
(out/'report.json').write_text(r.stdout);print('syntax exit',r.returncode, 'errors',json.loads(r.stdout)['syntaxErrors'],'configurations',len(json.loads(r.stdout)['configurations']));print(r.stderr);raise SystemExit(r.returncode)
