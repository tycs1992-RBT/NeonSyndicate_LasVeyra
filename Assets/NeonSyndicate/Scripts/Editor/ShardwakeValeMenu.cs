#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

namespace NeonSyndicate.Editor
{
    /// <summary>
    /// The Shardwake Vale menu: open the pilot scene, open and play it, and build the pilot for Windows. The original
    /// game keeps its own scene, build list and Neon Syndicate menu; nothing here changes them.
    /// </summary>
    internal static class ShardwakeValeMenu
    {
        [MenuItem("Shardwake Vale/Open Shardwake Vale", false, 1)]
        private static void Open()
        {
            OpenPilot();
        }

        [MenuItem("Shardwake Vale/Play Shardwake Vale", false, 2)]
        private static void Play()
        {
            if (OpenPilot()) EditorApplication.isPlaying = true;
        }

        [MenuItem("Shardwake Vale/Build Shardwake Vale for Windows (pilot only)", false, 20)]
        private static void Build()
        {
            string output = EditorUtility.SaveFilePanel("Build Shardwake Vale", "Builds/ShardwakeVale", "ShardwakeVale", "exe");
            if (string.IsNullOrEmpty(output)) return;
            global::NeonSyndicate.EditorTools.ShardwakeValeBuild.BuildTo(output);
        }

        private static bool OpenPilot()
        {
            if (EditorApplication.isPlaying) return false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EditorSceneManager.OpenScene(ValeLaunch.PilotScene, OpenSceneMode.Single);
            return true;
        }
    }
}
#endif
