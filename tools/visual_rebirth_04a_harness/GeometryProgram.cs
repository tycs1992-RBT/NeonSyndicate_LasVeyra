// Offline CPU geometry/coverage recorder only. These adapters do not emulate Unity rendering, physics or imports.
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using NeonSyndicate;
using UnityEngine;
public static class GeometryProgram
{
 static int invalidVertices,degenerateTriangles,opposedVertexNormals,envelopeViolations,landingViolations;
 static FieldInfo Triangles=typeof(MeshBuilder).GetField("_tris",BindingFlags.NonPublic|BindingFlags.Instance);
 public static Dictionary<Material,MeshBuilder> Build(Building bld,bool detailed)
 {
  RectF fp=bld.Footprint;float baseY=CityLayout.CurbHeight;
  TIER_SETUP
  Dictionary<Material,MeshBuilder> geometry=new Dictionary<Material,MeshBuilder>();
  Func<Material,MeshBuilder> batch=mat=>{if(!geometry.ContainsKey(mat))geometry[mat]=new MeshBuilder(1);return geometry[mat];};
  int style=VisualRebirth04ACityPlan.StyleAt(fp.CenterX,fp.CenterZ);float bottom=0;
  for(int t=0;t<tiers;t++) {RectF tf=fp.Inset(tierInset[t]);if(!tf.Valid||tf.Width<3||tf.Depth<3)break;
   VisualRebirth04AArchitecture.Tier(bld,tf,baseY+bottom,baseY+tierTop[t],t,style,detailed,batch);bottom=tierTop[t];}
  VisualRebirth04AArchitecture.Crown(bld,fp.Inset(tierInset[tiers-1]),baseY+tierTop[tiers-1],style,detailed,batch);
  return geometry;
 }
 static int Count(Dictionary<Material,MeshBuilder> geometry) {return geometry.Values.Sum(b=>((List<int>[])Triangles.GetValue(b))[0].Count/3);}
 static void Validate(Building bld,Dictionary<Material,MeshBuilder> geometry)
 {
  foreach(MeshBuilder b in geometry.Values) {
   foreach(Vector3 p in b.Vertices) {
    if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))invalidVertices++;
    if(p.x<bld.Footprint.X0-.1301f||p.x>bld.Footprint.X1+.1301f||p.z<bld.Footprint.Z0-.1301f||p.z>bld.Footprint.Z1+.1301f)envelopeViolations++;
   }
   List<int> t=((List<int>[])Triangles.GetValue(b))[0];
   for(int i=0;i<t.Count;i+=3) {Vector3 cross=Vector3.Cross(b.Vertices[t[i+1]]-b.Vertices[t[i]],b.Vertices[t[i+2]]-b.Vertices[t[i]]);
    if(cross.sqrMagnitude<1e-12f)degenerateTriangles++;else if(Vector3.Dot(cross,b.Normals[t[i]])<-.000001f)opposedVertexNormals++;}
  }
 }
 static void ValidateRoof(Building bld)
 {
  RectF fp=bld.Footprint;float baseY=CityLayout.CurbHeight;
  TIER_SETUP
  RectF roof=fp.Inset(tierInset[tiers-1]);Dictionary<Material,MeshBuilder> meshes=new Dictionary<Material,MeshBuilder>();
  Func<Material,MeshBuilder> get=mat=>{if(!meshes.ContainsKey(mat))meshes[mat]=new MeshBuilder(1);return meshes[mat];};
  VisualRebirth04AArchitecture.Crown(bld,roof,baseY+tierTop[tiers-1],VisualRebirth04ACityPlan.StyleAt(fp.CenterX,fp.CenterZ),false,get);
  foreach(MeshBuilder mesh in meshes.Values) {
   List<int> t=((List<int>[])Triangles.GetValue(mesh))[0];
   for(int i=0;i<t.Count;i+=3) {Vector3 a=mesh.Vertices[t[i]],b=mesh.Vertices[t[i+1]],c=mesh.Vertices[t[i+2]];
    float x0=Math.Min(a.x,Math.Min(b.x,c.x)),x1=Math.Max(a.x,Math.Max(b.x,c.x));
    float z0=Math.Min(a.z,Math.Min(b.z,c.z)),z1=Math.Max(a.z,Math.Max(b.z,c.z));
    if(x1>roof.CenterX-2.1f&&x0<roof.CenterX+2.1f&&z1>roof.CenterZ-2.1f&&z0<roof.CenterZ+2.1f)landingViolations++;
   }
  }
 }
 static object Export(Building building,string id,string folder)
 {
  Dictionary<Material,MeshBuilder> geometry=Build(building,false);List<object> parts=new List<object>();
  Vector3 center=building.Footprint.Center(0f);int tri=0;
  foreach(KeyValuePair<Material,MeshBuilder> pair in geometry) {
   MeshBuilder b=pair.Value;List<int> indices=((List<int>[])Triangles.GetValue(b))[0];string model=id+"_"+pair.Key.Role;
   using(BinaryWriter writer=new BinaryWriter(File.Create(Path.Combine(folder,model+".bytes")))) {
    writer.Write(new byte[]{67,77,72,49});writer.Write(indices.Count);
    foreach(int index in indices){Vector3 p=b.Vertices[index]-center,n=b.Normals[index];Vector2 uv=b.UVs[index];writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);writer.Write(uv.x);writer.Write(uv.y);}
   }
   parts.Add(new {model,role=pair.Key.Role,style=pair.Key.Style,position=new[]{0f,0f,0f},scale=new[]{1f,1f,1f},euler=new[]{0f,0f,0f},triangles=indices.Count/3});tri+=indices.Count/3;
  }
  object result=new {id,scope="Offline source geometry extraction of actual CityLayout 61724 parcel, ordinary architecture budget. No Unity rendering.",
   seed=building.Seed,archetype=building.Style.ToString(),building.Height,building.Tiers,
   worldCenter=new[]{center.x,center.y,center.z},footprint=new[]{building.Footprint.Width,building.Footprint.Depth},triangles=tri,parts};
  File.WriteAllText(Path.Combine(folder,id+".json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true,IncludeFields=true}));return result;
 }
 static float cityMinX=float.PositiveInfinity,cityMinY=float.PositiveInfinity,cityMinZ=float.PositiveInfinity;
 static float cityMaxX=float.NegativeInfinity,cityMaxY=float.NegativeInfinity,cityMaxZ=float.NegativeInfinity;
 static Dictionary<string,BinaryWriter> cityWriters=new Dictionary<string,BinaryWriter>();
 static Dictionary<string,int> cityVertices=new Dictionary<string,int>();
 static Dictionary<string,Material> cityMaterials=new Dictionary<string,Material>();
 static void AppendCity(Building building,Dictionary<Material,MeshBuilder> geometry,string folder)
 {
  int chunkX=Mathf.FloorToInt(building.Footprint.CenterX/160f),chunkZ=Mathf.FloorToInt(building.Footprint.CenterZ/160f);
  foreach(KeyValuePair<Material,MeshBuilder> pair in geometry) {
   string model="city_all_"+pair.Key.Style+"_"+pair.Key.Role+"_"+chunkX+"_"+chunkZ;
   if(!cityWriters.ContainsKey(model)) {BinaryWriter next=new BinaryWriter(File.Create(Path.Combine(folder,model+".bytes")));next.Write(new byte[]{67,77,72,49});next.Write(0);cityWriters[model]=next;cityVertices[model]=0;cityMaterials[model]=pair.Key;}
   BinaryWriter writer=cityWriters[model];MeshBuilder b=pair.Value;List<int> indices=((List<int>[])Triangles.GetValue(b))[0];
   foreach(int index in indices){Vector3 p=b.Vertices[index],n=b.Normals[index];Vector2 uv=b.UVs[index];
    cityMinX=Math.Min(cityMinX,p.x);cityMinY=Math.Min(cityMinY,p.y);cityMinZ=Math.Min(cityMinZ,p.z);
    cityMaxX=Math.Max(cityMaxX,p.x);cityMaxY=Math.Max(cityMaxY,p.y);cityMaxZ=Math.Max(cityMaxZ,p.z);writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);writer.Write(uv.x);writer.Write(uv.y);}
   cityVertices[model]+=indices.Count;
  }
 }
 static void FinishCity(string folder)
 {
  List<object> parts=new List<object>();
  foreach(KeyValuePair<string,BinaryWriter> pair in cityWriters) {pair.Value.Seek(4,SeekOrigin.Begin);pair.Value.Write(cityVertices[pair.Key]);pair.Value.Dispose();Material material=cityMaterials[pair.Key];
   parts.Add(new {model=pair.Key,role=material.Role,style=material.Style,position=new[]{0f,0f,0f},scale=new[]{1f,1f,1f},euler=new[]{0f,0f,0f},triangles=cityVertices[pair.Key]/3});}
  object report=new {id="city_all",seed=61724,ordinaryBudget=true,bounds_min=new[]{cityMinX,cityMinY,cityMinZ},bounds_max=new[]{cityMaxX,cityMaxY,cityMaxZ},scope="Actual CityLayout61724 all eligible architecture at ordinary mesh budget, source geometry extraction. No road/ground geometry, selected high landmark LODs, strongholds or live gameplay. Offline review only, never a Unity frame.",parts};
  File.WriteAllText(Path.Combine(folder,"city_all.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
 }
 public static void Main(string[] args)
 {
  VisualRebirth04AAssets.Folder=args[0];string folder=args[1];Directory.CreateDirectory(folder);
  string cityFolder=args.Length>3?args[3]:null;if(cityFolder!=null)Directory.CreateDirectory(cityFolder);
  CityLayout layout=new CityLayout(61724);layout.Generate();List<Building> buildings=layout.Blocks.SelectMany(b=>b.Lots).Concat(layout.DesertLots).Where(l=>!l.CustomBuilt).SelectMany(l=>l.Buildings.Where(b=>VisualRebirth04ACityPlan.CanTheme(b,l))).ToList();
  long triangles=0;int max=0;List<object> counts=new List<object>();
  if(args.Length<3||args[2]!="examples") foreach(Building building in buildings){Dictionary<Material,MeshBuilder> mesh=Build(building,false);int n=Count(mesh);Validate(building,mesh);ValidateRoof(building);if(cityFolder!=null)AppendCity(building,mesh,cityFolder);triangles+=n;max=Math.Max(max,n);counts.Add(new {building.Seed,archetype=building.Style.ToString(),triangles=n});}
  if(cityFolder!=null)FinishCity(cityFolder);
  Dictionary<string,Vector3> anchors=new Dictionary<string,Vector3>{{"city_spawn",new Vector3(-9.5f,0,-65.72732f)},{"city_verdant",new Vector3(-400,0,120)},{"city_dawnstone",new Vector3(420,0,120)},{"city_obsidian",new Vector3(0,0,-600)}};
  List<object> examples=new List<object>();foreach(KeyValuePair<string,Vector3> pair in anchors){Building nearest=buildings.OrderBy(b=>(b.Footprint.Center(0)-pair.Value).sqrMagnitude).First();examples.Add(Export(nearest,pair.Key,folder));}
  object report=new {scope="Executed unchanged 04A Tier and Crown plus original MeshBuilder CPU geometry, actual CityLayout 61724 and original tier formulas. Math/material/resource recording adapters replace Unity native APIs. Count is complete ordinary-equivalent architecture, excludes additional near landmark LODs, old road geometry and retained roof equipment. Not frame rate or Unity execution.",seed=61724,buildings=buildings.Count,ordinaryEquivalentTriangles=triangles,maximumBuildingTriangles=max,invalidVertices,degenerateTriangles,opposedVertexNormals,envelopeViolations,landingViolations,examples,counts};
  Console.WriteLine(JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
 }
}
