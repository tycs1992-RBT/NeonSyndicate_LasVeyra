using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure maths behind the HumanoidAnimator gestures added for 04E (Greet, Inspect, Point and the head look). Arm
    /// gestures are given as directions in the chest's frame (x right, y up, z forward): where the upper arm points
    /// and where the forearm points. From those this class builds the bones' local rotations exactly (the rig's arm
    /// bones hang along local -y and the elbow folds about local x), so the hand lands where the gesture means it to,
    /// never inside the face or the chest. Engine-free (QMath, Mathf, Vector3), tested in Guidance04ETests.
    /// </summary>
    public static class CastGestures04E
    {
        /// <summary>Length of one greeting wave in seconds (raise, three waves, lower).</summary>
        public const float GreetSeconds = 1.9f;
        /// <summary>Head look limits in degrees (turning left or right, looking up, looking down).</summary>
        public const float MaxYaw = 70f, MaxPitchUp = 30f, MaxPitchDown = 40f;
        /// <summary>How far the right arm may point across the body, and how far out to the right (degrees from forward).</summary>
        public const float PointAcross = 15f, PointSide = 80f;

        /// <summary>The upper arm's local rotation (relative to the chest) that points the arm along upperDir with the
        /// elbow hinge set so the forearm can fold toward foreDir.</summary>
        public static Quaternion UpperArm(Vector3 upperDir, Vector3 foreDir)
        {
            Vector3 d1 = Safe(upperDir, Vector3.down);
            Vector3 d2 = Safe(foreDir, d1);
            // The forearm folds toward the upper arm's local +z: z is the part of foreDir across the upper arm.
            Vector3 z = d2 - d1 * Vector3.Dot(d2, d1);
            if (z.sqrMagnitude < 1e-6f)
            {
                // Straight arm: keep the hinge facing forward (or up when the arm itself points forward or back).
                Vector3 hint = Mathf.Abs(d1.z) < 0.9f ? Vector3.forward : Vector3.up;
                z = hint - d1 * Vector3.Dot(hint, d1);
            }
            z.Normalize();
            Vector3 y = -d1;
            Vector3 x = Vector3.Cross(y, z);
            return QMath.FromBasis(x.normalized, y, z);
        }

        /// <summary>The elbow bend in degrees between the two directions (0 straight, at most 150).</summary>
        public static float Elbow(Vector3 upperDir, Vector3 foreDir)
        {
            Vector3 d1 = Safe(upperDir, Vector3.down);
            Vector3 d2 = Safe(foreDir, d1);
            float c = Mathf.Clamp(Vector3.Dot(d1, d2), -1f, 1f);
            return Mathf.Min(150f, Mathf.Acos(c) * Mathf.Rad2Deg);
        }

        /// <summary>The forearm's local rotation (relative to the upper arm) for an elbow bend in degrees.</summary>
        public static Quaternion Forearm(float elbowDegrees)
        {
            return QMath.AX(-Mathf.Clamp(elbowDegrees, 0f, 150f));
        }

        /// <summary>
        /// The greeting wave for the right arm at time t (seconds since it started): directions and the blend weight.
        /// The arm rises out to the side, the forearm stands up and sways three times, then the arm lowers.
        /// </summary>
        public static float Greet(float t, out Vector3 upper, out Vector3 fore)
        {
            float k = Mathf.Clamp01(t / GreetSeconds);
            float weight = QMath.SmoothRange(0f, 0.16f, k) * (1f - QMath.SmoothRange(0.82f, 1f, k));
            upper = new Vector3(0.78f, 0.28f, 0.42f).normalized;
            float sway = Mathf.Sin(QMath.SmoothRange(0.16f, 0.82f, k) * Mathf.PI * 6f) * 0.38f;
            fore = new Vector3(0.12f + sway, 1f, 0.22f).normalized;
            return weight;
        }

        /// <summary>Both hands forward at chest height with the forearms angled in, as when examining a receiver part.
        /// side is -1 for the left arm, 1 for the right.</summary>
        public static void Inspect(int side, out Vector3 upper, out Vector3 fore)
        {
            float s = side < 0 ? -1f : 1f;
            upper = new Vector3(0.18f * s, -0.62f, 0.76f).normalized;
            fore = new Vector3(-0.42f * s, 0.18f, 0.89f).normalized;
        }

        /// <summary>
        /// The right arm pointing along a chest-frame direction. Directions behind the body or across it are turned to
        /// the nearest comfortable point so the arm never folds through the torso.
        /// </summary>
        public static void Point(Vector3 chestDir, out Vector3 upper, out Vector3 fore)
        {
            Vector3 d = Safe(chestDir, Vector3.forward);
            // Keep it in front or to the right: at most a little across the body (15 degrees), never behind it (80
            // degrees to the right at most), and not straight up or down. Clamped as angles so the result stays exact.
            float pitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) * Mathf.Rad2Deg, -37f, 48f);
            float yaw = Mathf.Abs(d.x) + Mathf.Abs(d.z) < 1e-5f ? 0f : Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            yaw = Mathf.Clamp(yaw, -PointAcross, PointSide);
            float flat = Mathf.Cos(pitch * Mathf.Deg2Rad);
            d = new Vector3(Mathf.Sin(yaw * Mathf.Deg2Rad) * flat, Mathf.Sin(pitch * Mathf.Deg2Rad), Mathf.Cos(yaw * Mathf.Deg2Rad) * flat);
            upper = (d + new Vector3(0.12f, -0.18f, 0f)).normalized;
            fore = d;
        }

        /// <summary>Head yaw and pitch (degrees, pitch positive looks down) toward a chest-frame direction, clamped to a
        /// natural range. Returns false (and zeros) when the target is behind the person.</summary>
        public static bool Look(Vector3 chestDir, out float yaw, out float pitch)
        {
            yaw = 0f;
            pitch = 0f;
            if (chestDir.sqrMagnitude < 1e-6f) return false;
            Vector3 d = chestDir.normalized;
            float flat = Mathf.Sqrt(d.x * d.x + d.z * d.z);
            float rawYaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            if (Mathf.Abs(rawYaw) > 115f) return false;
            yaw = Mathf.Clamp(rawYaw, -MaxYaw, MaxYaw);
            pitch = Mathf.Clamp(-Mathf.Atan2(d.y, Mathf.Max(0.0001f, flat)) * Mathf.Rad2Deg, -MaxPitchUp, MaxPitchDown);
            return true;
        }

        /// <summary>The neck and head local rotations sharing a look (40 percent neck, 60 percent head).</summary>
        public static void LookRotations(float yaw, float pitch, out Quaternion neck, out Quaternion head)
        {
            neck = QMath.AY(yaw * 0.4f) * QMath.AX(pitch * 0.4f);
            head = QMath.AX(pitch * 0.6f) * QMath.AY(yaw * 0.6f);
        }

        private static Vector3 Safe(Vector3 v, Vector3 fallback)
        {
            float m = v.magnitude;
            if (m < 1e-6f || float.IsNaN(m)) return fallback.normalized;
            return v / m;
        }
    }
}
