using System;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class VisualRebirth04AArchitecture
    {
        /// <summary>Recessed rose tracery and stepped mineral reveals. Uses existing material batches and LODs.</summary>
        private static void RenaissanceFrontage04H(Building building, RectF f, float bottom, float top,
            int style, bool detailed, Func<Material, MeshBuilder> batch)
        {
            MeshBuilder trim = batch(Surface(style,"trim"));
            MeshBuilder metal = batch(Surface(style,"metal"));
            Tracery04H(trim,metal,building.Seed,f,bottom,top,style,detailed,Settings.Graphics);
        }

        /// <summary>Actual facade geometry, also callable by offline geometry QA without materials or a renderer.</summary>
        public static void Tracery04H(MeshBuilder trim,MeshBuilder metal,int seed,RectF f,float bottom,float top,
            int style,bool detailed,GraphicsPreset preset)
        {
            float height=top-bottom;
            if(height<7f || !f.Valid || trim==null || metal==null) return;
            // One or two faces per building; no props, lights, colliders or navigation changes.
            int faces = detailed && preset >= GraphicsPreset.High ? 2 : 1;
            int segments = detailed ? (preset == GraphicsPreset.Ultra ? 20 : 14) : 8;
            for (int face=0;face<faces;face++)
            {
                int side = ((seed & 3) + face*2) % 4;
                Quaternion rot = QMath.AY(side*90f);
                Vector3 at = FaceCenter(f,bottom,side);
                float width = side%2==0?f.Width:f.Depth;
                float radius = Mathf.Min(1.45f,Mathf.Min(width*.13f,height*.12f));
                Vector3 centre = at+Vector3.up*Mathf.Min(height-2.1f,5.8f)-rot*Vector3.forward*.10f;
                for (int ring=0;ring<(detailed?2:1);ring++)
                {
                    float r = radius*(ring==0?1f:.74f);
                    for (int i=0;i<segments;i++)
                    {
                        float a=i*Mathf.PI*2f/segments,b=(i+1)*Mathf.PI*2f/segments;
                        Vector3 p=centre+rot*new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0f);
                        Vector3 q=centre+rot*new Vector3(Mathf.Cos(b)*r,Mathf.Sin(b)*r,0f);
                        Rod04H(trim,p,q,ring==0?.075f:.04f);
                    }
                }
                int petals = style==2?6:8;
                for (int i=0;i<petals;i++)
                {
                    float a=i*Mathf.PI*2f/petals;
                    Vector3 tip=centre+rot*new Vector3(Mathf.Cos(a),Mathf.Sin(a),0f)*radius*.72f;
                    Rod04H(metal,centre,tip,.026f);
                }
                // Three stepped reveals form a deep architectural surround above all walkable entrances.
                if (detailed)
                    for (int step=0;step<3;step++)
                    {
                        float half=radius+ .25f+step*.14f;
                        Vector3 c=centre+rot*new Vector3(0f,half,-.03f-step*.04f);
                        trim.Box(0,c,new Vector3(half*2f,.10f,.12f),rot,UVMode.World);
                        for(int sign=-1;sign<=1;sign+=2)
                            trim.Box(0,centre+rot*new Vector3(sign*half,0f,-.03f-step*.04f),
                                new Vector3(.10f,half*2f,.12f),rot,UVMode.World);
                    }
            }
        }

        private static void Rod04H(MeshBuilder mesh,Vector3 a,Vector3 b,float radius)
        {
            Vector3 delta=b-a;
            float length=delta.magnitude;
            if(length<.001f) return;
            // Four planar sections, no repeated end spheres at each ring joint.
            mesh.Cylinder(0,a,radius,radius,length,4,QMath.FromTo(Vector3.up,delta/length),false,UVMode.World);
        }
    }
}
