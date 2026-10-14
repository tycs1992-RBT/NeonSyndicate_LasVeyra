using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Small fairness and fight rules shared by the creature runtime, the player's combat, the wanted system and the
    /// boss arenas: which hits are too small to flinch the player, the golems' chest-core weak point, a shot that is
    /// clearly aimed at a creature, a shove stopped by a wall, the breath cone's opening aim, how long a stalker without a
    /// pounce waits behind you, the respite between boss ring hits and the arena start rule. Pure (tested offline).
    /// </summary>
    public static class CreatureFightRules
    {
        /// <summary>Hits under this much damage, with no force and nothing heavy about them, do not flinch the player.</summary>
        public const float NibbleDamage = 8f;
        /// <summary>The chest core of a golem, the Titan or the Foreman: hits this close (times the rig's scale) are criticals.</summary>
        public const float CoreRadius = 0.4f;
        /// <summary>Seconds after a boss ring hits the player during which further ring bursts pass harmlessly.</summary>
        public const float RingRespite = 0.6f;
        /// <summary>Metres past the start radius the player must step out before an arena they stood in can start.</summary>
        public const float ArenaExitMargin = 4f;
        /// <summary>Seconds an arena stays quiet after a mission or a nearby world event ends.</summary>
        public const float ArenaGrace = 10f;

        /// <summary>
        /// True for a hit too small to flinch the player (a Glass Mite's nibble): almost no knockback force, neither heavy
        /// nor a knockdown, and only a few points of damage. It still hurts; it just does not break a roll or a guard.
        /// </summary>
        public static bool IsNibble(float force, DamageFlags flags, float amount)
        {
            if ((flags & (DamageFlags.Heavy | DamageFlags.Knockdown)) != 0) return false;
            return force < 1f && amount < NibbleDamage;
        }

        /// <summary>True when a hit point lands on a golem's glowing chest core (within CoreRadius times the rig's scale).</summary>
        public static bool OnCore(Vector3 point, Vector3 chest, float rigScale)
        {
            float r = CoreRadius * Mathf.Max(0.3f, rigScale);
            return (point - chest).sqrMagnitude <= r * r;
        }

        /// <summary>True when point lies within radius of the segment from a shot's muzzle to where it went.</summary>
        public static bool NearShotLine(Vector3 from, Vector3 to, Vector3 point, float radius)
        {
            Vector3 seg = to - from;
            float len2 = seg.sqrMagnitude;
            float t = len2 > 1e-6f ? Mathf.Clamp01(Vector3.Dot(point - from, seg) / len2) : 0f;
            Vector3 closest = from + seg * t;
            return (point - closest).sqrMagnitude <= radius * radius;
        }

        /// <summary>
        /// A shove that runs into a wall: the part of push that drives into the surface (wallNormal points out of the
        /// wall) is dropped, the part along it is kept, so the body slides instead of passing through.
        /// </summary>
        public static Vector3 SlideAlongWall(Vector3 push, Vector3 wallNormal)
        {
            Vector3 n = new Vector3(wallNormal.x, 0f, wallNormal.z);
            float m = n.magnitude;
            if (m < 1e-4f) return new Vector3(0f, push.y, 0f);
            n /= m;
            float into = Vector3.Dot(push, n);
            return into < 0f ? push - n * into : push;
        }

        /// <summary>
        /// Where a breath or gaze cone points as it opens: straight at the target in three dimensions (a flyer breathes
        /// down at you) when the target is within maxAngle degrees of the creature's flat facing, else along the facing.
        /// </summary>
        public static Vector3 ConeOpening(Vector3 facing, Vector3 mouth, Vector3 target, float maxAngle)
        {
            Vector3 fwd = new Vector3(facing.x, 0f, facing.z);
            fwd = fwd.sqrMagnitude > 1e-6f ? fwd.normalized : Vector3.forward;
            Vector3 aim = target - mouth;
            if (aim.sqrMagnitude < 1e-4f) return fwd;
            Vector3 flat = new Vector3(aim.x, 0f, aim.z);
            if (flat.sqrMagnitude > 1e-4f && Vector3.Angle(fwd, flat) > maxAngle) return fwd;
            return aim.normalized;
        }

        /// <summary>
        /// A stalker with no pounce (the Hollow Shade) strikes once it has hung behind the player for strikeAfter seconds;
        /// behindFor is the time it has spent there so far.
        /// </summary>
        public static bool StalkerStrikes(bool hasLeap, float behindFor, float strikeAfter)
        {
            return !hasLeap && behindFor >= strikeAfter;
        }

        /// <summary>True when a boss ring may hurt the player now: none has hit them in the last RingRespite seconds.</summary>
        public static bool RingMayHit(float now, float lastRingHit)
        {
            return now - lastRingHit >= RingRespite;
        }

        /// <summary>
        /// One tick of a boss arena's start rule. distance is the player's distance from the arena centre, startRadius
        /// where walking in starts the fight, busy is true while a mission or a nearby world event runs (and for
        /// ArenaGrace seconds after), canFight is the arena's own rule (found, right world, right hour, no cooldown).
        /// needsExit remembers a player who stood inside while it was busy (or when a fight there ended): the fight then
        /// waits until they have stepped out past the margin and walked back in. Returns true when the fight starts.
        /// </summary>
        public static bool ArenaStarts(float distance, float startRadius, bool busy, bool canFight, ref bool needsExit)
        {
            bool inside = distance <= startRadius;
            if (distance > startRadius + ArenaExitMargin) needsExit = false;
            else if (inside && busy) needsExit = true;
            return inside && !busy && !needsExit && canFight;
        }
    }
}
