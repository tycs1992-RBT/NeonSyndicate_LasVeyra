using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The ACCESS pause page (H8S, MenuPages order 106, after STATUS): one place for the readability and comfort options
    /// that the guidance, cast and companion HUD honour. The HUD scale, subtitle size and subtitles rows edit the same
    /// Settings fields as the General settings page (one owner, two views); the H8S rows add the text size of the
    /// companion barks, the quest tracker and waypoint labels, high-contrast markers, the companion barks themselves and
    /// "double tap to call" for the companion button. Everything is kept in PlayerPrefs (NSLV_ prefix) when saved, never
    /// in a save file. Reduced impact effects are shown here and changed on their own page (they are part of the save).
    /// Every control is a UIKit button or slider, so mouse, keyboard and controller all work.
    /// </summary>
    public static class AccessibilityPageH8S
    {
        /// <summary>The tab's order: after STATUS (105), before WORLD (108).</summary>
        public const int Order = 106;
        public const string Title = "ACCESS";

        private static string _status = "";
        private static float _statusUntil;
        // Cached texts: rebuilt only when what they show changes, never on every GUI pass.
        private static int _uiPct = -1, _subPct = -1, _guidePct = -1, _helpKey = -1, _reducedKey = -1;
        private static string _uiText = "", _subText = "", _guideText = "", _help = "", _reducedText = "";

        /// <summary>Adds the page (called once at boot by the campaign director's UI setup).</summary>
        public static void Register()
        {
            MenuPages.Register(new MenuPage { Title = Title, Order = Order, Draw = Draw });
        }

        public static void ResetStatics()
        {
            _status = "";
            _statusUntil = 0f;
            _uiPct = _subPct = _guidePct = _helpKey = _reducedKey = -1;
            _uiText = _subText = _guideText = _help = _reducedText = "";
        }

        /// <summary>"125%" for a scale, rebuilt only when the whole percent changes.</summary>
        private static string Percent(float value, ref int key, ref string text)
        {
            int pct = Mathf.RoundToInt(value * 100f);
            if (pct != key)
            {
                key = pct;
                text = pct + "%";
            }
            return text;
        }

        private static void Draw(Rect r)
        {
            UIKit.Rect(r, UIKit.Panel);
            float x = r.x + 36f, y = r.y + 20f, w = r.width - 72f;
            UIKit.Text(new Rect(x, y, w, 40f), "ACCESSIBILITY AND READABILITY", 30, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 46f;
            UIKit.Text(new Rect(x, y, w, 48f), "Larger text, clearer markers and easier companion commands. Changes apply at once; Save keeps them for the next session.",
                18, new Color(0.85f, 0.88f, 0.92f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 58f;
            float lw = 360f, sw = Mathf.Min(420f, w - lw - 140f);
            Settings.UiScale = Slider(ref y, x, lw, sw, "HUD scale (all HUD panels)", Settings.UiScale, 0.75f, 1.5f, Percent(Settings.UiScale, ref _uiPct, ref _uiText));
            Settings.SubtitleScale04L = Slider(ref y, x, lw, sw, "Subtitle size (dialogue)", Settings.SubtitleScale04L, 0.9f, 1.4f, Percent(Settings.SubtitleScale04L, ref _subPct, ref _subText));
            Settings.GuideTextScaleH8S = AccessibilityRulesH8S.ClampTextScale(Slider(ref y, x, lw, sw, "Guidance and companion text size", Settings.GuideTextScaleH8S,
                AccessibilityRulesH8S.MinTextScale, AccessibilityRulesH8S.MaxTextScale, Percent(Settings.GuideTextScaleH8S, ref _guidePct, ref _guideText)));
            UIKit.Text(new Rect(x, y - 6f, w, 24f), "Scales the quest tracker line, waypoint labels, journal guidance, companion barks and the target bracket label.",
                15, new Color(0.7f, 0.75f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 30f;
            float bw = Mathf.Min(360f, (w - 20f) * 0.5f);
            Settings.Subtitles = Toggle(new Rect(x, y, bw, 44f), "Subtitles", Settings.Subtitles);
            Settings.HighContrastMarkersH8S = Toggle(new Rect(x + bw + 20f, y, bw, 44f), "High-contrast markers", Settings.HighContrastMarkersH8S);
            y += 54f;
            Settings.CompanionBarksH8S = Toggle(new Rect(x, y, bw, 44f), "Companion barks", Settings.CompanionBarksH8S);
            Settings.CompanionDoubleTapCallH8S = Toggle(new Rect(x + bw + 20f, y, bw, 44f), "Double tap to call companion", Settings.CompanionDoubleTapCallH8S);
            y += 58f;
            int helpKey = Pad.Connected && Pad.LastUsed ? 1 : 0;
            if (helpKey != _helpKey)
            {
                _helpKey = helpKey;
                string keys = helpKey == 1 ? "hold LB and tap D-pad down twice" : "tap N twice";
                _help = "High-contrast markers: quest and giver glyphs, the waypoint pin, edge arrows and overhead markers grow, use full-strength colours and sit on a dark outline.\n" +
                    "Companion barks: short subtitles for your companion's calls (an assist, a marked target, low health, downed, regrouping). Its sounds always play. Barks also follow the Subtitles switch.\n" +
                    "Double tap to call: " + keys + " to call your companion to your side, for anyone who finds holding the button hard. Holding still works; a single tap then waits a third of a second before it toggles follow and wait.";
            }
            UIKit.Text(new Rect(x, y, w, 150f), _help, 16, new Color(0.82f, 0.86f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 156f;
            bool reduced = Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects;
            int reducedKey = reduced ? 1 : 0;
            if (reducedKey != _reducedKey)
            {
                _reducedKey = reducedKey;
                _reducedText = "Reduced impact effects: " + (reduced ? "ON" : "OFF") + " (change it on the COMBAT page; it is kept with your save). When on, the companion card and its target bracket stop pulsing.";
            }
            UIKit.Text(new Rect(x, y, w, 48f), _reducedText, 16, reduced ? UIKit.Cyan : new Color(0.75f, 0.78f, 0.82f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (UIKit.Button(new Rect(r.xMax - 300f, r.yMax - 64f, 260f, 46f), "Save settings", false, 20))
            {
                Settings.Save();
                _status = "Saved. These options stay on this computer, not in your save file.";
                _statusUntil = Time.unscaledTime + 4f;
            }
            if (_status.Length > 0 && Time.unscaledTime < _statusUntil)
                UIKit.Text(new Rect(x, r.yMax - 58f, w - 320f, 40f), _status, 17, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true, true);
        }

        private static float Slider(ref float y, float x, float lw, float sw, string label, float value, float min, float max, string shown)
        {
            UIKit.Text(new Rect(x, y + 6f, lw, 30f), label, 21, Color.white, TextAnchor.UpperLeft);
            float v = UIKit.Slider(new Rect(x + lw, y + 4f, sw, 30f), value, min, max);
            UIKit.Text(new Rect(x + lw + sw + 20f, y + 6f, 120f, 30f), shown, 21, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 46f;
            return v;
        }

        private static bool Toggle(Rect r, string label, bool value)
        {
            // The two labels per switch are constants built once by the compiler's string interning of the literals below.
            return UIKit.Button(r, Label(label, value), value, 20) ? !value : value;
        }

        private static string Label(string label, bool on)
        {
            switch (label)
            {
                case "Subtitles": return on ? "Subtitles: ON" : "Subtitles: OFF";
                case "High-contrast markers": return on ? "High-contrast markers: ON" : "High-contrast markers: OFF";
                case "Companion barks": return on ? "Companion barks: ON" : "Companion barks: OFF";
                default: return on ? "Double tap to call companion: ON" : "Double tap to call companion: OFF";
            }
        }
    }
}
