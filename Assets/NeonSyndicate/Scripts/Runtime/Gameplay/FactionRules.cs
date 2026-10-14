using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Reputation ranks from worst to best.</summary>
    public enum FactionRank
    {
        Hunted = 0,
        Hostile = 1,
        Neutral = 2,
        Friendly = 3,
        Trusted = 4,
        Family = 5
    }

    /// <summary>One reputation change that moved the whole-number value (for events and toasts).</summary>
    public struct RepChange
    {
        public string Id;
        public int Old;
        public int New;
    }

    /// <summary>
    /// The rules of faction reputation (engine-free, tested offline): rank bands, what each rank gives, the rival
    /// penalty, gang heat behaviour and which kills move which faction. Bands: Hunted -100 to -60, Hostile -59 to -20,
    /// Neutral -19 to 19, Friendly 20 to 49, Trusted 50 to 79, Family 80 to 100.
    /// </summary>
    public static class FactionRules
    {
        public const int MinRep = -100;
        public const int MaxRep = 100;
        /// <summary>Share of a gain that the gaining faction's rival loses (of the base amount, before skill bonuses).</summary>
        public const float RivalShare = 0.5f;

        // Gang kills.
        public const float GangKillLoss = 2f;
        public const float EliteKillLoss = 5f;
        public const float RivalKillGain = 1f;
        /// <summary>Gang heat above this line means the gang's members attack the player on sight.</summary>
        public const float AttackOnSightHeat = 0.6f;
        /// <summary>
        /// Seconds a gang's heat has to stay past the attack-on-sight line before the fight counts as a war. Kills in a
        /// war are self-defense and cost no reputation; kills in the first seconds of a fight the player started still do.
        /// </summary>
        public const float WarGraceSeconds = 10f;
        /// <summary>
        /// The steady part of the grudge fade: a syndicate grudge fades by at least this much per in-game day, spread over
        /// the hours (never past 0). HourlyForgiveShare comes on top, so deep grudges fade faster.
        /// </summary>
        public const int DailyForgiveness = 10;
        /// <summary>Every in-game hour a syndicate grudge also fades by this share of what is left of it.</summary>
        public const float HourlyForgiveShare = 0.04f;

        // Guild perk numbers (the FACTIONS page text is built from these, so it always matches).
        public const int MendersHealPrice = 150;
        public const float MendersTonicHealFamily = 0.1f;
        public const float VeilAttuneDrain = 0.85f;
        public const float VeilAttuneShift = 0.8f;
        public const float OrderTrustedDrain = 0.9f;
        public const float OrderFamilyShift = 0.9f;
        public const float WardenFamilyFrost = 0.1f;
        public const float CultFriendlyPower = 0.08f;
        public const float CultTrustedPower = 0.15f;
        public const float CultFamilyPower = 0.25f;
        public const float CultFamilyDrain = 0.85f;
        public const string VeilAttunement = "veil_attunement";

        // ------------------------------------------------------------------ ranks

        public static FactionRank RankOf(int rep)
        {
            if (rep >= 80) return FactionRank.Family;
            if (rep >= 50) return FactionRank.Trusted;
            if (rep >= 20) return FactionRank.Friendly;
            if (rep > -20) return FactionRank.Neutral;
            if (rep > -60) return FactionRank.Hostile;
            return FactionRank.Hunted;
        }

        /// <summary>Lowest whole-number reputation inside a rank.</summary>
        public static int RankMin(FactionRank r)
        {
            switch (r)
            {
                case FactionRank.Hunted: return -100;
                case FactionRank.Hostile: return -59;
                case FactionRank.Neutral: return -19;
                case FactionRank.Friendly: return 20;
                case FactionRank.Trusted: return 50;
                default: return 80;
            }
        }

        /// <summary>Highest whole-number reputation inside a rank.</summary>
        public static int RankMax(FactionRank r)
        {
            switch (r)
            {
                case FactionRank.Hunted: return -60;
                case FactionRank.Hostile: return -20;
                case FactionRank.Neutral: return 19;
                case FactionRank.Friendly: return 49;
                case FactionRank.Trusted: return 79;
                default: return 100;
            }
        }

        public static string RankName(FactionRank r)
        {
            return r.ToString();
        }

        public static Color RankColor(FactionRank r)
        {
            switch (r)
            {
                case FactionRank.Hunted: return new Color(1f, 0.2f, 0.2f);
                case FactionRank.Hostile: return new Color(1f, 0.55f, 0.3f);
                case FactionRank.Neutral: return new Color(0.82f, 0.82f, 0.82f);
                case FactionRank.Friendly: return new Color(0.45f, 1f, 0.55f);
                case FactionRank.Trusted: return new Color(0.35f, 0.85f, 1f);
                default: return new Color(1f, 0.82f, 0.3f);
            }
        }

        /// <summary>How far a value sits across the whole -100..100 track (0..1), for bars.</summary>
        public static float TrackFraction(int rep)
        {
            return Mathf.InverseLerp(MinRep, MaxRep, rep);
        }

        // ------------------------------------------------------------------ what ranks give

        /// <summary>Shop discount a faction gives at its own shops (syndicates and companies: shops on their turf).</summary>
        public static float Discount(FactionRank r)
        {
            switch (r)
            {
                case FactionRank.Friendly: return 0.05f;
                case FactionRank.Trusted: return 0.1f;
                case FactionRank.Family: return 0.15f;
                default: return 0f;
            }
        }

        /// <summary>Lowest gang heat a syndicate's rank allows (above 0.6 its members attack on sight).</summary>
        public static float HeatFloor(FactionRank r)
        {
            if (r == FactionRank.Hunted) return 0.9f;
            if (r == FactionRank.Hostile) return 0.65f;
            return 0f;
        }

        /// <summary>Multiplier on gang heat from offences (friends shrug off minor ones).</summary>
        public static float HeatScale(FactionRank r)
        {
            switch (r)
            {
                case FactionRank.Friendly: return 0.5f;
                case FactionRank.Trusted: return 0.35f;
                case FactionRank.Family: return 0.25f;
                default: return 1f;
            }
        }

        /// <summary>Multiplier on how fast gang heat cools.</summary>
        public static float CoolScale(FactionRank r)
        {
            switch (r)
            {
                case FactionRank.Friendly: return 2f;
                case FactionRank.Trusted: return 3f;
                case FactionRank.Family: return 4f;
                default: return 1f;
            }
        }

        /// <summary>Friendly or better: a member who turns on you over a minor offence calms down again.</summary>
        public static bool ForgivesMinorOffences(FactionRank r)
        {
            return r >= FactionRank.Friendly;
        }

        /// <summary>The best gear rarity the Forge will make at a Forgewrights rank.</summary>
        public static Rarity MaxForgeRarity(FactionRank forgewrights)
        {
            switch (forgewrights)
            {
                case FactionRank.Hunted:
                case FactionRank.Hostile: return Rarity.Common;
                case FactionRank.Neutral: return Rarity.Rare;
                case FactionRank.Friendly: return Rarity.Epic;
                case FactionRank.Trusted: return Rarity.Legendary;
                default: return Rarity.Mythic;
            }
        }

        /// <summary>The Forgewrights rank needed to forge a rarity.</summary>
        public static FactionRank RankForRarity(Rarity r)
        {
            for (int i = 0; i <= (int)FactionRank.Family; i++) if (MaxForgeRarity((FactionRank)i) >= r) return (FactionRank)i;
            return FactionRank.Family;
        }

        /// <summary>Extra salvage pay from the Forgewrights for good standing (Trusted 10%, Family 20%).</summary>
        public static float SalvageRankBonus(FactionRank r)
        {
            if (r == FactionRank.Family) return 0.2f;
            if (r == FactionRank.Trusted) return 0.1f;
            return 0f;
        }

        /// <summary>A guild's own services refuse you at Hostile or worse.</summary>
        public static bool Refuses(FactionRank r)
        {
            return r <= FactionRank.Hostile;
        }

        // ------------------------------------------------------------------ kills

        /// <summary>Reputation moved by the player killing a gang member: their syndicate loses, its rival syndicate gains.</summary>
        public static void GangKill(int gangId, bool elite, bool silverTongue, List<KeyValuePair<string, float>> into)
        {
            if (into == null) return;
            FactionDef f = FactionDb.ForGang(gangId);
            if (f == null) return;
            float loss = elite ? EliteKillLoss : GangKillLoss;
            if (silverTongue) loss *= 0.5f;
            into.Add(new KeyValuePair<string, float>(f.Id, -loss));
            FactionDef rival = FactionDb.Get(f.Rival);
            if (rival != null && rival.Kind == FactionKind.Syndicate) into.Add(new KeyValuePair<string, float>(rival.Id, RivalKillGain));
        }

        private struct CreatureRule
        {
            public int Tier;
            public string IdContains;
            public string Faction;
            public float Normal;
            public float GreatBeast;
            public float Boss;
        }

        /// <summary>Which creature kills move which faction (tier 0 means any tier).</summary>
        private static readonly CreatureRule[] CreatureRules =
        {
            new CreatureRule { Tier = (int)CreatureTier.Frostline, Faction = "frost_wardens", Normal = 1f, GreatBeast = 4f, Boss = 10f },
            new CreatureRule { Tier = (int)CreatureTier.InnerWorld, Faction = "frequency_order", Normal = 1f, GreatBeast = 4f, Boss = 10f },
            new CreatureRule { Tier = 0, IdContains = "hollow", Faction = "hollow_cult", Normal = -1f, GreatBeast = -3f, Boss = -5f },
            new CreatureRule { Tier = (int)CreatureTier.Badlands, Faction = "quartz_kings", Normal = 0f, GreatBeast = 3f, Boss = 5f },
            new CreatureRule { Tier = (int)CreatureTier.OuterCity, Faction = "forgewrights", Normal = 0f, GreatBeast = 2f, Boss = 5f }
        };

        /// <summary>Reputation moved by the player killing a creature of a species.</summary>
        public static void CreatureKill(CreatureSpecies s, List<KeyValuePair<string, float>> into)
        {
            if (s == null || into == null) return;
            string id = s.Id ?? "";
            foreach (CreatureRule r in CreatureRules)
            {
                if (r.Tier != 0 && r.Tier != (int)s.Tier) continue;
                if (!string.IsNullOrEmpty(r.IdContains) && id.IndexOf(r.IdContains, StringComparison.Ordinal) < 0) continue;
                float v = s.Boss ? r.Boss : (s.GreatBeast ? r.GreatBeast : r.Normal);
                if (v != 0f) into.Add(new KeyValuePair<string, float>(r.Faction, v));
            }
        }

        /// <summary>
        /// Whether the player killing a gang member moves reputation. Scripted fights never do (mission, rampage, story,
        /// quest and world-event enemies), and neither does self-defense: a kill while the gang is at war with the player,
        /// meaning its heat is past the attack-on-sight line and either a floor holds it there (Hostile standing or the
        /// story) or it has stayed there for WarGraceSeconds.
        /// </summary>
        public static bool GangKillCounts(bool scripted, float heat, float heatFloor, float secondsAtWar)
        {
            if (scripted) return false;
            if (heat <= AttackOnSightHeat) return true;
            if (heatFloor > AttackOnSightHeat) return false;
            return secondsAtWar < WarGraceSeconds;
        }

        /// <summary>
        /// A whole-number syndicate grudge one in-game day later at the steady rate alone: the least it can have faded
        /// (negative reputation moves DailyForgiveness toward 0, never past it). The hourly share makes it fade faster.
        /// </summary>
        public static int Forgive(int rep)
        {
            return rep < 0 ? Mathf.Min(0, rep + DailyForgiveness) : rep;
        }

        /// <summary>
        /// How much a syndicate grudge fades in one in-game hour: DailyForgiveness / 24 plus HourlyForgiveShare of the
        /// grudge, never past 0. Good standing (0 or more) does not fade. At two real minutes per game hour, Hostile -25
        /// is Neutral again after about 8 real minutes, -59 after about 42 and Hunted -100 after about 64.
        /// </summary>
        public static float HourForgiveness(float rep)
        {
            if (rep >= 0f || float.IsNaN(rep)) return 0f;
            float fade = DailyForgiveness / 24f - rep * HourlyForgiveShare;
            return Mathf.Min(fade, -rep);
        }

        // ------------------------------------------------------------------ perk text

        private static string Pct(float f) { return Mathf.RoundToInt(f * 100f) + "%"; }

        /// <summary>What a faction gives (or does to you) at one rank, as short lines for the FACTIONS page.</summary>
        public static List<string> Perks(FactionDef f, FactionRank r)
        {
            List<string> l = new List<string>();
            if (f == null) return l;
            switch (f.Kind)
            {
                case FactionKind.Syndicate:
                case FactionKind.Corporate:
                {
                    string turf = CityLayout.DistrictName(f.Home);
                    if (f.Kind == FactionKind.Syndicate)
                    {
                        if (r == FactionRank.Hunted) l.Add("Members attack on sight and the gang's heat never cools.");
                        else if (r == FactionRank.Hostile) l.Add("Members attack on sight.");
                        else if (r == FactionRank.Neutral) l.Add("Members leave you alone unless provoked.");
                    }
                    else if (r <= FactionRank.Hostile) l.Add("Their people are watching you.");
                    if (r >= FactionRank.Friendly)
                    {
                        l.Add(Pct(Discount(r)) + " off at shops in " + turf + ".");
                        if (f.Kind == FactionKind.Syndicate)
                        {
                            l.Add("Members shrug off minor offences.");
                            l.Add("Heat with them cools " + Mathf.RoundToInt(CoolScale(r)) + " times as fast.");
                        }
                    }
                    break;
                }
                case FactionKind.Guild:
                    GuildPerks(f.Id, r, l);
                    break;
                case FactionKind.Cult:
                    if (r <= FactionRank.Hostile) l.Add("The cult counts you among its enemies.");
                    else if (r == FactionRank.Neutral) l.Add("They watch, and wait.");
                    else if (r == FactionRank.Friendly) l.Add("+" + Pct(CultFriendlyPower) + " crystal power damage.");
                    else if (r == FactionRank.Trusted) l.Add("+" + Pct(CultTrustedPower) + " crystal power damage.");
                    else l.Add("+" + Pct(CultFamilyPower) + " crystal power damage, and the Veil drains " + Pct(1f - CultFamilyDrain) + " less.");
                    break;
            }
            if (l.Count == 0) l.Add("Nothing special.");
            return l;
        }

        private static void GuildPerks(string id, FactionRank r, List<string> l)
        {
            switch (id)
            {
                case "forgewrights":
                    if (Refuses(r)) { l.Add("Dante Rook's forge is closed to you."); l.Add("Other forges make Common gear only."); return; }
                    l.Add("Forge gear up to " + MaxForgeRarity(r) + (r == FactionRank.Family ? ", built around a boss heart" : "") + ".");
                    if (r == FactionRank.Neutral) l.Add("Tonic flasks, energy cells, infusions and brews.");
                    if (r >= FactionRank.Friendly) l.Add(Pct(Discount(r)) + " off Forge fees.");
                    if (r == FactionRank.Friendly) l.Add("Second energy cell and the Focus Draught.");
                    if (r >= FactionRank.Trusted) l.Add("Materials sell for " + Pct(SalvageRankBonus(r)) + " more.");
                    if (r == FactionRank.Trusted) l.Add("Second tonic flask, third energy cell.");
                    return;
                case "menders":
                    if (Refuses(r)) { l.Add("Mara's people won't treat you."); return; }
                    if (r == FactionRank.Neutral) l.Add("Healing at Mara Solane's corner costs " + MathUtil.Money(MendersHealPrice) + ".");
                    if (r >= FactionRank.Friendly) l.Add("Free healing and tonic refills at Mara Solane's corner.");
                    if (r >= FactionRank.Trusted) l.Add("+1 Shard Tonic charge.");
                    if (r == FactionRank.Family) l.Add("Shard Tonics heal " + Pct(MendersTonicHealFamily) + " more.");
                    return;
                case "frost_wardens":
                    if (Refuses(r)) { l.Add("The outpost forge is closed to you."); return; }
                    l.Add("The outpost forge and Frost Lining (rank " + (r >= FactionRank.Trusted ? 3 : (r >= FactionRank.Friendly ? 2 : 1)) + ").");
                    if (r >= FactionRank.Friendly) l.Add("They sell cold-weather gear: " + WardenRarity(r) + " Frostline Warden pieces and Frost Ward Tea.");
                    if (r >= FactionRank.Friendly) l.Add(Pct(Discount(r)) + " off the Wardens' gear and tea.");
                    if (r == FactionRank.Family) l.Add("+" + Pct(WardenFamilyFrost) + " frost resistance.");
                    return;
                case "frequency_order":
                    if (Refuses(r)) { l.Add("Iris Moon's people shut you out. Veil Attunement fails you."); return; }
                    if (r == FactionRank.Neutral) l.Add("Offer rift shards to earn their trust.");
                    if (r >= FactionRank.Friendly) l.Add("Learn Veil Attunement: the Veil drains " + Pct(1f - VeilAttuneDrain) + " less, shifting costs " + Pct(1f - VeilAttuneShift) + " less.");
                    if (r >= FactionRank.Trusted) l.Add("The Veil drains another " + Pct(1f - OrderTrustedDrain) + " less.");
                    if (r == FactionRank.Family) l.Add("Shifting costs another " + Pct(1f - OrderFamilyShift) + " less.");
                    return;
            }
        }

        /// <summary>Rarity of the Frostline Warden pieces the outpost sells at a Frost Wardens rank.</summary>
        public static Rarity WardenRarity(FactionRank r)
        {
            if (r >= FactionRank.Family) return Rarity.Legendary;
            if (r >= FactionRank.Trusted) return Rarity.Epic;
            return Rarity.Rare;
        }

        /// <summary>The next better rank, or the same rank at Family.</summary>
        public static FactionRank Next(FactionRank r)
        {
            return r >= FactionRank.Family ? FactionRank.Family : (FactionRank)((int)r + 1);
        }
    }

    /// <summary>
    /// Reputation values for every faction with a fractional carry (engine-free, tested offline). Gains are boosted by
    /// the skill bonus and cost the rival RivalShare of the base gain; small changes accumulate in the carry so that
    /// half points and skill bonuses are never lost to rounding. Values stay in -100..100.
    /// </summary>
    public sealed class RepTrack
    {
        private readonly Dictionary<string, int> _rep = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _carry = new Dictionary<string, float>();

        public int Get(string id)
        {
            int r;
            return id != null && _rep.TryGetValue(id, out r) ? r : 0;
        }

        /// <summary>The value including the fractional carry.</summary>
        public float Exact(string id)
        {
            float c;
            return Get(id) + (id != null && _carry.TryGetValue(id, out c) ? c : 0f);
        }

        public void Clear()
        {
            _rep.Clear();
            _carry.Clear();
        }

        /// <summary>Sets a whole-number value directly (debug and tests). Clears the carry.</summary>
        public void Set(string id, int value, List<RepChange> changes)
        {
            if (FactionDb.Get(id) == null) return;
            int old = Get(id);
            int now = Mathf.Clamp(value, FactionRules.MinRep, FactionRules.MaxRep);
            _rep[id] = now;
            _carry[id] = 0f;
            if (changes != null && now != old) changes.Add(new RepChange { Id = id, Old = old, New = now });
        }

        /// <summary>
        /// Changes a faction's reputation. A gain is multiplied by (1 + gainBonus) and, when rival is true, the faction's
        /// rival loses RivalShare of the base gain. Every whole-number change is appended to changes (when not null).
        /// </summary>
        public void Apply(string id, float amount, float gainBonus, bool rival, List<RepChange> changes)
        {
            FactionDef f = FactionDb.Get(id);
            if (f == null || amount == 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float eff = amount > 0f ? amount * (1f + Mathf.Max(0f, gainBonus)) : amount;
            ApplyRaw(id, eff, changes);
            if (rival && amount > 0f && FactionDb.Get(f.Rival) != null && f.Rival != id) ApplyRaw(f.Rival, -amount * FactionRules.RivalShare, changes);
        }

        private void ApplyRaw(string id, float amount, List<RepChange> changes)
        {
            int old = Get(id);
            float exact = Exact(id) + amount;
            int whole;
            float carry;
            if (exact >= FactionRules.MaxRep) { whole = FactionRules.MaxRep; carry = 0f; }
            else if (exact <= FactionRules.MinRep) { whole = FactionRules.MinRep; carry = 0f; }
            else
            {
                // Round toward zero, with a little tolerance so 2.9999 counts as 3.
                double nudged = exact >= 0f ? exact + 1e-4 : exact - 1e-4;
                whole = (int)Math.Truncate(nudged);
                carry = exact - whole;
                if (Mathf.Abs(carry) < 1e-4f) carry = 0f;
            }
            _rep[id] = whole;
            _carry[id] = carry;
            if (changes != null && whole != old) changes.Add(new RepChange { Id = id, Old = old, New = whole });
        }

        /// <summary>Writes every faction with a non-zero value into the save block.</summary>
        public void Capture(FactionSave s)
        {
            if (s == null) return;
            s.Ids.Clear();
            s.Rep.Clear();
            s.Carry.Clear();
            foreach (FactionDef f in FactionDb.All)
            {
                int r = Get(f.Id);
                float c;
                if (!_carry.TryGetValue(f.Id, out c)) c = 0f;
                if (r == 0 && c == 0f) continue;
                s.Ids.Add(f.Id);
                s.Rep.Add(r);
                s.Carry.Add(c);
            }
        }

        /// <summary>Reads a save block (unknown ids are skipped, values clamped; old blocks without a carry load as whole numbers).</summary>
        public void Restore(FactionSave s)
        {
            Clear();
            if (s == null || s.Ids == null || s.Rep == null) return;
            int n = Mathf.Min(s.Ids.Count, s.Rep.Count);
            for (int i = 0; i < n; i++)
            {
                string id = s.Ids[i];
                if (FactionDb.Get(id) == null) continue;
                int r = Mathf.Clamp(s.Rep[i], FactionRules.MinRep, FactionRules.MaxRep);
                float c = s.Carry != null && i < s.Carry.Count ? Mathf.Clamp(s.Carry[i], -0.999f, 0.999f) : 0f;
                if (r == FactionRules.MaxRep && c > 0f) c = 0f;
                if (r == FactionRules.MinRep && c < 0f) c = 0f;
                _rep[id] = r;
                _carry[id] = c;
            }
        }
    }
}
