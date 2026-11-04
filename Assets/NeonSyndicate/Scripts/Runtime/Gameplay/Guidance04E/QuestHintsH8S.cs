namespace NeonSyndicate
{
    /// <summary>
    /// Extra guidance for quest phases, keyed by quest id and phase id (H8S). The quest files stay exactly as shipped
    /// (saved progress remembers their steps); these lines only add a practical hint that restates what the quest's own
    /// objectives already say about the route, so a hint can never send the player somewhere the quest does not. The
    /// tracker shows a phase's hint once as a toast when the phase begins and the journal shows it beside the objective.
    /// Engine-free and tested against the shipped quest's phase ids.
    /// </summary>
    public static class QuestHintsH8S
    {
        /// <summary>The Hearth Witness, Dante Rook's revision 9 and 10 survey at the Forge.</summary>
        public const string HearthWitness = "fg_04oh_h3_hearth_witness";

        /// <summary>The phase ids of the Hearth Witness, in order, as the shipped quest names them.</summary>
        public static readonly string[] HearthWitnessPhases = { "briefing", "hearth", "witness", "spring", "return" };

        /// <summary>The hint for a quest phase, or "" when there is none.</summary>
        public static string For(string questId, string phaseId)
        {
            if (questId != HearthWitness || string.IsNullOrEmpty(phaseId)) return "";
            switch (phaseId)
            {
                case "briefing":
                    return "Hear Dante out at the Forge; the survey starts once he has explained the three readings.";
                case "hearth":
                    return "Climb the home-side stair from the court to its supported reading porch, then read the crystal there.";
                case "witness":
                    return "Follow the Oath passage to the witness chamber. The two disturbed mites come one at a time; settle each before the next.";
                case "spring":
                    return "Cross into Azure Shardwood by the regional route, settle the lone predator at the shrine, then take the grounded lower cavern route to the spring.";
                case "return":
                    return "Walk back to the Forge entrance with the survey; Dante speaks when you arrive.";
                default:
                    return "";
            }
        }

        /// <summary>True when any hint exists for the quest (the journal reserves a line for it).</summary>
        public static bool Has(string questId)
        {
            return questId == HearthWitness;
        }
    }
}
