using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Original civic masonry and working-room details, appended to existing batches. No new collider/light owner.</summary>
    public static class CityLivingGeometry04M
    {
        public static bool InCorridor(Vector3 point, Vector3 from, Vector3 to, float radius = 75f)
        {
            Vector3 line=MathUtil.Flat(to-from), delta=MathUtil.Flat(point-from);
            float length=line.sqrMagnitude;
            float t=length>.001f?Mathf.Clamp01(Vector3.Dot(delta,line)/length):0f;
            return (delta-line*t).sqrMagnitude<=radius*radius;
        }

        public static void Frontage(MeshBuilder stone,MeshBuilder metal,MeshBuilder crystal,RectF f,float bottom,float top,bool near,bool canal)
        {
            if(!f.Valid || top-bottom<5.5f)return;
            for(int face=0;face<4;face++)
            {
                float width=face%2==0?f.Width:f.Depth;
                if(width<6f)continue;
                Quaternion rot=QMath.AY(face*90f);
                Vector3 origin=face==0?new Vector3(f.CenterX,bottom,f.Z1):face==1?new Vector3(f.X1,bottom,f.CenterZ):
                    face==2?new Vector3(f.CenterX,bottom,f.Z0):new Vector3(f.X0,bottom,f.CenterZ);
                int bays=near?Mathf.Clamp((int)(width/5f),2,5):2;
                for(int i=0;i<bays;i++)
                {
                    float x=(i+.5f)*width/bays-width*.5f;
                    float span=Mathf.Min(3.8f,width/bays-.4f);
                    // A sloping mineral canopy and closed service transom. Lowest outward detail is above head height.
                    stone.Box(0,origin+rot*new Vector3(x,3.42f,.18f),new Vector3(span,.20f,.70f),rot*QMath.AX(-12f),UVMode.World);
                    metal.Box(0,origin+rot*new Vector3(x,3.66f,.15f),new Vector3(span,.09f,.09f),rot,UVMode.World);
                    for(int side=-1;side<=1;side+=2)
                        metal.Box(0,origin+rot*new Vector3(x+side*span*.46f,3.07f,.03f),new Vector3(.09f,.55f,.15f),rot,UVMode.World);
                    if(!near)continue;
                    for(int pane=0;pane<3;pane++)
                    {
                        float px=x+(pane-1)*span*.25f;
                        metal.Box(0,origin+rot*new Vector3(px,4.35f,.04f),new Vector3(.055f,.84f,.08f),rot,UVMode.World);
                        crystal.Box(0,origin+rot*new Vector3(px,4.35f,.065f),new Vector3(span*.15f,.28f,.035f),rot*QMath.AZ(canal?25f:45f),UVMode.World);
                    }
                }
                // Ground-level wall relief stays within 12cm of the original solid facade. No street furniture in walking lanes.
                for(int side=-1;side<=1;side+=2)
                {
                    float x=side*(width*.5f-.30f);
                    stone.Box(0,origin+rot*new Vector3(x,1.58f,-.02f),new Vector3(.24f,2.85f,.18f),rot,UVMode.World);
                    if(near)metal.Box(0,origin+rot*new Vector3(x,1.62f,.085f),new Vector3(.055f,2.4f,.045f),rot,UVMode.World);
                }
                if(canal)
                {
                    // Canal Choir's waterline grammar: three stepped copper waves rather than the arrival's pointed transoms.
                    for(int wave=0;wave<(near?5:3);wave++)
                        metal.Box(0,origin+rot*new Vector3((wave-(near?2:1))* .36f,4.85f+(wave%2)*.14f,.12f),
                            new Vector3(.42f,.08f,.08f),rot*QMath.AZ(wave%2==0?18f:-18f),UVMode.World);
                }
                else crystal.Box(0,origin+rot*new Vector3(0f,4.95f,.12f),new Vector3(.40f,.40f,.10f),rot*QMath.AZ(45f),UVMode.World);
            }
        }

        public static bool HasRoom(string id)
        {return id==VenueIds04E.Dispatch || id==VenueIds04E.Pressure || id==VenueIds04E.Bondkeeper;}

        public static void Room(string id,MeshBuilder stone,MeshBuilder metal,MeshBuilder crystal,bool near)
        {
            if(!HasRoom(id))return;
            int count=near?4:2;
            for(int i=0;i<count;i++)
            {
                float z=4f+i*10f/(count-1);
                // All equipment is a wall relief outside the existing |x|<7.2m occupied route/stair volume.
                float x=id==VenueIds04E.Pressure?-7.57f:7.57f;
                stone.Box(0,new Vector3(x,2.25f,z),new Vector3(.16f,1.65f,.85f),UVMode.World);
                metal.Box(0,new Vector3(x*.986f,2.25f,z),new Vector3(.065f,1.42f,.69f),UVMode.World);
                if(id==VenueIds04E.Dispatch)
                {
                    for(int cell=0;cell<(near?4:2);cell++)
                        crystal.Box(0,new Vector3(x*.979f,1.76f+cell*.28f,z),new Vector3(.035f,.09f,.46f),UVMode.World);
                }
                else if(id==VenueIds04E.Pressure)
                {
                    for(int fin=0;fin<(near?6:3);fin++)
                        metal.Box(0,new Vector3(x*.976f,1.65f+fin*(near?.22f:.44f),z),new Vector3(.08f,.06f,.60f),UVMode.World);
                }
                else
                {
                    // Refuge's carved feeding niches remain visual; care and earned ownership belong to BONDS.
                    crystal.Box(0,new Vector3(x*.977f,2.5f,z),new Vector3(.045f,.50f,.25f),QMath.AX(35f),UVMode.World);
                    stone.Box(0,new Vector3(x*.985f,1.48f,z),new Vector3(.12f,.10f,.72f),UVMode.World);
                }
            }
        }
    }
}
