using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Which kind of space a point (or the player) is in. Surface is the open city, Interior a 04C venue,
    /// Underworld the Underheart network under the city, Veil the Inner World. The Underworld is never the Veil.</summary>
    public enum WorldRegion
    {
        Surface = 0,
        Interior = 1,
        Underworld = 2,
        Veil = 3
    }

    /// <summary>
    /// World limits shared by every system that used to assume a flat city. KillY is the fall-out-of-the-world plane,
    /// placed well below the deepest Underheart floor so nothing underground is reset to the street. IsUnderground asks
    /// the Underheart world (when it is running) whether a point lies inside one of its cells. Engine-free apart from
    /// the Vector3 maths.
    /// </summary>
    public static class WorldBounds
    {
        /// <summary>Below this height players respawn, NPCs, creatures, projectiles and lost vehicles are removed.</summary>
        public const float KillY = -400f;
        /// <summary>Points above this height are never treated as underground (the street, roofs, the canal bed).</summary>
        public const float UndergroundCeiling = -1.5f;
        /// <summary>Two points within this vertical distance of each other share a quiet zone or an encounter band underground.</summary>
        public const float VerticalBand = 14f;
        /// <summary>Below this height a point belongs to the underground layer for quiet zones and encounter bands.</summary>
        public const float LayerSplitY = -4f;

        /// <summary>Set by the Underheart world: true when the point is inside one of its cells.</summary>
        public static Func<Vector3, bool> UndergroundProbe;
        /// <summary>Set by the Underheart world: the camera boom limit in tight underground spaces (positive infinity elsewhere).</summary>
        public static Func<Vector3, float> CameraLimitProbe;

        public static void ResetStatics()
        {
            UndergroundProbe = null;
            CameraLimitProbe = null;
        }

        /// <summary>True when the point is inside the Underheart network. Never true at street level or above.</summary>
        public static bool IsUnderground(Vector3 p)
        {
            if (p.y > UndergroundCeiling || UndergroundProbe == null) return false;
            try { return UndergroundProbe(p); }
            catch (Exception e)
            {
                Log.Exception("Underground probe", e);
                UndergroundProbe = null;
                return false;
            }
        }

        /// <summary>The camera boom limit at a point (positive infinity when nothing limits it).</summary>
        public static float CameraLimit(Vector3 p)
        {
            if (CameraLimitProbe == null || p.y > UndergroundCeiling) return float.PositiveInfinity;
            try { return Mathf.Max(0.6f, CameraLimitProbe(p)); }
            catch (Exception e)
            {
                Log.Exception("Camera limit probe", e);
                CameraLimitProbe = null;
                return float.PositiveInfinity;
            }
        }

        /// <summary>True when two points share a vertical layer: both at street level and above, or underground within VerticalBand.</summary>
        public static bool SameLayer(Vector3 a, Vector3 b)
        {
            bool au = a.y < LayerSplitY, bu = b.y < LayerSplitY;
            if (!au && !bu) return true;
            if (au != bu) return false;
            return Mathf.Abs(a.y - b.y) <= VerticalBand;
        }
    }

    /// <summary>
    /// The player's current world region, refreshed by the Underheart world each frame (or lazily when asked), plus the
    /// last place the player stood on solid ground and which region that was. Falling out of the world uses these to
    /// put an underground player back at an underground rest point instead of a street.
    /// </summary>
    public static class WorldContext
    {
        public static WorldRegion Current { get; private set; }
        public static WorldRegion LastGroundedRegion { get; private set; }
        public static Vector3 LastGroundedPosition { get; private set; }
        public static bool HasGrounded { get; private set; }
        public static event Action<WorldRegion, WorldRegion> RegionChanged;

        public static void ResetStatics()
        {
            Current = WorldRegion.Surface;
            LastGroundedRegion = WorldRegion.Surface;
            LastGroundedPosition = Vector3.zero;
            HasGrounded = false;
            RegionChanged = null;
        }

        /// <summary>The region of a point. Interior only applies to the player position (04C venues track the player).</summary>
        public static WorldRegion RegionAt(Vector3 p, bool isPlayer)
        {
            if (Game.Inner != null && Game.Inner.IsInner) return WorldRegion.Veil;
            if (WorldBounds.IsUnderground(p)) return WorldRegion.Underworld;
            if (isPlayer && Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.Inside) return WorldRegion.Interior;
            return WorldRegion.Surface;
        }

        /// <summary>Recomputes the player's region and the grounded memory. Called once per frame by the Underheart world.</summary>
        public static void Refresh()
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            Vector3 pos = Game.PlayerPosition;
            WorldRegion now = RegionAt(pos, true);
            if ((p.IsGroundedPublic || p.InVehicle) && !p.IsDead && !p.IsRagdolled && MathUtil.IsFinite(pos))
            {
                LastGroundedRegion = now;
                LastGroundedPosition = pos;
                HasGrounded = true;
            }
            if (now == Current) return;
            WorldRegion was = Current;
            Current = now;
            if (RegionChanged == null) return;
            foreach (Delegate d in RegionChanged.GetInvocationList())
            {
                try { ((Action<WorldRegion, WorldRegion>)d)(was, now); }
                catch (Exception e) { Log.Exception("RegionChanged listener", e); }
            }
        }

        /// <summary>Forgets the session (new game, load, title).</summary>
        public static void ResetSession()
        {
            Current = WorldRegion.Surface;
            LastGroundedRegion = WorldRegion.Surface;
            LastGroundedPosition = Vector3.zero;
            HasGrounded = false;
        }
    }
}
