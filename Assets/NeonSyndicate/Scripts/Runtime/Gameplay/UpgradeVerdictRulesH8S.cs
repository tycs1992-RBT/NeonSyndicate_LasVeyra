using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The overall call on an item next to the one it is compared with or would replace.</summary>
    public enum UpgradeVerdictH8S
    {
        /// <summary>Nothing to compare with (or a blade against a gun): no call is made.</summary>
        None = 0,
        /// <summary>Clearly better on the main number (at least UpgradeVerdictRulesH8S.BandPercent).</summary>
        Upgrade = 1,
        /// <summary>Within the band either way.</summary>
        Similar = 2,
        /// <summary>Clearly worse on the main number.</summary>
        Weaker = 3,
        /// <summary>Gear only: nothing is worn in that slot, so the piece only adds to the build.</summary>
        EmptySlot = 4,
        /// <summary>Gear only: this is the piece already worn.</summary>
        Equipped = 5
    }

    /// <summary>A gun call: the verdict and the numbers behind it.</summary>
    public struct GunVerdictH8S
    {
        public UpgradeVerdictH8S Verdict;
        /// <summary>The main number of the candidate and of the compared weapon (damage per second, or blade damage).</summary>
        public float Candidate, Baseline;
        /// <summary>The rounded signed change of the main number, in percent.</summary>
        public int Percent;
        /// <summary>True when both are regional blades (the main number is the base blade damage).</summary>
        public bool Blade;
    }

    /// <summary>A gear piece call against the piece worn in the same slot, by item score.</summary>
    public struct GearVerdictH8S
    {
        public UpgradeVerdictH8S Verdict;
        /// <summary>The candidate's item score (GearScoreRules.GearScore).</summary>
        public int Score;
        /// <summary>The worn piece's item score (0 when the slot is empty).</summary>
        public int WornScore;
        /// <summary>Score minus WornScore: what equipping it adds to the gear score.</summary>
        public int Delta;
    }

    /// <summary>One affix that equipping a piece would change: its total on the worn piece and on the candidate.</summary>
    public struct AffixChangeH8S
    {
        public AffixType Type;
        public float Before;
        public float After;
        /// <summary>True when the candidate carries more of it.</summary>
        public bool Gain { get { return After > Before; } }
    }

    /// <summary>One set whose worn piece count equipping a piece would change, and the bonus that turns on or off.</summary>
    public struct SetChangeH8S
    {
        public int Set;
        public int PiecesBefore;
        public int PiecesAfter;
        /// <summary>The bonus tier (2, 3 or 4) that turns on or off, or 0 when no bonus changes.</summary>
        public int Tier;
        /// <summary>True when Tier turns on, false when it turns off (meaningless when Tier is 0).</summary>
        public bool TurnsOn;
    }

    /// <summary>How far the gear score has come inside its city tier, and what is left to the next one.</summary>
    public struct TierProgressH8S
    {
        public int Score;
        public int Tier;
        /// <summary>The gear score where the current tier starts.</summary>
        public int Floor;
        /// <summary>The gear score of the next tier (-1 at the top).</summary>
        public int Next;
        /// <summary>Gear score still needed for the next tier (0 at the top).</summary>
        public int ToGo;
        /// <summary>0 at the start of the tier, 1 at the next one (1 at the top tier).</summary>
        public float Fraction;
        public bool Top;
    }

    /// <summary>
    /// H8S progression readability: the plain rules behind the "is this better?" calls. A loot gun is judged on the
    /// card's first row (heat-cycle damage per second, or base blade damage between two regional blades) against what
    /// the card compares it with; a gear piece is judged on its item score against the piece worn in the same slot, and
    /// lists the affixes and set bonuses equipping it would change. Also the gear score after a swap and the progress
    /// toward the next city tier. Presentation only: it reads the existing generators and tables and changes nothing.
    /// Engine-free, tested by ProgressionReadoutH8STests.
    /// </summary>
    public static class UpgradeVerdictRulesH8S
    {
        /// <summary>Changes smaller than this many percent either way read as "about the same".</summary>
        public const int BandPercent = 3;
        /// <summary>The arrow between an old and a new value in readouts.</summary>
        public const string To = " → ";
        private const float MaxPercent = 9999f;

        // ------------------------------------------------------------------ the call

        /// <summary>The rounded signed change from baseline to candidate in percent (0 without a positive baseline).</summary>
        public static int PercentChange(float candidate, float baseline)
        {
            if (!(baseline > 0f) || float.IsNaN(candidate) || float.IsInfinity(candidate) || float.IsInfinity(baseline)) return 0;
            float pct = Mathf.Clamp((candidate - baseline) / baseline * 100f, -MaxPercent, MaxPercent);
            return Mathf.RoundToInt(pct);
        }

        /// <summary>
        /// Upgrade, Similar or Weaker from the rounded percent change (so the number shown always agrees with the call);
        /// None without a positive baseline or with a broken candidate.
        /// </summary>
        public static UpgradeVerdictH8S Judge(float candidate, float baseline)
        {
            if (!(baseline > 0f) || !(candidate >= 0f) || float.IsInfinity(candidate) || float.IsInfinity(baseline)) return UpgradeVerdictH8S.None;
            int pct = PercentChange(candidate, baseline);
            if (pct >= BandPercent) return UpgradeVerdictH8S.Upgrade;
            if (pct <= -BandPercent) return UpgradeVerdictH8S.Weaker;
            return UpgradeVerdictH8S.Similar;
        }

        /// <summary>
        /// A gun card's call against the compared card, on the same number as the card's first row: sustained (heat-cycle)
        /// damage per second for guns, base blade damage between two regional blades. A blade against a gun is None.
        /// </summary>
        public static GunVerdictH8S JudgeGun(LootArmory.CardStats candidate, LootArmory.CardStats compared)
        {
            GunVerdictH8S v = new GunVerdictH8S();
            if (!candidate.Valid || !compared.Valid || candidate.RegionalMelee != compared.RegionalMelee) return v;
            v.Blade = candidate.RegionalMelee;
            v.Candidate = v.Blade ? candidate.Damage : candidate.SustainedDps;
            v.Baseline = v.Blade ? compared.Damage : compared.SustainedDps;
            v.Verdict = Judge(v.Candidate, v.Baseline);
            v.Percent = v.Verdict == UpgradeVerdictH8S.None ? 0 : PercentChange(v.Candidate, v.Baseline);
            return v;
        }

        /// <summary>A loot gun's item score (what it adds to the gear score from a loot slot); -1 for no gun.</summary>
        public static int GunItemScore(LootGun g)
        {
            return g != null ? GearScoreRules.GunScore(g.Level, g.Ascensions, g.Rarity) : -1;
        }

        /// <summary>A gear piece's item score; -1 for no piece.</summary>
        public static int GearItemScore(GearItem it)
        {
            return it != null ? GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity) : -1;
        }

        /// <summary>
        /// A gear piece against the piece worn in its slot (null for an empty slot), by item score. Upgrade or Weaker
        /// needs a change of at least BandPercent of the worn score (never less than one point). A worn piece from
        /// another slot gives None.
        /// </summary>
        public static GearVerdictH8S JudgeGear(GearItem candidate, GearItem worn)
        {
            GearVerdictH8S v = new GearVerdictH8S();
            if (candidate == null) return v;
            v.Score = GearItemScore(candidate);
            if (worn == candidate)
            {
                v.Verdict = UpgradeVerdictH8S.Equipped;
                v.WornScore = v.Score;
                return v;
            }
            if (worn == null)
            {
                v.Verdict = UpgradeVerdictH8S.EmptySlot;
                v.Delta = v.Score;
                return v;
            }
            if (worn.Slot != candidate.Slot) return new GearVerdictH8S();
            v.WornScore = GearItemScore(worn);
            v.Delta = v.Score - v.WornScore;
            int band = Mathf.Max(1, Mathf.RoundToInt(v.WornScore * BandPercent / 100f));
            v.Verdict = v.Delta >= band ? UpgradeVerdictH8S.Upgrade : (v.Delta <= -band ? UpgradeVerdictH8S.Weaker : UpgradeVerdictH8S.Similar);
            return v;
        }

        // ------------------------------------------------------------------ what equipping a gear piece changes

        /// <summary>
        /// Every affix whose total differs between the worn piece (null: none) and the candidate, in AffixType order,
        /// upgrades included (GearItem.Get). Fills into (cleared first) and returns the count.
        /// </summary>
        public static int AffixChanges(GearItem candidate, GearItem worn, List<AffixChangeH8S> into)
        {
            if (into == null) return 0;
            into.Clear();
            if (candidate == null || candidate == worn) return 0;
            for (int i = 0; i < (int)AffixType.Count; i++)
            {
                AffixType t = (AffixType)i;
                float before = worn != null ? worn.Get(t) : 0f;
                float after = candidate.Get(t);
                if (Mathf.Abs(after - before) > 1e-4f) into.Add(new AffixChangeH8S { Type = t, Before = before, After = after });
            }
            return into.Count;
        }

        /// <summary>
        /// The sets whose worn count equipping candidate in place of worn (null: empty slot) would change, from the worn
        /// counts indexed by set: the outgoing piece's set first, then the incoming one's. Each says the bonus tier that
        /// turns on or off (GearBuildRules.TierOf, so the classic sets have no 3 piece bonus). Fills into (cleared first).
        /// </summary>
        public static int SetChanges(IList<int> wornCounts, GearItem candidate, GearItem worn, List<SetChangeH8S> into)
        {
            if (into == null) return 0;
            into.Clear();
            if (candidate == null || candidate == worn) return 0;
            int outSet = worn != null ? worn.Set : -1;
            int inSet = candidate.Set;
            if (outSet == inSet) return 0;
            if (outSet >= 0) into.Add(SetChange(wornCounts, outSet, -1));
            if (inSet >= 0) into.Add(SetChange(wornCounts, inSet, 1));
            return into.Count;
        }

        private static SetChangeH8S SetChange(IList<int> wornCounts, int set, int step)
        {
            int before = wornCounts != null && set >= 0 && set < wornCounts.Count ? Mathf.Max(0, wornCounts[set]) : 0;
            int after = Mathf.Max(0, before + step);
            int tb = GearBuildRules.TierOf(set, before);
            int ta = GearBuildRules.TierOf(set, after);
            SetChangeH8S c = new SetChangeH8S { Set = set, PiecesBefore = before, PiecesAfter = after };
            if (ta > tb)
            {
                c.Tier = ta;
                c.TurnsOn = true;
            }
            else if (ta < tb)
            {
                c.Tier = tb;
                c.TurnsOn = false;
            }
            return c;
        }

        /// <summary>The smallest piece count above pieces that turns on a new bonus of the set (2, 3 or 4), or -1.</summary>
        public static int NextBonusPieces(int set, int pieces)
        {
            int now = GearBuildRules.TierOf(set, pieces);
            for (int k = Mathf.Max(0, pieces) + 1; k <= 4; k++) if (GearBuildRules.TierOf(set, k) > now) return k;
            return -1;
        }

        // ------------------------------------------------------------------ gear score and city tier

        /// <summary>The gear score after swapping one item for another (outgoing -1 or 0 for an empty slot).</summary>
        public static int ScoreAfterSwap(int total, int outgoingItemScore, int incomingItemScore)
        {
            return Mathf.Max(0, Mathf.Max(0, total) - Mathf.Max(0, outgoingItemScore) + Mathf.Max(0, incomingItemScore));
        }

        /// <summary>Where a gear score sits in its city tier (GearScoreRules thresholds) and what is left to the next.</summary>
        public static TierProgressH8S TierProgress(int score)
        {
            TierProgressH8S p = new TierProgressH8S();
            p.Score = Mathf.Max(0, score);
            p.Tier = GearScoreRules.TierFor(p.Score);
            p.Floor = GearScoreRules.TierThreshold[Mathf.Clamp(p.Tier - 1, 0, GearScoreRules.TierThreshold.Length - 1)];
            p.Next = GearScoreRules.NextTierScore(p.Tier);
            p.Top = p.Next < 0;
            if (p.Top)
            {
                p.Fraction = 1f;
                p.ToGo = 0;
            }
            else
            {
                p.ToGo = Mathf.Max(0, p.Next - p.Score);
                p.Fraction = Mathf.Clamp01((p.Score - p.Floor) / (float)Mathf.Max(1, p.Next - p.Floor));
            }
            return p;
        }

        // ------------------------------------------------------------------ words

        /// <summary>"+18%", "-12%" or "0%".</summary>
        public static string SignedPercent(int pct)
        {
            return pct > 0 ? "+" + pct + "%" : (pct < 0 ? "-" + (-pct) + "%" : "0%");
        }

        /// <summary>"+4", "-8" or "0".</summary>
        public static string Signed(int n)
        {
            return n > 0 ? "+" + n : n.ToString();
        }

        /// <summary>
        /// The bold headline of a gun call, one short line: "UPGRADE +18% damage per second", "WEAKER -12% damage per
        /// second", "ABOUT THE SAME (within 3%)"; "blade damage" between two regional blades (empty for None).
        /// </summary>
        public static string GunHeadline(GunVerdictH8S v)
        {
            string basis = v.Blade ? " blade damage" : " damage per second";
            switch (v.Verdict)
            {
                case UpgradeVerdictH8S.Upgrade: return "UPGRADE " + SignedPercent(v.Percent) + basis;
                case UpgradeVerdictH8S.Weaker: return "WEAKER " + SignedPercent(v.Percent) + basis;
                case UpgradeVerdictH8S.Similar: return "ABOUT THE SAME (within " + BandPercent + "%)";
                default: return "";
            }
        }

        /// <summary>
        /// The small line under a gun call: what was compared and the item scores, for example "vs Coil Pistol (slot 2).
        /// Item score 26 vs 22." itemScore or comparedScore below 0 leaves that part out.
        /// </summary>
        public static string GunDetail(GunVerdictH8S v, string comparedLabel, int itemScore, int comparedScore)
        {
            string text = "vs " + (string.IsNullOrEmpty(comparedLabel) ? "the compared weapon" : comparedLabel) + ".";
            if (itemScore >= 0) text += " Item score " + itemScore + (comparedScore >= 0 ? " vs " + comparedScore : "") + ".";
            return text;
        }

        /// <summary>The short badge of a gear call for the pickup toast: "UPGRADE +12", "NEW SLOT", "WEAKER -8", "SAME SCORE".</summary>
        public static string GearBadge(GearVerdictH8S v)
        {
            switch (v.Verdict)
            {
                case UpgradeVerdictH8S.Upgrade: return "UPGRADE " + Signed(v.Delta);
                case UpgradeVerdictH8S.Weaker: return "WEAKER " + Signed(v.Delta);
                case UpgradeVerdictH8S.Similar: return "SAME SCORE";
                case UpgradeVerdictH8S.EmptySlot: return "NEW SLOT +" + v.Score;
                case UpgradeVerdictH8S.Equipped: return "EQUIPPED";
                default: return "";
            }
        }

        /// <summary>The GEAR tab's score line for a piece against the worn one ("Item score 45: 12 more than your Sable Shell (33).").</summary>
        public static string GearScoreLine(GearVerdictH8S v, string slotName, string wornName)
        {
            string worn = string.IsNullOrEmpty(wornName) ? "your " + slotName : "your " + wornName;
            switch (v.Verdict)
            {
                case UpgradeVerdictH8S.Upgrade: return "UPGRADE: item score " + v.Score + ", " + v.Delta + " more than " + worn + " (" + v.WornScore + ").";
                case UpgradeVerdictH8S.Weaker: return "WEAKER: item score " + v.Score + ", " + (-v.Delta) + " less than " + worn + " (" + v.WornScore + ").";
                case UpgradeVerdictH8S.Similar: return "ABOUT THE SAME: item score " + v.Score + " against " + v.WornScore + " for " + worn + ".";
                case UpgradeVerdictH8S.EmptySlot: return "NEW SLOT: nothing is worn as " + slotName + "; equipping adds " + v.Score + " gear score.";
                case UpgradeVerdictH8S.Equipped: return "Worn now: item score " + v.Score + ".";
                default: return "";
            }
        }

        /// <summary>One affix change in words: "Gains +12% gun damage", "Loses +45 max health", "Crit chance: +6% → +9%".</summary>
        public static string AffixChangeLine(AffixChangeH8S c)
        {
            if (c.Before <= 1e-4f) return "Gains " + GearSystem.AffixText(c.Type, c.After);
            if (c.After <= 1e-4f) return "Loses " + GearSystem.AffixText(c.Type, c.Before);
            return GearSystem.AffixText(c.Type, c.Before) + To + GearSystem.AffixText(c.Type, c.After);
        }

        /// <summary>
        /// One set change in words, from GearSystem's set names and bonus texts: "Longsight Lattice 1 → 2 of 4: turns ON
        /// +12% gun damage beyond 25 m", or the next bonus to aim for when nothing turns on or off.
        /// </summary>
        public static string SetChangeLine(SetChangeH8S c)
        {
            string name = c.Set >= 0 && c.Set < GearSystem.SetNames.Length ? GearSystem.SetNames[c.Set] : "Set " + c.Set;
            string counts = name + " " + c.PiecesBefore + To + c.PiecesAfter + " of 4";
            if (c.Tier > 0) return counts + ": " + c.Tier + " piece bonus turns " + (c.TurnsOn ? "ON: " : "OFF: ") + GearSystem.SetBonus(c.Set, c.Tier);
            int next = NextBonusPieces(c.Set, c.PiecesAfter);
            if (next < 0) return counts + ": every bonus stays as it is.";
            return counts + ": " + next + " pieces turn on " + GearSystem.SetBonus(c.Set, GearBuildRules.TierOf(c.Set, next));
        }

        /// <summary>"38 more gear score reaches CITY TIER 3: SHARD SEASON." or the top tier note.</summary>
        public static string TierProgressLine(TierProgressH8S p)
        {
            if (p.Top) return "Top city tier: " + GearScoreRules.TierName(p.Tier) + ".";
            return p.ToGo + " more gear score reaches CITY TIER " + (p.Tier + 1) + ": " + GearScoreRules.TierName(p.Tier + 1).ToUpperInvariant() + ".";
        }

        /// <summary>
        /// What a Forge change did, appended to its status: "Heat-cycle damage per second 112 → 131 (+17%), item score
        /// 22 → 26." (each part is left out when its rounded number did not change or no call can be made).
        /// </summary>
        public static string ChangeSummary(GunVerdictH8S v, int scoreBefore, int scoreAfter)
        {
            string text = "";
            // An unchanged number (as an element infusion leaves it) is not worth saying.
            if (v.Verdict != UpgradeVerdictH8S.None && Mathf.RoundToInt(v.Baseline) != Mathf.RoundToInt(v.Candidate))
                text =(v.Blade ? "Base blade damage " : "Heat-cycle damage per second ") + Mathf.RoundToInt(v.Baseline) + To + Mathf.RoundToInt(v.Candidate) + " (" + SignedPercent(v.Percent) + ")";
            if (scoreBefore >= 0 && scoreAfter >= 0 && scoreAfter != scoreBefore)
                text += (text.Length > 0 ? ", item score " : "Item score ") + scoreBefore + To + scoreAfter;
            return text.Length > 0 ? text + "." : "";
        }

        /// <summary>
        /// The Forge preview's score line: "Item score 22 → 26. In loot slot 2: gear score 212 → 216." or, for a gun
        /// in the backpack, that it counts toward the gear score once equipped. slotIndex below 0 means the backpack.
        /// </summary>
        public static string ForgeScoreLine(int scoreBefore, int scoreAfter, int slotIndex, int gearBefore, int gearAfter)
        {
            string text = "Item score " + scoreBefore + (scoreAfter != scoreBefore ? To + scoreAfter : " (unchanged)") + ".";
            if (slotIndex >= 0) return text + " In loot slot " + (slotIndex + 1) + ": gear score " + gearBefore + (gearAfter != gearBefore ? To + gearAfter : " (unchanged)") + ".";
            return text + " It counts toward the gear score once equipped.";
        }

        /// <summary>
        /// The hold-to-equip what-if under the pickup card: "Equipping: loot slot 2 (empty), gear score 212 → 236."
        /// with "reaches CITY TIER 3" when the swap crosses a tier.
        /// </summary>
        public static string EquipWhatIf(int slotIndex, string replacedName, int before, int after)
        {
            return EquipWhatIf(slotIndex, replacedName, before, after, new GunVerdictH8S());
        }

        /// <summary>The short form of a call for one-line readouts: "UPGRADE +18%", "ABOUT THE SAME", "WEAKER -12%" (empty for None).</summary>
        public static string ShortCall(GunVerdictH8S v)
        {
            switch (v.Verdict)
            {
                case UpgradeVerdictH8S.Upgrade: return "UPGRADE " + SignedPercent(v.Percent);
                case UpgradeVerdictH8S.Weaker: return "WEAKER " + SignedPercent(v.Percent);
                case UpgradeVerdictH8S.Similar: return "ABOUT THE SAME";
                default: return "";
            }
        }

        /// <summary>
        /// The what-if with the new gun's own call against the gun it would replace, so the line never leans on the card's
        /// comparison (which may be the gun in hand, not the slot's gun): "Equipping: loot slot 1 (replaces Stormcoil:
        /// WEAKER -12%), gear score 212 → 204." replacedCall None leaves the call out.
        /// </summary>
        public static string EquipWhatIf(int slotIndex, string replacedName, int before, int after, GunVerdictH8S replacedCall)
        {
            string call = ShortCall(replacedCall);
            string slot = "loot slot " + (slotIndex + 1) + (string.IsNullOrEmpty(replacedName) ? " (empty)" : " (replaces " + replacedName + (call.Length > 0 ? ": " + call : "") + ")");
            string text = "Equipping: " + slot + ", gear score " + before + To + after;
            int tb = GearScoreRules.TierFor(before), ta = GearScoreRules.TierFor(after);
            if (ta > tb) text += ", reaches CITY TIER " + ta;
            else if (ta < tb) text += ", drops to CITY TIER " + ta;
            return text + ".";
        }
    }
}
