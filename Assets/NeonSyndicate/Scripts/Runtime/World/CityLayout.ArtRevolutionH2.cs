using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        public bool ArtRevolutionH2 {get{return Revision04O==CityRevision04O.ArtRevolutionH2;}}
        private bool ApplyArtRevolutionH2(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyVisualAscension04OH(venues))return false;
            Revision04O=CityRevision04O.ArtRevolutionH2;return true;
        }
    }
    public sealed class ArtPlacementH2
    {
        public string Model;public Vector3 Position,Scale;public float Yaw;
        public ArtPlacementH2(string model,Vector3 at,float yaw,Vector3 scale){Model=model;Position=at;Yaw=yaw;Scale=scale;}
    }
    /// <summary>Fixed-asset placement and traversable ground shared by real CityBuilder and deterministic route tests.</summary>
    public static class AscensionCompositionH2
    {
        public static void ResetStatics() { }
        public static List<ArtPlacementH2> Plan(RealmRoute04OB route)
        {
            List<ArtPlacementH2> result=new List<ArtPlacementH2>();
            if(route==null || !route.Id.StartsWith("04og/") || route.Id.Contains("access"))return result;
            bool azure=route.Id.Contains("storm"),loop=route.Id.EndsWith("loop");
            if(!azure)
            {
                Vector3 a=route.Points[loop?1:0],b=route.Points[loop?2:route.Points.Count-1],forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);
                float length=Vector3.Distance(a,b),yaw=MathUtil.YawOf(forward);
                for(float d=3;d<length-2;d+=5.5f)
                {
                    Vector3 at=a+forward*d;
                    result.Add(new ArtPlacementH2("portal_bay",at,yaw,Vector3.one));
                    for(int sign=-1;sign<=1;sign+=2)
                    {
                        result.Add(new ArtPlacementH2("flying_buttress",at+side*sign*7.6f,yaw-sign*90,Vector3.one));
                        result.Add(new ArtPlacementH2("cluster_column",at+side*sign*4.8f,yaw,new Vector3(.7f,1,.7f)));
                        result.Add(new ArtPlacementH2("mineral_pier",at+side*sign*6.4f+Vector3.up*1.8f,yaw+sign*15,new Vector3(.5f,.8f,.5f)));
                    }
                }
                for(float d=2.2f;d<length;d+=4.4f)result.Add(new ArtPlacementH2("ribbed_vault",a+forward*d,yaw,Vector3.one));
                for(float d=4;d<length;d+=7.8f)result.Add(new ArtPlacementH2("slate_roof",a+forward*d+Vector3.up*7.2f,yaw,Vector3.one));
                // Perforated lateral galleries keep the nave open. Do not close the ramp mouths or witness exit.
                for(float d=7;d<length-5;d+=4.2f)for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector3 at=a+forward*d+side*sign*5.55f;
                    result.Add(new ArtPlacementH2("tracery_wall",at,yaw+90,Vector3.one));
                    if(loop)result.Add(new ArtPlacementH2("gallery_rail",a+forward*d+side*sign*4.25f,yaw+90,Vector3.one));
                }
                if(!loop)
                {
                    result.Add(new ArtPlacementH2("broken_apse",b-forward*1.5f,yaw,new Vector3(1,1.1f,1)));
                    // A real accessible alcove behind the witness, with clear entrance, not a second quest/service.
                    result.Add(new ArtPlacementH2("reliquary_door",b-forward*3.0f,yaw,new Vector3(1.3f,1.2f,1)));
                }
            }
            else if(loop)
            {
                Vector3 a=route.Points[0],b=route.Points[3],forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);float yaw=MathUtil.YawOf(forward);
                for(int i=0;i<5;i++)for(int sign=-1;sign<=1;sign+=2)
                {
                    Vector3 at=Vector3.Lerp(a,b,(i+1f)/6)+side*sign*6.8f;
                    result.Add(new ArtPlacementH2("basalt_strata",at-Vector3.up*.3f,yaw+i*17,new Vector3(1.65f,2.6f+i%2*.25f,2.0f)));
                    result.Add(new ArtPlacementH2("root_causeway",at+Vector3.up*7.0f,yaw+sign*13,new Vector3(.7f,1.2f,.75f)));
                    result.Add(new ArtPlacementH2("fern_colony",at-side*sign*1.6f,yaw+i*31,new Vector3(.85f,1.4f,.85f)));
                    result.Add(new ArtPlacementH2("crystal_seam",at-side*sign*.8f+Vector3.up*1.3f,yaw+sign*32,new Vector3(1,1.4f,1)));
                }
                Vector3 bridge=route.Points[1],end=route.Points[2],bridgeForward=MathUtil.Flat(end-bridge).normalized,bridgeSide=MathUtil.RightOf(bridgeForward);float bridgeYaw=MathUtil.YawOf(bridgeForward);
                for(float d=3;d<Vector3.Distance(bridge,end)-3;d+=5.5f)for(int sign=-1;sign<=1;sign+=2)
                    result.Add(new ArtPlacementH2("root_causeway",bridge+bridgeForward*d+bridgeSide*sign*4.3f-Vector3.up*.2f,bridgeYaw,new Vector3(.45f,1,.85f)));
            }
            else
            {
                for(int span=1;span<route.Points.Count;span++)
                {
                    Vector3 a=route.Points[span-1],b=route.Points[span],forward=MathUtil.Flat(b-a).normalized,side=MathUtil.RightOf(forward);float length=Vector3.Distance(a,b),yaw=MathUtil.YawOf(forward);
                    for(float d=5.8f;d<length-5;d+=4.6f)
                    {
                        result.Add(new ArtPlacementH2("cavern_shell",a+forward*d,yaw,Vector3.one));
                        for(int sign=-1;sign<=1;sign+=2)
                        {
                            result.Add(new ArtPlacementH2("crystal_seam",a+forward*d+side*sign*4.7f+Vector3.up*.2f,yaw+sign*10,Vector3.one));
                            result.Add(new ArtPlacementH2("fern_colony",a+forward*d+side*sign*4.8f,yaw,Vector3.one));
                        }
                    }
                }
            }
            return result;
        }
        public static void Ground(MeshBuilder mesh,RealmRoute04OB route)
        {
            bool azure=route.Id.Contains("storm"),gallery=route.Id.EndsWith("oath-loop"),witness=route.Id.EndsWith("oath-witness");
            for(int i=1;i<route.Points.Count;i++)
            {
                float width=witness || route.Id.EndsWith("storm-vein")?9f:gallery && i==2?8.4f:route.Width;
                GothicRealmGeometry04OB.WalkSpan04OE(mesh,route.Points[i-1]+Vector3.up*.02f,route.Points[i]+Vector3.up*.02f,width);
            }
            // Preserve original supported ramps and clear access. New asset collisions remain outside their body lanes.
        }
    }
    public sealed partial class CityBuilder
    {
        private bool BuildArtRouteH2(RealmRoute04OB route)
        {
            if(!AscensionArtH2.Ready())return false;
            if(route.Id.Contains("access"))
            {
                MeshBuilder stone=new MeshBuilder(1),crystal=new MeshBuilder(1),roots=new MeshBuilder(1);AscensionGeometry04OH.Build(stone,crystal,roots,route);
                EmitWildMesh04OC(route.Points[0],stone,AscensionArtH2.Surface(route.Id.Contains("storm")?"basalt":"masonry"),true);return true;
            }
            MeshBuilder ground=new MeshBuilder(1);AscensionCompositionH2.Ground(ground,route);
            EmitWildMesh04OC(route.Points[0],ground,AscensionArtH2.Surface(route.Id.Contains("storm")?"basalt":"masonry"),true);
            Dictionary<string,MeshBuilder> batches=new Dictionary<string,MeshBuilder>();MeshBuilder collision=new MeshBuilder(1);
            foreach(ArtPlacementH2 part in AscensionCompositionH2.Plan(route))
            {
                string role=AscensionArtH2.Role(part.Model);MeshBuilder batch;
                if(!batches.TryGetValue(role,out batch)){batch=new MeshBuilder(1);batches.Add(role,batch);}
                AscensionArtH2.Append(batch,AscensionArtH2.Source(part.Model,Settings.Graphics==GraphicsPreset.Low),part.Position,part.Yaw,part.Scale);
                if(AscensionArtH2.HasCollision(part.Model))AscensionArtH2.Append(collision,AscensionArtH2.Source(part.Model,true),part.Position,part.Yaw,part.Scale);
            }
            foreach(KeyValuePair<string,MeshBuilder> batch in batches)EmitWildMesh04OC(route.Points[0],batch.Value,AscensionArtH2.Surface(batch.Key),false);
            // Uses the original terrain collision batch and its original cleanup/flush. No collider per decorative leaf.
            if(collision.VertexCount>0)
            {
                long key=ChunkKey(route.Points[0].x,route.Points[0].z);MeshBuilder prior;
                if(!_wildCollision04OC.TryGetValue(key,out prior) || prior.VertexCount+collision.VertexCount>220000)
                {if(prior!=null)FlushWildCollision04OC(prior);prior=new MeshBuilder(1);_wildCollision04OC[key]=prior;}
                prior.Append(collision);
            }
            return true;
        }
    }
}
