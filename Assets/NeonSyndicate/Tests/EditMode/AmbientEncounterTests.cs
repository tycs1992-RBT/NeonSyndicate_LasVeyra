using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Automatic encounter boundaries: protection at either end, deliberate consent, and unchanged key opening.</summary>
    public sealed class AmbientEncounterTests
    {
        [Test]
        public void CrossingTheBoundaryCannotSpawnAnAttackIntoAProtectedArea()
        {
            Assert.That(AmbientEncounterRules.MaySpawn(true, false, false), Is.False, "player inside, candidate outside");
            Assert.That(AmbientEncounterRules.MaySpawn(false, true, false), Is.False, "player outside, candidate inside");
            Assert.That(AmbientEncounterRules.MaySpawn(false, false, false), Is.True);
        }

        [Test]
        public void ExplicitRequestsSurviveBothProtectionChecks()
        {
            Assert.That(AmbientEncounterRules.MaySpawn(true, true, true), Is.True);
            Assert.That(AmbientEncounterRules.RetireForSafety(true, true, true, false), Is.False);
        }

        [Test]
        public void AnUnrequestedEventRetiresWithoutTreatingProximityAsConsent()
        {
            Assert.That(AmbientEncounterRules.RetireForSafety(true, false, false, false), Is.True);
            Assert.That(AmbientEncounterRules.RetireForSafety(false, true, false, false), Is.True);
            Assert.That(AmbientEncounterRules.RetireForSafety(false, false, false, false), Is.False);
            Assert.That(AmbientEncounterRules.RetireForSafety(true, true, false, true), Is.False, "a completed reward is never rolled back");
        }

        [Test]
        public void HavingAKeyPreservesNormalOpeningInsteadOfStartingCombat()
        {
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(true, true, false, false, false, false), Is.False);
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(true, false, false, false, false, false), Is.True);
        }

        [Test]
        public void GuardChallengeCannotRespawnDefeatedCrewsOrOverrideMissions()
        {
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(true, false, true, false, false, false), Is.False);
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(true, false, false, true, false, false), Is.False);
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(true, false, false, false, true, false), Is.False);
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(true, false, false, false, false, true), Is.False);
            Assert.That(AmbientEncounterRules.OfferGuardChallenge(false, false, false, false, false, false), Is.False);
        }
    }
}
