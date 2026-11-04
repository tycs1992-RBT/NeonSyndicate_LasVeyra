#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Prepares the live characters before any player build, including builds started from Unity's own Build window. The
    /// project's build menus and BuildScripts call LiveCastSetup.PrepareForBuild themselves too; a second call within the
    /// same build is skipped, and an unchanged project costs only the fingerprint check.
    /// </summary>
    public sealed class LiveCastBuildStep : BuildPlayerProcessor
    {
        /// <summary>Early, so every later build step sees the generated registry.</summary>
        public override int callbackOrder { get { return -100; } }

        /// <summary>Runs the live character generator (never fails the build; a refusal keeps the procedural characters).</summary>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            LiveCastSetup.PrepareForBuild();
        }
    }

    /// <summary>
    /// Schedules the live character generator when a drop-in folder under Art/LiveCast or the H9 art changes while the
    /// editor is open, so dropping a character in is enough. The generator's own Prepared output never schedules a run.
    /// </summary>
    public sealed class LiveCastWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (LiveCastSetup.Running || Application.isBatchMode) return;
            if (Watched(importedAssets) || Watched(deletedAssets) || Watched(movedAssets) || Watched(movedFromAssetPaths)) LiveCastSetup.Schedule(true);
        }

        private static bool Watched(string[] paths)
        {
            if (paths == null) return false;
            foreach (string path in paths) if (LiveCastFolderRules.WatchedSource(path)) return true;
            return false;
        }
    }
}
#endif
