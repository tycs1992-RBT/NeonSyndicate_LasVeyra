"""Export genuine existing Archive structure/placements for labelled DCC context, never native proof."""
import argparse,contextlib,io,json,sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
import measure_city_rebirth_04o as compiler
from measure_world_rewrite_wr01 import source_digest,commit_source_digest
CODE=r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Text;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class ArchiveProbe {
static float[] P(Vector3 p){return new[]{p.x,p.y,p.z};}
static object Write(string name,MeshBuilder mesh){StringBuilder t=new StringBuilder();foreach(Vector3 p in mesh.Vertices)t.AppendFormat(CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}\n",p.x,p.y,p.z);IReadOnlyList<int> f=mesh.Triangles(0);for(int i=0;i<f.Count;i+=3)t.AppendLine("f "+(f[i]+1)+" "+(f[i+1]+1)+" "+(f[i+2]+1));File.WriteAllText(name+".obj",t.ToString());return new{name};}
public static void Main(string[]args){List<string> errors=new List<string>();Input input=QuestJson.Read<Input>(File.ReadAllText(args[0]),args[0],errors,null);if(errors.Count>0)throw new Exception(string.Join(";",errors));CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(l,new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()});l.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues);VenueBinding04C archive=venues.First(v=>v.Definition.Id=="archive");MeshBuilder stone=new MeshBuilder(1),mineral=new MeshBuilder(1);GothicCivicH5.Structure(stone,archive,mineral);Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled original Archive H5 structure and source model placements. DCC context only, no native render or contact.",geometry=new[]{Write("ArchiveOriginalStructure",stone),Write("ArchiveOriginalMineral",mineral)},parts=GothicCivicH5.Plan(archive).Select(p=>new{model=p.Model,position=P(p.Position),yaw=p.Yaw,scale=P(p.Scale),physicalWitness=HeartPlanFR01.CrownPartWR01(p.Model)}),nativeUnity="NOT RUN"}));}}
'''
def main():
 p=argparse.ArgumentParser()
 for key in ['root','dotnet','assembly','unity-references','output','matrix-report']:p.add_argument('--'+key,type=Path,required=True)
 p.add_argument('--source-commit',required=True);a=p.parse_args();finger=source_digest(a.root.resolve())
 if finger!=json.loads(a.matrix_report.read_text())['source_sha256']or finger!=commit_source_digest(a.root.resolve(),a.source_commit):raise SystemExit('Stale source context refused.')
 compiler.CODE=CODE;sys.argv=[__file__]+sum((['--'+k,str(getattr(a,k.replace('-','_')).resolve())]for k in ['root','dotnet','assembly','unity-references','output']),[])
 with contextlib.redirect_stdout(io.StringIO()):compiler.main()
 d=json.loads((a.output/'world-manifest.json').read_text());d['sourceCommit']=a.source_commit;d['sourceCSharpSha256']=finger;(a.output/'world-manifest.json').write_text(json.dumps(d,indent=2)+'\n');print('Actual retained Archive context exported.')
if __name__=='__main__':main()
