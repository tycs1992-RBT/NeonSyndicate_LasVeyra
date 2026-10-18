using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        /// <summary>Closed tailored surfaces in the original skin, never new rig bones or gameplay colliders.</summary>
        private void BuildAscension04I()
        {
            if (_detail == 0 || _look.Creature != HumanoidCreature.None) return;
            int limit = _detail == 2 ? 13500 : 8200;
            float s = _s;
            if (_d.Vertices.Count + 1100 > limit) return;
            if (_look.Jacket && _look.SuitStyle != 3)
            {
                // Split coat tails, shaped in cross section and weighted into the thighs so stairs/riding stay articulated.
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    int thigh = side == 0 ? RigLayout.ThighL : RigLayout.ThighR;
                    Loft cloth = new Loft(new Vector3(sign*.105f,1.09f,-.131f)*s,
                        new Vector3(sign*.142f,.70f,-.157f)*s,Vector3.right,Vector3.forward,new float[]
                        {0f,.082f*s,.014f*s,0f,0f,3f,
                         .28f,.094f*s,.017f*s,0f,-.008f*s,3f,
                         .65f,.104f*s,.019f*s,sign*.012f*s,-.021f*s,3f,
                         1f,.084f*s,.014f*s,sign*.019f*s,-.014f*s,3f});
                    cloth.Bump = (t,th) => .003f*s*Mathf.Sin(t*12f+th*.08f)*Mathf.Sin(t*Mathf.PI);
                    cloth.Weight = (t,th) => W2(RigLayout.Hips,thigh,.48f*QMath.SmoothRange(.2f,.9f,t));
                    EmitLoft(cloth,0f,1f,_detail==2?10:6,8,t=>t>.92f?TrimUV:JacketUV,true,true);
                    // Raised sleeve cuffs follow the forearm rather than the hand or chest.
                    int fore = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
                    Plate04F(_L.World[fore]+new Vector3(0f,-.16f,-.049f)*s,Vector3.back,Vector3.up,
                        .039f*s,.052f*s,.012f*s,DarkTechUV,TrimUV,W1(fore),sign*.18f);
                }
            }
            // Hero-only material breaks read as a fitted quartz harness at regular third-person distance.
            // Chest-weighted facets cannot drift off the shoulders while the arms swing or hold a focus.
            if (_detail == 2 && _look.Jacket && _look.SuitStyle != 3 && _d.Vertices.Count + 450 < limit)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Plate04F(new Vector3(side * .17f, 1.40f, .112f) * s, Vector3.forward, Vector3.up,
                        .054f * s, .14f * s, .014f * s, DarkTechUV, TrimUV, W1(RigLayout.Chest), side * .26f);
                    Plate04F(new Vector3(side * .13f, 1.30f, .149f) * s, Vector3.forward, Vector3.up,
                        .08f * s, .040f * s, .011f * s, JacketUV, GlowUV, W2(RigLayout.Spine, RigLayout.Chest, .75f), side * .18f);
                }
                Plate04F(new Vector3(0f, 1.36f, .164f) * s, Vector3.forward, Vector3.up,
                    .033f * s, .054f * s, .012f * s, GlowUV, DarkTechUV, W1(RigLayout.Chest), 0f);
            }
            else if (_look.SuitStyle == 3)
            {
                bool heavy = _look.ShellStyle >= 3;
                bool caster = _look.Jacket && _look.ShellStyle <= 1;
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    // A layered heraldic collar and rear shoulder blades change the role's full-body silhouette.
                    int layers = heavy ? 3 : caster ? 2 : 1;
                    for (int i=0;i<layers;i++)
                        Plate04F(new Vector3(sign*(.16f+i*.035f),1.46f-i*.035f,-.08f)*s,
                            new Vector3(sign*.4f,.55f,-.7f),Vector3.up,(heavy?.075f:.045f)*s,
                            (caster?.19f:.115f)*s,.024f*s,ArmorUV,TrimUV,W1(RigLayout.Chest),sign*.45f);
                    // Segmented abdomen leaves a clear waist and respects the original spine weights.
                    for (int i=0;i<2;i++)
                        Plate04F(new Vector3(sign*.073f,1.22f-i*.068f,.138f)*s,Vector3.forward,Vector3.up,
                            .065f*s,.033f*s,.014f*s,ArmorUV,DarkTechUV,W2(RigLayout.Spine,RigLayout.Chest,.65f-i*.3f),sign*.15f);
                }
            }
        }
    }
}
