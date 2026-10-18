#!/usr/bin/env python3
"""Generate deterministic character, companion, fusion and arrival venue mesh statistics from the actual compiled game, without rendering."""
import argparse,json,shutil,subprocess
from pathlib import Path


def main():
    a=argparse.ArgumentParser(description=__doc__)
    for name in ('dotnet','assembly','unity-references','output'): a.add_argument('--'+name,type=Path,required=True)
    x=a.parse_args(); x.unity_references=x.unity_references.resolve(); out=x.output.resolve();out.mkdir(parents=True,exist_ok=True)
    sdk=x.dotnet.parent; csc=sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    code='''using System; using System.Collections.Generic; using System.Text.Json; using System.Reflection; using NeonSyndicate; using UnityEngine;
public static class Probe { public static void Main() {
List<object> actors=new List<object>();
for(int i=0;i<7;i++) {
Appearance a=i<2?Appearance.Malik():i==3?Appearance.Swat(new System.Random(100)):i==5?Appearance.Random(new System.Random(123)):Appearance.Police(new System.Random(100));
if(i==4){a.Jacket=true;a.ShellStyle=1;} if(i==5){a.SuitStyle=3;a.GauntletStyle=3;a.ShellStyle=1;}
int detail=i==0||i==3?2:i==6?0:1; RigLayout rig=RigLayout.From(a); BodyData d=BodyBuilder.Build(a,rig,detail);
actors.Add(new {actor=new[]{"Malik hero","Malik standard","Blade Warden","Heavy Sentinel","Focus Weaver","Rift duelist","Warden crowd"}[i],detail,vertices=d.Vertices.Count,triangles=d.Triangles.Count/3,weights=d.Weights.Count});}
List<object> creatures=new List<object>();
foreach(string id in BondIds.Archetypes) for(int evo=0;evo<=2;evo++) {
CreatureBody b=CompanionBodyFactory.BuildNow(id,61724,evo,"",1);
creatures.Add(new {archetype=id,evolution=evo,detail=1,vertices=b.Mesh.Vertices.Count,triangles=b.Mesh.Triangles.Count/3,bones=b.Skeleton.Count});}
List<object> venues=new List<object>();
Type t=typeof(BodyBuilder).Assembly.GetType("NeonSyndicate.CityAscensionGeometry04I"); MethodInfo method=t!=null?t.GetMethod("Venue"):null;
if(method!=null) foreach(string id in new[]{"lofts","dispatch","pressure","bondkeeper"})
for(int preset=0;preset<4;preset++) for(int lod=0;lod<2;lod++) {
MeshBuilder a=new MeshBuilder(1),b=new MeshBuilder(1),c=new MeshBuilder(1);
method.Invoke(null,new object[]{id,a,b,c,lod==0,(GraphicsPreset)preset});
float minY=float.MaxValue,maxY=float.MinValue;
foreach(MeshBuilder mesh in new[]{a,b,c}) foreach(Vector3 v in mesh.Vertices){minY=Mathf.Min(minY,v.y);maxY=Mathf.Max(maxY,v.y);}
venues.Add(new {id,preset=((GraphicsPreset)preset).ToString(),lod=lod==0?"near":"far",vertices=a.VertexCount+b.VertexCount+c.VertexCount,minY,maxY});}
List<object> fusion=new List<object>();
foreach(string form in new[]{FusionIds.Ventwing,FusionIds.Kilnback,FusionIds.Coilrunner})
foreach(string cls in new[]{FusionIds.Sport,FusionIds.Cruiser,FusionIds.Skiff}) for(int low=0;low<2;low++){
FormBlueprint bp=FusionFormGeometry.Build(form,cls,low==1);fusion.Add(new {form,chassis=cls,low=low==1,vertices=bp.VertexCount,parts=bp.Parts.Count});}
List<object> services=new List<object>();
MethodInfo costume=typeof(CastCostumeBuilder04E).GetMethod("ServiceTorso04I",BindingFlags.NonPublic|BindingFlags.Static);
foreach(HeldTool tool in new[]{HeldTool.Tablet,HeldTool.Wrench}) {MeshBuilder b=new MeshBuilder(3);if(costume!=null)costume.Invoke(null,new object[]{b,tool,1f});services.Add(new {tool=tool.ToString(),addedVertices=b.VertexCount});}
Console.WriteLine(JsonSerializer.Serialize(new {scope="Actual source mesh builders on genuine managed modules; no Unity rendering or frame-time measurement",actors,creatures,venues,fusion,services})); }}'''
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
