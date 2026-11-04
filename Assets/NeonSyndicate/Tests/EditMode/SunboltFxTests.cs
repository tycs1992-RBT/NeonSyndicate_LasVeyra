using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>The Vale's Sunbolt look: gather, flight and trail, burst, fizzle and the collar flare.</summary>
    public sealed class SunboltFxTests
    {
        [Test] public void TheBoltGathersInTheHandThenLeavesAtTheOriginalSpeed()
        {
            Assert.AreEqual(0f, SunboltFx.Gather(0f));
            Assert.True(SunboltFx.Gathering(0f) && SunboltFx.Gathering(SunboltFx.GatherSeconds * .5f));
            Assert.False(SunboltFx.Gathering(SunboltFx.GatherSeconds));
            Assert.AreEqual(1f, SunboltFx.Gather(SunboltFx.GatherSeconds), 1e-5f);
            Assert.Less(SunboltFx.GatherRing(SunboltFx.GatherSeconds), SunboltFx.GatherRing(0f), "The ring closes onto the hand");
            Assert.AreEqual(23f / 23f, SunboltFx.FlightSeconds(23f), 1e-5f);
            Assert.AreEqual(.12f, SunboltFx.FlightSeconds(.5f), 1e-5f, "A point-blank cast is still seen");
            Assert.AreEqual(0f, SunboltFx.Progress(0f, .5f));
            Assert.AreEqual(1f, SunboltFx.Progress(.6f, .5f));
        }

        [Test] public void TheTrailStaysBehindTheBoltOnItsFlightLine()
        {
            Vector3 origin = new Vector3(1f, 2f, 3f), destination = new Vector3(6f, 3.5f, 14f);
            Vector3 line = destination - origin;
            Vector3[] points = new Vector3[SunboltFx.TrailPoints];
            foreach (float progress in new[] { 0f, .1f, .5f, .9f, 1f })
            {
                SunboltFx.Trail(origin, destination, progress, 1.3f, points);
                Assert.Less(Vector3.Distance(points[points.Length - 1], Vector3.Lerp(origin, destination, progress)), 1e-4f, "The last point is the bolt");
                for (int i = 0; i < points.Length; i++)
                {
                    float along = Vector3.Dot(points[i] - origin, line.normalized) / line.magnitude;
                    Assert.GreaterOrEqual(along, -1e-4f, "Never behind the hand");
                    Assert.LessOrEqual(along, progress + 1e-4f, "Never ahead of the bolt");
                    Vector3 onLine = origin + line * along;
                    Assert.LessOrEqual(Vector3.Distance(points[i], onLine), SunboltFx.TwistRadius + 1e-4f);
                }
            }
            Assert.AreEqual(0f, SunboltFx.TrailWidthAt(0f));
            Assert.AreEqual(1f, SunboltFx.TrailWidthAt(1f), 1e-5f);
            Assert.Less(SunboltFx.TrailAlphaAt(.3f), SunboltFx.TrailAlphaAt(.8f));
        }

        [Test] public void OnlyAGroundedCollarFlaresAndItSettlesBack()
        {
            Assert.AreEqual(0f, SunboltFx.Flare(-1f), "Nothing before a hit");
            Assert.AreEqual(1f, SunboltFx.Flare(0f), 1e-5f);
            Assert.AreEqual(0f, SunboltFx.Flare(SunboltFx.FlareSeconds));
            Assert.AreEqual(.64f, SunboltFx.Lift(.64f, 0f), 1e-5f, "No flare keeps the spent look");
            Assert.AreEqual(SunboltFx.FlareLift, SunboltFx.Lift(.64f, 1f), 1e-5f);
            Color resting = new Color(.45f, .46f, .55f);
            Assert.AreEqual(resting, SunboltFx.Tint(resting, 0f));
            Assert.Greater(SunboltFx.BurstRadius(SunboltFx.BurstSeconds * .9f), SunboltFx.FizzleRadius(SunboltFx.FizzleSeconds), "A miss never looks like a burst");
            Assert.AreEqual(0f, SunboltFx.BurstAlpha(SunboltFx.BurstSeconds));
            Assert.AreEqual(0f, SunboltFx.FizzleAlpha(SunboltFx.FizzleSeconds));
            Assert.Less(SunboltFx.FizzleColor.r, SunboltFx.FlareTint.r, "The fizzle is cooler than the flare");
        }
    }
}
