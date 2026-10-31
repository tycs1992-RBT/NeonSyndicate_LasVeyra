using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class GrottoCompositionTests
    {
        [Test]
        public void ExistingGrottoPathsShareOneBudgetSeparateFromTheWoodland()
        {
            Assert.That(HeartPlanFR01.CompositionZoneWR02B("grotto-through-route"), Is.EqualTo("Grotto approach"));
            Assert.That(HeartPlanFR01.CompositionZoneWR02B("grotto-return"), Is.EqualTo("Grotto approach"));
            Assert.That(HeartPlanFR01.CompositionZoneWR02B("shardwood-threshold"), Is.EqualTo("Woodland causeway"));
            Assert.That(HeartPlanFR01.CompositionZoneLimitWR02B("Grotto approach"), Is.EqualTo(9));
            Assert.That(HeartPlanFR01.CompositionZoneLimitWR02B("Woodland causeway"), Is.EqualTo(12));
            Assert.That(HeartPlanFR01.CompositionSoftLimitWR02B, Is.EqualTo(48));
        }
    }
}
