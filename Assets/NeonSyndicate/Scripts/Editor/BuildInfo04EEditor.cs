using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// Source revision support for the 04E STATUS page.
    /// At editor load it runs one bounded background `git status --porcelain -uno` (2 second timeout, killed after
    /// that), caches the answer for the editor session (SessionState survives script reloads) and hands it to the
    /// runtime (BuildInfo04E.SetEditorState), again whenever play mode starts. It never runs Git per frame or per play.
    /// For builds it writes Resources/BuildManifest04E.json just before the player build (revision, branch, dirty
    /// state, build time; no usernames or paths) and removes it afterwards; .gitignore lists the file as well.
    /// </summary>
    [InitializeOnLoad]
    public static class BuildInfo04EEditor
    {
        public const string ManifestPath = "Assets/NeonSyndicate/Resources/BuildManifest04E.json";
        private const string StateKey = "NeonSyndicate.04E.GitState";
        private const string NoteKey = "NeonSyndicate.04E.GitNote";
        private const string Tag = "[Neon Syndicate] ";
        private const int StatusTimeoutMs = 2000;

        private static volatile bool _pending;
        private static volatile bool _resultReady;
        private static double _manifestWrittenAt = -1000.0;
        private static string _resultState = "";
        private static string _resultNote = "";

        static BuildInfo04EEditor()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
            // A manifest left behind by a build that stopped early is removed once the editor is idle.
            EditorApplication.delayCall += RemoveStaleManifest;
            if (SessionState.GetBool(StateKey + ".Done", false))
            {
                Push();
                return;
            }
            StartBackgroundStatus();
        }

        private static string ProjectRoot { get { return Path.GetDirectoryName(Application.dataPath); } }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            // Entering play mode resets the runtime statics (Game.ResetStatics); give it the cached state again.
            if (change == PlayModeStateChange.EnteredPlayMode) Push();
        }

        private static void Push()
        {
            try { BuildInfo04E.SetEditorState(SessionState.GetString(StateKey, ""), SessionState.GetString(NoteKey, "")); }
            catch (Exception e) { Debug.LogWarning(Tag + "Could not pass the revision state to the game: " + e.Message); }
        }

        private static void StartBackgroundStatus()
        {
            if (_pending) return;
            _pending = true;
            _resultReady = false;
            string root = ProjectRoot;
            // The worker only sets fields; the main thread picks the answer up in EditorApplication.update.
            EditorApplication.update += Poll;
            Thread t = new Thread(() =>
            {
                string state, note;
                RunStatus(root, StatusTimeoutMs, out state, out note);
                _resultState = state;
                _resultNote = note;
                _resultReady = true;
            });
            t.IsBackground = true;
            t.Name = "Neon Syndicate git status";
            t.Start();
        }

        private static void Poll()
        {
            if (!_resultReady) return;
            EditorApplication.update -= Poll;
            Finish();
        }

        private static void Finish()
        {
            _pending = false;
            SessionState.SetString(StateKey, _resultState ?? "");
            SessionState.SetString(NoteKey, _resultNote ?? "");
            SessionState.SetBool(StateKey + ".Done", true);
            Push();
        }

        /// <summary>
        /// Runs `git status --porcelain -uno` in the project folder for at most timeoutMs. state is "clean", "local
        /// changes" or "" when it could not tell (note says why: Git missing, not a repository, timed out). Never throws.
        /// </summary>
        public static void RunStatus(string root, int timeoutMs, out string state, out string note)
        {
            state = "";
            note = "";
            if (string.IsNullOrEmpty(root) || (!Directory.Exists(Path.Combine(root, ".git")) && !File.Exists(Path.Combine(root, ".git"))))
            {
                note = "no Git metadata in this folder";
                return;
            }
            Process p = null;
            try
            {
                ProcessStartInfo info = new ProcessStartInfo("git", "status --porcelain -uno")
                {
                    WorkingDirectory = root,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                StringBuilder output = new StringBuilder();
                p = new Process { StartInfo = info };
                p.OutputDataReceived += (s, e) => { if (e.Data != null) lock (output) output.Append(e.Data).Append('\n'); };
                p.ErrorDataReceived += (s, e) => { };
                if (!p.Start())
                {
                    note = "Git could not start";
                    return;
                }
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch (Exception) { }
                    note = "the dirty check timed out after " + (timeoutMs / 1000f).ToString("0.#", CultureInfo.InvariantCulture) + " s";
                    return;
                }
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    note = "Git reported an error (exit " + p.ExitCode + ")";
                    return;
                }
                string text;
                lock (output) text = output.ToString();
                state = GitRefReader04E.PorcelainIsDirty(text) ? "local changes" : "clean";
            }
            catch (Exception e)
            {
                // Typically Git is not installed or not on the PATH. The revision still shows from the .git files.
                note = "Git is not available (" + e.GetType().Name + ")";
            }
            finally
            {
                if (p != null) p.Dispose();
            }
        }

        // ================================================================== build manifest

        /// <summary>Writes Resources/BuildManifest04E.json for a player build (bounded dirty check, up to 5 seconds).</summary>
        public static void WriteManifest(BuildTarget target)
        {
            string root = ProjectRoot;
            string sha, branch, reason;
            bool hasRevision = GitRefReader04E.TryRead(root, out sha, out branch, out reason);
            string state, note;
            RunStatus(root, 5000, out state, out note);
            BuildManifest04E m = new BuildManifest04E
            {
                Label = ModuleStatus.Label,
                Revision = hasRevision ? sha : "",
                Branch = hasRevision ? branch : "",
                Dirty = state == "local changes",
                DirtyKnown = state.Length > 0,
                BuiltUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC",
                Unity = Application.unityVersion,
                Target = target.ToString()
            };
            string folder = Path.GetDirectoryName(ManifestPath);
            if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
            File.WriteAllText(ManifestPath, JsonUtility.ToJson(m, true));
            AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceSynchronousImport);
            _manifestWrittenAt = EditorApplication.timeSinceStartup;
            Debug.Log(Tag + "Build manifest written: " + (hasRevision ? sha.Substring(0, Math.Min(7, sha.Length)) + " (" + branch + ")" : "revision unavailable, " + reason)
                + (state.Length > 0 ? ", " + state : ", dirty state unknown" + (note.Length > 0 ? " (" + note + ")" : "")) + ".");
        }

        /// <summary>True when the build script wrote the manifest moments ago (the build hook then leaves it alone).</summary>
        public static bool ManifestFresh { get { return EditorApplication.timeSinceStartup - _manifestWrittenAt < 120.0 && File.Exists(ManifestPath); } }

        private static void RemoveStaleManifest()
        {
            try
            {
                if (!BuildPipeline.isBuildingPlayer && !ManifestFresh && File.Exists(ManifestPath)) RemoveManifest();
            }
            catch (Exception e) { Debug.LogWarning(Tag + "Could not check for a leftover build manifest: " + e.Message); }
        }

        /// <summary>Deletes the generated manifest (and its meta) after a build so it never lingers in the project.</summary>
        public static void RemoveManifest()
        {
            _manifestWrittenAt = -1000.0;
            try
            {
                if (File.Exists(ManifestPath) || File.Exists(ManifestPath + ".meta"))
                {
                    if (!AssetDatabase.DeleteAsset(ManifestPath))
                    {
                        if (File.Exists(ManifestPath)) File.Delete(ManifestPath);
                        if (File.Exists(ManifestPath + ".meta")) File.Delete(ManifestPath + ".meta");
                    }
                }
            }
            catch (Exception e) { Debug.LogWarning(Tag + "Could not remove the build manifest (it is git-ignored): " + e.Message); }
        }
    }

    /// <summary>Writes the manifest for builds started from Unity's own Build window too, and removes it afterwards.</summary>
    public sealed class BuildManifest04EBuildHooks : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (BuildInfo04EEditor.ManifestFresh) return;
            try { BuildInfo04EEditor.WriteManifest(report.summary.platform); }
            catch (Exception e) { Debug.LogWarning("[Neon Syndicate] Build manifest not written (the player will say revision unavailable): " + e.Message); }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            BuildInfo04EEditor.RemoveManifest();
        }
    }
}
