using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>The pilot build starts in the Vale and can never contain the original city scene.</summary>
    public sealed class ValeLaunchTests
    {
        [Test] public void PilotBuildHoldsOnlyTheValeAsItsStartupScene()
        {
            string[] scenes = ValeLaunch.BuildScenes();
            Assert.AreEqual(1, scenes.Length);
            Assert.AreEqual("Assets/Scenes/ShardwakeVale.unity", scenes[0]);
            Assert.True(ValeLaunch.Validate(scenes, out string reason), reason);
            scenes[0] = "changed";
            Assert.AreEqual(ValeLaunch.PilotScene, ValeLaunch.BuildScenes()[0], "Each call returns a fresh list");
        }

        [Test] public void TheOriginalCitySceneIsRefusedAnywhereInThePilotList()
        {
            Assert.False(ValeLaunch.Validate(new[] { ValeLaunch.PilotScene, ValeLaunch.LegacyScene }, out string reason));
            StringAssert.Contains("original city scene", reason);
            Assert.False(ValeLaunch.Validate(new[] { ValeLaunch.LegacyScene }, out reason));
            Assert.False(ValeLaunch.Validate(new[] { "Assets\\Scenes\\NeonSyndicate.unity", ValeLaunch.PilotScene }, out reason));
        }

        [Test] public void AnEmptyOrDifferentStartupSceneIsRefused()
        {
            Assert.False(ValeLaunch.Validate(null, out string reason));
            Assert.False(ValeLaunch.Validate(new string[0], out reason));
            Assert.False(ValeLaunch.Validate(new[] { "Assets/Scenes/Other.unity", ValeLaunch.PilotScene }, out reason));
            Assert.False(ValeLaunch.Validate(new[] { ValeLaunch.PilotScene, "Assets/Scenes/Other.unity" }, out reason));
            Assert.True(ValeLaunch.Validate(new[] { "Assets\\Scenes\\ShardwakeVale.unity" }, out reason), reason);
        }
    }
}
