using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Factions, the Forge and the skill trees: rank bands, the rival penalty and fractional carry, kill rules, recipe
    /// prices, all-or-nothing payment and unlocks, skill prerequisites and point accounting, modifiers computed from a
    /// learned set, and save round trips of the three save blocks (the JsonUtility ones need the engine).
    /// </summary>
    public sealed class ProgressionTests
    {
        private sealed class FakeWallet : IForgeWallet
        {
            public int Money;
            public bool FailMaterials;
            public readonly Dictionary<string, int> Mats = new Dictionary<string, int>();
            public int Cash { get { return Money; } }
            public int Count(string id) { int n; return Mats.TryGetValue(id, out n) ? n : 0; }
            public bool TakeCash(int amount) { if (Money < amount) return false; Money -= amount; return true; }
            public void GiveCash(int amount) { Money += amount; }

            public bool TakeMaterials(IList<KeyValuePair<string, int>> m)
            {
                if (FailMaterials) return false;
                foreach (KeyValuePair<string, int> kv in m) if (Count(kv.Key) < kv.Value) return false;
                foreach (KeyValuePair<string, int> kv in m) Mats[kv.Key] = Count(kv.Key) - kv.Value;
                return true;
            }
        }

        private sealed class FakeContext : IForgeContext
        {
            public readonly Dictionary<string, FactionRank> Ranks = new Dictionary<string, FactionRank>();
            public bool Eye;
            public readonly HashSet<string> Quests = new HashSet<string>();
            public readonly HashSet<string> Grants = new HashSet<string>();
            public FactionRank RankOf(string id) { FactionRank r; return id != null && Ranks.TryGetValue(id, out r) ? r : FactionRank.Neutral; }
            public bool ThirdEye { get { return Eye; } }
            public bool HasStoryFlag(string f) { return false; }
            public bool QuestDone(string q) { return Quests.Contains(q); }
            public int StoryProgress { get { return 0; } }
            public bool Granted(string id) { return Grants.Contains(id); }
        }

        [TearDown]
        public void Clean()
        {
            SkillMods.ResetStatics();
            ForgeDb.ResetStatics();
        }

        // ------------------------------------------------------------------ factions

        [Test]
        public void RankBandsMatchTheDesign()
        {
            Assert.That(FactionRules.RankOf(-100), Is.EqualTo(FactionRank.Hunted));
            Assert.That(FactionRules.RankOf(-60), Is.EqualTo(FactionRank.Hunted));
            Assert.That(FactionRules.RankOf(-59), Is.EqualTo(FactionRank.Hostile));
            Assert.That(FactionRules.RankOf(-20), Is.EqualTo(FactionRank.Hostile));
            Assert.That(FactionRules.RankOf(-19), Is.EqualTo(FactionRank.Neutral));
            Assert.That(FactionRules.RankOf(19), Is.EqualTo(FactionRank.Neutral));
            Assert.That(FactionRules.RankOf(20), Is.EqualTo(FactionRank.Friendly));
            Assert.That(FactionRules.RankOf(50), Is.EqualTo(FactionRank.Trusted));
            Assert.That(FactionRules.RankOf(80), Is.EqualTo(FactionRank.Family));
            for (int v = -100; v <= 100; v++)
            {
                FactionRank r = FactionRules.RankOf(v);
                Assert.That(v, Is.InRange(FactionRules.RankMin(r), FactionRules.RankMax(r)), "rep " + v);
            }
            Assert.That(FactionRules.Forgive(-10), Is.EqualTo(-10 + FactionRules.DailyForgiveness), "grudges fade");
            Assert.That(FactionRules.Forgive(-1), Is.EqualTo(0), "never past zero");
            Assert.That(FactionRules.Forgive(25), Is.EqualTo(25), "good standing does not fade");
        }

        [Test]
        public void GainsCostTheRivalAndCarryFractions()
        {
            RepTrack t = new RepTrack();
            List<RepChange> changes = new List<RepChange>();
            t.Apply("frost_wardens", 10f, 0f, true, changes);
            Assert.That(t.Get("frost_wardens"), Is.EqualTo(10));
            Assert.That(t.Get("hollow_cult"), Is.EqualTo(-5), "the rival loses half the gain");
            Assert.That(changes.Count, Is.EqualTo(2));
            t.Clear();
            t.Apply("frost_wardens", 10f, 0.25f, true, null);
            Assert.That(t.Get("frost_wardens"), Is.EqualTo(12));
            Assert.That(t.Exact("frost_wardens"), Is.EqualTo(12.5f).Within(0.001f), "the skill bonus keeps its half point");
            Assert.That(t.Get("hollow_cult"), Is.EqualTo(-5), "the rival pays for the base gain only");
            t.Clear();
            for (int i = 0; i < 4; i++) t.Apply("menders", 0.5f, 0f, true, null);
            Assert.That(t.Get("menders"), Is.EqualTo(2), "half points add up");
            t.Apply("ember_court", -10f, 0.5f, true, null);
            Assert.That(t.Get("ember_court"), Is.EqualTo(-10), "losses are not boosted");
            Assert.That(t.Get("brass_union"), Is.EqualTo(0), "losses leave the rival alone");
            t.Apply("ember_court", -500f, 0f, true, null);
            Assert.That(t.Get("ember_court"), Is.EqualTo(-100));
            t.Apply("ember_court", 500f, 0f, true, null);
            Assert.That(t.Get("ember_court"), Is.EqualTo(100));
            t.Apply("nobody", 5f, 0f, true, null);
            Assert.That(t.Get("nobody"), Is.EqualTo(0));
        }

        [Test]
        public void KillsMoveTheRightFactions()
        {
            List<KeyValuePair<string, float>> d = new List<KeyValuePair<string, float>>();
            FactionRules.GangKill(0, false, false, d);
            Assert.That(d[0].Key, Is.EqualTo("ember_court"));
            Assert.That(d[0].Value, Is.LessThan(0f));
            Assert.That(d.Exists(kv => kv.Key == "brass_union" && kv.Value > 0f), Is.True, "the rival syndicate approves");
            d.Clear();
            FactionRules.GangKill(0, false, true, d);
            Assert.That(d[0].Value, Is.EqualTo(-FactionRules.GangKillLoss * 0.5f).Within(0.001f), "Silver Tongue halves it");
            Assert.That(Delta("frost_stalker", "frost_wardens"), Is.GreaterThan(0f));
            Assert.That(Delta("glacier_titan", "frost_wardens"), Is.GreaterThan(Delta("frost_stalker", "frost_wardens")));
            Assert.That(Delta("hollow", "frequency_order"), Is.GreaterThan(0f));
            Assert.That(Delta("hollow", "hollow_cult"), Is.LessThan(0f));
            Assert.That(Delta("snow_hollow", "hollow_cult"), Is.LessThan(0f));
            Assert.That(Delta("glass_mite", "frost_wardens"), Is.EqualTo(0f));
        }

        private static float Delta(string species, string faction)
        {
            List<KeyValuePair<string, float>> l = new List<KeyValuePair<string, float>>();
            FactionRules.CreatureKill(CreatureSpeciesDb.Get(species), l);
            float s = 0f;
            foreach (KeyValuePair<string, float> kv in l) if (kv.Key == faction) s += kv.Value;
            return s;
        }

        [Test]
        public void EveryFactionDescribesEveryRank()
        {
            foreach (FactionDef f in FactionDb.All)
            {
                for (int r = 0; r <= (int)FactionRank.Family; r++)
                {
                    List<string> perks = FactionRules.Perks(f, (FactionRank)r);
                    Assert.That(perks, Is.Not.Empty, f.Id + " at " + (FactionRank)r);
                    foreach (string p in perks) Assert.That(p.IndexOf((char)0x2014) < 0 && p.IndexOf((char)0x2013) < 0, Is.True, "no dashes: " + p);
                }
            }
        }

        // ------------------------------------------------------------------ the Forge

        [Test]
        public void GearPricesFollowTheGearRules()
        {
            int legal = 0;
            for (int slot = 0; slot < GearSystem.SlotCount; slot++)
            {
                for (int rar = 0; rar <= (int)Rarity.Mythic; rar++)
                {
                    for (int set = -1; set < GearSystem.SetNames.Length; set++)
                    {
                        for (int el = 0; el <= (int)Element.Void; el++)
                        {
                            GearOrder o = new GearOrder { Slot = (GearSlot)slot, Rarity = (Rarity)rar, Set = set, Element = (Element)el, Heart = rar == (int)Rarity.Mythic ? "titan_heart" : null };
                            bool ok = (set < 0 || GearSystem.SetAllowed(o.Slot, o.Rarity)) && (o.Element == Element.None ? !GearSystem.ElementRequired(o.Slot) : GearSystem.ElementAllowed(o.Slot, o.Rarity));
                            string problem = ForgeMath.GearOrderProblem(o);
                            Assert.That(problem == null, Is.EqualTo(ok), o.Rarity + " " + o.Slot + " set " + set + " " + o.Element);
                            if (problem != null) continue;
                            legal++;
                            ForgeCost c = ForgeMath.GearCost(o);
                            Assert.That(c.Cash, Is.EqualTo(ForgeMath.GearFee[rar]));
                            HashSet<string> seen = new HashSet<string>();
                            foreach (KeyValuePair<string, int> kv in c.Materials)
                            {
                                Assert.That(MaterialDb.Get(kv.Key), Is.Not.Null, kv.Key);
                                Assert.That(seen.Add(kv.Key), Is.True, "each material once: " + kv.Key);
                            }
                            Assert.That(c.CountOf("titan_heart") > 0, Is.EqualTo(o.Rarity == Rarity.Mythic), "only mythic pieces take a boss heart");
                        }
                    }
                }
            }
            Assert.That(legal, Is.GreaterThan(100));
        }

        [Test]
        public void PaymentIsAllOrNothing()
        {
            ForgeCost price = new ForgeCost { Cash = 300 }.Add("shard_chunk", 4).Add("mite_dust", 2);
            FakeWallet w = new FakeWallet { Money = 1000 };
            w.Mats["shard_chunk"] = 3;
            w.Mats["mite_dust"] = 5;
            Assert.That(ForgeMath.TryPay(price, w), Is.False);
            Assert.That(w.Money, Is.EqualTo(1000));
            Assert.That(w.Count("mite_dust"), Is.EqualTo(5));
            Assert.That(ForgeMath.Missing(price, w).Count, Is.EqualTo(1));
            w.Mats["shard_chunk"] = 4;
            Assert.That(ForgeMath.TryPay(price, w), Is.True);
            Assert.That(w.Money, Is.EqualTo(700));
            Assert.That(w.Count("shard_chunk"), Is.EqualTo(0));
            Assert.That(w.Count("mite_dust"), Is.EqualTo(3));
            FakeWallet broken = new FakeWallet { Money = 1000, FailMaterials = true };
            broken.Mats["shard_chunk"] = 9;
            broken.Mats["mite_dust"] = 9;
            Assert.That(ForgeMath.TryPay(price, broken), Is.False);
            Assert.That(broken.Money, Is.EqualTo(1000), "cash is refunded when the materials fail");
        }

        [Test]
        public void RecipesUnlockByRankConditionsAndQuests()
        {
            FakeContext ctx = new FakeContext();
            RecipeDef anchor = ForgeDb.Get("upg_veil_anchor");
            RecipeDef focus = ForgeDb.Get("brew_focus_draught");
            Assert.That(ForgeMath.Known(ForgeDb.Get("brew_hunters_oil"), ctx), Is.True);
            Assert.That(ForgeMath.Known(anchor, ctx), Is.False, "needs the Third Eye");
            ctx.Eye = true;
            Assert.That(ForgeMath.Known(anchor, ctx), Is.True);
            Assert.That(ForgeMath.Known(focus, ctx), Is.False, "needs Forgewrights Friendly");
            ctx.Ranks["forgewrights"] = FactionRank.Friendly;
            Assert.That(ForgeMath.Known(focus, ctx), Is.True);
            ctx.Ranks["forgewrights"] = FactionRank.Hostile;
            Assert.That(ForgeMath.Known(ForgeDb.Get("infuse_ember"), ctx), Is.False);
            ctx.Grants.Add("infuse_ember");
            Assert.That(ForgeMath.Known(ForgeDb.Get("infuse_ember"), ctx), Is.True, "granted recipes stay known");
            Assert.That(ForgeMath.StationAllows(anchor, ForgeStation.Shrine), Is.False);
            Assert.That(ForgeMath.StationAllows(ForgeDb.Get("upg_frost_lining"), ForgeStation.Forgewright), Is.False);
            Assert.That(ForgeMath.StationAllows(ForgeDb.Get("upg_frost_lining"), ForgeStation.Outpost), Is.True);
            ForgeDb.RegisterQuestUnlock("q_any", "upg_tonic_flask");
            ForgeDb.RegisterQuestUnlock("q_any", "missing_recipe");
            List<string> l = new List<string>();
            ForgeDb.UnlocksFor("q_any", l);
            Assert.That(l, Is.EqualTo(new List<string> { "upg_tonic_flask" }));
            Assert.That(ForgeMath.RuleHolds(new UnlockRule(UnlockKind.QuestDone, "q_any", 0), ctx), Is.False);
            ctx.Quests.Add("q_any");
            Assert.That(ForgeMath.RuleHolds(new UnlockRule(UnlockKind.QuestDone, "q_any", 0), ctx), Is.True);
        }

        [Test]
        public void DiscountsStackAndCap()
        {
            Assert.That(ForgeMath.CombineDiscounts(0.1f, 0.1f), Is.EqualTo(0.19f).Within(0.0001f));
            Assert.That(ForgeMath.CombineDiscounts(0.4f, 0.4f), Is.EqualTo(0.5f));
            Assert.That(ForgeMath.Discounted(100, 0.2f), Is.EqualTo(80));
            Assert.That(ForgeMath.Discounted(0, 0.2f), Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ skill trees

        [Test]
        public void TreesHaveTheRightShape()
        {
            Assert.That(SkillDb.All.Length, Is.EqualTo(90));
            HashSet<string> ids = new HashSet<string>();
            int[] count = new int[3], sigs = new int[3];
            foreach (SkillNode n in SkillDb.All)
            {
                Assert.That(ids.Add(n.Id), Is.True, n.Id);
                Assert.That(n.Tier == 0 ? n.Prereqs.Length == 0 : n.Prereqs.Length > 0, Is.True, n.Id + " links");
                foreach (string p in n.Prereqs)
                {
                    SkillNode pn = SkillDb.Get(p);
                    Assert.That(pn, Is.Not.Null, n.Id + " prereq " + p);
                    Assert.That(pn.Tree, Is.EqualTo(n.Tree));
                    Assert.That(pn.Tier, Is.LessThan(n.Tier));
                }
                count[(int)n.Tree]++;
                if (n.Signature) sigs[(int)n.Tree]++;
            }
            for (int t = 0; t < 3; t++)
            {
                Assert.That(count[t], Is.EqualTo(30), SkillDb.TreeName((SkillTreeId)t));
                Assert.That(sigs[t], Is.GreaterThanOrEqualTo(4), SkillDb.TreeName((SkillTreeId)t) + " signatures");
            }
        }

        [Test]
        public void PointsPrerequisitesAndTiers()
        {
            SkillBook b = new SkillBook();
            Assert.That(b.Available, Is.EqualTo(0), "level 1 has no points");
            b.Level = 4;
            Assert.That(b.Available, Is.EqualTo(3));
            Assert.That(b.Learn("body_heavy_hands"), Is.True);
            Assert.That(b.Check("body_brawler").Block, Is.EqualTo(LearnBlock.Tier));
            Assert.That(b.Check("body_brick_fists").Block, Is.EqualTo(LearnBlock.Prereq));
            Assert.That(b.Learn("body_heavy_hands"), Is.True);
            Assert.That(b.Learn("body_brawler"), Is.True);
            Assert.That(b.Available, Is.EqualTo(0));
            Assert.That(b.Learn("body_deep_lungs"), Is.False);
            Assert.That(b.Check("body_deep_lungs").Block, Is.EqualTo(LearnBlock.Points));
            b.Granted = 1;
            Assert.That(b.Learn("body_heavy_hands"), Is.True);
            Assert.That(b.Check("body_heavy_hands").Block, Is.EqualTo(LearnBlock.Maxed));
            b.Respec();
            Assert.That(b.Spent, Is.EqualTo(0));
            Assert.That(b.Available, Is.EqualTo(4));
            Assert.That(b.Respecs, Is.EqualTo(1));
            Assert.That(SkillBook.RespecCost(10, 1), Is.GreaterThan(SkillBook.RespecCost(10, 0)));
        }

        [Test]
        public void ModifiersComeFromTheLearnedSet()
        {
            SkillBook b = new SkillBook { Level = 100 };
            foreach (string id in new[] { "body_heavy_hands", "body_heavy_hands", "body_heavy_hands", "body_brawler", "body_brawler" }) Assert.That(b.Learn(id), Is.True, id);
            SkillModValues v = new SkillModValues();
            b.Compute(v);
            Assert.That(v.Get(SkillStat.MeleeDamage), Is.EqualTo(1.24f).Within(0.0001f));
            Assert.That(v.Get(SkillStat.GunDamage), Is.EqualTo(1f));
            SkillBook veil = new SkillBook { Level = 100 };
            foreach (string id in new[] { "shard_veil_sense", "shard_veil_sense", "shard_veil_sense", "shard_thin_veil", "shard_thin_veil", "shard_veil_walker" }) veil.Learn(id);
            Assert.That(veil.Has("shard_veil_walker"), Is.True);
            SkillModValues vv = new SkillModValues();
            veil.Compute(vv);
            Assert.That(vv.Get(SkillStat.VeilDrain), Is.EqualTo(0.38f).Within(0.0001f), "adds first, then Veil Walker halves");
            SkillMods.Apply(vv);
            Assert.That(SkillMods.VeilDrain, Is.EqualTo(0.38f).Within(0.0001f));
            Assert.That(SkillMods.VeilShiftCost, Is.EqualTo(0.48f).Within(0.0001f));
            SkillMods.ResetStatics();
            for (int s = 0; s < (int)SkillStat.Count; s++) Assert.That(SkillMods.Get((SkillStat)s), Is.EqualTo(SkillModValues.BaseOf((SkillStat)s)), ((SkillStat)s).ToString());
        }

        [Test]
        public void SkillSaveRoundTripAndValidation()
        {
            SkillBook b = new SkillBook { Level = 9, Granted = 2, Respecs = 1 };
            foreach (string id in new[] { "mind_keen_eye", "mind_keen_eye", "mind_weak_spots", "shard_deep_well" }) b.Learn(id);
            SkillSave s = new SkillSave();
            b.Capture(s);
            SkillBook back = new SkillBook();
            Assert.That(back.Restore(s, 9), Is.True);
            Assert.That(back.Rank("mind_keen_eye"), Is.EqualTo(2));
            Assert.That(back.Rank("mind_weak_spots"), Is.EqualTo(1));
            Assert.That(back.Available, Is.EqualTo(b.Available));
            Assert.That(back.Granted, Is.EqualTo(2));
            Assert.That(back.Respecs, Is.EqualTo(1));
            SkillSave greedy = new SkillSave();
            greedy.Learned.Add("body_heavy_hands");
            greedy.Ranks.Add(3);
            greedy.Learned.Add("body_deep_lungs");
            greedy.Ranks.Add(3);
            SkillBook g = new SkillBook();
            Assert.That(g.Restore(greedy, 2), Is.False, "spent more than it had: refunded");
            Assert.That(g.LearnedCount, Is.EqualTo(0));
            SkillSave orphan = new SkillSave();
            orphan.Learned.Add("body_brawler");
            SkillBook o = new SkillBook();
            o.Restore(orphan, 20);
            Assert.That(o.Has("body_brawler"), Is.False, "a node without its link is dropped");
        }

        [Test]
        public void RepSaveRoundTrip()
        {
            RepTrack t = new RepTrack();
            t.Apply("canal_choir", 7.5f, 0f, true, null);
            t.Apply("glass_jury", -33f, 0f, true, null);
            FactionSave s = new FactionSave();
            t.Capture(s);
            RepTrack back = new RepTrack();
            back.Restore(s);
            foreach (FactionDef f in FactionDb.All) Assert.That(back.Exact(f.Id), Is.EqualTo(t.Exact(f.Id)).Within(0.001f), f.Id);
        }

        // ------------------------------------------------------------------ save blocks

        private static readonly string[] FactionSaveFields = { "Ids:List<String>", "Rep:List<Int32>", "Carry:List<Single>", "Perks:List<String>" };
        private static readonly string[] CraftSaveFields = { "Known:List<String>", "CraftedIds:List<String>", "CraftedCounts:List<Int32>", "BuffIds:List<String>", "BuffTimes:List<Single>" };
        private static readonly string[] SkillSaveFields = { "Learned:List<String>", "Points:Int32", "Ranks:List<Int32>", "Granted:Int32", "Respecs:Int32" };

        /// <summary>The progression save blocks are append-only too: never rename, retype or remove a field (add new ones here).</summary>
        [Test]
        public void SaveBlocksAreOnlyEverAppended()
        {
            CheckFields(typeof(FactionSave), FactionSaveFields);
            CheckFields(typeof(CraftSave), CraftSaveFields);
            CheckFields(typeof(SkillSave), SkillSaveFields);
        }

        private static void CheckFields(Type type, string[] frozen)
        {
            Assert.That(Attribute.IsDefined(type, typeof(SerializableAttribute)), Is.True, type.Name + " stays [Serializable]");
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                Type ft = f.FieldType;
                now[f.Name] = ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>) ? "List<" + ft.GetGenericArguments()[0].Name + ">" : ft.Name;
            }
            foreach (string entry in frozen)
            {
                string[] parts = entry.Split(':');
                string have;
                Assert.That(now.TryGetValue(parts[0], out have), Is.True, type.Name + "." + parts[0] + " was renamed or removed");
                Assert.That(have, Is.EqualTo(parts[1]), type.Name + "." + parts[0] + " changed type");
            }
        }

        [Test]
        [Category(TestData.NeedsEngine)]
        public void SaveBlocksSurviveJson()
        {
            SaveData d = new SaveData();
            d.Factions.Ids.Add("menders");
            d.Factions.Rep.Add(42);
            d.Factions.Carry.Add(0.5f);
            d.Factions.Perks.Add(FactionRules.VeilAttunement);
            d.Crafting.Known.Add("upg_veil_anchor");
            d.Crafting.CraftedIds.Add("upg_energy_cell");
            d.Crafting.CraftedCounts.Add(2);
            d.Crafting.BuffIds.Add("brew_hunters_oil");
            d.Crafting.BuffTimes.Add(123.5f);
            d.Skills.Learned.Add("body_heavy_hands");
            d.Skills.Ranks.Add(3);
            d.Skills.Points = 4;
            d.Skills.Granted = 2;
            d.Skills.Respecs = 1;
            SaveData back = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(d));
            Assert.That(back.Factions.Rep[0], Is.EqualTo(42));
            Assert.That(back.Factions.Carry[0], Is.EqualTo(0.5f));
            Assert.That(back.Factions.Perks[0], Is.EqualTo(FactionRules.VeilAttunement));
            Assert.That(back.Crafting.Known[0], Is.EqualTo("upg_veil_anchor"));
            Assert.That(back.Crafting.CraftedCounts[0], Is.EqualTo(2));
            Assert.That(back.Crafting.BuffTimes[0], Is.EqualTo(123.5f));
            Assert.That(back.Skills.Ranks[0], Is.EqualTo(3));
            Assert.That(back.Skills.Granted, Is.EqualTo(2));
            Assert.That(back.Skills.Respecs, Is.EqualTo(1));
            // A save from before the progression work has none of the new fields.
            SaveData old = JsonUtility.FromJson<SaveData>("{\"Version\":1,\"Level\":7,\"Skills\":{\"Learned\":[\"body_heavy_hands\"],\"Points\":0}}");
            Assert.That(old.Skills.Ranks, Is.Not.Null.And.Empty);
            Assert.That(old.Skills.Granted, Is.EqualTo(0));
            Assert.That(old.Factions.Carry, Is.Not.Null);
            Assert.That(old.Crafting.BuffIds, Is.Not.Null);
            SkillBook b = new SkillBook();
            Assert.That(b.Restore(old.Skills, old.Level), Is.True);
            Assert.That(b.Rank("body_heavy_hands"), Is.EqualTo(1));
            Assert.That(b.Available, Is.EqualTo(5), "an old save gets the points for its levels");
        }
    }
}
