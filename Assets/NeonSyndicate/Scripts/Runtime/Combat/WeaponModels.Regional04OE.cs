using System.Collections.Generic;
using UnityEngine;
namespace NeonSyndicate
{
    public static partial class WeaponModels
    {
        private static readonly Dictionary<int,Mesh> _regional04OE=new Dictionary<int,Mesh>();
        /// <summary>Closed faceted blades with different silhouettes, grip winding, guards and ascension inlays.</summary>
        public static void BuildRegional04OE(MeshBuilder mesh,int unique,int ascensions)
        {
            bool oath=unique==RegionalArsenal04OE.Oathblade;
            for(int i=0;i<12;i++)
                Section04F(mesh,0,new Vector3(0,0,-.10f+i*.021f),new Vector3(0,0,-.082f+i*.021f),.019f,.016f,.018f,.015f,8,i*18f);
            Section04F(mesh,1,new Vector3(0,0,-.13f),new Vector3(0,0,-.10f),.026f,.024f,.018f,.016f,8,0);
            for(int side=-1;side<=1;side+=2)
            {
                Section04F(mesh,1,new Vector3(0,0,.16f),new Vector3(side*(oath?.14f:.10f),0,.21f),.024f,.018f,.014f,.009f,6,30);
                Section04F(mesh,2,new Vector3(side*(oath?.14f:.10f),0,.21f),new Vector3(side*(oath?.16f:.13f),0,.29f),.015f,.01f,.001f,.001f,4,45);
            }
            float[] zs=oath?new[]{.18f,.31f,.68f,.99f,1.18f}:new[]{.18f,.33f,.62f,.89f,1.08f};
            float[] widths=oath?new[]{.038f,.072f,.062f,.040f,.0005f}:new[]{.025f,.043f,.060f,.073f,.0005f};
            for(int i=0;i<4;i++)
            {
                float x0=oath?0f:-.045f*zs[i]*zs[i],x1=oath?0f:-.045f*zs[i+1]*zs[i+1];
                Vector3 a=new Vector3(x0,oath?0:.025f*zs[i],zs[i]),b=new Vector3(x1,oath?0:.025f*zs[i+1],zs[i+1]);
                Section04F(mesh,1,a,b,widths[i],.012f,widths[i+1],.001f+i*.002f,4,45);
                // A translucent-looking mineral edge is geometry, not an obscuring particle sheet.
                for(int side=-1;side<=1;side+=2)
                    Section04F(mesh,2,a+Vector3.right*(widths[i]*side*.70f),b+Vector3.right*(widths[i+1]*side*.70f),
                        widths[i]*.22f,.005f,Mathf.Max(.0003f,widths[i+1]*.22f),.003f,4,45);
            }
            for(int i=0;i<Mathf.Clamp(ascensions,0,5);i++)
                Section04F(mesh,2,new Vector3(0,.017f,.34f+i*.095f),new Vector3(0,.028f,.375f+i*.095f),.012f,.008f,.001f,.001f,4,45);
        }
        public static GameObject CreateRegional04OE(LootGun item,Transform parent,out Transform muzzle)
        {
            int key=item.Unique*8+Mathf.Clamp(item.Ascensions,0,5);Mesh mesh;
            if(!_regional04OE.TryGetValue(key,out mesh) || mesh==null)
            {MeshBuilder build=new MeshBuilder(3);BuildRegional04OE(build,item.Unique,item.Ascensions);mesh=build.ToMesh("Regional blade "+key);_regional04OE[key]=mesh;}
            bool oath=item.Unique==RegionalArsenal04OE.Oathblade;
            GameObject model=Shapes.MeshObject(item.Name,mesh,new[]{Mats.DarkMetal,Mats.Metal,Mats.Emissive(oath?new Color(.52f,.23f,.78f):new Color(.12f,.65f,.75f),.7f)},parent,true);
            foreach(string name in new[]{"BladeHilt","BladeTip","Muzzle"})
            {GameObject point=new GameObject(name);point.transform.SetParent(model.transform,false);point.transform.localPosition=name=="BladeHilt"?new Vector3(0,0,.17f):RegionalArsenal04OE.Tip(item.Unique);}
            muzzle=model.transform.Find("Muzzle");return model;
        }
    }
}
