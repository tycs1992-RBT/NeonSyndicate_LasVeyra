namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, H8S presence: a story identity normally steps out of sight while any non-campaign mission runs
    /// (GateReason), because story missions stage that same person themselves. While the running quest is the person's
    /// own venue quest (CastPresenceRulesH8S: they present it at their venue or it hands in there in their words, and it
    /// stages no other copy of them), they stay at their post, so the Hearth Witness and the other venue surveys hand in
    /// to the real person and the TURN IN check has someone to stand over. Story missions, activities, contracts and
    /// every other quest keep the original hiding.
    /// </summary>
    public sealed partial class CampaignDirector
    {
        /// <summary>True while the running quest is this person's own venue quest and stages no copy of them.</summary>
        private static bool KeepsPostForOwnQuestH8S(CastSlot s)
        {
            if (s == null || s.Def == null || Game.Missions == null || Game.Quests == null) return false;
            MissionContext active = Game.Missions.Active;
            QuestRun run = Game.Quests.Running;
            if (active == null || run == null || run.C != active) return false;
            bool story = active.Def != null && active.Def.Story;
            return CastPresenceRulesH8S.KeepsPostDuringQuest(run.Quest, run.Contract, story, s.Def.Venue, s.Def.Name, s.Def.Id);
        }
    }
}
