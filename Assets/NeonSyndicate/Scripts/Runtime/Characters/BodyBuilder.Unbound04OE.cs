using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        private void BuildUnbound04OE()
        {
            if(_look.Creature!=HumanoidCreature.None || _detail==0 || !_look.Jacket || _look.SuitStyle!=1)return;
            int budget=_detail==2?13500:8200;
            if(_d.Vertices.Count+650>budget)return;
            int first=_d.Vertices.Count;
            // Curved palm pads and tendons track the original hand weights; never a block glued across finger joints.
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1f:1f;
                EmitPatch(_palm[side],.15f,.72f,side==0?125:35,side==0?165:75,3,4,.004f*_s,.002f*_s,DarkTechUV,TrimUV);
                EmitPatch(_fore[side],.70f,.91f,65,115,3,4,.006f*_s,.003f*_s,JacketUV,DarkTechUV);
                // A soft split shoulder yoke blends chest/upper-arm motion instead of bridging the shoulder as rigid armour.
                Loft yoke=new Loft(new Vector3(sign*.10f,1.49f,-.10f)*_s,new Vector3(sign*.25f,1.42f,-.07f)*_s,Vector3.up,Vector3.forward,
                    new[]{0f,.020f*_s,.032f*_s,0f,0f,2.6f,.5f,.026f*_s,.057f*_s,0f,0f,2.8f,1f,.008f*_s,.024f*_s,0f,0f,2.4f});
                int upper=side==0?RigLayout.UpperArmL:RigLayout.UpperArmR;
                yoke.Weight=(t,angle)=>W2(RigLayout.Chest,upper,.6f*t);
                EmitLoft(yoke,0,1,3,6,t=>t>.8f?TrimUV:JacketUV,true,true);
            }
            _d.UnboundTailoringVertices04OE=_d.Vertices.Count-first;
        }
    }
}
