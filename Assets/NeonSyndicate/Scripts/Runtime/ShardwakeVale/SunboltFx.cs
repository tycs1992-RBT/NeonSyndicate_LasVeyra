using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Timing and shape of the Vale's Sunbolt, engine-free so it is tested offline. A cast the journey accepts (energy
    /// and cooldown are charged there, unchanged) first gathers light in Malik's right hand, then releases a gold bolt
    /// that flies at the existing 23 metres per second with a tapered, gently twisting trail, and ends in a burst. Only a
    /// collar that is actually grounded flares; a bolt whose collar can no longer be grounded fizzles and lights nothing.
    /// </summary>
    public static class SunboltFx
    {
        /// <summary>Seconds the bolt gathers in the hand before it leaves.</summary>
        public const float GatherSeconds = 0.2f;
        /// <summary>Flight speed in metres per second (the pilot's original value).</summary>
        public const float Speed = 23f;
        /// <summary>Shortest flight, so a point-blank cast is still seen.</summary>
        public const float MinFlightSeconds = 0.12f;
        /// <summary>How far, as a fraction of the flight, the tail of the trail lags behind the bolt.</summary>
        public const float TailLag = 0.34f;
        /// <summary>Points along the trail, tail first.</summary>
        public const int TrailPoints = 12;
        /// <summary>Trail width at the bolt, in metres.</summary>
        public const float TrailWidth = 0.17f;
        /// <summary>Largest sideways twist of the trail around its flight line, in metres.</summary>
        public const float TwistRadius = 0.07f;
        /// <summary>Twist turns, in radians per metre flown.</summary>
        public const float TwistRate = 2.4f;
        /// <summary>Seconds of the burst on a grounded collar.</summary>
        public const float BurstSeconds = 0.5f;
        /// <summary>Seconds of the dull puff when the bolt grounds nothing.</summary>
        public const float FizzleSeconds = 0.4f;
        /// <summary>Seconds a grounded collar takes to settle from its flare to its spent look.</summary>
        public const float FlareSeconds = 1.1f;
        /// <summary>Paint brightness of a collar at the peak of its flare.</summary>
        public const float FlareLift = 1.9f;
        /// <summary>Warm tint of a collar at the peak of its flare.</summary>
        public static Color FlareTint { get { return new Color(1f, 0.86f, 0.52f); } }
        /// <summary>Colour of the fizzle (a cool, dim grey violet that never reads as a grounded hit).</summary>
        public static Color FizzleColor { get { return new Color(0.58f, 0.55f, 0.72f, 0.55f); } }

        /// <summary>Flight time for a distance, as before: distance at 23 m/s, never under 0.12 s.</summary>
        public static float FlightSeconds(float distance)
        {
            return Mathf.Max(MinFlightSeconds, distance / Speed);
        }

        /// <summary>Hand glow size from 0 to 1, t seconds after the cast; full at the release.</summary>
        public static float Gather(float t)
        {
            if (t <= 0f) return 0f;
            float k = Mathf.Clamp01(t / GatherSeconds);
            return k * k * (3f - 2f * k);
        }

        /// <summary>Radius in metres of the ring of light that closes onto the hand while the bolt gathers.</summary>
        public static float GatherRing(float t)
        {
            return Mathf.Lerp(0.42f, 0.08f, Mathf.Clamp01(t / GatherSeconds));
        }

        /// <summary>True while the bolt is still in the hand.</summary>
        public static bool Gathering(float t)
        {
            return t >= 0f && t < GatherSeconds;
        }

        /// <summary>Flight progress 0 to 1, given seconds since the release and the flight time.</summary>
        public static float Progress(float sinceRelease, float flightSeconds)
        {
            return Mathf.Clamp01(sinceRelease / Mathf.Max(1e-4f, flightSeconds));
        }

        /// <summary>Trail width from tail (u 0) to bolt (u 1), as a fraction of TrailWidth.</summary>
        public static float TrailWidthAt(float u)
        {
            return Mathf.Pow(Mathf.Clamp01(u), 0.8f);
        }

        /// <summary>Trail opacity from tail (u 0) to bolt (u 1).</summary>
        public static float TrailAlphaAt(float u)
        {
            return Mathf.Pow(Mathf.Clamp01(u), 1.5f);
        }

        /// <summary>
        /// Fills the trail from tail (index 0) to the bolt (last index). Every point lies on the flight line between
        /// the origin and the bolt, never ahead of it, pushed sideways by a twist that is zero at both ends.
        /// </summary>
        public static void Trail(Vector3 origin, Vector3 destination, float progress, float phase, Vector3[] points)
        {
            int n = points.Length;
            if (n == 0) return;
            Vector3 line = destination - origin;
            float length = line.magnitude;
            Vector3 forward = length > 1e-5f ? line / length : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            side.Normalize();
            Vector3 lift = Vector3.Cross(forward, side);
            float head = Mathf.Clamp01(progress), tail = Mathf.Max(0f, head - TailLag);
            for (int i = 0; i < n; i++)
            {
                float u = n == 1 ? 1f : i / (float)(n - 1);
                float s = Mathf.Lerp(tail, head, u);
                float twist = TwistRadius * Mathf.Sin(u * Mathf.PI);
                float angle = phase + s * length * TwistRate;
                points[i] = origin + line * s + (side * Mathf.Cos(angle) + lift * Mathf.Sin(angle)) * twist;
            }
        }

        /// <summary>Burst radius in metres, t seconds after a grounded hit.</summary>
        public static float BurstRadius(float t)
        {
            float k = Mathf.Clamp01(t / BurstSeconds);
            return 0.22f + 0.95f * (1f - (1f - k) * (1f - k));
        }

        /// <summary>Burst opacity, 1 at the hit and 0 when it ends.</summary>
        public static float BurstAlpha(float t)
        {
            float k = Mathf.Clamp01(t / BurstSeconds);
            return (1f - k) * (1f - k);
        }

        /// <summary>Fizzle radius in metres: small, so a miss never looks like a burst.</summary>
        public static float FizzleRadius(float t)
        {
            return 0.12f + 0.22f * Mathf.Clamp01(t / FizzleSeconds);
        }

        /// <summary>Fizzle opacity as a fraction of FizzleColor's alpha.</summary>
        public static float FizzleAlpha(float t)
        {
            return 1f - Mathf.Clamp01(t / FizzleSeconds);
        }

        /// <summary>Flare strength of a grounded collar, t seconds after its hit (0 before the hit and after it settles).</summary>
        public static float Flare(float t)
        {
            if (t < 0f || t >= FlareSeconds) return 0f;
            float k = 1f - t / FlareSeconds;
            return k * k;
        }

        /// <summary>Collar paint brightness: its resting value blended toward the flare.</summary>
        public static float Lift(float resting, float flare)
        {
            return Mathf.Lerp(resting, FlareLift, Mathf.Clamp01(flare));
        }

        /// <summary>Collar tint: its resting tint blended toward the warm flare.</summary>
        public static Color Tint(Color resting, float flare)
        {
            return Color.Lerp(resting, FlareTint, Mathf.Clamp01(flare));
        }
    }
}
