using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure maths for the physical camera (Phase 5 of the visual upgrade). The game camera is a full-frame 36 by 24 mm
    /// sensor whose focal length is computed from the vertical field of view the game already uses, so the perspective
    /// is exactly what it was before the camera went physical: FocalLength(fov) and VerticalFov(focal) invert each other.
    /// It also holds the exposure numbers the post stack may read for depth of field and the small, bounded drift
    /// curves behind the title flyover sway, the pause menu drift and the opt-in cinematic breathing.
    /// No engine calls and no mutable state, so it runs in the offline tests.
    /// </summary>
    public static class CameraOptics
    {
        /// <summary>Full-frame sensor, millimetres. The vertical size is what the field of view is measured against.</summary>
        public const float SensorWidth = 36f;
        public const float SensorHeight = 24f;

        /// <summary>Lens numbers for the post stack's depth of field: an open lens for cinematic and menu views, a
        /// stopped-down one for play (where depth of field stays off; the number is only there to be read).</summary>
        public const float CinematicAperture = 2.8f;
        public const float GameplayAperture = 8f;
        /// <summary>A 180 degree shutter at 60 frames a second, and a quiet daylight ISO.</summary>
        public const float ShutterSeconds = 1f / 120f;
        public const int Iso = 200;
        public const int BladeCount = 7;

        /// <summary>Focal length in millimetres for a vertical field of view in degrees on a sensor of that height.</summary>
        public static float FocalLength(float verticalFovDeg, float sensorHeight)
        {
            float fov = Mathf.Clamp(verticalFovDeg, 0.5f, 179f);
            float t = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            return sensorHeight * 0.5f / Mathf.Max(1e-5f, t);
        }

        /// <summary>Vertical field of view in degrees for a focal length (millimetres) on a sensor of that height.</summary>
        public static float VerticalFov(float focalLengthMm, float sensorHeight)
        {
            float f = Mathf.Max(1e-4f, focalLengthMm);
            return 2f * Mathf.Atan(sensorHeight * 0.5f / f) * Mathf.Rad2Deg;
        }

        /// <summary>Horizontal field of view in degrees for a vertical one at a viewport aspect (width over height).</summary>
        public static float HorizontalFov(float verticalFovDeg, float aspect)
        {
            float half = Mathf.Tan(Mathf.Clamp(verticalFovDeg, 0.5f, 179f) * 0.5f * Mathf.Deg2Rad);
            return 2f * Mathf.Atan(half * Mathf.Max(0.01f, aspect)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// A slow, layered drift that never repeats visibly and stays inside the unit box: position in metres and
        /// rotation in degrees for an amplitude of one. Callers scale both. Two incommensurate frequencies per axis
        /// keep it from reading as a loop; seed offsets the phases so two users of it never move in step.
        /// </summary>
        public static void Drift(float time, float seed, out Vector3 position, out Vector3 eulerDeg)
        {
            float t = time + seed * 7.31f;
            float x = 0.6f * Mathf.Sin(t * 0.37f + seed) + 0.4f * Mathf.Sin(t * 0.91f + 1.7f);
            float y = 0.6f * Mathf.Sin(t * 0.29f + 2.3f + seed) + 0.4f * Mathf.Sin(t * 0.73f + 0.4f);
            float z = 0.5f * Mathf.Sin(t * 0.23f + 4.1f) + 0.5f * Mathf.Sin(t * 0.61f + seed * 0.5f);
            position = new Vector3(x, y, z);
            float pitch = 0.6f * Mathf.Sin(t * 0.31f + 0.9f + seed) + 0.4f * Mathf.Sin(t * 0.79f + 3.3f);
            float yaw = 0.6f * Mathf.Sin(t * 0.27f + 5.1f) + 0.4f * Mathf.Sin(t * 0.67f + seed);
            float roll = 0.5f * Mathf.Sin(t * 0.19f + 1.3f + seed) + 0.5f * Mathf.Sin(t * 0.53f + 2.9f);
            eulerDeg = new Vector3(pitch, yaw, roll);
        }

        /// <summary>
        /// A breathing cycle for hand-held cinematic shots: a soft rise and fall about four seconds long plus a faint
        /// faster tremor. Returns position (metres) and rotation (degrees) for an amplitude of one, both inside the unit box.
        /// </summary>
        public static void Breath(float time, out Vector3 position, out Vector3 eulerDeg)
        {
            float slow = Mathf.Sin(time * 1.55f);
            float tremor = 0.12f * Mathf.Sin(time * 6.1f + 1.1f) + 0.08f * Mathf.Sin(time * 9.7f + 2.6f);
            position = new Vector3(0.25f * Mathf.Sin(time * 0.93f + 0.7f), 0.75f * slow + tremor, 0f);
            eulerDeg = new Vector3(-0.7f * slow + tremor, 0.3f * Mathf.Sin(time * 0.71f + 2.1f), 0.2f * Mathf.Sin(time * 0.47f + 0.3f));
        }

        /// <summary>Ease-in weight 0..1 for a drift that started secondsAgo and takes rampSeconds to arrive (no pop).</summary>
        public static float RampIn(float secondsAgo, float rampSeconds)
        {
            if (rampSeconds <= 0f) return 1f;
            float u = Mathf.Clamp01(secondsAgo / rampSeconds);
            return u * u * (3f - 2f * u);
        }
    }
}
