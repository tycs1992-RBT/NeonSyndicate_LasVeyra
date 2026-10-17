namespace NeonSyndicate
{
    /// <summary>
    /// How many companion bodies are live right now, so the creature director counts them against its body budget
    /// (CreatureDirector.CountAmbient adds BodyCost): the active companion's visible body, a rescue body and each
    /// resting companion shown at Bondkeeper's Refuge cost one ambient body each. A hidden body (fused, riding along,
    /// retreated to rest) costs nothing because it neither draws nor moves. Companions can never slip past the creature
    /// caps through an untracked body.
    /// </summary>
    public static class CompanionBudget
    {
        /// <summary>Visible active companion bodies (0 or 1).</summary>
        public static int Bodies;
        /// <summary>Rescue bodies waiting at a rescue site (0 or 1).</summary>
        public static int Rescues;
        /// <summary>Resting companions shown at Bondkeeper's Refuge while the player is near (0 to 2).</summary>
        public static int Homes;

        /// <summary>The ambient body budget the companions use (one per live body).</summary>
        public static float BodyCost { get { return Bodies + Rescues + Homes; } }

        public static void ResetStatics()
        {
            Bodies = 0;
            Rescues = 0;
            Homes = 0;
        }
    }
}
