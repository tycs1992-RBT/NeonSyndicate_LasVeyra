using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The descent window ("Down to the Crown", the descent_window choice) applied to the Ash Reliquary. The window is not
    /// saved and has no switch of its own: four times a second it is read from the running quest's phase
    /// (QuestPhase.Window: "early", "late" or "choice" for the stored decision), so completing, failing or abandoning the
    /// quest, and loading a save, all clear it the moment the run ends. While it is set, only inside the reliquary:
    /// breathing heat and lava there scale (UnderheartWindowRules.HeatScale, read by the hazard code and HazardAt), the
    /// practical lights and the ambient light dim (early) or brighten (late), and the late window shows the lit column
    /// bands and turns the four column lights into the light budget. The F2 Underheart page can force a window for testing.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private const float WindowInterval = 0.25f;

        /// <summary>The descent window in effect: "" (none), "early" or "late".</summary>
        public string DescentWindow { get; private set; } = "";

        /// <summary>True when the window comes from the F2 test override rather than the running quest.</summary>
        public bool DescentWindowForced { get { return _windowForced != null; } }

        /// <summary>The F2 override: null follows the quest; "", "early" or "late" force that window.</summary>
        private string _windowForced;
        private float _windowClock;
        private bool _windowLitShown;

        /// <summary>The heat multiplier inside a region under the current window (1 outside the reliquary).</summary>
        public float WindowHeatScale(string region)
        {
            return UnderheartWindowRules.HeatScale(DescentWindow, region);
        }

        /// <summary>Forces a window from the F2 page (null hands it back to the running quest).</summary>
        public void ForceDescentWindow(string window)
        {
            _windowForced = window == null ? null : UnderheartWindowRules.Normalize(window);
            _windowClock = 0f;
        }

        /// <summary>Every frame: re-reads the wanted window four times a second and applies it when it changed.</summary>
        private void UpdateWindow()
        {
            _windowClock -= Time.unscaledDeltaTime;
            if (_windowClock > 0f) return;
            _windowClock = WindowInterval;
            string want = _windowForced ?? WindowFromQuest();
            if (want != DescentWindow) ApplyWindow(want, _windowForced != null ? "test override" : "quest");
        }

        /// <summary>The window the running quest's current phase asks for ("" when no quest runs or the phase sets none).</summary>
        private static string WindowFromQuest()
        {
            QuestBook book = Game.Quests;
            QuestRun r = book != null ? book.Running : null;
            if (r == null || r.Quest == null || r.Phase < 0 || r.Phase >= r.Quest.Phases.Count) return "";
            // Only while that run is the live mission (the fail and pass paths end it at once).
            if (Game.Missions == null || Game.Missions.Active == null || Game.Missions.Active != r.C) return "";
            QuestPhase ph = r.Quest.Phases[r.Phase];
            if (ph == null || string.IsNullOrEmpty(ph.Window)) return "";
            return CampaignRules04E.ResolveWindow(ph.Window, book.Choice(CampaignRules04E.DescentWindowKey));
        }

        private void ApplyWindow(string window, string source)
        {
            DescentWindow = UnderheartWindowRules.Normalize(window);
            ShowWindowLit(UnderheartWindowRules.ColumnsLit(DescentWindow));
            // Relight at once so the lamps and the column lights change with the heat, not a quarter second later.
            _lightClock = 0f;
            Log.Info("UNDERHEART_04E descent window: " + UnderheartWindowRules.Title(DescentWindow) + " (" + source + ").");
        }

        private void ShowWindowLit(bool on)
        {
            if (_builder == null)
            {
                _windowLitShown = false;
                return;
            }
            if (_windowLitShown == on) return;
            _windowLitShown = on;
            foreach (GameObject go in _builder.WindowLitObjects)
                if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        /// <summary>New game, load and the F2 reset: no window, no override, the lit bands hidden.</summary>
        private void ResetWindow()
        {
            _windowForced = null;
            _windowClock = 0f;
            DescentWindow = "";
            _windowLitShown = true;
            ShowWindowLit(false);
        }
    }
}
