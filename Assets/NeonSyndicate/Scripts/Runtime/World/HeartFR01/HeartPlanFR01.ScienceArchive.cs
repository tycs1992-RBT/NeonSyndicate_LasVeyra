using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class HeartPlanFR01
    {
        /// <summary>New civic envelope around the exact occupied H5 sanctuary, including its original dais and roof receipt.</summary>
        private static ScienceDistrictArt ScienceArchive(MeshBuilder structure,MeshBuilder porch)
        {
            ScienceDistrictArt art=new ScienceDistrictArt();
            art.Surface("masonry").Append(structure);art.Surface("masonry").Append(porch);
            // Three stepped shells form a long reading hall. Their frames stand outside the retained room walls.
            for(int bay=0;bay<3;bay++)
            {
                float z=7+bay*16,spring=10.5f+bay*1.2f;
                ScienceRoof(art,new Vector3(0,spring,z),17.2f,18,2.6f);
                for(int end=-1;end<=1;end+=2)
                    ScienceArch(art.Surface("masonry"),new Vector3(0,0,z+end*7.6f),17.2f,spring,2.6f,.48f);
            }
            for(int side=-1;side<=1;side+=2)
            {
                // Lower gallery canopies and separate buttress feet give the hall a readable human scale.
                for(int bay=0;bay<3;bay++)
                {
                    float z=7+bay*16;
                    ScienceRoof(art,new Vector3(side*11.5f,6.7f+bay*.55f,z),6.7f,16,1.15f);
                    for(int end=-1;end<=1;end+=2)
                    {
                        float footZ=z+end*6.8f;
                        ScienceRib(art.Surface("masonry"),new Vector3(side*14.3f,0,footZ),new Vector3(side*14.3f,5.2f,footZ),new Vector3(side*8.6f,10.5f+bay*1.2f,footZ),.42f,.56f);
                        ScienceBox(art.Surface("iron"),new Vector3(side*14.3f,.3f,footZ),new Vector3(1,.6f,1.2f));
                    }
                }
                // A deep public portico ends at the existing door. No new door, service or interaction owner.
                SciencePlanter(art,new Vector3(side*6.5f,0,-3.5f),4.8f);
                ScienceLamp(art,new Vector3(side*5.4f,2.9f,-7.6f));
                ScienceBox(art.Surface("masonry"),new Vector3(side*9,.4f,-2.5f),new Vector3(3,.8f,.85f));
            }
            for(int frame=0;frame<3;frame++)
                    ScienceArch(art.Surface("masonry"),new Vector3(0,0,-7.2f+frame*3.2f),11,5,2.8f,.42f);
            ScienceRoof(art,new Vector3(0,6.9f,-4),12.6f,8.6f,1.4f);
            // Civic power spine: a real socket on the retained eastern plinth, with grounded conductors.
            ScienceCore(art,new Vector3(11.4f,5.7f,35.04f),1.15f,10.2f);
            for(int side=-1;side<=1;side+=2)
                ScienceRib(art.Surface("iron"),new Vector3(11.4f+side*1.7f,0,35.04f),new Vector3(11.4f+side*1.7f,7,35.04f),new Vector3(11.4f+side*.5f,15.5f,35.04f),.18f,.24f);
            ScienceCore(art,new Vector3(-11.4f,5.7f,35.04f),.85f,4.2f);
            return art;
        }

        /// <summary>Public loggias alongside existing routes. Refuse a site that overlaps another building or receipt.</summary>
        public static void ScienceApproach(ScienceDistrictArt art,CityLayout world)
        {
            ScienceDistrictArt frontier=ScienceFrontier(world);
            foreach(KeyValuePair<string,MeshBuilder> part in frontier.Surfaces)art.Surface(part.Key).Append(part.Value);
            ScienceDistrictArt grotto=ScienceGrotto(world);
            foreach(KeyValuePair<string,MeshBuilder> part in grotto.Surfaces)art.Surface(part.Key).Append(part.Value);
            // Forge junction, Archive approach, then the eastern edge of the existing woodland connection.
            foreach(Vector3 site in ScienceApproachSites())
            {
                if(world!=null && !ScienceApproachClear(world,site))continue;
                ScienceLoggia(art,site);
            }
        }

        public static Vector3[] ScienceApproachSites()
        { return new[]{new Vector3(42,CityLayout.CurbHeight,55),new Vector3(23,CityLayout.CurbHeight,85),new Vector3(-24,CityLayout.CurbHeight,100)}; }

        private static bool ScienceApproachClear(CityLayout world,Vector3 site)
        {
            if(world.IsInsideBuilding(site,9.5f))return false;
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=MathUtil.Flat(route.Points[i-1]),d=MathUtil.Flat(route.Points[i]-route.Points[i-1]);
                float t=d.sqrMagnitude>.001f?Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(site)-a,d)/d.sqrMagnitude):0;
                // Whole fixture footprint plus a pedestrian margin, not just the pivot.
                Vector3 separation=site-(a+d*t);
                if(Mathf.Abs(separation.x)<4.7f+route.Width*.5f && Mathf.Abs(separation.z)<8.3f+route.Width*.5f)return false;
            }
            foreach(List<Vector3> receipts in new[]{world.CrystalShardSpots,world.DataChipSpots,world.SpellStoneSpots,world.PickupSpots})
                foreach(Vector3 receipt in receipts)if(MathUtil.FlatDistance(site,receipt)<10.3f)return false;
            return true;
        }

        private static void ScienceLoggia(ScienceDistrictArt art,Vector3 site)
        {
            ScienceRoof(art,site+Vector3.up*5.4f,7.8f,15,1.7f);
            for(int end=-1;end<=1;end+=2)
                ScienceArch(art.Surface("masonry"),site+Vector3.forward*end*6.4f,6.8f,4.2f,1.4f,.4f);
            // Open, ground-level gathering bay. Seating and planted edges stay out of its central passage.
            for(int side=-1;side<=1;side+=2)
            {
                SciencePlanter(art,site+new Vector3(side*3.15f,0,2.8f),4.2f);
                ScienceBox(art.Surface("masonry"),site+new Vector3(side*2.9f,.42f,-2.2f),new Vector3(.7f,.84f,2.8f));
                ScienceLamp(art,site+new Vector3(side*3.4f,2.9f,-6.4f));
            }
            // Low utility core is bracketed into an end pier, outside the walking bay.
            ScienceCore(art,site+new Vector3(3.35f,0,6.35f),.42f,2.4f);
        }
    }
}
