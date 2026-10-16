using System;

namespace NeonSyndicate
{
    /// <summary>Authored action references shared by quest validation and the inhabited rooms. No progress lives here.</summary>
    public static class QuietSignal04C
    {
        public static readonly string[] QuestIds =
        {
            "quiet_signal_01_familiar_door", "quiet_signal_02_supply_and_repair", "quiet_signal_03_read_the_city",
            "quiet_signal_04_missing_piece", "quiet_signal_05_signal_restored"
        };

        public static readonly string[] Venues = { "lofts", "clinic", "forge", "inn", "archive", "observatory", "garage", "depot" };
        private static readonly string[] Actions =
        {
            "lofts:request", "lofts:instrument_case", "clinic:supply_manifest", "clinic:inspect_supplies", "forge:calibrate",
            "archive:record_origins", "archive:record_route", "archive:record_frequency", "inn:interview",
            "depot:retrieve_safely", "depot:clear_threat", "depot:recover_relay",
            "observatory:align_lens", "observatory:tune_frequency", "observatory:focus_receiver", "lofts:install_display"
        };
        private static readonly string[] Anchors =
        {
            "staff", "activity", "record1", "activity", "activity", "record1", "record2", "record3", "visitor1",
            "record1", "activity", "retrieve", "instrument1", "instrument2", "instrument3", "activity"
        };
        private static readonly int[] Stages = { 0, 0, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4 };

        public static void ResetStatics() { }
        public static bool IsChapterQuest(string id) { return Array.IndexOf(QuestIds, id) >= 0; }
        public static bool ValidVenue(string id) { return Array.IndexOf(Venues, id) >= 0; }
        public static string ActionKey(string venue, string action) { return (venue ?? "") + ":" + (action ?? ""); }
        public static bool ValidAction(string venue, string action) { return Array.IndexOf(Actions, ActionKey(venue, action)) >= 0; }
        public static string AnchorFor(string venue, string action)
        {
            int i = Array.IndexOf(Actions, ActionKey(venue, action));
            return i >= 0 ? Anchors[i] : "";
        }
        public static string ActionQuestId(string venue, string action)
        {
            int i = Array.IndexOf(Actions, ActionKey(venue, action));
            return i >= 0 ? QuestIds[Stages[i]] : "";
        }
        /// <summary>Two authored spawn points, at most two bodies. Difficulty changes their strength, never adds waves.</summary>
        public static int DepotThreatCount(int difficulty) { return difficulty <= 0 ? 1 : 2; }

        /// <summary>A scene disappearance never counts as a defeat. The caller records health death events explicitly.</summary>
        public static bool DepotCompleted(int expected, int spawned, int deathEvents, bool invalidBody)
        {
            return expected > 0 && spawned == expected && deathEvents == expected && !invalidBody;
        }

        public static bool MayUseAction(string questId, string currentVenue, string currentAction, string venue, string action, bool occupied, bool safe)
        {
            return safe && occupied && ValidAction(venue, action) && questId == ActionQuestId(venue, action)
                && currentVenue == venue && currentAction == action;
        }
    }
}
