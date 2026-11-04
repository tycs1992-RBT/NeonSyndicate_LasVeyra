#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;

namespace NeonSyndicate.Tests
{
    public sealed class FantasyPreparationFailureTests
    {
        private static Type EditorType(string name)
        {
            Type value = Type.GetType("NeonSyndicate.Editor." + name + ", NeonSyndicate.Editor");
            Assert.That(value, Is.Not.Null, "Editor preparation assembly is required for this test.");
            return value;
        }

        [TestCase(false, "FR01")]
        [TestCase(true, "FR02A")]
        public void MissingFirstCatalogRetainsTheSpecificPreparationException(bool living, string layer)
        {
            string key = "NeonSyndicate.HeartPreparationFailure." + layer;
            string prior = SessionState.GetString(key, "");
            string failure = "FBX import unavailable: Assets/example/" + layer + ".fbx";
            try
            {
                SessionState.SetString(key, failure);
                MethodInfo refine = EditorType("FantasyAcceptanceV1").GetMethod("RefinePreparationFailures", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(refine, Is.Not.Null);
                string generic = (living ? "Living Streets" : "Gothic Heart") + " catalog is unavailable: preparation created no catalog";
                List<string> blockers = new List<string> { generic, generic };
                refine.Invoke(null, new object[] { blockers, true, true });
                Assert.That(blockers.Count, Is.EqualTo(2));
                Assert.That(blockers[0], Does.Contain(failure));
                Assert.That(blockers[1], Is.EqualTo(generic), "Later independent catalog blockers must not be relabelled.");
                blockers = new List<string> { generic };
                refine.Invoke(null, new object[] { blockers, false, false });
                Assert.That(blockers[0], Is.EqualTo(generic), "Existing catalogs cannot rewrite unrelated blockers.");
            }
            finally { SessionState.SetString(key, prior); }
        }

        [Test]
        public void RetryClearsOnlyItsOwnStalePreparationFailure()
        {
            const string fr01 = "NeonSyndicate.HeartPreparationFailure.FR01", fr02a = "NeonSyndicate.HeartPreparationFailure.FR02A";
            string first = SessionState.GetString(fr01, ""), second = SessionState.GetString(fr02a, "");
            try
            {
                SessionState.SetString(fr01, "old FR01 error"); SessionState.SetString(fr02a, "other layer error");
                MethodInfo clear = EditorType("HeartSetupFR01").GetMethod("ClearPreparationFailureFR01", BindingFlags.Static | BindingFlags.Public);
                Assert.That(clear, Is.Not.Null); clear.Invoke(null, new object[] { false });
                Assert.That(SessionState.GetString(fr01, ""), Is.Empty);
                Assert.That(SessionState.GetString(fr02a, ""), Is.EqualTo("other layer error"));
                clear.Invoke(null, new object[] { true });
                Assert.That(SessionState.GetString(fr02a, ""), Is.Empty);
            }
            finally { SessionState.SetString(fr01, first); SessionState.SetString(fr02a, second); }
        }
    }
}
#endif
