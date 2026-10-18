using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Bone layout of the humanoid in its bind pose (all bone rotations identity, arms hanging straight down).
    /// Pure math shared by HumanoidRig, the mesh builder, the pose solver and the offline tests.
    /// Bone order: 0 Hips, 1 Spine, 2 Chest, 3 Neck, 4 Head, 5 UpperArm.L, 6 Forearm.L, 7 Hand.L,
    /// 8 UpperArm.R, 9 Forearm.R, 10 Hand.R, 11 Thigh.L, 12 Shin.L, 13 Foot.L, 14 Thigh.R, 15 Shin.R, 16 Foot.R.
    /// Left is -X, forward is +Z.
    /// </summary>
    public sealed class RigLayout
    {
        public const int BodyBoneCount = 17;
        public const int FingerCount = 5, FingerJoints = 3;
        public const int BoneCount = BodyBoneCount + 2 * FingerCount * FingerJoints;
        public const int Hips = 0, Spine = 1, Chest = 2, Neck = 3, Head = 4;
        public const int UpperArmL = 5, ForearmL = 6, HandL = 7, UpperArmR = 8, ForearmR = 9, HandR = 10;
        public const int ThighL = 11, ShinL = 12, FootL = 13, ThighR = 14, ShinR = 15, FootR = 16;

        public static readonly int[] Parent =
        {
            -1, 0, 1, 2, 3, 2, 5, 6, 2, 8, 9, 0, 11, 12, 0, 14, 15,
            7, 17, 18, 7, 20, 21, 7, 23, 24, 7, 26, 27, 7, 29, 30,
            10, 32, 33, 10, 35, 36, 10, 38, 39, 10, 41, 42, 10, 44, 45
        };
        public static readonly string[] Names =
        {
            "Hips", "Spine", "Chest", "Neck", "Head", "UpperArm.L", "Forearm.L", "Hand.L", "UpperArm.R", "Forearm.R", "Hand.R",
            "Thigh.L", "Shin.L", "Foot.L", "Thigh.R", "Shin.R", "Foot.R",
            "Index.1.L", "Index.2.L", "Index.3.L", "Middle.1.L", "Middle.2.L", "Middle.3.L",
            "Ring.1.L", "Ring.2.L", "Ring.3.L", "Little.1.L", "Little.2.L", "Little.3.L",
            "Thumb.1.L", "Thumb.2.L", "Thumb.3.L",
            "Index.1.R", "Index.2.R", "Index.3.R", "Middle.1.R", "Middle.2.R", "Middle.3.R",
            "Ring.1.R", "Ring.2.R", "Ring.3.R", "Little.1.R", "Little.2.R", "Little.3.R",
            "Thumb.1.R", "Thumb.2.R", "Thumb.3.R"
        };

        public float Scale;
        public float Bulk;
        public float ShoulderWidth;
        public bool Female;
        /// <summary>Bind position of each bone relative to its parent.</summary>
        public readonly Vector3[] Local = new Vector3[BoneCount];
        /// <summary>Bind position of each bone in root space.</summary>
        public readonly Vector3[] World = new Vector3[BoneCount];

        /// <summary>The humanoid creature this rig belongs to (None for people, whose rigs never use the fields below).</summary>
        public HumanoidCreature Creature;
        /// <summary>
        /// Creatures: how much slower the gait and the planted-foot steps run than a person's (1 for people). Big bodies use
        /// about the square root of their scale, so a giant takes long, slow, heavy strides instead of a scaled-up jog.
        /// </summary>
        public float TimeScale = 1f;
        /// <summary>Creatures: the body hovers (no planted feet; the legs hang together and trail as a tail). The Mirage Djinn.</summary>
        public bool Floating;
        /// <summary>Creatures: extra forward lean of the spine in degrees (a stalker's hunch, a golem's heavy shoulders).</summary>
        public float Hunch;
        /// <summary>Creatures: extra arm spread in degrees, so massive arms swing clear of a massive body.</summary>
        public float ArmSpread;

        public float ThighLength { get { return Local[ShinL].magnitude; } }
        public float ShinLength { get { return Local[FootL].magnitude; } }
        public float UpperArmLength { get { return Local[ForearmL].magnitude; } }
        public float ForearmLength { get { return Local[HandL].magnitude; } }

        public static RigLayout From(Appearance look)
        {
            if (look.Creature != HumanoidCreature.None) return ForCreature(look);
            RigLayout L = new RigLayout();
            float s = Mathf.Clamp(look.Height <= 0f ? 1f : look.Height, 0.6f, 1.2f);
            float bulk = Mathf.Clamp(look.Bulk <= 0f ? 1f : look.Bulk, 0.75f, 1.4f);
            float sh = look.Female ? 0.88f : 1f;
            L.Scale = s;
            L.Bulk = bulk;
            L.ShoulderWidth = sh;
            L.Female = look.Female;
            float shoulderX = 0.2f * s * sh * Mathf.Lerp(0.95f, 1.1f, bulk - 0.75f);
            float hipX = 0.1f * s * (look.Female ? 1.08f : 1f);
            L.Local[Hips] = new Vector3(0f, 0.95f * s, 0f);
            L.Local[Spine] = new Vector3(0f, 0.1f * s, 0f);
            L.Local[Chest] = new Vector3(0f, 0.24f * s, 0f);
            L.Local[Neck] = new Vector3(0f, 0.25f * s, 0f);
            L.Local[Head] = new Vector3(0f, 0.09f * s, 0f);
            L.Local[UpperArmL] = new Vector3(-shoulderX, 0.19f * s, 0f);
            L.Local[ForearmL] = new Vector3(0f, -0.29f * s, 0f);
            L.Local[HandL] = new Vector3(0f, -0.26f * s, 0f);
            L.Local[UpperArmR] = new Vector3(shoulderX, 0.19f * s, 0f);
            L.Local[ForearmR] = new Vector3(0f, -0.29f * s, 0f);
            L.Local[HandR] = new Vector3(0f, -0.26f * s, 0f);
            L.Local[ThighL] = new Vector3(-hipX, -0.05f * s, 0f);
            L.Local[ShinL] = new Vector3(0f, -0.43f * s, 0f);
            L.Local[FootL] = new Vector3(0f, -0.42f * s, 0f);
            L.Local[ThighR] = new Vector3(hipX, -0.05f * s, 0f);
            L.Local[ShinR] = new Vector3(0f, -0.43f * s, 0f);
            L.Local[FootR] = new Vector3(0f, -0.42f * s, 0f);
            L.SetFingers();
            for (int i = 0; i < BoneCount; i++)
                L.World[i] = Parent[i] < 0 ? L.Local[i] : L.World[Parent[i]] + L.Local[i];
            return L;
        }

        /// <summary>
        /// Rig for a humanoid creature. Scale is Height times CreatureScale (up to 3). Wraiths keep their source's human
        /// proportions; Hollows get long gaunt limbs and a forward-thrust neck; the Djinn broad shoulders and long arms
        /// (its legs hang inside the tail); golems very wide shoulders, long heavy arms, short legs and a head sunk
        /// between the shoulders. Feet keep a person's ankle-to-heel and ankle-to-ball proportions, which the pose solver
        /// plants on.
        /// </summary>
        private static RigLayout ForCreature(Appearance look)
        {
            RigLayout L = new RigLayout();
            float h = Mathf.Clamp(look.Height <= 0f ? 1f : look.Height, 0.6f, 1.2f);
            float k = look.CreatureScale <= 0f ? 1f : look.CreatureScale;
            float s = Mathf.Clamp(h * k, 0.5f, 3f);
            float bulk = Mathf.Clamp(look.Bulk <= 0f ? 1f : look.Bulk, 0.75f, 1.4f);
            L.Scale = s;
            L.Bulk = bulk;
            L.Creature = look.Creature;
            L.Female = look.Female;
            L.ShoulderWidth = look.Female ? 0.88f : 1f;
            switch (look.Creature)
            {
                case HumanoidCreature.MirrorWraith:
                case HumanoidCreature.WraithQueen:
                {
                    float shoulderX = 0.2f * L.ShoulderWidth * Mathf.Lerp(0.95f, 1.1f, bulk - 0.75f);
                    float hipX = 0.1f * (look.Female ? 1.08f : 1f);
                    L.Set(s, 0.05f, 0.42f, 0.43f, 0.05f, hipX, 0.1f, 0.24f, 0.25f, 0f, 0.09f, 0f, shoulderX, 0.19f, 0.29f, 0.26f);
                    if (look.Creature == HumanoidCreature.WraithQueen) L.TimeScale = Mathf.Sqrt(s);
                    break;
                }
                case HumanoidCreature.Hollow:
                case HumanoidCreature.SnowHollow:
                    L.Set(s, 0.055f, 0.47f, 0.48f, 0.05f, 0.085f, 0.1f, 0.25f, 0.235f, 0.05f, 0.085f, 0.03f, 0.18f, 0.18f, 0.355f, 0.335f);
                    L.Hunch = 12f;
                    L.ArmSpread = 4f;
                    break;
                case HumanoidCreature.MirageDjinn:
                    L.Set(s, 0.05f, 0.42f, 0.43f, 0.05f, 0.1f, 0.11f, 0.27f, 0.215f, 0.02f, 0.09f, 0.01f, 0.3f, 0.17f, 0.32f, 0.29f);
                    L.Floating = true;
                    L.TimeScale = Mathf.Sqrt(s);
                    L.ArmSpread = 9f;
                    break;
                default:
                    // Golems: the shoulders sit far out so the massive arms hang clear of the massive chest.
                    L.Set(s, 0.075f, 0.35f, 0.36f, 0.06f, 0.14f, 0.13f, 0.25f, 0.13f, 0.07f, 0.07f, 0.04f, 0.4f, 0.17f, 0.34f, 0.35f);
                    L.TimeScale = Mathf.Sqrt(s);
                    L.Hunch = 8f;
                    L.ArmSpread = 10f;
                    break;
            }
            L.SetFingers();
            for (int i = 0; i < BoneCount; i++)
                L.World[i] = Parent[i] < 0 ? L.Local[i] : L.World[Parent[i]] + L.Local[i];
            return L;
        }

        /// <summary>Append-only finger index. Existing body and save appearance indices remain unchanged.</summary>
        public static int Finger(int side, int finger, int joint)
        {
            return BodyBoneCount + Mathf.Clamp(side, 0, 1) * 15 + Mathf.Clamp(finger, 0, 4) * 3 + Mathf.Clamp(joint, 0, 2);
        }

        /// <summary>Three joints per digit, bound straight so open hands and handle grips use the same mesh.</summary>
        private void SetFingers()
        {
            float hs = Scale * (Female ? 0.9f : 1f);
            for (int side = 0; side < 2; side++)
            {
                float sx = side == 0 ? -1f : 1f;
                for (int finger = 0; finger < 5; finger++)
                {
                    int root = Finger(side, finger, 0);
                    if (finger == 4)
                    {
                        Local[root] = new Vector3(-sx * 0.008f, -0.032f, 0.03f) * hs;
                        Local[root + 1] = new Vector3(-sx * 0.55f, -0.62f, 0.5f).normalized * (0.024f * hs);
                        Local[root + 2] = Local[root + 1] * 0.72f;
                    }
                    else
                    {
                        float z = finger == 0 ? 0.028f : finger == 1 ? 0.009f : finger == 2 ? -0.01f : -0.027f;
                        float length = FingerLength(finger) * hs;
                        Local[root] = new Vector3(-sx * 0.004f, -0.096f, z) * hs;
                        Local[root + 1] = Vector3.down * (length * 0.44f);
                        Local[root + 2] = Vector3.down * (length * 0.32f);
                    }
                }
            }
        }

        /// <summary>Digit lengths in unscaled metres, index through little finger and thumb.</summary>
        public static float FingerLength(int finger)
        {
            return finger == 0 ? 0.072f : finger == 1 ? 0.08f : finger == 2 ? 0.076f : finger == 3 ? 0.06f : 0.058f;
        }

        /// <summary>
        /// Bind offsets from body proportions in unscaled metres. A person is ankle 0.05, shin 0.42, thigh 0.43, hip drop
        /// 0.05, hip 0.1 out, spine 0.1, chest 0.24, neck 0.25, head 0.09, shoulders 0.2 out and 0.19 up, upper arm 0.29,
        /// forearm 0.26; neckZ and headZ push the neck and head forward.
        /// </summary>
        private void Set(float s, float ankle, float shin, float thigh, float hipDrop, float hipX, float spine, float chest, float neck, float neckZ,
            float head, float headZ, float shoulderX, float shoulderY, float upper, float fore)
        {
            Local[Hips] = new Vector3(0f, (ankle + shin + thigh + hipDrop) * s, 0f);
            Local[Spine] = new Vector3(0f, spine * s, 0f);
            Local[Chest] = new Vector3(0f, chest * s, 0f);
            Local[Neck] = new Vector3(0f, neck * s, neckZ * s);
            Local[Head] = new Vector3(0f, head * s, headZ * s);
            Local[UpperArmL] = new Vector3(-shoulderX * s, shoulderY * s, 0f);
            Local[ForearmL] = new Vector3(0f, -upper * s, 0f);
            Local[HandL] = new Vector3(0f, -fore * s, 0f);
            Local[UpperArmR] = new Vector3(shoulderX * s, shoulderY * s, 0f);
            Local[ForearmR] = new Vector3(0f, -upper * s, 0f);
            Local[HandR] = new Vector3(0f, -fore * s, 0f);
            Local[ThighL] = new Vector3(-hipX * s, -hipDrop * s, 0f);
            Local[ShinL] = new Vector3(0f, -thigh * s, 0f);
            Local[FootL] = new Vector3(0f, -shin * s, 0f);
            Local[ThighR] = new Vector3(hipX * s, -hipDrop * s, 0f);
            Local[ShinR] = new Vector3(0f, -thigh * s, 0f);
            Local[FootR] = new Vector3(0f, -shin * s, 0f);
        }
    }
}
