using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Venue-specific overhead infrastructure, batched into the existing near/far meshes.</summary>
    public static class CityMotionGeometry04K
    {
        public static void Venue(string id,MeshBuilder stone,MeshBuilder metal,bool near)
        {
            if(id!=VenueIds04E.Dispatch && id!=VenueIds04E.Pressure) return;
            int count=near?5:3;
            for(int i=0;i<count;i++)
            {
                float z=3f+i*11f/(count-1);
                if(id==VenueIds04E.Dispatch)
                {
                    // A deep ribbed dispatch coffer suspended above the old receivers and traffic lanes.
                    for(int side=-1;side<=1;side+=2)
                    {
                        metal.Box(0,new Vector3(side*2.3f,3.65f,z),new Vector3(.16f,.40f,1.3f),UVMode.World);
                        stone.Box(0,new Vector3(side*4.2f,3.7f,z),new Vector3(2.8f,.20f,.18f),UVMode.World);
                    }
                    metal.Box(0,new Vector3(0f,3.95f,z),new Vector3(4.8f,.12f,.12f),UVMode.World);
                }
                else
                {
                    // Pressure House: paired mineral heat exchangers around the original service stair.
                    for(int side=-1;side<=1;side+=2)
                    {
                        metal.Cylinder(0,new Vector3(side*7.55f,2.65f,z),.10f,.13f,.6f,near?8:4,QMath.Identity,true,UVMode.World);
                        stone.Box(0,new Vector3(side*7.50f,3.3f,z),new Vector3(.18f,.25f,.6f),UVMode.World);
                    }
                }
            }
        }
    }
}
