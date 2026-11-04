using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The companion HUD: the compact companion card above the minimap (BondSystem.CardH8S: name, kind and rank, health
    /// with points, a mode chip with the detail or the reason it cannot follow, and both assists filling as their
    /// cooldowns run out), the hold-N progress and the brace capacity, plus the last one or two command lines and
    /// barks. During a rescue it also draws the keep-distance warning and the closing tuning ring over the creature, and
    /// a combat order brackets its target. The panel's place comes from HudLayout04E (above the guidance route hint,
    /// below the ward panel).
    /// </summary>
    public sealed partial class BondSystem
    {
        private float _hintUntil;

        private void DrawHud(float W, float H)
        {
            PlayerController pl = Game.Player;
            if (pl == null || !Game.IsPlaying) return;
            if (_rescue != null) DrawRescue(W, H);
            DrawTargetBracketH8S();
            CompanionRecord r = Active;
            // The layout reserves two message lines at the default HUD scale, fewer (and narrower) at a larger scale.
            float maxLine = HudLayout04E.BondsMessageMaxWidth(W, H);
            int maxMessages = HudLayout04E.BondsMessageLines(W, H);
            if (r == null)
            {
                DrawMessages(HudLayout04E.MinimapX, HudLayout04E.BondsIdleMessagesBottom(W, H), HudLayout04E.MinimapW, maxLine, maxMessages);
                return;
            }
            CompanionProfile prof = ProfileOf(r);
            Rect panel = HudLayout04E.BondsPanel(W, H);
            float pw = panel.width, ph = panel.height;
            float x = panel.x, y = panel.y;
            // H8S: the compact card (BondSystem.CardH8S) fills the same box the panel always used.
            DrawCardH8S(panel, r, prof, pl);
            float hold = HoldProgress;
            float brace = BraceCapacity01;
            if (hold > 0f)
            {
                UIKit.Bar(new Rect(x, y + ph - 4f, pw, 4f), hold, prof.Accent, new Color(1f, 1f, 1f, 0.1f));
                // Above the brace strip while the brace is up, so the two never print over each other.
                UIKit.Text(new Rect(x, y - (brace >= 0f ? 54f : 24f), pw, 20f), "Calling " + NameOf(r) + "...", 15, prof.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
            }
            if (brace >= 0f)
            {
                UIKit.Rect(new Rect(x - 4f, y - 30f, pw + 8f, 24f), new Color(0f, 0f, 0f, 0.55f));
                UIKit.Text(new Rect(x + 4f, y - 29f, 90f, 22f), "BRACE", 15, prof.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Bar(new Rect(x + 74f, y - 22f, pw - 84f, 8f), brace, prof.Accent, new Color(1f, 1f, 1f, 0.12f));
            }
            if (Time.unscaledTime < _hintUntil && hold <= 0f && brace < 0f)
            {
                string keys = Pad.Connected && Pad.LastUsed
                    ? Pad.Tag(PadButton.LB) + "+Down: follow / wait, hold: call   " + Pad.Tag(PadButton.LB) + "+Up: assist"
                    : "N: follow / wait   hold N: call   U: assist";
                UIKit.Text(new Rect(x, y - 24f, pw + 200f, 20f), keys, 15, new Color(1f, 1f, 1f, 0.85f), TextAnchor.LowerLeft, FontStyle.Bold);
            }
            bool hintLine = (Time.unscaledTime < _hintUntil && brace < 0f) || (hold > 0f && brace >= 0f);
            DrawMessages(x, y - (brace >= 0f ? 34f : 26f) - (hintLine ? 22f : 0f), pw, maxLine, maxMessages);
        }

        /// <summary>
        /// The last command lines (the newest maxCount of them), stacked upward from `bottom`; a line wider than maxWidth
        /// wraps onto two rows.
        /// </summary>
        private void DrawMessages(float x, float bottom, float w, float maxWidth, int maxCount)
        {
            float yy = bottom;
            int shown = 0;
            for (int i = _messages.Count - 1; i >= 0 && shown < maxCount; i--, shown++)
            {
                Message m = _messages[i];
                if (!m.Measured)
                {
                    // Measured once per line (not every GUI pass). A bark grows with the H8S text size only while it still
                    // fits on one row; a line that would wrap keeps the base size, so it never outgrows its reserved row.
                    m.Measured = true;
                    m.Width = UIKit.Measure(m.Text, 16).x;
                    m.ScaledFont = m.Bark ? AccessibilityRulesH8S.Font(16, Settings.GuideTextScaleH8S, 20) : 16;
                    m.WidthScaled = m.ScaledFont > 16 ? UIKit.Measure(m.Text, m.ScaledFont).x : m.Width;
                    _messages[i] = m;
                }
                bool big = m.ScaledFont > 16 && m.WidthScaled + 16f <= maxWidth;
                int font = big ? m.ScaledFont : 16;
                float width = big ? m.WidthScaled : m.Width;
                float a = Mathf.Clamp01(m.Left * 2f);
                float lw = Mathf.Min(Mathf.Max(w, width + 16f), maxWidth);
                float lh = width + 16f > maxWidth ? 42f : (big ? font + 10f : 24f);
                yy -= lh + 2f;
                UIKit.Rect(new Rect(x - 4f, yy, lw + 8f, lh), new Color(0f, 0f, 0f, (m.Bark ? 0.62f : 0.55f) * a));
                UIKit.Text(new Rect(x + 4f, yy + 2f, lw - 8f, lh - 2f), m.Text, font, m.Warn ? new Color(1f, 0.78f, 0.5f, a) : new Color(0.9f, 0.95f, 1f, a), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
        }

        private void DrawRescue(float W, float H)
        {
            CompanionRescueSite s = _rescue;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (s == null || s.Rules == null || cam == null) return;
            Vector3 at = s.Body != null ? s.Body.ChestPosition : s.Site + Vector3.up * 0.6f;
            Vector3 sp = cam.WorldToScreenPoint(at);
            if (sp.z <= 0.1f) return;
            float k = UIKit.Scale;
            Vector2 g = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
            Color accent = s.Profile.Accent;
            if (s.Rules.Flaring)
            {
                string t = s.Rules.TooClose ? "BACK AWAY: it is warning you" : "Keep your distance";
                UIKit.Text(new Rect(g.x - 260f, g.y - 110f, 520f, 30f), t, 22, s.Rules.TooClose ? new Color(1f, 0.5f, 0.6f) : new Color(1f, 0.8f, 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (s.Rules.Phase == RescuePhase.Tuning)
            {
                float ring = s.Rules.Ring;
                float size = Mathf.Lerp(46f, 170f, ring);
                bool window = ring <= RescueRules.BeatWindow;
                UIKit.Tex(new Rect(g.x - 28f, g.y - 28f, 56f, 56f), UIKit.Ring, new Color(1f, 1f, 1f, 0.55f));
                UIKit.Tex(new Rect(g.x - size * 0.5f, g.y - size * 0.5f, size, size), UIKit.Ring, window ? new Color(0.5f, 1f, 0.7f, 0.95f) : new Color(accent.r, accent.g, accent.b, 0.85f));
                for (int i = 0; i < RescueRules.BeatsNeeded; i++)
                {
                    bool done = i < s.Rules.Beats;
                    UIKit.Tex(new Rect(g.x - 30f + i * 24f, g.y + 96f, 14f, 14f), UIKit.Circle, done ? accent : new Color(1f, 1f, 1f, 0.3f));
                }
                string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.North) : "E";
                UIKit.Text(new Rect(g.x - 260f, g.y + 116f, 520f, 26f), "Press " + key + " as the ring closes", 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else if (s.Rules.Phase == RescuePhase.Approach && !s.Rules.Flaring && s.Rules.Calm < 1f)
            {
                UIKit.Bar(new Rect(g.x - 60f, g.y + 70f, 120f, 6f), s.Rules.Calm, accent, new Color(0f, 0f, 0f, 0.5f));
                UIKit.Text(new Rect(g.x - 200f, g.y + 78f, 400f, 22f), "Walk in calmly", 16, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
            }
        }

        /// <summary>Shows the control hint on the companion panel for a while (after a bond, and for the first sessions).</summary>
        private void ShowControlHint(float seconds)
        {
            _hintUntil = Time.unscaledTime + seconds;
        }
    }
}
