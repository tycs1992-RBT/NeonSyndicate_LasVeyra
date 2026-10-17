using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Campaign presentation facts about a quest (kind, act, suggested difficulty, region, floor, entrance, reward
    /// claims, checkpoint resume), read without reflection. Every value is first derived from data that already exists
    /// in this build (the QuestIds04E tables, the step place tokens and step types), then the optional authored hooks
    /// below may replace it.
    ///
    /// The authored campaign fields (QuestDef.Kind, Campaign, Act, Difficulty, Region, Floor, Entrance, Claims and
    /// QuestBook.CanResume and ResumeQuest) are wired in Gameplay/Guidance04E/QuestMeta04E.Authored.cs and replace the
    /// derived values when present. Engine-free.
    /// </summary>
    public static partial class QuestMeta04E
    {
        // ------------------------------------------------------------------ optional authored overrides (no body yet)
        static partial void AuthoredKind(QuestDef q, ref string kind);
        static partial void AuthoredAct(QuestDef q, ref int act);
        static partial void AuthoredDifficulty(QuestDef q, ref int difficulty);
        static partial void AuthoredRegion(QuestDef q, ref string region);
        static partial void AuthoredFloor(QuestDef q, ref string floor);
        static partial void AuthoredEntrance(QuestDef q, ref string entrance);
        static partial void AuthoredClaims(QuestDef q, List<string> into, ref bool authored);
        static partial void AuthoredCanResume(QuestDef q, ref bool canResume);
        static partial void AuthoredResume(QuestDef q, ref bool started);
        static partial void AuthoredResumeSupported(ref bool supported);

        public const string Main = "Main", Side = "Side", Contract = "Contract";

        private static readonly string[] CombatSteps = { "Kill", "Boss", "Survive", "Defend", "Hunt", "CloseRift", "Skirmish", "UBoss", "Mechanism" };

        /// <summary>True for an Underheart campaign quest (main or side), the same rule as QuestBook.IsCampaign.</summary>
        public static bool IsCampaign(QuestDef q)
        {
            return q != null && q.Id != null && q.Id.StartsWith("uh05_", StringComparison.Ordinal);
        }

        /// <summary>"Main", "Side" or "" for a legacy quest.</summary>
        public static string Kind(QuestDef q)
        {
            if (q == null) return "";
            string kind = QuestIds04E.IsMain(q.Id) ? Main : QuestIds04E.IsSide(q.Id) ? Side : (IsCampaign(q) ? Side : "");
            AuthoredKind(q, ref kind);
            return kind ?? "";
        }

        public static bool IsMain(QuestDef q) { return Kind(q) == Main; }

        /// <summary>Act 1 to 4 for main quests (four quests per act), the act of the main quest a side arc follows, 0 for legacy.</summary>
        public static int Act(QuestDef q)
        {
            if (q == null) return 0;
            int act = 0;
            int main = Array.IndexOf(QuestIds04E.Main, q.Id);
            if (main >= 0) act = main / 4 + 1;
            else
            {
                int side = Array.IndexOf(QuestIds04E.Side, q.Id);
                if (side >= 0) act = Mathf.Clamp(side / 3 + 1, 1, 4);
            }
            AuthoredAct(q, ref act);
            return act;
        }

        /// <summary>Suggested difficulty 1 to 5: the act, one more when the quest holds a boss or a skirmish.</summary>
        public static int Difficulty(QuestDef q)
        {
            if (q == null) return 1;
            int d = Mathf.Max(1, Act(q));
            if (HasStep(q, "UBoss") || HasStep(q, "Boss")) d++;
            int authored = 0;
            AuthoredDifficulty(q, ref authored);
            return Mathf.Clamp(authored > 0 ? authored : d, 1, 5);
        }

        public static string DifficultyName(int d)
        {
            switch (Mathf.Clamp(d, 1, 5))
            {
                case 1: return "Gentle";
                case 2: return "Moderate";
                case 3: return "Demanding";
                case 4: return "Hard";
                default: return "Severe";
            }
        }

        /// <summary>True when any step fights (kill, boss, survive, defend, skirmish, boss arena, mechanism defence).</summary>
        public static bool HasCombat(QuestDef q)
        {
            for (int i = 0; i < CombatSteps.Length; i++) if (HasStep(q, CombatSteps[i])) return true;
            return false;
        }

        public static bool HasStep(QuestDef q, string type)
        {
            if (q == null || q.Phases == null) return false;
            foreach (QuestPhase p in q.Phases)
            {
                if (p == null || p.Steps == null) continue;
                foreach (QuestStep s in p.Steps) if (s != null && s.Type == type) return true;
            }
            return false;
        }

        /// <summary>The first place token of the quest that is not its own giver: "Venue:x.y", "Under:id" or "" when none.</summary>
        public static string FirstDestination(QuestDef q)
        {
            if (q == null || q.Phases == null) return "";
            foreach (QuestPhase p in q.Phases)
            {
                if (p == null || p.Steps == null) continue;
                foreach (QuestStep s in p.Steps)
                {
                    if (s == null) continue;
                    if (IsDestination(s.At, q.Giver)) return s.At;
                    if (!string.IsNullOrEmpty(s.VenueId)) return "Venue:" + s.VenueId + "." + (string.IsNullOrEmpty(s.ActionId) ? "service" : s.ActionId);
                    if (IsDestination(s.To, q.Giver)) return s.To;
                }
            }
            return "";
        }

        private static bool IsDestination(string at, string giver)
        {
            if (string.IsNullOrEmpty(at) || at == giver) return false;
            return at.StartsWith("Venue:", StringComparison.Ordinal) || at.StartsWith("Under:", StringComparison.Ordinal);
        }

        /// <summary>The Underheart region id the quest mostly takes place in ("" for a surface quest).</summary>
        public static string Region(QuestDef q)
        {
            string region = "";
            if (q != null && q.Phases != null)
            {
                foreach (QuestPhase p in q.Phases)
                {
                    if (p == null || p.Steps == null) continue;
                    foreach (QuestStep s in p.Steps)
                    {
                        if (s == null) continue;
                        string r = RegionOfToken(s.At);
                        if (r.Length == 0) r = RegionOfToken(s.To);
                        if (r.Length > 0 && r != UnderheartIds.ServiceLevel) { region = r; break; }
                        if (r.Length > 0 && region.Length == 0) region = r;
                    }
                    if (region.Length > 0 && region != UnderheartIds.ServiceLevel) break;
                }
            }
            AuthoredRegion(q, ref region);
            return region ?? "";
        }

        public static string RegionOfToken(string token)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Under:", StringComparison.Ordinal)) return "";
            return UnderheartIds.RegionOfAnchor(token.Substring(6));
        }

        /// <summary>"Venue:dispatch.receiver_a" gives "dispatch"; anything else gives "".</summary>
        public static string VenueOfToken(string token)
        {
            if (string.IsNullOrEmpty(token) || !token.StartsWith("Venue:", StringComparison.Ordinal)) return "";
            string rest = token.Substring(6);
            int dot = rest.IndexOf('.');
            return dot > 0 ? rest.Substring(0, dot) : rest;
        }

        /// <summary>"Venue:dispatch.receiver_a" gives "receiver_a"; anything else gives "".</summary>
        public static string AnchorOfToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            if (token.StartsWith("Under:", StringComparison.Ordinal)) return token.Substring(6);
            if (!token.StartsWith("Venue:", StringComparison.Ordinal)) return "";
            string rest = token.Substring(6);
            int dot = rest.IndexOf('.');
            return dot > 0 && dot < rest.Length - 1 ? rest.Substring(dot + 1) : "";
        }

        /// <summary>A readable floor for the first destination: "ground floor", "upper floor" or "18 m below the street".</summary>
        public static string Floor(QuestDef q, Func<string, float> anchorHeight)
        {
            string floor = "";
            string dest = FirstDestination(q);
            if (dest.StartsWith("Under:", StringComparison.Ordinal))
            {
                string region = UnderheartIds.RegionOfAnchor(dest.Substring(6));
                floor = UnderheartIds.RegionTitle(region) + " (underground)";
            }
            else if (dest.StartsWith("Venue:", StringComparison.Ordinal))
            {
                float h = anchorHeight != null ? anchorHeight(dest) : 0f;
                floor = h > 2f ? "upper floor" : "ground floor";
            }
            AuthoredFloor(q, ref floor);
            return floor ?? "";
        }

        /// <summary>The entrance the route leads to first: the venue's front door, or the Pressure House stair for the Underheart.</summary>
        public static string Entrance(QuestDef q, Func<string, string> venueTitle)
        {
            string entrance = "";
            string dest = FirstDestination(q);
            if (dest.StartsWith("Under:", StringComparison.Ordinal)) entrance = "The Pressure House stair (down)";
            else if (dest.StartsWith("Venue:", StringComparison.Ordinal))
            {
                string v = VenueOfToken(dest);
                string title = venueTitle != null ? venueTitle(v) : v;
                entrance = (string.IsNullOrEmpty(title) ? v : title) + " front door";
            }
            AuthoredEntrance(q, ref entrance);
            return entrance ?? "";
        }

        /// <summary>The durable reward ids the quest claims (for display), authored or scanned from its Claim steps.</summary>
        public static void Claims(QuestDef q, List<string> into)
        {
            into.Clear();
            if (q == null) return;
            bool authored = false;
            AuthoredClaims(q, into, ref authored);
            if (authored) return;
            // Without the authored list, Claim steps name their reward in Text or Name (validated by the CAMPAIGN stream).
            if (q.Phases == null) return;
            foreach (QuestPhase p in q.Phases)
            {
                if (p == null || p.Steps == null) continue;
                foreach (QuestStep s in p.Steps)
                {
                    if (s == null || s.Type != "Claim") continue;
                    string id = RewardIds04E.Has(s.Text) ? s.Text : (RewardIds04E.Has(s.Name) ? s.Name : "");
                    if (id.Length > 0 && !into.Contains(id)) into.Add(id);
                }
            }
        }

        /// <summary>True when the quest has a committed checkpoint it can resume from (the CAMPAIGN stream decides).</summary>
        public static bool CanResume(QuestDef q)
        {
            bool can = false;
            if (q != null) AuthoredCanResume(q, ref can);
            return can;
        }

        /// <summary>Resumes a checkpointed quest through the CAMPAIGN stream's QuestBook.ResumeQuest. False when unavailable.</summary>
        public static bool Resume(QuestDef q)
        {
            bool started = false;
            if (q != null) AuthoredResume(q, ref started);
            return started;
        }

        /// <summary>True when the authored hooks are wired (the integrator added QuestMeta04E.Authored.cs).</summary>
        public static bool ResumeSupported
        {
            get
            {
                bool supported = false;
                AuthoredResumeSupported(ref supported);
                return supported;
            }
        }

        /// <summary>The cast id of a quest whose Giver is "Cast:id", else "".</summary>
        public static string CastGiver(QuestDef q)
        {
            return q == null ? "" : CastRoster04E.IdFromToken(q.Giver);
        }
    }
}
