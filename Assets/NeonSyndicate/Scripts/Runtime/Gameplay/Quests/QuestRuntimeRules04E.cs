using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Small pure rules behind the 04E quest runtime (engine-free, tested offline by QuestRuntime04ETests): when a
    /// scattered quest prop must stay underground, whether a ring of props fits inside an Underheart cell, prop spacing,
    /// the fallback spots around an in-cell anchor, where an offset resume marker may stand, when a stored decision
    /// stands, when a boss fight that ended without victory is a reset rather than a failed quest, when a checkpoint asks
    /// for its autosave a second time, and the campaign flag that stops the Pressure House sparks.
    /// </summary>
    public static class QuestRuntimeRules04E
    {
        /// <summary>Campaign flag set when the Pressure House vent is diverted in uh05_02: the room's sparks stop for good.</summary>
        public const string PressureVentFlag = "pressure_vent_diverted";

        /// <summary>Minimum flat distance between two props one step scatters underground.</summary>
        public const float PropSpacing = 1.0f;
        /// <summary>Room a scattered prop keeps from a cell wall (the capsule the placement test sweeps).</summary>
        public const float WallMargin = 0.4f;
        /// <summary>Room a prop placed underground keeps from damaging heat (lava, hot vents), so the player can reach it safely.</summary>
        public const float HazardMargin = 0.6f;

        /// <summary>How far an offset resume marker stands from an anchor that already carries a world prompt (a rest point).</summary>
        public const float ResumeOffset = 1.6f;
        /// <summary>Prompt radius of an offset resume marker: smaller than ResumeOffset, so standing on the anchor shows the anchor's own prompt.</summary>
        public const float ResumeOffsetRadius = 1.3f;

        /// <summary>Times a quest resets a boss fight the player stepped out of before it stops with a retryable failure.</summary>
        public const int MaxBossResets = 12;

        // ================================================================== underground scatter

        /// <summary>
        /// True when a scattered point must be placed underground: the point itself lies in an Underheart cell, or the step's
        /// anchor does and the point sits at underground depth (a ring point that landed beyond the cell wall). A surface
        /// point whose reference happens to be underground (the player) stays a surface point.
        /// </summary>
        public static bool UseUnderground(bool pointUnderground, float pointY, bool anchorUnderground)
        {
            if (pointUnderground) return true;
            return anchorUnderground && pointY < WorldBounds.UndergroundCeiling;
        }

        /// <summary>Distance from a venue-frame point to the nearest wall of a cell (negative outside it).</summary>
        public static float WallClearance(UhCell c, float x, float z)
        {
            if (c == null) return -1f;
            return Mathf.Min(Mathf.Min(x - c.X0, c.X1 - x), Mathf.Min(z - c.Z0, c.Z1 - z));
        }

        /// <summary>True when every prop scattered within radius of a venue-frame point stays inside the cell with WallMargin to spare.</summary>
        public static bool ScatterFits(UhCell c, float x, float z, float radius)
        {
            return WallClearance(c, x, z) >= radius + WallMargin;
        }

        /// <summary>True when p is at least min metres (flat) from every point already taken.</summary>
        public static bool Spaced(Vector3 p, IList<Vector3> taken, float min)
        {
            if (taken == null) return true;
            float m2 = min * min;
            for (int i = 0; i < taken.Count; i++)
            {
                float dx = p.x - taken[i].x, dz = p.z - taken[i].z;
                if (dx * dx + dz * dz < m2) return false;
            }
            return true;
        }

        /// <summary>
        /// Fallback spots around an in-cell anchor when the wanted point (beyond a wall, inside a prop or over heat) cannot
        /// be used: rings of eight at 1.6 m and 0.9 m, nearest to the wanted point's direction first so several props fan out
        /// around the anchor, then the anchor itself. Flat offsets at the anchor's height; the caller finds the floor.
        /// </summary>
        public static List<Vector3> SpreadAround(Vector3 home, Vector3 wanted)
        {
            List<Vector3> list = new List<Vector3>(17);
            float dx = wanted.x - home.x, dz = wanted.z - home.z;
            float start = dx * dx + dz * dz > 0.0001f ? Mathf.Atan2(dz, dx) : 0f;
            float[] radii = { 1.6f, 0.9f };
            for (int r = 0; r < radii.Length; r++)
                for (int k = 0; k < 8; k++)
                {
                    float a = start + FanStep(k) * Mathf.PI * 0.25f;
                    list.Add(new Vector3(home.x + Mathf.Cos(a) * radii[r], home.y, home.z + Mathf.Sin(a) * radii[r]));
                }
            list.Add(home);
            return list;
        }

        /// <summary>The k-th step of a fan around a direction: 0, +1, -1, +2, -2, +3, -3, +4 (eighths of a turn).</summary>
        public static int FanStep(int k)
        {
            int step = (k + 1) / 2;
            return k % 2 == 1 ? step : -step;
        }

        /// <summary>
        /// The eight places an offset resume marker may stand around an anchor, ResumeOffset away, starting in front of the
        /// anchor (its yaw, Unity convention: 0 faces +z) and fanning out. The caller keeps the first one with floor and room.
        /// </summary>
        public static List<Vector3> ResumeSpots(Vector3 anchor, float yawDegrees)
        {
            List<Vector3> list = new List<Vector3>(8);
            for (int k = 0; k < 8; k++)
            {
                float a = (yawDegrees + FanStep(k) * 45f) * Mathf.Deg2Rad;
                list.Add(new Vector3(anchor.x + Mathf.Sin(a) * ResumeOffset, anchor.y, anchor.z + Mathf.Cos(a) * ResumeOffset));
            }
            return list;
        }

        // ================================================================== decisions, bosses, checkpoints

        /// <summary>A stored decision stands (no menu) unless the step asks again; an empty answer never stands.</summary>
        public static bool KeepsEarlierChoice(bool reask, string existing)
        {
            return !reask && !string.IsNullOrEmpty(existing);
        }

        /// <summary>
        /// A boss fight that ended without victory while the player is alive, not wasted or busted, and the quest still runs,
        /// is a reset (the player stepped out of the arena or withdrew through its seal): the quest tells them and waits for
        /// them to come back instead of failing. Death, an arrest or a quest that already ended fail normally.
        /// </summary>
        public static bool BossEndIsReset(bool playerAlive, bool wastedOrBusted, bool questRunning)
        {
            return playerAlive && !wastedOrBusted && questRunning;
        }

        /// <summary>
        /// A checkpoint asks for its autosave once; it asks a second time a few seconds later only when the first ask saved
        /// nothing (refused by the autosave's 4 s throttle, a menu or a transition), so a checkpoint never saves twice.
        /// </summary>
        public static bool CheckpointNeedsSecondAsk(bool firstAskSaved)
        {
            return !firstAskSaved;
        }
    }
}
