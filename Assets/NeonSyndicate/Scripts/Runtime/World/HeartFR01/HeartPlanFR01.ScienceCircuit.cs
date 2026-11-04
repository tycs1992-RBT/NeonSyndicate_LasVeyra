using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class HeartPlanFR01
    {
        /// <summary>The southern seam walk and the direct Archive promenade in the canonical civic layout.</summary>
        public static ScienceDistrictArt ScienceCircuit(CityLayout world)
        {
            ScienceDistrictArt art = new ScienceDistrictArt();
            Building cave = ScienceGrottoOwner(world);
            if (cave == null || !CircuitRoutesMatch(world)) return art;

            CircuitWalk(art,world,cave,true);
            CircuitWalk(art,world,null,false);
            // The leeward banks sweep across the view at different heights. The existing route remains the floor.
            CircuitBank(art,world,new Vector3(-119,0,57.5f),12,9.2f,3.7f,3.5f);
            CircuitBank(art,world,new Vector3(-99,0,50),-26,7.4f,3.2f,2.15f);
            CircuitTurningFan(art,world);
            CircuitStation(art,world,new Vector3(-86,0,42));
            CircuitGarden(art,world,new Vector3(-65,0,39));
            return art;
        }

        private static bool CircuitHasRoute(CityLayout world,string id,float width,params Vector2[] expected)
        {
            RealmRoute04OB route = world.RealmRoutes04OB.Find(item => item.Id == id);
            if (route == null || route.Points.Count != expected.Length || Mathf.Abs(route.Width-width)>.01f) return false;
            for (int i=0;i<expected.Length;i++)
            {
                Vector3 point = route.Points[i];
                if (Mathf.Abs(point.y-CityLayout.CurbHeight)>.01f ||
                    Vector2.Distance(new Vector2(point.x,point.z),expected[i])>.02f) return false;
            }
            return true;
        }

        private static bool CircuitRoutesMatch(CityLayout world)
        {
            return CircuitHasRoute(world,"grotto-through-route",5,new Vector2(-108,28),new Vector2(-108,48),
                new Vector2(-110,60),new Vector2(-110,74),new Vector2(-110,85)) &&
                CircuitHasRoute(world,"shardwood-threshold",7,new Vector2(-20,6),new Vector2(-38,30),new Vector2(-76,30),new Vector2(-108,28)) &&
                CircuitHasRoute(world,"sanctuary-approach",8,new Vector2(0,38),new Vector2(12,66),new Vector2(12,92),new Vector2(-6,118)) &&
                CircuitHasRoute(world,"sanctuary-wilds",5,new Vector2(-38,72),new Vector2(-38,92),new Vector2(-76,92),new Vector2(-110,85));
        }

        private static Vector3 CircuitWalkPoint(bool sill,float t,float lane)
        {
            if (sill)
            {
                float half = Mathf.Lerp(2.5f,4.7f,Mathf.SmoothStep(0,1,t));
                return new Vector3(-110+half*lane,0,60+10*t);
            }
            // A slight bow skirts the west loggia's south piers, linking actual route centres.
            float wave = Mathf.Sin(t*Mathf.PI);
            return new Vector3(12-50*t,0,92-2*wave*wave+lane*2.25f);
        }

        private static float CircuitBedHeight(CityLayout world,Vector3 point,float t,float lane,bool sill)
        {
            float endRise = Mathf.Min(1,Mathf.Min(t,1-t)*12);
            float bed = CityLayout.CurbHeight+.014f+.028f*endRise;
            if (sill || Mathf.Abs(lane)<.9f) return bed;
            // Outer edges bank into the ground, rising to curb level again at the original route junctions.
            return Mathf.Lerp(CityLayout.CurbHeight+.014f,ScienceFrontierGround(world,point)+.022f,endRise);
        }

        private static void CircuitWalk(ScienceDistrictArt art,CityLayout world,Building permittedShell,bool sill)
        {
            int rows = sill ? 10 : 32;
            float[] lanes = { -1,-.72f,.72f,1 };
            ScienceDistrictArt stage = new ScienceDistrictArt();
            for (int row=0;row<rows;row++)
            for (int band=0;band<lanes.Length-1;band++)
            {
                float start = row/(float)rows, end = (row+1)/(float)rows;
                float left = lanes[band], right = lanes[band+1];
                Vector3[] top = { CircuitWalkPoint(sill,start,left), CircuitWalkPoint(sill,start,right),
                    CircuitWalkPoint(sill,end,right), CircuitWalkPoint(sill,end,left) };
                RectF bounds = CircuitBounds(top);
                if (!ScienceSiteClear(world,bounds.Center(0),bounds,permittedShell,true)) return;
                Vector3[] prism = new Vector3[8];
                for (int i=0;i<4;i++)
                {
                    float ground = ScienceFrontierGround(world,top[i]);
                    float height = CircuitBedHeight(world,top[i],i<2?start:end,i==0||i==3?left:right,sill);
                    if (ground>height+.012f) return;
                    prism[i]=new Vector3(top[i].x,ground-.08f,top[i].z);
                    prism[i+4]=new Vector3(top[i].x,height,top[i].z);
                }
                string role = band==1 ? (sill ? "basalt" : "masonry") : "earth";
                GothicRealmGeometry04OB.Hex(stage.Surface(role),prism);
            }
            foreach(KeyValuePair<string,MeshBuilder> part in stage.Surfaces) art.Surface(part.Key).Append(part.Value);
        }

        private static RectF CircuitBounds(Vector3[] points)
        {
            Vector3 low=points[0], high=points[0];
            foreach(Vector3 point in points){low=Vector3.Min(low,point);high=Vector3.Max(high,point);}
            return new RectF(low.x,low.z,high.x,high.z);
        }

        private static bool CircuitParcel(CityLayout world,Vector3 centre,float halfX,float halfZ)
        {
            // Mara and Dante retain their existing quest destinations, including the outer seam's observation court.
            float radius=Mathf.Sqrt(halfX*halfX+halfZ*halfZ);
            return MathUtil.FlatDistance(centre,world.RealmThreshold04OB)>16+radius &&
                ScienceFrontierClear(world,centre,halfX,halfZ);
        }

        private static void CircuitBank(ScienceDistrictArt art,CityLayout world,Vector3 centre,float yaw,float length,float width,float height)
        {
            Quaternion turn=QMath.AY(yaw);
            Vector3[] corners=new Vector3[4];
            for(int i=0;i<4;i++) corners[i]=centre+turn*new Vector3((i<2?-1:1)*width*.5f,0,(i%2==0?-1:1)*length*.5f);
            RectF parcel=CircuitBounds(corners);
            if(!CircuitParcel(world,centre,parcel.Width*.5f,parcel.Depth*.5f)) return;
            float[] stations={0,.17f,.39f,.61f,.85f,1};
            float[] crests={.30f,.84f,1,.70f,.49f,.18f};
            Vector3[][] rings=new Vector3[stations.Length][];
            for(int i=0;i<stations.Length;i++)
            {
                float half=width*(.31f+.17f*crests[i]), ridge=height*crests[i];
                float z=(stations[i]-.5f)*length;
                rings[i]=new[]{new Vector3(-half,-.1f,z),new Vector3(half,-.1f,z),
                    new Vector3(half*.88f,ridge*.51f,z),new Vector3(half*.47f,ridge,z),
                    new Vector3(-half*.43f,ridge*.79f,z),new Vector3(-half*.92f,ridge*.36f,z)};
                for(int j=0;j<rings[i].Length;j++)
                {
                    Vector3 point=centre+turn*rings[i][j];
                    point.y+=ScienceFrontierGround(world,point); rings[i][j]=point;
                }
            }
            for(int i=0;i<rings.Length-1;i++)
            {
                Vector3 middle=(centre+turn*new Vector3(0,height*(crests[i]+crests[i+1])*.22f,
                    (stations[i]+stations[i+1]-1)*length*.5f));
                for(int j=0;j<6;j++)
                {
                    int next=(j+1)%6;
                    Vector3 a=rings[i][j],b=rings[i][next],c=rings[i+1][next],d=rings[i+1][j];
                    Vector3 outward=(a+b+c+d)*.25f-middle;
                    if(j!=2){ScienceFace(art.Surface(j==4?"masonry":"basalt"),a,b,c,d,outward);continue;}
                    // The mineral seam is part of the bank skin, with no second coplanar face underneath it.
                    float band=.35f+.11f*stations[i],nextBand=.35f+.11f*stations[i+1];
                    Vector3 low=Vector3.Lerp(a,b,band),high=Vector3.Lerp(a,b,band+.095f);
                    Vector3 nextLow=Vector3.Lerp(d,c,nextBand),nextHigh=Vector3.Lerp(d,c,nextBand+.095f);
                    ScienceFace(art.Surface("basalt"),a,low,nextLow,d,outward);
                    ScienceFace(art.Surface("masonry"),low,high,nextHigh,nextLow,outward);
                    ScienceFace(art.Surface("basalt"),high,b,c,nextHigh,outward);
                }
            }
            CircuitEndCap(art.Surface("basalt"),rings[0],turn*Vector3.back);
            CircuitEndCap(art.Surface("basalt"),rings[rings.Length-1],turn*Vector3.forward);
            // Plant beyond the solid toe and check each individual non-solid cluster.
            for(int i=1;i<4;i++)
            {
                Vector3 at=centre+turn*new Vector3(-width*.53f-.25f,0,(stations[i]-.48f)*length);
                if(!CircuitParcel(world,at,1.0f,1.0f))continue;
                at.y=ScienceFrontierGround(world,at);FrontierFern(art,at,.85f+i*.08f,81+i);
                CircuitReeds(art,at,1.1f+i*.12f,81+i);
            }
        }

        private static void CircuitEndCap(MeshBuilder mesh,Vector3[] rim,Vector3 outward)
        {
            Vector3 centre=Vector3.zero;foreach(Vector3 point in rim)centre+=point/rim.Length;
            for(int i=0;i<rim.Length;i++)
            {
                Vector3 a=rim[i],b=rim[(i+1)%rim.Length];
                if(Vector3.Dot(Vector3.Cross(a-centre,b-centre),outward)<0){Vector3 swap=a;a=b;b=swap;}
                mesh.Triangle(0,centre,a,b,outward,new Vector2(centre.x,centre.y)*.5f,
                    new Vector2(a.x,a.y)*.5f,new Vector2(b.x,b.y)*.5f);
            }
        }

        private static void CircuitStation(ScienceDistrictArt art,CityLayout world,Vector3 centre)
        {
            if(!CircuitParcel(world,centre,5.25f,3.2f)) return;
            centre.y=ScienceFrontierGround(world,centre);
            CircuitStationApron(art,world,centre);
            CourtFooting(art,centre+Vector3.up*.14f,9.8f,5.5f);
            // Split rear cheeks and an alloy-lined inspection gap connect the yoke to its foundation.
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 basePoint=centre+new Vector3(side*2.85f,0,2.0f);
                ScienceBox(art.Surface("basalt"),basePoint+Vector3.up*.43f,new Vector3(3.6f,.94f,.85f));
                ScienceRib(art.Surface("masonry"),basePoint+new Vector3(-side*1.25f,.85f,0),
                    basePoint+new Vector3(0,1.65f,0),basePoint+new Vector3(side*1.25f,2.05f,0),.26f,.34f);
                ScienceBox(art.Surface("iron"),basePoint+new Vector3(side*1.25f,1.42f,0),new Vector3(.48f,1.28f,.52f));
            }
            // A sloped stone shoulder carries a separate alloy compression yoke. The core seats on its stone base.
            for(int side=-1;side<=1;side+=2)
            {
                float height=side<0?5.9f:3.65f;
                Vector3 foot=centre+new Vector3(side*3.65f,-.08f,.35f);
                ScienceBox(art.Surface("basalt"),foot+Vector3.up*.36f,new Vector3(1.95f,.72f,3.55f));
                for(int rib=0;rib<2;rib++)
                {
                    Vector3 toe=foot+Vector3.forward*(rib==0?-1.12f:1.12f);
                    ScienceRib(art.Surface("masonry"),toe+Vector3.up*.43f,toe+new Vector3(side*.3f,height*.77f,.1f),
                        centre+new Vector3(side*1.65f,height,.1f+(rib==0?-1.05f:1.05f)),.46f,.35f);
                }
                Vector3 tip=centre+new Vector3(side*1.65f,height,.1f);
                ScienceBox(art.Surface("iron"),tip,new Vector3(.62f,.48f,2.5f));
                ScienceRib(art.Surface("iron"),tip,tip+new Vector3(-side*.66f,-.28f,0),centre+new Vector3(side*.62f,3.55f,.1f),.17f,.14f);
            }
            ScienceCore(art,centre+Vector3.forward*.1f,.85f,3.6f);
            ScienceLamp(art,centre+new Vector3(-3.65f,1.6f,-.98f));
        }

        private static void CircuitStationApron(ScienceDistrictArt art,CityLayout world,Vector3 centre)
        {
            // Reach the existing Shardwood road at its actual interpolated height and position.
            RealmRoute04OB route=world.RealmRoutes04OB.Find(item=>item.Id=="shardwood-threshold");
            Vector3 a=route.Points[2],b=route.Points[3];
            Vector3 start=Vector3.Lerp(a,b,(centre.x-a.x)/(b.x-a.x));
            Vector3 end=centre+Vector3.back*(5.5f*.5f-.32f);
            MeshBuilder deck=new MeshBuilder(1);
            for(int row=0;row<10;row++)
            {
                float t=row*.1f,next=(row+1)*.1f;
                Vector3 p=Vector3.Lerp(start,end,t),q=Vector3.Lerp(start,end,next);
                Vector3[] corners={p-Vector3.right*(1.8f+t),p+Vector3.right*(1.8f+t),
                    q+Vector3.right*(1.8f+next),q-Vector3.right*(1.8f+next)};
                RectF area=CircuitBounds(corners);if(!ScienceSiteClear(world,area.Center(0),area,null,true))return;
                Vector3[] prism=new Vector3[8];
                for(int j=0;j<4;j++)
                {
                    float ground=ScienceFrontierGround(world,corners[j]);
                    float h=Mathf.Lerp(CityLayout.CurbHeight+.019f,centre.y+.146f,j<2?t:next);
                    if(ground>h)return;
                    prism[j]=new Vector3(corners[j].x,ground-.09f,corners[j].z);
                    prism[j+4]=new Vector3(corners[j].x,h,corners[j].z);
                }
                GothicRealmGeometry04OB.Hex(deck,prism);
            }
            art.Surface("basalt").Append(deck);
        }

        private static void CircuitTurningFan(ScienceDistrictArt art,CityLayout world)
        {
            // Flush fitted courses round the inside corner of the actual north-to-east turn.
            // The observation objective owns the centre. All new surfaces here are low walking surfaces.
            Vector3 centre=world.RealmThreshold04OB;
            float[] radii={.65f,2.25f,4.6f,7.2f};
            ScienceDistrictArt stage=new ScienceDistrictArt();
            for(int ring=0;ring<radii.Length-1;ring++)for(int wedge=0;wedge<10;wedge++)
            {
                Vector3[] top=new Vector3[4];
                for(int k=0;k<4;k++)
                {
                    float angle=(wedge+(k==1||k==2?1:0))*Mathf.PI/20;
                    float radius=radii[ring+(k>=2?1:0)];
                    Vector3 point=centre+new Vector3(Mathf.Sin(angle)*radius,0,Mathf.Cos(angle)*radius);
                    float ground=ScienceFrontierGround(world,point);
                    if(ground>CityLayout.CurbHeight)return;
                    bool onOriginalRoad=point.x-centre.x<=2.5f || point.z-centre.z<=3.5f;
                    point.y=(!onOriginalRoad && radius>7)?ground+.023f:CityLayout.CurbHeight+.036f;
                    top[k]=point;
                }
                RectF area=CircuitBounds(top);if(!ScienceSiteClear(world,area.Center(0),area,null,true))return;
                Vector3[] prism=new Vector3[8];for(int k=0;k<4;k++)
                {prism[k]=new Vector3(top[k].x,-.1f,top[k].z);prism[k+4]=top[k];}
                GothicRealmGeometry04OB.Hex(stage.Surface(ring==1?"masonry":"basalt"),prism);
            }
            foreach(KeyValuePair<string,MeshBuilder> part in stage.Surfaces)art.Surface(part.Key).Append(part.Value);
        }

        private static void CircuitReeds(ScienceDistrictArt art,Vector3 origin,float height,int seed)
        {
            MeshBuilder mesh=art.Surface("leaf");
            for(int blade=0;blade<9;blade++)
            {
                float angle=(blade*137+seed)*Mathf.Deg2Rad;
                Vector3 axis=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)), side=MathUtil.RightOf(axis);
                Vector3 root=origin+axis*(.05f+.06f*(blade%3)),middle=root+Vector3.up*height*.56f+axis*.17f;
                Vector3 tip=middle+Vector3.up*height*(.23f+.09f*(blade%3))+axis*.42f+Vector3.right*.22f;
                Vector3 normal=Vector3.Cross(side,tip-root).normalized;
                for(int face=-1;face<=1;face+=2)
                {
                    ScienceFace(mesh,root-side*.035f,root+side*.035f,middle+side*.10f,middle-side*.10f,normal*face);
                    ScienceFace(mesh,middle-side*.10f,middle+side*.10f,tip+side*.007f,tip-side*.007f,normal*face);
                }
            }
        }

        private static void CircuitGarden(ScienceDistrictArt art,CityLayout world,Vector3 centre)
        {
            if(!CircuitParcel(world,centre,4.8f,2.4f))return;
            centre.y=ScienceFrontierGround(world,centre);
            // A weathered seat turns toward the returning route, sheltering a narrow reed bed behind it.
            ScienceBox(art.Surface("basalt"),centre+Vector3.up*.17f,new Vector3(6.6f,.42f,1.15f));
            ScienceBox(art.Surface("masonry"),centre+Vector3.up*.46f,new Vector3(7,.18f,1.35f));
            for(int i=0;i<5;i++)
            {
                Vector3 at=centre+new Vector3(-3+i*1.15f,0,1.28f);
                FrontierFern(art,at,.8f+(i%2)*.28f,65+i);
                if(i%2==0)CircuitReeds(art,at,1.22f+i*.09f,65+i);
            }
        }
    }
}
