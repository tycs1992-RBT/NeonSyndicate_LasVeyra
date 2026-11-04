using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class HeartPlanFR01
    {
        /// <summary>Finite composition beside the real sanctuary-wilds route. No new world or traversal owner.</summary>
        public static ScienceDistrictArt ScienceFrontier(CityLayout world)
        {
            ScienceDistrictArt art=new ScienceDistrictArt();
            if(world==null || !world.GothicCivicH5)return art;
            RealmRoute04OB route=world.RealmRoutes04OB.Find(r=>r.Id=="sanctuary-wilds");
            if(route==null || route.Points.Count<4)return art;
            // The authored flat, five-metre service route is a prerequisite, never silently moved.
            if(MathUtil.FlatDistance(route.Points[1],new Vector3(-38,0,92))>.1f ||
               MathUtil.FlatDistance(route.Points[2],new Vector3(-76,0,92))>.1f || route.Width<5)return art;
            Vector3 gate=new Vector3(-52,0,92);
            gate.y=ScienceFrontierGround(world,gate);
            bool gateway=true;
            foreach(int end in new[]{-1,1})foreach(int side in new[]{-1,1})
                gateway &= ScienceFrontierClear(world,gate+new Vector3(end*3,0,side*3.8f),.42f,.52f) &&
                    Mathf.Abs(ScienceFrontierGround(world,gate+new Vector3(end*3,0,side*3.8f))-gate.y)<.04f;
            if(gateway)
            {
                ScienceDistrictArt pavilion=new ScienceDistrictArt();
                ScienceRoof(pavilion,Vector3.up*6,8,8.5f,1.8f);
                for(int end=-1;end<=1;end+=2)
                {
                    ScienceArch(pavilion.Surface("masonry"),Vector3.forward*end*3,7.6f,4.6f,2,.4f);
                    for(int side=-1;side<=1;side+=2)ScienceLamp(pavilion,new Vector3(side*3.8f,3.2f,end*3));
                }
                FrontierAppend(art,pavilion,gate,90);
            }
            // Graded shoulders and broken stone shelves flank, rather than cover, the existing paved route.
            for(int bay=0;bay<9;bay++)for(int side=-1;side<=1;side+=2)
            {
                Vector3 at=new Vector3(-43-bay*5,0,92+side*(4.8f+(bay%3)*.45f));
                if(!ScienceFrontierClear(world,at,2.1f,1.25f))continue;
                at.y=ScienceFrontierGround(world,at);
                FrontierShelf(art,at,new Vector3(4.2f,.35f+(bay%3)*.18f,2.5f),bay+31);
                if(bay%2==0)FrontierFern(art,at+new Vector3(.7f,.45f,0),.85f,bay);
            }
            // Continuous low graded verges meet the stone route without adding a stair or a wall across it.
            for(int bay=0;bay<8;bay++)for(int side=-1;side<=1;side+=2)
            {
                Vector3 at=new Vector3(-48-bay*5,0,92+side*4.05f);
                if(!ScienceFrontierClear(world,at,2.48f,1))continue;
                float y=ScienceFrontierGround(world,at);
                Vector3[] v={new Vector3(at.x-2.5f,y-.08f,at.z-1),new Vector3(at.x+2.5f,y-.08f,at.z-1),
                    new Vector3(at.x+2.5f,y-.08f,at.z+1),new Vector3(at.x-2.5f,y-.08f,at.z+1),
                    new Vector3(at.x-2.5f,y+.025f,at.z-1),new Vector3(at.x+2.5f,y+.025f,at.z-1),
                    new Vector3(at.x+2.5f,y+.025f,at.z+1),new Vector3(at.x-2.5f,y+.025f,at.z+1)};
                for(int j=0;j<4;j++){float ground=ScienceFrontierGround(world,v[j]);v[j].y=ground-.08f;v[j+4].y=ground+.025f;}
                // Grade rises toward the outer planted bank, leaving the inner verge nearly flush.
                if(side>0){v[6].y+=.24f;v[7].y+=.24f;}else{v[4].y+=.24f;v[5].y+=.24f;}
                GothicRealmGeometry04OB.Hex(art.Surface("earth"),v);
            }
            // Unequal clusters frame a clear middle distance and a view back to the civic roofs.
            Vector3[] trees={new Vector3(-51,0,98),new Vector3(-59,0,97),new Vector3(-70,0,88),
                new Vector3(-78,0,84),new Vector3(-90,0,72),new Vector3(-80,0,70),
                new Vector3(-92,0,82),new Vector3(-75,0,76),new Vector3(-59,0,102)};
            for(int i=0;i<trees.Length;i++)
            {
                Vector3 at=trees[i];if(!ScienceFrontierClear(world,at,1.15f,1.15f))continue;
                if(MathUtil.FlatDistance(at,new Vector3(-82,0,78))<5 || MathUtil.FlatDistance(at,new Vector3(-94,0,96))<5)continue;
                at.y=ScienceFrontierGround(world,at);FrontierPine(art,at,8.5f+(i%4)*1.65f,i+71);
                for(int n=0;n<3;n++)
                {
                    Vector3 fern=at+new Vector3((n-1)*1.2f,0,1.4f);
                    if(ScienceFrontierClear(world,fern,.7f,.7f)){fern.y=ScienceFrontierGround(world,fern);FrontierFern(art,fern,1.1f,n+i);}
                }
            }
            Vector3 ridge=new Vector3(-88,0,71);
            if(ScienceFrontierClear(world,ridge,3.4f,2))
            {
                ridge.y=ScienceFrontierGround(world,ridge);
                FrontierShelf(art,ridge,new Vector3(6.8f,2.6f,4),171);
            }
            // A low, asymmetric mineral instrument on layered bedrock, not a portal or reward source.
            Vector3 landmark=new Vector3(-94,0,96);
            if(ScienceFrontierClear(world,landmark,3.1f,2.6f))
            {
                landmark.y=ScienceFrontierGround(world,landmark);
                FrontierShelf(art,landmark,new Vector3(6.2f,1.4f,5.2f),93);
                for(int i=0;i<3;i++)
                {
                    Vector3 at=landmark+new Vector3((i-1)*1.55f,.75f,(i%2)*.6f);
                    GothicRealmGeometry04OB.Crystal(art.Surface("basalt"),at,new Vector3(1.5f,4.8f+i*.9f,1.7f),.06f*(i-1),93+i);
                    ScienceCore(art,at+new Vector3(.25f,.25f,-.5f),.32f,2.7f+i*.35f);
                }
            }
            // Ground-level arrival shelter. The existing grotto-through and return routes stay open.
            Vector3 arrival=new Vector3(-82,0,78);
            if(ScienceFrontierClear(world,arrival,4.1f,3.3f))
            {
                arrival.y=ScienceFrontierGround(world,arrival);
                ScienceRoof(art,arrival+Vector3.up*4.7f,7.4f,5.5f,1.1f);
                for(int side=-1;side<=1;side+=2)
                {
                    ScienceArch(art.Surface("masonry"),arrival+Vector3.forward*side*2.1f,6.6f,3.6f,1.2f,.3f);
                    ScienceBox(art.Surface("masonry"),arrival+new Vector3(side*2.8f,.38f,0),new Vector3(.65f,.76f,2.4f));
                    ScienceLamp(art,arrival+new Vector3(side*3.3f,2.6f,2.1f));
                }
                // A flush splayed apron connects the clearing to the existing route, no stair-only access.
                FrontierApron(art,world,new Vector3(-82,0,78),new Vector3(-83.56732f,0,84.46526f),4);
            }
            return art;
        }

        /// <summary>Whole footprint versus actual routes, buildings, receipts, sites and old solid scenery.</summary>
        public static bool ScienceFrontierClear(CityLayout world,Vector3 p,float halfX,float halfZ)
        {
            RectF area=new RectF(p.x-halfX,p.z-halfZ,p.x+halfX,p.z+halfZ);
            return ScienceSiteClear(world,p,area,null,false);
        }

        // Solid walking beds can meet the existing supported paths. Fixtures still reserve their full lanes.
        private static bool ScienceSiteClear(CityLayout world,Vector3 p,RectF area,Building permittedShell,bool walkSurface)
        {
            float halfX=area.Width*.5f,halfZ=area.Depth*.5f;
            if(WorldApertures.Overlaps(area))return false;
            foreach(Block block in world.Blocks)foreach(Lot lot in block.Lots)foreach(Building b in lot.Buildings)
                if(!ReferenceEquals(b,permittedShell) && b.Footprint.Overlaps(area,.45f))return false;
            foreach(RoadEdge edge in world.Edges)if(world.EdgeRect(edge).Overlaps(area,1))return false;
            if(!walkSurface)foreach(RealmRoute04OB route in world.RealmRoutes04OB)for(int i=1;i<route.Points.Count;i++)
            {
                Vector3 a=route.Points[i-1],b=route.Points[i];
                // Expanded rectangle intersection is conservative even for diagonal neighbour paths.
                if(FrontierSegmentBox(a,b,area,route.Width*.5f+.45f))return false;
            }
            float radius=Mathf.Sqrt(halfX*halfX+halfZ*halfZ);
            foreach(List<Vector3> points in new[]{world.CrystalShardSpots,world.DataChipSpots,world.SpellStoneSpots,world.PickupSpots,world.CreatureSpots,world.RadioMastSpots,world.ParkingSpots})
                foreach(Vector3 point in points)if(MathUtil.FlatDistance(point,p)<radius+2)return false;
            foreach(SpecialSite site in world.Sites)if(MathUtil.FlatDistance(site.Position,p)<site.Radius+radius+2)return false;
            foreach(PropSpot prop in world.Props)if(MathUtil.FlatDistance(prop.Pos,p)<radius+1)return false;
            foreach(RealmRock04OB rock in world.Landscape04OB)if(MathUtil.FlatDistance(rock.Position,p)<radius+Mathf.Max(rock.Size.x,rock.Size.z)*.65f)return false;
            foreach(WildScenery04OC item in world.WildScenery04OC)if(MathUtil.FlatDistance(item.Position,p)<radius+Mathf.Max(item.Size.x,item.Size.z)*.65f)return false;
            return true;
        }

        private static bool FrontierSegmentBox(Vector3 a,Vector3 b,RectF area,float margin)
        {
            float lo=0,hi=1;
            for(int axis=0;axis<2;axis++)
            {
                float start=axis==0?a.x:a.z,delta=axis==0?b.x-a.x:b.z-a.z;
                float min=(axis==0?area.X0:area.Z0)-margin,max=(axis==0?area.X1:area.Z1)+margin;
                if(Mathf.Abs(delta)<.00001f){if(start<min||start>max)return false;continue;}
                float enter=(min-start)/delta,leave=(max-start)/delta;
                if(enter>leave){float swap=enter;enter=leave;leave=swap;}
                lo=Mathf.Max(lo,enter);hi=Mathf.Min(hi,leave);if(lo>hi)return false;
            }
            return true;
        }

        /// <summary>Sample the real six-cell terrain triangles, not the smoother analytic height function.</summary>
        public static float ScienceFrontierGround(CityLayout world,Vector3 p)
        {
            float height=0;
            foreach(WildPatch04OC patch in world.WildPatches04OC)
            {
                if(!patch.Area.Contains(p))continue;
                float u=(p.x-patch.Area.X0)/patch.Area.Width*6,v=(p.z-patch.Area.Z0)/patch.Area.Depth*6;
                int x=Mathf.Min(5,Mathf.FloorToInt(u)),z=Mathf.Min(5,Mathf.FloorToInt(v));
                float fx=u-x,fz=v-z;float[] h=new float[4];
                int[] dx={0,0,1,1},dz={0,1,1,0};
                for(int i=0;i<4;i++)h[i]=.018f+world.WildHeight04OC(patch,new Vector3(patch.Area.X0+(x+dx[i])*patch.Area.Width/6,0,patch.Area.Z0+(z+dz[i])*patch.Area.Depth/6));
                height=Mathf.Max(height,fx<=fz?h[0]*(1-fz)+h[1]*(fz-fx)+h[2]*fx:h[0]*(1-fx)+h[2]*fz+h[3]*(fx-fz));
            }
            return height;
        }

        private static void FrontierAppend(ScienceDistrictArt target,ScienceDistrictArt source,Vector3 at,float yaw)
        {
            foreach(KeyValuePair<string,MeshBuilder> part in source.Surfaces)
            {
                MeshBuilder transformed=new MeshBuilder(1);transformed.Append(part.Value);
                for(int i=0;i<transformed.VertexCount;i++)
                {transformed.Vertices[i]=at+QMath.AY(yaw)*transformed.Vertices[i];transformed.Normals[i]=QMath.AY(yaw)*transformed.Normals[i];}
                target.Surface(part.Key).Append(transformed);
            }
        }
        private static void FrontierShelf(ScienceDistrictArt art,Vector3 at,Vector3 size,int seed)
        {
            // Three interlocking strata, embedded below ground, provide physical graded banks.
            for(int i=0;i<3;i++)
                GothicRealmGeometry04OB.Rock(art.Surface("basalt"),at+new Vector3(i*.13f,-.18f+i*size.y*.22f,i*.11f),new Vector3(size.x*(1-i*.16f),size.y*.65f,size.z*(1-i*.13f)),seed+i);
        }
        private static void FrontierApron(ScienceDistrictArt art,CityLayout world,Vector3 a,Vector3 b,float width)
        {
            Vector3 side=MathUtil.RightOf(MathUtil.Flat(b-a).normalized)*width*.5f;
            MeshBuilder bed=new MeshBuilder(1);
            // Continue the apron behind its original first edge, making the shelter a supported arrival space.
            Vector3 inner=a-MathUtil.Flat(b-a).normalized*2.35f;
            for(int i=-1;i<8;i++)
            {
                Vector3 p=i<0?inner:Vector3.Lerp(a,b,i/8f),q=Vector3.Lerp(a,b,(i+1)/8f);
                Vector3[] top={p-side,p+side,q+side,q-side};
                RectF footprint=CircuitBounds(top);
                if(!ScienceSiteClear(world,footprint.Center(0),footprint,null,true))return;
                for(int j=0;j<4;j++)top[j].y=Mathf.Lerp(ScienceFrontierGround(world,top[j])+.022f,
                    CityLayout.CurbHeight+.024f,Mathf.Clamp01((j<2?i:i+1)/8f));
                Vector3[] v=new Vector3[8];for(int j=0;j<4;j++){v[j]=top[j]-Vector3.up*.15f;v[j+4]=top[j];}
                GothicRealmGeometry04OB.Hex(bed,v);
            }
            art.Surface("earth").Append(bed);
        }
        private static void FrontierPine(ScienceDistrictArt art,Vector3 at,float height,int seed)
        {
            GothicRealmGeometry04OB.Crystal(art.Surface("root"),at-Vector3.up*.1f,new Vector3(.65f,height*.92f,.65f),.015f,seed);
            // Layered irregular whorls with branch-free walking space below, all needles are non-solid.
            for(int tier=0;tier<7;tier++)
            {
                float y=2.5f+tier*(height-3)/7,r=(height-y)*.27f;
                for(int n=0;n<10;n++)
                {
                    float angle=(n*36+seed+tier*17)*Mathf.Deg2Rad;
                    Vector3 direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle)),cross=Vector3.Cross(direction,Vector3.up);
                    Vector3 root=at+Vector3.up*(y+1.1f),tip=at+Vector3.up*y+direction*r;
                    Vector3 l=at+Vector3.up*(y+.2f)+direction*r*.55f+cross*r*.35f,rr=l-cross*r*.7f;
                    ScienceFace(art.Surface("leaf"),root,l,tip,rr,Vector3.up);
                    ScienceFace(art.Surface("leaf"),root,l,tip,rr,Vector3.down);
                }
            }
            for(int n=0;n<4;n++)
            {
                float angle=n*Mathf.PI*.5f;Vector3 d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                ScienceRib(art.Surface("root"),at+d*.15f+Vector3.up*.35f,at+d*.6f+Vector3.up*.12f,at+d*.85f,.09f,.09f);
            }
        }
        private static void FrontierFern(ScienceDistrictArt art,Vector3 at,float scale,int seed)
        {
            for(int frond=0;frond<7;frond++)
            {
                float angle=(frond*360f/7+seed*21)*Mathf.Deg2Rad;Vector3 d=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Vector3 side=Vector3.Cross(d,Vector3.up);
                for(int n=0;n<6;n++)
                {
                    float t=n/6f,tn=(n+1)/6f;
                    Vector3 a=at+scale*(d*t+Vector3.up*(.1f+Mathf.Sin(t*Mathf.PI)*.45f));
                    Vector3 b=at+scale*(d*tn+Vector3.up*(.1f+Mathf.Sin(tn*Mathf.PI)*.45f));
                    ScienceFace(art.Surface("leaf"),a,a+side*(1-t)*scale*.17f,b,a-side*(1-t)*scale*.17f,Vector3.up);
                    ScienceFace(art.Surface("leaf"),a,a+side*(1-t)*scale*.17f,b,a-side*(1-t)*scale*.17f,Vector3.down);
                }
            }
        }
    }
}
