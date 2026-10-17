using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// Command-line builds, used by tools/build.ps1 (Windows) and tools/build.sh (macOS). For example:
    /// <code>Unity -batchmode -quit -projectPath . -executeMethod NeonSyndicate.EditorTools.BuildScripts.BuildWindows64 -buildTarget StandaloneWindows64 -logFile Logs/build.log</code>
    /// Each build first runs the same project repair the editor runs when it opens the project (input handling, physics
    /// layers, fog and shader variants, and the generated game scene first in the build list), then builds to
    /// Builds/Windows or Builds/Mac and logs a summary. In batch mode a failed build exits Unity with code 1, so scripts
    /// can tell. Add -nsDevelopment to the command line for a development build (profiler and debug console).
    /// Just before the player build the 04E build manifest (Resources/BuildManifest04E.json: revision, branch, dirty
    /// state, build time) is written for the in-game STATUS page, and it is removed again when the build ends.
    /// </summary>
    public static class BuildScripts
    {
        public const string WindowsPath = "Builds/Windows/NeonSyndicate.exe";
        public const string MacPath = "Builds/Mac/NeonSyndicate.app";
        private const string Tag = "[Neon Syndicate] ";

        /// <summary>Windows 64-bit player in Builds/Windows (NeonSyndicate.exe plus its data folder).</summary>
        public static void BuildWindows64()
        {
            Run(BuildTarget.StandaloneWindows64, WindowsPath, "Windows");
        }

        /// <summary>macOS player in Builds/Mac (NeonSyndicate.app).</summary>
        public static void BuildMacOS()
        {
            Run(BuildTarget.StandaloneOSX, MacPath, "Mac");
        }

        private static void Run(BuildTarget target, string path, string label)
        {
            bool ok;
            try
            {
                ok = Build(target, path, label);
            }
            catch (Exception e)
            {
                Debug.LogError(Tag + "BUILD FAILED: the " + label + " build stopped with an error: " + e);
                ok = false;
            }
            if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static bool Build(BuildTarget target, string path, string label)
        {
            Debug.Log(Tag + "Command-line build for " + label + " started (Unity " + Application.unityVersion + ").");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                Debug.LogError(Tag + "BUILD FAILED: " + label + " Build Support is not installed for Unity " + Application.unityVersion
                    + ". In Unity Hub open Installs, click the gear next to this version, choose Add modules and tick " + label + " Build Support (Mono).");
                return false;
            }

            // The same repair the editor runs on open (it never runs by itself in a one-shot batch session).
            NeonSyndicateSetup.FixInputHandling(false);
            NeonSyndicateSetup.RunSetup(false);
            NeonSyndicateSetup.EnsureShaderKeep(true);
            string scene = NeonSyndicateSetup.ScenePath;
            if (!File.Exists(scene))
            {
                Debug.LogError(Tag + "BUILD FAILED: the game scene " + scene + " could not be created.");
                return false;
            }
            EditorBuildSettingsScene[] list = EditorBuildSettings.scenes;
            if (list.Length == 0 || list[0].path != scene || !list[0].enabled)
            {
                Debug.LogError(Tag + "BUILD FAILED: " + scene + " is not the first enabled scene in the build list.");
                return false;
            }

            string folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            BuildOptions options = BuildOptions.None;
            if (HasArgument("-nsDevelopment")) options |= BuildOptions.Development;
            BuildPlayerOptions o = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = path,
                target = target,
                targetGroup = BuildTargetGroup.Standalone,
                options = options
            };
            // 04E: the player's STATUS page reads its source revision from this manifest (written now, removed after).
            try { BuildInfo04EEditor.WriteManifest(target); }
            catch (Exception e) { Debug.LogWarning(Tag + "Build manifest not written; the player will say revision unavailable: " + e.Message); }
            BuildReport report;
            try { report = BuildPipeline.BuildPlayer(o); }
            finally { BuildInfo04EEditor.RemoveManifest(); }
            BuildSummary s = report.summary;
            string summary = label + " build " + s.result + " in " + s.totalTime.TotalSeconds.ToString("0") + " s: "
                + s.totalErrors + " error(s), " + s.totalWarnings + " warning(s), " + (s.totalSize / (1024 * 1024)) + " MB"
                + ((options & BuildOptions.Development) != 0 ? ", development build" : "") + ", output " + Path.GetFullPath(path);
            if (s.result == BuildResult.Succeeded)
            {
                Debug.Log(Tag + "BUILD SUCCEEDED: " + summary);
                return true;
            }
            List<string> errors = new List<string>();
            foreach (BuildStep step in report.steps)
            {
                foreach (BuildStepMessage m in step.messages)
                {
                    if (m.type != LogType.Error && m.type != LogType.Exception) continue;
                    errors.Add(m.content);
                    if (errors.Count >= 20) break;
                }
                if (errors.Count >= 20) break;
            }
            Debug.LogError(Tag + "BUILD FAILED: " + summary + (errors.Count > 0 ? "\n  " + string.Join("\n  ", errors.ToArray()) : ""));
            return false;
        }

        private static bool HasArgument(string name)
        {
            foreach (string a in Environment.GetCommandLineArgs())
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
