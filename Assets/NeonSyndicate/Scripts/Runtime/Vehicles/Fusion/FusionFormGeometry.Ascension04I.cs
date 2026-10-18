using UnityEngine;

namespace NeonSyndicate
{
    public static partial class FusionFormGeometry
    {
        /// <summary>Inset radiator ribs and overlapping mineral cheeks in the existing body part, clear of rider and legs.</summary>
        private static void AscensionFairing04I(FormBlueprint bp)
        {
            if(bp.Parts.Count==0) return;
            FusionFrame f=bp.Frame;
            MeshBuilder b=bp.Parts[0].Mesh;
            int rows=bp.Low?2:5;
            float z0=Mathf.Max(f.LegFrontZ+.08f,f.BarZ+.24f),z1=f.NoseZ-.04f;
            if(z1<=z0) return;
            for(int side=-1;side<=1;side+=2)
            for(int row=0;row<rows;row++)
            {
                float z=Mathf.Lerp(z0,z1,(row+.5f)/rows),bottom,top,half;
                f.HullAt(z,out bottom,out top,out half);
                float y=Mathf.Lerp(bottom,top,.5f);
                float length=(z1-z0)/rows*.85f;
                float width=bp.Form==FusionIds.Kilnback?.10f:.06f;
                // A closed six-sided wedge with a raised ridge, authored directly in the source mesh.
                Vector3[] p={
                    new Vector3(half+.009f,y-.06f,z-length*.5f),new Vector3(half+.009f,y-.06f,z+length*.5f),
                    new Vector3(half+width,y-.02f,z+length*.28f),new Vector3(half+width,y-.02f,z-length*.28f),
                    new Vector3(half+.009f,y+.09f,z-length*.5f),new Vector3(half+.009f,y+.09f,z+length*.5f),
                    new Vector3(half+width,y+.06f,z+length*.28f),new Vector3(half+width,y+.06f,z-length*.28f)};
                HexaSided(b,bp.Form==FusionIds.Coilrunner?FormSlots.Metal:FormSlots.Shell,p,side);
                Rod(b,FormSlots.Metal,new Vector3(side*(half+width*.8f),y+.065f,z-length*.25f),
                    new Vector3(side*(half+width*.8f),y+.065f,z+length*.25f),.009f,.009f,bp.Low?4:6);
            }
        }
    }
}
