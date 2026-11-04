#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class FantasyRoutePriorityWR02GTests
    {
        [Test]
        public void AutomaticPreviewRouteNeverReplacesAnExistingNavigationOwner()
        {
            MethodInfo rule = typeof(VisibleRealityV0).GetMethod("CanSuggestFantasyRouteWR02G", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(rule, Is.Not.Null);
            for (int mask = 0; mask < 32; mask++)
            {
                object[] flags = new object[5];
                for (int bit = 0; bit < flags.Length; bit++) flags[bit] = (mask & (1 << bit)) != 0;
                Assert.That((bool)rule.Invoke(null, flags), Is.EqualTo(mask == 0), "Navigation ownership mask " + mask);
            }
        }
    }
}
#endif
