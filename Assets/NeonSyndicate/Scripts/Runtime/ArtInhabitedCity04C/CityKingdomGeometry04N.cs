using UnityEngine;
namespace NeonSyndicate
{
    /// <summary>Four structural grammars, appended to the original parcel batches; no road/door colliders.</summary>
    public static class CityKingdomGeometry04N
    {
        public static int Identity(District d)
        { return d==District.SunkenRow?1:d==District.OldSilver?2:d==District.GraveFrequency?3:0; }
        public static void Frontage(MeshBuilder stone,MeshBuilder metal,MeshBuilder glass,RectF f,float bottom,float top,int identity,bool near)
        {
            if(!f.Valid || top-bottom<7f)return;
            float cornice=Mathf.Min(top-bottom-.65f,10.5f);
            for(int face=0;face<4;face++)
            {
                float width=face%2==0?f.Width:f.Depth;if(width<7f)continue;
                Quaternion r=QMath.AY(face*90f);
                Vector3 o=face==0?new Vector3(f.CenterX,bottom,f.Z1):face==1?new Vector3(f.X1,bottom,f.CenterZ):face==2?new Vector3(f.CenterX,bottom,f.Z0):new Vector3(f.X0,bottom,f.CenterZ);
                int bays=near?Mathf.Clamp((int)(width/6f),2,4):2;
                for(int i=0;i<bays;i++)
                {
                    float x=(i+.5f)*width/bays-width*.5f;float span=Mathf.Min(4.5f,width/bays-.5f);
                    if(identity==0) // Lofts: inhabited, layered stone loggias and balustrades.
                    {
                        stone.Box(0,o+r*new Vector3(x,5.8f,-.10f),new Vector3(span,.28f,1.05f),r,UVMode.World);
                        metal.Box(0,o+r*new Vector3(x,6.65f,.33f),new Vector3(span,.10f,.09f),r,UVMode.World);
                        int bars=near?6:3;for(int j=0;j<bars;j++)metal.Box(0,o+r*new Vector3(x+(j/(float)(bars-1)-.5f)*span,6.23f,.33f),new Vector3(.075f,.8f,.075f),r,UVMode.World);
                        for(int side=-1;side<=1;side+=2)stone.Box(0,o+r*new Vector3(x+side*span*.48f,6.86f,-.04f),new Vector3(.20f,1.9f,.40f),r,UVMode.World);
                    }
                    else if(identity==1) // Sunken Row: stepped flood ribs and copper sluice wheels.
                    {
                        for(int j=0;j<3;j++)stone.Box(0,o+r*new Vector3(x,5.3f+j*.38f,-.02f-j*.09f),new Vector3(span-j*.48f,.19f,.45f),r,UVMode.World);
                        metal.Cylinder(0,o+r*new Vector3(x,6.9f,.04f),.48f,.48f,.15f,near?12:6,r*QMath.AX(90f),true,UVMode.World);
                        glass.Box(0,o+r*new Vector3(x,6.9f,.20f),new Vector3(.08f,.65f,.06f),r*QMath.AZ(45f),UVMode.World);
                    }
                    else if(identity==2) // Old Silver: industrial heat stacks, angled bracework and glazed inspection slits.
                    {
                        for(int j=0;j<(near?3:2);j++)metal.Box(0,o+r*new Vector3(x+(j-1)*.35f,6.5f,-.02f),new Vector3(.22f,2.6f,.38f),r,UVMode.World);
                        metal.Box(0,o+r*new Vector3(x,5.15f,.12f),new Vector3(span,.16f,.22f),r,UVMode.World);
                        metal.Box(0,o+r*new Vector3(x,6.4f,.19f),new Vector3(.15f,2.6f,.13f),r*QMath.AZ(32f),UVMode.World);
                        glass.Box(0,o+r*new Vector3(x,7.7f,.06f),new Vector3(span*.5f,.16f,.06f),r,UVMode.World);
                    }
                    else // Grave Frequency: split mineral organs and tall tapered resonator crowns.
                    {
                        int pipes=near?5:3;for(int j=0;j<pipes;j++)
                        {
                            float h=1.1f+(pipes/2-Mathf.Abs(j-pipes/2))*.4f;
                            stone.Box(0,o+r*new Vector3(x+(j-(pipes-1)*.5f)*.28f,5.5f+h*.5f,.02f),new Vector3(.20f,h,.35f),r,UVMode.World);
                            glass.Box(0,o+r*new Vector3(x+(j-(pipes-1)*.5f)*.28f,5.4f+h,.06f),new Vector3(.12f,.25f,.08f),r*QMath.AZ(45f),UVMode.World);
                        }
                    }
                    if(near) // Lit-looking glazing uses original glass material; no invisible new light or VFX spam.
                        for(int j=0;j<2;j++)glass.Box(0,o+r*new Vector3(x+(j==0?-.45f:.45f),4.1f,.035f),new Vector3(.60f,.85f,.04f),r,UVMode.World);
                }
                stone.Box(0,o+r*new Vector3(0,cornice,-.08f),new Vector3(width,.22f,.48f),r,UVMode.World);
                if(near)for(int side=-1;side<=1;side+=2)
                    metal.Box(0,o+r*new Vector3(side*(width*.5f-.65f),cornice*.5f+2f,-.02f),new Vector3(.12f,Mathf.Max(.5f,cornice-4f),.16f),r,UVMode.World);
            }
        }
    }
}
