#!/usr/bin/env python3
"""Run original revision-10 source geometry and export an outdoor ownership census. Not Unity rendering or physics."""
import argparse,contextlib,hashlib,io,json,sys,subprocess
from pathlib import Path
import measure_city_rebirth_04o as compiler
from measure_world_rewrite_wr01 import source_digest,commit_source_digest

CODE=r'''
using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Globalization;using System.Text;using System.Text.Json;using NeonSyndicate;using UnityEngine;
public sealed class Input {public string Version;public List<VenueDefinition04C> Venues=new List<VenueDefinition04C>();}
public static class OutdoorProbe {
static object P(Vector3 p){return new[]{p.x,p.y,p.z};}
static bool Region(Vector3 p){int x=Mathf.FloorToInt(p.x/160),z=Mathf.FloorToInt(p.z/160);return x>=-3&&x<=1&&z>=-1&&z<=1;}
static object R(RectF a){return new[]{a.X0,a.Z0,a.X1,a.Z1};}
static Input Read(string path){List<string> errors=new List<string>();Input d=QuestJson.Read<Input>(File.ReadAllText(path),path,errors,null);if(errors.Count>0)throw new Exception(string.Join(";",errors));return d;}
static object Write(string name,MeshBuilder mesh,Vector3 origin){StringBuilder text=new StringBuilder();foreach(Vector3 p in mesh.Vertices){Vector3 q=p-origin;text.AppendFormat(CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}\n",q.x,q.y,q.z);}IReadOnlyList<int> triangles=mesh.Triangles(0);for(int i=0;i<triangles.Count;i+=3)text.AppendLine("f "+(triangles[i]+1)+" "+(triangles[i+1]+1)+" "+(triangles[i+2]+1));File.WriteAllText(name+".obj",text.ToString());Vector3 min=mesh.Vertices[0],max=min;foreach(Vector3 p in mesh.Vertices){min=Vector3.Min(min,p);max=Vector3.Max(max,p);}return new{name,vertices=mesh.VertexCount,triangles=triangles.Count/3,min=P(min),max=P(max),origin=P(origin)};}
public static void Main(string[] args){Input input=Read(args[0]);CityLayout l=new CityLayout(61724);l.Generate();List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(l,new VenueCatalog04C{Version=input.Version,Venues=input.Venues.ToArray()});if(!l.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues))throw new Exception("Revision 10 rejected");
List<object> geometry=new List<object>();foreach(RealmRock04OB rock in l.Landscape04OB.Where(r=>Region(r.Position)).OrderByDescending(r=>r.Size.x*r.Size.y*r.Size.z).Take(3)){MeshBuilder mesh=new MeshBuilder(1);GothicRealmGeometry04OB.Rock(mesh,rock.Position,rock.Size,rock.Seed);geometry.Add(Write("Cliff"+rock.Seed,mesh,rock.Position));}
foreach(WildPatch04OC patch in l.WildPatches04OC.Where(p=>Region(p.Area.Center(0)))){MeshBuilder mesh=new MeshBuilder(1);GothicRealmGeometry04OB.WildTerrain04OC(mesh,l,patch);geometry.Add(Write("terrain-"+patch.Seed,mesh,Vector3.zero));}
List<object> trees=new List<object>();foreach(WildScenery04OC tree in l.WildScenery04OC.Where(s=>s.Kind==0&&Region(s.Position))){MeshBuilder wood=new MeshBuilder(1),leaves=new MeshBuilder(1);GothicRealmGeometry04OB.WildTree04OC(wood,leaves,tree,true);object leaf=Write("crown-"+tree.Seed,leaves,Vector3.zero);trees.Add(new{tree.Seed,tree.Region,position=P(tree.Position),size=P(tree.Size),eligible=tree.Region==1,sourceOwner="CityBuilder.BuildFracturedWilds04OC / WildTree04OC",leaf,wood=Write("trunk-"+tree.Seed,wood,Vector3.zero)});}
object report=new{scope="Fresh compiled original revision-10 source census. Potential source candidates, not native admissions, active objects, screenshots, collision or gameplay.",seed=61724,revision=10,nativeUnity="NOT RUN",chunkEnvelope=new{x0=-3,x1=1,z0=-1,z1=1,metres=160},geometry,trees,
roads=l.Edges.Select(e=>new{id=e.Id,a=P(l.Nodes[e.A].Pos),b=P(l.Nodes[e.B].Pos),width=e.Width,kind=e.Kind.ToString(),bridge=e.HasBridge,eligible=Region((l.Nodes[e.A].Pos+l.Nodes[e.B].Pos)*.5f),sourceOwner="CityBuilder.BuildRoadEdge / SolidLine / DashedLine / StopAndCrosswalk",collisionOwner="Original ground slabs and CityBuilder.BuildMedian / BuildBridge"}),
intersections=l.Nodes.Where(n=>Region(n.Pos)).Select(n=>new{id=n.Id,position=P(n.Pos),halfX=n.HalfX,halfZ=n.HalfZ,signal=n.Signal,sourceOwner="CityBuilder.BuildIntersection",signalPolicy="PROTECTED: original SignalRenderers and traffic phases"}),
blocks=l.Blocks.Where(b=>Region(b.Curb.Center(0))).Select(b=>new{id=b.Id,curb=R(b.Curb),inner=R(b.Inner),sourceOwner="CityBuilder.BuildGothicBlock04OB / TiledRectPiece",lots=b.Lots.Select(a=>new{seed=a.Seed,area=R(a.Area),use=a.Use.ToString(),poi=a.PoiName,buildings=a.Buildings.Select(v=>new{v.Seed,footprint=R(v.Footprint),v.Height})})}),
cliffs=l.Landscape04OB.Where(r=>Region(r.Position)).Select(r=>new{r.Seed,position=P(r.Position),size=P(r.Size),r.Teal,sourceOwner="CityBuilder.BuildRealmLandscape04OB / EmitRealmSolid04OB",collisionOwner="Original exact compound solid"}),
terrain=l.WildPatches04OC.Where(p=>Region(p.Area.Center(0))).Select(p=>new{p.Seed,p.Region,area=R(p.Area),p.Height,sourceOwner="CityBuilder.BuildFracturedWilds04OC / WildTerrain04OC",collisionOwner="Original exact terrain chunk"}),
props=l.Props.Where(p=>Region(p.Pos)).Select(p=>new{kind=p.Kind.ToString(),position=P(p.Pos),p.Yaw,policy="PROTECTED: breakable, lamp, bench and physical prop owners remain"}),
routes=l.RealmRoutes04OB.Select(r=>new{id=r.Id,width=r.Width,points=r.Points.Select(p=>P(p)).ToArray()}),
services=venues.Select(v=>new{id=v.Definition.Id,position=P(v.Origin),entrance=P(v.Entrance),outside=P(v.Outside),yaw=v.Yaw,sourceOwner="InhabitedCityBindings04C / original VenueView04C"}),
groundPolicy="Original BuildGround and TiledRectPiece geometry is reused exactly, including aperture subtraction. Raw region chunks may contain append bounds crossing a chunk edge. Native captured groups report their actual bounds.",
protectedPolicy="Every original collider, all signal lamps/phases, readable service signs, NPCs, quests, saved identities, five Wilds regions and seventeen discoveries remain. Only eligible city-source material appends and noncolliding Azure leaf crowns exchange presentation."};Console.WriteLine(JsonSerializer.Serialize(report));}}
'''

def main():
 p=argparse.ArgumentParser(description=__doc__)
 for name in ('root','dotnet','assembly','unity-references','output'):p.add_argument('--'+name,type=Path,required=True)
 p.add_argument('--source-commit',required=True);p.add_argument('--matrix-report',type=Path,required=True)
 a=p.parse_args();root=a.root.resolve();matrix=json.loads(a.matrix_report.read_text());fingerprint=source_digest(root)
 if matrix['source_sha256']!=fingerprint or commit_source_digest(root,a.source_commit)!=fingerprint:raise SystemExit('Refusing stale assembly or mismatched source commit.')
 compiler.CODE=CODE;prior=sys.argv
 sys.argv=[__file__,'--root',str(a.root.resolve()),'--dotnet',str(a.dotnet.resolve()),'--assembly',str(a.assembly.resolve()),'--unity-references',str(a.unity_references.resolve()),'--output',str(a.output.resolve())]
 try:
  with contextlib.redirect_stdout(io.StringIO()):compiler.main()
 finally:sys.argv=prior
 path=a.output/'world-manifest.json';d=json.loads(path.read_text());d['sourceCommit']=a.source_commit;d['source_sha256']=fingerprint;d['originalPhysicalSourceCommit']='99072f180f11da7edf168602b6fc603d5eb71daf';d['matrix_sha256']=hashlib.sha256(a.matrix_report.read_bytes()).hexdigest()
 d['counts']={'eligibleRoadCandidates':sum(r['eligible'] for r in d['roads']),'intersections':len(d['intersections']),'physicalCliffFormations':len(d['cliffs']),'existingTerrainPatches':len(d['terrain']),'existingTreeCandidates':len(d['trees']),'eligibleAzureCrowns':sum(t['eligible'] for t in d['trees']),'protectedProps':len(d['props']),'newQuests':0,'newColliders':0}
 path.write_text(json.dumps(d,indent=2)+'\n');print(json.dumps(d['counts']))
if __name__=='__main__':main()
