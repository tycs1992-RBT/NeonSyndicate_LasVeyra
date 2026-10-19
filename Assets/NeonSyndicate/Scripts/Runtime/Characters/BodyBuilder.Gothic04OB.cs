using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        private void BuildGothicTravel04OB()
        {
            // Cloth/leather production follows the worn outfit, not inventory grants or new armour statistics.
            if(_look.Creature!=HumanoidCreature.None || !_look.Jacket || _look.SuitStyle!=1 || _detail==0)return;
            int budget=_detail==2?13500:8200;if(_d.Vertices.Count+750>=budget)return;
            for(int side=0;side<2;side++)
            {
                float sign=side==0?-1f:1f;int thigh=side==0?RigLayout.ThighL:RigLayout.ThighR;
                // Split articulated travel panels with a rolled edge, blended over hip flexion.
                Loft panel=new Loft(new Vector3(sign*.155f,1.025f,-.105f)*_s,new Vector3(sign*.19f,.815f,-.12f)*_s,Vector3.right,Vector3.forward,
                    new[]{0f,.065f*_s,.012f*_s,0f,0f,2.6f,.28f,.078f*_s,.014f*_s,0f,0f,2.4f,1f,.062f*_s,.012f*_s,0f,0f,2.6f});
                panel.Weight=(t,angle)=>W2(RigLayout.Hips,thigh,.15f+.45f*t);
                EmitLoft(panel,0f,1f,4,6,t=>t>.88f?TrimUV:DarkTechUV,true,true);
                // Knees articulate independently of the shin; no rigid plate bridges the deformation hinge.
                EmitPatch(_thigh[side],.75f,.94f,58f,122f,3,4,.016f*_s,.005f*_s,JacketUV,TrimUV);
                EmitPatch(_shin[side],.05f,.21f,60f,120f,3,4,.013f*_s,.004f*_s,DarkTechUV,TrimUV);
            }
        }
    }
}
