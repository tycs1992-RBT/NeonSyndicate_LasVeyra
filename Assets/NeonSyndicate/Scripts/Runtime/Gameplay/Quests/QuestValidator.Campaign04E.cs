using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart and Bonds 04E half of the validator: campaign declarations, campaign quest rules (kind, act,
    /// difficulty, region, claims, checkpoints, idempotent rewards, exact underground places), the 04E step types and the
    /// campaign contract templates. Plain C#, no engine calls.
    /// </summary>
    public static partial class QuestValidator
    {
        /// <summary>Step types that place creatures or people with outdoor ground probes: never at an Underheart anchor (use Skirmish or UBoss).</summary>
        private static readonly string[] SurfaceOnlyTypes = { "Kill", "Spawn", "Defend", "Survive", "Escort", "Hunt", "CloseRift", "Scan", "Boss", "Ally", "Cinematic" };

        /// <summary>Step types whose place must be written out in campaign quests (a resumed checkpoint has no "Previous" place).</summary>
        private static readonly string[] ExplicitPlaceTypes = { "GoTo", "Interact", "Collect", "Build", "Care", "Assist", "Rescue", "Skirmish", "UBoss", "Mechanism" };

        private static readonly string[] SkirmishGrammars = { "EliteHunt", "ValveDefense", "ExtractionTrial" };

        // ================================================================== campaign declarations

        private static void ValidateCampaignDefs(string file, QuestFile f, QuestRefs refs, HashSet<string> campaigns, QuestReport rep, QuestJsonTrace trace)
        {
            foreach (CampaignDef c in f.Campaigns)
            {
                string w = file + At(trace, c) + ": campaign '" + c.Id + "'";
                if (!IsId(c.Id)) rep.Errors.Add(w + ": Id must be lower case letters, digits and underscores");
                else if (refs.Factions.Contains(c.Id)) rep.Errors.Add(w + ": a campaign id cannot also be a faction id");
                else if (!campaigns.Add(c.Id)) rep.Errors.Add(w + ": this campaign is declared twice");
                CheckText(w, "Title", c.Title, rep, 40, true);
                float r, g, b;
                if (!CampaignRules04E.ParseColor(c.Color, out r, out g, out b)) rep.Errors.Add(w + ": Color must be #RRGGBB");
            }
        }

        // ================================================================== campaign quests

        private static void ValidateCampaignQuest(string where, QuestDef q, Dictionary<string, QuestDef> byId, bool campaignArc, HashSet<string> campaigns, QuestRefs refs, QuestReport rep)
        {
            bool contractJob = q.Id.StartsWith("contract_", StringComparison.Ordinal);
            if (!campaignArc)
            {
                if (q.Kind != "" || q.Campaign != "" || q.Act != 0 || q.Difficulty != 0 || q.Region != "" || q.Floor != "" || q.Entrance != "" || q.Claims.Count > 0)
                    rep.Errors.Add(where + ": Kind, Campaign, Act, Difficulty, Region, Floor, Entrance and Claims are only read by campaign quests");
                foreach (QuestPhase ph in q.Phases)
                    if (ph.Checkpoint || ph.ResumeAt != "") rep.Errors.Add(where + ", phase '" + ph.Id + "': checkpoints are only committed for campaign quests");
                return;
            }
            if (q.Campaign != q.Arc) rep.Errors.Add(where + ": Campaign must equal Arc ('" + q.Arc + "') for campaign quests");
            if (contractJob)
            {
                if (q.Kind != "") rep.Errors.Add(where + ": generated contract jobs have no Kind");
            }
            else if (q.Kind != "Main" && q.Kind != "Side") rep.Errors.Add(where + ": Kind must be Main or Side for campaign quests");
            if (q.Kind == "Main")
            {
                if (q.Act < 1 || q.Act > 4) rep.Errors.Add(where + ": main quests need Act 1 to 4");
                if (q.Order > 99) rep.Errors.Add(where + ": main quests use Order 1 to 99");
                if (q.Phases.Count < 3) rep.Errors.Add(where + ": main quests need at least three real phases");
            }
            else if (q.Kind == "Side")
            {
                if (q.Act < 0 || q.Act > 4) rep.Errors.Add(where + ": Act must be 0 to 4");
                if (q.Order < 100) rep.Errors.Add(where + ": side quests use Order 100 or more");
            }
            if (q.Difficulty < 1 || q.Difficulty > 5) rep.Errors.Add(where + ": Difficulty must be 1 to 5");
            if (!CampaignRules04E.KnownRegion(q.Region)) rep.Errors.Add(where + ": Region '" + q.Region + "' must be surface or an Underheart region id");
            if (!contractJob)
            {
                CheckText(where, "Floor", q.Floor, rep, 70, true);
                CheckText(where, "Entrance", q.Entrance, rep, 90, true);
            }
            // Claims listed for display must match the Claim steps exactly.
            HashSet<string> listed = new HashSet<string>();
            foreach (string id in q.Claims)
            {
                if (!RewardIds04E.Has(id)) rep.Errors.Add(where + ": Claims lists unknown reward '" + id + "'" + Hint(id, RewardIds04E.All));
                else if (!listed.Add(id)) rep.Errors.Add(where + ": Claims lists '" + id + "' twice");
            }
            HashSet<string> claimed = new HashSet<string>();
            foreach (QuestPhase ph in q.Phases)
            {
                string pw = where + ", phase '" + ph.Id + "'";
                if (ph.Checkpoint)
                {
                    if (string.IsNullOrEmpty(ph.ResumeAt)) rep.Errors.Add(pw + ": a checkpoint phase needs ResumeAt (where a resumed quest starts)");
                    else if (!PlaceOk(refs, ph.ResumeAt) || ph.ResumeAt == "Previous" || ph.ResumeAt == "Player" || ph.ResumeAt == "")
                        rep.Errors.Add(pw + ": ResumeAt '" + ph.ResumeAt + "' must be a named place or a place token");
                    else if (ph.ResumeAt.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal))
                        rep.Errors.Add(pw + ": ResumeAt cannot follow a person (use their venue or anchor)");
                }
                else if (!string.IsNullOrEmpty(ph.ResumeAt)) rep.Errors.Add(pw + ": ResumeAt is only read on a Checkpoint phase");
                foreach (QuestStep s in ph.Steps)
                {
                    string sw = pw + " (" + s.Type + ")";
                    if (s.Type == "Give" || s.Type == "GiveGun") rep.Errors.Add(sw + ": campaign quests pay through Claim steps and the quest Reward (Give is not idempotent across checkpoints)");
                    if (s.Type == "Collect" && s.Spend && ph.Checkpoint) rep.Errors.Add(sw + ": a Collect that spends materials cannot sit in a checkpoint phase (a resume would ask again)");
                    if (Array.IndexOf(SurfaceOnlyTypes, s.Type) >= 0 && (s.At.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal) || s.To.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal)))
                        rep.Errors.Add(sw + ": " + s.Type + " uses outdoor ground probes; underground fights use Skirmish or UBoss");
                    if (Array.IndexOf(ExplicitPlaceTypes, s.Type) >= 0 && (s.At == "" || s.At == "Previous"))
                        rep.Errors.Add(sw + ": campaign steps write their place out (a resumed checkpoint has no Previous place)");
                    if (s.Type == "Talk" && s.Look != "" && (s.At == "" || s.At == "Previous")) rep.Errors.Add(sw + ": a Talk that brings a person needs its place written out");
                    if (s.Type == "Talk" && s.At.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal) && s.Look != "")
                        rep.Errors.Add(sw + ": a cast member is a real person already; leave Look out (never two copies of someone)");
                    if (s.Type == "Claim" && s.Reward != "")
                    {
                        claimed.Add(s.Reward);
                        if (!listed.Contains(s.Reward)) rep.Errors.Add(sw + ": reward '" + s.Reward + "' is not listed in the quest's Claims");
                    }
                    foreach (ChoiceOption o in s.Options)
                        if (o.When.StartsWith("quest:", StringComparison.Ordinal) || o.When.Contains("&quest:") || o.When.Contains("!quest:"))
                            CheckCondition(sw, o.When, refs, rep, s.Type == "Branch", byId);
                }
            }
            foreach (string id in listed) if (!claimed.Contains(id)) rep.Errors.Add(where + ": Claims lists '" + id + "' but no Claim step grants it");
        }

        // ================================================================== 04E steps

        private static void ValidateCampaignStep(string w, QuestStep s, QuestDef q, HashSet<string> phaseIds, QuestRefs refs, QuestReport rep)
        {
            switch (s.Type)
            {
                case "Build":
                    break;
                case "Claim":
                {
                    if (!RewardIds04E.Has(s.Reward)) rep.Errors.Add(w + ": unknown Reward '" + s.Reward + "'" + Hint(s.Reward, RewardIds04E.All));
                    string boss = CampaignRules04E.BossTokenFor(s.Reward);
                    if (boss != null && !string.IsNullOrEmpty(s.Token) && s.Token != boss)
                        rep.Errors.Add(w + ": '" + s.Reward + "' is a boss reward; its Token must be '" + boss + "' (or left out) so it is never granted twice");
                    if (!string.IsNullOrEmpty(s.Token) && (s.Token.Length > 80 || s.Token.IndexOf(' ') >= 0)) rep.Errors.Add(w + ": Token must be up to 80 characters with no spaces");
                    break;
                }
                case "Equip":
                    if (!string.IsNullOrEmpty(s.At) && s.At != CampaignRules04E.ArsenalCase && s.At != CampaignRules04E.ArsenalLane)
                        rep.Errors.Add(w + ": Equip guides to the resonance case (Arsenal:case or Arsenal:lane, or leave At out)");
                    break;
                case "Rescue":
                    if (!BondIds.Has(s.Bond)) rep.Errors.Add(w + ": unknown Bond '" + s.Bond + "'" + Hint(s.Bond, BondIds.Archetypes));
                    if (s.Points.Count > 5) rep.Errors.Add(w + ": at most 5 clamp Points");
                    foreach (string p in s.Points)
                        if (!CampaignRules04E.IsPlaceToken(p) || CampaignRules04E.PlaceTokenError(p) != null || p.StartsWith(CampaignRules04E.CastPrefix, StringComparison.Ordinal))
                            rep.Errors.Add(w + ": clamp point '" + p + "' must be a Venue: or Under: place token");
                    break;
                case "BondRank":
                    if (!BondIds.Has(s.Bond)) rep.Errors.Add(w + ": unknown Bond '" + s.Bond + "'" + Hint(s.Bond, BondIds.Archetypes));
                    Range(w, "Rank", s.Rank, 1, BondIds.RankAwakened, rep);
                    break;
                case "Care":
                    break;
                case "Assist":
                    Range(w, "Seconds", s.Seconds, 0f, 30f, rep);
                    Range(w, "Radius", s.Radius, 0f, 30f, rep);
                    break;
                case "Fuse":
                    if (!string.IsNullOrEmpty(s.Form) && !FusionIds.Has(s.Form)) rep.Errors.Add(w + ": unknown Form '" + s.Form + "'" + Hint(s.Form, FusionIds.Forms));
                    if (s.Text != "" && s.Text != "separate") rep.Errors.Add(w + ": Fuse Text is empty (wait for a fusion) or 'separate' (wait for a safe separation)");
                    if (s.Text == "separate" && (s.Form != "" || s.At != "")) rep.Errors.Add(w + ": a separation step takes no Form or At");
                    break;
                case "Course":
                    if (Array.IndexOf(FusionIds.Courses, s.Course) < 0) rep.Errors.Add(w + ": unknown Course '" + s.Course + "'" + Hint(s.Course, FusionIds.Courses));
                    Range(w, "Count", s.Count, 0, 5, rep);
                    break;
                case "Park":
                    break;
                case "Skirmish":
                {
                    if (s.Roster.Count == 0 || s.Roster.Count > 6) rep.Errors.Add(w + ": a Skirmish declares 1 to 6 roster lines");
                    int total = 0;
                    foreach (RosterEntry e in s.Roster) total += CheckRosterEntry(w, e, refs, rep);
                    if (total > 12) rep.Errors.Add(w + ": a Skirmish roster holds at most 12 bodies (" + total + " declared)");
                    Range(w, "Reinforce", s.Reinforce, 0, 4, rep);
                    if (!string.IsNullOrEmpty(s.Room) && !UnderheartIds.HasAnchor(s.Room)) rep.Errors.Add(w + ": Room '" + s.Room + "' must be an Underheart anchor id (the room marked cleared)");
                    break;
                }
                case "UBoss":
                    if (!UnderBossIds.Has(s.Boss)) rep.Errors.Add(w + ": unknown Boss '" + s.Boss + "'" + Hint(s.Boss, UnderBossIds.All));
                    Range(w, "Radius", s.Radius, 0f, 60f, rep);
                    break;
                case "Mechanism":
                    if (Array.IndexOf(UnderBossIds.Mechanisms, s.Mechanism) < 0) rep.Errors.Add(w + ": unknown Mechanism '" + s.Mechanism + "'" + Hint(s.Mechanism, UnderBossIds.Mechanisms));
                    break;
                case "Shortcut":
                    if (!UnderheartIds.HasShortcut(s.Shortcut)) rep.Errors.Add(w + ": unknown Shortcut '" + s.Shortcut + "'" + Hint(s.Shortcut, UnderheartIds.Shortcuts));
                    break;
                case "Decide":
                    ValidateDecide(w, s, phaseIds, refs, rep);
                    break;
                case "Branch":
                {
                    if (s.Options.Count < 2 || s.Options.Count > 6) rep.Errors.Add(w + ": a Branch needs 2 to 6 Options");
                    for (int i = 0; i < s.Options.Count; i++)
                    {
                        ChoiceOption o = s.Options[i];
                        string ow = w + ", option " + (i + 1);
                        if (o.Phase != QuestDefaults.End && !phaseIds.Contains(o.Phase)) rep.Errors.Add(ow + ": leads to unknown phase '" + o.Phase + "'" + Hint(o.Phase, phaseIds));
                        if (!string.IsNullOrEmpty(o.Value)) rep.Errors.Add(ow + ": Value is only read by Decide steps");
                        CheckText(ow, "Options.Label", o.Label, rep, 16, false);
                        CheckCondition(ow, o.When, refs, rep, true, null);
                        if (o.When == "else" && i != s.Options.Count - 1) rep.Errors.Add(ow + ": 'else' must be the last option");
                    }
                    if (s.Options.Count > 0 && s.Options[s.Options.Count - 1].When != "else") rep.Errors.Add(w + ": a Branch ends with an 'else' option so it always goes somewhere");
                    break;
                }
                case "Conduit":
                    break;
                case "Flag":
                    if (!CampaignRules04E.IsName(s.Key)) rep.Errors.Add(w + ": flag Key must be lower case letters, digits and underscores");
                    break;
            }
        }

        private static void ValidateDecide(string w, QuestStep s, HashSet<string> phaseIds, QuestRefs refs, QuestReport rep)
        {
            if (Array.IndexOf(QuestIds04E.ChoiceKeys, s.Key) < 0) { rep.Errors.Add(w + ": unknown choice Key '" + s.Key + "'" + Hint(s.Key, QuestIds04E.ChoiceKeys)); return; }
            if (s.Options.Count == 0)
            {
                if (!CampaignRules04E.ValidChoice(s.Key, s.Value)) rep.Errors.Add(w + ": Value '" + s.Value + "' is not a value of '" + s.Key + "' (" + string.Join(", ", QuestIds04E.ChoiceValues(s.Key)) + ")");
                if (s.Text != "") rep.Errors.Add(w + ": a Decide without Options records its Value at once; Text is not shown");
                if (s.Reask) rep.Errors.Add(w + ": Reask only applies to a Decide with Options (a question that can be asked again)");
                return;
            }
            if (s.Value != "") rep.Errors.Add(w + ": a Decide with Options takes each option's Value, not its own");
            if (s.Options.Count < 2 || s.Options.Count > 4) rep.Errors.Add(w + ": a Decide needs 2 to 4 Options");
            CheckText(w, "Text", s.Text, rep, 400, true);
            int withPhase = 0;
            HashSet<string> values = new HashSet<string>();
            foreach (ChoiceOption o in s.Options)
            {
                string ow = w + ", option '" + o.Label + "'";
                CheckText(ow, "Options.Label", o.Label, rep, 16, true);
                if (!CampaignRules04E.ValidChoice(s.Key, o.Value)) rep.Errors.Add(ow + ": Value '" + o.Value + "' is not a value of '" + s.Key + "'");
                else if (!values.Add(o.Value)) rep.Errors.Add(ow + ": two options record the same value");
                if (!string.IsNullOrEmpty(o.Phase))
                {
                    withPhase++;
                    if (o.Phase != QuestDefaults.End && !phaseIds.Contains(o.Phase)) rep.Errors.Add(ow + ": leads to unknown phase '" + o.Phase + "'" + Hint(o.Phase, phaseIds));
                }
                if (!string.IsNullOrEmpty(o.When)) CheckCondition(ow, o.When, refs, rep, false, null);
            }
            if (withPhase != 0 && withPhase != s.Options.Count) rep.Errors.Add(w + ": either every option leads to a phase or none does");
        }

        private static int CheckRosterEntry(string w, RosterEntry e, QuestRefs refs, QuestReport rep)
        {
            if (e == null) return 0;
            if (!refs.Species.Contains(e.Species)) rep.Errors.Add(w + ": unknown roster species '" + e.Species + "'" + Hint(e.Species, refs.Species));
            else if (refs.Bosses.Contains(e.Species)) rep.Errors.Add(w + ": bosses never join a roster ('" + e.Species + "')");
            Range(w, "Roster.Count", e.Count, 1, 8, rep);
            return Math.Max(0, e.Count);
        }

        private static void CheckCondition(string w, string cond, QuestRefs refs, QuestReport rep, bool allowElse, Dictionary<string, QuestDef> byId)
        {
            if (string.IsNullOrEmpty(cond)) { rep.Errors.Add(w + ": When is required"); return; }
            if (cond == "else" && !allowElse) { rep.Errors.Add(w + ": 'else' is only for Branch options"); return; }
            Func<string, bool> questKnown = null;
            if (byId != null) questKnown = id => byId.ContainsKey(id);
            string e = CampaignRules04E.ConditionError(cond, questKnown, id => refs.Materials.Contains(id));
            if (e != null) rep.Errors.Add(w + ": When '" + cond + "': " + e);
        }

        // ================================================================== campaign contracts

        private static void ValidateCampaignContract(string file, ContractDef c, Dictionary<string, QuestDef> byId, QuestRefs refs, QuestReport rep, QuestJsonTrace trace)
        {
            rep.Contracts++;
            int before = rep.Errors.Count;
            string w = file + At(trace, c) + ": campaign contract '" + c.Id + "'";
            if (!IsId(c.Id)) rep.Errors.Add(w + ": Id must be lower case letters, digits and underscores");
            if (!QuestCatalog.Contains(QuestCatalog.CampaignContractKinds, c.Kind)) rep.Errors.Add(w + ": unknown Kind '" + c.Kind + "'" + Hint(c.Kind, QuestCatalog.CampaignContractKinds));
            CheckText(w, "Title", c.Title, rep, 48, true);
            CheckText(w, "Brief", c.Brief, rep, 230, true);
            foreach (string label in new[] { c.Title, c.Brief })
                if (label.Contains("{species}")) rep.Errors.Add(w + ": campaign contracts use {place}, {dest} and {count}, not {species}");
            CheckTokens(w, "Title", c.Title, rep);
            CheckTokens(w, "Brief", c.Brief, rep);
            Range(w, "Weight", c.Weight, 0f, 10f, rep);
            Range(w, "Risk", c.Risk, 1, 5, rep);
            if (c.MinRep != 0 || c.Rep != 0) rep.Errors.Add(w + ": a campaign board has no guild reputation (MinRep and Rep stay 0)");
            if (c.Species.Count > 0 || c.CountMin != 0 || c.CountMax != 0) rep.Errors.Add(w + ": campaign contracts declare their encounter with Roster, not Species or counts");
            Range(w, "Cash", c.Cash, 0, 10000, rep);
            Range(w, "Lumen", c.Lumen, 0, 3000, rep);
            foreach (MaterialAmount m in c.Materials)
            {
                if (!refs.Materials.Contains(m.Id)) rep.Errors.Add(w + ": unknown material '" + m.Id + "'" + Hint(m.Id, refs.Materials));
                Range(w, "Materials.Count", m.Count, 1, 10, rep);
            }
            if (c.Regions.Count == 0) rep.Errors.Add(w + ": Regions lists where the board may send the player");
            foreach (string r in c.Regions)
                if (r == "surface" || !CampaignRules04E.KnownRegion(r)) rep.Errors.Add(w + ": Regions holds '" + r + "', which is not an Underheart region id");
            if (c.Destinations.Count == 0) rep.Errors.Add(w + ": Destinations lists the authored anchor set");
            foreach (string d in c.Destinations)
            {
                if (!d.StartsWith(CampaignRules04E.UnderPrefix, StringComparison.Ordinal) || CampaignRules04E.PlaceTokenError(d) != null) rep.Errors.Add(w + ": destination '" + d + "' must be a valid Under: anchor");
                else if (!c.Regions.Contains(CampaignRules04E.RegionOf(d))) rep.Errors.Add(w + ": destination '" + d + "' lies in " + CampaignRules04E.RegionOf(d) + ", which Regions does not list");
            }
            if (!string.IsNullOrEmpty(c.After) && !byId.ContainsKey(c.After)) rep.Errors.Add(w + ": After names unknown quest '" + c.After + "'");
            int total = 0;
            foreach (RosterEntry e in c.Roster) total += CheckRosterEntry(w, e, refs, rep);
            if (total > 10) rep.Errors.Add(w + ": a contract roster holds at most 10 bodies");
            Range(w, "Reinforce", c.Reinforce, 0, 4, rep);
            bool fights = Array.IndexOf(SkirmishGrammars, c.Kind) >= 0;
            if (fights && c.Roster.Count == 0) rep.Errors.Add(w + ": " + c.Kind + " contracts declare a Roster");
            if (!fights && c.Kind != "Salvage" && (c.Roster.Count > 0 || c.Reinforce > 0)) rep.Errors.Add(w + ": " + c.Kind + " contracts are peaceful (no Roster)");
            if (c.Kind == "EliteHunt")
            {
                bool elite = false;
                foreach (RosterEntry e in c.Roster) if (e != null && e.Elite) elite = true;
                if (!elite) rep.Errors.Add(w + ": an EliteHunt names its elite (a roster line with Elite true)");
            }
            if (c.Kind == "ShortCourier" || c.Kind == "ExtractionTrial") Range(w, "Seconds", c.Seconds, 60f, 1800f, rep);
            else if (c.Seconds != 0f) rep.Errors.Add(w + ": Seconds is only read by ShortCourier and ExtractionTrial");
            if (c.Kind == "SurveyorSearch")
            {
                if (!QuestCatalog.Contains(QuestCatalog.Looks, c.Look)) rep.Errors.Add(w + ": SurveyorSearch needs a Look for the surveyor" + Hint(c.Look, QuestCatalog.Looks));
                CheckText(w, "Name", c.Name, rep, 40, true);
            }
            else if (c.Look != "" || c.Name != "") rep.Errors.Add(w + ": Look and Name are only read by SurveyorSearch");
            if (rep.Errors.Count != before) return;
            // Every destination must produce a valid runnable job, on the normal and the endgame tier.
            CampaignBoardContext ctx = new CampaignBoardContext();
            foreach (string id in byId.Keys) ctx.Completed.Add(id);
            HashSet<string> campaignIds = new HashSet<string> { c.Guild };
            for (int tier = 0; tier < 2; tier++)
            {
                ctx.Endgame = tier == 1;
                for (int serial = 1; serial <= Math.Max(4, c.Destinations.Count * 3); serial++)
                {
                    ContractOffer o = CampaignContracts04E.Make(c, serial, ctx, serial % 5);
                    if (o == null) { rep.Errors.Add(w + ": could not post a job (serial " + serial + ")"); return; }
                    QuestReport sub = new QuestReport();
                    o.Quest.Order = 1;
                    ValidateQuest(file, o.Quest, byId, new HashSet<string>(), campaignIds, refs, sub, null);
                    foreach (string e in sub.Errors) rep.Errors.Add(w + " (generated job " + serial + "): " + e);
                    if (sub.Errors.Count > 0) return;
                }
            }
        }
    }
}
