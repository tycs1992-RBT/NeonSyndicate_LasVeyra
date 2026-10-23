using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the boss phase banner (a HudOverlays layer registered with the creature HUD). When a boss opens a phase a
    /// short band appears in the upper middle of the screen, clear of the world event callout above and the crosshair
    /// below: the phase ("PHASE 2 / 3") between two gold rules, the boss's name under it, and an optional hint line. It
    /// fades in and out on unscaled time (BossPhaseRulesH8S), so slow motion or a pause cannot stretch it. Text and
    /// shape carry the information, never colour alone. No allocation while drawing (the strings are set once per phase).
    /// </summary>
    public static class CombatBannerH8S
    {
        private const string OverlayKey = "combat_banner_h8s";
        private static readonly Color Gold = new Color(1f, 0.84f, 0.5f, 1f);

        private static string _title = "";
        private static string _name = "";
        private static string _hint = "";
        private static float _shownAt = -99f;
        private static float _life = BossPhaseRulesH8S.BannerSeconds;

        /// <summary>Chained from CombatDirector.ResetStatics.</summary>
        public static void ResetStatics()
        {
            _title = "";
            _name = "";
            _hint = "";
            _shownAt = -99f;
            _life = BossPhaseRulesH8S.BannerSeconds;
        }

        /// <summary>Adds the banner to the gameplay HUD (CreatureHud.Register calls it at boot).</summary>
        public static void Register()
        {
            HudOverlays.Register(OverlayKey, Draw);
        }

        /// <summary>Shows the banner for a boss's phase (index 0 based); hint may be null.</summary>
        public static void ShowPhase(string bossName, int phaseIndex, int phaseCount, string hint)
        {
            _title = BossPhaseRulesH8S.PhaseTitle(phaseIndex, phaseCount);
            _name = bossName ?? "";
            _hint = hint ?? "";
            _shownAt = Time.unscaledTime;
            _life = BossPhaseRulesH8S.BannerSeconds;
        }

        /// <summary>True while the banner is on screen.</summary>
        public static bool Showing { get { return BossPhaseRulesH8S.BannerAlpha(Time.unscaledTime - _shownAt, _life) > 0f; } }

        /// <summary>The title shown last (the offline checks and the debug page read it).</summary>
        public static string Title { get { return _title; } }

        private static void Draw(float w, float h)
        {
            if (_title.Length == 0) return;
            float a = BossPhaseRulesH8S.BannerAlpha(Time.unscaledTime - _shownAt, _life);
            if (a <= 0f) return;
            float y = h * 0.27f;
            float half = Mathf.Min(380f, w * 0.4f);
            float tall = _hint.Length > 0 ? 100f : 78f;
            UIKit.Rect(new Rect(w * 0.5f - half, y - 10f, half * 2f, tall), new Color(0f, 0f, 0f, 0.58f * a));
            Color rule = new Color(Gold.r, Gold.g, Gold.b, 0.9f * a);
            UIKit.Rect(new Rect(w * 0.5f - half * 0.8f, y - 10f, half * 1.6f, 2f), rule);
            UIKit.Rect(new Rect(w * 0.5f - half * 0.8f, y + tall - 12f, half * 1.6f, 2f), rule);
            UIKit.Text(new Rect(w * 0.5f - half, y - 4f, half * 2f, 40f), _title, 34, new Color(Gold.r, Gold.g, Gold.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Text(new Rect(w * 0.5f - half, y + 34f, half * 2f, 24f), _name, 18, new Color(1f, 0.93f, 0.86f, 0.92f * a), TextAnchor.MiddleCenter, FontStyle.Bold);
            if (_hint.Length > 0)
                UIKit.Text(new Rect(w * 0.5f - half, y + 58f, half * 2f, 24f), _hint, 16, new Color(1f, 0.75f, 0.45f, a), TextAnchor.MiddleCenter);
        }
    }
}
