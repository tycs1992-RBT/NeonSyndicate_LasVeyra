using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        public bool Unbound04OE {get{return Revision04O==CityRevision04O.CrystalUnbound04OE;}}
        public Vector3 OathTerrace04OE {get;private set;}
        public Vector3 StormCavern04OE {get;private set;}
        public Vector3 StormLedge04OE {get;private set;}
        private bool ResolveUnboundAnchor04OE(string token,out Vector3 point)
        {
            point=Vector3.zero;
            if(token=="Realm:oath-terrace")point=OathTerrace04OE;
            else if(token=="Realm:storm-cavern")point=StormCavern04OE;
            else if(token=="Realm:storm-ledge")point=StormLedge04OE;
            else return false;return true;
        }
        private void RouteUnbound04OE(string id,float width,params Vector3[] points)
        {RealmRoute04OB route=new RealmRoute04OB{Id="04oe/"+id,Width=width};route.Points.AddRange(points);RealmRoutes04OB.Add(route);}
        private Vector3[] TerracePlan04OE(Vector3 start)
        {
            // A 3m rise over 24m ramps, a 24m viewing walk and a ground return. Bounded deterministic admission.
            for(int orientation=0;orientation<8;orientation++)for(int reach=42;reach<=130;reach+=8)
            {
                Quaternion yaw=QMath.AY(orientation*45f);
                Vector3 a=start+yaw*(Vector3.left*reach),b=a+yaw*Vector3.back*24f+Vector3.up*3f;
                Vector3 c=b+yaw*Vector3.right*24f,d=c+yaw*Vector3.right*24f-Vector3.up*3f;
                Vector3[] points={a,b,c,d};bool safe=true;
                for(int i=1;i<points.Length;i++)if(!ClearPassage04O(points[i-1],points[i],4.8f)){safe=false;break;}
                if(safe)return points;
            }
            throw new System.InvalidOperationException("04OE terrace admission failed for seed "+Seed+"; earlier banks remain available.");
        }
        private bool OnUnboundWalk04OE(Vector3 point)
        {
            foreach(RealmRoute04OB route in RealmRoutes04OB)if(route.Id.StartsWith("04oe/"))
                for(int i=1;i<route.Points.Count;i++)if(DistanceSpan04OE(point,route.Points[i-1],route.Points[i])<2f)return true;
            return false;
        }
        private bool SharedUnboundSpan04OE(Vector3 point,Vector3 node)
        {
            foreach(RealmRoute04OB route in RealmRoutes04OB)if(route.Id.StartsWith("04oe/"))
                for(int i=1;i<route.Points.Count;i++)if(DistanceSpan04OE(point,route.Points[i-1],route.Points[i])<.3f && DistanceSpan04OE(node,route.Points[i-1],route.Points[i])<.3f)return true;
            return false;
        }
        private static float DistanceSpan04OE(Vector3 point,Vector3 a,Vector3 b)
        {Vector3 d=b-a;float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(point-a,d)/d.sqrMagnitude):0;return Vector3.Distance(point,a+d*t);}
        private bool ApplyCrystalUnbound04OE(IReadOnlyList<VenueBinding04C> venues)
        {
            // Never reinterpret a running revision 4. Only derive from a fresh original layout.
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyCrystalRealms04OD(venues))return false;
            Vector3[] gallery=TerracePlan04OE(RealmChallenge04OC+Vector3.back*18f);
            Vector3[] ridge=TerracePlan04OE(AzureGlade04OC+Vector3.forward*18f);
            Revision04O=CityRevision04O.CrystalUnbound04OE;
            RouteUnbound04OE("oath-terrace",5f,gallery);OathTerrace04OE=(gallery[1]+gallery[2])*.5f;
            RouteUnbound04OE("oath-access",4f,RegionalPath04OD(RealmChallenge04OC+Vector3.back*18f,gallery[0]));
            RouteUnbound04OE("oath-return-access",4f,RegionalPath04OD(gallery[3],RealmChallenge04OC+Vector3.back*18f));
            RouteUnbound04OE("storm-ledge",5f,ridge);StormLedge04OE=(ridge[1]+ridge[2])*.5f;
            RouteUnbound04OE("storm-access",4f,RegionalPath04OD(AzureGlade04OC+Vector3.forward*18f,ridge[0]));
            RouteUnbound04OE("storm-return-access",4f,RegionalPath04OD(ridge[3],AzureGlade04OC+Vector3.forward*18f));
            Vector3 entrance=Vector3.zero,end=Vector3.zero;bool admitted=false;
            for(int radius=20;radius<=84 && !admitted;radius+=8)for(int direction=0;direction<8;direction++)
            {
                Vector3 candidate=ridge[0]+MathUtil.DirFromYaw(direction*45f)*radius;
                Vector3 far=candidate+Vector3.forward*16f;
                if(!ClearPassage04O(candidate-Vector3.forward*8f,far,7f) || IsInsideBuilding((candidate+far)*.5f,12f))continue;
                entrance=candidate;end=far;admitted=true;break;
            }
            if(!admitted)throw new System.InvalidOperationException("04OE cavern admission failed for seed "+Seed+"; no existing save was moved.");
            RouteUnbound04OE("storm-cave-access",4f,RegionalPath04OD(ridge[0],entrance-Vector3.forward*8f));
            RouteUnbound04OE("storm-cavern",6f,entrance-Vector3.forward*8f,entrance,end);StormCavern04OE=end;
            WildScenery04OC.RemoveAll(s=>IntersectsWildRoute04OC(new RectF(s.Position.x-s.Size.x*.55f,s.Position.z-s.Size.z*.55f,s.Position.x+s.Size.x*.55f,s.Position.z+s.Size.z*.55f),2f));
            BuildSpatialHashes();ReauthorWalks04OB(venues);BuildSpatialHashes();return true;
        }
    }
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>Walkable closed ramp prisms. Exact render triangles also feed original chunk collision.</summary>
        public static void WalkSpan04OE(MeshBuilder mesh,Vector3 from,Vector3 to,float width)
        {
            Vector3 direction=MathUtil.Flat(to-from);if(direction.sqrMagnitude<.001f)return;
            Vector3 side=MathUtil.RightOf(direction.normalized)*width*.5f,down=Vector3.down*.32f;
            Vector3 a=from-side,b=to-side,c=to+side,d=from+side;
            Vector3 normal=Vector3.Cross(b-a,d-a).normalized;
            Vector2 zero=Vector2.zero,one=Vector2.one;
            mesh.Quad(0,a,b,c,d,normal,zero,one,zero,one);
            mesh.Quad(0,d+down,c+down,b+down,a+down,-normal,zero,one,zero,one);
            mesh.Quad(0,a+down,b+down,b,a,-side.normalized,zero,one,zero,one);
            mesh.Quad(0,c+down,d+down,d,c,side.normalized,zero,one,zero,one);
            mesh.Quad(0,d+down,a+down,a,d,-direction.normalized,zero,one,zero,one);
            mesh.Quad(0,b+down,c+down,c,b,direction.normalized,zero,one,zero,one);
        }
        public static void UnboundRoute04OE(MeshBuilder stone,MeshBuilder mineral,RealmRoute04OB route)
        {
            for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i];WalkSpan04OE(stone,a+Vector3.up*.02f,b+Vector3.up*.02f,route.Width);
                Vector3 side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized)*(route.Width*.5f+.25f);
                if(a.y>1f || b.y>1f)
                {
                    // Continuous low parapets have no end barrier and keep the 5m clear corridor.
                    Beam(stone,a+side+Vector3.up*.5f,b+side+Vector3.up*.5f,.35f,1f);
                    Beam(stone,a-side+Vector3.up*.5f,b-side+Vector3.up*.5f,.35f,1f);
                }
            }
            if(route.Id.Contains("access")) return;
            if(route.Id.Contains("cavern"))
            {
                Vector3 end=route.Points[route.Points.Count-1],mouth=end-Vector3.forward*16;
                Vault(stone,mouth,0,9f,19f,5f,3f,1.4f,10);
                for(int side=-1;side<=1;side+=2)
                {
                    Rock(stone,mouth+new Vector3(side*7f,0,9f),new Vector3(4,10,21),601+side);
                    for(int i=0;i<3;i++)Crystal(mineral,mouth+new Vector3(side*5f,0,3+i*6),new Vector3(1.7f,4+i,2),side*.2f,620+i+side);
                }
                Rock(stone,end+Vector3.forward*5f,new Vector3(10,8,4),641);
            }
            else
            {
                Vector3 a=route.Points[1],b=route.Points[2],mid=(a+b)*.5f;
                // Gallery ribs stand above the raised floor; their visible bore matches the walk corridor.
                Vector3 forward=MathUtil.Flat(b-a).normalized;
                float yaw=MathUtil.YawOf(forward);
                Vault(stone,a,yaw,7f,Vector3.Distance(a,b),5f,2.8f,.65f,10);
                for(int i=0;i<4;i++)Arch(mineral,Vector3.Lerp(a,b,i/3f),yaw,7f,5f,2.8f,.18f,.3f,10);
                Crystal(mineral,mid+MathUtil.RightOf(forward)*5f-Vector3.up*3f,new Vector3(2.4f,11f,2.8f),.1f,route.Id.Contains("oath")?701:702);
            }
        }
    }
    public sealed partial class CityBuilder
    {
        private void BuildUnbound04OE()
        {
            if(!L.Unbound04OE)return;
            foreach(RealmRoute04OB route in L.RealmRoutes04OB)
            {
                if(!route.Id.StartsWith("04oe/"))continue;
                MeshBuilder stone=new MeshBuilder(1),mineral=new MeshBuilder(1);
                GothicRealmGeometry04OB.UnboundRoute04OE(stone,mineral,route);
                MeshBuilder root=new MeshBuilder(1);
                GothicRealmGeometry04OB.CinematicRoute04OF(stone,mineral,root,route);
                if(root.VertexCount>0)EmitWildMesh04OC(route.Points[0],root,GothicRealmMaterials04OB.For("timber"),true);
                EmitWildMesh04OC(route.Points[0],stone,GothicRealmMaterials04OB.For("stone"),true);
                EmitWildMesh04OC(route.Points[0],mineral,GothicRealmMaterials04OB.For(route.Id.Contains("oath")?"crystal":"teal"),true);
            }
        }
    }
}
