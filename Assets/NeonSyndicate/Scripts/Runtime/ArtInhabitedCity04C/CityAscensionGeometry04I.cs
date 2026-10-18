using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Original pointed vaults, portal ribs and service infrastructure in the existing arrival venues.</summary>
    public static class CityAscensionGeometry04I
    {
        public static void ResetStatics() { }
        public static bool Applies(string id)
        { return id=="lofts" || id==VenueIds04E.Dispatch || id==VenueIds04E.Pressure || id==VenueIds04E.Bondkeeper; }

        /// <summary>Pure geometry. All occupied floor/door/stair/quest volumes stay clear; no new collision or lights.</summary>
        public static void Venue(string id,MeshBuilder trim,MeshBuilder metal,MeshBuilder stone,bool detailed,GraphicsPreset preset)
        {
            if(!Applies(id) || trim==null || metal==null || stone==null) return;
            int ribs=detailed?(preset>=GraphicsPreset.High?5:3):2;
            int sections=detailed?(preset==GraphicsPreset.Ultra?16:12):6;
            bool pressure=id==VenueIds04E.Pressure;
            // Each vault is a shaped closed extrusion, not a row of floating boxes or luminous hoops.
            for(int i=0;i<ribs;i++)
            {
                float z=2.8f+i*12.2f/(ribs-1);
                Arch(trim,new Vector3(0f,3.38f,z),7.40f,1.18f,.105f,.14f,sections);
                if(detailed)
                    Arch(metal,new Vector3(0f,3.40f,z+.17f),7.40f,1.13f,.026f,.035f,sections);
            }
            // Full-height fluted jambs are flush to the old wall; no floor prop can block a resident or the stair.
            for(int side=-1;side<=1;side+=2)
            {
                for(int i=0;i<ribs;i++)
                {
                    float z=2.8f+i*12.2f/(ribs-1);
                    trim.Box(0,new Vector3(side*7.57f,1.83f,z),new Vector3(.13f,3.06f,.28f),UVMode.World);
                    if(detailed) metal.Box(0,new Vector3(side*7.485f,1.84f,z),new Vector3(.025f,2.9f,.046f),UVMode.World);
                }
                // Raised pipes, brackets and mineral transom panels explain the district's crystal services.
                int runs=pressure?3:2;
                for(int run=0;run<runs;run++)
                {
                    Vector3 a=new Vector3(side*(7.48f-run*.065f),3.12f+run*.17f,1.3f);
                    metal.Cylinder(0,a,.035f,.035f,15.2f,6,QMath.AX(90f),true,UVMode.World);
                }
                for(int bay=0;bay<(detailed?4:2);bay++)
                {
                    Vector3 c=new Vector3(side*4.25f,2.08f,-.26f);
                    float x=(bay-((detailed?4:2)-1)*.5f)*.66f;
                    Diamond(trim,c+new Vector3(x,0f,0f),.28f,.72f,.075f);
                }
            }
            // The new twin portal has its lowest crosspiece above the existing 3.2m entrance clearance.
            Arch(stone,new Vector3(0f,3.23f,-.48f),2.14f,1.24f,.19f,.18f,sections);
            Arch(trim,new Vector3(0f,3.26f,-.69f),1.87f,1.03f,.07f,.06f,sections);
            for(int side=-1;side<=1;side+=2)
            {
                stone.Box(0,new Vector3(side*2.14f,1.78f,-.40f),new Vector3(.28f,2.9f,.40f),UVMode.World);
                trim.Box(0,new Vector3(side*2.14f,3.11f,-.42f),new Vector3(.47f,.22f,.49f),UVMode.World);
            }
            // 04I follow-up: overhead civic sigils and wall-mounted carved service ribs give four
            // arrival venues a readable fantasy silhouette in normal interior play. They are non-colliding,
            // keep the full stair and corridor volume clear and scale with the existing near/far mesh LOD.
            if(detailed && preset>=GraphicsPreset.Medium)
            {
                int sigils=preset==GraphicsPreset.Ultra?4:3;
                for(int i=0;i<sigils;i++)
                {
                    float z=4.1f+i*(preset==GraphicsPreset.Ultra?3.3f:4.8f);
                    // The three faceted keystones sit above the occupied routes at y>=3.55m.
                    for(int shard=-1;shard<=1;shard++)
                    {
                        Vector3 keystone=new Vector3(shard*.54f,3.95f,z);
                        Diamond(shard==0?stone:trim,keystone,.18f,.28f,.08f);
                    }
                    // Side relief is kept outside the |x|<7.3m reserved traffic/stair width.
                    for(int side=-1;side<=1;side+=2)
                    {
                        Vector3 wall=new Vector3(side*7.58f,2.76f,z);
                        Diamond(pressure?metal:trim,wall,.11f,.25f,.055f);
                        metal.Box(0,wall+Vector3.up*.52f,new Vector3(.08f,.20f,.12f),UVMode.World);
                    }
                }
            }
        }

        private static Vector3 Point(Vector3 c,float width,float rise,float u)
        { return c+new Vector3(Mathf.Lerp(-width,width,u),rise*(1f-Mathf.Pow(Mathf.Abs(u*2f-1f),.72f)),0f); }

        /// <summary>A pointed arch extruded as a closed four-sided strip. UVs use real metres.</summary>
        private static void Arch(MeshBuilder b,Vector3 c,float width,float rise,float thickness,float depth,int sections)
        {
            for(int i=0;i<sections;i++)
            {
                Vector3 a=Point(c,width,rise,(float)i/sections),d=Point(c,width,rise,(float)(i+1)/sections);
                Vector3 tangent=(d-a).normalized, n=new Vector3(-tangent.y,tangent.x,0f)*thickness;
                Vector3 z=Vector3.forward*depth;
                Face(b,a-n-z,d-n-z,d+n-z,a+n-z,Vector3.back);
                Face(b,a+n+z,d+n+z,d-n+z,a-n+z,Vector3.forward);
                Face(b,a+n-z,d+n-z,d+n+z,a+n+z,n.normalized);
                Face(b,a-n+z,d-n+z,d-n-z,a-n-z,-n.normalized);
                if(i==0) Face(b,a-n+z,a-n-z,a+n-z,a+n+z,-tangent);
                if(i==sections-1) Face(b,d-n-z,d-n+z,d+n+z,d+n-z,tangent);
            }
        }
        private static void Face(MeshBuilder b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,Vector3 n)
        {
            // Explicit outward winding keeps the source usable on both render paths with backface culling.
            if(Vector3.Dot(Vector3.Cross(c-a,d-a),n)<0f) { Vector3 t=c;c=e;e=t; }
            b.Quad(0,a,c,d,e,n,new Vector2(a.x,a.y),new Vector2(c.x,c.y),new Vector2(d.x,d.y),new Vector2(e.x,e.y));
        }
        private static void Diamond(MeshBuilder b,Vector3 c,float width,float height,float depth)
        {
            Vector3 a=c+Vector3.up*height,d=c+Vector3.right*width,e=c-Vector3.up*height,f=c-Vector3.right*width;
            Vector3 apex=c+Vector3.back*depth;
            Vector3[] rim={a,d,e,f};
            for(int i=0;i<4;i++)
            {
                Vector3 p= rim[i],q=rim[(i+1)%4],n=Vector3.Cross(q-p,apex-p).normalized;
                if(n.z>0f) b.Triangle(0,q,p,apex,-n,Vector2.zero,Vector2.right,Vector2.one);
                else b.Triangle(0,p,q,apex,n,Vector2.zero,Vector2.right,Vector2.one);
            }
        }
    }
}
