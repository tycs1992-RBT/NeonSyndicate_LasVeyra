using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the poise strip and stagger window the combat HUD draws under enemy bars (PoiseRulesH8S decides). A nearly
    /// broken poise strip pulses bright with a white end tick, so the player knows one more solid hit staggers; a
    /// staggered enemy shows a thin bar shrinking over its punish window. Shapes and motion carry the meaning, not colour
    /// alone. No allocation.
    /// </summary>
    public static class PoiseHudH8S
    {
        private static readonly Color Strip = new Color(1f, 0.85f, 0.45f, 0.8f);
        private static readonly Color Window = new Color(1f, 0.9f, 0.55f, 0.95f);

        /// <summary>A poise strip across r (its height is the strip's), pulsing when nearly broken.</summary>
        public static void Draw(Rect r, float poise, float maxPoise)
        {
            float fill = PoiseRulesH8S.Fill(poise, maxPoise);
            if (!PoiseRulesH8S.NearBreak(poise, maxPoise))
            {
                UIKit.Rect(new Rect(r.x, r.y, r.width * fill, r.height), Strip);
                return;
            }
            float a = PoiseRulesH8S.Pulse(Time.unscaledTime);
            UIKit.Rect(new Rect(r.x, r.y - 1f, r.width * fill, r.height + 2f), new Color(1f, 0.95f, 0.7f, a));
            UIKit.Rect(new Rect(r.x + r.width * fill, r.y - 2f, 2f, r.height + 4f), new Color(1f, 1f, 1f, a));
        }

        /// <summary>The stagger window bar centred on cx at y: full when the stagger starts, gone when it ends.</summary>
        public static void StaggerWindow(float cx, float y, float width, float staggerProgress)
        {
            float k = PoiseRulesH8S.WindowLeft(staggerProgress);
            if (k <= 0f) return;
            UIKit.Rect(new Rect(cx - width * 0.5f * k, y, width * k, 2f), Window);
        }
    }
}
