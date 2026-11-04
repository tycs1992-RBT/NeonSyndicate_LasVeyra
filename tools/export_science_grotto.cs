// Export the exact authored C# meshes. This does not import, render or simulate Unity.
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Reflection;
using NeonSyndicate;
using UnityEngine;
class Export
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
    static int Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);
        CityLayout w=new CityLayout(61724);w.Generate();
        List<VenueBinding04C> v=InhabitedCityBindings04C.Select(w,NeonSyndicate.Tests.InhabitedCityBindings04CTests.ReadCatalog());
        w.ApplyProfile04O(CityRevision04O.GothicCivicH5,v);
        Building cave=HeartPlanFR01.ScienceGrottoOwner(w);
        if(cave==null)throw new Exception("Canonical grotto owner refused");
        Console.WriteLine("Grotto seed "+cave.Seed+" parcel "+cave.Footprint.X0+","+cave.Footprint.Z0+","+cave.Footprint.X1+","+cave.Footprint.Z1);
        ScienceDistrictArt art=HeartPlanFR01.ScienceGrotto(w);Write(args[0],"grotto_added",art.Surfaces);
        Dictionary<string,MeshBuilder> context=new Dictionary<string,MeshBuilder>();
        MeshBuilder stone=new MeshBuilder(1),trim=new MeshBuilder(1),crystal=new MeshBuilder(1);
        GothicRealmGeometry04OB.Family(stone,trim,crystal,cave,true);
        context.Add("basalt",stone);context.Add("iron",trim);context.Add("mineral",crystal);
        MeshBuilder ground=new MeshBuilder(1);ground.Box(0,new Vector3(-109,-.1f,84),new Vector3(45,.2f,45),UVMode.World);context.Add("ground",ground);
        MeshBuilder paving=new MeshBuilder(1);paving.Box(0,new Vector3(-110,.001f,73),new Vector3(27,.302f,19),UVMode.World);context.Add("path",paving);
        using(StreamWriter inventory=new StreamWriter(Path.Combine(args[0],"routes.csv")))
        {
            inventory.WriteLine("id,width,ax,az,bx,bz");
            foreach(RealmRoute04OB route in w.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i];
                if(Mathf.Max(a.x,b.x)<-135||Mathf.Min(a.x,b.x)>-85||Mathf.Max(a.z,b.z)<60||Mathf.Min(a.z,b.z)>108)continue;
                inventory.WriteLine(route.Id+","+route.Width+","+a.x+","+a.z+","+b.x+","+b.z);
                Vector3 side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized)*route.Width*.5f;
                int steps=Mathf.CeilToInt(MathUtil.FlatDistance(a,b)/2);
                for(int n=0;n<steps;n++)
                {
                    Vector3 p=Vector3.Lerp(a,b,n/(float)steps),q=Vector3.Lerp(a,b,(n+1)/(float)steps);
                    if(p.x < -132||p.x > -88||p.z < 64||p.z > 106)continue;
                    Vector3[] top={p-side,p+side,q+side,q-side};Vector3[] cube=new Vector3[8];
                    for(int k=0;k<4;k++){top[k].y=CityLayout.CurbHeight+.014f;cube[k]=top[k]-Vector3.up*.24f;cube[k+4]=top[k];}
                    GothicRealmGeometry04OB.Hex(paving,cube);
                }
            }
        }
        Append(context,HeartPlanFR01.ScienceFrontier(w).Surfaces,Vector3.zero,0);
        Write(args[0],"grotto_before",context);Append(context,art.Surfaces,Vector3.zero,0);Write(args[0],"grotto_after",context);
        return 0;
    }
}
