#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace NeonSyndicate.Editor
{
    internal static class ShardwakeValeMenu
    {
        private const string PilotScene="Assets/Scenes/ShardwakeVale.unity";

        [MenuItem("Shardwake Vale/Open pilot scene")]
        private static void Open()
        {
            if(!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(PilotScene,OpenSceneMode.Single);
        }

        [MenuItem("Shardwake Vale/Build Windows pilot")]
        private static void Build()
        {
            string output=EditorUtility.SaveFilePanel("Build Shardwake Vale","","ShardwakeVale","exe");
            if(string.IsNullOrEmpty(output))return;
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{PilotScene},locationPathName=output,
                target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            UnityEngine.Debug.Log("Shardwake Vale build "+report.summary.result+" ("+report.summary.totalErrors+" errors).");
        }
    }
}
#endif
