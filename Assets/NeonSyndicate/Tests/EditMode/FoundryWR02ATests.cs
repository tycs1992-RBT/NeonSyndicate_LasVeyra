using NUnit.Framework;
namespace NeonSyndicate.Tests
{
    public sealed class FoundryWR02ATests
    {
        private static HeartRewritePlanWR01 Archive()
        { return new HeartRewritePlanWR01 { Seed = 479130, Service = "archive", Family = "WRArchiveCrown", StableId = "revision:10/venue:archive/seed:479130/crown" }; }
        [Test] public void OnlyTheExactExistingArchiveIdentityQualifies()
        {
            Assert.That(HeartPlanFR01.FoundryArchiveWR02A(Archive()), Is.True);
            Assert.That(HeartPlanFR01.FoundryArchiveWR02A(null), Is.False);
            HeartRewritePlanWR01 plan = Archive(); plan.Seed++; Assert.That(HeartPlanFR01.FoundryArchiveWR02A(plan), Is.False);
            plan = Archive(); plan.Service = "office"; Assert.That(HeartPlanFR01.FoundryArchiveWR02A(plan), Is.False);
            plan = Archive(); plan.Family = "WROfficeCrown"; Assert.That(HeartPlanFR01.FoundryArchiveWR02A(plan), Is.False);
            plan = Archive(); plan.StableId += "other"; Assert.That(HeartPlanFR01.FoundryArchiveWR02A(plan), Is.False);
        }
        [TestCase("ARCHIVE_CLOSEUP", true)] [TestCase("ARCHIVE_PLAYER_SCALE", true)] [TestCase("HOME", false)] [TestCase("CREATURE_IDLE", false)] [TestCase(null, false)]
        public void FoundryViewRolesDoNotRelabelOriginalCreatures(string view, bool expected)
        { Assert.That(HeartPlanFR01.FoundryViewRequestedWR02A(view), Is.EqualTo(expected)); }
        [Test] public void EveryObservedArchitectureGateIsRequired()
        {
            Assert.That(HeartPlanFR01.FoundryViewObservedWR02A(null), Is.False);
            FantasyProofSnapshotV1 proof = new FantasyProofSnapshotV1(); Assert.That(HeartPlanFR01.FoundryViewObservedWR02A(proof), Is.False);
            proof.worldRewrite.selected = true; proof.foundryWR02A.architecturePrepared = true; proof.foundryWR02A.architectureAdmitted = true; proof.foundryWR02A.architectureActive = true; proof.foundryWR02A.architectureCameraCandidate = true;
            Assert.That(HeartPlanFR01.FoundryViewObservedWR02A(proof), Is.True);
            proof.foundryWR02A.architectureActive = false; Assert.That(HeartPlanFR01.FoundryViewObservedWR02A(proof), Is.False); proof.foundryWR02A.architectureActive = true;
            proof.foundryWR02A.architectureCameraCandidate = false; Assert.That(HeartPlanFR01.FoundryViewObservedWR02A(proof), Is.False);
        }
        [Test] public void PreparationNeverGrantsCreatureMotionOrOwnerAcceptance()
        {
            FantasyFoundryProofWR02A proof = new FantasyFoundryProofWR02A { creatureCandidatePrepared = true };
            Assert.That(proof.creatureActivationAllowed, Is.False); Assert.That(proof.nativeAcceptance, Is.EqualTo("NOT RUN")); Assert.That(proof.creatureStatus, Does.Contain("CANDIDATE ONLY"));
        }
    }
}
