using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        public bool Frontier04OG {get{return Revision04O==CityRevision04O.LivingFrontier04OG || Ascension04OH || ArtRevolutionH2;}}
        public Vector3 OathSeal04OG {get;private set;}
        public Vector3 OathWitness04OG {get;private set;}
        public Vector3 StormCrossing04OG {get;private set;}
        public Vector3 StormSpring04OG {get;private set;}
        public Vector3 StormVein04OG {get;private set;}
        private bool ResolveFrontierAnchor04OG(string token,out Vector3 point)
        {
            point=Vector3.zero;if(!Frontier04OG)return false;
            switch(token)
            {
                case "Realm:oath-seal":point=OathSeal04OG;return true;
                case "Realm:oath-witness":point=OathWitness04OG;return true;
                case "Realm:storm-crossing":point=StormCrossing04OG;return true;
                case "Realm:storm-spring":point=StormSpring04OG;return true;
                case "Realm:storm-vein":point=StormVein04OG;return true;
                default:return false;
            }
        }
        private void FrontierRoute04OG(string id,float width,params Vector3[] points)
        {RealmRoute04OB route=new RealmRoute04OB{Id="04og/"+id,Width=width};route.Points.AddRange(points);RealmRoutes04OB.Add(route);}
        private Vector3[] PlanFrontier04OG(Vector3 start,bool azure)
        {
            for(int reach=24;reach<=152;reach+=8)for(int orientation=0;orientation<8;orientation++)
            {
                Quaternion rotation=QMath.AY(orientation*45);
                Vector3 right=rotation*Vector3.right,forward=rotation*Vector3.forward;
                Vector3 a=start-right*reach,b=a+right*32+Vector3.up*4,c=b+forward*28,d=a+forward*28;
                Vector3 cave=d+forward*(azure?32:16);Vector3[] loop={a,b,c,d,a};bool clear=true;
                // Full perimeter and inner ground are admitted outside existing buildings, roads and walk corridors.
                for(int i=0;i<loop.Length-1 && clear;i++)
                {
                    if(!ClearPassage04O(loop[i],loop[i+1],8f))clear=false;
                    for(int step=0;step<=8 && clear;step++)
                    {
                        Vector3 point=Vector3.Lerp(loop[i],loop[i+1],step/8f);
                        if(IsInsideBuilding(point,8f) || RoadAt(point,7f)!=null)clear=false;
                        foreach(RealmRoute04OB route in RealmRoutes04OB)
                            for(int j=1;j<route.Points.Count;j++)if(DistanceSpan04OE(new Vector3(point.x,route.Points[j].y,point.z),route.Points[j-1],route.Points[j])<route.Width*.5f+8f){clear=false;break;}
                    }
                }
                if(clear && ClearPassage04O(d,cave,8f) && !IsInsideBuilding(cave,10f) && RoadAt(cave,8f)==null)
                {
                    Vector3[] branch={a,b,c,d,cave};
                    try
                    {
                        Vector3[] access=FrontierAccess04OG(start,branch);
                        for(int segment=1;segment<access.Length-1 && clear;segment++)
                        {
                            int samples=Mathf.CeilToInt(Vector3.Distance(access[segment-1],access[segment])/2f);
                            for(int n=0;n<=samples;n++)
                            {Vector3 local=Vector3.Lerp(access[segment-1],access[segment],n/(float)Mathf.Max(1,samples))-a;float x=Vector3.Dot(local,right),z=Vector3.Dot(local,forward);if(x> -5 && x<37 && z> -6 && z<34){clear=false;break;}}
                        }
                        if(clear)return branch;
                    }
                    catch(System.InvalidOperationException){ }
                }
            }
            throw new System.InvalidOperationException("04OG cannot admit a complete physical branch for seed "+Seed+". Existing banks are untouched.");
        }
        private Vector3[] FrontierAccess04OG(Vector3 start,Vector3[] branch)
        {
            Vector3 approach=branch[0]-MathUtil.Flat(branch[3]-branch[0]).normalized*12f;
            Vector3 outside=start-MathUtil.Flat(branch[3]-branch[0]).normalized*12f;
            List<Vector3> path=new List<Vector3>(RegionalPath04OD(start,outside));
            Vector3[] along=RegionalPath04OD(outside,approach);for(int i=1;i<along.Length;i++)path.Add(along[i]);path.Add(branch[0]);return path.ToArray();
        }
        private bool ApplyLivingFrontier04OG(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyCrystalUnbound04OE(venues))return false;
            Vector3 oathStart=Vector3.zero,stormStart=Vector3.zero;
            foreach(RealmRoute04OB route in RealmRoutes04OB)
            {if(route.Id=="04oe/oath-terrace")oathStart=route.Points[0];if(route.Id=="04oe/storm-cavern")stormStart=route.Points[0];}
            Vector3[] oath=PlanFrontier04OG(oathStart,false);
            Revision04O=CityRevision04O.LivingFrontier04OG;
            FrontierRoute04OG("oath-access",5,FrontierAccess04OG(oathStart,oath));
            FrontierRoute04OG("oath-loop",6,oath[0],oath[1],oath[2],oath[3],oath[0]);
            FrontierRoute04OG("oath-witness",6,oath[3],oath[4]);
            Vector3[] storm=PlanFrontier04OG(stormStart,true);
            FrontierRoute04OG("storm-access",5,FrontierAccess04OG(stormStart,storm));
            FrontierRoute04OG("storm-loop",6,storm[0],storm[1],storm[2],storm[3],storm[0]);
            Vector3 mouth=(storm[3]+storm[4])*.5f;
            FrontierRoute04OG("storm-cavern",7,storm[3],mouth,storm[4]);
            Vector3 side=MathUtil.RightOf(MathUtil.Flat(storm[4]-storm[3]).normalized);
            FrontierRoute04OG("storm-vein",6,mouth,mouth+side*14f);
            OathSeal04OG=(oath[1]+oath[2])*.5f;OathWitness04OG=oath[4];StormCrossing04OG=(storm[1]+storm[2])*.5f;
            StormSpring04OG=mouth;StormVein04OG=mouth+side*14f;
            WildScenery04OC.RemoveAll(s=>IntersectsWildRoute04OC(new RectF(s.Position.x-s.Size.x*.55f,s.Position.z-s.Size.z*.55f,s.Position.x+s.Size.x*.55f,s.Position.z+s.Size.z*.55f),3f));
            BuildSpatialHashes();ReauthorWalks04OB(venues);BuildSpatialHashes();return true;
        }
    }
    public static class FrontierGeometry04OG
    {
        public static void ResetStatics() { }
        private static void MineralRoof(MeshBuilder stone,Vector3 a,Vector3 forward,float length)
        {
            Vector3 side=MathUtil.RightOf(forward);float[] xs={-4.5f,-2.2f,.4f,2.8f,4.5f},ys={4.8f,6.3f,7.6f,6.1f,4.8f};
            for(int slice=0;slice<4;slice++)
            {
                Vector3 l=a+side*xs[slice]+Vector3.up*ys[slice],r=a+side*xs[slice+1]+Vector3.up*ys[slice+1];
                Vector3 lEnd=l+forward*length+Vector3.up*(slice%2==0?.25f:-.2f),rEnd=r+forward*length+Vector3.up*(slice%2==0?.25f:-.2f);
                GothicRealmGeometry04OB.Hex(stone,new[]{l,r,rEnd,lEnd,l+Vector3.up*1.2f,r+Vector3.up*1.2f,rEnd+Vector3.up*1.2f,lEnd+Vector3.up*1.2f});
            }
        }
        public static void Build(MeshBuilder stone,MeshBuilder crystal,MeshBuilder roots,RealmRoute04OB route)
        {
            if(route==null || !route.Id.StartsWith("04og/"))return;
            for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i],direction=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(direction);
                GothicRealmGeometry04OB.WalkSpan04OE(stone,a+Vector3.up*.02f,b+Vector3.up*.02f,route.Width);
                if(a.y>1 || b.y>1)
                {
                    for(int sign=-1;sign<=1;sign+=2)
                    {
                        Vector3 edge=side*sign*(route.Width*.5f+.3f);
                        Vector3 inset=(b-a).normalized*3.3f;
                        GothicRealmGeometry04OB.Beam(stone,a+inset+edge+Vector3.up*.55f,b-inset+edge+Vector3.up*.55f,.45f,1.1f);
                        for(int p=1;p<5;p++)
                        {
                            Vector3 top=Vector3.Lerp(a,b,p/5f)+edge;
                            GothicRealmGeometry04OB.Beam(stone,new Vector3(top.x,CityLayout.CurbHeight-.2f,top.z),top,.75f,.9f);
                        }
                    }
                }
            }
            if(route.Id.Contains("access"))return;
            bool azure=route.Id.Contains("storm");
            if(route.Id.EndsWith("loop"))
            {
                Vector3 a=route.Points[1],b=route.Points[2];float yaw=MathUtil.YawOf(MathUtil.Flat(b-a));
                if(!azure)
                {
                    GothicRealmGeometry04OB.Vault(stone,a,yaw,8,Vector3.Distance(a,b),5,3,.8f,12);
                    for(int i=0;i<5;i++)GothicRealmGeometry04OB.Arch(crystal,Vector3.Lerp(a,b,(5+i*4.5f)/28f),yaw,8,5,3,.22f,.35f,12);
                }
                else
                {
                    // Faceted banks border the low path, with a supported high route and open exits at both ends.
                    Vector3 lowA=route.Points[0],lowB=route.Points[3],forward=MathUtil.Flat(lowB-lowA).normalized,side=MathUtil.RightOf(forward);
                    for(int sign=-1;sign<=1;sign+=2)for(int i=0;i<3;i++)
                    {
                        Vector3 at=Vector3.Lerp(lowA,lowB,(i+1f)/4)+side*sign*9;
                        GothicRealmGeometry04OB.Rock(stone,at,new Vector3(7,6+i%2,6),1500+i+sign);
                        GothicRealmGeometry04OB.Beam(roots,at+Vector3.up*5,at+side*sign*4+Vector3.up*7,.55f,.65f);
                        GothicRealmGeometry04OB.Crystal(crystal,at+Vector3.up*3,new Vector3(1.1f,3,1.3f),sign*.2f,1520+i+sign);
                    }
                }
            }
            else
            {
                for(int i=1;i<route.Points.Count;i++)
                {
                    Vector3 a=route.Points[i-1],b=route.Points[i],forward=MathUtil.Flat(b-a).normalized;
                    float yaw=MathUtil.YawOf(forward);float length=Vector3.Distance(a,b);
                    // Each bore stays open at both ends. Side walls leave a ten-metre opening at the T junction.
                    if(azure)MineralRoof(stone,a,forward,length);
                    else GothicRealmGeometry04OB.Vault(stone,a,yaw,8,length,4.8f,3.2f,.9f,12);
                    Vector3 side=MathUtil.RightOf(forward);
                    float insetStart=5f;
                    float insetEnd=route.Id.EndsWith("storm-cavern") && i==1?5f:0;
                    float wallLength=length-insetStart-insetEnd;
                    if(wallLength>0)for(int sign=-1;sign<=1;sign+=2)
                    {
                        if(!azure)GothicRealmGeometry04OB.Box(stone,a,yaw,new Vector3(sign*4.45f,2.4f,insetStart+wallLength*.5f),new Vector3(.9f,4.8f,wallLength));
                        else
                        {
                            int stones=Mathf.CeilToInt(wallLength/4f);
                            for(int rock=0;rock<stones;rock++)
                            {
                                Vector3 at=a+forward*(insetStart+wallLength*(rock+.5f)/stones)+side*sign*7f;
                                GothicRealmGeometry04OB.Rock(stone,at,new Vector3(3,5.6f+rock%2,3),1560+rock+sign);
                            }
                        }
                    }
                    for(int sign=-1;sign<=1;sign+=2)
                    {
                        GothicRealmGeometry04OB.Crystal(crystal,(a+b)*.5f+side*sign*5,new Vector3(1,3,1.2f),sign*.15f,1540+i+sign);
                        if(azure)GothicRealmGeometry04OB.Beam(roots,a+forward*insetStart+side*sign*5,b-forward*insetEnd+side*sign*5+Vector3.up*2,.4f,.45f);
                    }
                }
            }
        }
    }
    public sealed partial class CityBuilder
    {
        private void BuildFrontier04OG()
        {
            if(!L.Frontier04OG)return;
            foreach(RealmRoute04OB route in L.RealmRoutes04OB)
            {
                if(!route.Id.StartsWith("04og/"))continue;
                if(L.ArtRevolutionH2 && BuildArtRouteH2(route))continue;
                MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1),roots=new MeshBuilder(1);
                if(L.Ascension04OH || L.ArtRevolutionH2)AscensionGeometry04OH.Build(stone,crystal,roots,route);
                else FrontierGeometry04OG.Build(stone,crystal,roots,route);
                EmitWildMesh04OC(route.Points[0],stone,GothicRealmMaterials04OB.For("stone"),true);
                EmitWildMesh04OC(route.Points[0],crystal,GothicRealmMaterials04OB.For(route.Id.Contains("storm")?"teal":"crystal"),true);
                if(roots.VertexCount>0)EmitWildMesh04OC(route.Points[0],roots,GothicRealmMaterials04OB.For("timber"),true);
            }
        }
    }
}
