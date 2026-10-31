// Runs the real street planner on .NET vector stubs. This is not a Unity rendering or physics test.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude { get { return (float)Math.Sqrt(x*x+y*y); } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x+b.x,a.y+b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x-b.x,a.y-b.y); }
        public static Vector2 operator *(Vector2 a, float b) { return new Vector2(a.x*b,a.y*b); }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
    }
    public static class Mathf { public static float Abs(float a) { return Math.Abs(a); } }
}
namespace NeonSyndicate
{
    public sealed class RealmRoute04OB { public string Id; public float Width; public List<UnityEngine.Vector3> Points = new List<UnityEngine.Vector3>(); }
    public static class MathUtil { public static bool IsFinite(UnityEngine.Vector3 v) { return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z); } }
}

public static class StreetHarness
{
    private static int count;
    private static void Check(bool ok,string message) { count++; if (!ok) throw new InvalidOperationException(message); }
    private static float Cross(UnityEngine.Vector2 a,UnityEngine.Vector2 b,UnityEngine.Vector2 p) { return (b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x); }
    private static bool Inside(List<UnityEngine.Vector2> poly,UnityEngine.Vector2 p,float tolerance)
    { for(int i=0;i<poly.Count;i++) { var a=poly[i];var b=poly[(i+1)%poly.Count];float len=(b-a).magnitude;if(len>1e-5f && Cross(a,b,p)/len<tolerance)return false; } return true; }
    private static bool InsideAnySource(List<NeonSyndicate.RealmRoute04OB> routes,float x,float z,out bool boundary)
    {
        bool found=false; boundary=false;
        foreach(var route in routes) for(int i=1;i<route.Points.Count;i++)
        {
            var a=route.Points[i-1]; var b=route.Points[i]; float dx=b.x-a.x,dz=b.z-a.z;
            float length=(float)Math.Sqrt(dx*dx+dz*dz), along=((x-a.x)*dx+(z-a.z)*dz)/length, side=((x-a.x)*(-dz)+(z-a.z)*dx)/length;
            if(along>=0 && along<=length && Math.Abs(side)<=route.Width*.5f) found=true;
            if(along>=-.01f && along<=length+.01f && Math.Abs(Math.Abs(side)-route.Width*.5f)<.01f) boundary=true;
            if(Math.Abs(side)<=route.Width*.5f+.01f && (Math.Abs(along)<.01f || Math.Abs(along-length)<.01f)) boundary=true;
        }
        return found;
    }
    public static int Main(string[] args)
    {
        try
        {
            var routes=new List<NeonSyndicate.RealmRoute04OB>();
            using(var doc=JsonDocument.Parse(File.ReadAllText(args[0])))
                foreach(var item in doc.RootElement.EnumerateArray())
                {
                    var route=new NeonSyndicate.RealmRoute04OB { Id=item.GetProperty("id").GetString(), Width=item.GetProperty("width").GetSingle() };
                    foreach(var point in item.GetProperty("points").EnumerateArray()) route.Points.Add(new UnityEngine.Vector3(point[0].GetSingle(),.15f,point[1].GetSingle()));
                    routes.Add(route);
                }
            float height; var pieces=NeonSyndicate.HeartPlanFR01.StreetPlanWR02D(routes,out height);
            Check(pieces.Count>0,"No street pieces produced"); Check(Math.Abs(height-.15f)<.0001f,"Wrong street height");
            int borders=0; foreach(var part in pieces)
            {
                Check(part.Points.Count>=3,"Degenerate polygon"); if(part.Edge)borders++;
                for(int i=0;i<part.Points.Count;i++)
                {
                    var a=part.Points[i]; var b=part.Points[(i+1)%part.Points.Count]; var c=part.Points[(i+2)%part.Points.Count];
                    Check(float.IsFinite(a.x)&&float.IsFinite(a.y),"Nonfinite street vertex");
                    Check(Cross(a,b,c)>=-.005f,"Street polygon is not convex and counterclockwise");
                }
            }
            Check(borders>0 && borders<pieces.Count,"No separate route edging");
            var random=new Random(81721);
            for(int i=0;i<24000;i++)
            {
                float x=(float)(-30+random.NextDouble()*115), z=(float)(-36+random.NextDouble()*162);
                bool boundary, expected=InsideAnySource(routes,x,z,out boundary); if(boundary) continue;
                var p=new UnityEngine.Vector2(x,z); int overlaps=0; bool nearBoundary=false;
                foreach(var piece in pieces)
                {
                    bool closed=Inside(piece.Points,p,-.0005f), strict=Inside(piece.Points,p,.0005f);
                    if(strict) overlaps++; else if(closed) nearBoundary=true;
                }
                if(nearBoundary)continue;
                Check(overlaps<=1,"Street areas overlap at "+x+","+z);
                Check((overlaps>0)==expected,"Street left its original supported routes or introduced a gap at "+x+","+z);
            }
            var bad=new List<NeonSyndicate.RealmRoute04OB>(routes); bad.RemoveAt(0);
            Check(NeonSyndicate.HeartPlanFR01.StreetPlanWR02D(bad,out height).Count==0,"Incomplete routes must refuse");
            bad=new List<NeonSyndicate.RealmRoute04OB>(routes);bad.Add(routes[0]);
            Check(NeonSyndicate.HeartPlanFR01.StreetPlanWR02D(bad,out height).Count==0,"Duplicate routes must refuse");
            Console.WriteLine("Street plan: "+count+" geometry, union, overlap and route checks passed; "+pieces.Count+" pieces. Unity: NOT RUN.");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e.ToString());return 1;}
    }
}
