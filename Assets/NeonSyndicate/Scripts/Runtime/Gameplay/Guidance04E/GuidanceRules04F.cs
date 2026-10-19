using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>Small engine-free 04F rules. Existing campaign ids and prerequisites remain authoritative.</summary>
    public static class GuidanceRules04F
    {
        /// <summary>Pure rules retain no session state; the common reset hub calls this explicitly.</summary>
        public static void ResetStatics() { }

        public const string RescueQuest = "uh05_03_bond";
        public const string ReserveJob = "uh05_04f_01_reserve";
        public const string PressureJob = "uh05_04f_02_pressure";
        public const string TemperJob = "uh05_04f_03_temper";

        /// <summary>Finds the first unfinished prerequisite by following the actual quest data, then the rescue itself.
        /// A missing file or a cyclic prerequisite fails honestly rather than inventing a rescue or granting a bond.</summary>
        public static string CompanionLead(Func<string, QuestDef> find, Func<string, bool> done)
        {
            if (find == null || done == null || done(RescueQuest)) return "";
            return FirstUnfinished(RescueQuest, find, done, new HashSet<string>());
        }

        private static string FirstUnfinished(string id, Func<string, QuestDef> find, Func<string, bool> done, HashSet<string> visiting)
        {
            if (done(id)) return "";
            QuestDef q = find(id);
            if (q == null || !visiting.Add(id)) return "";
            if (q.Requires != null && q.Requires.Quests != null)
            {
                foreach (string need in q.Requires.Quests)
                {
                    if (done(need)) continue;
                    string first = FirstUnfinished(need, find, done, visiting);
                    visiting.Remove(id);
                    return first;
                }
            }
            visiting.Remove(id);
            return q.Id;
        }

        /// <summary>Four independent choices, no automatic acceptance and no companion reward.</summary>
        public static List<NextOption04E> SessionChoices(NextOption04E? story, NextOption04E? contract,
            NextOption04E? recharge, NextOption04E? companion)
        {
            List<NextOption04E> result = new List<NextOption04E>(4);
            if (story.HasValue) result.Add(story.Value);
            if (contract.HasValue) result.Add(contract.Value);
            if (recharge.HasValue) result.Add(recharge.Value);
            if (companion.HasValue) result.Add(companion.Value);
            return result;
        }

        /// <summary>A deliberately tracked eligible side job wins over another offer at the same person.
        /// Accepted or running quests are handled first by CampaignDirector; tracking never bypasses a lock.</summary>
        public static QuestDef TrackedOffer(IList<QuestDef> offered, string trackedId,
            Func<string, bool> done, Func<QuestDef, bool> available)
        {
            if (offered == null || string.IsNullOrEmpty(trackedId) || done == null || available == null) return null;
            foreach (QuestDef q in offered)
                if (q != null && q.Id == trackedId && !done(q.Id) && available(q)) return q;
            return null;
        }

        public static bool IsInteriorContract(string id)
        {
            return id!=null && id.StartsWith("uh05_04i_",StringComparison.Ordinal) || id == VeilMemoryRules04L.Quest || id == VanguardRules04K.Quest || id == "uh05_04h_01_quiet_circuit" || id == "uh05_04g_01_courier" || id == ReserveJob || id == PressureJob || id == TemperJob;
        }
    }
}
