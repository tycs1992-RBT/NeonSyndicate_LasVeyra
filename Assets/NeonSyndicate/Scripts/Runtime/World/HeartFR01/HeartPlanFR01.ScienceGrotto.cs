using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class HeartPlanFR01
    {
        /// <summary>Entrance retrofit of the existing walk-through grotto. All positions are world metres.</summary>
        public static ScienceDistrictArt ScienceGrotto(CityLayout world)
        {
            ScienceDistrictArt art = new ScienceDistrictArt();
            Building cave = ScienceGrottoOwner(world);
            if (cave == null) return art;
            Vector3 centre = world.RealmGrotto04OB;
            // The western approach has several real crossing routes. Whole rock footprints are admitted separately.
            GrottoOutcrop(art,world,new Vector3(-109,0,97),new Vector3(7,7.2f,4.8f),408);
            GrottoOutcrop(art,world,new Vector3(-101,0,94),new Vector3(3.6f,3.2f,2.4f),421);
            GrottoOutcrop(art,world,new Vector3(-106,0,92),new Vector3(2.4f,1.5f,1.8f),433);
            // The last trees are behind the mineral banks. The mouth and return view remain free of canopy.
            foreach (Vector3 site in new[]{new Vector3(-115,0,100),new Vector3(-103,0,101)})
                if (ScienceFrontierClear(world,site,1.15f,1.15f))
                {
                    Vector3 at=site;at.y=ScienceFrontierGround(world,at);
                    FrontierPine(art,at,site.x < -110 ? 10.8f : 8.2f,(int)-site.x);
                }
            // Banks taper toward the mouth, with an uninterrupted five-metre original route between them.
            for(int i=0;i<5;i++)
            {
                Vector3 at=new Vector3(-99-i*2.4f,0,94.5f-i*.3f);
                if(!ScienceFrontierClear(world,at,.65f,.65f))continue;
                at.y=ScienceFrontierGround(world,at);FrontierFern(art,at,.7f+i*.05f,500+i);
            }

            // This envelope is entirely within the original cave parcel and below its original stone ceiling.
            // Its north edge stops before the return route at z=82.5. Nothing seals either existing mouth.
            if (!ScienceGrottoEnvelopeClear(world,cave)) return art;
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 shoulder=new Vector3(centre.x+side*8.2f,CityLayout.CurbHeight,78.8f);
                GrottoStrata(art,shoulder,new Vector3(3.1f,6.1f+(side<0?1.2f:0),5.4f),540+side);
                // Buttresses carry the mouth and leave a broad interior lane and the quest's four-metre radius.
                ScienceRib(art.Surface("masonry"),new Vector3(centre.x+side*6.8f,CityLayout.CurbHeight,81.2f),
                    new Vector3(centre.x+side*6.9f,4.9f,81.2f),new Vector3(centre.x+side*4.5f,6.4f,81.2f),.42f,.46f);
                ScienceBox(art.Surface("iron"),new Vector3(centre.x+side*6.8f,.48f,81.2f),new Vector3(1.05f,.66f,1.1f));
                // Practical access to the exposed support machinery remains behind each buttress.
                ScienceCore(art,new Vector3(centre.x+side*6.8f,CityLayout.CurbHeight,79.3f),.4f,3.5f);
                ScienceRib(art.Surface("iron"),new Vector3(centre.x+side*6.8f,2.7f,79.3f),
                    new Vector3(centre.x+side*6.5f,5.3f,79.3f),new Vector3(centre.x+side*3.7f,6.5f,79.3f),.10f,.10f);
                ScienceDistrictArt lamp=new ScienceDistrictArt();ScienceLamp(lamp,Vector3.zero);
                FrontierAppend(art,lamp,new Vector3(centre.x+side*6.25f,3.4f,81.45f),180);
                // Two low resting bays flank the existing destination, never pretending to be new interactions.
                ScienceBox(art.Surface("masonry"),new Vector3(centre.x+side*5.8f,.47f,73.8f),new Vector3(.9f,.64f,3.1f));
                ScienceBox(art.Surface("iron"),new Vector3(centre.x+side*6.2f,1.05f,73.8f),new Vector3(.14f,.68f,3.1f));
                ScienceLamp(art,new Vector3(centre.x+side*6.15f,2.55f,76.6f));
                ScienceBox(art.Surface("iron"),new Vector3(centre.x+side*6.5f,2.55f,76.7f),new Vector3(.72f,.16f,.18f));
                // A restrained fixture accents the retained mineral beside Linnet's existing quest destination.
                ScienceCore(art,new Vector3(centre.x+side*6.5f,CityLayout.CurbHeight,70.4f),.27f,2.5f);
            }
            GrottoCollar(art,centre.x,78.5f,82.05f);
            // A second, lighter rib makes the interior depth readable without constructing another cave.
            ScienceArch(art.Surface("iron"),new Vector3(centre.x,CityLayout.CurbHeight,76.8f),13.6f,3.7f,3.15f,.18f);
            GrottoArrivalFloor(art,centre.x);
            return art;
        }

        /// <summary>Bind to the real family-8 shell, destination and route, refusing changed layouts.</summary>
        public static Building ScienceGrottoOwner(CityLayout world)
        {
            if(world==null || !world.GothicCivicH5 || MathUtil.FlatDistance(world.RealmGrotto04OB,new Vector3(-110,0,74))>.01f)return null;
            RealmRoute04OB route=world.RealmRoutes04OB.Find(r=>r.Id=="grotto-through-route");
            RealmRoute04OB connection=world.RealmRoutes04OB.Find(r=>r.Id=="sanctuary-wilds");
            if(route==null || route.Width<5 || route.Points.Count!=5 || connection==null || connection.Points.Count<4 ||
                MathUtil.FlatDistance(route.Points[3],world.RealmGrotto04OB)>.01f ||
                MathUtil.FlatDistance(route.Points[4],new Vector3(-110,0,85))>.01f ||
                MathUtil.FlatDistance(connection.Points[connection.Points.Count-1],route.Points[4])>.01f)return null;
            foreach(Building b in world.RealmBuildings04OB)
                if(b.GothicFamily04OB==8 && b.Seed==world.Seed+900100 &&
                    Mathf.Abs(b.Footprint.X0+123)<.01f && Mathf.Abs(b.Footprint.X1+97)<.01f &&
                    Mathf.Abs(b.Footprint.Z0-64)<.01f && Mathf.Abs(b.Footprint.Z1-82)<.01f)return b;
            return null;
        }

        /// <summary>Only the known hollow owner may overlap the retrofit; other physical and receipt owners veto it.</summary>
        public static bool ScienceGrottoEnvelopeClear(CityLayout world,Building cave)
        {
            if(cave==null)return false;
            RectF area=new RectF(-120.5f,69,-99.5f,82.1f);
            if(WorldApertures.Overlaps(area))return false;
            for(float x=area.X0;x<=area.X1;x+=2)for(float z=area.Z0;z<=area.Z1;z+=2)
                if(ScienceFrontierGround(world,new Vector3(x,0,z))>CityLayout.CurbHeight)return false;
            foreach(Block block in world.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)
                if(!ReferenceEquals(b,cave) && b.Footprint.Overlaps(area,.3f))return false;
            foreach(RoadEdge edge in world.Edges)if(world.EdgeRect(edge).Overlaps(area,.3f))return false;
            foreach(List<Vector3> points in new[]{world.CrystalShardSpots,world.DataChipSpots,world.SpellStoneSpots,world.PickupSpots,world.ParkingSpots})
                foreach(Vector3 p in points)if(area.Contains(p))return false;
            foreach(PropSpot prop in world.Props)if(area.Contains(prop.Pos))return false;
            foreach(SpecialSite site in world.Sites)
                if(new RectF(site.Position.x-site.Radius,site.Position.z-site.Radius,site.Position.x+site.Radius,site.Position.z+site.Radius).Overlaps(area,0))return false;
            // Existing routes inside the bore are expected. No added crossing may run into its side walls.
            foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i];float margin=route.Width*.5f+.1f;
                foreach(RectF wall in new[]{new RectF(-120.5f,70,-115.3f,81.9f),new RectF(-104.7f,70,-99.5f,81.9f)})
                    if(FrontierSegmentBox(a,b,wall,margin))return false;
            }
            return true;
        }

        private static void GrottoOutcrop(ScienceDistrictArt art,CityLayout world,Vector3 at,Vector3 size,int seed)
        {
            if(!ScienceFrontierClear(world,at,size.x*.65f+.25f,size.z*.58f+.3f))return;
            at.y=ScienceFrontierGround(world,at);
            GrottoStrata(art,at,size,seed);
            // Short contained seams, not an energy trail across the ground.
            GothicRealmGeometry04OB.Crystal(art.Surface("slate"),at+new Vector3(.3f,.1f,-size.z*.2f),new Vector3(.6f,size.y*.8f,.7f),.045f,seed);
        }

        private static void GrottoStrata(ScienceDistrictArt art,Vector3 at,Vector3 size,int seed)
        {
            // Closed, unequal overlapping strata form a supported mineral bank with a weathered silhouette.
            for(int layer=0;layer<4;layer++)
            {
                float t=layer/4f;
                Vector3 p=at+new Vector3(Mathf.Sin(seed+layer)*size.x*.035f,-.16f+t*size.y*.66f,Mathf.Cos(seed+layer)*size.z*.035f);
                GothicRealmGeometry04OB.Rock(art.Surface(layer%3==1?"masonry":"basalt"),p,
                    new Vector3(size.x*(1-t*.5f),size.y*(.44f-t*.15f),size.z*(1-t*.42f)),seed+layer*31);
            }
            for(int root=0;root<3;root++)
            {
                Vector3 p=at+new Vector3((root-1)*size.x*.2f,.08f,size.z*.4f);
                ScienceRib(art.Surface("root"),p+Vector3.up*.6f,p+new Vector3(.25f,.18f,.16f),p+new Vector3(.35f,0,.22f),.06f,.06f);
                FrontierFern(art,p,.6f,seed+root);
            }
        }

        private static void GrottoCollar(ScienceDistrictArt art,float x,float back,float front)
        {
            // Two-sided 55 cm shell, with closed end faces, follows the existing hollow bore.
            const int segments=20;
            for(int i=0;i<segments;i++)
            {
                float u=-1+2f*i/segments,v=-1+2f*(i+1)/segments;
                float ya=3.85f+3.2f*(1-u*u),yb=3.85f+3.2f*(1-v*v);
                Vector3 a=new Vector3(x+u*6.8f,ya,back),b=new Vector3(x+v*6.8f,yb,back);
                Vector3 c=new Vector3(b.x,b.y,front),d=new Vector3(a.x,a.y,front),up=Vector3.up*.55f;
                ScienceFace(art.Surface("slate"),a+up,b+up,c+up,d+up,Vector3.up);
                ScienceFace(art.Surface("masonry"),a,b,c,d,Vector3.down);
                ScienceFace(art.Surface("masonry"),d,c,c+up,d+up,Vector3.forward);
                ScienceFace(art.Surface("masonry"),a,b,b+up,a+up,Vector3.back);
                if(i==0)ScienceFace(art.Surface("masonry"),a,d,d+up,a+up,Vector3.left);
                if(i==segments-1)ScienceFace(art.Surface("masonry"),b,c,c+up,b+up,Vector3.right);
            }
            for(int end=0;end<2;end++)
                ScienceArch(art.Surface("masonry"),new Vector3(x,CityLayout.CurbHeight,end==0?back+.45f:front-.45f),13.6f,3.7f,3.2f,.32f);
        }

        private static void GrottoArrivalFloor(ScienceDistrictArt art,float x)
        {
            // Flush supported paving over the retained ground. Both ends taper to the real route height.
            for(int i=0;i<12;i++)
            {
                float z=70+i*1.2f,zn=z+1.2f;
                float a=.164f+Mathf.Min(1,Mathf.Min(i,12-i))*.024f,b=.164f+Mathf.Min(1,Mathf.Min(i+1,11-i))*.024f;
                Vector3[] top={new Vector3(x-4.7f,a,z),new Vector3(x+4.7f,a,z),new Vector3(x+4.7f,b,zn),new Vector3(x-4.7f,b,zn)};
                Vector3[] points=new Vector3[8];for(int n=0;n<4;n++){points[n]=top[n]-Vector3.up*.22f;points[n+4]=top[n];}
                GothicRealmGeometry04OB.Hex(art.Surface(i<3?"basalt":"earth"),points);
            }
            // Fitted, bevel-edged stones finish the supported arrival bed. Recessed seams retain earth between them.
            for(int row=0;row<10;row++)for(int column=0;column<6;column++)
            {
                Vector3 at=new Vector3(x-3.75f+column*1.5f,.188f,71.8f+row*1.12f);
                GrottoFlagstone(art.Surface((row+column)%5==0?"slate":"basalt"),at,.70f,.51f,row*13+column);
            }
            // Low side channels make the return direction readable by shape in daylight.
            for(int side=-1;side<=1;side+=2)
                ScienceBox(art.Surface("iron"),new Vector3(x+side*4.65f,.196f,76.5f),new Vector3(.07f,.015f,10.8f));
        }

        private static void GrottoFlagstone(MeshBuilder mesh,Vector3 at,float halfX,float halfZ,int seed)
        {
            float clip=.09f+(seed%3)*.025f;
            Vector3[] outline={new Vector3(-halfX+clip,0,-halfZ),new Vector3(halfX-clip,0,-halfZ),
                new Vector3(halfX,0,-halfZ+clip),new Vector3(halfX,0,halfZ-clip),
                new Vector3(halfX-clip,0,halfZ),new Vector3(-halfX+clip,0,halfZ),
                new Vector3(-halfX,0,halfZ-clip),new Vector3(-halfX,0,-halfZ+clip)};
            for(int i=0;i<8;i++)
            {
                Vector3 a=at+outline[i],b=at+outline[(i+1)%8];
                Vector3 upperA=a+Vector3.up*.012f,upperB=b+Vector3.up*.012f;
                ScienceFace(mesh,a-Vector3.up*.014f,b-Vector3.up*.014f,upperB,upperA,(a+b)*.5f-at);
                // Counterclockwise outline faces down; reverse the upper fan for a solid walking surface.
                mesh.Triangle(0,at+Vector3.up*.012f,upperB,upperA,Vector3.up,new Vector2(at.x,at.z)*.5f,
                    new Vector2(b.x,b.z)*.5f,new Vector2(a.x,a.z)*.5f);
                mesh.Triangle(0,at-Vector3.up*.014f,a-Vector3.up*.014f,b-Vector3.up*.014f,Vector3.down,new Vector2(at.x,at.z)*.5f,
                    new Vector2(a.x,a.z)*.5f,new Vector2(b.x,b.z)*.5f);
            }
        }
    }
}
