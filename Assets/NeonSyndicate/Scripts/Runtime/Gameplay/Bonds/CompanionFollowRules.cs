using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Counts how long a follower has been trying to move without getting anywhere, and how many bounded repaths it
    /// has tried, so a stuck companion escalates: skip ahead on the trail, re-find the trail, then recall (off camera)
    /// or wait with a reason. Engine-free.
    /// </summary>
    public struct StuckMeter
    {
        public float Timer;
        public int Attempts;

        /// <summary>The time without progress that counts as stuck.</summary>
        public const float StuckSeconds = 1.2f;
        /// <summary>Repaths before it gives up and asks for a recall.</summary>
        public const int MaxRepaths = 2;

        /// <summary>One frame: moved is the distance covered toward the goal, wanted the distance it tried to cover.
        /// True on the frame it counts as stuck (the timer restarts).</summary>
        public bool Update(float moved, float wanted, float dt)
        {
            if (wanted < 0.02f)
            {
                Timer = 0f;
                return false;
            }
            if (moved >= wanted * 0.25f)
            {
                Timer = Mathf.Max(0f, Timer - dt * 2f);
                if (Timer <= 0f) Attempts = 0;
                return false;
            }
            Timer += dt;
            if (Timer < StuckSeconds) return false;
            Timer = 0f;
            Attempts++;
            return true;
        }

        public bool ShouldRecall { get { return Attempts > MaxRepaths; } }

        public void Reset()
        {
            Timer = 0f;
            Attempts = 0;
        }
    }

    /// <summary>
    /// Plain follow decisions for companions: where beside the player it settles, how fast it moves by distance, when
    /// it may stop, the hover height under a ceiling, the ordered recall points around the player and when a gap is
    /// big enough to need a recall. Engine-free and tested.
    /// </summary>
    public static class CompanionFollowRules
    {
        /// <summary>Further than this from the player (teleport, travel powers, a lift) it catches up by recall.</summary>
        public const float CatchUpDistance = 40f;
        /// <summary>A follower further than this from every crumb has lost the trail.</summary>
        public const float LostTrailDistance = 7f;
        /// <summary>Direct (off-trail) steering only within this distance of the player and with a clear line.</summary>
        public const float DirectRange = 14f;
        /// <summary>A crumb counts as reached within this flat distance and height difference.</summary>
        public const float ReachFlat = 0.6f;
        public const float ReachHeight = 1.1f;

        /// <summary>The idle spot beside and a little behind the player.</summary>
        public static Vector3 Slot(Vector3 player, Vector3 playerForward, int side, float distance)
        {
            Vector3 f = new Vector3(playerForward.x, 0f, playerForward.z);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            f.Normalize();
            Vector3 right = new Vector3(f.z, 0f, -f.x);
            float s = side >= 0 ? 1f : -1f;
            return player - f * (distance * 0.65f) + right * (s * distance * 0.75f);
        }

        /// <summary>Speed by distance to the player: walk close, run further, sprint to catch up; never stands still in a fight.</summary>
        public static float Speed(float distance, CompanionProfile p, bool combat)
        {
            if (p == null) return 0f;
            float walk = p.WalkSpeed, run = p.RunSpeed, sprint = p.SprintSpeed;
            if (distance < 4f) return combat ? Mathf.Lerp(walk, run, 0.5f) : walk;
            if (distance < 9f) return Mathf.Lerp(walk, run, (distance - 4f) / 5f);
            return Mathf.Lerp(run, sprint, Mathf.Clamp01((distance - 9f) / 8f));
        }

        /// <summary>True when it may stop beside the player.</summary>
        public static bool ShouldIdle(float distance, float followDistance, bool lineOfSight, bool sameLayer)
        {
            return sameLayer && lineOfSight && distance <= followDistance + 0.6f;
        }

        /// <summary>Hover height under any ceiling: the wanted height, kept a body height plus a margin below the ceiling and above the floor.</summary>
        public static float HoverY(float groundY, float ceilingY, float hover, float bodyHeight)
        {
            float y = groundY + Mathf.Max(0f, hover);
            if (!float.IsInfinity(ceilingY) && !float.IsNaN(ceilingY)) y = Mathf.Min(y, ceilingY - bodyHeight - 0.35f);
            return Mathf.Max(y, groundY + 0.3f);
        }

        /// <summary>
        /// Recall candidates around the player in order of preference: behind on its side, straight behind, beside, the
        /// other side, then the same ring further out. Offsets are flat (add them to the player's feet).
        /// </summary>
        public static void RecallOffsets(Vector3 playerForward, int side, List<Vector3> into)
        {
            into.Clear();
            Vector3 f = new Vector3(playerForward.x, 0f, playerForward.z);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.forward;
            f.Normalize();
            Vector3 r = new Vector3(f.z, 0f, -f.x) * (side >= 0 ? 1f : -1f);
            float[] radii = { 2.2f, 3.4f };
            for (int k = 0; k < radii.Length; k++)
            {
                float d = radii[k];
                into.Add((-f + r).normalized * d);
                into.Add(-f * d);
                into.Add(r * d);
                into.Add((-f - r).normalized * d);
                into.Add(-r * d);
                into.Add((f + r).normalized * d);
            }
        }

        public static bool NeedsCatchUp(float distance) { return distance > CatchUpDistance; }

        /// <summary>True when a crumb counts as reached.</summary>
        public static bool Reached(Vector3 at, Vector3 crumb)
        {
            float dx = at.x - crumb.x, dz = at.z - crumb.z;
            return dx * dx + dz * dz <= ReachFlat * ReachFlat && Mathf.Abs(at.y - crumb.y) <= ReachHeight;
        }

        /// <summary>A ground companion can hop a short heat gap or step: flat gap and climb in metres.</summary>
        public static bool CanHop(float flatGap, float climb, CompanionLocomotion loco)
        {
            if (loco != CompanionLocomotion.Walker) return true;
            return flatGap <= 4.5f && climb <= 1.6f;
        }
    }
}
