using System;using System.Collections.Generic;using System.IO;using System.Text;using System.Text.Json;using NeonSyndicate;using NeonSyndicate.Tests;using UnityEngine;
public static class L2Plan
{
 static string F(float v){return v.ToString("R",System.Globalization.CultureInfo.InvariantCulture);}
 public static void Main()
 {
  string folder="Docs/LiveWildL2/SourceGeometry";Directory.CreateDirectory(folder);CityLayout layout=new CityLayout(61724);layout.Generate();layout.ApplyProfile04O(CityRevision04O.GothicCivicH5,InhabitedCityBindings04C.Select(layout,InhabitedCityBindings04CTests.ReadCatalog()));WildsPlan plan=WildsLayout.Plan(layout,null);Dictionary<string,int> counts=new Dictionary<string,int>();HashSet<long> chunks=new HashSet<long>();List<object> sites=new List<object>();
  foreach(WildPiece p in plan.Pieces){long key=((long)Mathf.FloorToInt(p.Position.x/160)<<32)^(uint)Mathf.FloorToInt(p.Position.z/160);chunks.Add(key);if(LiveWildsRulesL2.Eligible(p)){string k=p.Kind.ToString();counts[k]=counts.ContainsKey(k)?counts[k]+1:1;}}
  foreach(WildSite s in plan.Sites)sites.Add(new{name=s.Name,id=s.Id,kind=s.Kind.ToString(),biome=s.Biome.ToString(),x=s.Center.x,y=s.Center.y,z=s.Center.z});
  foreach(WildPieceKind kind in new[]{WildPieceKind.Boulder,WildPieceKind.RockSpire,WildPieceKind.DeadTree})
   for(int variant=0;variant<2;variant++)
   {
    WildPiece p=new WildPiece{Kind=kind,Seed=123+variant*97,Site=-1,Biome=WildBiome.HoodooBadlands,Size=kind==WildPieceKind.RockSpire?new Vector3(2.8f,7.2f,2.8f):kind==WildPieceKind.DeadTree?new Vector3(2.6f,4.8f,2.6f):new Vector3(3.2f,2.15f,2.8f),Solid=true};MeshBuilder mesh=new MeshBuilder(WildsShapes.MatCount),col=new MeshBuilder(1);WildsShapes.Build(p,mesh,col);StringBuilder obj=new StringBuilder();foreach(Vector3 v in mesh.Vertices)obj.Append("v ").Append(F(v.x)).Append(' ').Append(F(-v.z)).Append(' ').Append(F(v.y)).Append('\n');for(int slot=0;slot<mesh.SubmeshCount;slot++){obj.Append("g mat").Append(slot).Append('\n');IReadOnlyList<int> tris=mesh.Triangles(slot);for(int i=0;i<tris.Count;i+=3)obj.Append("f ").Append(tris[i]+1).Append(' ').Append(tris[i+1]+1).Append(' ').Append(tris[i+2]+1).Append('\n');}File.WriteAllText(folder+"/Before_"+kind+"_"+(variant==0?"A":"B")+".obj",obj.ToString());
   }
  File.WriteAllText("Docs/LiveWildL2/source-plan.json",JsonSerializer.Serialize(new{scope="Actual unchanged seeded revision-10 plan, no runtime reserved list, source evidence not native gameplay",seed=61724,pieces=plan.Pieces.Count,chunks=chunks.Count,eligible=counts,sites=sites,native_unity="NOT RUN"},new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine("Plan "+plan.Pieces.Count+" pieces, "+chunks.Count+" chunks; "+JsonSerializer.Serialize(counts));
 }
}
