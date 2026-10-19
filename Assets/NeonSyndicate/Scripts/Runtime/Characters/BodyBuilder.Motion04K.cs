using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        // Tailored volume, not a second renderer. Uses the same palette, UV/detail streams and skin.
        private void BuildMotion04K()
        {
            if (_look.Creature != HumanoidCreature.None || _detail == 0) return;
            int budget = _detail == 2 ? 13500 : 8200;
            if (_d.Vertices.Count + 1000 > budget) return;
            float s = _s;
            // Lower orbital folds close the visible gap below the eyes. Existing eyeballs and brows remain.
            if (_look.VisorStyle == 0 && _look.Headwear != Headwear.Helmet)
                for (int side = -1; side <= 1; side += 2)
                {
                    float centre = 90f + side * 20f;
                    EmitPatch(_head, HeadT(HY(.019f)*s), HeadT(HY(.026f)*s),
                        centre-10f, centre+10f, 2, 5, .0018f*s, .001f*s, SkinUV, SkinUV);
                }
            if(_look.SuitStyle==3)
            {
                // Layered role armour stays inside the same skin and existing dense-outfit budget.
                for(int side=0;side<2;side++)
                {
                    float angle=side==0?165f:-15f;
                    if(_look.ShellStyle>=2)
                        EmitPatch(_upper[side],-.04f,.32f,angle-40f,angle+40f,4,5,.024f*s,.014f*s,ArmorUV,TrimUV);
                    else if(_look.Jacket)
                        EmitPatch(_torso,.35f,.86f,side==0?155f:345f,side==0?189f:379f,
                            5,4,.025f*s,.008f*s,JacketUV,TrimUV);
                    else
                        EmitPatch(_fore[side],.23f,.75f,angle-35f,angle+35f,4,4,.019f*s,.008f*s,ArmorUV,DarkTechUV);
                }
                return;
            }
            if (!_look.Jacket) return;
            // Thick curved lapels and collar roll meet the coat shell; no disconnected flat shoulder shards.
            float span = (TorsoTop-TorsoBottom)*s;
            for (int side = 0; side < 2; side++)
            {
                float a = side == 0 ? 107f : 59f;
                EmitPatch(_torso, (1.24f-TorsoBottom)*s/span, (1.48f-TorsoBottom)*s/span,
                    a, a+14f, 7, 3, .019f*s, .009f*s, JacketUV, TrimUV);
                int thigh = side == 0 ? RigLayout.ThighL : RigLayout.ThighR;
                float sign = side == 0 ? -1f : 1f;
                // Articulated boot tongue and cuff retain the original foot/ankle articulation.
                EmitPatch(_shin[side], .64f, .91f, 64f, 116f, 3, 4,
                    .015f*s, .007f*s, ShoesUV, TrimUV);
                // Broad leather elbow inset on the sleeve, weighted by its original surface.
                EmitPatch(_fore[side], .06f, .28f, side==0?145f:-35f, side==0?215f:35f,
                    3, 4, .005f*s, .004f*s, DarkTechUV, JacketUV);
                // A physical curved coat hem with blended hip/thigh weights, open down the centre.
                Loft hem = new Loft(new Vector3(sign*.16f,1.04f,-.12f)*s,
                    new Vector3(sign*.18f,.78f,-.14f)*s,Vector3.right,Vector3.forward,
                    new float[]{0f,.07f*s,.012f*s,0f,0f,2.5f,.6f,.084f*s,.018f*s,0f,-.012f*s,2.8f,1f,.075f*s,.012f*s,0f,0f,2.5f});
                hem.Weight=(t,th)=>W2(RigLayout.Hips,thigh,.42f*SS(.1f,.9f,t));
                EmitLoft(hem,0f,1f,5,6,t=>t>.9f?TrimUV:JacketUV,true,true);
            }
        }
    }
}
