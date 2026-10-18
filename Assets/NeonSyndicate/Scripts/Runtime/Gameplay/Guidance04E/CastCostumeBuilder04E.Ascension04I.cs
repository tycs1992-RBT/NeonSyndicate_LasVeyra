using UnityEngine;

namespace NeonSyndicate
{
    public static partial class CastCostumeBuilder04E
    {
        /// <summary>Corin's dispatch harness and Tamsin's reinforced work bib preserve their original faces and tools.</summary>
        private static void ServiceTorso04I(MeshBuilder b,HeldTool tool,float s)
        {
            if(tool!=HeldTool.Tablet && tool!=HeldTool.Wrench) return;
            if(tool==HeldTool.Tablet)
            {
                for(int side=-1;side<=1;side+=2)
                {
                    Bar(b,Main,new Vector3(side*.13f,.2f,.095f)*s,new Vector3(side*.1f,-.23f,.14f)*s,.032f*s,.018f*s);
                    b.Box(Trim,new Vector3(side*.11f,-.04f,.15f)*s,new Vector3(.045f,.026f,.014f)*s,UVMode.Unit);
                }
                b.Box(Main,new Vector3(-.105f,-.14f,.151f)*s,new Vector3(.083f,.10f,.025f)*s,UVMode.Unit);
            }
            else
            {
                // Curved, tapered double-sided bib with folds and stitched binding, not a flat chest box.
                for(int i=0;i<8;i++)
                {
                    float x0=Mathf.Lerp(-.14f,.14f,i/8f),x1=Mathf.Lerp(-.14f,.14f,(i+1)/8f);
                    Vector3 a=new Vector3(x0*.72f,.17f,.13f-Mathf.Abs(x0)*.18f)*s;
                    Vector3 c=new Vector3(x1*.72f,.17f,.13f-Mathf.Abs(x1)*.18f)*s;
                    Vector3 d=new Vector3(x1,-.23f,.146f+Mathf.Cos(i*1.8f)*.005f)*s;
                    Vector3 e=new Vector3(x0,-.23f,.146f+Mathf.Cos((i-1)*1.8f)*.005f)*s;
                    Vector3 n=Vector3.Cross(c-a,d-a).normalized;
                    b.Quad(Main,a,c,d,e,n,Vector2.zero,Vector2.right,Vector2.one,Vector2.up);
                    b.Quad(Main,e,d,c,a,-n,Vector2.up,Vector2.one,Vector2.right,Vector2.zero);
                }
                for(int side=-1;side<=1;side+=2)
                {
                    Bar(b,Trim,new Vector3(side*.1f,.17f,.126f)*s,new Vector3(side*.14f,-.23f,.15f)*s,.012f*s,.01f*s);
                    b.Box(Main,new Vector3(side*.071f,-.10f,.163f)*s,new Vector3(.09f,.095f,.02f)*s,UVMode.Unit);
                    b.Box(Trim,new Vector3(side*.071f,-.06f,.176f)*s,new Vector3(.092f,.013f,.012f)*s,UVMode.Unit);
                }
            }
        }
    }
}
