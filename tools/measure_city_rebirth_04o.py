#!/usr/bin/env python3
"""Export the actual compiled 04O layout and geometry. This is not Unity rendering or native traversal evidence."""
import argparse, hashlib, json, shutil, subprocess
from pathlib import Path
CODE=r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Text;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class Probe {
static object P(Vector3 p){return new[]{p.x,p.y,p.z};}
static float Length(IList<Vector3> p){float n=0;for(int i=1;i<p.Count;i++)n+=Vector3.Distance(p[i-1],p[i]);return n;}
static object Write(string name,params MeshBuilder[] meshes){var v=new List<Vector3>();var t=new List<int>();foreach(var m in meshes){int offset=v.Count;v.AddRange(m.Vertices);foreach(var sub in (List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(m))foreach(int index in sub)t.Add(index+offset);}
var text=new StringBuilder();foreach(var a in v){if(!MathUtil.IsFinite(a))throw new Exception("Nonfinite vertex");text.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}\n",a.x,a.y,a.z);}for(int i=0;i<t.Count;i+=3){for(int n=0;n<3;n++)if(t[i+n]<0||t[i+n]>=v.Count)throw new Exception("Bad topology");text.AppendLine("f "+(t[i]+1)+" "+(t[i+1]+1)+" "+(t[i+2]+1));}File.WriteAllText(name+".obj",text.ToString());return new{name,vertices=v.Count,triangles=t.Count/3};}
static VenueCatalog04C Catalog(string file){var errors=new List<string>();var input=QuestJson.Read<Input>(File.ReadAllText(file),file,errors,null);if(errors.Count>0)throw new Exception(string.Join(";",errors));return new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()};}
public static void Main(string[] args){var legacy=new CityLayout(61724);legacy.Generate();var revised=new CityLayout(61724);revised.Generate();var cat=Catalog(args[0]);var oldVenues=InhabitedCityBindings04C.Select(legacy,cat);var venues=InhabitedCityBindings04C.Select(revised,cat);if(!revised.ApplyProfile04O(CityRevision04O.Crystalline04O,venues))throw new Exception("Profile rejected");var origin=venues.Find(v=>v.Definition.Id=="lofts").Outside;
var routes=new List<object>();foreach(var venue in venues){var old=oldVenues.Find(v=>v.Definition.Id==venue.Definition.Id);var before=legacy.FindWalkRoute04O(origin,old.Outside);var after=revised.FindWalkRoute04O(origin,venue.Outside);routes.Add(new{id=venue.Definition.Id,title=venue.Definition.Title,oldPosition=P(old.Outside),position=P(venue.Outside),legacyGraphMetres=before.Count>0?(float?)Length(before):null,revisedGraphMetres=after.Count>0?(float?)Length(after):null,route=after.Select(p=>P(p)).ToArray()});}
var geometry=new List<object>();for(int family=1;family<=8;family++)foreach(bool near in new[]{true,false}){var b=new Building{Footprint=new RectF(-9,-12,9,12),Height=CityRebirthRules04O.Height((MassingFamily04O)family,61724,0),RebirthFamily04O=family};var a=new MeshBuilder(1);var m=new MeshBuilder(1);var c=new MeshBuilder(1);CityRebirthGeometry04O.Building(a,m,c,b,near);geometry.Add(Write("family-"+family+(near?"-near":"-far"),a,m,c));}
foreach(var site in revised.Spine04O)foreach(bool near in new[]{true,false}){var a=new MeshBuilder(1);var b=new MeshBuilder(1);var c=new MeshBuilder(1);CityRebirthGeometry04O.Spine(a,b,c,site,near);geometry.Add(Write(site.Id+(near?"-near":"-far"),a,b,c));}
var hero=new MeshBuilder(1);foreach(var block in revised.Blocks)foreach(var lot in block.Lots)foreach(var b in lot.Buildings)if(MathUtil.FlatDistance(b.Footprint.Center(0),origin)<210f && b.RebirthFamily04O>0)CityRebirthGeometry04O.Building(hero,hero,hero,b,true);foreach(var site in revised.Spine04O)if(MathUtil.FlatDistance(site.Position,origin)<210f)CityRebirthGeometry04O.Spine(hero,hero,hero,site,true);geometry.Add(Write("hero-route-shipping-mesh",hero));
Func<CityLayout,object[]> massing=l=>l.Blocks.SelectMany(b=>b.Lots).SelectMany(lot=>lot.Buildings).SelectMany(b=>b.RebirthFamily04O>0?CityRebirthGeometry04O.Solids(b):new List<CityMassPiece04O>{new CityMassPiece04O(b.Footprint.Center(b.Height*.5f),new Vector3(b.Footprint.Width,b.Height,b.Footprint.Depth))}).Select(s=>(object)new{center=P(s.Center),size=P(s.Size)}).ToArray();
Console.WriteLine(JsonSerializer.Serialize(new{scope="Compiled source layout, graph lengths and exported geometry. Not Unity screenshots, measured player travel time or performance.",seed=61724,origin=P(origin),rebuiltLots=revised.RebuiltLots04O,originalBuiltArea=revised.OriginalBuiltArea04O,revisedBuiltArea=revised.RevisedBuiltArea04O,passages=revised.Passages04O.Select(p=>new{p.BlockId,a=P(p.A),b=P(p.B),length=Vector3.Distance(p.A,p.B)}),routes,links=revised.SpineLinks04O.Select(l=>new{l.From,l.To,points=l.Points.Select(p=>P(p)).ToArray()}),spine=revised.Spine04O.Select(s=>new{s.Id,s.Title,position=P(s.Position),s.Height,s.Radius,s.Character}),pois=revised.Pois.Select(p=>new{p.Name,type=p.Type.ToString(),position=P(p.Position)}),sites=revised.Sites.Select(s=>new{s.Key,s.Name,position=P(s.Position)}),geometry,legacyMassing=massing(legacy),revisedMassing=massing(revised)}));}}
'''
def main():
 a=argparse.ArgumentParser(description=__doc__)
 for key in ('dotnet','assembly','unity-references','output'):a.add_argument('--'+key,type=Path,required=True)
 a.add_argument('--root',type=Path,default=Path(__file__).resolve().parents[1]);x=a.parse_args();out=x.output.resolve();out.mkdir(parents=True,exist_ok=True)
 sdk=x.dotnet.resolve().parent;unity=x.unity_references.resolve();csc=sorted(sdk.glob('sdk/*/Roslyn/bincore/csc.dll'))[-1]
 src=out/'CityProbe.cs';src.write_text(CODE);dll=out/'CityProbe.dll'
 refs=list(sdk.glob('packs/Microsoft.NETCore.App.Ref/*/ref/net8.0/*.dll'))+list(unity.glob('*.dll'))+[x.assembly.resolve()]
 rsp=out/'probe.rsp';rsp.write_text('\n'.join('"'+v+'"' for v in ['-nologo','-target:exe','-langversion:9','-out:'+str(dll)]+['-r:'+str(p) for p in refs]+[str(src)]))
 subprocess.run([str(x.dotnet),str(csc),'@'+str(rsp)],check=True)
 for p in refs:
  if p.parent==unity or p==x.assembly.resolve():shutil.copy2(p,out/p.name)
 dll.with_suffix('.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
 run=subprocess.run([str(x.dotnet),str(dll),str(x.root.resolve()/'Assets/NeonSyndicate/Resources/InhabitedCity04C/venues.json')],capture_output=True,text=True,cwd=out,check=True)
 report=json.loads(run.stdout)
 for row in report['geometry']:row['sha256']=hashlib.sha256((out/(row['name']+'.obj')).read_bytes()).hexdigest()
 (out/'world-manifest.json').write_text(json.dumps(report,indent=2)+'\n')
 print(json.dumps({k:v for k,v in report.items() if k not in ('legacyMassing','revisedMassing','pois','sites','routes')},indent=2))
if __name__=='__main__':main()
