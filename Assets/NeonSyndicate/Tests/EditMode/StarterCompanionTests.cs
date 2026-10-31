using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>The starter companion (StarterCompanion.Due): at once in Free Roam, after the prologue in the story, never during a mission, never twice. Engine-free.</summary>
    public sealed class StarterCompanionTests
    {
        [Test]
        public void ArrivesInFreeRoamAndAfterThePrologueOnly()
        {
            Assert.That(StarterCompanion.Due(true, true, 0, false, false, true), Is.True, "Free Roam: at once");
            Assert.That(StarterCompanion.Due(true, false, 0, false, false, true), Is.False, "story: not during the prologue");
            Assert.That(StarterCompanion.Due(true, false, 1, false, false, true), Is.True, "story: after the prologue");
            Assert.That(StarterCompanion.Due(true, false, 3, true, false, true), Is.False, "never in the middle of a mission");
            Assert.That(StarterCompanion.Due(true, true, 5, false, true, true), Is.False, "never twice");
            Assert.That(StarterCompanion.Due(false, true, 0, false, false, true), Is.False, "only while playing");
            Assert.That(StarterCompanion.Due(true, true, 0, false, false, false), Is.False, "not while dead");
        }
    }
}
