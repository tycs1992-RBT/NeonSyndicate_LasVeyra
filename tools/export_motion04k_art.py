#!/usr/bin/env python3
"""Export the actual compiled Malik skin and three earned drake bodies for a Blender handoff.

No Unity import/render is performed. Requires the genuine-metadata game assembly,
.NET 8 and matching Unity modules. Output is inspectable JSON, not a claimed FBX sculpt.
"""
import argparse, json, shutil, subprocess
from pathlib import Path

PROGRAM = r'''
using System;using System.IO;using System.Linq;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public static class Export {
 static float[] V(Vector3 v)=>new[]{v.x,v.y,v.z};
 static object Skin(BodyData d,string[] names,int[] parents,Vector3[] world) {
  return new{format=1,axes="Unity metres: left -X, up +Y, forward +Z",names,parents,world=world.Select(V),
   vertices=d.Vertices.Select(V),normals=d.Normals.Select(V),triangles=d.Triangles,
   uv=d.Palette.Select(v=>new[]{v.x,v.y}),detail=d.Detail.Select(v=>new[]{v.x,v.y}),
   colors=d.Palette.Select(v=>{Color c=CharacterPaletteData.Albedo(Mathf.Clamp((int)(v.y*16),0,15),Mathf.Clamp((int)(v.x*16),0,15));return new[]{c.r,c.g,c.b,1f};}),
   joints=d.Weights.Select(w=>new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}),
   weights=d.Weights.Select(w=>new[]{w.weight0,w.weight1,w.weight2,w.weight3})};
 }
 public static void Main(string[] args) {
  string dir=args[0];Directory.CreateDirectory(dir);Appearance look=Appearance.Malik();RigLayout rig=RigLayout.From(look);
  File.WriteAllText(Path.Combine(dir,"malik.json"),JsonSerializer.Serialize(Skin(BodyBuilder.Build(look,rig,2),RigLayout.Names,RigLayout.Parent,rig.World)));
  foreach(string id in BondIds.Archetypes)for(int evo=0;evo<3;evo++){
   CreatureBody b=CompanionBodyFactory.BuildNow(id,61724,evo,"",1);CreatureSkeleton s=b.Skeleton;
   File.WriteAllText(Path.Combine(dir,id+"_stage"+evo+".json"),JsonSerializer.Serialize(Skin(b.Mesh,s.Names.ToArray(),s.Parents.ToArray(),Enumerable.Range(0,s.Count).Select(s.ModelPosition).ToArray())));
  }
 }
}'''

def main():
    a=argparse.ArgumentParser(description=__doc__)
    for n in ('dotnet','assembly','unity-references','output'):a.add_argument('--'+n,type=Path,required=True)
    x=a.parse_args();dotnet=x.dotnet.resolve();out=x.output.resolve();work=out/'compiler';work.mkdir(parents=True,exist_ok=True)
    sdk=dotnet.parent;csc=sorted(sdk.glob('sdk/8.*/Roslyn/bincore/csc.dll'))[-1]
    refs=list(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+list(x.unity_references.resolve().glob('*.dll'))+[x.assembly.resolve()]
    src=work/'Export.cs';src.write_text(PROGRAM);dll=work/'ArtExport.dll'
    opts=['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(src)]
    rsp=work/'compile.rsp';rsp.write_text('\n'.join('"'+v+'"' for v in opts))
    subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],check=True)
    for p in refs:
        if p.parent==x.unity_references.resolve() or p==x.assembly.resolve():shutil.copy2(p,work/p.name)
    dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
    subprocess.run([str(dotnet),str(dll),str(out/'skins')],check=True)
    print(json.dumps({'exported':len(list((out/'skins').glob('*.json'))),'native_unity':False,'blender_import':'NOT RUN'}))

if __name__=='__main__':main()
