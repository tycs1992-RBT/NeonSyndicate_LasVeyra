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
            Assert.That((HeartPlanFR01.CompositionSoftLimitWR02B / 5) * 5, Is.LessThanOrEqualTo(HeartPlanFR01.CompositionSoftLimitWR02B));
        }
    }
}
