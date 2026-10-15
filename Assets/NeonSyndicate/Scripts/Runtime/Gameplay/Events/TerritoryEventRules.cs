using System;

namespace NeonSyndicate
{
    /// <summary>Engine-free rules shared by territory event validation, runtime and EditMode tests.</summary>
    public static class TerritoryEventRules
    {
        public const float LeaveRange = 90f;
        public const float RetryCooldown = 30f;
        public const int MaximumCombatants = 4;

        public static bool ValidActivity(string kind, string activity)
        {
            return kind == "Fieldwork" ? activity == "Gather" || activity == "Aid" || activity == "Observe" || activity == "Resonance"
                : kind == "Skirmish" && (activity == "Defense" || activity == "Stabilize");
        }

        /// <summary>Story 2, Standard 3, Veteran 4 at most; Low always allows at most two.</summary>
        public static int CombatCount(int authored, int difficulty, bool low)
        {
            int cap = difficulty <= 0 ? 2 : difficulty == 1 ? 3 : MaximumCombatants;
            if (low) cap = Math.Min(cap, 2);
            return Math.Max(1, Math.Min(Math.Min(authored, MaximumCombatants), cap));
        }

        /// <summary>Wrong resonance notes restart the sequence; duplicate collection never advances progress.</summary>
        public static int ResonanceStep(int current, int selected, int total)
        {
            if (total < 1 || current >= total) return Math.Max(0, current);
            return selected == current ? current + 1 : selected == 0 ? 1 : 0;
        }

        /// <summary>Observation needs continuous proximity, view and line of sight. Losing any condition resets it.</summary>
        public static float Observe(float accumulated, float delta, bool visible, bool paused)
        {
            if (paused) return accumulated;
            if (!visible) return 0f;
            return Math.Max(0f, accumulated) + Math.Max(0f, delta);
        }

        public static string StopReason(bool dead, bool wanted, bool inner, float distance, float age, float duration)
        {
            if (dead) return "The territory activity ended when you fell.";
            if (wanted) return "The territory activity paused its work because of the pursuit. Return when clear.";
            if (inner) return "The territory activity ended when you crossed the Veil.";
            if (distance > LeaveRange) return "You left the activity area. Return to the terminal to try again.";
            if (age >= duration) return "The activity window closed. Return to the terminal to try again.";
            return null;
        }

        /// <summary>Owned relay wards shorten the post-combat hold by four seconds per tier, with an eight-second floor.</summary>
        public static float DefenseHold(float authored, int fortification)
        {
            return Math.Max(8f, authored - Math.Max(0, Math.Min(2, fortification)) * 4f);
        }

        /// <summary>At most twenty percent less assault health, no change to the player's own stats or original echoes.</summary>
        public static float DefenseHealthScale(int fortification)
        {
            return 1f - Math.Max(0, Math.Min(2, fortification)) * 0.1f;
        }

        public static float SavedCooldown(float remaining)
        {
            return float.IsNaN(remaining) || float.IsInfinity(remaining) ? 0f : Math.Max(0f, Math.Min(7200f, remaining));
        }

        /// <summary>No mutable static state; present for the project's domain-reload lifecycle contract.</summary>
        public static void ResetStatics() { }
    }
}
