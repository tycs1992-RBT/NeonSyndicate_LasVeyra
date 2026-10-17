using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One material and count in a previewed bond upgrade cost (ids are real MaterialDb ids).</summary>
    public struct BondCost
    {
        public string Material;
        public int Count;

        public BondCost(string material, int count)
        {
            Material = material;
            Count = count;
        }
    }

    /// <summary>The Ashshell Warden's brace for one use: how long, how wide, how much it takes and how much it blocks.</summary>
    public struct BraceStats
    {
        public float Duration;
        /// <summary>Half angle of the protected cone around the brace direction, degrees.</summary>
        public float HalfAngle;
        /// <summary>Share of a hit from inside the cone the shell takes (0..1).</summary>
        public float Absorb;
        /// <summary>Total damage the shell takes before it breaks.</summary>
        public float Capacity;
        /// <summary>Share of the absorbed damage that wears the warden's own health.</summary>
        public float Strain;
        /// <summary>The player must stay this close to the warden for the brace to cover them.</summary>
        public float CoverRange;
        /// <summary>Kiln Heart: share of hazard heat on the player the warden shades while braced (0 without it).</summary>
        public float HeatShade;
    }

    /// <summary>What a care or tuning interaction did (pure result, the runtime shows it).</summary>
    public struct CareOutcome
    {
        public bool Performed;
        public int TrustGained;
        public bool RankChanged;
        public int NewRank;
        public bool Revived;
        public bool OnCooldown;
        public string Message;
    }

    /// <summary>
    /// The numbers and rules behind bonds: rank thresholds (stable ids 0 Rescued to 4 Awakened), trust sources and
    /// their caps, the care cooldown, health and recovery, assist power and cooldowns per archetype, rank, tuning and
    /// specialization, the Harmonized specializations, the crystal tuning costs from real materials, name hygiene and
    /// the no-refill rule for fusion snapshots. Engine-free and tested by Bonds04ETests.
    /// </summary>
    public static class BondRules
    {
        public const int MaxTrust = 1000;
        /// <summary>Trust that unlocks each rank by itself. Rescued becomes Bonded through its first care (or a grant);
        /// Awakened also needs an awaken entitlement (RaiseRank from a reward), trust alone stops at Harmonized.</summary>
        public static readonly int[] RankTrust = { 0, 0, 150, 450, 800 };

        public const int TrustCare = 25;
        public const int TrustMainQuest = 40;
        public const int TrustSideQuest = 30;
        public const int TrustContract = 8;
        public const int TrustTuning = 60;
        /// <summary>Gameplay seconds between rewarded care interactions (repeats during it give nothing).</summary>
        public const float CareCooldown = 240f;
        /// <summary>Gameplay seconds between contract trust gains.</summary>
        public const float ContractTrustCooldown = 600f;

        /// <summary>A downed companion lies down this long, then retreats to rest out of sight.</summary>
        public const float DownedRetreatSeconds = 4f;
        /// <summary>Gameplay seconds a downed companion rests before it comes back by itself (care brings it back at once).</summary>
        public const float RecoverSeconds = 75f;
        /// <summary>Health share a companion comes back with after resting it off.</summary>
        public const float RecoverHealth = 0.5f;
        /// <summary>Out of combat a companion mends this share of its health per second after RegenDelay without harm.</summary>
        public const float RegenPerSecond = 0.02f;
        public const float RegenDelay = 6f;

        public const int MaxTuning = 3;
        /// <summary>Combat assist range from the player, metres (the brief: a valid hostile within 25 m).</summary>
        public const float AssistRange = 25f;
        /// <summary>Cooldown of an automatic or manual recall to the player's side.</summary>
        public const float RecallCooldown = 6f;
        public const float ManualRecallCooldown = 3f;
        public const int MaxNameLength = 16;

        // ================================================================== ranks and trust

        public static string RankName(int rank)
        {
            return BondIds.RankNames[Mathf.Clamp(rank, 0, BondIds.RankNames.Length - 1)];
        }

        public static int ClampRank(int rank) { return Mathf.Clamp(rank, BondIds.RankStray, BondIds.RankAwakened); }

        /// <summary>The trust a companion of a rank has at least (raising a rank lifts trust to it).</summary>
        public static int TrustFloor(int rank) { return RankTrust[ClampRank(rank)]; }

        /// <summary>The rank trust alone reaches from the current rank: never lower, never past Harmonized, and a Rescued
        /// companion waits for its first care.</summary>
        public static int RankForTrust(int trust, int currentRank)
        {
            int rank = ClampRank(currentRank);
            if (rank == BondIds.RankStray) return rank;
            for (int r = BondIds.RankAttuned; r <= BondIds.RankHarmonized; r++)
                if (trust >= RankTrust[r] && r > rank) rank = r;
            return rank;
        }

        /// <summary>Trust needed for the next rank trust can reach, or -1 when trust no longer raises the rank.</summary>
        public static int NextTrustTarget(int rank)
        {
            if (rank < BondIds.RankBonded || rank >= BondIds.RankHarmonized) return -1;
            return RankTrust[rank + 1];
        }

        /// <summary>What reaching a rank unlocks (plain words for the page and the rank-up banner).</summary>
        public static string RankUnlock(int rank)
        {
            switch (ClampRank(rank))
            {
                case BondIds.RankStray: return "Follows, waits and comes when called.";
                case BondIds.RankBonded: return "Combat assist unlocked.";
                case BondIds.RankAttuned: return "First safe fusion with your personal chassis.";
                case BondIds.RankHarmonized: return "Choose a specialization.";
                default: return "Awakened form: final visual evolution and stronger assists.";
            }
        }

        /// <summary>Adds trust (capped) and lets the rank follow it. Returns the trust actually added.</summary>
        public static int AddTrust(CompanionRecord r, int amount)
        {
            if (r == null || amount <= 0) return 0;
            int before = r.Trust;
            r.Trust = Mathf.Clamp(r.Trust + amount, 0, MaxTrust);
            r.Rank = RankForTrust(r.Trust, r.Rank);
            return r.Trust - before;
        }

        /// <summary>Raises a rank to at least minRank (never lowers) and lifts trust to that rank's floor. True when the rank changed.</summary>
        public static bool RaiseRank(CompanionRecord r, int minRank)
        {
            if (r == null) return false;
            int target = ClampRank(minRank);
            if (target <= r.Rank) return false;
            r.Rank = target;
            r.Trust = Mathf.Clamp(Mathf.Max(r.Trust, TrustFloor(target)), 0, MaxTrust);
            if (target >= BondIds.RankAwakened) r.Evolution = Mathf.Max(r.Evolution, 2);
            return true;
        }

        public static bool CanCombatAssist(int rank) { return rank >= BondIds.RankBonded; }
        public static bool CanFuseRank(int rank) { return rank >= BondIds.FusionRank; }
        public static bool CanSpecialize(int rank) { return rank >= BondIds.RankHarmonized; }

        /// <summary>The visual evolution stage: 0 base, 1 Harmonized accents, 2 Awakened form. A cosmetic stage granted by a
        /// reward is kept (never lowered); it changes looks only, never stats.</summary>
        public static int EvolutionStage(int rank, int recorded)
        {
            int byRank = rank >= BondIds.RankAwakened ? 2 : (rank >= BondIds.RankHarmonized ? 1 : 0);
            return Mathf.Clamp(Mathf.Max(byRank, recorded), 0, 2);
        }

        // ================================================================== care

        /// <summary>
        /// One care or tuning interaction at a station. It always heals the companion and revives a downed one; trust is
        /// given only off cooldown; a Rescued companion becomes Bonded on its first care whatever the cooldown.
        /// </summary>
        public static CareOutcome Care(CompanionRecord r, string displayName)
        {
            CareOutcome o = new CareOutcome();
            if (r == null)
            {
                o.Message = "You have no bonded companion with you.";
                return o;
            }
            string name = string.IsNullOrEmpty(displayName) ? BondIds.DefaultName(r.Archetype) : displayName;
            o.Performed = true;
            int rankBefore = r.Rank;
            if (r.State == (int)CompanionState.Downed)
            {
                o.Revived = true;
                r.State = (int)CompanionState.Following;
                r.RecoverIn = 0f;
            }
            r.Health01 = 1f;
            if (r.Rank == BondIds.RankStray) r.Rank = BondIds.RankBonded;
            if (r.CareCooldown > 0f)
            {
                o.OnCooldown = true;
                o.Message = name + " is content. Care gives trust again in " + Clock(r.CareCooldown) + ".";
            }
            else
            {
                o.TrustGained = AddTrust(r, TrustCare);
                r.CareCooldown = CareCooldown;
                r.CareCount++;
                o.Message = name + " leans into the care. Trust +" + o.TrustGained + ".";
            }
            r.Rank = RankForTrust(r.Trust, r.Rank);
            o.NewRank = r.Rank;
            o.RankChanged = r.Rank != rankBefore;
            if (o.Revived) o.Message = name + " is back on its feet. " + o.Message;
            if (o.RankChanged) o.Message += " Now " + RankName(r.Rank) + ": " + RankUnlock(r.Rank);
            return o;
        }

        /// <summary>Counts down a record's gameplay timers (care, contract trust, downed recovery). True when a downed companion recovered.</summary>
        public static bool TickTimers(CompanionRecord r, float dt)
        {
            if (r == null || dt <= 0f) return false;
            r.CareCooldown = Mathf.Max(0f, r.CareCooldown - dt);
            r.ContractCooldown = Mathf.Max(0f, r.ContractCooldown - dt);
            if (r.State != (int)CompanionState.Downed) return false;
            r.RecoverIn = Mathf.Max(0f, r.RecoverIn - dt);
            if (r.RecoverIn > 0f) return false;
            r.State = (int)CompanionState.Following;
            r.Health01 = Mathf.Max(r.Health01, RecoverHealth);
            return true;
        }

        /// <summary>Wears a companion's health; at zero it is downed (never lost) and starts resting. True when this downed it.</summary>
        public static bool Hurt(CompanionRecord r, float damage, float maxHealth)
        {
            if (r == null || damage <= 0f || r.State == (int)CompanionState.Downed) return false;
            r.Health01 = Mathf.Clamp01(r.Health01 - damage / Mathf.Max(1f, maxHealth));
            if (r.Health01 > 0f) return false;
            r.Health01 = 0f;
            r.State = (int)CompanionState.Downed;
            r.RecoverIn = RecoverSeconds;
            return true;
        }

        public static string Clock(float seconds)
        {
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return (s / 60) + ":" + (s % 60).ToString("00");
        }

        // ================================================================== quest trust

        /// <summary>Trust for a completed quest: campaign main and side quests once per quest and companion, contracts on a
        /// cooldown, nothing for other quests. Records the milestone so a replay never pays twice.</summary>
        public static int QuestTrust(CompanionRecord r, string questId)
        {
            if (r == null || string.IsNullOrEmpty(questId)) return 0;
            if (r.Milestones == null) r.Milestones = new List<string>();
            if (QuestIds04E.IsMain(questId) || QuestIds04E.IsSide(questId))
            {
                if (r.Milestones.Contains(questId)) return 0;
                r.Milestones.Add(questId);
                return AddTrust(r, QuestIds04E.IsMain(questId) ? TrustMainQuest : TrustSideQuest);
            }
            if (questId.StartsWith("uhc_", System.StringComparison.Ordinal))
            {
                if (r.ContractCooldown > 0f) return 0;
                r.ContractCooldown = ContractTrustCooldown;
                return AddTrust(r, TrustContract);
            }
            return 0;
        }

        // ================================================================== health and assists

        public static float MaxHealth(string archetype, int rank, int tuning)
        {
            float b;
            switch (archetype)
            {
                case BondIds.AshWarden: b = 140f; break;
                case BondIds.StormSkimmer: b = 55f; break;
                default: b = 70f; break;
            }
            int above = Mathf.Max(0, ClampRank(rank) - BondIds.RankBonded);
            return b * (1f + 0.15f * above) * (1f + 0.1f * Mathf.Clamp(tuning, 0, MaxTuning));
        }

        /// <summary>Assist strength multiplier from rank (Rescued and Bonded 1).</summary>
        public static float RankPower(int rank)
        {
            switch (ClampRank(rank))
            {
                case BondIds.RankAttuned: return 1.25f;
                case BondIds.RankHarmonized: return 1.5f;
                case BondIds.RankAwakened: return 1.8f;
                default: return 1f;
            }
        }

        public static float TuningPower(int tuning) { return 1f + 0.08f * Mathf.Clamp(tuning, 0, MaxTuning); }

        /// <summary>Damage of the drakeling's Glass Lance or the skimmer's arc (the warden's brace deals none).</summary>
        public static float AssistDamage(string archetype, int rank, int tuning, string spec)
        {
            float b;
            switch (archetype)
            {
                case BondIds.StormSkimmer: b = 9f; break;
                case BondIds.AshWarden: return 0f;
                default: b = 20f; break;
            }
            float d = b * RankPower(rank) * TuningPower(tuning);
            if (spec == SpecPrism) d *= 1.25f;
            return d;
        }

        /// <summary>Seconds between combat assists.</summary>
        public static float CombatCooldown(string archetype, int rank, string spec)
        {
            float b;
            switch (archetype)
            {
                case BondIds.AshWarden: b = 18f; break;
                case BondIds.StormSkimmer: b = 15f; break;
                default: b = 14f; break;
            }
            int above = Mathf.Max(0, ClampRank(rank) - BondIds.RankBonded);
            b -= above * 1f;
            if (spec == SpecBulwark) b += 2f;
            return Mathf.Max(8f, b);
        }

        /// <summary>Seconds between exploration assists.</summary>
        public static float ExploreCooldown(string archetype, string spec)
        {
            float b = archetype == BondIds.StormSkimmer ? 6f : 8f;
            if (spec == SpecLantern || spec == SpecUpdraft) b -= 2f;
            return b;
        }

        /// <summary>How far an exploration assist reveals the route ahead, metres.</summary>
        public static float RevealLength(string archetype, string spec)
        {
            float b = archetype == BondIds.StormSkimmer ? 60f : 42f;
            if (spec == SpecLantern || spec == SpecUpdraft) b *= 1.5f;
            return b;
        }

        public static BraceStats Brace(int rank, int tuning, string spec)
        {
            BraceStats s = new BraceStats();
            float p = RankPower(rank) * TuningPower(tuning);
            s.Duration = 6f;
            s.HalfAngle = 65f;
            s.Absorb = 0.6f;
            s.Capacity = 90f * p;
            s.Strain = 0.35f;
            s.CoverRange = 5f;
            s.HeatShade = 0f;
            if (spec == SpecBulwark)
            {
                s.Duration = 8f;
                s.HalfAngle = 85f;
                s.Absorb = 0.7f;
                s.Capacity *= 1.5f;
            }
            else if (spec == SpecKilnHeart) s.HeatShade = 0.5f;
            return s;
        }

        /// <summary>Stagger seconds of the skimmer's arc (bosses get none: their poise is drained instead).</summary>
        public static float DisruptSeconds(bool bossLike, bool elite, int rank, string spec)
        {
            if (bossLike) return 0f;
            float s = elite ? 0.7f : 1.5f;
            s += 0.1f * Mathf.Max(0, ClampRank(rank) - BondIds.RankBonded);
            return s;
        }

        /// <summary>Health the companion loses when its assist puts it next to an engaged foe (the cost of helping).</summary>
        public static float AssistExposure(string archetype, float maxHealth, bool foeClose)
        {
            if (!foeClose) return 0f;
            return maxHealth * (archetype == BondIds.AshWarden ? 0.04f : 0.08f);
        }

        // ================================================================== specializations (Harmonized)

        public const string SpecPrism = "prism";
        public const string SpecLantern = "lantern";
        public const string SpecBulwark = "bulwark";
        public const string SpecKilnHeart = "kiln_heart";
        public const string SpecStaticNet = "static_net";
        public const string SpecUpdraft = "updraft";

        private static readonly string[] GuideSpecs = { SpecPrism, SpecLantern };
        private static readonly string[] WardenSpecs = { SpecBulwark, SpecKilnHeart };
        private static readonly string[] SkimmerSpecs = { SpecStaticNet, SpecUpdraft };
        private static readonly string[] NoSpecs = new string[0];

        /// <summary>The two specializations an archetype can choose from at Harmonized.</summary>
        public static string[] Specializations(string archetype)
        {
            switch (archetype)
            {
                case BondIds.QuartzGuide: return GuideSpecs;
                case BondIds.AshWarden: return WardenSpecs;
                case BondIds.StormSkimmer: return SkimmerSpecs;
                default: return NoSpecs;
            }
        }

        public static bool IsSpecOf(string archetype, string spec)
        {
            return !string.IsNullOrEmpty(spec) && System.Array.IndexOf(Specializations(archetype), spec) >= 0;
        }

        public static string SpecTitle(string spec)
        {
            switch (spec)
            {
                case SpecPrism: return "Prism Lance";
                case SpecLantern: return "Lantern Sense";
                case SpecBulwark: return "Bulwark";
                case SpecKilnHeart: return "Kiln Heart";
                case SpecStaticNet: return "Static Net";
                case SpecUpdraft: return "Updraft Scout";
                default: return "None";
            }
        }

        public static string SpecText(string spec)
        {
            switch (spec)
            {
                case SpecPrism: return "Glass Lance hits 25% harder and splinters into one more foe nearby for half damage.";
                case SpecLantern: return "Resonant Trail reaches 50% further and recharges 2 s sooner.";
                case SpecBulwark: return "The brace lasts 8 s, covers a wider arc and holds 50% more, but recharges 2 s slower.";
                case SpecKilnHeart: return "While braced, the warden also shades you from half of any hazard heat.";
                case SpecStaticNet: return "Static Snare jumps to a second foe nearby.";
                case SpecUpdraft: return "Relay Hop reveals 50% further and recharges 2 s sooner.";
                default: return "";
            }
        }

        // ================================================================== crystal tuning (previewed costs, real materials)

        /// <summary>The cost of raising tuning to targetLevel (1 to 3). Empty for levels outside that range.</summary>
        public static BondCost[] TuningCost(string archetype, int targetLevel)
        {
            switch (archetype)
            {
                case BondIds.AshWarden:
                    switch (targetLevel)
                    {
                        case 1: return new[] { new BondCost(MaterialIds04E.AshResin, 2), new BondCost(MaterialIds04E.CoolantSalt, 3) };
                        case 2: return new[] { new BondCost(MaterialIds04E.AshResin, 4), new BondCost("basilisk_scale", 1), new BondCost(MaterialIds04E.RegulatorCog, 1) };
                        case 3: return new[] { new BondCost(MaterialIds04E.SeedCore, 1), new BondCost(MaterialIds04E.AshResin, 6), new BondCost("wyrm_scale", 2) };
                    }
                    break;
                case BondIds.StormSkimmer:
                    switch (targetLevel)
                    {
                        case 1: return new[] { new BondCost("glass_chitin", 3), new BondCost(MaterialIds04E.CoolantSalt, 2) };
                        case 2: return new[] { new BondCost(MaterialIds04E.RegulatorCog, 2), new BondCost("neon_hide", 1), new BondCost("glass_chitin", 4) };
                        case 3: return new[] { new BondCost("core_volt", 1), new BondCost(MaterialIds04E.RegulatorCog, 3), new BondCost("wyvern_membrane", 1) };
                    }
                    break;
                case BondIds.QuartzGuide:
                    switch (targetLevel)
                    {
                        case 1: return new[] { new BondCost(MaterialIds04E.EmberCrystal, 2), new BondCost("mite_dust", 4) };
                        case 2: return new[] { new BondCost(MaterialIds04E.EmberCrystal, 4), new BondCost("shard_chunk", 3), new BondCost(MaterialIds04E.RegulatorCog, 1) };
                        case 3: return new[] { new BondCost(MaterialIds04E.SeedCore, 1), new BondCost(MaterialIds04E.EmberCrystal, 6), new BondCost("rift_shard", 2) };
                    }
                    break;
            }
            return new BondCost[0];
        }

        /// <summary>What a tuning level adds (preview text).</summary>
        public static string TuningEffect(string archetype, int targetLevel)
        {
            if (targetLevel < 1 || targetLevel > MaxTuning) return "";
            return "+" + TrustTuning + " trust, +10% health, +8% assist power (level " + targetLevel + " of " + MaxTuning + ").";
        }

        // ================================================================== names

        /// <summary>A safe display name: trimmed, single spaces, printable letters, digits, spaces, apostrophes and
        /// hyphens only (no typographic dashes), at most MaxNameLength characters; the fallback when nothing is left.</summary>
        public static string SanitizeName(string raw, string fallback)
        {
            if (string.IsNullOrEmpty(raw)) return fallback ?? "";
            StringBuilder sb = new StringBuilder(MaxNameLength);
            bool space = false;
            for (int i = 0; i < raw.Length && sb.Length < MaxNameLength; i++)
            {
                char c = raw[i];
                if (char.IsWhiteSpace(c))
                {
                    if (sb.Length > 0) space = true;
                    continue;
                }
                bool ok = char.IsLetterOrDigit(c) || c == '\'' || c == '-';
                if (!ok) continue;
                if (space && sb.Length < MaxNameLength - 1) sb.Append(' ');
                space = false;
                sb.Append(c);
            }
            string s = sb.ToString().Trim();
            return s.Length == 0 ? (fallback ?? "") : s;
        }

        // ================================================================== fusion snapshots (no refill)

        /// <summary>The health a separated companion comes back with: never more than it had when it fused.</summary>
        public static float RestoredHealth(float atSnapshot, float provided)
        {
            return Mathf.Min(Mathf.Clamp01(atSnapshot), Mathf.Clamp01(provided));
        }

        /// <summary>The assist cooldown after separation: never shorter than the time that would naturally be left.</summary>
        public static float RestoredCooldown(float atSnapshot, float provided, float elapsed)
        {
            return Mathf.Max(Mathf.Max(0f, atSnapshot - Mathf.Max(0f, elapsed)), Mathf.Max(0f, provided));
        }
    }
}
