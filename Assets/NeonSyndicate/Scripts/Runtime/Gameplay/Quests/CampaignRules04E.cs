using System;
using System.Collections.Generic;
using System.Text;

namespace NeonSyndicate
{
    /// <summary>
    /// The live facts a campaign condition reads. The quest book fills it in the game; tests fill it by hand. Every
    /// lookup is a delegate so the rules stay engine-free.
    /// </summary>
    public sealed class CampaignFacts
    {
        /// <summary>The original ending's law: "Mercy", "Power" or "" (story not finished, or Free Roam).</summary>
        public string Law = "";
        /// <summary>The original twelve-chapter story is complete.</summary>
        public bool StoryDone;
        public bool FreeRoam;
        /// <summary>The player is in a fused form right now.</summary>
        public bool Fused;
        /// <summary>The persistent fusion entitlement exists.</summary>
        public bool FusionUnlocked;
        public Func<string, string> Choice = k => "";
        public Func<string, bool> Flag = f => false;
        public Func<string, bool> OwnsBond = a => false;
        public Func<string, bool> QuestDone = q => false;
        public Func<string, int> Material = m => 0;
    }

    /// <summary>
    /// Underheart and Bonds 04E campaign rules in plain C# (no engine calls), shared by the runtime, the validator and the
    /// EditMode tests: the quest place tokens (Venue:, Under:, Cast:, Arsenal:case, Arsenal:lane), the Branch and option
    /// condition grammar, checkpoint and choice records, the claim token policy and readable labels.
    /// </summary>
    public static class CampaignRules04E
    {
        // ================================================================== place tokens

        public const string VenuePrefix = "Venue:";
        public const string UnderPrefix = "Under:";
        public const string CastPrefix = "Cast:";
        public const string ArsenalCase = "Arsenal:case";
        public const string ArsenalLane = "Arsenal:lane";

        /// <summary>The eight 04C venues (their catalog ids). The four 04E venues are in VenueIds04E.All.</summary>
        public static readonly string[] LegacyVenues = { "lofts", "clinic", "forge", "inn", "garage", "archive", "observatory", "depot" };

        /// <summary>Anchors every 04C and 04E venue layout provides (InhabitedCity04CComposition checks them at load).</summary>
        public static readonly string[] BaseVenueAnchors = { "service", "activity", "staff", "visitor1", "visitor2", "exit" };

        /// <summary>The extra activity anchors a 04C layout must carry (the composition's per-venue check), plus the 04E extras.</summary>
        public static string[] ExtraVenueAnchors(string venueId)
        {
            switch (venueId)
            {
                case "archive": return new[] { "record1", "record2", "record3" };
                case "observatory": return new[] { "instrument1", "instrument2", "instrument3" };
                case "depot": return new[] { "record1", "defense1", "defense2", "retrieve" };
                case "clinic": return new[] { "record1" };
                case "lofts": return new[] { "bed", "desk" };
                default: return VenueIds04E.ExtraAnchors(venueId);
            }
        }

        public static bool KnownVenue(string id)
        {
            return !string.IsNullOrEmpty(id) && (Array.IndexOf(LegacyVenues, id) >= 0 || VenueIds04E.Has(id));
        }

        public static bool KnownVenueAnchor(string venueId, string anchor)
        {
            if (!KnownVenue(venueId) || string.IsNullOrEmpty(anchor)) return false;
            if (Array.IndexOf(BaseVenueAnchors, anchor) >= 0) return true;
            if (Array.IndexOf(ExtraVenueAnchors(venueId), anchor) >= 0) return true;
            // garage keeps its 04C anchors and gains the 04E bay
            return venueId == "garage" && Array.IndexOf(VenueIds04E.ExtraAnchors("garage"), anchor) >= 0;
        }

        /// <summary>True for a 04E place token (it resolves at runtime to an exact 3D point, never ground-snapped).</summary>
        public static bool IsPlaceToken(string at)
        {
            if (string.IsNullOrEmpty(at)) return false;
            return at.StartsWith(VenuePrefix, StringComparison.Ordinal) || at.StartsWith(UnderPrefix, StringComparison.Ordinal)
                || at.StartsWith(CastPrefix, StringComparison.Ordinal) || at == ArsenalCase || at == ArsenalLane
                || at.StartsWith("Arsenal:", StringComparison.Ordinal);
        }

        /// <summary>Splits "Venue:dispatch.fitting" into the venue and the anchor. False when malformed.</summary>
        public static bool SplitVenue(string token, out string venue, out string anchor)
        {
            venue = "";
            anchor = "";
            if (string.IsNullOrEmpty(token) || !token.StartsWith(VenuePrefix, StringComparison.Ordinal)) return false;
            string rest = token.Substring(VenuePrefix.Length);
            int dot = rest.IndexOf('.');
            if (dot <= 0 || dot >= rest.Length - 1) return false;
            venue = rest.Substring(0, dot);
            anchor = rest.Substring(dot + 1);
            return true;
        }

        /// <summary>The anchor id of an "Under:" token ("" otherwise).</summary>
        public static string UnderAnchor(string token)
        {
            return token != null && token.StartsWith(UnderPrefix, StringComparison.Ordinal) ? token.Substring(UnderPrefix.Length) : "";
        }

        /// <summary>The cast id of a "Cast:" token ("" otherwise).</summary>
        public static string CastOf(string token)
        {
            return token != null && token.StartsWith(CastPrefix, StringComparison.Ordinal) ? token.Substring(CastPrefix.Length) : "";
        }

        /// <summary>Null when the token is valid, otherwise why not (the validator prints it).</summary>
        public static string PlaceTokenError(string at)
        {
            if (!IsPlaceToken(at)) return "not a place token";
            if (at.StartsWith(VenuePrefix, StringComparison.Ordinal))
            {
                string v, a;
                if (!SplitVenue(at, out v, out a)) return "a venue place is written Venue:<venue>.<anchor>";
                if (!KnownVenue(v)) return "unknown venue '" + v + "'";
                if (!KnownVenueAnchor(v, a)) return "venue '" + v + "' has no anchor '" + a + "'";
                return null;
            }
            if (at.StartsWith(UnderPrefix, StringComparison.Ordinal))
            {
                string a = UnderAnchor(at);
                return UnderheartIds.HasAnchor(a) ? null : "unknown Underheart anchor '" + a + "'";
            }
            if (at.StartsWith(CastPrefix, StringComparison.Ordinal))
            {
                string c = CastOf(at);
                return CastIds04E.Has(c) ? null : "unknown cast member '" + c + "'";
            }
            if (at == ArsenalCase || at == ArsenalLane) return null;
            return "unknown Arsenal place (use Arsenal:case or Arsenal:lane)";
        }

        /// <summary>The region a place lies in: an UnderheartIds region for Under: anchors, "surface" for every other place.</summary>
        public static string RegionOf(string at)
        {
            if (string.IsNullOrEmpty(at) || !at.StartsWith(UnderPrefix, StringComparison.Ordinal)) return "surface";
            string r = UnderheartIds.RegionOfAnchor(UnderAnchor(at));
            return string.IsNullOrEmpty(r) ? "surface" : r;
        }

        /// <summary>True for Region values a campaign quest may name.</summary>
        public static bool KnownRegion(string region)
        {
            return region == "surface" || Array.IndexOf(UnderheartIds.Regions, region) >= 0;
        }

        // ================================================================== conditions

        /// <summary>
        /// Null when a condition is well formed, else why not. Grammar: terms joined by '&amp;' (all must hold), each
        /// optionally negated with '!'. Terms: else, law=Mercy, law=Power, law=none, story_done, story_open, freeroam,
        /// fused, fusion_unlocked, choice:key=value, flag:name, bond:archetype, quest:id, has:material:count. questKnown and
        /// materialKnown check the ids when given (null skips that check).
        /// </summary>
        public static string ConditionError(string cond, Func<string, bool> questKnown, Func<string, bool> materialKnown)
        {
            if (string.IsNullOrEmpty(cond)) return "empty condition";
            string[] terms = cond.Split('&');
            for (int i = 0; i < terms.Length; i++)
            {
                string t = terms[i].Trim();
                if (t != terms[i]) return "no spaces around '&' or inside terms";
                if (t == "else")
                {
                    if (terms.Length > 1) return "'else' must stand alone";
                    continue;
                }
                if (t.StartsWith("!", StringComparison.Ordinal)) t = t.Substring(1);
                string e = TermError(t, questKnown, materialKnown);
                if (e != null) return e;
            }
            return null;
        }

        private static string TermError(string t, Func<string, bool> questKnown, Func<string, bool> materialKnown)
        {
            switch (t)
            {
                case "law=Mercy":
                case "law=Power":
                case "law=none":
                case "story_done":
                case "story_open":
                case "freeroam":
                case "fused":
                case "fusion_unlocked":
                    return null;
            }
            if (t.StartsWith("choice:", StringComparison.Ordinal))
            {
                string kv = t.Substring(7);
                int eq = kv.IndexOf('=');
                if (eq <= 0) return "choice terms are choice:key=value";
                string key = kv.Substring(0, eq), value = kv.Substring(eq + 1);
                if (!ValidChoice(key, value)) return "unknown choice '" + key + "=" + value + "'";
                return null;
            }
            if (t.StartsWith("flag:", StringComparison.Ordinal)) return IsName(t.Substring(5)) ? null : "flag names are lower case letters, digits and underscores";
            if (t.StartsWith("bond:", StringComparison.Ordinal)) return BondIds.Has(t.Substring(5)) ? null : "unknown bond archetype '" + t.Substring(5) + "'";
            if (t.StartsWith("quest:", StringComparison.Ordinal))
            {
                string id = t.Substring(6);
                if (!IsName(id)) return "quest terms are quest:<id>";
                return questKnown == null || questKnown(id) ? null : "unknown quest '" + id + "'";
            }
            if (t.StartsWith("has:", StringComparison.Ordinal))
            {
                string rest = t.Substring(4);
                int colon = rest.LastIndexOf(':');
                int n;
                if (colon <= 0 || !int.TryParse(rest.Substring(colon + 1), out n) || n < 1 || n > 50) return "has terms are has:<material>:<count 1 to 50>";
                string m = rest.Substring(0, colon);
                return materialKnown == null || materialKnown(m) ? null : "unknown material '" + m + "'";
            }
            return "unknown condition '" + t + "'";
        }

        /// <summary>Evaluates a well formed condition (a malformed one is false, except "else").</summary>
        public static bool Evaluate(string cond, CampaignFacts f)
        {
            if (string.IsNullOrEmpty(cond) || f == null) return false;
            if (cond == "else") return true;
            string[] terms = cond.Split('&');
            for (int i = 0; i < terms.Length; i++)
            {
                string t = terms[i].Trim();
                bool negate = t.StartsWith("!", StringComparison.Ordinal);
                if (negate) t = t.Substring(1);
                bool v = Term(t, f);
                if (negate) v = !v;
                if (!v) return false;
            }
            return true;
        }

        private static bool Term(string t, CampaignFacts f)
        {
            switch (t)
            {
                case "law=Mercy": return f.Law == "Mercy";
                case "law=Power": return f.Law == "Power";
                case "law=none": return f.Law != "Mercy" && f.Law != "Power";
                case "story_done": return f.StoryDone;
                case "story_open": return !f.StoryDone;
                case "freeroam": return f.FreeRoam;
                case "fused": return f.Fused;
                case "fusion_unlocked": return f.FusionUnlocked;
            }
            try
            {
                if (t.StartsWith("choice:", StringComparison.Ordinal))
                {
                    string kv = t.Substring(7);
                    int eq = kv.IndexOf('=');
                    return eq > 0 && f.Choice != null && f.Choice(kv.Substring(0, eq)) == kv.Substring(eq + 1);
                }
                if (t.StartsWith("flag:", StringComparison.Ordinal)) return f.Flag != null && f.Flag(t.Substring(5));
                if (t.StartsWith("bond:", StringComparison.Ordinal)) return f.OwnsBond != null && f.OwnsBond(t.Substring(5));
                if (t.StartsWith("quest:", StringComparison.Ordinal)) return f.QuestDone != null && f.QuestDone(t.Substring(6));
                if (t.StartsWith("has:", StringComparison.Ordinal))
                {
                    string rest = t.Substring(4);
                    int colon = rest.LastIndexOf(':');
                    int n;
                    if (colon <= 0 || !int.TryParse(rest.Substring(colon + 1), out n)) return false;
                    return f.Material != null && f.Material(rest.Substring(0, colon)) >= n;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        /// <summary>The phase a Branch jumps to: the first option whose condition holds ("" when none does).</summary>
        public static string BranchTarget(IList<ChoiceOption> options, CampaignFacts f)
        {
            if (options == null) return "";
            for (int i = 0; i < options.Count; i++)
            {
                ChoiceOption o = options[i];
                if (o != null && Evaluate(o.When, f)) return o.Phase ?? "";
            }
            return "";
        }

        /// <summary>Indices of the options to offer: those with no condition or whose condition holds. Never empty for a non-empty list (the last option is the fallback).</summary>
        public static List<int> VisibleOptions(IList<ChoiceOption> options, CampaignFacts f)
        {
            List<int> list = new List<int>();
            if (options == null || options.Count == 0) return list;
            for (int i = 0; i < options.Count; i++)
            {
                ChoiceOption o = options[i];
                if (o == null) continue;
                if (string.IsNullOrEmpty(o.When) || Evaluate(o.When, f)) list.Add(i);
            }
            if (list.Count == 0) list.Add(options.Count - 1);
            return list;
        }

        // ================================================================== checkpoints and choices

        public static string CheckpointKey(string questId, string phaseId)
        {
            return (questId ?? "") + ":" + (phaseId ?? "");
        }

        /// <summary>The committed checkpoint phase of a quest ("" when none).</summary>
        public static string CheckpointPhase(IList<string> checkpoints, string questId)
        {
            if (checkpoints == null || string.IsNullOrEmpty(questId)) return "";
            string prefix = questId + ":";
            for (int i = checkpoints.Count - 1; i >= 0; i--)
            {
                string c = checkpoints[i];
                if (c != null && c.StartsWith(prefix, StringComparison.Ordinal)) return c.Substring(prefix.Length);
            }
            return "";
        }

        /// <summary>Commits a quest's checkpoint (replacing any earlier one for the same quest). True when it changed.</summary>
        public static bool CommitCheckpoint(List<string> checkpoints, string questId, string phaseId)
        {
            if (checkpoints == null || string.IsNullOrEmpty(questId) || string.IsNullOrEmpty(phaseId)) return false;
            string key = CheckpointKey(questId, phaseId);
            string prefix = questId + ":";
            bool had = false;
            for (int i = checkpoints.Count - 1; i >= 0; i--)
            {
                string c = checkpoints[i];
                if (c == null || !c.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (c == key && !had) { had = true; continue; }
                checkpoints.RemoveAt(i);
            }
            if (had) return false;
            checkpoints.Add(key);
            return true;
        }

        /// <summary>Forgets a quest's checkpoint (on completion). True when one was removed.</summary>
        public static bool ClearCheckpoint(List<string> checkpoints, string questId)
        {
            if (checkpoints == null || string.IsNullOrEmpty(questId)) return false;
            string prefix = questId + ":";
            int n = checkpoints.RemoveAll(c => c != null && c.StartsWith(prefix, StringComparison.Ordinal));
            return n > 0;
        }

        public static bool ValidChoice(string key, string value)
        {
            return Array.IndexOf(QuestIds04E.ChoiceKeys, key) >= 0 && Array.IndexOf(QuestIds04E.ChoiceValues(key), value) >= 0;
        }

        /// <summary>The stored value of a choice ("" when not made). Records are "key=value".</summary>
        public static string GetChoice(IList<string> choices, string key)
        {
            if (choices == null || string.IsNullOrEmpty(key)) return "";
            string prefix = key + "=";
            for (int i = 0; i < choices.Count; i++)
            {
                string c = choices[i];
                if (c != null && c.StartsWith(prefix, StringComparison.Ordinal)) return c.Substring(prefix.Length);
            }
            return "";
        }

        /// <summary>Records a choice. The first answer sticks unless overwrite is true. Invalid pairs are refused. True when stored.</summary>
        public static bool SetChoice(List<string> choices, string key, string value, bool overwrite)
        {
            if (choices == null || !ValidChoice(key, value)) return false;
            string prefix = key + "=";
            for (int i = 0; i < choices.Count; i++)
            {
                string c = choices[i];
                if (c == null || !c.StartsWith(prefix, StringComparison.Ordinal)) continue;
                if (!overwrite) return false;
                choices[i] = prefix + value;
                return true;
            }
            choices.Add(prefix + value);
            return true;
        }

        /// <summary>True for a valid flag or quest-style name (lower case letters, digits, underscores, 1 to 48).</summary>
        public static bool IsName(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length > 48) return false;
            foreach (char c in s) if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) return false;
            return true;
        }

        /// <summary>The flag a finished Choir mechanism leaves ("mech_crown_valve").</summary>
        public static string MechanismFlag(string mechanismId)
        {
            return "mech_" + (mechanismId ?? "");
        }

        // ================================================================== claims

        /// <summary>The default once-per-token entitlement for a quest Claim step.</summary>
        public static string DefaultClaimToken(string questId, string rewardId)
        {
            return "quest:" + (questId ?? "") + ":" + (rewardId ?? "");
        }

        /// <summary>
        /// The token a boss reward must use (the boss grants it itself on victory, so the quest's Claim never grants a second
        /// copy), or null for rewards that are not boss rewards.
        /// </summary>
        public static string BossTokenFor(string rewardId)
        {
            switch (rewardId)
            {
                case "uh_descent_piece": return UnderBossIds.Token(UnderBossIds.ValveGuardian);
                case "uh_matron_aspect": return UnderBossIds.Token(UnderBossIds.CinderMatron);
                case "uh_matron_gear_choice": return UnderBossIds.Token(UnderBossIds.CinderMatron) + ":gear";
                case "uh_vault_module": return UnderBossIds.Token(UnderBossIds.Vaultbreaker);
                case "uh_regent_pinnacle": return UnderBossIds.Token(UnderBossIds.KilnRegent);
                case "uh_bond_awaken": return UnderBossIds.Token(UnderBossIds.KilnRegent) + ":bond";
                default: return null;
            }
        }

        /// <summary>The token a Claim step uses: its own Token, else the boss token for boss rewards, else the quest default.</summary>
        public static string ClaimToken(string questId, QuestStep s)
        {
            if (s == null) return "";
            if (!string.IsNullOrEmpty(s.Token)) return s.Token;
            string boss = BossTokenFor(s.Reward);
            return boss ?? DefaultClaimToken(questId, s.Reward);
        }

        /// <summary>A short readable name for a reward id (offer cards and the journal; the Builds catalog owns the real text).</summary>
        public static string RewardLabel(string id)
        {
            switch (id)
            {
                case "uh_ward_starter": return "Starter ward (Lattice Shield buckler)";
                case "uh_kit_wardkeeper": return "Wardkeeper starter kit";
                case "uh_kit_conduit": return "Conduit starter kit";
                case "uh_kit_riftstrider": return "Riftstrider starter kit";
                case "uh_gear_choice_early": return "Early gear choice";
                case "uh_descent_piece": return "Guaranteed build piece";
                case "uh_coupler_part": return "Crystal coupler part";
                case "uh_fusion_license": return "Fusion entitlement";
                case "uh_trim_choice": return "Chassis trim choice";
                case "uh_augment_choice": return "Augment choice";
                case "uh_recipe_coolant": return "Coolant Forge recipe";
                case "uh_matron_aspect": return "Ember Mantle ward aspect";
                case "uh_matron_gear_choice": return "High-quality gear choice";
                case "uh_vault_module": return "Fracture Lattice focus module";
                case "uh_loadout_slot2": return "Second loadout preset";
                case "uh_regent_pinnacle": return "Pinnacle focus or ward choice";
                case "uh_bond_awaken": return "Final bond evolution";
                case "uh_frostglass_aspect": return "Frostglass ward aspect";
                case "uh_volt_aspect": return "Volt Lattice ward aspect";
                case "uh_void_aspect": return "Void Screen ward aspect";
                case "uh_trim_second": return "Second chassis trim";
                case "uh_handling_sidegrade": return "Handling sidegrade";
                case "uh_chassis_cruiser": return "Cruiser chassis class";
                case "uh_care_kit": return "Care kit";
                case "uh_ward_visual": return "Ward visual";
                case "uh_map_knowledge": return "Map knowledge";
                case "uh_elite_choice": return "High-value equipment choice";
                case "uh_home_display": return "Home display";
                case "uh_utility_recipe": return "Utility Forge recipe";
                case "uh_bond_storm_evolve": return "Stormskimmer evolution";
                case "uh_bond_ash_evolve": return "Ashshell Warden evolution";
                case "uh_bond_drake_evolve": return "Drakeling evolution";
                default: return id ?? "";
            }
        }

        public static string DifficultyLabel(int d)
        {
            switch (d)
            {
                case 1: return "Gentle";
                case 2: return "Moderate";
                case 3: return "Demanding";
                case 4: return "Hard";
                case 5: return "Extreme";
                default: return "Unrated";
            }
        }

        /// <summary>"Ember Mantle ward aspect, Fusion entitlement" for a list of reward ids ("" when empty).</summary>
        public static string ClaimsText(IList<string> claims)
        {
            if (claims == null || claims.Count == 0) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < claims.Count; i++)
            {
                if (string.IsNullOrEmpty(claims[i])) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(RewardLabel(claims[i]));
            }
            return sb.ToString();
        }

        /// <summary>Parses "#RRGGBB" into 0..1 channels. False when malformed.</summary>
        public static bool ParseColor(string hex, out float r, out float g, out float b)
        {
            r = g = b = 1f;
            if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#') return false;
            int v = 0;
            for (int i = 1; i < 7; i++)
            {
                char c = hex[i];
                int d = c >= '0' && c <= '9' ? c - '0' : (c >= 'a' && c <= 'f' ? c - 'a' + 10 : (c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1));
                if (d < 0) return false;
                v = v * 16 + d;
            }
            r = ((v >> 16) & 255) / 255f;
            g = ((v >> 8) & 255) / 255f;
            b = (v & 255) / 255f;
            return true;
        }

        /// <summary>The main quest that opens contracts in a region (the board never sends the player somewhere the story has not reached).</summary>
        public static string RegionOpenedBy(string region)
        {
            switch (region)
            {
                case UnderheartIds.ServiceLevel:
                case UnderheartIds.LanternRefuge:
                case UnderheartIds.EmberCanals: return "uh05_04_descent";
                case UnderheartIds.PyreGardens: return "uh05_07_gardens";
                case UnderheartIds.CrucibleWorkshops: return "uh05_10_road";
                case UnderheartIds.AshReliquary: return "uh05_13_crown";
                case UnderheartIds.FoundryCrown: return "uh05_15_regent";
                default: return "";
            }
        }
    }
}
