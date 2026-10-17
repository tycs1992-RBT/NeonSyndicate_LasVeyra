using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The companion HUD: a small panel above the minimap with the companion's name, kind and rank, a health bar, its
    /// state (following, waiting, assisting, resting, down with the time left, fused, riding along), why it cannot
    /// follow when it cannot, when its assists are ready, the hold-N progress and the brace capacity, plus the last one
    /// or two command lines. During a rescue it also draws the keep-distance warning and the closing tuning ring over
    /// the creature.
    /// </summary>
    public sealed partial class BondSystem
    {
        private float _hintUntil;
        private float _hudRefresh;
        private string _hudStatus = "";
        private string _hudAssist = "";
        private int _hudUid;

        private void DrawHud(float W, float H)
        {
            PlayerController pl = Game.Player;
            if (pl == null || !Game.IsPlaying) return;
            if (_rescue != null) DrawRescue(W, H);
            CompanionRecord r = Active;
            if (r == null)
            {
                DrawMessages(36f, H - 216f - 70f - 18f, 330f);
                return;
            }
            CompanionProfile prof = ProfileOf(r);
            float pw = 330f, ph = 92f;
            float x = 36f, y = H - 216f - 70f - 14f - ph;
            UIKit.Rect(new Rect(x - 4f, y - 4f, pw + 8f, ph + 8f), new Color(0f, 0f, 0f, 0.6f));
            UIKit.Rect(new Rect(x - 4f, y - 4f, 4f, ph + 8f), prof.Accent);
            UIKit.Text(new Rect(x + 6f, y, pw - 12f, 26f), NameOf(r), 20, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x + 6f, y + 2f, pw - 12f, 24f), prof.Title + "  " + BondRules.RankName(r.Rank), 15, prof.Accent, TextAnchor.UpperRight, FontStyle.Bold);
            bool downed = r.State == (int)CompanionState.Downed;
            Color hc = downed ? new Color(0.5f, 0.5f, 0.55f) : (r.Health01 < 0.3f ? new Color(1f, 0.4f, 0.3f) : UIKit.Health);
            UIKit.Bar(new Rect(x + 6f, y + 28f, pw - 12f, 7f), r.Health01, hc, new Color(1f, 1f, 1f, 0.12f));
            if (Time.unscaledTime >= _hudRefresh || _hudUid != r.Uid)
            {
                // The two text lines are rebuilt four times a second, not on every GUI pass.
                _hudRefresh = Time.unscaledTime + 0.25f;
                _hudUid = r.Uid;
                _hudStatus = StatusLine(r, pl);
                _hudAssist = AssistLine(r);
            }
            string line = _hudStatus;
            Color lc = new Color(0.85f, 0.88f, 0.92f);
            if (_body != null && !string.IsNullOrEmpty(_body.BlockReason) && r.State == (int)CompanionState.Following && !_riding)
            {
                line = _body.BlockReason;
                lc = new Color(1f, 0.75f, 0.45f);
            }
            UIKit.Text(new Rect(x + 6f, y + 38f, pw - 12f, 22f), line, 15, lc, TextAnchor.UpperLeft, FontStyle.Normal, true, false);
            UIKit.Text(new Rect(x + 6f, y + 60f, pw - 12f, 22f), _hudAssist, 14, new Color(0.75f, 0.85f, 0.95f), TextAnchor.UpperLeft);
            float hold = HoldProgress;
            if (hold > 0f)
            {
                UIKit.Bar(new Rect(x, y + ph - 4f, pw, 4f), hold, prof.Accent, new Color(1f, 1f, 1f, 0.1f));
                UIKit.Text(new Rect(x, y - 24f, pw, 20f), "Calling " + NameOf(r) + "...", 15, prof.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
            }
            float brace = BraceCapacity01;
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
            DrawMessages(x, y - (brace >= 0f ? 34f : 26f) - (Time.unscaledTime < _hintUntil ? 22f : 0f), pw);
        }

        private string StatusLine(CompanionRecord r, PlayerController pl)
        {
            switch ((CompanionState)r.State)
            {
                case CompanionState.Downed: return "Down: back in " + BondRules.Clock(r.RecoverIn) + " (or at a care station)";
                case CompanionState.Resting: return "Resting at home (summon it from the BONDS page)";
                case CompanionState.Fused: return "Fused: " + FusionIds.Title(FusionIds.FormFor(r.Archetype));
                case CompanionState.Waiting: return "Waiting where you left it (tap N to call)";
                case CompanionState.Assisting: return "Assisting";
            }
            if (_separationGrace > 0f) return "Separated beside your chassis";
            if (_riding || (pl.InVehicle && _body == null)) return "Riding along with you";
            if (_body == null) return "On its way";
            return "Following";
        }

        private string AssistLine(CompanionRecord r)
        {
            CompanionProfile prof = ProfileOf(r);
            float e = ExploreCooldownLeft(r.Uid);
            string explore = prof.ExploreTitle + (e > 0f ? " " + Mathf.CeilToInt(e) + "s" : " ready");
            if (!BondRules.CanCombatAssist(r.Rank)) return "U: " + explore + "   (care to unlock " + prof.CombatTitle + ")";
            float c = CombatCooldownLeft(r.Uid);
            string combat = prof.CombatTitle + (c > 0f ? " " + Mathf.CeilToInt(c) + "s" : " ready");
            return "U: " + explore + "   " + combat;
        }

        private void DrawMessages(float x, float bottom, float w)
        {
            float yy = bottom;
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                Message m = _messages[i];
                float a = Mathf.Clamp01(m.Left * 2f);
                Vector2 size = UIKit.Measure(m.Text, 16);
                float lw = Mathf.Min(Mathf.Max(w, size.x + 16f), 720f);
                float lh = size.x + 16f > 720f ? 42f : 24f;
                yy -= lh + 2f;
                UIKit.Rect(new Rect(x - 4f, yy, lw + 8f, lh), new Color(0f, 0f, 0f, 0.55f * a));
                UIKit.Text(new Rect(x + 4f, yy + 2f, lw - 8f, lh - 2f), m.Text, 16, m.Warn ? new Color(1f, 0.78f, 0.5f, a) : new Color(0.9f, 0.95f, 1f, a), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
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
