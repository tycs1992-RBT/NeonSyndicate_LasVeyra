#!/usr/bin/env python3
"""Run actual H2 profile/composition source and measure graph metres. NOT UNITY or playtime evidence."""
import measure_city_rebirth_04o as base
base.CODE=r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class Probe {
static object P(Vector3 p){return new[]{p.x,p.y,p.z};}
static float Length(IList<Vector3> p){float n=0;for(int i=1;i<p.Count;i++)n+=Vector3.Distance(p[i-1],p[i]);return n;}
static float? Distance(CityLayout w,Vector3 a,Vector3 b){List<Vector3> path=w.FindWalkRoute04O(a,b);return path.Count>0?(float?)Length(path):null;}
public static void Main(string[] args){
List<string> errors=new List<string>();Input input=QuestJson.Read<Input>(File.ReadAllText(args[0]),args[0],errors,null);if(errors.Count>0)throw new Exception(string.Join(";",errors));
VenueCatalog04C catalog=new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()};List<object> worlds=new List<object>();
string root=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(args[0]),"..","AscensionH2","Meshes"));Dictionary<string,MeshBuilder> low=new Dictionary<string,MeshBuilder>(),high=new Dictionary<string,MeshBuilder>();
foreach(string model in AscensionArtH2.Models){low.Add(model,AscensionArtH2.Decode(File.ReadAllBytes(Path.Combine(root,model+"_low.bytes"))));high.Add(model,AscensionArtH2.Decode(File.ReadAllBytes(Path.Combine(root,model+"_high.bytes"))));}
foreach(int revision in new[]{7,8}){
CityLayout w=new CityLayout(61724);w.Generate();if(!w.ApplyProfile04O((CityRevision04O)revision,InhabitedCityBindings04C.Select(w,catalog)))throw new Exception("Profile rejected");
Poi home=w.FindPoi("Veyra Lofts");Vector3 start=home.Position+home.Facing*3.5f;List<object> anchors=new List<object>(),routes=new List<object>();
foreach(string id in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-crossing","Realm:storm-spring","Realm:storm-vein"}){Vector3 at;if(w.ResolveRealmAnchor04OC(id,out at))anchors.Add(new{id,position=P(at),metres=Distance(w,start,at)});}
foreach(RealmRoute04OB route in w.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04og/"))){List<ArtPlacementH2> pieces=revision==8?AscensionCompositionH2.Plan(route):new List<ArtPlacementH2>();routes.Add(new{id=route.Id,width=route.Width,metres=Length(route.Points),points=route.Points.Select(P),placements=pieces.Select(p=>new{model=p.Model,position=P(p.Position),yaw=p.Yaw,scale=P(p.Scale),collision=AscensionArtH2.HasCollision(p.Model)}),highVertices=pieces.Sum(p=>high[p.Model].VertexCount),lowVertices=pieces.Sum(p=>low[p.Model].VertexCount),collisionVertices=pieces.Where(p=>AscensionArtH2.HasCollision(p.Model)).Sum(p=>low[p.Model].VertexCount)});}
worlds.Add(new{revision,bank=SaveSystem.SlotFileName04OB(0,revision),arrival=P(start),rejectedNeighbors=w.RejectedNeighbors04OH,castellan=new{position=P(w.CastellanCourt04OD),metres=Distance(w,start,w.CastellanCourt04OD)},sovereign=new{position=P(w.SovereignCourt04OD),metres=Distance(w,start,w.SovereignCourt04OD)},anchors,routes});}
Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled CityLayout and H2 fixed-mesh placements. Graph metres, not native walk time or compression. NOT UNITY rendering, PhysX or FPS.",worlds,geometry=new object[0]}));
}}
'''
if __name__=='__main__':base.main()
