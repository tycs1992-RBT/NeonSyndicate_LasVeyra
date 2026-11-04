using System;

namespace NeonSyndicate
{
    /// <summary>
    /// Which scenes the Shardwake Vale pilot build contains. Pure rules shared by the Editor menu, the batch build and the
    /// EditMode tests: the pilot build holds exactly one scene, the Vale, so its player can never start the original
    /// city scene. The original game's build list and its own build command are not touched by the pilot.
    /// </summary>
    public static class ValeLaunch
    {
        /// <summary>The pilot scene, the only scene of a pilot build and its startup scene.</summary>
        public const string PilotScene = "Assets/Scenes/ShardwakeVale.unity";
        /// <summary>The original city scene, which a pilot build must never contain.</summary>
        public const string LegacyScene = "Assets/Scenes/NeonSyndicate.unity";
        /// <summary>Where the batch pilot build writes the Windows player.</summary>
        public const string WindowsPlayer = "Builds/ShardwakeVale/ShardwakeVale.exe";

        /// <summary>A fresh copy of the pilot build's scene list: only the Vale, so it is also the startup scene.</summary>
        public static string[] BuildScenes()
        {
            return new[] { PilotScene };
        }

        /// <summary>
        /// True when a scene list starts the Vale and nothing else. Any other first scene, any extra scene, an empty entry
        /// or the original city scene is refused with a plain reason.
        /// </summary>
        public static bool Validate(string[] scenes, out string reason)
        {
            reason = "";
            if (scenes == null || scenes.Length == 0) reason = "The pilot build has no scene.";
            else if (Array.Exists(scenes, s => string.Equals(Normalize(s), LegacyScene, StringComparison.OrdinalIgnoreCase)))
                reason = "The pilot build must not contain the original city scene " + LegacyScene + ".";
            else if (!string.Equals(Normalize(scenes[0]), PilotScene, StringComparison.OrdinalIgnoreCase))
                reason = "The pilot build must start with " + PilotScene + ".";
            else if (scenes.Length != 1) reason = "The pilot build contains only " + PilotScene + ".";
            return reason.Length == 0;
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : path.Replace('\\', '/').Trim();
        }
    }
}
