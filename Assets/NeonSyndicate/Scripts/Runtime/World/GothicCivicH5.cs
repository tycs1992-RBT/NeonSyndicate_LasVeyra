using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class CityLayout
    {
        public bool GothicCivicH5 {get{return Revision04O==CityRevision04O.GothicCivicH5;}}
        private bool ApplyGothicCivicH5(IReadOnlyList<VenueBinding04C> venues)
        {
            if(Revision04O!=CityRevision04O.Legacy04N || !ApplyGothicHeartH3(venues))return false;
            // Roofs and colliding civic structure change. Saved revision-nine positions are never reinterpreted.
            Revision04O=CityRevision04O.GothicCivicH5;return true;
        }
    }
    /// <summary>Two original civic shells in an explicit isolated preview, never duplicate services.</summary>
    public static class GothicCivicH5
    {
        public static void ResetStatics() { }
        public static bool Replaces(string id){return id=="archive" || id=="office";}
        private static void Add(List<ArtPlacementH2> p,string model,Vector3 at,float yaw,Vector3 scale)
        {p.Add(new ArtPlacementH2(model,at,yaw,scale));}
        public static List<ArtPlacementH2> Plan(VenueBinding04C venue)
        {
            List<ArtPlacementH2> parts=new List<ArtPlacementH2>();if(!Replaces(venue.Definition.Id))return parts;
            bool archive=venue.Definition.Id=="archive";float depth=venue.Depth,spring=archive?11.4f:9.4f;
            Add(parts,"reliquary_door",new Vector3(0,0,-.6f),0,new Vector3(1.12f,1.18f,1.35f));
            int bays=archive?7:4;
            for(int bay=0;bay<bays;bay++)
            {
                float z=2.6f+bay*(depth-5.2f)/(bays-1);
                Add(parts,"ribbed_vault",new Vector3(0,spring-6.89f*.9f,z),0,new Vector3(1.48f,.9f,.95f));
                Add(parts,"slate_roof",new Vector3(0,spring+3.4f,z),0,new Vector3(1.56f,archive?.72f:.48f,.85f));
                for(int side=-1;side<=1;side+=2)
                {
                    Add(parts,"cluster_column",new Vector3(side*8.2f,0,z),0,new Vector3(.74f,spring/7.16f,.74f));
                    Add(parts,"tracery_wall",new Vector3(side*7.85f,4.1f,z),90,new Vector3(.85f,archive?1.15f:.78f,1.25f));
                    Add(parts,"portal_bay",new Vector3(side*11.6f,0,z),90,new Vector3(.6f,.68f,.78f));
                    Add(parts,"slate_roof",new Vector3(side*11.6f,8.4f,z),90,new Vector3(.68f,.38f,.82f));
                    if(archive)Add(parts,"flying_buttress",new Vector3(side*13.6f,0,z+1.2f),side*90,new Vector3(.68f,1.3f,.82f));
                }
            }
            if(archive)
            {
                // Paired organ-like mineral spires rise from the real side galleries, visible north of the home court.
                for(int side=-1;side<=1;side+=2)
                {
                    Add(parts,"mineral_pier",new Vector3(side*11.4f,5.7f,depth*.73f),side*8,new Vector3(1.75f,5.2f,1.8f));
                    Add(parts,"broken_apse",new Vector3(side*11.2f,0,depth-5.8f),side*90,new Vector3(.72f,1.8f,.72f));
                }
            }
            else
            {
                // Low asymmetric civic hall and one offset bell lantern, distinct from the Archive's long sanctuary.
                Add(parts,"portal_bay",new Vector3(-11.6f,8.3f,depth*.58f),0,new Vector3(.54f,.83f,.8f));
                Add(parts,"slate_roof",new Vector3(-11.6f,18.7f,depth*.58f),0,new Vector3(.61f,.8f,.94f));
                Add(parts,"mineral_pier",new Vector3(11.9f,0,depth*.7f),17,new Vector3(1.4f,2.7f,1.4f));
            }
            return parts;
        }
        public static void Structure(MeshBuilder stone,VenueBinding04C venue,MeshBuilder mineral=null)
        {
            float depth=venue.Depth;bool archive=venue.Definition.Id=="archive";
            for(int side=-1;side<=1;side+=2)
            {
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(side*11.5f,-.14f,depth*.5f),new Vector3(6.8f,.28f,depth));
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(side*(venue.Width*.5f-.22f),2.8f,depth*.5f),new Vector3(.44f,5.6f,depth));
            }
            if(archive)
            {
                GothicRealmGeometry04OB.ArchiveInteriorH5(stone,depth,mineral);
                for(int side=-1;side<=1;side+=2)GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(side*11.4f,2.85f,depth*.73f),new Vector3(4,5.7f,4));
            }
            else
            {
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(0,3,depth-.3f),new Vector3(venue.Width,6,.6f));
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(-11.6f,7.9f,depth*.58f),new Vector3(6.8f,.8f,7));
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(-11.6f+x*2.8f,3.75f,depth*.58f+z*2.8f),new Vector3(.65f,7.5f,.65f));
            }
            if(venue.Building.RoofPad04OB)
            {
                float top=venue.Building.Height;
                GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(-11.75f,top-.2f,depth*.72f),new Vector3(5,.4f,6));
                for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                    GothicRealmGeometry04OB.Box(stone,Vector3.zero,0,new Vector3(-11.75f+x*1.9f,top*.5f,depth*.72f+z*2.4f),new Vector3(.7f,top,.7f));
            }
        }
    }
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>Preserve original sanctuary gallery, stair, altar and interaction positions in the rebuilt shell.</summary>
        public static void ArchiveInteriorH5(MeshBuilder stone,float depth,MeshBuilder mineral=null)
        {
            SanctuaryGallery(stone,stone);
            Box(stone,Vector3.zero,0,new Vector3(0,-.1f,depth*.5f),new Vector3(16,.2f,depth));
            Box(stone,Vector3.zero,0,new Vector3(0,5,depth-.3f),new Vector3(16,10,.6f));
            for(int side=-1;side<=1;side+=2)Box(stone,Vector3.zero,0,new Vector3(side*7.85f,4.5f,depth*.5f),new Vector3(.3f,9,depth));
            Box(stone,Vector3.zero,0,new Vector3(-5,.7f,28),new Vector3(1.1f,1.4f,.9f));
            Box(stone,Vector3.zero,0,new Vector3(0,1.85f,43),new Vector3(1.3f,1.3f,1));
            Stairs(stone,new Vector3(0,0,36),0,7,1.2f,4.2f,6);
            Box(stone,Vector3.zero,0,new Vector3(0,.6f,43.5f),new Vector3(7,1.2f,7));
            Crystal(mineral??stone,new Vector3(0,1.2f,46),new Vector3(2.8f,12,2),.06f,92);
        }
    }
}
