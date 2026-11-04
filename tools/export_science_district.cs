// Export the exact authored C# meshes. This does not import, render or simulate Unity.
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using NeonSyndicate;
using UnityEngine;
partial class Export
{
    static void Write(string root,string name,Dictionary<string,MeshBuilder> groups)
    {
        using(StreamWriter f=new StreamWriter(Path.Combine(root,name+".obj")))
        {
            int offset=1;
            foreach(KeyValuePair<string,MeshBuilder> part in groups)
            {
                f.WriteLine("g "+part.Key);
                foreach(Vector3 p in part.Value.Vertices)f.WriteLine(string.Format(CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}",p.x,p.y,p.z));
                IReadOnlyList<int> t=part.Value.Triangles(0);
                for(int i=0;i<t.Count;i+=3)f.WriteLine("f "+(t[i]+offset)+" "+(t[i+1]+offset)+" "+(t[i+2]+offset));
                offset+=part.Value.VertexCount;Console.WriteLine(name+" / "+part.Key+" : "+t.Count/3+" triangles");
            }
        }
    }
    static void Append(Dictionary<string,MeshBuilder> target,Dictionary<string,MeshBuilder> source,Vector3 at,float yaw)
    {
        foreach(KeyValuePair<string,MeshBuilder> part in source)
        {
            if(!target.ContainsKey(part.Key))target.Add(part.Key,new MeshBuilder(1));
            MeshBuilder transformed=new MeshBuilder(1);transformed.Append(part.Value);
            for(int i=0;i<transformed.VertexCount;i++){transformed.Vertices[i]=at+QMath.AY(yaw)*transformed.Vertices[i];transformed.Normals[i]=QMath.AY(yaw)*transformed.Normals[i];}
            target[part.Key].Append(transformed);
        }
    }
    static partial void PrepareCircuitContext(CityLayout world,List<VenueBinding04C> venues);
    static partial void WriteCircuitContext(string path,CityLayout world,Dictionary<string,MeshBuilder> district);
    static int Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);
        CityLayout world=new CityLayout(61724);world.Generate();
        List<VenueBinding04C> venues=InhabitedCityBindings04C.Select(world,NeonSyndicate.Tests.InhabitedCityBindings04CTests.ReadCatalog());
        world.ApplyProfile04O(CityRevision04O.GothicCivicH5,venues);
        PrepareCircuitContext(world,venues);
        Dictionary<string,MeshBuilder> district=new Dictionary<string,MeshBuilder>();
        foreach(string id in new[]{"lofts","forge","archive"})
        {
            VenueBinding04C binding=venues.Find(v=>v.Definition.Id==id);
            Console.WriteLine(id+" actual frame "+binding.Origin+" yaw "+binding.Yaw+" roofpad "+binding.Building.RoofPad04OB);
            MeshBuilder structure=new MeshBuilder(1),porch=new MeshBuilder(1),mineral=new MeshBuilder(1);
            if(id=="archive")GothicCivicH5.Structure(structure,binding,mineral);else GothicHeartH3.Structure(structure,binding);
            Dictionary<string,MeshBuilder> old=new Dictionary<string,MeshBuilder>();old.Add("masonry",new MeshBuilder(1));old["masonry"].Append(structure);
            if(mineral.VertexCount>0){old.Add("amethyst",new MeshBuilder(1));old["amethyst"].Append(mineral);}
            foreach(ArtPlacementH2 part in id=="archive"?GothicCivicH5.Plan(binding):GothicHeartH3.ServicePlan(binding))
            {
                string role=AscensionArtH2.Role(part.Model);if(!old.ContainsKey(role))old.Add(role,new MeshBuilder(1));
                MeshBuilder mesh=AscensionArtH2.Decode(File.ReadAllBytes("Assets/NeonSyndicate/Resources/AscensionH2/Meshes/"+part.Model+"_low.bytes"));
                AscensionArtH2.Append(old[role],mesh,part.Position,part.Yaw,part.Scale);
                if(part.Model=="processional_stair" || part.Model=="gallery_rail")AscensionArtH2.Append(porch,mesh,part.Position,part.Yaw,part.Scale);
            }
            ScienceDistrictArt art=HeartPlanFR01.ScienceService(id,structure,porch);
            if(mineral.VertexCount>0)art.Surface("amethyst").Append(mineral);
            Write(args[0],id+"_before",old);Write(args[0],id+"_after",art.Surfaces);Append(district,art.Surfaces,binding.Origin,binding.Yaw);
        }
        Write(args[0],"frontier",HeartPlanFR01.ScienceFrontier(world).Surfaces);
        MethodInfo surveyedCourt=typeof(HeartPlanFR01).GetMethod("ScienceCourt",new[]{typeof(CityLayout)});
        ScienceDistrictArt court=surveyedCourt==null?HeartPlanFR01.ScienceCourt():(ScienceDistrictArt)surveyedCourt.Invoke(null,new object[]{world});HeartPlanFR01.ScienceApproach(court,world);
        Write(args[0],"court_after",court.Surfaces);Append(district,court.Surfaces,Vector3.zero,0);Write(args[0],"district_after",district);
        WriteCircuitContext(args[0],world,district);
        MethodInfo admission=typeof(HeartPlanFR01).GetMethod("ScienceApproachClear",BindingFlags.NonPublic|BindingFlags.Static);
        foreach(Vector3 site in HeartPlanFR01.ScienceApproachSites())Console.WriteLine("LOGGIA "+site+" admitted "+admission.Invoke(null,new object[]{world,site})+" buildingOverlap "+world.IsInsideBuilding(site,9.5f));
        foreach(Vector3 site in HeartPlanFR01.ScienceApproachSites())
        {
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=MathUtil.Flat(route.Points[i-1]),d=MathUtil.Flat(route.Points[i]-route.Points[i-1]);
                float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(site)-a,d)/d.sqrMagnitude):0;
                Vector3 delta=site-(a+d*t);
                if(Mathf.Abs(delta.x)<4.7f+route.Width*.5f && Mathf.Abs(delta.z)<8.3f+route.Width*.5f)Console.WriteLine("BLOCKED route "+site+" "+route.Id);
            }
            foreach(List<Vector3> receipts in new[]{world.CrystalShardSpots,world.DataChipSpots,world.SpellStoneSpots,world.PickupSpots})foreach(Vector3 p in receipts)if(MathUtil.FlatDistance(site,p)<10.3f)Console.WriteLine("BLOCKED receipt "+site+" "+p);
        }
        using(StreamWriter inventory=new StreamWriter(Path.Combine(args[0],"near_home_structure_candidates.csv")))
        {
            inventory.WriteLine("seed,family,footprint_x,footprint_z,height,min_x,min_y,min_z,max_x,max_y,max_z,triangles");
            foreach(Block block in world.Blocks)foreach(Lot lot in block.Lots)foreach(Building building in lot.Buildings)
            {
                if(building.GothicFamily04OB!=9 && (building.Height<24 || MathUtil.FlatDistance(building.Footprint.Center(0),new Vector3(0,0,-34))>160))continue;
                MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),crystal=new MeshBuilder(1);
                GothicRealmGeometry04OB.Family(stone,trim,crystal,building,true);MeshBuilder mesh=new MeshBuilder(1);mesh.Append(stone);mesh.Append(trim);mesh.Append(crystal);
                if(mesh.VertexCount==0)continue;
                Vector3 min=mesh.Vertices[0],max=min;foreach(Vector3 v in mesh.Vertices){min=Vector3.Min(min,v);max=Vector3.Max(max,v);}
                inventory.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}",building.Seed,building.GothicFamily04OB,building.Footprint.Center(0).x,building.Footprint.Center(0).z,building.Height,min.x,min.y,min.z,max.x,max.y,max.z,mesh.Triangles(0).Count/3));
            }
        }
        using(StreamWriter roofs=new StreamWriter(Path.Combine(args[0],"optional_roof_landmark_candidates.csv")))
        {
            roofs.WriteLine("seed,blueprint,base_x,base_y,base_z,scale,near_arrival");
            foreach(CrystalCity02FRoofSite site in CrystalCity02FPlanner.SelectRoofs(world))
                roofs.WriteLine(string.Format(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6}",site.Building.Seed,site.BlueprintId,site.Position.x,site.Position.y,site.Position.z,site.Scale,site.NearArrival));
        }
        return 0;
    }
}
