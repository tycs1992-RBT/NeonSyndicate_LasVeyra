using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Plain decisions that keep a following companion out of the player's way (H8S): whether its body sits in the
    /// camera's line to the player, whether it stands in the lane the player is walking along and where to step aside
    /// (or lead the way when there is no room beside it), the sheltered slot behind the player when it is hurt in a
    /// fight, the lower hover a flyer uses in tight spaces, and when a companion that fell far behind counts as having
    /// regrouped. Engine-free (Vector3 and Mathf only) and tested in CastCompanionsH8STests.
    /// </summary>
    public static class CompanionSpacingRulesH8S
    {
        /// <summary>Hover height in tight spaces, low enough to stay under the camera's line to the player's chest.</summary>
        public const float TightHover = 0.75f;
        /// <summary>The player counts as walking somewhere above this flat speed (metres per second).</summary>
        public const float MovingSpeed = 1f;
        /// <summary>How far ahead along the player's walk the companion counts as being in the way.</summary>
        public const float LaneLookAhead = 3.5f;
        /// <summary>Extra room kept between the player's lane and the companion's body after it steps aside.</summary>
        public const float SidestepMargin = 0.35f;
        /// <summary>How far ahead it walks to lead the way when there is no room to step aside.</summary>
        public const float LeadDistance = 3.2f;
        /// <summary>Below this share of health the companion keeps out of harm's way in a fight.</summary>
        public const float LowHealth = 0.3f;
        /// <summary>The sheltered slot sits this much further back than the normal follow distance.</summary>
        public const float ShelterExtra = 0.8f;
        /// <summary>A companion further than this from the player has fallen behind; coming back within reach is a regroup.</summary>
        public const float RegroupFar = 20f;
        /// <summary>A waiting companion further than this reminds the player once that it is still waiting.</summary>
        public const float WaitReminderDistance = 60f;
        /// <summary>Clearance added to the body radius when testing the camera line.</summary>
        public const float ViewMargin = 0.35f;
        /// <summary>A hurt companion counts as settled on its sheltered slot within this flat distance.</summary>
        public const float ShelterStopRadius = 1.6f;
        /// <summary>A new threat replaces the one it shelters from only when nearer by this many metres...</summary>
        public const float ThreatSwitchMetres = 2f;
        /// <summary>...or by this share of the current threat's distance, whichever is larger.</summary>
        public const float ThreatSwitchShare = 0.3f;

        /// <summary>The shortest distance from p to the segment a to b; t is where along it (0 at a, 1 at b).</summary>
        public static float SegmentDistance(Vector3 a, Vector3 b, Vector3 p, out float t)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            t = len2 > 1e-8f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
            return (a + ab * t - p).magnitude;
        }

        /// <summary>
        /// True when a body (centre point and radius) blocks the camera's view of the player: it lies close to the line
        /// from the camera to the player's chest, between the two (not behind the camera, not beyond the player).
        /// </summary>
        public static bool OccludesView(Vector3 camera, Vector3 playerChest, Vector3 bodyCentre, float bodyRadius)
        {
            float t;
            float d = SegmentDistance(camera, playerChest, bodyCentre, out t);
            if (t <= 0.05f || t >= 0.92f) return false;
            return d < Mathf.Max(0.1f, bodyRadius) + ViewMargin;
        }

        /// <summary>
        /// True when the companion stands in the lane the player is walking along: the player moves faster than
        /// MovingSpeed and the companion lies ahead within lookAhead and sideways within halfWidth (flat distances).
        /// </summary>
        public static bool InPlayerLane(Vector3 player, Vector3 playerVelocity, Vector3 companion, float halfWidth, float lookAhead)
        {
            Vector3 v = new Vector3(playerVelocity.x, 0f, playerVelocity.z);
            float speed = v.magnitude;
            if (!(speed >= MovingSpeed)) return false;
            Vector3 dir = v / speed;
            Vector3 to = new Vector3(companion.x - player.x, 0f, companion.z - player.z);
            float along = Vector3.Dot(to, dir);
            if (along < -0.2f || along > lookAhead) return false;
            Vector3 lateral = to - dir * along;
            return lateral.magnitude < halfWidth;
        }

        /// <summary>
        /// Where to step aside from the player's lane: toward the side the companion already leans to (the preferred
        /// side on a tie, the other side when otherSide is set), just clear of the lane, level with where it stands
        /// along the walk (kept between 0.4 m and 2.5 m ahead). The height is the player's; the caller finds the floor.
        /// </summary>
        public static Vector3 SidestepPoint(Vector3 player, Vector3 moveDir, Vector3 companion, int preferredSide, float halfWidth, bool otherSide)
        {
            Vector3 dir = FlatDir(moveDir);
            Vector3 right = new Vector3(dir.z, 0f, -dir.x);
            Vector3 to = new Vector3(companion.x - player.x, 0f, companion.z - player.z);
            float lateral = Vector3.Dot(to, right);
            float side = Mathf.Abs(lateral) > 0.05f ? Mathf.Sign(lateral) : (preferredSide >= 0 ? 1f : -1f);
            if (otherSide) side = -side;
            float along = Mathf.Clamp(Vector3.Dot(to, dir), 0.4f, 2.5f);
            return player + dir * along + right * (side * (halfWidth + SidestepMargin));
        }

        /// <summary>
        /// How far ahead a companion leads the way: LeadDistance at most, and always inside its idle radius (the follow
        /// distance plus 0.6 m) with a margin, so when the player stops it is already settled and never walks back
        /// through the player to the slot behind.
        /// </summary>
        public static float LeadFor(float followDistance)
        {
            return Mathf.Clamp(followDistance + 0.6f - 0.4f, 1.2f, LeadDistance);
        }

        /// <summary>A point ahead along the player's walk, for leading the way when there is no room beside the lane.</summary>
        public static Vector3 LeadPoint(Vector3 player, Vector3 moveDir, float lead)
        {
            return player + FlatDir(moveDir) * Mathf.Max(0f, lead);
        }

        /// <summary>
        /// The sheltered slot for a hurt companion in a fight: behind the player as seen from the threat (so the player
        /// stands between them), at the given distance and a little to its own side. With no usable threat direction it
        /// falls back to straight behind the player.
        /// </summary>
        public static Vector3 ShelterSlot(Vector3 player, Vector3 threat, Vector3 playerForward, int side, float distance)
        {
            Vector3 away = new Vector3(player.x - threat.x, 0f, player.z - threat.z);
            if (away.sqrMagnitude < 0.01f) away = -FlatDir(playerForward);
            away.Normalize();
            Vector3 right = new Vector3(away.z, 0f, -away.x) * (side >= 0 ? 1f : -1f);
            float d = Mathf.Max(0.5f, distance);
            return player + away * d + right * (d * 0.35f);
        }

        /// <summary>The hover height for the space: in a tight space at most TightHover (never lower than the body needs).</summary>
        public static float HoverFor(float hover, bool tight, float bodyHeight)
        {
            if (!tight) return hover;
            float floor = Mathf.Max(0.3f, bodyHeight * 0.5f + 0.2f);
            return Mathf.Min(hover, Mathf.Max(TightHover, floor));
        }

        /// <summary>
        /// Threat hysteresis: true when a candidate threat at candidateDistance should replace the current one at
        /// currentDistance (clearly nearer: by ThreatSwitchMetres or ThreatSwitchShare of the current distance, whichever
        /// is larger), so foes on both sides never make the sheltered slot jump across the player.
        /// </summary>
        public static bool SwitchThreat(float currentDistance, float candidateDistance)
        {
            if (float.IsNaN(candidateDistance)) return false;
            if (float.IsNaN(currentDistance) || float.IsInfinity(currentDistance)) return true;
            return candidateDistance < currentDistance - Mathf.Max(ThreatSwitchMetres, ThreatSwitchShare * currentDistance);
        }

        /// <summary>True when a hurt companion keeps out of harm's way: a fight is on and health is above zero but low.</summary>
        public static bool ShouldShelter(float health01, bool combat)
        {
            return combat && health01 > 0f && health01 < LowHealth;
        }

        /// <summary>True on the drop that crosses below the low-health threshold (from at or above it).</summary>
        public static bool CrossedLow(float before01, float after01)
        {
            return before01 >= LowHealth && after01 < LowHealth && after01 > 0f;
        }

        private static Vector3 FlatDir(Vector3 v)
        {
            Vector3 f = new Vector3(v.x, 0f, v.z);
            return f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
        }
    }

    /// <summary>
    /// Watches the distance between the player and a following companion: once it has fallen beyond RegroupFar, the
    /// first time it comes back within reach counts as one regroup. Engine-free.
    /// </summary>
    public struct RegroupTrackerH8S
    {
        /// <summary>True while the companion is counted as left behind.</summary>
        public bool Far;

        /// <summary>One sample: true on the sample it regroups (back within reach after being far).</summary>
        public bool Update(float distance, float reach)
        {
            if (float.IsNaN(distance)) return false;
            if (distance > CompanionSpacingRulesH8S.RegroupFar)
            {
                Far = true;
                return false;
            }
            if (Far && distance <= Mathf.Max(1f, reach))
            {
                Far = false;
                return true;
            }
            return false;
        }

        public void Reset() { Far = false; }
    }
}
