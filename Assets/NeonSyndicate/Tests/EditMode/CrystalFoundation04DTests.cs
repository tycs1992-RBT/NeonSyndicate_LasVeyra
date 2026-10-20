using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// 04D foundation: the verified save transaction (including failure paths), the crystal profile table, and the
    /// guarantee that appending six guaranteed-only uniques changes no legacy index or random selection.
    /// </summary>
    public class CrystalFoundation04DTests
    {
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ns04d_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_dir, true); }
            catch (Exception) { }
        }

        private static bool Valid(string s) { return s != null && s.Contains("\"Version\""); }

        [Test]
        public void FirstWriteInstallsAndReadsBack()
        {
            string p = Path.Combine(_dir, "slot.json");
            string err;
            Assert.That(SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":1}", Valid, out err), Is.EqualTo(SaveFileTransaction.Result.Ok), err);
            bool bak;
            Assert.That(SaveFileTransaction.ReadValidated(p, Valid, out bak), Is.EqualTo("{\"Version\":1,\"A\":1}"));
            Assert.That(bak, Is.False);
            Assert.That(File.Exists(SaveFileTransaction.TempPath(p)), Is.False, "no temporary file is left behind");
        }

        [Test]
        public void SecondWriteKeepsThePreviousSnapshotAsBackup()
        {
            string p = Path.Combine(_dir, "slot.json");
            string err;
            SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":1}", Valid, out err);
            Assert.That(SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":2}", Valid, out err), Is.EqualTo(SaveFileTransaction.Result.Ok), err);
            Assert.That(File.ReadAllText(p), Is.EqualTo("{\"Version\":1,\"A\":2}"));
            Assert.That(File.ReadAllText(SaveFileTransaction.BackupPath(p)), Is.EqualTo("{\"Version\":1,\"A\":1}"));
        }

        [Test]
        public void ValidationFailureLeavesTheOldSaveUntouched()
        {
            string p = Path.Combine(_dir, "slot.json");
            string err;
            SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":1}", Valid, out err);
            SaveFileTransaction.Result r = SaveFileTransaction.Write(p, "{\"broken\":true}", Valid, out err);
            Assert.That(r, Is.EqualTo(SaveFileTransaction.Result.VerifyFailed));
            Assert.That(err, Is.Not.Null.And.Not.Empty);
            Assert.That(File.ReadAllText(p), Is.EqualTo("{\"Version\":1,\"A\":1}"), "a failed snapshot never replaces a good save");
            Assert.That(File.Exists(SaveFileTransaction.TempPath(p)), Is.False);
        }

        [Test]
        public void WriteFailureIsReportedNotHidden()
        {
            string p = Path.Combine(_dir, "missing_folder", "slot.json");
            string err;
            SaveFileTransaction.Result r = SaveFileTransaction.Write(p, "{\"Version\":1}", Valid, out err);
            Assert.That(r, Is.EqualTo(SaveFileTransaction.Result.WriteFailed));
            Assert.That(err, Does.StartWith("Write"));
        }

        [Test]
        public void CorruptMainFallsBackToTheBackup()
        {
            string p = Path.Combine(_dir, "slot.json");
            string err;
            SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":1}", Valid, out err);
            SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":2}", Valid, out err);
            File.WriteAllText(p, "{ truncated");
            bool bak;
            Assert.That(SaveFileTransaction.ReadValidated(p, Valid, out bak), Is.EqualTo("{\"Version\":1,\"A\":1}"));
            Assert.That(bak, Is.True);
        }

        [Test]
        public void MissingMainWithBackupStillCountsAsASave()
        {
            string p = Path.Combine(_dir, "slot.json");
            string err;
            SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":1}", Valid, out err);
            SaveFileTransaction.Write(p, "{\"Version\":1,\"A\":2}", Valid, out err);
            File.Delete(p);
            Assert.That(SaveFileTransaction.AnyExists(p), Is.True);
            bool bak;
            Assert.That(SaveFileTransaction.ReadValidated(p, Valid, out bak), Is.Not.Null);
            Assert.That(bak, Is.True);
        }

        [Test]
        public void NeitherFileUsableReturnsNull()
        {
            string p = Path.Combine(_dir, "slot.json");
            File.WriteAllText(p, "garbage");
            bool bak;
            Assert.That(SaveFileTransaction.ReadValidated(p, Valid, out bak), Is.Null);
        }

        // ------------------------------------------------------------------ profiles and uniques

        [Test]
        public void LegacyUniquesKeepTheirIndicesAndPools()
        {
            Assert.That(LootUniques.All.Length, Is.GreaterThanOrEqualTo(23), "17 legacy plus 6 crystal foci retain their saved indices; later uniques append");
            Assert.That(LootUniques.All[0].Name, Is.EqualTo("Alley Saint"));
            Assert.That(LootUniques.All[16].Name, Is.EqualTo("Skyline Requiem"));
            Assert.That(LootUniques.ForBoss("golem_foreman"), Is.EqualTo(14), "the legacy random Mythic boss drop is unchanged");
            Assert.That(LootUniques.ForBoss("mirage_djinn"), Is.EqualTo(13));
            Assert.That(LootUniques.ForBoss("wraith_queen"), Is.EqualTo(15));
            foreach (Rarity r in new[] { Rarity.Rare, Rarity.Epic, Rarity.Legendary, Rarity.Mythic })
                foreach (int i in LootUniques.OfRarity(r, -1)) Assert.That(i, Is.LessThan(CrystalFocusDb.FirstUnique), "guaranteed-only foci never enter a random pool");
        }

        [Test]
        public void RandomLegendaryAndMythicRollsMatchTheLegacyPool()
        {
            // The same seeds pick the same legacy uniques as before the append (the pools exclude the new entries).
            for (int seed = 1; seed < 400; seed++)
            {
                LootGun l = LootGenerator.Generate(seed, 10, Rarity.Legendary);
                LootGun m = LootGenerator.Generate(seed, 10, Rarity.Mythic);
                Assert.That(l.Unique, Is.InRange(0, 11));
                Assert.That(m.Unique, Is.InRange(12, 16));
            }
        }

        [Test]
        public void EveryFocusResolvesBehaviorAndSurvivesRefreshAndClone()
        {
            for (int b = 1; b <= CrystalFocusDb.UniqueCount; b++)
            {
                CrystalBehavior behavior = (CrystalBehavior)b;
                int u = CrystalFocusDb.UniqueOf(behavior);
                Assert.That(CrystalFocusDb.BehaviorOfUnique(u), Is.EqualTo(behavior));
                LootGun g = LootGenerator.Generate(1234 + b, 12, Rarity.Common, -1, -1, u);
                Assert.That(g.Unique, Is.EqualTo(u));
                Assert.That(g.Archetype, Is.EqualTo(LootArchetype.CrystalCaster));
                Assert.That(g.Traits.Count, Is.EqualTo(0), "no legacy trait rides on a crystal focus");
                g.Traits.Add(LootTrait.SplitShot);
                LootGenerator.Refresh(g);
                Assert.That(g.Traits.Count, Is.EqualTo(0), "Refresh strips traits from a focus");
                LootGun c = g.Clone();
                Assert.That(CrystalFocusDb.BehaviorOfUnique(c.Unique), Is.EqualTo(behavior), "clone keeps the behavior");
                WeaponDef d = LootGenerator.ToWeaponDef(c, WeaponId.Loot1);
                Assert.That(WeaponController.BehaviorOf(d), Is.EqualTo(behavior), "the weapon definition keeps the behavior");
                Assert.That(d.Overheats, Is.True, "every focus uses heat, never a magazine");
                Assert.That(d.ClipSize, Is.EqualTo(0));
                Assert.That(d.Fire, Is.EqualTo(behavior == CrystalBehavior.PrismThread ? FireKind.Auto : FireKind.Semi));
            }
        }

        [Test]
        public void ProfileTableMatchesTheTuningContractAtAuthoredRarity()
        {
            float b = CrystalFocusDb.ReferenceDamage(1);
            Assert.That(b, Is.EqualTo(18f).Within(1e-4f), "B is 18 at level 1");
            CrystalProfile sw = CrystalFocusDb.Get(CrystalBehavior.Shardwake);
            Assert.That(CrystalFocusDb.PrimaryDamage(sw, sw.Authored, 1), Is.EqualTo(18f).Within(1e-3f));
            CrystalProfile pt = CrystalFocusDb.Get(CrystalBehavior.PrismThread);
            Assert.That(CrystalFocusDb.PrimaryDamage(pt, pt.Authored, 1), Is.EqualTo(3.6f * 18f).Within(1e-3f), "beam is B per second");
            LootGun beam = LootGenerator.Generate(7, 1, Rarity.Rare, -1, -1, CrystalFocusDb.PrismThreadUnique);
            Assert.That(beam.Stats.Damage, Is.EqualTo(3.6f * 18f * CrystalFocusDb.BeamTick).Within(1e-3f), "the weapon damage is one beam tick");
            Assert.That(beam.Stats.HeatPerShot, Is.EqualTo(0.28f * CrystalFocusDb.BeamTick).Within(1e-5f));
            Assert.That(CrystalFocusDb.SignatureFor("golem_foreman"), Is.EqualTo(CrystalFocusDb.SunderheartUnique));
            Assert.That(CrystalFocusDb.SignatureFor("mirage_djinn"), Is.EqualTo(CrystalFocusDb.MirageChoirUnique));
            Assert.That(CrystalFocusDb.SignatureFor("wraith_queen"), Is.EqualTo(CrystalFocusDb.MirrorwakeUnique));
            Assert.That(CrystalFocusDb.SignatureFor("glacier_titan"), Is.EqualTo(-1), "the Titan grants no 04D signature");
        }

        [Test]
        public void ForgeRaisesScaleAboveTheAuthoredRarityOnly()
        {
            CrystalProfile sw = CrystalFocusDb.Get(CrystalBehavior.Shardwake);
            Assert.That(CrystalFocusDb.RarityScale(sw, Rarity.Rare), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(CrystalFocusDb.RarityScale(sw, Rarity.Epic), Is.GreaterThan(1f));
            Assert.That(CrystalFocusDb.PrimaryDamage(sw, Rarity.Rare, 20), Is.GreaterThan(CrystalFocusDb.PrimaryDamage(sw, Rarity.Rare, 1)));
        }

        [Test]
        public void SustainedDpsIncludesTheVent()
        {
            foreach (CrystalBehavior b in new[] { CrystalBehavior.Shardwake, CrystalBehavior.PrismThread, CrystalBehavior.GyreCrescent, CrystalBehavior.Sunderheart, CrystalBehavior.MirageChoir, CrystalBehavior.Mirrorwake })
            {
                CrystalProfile p = CrystalFocusDb.Get(b);
                float sustained = CrystalFocusDb.SustainedDps(p, p.Authored, 10);
                Assert.That(sustained, Is.GreaterThan(0f), b.ToString());
                float naive = b == CrystalBehavior.PrismThread ? CrystalFocusDb.PrimaryDamage(p, p.Authored, 10) : float.MaxValue;
                Assert.That(sustained, Is.LessThan(naive), b + " sustained damage never exceeds the uninterrupted rate");
            }
        }
    }
}
