using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.EditorTools
{
    /// <summary>
    /// The Neon Syndicate > QA menu: golden screenshots, comparing two sets of them, and the perf flythrough. The
    /// captures run in Play mode; picking one outside Play mode offers to start Play mode, and the capture then begins
    /// by itself once the game has loaded (from the title screen it starts a Free Roam game first).
    /// </summary>
    [InitializeOnLoad]
    public static class QaMenu
    {
        private const string PendingKey = "NeonSyndicate.QA.PendingCapture";
        private const string Golden = "golden";
        private const string Perf = "perf";
        private const string Rebirth = "rebirth04a";

        static QaMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Neon Syndicate/QA/Capture Golden Shots", false, 80)]
        public static void CaptureGoldenShots()
        {
            StartOrQueue(Golden, "Golden shots",
                "Takes 28 screenshots (14 fixed views at noon and at 22:00, the last two in the Frostline and inside the Veil) with the HUD hidden. It takes about three minutes; keep the Game view visible and don't touch the controls. Esc cancels.");
        }

        [MenuItem("Neon Syndicate/QA/Compare Golden Shots...", false, 81)]
        public static void CompareGoldenShots()
        {
            GoldenCompare.CompareWithDialogs();
        }

        [MenuItem("Neon Syndicate/QA/Run Perf Flythrough", false, 82)]
        public static void RunPerfFlythrough()
        {
            StartOrQueue(Perf, "Perf flythrough",
                "Flies the camera along a fixed route through the city for about 80 seconds and logs every frame's time. Close other heavy programs first; Esc cancels.");
        }

        [MenuItem("Neon Syndicate/QA/Capture Visual Rebirth 04A Looks", false, 84)]
        public static void CaptureVisualRebirth()
        {
            StartOrQueue(Rebirth, "Visual Rebirth 04A look tests",
                "Captures the city at street and flight height, actual placed stronghold gates, and the Hearthspire garden/upper terrace. Tests noon, golden hour, dusk, night and rain; saves real Game view PNGs, a comparison manifest and measured frame/memory samples. Allow about five minutes and keep the Game view visible. Esc cancels; camera, weather, clock and player state are restored.");
        }

        [MenuItem("Neon Syndicate/QA/Cancel Running Capture", false, 83)]
        public static void CancelCapture()
        {
            QaTools.Cancel();
        }

        [MenuItem("Neon Syndicate/QA/Cancel Running Capture", true)]
        private static bool CanCancelCapture()
        {
            return EditorApplication.isPlaying && QaTools.Busy;
        }

        [MenuItem("Neon Syndicate/QA/Open QA Output Folder", false, 95)]
        public static void OpenOutputFolder()
        {
            Directory.CreateDirectory(QaTools.GoldenRoot);
            Directory.CreateDirectory(QaTools.PerfRoot);
            EditorUtility.RevealInFinder(QaTools.GoldenRoot);
        }

        // ------------------------------------------------------------------ starting

        private static void StartOrQueue(string kind, string title, string what)
        {
            if (EditorApplication.isPlaying)
            {
                StartNow(kind);
                return;
            }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorUtility.DisplayDialog("Neon Syndicate", "Unity is still compiling or importing. Try again in a moment.", "OK");
                return;
            }
            if (!EditorUtility.DisplayDialog("Neon Syndicate: " + title, what + "\n\nThis runs in Play mode. Start Play mode now? The capture begins by itself once the game has loaded.", "Start Play mode", "Cancel"))
                return;
            SessionState.SetString(PendingKey, kind);
            NeonSyndicateSetup.PlayGame();
            if (!EditorApplication.isPlayingOrWillChangePlaymode) SessionState.EraseString(PendingKey);
        }

        private static void StartNow(string kind)
        {
            bool started = kind == Perf ? QaTools.StartPerfCapture() :
                (kind == Rebirth ? QaTools.StartRebirthShots() : QaTools.StartGoldenShots());
            if (started)
            {
                Debug.Log("[Neon Syndicate] " + QaTools.LastMessage + " Esc cancels.");
                WatchUntilDone();
            }
            else
            {
                Debug.LogWarning("[Neon Syndicate] " + QaTools.LastMessage);
                EditorUtility.DisplayDialog("Neon Syndicate QA", QaTools.LastMessage, "OK");
            }
        }

        // ------------------------------------------------------------------ result

        private static bool _watching;

        /// <summary>Shows the result in a dialog once the capture ends (with a button to open its files).</summary>
        private static void WatchUntilDone()
        {
            if (_watching) return;
            _watching = true;
            EditorApplication.update += PollCapture;
        }

        private static void PollCapture()
        {
            if (EditorApplication.isPlaying && QaTools.Busy) return;
            EditorApplication.update -= PollCapture;
            _watching = false;
            if (!EditorApplication.isPlaying) return;
            string output = QaTools.LastOutput;
            if (QaTools.LastSucceeded && (File.Exists(output) || Directory.Exists(output)))
            {
                if (EditorUtility.DisplayDialog("Neon Syndicate QA", QaTools.LastMessage, "Show the files", "Close"))
                    EditorUtility.RevealInFinder(output);
            }
            else EditorUtility.DisplayDialog("Neon Syndicate QA", QaTools.LastMessage, "OK");
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                string kind = SessionState.GetString(PendingKey, "");
                if (string.IsNullOrEmpty(kind)) return;
                SessionState.EraseString(PendingKey);
                StartNow(kind);
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseString(PendingKey);
            }
        }
    }
}
