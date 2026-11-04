using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Editable, metre-scale architecture. Every solid role supplies the exact collision triangles.</summary>
    public sealed class ScienceDistrictArt
    {
        public readonly Dictionary<string, MeshBuilder> Surfaces = new Dictionary<string, MeshBuilder>();
        public MeshBuilder Surface(string role)
        {
            MeshBuilder mesh;
            if (!Surfaces.TryGetValue(role, out mesh)) { mesh = new MeshBuilder(1); mesh.Tile = 2; Surfaces.Add(role, mesh); }
            return mesh;
        }
        public MeshBuilder Solid()
        {
            MeshBuilder mesh = new MeshBuilder(1);
            foreach (KeyValuePair<string, MeshBuilder> part in Surfaces) if (part.Key != "leaf") mesh.Append(part.Value);
            return mesh;
        }
    }

    public static partial class HeartPlanFR01
    {
        /// <summary>Only the existing occupied service frames qualify. No new layout or save revision.</summary>
        public static bool ScienceServiceFrame(string id, float width, float depth)
        { return Mathf.Abs(width - 30) < .01f && ((id == "lofts" || id == "forge") && Mathf.Abs(depth - 30) < .01f || id == "archive" && Mathf.Abs(depth - 48) < .01f); }

        public static ScienceDistrictArt ScienceService(string id, MeshBuilder retainedStructure, MeshBuilder retainedPorch)
        {
            if (id == "archive") return ScienceArchive(retainedStructure,retainedPorch);
            if (id != "lofts" && id != "forge") throw new ArgumentException("Unknown science district service.");
            bool home = id == "lofts";
            ScienceDistrictArt art = new ScienceDistrictArt();
            art.Surface("masonry").Append(retainedStructure);
            art.Surface("masonry").Append(retainedPorch);
            // Closed, swept shells with separate intrados, eaves and radial seams. Both LODs keep this envelope.
            ScienceRoof(art, new Vector3(0, home ? 9.5f : 6.3f, 12), 17, 25, home ? 2.1f : 3.5f);
            // Full load-bearing arch frames connect the vault to the ground, outside the occupied room walls.
            for (int frame = 0; frame < 4; frame++)
                ScienceArch(art.Surface("masonry"), new Vector3(0,0,.3f+frame*8),17,home?9.5f:6.3f,home?2.1f:3.5f,.38f);
            for (int side = -1; side <= 1; side += 2)
            {
                ScienceRoof(art, new Vector3(side * 11.35f, 5.7f, 14), 6.6f, 28, 1.25f);
                for (int bay = 0; bay < 4; bay++)
                {
                    float z = 1.6f + bay * 8.5f;
                    ScienceRib(art.Surface("masonry"), new Vector3(side * 14.4f, 0, z),
                        new Vector3(side * 14.4f, 4.3f, z), new Vector3(side * 10.4f, 6.6f, z), .38f, .5f);
                    // A tapered socket makes the stone rib read as an assembled structural member.
                    ScienceBox(art.Surface("iron"), new Vector3(side * 14.4f, .32f, z), new Vector3(.95f, .64f, 1));
                }
                ScienceArch(art.Surface("masonry"), new Vector3(side * 5.15f, 0, -.45f), 4.25f, 2.7f, 1.6f, .34f);
                if (home) ScienceArch(art.Surface("masonry"), new Vector3(side * 5.15f, 4.35f, -.45f), 4.25f, 2.5f, 1.4f, .28f);
                if (home) SciencePlanter(art,new Vector3(side*5.15f,4.35f,-.5f),1.6f);
                SciencePlanter(art, new Vector3(side * 12.7f, home ? .5f : 0, 25.8f), 2.1f);
            }
            // Unchanged canonical threshold, 3.2m clear at the base and more than 3.5m headroom.
            ScienceArch(art.Surface("masonry"), new Vector3(0, 0, -.85f), 4.1f, 3.3f, 1.7f, .42f);
            ScienceRoof(art, new Vector3(0, 5.2f, -.4f), 7.6f, 3.8f, .9f);
            for(int side=-1;side<=1;side+=2) ServiceReveal(art,side,home);
            if (home)
            {
                ScienceCore(art, new Vector3(-11.3f, 0, 7.8f), .75f, 3.2f);
                ScienceLamp(art, new Vector3(-3.35f,2.1f,-1.98f));
                ScienceLamp(art, new Vector3(3.35f,2.1f,-1.98f));
            }
            else
            {
                // Outside the occupied workroom: neither Dante nor the original anvil is moved or duplicated.
                ScienceCore(art, new Vector3(11.2f, 0, 9), 1.35f, 4.8f);
                ScienceRib(art.Surface("iron"), new Vector3(11.2f, 4.5f, 9), new Vector3(11.2f, 6, 9), new Vector3(8.3f, 4.5f, 9), .19f, .19f);
                for (int i = 0; i < 5; i++)
                    ScienceBox(art.Surface("iron"), new Vector3(11.2f, 1.4f + i * .48f, 11), new Vector3(2.3f, .15f, .7f));
                // A supported offset condenser replaces the old pointed upper bay, giving the Forge its own silhouette.
                for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)
                    ScienceBox(art.Surface("iron"),new Vector3(11.2f+a,3.4f,20+b),new Vector3(.28f,6.8f,.28f));
                ScienceCore(art,new Vector3(11.2f,6.8f,20),.85f,4.2f);
                ScienceLamp(art, new Vector3(-3.35f,2.1f,-1.98f));
            }
            return art;
        }

        // A thick recessed frontage connects the low entrance to the tall roof. Keep the occupied doorway open.
        private static void ServiceReveal(ScienceDistrictArt art,int side,bool home)
        {
            float sill = home ? 1.12f : .65f;
            float head = home ? 4.35f : 3.25f;
            ServiceStone(art,side,2.75f,8.1f,0,sill,sill*.85f,-1.85f,.8f);
            ServiceStone(art,side,2.75f,3.9f,sill*.85f,head+.75f,head+.5f,-1.85f,.7f);
            ServiceStone(art,side,7.0f,8.1f,sill*.85f,head+.5f,head+.22f,-1.85f,.7f);
            ServiceStone(art,side,3.75f,7.15f,head,head+.9f,head+.55f,-1.75f,.7f);
            if(home)
            {
                // The inset screen and its planted sill sit behind the reveal, leaving the porch clear.
                for(int fin=0;fin<4;fin++)
                    ScienceBox(art.Surface("iron"),new Vector3(side*(4.17f+fin*.80f),(head+sill)*.5f,.42f),new Vector3(.10f,head-sill,.24f));
                SciencePlanter(art,new Vector3(side*5.35f,sill*.92f-.02f,-.25f),1.45f);
            }
            else
            {
                // Vent blades terminate in the two masonry jambs. The supply elbow joins the actual Forge core.
                for(int blade=0;blade<4;blade++)
                    ScienceBox(art.Surface("iron"),new Vector3(side*5.46f,sill+.42f+blade*.52f,.16f),new Vector3(3.3f,.14f,.45f));
                if(side>0)ScienceRib(art.Surface("iron"),new Vector3(7.45f,3.4f,0),new Vector3(11.2f,3.4f,0),new Vector3(11.2f,3.4f,8.3f),.22f,.22f);
            }
        }

        private static void ServiceStone(ScienceDistrictArt art,int side,float inner,float outer,float baseY,
            float innerTop,float outerTop,float face,float back)
        {
            float a=side*inner,b=side*outer;
            Vector3[] block={new Vector3(a,baseY,face),new Vector3(b,baseY,face),new Vector3(b,baseY,back),new Vector3(a,baseY,back),
                new Vector3(a,innerTop,face+.16f),new Vector3(b,outerTop,face+.32f),new Vector3(b,outerTop,back),new Vector3(a,innerTop,back)};
            GothicRealmGeometry04OB.Hex(art.Surface("masonry"),block);
        }

        public static ScienceDistrictArt ScienceCourt() { return ScienceCourt(null); }

        /// <summary>Six surveyed civic bays. Admission checks the whole footprint against live routes and protected owners.</summary>
        public static ScienceDistrictArt ScienceCourt(CityLayout world)
        {
            ScienceDistrictArt art=new ScienceDistrictArt();
            Vector2[] sites={new Vector2(-24,-19),new Vector2(-24,32),new Vector2(16,48),
                new Vector2(28,0),new Vector2(28,26),new Vector2(24,52)};
            float[] lengths={8,8,8,11.8f,10.8f,12};
            for(int bay=0;bay<sites.Length;bay++)
            {
                float width=bay<3?5.8f:7.2f, length=lengths[bay];
                Vector3 centre=new Vector3(sites[bay].x,CityLayout.CurbHeight,sites[bay].y);
                if(world!=null && !ScienceFrontierClear(world,centre,width*.5f+.25f,length*.5f+.2f))continue;
                int outer=centre.x<0?-1:1;
                float spring=3.45f+(bay%3)*.4f, rise=1.15f+(bay%2)*.5f;
                CourtFooting(art,centre,width,length);
                ScienceRoof(art,centre+Vector3.up*(spring+.08f),width,length,rise);
                for(int end=-1;end<=1;end+=2)
                    ScienceArch(art.Surface("masonry"),centre+Vector3.forward*end*(length*.5f-.7f),width-.9f,spring,rise,.29f);
                float wallX=outer*(width*.5f-.55f);
                ScienceBox(art.Surface("basalt"),centre+new Vector3(wallX,.36f,-.55f),new Vector3(.82f,.72f,length*.50f));
                ScienceBox(art.Surface("masonry"),centre+new Vector3(wallX,.78f,-.55f),new Vector3(1.02f,.16f,length*.50f+.23f));
                // A planted back and solid wind cheek give each bay a sheltered side.
                SciencePlanter(art,centre+new Vector3(wallX,0,length*.28f),1.45f);
                ScienceBox(art.Surface("masonry"),centre+new Vector3(wallX,1.55f,-length*.32f),new Vector3(.55f,2.9f,1.1f));
                ScienceLamp(art,centre+new Vector3(wallX,2.1f,-length*.32f-.62f));
            }
            return art;
        }

        private static void CourtFooting(ScienceDistrictArt art,Vector3 centre,float width,float length)
        {
            Vector3[] bed=new Vector3[8];
            for(int i=0;i<4;i++)
            {
                float x=i<2?-1:1,z=i==0||i==3?-1:1;
                bed[i]=centre+new Vector3(x*width*.5f,-.23f,z*length*.5f);
                bed[i+4]=centre+new Vector3(x*(width*.5f-.32f),.006f,z*(length*.5f-.32f));
            }
            GothicRealmGeometry04OB.Hex(art.Surface("basalt"),bed);
        }

        public static float ScienceGlow(float night, bool thirdEye, bool warm)
        { return warm ? Mathf.Lerp(.08f, .8f, Mathf.Clamp01(night)) : thirdEye ? 2.2f : Mathf.Lerp(.12f, .38f, Mathf.Clamp01(night)); }

        private static void ScienceBox(MeshBuilder mesh, Vector3 at, Vector3 size)
        { mesh.Box(0, at, size, UVMode.World); }

        private static void ScienceFace(MeshBuilder mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) < 0) { Vector3 swap = b; b = d; d = swap; }
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            mesh.Quad(0, a, b, c, d, normal, Vector2.zero, new Vector2(Vector3.Distance(a,b) / 2,0),
                new Vector2(Vector3.Distance(a,b) / 2,Vector3.Distance(b,c) / 2), new Vector2(0,Vector3.Distance(a,d) / 2));
        }

        private static void ScienceRoofFace(MeshBuilder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 outward)
        {
            int start=mesh.VertexCount;
            ScienceFace(mesh,a,b,c,d,outward);
            // Keep one two-metre texture field across all panels, including shared edges and undersides.
            for(int i=start;i<mesh.VertexCount;i++)
            {
                Vector3 point=mesh.Vertices[i];mesh.UVs[i]=new Vector2(point.x,point.z)*.5f;
            }
        }

        private static Vector3 ScienceRoofPoint(Vector3 at, float width, float length, float rise, float u, float v)
        { return at + new Vector3(u * width * .5f, rise * (1 - u * u) + .38f * v * v, v * length * .5f); }

        private static void ScienceRoof(ScienceDistrictArt art, Vector3 at, float width, float length, float rise)
        {
            MeshBuilder roof = art.Surface("slate"); const int across = 16, along = 8;
            Vector3 down = Vector3.down * .36f;
            for (int x = 0; x < across; x++) for (int z = 0; z < along; z++)
            {
                float u = -1 + 2f * x / across, un = -1 + 2f * (x + 1) / across;
                float v = -1 + 2f * z / along, vn = -1 + 2f * (z + 1) / along;
                Vector3 a = ScienceRoofPoint(at,width,length,rise,u,v), b = ScienceRoofPoint(at,width,length,rise,un,v);
                Vector3 c = ScienceRoofPoint(at,width,length,rise,un,vn), d = ScienceRoofPoint(at,width,length,rise,u,vn);
                ScienceRoofFace(roof,a,b,c,d,Vector3.up); ScienceRoofFace(roof,a+down,b+down,c+down,d+down,Vector3.down);
                if (z == 0) ScienceFace(art.Surface("masonry"),a,b,b+down,a+down,Vector3.back);
                if (z == along-1) ScienceFace(art.Surface("masonry"),d,c,c+down,d+down,Vector3.forward);
                if (x == 0) ScienceFace(art.Surface("masonry"),a,d,d+down,a+down,Vector3.left);
                if (x == across-1) ScienceFace(art.Surface("masonry"),b,c,c+down,b+down,Vector3.right);
            }
            for (int band = 0; band <= 4; band++)
            {
                float v = -1 + band * .5f;
                Vector3 a = ScienceRoofPoint(at,width,length,rise,-1,v) + Vector3.up * .08f;
                Vector3 b = at + new Vector3(0, rise * 2 + .38f * v * v + .08f, v * length * .5f);
                Vector3 c = ScienceRoofPoint(at,width,length,rise,1,v) + Vector3.up * .08f;
                ScienceRib(art.Surface("masonry"),a,b,c,.14f,.18f);
            }
        }

        private static void ScienceArch(MeshBuilder mesh, Vector3 at, float width, float spring, float rise, float radius)
        {
            float half = width * .5f;
            ScienceBox(mesh,at + new Vector3(-half,spring*.5f,0),new Vector3(radius*2,spring,.8f));
            ScienceBox(mesh,at + new Vector3(half,spring*.5f,0),new Vector3(radius*2,spring,.8f));
            ScienceRib(mesh,at+new Vector3(-half,spring,0),at+new Vector3(0,spring+rise*2,0),at+new Vector3(half,spring,0),radius,.4f);
        }

        /// <summary>Closed elliptical beam swept along a quadratic curve, with metre-scaled UVs.</summary>
        private static void ScienceRib(MeshBuilder mesh, Vector3 start, Vector3 control, Vector3 end, float radius, float depth)
        {
            const int steps = 16, sides = 8; Vector3[][] rings = new Vector3[steps+1][];
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector3 centre = (1-t)*(1-t)*start + 2*(1-t)*t*control + t*t*end;
                Vector3 tangent = ((control-start)*(1-t)+(end-control)*t).normalized;
                Vector3 lateral = Vector3.Cross(tangent,Vector3.forward).normalized;
                if (lateral.sqrMagnitude < .5f) lateral = Vector3.right;
                Vector3 binormal = Vector3.Cross(tangent,lateral).normalized;
                rings[i] = new Vector3[sides];
                for (int s = 0; s < sides; s++)
                {
                    float angle = s * Mathf.PI * 2 / sides;
                    rings[i][s] = centre + lateral * Mathf.Cos(angle)*radius + binormal*Mathf.Sin(angle)*depth;
                }
            }
            for (int i = 0; i < steps; i++) for (int s = 0; s < sides; s++)
            {
                int next=(s+1)%sides; Vector3 centre=Vector3.zero;
                foreach(Vector3 p in rings[i]) centre+=p/sides;
                ScienceFace(mesh,rings[i][s],rings[i+1][s],rings[i+1][next],rings[i][next],rings[i][s]-centre);
            }
            for (int endIndex = 0; endIndex <= steps; endIndex += steps)
            {
                Vector3 centre=endIndex==0?start:end, outward=endIndex==0?start-control:end-control;
                for(int s=0;s<sides;s++)
                {
                    Vector3 a=rings[endIndex][s],b=rings[endIndex][(s+1)%sides];
                    if(Vector3.Dot(Vector3.Cross(a-centre,b-centre),outward)<0){Vector3 swap=a;a=b;b=swap;}
                    mesh.Triangle(0,centre,a,b,outward.normalized,Vector2.zero,new Vector2(1,0),new Vector2(0,1));
                }
            }
        }

        private static void ScienceCore(ScienceDistrictArt art, Vector3 at, float radius, float height)
        {
            ScienceBox(art.Surface("masonry"),at+Vector3.up*.3f,new Vector3(radius*2.5f,.6f,radius*2.5f));
            for (int s = 0; s < 6; s++)
            {
                float a=s*Mathf.PI/3; Vector3 offset=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);
                ScienceRib(art.Surface("iron"),at+offset+Vector3.up*.55f,at+offset*1.3f+Vector3.up*(height*.55f),at+offset*.6f+Vector3.up*height,.12f,.12f);
            }
            ScienceBox(art.Surface("iron"),at+Vector3.up*(height+.1f),new Vector3(radius*1.6f,.3f,radius*1.6f));
            GothicRealmGeometry04OB.Crystal(art.Surface("azure"),at+Vector3.up*.7f,new Vector3(radius*.8f,height-.9f,radius*.8f),.04f,61724);
            for(int i=0;i<3;i++) ScienceBox(art.Surface("iron"),at+new Vector3(0,.6f+i*(height-.6f)/2,radius),new Vector3(radius*2.1f,.16f,.2f));
        }

        private static void ScienceLamp(ScienceDistrictArt art, Vector3 at)
        {
            ScienceBox(art.Surface("iron"),at,new Vector3(.5f,.9f,.5f));
            ScienceBox(art.Surface("warm"),at+Vector3.back*.27f,new Vector3(.32f,.62f,.09f));
            for(int s=-1;s<=1;s+=2)ScienceBox(art.Surface("iron"),at+new Vector3(s*.2f,0,-.32f),new Vector3(.07f,.96f,.12f));
        }

        private static void SciencePlanter(ScienceDistrictArt art, Vector3 at, float length)
        {
            ScienceBox(art.Surface("masonry"),at+Vector3.up*.3f,new Vector3(.85f,.6f,length));
            for(int i=0;i<5;i++)
            {
                Vector3 root=at+new Vector3(0,.6f,(i-2)*length/5);
                for(int j=0;j<5;j++)
                {
                    float angle=j*Mathf.PI*2/5;Vector3 tip=root+new Vector3(Mathf.Cos(angle)*.5f,.45f,Mathf.Sin(angle)*.5f);
                    Vector3 across=new Vector3(-Mathf.Sin(angle),0,Mathf.Cos(angle))*.13f;
                    ScienceFace(art.Surface("leaf"),root,Vector3.Lerp(root,tip,.55f)+across,tip,Vector3.Lerp(root,tip,.55f)-across,Vector3.up);
                    ScienceFace(art.Surface("leaf"),root,Vector3.Lerp(root,tip,.55f)+across,tip,Vector3.Lerp(root,tip,.55f)-across,Vector3.down);
                }
            }
        }
    }
}
