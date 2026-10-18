using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class CombatLife04GTests
    {
        [TestCase(MeleeClass.Blade)] [TestCase(MeleeClass.Katana)] [TestCase(MeleeClass.Maul)]
        public void DefenseCannotSkipTheHitWindow(MeleeClass style)
        {
            foreach (MeleeMove move in MeleeMoves.Lights(style))
            {
                Assert.IsFalse(CombatResponse04G.CanDefend(move, move.Windup + move.Active - 0.001f, style));
                Assert.IsTrue(CombatResponse04G.CanDefend(move, move.Total, style));
                Assert.That(CombatResponse04G.DefenseTime(move, style), Is.InRange(move.Windup + move.Active, move.Total));
            }
        }
        [Test] public void EarlyQueuedHeavySurvivesItsMaximumChargeCommitment()
        {
            MeleeMove heavy = MeleeMoves.Heavy(MeleeClass.Maul);
            float earliestNext = heavy.Windup + heavy.ChargeMax + heavy.Active + heavy.Recovery * heavy.CancelAt;
            Assert.Greater(MeleeMoves.InputBufferSeconds(heavy), earliestNext);
        }
        [TestCase(MeleeClass.Blade)] [TestCase(MeleeClass.Katana)] [TestCase(MeleeClass.Maul)]
        public void FinisherMustRecoverBeforeAnotherCombo(MeleeClass style)
        {
            MeleeMove[] chain = MeleeMoves.Lights(style);
            Assert.AreEqual(1f, chain[chain.Length - 1].CancelAt);
            Assert.LessOrEqual(MeleeMoves.InputBufferSeconds(chain[chain.Length - 1]), 2.2f);
        }
        [Test] public void RollAndPursuitRetainTheWeaponCommitment()
        {
            Assert.Less(MeleeMoves.RollAttack(MeleeClass.Blade).Windup, MeleeMoves.RollAttack(MeleeClass.Katana).Windup);
            Assert.Greater(MeleeMoves.RollAttack(MeleeClass.Maul).Windup, 0.25f);
            Assert.Greater(MeleeMoves.Running(MeleeClass.Maul).Stamina, MeleeMoves.Running(MeleeClass.Blade).Stamina);
        }
        [TestCase(0f, 0.15f)] [TestCase(1.4f, 1f)] [TestCase(10f, 1f)]
        public void ApproachSlowsNearItsFormation(float distance, float expected)
        { Assert.AreEqual(expected, CompanionMotion04G.Arrival(distance), 0.001f); }
        [Test] public void TurningAndProgressionStayBounded()
        {
            Assert.Less(CompanionMotion04G.TurnSpeed(160f), CompanionMotion04G.TurnSpeed(15f));
            Assert.AreEqual(1f, CompanionMotion04G.SupportRecovery(BondIds.RankBonded, 3));
            Assert.AreEqual(0.88f, CompanionMotion04G.SupportRecovery(BondIds.RankHarmonized, 9), 0.001f);
        }
        [Test] public void EventDrakeCannotBeOneShotOrHealedByInvalidDamage()
        {
            Assert.AreEqual(60f, EventDrakeRules04G.AfterHit(90f, 900f));
            Assert.AreEqual(0f, EventDrakeRules04G.AfterHit(10f, 20f));
            Assert.AreEqual(90f, EventDrakeRules04G.AfterHit(90f, float.NaN));
            Assert.AreEqual(90f, EventDrakeRules04G.AfterHit(90f, -10f));
        }
        [TestCase(false, false, 1f, false)]
        [TestCase(true, false, 1f, true)]
        [TestCase(false, true, 1f, true)]
        [TestCase(false, false, 10f, true)]
        public void ParcelDropsOnlyForRecoverableInterruptions(bool vehicle, bool hurt, float step, bool drops)
        { Assert.AreEqual(drops, CourierRules04G.ShouldDrop(vehicle, hurt, step)); }

        [Test] public void CourierUsesTheExistingContractGuidanceAndStrictSchema()
        {
            Assert.IsTrue(GuidanceRules04F.IsInteriorContract("uh05_04g_01_courier"));
            Assert.IsTrue(QuestCatalog.StepFields("Courier").Contains("To"));
            Assert.IsFalse(QuestCatalog.StepFields("Courier").Contains("Waves"));
            CollectionAssert.AreEquivalent(new[] { "At", "To" }, QuestCatalog.StepRequired("Courier"));
        }

        [TestCase("malik", 2)] [TestCase("police", 1)] [TestCase("swat", 2)]
        public void NewCostumesRetainFiniteSkinningAndTheOriginalRig(string name, int detail)
        {
            Appearance look = TestData.Look(name);
            BodyData body = BodyBuilder.Build(look, RigLayout.From(look), detail);
            Assert.Less(body.Vertices.Count, detail == 2 ? 13500 : 8200);
            foreach (BoneWeight weight in body.Weights)
                Assert.AreEqual(1f, weight.weight0 + weight.weight1 + weight.weight2 + weight.weight3, 0.001f);
        }
    }
}
