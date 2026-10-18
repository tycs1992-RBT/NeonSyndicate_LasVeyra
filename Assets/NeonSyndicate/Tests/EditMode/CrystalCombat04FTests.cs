using System;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class CrystalCombat04FTests
    {
        [Test] public void ReservoirStartsFullAndNeverBorrowsAnotherResource()
        {
            ResonanceCharge04F r = new ResonanceCharge04F();
            Assert.AreEqual(100f, r.Value);
            Assert.IsTrue(r.Spend(100f)); Assert.AreEqual(0f, r.Value);
            Assert.IsFalse(r.Spend(4f)); Assert.AreEqual(0f, r.Value);
            Assert.IsFalse(r.Spend(-1f)); Assert.IsFalse(r.Spend(float.NaN)); Assert.IsFalse(r.Spend(float.PositiveInfinity));
        }
        [TestCase(float.NaN, 100f)] [TestCase(float.PositiveInfinity, 100f)]
        [TestCase(-50f, 0f)] [TestCase(120f, 100f)] [TestCase(28f, 28f)]
        public void SaveChargeIsSanitized(float value, float expected) { Assert.AreEqual(expected, ResonanceCharge04F.Sanitize(value)); }
        [Test] public void OnlyDamageOnAnAlreadyHostileLifeCanEarn()
        {
            ResonanceCharge04F r = new ResonanceCharge04F(); r.Spend(100f);
            Assert.AreEqual(0f, r.Earn(1, 0f, false, false, 10f, true, false, false));
            Assert.AreEqual(0f, r.Earn(2, 0f, true, true, 10f, true, false, false));
            Assert.AreEqual(0f, r.Earn(3, 0f, true, false, 0f, true, false, false));
            Assert.AreEqual(0f, r.Earn(0, 0f, true, false, 10f, true, false, true));
            Assert.AreEqual(0f, r.Value);
        }
        [Test] public void RapidCallbacksAndLongLivedDummiesCannotMintUnlimitedCharge()
        {
            ResonanceCharge04F r = new ResonanceCharge04F(); r.Spend(100f);
            Assert.AreEqual(8f, r.Earn(1, 0f, true, false, 10f, true, false, false));
            Assert.AreEqual(0f, r.Earn(1, 0.1f, true, false, 10f, true, false, false));
            for (int i = 1; i < 30; i++) r.Earn(1, i, true, false, 10f, true, false, false);
            Assert.AreEqual(32f, r.Value);
            Assert.AreEqual(12f, r.Earn(1, 31f, true, false, 10f, false, false, true));
            Assert.AreEqual(0f, r.Earn(1, 32f, true, false, 10f, true, false, true));
            Assert.AreEqual(44f, r.Value);
        }
        [Test] public void CounterHarvestAndKillingBlowPayOnceEvenWhenHitWasRateLimited()
        {
            ResonanceCharge04F r = new ResonanceCharge04F(); r.Spend(100f);
            Assert.AreEqual(14f, r.Earn(11, 0f, true, false, 10f, true, true, false));
            Assert.AreEqual(12f, r.Earn(11, 0.1f, true, false, 10f, true, true, true));
            Assert.AreEqual(0f, r.Earn(11, 4f, true, false, 10f, true, true, true));
        }
        [Test] public void PaidLivesAreNeverEvictedIntoAnotherReward()
        {
            ResonanceCharge04F r = new ResonanceCharge04F(); r.Spend(100f);
            for (int i = 1; i <= ResonanceCharge04F.TrackedLives + 10; i++) { r.Earn(i, i, true, false, 1f, false, false, true); r.Spend(r.Value); }
            Assert.AreEqual(0f, r.Earn(1, 1000f, true, false, 1f, false, false, true));
            Assert.AreEqual(0f, r.Value);
        }
        [Test] public void RechargeAndRestoreDoNotAccumulateOverCapacity()
        {
            ResonanceCharge04F r = new ResonanceCharge04F(); r.Spend(40f);
            Assert.AreEqual(40f, r.Refill()); Assert.AreEqual(0f, r.Refill());
            r.Restore(15f); Assert.AreEqual(15f, r.Value);
        }
        [Test] public void AllSavedWeaponIdsAndSignaturesStayStable()
        {
            Assert.AreEqual(4, (int)WeaponId.VectorSidearm); Assert.AreEqual(18, (int)WeaponId.VoltKatana);
            Assert.AreEqual(21, (int)WeaponId.Loot1); Assert.AreEqual(25, (int)WeaponId.Count);
            for (int i = 0; i < 6; i++) Assert.AreEqual((CrystalBehavior)(i + 1), CrystalFocusDb.BehaviorOfUnique(17 + i));
            Assert.AreEqual("Asterblade", Weapons.Get(WeaponId.VoltKatana).Name);
            for (int i = 4; i <= 15; i++)
            {
                WeaponDef d = Weapons.Get((WeaponId)i);
                Assert.AreNotEqual(CrystalBehavior.None, WeaponController.BehaviorOf(d));
                Assert.IsFalse(d.UsesAmmo); Assert.AreEqual(0, d.ClipSize);
            }
        }
        [Test] public void OrdinaryLootConversionKeepsRecordIdentityAndSixSignatures()
        {
            foreach (LootArchetype a in Enum.GetValues(typeof(LootArchetype)))
            {
                LootGun g = new LootGun { Archetype = a, Unique = -1, Uid = 123 };
                CrystalBehavior b = CrystalCombatPolicy04F.LootBehavior(g);
                Assert.AreNotEqual(CrystalBehavior.None, b);
                Assert.AreEqual(a, g.Archetype); Assert.AreEqual(-1, g.Unique); Assert.AreEqual(123, g.Uid);
            }
            for (int i = 17; i <= 22; i++) Assert.AreEqual((CrystalBehavior)(i - 16), CrystalCombatPolicy04F.LootBehavior(new LootGun { Unique = i }));
        }
        [Test] public void FoundCellsAreFiniteAndTheirReceiptsSurviveRestoration()
        {
            ResonanceCharge04F r = new ResonanceCharge04F(); r.Spend(100f);
            Assert.AreEqual(30f, r.ClaimCell("dispatch.reserve"));
            Assert.AreEqual(0f, r.ClaimCell("dispatch.reserve"));
            Assert.AreEqual(0f, r.ClaimCell("unknown"));
            System.Collections.Generic.List<string> cells = new System.Collections.Generic.List<string>(r.CollectedCells);
            r.Restore(0f, cells);
            Assert.AreEqual(0f, r.ClaimCell("dispatch.reserve"));
            Assert.AreEqual(30f, r.ClaimCell("pressure.reserve"));
            Assert.AreEqual(30f, r.ClaimCell("forge.reserve"));
            r.Refill(); Assert.AreEqual(0f, r.ClaimCell("forge.reserve"));
        }
        [Test] public void ConvertedItemCardsUseActualCrystalDamageAndHeat()
        {
            for (int i = 0; i < LootGenerator.ArchetypeCount; i++)
            {
                LootGun g = LootGenerator.Generate(47290 + i, 5, Rarity.Uncommon, i);
                WeaponDef d = LootGenerator.ToWeaponDef(g, WeaponId.Loot1);
                LootArmory.CardStats card = LootArmory.StatsOf(g);
                Assert.IsTrue(card.Focus && card.Heat);
                bool beam = WeaponController.BehaviorOf(d) == CrystalBehavior.PrismThread;
                Assert.AreEqual(beam ? d.Damage / CrystalFocusDb.BeamTick : d.Damage, card.Damage);
                Assert.Greater(card.SustainedDps, 0f);
                Assert.AreEqual(CrystalFocusDb.Get(WeaponController.BehaviorOf(d)).Range, card.Range);
            }
        }
        [Test] public void BufferedCrystalCombosRespectWeaponRecoveryAndStayFinite()
        {
            MeleeMove daggers = MeleeMoves.Lights(MeleeClass.Blade)[0];
            MeleeMove katana = MeleeMoves.Lights(MeleeClass.Katana)[0];
            MeleeMove maul = MeleeMoves.Heavy(MeleeClass.Maul);
            float fast = MeleeMoves.InputBufferSeconds(daggers);
            float slow = MeleeMoves.InputBufferSeconds(maul);
            Assert.That(fast, Is.GreaterThanOrEqualTo(0.5f));
            Assert.That(slow, Is.GreaterThan(1f));
            Assert.That(slow, Is.GreaterThan(fast));
            Assert.That(MeleeMoves.InputBufferSeconds(katana), Is.LessThan(slow));
            Assert.That(slow, Is.LessThanOrEqualTo(1.5f));
            Assert.That(MeleeMoves.InputBufferSeconds(null), Is.EqualTo(0.5f));
            foreach (MeleeClass style in new[] { MeleeClass.Blade, MeleeClass.Katana, MeleeClass.Maul, MeleeClass.Gauntlet })
                foreach (MeleeMove move in MeleeMoves.Lights(style))
                    Assert.That(MeleeMoves.InputBufferSeconds(move), Is.InRange(0.5f, 1.5f));
        }

        [Test] public void EconomicalBasicShotAndMeleeGiveARecoverableCombatLoop()
        {
            ResonanceCharge04F r = new ResonanceCharge04F();
            float cost = CrystalCombatPolicy04F.ShotCost(CrystalBehavior.Shardwake, false);
            int fired = 0; while (r.Spend(cost)) fired++;
            Assert.AreEqual(25, fired); Assert.AreEqual(0f, r.Value);
            Assert.AreEqual(8f, r.Earn(7, 0f, true, false, 5f, true, false, false));
            Assert.IsTrue(r.Spend(cost)); Assert.IsTrue(r.Spend(cost)); Assert.IsFalse(r.Spend(cost));
            Assert.AreEqual(3, MeleeMoves.Lights(MeleeClass.Katana).Length);
            Assert.Less(MeleeMoves.Lights(MeleeClass.Blade)[0].Total, MeleeMoves.Lights(MeleeClass.Maul)[0].Total);
        }
    }
}
