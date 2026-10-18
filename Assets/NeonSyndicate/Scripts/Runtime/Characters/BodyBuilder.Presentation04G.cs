using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class BodyBuilder
    {
        /// <summary>Closed, bone-weighted costume pieces. The low crowd rig and its geometry budget stay unchanged.</summary>
        private void BuildPresentation04G()
        {
            if (_detail == 0 || _look.Creature != HumanoidCreature.None) return;
            float s = _s;
            Vector2 metal = Pal(5, 2), textile = JacketUV;
            bool armor = _look.SuitStyle == 3;
            bool heavy = armor && _look.ShellStyle >= 3;
            bool weaver = armor && _look.Jacket && _look.ShellStyle <= 1;
            bool duelist = armor && _look.GauntletStyle == 3;
            if (heavy)
            {
                // A broad solid dorsal mantle and segmented greaves, with knees and elbows left free.
                Plate04F(new Vector3(0f, 1.39f, -0.17f) * s, Vector3.back, Vector3.up,
                    0.22f * s, 0.14f * s, 0.035f * s, ArmorUV, metal, W1(RigLayout.Chest), 0f);
                for (int side = 0; side < 2; side++)
                {
                    int shin = side == 0 ? RigLayout.ShinL : RigLayout.ShinR;
                    Plate04F(_L.World[shin] + new Vector3(0f, -0.16f, 0.065f) * s,
                        Vector3.forward, Vector3.up, 0.066f * s, 0.15f * s, 0.021f * s,
                        ArmorUV, metal, W1(shin), 0f);
                }
            }
            else if (weaver)
            {
                // Three split coat leaves create a narrow caster silhouette, attached above each moving leg.
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    int thigh = side == 0 ? RigLayout.ThighL : RigLayout.ThighR;
                    Plate04F(_L.World[thigh] + new Vector3(sign * 0.06f, -0.1f, -0.075f) * s,
                        Vector3.back, Vector3.up, 0.09f * s, 0.19f * s, 0.009f * s,
                        textile, TrimUV, W1(thigh), sign * 0.25f);
                }
                Plate04F(new Vector3(-0.14f, 1.39f, 0.14f) * s, Vector3.forward, Vector3.up,
                    0.052f * s, 0.17f * s, 0.014f * s, textile, metal, W1(RigLayout.Chest), 0.2f);
            }
            else if (duelist)
            {
                // An asymmetric short mantle and fitted forearm lames distinguish mobile rival fighters.
                Plate04F(new Vector3(-0.19f, 1.41f, -0.1f) * s, new Vector3(-0.6f, 0f, -0.8f), Vector3.up,
                    0.08f * s, 0.15f * s, 0.014f * s, ArmorUV, TrimUV, W1(RigLayout.Chest), 0.4f);
                for (int side = 0; side < 2; side++)
                {
                    int forearm = side == 0 ? RigLayout.ForearmL : RigLayout.ForearmR;
                    Plate04F(_L.World[forearm] + new Vector3(0f, -0.07f, -0.047f) * s,
                        Vector3.back, Vector3.up, 0.034f * s, 0.085f * s, 0.01f * s,
                        ArmorUV, metal, W1(forearm), 0f);
                }
            }
            else if (_look.Jacket && !armor)
            {
                // Tailored collar, metal buckles and a fitted left shoulder strap remain recognizable on Malik's coat.
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    Plate04F(new Vector3(sign * 0.067f, 1.49f, 0.07f) * s,
                        new Vector3(sign * 0.45f, 0.1f, 0.85f), Vector3.up,
                        0.04f * s, 0.058f * s, 0.011f * s, textile, TrimUV, W1(RigLayout.Chest), -sign * 0.2f);
                }
                Plate04F(new Vector3(-0.12f, 1.28f, 0.157f) * s, Vector3.forward, Vector3.up,
                    0.028f * s, 0.115f * s, 0.009f * s, DarkTechUV, TrimUV, W1(RigLayout.Chest), 0.15f);
                Plate04F(new Vector3(-0.12f, 1.28f, 0.168f) * s, Vector3.forward, Vector3.up,
                    0.016f * s, 0.02f * s, 0.003f * s, metal, DarkTechUV, W1(RigLayout.Chest), 0f);
            }
            if (_detail == 2 && _head != null && _look.VisorStyle != 2)
            {
                // Lower orbital and cheek folds follow this exact head loft, preserving the existing anatomy and eyes.
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? -1f : 1f;
                    FaceRibbon04F(90f + sign * 27f, HY(0.008f) * s, 90f + sign * 39f, HY(-0.028f) * s,
                        0.0022f * s, 0.0016f * s, SkinUV);
                    FaceRibbon04F(90f + sign * 29f, HY(-0.046f) * s, 90f + sign * 15f, HY(-0.082f) * s,
                        0.0013f * s, 0.001f * s, SkinUV);
                }
            }
        }
    }
}
