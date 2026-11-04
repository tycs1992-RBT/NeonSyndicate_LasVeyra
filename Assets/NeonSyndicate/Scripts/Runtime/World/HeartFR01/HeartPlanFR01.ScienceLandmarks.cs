using UnityEngine;

namespace NeonSyndicate
{
    public static partial class HeartPlanFR01
    {
        // Venue-local metres. The retained occupied rooms are 16 x 18 and rise to 8.6 or 5.
        // The separate collectible pad at (-11.75, depth*.72) keeps its original footprint and sky.
        private static void LoftsTerrace(ScienceDistrictArt art)
        {
            const float deck = 9.16f;
            MeshBuilder stone = art.Surface("masonry"), metal = art.Surface("iron");
            // The deck bears on the occupied side walls and on these piers at both projecting ends.
            ScienceBox(stone,new Vector3(0,deck-.28f,11.4f),new Vector3(17.4f,.56f,26.4f));
            foreach(float x in new[]{-8.27f,8.27f})
            foreach(float z in new[]{.2f,12.2f,23.9f})
            {
                ScienceBox(stone,new Vector3(x,4.3f,z),new Vector3(.8f,8.6f,.95f));
                ScienceRib(stone,new Vector3(x,7.1f,z),new Vector3(x,8.45f,z),
                    new Vector3(x*.65f,8.76f,z),.32f,.47f);
            }
            // Open sky above the negative local-X half. The existing upstairs rooms remain independently accessible.
            // A flight landing on the deck leads directly to the canopied seat; no false stair or upper door.
            for(int x=-8;x<=8;x+=2)
                ScienceBox(metal,new Vector3(x,deck+.56f,-1.45f),new Vector3(.12f,1.12f,.16f));
            foreach(float y in new[]{.46f,1.1f})
                ScienceBox(metal,new Vector3(0,deck+y,-1.45f),new Vector3(16.8f,.09f,.14f));
            foreach(float side in new[]{-8.45f,8.45f})
            {
                ScienceBox(stone,new Vector3(side,deck+.46f,11.5f),new Vector3(.34f,.92f,25.8f));
                ScienceBox(metal,new Vector3(side,deck+.99f,11.5f),new Vector3(.39f,.14f,25.8f));
            }
            ScienceBox(stone,new Vector3(0,deck+.55f,24.36f),new Vector3(17.4f,1.1f,.48f));
            // Low rear roof steps back from the open forecourt. A real back wall stops the seating bay.
            ScienceRoof(art,new Vector3(0,11.32f,17.75f),16.7f,13.45f,1.38f);
            foreach(float x in new[]{-7.9f,7.9f})
            foreach(float z in new[]{11.5f,23.8f})
                ScienceBox(stone,new Vector3(x,deck+1.15f,z),new Vector3(.54f,2.3f,.68f));
            ScienceBox(stone,new Vector3(4.3f,deck+1.62f,8.15f),new Vector3(7.65f,3.24f,.48f));
            // Recess at the forward edge of the canopy. The high backrest and amber opening are
            // visible over the deck fascia at street level. The return walls give the window depth.
            ScienceBox(stone,new Vector3(4.55f,deck+1.12f,2.65f),new Vector3(5.7f,2.24f,1.82f));
            ScienceBox(stone,new Vector3(4.55f,deck+2.04f,3.72f),new Vector3(5.7f,4.08f,.40f));
            ScienceBox(stone,new Vector3(4.55f,deck+4.12f,2.64f),new Vector3(5.7f,.28f,2.16f));
            ScienceBox(art.Surface("amber-glass"),new Vector3(4.55f,deck+3.17f,3.44f),new Vector3(4.45f,1.60f,.12f));
            foreach(float x in new[]{2.19f,6.9f})
                ScienceBox(stone,new Vector3(x,deck+2.04f,2.56f),new Vector3(.35f,4.08f,2.18f));
            ScienceBox(metal,new Vector3(4.55f,deck+3.17f,3.23f),new Vector3(.13f,1.68f,.32f));
            ScienceBox(stone,new Vector3(4.55f,deck+2.31f,1.74f),new Vector3(4.85f,.17f,.65f));
            foreach(float x in new[]{2.72f,6.3f})
                ScienceBox(stone,new Vector3(x,deck+.21f,-.59f),new Vector3(.38f,.42f,.77f));
            ScienceBox(art.Surface("root"),new Vector3(4.51f,deck+.49f,-.59f),new Vector3(4.12f,.16f,.88f));
            foreach(float x in new[]{2.72f,6.30f})
                ScienceBox(metal,new Vector3(x,deck+.90f,-.20f),new Vector3(.12f,.93f,.14f));
            foreach(float level in new[]{.84f,1.27f})
                ScienceBox(art.Surface("root"),new Vector3(4.51f,deck+level,-.21f),new Vector3(4.12f,.22f,.14f));
            foreach(float x in new[]{2.48f,6.54f})
                ScienceBox(metal,new Vector3(x,deck+.77f,-.50f),new Vector3(.13f,.55f,.65f));
            // A sheltered edge with a deep soffit sweeps forward over the seat and leaves the landing uncovered.
            MeshBuilder slate = art.Surface("slate");
            for(int x=0;x<12;x++) for(int z=0;z<6;z++)
            {
                float u=x/12f, v=z/6f;
                LandmarkPanel(art,slate,TerraceCanopy(u,v),TerraceCanopy((x+1)/12f,v),
                    TerraceCanopy((x+1)/12f,(z+1)/6f),TerraceCanopy(u,(z+1)/6f),.42f,x==0,x==11,z==0,z==5);
            }
            foreach(float z in new[]{1.75f,7.35f})
            {
                ScienceRib(stone,new Vector3(7.64f,deck,z),new Vector3(7.8f,12.08f,z),
                    new Vector3(4.45f,14.47f,z),.33f,.38f);
                ScienceRib(stone,new Vector3(.65f,deck,z),new Vector3(.45f,12.42f,z),
                    new Vector3(4.55f,14.47f,z),.28f,.31f);
            }
            foreach(float x in new[]{-7.66f,7.64f})
            {
                SciencePlanter(art,new Vector3(x,deck,-.1f),2.6f);
                // Planting spills over the carved lip. Leaf panels never enter the physical mesh.
                for(int i=0;i<7;i++)
                {
                    float dx=(i-3)*.16f, tip=deck-.36f-.13f*((i*3)%5);
                    Vector3 a=new Vector3(x+dx,deck+.64f,-1.05f);
                    Vector3 b=new Vector3(x+dx+.24f,deck+.34f,-1.86f);
                    Vector3 c=new Vector3(x+dx-.06f,tip,-1.95f);
                    Vector3 d=b-Vector3.right*.32f;
                    ScienceFace(art.Surface("leaf"),a,b,c,d,Vector3.back);
                    ScienceFace(art.Surface("leaf"),d,c,b,a,Vector3.forward);
                }
            }
        }

        private static Vector3 TerraceCanopy(float u,float v)
        {
            float bow=4*u*(1-u);
            return new Vector3(-.5f+9.95f*u,12.28f+2.05f*bow+.7f*u+.24f*v,
                Mathf.Lerp(-1.35f-2.25f*bow,9.3f,v));
        }

        // Continuous world-metre roof UVs, separately closed soffit and exposed end grain.
        private static void LandmarkPanel(ScienceDistrictArt art,MeshBuilder roof,Vector3 a,Vector3 b,
            Vector3 c,Vector3 d,float depth,bool left,bool right,bool front,bool back)
        {
            Vector3 down=Vector3.down*depth;
            ScienceRoofFace(roof,a,b,c,d,Vector3.up);
            ScienceRoofFace(roof,a+down,b+down,c+down,d+down,Vector3.down);
            MeshBuilder fascia=art.Surface("masonry");
            if(left) LandmarksFascia(fascia,a,d,down,Vector3.left);
            if(right) LandmarksFascia(fascia,b,c,down,Vector3.right);
            if(front) LandmarksFascia(fascia,a,b,down,Vector3.back);
            if(back) LandmarksFascia(fascia,d,c,down,Vector3.forward);
        }

        private static void LandmarksFascia(MeshBuilder mesh,Vector3 a,Vector3 b,Vector3 down,Vector3 face)
        {
            int first=mesh.VertexCount;
            ScienceFace(mesh,a,b,b+down,a+down,face);
            for(int i=first;i<mesh.VertexCount;i++)
            {
                Vector3 p=mesh.Vertices[i];
                mesh.UVs[i]=new Vector2(face.x!=0?p.z:p.x,p.y)*.5f;
            }
        }

        private static void ForgeMonitor(ScienceDistrictArt art)
        {
            MeshBuilder metal=art.Surface("iron"), stone=art.Surface("masonry"), slate=art.Surface("slate");
            // Two pitches meet off centre. Only the perimeter of the throat is roofed.
            float[] x={-8.8f,1.4f,8.8f}, y={5.8f,8.42f,8.42f}, z={-1,5.4f,12.6f,25};
            for(int bay=0;bay<3;bay++)for(int span=0;span<2;span++)
            {
                if(bay==1 && span==1)continue;
                LandmarkPanel(art,slate,new Vector3(x[span],y[span],z[bay]),new Vector3(x[span+1],y[span+1],z[bay]),
                    new Vector3(x[span+1],y[span+1],z[bay+1]),new Vector3(x[span],y[span],z[bay+1]),
                    .48f,span==0,span==1 || bay==1,bay==0 || (span==1 && bay==2),bay==2 || (span==1 && bay==0));
            }
            foreach(float at in new[]{.4f,5.45f,12.55f,24.2f})
            {
                foreach(int side in new[]{-1,1})
                {
                    float height=side<0?5.5f:8.1f;
                    ScienceBox(stone,new Vector3(side*8.43f,height*.5f,at),new Vector3(.74f,height,.96f));
                    ScienceBox(metal,new Vector3(side*8.43f,height-.28f,at),new Vector3(.97f,.4f,1.17f));
                }
                ScienceRib(metal,new Vector3(-8.43f,5.36f,at),new Vector3(-3.5f,6.85f,at),
                    new Vector3(1.4f,7.95f,at),.24f,.36f);
            }
            // A collector above the existing east core joins the flue through the break in its side roof.
            ScienceRib(metal,new Vector3(11.2f,4.88f,9),new Vector3(11.2f,8.65f,9),
                new Vector3(8.35f,9.12f,9),.63f,.78f);
            // Faceted square-to-octagonal transition. Outer wall, lined bore and rim all carry collision.
            Vector3 centre=new Vector3(5.1f,0,9);
            float[] heights={8.42f,9.34f,11.76f,13.1f,15.55f,16.34f};
            Vector2[] half={new Vector2(3.7f,3.6f),new Vector2(3.4f,3.32f),new Vector2(1.65f,1.62f),
                new Vector2(1.65f,1.62f),new Vector2(2.84f,2.6f),new Vector2(3.32f,3.04f)};
            for(int face=0;face<8;face++)
            {
                Vector3 dir=ForgeRingDirection(face), next=ForgeRingDirection((face+1)%8);
                Vector3 outward=(dir+next).normalized;
                for(int j=0;j<heights.Length-1;j++)
                {
                    Vector3 a=ForgePoint(centre,dir,half[j],heights[j],j==0), b=ForgePoint(centre,next,half[j],heights[j],j==0);
                    Vector3 c=ForgePoint(centre,next,half[j+1],heights[j+1],false), d=ForgePoint(centre,dir,half[j+1],heights[j+1],false);
                    Vector3 insetA=new Vector3(dir.x*.32f,0,dir.z*.32f),insetB=new Vector3(next.x*.32f,0,next.z*.32f);
                    ScienceFace(j==1 || j==3?stone:metal,a,b,c,d,outward);
                    ScienceFace(art.Surface("basalt"),a-insetA,b-insetB,c-insetB,d-insetA,-outward);
                    if(j==0)ScienceFace(metal,a,b,b-insetB,a-insetA,Vector3.down);
                    if(j==heights.Length-2)ScienceFace(metal,d,c,c-insetB,d-insetA,Vector3.up);
                }
                if(face%2==0)
                {
                    Vector3 root=ForgePoint(centre,dir,half[1],heights[1],false);
                    Vector3 neck=ForgePoint(centre,dir,half[3],heights[3],false);
                    Vector3 rim=ForgePoint(centre,dir,half[5],heights[5],false);
                    ScienceRib(metal,root,neck+dir*.95f,rim+dir*.1f,.18f,.22f);
                }
            }
            // A recessed amber service lens on the public face responds only to Dante's actual station.
            ScienceBox(metal,new Vector3(5.1f,12.46f,7.34f),new Vector3(1.53f,.61f,.36f));
            ScienceBox(art.Surface("forge-lens"),new Vector3(5.1f,12.46f,7.14f),new Vector3(1.15f,.31f,.07f));
        }

        private static Vector3 ForgeRingDirection(int i)
        {
            float angle=i*Mathf.PI*.25f;
            return new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
        }

        private static Vector3 ForgePoint(Vector3 centre,Vector3 dir,Vector2 half,float y,bool square)
        {
            if(square)dir/=Mathf.Max(Mathf.Abs(dir.x),Mathf.Abs(dir.z));
            return centre+new Vector3(dir.x*half.x,y,dir.z*half.y);
        }
    }
}
