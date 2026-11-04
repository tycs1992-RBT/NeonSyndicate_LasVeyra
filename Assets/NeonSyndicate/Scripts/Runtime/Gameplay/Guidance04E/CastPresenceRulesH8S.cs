namespace NeonSyndicate
{
    /// <summary>
    /// Engine-free rule (H8S) for when an original story character stays at their post while a non-campaign mission runs.
    /// The 04E rule hides story identities (Dante, Iris, Mara, Ion, Jax, June) during every non-campaign mission because
    /// story missions stage the same person themselves (MissionContext.StageStoryContacts04L and the scripted contacts in
    /// StoryMissions), which would make a second copy. This rule keeps exactly one exception: the running mission is a
    /// quest (not a story mission, activity or contract) that is the person's own venue quest, so it never stages them
    /// anywhere else. Then the briefing, the hand-in lines and the TURN IN check all have the real person at the venue.
    /// </summary>
    public static class CastPresenceRulesH8S
    {
        /// <summary>
        /// True when the person keeps their post while this quest runs: it is a quest run (not a story mission, not a
        /// contract), it is their own venue quest, and it stages no other copy of them.
        /// </summary>
        public static bool KeepsPostDuringQuest(QuestDef running, bool contract, bool storyMission, string venue, string name, string castId)
        {
            if (running == null || contract || storyMission) return false;
            if (string.IsNullOrEmpty(venue) || string.IsNullOrEmpty(name)) return false;
            return OwnVenueQuest(running, venue, name) && !StagesElsewhere(running, name, castId);
        }

        /// <summary>
        /// True when the quest belongs to this person's venue: they present it there (VenueGiver plus Presenter), or its
        /// last phase hands in at their venue in their own words (the walk back ends with their dialogue there).
        /// </summary>
        public static bool OwnVenueQuest(QuestDef q, string venue, string name)
        {
            if (q == null || string.IsNullOrEmpty(venue) || string.IsNullOrEmpty(name)) return false;
            if (VenueQuestRulesH8S.Presents(q, venue, name)) return true;
            if (q.Phases == null) return false;
            for (int i = 0; i < q.Phases.Count; i++)
            {
                QuestPhase ph = q.Phases[i];
                if (ph == null || ph.Steps == null) continue;
                bool last = i == q.Phases.Count - 1 || ph.Next == QuestDefaults.End;
                if (last && SpeaksIn(ph, name) && VenueQuestRulesH8S.HandIn(ph, true, 0, venue, name)) return true;
            }
            return false;
        }

        /// <summary>
        /// True when any step would put a second copy of this person in the world: a spawned person with their name, or
        /// with their own look (the quest looks "Dante", "Iris", "Mara" and "Ion" match the cast ids). A "Cast:" place is
        /// the real person at their post, so it is not a copy.
        /// </summary>
        public static bool StagesElsewhere(QuestDef q, string name, string castId)
        {
            if (q == null || q.Phases == null) return false;
            for (int i = 0; i < q.Phases.Count; i++)
            {
                QuestPhase ph = q.Phases[i];
                if (ph == null || ph.Steps == null) continue;
                for (int k = 0; k < ph.Steps.Count; k++)
                {
                    QuestStep s = ph.Steps[k];
                    if (s == null) continue;
                    if (!string.IsNullOrEmpty(name) && s.Name == name) return true;
                    if (!string.IsNullOrEmpty(castId) && !string.IsNullOrEmpty(s.Look) && string.Equals(s.Look, castId, System.StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }

        private static bool SpeaksIn(QuestPhase ph, string name)
        {
            for (int k = 0; k < ph.Steps.Count; k++)
            {
                QuestStep s = ph.Steps[k];
                if (s == null || s.Type != "Talk" || s.Lines == null) continue;
                for (int l = 0; l < s.Lines.Count; l++)
                    if (s.Lines[l] != null && s.Lines[l].Speaker == name) return true;
            }
            return false;
        }
    }
}
