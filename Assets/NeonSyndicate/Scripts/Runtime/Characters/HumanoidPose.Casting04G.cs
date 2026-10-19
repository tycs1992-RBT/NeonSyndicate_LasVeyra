using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class HumanoidPose
    {
        /// <summary>The casting offhand supports a live focus, with distinct palm shapes for beams, discs and pulses.</summary>
        private void Casting04G(ref PoseInput p, ref Body b)
        {
            if (p.ParcelCarry04G && p.AttackT < 0f && p.DodgeT < 0f && p.StaggerT < 0f && p.Ride == RidePose.None && !p.Aiming && p.DrinkT < 0f)
            {
                Quaternion frame = ChestRot(ref b);
                b.AL = IkArm(ref b, -1, ChestPos(ref b) + frame * S(-0.27f, -0.12f, 0.22f),
                    frame * new Vector3(-0.8f, -0.6f, -0.2f), frame * Vector3.right, frame * Vector3.up, 1f);
                return;
            }
            if (p.CrystalCast04G <= 0.001f || p.AttackT >= 0f || p.DodgeT >= 0f || p.DrinkT >= 0f ||
                p.StaggerT >= 0f || p.Ride != RidePose.None || p.TravelBlend > 0.2f || p.Guard > 0.1f || p.Cover > 0.4f) return;
            float amount = Mathf.Clamp01(p.CrystalCast04G);
            Quaternion chest = ChestRot(ref b);
            Vector3 centre = ChestPos(ref b);
            bool beam = p.CrystalStyle04G == CrystalBehavior.PrismThread;
            bool disc = p.CrystalStyle04G == CrystalBehavior.GyreCrescent;
            Vector3 local;
            switch(p.CrystalStyle04G)
            {
                case CrystalBehavior.PrismThread: local=S(-.10f,-.13f,.40f);break;
                case CrystalBehavior.GyreCrescent: local=S(-.25f,-.14f,.26f);break;
                case CrystalBehavior.Sunderheart: local=S(-.18f,-.27f,.35f);break;
                case CrystalBehavior.MirageChoir: local=S(-.36f,-.06f,.23f);break;
                case CrystalBehavior.Mirrorwake: local=S(-.23f,.05f,.31f);break;
                default: local=S(-.23f,-.05f,.33f);break;
            }
            Arm support = IkArm(ref b, -1, centre + chest * local,
                chest * new Vector3(-0.8f, -0.6f, -0.2f), chest * (disc ? Vector3.forward : Vector3.up),
                chest * Vector3.right, amount);
            b.AL = support;
        }
    }
}
