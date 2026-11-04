using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// The Shardwake Vale pilot build. It passes exactly one scene, Assets/Scenes/ShardwakeVale.unity, to the build
    /// pipeline, so the player starts in the Vale and cannot open the original city scene. It never reads or changes
    /// the project's build list (EditorBuildSettings) and never runs the city setup, so the original game's
    /// BuildScripts.BuildWindows64 and its scene list stay exactly as they were. From the command line:
    /// <code>Unity -batchmode -quit -projectPath . -buildTarget StandaloneWindows64 -executeMethod NeonSyndicate.EditorTools.ShardwakeValeBuild.BuildWindowsPilot -logFile Logs/vale-build.log</code>
    /// tools/build_vale.ps1 runs that line. In batch mode a refused or failed build exits Unity with code 1.
    /// </summary>
    public static class ShardwakeValeBuild
    {
        private const string Tag = "[Shardwake Vale] ";

        /// <summary>Batch entry: the Windows pilot player in Builds/ShardwakeVale/ShardwakeVale.exe.</summary>
        public static void BuildWindowsPilot()
        {
            bool ok;
            try { ok = BuildTo(ValeLaunch.WindowsPlayer); }
            catch (Exception e) { Debug.LogError(Tag + "BUILD FAILED: " + e); ok = false; }
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        /// <summary>Builds the pilot to the given .exe path. Returns false, with a Console line, when refused or failed.</summary>
        public static bool BuildTo(string exePath)
        {
            string[] scenes = ValeLaunch.BuildScenes();
            if (!ValeLaunch.Validate(scenes, out string reason))
            {
                Debug.LogError(Tag + "BUILD REFUSED: " + reason);
                return false;
            }
            if (!File.Exists(ValeLaunch.PilotScene))
            {
                Debug.LogError(Tag + "BUILD REFUSED: " + ValeLaunch.PilotScene + " is missing. Fetch the pilot/shardwake-vale branch.");
                return false;
            }
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                Debug.LogError(Tag + "BUILD FAILED: Windows Build Support is not installed for Unity " + Application.unityVersion
                    + ". In Unity Hub open Installs, click the gear next to this version, choose Add modules and tick Windows Build Support.");
                return false;
            }
            string folder = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            Debug.Log(Tag + "Pilot build started. Scenes: " + string.Join(", ", scenes) + ". The original game's build list is not used.");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            });
            BuildSummary s = report.summary;
            string summary = "pilot build " + s.result + ": " + s.totalErrors + " error(s), " + s.totalWarnings + " warning(s), output " + Path.GetFullPath(exePath);
            if (s.result == BuildResult.Succeeded)
            {
                Debug.Log(Tag + "BUILD SUCCEEDED: " + summary + ". Startup scene: " + ValeLaunch.PilotScene + ".");
                return true;
            }
            Debug.LogError(Tag + "BUILD FAILED: " + summary);
            return false;
        }
    }
}
