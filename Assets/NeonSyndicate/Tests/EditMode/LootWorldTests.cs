using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The loot world's pure parts: where the chests and Satchels go (counts, kinds, open ground, determinism), the
    /// chest loot tables, respawns, guards, bounties, keys, the Collector, the GiveGun quest step and the save block.
    /// </summary>
    public sealed class LootWorldTests
    {
        /// <summary>Every LootWorldSave field that has shipped. Append new fields here; never edit or remove a line.</summary>
        private static readonly string[] LootWorldSaveFields =
        {
            "Opened:List<Int32>", "OpenedAt:List<Single>", "Discovered:List<Int32>", "Caches:List<Int32>", "CrewDown:List<Int32>", "Keys:Int32",
            "ChestsOpened:Int32", "CollectorClaimed:Int32", "BountyDay:Int32", "Bounties:List<Int32>", "BountiesDone:Int32"
        };

        private static List<LootChestSite> _chests;
        private static List<LootCacheSite> _caches;

        private static List<LootChestSite> Chests
        {
            get
            {
                if (_chests == null) _chests = LootWorldLayout.Chests(TestData.City, TestData.DefaultSeed);
                return _chests;
            }
        }

        private static List<LootCacheSite> Caches
        {
            get
            {
                if (_caches == null) _caches = LootWorldLayout.Caches(TestData.City, TestData.DefaultSeed);
                return _caches;
            }
        }

        // ------------------------------------------------------------------ layout

        [Test]
        public void TheCityHas140ChestsOfTheThreeKinds()
        {
            List<LootChestSite> c = Chests;
            Assert.That(c.Count, Is.EqualTo(LootWorldLayout.ChestCount));
            int[,] n = new int[3, 3];
            for (int i = 0; i < c.Count; i++)
            {
                Assert.That(c[i].Index, Is.EqualTo(i), "indices are the list order");
                n[(int)c[i].Region, (int)c[i].Kind]++;
            }
            for (int r = 0; r < 3; r++)
            {
                Assert.That(n[r, 0], Is.EqualTo(LootWorldLayout.CommonQuota[r]), (LootRegion)r + " common chests");
                Assert.That(n[r, 1], Is.EqualTo(LootWorldLayout.SealedQuota[r]), (LootRegion)r + " sealed chests");
            }
            Assert.That(n[0, 2], Is.EqualTo(LootWorldLayout.VaultCount), "vaults stand over the city");
            Assert.That(n[1, 2] + n[2, 2], Is.EqualTo(0), "no vaults in the wild");
        }

        [Test]
        public void ChestsStandOnOpenGroundAwayFromRoadsBuildingsAndArenas()
        {
            CityLayout L = TestData.City;
            foreach (LootChestSite s in Chests)
            {
                string w = "chest " + s.Index + " " + s.Kind + " at " + s.Pos;
                Assert.That(L.IsInsideBuilding(s.Pos, 3f), Is.False, w + " is inside a building");
                Assert.That(L.NearAnyRoad(s.Pos, 5f), Is.False, w + " is on or beside a road");
                Assert.That(L.InsideRidge(s.Pos, 2f), Is.False, w + " is inside the ridge");
                Assert.That(QuestPlaces.InBossArena(L, s.Pos, QuestPlaces.ArenaMargin), Is.False, w + " is in a boss arena");
                Assert.That(Mathf.Abs(s.Pos.x) < CityLayout.MapHalf - 30f && Mathf.Abs(s.Pos.z) < CityLayout.MapHalf - 30f, Is.True, w + " is off the map");
                District d = CityLayout.DistrictAt(s.Pos);
                if (s.Region == LootRegion.City) Assert.That(CityLayout.IsWilderness(d), Is.False, w);
                if (s.Region == LootRegion.Badlands) Assert.That(d, Is.EqualTo(District.QuartzBadlands), w);
                if (s.Region == LootRegion.Frostline) Assert.That(d, Is.EqualTo(District.Frostline), w);
            }
        }

        [Test]
        public void ChestsAreSpreadOut()
        {
            List<LootChestSite> c = Chests;
            for (int i = 0; i < c.Count; i++)
                for (int j = i + 1; j < c.Count; j++)
                    Assert.That(MathUtil.FlatDistance(c[i].Pos, c[j].Pos), Is.GreaterThan(20f), "chests " + i + " and " + j);
            HashSet<District> districts = new HashSet<District>();
            foreach (LootChestSite s in c) if (s.Region == LootRegion.City) districts.Add(CityLayout.DistrictAt(s.Pos));
            Assert.That(districts.Count, Is.GreaterThanOrEqualTo(5), "city chests reach several districts");
        }

        [Test]
        public void LayoutIsDeterministic()
        {
            List<LootChestSite> a = LootWorldLayout.Chests(TestData.City, TestData.DefaultSeed);
            List<LootCacheSite> b = LootWorldLayout.Caches(TestData.City, TestData.DefaultSeed);
            Assert.That(a.Count, Is.EqualTo(Chests.Count));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(a[i].Pos, Is.EqualTo(Chests[i].Pos), "chest " + i);
                Assert.That(a[i].Kind, Is.EqualTo(Chests[i].Kind), "chest " + i);
            }
            for (int i = 0; i < b.Count; i++) Assert.That(b[i].Pos, Is.EqualTo(Caches[i].Pos), "satchel " + i);
        }

        [Test]
        public void FortySatchelsSitOnRoofsLedgesAndUnderBridges()
        {
            List<LootCacheSite> c = Caches;
            Assert.That(c.Count, Is.EqualTo(LootWorldLayout.CacheCount));
            int[] k = new int[3];
            foreach (LootCacheSite s in c)
            {
                k[(int)s.Kind]++;
                switch (s.Kind)
                {
                    case LootCacheKind.Rooftop:
                        Assert.That(s.Pos.y, Is.InRange(12f, 71f), "rooftop " + s.Index + " height");
                        break;
                    case LootCacheKind.Ledge:
                        Assert.That(s.Pos.y, Is.GreaterThan(8f), "ledge " + s.Index + " height");
                        break;
                    case LootCacheKind.Bridge:
                        Assert.That(s.Pos.y, Is.LessThan(-4f), "bridge satchel " + s.Index + " is down in the canal");
                        Assert.That(Mathf.Abs(s.Pos.z - CityLayout.CanalZ), Is.LessThan(CityLayout.CanalHalfWidth), "bridge satchel " + s.Index + " is inside the channel");
                        break;
                }
            }
            Assert.That(k[(int)LootCacheKind.Rooftop], Is.GreaterThanOrEqualTo(20));
            Assert.That(k[(int)LootCacheKind.Ledge], Is.EqualTo(LootWorldLayout.LedgeCount));
            Assert.That(k[(int)LootCacheKind.Bridge], Is.EqualTo(LootWorldLayout.BridgeCount));
        }

        [Test]
        public void LedgesLieOnTheFirstSetbackInsideTheFootprint()
        {
            int seen = 0;
            foreach (Block b in TestData.City.Blocks)
                foreach (Lot lot in b.Lots)
                    foreach (Building bld in lot.Buildings)
                    {
                        Vector3 p;
                        if (!LootWorldLayout.LedgeSpot(bld, out p)) continue;
                        seen++;
                        Assert.That(bld.Footprint.Contains(p), Is.True, "ledge inside its tower's footprint");
                        Assert.That(p.y, Is.LessThan(CityLayout.CurbHeight + bld.Height * 0.66f), "ledge below the top tier");
                        Assert.That(p.z - bld.Footprint.Z0, Is.GreaterThan(0.75f), "clear of the parapet");
                    }
            Assert.That(seen, Is.GreaterThan(LootWorldLayout.LedgeCount), "enough stepped towers");
        }

        // ------------------------------------------------------------------ rules

        [Test]
        public void ChestOddsClimbWithKindAndLevel()
        {
            Assert.That(LootWorldRules.Floor(LootChestKind.Vault), Is.EqualTo(Rarity.Epic), "vaults are Epic or better");
            Assert.That(LootWorldRules.Floor(LootChestKind.Sealed), Is.GreaterThan(Rarity.Common));
            Assert.That(Mean(LootChestKind.Sealed, 1), Is.GreaterThan(Mean(LootChestKind.Common, 1)), "sealed beats common");
            Assert.That(Mean(LootChestKind.Vault, 1), Is.GreaterThan(Mean(LootChestKind.Sealed, 1)), "vault beats sealed");
            Assert.That(Mean(LootChestKind.Common, 40), Is.GreaterThan(Mean(LootChestKind.Common, 1) + 0.3f), "level appropriate: higher levels roll rarer");
            for (int i = 0; i <= 100; i++)
            {
                float roll = i / 100f * 0.9999f;
                Assert.That(LootWorldRules.RollRarity(LootChestKind.Vault, 1, false, roll), Is.GreaterThanOrEqualTo(Rarity.Epic));
                Assert.That(LootWorldRules.GunCount(LootChestKind.Common, roll), Is.InRange(1, 2));
                Assert.That(LootWorldRules.GunCount(LootChestKind.Vault, roll), Is.InRange(2, 3));
            }
            Assert.That(LootWorldRules.Cash(LootChestKind.Common, 1, 0f), Is.GreaterThan(0));
            Assert.That(LootWorldRules.Cash(LootChestKind.Vault, 30, 1f), Is.GreaterThan(LootWorldRules.Cash(LootChestKind.Common, 30, 1f)));
        }

        private static float Mean(LootChestKind k, int level)
        {
            float[] w = LootWorldRules.RarityWeights(k, level, false);
            float t = 0f, s = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                t += w[i];
                s += w[i] * i;
            }
            return s / t;
        }

        [Test]
        public void ChestMaterialsAreRealMaterials()
        {
            GRand r = new GRand(5);
            List<KeyValuePair<string, int>> into = new List<KeyValuePair<string, int>>();
            for (int k = 0; k < 3; k++)
                for (int reg = 0; reg < 3; reg++)
                    for (int i = 0; i < 30; i++)
                    {
                        into.Clear();
                        LootWorldRules.RollMaterials((LootChestKind)k, (LootRegion)reg, r, into);
                        Assert.That(into.Count, Is.GreaterThan(0));
                        foreach (KeyValuePair<string, int> m in into)
                        {
                            Assert.That(MaterialDb.Get(m.Key), Is.Not.Null, m.Key);
                            Assert.That(m.Value, Is.GreaterThan(0));
                        }
                    }
        }

        [Test]
        public void ChestsRefillAfterTwoGameDays()
        {
            float opened = LootWorldRules.GameHours(3, 14f);
            Assert.That(opened, Is.EqualTo(62f));
            Assert.That(LootWorldRules.Respawned(opened, opened + 47.9f), Is.False);
            Assert.That(LootWorldRules.Respawned(opened, LootWorldRules.GameHours(5, 14f)), Is.True, "two days later");
            Assert.That(LootWorldRules.HoursLeft(opened, opened + 12f), Is.EqualTo(36f));
        }

        [Test]
        public void GuardsScaleWithLevelAndBountiesAreDeterministic()
        {
            Assert.That(LootWorldRules.GuardCount(1, false), Is.EqualTo(3));
            Assert.That(LootWorldRules.GuardCount(50, true), Is.LessThanOrEqualTo(7));
            Assert.That(LootWorldRules.GuardCount(40, false), Is.GreaterThan(LootWorldRules.GuardCount(1, false)));
            Assert.That(LootWorldRules.GuardHealthScale(30, false), Is.GreaterThan(LootWorldRules.GuardHealthScale(1, false)));
            Assert.That(LootWorldRules.GuardKeyChance(true, false), Is.EqualTo(1f), "the crew boss always drops a key");
            Assert.That(LootWorldRules.LeaderIsChampion(1, true), Is.True, "bounty crews are led by a champion");
            List<int> sealedIdx = new List<int>();
            foreach (LootChestSite s in Chests) if (s.Kind == LootChestKind.Sealed) sealedIdx.Add(s.Index);
            HashSet<int> open = new HashSet<int> { sealedIdx[0], sealedIdx[1] };
            List<int> a = LootWorldRules.PickBounties(sealedIdx, open, 61724, 4, LootWorldRules.BountiesPerDay);
            List<int> b = LootWorldRules.PickBounties(sealedIdx, open, 61724, 4, LootWorldRules.BountiesPerDay);
            List<int> c = LootWorldRules.PickBounties(sealedIdx, open, 61724, 5, LootWorldRules.BountiesPerDay);
            Assert.That(a, Is.EqualTo(b), "same day, same board");
            Assert.That(a.Count, Is.EqualTo(3));
            Assert.That(a, Is.Not.EqualTo(c), "a new day posts a new board");
            foreach (int i in a) Assert.That(open.Contains(i), Is.False, "open chests are never posted");
        }

        [Test]
        public void SatchelsAndTheCollector()
        {
            int keys = 0;
            for (int i = 0; i < LootWorldLayout.CacheCount; i++)
            {
                if (LootWorldRules.CacheGivesKey(i)) keys++;
                else Assert.That(MaterialDb.Get(LootWorldRules.CacheCore(i)), Is.Not.Null, "satchel " + i + " core");
            }
            Assert.That(keys, Is.InRange(10, 16), "about a third hold a key");
            Assert.That(LootWorldRules.CollectorEarned(9), Is.EqualTo(0));
            Assert.That(LootWorldRules.CollectorEarned(10), Is.EqualTo(1));
            Assert.That(LootWorldRules.CollectorEarned(25), Is.EqualTo(2));
            Assert.That(LootWorldRules.CollectorEarned(40), Is.EqualTo(3));
            Assert.That(LootWorldRules.NextMilestone(12), Is.EqualTo(25));
            Assert.That(LootWorldRules.NextMilestone(40), Is.EqualTo(0));
            Assert.That(LootWorldRules.CollectorRewards, Is.EqualTo(new[] { Rarity.Rare, Rarity.Epic, Rarity.Legendary }));
            Assert.That(LootWorldRules.CollectorMilestones[LootWorldRules.CollectorMilestones.Length - 1], Is.EqualTo(LootWorldLayout.CacheCount), "the last reward needs every satchel");
        }

        [Test]
        public void CompassAndUniqueNames()
        {
            Assert.That(LootWorldRules.Compass(Vector3.zero, new Vector3(0f, 0f, 10f)), Is.EqualTo("N"));
            Assert.That(LootWorldRules.Compass(Vector3.zero, new Vector3(10f, 0f, 0f)), Is.EqualTo("E"));
            Assert.That(LootWorldRules.Compass(Vector3.zero, new Vector3(-10f, 0f, -10f)), Is.EqualTo("SW"));
            Assert.That(LootWorldRules.UniqueByName("skyline requiem "), Is.GreaterThanOrEqualTo(0));
            Assert.That(LootWorldRules.UniqueByName("No Such Gun"), Is.EqualTo(-1));
        }

        // ------------------------------------------------------------------ the GiveGun step

        private static string J(string s)
        {
            return s.Replace('\'', '"');
        }

        private static string OneQuest(string steps)
        {
            return J("{ 'Format': 1, 'Givers': [ { 'Guild': 'menders', 'At': 'Mara Solane', 'Name': 'Mara Solane' } ], 'Quests': [ { 'Id': 't_gun', 'Title': 'Test', "
                + "'Arc': 'menders', 'Order': 1, 'Summary': 'A test quest.', 'Phases': [ { 'Id': 'a', 'Steps': [ " + steps + " ] } ], 'Reward': { 'Cash': 100 } } ] }");
        }

        [Test]
        public void GiveGunStepIsValidated()
        {
            List<string> read = new List<string>();
            QuestReport ok = QuestTestData.ValidateQuestJson(OneQuest("{ 'Type': 'GiveGun' }, { 'Type': 'GiveGun', 'Rarity': 'Epic', 'Archetype': 'Shotgun' }, { 'Type': 'GiveGun', 'Unique': 'Skyline Requiem' }"), read);
            Assert.That(read, Is.Empty, string.Join("\n", read.ToArray()));
            Assert.That(ok.Errors, Is.Empty, ok.Details());
            QuestReport bad = QuestTestData.ValidateQuestJson(OneQuest("{ 'Type': 'GiveGun', 'Unique': 'Skyline Reqiem' }, { 'Type': 'GiveGun', 'Archetype': 'Shotgn' }, "
                + "{ 'Type': 'GiveGun', 'Unique': 'Skyline Requiem', 'Rarity': 'Rare' }, { 'Type': 'GiveGun', 'Rarity': 'Epic', 'Gear': 'Core' }"), read);
            Assert.That(QuestTestData.Mentions(bad.Errors, "unknown Unique 'Skyline Reqiem'"), Is.True, bad.Details());
            Assert.That(QuestTestData.Mentions(bad.Errors, "did you mean 'Skyline Requiem'"), Is.True, bad.Details());
            Assert.That(QuestTestData.Mentions(bad.Errors, "unknown Archetype 'Shotgn'"), Is.True, bad.Details());
            Assert.That(QuestTestData.Mentions(bad.Errors, "does not match the unique"), Is.True, bad.Details());
            Assert.That(QuestTestData.Mentions(bad.Errors, "field Gear is not used by step type GiveGun"), Is.True, bad.Details());
        }

        [Test]
        public void TheGunsmithArcEndsWithAUnique()
        {
            List<string> errors = new List<string>();
            bool unique = false;
            int quests = 0;
            foreach (KeyValuePair<string, QuestFile> kv in QuestTestData.Load<QuestFile>("Quests", errors, new QuestJsonTrace()))
            {
                if (!kv.Key.StartsWith("loot_", StringComparison.Ordinal) || kv.Value == null) continue;
                foreach (QuestDef q in kv.Value.Quests)
                {
                    quests++;
                    bool rewardStep = false;
                    QuestPhase last = q.Phases[q.Phases.Count - 1];
                    foreach (QuestStep s in last.Steps)
                    {
                        if (s.Type == "GiveGun" || (s.Type == "Give" && !string.IsNullOrEmpty(s.Gear))) rewardStep = true;
                        if (s.Type == "GiveGun" && LootWorldRules.UniqueByName(s.Unique) >= 0) unique = true;
                    }
                    Assert.That(rewardStep, Is.True, q.Id + " ends with a gun or gear reward");
                }
            }
            Assert.That(errors, Is.Empty, string.Join("\n", errors.ToArray()));
            Assert.That(quests, Is.EqualTo(8), "eight loot quests");
            Assert.That(unique, Is.True, "the arc's last quest hands over a named unique");
        }

        // ------------------------------------------------------------------ save block

        [Test]
        public void SaveBlockFieldsAreOnlyEverAppended()
        {
            Dictionary<string, string> now = new Dictionary<string, string>();
            foreach (FieldInfo f in typeof(LootWorldSave).GetFields(BindingFlags.Public | BindingFlags.Instance)) now[f.Name] = TypeName(f.FieldType);
            foreach (string entry in LootWorldSaveFields)
            {
                string[] parts = entry.Split(':');
                string have;
                Assert.That(now.TryGetValue(parts[0], out have), Is.True, "LootWorldSave." + parts[0] + " was renamed or removed. Only append new fields.");
                Assert.That(have, Is.EqualTo(parts[1]), "LootWorldSave." + parts[0] + " changed type. Add a new field instead.");
            }
            Assert.That(now.Count, Is.EqualTo(LootWorldSaveFields.Length), "LootWorldSave has a field missing from the freeze list in LootWorldTests: add it there.");
            Assert.That(Attribute.IsDefined(typeof(LootWorldSave), typeof(SerializableAttribute)), Is.True);
            LootWorldSave fresh = new SaveData().LootWorld;
            Assert.That(fresh, Is.Not.Null, "old saves get an empty block");
            Assert.That(fresh.BountyDay, Is.EqualTo(-1), "a fresh block posts a board on load");
        }

        private static string TypeName(Type t)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)) return "List<" + t.GetGenericArguments()[0].Name + ">";
            return t.Name;
        }
    }
}
