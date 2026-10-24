using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Puts the reason a live character is not showing on the screen for a few seconds, so the owner never has to dig through
    /// the Console (owner report 2026-10-23: the Blender Malik stayed procedural and nothing in the game said why). Two
    /// sources: the Editor generator's last refusal, read once when the player's body is built in the Editor, and any runtime
    /// fallback of the live adapter. One short panel near the top of the gameplay HUD; nothing is saved or allocated per frame.
    /// </summary>
    public static class LiveCastNotice
    {
        /// <summary>The Editor session key where the live character generator leaves its last refusal ("" when all is well).</summary>
        public const string EditorProblemKey = "NeonSyndicate.LiveCast.Problem";
        /// <summary>The menu that prepares the live characters again (mirrors LiveCastSetup.MenuPath in the Editor assembly).</summary>
        public const string RebuildMenu = "Neon Syndicate > Characters > Rebuild live characters";

        private const string OverlayKey = "livecast.notice";
        private const float ShowSeconds = 30f;
        private const int MaxReason = 420;

        private static string _text = "";
        private static float _remaining;
        private static int _lastFrame = -1;
        private static bool _checkedPlayer;
        private static readonly System.Collections.Generic.HashSet<string> _shown = new System.Collections.Generic.HashSet<string>();
        private static readonly System.Collections.Generic.List<string> _problems = new System.Collections.Generic.List<string>();
        public static System.Collections.Generic.IReadOnlyList<string> Problems { get { return _problems; } }
        public static string LastProblem { get; private set; } = "";
        public static bool CompareLegacy { get; set; }

        /// <summary>Forgets the notice (Enter Play Mode without a domain reload starts clean).</summary>
        public static void ResetStatics()
        {
            _text = "";
            _remaining = 0f;
            _lastFrame = -1;
            _checkedPlayer = false;
            _shown.Clear();
            _problems.Clear();
            LastProblem = "";
            CompareLegacy = false;
            VisibleRealityV0.ResetStatics();
            LiveWorldImportRulesV0.ResetStatics();
        }

        /// <summary>The text currently queued or showing ("" when none), for diagnostics.</summary>
        public static string Current { get { return _remaining > 0f ? _text : ""; } }

        /// <summary>
        /// Called once per play session when the player's body is built. When no live row serves the player, the Editor shows
        /// the generator's recorded refusal (or that nothing was prepared on this computer). Player builds stay silent.
        /// </summary>
        public static void CheckPlayer(LiveCastRegistry registry)
        {
            if (_checkedPlayer) return;
            _checkedPlayer = true;
#if UNITY_EDITOR
            string problem = UnityEditor.SessionState.GetString(EditorProblemKey, "");
            // Every refusal reaches the screen (owner report 2026-10-24: only the last Console line was visible).
            if (!string.IsNullOrEmpty(problem))
                AssetProblem("Character preparation", problem, "Stop Play, then choose " + RebuildMenu + ".");
#endif
            if (registry == null || !registry.HasPlayerRow)
                AssetProblem("Malik", "No prepared player row is available on this computer.", "Stop Play, pull Git LFS, then choose " + RebuildMenu + ". A player build needs rebuilding after preparation.");
        }

        /// <summary>A live visual fell back to the procedural look while playing.</summary>
        public static void Fallback(string label, string reason)
        {
            AssetProblem(label, reason, Remedy(reason));
        }

        public static string Remedy(string reason)
        {
            string lower = (reason ?? "").ToLowerInvariant();
            if (lower.Contains("pointer") || lower.Contains("fingerprint") || lower.Contains("missing texture"))
                return "Stop Play, run git lfs pull, then " + RebuildMenu + ".";
            if (lower.Contains("17-bone")) return "Choose Medium or higher graphics, then restart Free Roam. Your Low preset is kept until you change it.";
            return "Stop Play, then " + RebuildMenu + ". Read F2 > Live characters for the retained reason.";
        }

        /// <summary>One notice per category per session. Reasons remain on the diagnostics page after the notice fades.</summary>
        public static void AssetProblem(string category, string reason, string remedy)
        {
            if (_shown.Count >= 16 || !_shown.Add(category ?? "Asset")) return;
            LastProblem = (category ?? "Asset") + ": " + Short(reason) + " " + remedy;
            _problems.Add(LastProblem);
            Show(LastProblem);
        }

        private static string Short(string reason)
        {
            if (string.IsNullOrEmpty(reason)) return "no reason was given.";
            string text = reason.Replace('\n', ' ').Replace('\r', ' ').Trim();
            return text.Length <= MaxReason ? text : text.Substring(0, MaxReason) + "...";
        }

        private static void Show(string text)
        {
            _text = text;
            _remaining = ShowSeconds;
            HudOverlays.Register(OverlayKey, Draw);
        }

        /// <summary>Counts down only while the gameplay HUD is drawn, so a notice raised during boot waits for play.</summary>
        private static void Draw(float w, float h)
        {
            if (_remaining <= 0f || string.IsNullOrEmpty(_text)) return;
            if (Time.frameCount != _lastFrame)
            {
                _lastFrame = Time.frameCount;
                _remaining -= Mathf.Min(Time.unscaledDeltaTime, 0.25f);
            }
            float alpha = Mathf.Clamp01(_remaining / 1.5f);
            float width = Mathf.Min(1180f, w - 80f);
            Rect panel = new Rect((w - width) * 0.5f, h * 0.11f, width, 150f);
            UIKit.Rect(panel, new Color(0.06f, 0.05f, 0.08f, 0.86f * alpha));
            UIKit.Frame(panel, new Color(0.68f, 0.53f, 0.31f, 0.9f * alpha), 2f);
            UIKit.Text(new Rect(panel.x + 20f, panel.y + 10f, panel.width - 40f, panel.height - 20f), _text, 19, new Color(0.95f, 0.91f, 0.82f, alpha), TextAnchor.MiddleCenter, FontStyle.Normal, true, true);
        }
    }
}
