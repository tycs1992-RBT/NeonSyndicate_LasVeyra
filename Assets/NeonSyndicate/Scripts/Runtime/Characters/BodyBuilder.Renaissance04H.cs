using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        /// <summary>Original closed hair locks, fitted leatherwork and role-specific armor in the existing skin.</summary>
        private void BuildRenaissance04H()
        {
            if (_detail == 0 || _look.Creature != HumanoidCreature.None) return;
            float s = _s;
            if (_head != null && _look.Headwear == Headwear.None && _look.HairStyle == HairStyle.Undercut)
            {
                // Tapered swept locks sit on the existing scalp cap. A dark root and raised ridge give real relief.
                int locks = _detail == 2 ? 9 : 5;
                for (int i = 0; i < locks; i++)
                {
                    float lane = (i / (locks - 1f) - 0.5f) * 1.65f;
                    Vector3 a = _headCenter + new Vector3(lane * 0.052f, HY(0.066f), 0.073f) * s;
                    Vector3 b = _headCenter + new Vector3(lane * 0.05f - 0.014f, HY(0.115f), -0.06f) * s;
                    float width = (_detail == 2 ? 0.008f : 0.014f) * s;
                    Loft lockMesh = new Loft(a, b, Vector3.right, Vector3.up, new float[]
                    {
                        0f, width * 0.4f, .003f*s, 0f, 0f, 2f,
                        .22f, width, .007f*s, 0f, .027f*s, 2f,
                        .6f, width*.9f, .008f*s, .003f*s, .035f*s, 2f,
                        .86f, width*.65f, .005f*s, .006f*s, .018f*s, 2f,
                        1f, .001f*s, .001f*s, .009f*s, 0f, 2f
                    });
                    lockMesh.Weight = (t, th) => W1(RigLayout.Head);
                    EmitLoft(lockMesh, 0f, 1f, _detail == 2 ? 7 : 5, 6, t => HairUV, true, true);
                }
            }
            if (_look.Jacket && _look.SuitStyle != 3)
            {
                // Fitted belt pouches and coat seams follow the pelvis and chest, leaving elbows and hip flexion clear.
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    Plate04F(new Vector3(sign*.175f, 1.02f, .03f)*s,
                        new Vector3(sign,0f,.3f),Vector3.up,.045f*s,.067f*s,.025f*s,
                        DarkTechUV,TrimUV,W1(RigLayout.Hips),0f);
                    if (_detail == 2)
                    {
                        Plate04F(new Vector3(sign*.105f,1.195f,.157f)*s,Vector3.forward,Vector3.up,
                            .041f*s,.055f*s,.009f*s,JacketUV,TrimUV,W1(RigLayout.Spine),-sign*.15f);
                        Loft seam = new Loft(new Vector3(sign*.15f,1.17f,.133f)*s,
                            new Vector3(sign*.22f,1.45f,.075f)*s, Vector3.right,Vector3.forward,
                            new float[]{0f,.002f*s,.003f*s,0f,0f,2f,1f,.002f*s,.003f*s,0f,0f,2f});
                        seam.Weight = (t,th) => W2(RigLayout.Spine,RigLayout.Chest,t);
                        EmitLoft(seam,0f,1f,4,4,t=>TrimUV,true,true);
                    }
                }
            }
            if (_look.SuitStyle == 3)
            {
                bool sentinel = _look.ShellStyle >= 3;
                bool weaver = _look.Jacket && _look.ShellStyle <= 1;
                if (sentinel)
                {
                    // Neck shield and ribbed dorsal vanes give the heavy role mass without widening hitboxes.
                    for (int side=0;side<2;side++)
                    {
                        float sign=side==0?-1f:1f;
                        Plate04F(new Vector3(sign*.18f,1.47f,-.115f)*s,new Vector3(sign*.6f,0f,-.8f),
                            Vector3.up,.045f*s,.12f*s,.025f*s,ArmorUV,TrimUV,W1(RigLayout.Chest),sign*.3f);
                    }
                }
                else if (weaver)
                {
                    for (int side=0;side<2;side++)
                    {
                        float sign=side==0?-1f:1f;
                        Plate04F(new Vector3(sign*.09f,1.48f,-.11f)*s,Vector3.back,Vector3.up,
                            .026f*s,.19f*s,.018f*s,ArmorUV,TrimUV,W1(RigLayout.Chest),sign*.6f);
                    }
                }
            }
        }
    }
}
