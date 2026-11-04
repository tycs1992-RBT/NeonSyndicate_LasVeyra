#!/usr/bin/env python3
"""Measure the actual compiled H3/H5 profiles. Graph metres and asset budgets, not Unity traversal, screenshots or FPS."""
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
foreach(int revision in new[]{9,10}){
CityLayout w=new CityLayout(61724);w.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(w,catalog);if(!w.ApplyProfile04O((CityRevision04O)revision,venues))throw new Exception("Profile rejected");
Vector3 start=venues.Single(v=>v.Definition.Id=="lofts").Outside;List<object> anchors=new List<object>(),services=new List<object>();
foreach(string id in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-crossing","Realm:storm-spring","Realm:storm-vein"}){Vector3 at;if(w.ResolveRealmAnchor04OC(id,out at))anchors.Add(new{id,position=P(at),graphMetres=Distance(w,start,at)});}
foreach(VenueBinding04C venue in venues){List<ArtPlacementH2> pieces=revision==10 && GothicCivicH5.Replaces(venue.Definition.Id)?GothicCivicH5.Plan(venue):GothicHeartH3.ServicePlan(venue);MeshBuilder structure=new MeshBuilder(1);if(revision==10 && GothicCivicH5.Replaces(venue.Definition.Id))GothicCivicH5.Structure(structure,venue);else if(pieces.Count>0)GothicHeartH3.Structure(structure,venue);
services.Add(new{id=venue.Definition.Id,title=venue.Definition.Title,position=P(venue.Outside),graphMetres=Distance(w,start,venue.Outside),replaced=pieces.Count>0,placements=pieces.Count,highVertices=structure.VertexCount+pieces.Sum(p=>high[p.Model].VertexCount),lowVertices=structure.VertexCount+pieces.Sum(p=>low[p.Model].VertexCount),collisionVertices=structure.VertexCount+pieces.Where(p=>AscensionArtH2.HasCollision(p.Model)).Sum(p=>low[p.Model].VertexCount),models=pieces.Select(p=>p.Model).Distinct().ToArray()});}
List<ArtPlacementH2> additional=GothicHeartH3.CourtPlan(w);additional.AddRange(GothicHeartH3.RegionalPlan(w));
worlds.Add(new{revision,bank=SaveSystem.SlotFileName04OB(0,revision),home=P(start),services,anchors,porch=revision>=9?new{position=P(w.HearthLensH3),graphMetres=Distance(w,start,w.HearthLensH3)}:null,castellan=new{position=P(w.CastellanCourt04OD),graphMetres=Distance(w,start,w.CastellanCourt04OD)},sovereign=new{position=P(w.SovereignCourt04OD),graphMetres=Distance(w,start,w.SovereignCourt04OD)},newRouteMetres=w.RealmRoutes04OB.Where(r=>r.Id=="04oh/h3/home-porch").Sum(r=>Length(r.Points)),additional=new{placements=additional.Count,highVertices=additional.Sum(p=>high[p.Model].VertexCount),lowVertices=additional.Sum(p=>low[p.Model].VertexCount),collisionVertices=additional.Where(p=>AscensionArtH2.HasCollision(p.Model)).Sum(p=>low[p.Model].VertexCount),models=additional.Select(p=>p.Model).Distinct().ToArray()}});}
Console.WriteLine(JsonSerializer.Serialize(new{scope="Actual compiled profile 9/10 and fixed-mesh data. Graph metres and bounded mesh vertices, not timed gameplay, Unity collision, visuals, GPU memory or FPS. Original destinations have not been compressed or duplicated.",worlds,geometry=new object[0]}));
}}
'''
if __name__=='__main__':base.main()
