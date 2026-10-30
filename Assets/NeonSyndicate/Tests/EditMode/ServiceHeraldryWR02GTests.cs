#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ServiceHeraldryWR02GTests
    {
        [TestCase("lofts", 30f, 7.7f, -.39f, 4.9f)]
        [TestCase("forge", 30f, 7.7f, -.39f, 4.9f)]
        [TestCase("archive", 48f, 14.35f, -.035f, 5.35f)]
        public void FabricUsesKnownExistingSupportsAndStaysClearOfTheDoor(string id, float depth, float side, float front, float top)
        {
            MethodInfo method = typeof(VenueView04C).GetMethod("HeraldryFrameWR02G", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] args = { id, 30f, depth, 0f, 0f, 0f };
            Assert.That((bool)method.Invoke(null, args), Is.True);
            Assert.That((float)args[3], Is.EqualTo(side).Within(.001f));
            Assert.That((float)args[4], Is.EqualTo(front).Within(.001f));
            Assert.That((float)args[5], Is.EqualTo(top).Within(.001f));
            Assert.That(side - .65f, Is.GreaterThan(1.4f), "Fabric must not cover the existing doorway.");
            args[1] = 29f; Assert.That((bool)method.Invoke(null, args), Is.False, "An unknown support frame must refuse the candidate.");
        }

        [Test]
        public void EachPurposeHasItsOwnMarkAndUnknownServicesHaveNone()
        {
            MethodInfo glyph = typeof(VenueView04C).GetMethod("HeraldryGlyphWR02G", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(glyph, Is.Not.Null);
            Assert.That((bool)glyph.Invoke(null, new object[] { "lofts", .30f, 0f }), Is.True);
            Assert.That((bool)glyph.Invoke(null, new object[] { "forge", .4f, .1f }), Is.True);
            Assert.That((bool)glyph.Invoke(null, new object[] { "archive", 0f, .2f }), Is.True);
            Assert.That((bool)glyph.Invoke(null, new object[] { "unknown", 0f, 0f }), Is.False);
        }
    }
}
#endif
