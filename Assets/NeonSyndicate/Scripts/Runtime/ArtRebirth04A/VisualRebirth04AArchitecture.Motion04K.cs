using System;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class VisualRebirth04AArchitecture
    {
        private static void ArrivalMotion04K(Building building,RectF f,float bottom,float top,int style,bool detailed,Func<Material,MeshBuilder> batch)
        {
            Poi home=Game.Layout!=null?Game.Layout.FindPoi("Veyra Lofts"):null;
            if(home==null || !InArrival04K(f.Center(bottom),home.Position)) return;
            ArrivalButtresses04K(batch(Surface(style,"stone")),batch(Surface(style,"metal")),f,bottom,top,detailed);
        }

        public static bool InArrival04K(Vector3 point,Vector3 home)
        {float x=point.x-home.x,z=point.z-home.z;return x*x+z*z<=180f*180f;}

        public static void ArrivalButtresses04K(MeshBuilder stone,MeshBuilder metal,RectF f,float bottom,float top,bool detailed)
        {
            if(!f.Valid||top-bottom<7f) return;
            for(int side=0;side<4;side++)
            {
                Quaternion rot=QMath.AY(side*90f);
                Vector3 centre=FaceCenter(f,bottom,side);
                float width=(side%2==0?f.Width:f.Depth)*.5f;
                int bays=detailed?3:2;
                for(int bay=0;bay<bays;bay++)
                {
                    float x=Mathf.Lerp(-width+.7f,width-.7f,bay/(bays-1f));
                    // Broad stepped corbels are seated in the wall ABOVE all door and street clearances.
                    Vector3 at=centre+rot*new Vector3(x,4.8f,-.02f);
                    stone.Box(0,at,new Vector3(.32f,1.4f,.34f),rot,UVMode.World);
                    stone.Box(0,at+Vector3.up*.68f,new Vector3(.58f,.22f,.5f),rot,UVMode.World);
                    if(detailed) metal.Box(0,at+rot*new Vector3(0f,0f,.18f),new Vector3(.08f,1.05f,.05f),rot,UVMode.World);
                }
                stone.Box(0,centre+Vector3.up*6f,new Vector3(width*2f,.24f,.34f),rot,UVMode.World);
            }
        }
    }
}
