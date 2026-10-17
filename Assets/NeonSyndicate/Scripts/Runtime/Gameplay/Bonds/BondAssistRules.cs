using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Why a companion command was refused (every refusal has a plain reason and a sound).</summary>
    public enum AssistRefusal
    {
        None = 0,
        NoCompanion,
        NotBonded,
        Downed,
        Resting,
        Busy,
        Cooldown,
        NoTarget,
        Neutral,
        TooFar,
        Occluded,
        Unreachable,
        InVehicle,
        Fused,
        Rescue,
        Waiting
    }

    /// <summary>What the U press should do, decided from the aim, the lock and the fight around the player.</summary>
    public enum AssistIntent
    {
        Explore = 0,
        Combat = 1,
        Refuse = 2
    }

    /// <summary>
    /// The decisions behind U and the brace, as plain math: which assist a press asks for, whether a combat target is
    /// valid (hostile, within 25 m, in line of sight, on a reachable layer), the refusal text, and how the Ashshell
    /// Warden's brace splits a hit (cone, share, capacity, no help against falls, vehicles or unblockable blows).
    /// Engine-free.
    /// </summary>
    public static class BondAssistRules
    {
        /// <summary>Vertical gap a ground companion can still reach a target across (flyers get more).</summary>
        public const float WalkerReach = 3.5f;
        public const float FlyerReach = 9f;

        /// <summary>
        /// The press decision. A locked or aimed hostile asks for the combat assist (even when it then fails a check);
        /// aiming at a non-hostile person refuses; with no target and a fight on, the player is told to aim or lock;
        /// otherwise it is the exploration assist.
        /// </summary>
        public static AssistIntent Decide(bool hasHostileTarget, bool aimingAtNeutral, bool fightNearby, out AssistRefusal refusal)
        {
            refusal = AssistRefusal.None;
            if (hasHostileTarget) return AssistIntent.Combat;
            if (aimingAtNeutral)
            {
                refusal = AssistRefusal.Neutral;
                return AssistIntent.Refuse;
            }
            if (fightNearby)
            {
                refusal = AssistRefusal.NoTarget;
                return AssistIntent.Refuse;
            }
            return AssistIntent.Explore;
        }

        /// <summary>Checks a combat target: range, line of sight and the reachable layer.</summary>
        public static AssistRefusal ValidateTarget(float distance, bool lineOfSight, bool sameLayer, float verticalGap, bool flyer)
        {
            if (distance > BondRules.AssistRange) return AssistRefusal.TooFar;
            if (!sameLayer || Mathf.Abs(verticalGap) > (flyer ? FlyerReach : WalkerReach)) return AssistRefusal.Unreachable;
            if (!lineOfSight) return AssistRefusal.Occluded;
            return AssistRefusal.None;
        }

        /// <summary>The companion's own state allows the assist (bond rank for combat, not downed, resting or busy).</summary>
        public static AssistRefusal ValidateCompanion(bool hasCompanion, int rank, int state, bool combat, bool busy, float cooldownLeft, bool fused, bool inVehicle)
        {
            if (!hasCompanion) return AssistRefusal.NoCompanion;
            if (inVehicle) return AssistRefusal.InVehicle;
            if (fused || state == (int)CompanionState.Fused) return AssistRefusal.Fused;
            if (state == (int)CompanionState.Downed) return AssistRefusal.Downed;
            if (state == (int)CompanionState.Resting) return AssistRefusal.Resting;
            if (state == (int)CompanionState.Rescue) return AssistRefusal.Rescue;
            if (combat && !BondRules.CanCombatAssist(rank)) return AssistRefusal.NotBonded;
            if (busy) return AssistRefusal.Busy;
            if (cooldownLeft > 0f) return AssistRefusal.Cooldown;
            return AssistRefusal.None;
        }

        /// <summary>The line shown for a refusal (name is the companion's name).</summary>
        public static string RefusalText(AssistRefusal r, string name, float cooldownLeft)
        {
            string n = string.IsNullOrEmpty(name) ? "Your companion" : name;
            switch (r)
            {
                case AssistRefusal.NoCompanion: return "You have no bonded companion yet.";
                case AssistRefusal.NotBonded: return n + " does not trust you enough to fight yet. Care for it at a care station first.";
                case AssistRefusal.Downed: return n + " is down and resting. It recovers by itself, or at once at a care station.";
                case AssistRefusal.Resting: return n + " is resting at home. Summon it from the BONDS page.";
                case AssistRefusal.Busy: return n + " is busy with the last command.";
                case AssistRefusal.Cooldown: return n + " needs " + Mathf.CeilToInt(Mathf.Max(0f, cooldownLeft)) + " s before that again.";
                case AssistRefusal.NoTarget: return "Aim at or lock onto a hostile for " + n + "'s assist.";
                case AssistRefusal.Neutral: return n + " will not attack someone who is not hostile.";
                case AssistRefusal.TooFar: return "Too far: " + n + " helps against foes within " + Mathf.RoundToInt(BondRules.AssistRange) + " m.";
                case AssistRefusal.Occluded: return n + " has no clear line to that target.";
                case AssistRefusal.Unreachable: return n + " cannot reach that target from here.";
                case AssistRefusal.InVehicle: return "Step off the vehicle to command " + n + ".";
                case AssistRefusal.Fused: return n + " is fused with your chassis.";
                case AssistRefusal.Rescue: return "Finish the rescue first.";
                case AssistRefusal.Waiting: return n + " is waiting where you left it. Tap N to call it.";
                default: return "";
            }
        }

        // ================================================================== the brace

        /// <summary>True when the attacker lies inside the brace cone: toAttacker and braceDir are flat directions.</summary>
        public static bool InCone(Vector3 braceDir, Vector3 toAttacker, float halfAngle)
        {
            Vector3 a = new Vector3(braceDir.x, 0f, braceDir.z);
            Vector3 b = new Vector3(toAttacker.x, 0f, toAttacker.z);
            if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) return false;
            return Vector3.Angle(a, b) <= halfAngle;
        }

        /// <summary>True for damage the shell can take: bullets, blows, blasts, powers and fire with a source.</summary>
        public static bool Braceable(DamageType type, bool unblockable, bool hasSource)
        {
            if (unblockable) return false;
            switch (type)
            {
                case DamageType.Bullet:
                case DamageType.Melee:
                case DamageType.Explosion:
                case DamageType.Power:
                    return true;
                case DamageType.Fire:
                    return hasSource;
                default:
                    return false;
            }
        }

        /// <summary>
        /// How much of a hit the brace takes. hitTravel is the hit's direction of travel (attacker toward the player).
        /// Capacity drops by the absorbed amount; nothing is absorbed outside the cone, when the player is out of cover
        /// range or when the shell is spent. Returns the absorbed amount (the caller subtracts it from the hit).
        /// </summary>
        public static float Absorb(Vector3 braceDir, Vector3 hitTravel, float amount, BraceStats s, ref float capacityLeft, bool braceable, float playerToWarden)
        {
            if (!braceable || amount <= 0f || capacityLeft <= 0f || playerToWarden > s.CoverRange) return 0f;
            if (!InCone(braceDir, -hitTravel, s.HalfAngle)) return 0f;
            float take = Mathf.Min(amount * Mathf.Clamp01(s.Absorb), capacityLeft);
            capacityLeft -= take;
            return take;
        }

        /// <summary>Kiln Heart: the share of sourceless hazard heat the warden shades while braced and close.</summary>
        public static float HeatShade(DamageType type, bool hasSource, float amount, BraceStats s, float playerToWarden)
        {
            if (type != DamageType.Fire || hasSource || s.HeatShade <= 0f || amount <= 0f || playerToWarden > s.CoverRange) return 0f;
            return amount * Mathf.Clamp01(s.HeatShade);
        }
    }
}
