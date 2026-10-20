using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        public bool Ascension04OH { get { return Revision04O==CityRevision04O.VisualAscension04OH; } }
        public int RejectedNeighbors04OH { get; private set; }
        private bool ClearAscensionShortcut04OH(RealmRoute04OB shortcut)
        {
            Vector3 a=MathUtil.Flat(shortcut.Points[0]),b=MathUtil.Flat(shortcut.Points[shortcut.Points.Count-1]);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)));
            // Authored branches already connect through their explicit access paths. Do not invent a short diagonal
            // through their raised floors, nave piers or mineral banks when binding generic street neighbours.
            foreach(RealmRoute04OB route in RealmRoutes04OB)
            {
                if(!route.Id.StartsWith("04og/") || route.Id.Contains("access"))continue;
                for(int span=1;span<route.Points.Count;span++)
                    for(int step=0;step<=steps;step++)
                        if(DistanceSpan04OE(Vector3.Lerp(a,b,step/(float)steps),MathUtil.Flat(route.Points[span-1]),MathUtil.Flat(route.Points[span]))<10f)return false;
            }
            return true;
        }
        private void PruneAscensionShortcuts04OH()
        {
            List<RealmRoute04OB> rejected=new List<RealmRoute04OB>();
            foreach(RealmRoute04OB route in RealmRoutes04OB)
                if(route.Id.StartsWith("neighbor-path/") && !ClearAscensionShortcut04OH(route))rejected.Add(route);
            RejectedNeighbors04OH=rejected.Count;if(rejected.Count==0)return;
            List<WalkLink> retained=new List<WalkLink>();
            foreach(WalkLink link in WalkLinks)
            {
                Vector3 a=WalkNodes[link.A].Pos,b=WalkNodes[link.B].Pos;bool blocked=false;
                foreach(RealmRoute04OB route in rejected)
                {
                    Vector3 from=route.Points[0],to=route.Points[route.Points.Count-1];
                    if(((a-from).sqrMagnitude<.001f && (b-to).sqrMagnitude<.001f) || ((a-to).sqrMagnitude<.001f && (b-from).sqrMagnitude<.001f)){blocked=true;break;}
                }
                if(!blocked)retained.Add(link);
            }
            RealmRoutes04OB.RemoveAll(route=>rejected.Contains(route));
            foreach(WalkNode node in WalkNodes)node.Links.Clear();WalkLinks.Clear();
            foreach(WalkLink link in retained)AddWalkLink(link.A,link.B,link.Crosswalk,link.SignalNode,link.CrossesNSRoad);
            CompactWalkGraph04OB();BuildSpatialHashes();
        }
        private bool ApplyVisualAscension04OH(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyLivingFrontier04OG(venues))return false;
            // The same semantic locations and authored routes, a separate bank for structures and admitted neighbour links.
            Revision04O=CityRevision04O.VisualAscension04OH;
            PruneAscensionShortcuts04OH();
            return true;
        }
    }
    /// <summary>Actual render and collision triangles, built by the original CityBuilder in the explicit H1 preview.</summary>
    public static class AscensionGeometry04OH
    {
        public static void ResetStatics() { }
        private static void NaturalFloor(MeshBuilder stone,Vector3 a,Vector3 b,float width,int seed)
        {
            Vector3 forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);
            int slices=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(a,b)/3));
            for(int i=0;i<slices;i++)
            {
                Vector3 start=Vector3.Lerp(a,b,i/(float)slices)+Vector3.up*.02f,end=Vector3.Lerp(a,b,(i+1f)/slices)+Vector3.up*.02f;
                // Taper shoulders at junctions so a perpendicular high floor cannot clip a ramp approach.
                float w0=width*.5f+Mathf.Sin(Mathf.PI*i/slices)*(.4f+.18f*Mathf.Sin(i*.9f+seed));
                float w1=width*.5f+Mathf.Sin(Mathf.PI*(i+1)/slices)*(.4f+.18f*Mathf.Sin((i+1)*.9f+seed));
                GothicRealmGeometry04OB.Hex(stone,new[]{start-side*w0-Vector3.up*.7f,start+side*w0-Vector3.up*.7f,end+side*w1-Vector3.up*.7f,end-side*w1-Vector3.up*.7f,
                    start-side*w0,start+side*w0,end+side*w1,end-side*w1});
            }
        }
        private static void OathGallery(MeshBuilder stone,MeshBuilder mineral,Vector3 a,Vector3 b)
        {
            Vector3 forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);float yaw=MathUtil.YawOf(forward),length=Vector3.Distance(a,b);
            // A deep two-storey nave, ribbed roof, lateral open arcades and flying buttresses rooted below the gallery.
            GothicRealmGeometry04OB.Vault(stone,a,yaw,11,length,7.5f,5.2f,1.05f,16);
            for(int bay=0;bay<5;bay++)
            {
                Vector3 at=a+forward*(5f+bay*(length-10)/4);
                GothicRealmGeometry04OB.Arch(stone,at,yaw,9.2f,7.2f,5.2f,.45f,.85f,14);
                for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector3 pier=at+side*sign*6.8f;
                    GothicRealmGeometry04OB.Beam(stone,new Vector3(pier.x,CityLayout.CurbHeight-.2f,pier.z),pier+Vector3.up*4.7f,1.15f,1.55f);
                    GothicRealmGeometry04OB.Beam(stone,pier+Vector3.up*3.9f,at+side*sign*4.9f+Vector3.up*8.1f,.7f,.8f);
                    // Short lateral vaults form a genuinely deep arcade behind the columns, without sealing the route.
                    GothicRealmGeometry04OB.Arch(stone,at+side*sign*5.6f,yaw+90,4.2f,3.7f,2.1f,.38f,1.2f,10);
                    GothicRealmGeometry04OB.Crystal(mineral,pier+Vector3.up*4.8f,new Vector3(.8f,3.9f,1.1f),sign*.13f,2400+bay+sign);
                }
            }
            // Supported side bays expand the playable gallery, while keeping their floor away from both ramp mouths.
            for(int sign=-1;sign<=1;sign+=2)
                GothicRealmGeometry04OB.WalkSpan04OE(stone,a+forward*5+side*sign*3.6f+Vector3.up*.02f,b-forward*5+side*sign*3.6f+Vector3.up*.02f,1.2f);
            // Mineral ribs frame the roof lightwell; use structure and silhouette rather than more particles.
            for(int bay=0;bay<3;bay++)GothicRealmGeometry04OB.Arch(mineral,a+forward*(length*(bay+1)/4f),yaw,10.4f,7.5f,5.1f,.16f,.25f,14);
        }
        private static void AzureBanks(MeshBuilder stone,MeshBuilder mineral,MeshBuilder roots,Vector3 a,Vector3 b)
        {
            Vector3 forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);float length=Vector3.Distance(a,b);
            for(int sign=-1;sign<=1;sign+=2)for(int i=0;i<5;i++)
            {
                Vector3 at=a+forward*(7+(length-14)*i/4f)+side*sign*(6.0f+(i%2)*.15f);
                GothicRealmGeometry04OB.Rock(stone,at-Vector3.up*2,new Vector3(3.6f,9.5f+i%3,6.5f),2500+i+sign);
                Vector3 trunk=at+Vector3.up*5;
                GothicRealmGeometry04OB.Beam(roots,at,trunk+side*sign*.9f,.85f,1.05f);
                GothicRealmGeometry04OB.Beam(roots,trunk,trunk+new Vector3(0,3.8f,0)-side*sign*2.6f,.6f,.65f);
                // Buttress roots return to the rock mass, leaving the walking/camera corridor clear.
                GothicRealmGeometry04OB.Beam(roots,at+side*sign*2,at+Vector3.up*4,.5f,.55f);
                GothicRealmGeometry04OB.Crystal(mineral,at+Vector3.up*(1+i%2),new Vector3(1.2f,4.2f+i%2,1.5f),sign*.18f,2520+i+sign);
            }
        }
        public static void Build(MeshBuilder stone,MeshBuilder mineral,MeshBuilder roots,RealmRoute04OB route)
        {
            if(route==null || !route.Id.StartsWith("04og/"))return;
            if(route.Id.Contains("access")){FrontierGeometry04OG.Build(stone,mineral,roots,route);return;}
            bool azure=route.Id.Contains("storm");
            if(!azure)
            {
                // Preserve the grounded path and supported stairs, replace the gallery envelope with the deeper nave.
                if(route.Id.EndsWith("loop"))
                {
                    for(int i=1;i<route.Points.Count;i++)
                    {
                        Vector3 start=route.Points[i-1],end=route.Points[i];
                        GothicRealmGeometry04OB.WalkSpan04OE(stone,start+Vector3.up*.02f,end+Vector3.up*.02f,route.Width);
                        if(Mathf.Abs(end.y-start.y)>1)
                        {
                            Vector3 forward=MathUtil.Flat(end-start).normalized,edge=MathUtil.RightOf(forward)*(route.Width*.5f+.3f);
                            for(int sign=-1;sign<=1;sign+=2)
                                GothicRealmGeometry04OB.Beam(stone,start+forward*3.3f+edge*sign+Vector3.up*.55f,end-forward*3.3f+edge*sign+Vector3.up*.55f,.45f,1.1f);
                        }
                    }
                    Vector3 a=route.Points[1],b=route.Points[2];OathGallery(stone,mineral,a,b);
                    for(int sign=-1;sign<=1;sign+=2)
                    {
                        Vector3 side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized)*sign*4.35f;
                        GothicRealmGeometry04OB.Beam(stone,a+MathUtil.Flat(b-a).normalized*5+side+Vector3.up*.65f,b-MathUtil.Flat(b-a).normalized*5+side+Vector3.up*.65f,.45f,1.2f);
                    }
                }
                else
                {
                    for(int i=1;i<route.Points.Count;i++)GothicRealmGeometry04OB.WalkSpan04OE(stone,route.Points[i-1]+Vector3.up*.02f,route.Points[i]+Vector3.up*.02f,8.4f);
                    OathGallery(stone,mineral,route.Points[0],route.Points[route.Points.Count-1]);
                }
                return;
            }
            // Organic ground is itself colliding rock. Endpoints, slopes and all quest anchors retain their heights.
            for(int i=1;i<route.Points.Count;i++)NaturalFloor(stone,route.Points[i-1],route.Points[i],route.Id.EndsWith("storm-vein")?9f:route.Width,2700+i);
            if(route.Id.EndsWith("loop"))
            {
                AzureBanks(stone,mineral,roots,route.Points[0],route.Points[3]);
                Vector3 a=route.Points[1],b=route.Points[2],side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized);
                // A stone-root bridge over the basin, structurally supported, with broad parapets outside body clearance.
                for(int sign=-1;sign<=1;sign+=2)
                {
                    GothicRealmGeometry04OB.Beam(roots,a+MathUtil.Flat(b-a).normalized*3.3f+side*sign*3.55f,b-MathUtil.Flat(b-a).normalized*3.3f+side*sign*3.55f,.65f,.6f);
                    for(int p=1;p<=3;p++)
                    {Vector3 top=Vector3.Lerp(a,b,p/4f)+side*sign*3.55f;GothicRealmGeometry04OB.Beam(stone,new Vector3(top.x,CityLayout.CurbHeight-.5f,top.z),top,.9f,1.2f);}
                }
            }
            else
            {
                // Interlocking mineral ceiling ribs and asymmetrical hanging growth leave at least 4.8 metres headroom.
                for(int i=1;i<route.Points.Count;i++)
                {
                    Vector3 a=route.Points[i-1],b=route.Points[i],forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);
                    float length=Vector3.Distance(a,b);
                    float[] xs={-5f,-2.8f,.6f,3.3f,5f},ys={5.1f,7.8f,8.6f,7.1f,5.1f};
                    for(int rib=0;rib<4;rib++)
                    {
                        Vector3 left=a+side*xs[rib]+Vector3.up*ys[rib],right=a+side*xs[rib+1]+Vector3.up*ys[rib+1];
                        Vector3 le=left+forward*length+Vector3.up*.25f,re=right+forward*length-Vector3.up*.15f;
                        GothicRealmGeometry04OB.Hex(stone,new[]{left,right,re,le,left+Vector3.up*1.4f,right+Vector3.up*1.4f,re+Vector3.up*1.4f,le+Vector3.up*1.4f});
                    }
                    // Side rock leaves five metres at each mouth and at the spring T junction for a full turning volume.
                    if(length>10)for(int sign=-1;sign<=1;sign+=2)
                        for(float distance=6;distance<length-5;distance+=4)
                            GothicRealmGeometry04OB.Rock(stone,a+forward*distance+side*sign*6.8f,new Vector3(3,6,3.5f),2860+(int)distance+sign);
                    for(int n=0;n<3;n++)
                    {
                        Vector3 at=Vector3.Lerp(a,b,(n+.5f)/3f);
                        for(int sign=-1;sign<=1;sign+=2)
                        {
                            Vector3 basePoint=at+side*sign*5+Vector3.up*4.8f;
                            GothicRealmGeometry04OB.Beam(roots,basePoint,at+side*sign*1.8f+Vector3.up*6.6f,.42f,.5f);
                            GothicRealmGeometry04OB.Crystal(mineral,basePoint,new Vector3(.65f,1.6f,.9f),-sign*.4f,2800+n+sign);
                        }
                    }
                }
            }
        }
    }
}
