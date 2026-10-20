using UnityEngine;
namespace NeonSyndicate
{
    public static partial class GothicRealmGeometry04OB
    {
        /// <summary>The exact old landing stays, but open leaning mineral supports replace the continuous tower facade.</summary>
        private static void FracturedReliquary04OC(MeshBuilder stone,MeshBuilder trim,MeshBuilder mineral,Building b)
        {
            RectF f=b.Footprint;Vector3 root=f.Center(CityLayout.CurbHeight);
            Box(stone,root,0,new Vector3(0,b.Height-.3f,0),new Vector3(f.Width,.6f,f.Depth));
            float r=Mathf.Min(f.Width,f.Depth)*.11f;
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 foot=root+new Vector3(x*f.Width*.34f,0,z*f.Depth*.34f);
                Crystal(stone,foot,new Vector3(r*2.4f,b.Height-.3f,r*2.4f),-x*.06f,b.Seed+x+z);
                Crystal(mineral,foot+new Vector3(-x*r*.3f,0,-z*r*.3f),new Vector3(r*.5f,b.Height*.62f,r*.5f),-x*.06f,b.Seed);
            }
            // Broken gabled side fins and crowning stone, without covering any point of the retained landing.
            for(int side=-1;side<=1;side+=2)
            {
                Vector3 a=root+new Vector3(side*(f.Width*.5f-.5f),0,0);
                Box(trim,a,0,new Vector3(0,b.Height-1.3f,0),new Vector3(.7f,1.7f,f.Depth));
                Crystal(stone,a+new Vector3(0,b.Height,side*f.Depth*.33f),new Vector3(1.4f,4f,1.4f),side*.08f,b.Seed+side);
            }
        }
        public static void WildTree04OC(MeshBuilder wood,MeshBuilder leaves,WildScenery04OC s,bool detailed)
        {
            Vector3 p=s.Position;float height=s.Size.y;
            Crystal(wood,p,new Vector3(.85f,height*.74f,.9f),.12f,s.Seed);
            int arms=detailed?5:3;
            for(int n=0;n<arms;n++)
            {
                float angle=(n*137f+(s.Seed%180))*Mathf.Deg2Rad;
                Vector3 side=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                Vector3 branch=p+Vector3.up*(height*(.36f+n*.08f))+side*(.55f+n*.20f);
                Crystal(wood,branch,new Vector3(.4f,height*.36f,.5f),side.x*.7f,s.Seed+n);
                // Heavy sparse leaf/mineral clusters leave creatures readable beneath the canopy.
                Rock(leaves,branch+side*1.3f+Vector3.up*height*.20f,new Vector3(s.Size.x*.85f,1.6f+n*.22f,s.Size.z*.85f),s.Seed+n);
            }
        }
        public static void WildTerrain04OC(MeshBuilder mesh,CityLayout layout,WildPatch04OC patch)
        {
            const int cells=6;
            for(int x=0;x<cells;x++)for(int z=0;z<cells;z++)
            {
                Vector3[] v=new Vector3[4];int[] dx={0,0,1,1},dz={0,1,1,0};
                for(int i=0;i<4;i++)
                {
                    Vector3 p=new Vector3(Mathf.Lerp(patch.Area.X0,patch.Area.X1,(x+dx[i])/(float)cells),0f,Mathf.Lerp(patch.Area.Z0,patch.Area.Z1,(z+dz[i])/(float)cells));
                    p.y=.018f+layout.WildHeight04OC(patch,p);v[i]=p;
                }
                // Native collision and visuals use these same two triangles, no solid box over the terrain.
                for(int i=0;i<2;i++)
                {
                    Vector3 a=v[0],b=v[i+1],c=v[i+2],normal=Vector3.Cross(b-a,c-a).normalized;
                    mesh.Triangle(0,a,b,c,normal,new Vector2(a.x,a.z)*.2f,new Vector2(b.x,b.z)*.2f,new Vector2(c.x,c.z)*.2f);
                }
            }
        }
    }
}
