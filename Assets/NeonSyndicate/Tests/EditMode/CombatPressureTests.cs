using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class CombatPressureTests
    {
        [Test]
        public void StoryIsWelcomingWithoutBeingInvulnerable()
        {
            Assert.That(CombatPressureRules.IncomingDamage(DifficultyLevel.Story), Is.EqualTo(0.35f));
            Assert.That(CombatPressureRules.IncomingDamage(DifficultyLevel.Story), Is.GreaterThan(0f));
            Assert.That(CombatPressureRules.ReactionSeconds(DifficultyLevel.Story), Is.EqualTo(1.8f));
            Assert.That(CombatPressureRules.GunSpread(DifficultyLevel.Story), Is.EqualTo(2.2f));
            Assert.That(CombatPressureRules.WindupScale(DifficultyLevel.Story), Is.EqualTo(1.45f));
        }

        [Test]
        public void ExistingHarderDifficultyDamageAndTimingRemain()
        {
            Assert.That(CombatPressureRules.IncomingDamage(DifficultyLevel.Balanced), Is.EqualTo(0.62f));
            Assert.That(CombatPressureRules.IncomingDamage(DifficultyLevel.Veteran), Is.EqualTo(1f));
            Assert.That(CombatPressureRules.ReactionSeconds(DifficultyLevel.Balanced), Is.EqualTo(0.95f));
            Assert.That(CombatPressureRules.ReactionSeconds(DifficultyLevel.Veteran), Is.EqualTo(0.6f));
            Assert.That(CombatPressureRules.GunSpread(DifficultyLevel.Balanced), Is.EqualTo(1.35f));
            Assert.That(CombatPressureRules.GunSpread(DifficultyLevel.Veteran), Is.EqualTo(1f));
            Assert.That(CombatPressureRules.WindupScale(DifficultyLevel.Balanced), Is.EqualTo(1f));
            Assert.That(CombatPressureRules.WindupScale(DifficultyLevel.Veteran), Is.EqualTo(0.85f));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void MixedRangedAttackersCannotGetIndependentBudgets(int cap)
        {
            for (int npc = 0; npc <= cap + 1; npc++)
                for (int creature = 0; creature <= cap + 1; creature++)
                    Assert.That(CombatPressureRules.CanAdmitRanged(npc, creature, cap),
                        Is.EqualTo(npc + creature < cap), "NPC " + npc + ", creature " + creature + ", cap " + cap);
            Assert.That(CombatPressureRules.CanAdmitRanged(1, 0, 1), Is.False);
            Assert.That(CombatPressureRules.CanAdmitRanged(0, 1, 1), Is.False);
        }

        [Test]
        public void InvalidCountsNeverCreatePhantomSlots()
        {
            Assert.That(CombatPressureRules.CanAdmitRanged(-1, 0, 1), Is.False);
            Assert.That(CombatPressureRules.CanAdmitRanged(0, -1, 1), Is.False);
            Assert.That(CombatPressureRules.CanAdmitRanged(0, 0, 0), Is.False);
            Assert.That(CombatPressureRules.CanAdmitRanged(int.MaxValue, int.MaxValue, 4), Is.False);
        }
    }
}
