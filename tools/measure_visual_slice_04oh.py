#!/usr/bin/env python3
"""Exact H1 shipping mesh and route export. Not a Unity render, playable-save result or performance measurement."""
import measure_living_frontier_04og as frontier
base=frontier.base
base.CODE=base.CODE.replace('new[]{5,6}', 'new[]{6,7}').replace('if(revision==6)foreach', 'if(revision>=6)foreach')
base.CODE=base.CODE.replace('FrontierGeometry04OG.Build(mesh,mesh,mesh,route);geometry.Add(Write(route.Id.Replace("/","-"),mesh));',
 'if(revision==7)AscensionGeometry04OH.Build(mesh,mesh,mesh,route);else FrontierGeometry04OG.Build(mesh,mesh,mesh,route);geometry.Add(Write("revision-"+revision+"-"+route.Id.Replace("/","-"),mesh));')
base.CODE=base.CODE.replace('Console.WriteLine(JsonSerializer.Serialize(new{scope=', '''List<object> actors=new List<object>();
for(int detail=1;detail<=2;detail++){Appearance look=Appearance.Malik();RigLayout rig=RigLayout.From(look);BodyData mesh=BodyBuilder.Build(look,rig,detail);actors.Add(new{identity="Malik fallback",detail,vertices=mesh.Vertices.Count,triangles=mesh.Triangles.Count/3,bones=rig.World.Length,source="BodyBuilder, same original rig, not imported art"});}
foreach(string identity in new[]{"storm_prowler","quartz_guide"}){CreatureLook look=identity=="storm_prowler"?FracturedWildsCombat04OC.AzureLook(CreatureLooks.For("neon_jackal",41)):CompanionLooks.For(BondIds.QuartzGuide,41,1);CreatureBody body=CreatureBuilder.Build(look,1);actors.Add(new{identity,vertices=body.Mesh.Vertices.Count,triangles=body.Mesh.Triangles.Count/3,bones=body.Skeleton.Count,hitVolumes=body.HitVolumes.Count,body.Radius,body.Height,source="CreatureBuilder fallback, not imported art"});}
Console.WriteLine(JsonSerializer.Serialize(new{actors,scope=''')
if __name__=='__main__':base.main()
