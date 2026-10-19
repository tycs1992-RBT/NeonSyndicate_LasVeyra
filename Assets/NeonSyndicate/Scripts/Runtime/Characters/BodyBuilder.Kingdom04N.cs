using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        private void BuildKingdom04N()
        {
            if(_detail!=2 || _look.Creature!=HumanoidCreature.None || !_look.Jacket || _look.SuitStyle!=1)return;
            // Hero tailoring follows the original forearm/chest weights and palette. No new bones or ownership grants.
            if(_d.Vertices.Count+400>13500)return;
            float s=_s;
            for(int side=0;side<2;side++)
            {
                int fore=side==0?RigLayout.ForearmL:RigLayout.ForearmR;
                float sign=side==0?-1f:1f;
                for(int i=0;i<3;i++)Plate04F(_L.World[fore]+new Vector3(sign*.018f,-.08f-i*.035f,-.051f)*s,
                    Vector3.back,Vector3.up,.040f*s,.018f*s,.009f*s,DarkTechUV,TrimUV,W1(fore),sign*.12f);
                Plate04F(new Vector3(sign*.185f,1.44f,-.065f)*s,Vector3.back,Vector3.up,.065f*s,.072f*s,.018f*s,
                    JacketUV,TrimUV,W1(RigLayout.Chest),sign*.22f);
            }
        }
    }
}
