#!/usr/bin/env python3
"""Generate deterministic character mesh statistics from the actual compiled game, without rendering."""
import argparse,json,shutil,subprocess
from pathlib import Path


def main():
    a=argparse.ArgumentParser(description=__doc__)
    for name in ('dotnet','assembly','unity-references','output'): a.add_argument('--'+name,type=Path,required=True)
    x=a.parse_args(); x.unity_references=x.unity_references.resolve(); out=x.output.resolve();out.mkdir(parents=True,exist_ok=True)
    sdk=x.dotnet.parent; csc=sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    code='''using System; using System.Collections.Generic; using System.Text.Json; using NeonSyndicate;
public static class Probe { public static void Main() {
List<object> rows=new List<object>();
for(int i=0;i<7;i++) { Appearance a=i<2?Appearance.Malik():i==3?Appearance.Swat(new Random(100)):i==5?Appearance.Random(new Random(123)):Appearance.Police(new Random(100));
if(i==4){a.Jacket=true;a.ShellStyle=1;} if(i==5){a.SuitStyle=3;a.GauntletStyle=3;a.ShellStyle=1;}
int detail=i==0||i==3?2:i==6?0:1; RigLayout rig=RigLayout.From(a); BodyData d=BodyBuilder.Build(a,rig,detail);
rows.Add(new {actor=new[]{"Malik hero","Malik standard","Blade Warden","Heavy Sentinel","Focus Weaver","Rift duelist","Warden crowd"}[i],detail,vertices=d.Vertices.Count,triangles=d.Triangles.Count/3,weights=d.Weights.Count});
} Console.WriteLine(JsonSerializer.Serialize(new {scope="Actual generated mesh data; no Unity rendering, draw-call or performance measurement",actors=rows})); }}'''
    src=out/'GeometryProbe.cs';src.write_text(code);dll=out/'GeometryProbe.dll'
    refs=list(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+list(x.unity_references.glob('*.dll'))+[x.assembly.resolve()]
    rsp=out/'probe.rsp';rsp.write_text('\n'.join('"'+v+'"' for v in ['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(src)]))
    subprocess.run([str(x.dotnet),str(csc),'@'+str(rsp)],check=True)
    for p in refs:
        if p.parent==x.unity_references or p==x.assembly.resolve():shutil.copy2(p,out/p.name)
    dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
    run=subprocess.run([str(x.dotnet),str(dll)],capture_output=True,text=True,check=True)
    report=json.loads(run.stdout);(out/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print(run.stdout)
if __name__=='__main__':main()
