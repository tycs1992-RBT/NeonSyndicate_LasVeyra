using System;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class VisualRebirth04AArchitecture
    {
        public static void ArrivalTracery04L(MeshBuilder stone, MeshBuilder metal, RectF footprint, float bottom, float top, bool detailed)
        {
            if(!footprint.Valid || top-bottom<9f)return;
            for(int face=0;face<4;face++)
            {
                float span=face%2==0?footprint.Width:footprint.Depth;
                if(span<5f)continue;
                Quaternion rot=QMath.AY(face*90f);
                Vector3 center=FaceCenter(footprint,bottom,face)+Vector3.up*7.6f;
                // Closed rose tracery sits on the facade above all original doors; one batch/material per existing surface.
                int count=detailed?12:8;
                for(int n=0;n<count;n++)
                {
                    float angle=n*360f/count, r=angle*Mathf.Deg2Rad;
                    Vector3 offset=new Vector3((float)Math.Sin(r)*1.05f,(float)Math.Cos(r)*1.05f,.08f);
                    stone.Box(0,center+rot*offset,new Vector3(.45f,.28f,.24f),rot*QMath.AZ(-angle),UVMode.World);
                    if(detailed)
                        metal.Box(0,center+rot*(offset*.55f+Vector3.forward*.16f),new Vector3(.06f,1.05f,.05f),rot*QMath.AZ(-angle),UVMode.World);
                }
                metal.Box(0,center+rot*new Vector3(0,0,.24f),new Vector3(.3f,.3f,.08f),rot*QMath.AZ(45f),UVMode.World);
            }
        }
    }
}
