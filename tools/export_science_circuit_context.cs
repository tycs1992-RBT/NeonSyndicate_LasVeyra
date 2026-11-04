// Optional matched before/after export. This writes mesh data for the software camera and walk survey.
using System;
using System.Collections.Generic;
using System.IO;
using NeonSyndicate;
using UnityEngine;

partial class Export
{
    static partial void AppendLandmarkRooms(List<VenueBinding04C> venues,Dictionary<string,MeshBuilder> context);

    static partial void PrepareCircuitContext(CityLayout world,List<VenueBinding04C> venues)
    {
        VenueBinding04C pressure=venues.Find(v=>v.Definition.Id==VenueIds04E.Pressure);
        UnderheartFrame frame=new UnderheartFrame(pressure.Origin.x,pressure.Origin.z,pressure.Yaw);
        UnderheartPlanner.Choice choice=UnderheartPlanner.Choose(world,frame,true);
        if(choice.Layout==null)throw new InvalidOperationException("Canonical Underheart layout was not admitted");
        WorldApertures.Register("survey-stair",frame.RectToWorld(VenueIds04E.StairX0,VenueIds04E.StairZ0,VenueIds04E.StairX1,VenueIds04E.StairZ1));
        if(choice.Layout.Portal!=null)WorldApertures.Register("survey-portal",choice.Layout.Portal.CutWorld);
    }

    static partial void WriteCircuitContext(string path,CityLayout world,List<VenueBinding04C> venues,Dictionary<string,MeshBuilder> district)
    {
        Dictionary<string,MeshBuilder> context=new Dictionary<string,MeshBuilder>();
        foreach(KeyValuePair<string,MeshBuilder> part in district)
        { context[part.Key]=new MeshBuilder(1); context[part.Key].Append(part.Value); }
        MeshBuilder rock=new MeshBuilder(1),trim=new MeshBuilder(1),mineral=new MeshBuilder(1);
        Building cave=HeartPlanFR01.ScienceGrottoOwner(world);
        GothicRealmGeometry04OB.Family(rock,trim,mineral,cave,true);
        context["retained-rock"]=rock;context["retained-metal"]=trim;context["retained-mineral"]=mineral;
        MeshBuilder terrain=new MeshBuilder(1); context["terrain"]=terrain;
        foreach(WildPatch04OC patch in world.WildPatches04OC)
            if(patch.Area.Overlaps(new RectF(-145,-45,105,155),0))GothicRealmGeometry04OB.WildTerrain04OC(terrain,world,patch);
        foreach(WildScenery04OC item in world.WildScenery04OC)
        {
            Vector3 p=item.Position;if(p.x< -145||p.x>105||p.z< -45||p.z>155)continue;
            if(item.Kind==0)GothicRealmGeometry04OB.WildTree04OC(context["root"],context["leaf"],item,true);
            else if(item.Kind==2)GothicRealmGeometry04OB.Rock(rock,p,item.Size,item.Seed);
            else GothicRealmGeometry04OB.Crystal(mineral,p,item.Size,.07f,item.Seed);
        }
        foreach(RealmRock04OB item in world.Landscape04OB)
            if(item.Position.x>=-145 && item.Position.x<=105 && item.Position.z>=-45 && item.Position.z<=155)
            {GothicRealmGeometry04OB.Rock(rock,item.Position,item.Size,item.Seed);for(int k=0;k<3;k++)GothicRealmGeometry04OB.Crystal(mineral,item.Position+new Vector3((k-1)*2,0,k*7-8),new Vector3(3.2f,12+k*3,3.5f),(k-1)*.10f,item.Seed+k);}
        MeshBuilder ground=new MeshBuilder(1),routeSurface=new MeshBuilder(1);context["ground"]=ground;context["paving"]=routeSurface;
        ground.Box(0,new Vector3(-20,-.15f,55),new Vector3(250,.3f,210),UVMode.World);
        using(StreamWriter f=new StreamWriter(Path.Combine(path,"parcels.csv")))
        {
            f.WriteLine("seed,family,x0,z0,x1,z1,height");
            foreach(Block block in world.Blocks)foreach(Lot lot in block.Lots)foreach(Building building in lot.Buildings)
            {
                RectF rect=building.Footprint;if(!rect.Overlaps(new RectF(-145,-45,105,155),0))continue;
                f.WriteLine(building.Seed+","+building.GothicFamily04OB+","+rect.X0+","+rect.Z0+","+rect.X1+","+rect.Z1+","+building.Height);
                routeSurface.Box(0,rect.Center(.001f),new Vector3(rect.Width+1,.302f,rect.Depth+1),UVMode.World);
            }
        }
        using(StreamWriter f=new StreamWriter(Path.Combine(path,"routes.csv")))
        {
            f.WriteLine("id,width,ax,ay,az,bx,by,bz");
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)
            {
                if(route.Id.Contains("grotto")||route.Id.Contains("shardwood")||route.Id.Contains("azure")||route.Id.Contains("forge-scar"))foreach(Vector3 point in route.Points)
                {
                    Vector3 p=point+Vector3.right*(route.Width*.5f+1);
                    if(p.x>=-145&&p.x<=105&&p.z>=-45&&p.z<=155&&!world.IsInsideBuilding(p,1.5f))GothicRealmGeometry04OB.Crystal(mineral,p,new Vector3(.8f,1.8f,.8f),.03f,(int)point.z);
                }
                if(route.Id.StartsWith("04oh/h3/")||route.Id.StartsWith("04oe/"))continue;
                for(int i=1;i<route.Points.Count;i++)
                {
                    Vector3 a=route.Points[i-1],b=route.Points[i];
                    if(Mathf.Min(a.x,b.x)>105||Mathf.Max(a.x,b.x)<-145||Mathf.Min(a.z,b.z)>155||Mathf.Max(a.z,b.z)<-45)continue;
                    f.WriteLine(route.Id+","+route.Width+","+a.x+","+a.y+","+a.z+","+b.x+","+b.y+","+b.z);
                    Vector3 side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized)*route.Width*.5f;
                    Vector3[] points={a-side,a+side,b+side,b-side};Vector3[] box=new Vector3[8];
                    for(int j=0;j<4;j++){points[j].y=CityLayout.CurbHeight+.014f;box[j]=points[j]-Vector3.up*.24f;box[j+4]=points[j];}
                    GothicRealmGeometry04OB.Hex(routeSurface,box);
                }
            }
        }
        List<RectF> openings=new List<RectF>();
        using(StreamWriter f=new StreamWriter(Path.Combine(path,"openings.csv")))
        {
            f.WriteLine("x0,z0,x1,z1");foreach(WorldAperture opening in WorldApertures.All)
            {RectF rect=opening.Rect;openings.Add(rect);f.WriteLine(rect.X0+","+rect.Z0+","+rect.X1+","+rect.Z1);}
        }
        float h;List<HeartStreetPieceWR02D> routes=HeartPlanFR01.KingdomStreetPlanWR02G(world.RealmRoutes04OB,out h);
        MeshBuilder flags=new MeshBuilder(1);context["flagstone"]=flags;
        foreach(KingdomStreetStoneWR02G piece in HeartPlanFR01.KingdomStreetStonesWR02G(routes,openings))
        for(int i=1;i+1<piece.Points.Count;i++)
        {Vector2 a=piece.Points[0],b=piece.Points[i+1],c=piece.Points[i];flags.Triangle(0,new Vector3(a.x,h+.021f,a.y),new Vector3(b.x,h+.021f,b.y),new Vector3(c.x,h+.021f,c.y),Vector3.up,a*.5f,b*.5f,c*.5f);}
        AppendLandmarkRooms(venues,context);
        using(BinaryWriter f=new BinaryWriter(File.Create(Path.Combine(path,"circuit.bin"))))
        {
            f.Write(0x53463035);f.Write(context.Count);
            foreach(KeyValuePair<string,MeshBuilder> part in context)
            {
                MeshBuilder mesh=part.Value;f.Write(part.Key);f.Write(mesh.VertexCount);
                for(int i=0;i<mesh.VertexCount;i++){Vector3 p=mesh.Vertices[i],n=mesh.Normals[i];Vector2 uv=mesh.UVs[i];
                    f.Write(p.x);f.Write(p.y);f.Write(p.z);f.Write(n.x);f.Write(n.y);f.Write(n.z);f.Write(uv.x);f.Write(uv.y);}
                IReadOnlyList<int> indices=mesh.Triangles(0);f.Write(indices.Count);foreach(int index in indices)f.Write(index);
            }
        }
        Console.WriteLine("Bounded survey written. Source meshes, no Unity execution.");
    }
}
