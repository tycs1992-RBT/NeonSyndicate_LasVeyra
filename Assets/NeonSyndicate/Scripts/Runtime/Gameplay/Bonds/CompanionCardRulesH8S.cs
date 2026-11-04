using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The one word the companion card shows for what the companion is doing (H8S).</summary>
    public enum CompanionModeH8S
    {
        Following = 0,
        Waiting = 1,
        Assisting = 2,
        Downed = 3,
        Resting = 4,
        Fused = 5,
        Riding = 6,
        Arriving = 7,
        Sheltering = 8,
        Separated = 9
    }

    /// <summary>
    /// The companion card's decisions as plain maths: the mode chip (word and colour) from the record state, the
    /// cooldown fill of an assist, the low-health warning, the command pulse after a key press and the readable health
    /// numbers. Engine-free and tested in CastCompanionsH8STests; the card itself is BondSystem.CardH8S.
    /// </summary>
    public static class CompanionCardRulesH8S
    {
        /// <summary>How long the mode chip glows after a command (seconds).</summary>
        public const float PulseSeconds = 0.7f;

        /// <summary>The mode from the saved state plus the live facts (riding along, just separated, no body yet, sheltering).</summary>
        public static CompanionModeH8S ModeOf(int state, bool riding, bool separated, bool hasBody, bool sheltering)
        {
            switch ((CompanionState)state)
            {
                case CompanionState.Downed: return CompanionModeH8S.Downed;
                case CompanionState.Resting: return CompanionModeH8S.Resting;
                case CompanionState.Fused: return CompanionModeH8S.Fused;
                case CompanionState.Waiting: return CompanionModeH8S.Waiting;
                case CompanionState.Assisting: return CompanionModeH8S.Assisting;
            }
            if (separated) return CompanionModeH8S.Separated;
            if (riding) return CompanionModeH8S.Riding;
            if (!hasBody) return CompanionModeH8S.Arriving;
            return sheltering ? CompanionModeH8S.Sheltering : CompanionModeH8S.Following;
        }

        /// <summary>The chip's word.</summary>
        public static string Label(CompanionModeH8S m)
        {
            switch (m)
            {
                case CompanionModeH8S.Waiting: return "WAITING";
                case CompanionModeH8S.Assisting: return "ASSISTING";
                case CompanionModeH8S.Downed: return "DOWN";
                case CompanionModeH8S.Resting: return "RESTING";
                case CompanionModeH8S.Fused: return "FUSED";
                case CompanionModeH8S.Riding: return "RIDING";
                case CompanionModeH8S.Arriving: return "ON ITS WAY";
                case CompanionModeH8S.Sheltering: return "KEEPING BACK";
                case CompanionModeH8S.Separated: return "BESIDE BIKE";
                default: return "FOLLOWING";
            }
        }

        /// <summary>The chip's colour (each mode also has its own word, so colour is never the only cue).</summary>
        public static Color ColorOf(CompanionModeH8S m)
        {
            switch (m)
            {
                case CompanionModeH8S.Waiting: return new Color(0.95f, 0.78f, 0.35f);
                case CompanionModeH8S.Assisting: return new Color(0.45f, 0.9f, 1f);
                case CompanionModeH8S.Downed: return new Color(1f, 0.42f, 0.36f);
                case CompanionModeH8S.Resting: return new Color(0.62f, 0.64f, 0.7f);
                case CompanionModeH8S.Fused: return new Color(0.8f, 0.6f, 1f);
                case CompanionModeH8S.Sheltering: return new Color(1f, 0.6f, 0.35f);
                case CompanionModeH8S.Riding:
                case CompanionModeH8S.Separated:
                case CompanionModeH8S.Arriving: return new Color(0.75f, 0.82f, 0.9f);
                default: return new Color(0.55f, 0.95f, 0.6f);
            }
        }

        /// <summary>How full an assist's ready bar is: 1 when ready, rising from 0 as the cooldown runs out.</summary>
        public static float CooldownFill(float left, float total)
        {
            if (!(left > 0f)) return 1f;
            if (!(total > 0f)) return 0f;
            return Mathf.Clamp01(1f - left / total);
        }

        /// <summary>True when the health share counts as low (the bar turns red and the card warns).</summary>
        public static bool LowHealth(float health01)
        {
            return health01 > 0f && health01 < CompanionSpacingRulesH8S.LowHealth;
        }

        /// <summary>The chip's glow after a command: fades over PulseSeconds; with reduced effects a short steady glow instead.</summary>
        public static float PulseAlpha(float sinceCommand, bool reducedEffects)
        {
            if (float.IsNaN(sinceCommand) || sinceCommand < 0f || sinceCommand >= PulseSeconds) return 0f;
            if (reducedEffects) return 0.45f;
            return 1f - sinceCommand / PulseSeconds;
        }

        /// <summary>Whole health points shown beside the bar, never below 0 or above the maximum.</summary>
        public static int HealthPoints(float health01, float maxHealth)
        {
            float max = Mathf.Max(1f, maxHealth);
            return Mathf.Clamp(Mathf.CeilToInt(Mathf.Clamp01(health01) * max - 0.001f), 0, Mathf.CeilToInt(max));
        }
    }
}
