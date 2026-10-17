using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// The authored campaign facts (CAMPAIGN stream fields) wired into QuestMeta04E. Every hook only replaces the derived
    /// value when the authored field is filled in, so legacy quests and quests that leave a field empty keep the derived
    /// value. Region "surface" means a surface quest ("" here).
    /// </summary>
    public static partial class QuestMeta04E
    {
        static partial void AuthoredKind(QuestDef q, ref string kind)
        {
            if (q != null && !string.IsNullOrEmpty(q.Kind)) kind = q.Kind;
        }

        static partial void AuthoredAct(QuestDef q, ref int act)
        {
            if (q != null && q.Act > 0) act = q.Act;
        }

        static partial void AuthoredDifficulty(QuestDef q, ref int difficulty)
        {
            if (q != null && q.Difficulty > 0) difficulty = q.Difficulty;
        }

        static partial void AuthoredRegion(QuestDef q, ref string region)
        {
            if (q == null || string.IsNullOrEmpty(q.Region)) return;
            region = q.Region == "surface" ? "" : q.Region;
        }

        static partial void AuthoredFloor(QuestDef q, ref string floor)
        {
            if (q != null && !string.IsNullOrEmpty(q.Floor)) floor = q.Floor;
        }

        static partial void AuthoredEntrance(QuestDef q, ref string entrance)
        {
            if (q != null && !string.IsNullOrEmpty(q.Entrance)) entrance = q.Entrance;
        }

        static partial void AuthoredClaims(QuestDef q, List<string> into, ref bool authored)
        {
            if (q == null || q.Claims == null || q.Claims.Count == 0) return;
            foreach (string id in q.Claims)
                if (!string.IsNullOrEmpty(id) && !into.Contains(id)) into.Add(id);
            authored = true;
        }

        static partial void AuthoredCanResume(QuestDef q, ref bool canResume)
        {
            canResume = Game.Quests != null && Game.Quests.CanResume(q);
        }

        static partial void AuthoredResume(QuestDef q, ref bool started)
        {
            started = Game.Quests != null && Game.Quests.ResumeQuest(q);
        }

        static partial void AuthoredResumeSupported(ref bool supported)
        {
            supported = true;
        }
    }
}
