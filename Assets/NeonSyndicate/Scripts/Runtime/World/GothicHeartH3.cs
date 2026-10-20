using System;
using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        private Vector3 _porchOriginH3;private float _porchYawH3;
        public bool GothicHeartH3 {get{return Revision04O==CityRevision04O.GothicHeartH3;}}
        private bool OnHeartPorchH3(Vector3 point,float radius)
        {
            if(!GothicHeartH3 || radius>1f)return false;
            Vector3 p=InhabitedCityBindings04C.WorldToLocal(_porchOriginH3,_porchYawH3,point);
            return Mathf.Abs(p.x-10)<1.9f-radius && p.z>=-.05f && p.z<=12.1f && Mathf.Abs(p.y-Mathf.Clamp01(p.z/4)*.5f)<.2f;
        }
        private bool ApplyGothicHeartH3(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyArtRevolutionH2(venues))return false;
            Revision04O=CityRevision04O.GothicHeartH3;
            // Only a new, explicit bank sees the porch geometry. Canonical doors and all old anchors stay put.
            foreach(VenueBinding04C venue in venues)if(venue.Definition.Id=="lofts")
            {
                _porchOriginH3=venue.Origin;_porchYawH3=venue.Yaw;
                RealmRoute04OB porch=new RealmRoute04OB{Id="04oh/h3/home-porch",Width=3.8f};
                foreach(Vector3 local in new[]{new Vector3(10,0,0),new Vector3(10,.5f,4),new Vector3(10,.5f,12)})
                    porch.Points.Add(InhabitedCityBindings04C.LocalToWorld(venue.Origin,venue.Yaw,local));
                RealmRoutes04OB.Add(porch);
                int previous=RealmWalkNode04OB(venue.Outside);
                foreach(Vector3 point in porch.Points){int id=RealmWalkNode04OB(point);AddWalkLink(previous,id,false,-1,false);previous=id;}
            }
            BuildSpatialHashes();return true;
        }
    }
    /// <summary>Original fixed-asset architectural composition. Same venue, door, interior and service owner.</summary>
    public static class GothicHeartH3
    {
        public static void ResetStatics() { }
        public static bool Replaces(string id)
        {return id=="lofts" || id=="forge" || id=="clinic" || id=="dispatch" || id=="bondkeeper";}
        public static void PrepareRoom(VenueExpanded04C room)
        {
            if(!Replaces(room.id))return;
            List<VenueNode04C> nodes=new List<VenueNode04C>();
            foreach(VenueNode04C node in room.nodes)
            {
                // Replace the rectangular street facade, never the real room, floors, stairs, props or source anchors.
                if(node.id.StartsWith("front_jamb_") || node.id.StartsWith("window_parapet_") || node.id.StartsWith("window_lintel_") ||
                    node.id.StartsWith("window_guard_") || node.id.StartsWith("window_mullion_") || node.id.StartsWith("canopy_bracket_") ||
                    node.id.StartsWith("window_sill_") || node.id.StartsWith("front_pier_") || node.id=="door_lintel" ||
                    node.id=="door_canopy" || node.id=="upper_front_center")continue;
                nodes.Add(node);
            }
            room.nodes=nodes.ToArray();
        }
        private static void Add(List<ArtPlacementH2> parts,string model,float x,float y,float z,float yaw,float sx,float sy,float sz)
        {parts.Add(new ArtPlacementH2(model,new Vector3(x,y,z),yaw,new Vector3(sx,sy,sz)));}
        public static List<ArtPlacementH2> ServicePlan(VenueBinding04C venue)
        {
            List<ArtPlacementH2> parts=new List<ArtPlacementH2>();string id=venue.Definition.Id;
            if(!Replaces(id))return parts;
            bool home=id=="lofts",forge=id=="forge",clinic=id=="clinic",keeper=id=="bondkeeper";
            float spring=home?9f:5.5f,roofY=home?10.1f:7.1f;
            // Deep, human-scale door and actual pierced window apertures. All ground doors retain 2.8m clear width.
            Add(parts,forge || id=="dispatch"?"forge_bay":"reliquary_door",0,0,-.75f,0,forge?1.1f:1.0f,1,1);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Add(parts,"tracery_wall",sign*5.15f,0,.15f,0,1.3f,.72f,2.2f);
                if(home)Add(parts,"tracery_wall",sign*5.15f,4.2f,.15f,0,1.3f,.62f,2.2f);
                // Each side wing is a roofed open loggia instead of the old full cuboid mass.
                for(int bay=0;bay<3;bay++)
                {
                    float z=3.3f+bay*9f;
                    Add(parts,"portal_bay",sign*11.5f,0,z,0,.54f,.54f,1.3f);
                    Add(parts,"slate_roof",sign*11.5f,6.9f,z+1,0,.59f,home?.52f:.42f,1.18f);
                    if(!keeper)Add(parts,"flying_buttress",sign*14.0f,0,z+2,sign*90,.65f,.85f,.75f);
                    else Add(parts,"mineral_pier",sign*13.7f,0,z+2,sign*15,.65f,1.7f,.8f);
                }
                Add(parts,"cluster_column",sign*8.1f,0,-.3f,0,.8f,spring/7.16f,.8f);
                if(forge)Add(parts,"mineral_pier",sign*13.2f,0,1.8f,sign*13,.85f,1.7f,.85f);
            }
            // Original occupied rooms get a real vaulted roof with a raised clerestory and deep ribs.
            for(int bay=0;bay<4;bay++)
            {
                float z=2.3f+bay*4.65f;
                Add(parts,"ribbed_vault",0,spring-6.89f*.55f,z,0,1.5f,.55f,1.02f);
                Add(parts,"slate_roof",0,roofY,z,0,1.5f,clinic?.34f:.48f,.61f);
            }
            // A useful side porch overlooking the actual home court. The low asset is a smooth supported ramp.
            if(home)
            {
                Add(parts,"processional_stair",10,0,0,0,1,1,1);
                Add(parts,"gallery_rail",13.7f,.5f,8.2f,90,1.9f,.8f,1);
            }
            if(forge)
            {
                // Offset furnace crest, workshop shelter and a broken mineral crown distinguish the Forge silhouette.
                Add(parts,"forge_bay",11.5f,0,16,90,.95f,1.15f,1.3f);
                Add(parts,"portal_bay",-11.5f,6.8f,20,0,.5f,.6f,.7f);
                Add(parts,"slate_roof",-11.5f,14.4f,21,0,.56f,.6f,.7f);
            }
            if(id=="dispatch")Add(parts,"slate_roof",0,roofY+2.5f,11,90,.6f,.45f,1.7f);
            return parts;
        }
        public static void Structure(MeshBuilder stone,VenueBinding04C venue)
        {
            string id=venue.Definition.Id;if(!Replaces(id))return;
            float width=venue.Width,depth=venue.Depth;
            for(int sign=-1;sign<=1;sign+=2)
            {
                // The old full wings are replaced by traversable floors, boundary walls, piers and open galleries.
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(sign*11.5f,-.14f,depth*.5f),new Vector3(6.8f,.28f,depth));
                for(int bay=0;bay<4;bay++)
                    GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(sign*(width*.5f-.22f),2.7f,2+bay*(depth-4)/3),new Vector3(.44f,5.4f,2.2f));
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(sign*7.7f,2.5f,.12f),new Vector3(.5f,5f,.9f));
            }
            GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(0,3,depth-.3f),new Vector3(width,6,.6f));
            if(id=="lofts")
            {
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(10,.25f,8.1f),new Vector3(6.8f,.5f,8.2f));
                // A genuine support for the existing upper occupied front, preserving two-storey room access.
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(0,6.45f,.2f),new Vector3(2.8f,4.3f,.5f));
            }
            if(venue.Building.RoofPad04OB)
            {
                // Retain the exact supported roof-receipt landing and its ordered collectible coordinates.
                float top=venue.Building.Height;
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(-11.75f,top-.2f,depth*.72f),new Vector3(5,.4f,6));
                for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)
                    GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(-11.75f+a*1.9f,top*.5f,depth*.72f+b*2.4f),new Vector3(.7f,top,.7f));
            }
        }
        private static bool GrowthClear(CityLayout world,Vector3 point)
        {
            if(world.IsInsideBuilding(point,3.2f))return false;
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=MathUtil.Flat(route.Points[i-1]),d=MathUtil.Flat(route.Points[i]-route.Points[i-1]);
                float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(point)-a,d)/d.sqrMagnitude):0;
                if(MathUtil.FlatDistance(point,a+d*t)<route.Width*.5f+2.8f)return false;
            }
            foreach(List<Vector3> receipts in new[]{world.CrystalShardSpots,world.DataChipSpots,world.SpellStoneSpots,world.PickupSpots})
                foreach(Vector3 receipt in receipts)if(MathUtil.FlatDistance(point,receipt)<4)return false;
            return true;
        }
        public static List<ArtPlacementH2> CourtPlan(CityLayout world)
        {
            List<ArtPlacementH2> parts=new List<ArtPlacementH2>();if(world==null || !world.GothicHeartH3)return parts;
            // Open perimeter arcades frame the original court and its many actual crossing paths, not a sealed plaza.
            for(int sign=-1;sign<=1;sign+=2)for(int bay=0;bay<3;bay++)
            {
                float z=-10f+bay*31f;
                Add(parts,"portal_bay",sign*22f,CityLayout.CurbHeight,z,90,.64f,.8f,1.5f);
                Add(parts,"slate_roof",sign*24.8f,CityLayout.CurbHeight+9.5f,z,90,.75f,.55f,.8f);
                Vector3 growth=new Vector3(sign*30.5f,CityLayout.CurbHeight,z+1.2f);
                if(!GrowthClear(world,growth))growth.x+=sign*4;
                // Optional growth never replaces a valid original walking/mission/receipt corridor.
                if(GrowthClear(world,growth))Add(parts,"mineral_pier",growth.x,growth.y,growth.z,sign*11,1.5f,2.6f,1.5f);
            }
            // An open ancestral screen beyond the northern square. Its centre stays free for sanctuary travel.
            for(int sign=-1;sign<=1;sign+=2)
            {
                Add(parts,"broken_apse",sign*33,CityLayout.CurbHeight,sign<0?44:48,sign*70,1.0f,1.9f,1.0f);
                Add(parts,"flying_buttress",sign*26,CityLayout.CurbHeight,61,sign*90,1.2f,1.7f,1.5f);
            }
            return parts;
        }
    }
}
