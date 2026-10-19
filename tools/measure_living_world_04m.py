#!/usr/bin/env python3
"""Generate new 04M frontage and venue OBJ meshes and statistics from the actual compiled game, without rendering."""
import argparse,json,shutil,subprocess
from pathlib import Path


def main():
    a=argparse.ArgumentParser(description=__doc__)
    for name in ('dotnet','assembly','unity-references','output'): a.add_argument('--'+name,type=Path,required=True)
    x=a.parse_args(); x.unity_references=x.unity_references.resolve(); out=x.output.resolve();out.mkdir(parents=True,exist_ok=True)
    sdk=x.dotnet.parent; csc=sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
    code='using System;using System.Reflection;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text;using System.Text.Json;using System.Security.Cryptography;using NeonSyndicate;using UnityEngine;\npublic static class Probe {\nstatic object Measure(string name,MeshBuilder[] meshes){\nvar text=new StringBuilder();int offset=0,vertices=0,triangles=0;float minY=float.MaxValue,maxY=float.MinValue;\nforeach(var m in meshes){var indices=((List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m))[0];foreach(var v in m.Vertices){text.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0} {1} {2}\\n",v.x,v.y,v.z);vertices++;minY=Math.Min(minY,v.y);maxY=Math.Max(maxY,v.y);}\nfor(int i=0;i<indices.Count;i+=3){for(int n=0;n<3;n++)if(indices[i+n]<0||indices[i+n]>=m.VertexCount)throw new Exception("Invalid topology");text.AppendLine("f "+(indices[i]+1+offset)+" "+(indices[i+1]+1+offset)+" "+(indices[i+2]+1+offset));triangles++;}offset+=m.VertexCount;}\nbyte[] payload=Encoding.UTF8.GetBytes(text.ToString());File.WriteAllBytes(name+".obj",payload);\nreturn new{name,vertices,triangles,minY,maxY,sha256=Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant()};}\npublic static void Main(){var rows=new List<object>();foreach(bool canal in new[]{false,true})foreach(bool near in new[]{false,true}){\nvar a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);CityLivingGeometry04M.Frontage(a,b,c,new RectF(-9,-9,9,9),0,10,near,canal);rows.Add(Measure((canal?"canal":"arrival")+(near?"-near":"-far"),new[]{a,b,c}));}\nforeach(string id in new[]{"dispatch","pressure","bondkeeper"})foreach(bool near in new[]{false,true}){var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);CityLivingGeometry04M.Room(id,a,b,c,near);rows.Add(Measure(id+(near?"-near":"-far"),new[]{a,b,c}));}\nConsole.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled 04M geometry exported as OBJ and measured. NOT Unity render, placement or frame performance.",meshes=rows}));}}\n'
    src=out/'GeometryProbe.cs';src.write_text(code);dll=out/'GeometryProbe.dll'
    refs=list(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+list(x.unity_references.glob('*.dll'))+[x.assembly.resolve()]
    rsp=out/'probe.rsp';rsp.write_text('\n'.join('"'+v+'"' for v in ['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(src)]))
    subprocess.run([str(x.dotnet),str(csc),'@'+str(rsp)],check=True)
    for p in refs:
        if p.parent==x.unity_references or p==x.assembly.resolve():shutil.copy2(p,out/p.name)
    dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
    run=subprocess.run([str(x.dotnet),str(dll)],capture_output=True,text=True,check=True,cwd=out)
    report=json.loads(run.stdout);(out/'geometry.json').write_text(json.dumps(report,indent=2)+'\n');print(run.stdout)
if __name__=='__main__':main()
