using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Underheart and Bonds 04E campaign support on the existing quest book: persistent campaign choices, safe phase
    /// checkpoints for long chapters (a restarted quest resumes at its last committed checkpoint phase, never repaying
    /// earlier rewards) and the campaign block of the save. FOUNDATION STUB: the CAMPAIGN work stream replaces the
    /// bodies and keeps every signature.
    /// </summary>
    public sealed partial class QuestBook
    {
        /// <summary>A persistent campaign decision ("" when not made). Keys and values in QuestIds04E.</summary>
        public string Choice(string key) { return ""; }

        /// <summary>Records a campaign decision (the first answer sticks unless overwrite is true).</summary>
        public void SetChoice(string key, string value, bool overwrite) { }

        /// <summary>True while the running quest sits at a committed checkpoint and may be autosaved safely.</summary>
        public bool CheckpointSafeNow { get { return false; } }

        /// <summary>True for Underheart campaign content (main or side quests).</summary>
        public static bool IsCampaign(QuestDef q) { return q != null && q.Id != null && q.Id.StartsWith("uh05_", StringComparison.Ordinal); }

        /// <summary>Called by Capture: writes the campaign block.</summary>
        private void CaptureCampaign04E(SaveData d) { }

        /// <summary>Called by Restore and ResetAll (d is null on a new game).</summary>
        private void RestoreCampaign04E(SaveData d) { }
    }

    /// <summary>The campaign's durable block (SaveData.Campaign04E). Append fields only.</summary>
    [Serializable]
    public sealed class CampaignSave
    {
        public int Version = 1;
        /// <summary>Committed checkpoints as "questId:phaseId".</summary>
        public List<string> Checkpoints = new List<string>();
        /// <summary>Decisions as "key=value".</summary>
        public List<string> Choices = new List<string>();
    }

    /// <summary>Reset hub for every static the CAMPAIGN work stream adds.</summary>
    public static class CampaignStatics04E
    {
        public static void ResetStatics() { }
    }
}
