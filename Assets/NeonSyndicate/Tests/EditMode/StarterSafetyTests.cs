using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Pure policy regression tests, with no scene, clocks, GameObject creation or Physics calls.</summary>
    public sealed class StarterSafetyTests
    {
        [TestCase(140f)]
        [TestCase(120f)]
        [TestCase(100f)]
        public void RadiusIncludesItsBoundaryButNotTheNextStreet(float radius)
        {
            Vector3 center = new Vector3(310f, 0f, -420f);
            Assert.That(StarterSafetyRules.InRadius(center + new Vector3(radius, 200f, 0f), center, radius), Is.True);
            Assert.That(StarterSafetyRules.InRadius(center + new Vector3(radius + 0.1f, 0f, 0f), center, radius), Is.False);
            Assert.That(StarterSafetyRules.InRadius(center + new Vector3(radius, 0f, radius), center, radius), Is.False,
                "The circular refuge must not accidentally protect the square bounding box.");
        }

        [Test]
        public void PromenadeScopeIsBounded()
        {
            Assert.That(StarterSafetyRules.InPromenade(new Vector3(22f, 0f, -85f)), Is.True);
            Assert.That(StarterSafetyRules.InPromenade(new Vector3(238f, 10f, 85f)), Is.True);
            Assert.That(StarterSafetyRules.InPromenade(new Vector3(21.9f, 0f, 0f)), Is.False);
            Assert.That(StarterSafetyRules.InPromenade(new Vector3(239f, 0f, 0f)), Is.False);
            Assert.That(StarterSafetyRules.InPromenade(new Vector3(132f, 0f, 85.1f)), Is.False);
        }

        [Test]
        public void QuietAreaDoesNotExpireAfterFiveMinutes()
        {
            Assert.That(StarterSafetyRules.Blocks(true, 0f, 0f), Is.True);
            Assert.That(StarterSafetyRules.Blocks(true, 300f, 0f), Is.True);
            Assert.That(StarterSafetyRules.Blocks(true, 3600f, 0f), Is.True);
            Assert.That(StarterSafetyRules.Blocks(false, 300f, 0f), Is.False);
        }

        [Test]
        public void ProvocationExpiresAtExactlySixtySeconds()
        {
            float until = 10f + StarterSafetyRules.AggressionCooldown;
            Assert.That(StarterSafetyRules.Blocks(true, 69.99f, until), Is.False);
            Assert.That(StarterSafetyRules.Blocks(true, 70f, until), Is.True);
        }

        [Test]
        public void SavedHeatDoesNotOverrideAmbientWelcomeButExplicitRetaliationDoes()
        {
            Assert.That(StarterSafetyRules.GangHostile(false, true, false, false, 1f), Is.False);
            Assert.That(StarterSafetyRules.GangHostile(false, true, true, true, 0f), Is.True);
            Assert.That(StarterSafetyRules.GangHostile(false, true, true, false, 1f), Is.False,
                "Fighting wildlife must not retarget an unprovoking player.");
            Assert.That(StarterSafetyRules.GangHostile(false, false, false, false, 1f), Is.True);
        }

        [Test]
        public void AuthoredActorsBypassOnlyTheirOwnAmbientGate()
        {
            Assert.That(StarterSafetyRules.GangHostile(true, true, true, false, 0f), Is.True);
            Assert.That(StarterSafetyRules.GangHostile(true, true, false, false, 0.85f), Is.True);
            Assert.That(StarterSafetyRules.GangHostile(false, true, false, false, 0.85f), Is.False);
            Assert.That(StarterSafetyRules.GangHostile(true, true, false, false, 0f), Is.False);
        }

        [Test]
        public void AimAndTrafficDoNotProvokeAndPracticeNoiseStaysLocal()
        {
            Assert.That(StarterSafetyRules.IsAggressiveThreat(ThreatKind.Aim, false), Is.False);
            Assert.That(StarterSafetyRules.IsAggressiveThreat(ThreatKind.CarCrash, false), Is.False);
            foreach (ThreatKind kind in new[] { ThreatKind.Gunshot, ThreatKind.Explosion, ThreatKind.Violence, ThreatKind.Fire, ThreatKind.Power })
            {
                Assert.That(StarterSafetyRules.IsAggressiveThreat(kind, false), Is.True, kind.ToString());
                Assert.That(StarterSafetyRules.IsAggressiveThreat(kind, true), Is.False, kind.ToString());
            }
        }

        [Test]
        public void HumanoidWildlifeIsQuietButAuthoredOrProvokedCreaturesStillFight()
        {
            Assert.That(StarterSafetyRules.QuietAmbientCreature(true, false, false, false, false, true), Is.True);
            Assert.That(StarterSafetyRules.QuietAmbientCreature(true, false, true, false, false, true), Is.False);
            Assert.That(StarterSafetyRules.QuietAmbientCreature(true, true, false, false, false, true), Is.False);
            Assert.That(StarterSafetyRules.QuietAmbientCreature(true, false, false, true, false, true), Is.False);
            Assert.That(StarterSafetyRules.QuietAmbientCreature(true, false, false, false, true, true), Is.False);
            Assert.That(StarterSafetyRules.QuietAmbientCreature(false, false, false, false, false, true), Is.False);
            Assert.That(StarterSafetyRules.QuietAmbientCreature(true, false, false, false, false, false), Is.False);
        }
    }
}
