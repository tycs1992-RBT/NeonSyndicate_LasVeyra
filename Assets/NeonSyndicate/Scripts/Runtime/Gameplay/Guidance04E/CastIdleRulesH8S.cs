using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How a named person passes the time at their post (H8S role-aware idle).</summary>
    public enum CastIdleStyleH8S
    {
        /// <summary>Standing watch: no busy hands, the head slowly scans the room or street.</summary>
        Watch = 0,
        /// <summary>Working with a tool at the station (the forgewright, the mechanics): hands busy, head down.</summary>
        Work = 1,
        /// <summary>Reading a ledger, clipboard or slate held at chest height.</summary>
        Read = 2,
        /// <summary>Tending supplies (the clinic and the refuge carers): shorter busy spells, more glances up.</summary>
        Tend = 3
    }

    /// <summary>One moment of a person's idle: hands busy (the existing Inspect gesture) and/or a glance (head look).</summary>
    public struct CastIdleBeatH8S
    {
        /// <summary>Both hands forward at chest height, head lowered (HumanoidAnimator.Inspect).</summary>
        public bool Busy;
        /// <summary>The head turns toward a point GlanceYaw degrees off the post's facing (HumanoidAnimator.HeadLook).</summary>
        public bool Glance;
        public float GlanceYaw;
    }

    /// <summary>
    /// Role-aware idle and conversation staging for the named cast, as plain maths: the idle style that fits a person's
    /// tool and role, the beat at any moment (busy spells and glances, offset per person so neighbours never move in
    /// step, calmer on a break, alert in a storm), when the player counts as engaging them in conversation (with
    /// hysteresis so the head does not flick back and forth at the edge) and when a quest presented at a venue is at
    /// its hand-in. Only existing animator flags are driven (Inspect, HeadLook); the residence routine and the
    /// director keep movement and facing. Engine-free and tested.
    /// </summary>
    public static class CastIdleRulesH8S
    {
        /// <summary>The player engages a person within this flat distance when they can see each other.</summary>
        public const float EngageRange = 4.5f;
        /// <summary>An engaged person stays engaged until the player is beyond this distance (or out of sight).</summary>
        public const float ReleaseRange = 7f;
        /// <summary>After the player leaves, the person waits this long before going back to work (seconds).</summary>
        public const float ReturnDelay = 2.5f;
        /// <summary>A glance looks at a point this far from the head along the glance direction (metres).</summary>
        public const float GlanceDistance = 6f;

        /// <summary>The idle style for a person's held tool (a missing tool stands watch).</summary>
        public static CastIdleStyleH8S StyleFor(HeldTool tool)
        {
            switch (tool)
            {
                case HeldTool.Hammer:
                case HeldTool.Wrench: return CastIdleStyleH8S.Work;
                case HeldTool.Ledger:
                case HeldTool.Clipboard:
                case HeldTool.Tablet:
                case HeldTool.DataSlate: return CastIdleStyleH8S.Read;
                case HeldTool.Medkit: return CastIdleStyleH8S.Tend;
                default: return CastIdleStyleH8S.Watch;
            }
        }

        /// <summary>
        /// The idle beat at time t (seconds, any clock) for a person with this style, seed and resident shift. Work,
        /// Read and Tend alternate a busy spell with a glance up; Watch only glances, alternating sides. On a break or
        /// at rest the busy spells shrink, and in a storm (Shelter) nobody keeps working: they glance around.
        /// </summary>
        public static CastIdleBeatH8S Beat(CastIdleStyleH8S style, ResidentShift04M shift, float t, int seed)
        {
            CastIdleBeatH8S b = new CastIdleBeatH8S();
            if (float.IsNaN(t) || float.IsInfinity(t)) return b;
            int s = seed < 0 ? -seed : seed;
            float busy, glance, rest;
            switch (style)
            {
                case CastIdleStyleH8S.Work: busy = 7f; glance = 2.4f; rest = 1.2f; break;
                case CastIdleStyleH8S.Read: busy = 8.5f; glance = 2f; rest = 1f; break;
                case CastIdleStyleH8S.Tend: busy = 4.5f; glance = 2.2f; rest = 1.5f; break;
                default: busy = 0f; glance = 2.2f; rest = 3.6f; break;
            }
            if (shift == ResidentShift04M.Break || shift == ResidentShift04M.Rest) busy *= 0.4f;
            if (shift == ResidentShift04M.Shelter) busy = 0f;
            float period = busy + glance + rest;
            float local = Mathf.Repeat(t + (s % 13) * 0.77f, period);
            int cycle = Mathf.FloorToInt((t + (s % 13) * 0.77f) / period);
            if (local < busy)
            {
                b.Busy = true;
                return b;
            }
            if (local < busy + glance)
            {
                b.Glance = true;
                float side = ((cycle + s) & 1) == 0 ? 1f : -1f;
                float width = style == CastIdleStyleH8S.Watch || shift == ResidentShift04M.Shelter ? 42f : 28f;
                b.GlanceYaw = side * (width - ((cycle * 7 + s) % 3) * 6f);
            }
            return b;
        }

        /// <summary>
        /// True while the player engages the person: inside EngageRange with sight, kept until beyond ReleaseRange or
        /// out of sight; always while a scene or the quest's dialogue with them runs (talking).
        /// </summary>
        public static bool Engaged(bool wasEngaged, float distance, bool lineOfSight, bool talking)
        {
            if (talking) return true;
            if (!lineOfSight || float.IsNaN(distance)) return false;
            return distance <= (wasEngaged ? ReleaseRange : EngageRange);
        }

        /// <summary>
        /// True when a quest presented at a venue is at its hand-in: the run is in its last phase and every step from
        /// the current one on is either instant or happens at that venue (atVenue[i] says so for step i: a step whose
        /// place is the venue, or dialogue with no place of its own), with at least one such venue step left. Used to
        /// show the giver's TURN IN check while the player walks back.
        /// </summary>
        public static bool AtVenueHandIn(bool lastPhase, string[] stepTypes, bool[] atVenue, int currentStep, string[] instantTypes)
        {
            if (!lastPhase || stepTypes == null || atVenue == null || stepTypes.Length != atVenue.Length) return false;
            if (currentStep < 0 || currentStep >= stepTypes.Length) return false;
            bool sawVenueStep = false;
            for (int i = currentStep; i < stepTypes.Length; i++)
            {
                if (atVenue[i])
                {
                    sawVenueStep = true;
                    continue;
                }
                string type = stepTypes[i] ?? "";
                if (instantTypes != null && System.Array.IndexOf(instantTypes, type) >= 0) continue;
                return false;
            }
            return sawVenueStep;
        }
    }
}
