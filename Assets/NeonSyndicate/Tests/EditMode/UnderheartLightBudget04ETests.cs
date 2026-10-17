using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The Underheart practical light budget beside a boss fight (UnderheartLightBudget): one region light fewer while an
    /// active fight's arena is near the view, so region plus fight lights stay at the region's own budget. Engine-free.
    /// </summary>
    public sealed class UnderheartLightBudget04ETests
    {
        private static readonly Vector3 Arena = new Vector3(120f, -26f, -90f);

        [Test]
        public void ANearbyBossFightTakesOneRegionLight()
        {
            Assert.That(UnderheartLightBudget.Budget(false, false), Is.EqualTo(8));
            Assert.That(UnderheartLightBudget.Budget(true, false), Is.EqualTo(5));
            Assert.That(UnderheartLightBudget.Budget(false, true), Is.EqualTo(7));
            Assert.That(UnderheartLightBudget.Budget(true, true), Is.EqualTo(4));
        }

        [Test]
        public void RegionPlusFightLightsStayAtTheRegionBudget()
        {
            // The region numbers agree with what the boss budget assumes, and the fight's key light fills the freed place.
            Assert.That(UnderheartLightBudget.Budget(false, false), Is.EqualTo(BossLightBudget04E.RegionLights));
            Assert.That(UnderheartLightBudget.Budget(true, false), Is.EqualTo(BossLightBudget04E.RegionLightsLow));
            foreach (bool low in new[] { false, true })
                Assert.That(UnderheartLightBudget.Budget(low, true) + BossLightBudget04E.MaxFightLights, Is.EqualTo(UnderheartLightBudget.Budget(low, false)),
                    "region lights during a fight plus the fight's key light, " + (low ? "Low" : "other presets"));
        }

        [Test]
        public void TheArenaIsNearOnlyWhileTheFightIsActiveAndTheViewIsThere()
        {
            float radius = 18f;
            Vector3 inside = Arena + new Vector3(10f, 3f, 4f);
            Assert.That(UnderheartLightBudget.ArenaNear(true, inside, Arena, radius), Is.True);
            Assert.That(UnderheartLightBudget.ArenaNear(false, inside, Arena, radius), Is.False, "ended, failed or reset: the full budget returns");
            Vector3 edge = Arena + new Vector3(radius + UnderheartLightBudget.ArenaMargin - 0.5f, 0f, 0f);
            Assert.That(UnderheartLightBudget.ArenaNear(true, edge, Arena, radius), Is.True, "within the margin outside the ring");
            Vector3 away = Arena + new Vector3(radius + UnderheartLightBudget.ArenaMargin + 1f, 0f, 0f);
            Assert.That(UnderheartLightBudget.ArenaNear(true, away, Arena, radius), Is.False, "the player left the arena");
            Vector3 above = Arena + new Vector3(2f, WorldBounds.VerticalBand + 1f, 0f);
            Assert.That(UnderheartLightBudget.ArenaNear(true, above, Arena, radius), Is.False, "another layer over the arena");
            Vector3 below = Arena + new Vector3(2f, -(WorldBounds.VerticalBand + 1f), 0f);
            Assert.That(UnderheartLightBudget.ArenaNear(true, below, Arena, radius), Is.False);
        }

        [Test]
        public void BadNumbersNeverCountAsNear()
        {
            Assert.That(UnderheartLightBudget.ArenaNear(true, new Vector3(float.NaN, 0f, 0f), Arena, 18f), Is.False);
            Assert.That(UnderheartLightBudget.ArenaNear(true, Arena, new Vector3(0f, float.PositiveInfinity, 0f), 18f), Is.False);
            Assert.That(UnderheartLightBudget.ArenaNear(true, Arena, Arena, float.NaN), Is.False);
            Assert.That(UnderheartLightBudget.ArenaNear(true, Arena, Arena, float.PositiveInfinity), Is.False);
            Assert.That(UnderheartLightBudget.ArenaNear(true, Arena + new Vector3(5f, 0f, 0f), Arena, -4f), Is.True, "a negative radius counts as zero plus the margin");
        }
    }
}
