using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>How an enemy blow travels to the player, which decides what the ground shows before it lands.</summary>
    public enum TellShapeH8S
    {
        /// <summary>No blow at all (a howl, a roar, a screech).</summary>
        None,
        /// <summary>A frontal melee blow (bite, claw, fist): the pose, the glow and the "!" carry it.</summary>
        Strike,
        /// <summary>A blow all round the body (a tail sweep, a flail): a ground ring around the body.</summary>
        Sweep,
        /// <summary>A leap at the player (a pounce): a ground lane along the leap.</summary>
        Lunge,
        /// <summary>A straight rush along the ground (a charge): a ground lane along the rush.</summary>
        Charge,
        /// <summary>A dive out of the air: no ground mark, the diving body is the tell.</summary>
        Dive,
        /// <summary>An area blow (a slam, an eruption, a falling rain): a ground ring that fills.</summary>
        Area,
        /// <summary>A cone from the mouth (breath, gaze, spray).</summary>
        Breath,
        /// <summary>A thrown or spat shot.</summary>
        Shot
    }

    /// <summary>What the player should do about a blow. It is also the blow's colour and mark.</summary>
    public enum TellSeverityH8S
    {
        /// <summary>It can be guarded or parried: red and one "!".</summary>
        Guard,
        /// <summary>It cannot be blocked: orange and "!!". Dodge it or leave the marked ground.</summary>
        Dodge
    }

    /// <summary>The ground mark a tell draws.</summary>
    public enum TellMarkH8S
    {
        /// <summary>Nothing on the ground.</summary>
        None,
        /// <summary>A ring that fills (areas and all-round sweeps).</summary>
        Ring,
        /// <summary>A lane along the path of a travelling blow (pounces and charges).</summary>
        Lane
    }

    /// <summary>
    /// The H8S tell language for every enemy blow (engine-free, tested offline). One colour and one mark per answer: red
    /// with one "!" for a blow you may guard or parry, orange with "!!" for a blow you cannot block (dodge it or leave
    /// the marked ground). The HUD glyphs, the ground rings, the lanes and the flash before an unblockable blow all use
    /// these two colours. One ground mark per body shape: a ring for blows that cover an area (slams, eruptions,
    /// all-round sweeps), a lane for blows that travel (pounces, charges), nothing for frontal strikes. Marks are drawn
    /// at the true reach of the hit test, a travelling blow's aim locks before it launches so its lane can be stepped
    /// out of, and every wind-up has a minimum by its class and the difficulty, long enough over an area to get out of
    /// it with one dodge and a few steps.
    /// </summary>
    public static class CombatTellsH8S
    {
        /// <summary>Red: a blow that can be guarded or parried (one "!").</summary>
        public static readonly Color GuardColor = new Color(1f, 0.25f, 0.18f, 1f);
        /// <summary>Orange: a blow that cannot be blocked ("!!"). Dodge it or leave the marked ground.</summary>
        public static readonly Color DodgeColor = new Color(1f, 0.62f, 0.12f, 1f);
        /// <summary>The hot flash a moment before an unblockable blow lands (the orange, burning brighter).</summary>
        public static readonly Color DodgeFlashColor = new Color(1f, 0.5f, 0.08f, 1f);

        /// <summary>Where the outline of the ground ring texture visibly ends, as a fraction of the quad's half size (UIKit.Ring).</summary>
        public const float RingVisibleEdge = 0.95f;
        /// <summary>The fraction of a wind-up after which a travelling blow's aim and the body's facing are fixed.</summary>
        public const float AimLockFraction = 0.7f;
        /// <summary>The widest re-aim (degrees off the body's facing) a travelling blow takes toward its target.</summary>
        public const float MaxAimSnap = 50f;
        /// <summary>The body radius the creature hit tests give the player (they never use less than this).</summary>
        public const float PlayerRadius = 0.3f;
        /// <summary>Extra reach the creature ring hit test adds beyond the ring radius, before the player's body.</summary>
        public const float CreatureRingSlack = 0.3f;
        /// <summary>Distance a full dodge roll covers (PlayerCombat).</summary>
        public const float DodgeDistance = 4.3f;
        /// <summary>Seconds a full dodge roll takes (PlayerCombat).</summary>
        public const float DodgeSeconds = 0.64f;
        /// <summary>The player's jog speed after the roll (TravelMath.JogSpeed).</summary>
        public const float JogSpeed = 4.4f;

        private static readonly float[] LightFloor = { 0.42f, 0.36f, 0.32f };
        private static readonly float[] HeavyFloor = { 0.75f, 0.62f, 0.55f };
        private static readonly float[] TravelFloor = { 0.8f, 0.65f, 0.55f };
        private static readonly float[] Reaction = { 0.45f, 0.35f, 0.28f };

        private static int Index(DifficultyLevel d)
        {
            return d == DifficultyLevel.Story ? 0 : (d == DifficultyLevel.Balanced ? 1 : 2);
        }

        // ------------------------------------------------------------------ colour and mark

        /// <summary>Unblockable blows are Dodge (orange "!!"); everything else is Guard (red "!").</summary>
        public static TellSeverityH8S SeverityOf(DamageFlags flags)
        {
            return (flags & DamageFlags.Unblockable) != 0 ? TellSeverityH8S.Dodge : TellSeverityH8S.Guard;
        }

        /// <summary>The severity of a melee move swung by a person or a humanoid creature.</summary>
        public static TellSeverityH8S SeverityOf(MeleeMove m)
        {
            return m != null && m.Unblockable ? TellSeverityH8S.Dodge : TellSeverityH8S.Guard;
        }

        /// <summary>The colour of a severity (glyphs, ring edges, lanes).</summary>
        public static Color ColorOf(TellSeverityH8S s)
        {
            return s == TellSeverityH8S.Dodge ? DodgeColor : GuardColor;
        }

        /// <summary>The glyph over the attacker: "!!" for a blow that cannot be blocked, "!" otherwise.</summary>
        public static string MarkOf(TellSeverityH8S s)
        {
            return s == TellSeverityH8S.Dodge ? "!!" : "!";
        }

        // ------------------------------------------------------------------ shape

        /// <summary>The tell shape of a creature attack.</summary>
        public static TellShapeH8S ShapeOf(CreatureAttack a)
        {
            if (a == null) return TellShapeH8S.None;
            return ShapeOf(a.Shape, a.Arc, a.Use);
        }

        /// <summary>The tell shape of an attack shape (arc of 300 degrees or more sweeps all round; air-only lines dive).</summary>
        public static TellShapeH8S ShapeOf(AttackShape shape, float arc, AttackUse use)
        {
            switch (shape)
            {
                case AttackShape.Arc: return arc >= 300f ? TellShapeH8S.Sweep : TellShapeH8S.Strike;
                case AttackShape.Leap: return TellShapeH8S.Lunge;
                case AttackShape.Line: return (use & AttackUse.Ground) != 0 ? TellShapeH8S.Charge : TellShapeH8S.Dive;
                case AttackShape.Ring:
                case AttackShape.Under: return TellShapeH8S.Area;
                case AttackShape.Cone: return TellShapeH8S.Breath;
                case AttackShape.Projectile: return TellShapeH8S.Shot;
                default: return TellShapeH8S.None;
            }
        }

        /// <summary>The tell shape of a melee move (all-round arcs sweep).</summary>
        public static TellShapeH8S ShapeOf(MeleeMove m)
        {
            if (m == null) return TellShapeH8S.None;
            return m.Arc >= 300f ? TellShapeH8S.Sweep : TellShapeH8S.Strike;
        }

        /// <summary>The ground mark a shape draws: rings for areas and sweeps, lanes for pounces and charges.</summary>
        public static TellMarkH8S MarkFor(TellShapeH8S s)
        {
            switch (s)
            {
                case TellShapeH8S.Sweep:
                case TellShapeH8S.Area: return TellMarkH8S.Ring;
                case TellShapeH8S.Lunge:
                case TellShapeH8S.Charge: return TellMarkH8S.Lane;
                default: return TellMarkH8S.None;
            }
        }

        /// <summary>True for blows that land at one moment at the end of their wind-up (what the impact cadence spaces out).</summary>
        public static bool LandsAtCommit(TellShapeH8S s)
        {
            return s == TellShapeH8S.Strike || s == TellShapeH8S.Sweep || s == TellShapeH8S.Lunge || s == TellShapeH8S.Charge || s == TellShapeH8S.Area;
        }

        // ------------------------------------------------------------------ minimum wind-ups

        /// <summary>Seconds the player needs to notice a tell and start to act, by difficulty.</summary>
        public static float ReactionSeconds(DifficultyLevel d)
        {
            return Reaction[Index(d)];
        }

        /// <summary>
        /// The shortest readable wind-up of a blow by its class and the difficulty: frontal strikes and shots 0.42, 0.36
        /// and 0.32 s (Story, Balanced, Veteran); heavy ones (knockdowns, unblockable) 0.75, 0.62 and 0.55 s; travelling
        /// blows and cones 0.8, 0.65 and 0.55 s. Veteran keeps the original floors exactly (0.32 and 0.55).
        /// </summary>
        public static float MinWindup(TellShapeH8S shape, bool heavy, DifficultyLevel d)
        {
            int i = Index(d);
            switch (shape)
            {
                case TellShapeH8S.None: return 0f;
                case TellShapeH8S.Lunge:
                case TellShapeH8S.Charge:
                case TellShapeH8S.Dive:
                case TellShapeH8S.Breath: return Mathf.Max(TravelFloor[i], heavy ? HeavyFloor[i] : 0f);
                default: return heavy ? HeavyFloor[i] : LightFloor[i];
            }
        }

        /// <summary>
        /// Seconds to get distance metres away: one dodge roll (DodgeDistance in DodgeSeconds), then a jog for the rest.
        /// Negative or broken distances count as zero.
        /// </summary>
        public static float EscapeSeconds(float distance)
        {
            if (!(distance > 0f)) return 0f;
            if (distance <= DodgeDistance) return distance / DodgeDistance * DodgeSeconds;
            return DodgeSeconds + (distance - DodgeDistance) / JogSpeed;
        }

        /// <summary>The shortest fill of an area blow the player must leave from escapeDistance metres deep: reaction plus escape.</summary>
        public static float MinAreaWindup(float escapeDistance, DifficultyLevel d)
        {
            return ReactionSeconds(d) + EscapeSeconds(escapeDistance);
        }

        /// <summary>
        /// How far inside its danger zone the player can stand for a creature attack (0 for blows that do not cover an
        /// area): an all-round sweep reaches Reach past the body's edge; a ring around the body reaches its radius plus
        /// the ring slack and the player's body, measured from the body's edge; a ring placed ahead of the body or under
        /// the player can be stood in at its centre.
        /// </summary>
        public static float EscapeDistance(CreatureAttack a, float bodyRadius, float meleeScale)
        {
            if (a == null) return 0f;
            TellShapeH8S s = ShapeOf(a);
            if (s == TellShapeH8S.Sweep) return Mathf.Max(0f, a.Reach * Mathf.Clamp01(meleeScale <= 0f ? 1f : meleeScale)) + PlayerRadius;
            if (s != TellShapeH8S.Area) return 0f;
            float hit = CreatureRingHitRadius(a.Reach);
            if (a.Shape == AttackShape.Ring && a.RingOffset <= 0f) return Mathf.Max(0f, hit - Mathf.Max(0f, bodyRadius));
            return hit;
        }

        /// <summary>The floor of a creature attack's wind-up: its class minimum and, over an area, the time to get out of it.</summary>
        public static float CreatureWindupFloor(CreatureAttack a, DifficultyLevel d, float bodyRadius, float meleeScale)
        {
            if (a == null) return 0f;
            TellShapeH8S s = ShapeOf(a);
            bool heavy = (a.Flags & (DamageFlags.Knockdown | DamageFlags.Unblockable)) != 0;
            float floor = MinWindup(s, heavy, d);
            float escape = EscapeDistance(a, bodyRadius, meleeScale);
            if (escape > 0f) floor = Mathf.Max(floor, MinAreaWindup(escape, d));
            return floor;
        }

        // ------------------------------------------------------------------ true reach

        /// <summary>The ring quad radius whose visible outline ends exactly at hitRadius (so the drawn edge is the real edge).</summary>
        public static float RingQuadRadius(float hitRadius)
        {
            return Mathf.Max(0.3f, hitRadius) / RingVisibleEdge;
        }

        /// <summary>How far from a creature ring's centre the player's feet are hit (Creature.InRing: radius, slack, body).</summary>
        public static float CreatureRingHitRadius(float radius)
        {
            return Mathf.Max(0f, radius) + CreatureRingSlack + PlayerRadius;
        }

        /// <summary>How far from the body's centre an all-round sweep reaches the player's feet (Creature.InArc).</summary>
        public static float SweepHitRadius(float bodyRadius, float reach)
        {
            return Mathf.Max(0f, bodyRadius) + Mathf.Max(0f, reach) + PlayerRadius;
        }

        /// <summary>Half the width of a travelling blow's lane: its contact reach to either side of the body's path.</summary>
        public static float LaneHalfWidth(float bodyRadius, float reach)
        {
            return Mathf.Max(0f, bodyRadius) + Mathf.Max(0f, reach) + PlayerRadius;
        }

        /// <summary>
        /// The length of a travelling blow's lane from the body's centre: a pounce covers the gap to its target plus a
        /// little (never more than its speed over the active window), a charge its full speed over the active window;
        /// either then reaches past the body's front by its contact reach.
        /// </summary>
        public static float LaneLength(TellShapeH8S shape, float speed, float active, float targetDistance, float bodyRadius, float reach)
        {
            float run = Mathf.Max(0f, speed) * Mathf.Max(0.05f, active);
            float travel;
            if (shape == TellShapeH8S.Lunge)
            {
                float gap = Mathf.Max(0f, targetDistance - Mathf.Max(0f, bodyRadius) - 0.4f);
                travel = Mathf.Min(run, gap + Mathf.Max(0.05f, active));
            }
            else travel = run;
            return travel + Mathf.Max(0f, bodyRadius) + Mathf.Max(0f, reach) + PlayerRadius;
        }

        /// <summary>
        /// The direction a travelling blow will take: toward the target when it lies within maxSnapDeg of the facing,
        /// else straight ahead. Both are flattened; a zero facing falls back to +Z.
        /// </summary>
        public static Vector3 AimFor(Vector3 facing, Vector3 toTarget, float maxSnapDeg)
        {
            Vector3 f = new Vector3(facing.x, 0f, facing.z);
            f = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            Vector3 t = new Vector3(toTarget.x, 0f, toTarget.z);
            if (t.sqrMagnitude <= 0.01f) return f;
            t.Normalize();
            float cos = Mathf.Clamp(Vector3.Dot(f, t), -1f, 1f);
            float deg = Mathf.Acos(cos) * Mathf.Rad2Deg;
            return deg < maxSnapDeg ? t : f;
        }

        /// <summary>True once a wind-up has run long enough for a travelling blow's aim to lock.</summary>
        public static bool AimLocked(float windupElapsed, float windup)
        {
            return windupElapsed >= Mathf.Max(0f, windup) * AimLockFraction;
        }
    }
}
