#!/usr/bin/env python3
"""Export actual compiled 04N meshes as OBJ and measure topology. This is NOT Unity rendering or visual/performance acceptance."""
import argparse,json,shutil,subprocess
from pathlib import Path
CODE=r'''
using System;using System.Reflection;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text;using System.Text.Json;using System.Security.Cryptography;using NeonSyndicate;using UnityEngine;
public static class Probe {
static object Write(string name,List<Vector3> vertices,List<int> indices,int skinBones=0,int shapes=0){
var text=new StringBuilder();float minY=float.MaxValue,maxY=float.MinValue;
foreach(var v in vertices){if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z))throw new Exception("Nonfinite geometry");text.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}\n",v.x,v.y,v.z);minY=Math.Min(minY,v.y);maxY=Math.Max(maxY,v.y);}
for(int i=0;i<indices.Count;i+=3){for(int n=0;n<3;n++)if(indices[i+n]<0||indices[i+n]>=vertices.Count)throw new Exception("Invalid topology");text.AppendLine("f "+(indices[i]+1)+" "+(indices[i+1]+1)+" "+(indices[i+2]+1));}
byte[] payload=Encoding.UTF8.GetBytes(text.ToString());File.WriteAllBytes(name+".obj",payload);
return new{name,vertices=vertices.Count,triangles=indices.Count/3,minY,maxY,skinBones,shapes,sha256=Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant()};}
static object Measure(string name,params MeshBuilder[] meshes){var v=new List<Vector3>();var t=new List<int>();foreach(var m in meshes){int offset=v.Count;v.AddRange(m.Vertices);var subs=(List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m);foreach(var sub in subs)foreach(int index in sub)t.Add(index+offset);}return Write(name,v,t);}
public static void Main(){var rows=new List<object>();
for(int identity=0;identity<4;identity++)foreach(bool near in new[]{false,true}){var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);CityKingdomGeometry04N.Frontage(a,b,c,new RectF(-12,-12,12,12),0,12,identity,near);rows.Add(Measure("district-"+identity+(near?"-near":"-far"),a,b,c));}
foreach(bool workshop in new[]{false,true}){var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);UnderheartKingdom04N.Room(a,b,c,new RectF(-12,-12,12,12),-50,8,workshop);rows.Add(Measure(workshop?"workshop-vault":"garden-vault",a,b,c));}
for(int i=0;i<8;i++){var a=new MeshBuilder(2);GearFittings04N.Bracer(a,i);rows.Add(Measure("forge-bracer-"+i,a));}
for(int i=0;i<3;i++){var a=new MeshBuilder(1);var b=new MeshBuilder(1);UnderheartKingdom04N.Waystone(a,b,i);rows.Add(Measure("field-waystone-"+i,a,b));}
{var a=new MeshBuilder(1);CrystalCantor04N.Crown(a);rows.Add(Measure("cantor-crown",a));}
foreach(bool equipped in new[]{false,true}){var look=Appearance.Malik();if(equipped){look.ShellStyle=2;look.GauntletStyle=1;look.StriderStyle=1;}var d=BodyBuilder.Build(look,RigLayout.From(look),2);int max=d.Weights.Max(w=>Math.Max(Math.Max(w.boneIndex0,w.boneIndex1),Math.Max(w.boneIndex2,w.boneIndex3)));rows.Add(Write(equipped?"malik-equipped":"malik-base",d.Vertices,d.Triangles,max+1,d.FaceShapes04L.Count));}
Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled mesh data; no Unity scene placement, rendering, visual comparison or measured frame performance.",meshes=rows}));}}
'''
def main():
 a=argparse.ArgumentParser(description=__doc__)
 for name in ('dotnet','assembly','unity-references','output'):a.add_argument('--'+name,type=Path,required=True)
 x=a.parse_args();out=x.output.resolve();out.mkdir(parents=True,exist_ok=True);sdk=x.dotnet.parent;unity=x.unity_references.resolve();csc=sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
 src=out/'GeometryProbe.cs';src.write_text(CODE);dll=out/'GeometryProbe.dll'
 refs=list(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+list(unity.glob('*.dll'))+[x.assembly.resolve()]
 rsp=out/'probe.rsp';rsp.write_text('\n'.join('"'+v+'"' for v in ['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(src)]))
 subprocess.run([str(x.dotnet),str(csc),'@'+str(rsp)],check=True)
 for p in refs:
  if p.parent==unity or p==x.assembly.resolve():shutil.copy2(p,out/p.name)
 dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
 run=subprocess.run([str(x.dotnet),str(dll)],capture_output=True,text=True,check=True,cwd=out)
 report=json.loads(run.stdout);(out/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print(run.stdout)
if __name__=='__main__':main()
