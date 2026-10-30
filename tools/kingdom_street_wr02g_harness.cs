// Executes the actual route and flagstone planners with small math stubs, not the Unity engine.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x; this.y=y; }
        public float magnitude { get { return (float)Math.Sqrt(x*x+y*y); } }
        public static Vector2 operator +(Vector2 a,Vector2 b) { return new Vector2(a.x+b.x,a.y+b.y); }
        public static Vector2 operator -(Vector2 a,Vector2 b) { return new Vector2(a.x-b.x,a.y-b.y); }
        public static Vector2 operator *(Vector2 a,float b) { return new Vector2(a.x*b,a.y*b); }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x; this.y=y; this.z=z; }
    }
    public static class Mathf
    {
        public static float Abs(float a) { return Math.Abs(a); }
        public static float Min(float a,float b) { return Math.Min(a,b); }
        public static float Max(float a,float b) { return Math.Max(a,b); }
        public static int FloorToInt(float a) { return (int)Math.Floor(a); }
    }
}
namespace NeonSyndicate
{
    public struct RectF
    {
        public float X0,Z0,X1,Z1;
        public RectF(float x0,float z0,float x1,float z1) { X0=x0;Z0=z0;X1=x1;Z1=z1; }
    }
    public sealed class RealmRoute04OB
    {
        public string Id; public float Width; public List<UnityEngine.Vector3> Points=new List<UnityEngine.Vector3>();
    }
    public static class MathUtil
    {
        public static bool IsFinite(UnityEngine.Vector3 p)
        { return !float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y) && !float.IsNaN(p.z) && !float.IsInfinity(p.z); }
    }
}
public static class KingdomStreetHarnessWR02G
{
    private static int checks;
    private static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    private static float Cross(UnityEngine.Vector2 a,UnityEngine.Vector2 b,UnityEngine.Vector2 p)
    { return (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x); }
    private static bool Inside(List<UnityEngine.Vector2> polygon,UnityEngine.Vector2 point,float margin)
    {
        for(int i=0;i<polygon.Count;i++)
        {
            UnityEngine.Vector2 a=polygon[i],b=polygon[(i+1)%polygon.Count]; float len=(b-a).magnitude;
            if(len>0.00001f && Cross(a,b,point)/len<margin)return false;
        }
        return true;
    }
    private static bool OnRoute(List<NeonSyndicate.RealmRoute04OB> routes,UnityEngine.Vector2 p)
    {
        foreach(NeonSyndicate.RealmRoute04OB route in routes) for(int i=1;i<route.Points.Count;i++)
        {
            UnityEngine.Vector3 a=route.Points[i-1],b=route.Points[i];float dx=b.x-a.x,dz=b.z-a.z;
            float len=(float)Math.Sqrt(dx*dx+dz*dz),along=((p.x-a.x)*dx+(p.y-a.z)*dz)/len;
            float side=((p.x-a.x)*(-dz)+(p.y-a.z)*dx)/len;
            if(along>=-.003f && along<=len+.003f && Math.Abs(side)<=route.Width*.5f+.003f)return true;
        }
        return false;
    }
    private static bool OverlapsOpening(List<UnityEngine.Vector2> polygon,float x0,float z0,float x1,float z1)
    {
        float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
        foreach(UnityEngine.Vector2 p in polygon)
        {minX=Math.Min(minX,p.x);maxX=Math.Max(maxX,p.x);minZ=Math.Min(minZ,p.y);maxZ=Math.Max(maxZ,p.y);}
        if(maxX<=x0 || minX>=x1 || maxZ<=z0 || minZ>=z1)return false;
        UnityEngine.Vector2[] corners={new UnityEngine.Vector2(x0,z0),new UnityEngine.Vector2(x1,z0),
            new UnityEngine.Vector2(x1,z1),new UnityEngine.Vector2(x0,z1)};
        for(int i=0;i<polygon.Count;i++)
        {
            bool separated=true;
            foreach(UnityEngine.Vector2 corner in corners)
                if(Cross(polygon[i],polygon[(i+1)%polygon.Count],corner)>=-.000001f){separated=false;break;}
            if(separated)return false;
        }
        return true;
    }
    private static bool Nearby(List<NeonSyndicate.KingdomStreetStoneWR02G> stones,float x,float z,float radius)
    {
        foreach(NeonSyndicate.KingdomStreetStoneWR02G stone in stones)foreach(UnityEngine.Vector2 p in stone.Points)
            if((p.x-x)*(p.x-x)+(p.y-z)*(p.y-z)<radius*radius)return true;
        return false;
    }
    public static int Main(string[] args)
    {
        try
        {
            List<NeonSyndicate.RealmRoute04OB> routes=new List<NeonSyndicate.RealmRoute04OB>();
            foreach(string line in File.ReadAllLines(args[0]))
            {
                string[] row=line.Split(';');NeonSyndicate.RealmRoute04OB route=new NeonSyndicate.RealmRoute04OB();
                route.Id=row[0];route.Width=float.Parse(row[1],CultureInfo.InvariantCulture);
                foreach(string point in row[2].Split('|'))
                {string[] p=point.Split(',');route.Points.Add(new UnityEngine.Vector3(float.Parse(p[0],CultureInfo.InvariantCulture),.15f,float.Parse(p[1],CultureInfo.InvariantCulture)));}
                routes.Add(route);
            }
            float y;List<NeonSyndicate.HeartStreetPieceWR02D> union=NeonSyndicate.HeartPlanFR01.KingdomStreetPlanWR02G(routes,out y);
            Check(union.Count>0 && Math.Abs(y-.15f)<.001f,"Route union was refused or height changed.");
            List<NeonSyndicate.RectF> holes=new List<NeonSyndicate.RectF> { new NeonSyndicate.RectF(-1f,36f,1f,40f) };
            List<NeonSyndicate.KingdomStreetStoneWR02G> stones=NeonSyndicate.HeartPlanFR01.KingdomStreetStonesWR02G(union,holes);
            List<NeonSyndicate.HeartStreetPieceWR02D> older=NeonSyndicate.HeartPlanFR01.StreetPlanWR02D(routes,out y);
            Check(!NeonSyndicate.HeartPlanFR01.KingdomLegacyStreetClearWR02G(older,holes),"Older paving cannot cover an opening as a fallback.");
            Check(NeonSyndicate.HeartPlanFR01.KingdomLegacyStreetClearWR02G(older,new List<NeonSyndicate.RectF>{new NeonSyndicate.RectF(300,300,305,305)}),"A distant opening should not refuse the old paving.");
            Check(NeonSyndicate.HeartPlanFR01.KingdomLegacyStreetClearWR02G(older,new List<NeonSyndicate.RectF>()),"No opening should keep the old paving eligible.");
            Check(stones.Count>500 && stones.Count<=18000,"Unreasonable or empty flagstone plan.");
            int trim=0, triangles=0; HashSet<string> chunks=new HashSet<string>();
            foreach(NeonSyndicate.KingdomStreetStoneWR02G stone in stones)
            {
                Check(stone.Points.Count>=3,"Degenerate stone."); if(stone.Trim)trim++;
                float cx=0,cz=0;
                for(int i=0;i<stone.Points.Count;i++)
                {
                    UnityEngine.Vector2 p=stone.Points[i],b=stone.Points[(i+1)%stone.Points.Count],c=stone.Points[(i+2)%stone.Points.Count];
                    Check(OnRoute(routes,p),"Stone vertex outside supported routes.");
                    Check(Cross(p,b,c)>=-.004f,"Stone is not convex and counterclockwise.");cx+=p.x;cz+=p.y;
                    Check(!(p.x>-.999f && p.x<.999f && p.y>36.001f && p.y<39.999f),"Stone touches protected opening.");
                }
                UnityEngine.Vector2 center=new UnityEngine.Vector2(cx/stone.Points.Count,cz/stone.Points.Count);
                chunks.Add(((int)Math.Floor(center.x/48f))+"/"+((int)Math.Floor(center.y/48f)));
                triangles += stone.Points.Count-2;
                Check(!(center.x>-1f && center.x<1f && center.y>36f && center.y<40f),"Stone crosses protected opening.");
                Check(!OverlapsOpening(stone.Points,-1f,36f,1f,40f),"Stone intersects protected opening.");
            }
            Check(trim>0 && trim<stones.Count,"Carved edges or stone field missing.");
            Check(chunks.Count<=96,"Culling budget exceeded.");
            Check(Nearby(stones,0,-30,4),"Home approach missing.");
            Check(Nearby(stones,-6,118,4),"Archive approach missing.");
            Check(Nearby(stones,75,96,4),"Forge approach missing.");
            Check(Nearby(stones,-108,28,4),"Threshold approach missing.");
            Check(Nearby(stones,-110,74,4),"Grotto approach missing.");
            Random random=new Random(81721);
            for(int i=0;i<2500;i++)
            {
                UnityEngine.Vector2 p=new UnityEngine.Vector2((float)(-140+random.NextDouble()*225),(float)(-35+random.NextDouble()*160));
                int overlap=0;foreach(NeonSyndicate.KingdomStreetStoneWR02G stone in stones)if(Inside(stone.Points,p,.0005f))overlap++;
                Check(overlap<=1,"Overlapping flagstone fragments.");
            }
            List<NeonSyndicate.RealmRoute04OB> incomplete=new List<NeonSyndicate.RealmRoute04OB>(routes);incomplete.RemoveAt(0);
            Check(NeonSyndicate.HeartPlanFR01.KingdomStreetPlanWR02G(incomplete,out y).Count==0,"Missing route was accepted.");
            incomplete=new List<NeonSyndicate.RealmRoute04OB>(routes);incomplete.Add(routes[0]);
            Check(NeonSyndicate.HeartPlanFR01.KingdomStreetPlanWR02G(incomplete,out y).Count==0,"Duplicate route was accepted.");
            holes.Add(new NeonSyndicate.RectF(float.NaN,0,1,1));
            Check(NeonSyndicate.HeartPlanFR01.KingdomStreetStonesWR02G(union,holes).Count==0,"Invalid opening was accepted.");
            Console.WriteLine("WR02G route and flagstones: "+checks+" checks, "+union.Count+" route polygons, "+stones.Count+" stones, "+trim+" trim, "+triangles+" triangles, "+chunks.Count+" culling chunks. Unity: NOT RUN.");return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e);return 1; }
    }
}
