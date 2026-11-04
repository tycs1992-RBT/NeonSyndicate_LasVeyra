using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The F2 debug page "Live crowd" (DebugPages order 55): the saved "Realistic people" switch, the session switch, the
    /// budget and radius, the live counters (candidates, granted, live, binds and reuse, pooled bodies, fallbacks, the Low
    /// preset declines, the tick cost) and the crowd rows the registry holds with their contract state. Registered once
    /// from the live adapter's Attach, so the page exists even when no crowd body was prepared and says so.
    /// </summary>
    public static class LiveCrowdPage
    {
        /// <summary>The F2 tab order (after Encounters 54).</summary>
        public const int Order = 55;
        public const string Title = "Live crowd";

        private static bool _registered;
        private static string _summary = "", _status = "";
        private static int _summaryKey = -1;
        private static float _statusUntil;

        /// <summary>Forgets the page (Enter Play Mode without a domain reload starts clean; DebugPages itself is reset by the registry hub).</summary>
        public static void ResetStatics()
        {
            _registered = false;
            _summary = "";
            _status = "";
            _summaryKey = -1;
            _statusUntil = 0f;
        }

        /// <summary>Adds the page to the F2 menu once per session.</summary>
        public static void EnsureRegistered()
        {
            if (_registered) return;
            _registered = true;
            DebugPages.Register(new MenuPage { Title = Title, Order = Order, Draw = Draw });
        }

        private static void Note(string text)
        {
            _status = text;
            _statusUntil = Time.unscaledTime + 4f;
        }

        private static void Draw(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f, w = r.width - 40f;
            UIKit.Text(new Rect(x, y, w, 30f), "LIVE CROWD (realistic people)", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            UIKit.Text(new Rect(x, y, w, 24f), "Location: camera at " + (Game.Cam != null ? Game.Cam.transform.position : Game.PlayerPosition).ToString("F1") + ". Only nearby people within the radius receive a budgeted body.", 16, Color.white, TextAnchor.UpperLeft);
            y += 28f;
            UIKit.Text(new Rect(x, y, w, 44f), "Civilians, clerks, police, sentinels, gang members and mission enemies nearest the camera are drawn with real skinned bodies; everyone else, and everyone beyond the budget, keeps the procedural body. Docs/LIVE_CROWD.md.",
                16, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 50f;
            float bw = 300f, bh = 44f, gap = 16f;
            if (UIKit.Button(new Rect(x, y, bw, bh), Settings.RealisticPeople ? "Realistic people: ON (saved)" : "Realistic people: OFF (saved)", Settings.RealisticPeople, 17))
            {
                Settings.RealisticPeople = !Settings.RealisticPeople;
                Settings.Save();
                Note(Settings.RealisticPeople ? "Realistic people on. Saved with the settings." : "Realistic people off. Saved with the settings; bodies are handed back on the next tick.");
            }
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), LiveCrowd.Enabled ? "This session: ON" : "This session: OFF", LiveCrowd.Enabled, 17))
            {
                LiveCrowd.Enabled = !LiveCrowd.Enabled;
                Note(LiveCrowd.Enabled ? "Crowd bodies on for this session." : "Crowd bodies off for this session (the saved setting is unchanged).");
            }
            y += bh + 14f;
            UIKit.Text(new Rect(x, y + 8f, 150f, 30f), "Budget " + LiveCrowd.Budget, 19, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(x + 160f, y, 60f, bh), "-", false, 22)) LiveCrowd.Budget = Mathf.Max(0, LiveCrowd.Budget - 2);
            if (UIKit.Button(new Rect(x + 230f, y, 60f, bh), "+", false, 22)) LiveCrowd.Budget = Mathf.Min(LiveCrowdRules.MaxBudget, LiveCrowd.Budget + 2);
            UIKit.Text(new Rect(x + 330f, y + 8f, 170f, 30f), "Radius " + Mathf.RoundToInt(LiveCrowd.Radius) + " m", 19, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(x + 500f, y, 60f, bh), "-", false, 22)) LiveCrowd.Radius = Mathf.Max(LiveCrowdRules.MinRadius, LiveCrowd.Radius - 10f);
            if (UIKit.Button(new Rect(x + 570f, y, 60f, bh), "+", false, 22)) LiveCrowd.Radius = Mathf.Min(LiveCrowdRules.MaxRadius, LiveCrowd.Radius + 10f);
            if (UIKit.Button(new Rect(x + 670f, y, 220f, bh), "Defaults", false, 17))
            {
                LiveCrowd.Budget = LiveCrowdRules.DefaultBudget;
                LiveCrowd.Radius = LiveCrowdRules.DefaultRadius;
                Note("Budget " + LiveCrowdRules.DefaultBudget + ", radius " + Mathf.RoundToInt(LiveCrowdRules.DefaultRadius) + " m.");
            }
            y += bh + 14f;
            // Rebuilt only when a number changes (the page is drawn only while F2 is open).
            int key = LiveCrowd.Candidates * 7919 + LiveCrowd.Granted * 131 + LiveCrowd.Live * 17 + LiveCrowd.Binds * 3 + LiveCrowd.PooledIdle + LiveCrowd.Fallbacks * 1013
                + LiveCrowd.DeclinedLowDetail * 29 + LiveCrowd.Instantiated * 5 + Mathf.RoundToInt(LiveCrowd.LastTickMs * 100f) * 4099;
            if (key != _summaryKey)
            {
                _summaryKey = key;
                _summary = "People a body could serve " + LiveCrowd.Candidates + "   granted " + LiveCrowd.Granted + "   drawn now " + LiveCrowd.Live
                    + "   binds " + LiveCrowd.Binds + " (" + LiveCrowd.Reused + " from the pool)   pooled idle " + LiveCrowd.PooledIdle + "   bodies instantiated " + LiveCrowd.Instantiated
                    + "   fallbacks " + LiveCrowd.Fallbacks + "   tick " + LiveCrowd.LastTickMs.ToString("0.00") + " ms";
            }
            UIKit.Text(new Rect(x, y, w, 26f), _summary, 17, Color.white, TextAnchor.UpperLeft);
            y += 30f;
            if (LiveCrowd.DeclinedLowDetail > 0)
            {
                UIKit.Text(new Rect(x, y, w, 26f), LiveCrowd.DeclinedLowDetail + " people have the reduced 17-bone body of the Low graphics preset and stay procedural. Choose Medium or higher and let them respawn.", 16, UIKit.Gold, TextAnchor.UpperLeft);
                y += 28f;
            }
            if (LiveCrowd.LastFallback.Length > 0)
            {
                UIKit.Text(new Rect(x, y, w, 44f), "Last fallback: " + LiveCrowd.LastFallback, 16, new Color(1f, 0.6f, 0.4f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 48f;
            }
            // Hair, beards and headwear (LiveCastVisual.Hair): the measured skulls and the meshes built this session.
            UIKit.Text(new Rect(x, y, w, 26f), "Hair and headwear: " + LiveHairKits.Summary() + (LiveHairMaterials.Failed ? "   strand materials failed: " + LiveHairMaterials.Why : "") + (LiveCrowd.HairProblems > 0 ? "   heads not dressed " + LiveCrowd.HairProblems + " (" + LiveCrowd.LastHairProblem + ")" : ""),
                16, LiveHairMaterials.Failed || LiveCrowd.HairProblems > 0 ? new Color(1f, 0.6f, 0.4f) : new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 30f;
            if (!Settings.RealisticPeople || !LiveCrowd.Enabled)
            {
                UIKit.Text(new Rect(x, y, w, 26f), "Switched off: every person is procedural.", 17, UIKit.Cyan, TextAnchor.UpperLeft);
                y += 30f;
            }
            LiveCastRegistry registry = LiveCastRegistry.Loaded;
            if (registry == null)
            {
                UIKit.Text(new Rect(x, y, w, 48f), "No live character registry on this computer. Unity prepares the crowd bodies on its first load (Neon Syndicate > Characters > Rebuild live characters prepares them again); a player build carries them from the machine that built it.",
                    16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            UIKit.Text(new Rect(x, y, w, 26f), "Crowd bodies in the registry (drop-ins first; the first body that serves a role and sex wins):", 17, new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperLeft);
            y += 28f;
            int shown = 0;
            foreach (LiveCastRow row in registry.Rows)
            {
                if (row == null || row.Model == null || row.CrowdRoles == 0) continue;
                string reason;
                bool ok = LiveCastRegistry.Contract(row, out reason);
                string line = (row.Label.Length > 0 ? row.Label : row.Model.Identity) + "   " + LiveCrowdRules.PeopleWord(row.CrowdFemale, row.CrowdAge) + "   " + LiveCrowdRules.RoleList(row.CrowdRoles)
                    + "   " + (row.Model.Enabled ? (ok ? "ready" : "refused: " + reason) : "disabled") + "   " + row.Origin;
                UIKit.Text(new Rect(x, y, w, 24f), line, 16, ok && row.Model.Enabled ? Color.white : new Color(1f, 0.6f, 0.4f), TextAnchor.UpperLeft);
                y += 24f;
                shown++;
            }
            if (shown == 0)
            {
                UIKit.Text(new Rect(x, y, w, 48f), "None. The two studies were not prepared (read the Console warning that starts with \"Live character not prepared\", then Neon Syndicate > Characters > Rebuild live characters).",
                    16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 52f;
            }
            if (_status.Length > 0 && Time.unscaledTime < _statusUntil)
                UIKit.Text(new Rect(x, y + 8f, w, 26f), _status, 17, UIKit.Cyan, TextAnchor.UpperLeft);
        }
    }
}
