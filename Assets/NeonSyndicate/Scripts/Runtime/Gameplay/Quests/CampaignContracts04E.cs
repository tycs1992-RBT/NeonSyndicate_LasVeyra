using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What the Lantern Refuge board knows about the player when it posts jobs (engine-free; filled by QuestBook).</summary>
    public sealed class CampaignBoardContext
    {
        /// <summary>Completed quest ids (regions open with the main quests, CampaignRules04E.RegionOpenedBy).</summary>
        public readonly HashSet<string> Completed = new HashSet<string>();
        /// <summary>The campaign is finished: the board posts the endgame tier (one more slot, harder, better paid).</summary>
        public bool Endgame;
    }

    /// <summary>
    /// The eight Underheart contract grammars (Salvage, EliteHunt, ValveDefense, CreatureRescue, LensCalibration,
    /// ShortCourier, SurveyorSearch, ExtractionTrial) turned into runnable jobs. Pure: the same serial always posts the same
    /// job, so the board is rebuilt exactly from the saved serials. Contracts pay bounded cash, Lumen and materials, never a
    /// unique or a boss entitlement.
    /// </summary>
    public static class CampaignContracts04E
    {
        public const string BoardPlace = "Under:refuge.map_board";
        public const string BoardName = "Lantern Refuge board";
        public const int Slots = 3;
        public const int EndgameSlots = 4;
        /// <summary>Active-play seconds after which every slot is reposted (no real-world timers).</summary>
        public const float RefreshSeconds = 1200f;

        /// <summary>True when a template is a campaign template (its Guild is a campaign id, not a guild faction).</summary>
        public static bool IsCampaignTemplate(ContractDef t, ICollection<string> campaignIds)
        {
            return t != null && campaignIds != null && campaignIds.Contains(t.Guild);
        }

        /// <summary>The template's destinations whose regions are listed and opened by the story so far, in file order.</summary>
        public static List<string> EligibleDestinations(ContractDef t, CampaignBoardContext ctx)
        {
            List<string> list = new List<string>();
            if (t == null || ctx == null) return list;
            foreach (string d in t.Destinations)
            {
                string region = CampaignRules04E.RegionOf(d);
                if (t.Regions == null || !t.Regions.Contains(region)) continue;
                string opener = CampaignRules04E.RegionOpenedBy(region);
                if (!string.IsNullOrEmpty(opener) && !ctx.Completed.Contains(opener)) continue;
                list.Add(d);
            }
            return list;
        }

        /// <summary>Null when the board may post the template now, else why not.</summary>
        public static string WhyNotEligible(ContractDef t, CampaignBoardContext ctx)
        {
            if (t == null || ctx == null) return "No template";
            if (!string.IsNullOrEmpty(t.After) && !ctx.Completed.Contains(t.After)) return "Opens after " + t.After;
            if (EligibleDestinations(t, ctx).Count == 0) return "No region open for it yet";
            return null;
        }

        /// <summary>Picks a template for a slot, weighted by Weight among the campaign's eligible templates.</summary>
        public static ContractDef PickTemplate(IList<ContractDef> all, string campaign, CampaignBoardContext ctx, int seed)
        {
            float total = 0f;
            foreach (ContractDef t in all) if (t != null && t.Guild == campaign && WhyNotEligible(t, ctx) == null) total += t.Weight > 0f ? t.Weight : 1f;
            if (total <= 0f) return null;
            GRand r = new GRand(seed);
            float pick = r.Value() * total;
            ContractDef last = null;
            foreach (ContractDef t in all)
            {
                if (t == null || t.Guild != campaign || WhyNotEligible(t, ctx) != null) continue;
                last = t;
                pick -= t.Weight > 0f ? t.Weight : 1f;
                if (pick < 0f) return t;
            }
            return last;
        }

        /// <summary>The seed a board slot uses for its template pick (deterministic per serial).</summary>
        public static int SlotSeed(int serial)
        {
            return MathUtil.Hash(serial * 131 + 17, 0x04e1);
        }

        /// <summary>Risk shown and paid (the endgame tier raises it by one, capped at 5).</summary>
        public static int EffectiveRisk(ContractDef t, bool endgame)
        {
            int risk = Mathf.Clamp(t != null && t.Risk > 0 ? t.Risk : 1, 1, 5);
            return endgame ? Mathf.Min(5, risk + 1) : risk;
        }

        public static string RiskLabel(int risk)
        {
            switch (Mathf.Clamp(risk, 1, 5))
            {
                case 1: return "Low risk";
                case 2: return "Moderate risk";
                case 3: return "High risk";
                case 4: return "Severe risk";
                default: return "Extreme risk";
            }
        }

        /// <summary>A contract's pay: base pay scaled by risk, loyalty (contracts done, capped) and the endgame tier. Bounded.</summary>
        public static QuestReward Pay(ContractDef t, int done, bool endgame)
        {
            QuestReward r = new QuestReward();
            if (t == null) return r;
            int risk = EffectiveRisk(t, endgame);
            float scale = (1f + 0.25f * (risk - 1)) * (1f + 0.04f * Mathf.Clamp(done, 0, 10)) * (endgame ? 1.3f : 1f);
            r.Cash = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0, t.Cash) * scale / 10f) * 10, 100, 12000);
            r.Lumen = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(0, t.Lumen) * scale / 5f) * 5, 10, 2500);
            if (t.Materials != null)
                foreach (MaterialAmount m in t.Materials)
                    if (m != null && !string.IsNullOrEmpty(m.Id) && m.Count > 0)
                        r.Materials.Add(new MaterialAmount { Id = m.Id, Count = Mathf.Clamp(m.Count + (endgame ? 1 : 0), 1, 10) });
            return r;
        }

        /// <summary>Builds the job a slot posts, or null when the template cannot post right now.</summary>
        public static ContractOffer Make(ContractDef t, int serial, CampaignBoardContext ctx, int done)
        {
            if (t == null || ctx == null || WhyNotEligible(t, ctx) != null) return null;
            List<string> dests = EligibleDestinations(t, ctx);
            if (dests.Count == 0) return null;
            GRand r = new GRand(MathUtil.Hash(serial * 31 + 7, Hash(t.Id)));
            string dest = dests[r.Range(0, dests.Count)];
            int risk = EffectiveRisk(t, ctx.Endgame);
            ContractOffer o = new ContractOffer { Guild = t.Guild, TemplateId = t.Id, Serial = serial, Kind = t.Kind, Destination = dest };
            o.Place = UnderheartIds.RegionTitle(CampaignRules04E.RegionOf(dest));
            o.Reward = Pay(t, done, ctx.Endgame);
            o.Count = RosterCount(t.Roster);
            o.Title = Fill(t.Title, o);
            o.Brief = Fill(t.Brief, o) + " " + RiskLabel(risk) + ".";
            o.Quest = BuildQuest(t, o, risk);
            return o;
        }

        private static int RosterCount(List<RosterEntry> roster)
        {
            int n = 0;
            if (roster != null) foreach (RosterEntry e in roster) if (e != null) n += Mathf.Max(1, e.Count);
            return n;
        }

        private static string Fill(string text, ContractOffer o)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("{place}", o.Place).Replace("{dest}", o.Place).Replace("{count}", o.Count.ToString()).Replace("{species}", "");
        }

        private static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                if (s != null) foreach (char c in s) h = h * 31 + c;
                return h;
            }
        }

        private static List<RosterEntry> CopyRoster(List<RosterEntry> src, bool endgame)
        {
            List<RosterEntry> list = new List<RosterEntry>();
            if (src == null) return list;
            foreach (RosterEntry e in src)
            {
                if (e == null || string.IsNullOrEmpty(e.Species)) continue;
                list.Add(new RosterEntry { Species = e.Species, Count = Mathf.Clamp(Mathf.Max(1, e.Count) + (endgame ? 1 : 0), 1, 8), Elite = e.Elite });
            }
            return list;
        }

        private static QuestStep Go(string at, string objective)
        {
            return new QuestStep { Type = "GoTo", At = at, Radius = 6f, Objective = objective };
        }

        private static QuestStep Use(string at, string prompt, string objective, string text)
        {
            return new QuestStep { Type = "Interact", At = at, Prompt = prompt, Objective = objective, Text = text };
        }

        private static QuestStep Fight(ContractDef t, string at, bool endgame, string objective)
        {
            return new QuestStep
            {
                Type = "Skirmish", At = at, Radius = 16f, Roster = CopyRoster(t.Roster, endgame), Reinforce = Mathf.Clamp(t.Reinforce, 0, 4),
                Objective = objective, HealthScale = endgame ? 1.25f : 0f
            };
        }

        /// <summary>The contract as quest data (the same step runtime as the campaign quests).</summary>
        private static QuestDef BuildQuest(ContractDef t, ContractOffer o, int risk)
        {
            bool endgame = risk > EffectiveRisk(t, false);
            QuestDef q = new QuestDef
            {
                Id = "contract_" + t.Guild + "_" + o.Serial,
                Title = o.Title,
                Arc = t.Guild,
                Campaign = t.Guild,
                Giver = BoardPlace,
                Presenter = BoardName,
                Region = CampaignRules04E.RegionOf(o.Destination),
                Difficulty = risk,
                Summary = o.Brief,
                Reward = o.Reward
            };
            QuestPhase ph = new QuestPhase { Id = "job", Title = o.Title };
            q.Phases.Add(ph);
            QuestStep talk = new QuestStep { Type = "Talk" };
            talk.Lines.Add(new DialogueLine { Speaker = BoardName, Text = o.Brief.Length <= 250 ? o.Brief : o.Brief.Substring(0, 250) });
            ph.Steps.Add(talk);
            string d = o.Destination;
            switch (t.Kind)
            {
                case "Salvage":
                    ph.Steps.Add(Go(d, "Reach the salvage site in " + o.Place + "."));
                    if (t.Roster != null && t.Roster.Count > 0) ph.Steps.Add(Fight(t, d, endgame, "Drive off the scavengers on the salvage."));
                    ph.Steps.Add(new QuestStep { Type = "Interact", At = d, Count = 3, Radius = 5f, Prop = "Crate", Name = "SALVAGE", Prompt = "Press E to strip the salvage", Objective = "Strip the three marked salvage crates.", Text = "Useful metal, still warm." });
                    ph.Steps.Add(Go(BoardPlace, "Bring the salvage back to the Lantern Refuge board."));
                    break;
                case "EliteHunt":
                    ph.Steps.Add(Go(d, "Find the elite's lair in " + o.Place + "."));
                    ph.Steps.Add(Fight(t, d, endgame, "Defeat the elite and its escort. Nothing else will come."));
                    ph.Steps.Add(Go(BoardPlace, "Report the kill at the Lantern Refuge board."));
                    break;
                case "ValveDefense":
                    ph.Steps.Add(Go(d, "Reach the valve station in " + o.Place + "."));
                    ph.Steps.Add(Use(d, "Press E to start the valve cycle", "Start the valve cycle.", "The valve groans and begins to turn."));
                    ph.Steps.Add(Fight(t, d, endgame, "Hold the valve while it turns. Reinforcements are announced."));
                    ph.Steps.Add(Use(d, "Press E to lock the valve", "Lock the valve before it slips.", "Locked. The pressure holds."));
                    break;
                case "CreatureRescue":
                    ph.Steps.Add(Go(d, "Reach the trapped resident in " + o.Place + "."));
                    ph.Steps.Add(new QuestStep { Type = "Assist", At = d, Radius = 10f, Seconds = 8f, Prompt = "Press E to search the rubble by hand", Objective = "Find the trapped resident: companion assist (U) or search by hand." });
                    ph.Steps.Add(Use(d, "Press E to free the trapped resident", "Free the trapped resident.", "It scrambles free, stops, and looks back once."));
                    ph.Steps.Add(Go(BoardPlace, "Tell the Lantern Refuge board the resident is safe."));
                    break;
                case "LensCalibration":
                    ph.Steps.Add(Go(d, "Reach the resonance lens in " + o.Place + "."));
                    ph.Steps.Add(new QuestStep { Type = "Interact", At = d, Count = 3, Radius = 5f, Prop = "Crystal", Name = "LENSES", Prompt = "Press E to align the lens", Objective = "Align the three lens facets.", Text = "The facet clicks into tune." });
                    ph.Steps.Add(new QuestStep { Type = "Message", Text = "The lens hums in tune. The refuge maps sharpen.", Seconds = 4f });
                    break;
                case "ShortCourier":
                    ph.Steps.Add(Use(BoardPlace, "Press E to take the sealed case", "Take the sealed case from the board.", "The case is cold and heavier than it looks."));
                    ph.Steps.Add(Go(d, "Carry the case to " + o.Place + " before the seal warms."));
                    ph.Steps.Add(Use(d, "Press E to hand over the case", "Hand over the sealed case.", "Delivered cold."));
                    if (t.Seconds > 0f) q.FailIf.Add(new QuestFailCondition { Type = "TimeLimit", Seconds = Mathf.Clamp(t.Seconds, 60f, 1800f), Message = "The seal warmed. The courier job is void; the board will post another." });
                    break;
                case "SurveyorSearch":
                    ph.Steps.Add(Go(d, "Search " + o.Place + " for the missing surveyor."));
                    ph.Steps.Add(new QuestStep { Type = "Assist", At = d, Radius = 12f, Seconds = 10f, Prompt = "Press E to follow the chalk marks by hand", Objective = "Find the surveyor's trail: companion assist (U) or follow the chalk marks by hand." });
                    QuestStep meet = new QuestStep { Type = "Talk", At = d, Look = string.IsNullOrEmpty(t.Look) ? "Prospector" : t.Look, Name = string.IsNullOrEmpty(t.Name) ? "Surveyor" : t.Name, Objective = "Reach the surveyor." };
                    meet.Lines.Add(new DialogueLine { Speaker = meet.Name, Text = "You found me. My lamp died and the passage changed under my feet. Tell the board I am alive; I will chalk my own way back." });
                    ph.Steps.Add(meet);
                    ph.Steps.Add(Go(BoardPlace, "Report the surveyor's position at the Lantern Refuge board."));
                    break;
                case "ExtractionTrial":
                    ph.Steps.Add(Go(d, "Reach the extraction trial site in " + o.Place + "."));
                    ph.Steps.Add(Fight(t, d, endgame, "Survive the extraction trial: the declared roster and announced reinforcements."));
                    if (t.Seconds > 0f) q.FailIf.Add(new QuestFailCondition { Type = "TimeLimit", Seconds = Mathf.Clamp(t.Seconds, 60f, 1800f), Message = "The trial window closed. Optional, so nothing is lost but the pay." });
                    break;
            }
            return q;
        }

        /// <summary>One line for the board: title, kind, place, risk and pay.</summary>
        public static string BoardLine(ContractOffer o, ContractDef t, bool endgame)
        {
            if (o == null) return "";
            StringBuilder sb = new StringBuilder();
            sb.Append("<b>").Append(o.Title).Append("</b>  ").Append(o.Place).Append(", ").Append(RiskLabel(EffectiveRisk(t, endgame))).Append("  ").Append(QuestLogic.RewardText(o.Reward));
            return sb.ToString();
        }
    }
}
