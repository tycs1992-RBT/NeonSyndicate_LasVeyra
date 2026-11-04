// Exercise the real placement safety source with small non-Unity substitutes. No scene or physics is simulated.
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x=x; this.y=y; }
        public float sqrMagnitude { get { return x*x+y*y; } }
        public static Vector2 operator +(Vector2 a,Vector2 b) { return new Vector2(a.x+b.x,a.y+b.y); }
        public static Vector2 operator -(Vector2 a,Vector2 b) { return new Vector2(a.x-b.x,a.y-b.y); }
        public static Vector2 operator *(Vector2 a,float b) { return new Vector2(a.x*b,a.y*b); }
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 zero { get { return new Vector3(); } }
        public static Vector3 operator +(Vector3 a,Vector3 b) { return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z); }
    }
    public static class Mathf { public static float Max(float a,float b) { return Math.Max(a,b); } }
}
namespace NeonSyndicate
{
    using UnityEngine;
    public class WorldRouteGuardH8S { public Vector3 A,B; public float Radius; public string Label; }
    public static class WorldRouteH8S { public const float LegHalfWidth=3f; }
    public static class LiveWorldPlannerH8S { public const float GuardRadius=100f; }
    public class WalkLink { public int A,B; }
    public class WalkNode { public Vector3 Pos; }
    public class RealmRoute04OB { public float Width; public string Id; public List<Vector3> Points=new List<Vector3>(); }
    public class CityLayout
    {
        public List<WalkLink> WalkLinks=new List<WalkLink>();
        public List<WalkNode> WalkNodes=new List<WalkNode>();
        public List<RealmRoute04OB> RealmRoutes04OB=new List<RealmRoute04OB>();
        public bool IsInsideBuilding(Vector3 p,float margin) { return false; }
        public object RoadAt(Vector3 p,float margin) { return null; }
    }
    public class VenueDefinition { public string Id; }
    public class VenueBinding04C { public object Building; public VenueDefinition Definition; public Vector3 Entrance,Outside; }
    public struct Rect { public float X0,Z0,X1,Z1; }
    public class WorldAperture { public Rect Rect; public string Id; }
    public struct PlanRectH8S
    {
        public Vector2 Min,Max; public string Label;
        public PlanRectH8S(Vector2 a,Vector2 b,string label)
        { Min=new Vector2(Math.Min(a.x,b.x),Math.Min(a.y,b.y)); Max=new Vector2(Math.Max(a.x,b.x),Math.Max(a.y,b.y)); Label=label; }
        public PlanRectH8S Expanded(float n) { return new PlanRectH8S(Min-new Vector2(n,n),Max+new Vector2(n,n),Label); }
    }
    public static class GalleryPlanH8S
    {
        public static float RectDistance(PlanRectH8S r,Vector2 p)
        { float x=Math.Max(Math.Max(r.Min.x-p.x,0f),p.x-r.Max.x),y=Math.Max(Math.Max(r.Min.y-p.y,0f),p.y-r.Max.y);return (float)Math.Sqrt(x*x+y*y); }
        public static bool Overlaps(PlanRectH8S a,PlanRectH8S b)
        { return a.Min.x<b.Max.x && b.Min.x<a.Max.x && a.Min.y<b.Max.y && b.Min.y<a.Max.y; }
        // Tests below do not create corridor segments; runtime uses the production geometry implementation.
        public static float PointSegmentDistance(Vector2 p,Vector2 a,Vector2 b) { throw new NotSupportedException(); }
        public static float SegmentRectDistance(Vector2 a,Vector2 b,PlanRectH8S r) { throw new NotSupportedException(); }
    }
    public class QuestDef { public object Requires; public string Giver,Id; public float GiverOffsetX,GiverOffsetZ; public List<QuestPhase> Phases; }
    public class QuestPhase { public List<QuestStep> Steps; }
    public class QuestStep { public string At,To; public float OffsetX,OffsetZ,Radius,ToOffsetX,ToOffsetZ,ToRadius; }
    public static class QuestLogic { public static bool WorldMatches(object o,int r) { return true; } }
    public static class CampaignRules04E
    {
        public const string UnderPrefix="Under:";
        public static bool IsPlaceToken(string text) { return text.StartsWith(UnderPrefix) || text.StartsWith("Venue:"); }
    }
    public static class QuestPlaces
    {
        public delegate bool TokenResolve(string token,out Vector3 point);
        public const string DistrictPrefix="District:";
        public static bool InBossArena(CityLayout l,Vector3 p,float n) { return false; }
        public static bool GiverPosition(CityLayout l,string s,float x,float z,out Vector3 p) { p=Vector3.zero;return false; }
        public static bool Resolve(CityLayout l,string s,float x,float z,Vector3 a,Vector3 b,bool c,Vector3 d,int e,out Vector3 p) { p=Vector3.zero;return false; }
    }
}
public static class SafetyHarness
{
    static int count;
    static void Check(bool value,string reason) { count++; if(!value) throw new Exception(reason); }
    static bool Tokens(string token,out UnityEngine.Vector3 point)
    { point=new UnityEngine.Vector3(0,token.StartsWith("Under:")?-26f:0f,0);return true; }
    static string Conflict(NeonSyndicate.LiveWorldRulesH8S r,float low,float high,float x=0)
    { return NeonSyndicate.LiveWorldSafetyH8S.Conflict(new UnityEngine.Vector3(x-1,low,-1),new UnityEngine.Vector3(x+1,high,1),low,r); }
    public static int Main()
    {
        try
        {
            var q=new NeonSyndicate.QuestDef { Id="uh05_06_pressure", Phases=new List<NeonSyndicate.QuestPhase> {
                new NeonSyndicate.QuestPhase { Steps=new List<NeonSyndicate.QuestStep> {
                    new NeonSyndicate.QuestStep { At="Under:canals.transit",Radius=16f,To="Venue:forge.exit",ToRadius=2f } } } } };
            var discs=new List<NeonSyndicate.LiveWorldDiscH8S>();
            int missed=NeonSyndicate.LiveWorldSafetyH8S.QuestDiscs(new NeonSyndicate.CityLayout(),new List<NeonSyndicate.QuestDef>{q},10,Tokens,discs);
            Check(missed==0 && discs.Count==2,"Quest places did not resolve");
            Check(discs[0].UndergroundQuest && !discs[1].UndergroundQuest,"Only Under: quest places may use height separation");
            Check(discs[0].Radius==18f && discs[0].At.y==-26f,"Quest radius, margin or underground altitude changed");
            var r=new NeonSyndicate.LiveWorldRulesH8S(); NeonSyndicate.LiveWorldSafetyH8S.AddDiscs(r,new List<NeonSyndicate.LiveWorldDiscH8S>{discs[0]});
            Check(Conflict(r,0f,8f)==null,"A separated surface volume was blocked by the underground quest");
            Check(Conflict(r,-2.5f,8f)!=null,"Touching the protected vertical band must remain blocked");
            Check(Conflict(r,-10f,8f)!=null,"A volume crossing the protected band must remain blocked");
            Check(Conflict(r,-27f,-25f)!=null,"An underground volume at the quest must remain blocked");
            Check(Conflict(r,-27f,-25f,50f)==null,"A distant volume must not be blocked by this disc");
            Check(Conflict(r,-60f,-50f)==null,"A volume fully below the protected band must be separated");
            Check(Conflict(r,-10f,8f).Contains("camera margin"),"Camera margin must remain visible on tall overlapping pieces");
            NeonSyndicate.LiveWorldSafetyH8S.AddDiscs(r,new List<NeonSyndicate.LiveWorldDiscH8S>{discs[1]});
            Check(Conflict(r,0f,8f)!=null,"A surface quest guard must remain protected");
            var door=new NeonSyndicate.LiveWorldRulesH8S(); NeonSyndicate.LiveWorldSafetyH8S.AddDisc(door,new UnityEngine.Vector3(0,-26,0),6f,"door");
            Check(Conflict(door,0f,8f)!=null,"Ordinary surface/door guards must retain planar protection");
            var opening=new NeonSyndicate.LiveWorldRulesH8S();
            NeonSyndicate.LiveWorldSafetyH8S.AddApertures(opening,new List<NeonSyndicate.WorldAperture>{new NeonSyndicate.WorldAperture{Id="stairs",Rect=new NeonSyndicate.Rect{X0=-1,Z0=-1,X1=1,Z1=1}}});
            Check(Conflict(opening,0f,8f)!=null,"Ground opening must remain protected");
            Console.WriteLine("Placement safety: "+count+" checks passed against the real safety source; Unity physics: NOT RUN."); return 0;
        }
        catch(Exception e) { Console.Error.WriteLine(e);return 1; }
    }
}
