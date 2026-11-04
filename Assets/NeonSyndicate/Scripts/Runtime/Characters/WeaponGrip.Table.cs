using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The pure half of WeaponGrip: where a weapon model sits in the hand for each hold type, shared by
    /// WeaponGrip.Attach (which places the model) and the pose solver (which aims the hand so the model points where the
    /// pose wants it). Engine-free, so the offline tests use the exact same numbers as the game.
    /// </summary>
    public static partial class WeaponGrip
    {
        // ---- The hand frame Attach assumes (both hands, bind pose, arm hanging at the side) ----
        // Origin: the wrist (the hand bone). -Y runs from the wrist to the knuckles and fingertips. +Z is the thumb and
        // index side, -Z the little finger side. The palm faces -X on the right hand and +X on the left. The resting
        // finger curl of the hand mesh wraps a handle whose axis runs along Z at about (HandleX, HandleY, 0) metres on
        // the right hand at scale 1 (X mirrored on the left hand). Weapon models have their origin at the grip, +Z along
        // the barrel or blade, +Y up.

        /// <summary>Right hand, scale 1: sideways (palm side) offset of the axis of a handle held in the finger curl.</summary>
        public const float HandleX = -0.035f;
        /// <summary>Right hand, scale 1: offset of that handle axis along the hand, from the wrist toward the fingers.</summary>
        public const float HandleY = -0.105f;

        /// <summary>
        /// The attach table, one row per HoldType: the model's position in the right hand in metres at scale 1 (x, y, z),
        /// then its rotation as Euler degrees (x, y, z). Euler(90, 0, 0) lays the barrel along the knuckle line with the top
        /// of the gun on the thumb side, so a pistol or rifle grip crosses the palm inside the finger curl; above 90 the
        /// barrel tips toward the little finger, so the forearm rises to the gun from below (a rifle's steep grip). Note that
        /// Euler(90, 0, 0) sends the model's +Z along the hand's -Y (wrist to fingertips): right for a gun barrel, wrong for a
        /// sword, whose handle must pass through the fist. Melee is Euler(45, 0, 0): the handle crosses the palm diagonally
        /// from the heel of the hand to the index knuckle, so the blade leaves the fist above the thumb and the edge (the
        /// model's -Y) faces the knuckles; the pose solver aims those hands through this grip (HumanoidPose.SwordHand).
        /// Adjust a row here and both the game and the solver follow.
        /// </summary>
        private static void Row(HoldType hold, out Vector3 pos, out Vector3 euler)
        {
            switch (hold)
            {
                //                                   position (m)                         rotation (deg)
                case HoldType.Pistol: pos = new Vector3(-0.034f, -0.106f, -0.008f); euler = new Vector3(96f, 0f, 0f); return;   // grip in the curl, slide above the index
                case HoldType.Rifle: pos = new Vector3(-0.034f, -0.106f, -0.008f); euler = new Vector3(118f, 0f, 0f); return;   // steeper grip: knuckles rise toward the barrel line
                case HoldType.Heavy: pos = new Vector3(-0.036f, -0.108f, -0.006f); euler = new Vector3(118f, 0f, 0f); return;   // rear grip of a launcher or minigun
                case HoldType.Melee: pos = new Vector3(-0.034f, -0.100f, 0f); euler = new Vector3(45f, 0f, 0f); return;          // handle across the fist, blade above the thumb
                case HoldType.Thrown: pos = new Vector3(-0.036f, -0.112f, 0.004f); euler = new Vector3(90f, 0f, 0f); return;   // grenade or bottle in the curl
                default: pos = new Vector3(0f, -0.075f, 0.015f); euler = new Vector3(90f, 0f, 0f); return;                      // None: gauntlets over the hand
            }
        }

        /// <summary>Model position in the right hand bone's space (metres, already scaled by the rig).</summary>
        public static Vector3 GripPosition(HoldType hold, float rigScale)
        {
            Vector3 pos, euler;
            Row(hold, out pos, out euler);
            return pos * rigScale;
        }

        /// <summary>Model rotation relative to the right hand bone.</summary>
        public static Quaternion GripRotation(HoldType hold)
        {
            Vector3 pos, euler;
            Row(hold, out pos, out euler);
            return QMath.Euler(euler.x, euler.y, euler.z);
        }

        /// <summary>The same placement mirrored for the left hand (the model also gets a mirrored scale, x = -1).</summary>
        public static Vector3 GripPositionLeft(HoldType hold, float rigScale)
        {
            Vector3 p = GripPosition(hold, rigScale);
            return new Vector3(-p.x, p.y, p.z);
        }

        /// <summary>Left hand rotation (mirror of GripRotation across the hand's YZ plane).</summary>
        public static Quaternion GripRotationLeft(HoldType hold)
        {
            Quaternion q = GripRotation(hold);
            return new Quaternion(q.x, -q.y, -q.z, q.w);
        }

        /// <summary>Left hand (support hand) on a foregrip: palm up under the handguard, fingers wrapping its right side,
        /// thumb along the barrel. Rotation of the left hand relative to the weapon model.</summary>
        public static Quaternion SupportRotation { get { return QMath.AZ(70f); } }

        /// <summary>Left hand cupping the firing hand on a pistol: turned like the right hand (knuckles forward, thumb up),
        /// palm against the right hand's fingers. Rotation of the left hand relative to the weapon model.</summary>
        public static Quaternion CupRotation(HoldType hold) { return QMath.Inverse(GripRotation(hold)); }

        /// <summary>Where the cupping left hand's grip point sits on a pistol, model space, metres at scale 1.</summary>
        public static Vector3 CupPoint { get { return new Vector3(-0.03f, -0.02f, -0.005f); } }

        /// <summary>Left hand handle point (the axis of whatever its finger curl wraps) in the left hand's space.</summary>
        public static Vector3 HandleLeft(float handScale) { return new Vector3(-HandleX, HandleY, 0f) * handScale; }

        /// <summary>
        /// Estimated support point (model space) for a two-handed model that has no Foregrip child, from its mesh bounds:
        /// rifles under the handguard about halfway to the muzzle, a hip-held heavy weapon on its top handle, a shoulder
        /// launcher a hand's width ahead of the grip.
        /// </summary>
        public static Vector3 EstimateForegrip(HoldType hold, Vector3 boundsMin, Vector3 boundsMax, bool shoulderHeavy)
        {
            float front = Mathf.Max(boundsMax.z, 0.2f);
            switch (hold)
            {
                case HoldType.Heavy:
                    if (shoulderHeavy) return new Vector3(0f, 0.035f, Mathf.Clamp(front * 0.3f, 0.16f, 0.3f));
                    return new Vector3(0f, Mathf.Max(boundsMax.y - 0.02f, 0.06f), Mathf.Clamp((boundsMin.z + boundsMax.z) * 0.5f, -0.05f, 0.2f));
                case HoldType.Pistol:
                    return new Vector3(0f, -0.02f, Mathf.Clamp(front * 0.45f, 0.08f, 0.16f));
                default:
                    return new Vector3(0f, 0.035f, Mathf.Clamp(front * 0.42f, 0.14f, 0.3f));
            }
        }

        /// <summary>The model's rear end (stock butt) from its bounds, or a typical carbine stock when unknown.</summary>
        public static Vector3 EstimateButt(Vector3 boundsMin, Vector3 boundsMax)
        {
            float z = boundsMin.z < -0.1f ? boundsMin.z : -0.26f;
            return new Vector3(0f, 0.03f, z);
        }

        /// <summary>A heavy model fired from the shoulder (a long launcher tube reaching well behind the grip).</summary>
        public static bool IsShoulderHeavy(HoldType hold, Vector3 boundsMin)
        {
            return hold == HoldType.Heavy && boundsMin.z < -0.3f;
        }
    }
}
