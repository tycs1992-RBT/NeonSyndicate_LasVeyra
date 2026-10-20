#!/usr/bin/env python3
"""Execute actual 04OG layout and emitted collision triangles. NOT UNITY, screenshots or performance."""
import measure_city_rebirth_04o as base
base.CODE=r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Text;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class Probe {
static float Length(IList<Vector3> path){float value=0;for(int i=1;i<path.Count;i++)value+=Vector3.Distance(path[i-1],path[i]);return value;}
static float? Route(CityLayout l,Vector3 a,Vector3 b){List<Vector3> path=l.FindWalkRoute04O(a,b);return path.Count==0?(float?)null:Length(path);}
static object P(Vector3 p){return new[]{p.x,p.y,p.z};}
static object Write(string name,MeshBuilder mesh){StringBuilder obj=new StringBuilder();int triangles=0,degenerate=0,normalErrors=0;
foreach(Vector3 v in mesh.Vertices){if(!MathUtil.IsFinite(v))throw new Exception("Nonfinite geometry");obj.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}\n",v.x,v.y,v.z);}
foreach(List<int> indices in (List<int>[])typeof(MeshBuilder).GetField("_tris",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mesh))for(int i=0;i<indices.Count;i+=3){Vector3 a=mesh.Vertices[indices[i]],b=mesh.Vertices[indices[i+1]],c=mesh.Vertices[indices[i+2]],face=Vector3.Cross(b-a,c-a);if(face.sqrMagnitude<1e-10f)degenerate++;else if(Vector3.Dot(face.normalized,mesh.Normals[indices[i]])<.98f)normalErrors++;obj.AppendLine("f "+(indices[i]+1)+" "+(indices[i+1]+1)+" "+(indices[i+2]+1));triangles++;}
File.WriteAllText(name+".obj",obj.ToString());return new{name,vertices=mesh.VertexCount,triangles,degenerate,normalErrors};}
public static void Main(string[] args){List<string> errors=new List<string>();Input input=QuestJson.Read<Input>(File.ReadAllText(args[0]),args[0],errors,null);if(errors.Count>0)throw new Exception(string.Join(";",errors));VenueCatalog04C catalog=new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()};List<object> worlds=new List<object>(),geometry=new List<object>();
foreach(int revision in new[]{5,6}){CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(l,catalog);if(!l.ApplyProfile04O((CityRevision04O)revision,venues))throw new Exception("Profile rejected");Poi home=l.FindPoi("Veyra Lofts");Vector3 start=home.Position+home.Facing*3.5f;
List<object> anchors=new List<object>();foreach(string id in new[]{"Realm:oath-seal","Realm:oath-witness","Realm:storm-crossing","Realm:storm-spring","Realm:storm-vein"}){Vector3 point;if(l.ResolveRealmAnchor04OC(id,out point))anchors.Add(new{id,position=P(point),metres=Route(l,start,point)});}
worlds.Add(new{revision,bank=SaveSystem.SlotFileName04OB(0,revision),arrival=P(start),services=venues.Select(v=>new{id=v.Definition.Id,position=P(v.Outside),metres=Route(l,start,v.Outside)}),castellan=new{position=P(l.CastellanCourt04OD),metres=Route(l,start,l.CastellanCourt04OD)},sovereign=new{position=P(l.SovereignCourt04OD),metres=Route(l,start,l.SovereignCourt04OD)},anchors,routes=l.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04og/")).Select(r=>new{r.Id,r.Width,metres=Length(r.Points),points=r.Points.Select(P)}),newCaptureViews=GoldenViews.Build(l).Where(v=>v.Id.StartsWith("04og_")).Select(v=>new{v.Id,position=P(v.Position),lookAt=P(v.LookAt)})});
if(revision==6)foreach(RealmRoute04OB route in l.RealmRoutes04OB.Where(r=>r.Id.StartsWith("04og/"))){MeshBuilder mesh=new MeshBuilder(1);FrontierGeometry04OG.Build(mesh,mesh,mesh,route);geometry.Add(Write(route.Id.Replace("/","-"),mesh));}}
MeshBuilder ward=new MeshBuilder(1);FrontierCombat04OG.Shield(ward);geometry.Add(Write("04og-oathguard-ward",ward));MeshBuilder roots=new MeshBuilder(1);for(int i=0;i<4;i++)FrontierCombat04OG.RootFan(roots,roots,i);geometry.Add(Write("04og-sovereign-hip-roots",roots));
Console.WriteLine(JsonSerializer.Serialize(new{scope="Exact compiled source route lengths and emitted geometry. NOT native Unity, screenshots, player physics, rendered visuals or FPS.",worlds,geometry}));}}
'''
if __name__=='__main__':base.main()
