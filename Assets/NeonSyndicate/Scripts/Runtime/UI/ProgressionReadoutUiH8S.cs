using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S progression readability, drawing side: the colour and arrow of an upgrade call, font sizes that follow the
    /// ACCESS page's guidance text size (never past what a row holds), high-contrast colours when that switch is on, and
    /// the thin city tier progress bar shared by the HUD gear score line and the GEAR tab. Allocation free: no strings
    /// are built here. The rules behind every call live in UpgradeVerdictRulesH8S.
    /// </summary>
    public static class ProgressionReadoutUiH8S
    {
        /// <summary>Grey for "about the same" and "worn now".</summary>
        public static readonly Color Neutral = new Color(0.86f, 0.86f, 0.9f);
        /// <summary>The small grey of the detail lines under a call.</summary>
        public static readonly Color Detail = new Color(0.72f, 0.74f, 0.8f);

        /// <summary>A font size scaled by the guidance text size setting, capped at maxSize.</summary>
        public static int Font(int baseSize, int maxSize)
        {
            return AccessibilityRulesH8S.Font(baseSize, Settings.GuideTextScaleH8S, maxSize);
        }

        /// <summary>A colour pushed to full strength when high-contrast markers are on (unchanged otherwise).</summary>
        public static Color Contrast(Color c)
        {
            return AccessibilityRulesH8S.MarkerColor(c, Settings.HighContrastMarkersH8S);
        }

        /// <summary>Green for an upgrade, red for weaker, cyan for a new slot, grey otherwise (high contrast honoured).</summary>
        public static Color VerdictColor(UpgradeVerdictH8S v)
        {
            switch (v)
            {
                case UpgradeVerdictH8S.Upgrade: return Contrast(ProgressionUI.Good);
                case UpgradeVerdictH8S.Weaker: return Contrast(ProgressionUI.Bad);
                case UpgradeVerdictH8S.EmptySlot: return Contrast(UIKit.Cyan);
                default: return Neutral;
            }
        }

        /// <summary>An up arrow for an upgrade or a new slot, a down arrow for weaker, nothing otherwise.</summary>
        public static void DrawVerdictIcon(Rect r, UpgradeVerdictH8S v, Color c)
        {
            if (v == UpgradeVerdictH8S.Upgrade || v == UpgradeVerdictH8S.EmptySlot)
            {
                if (Settings.HighContrastMarkersH8S) UIKit.Tex(Grow(r, 2f), UIKit.Arrow, AccessibilityRulesH8S.OutlineColor(c.a));
                UIKit.Tex(r, UIKit.Arrow, c);
            }
            else if (v == UpgradeVerdictH8S.Weaker)
            {
                Color old = GUI.color;
                if (Settings.HighContrastMarkersH8S)
                {
                    GUI.color = AccessibilityRulesH8S.OutlineColor(c.a);
                    GUI.DrawTextureWithTexCoords(Grow(r, 2f), UIKit.Arrow, new Rect(0f, 1f, 1f, -1f));
                }
                GUI.color = c;
                GUI.DrawTextureWithTexCoords(r, UIKit.Arrow, new Rect(0f, 1f, 1f, -1f));
                GUI.color = old;
            }
        }

        private static Rect Grow(Rect r, float by)
        {
            return new Rect(r.x - by, r.y - by, r.width + by * 2f, r.height + by * 2f);
        }

        /// <summary>
        /// The city tier progress bar: fills from the current tier's threshold to the next one in the next tier's colour
        /// (full at the top tier). Draws inside r only.
        /// </summary>
        public static void DrawTierBar(Rect r, int score)
        {
            TierProgressH8S p = UpgradeVerdictRulesH8S.TierProgress(score);
            Color fg = Contrast(GadgetSystem.TierColor(p.Top ? p.Tier : p.Tier + 1));
            UIKit.Bar(r, p.Fraction, fg, new Color(0f, 0f, 0f, Settings.HighContrastMarkersH8S ? 0.8f : 0.5f));
        }
    }

    /// <summary>
    /// The H8S progression readability statics hub: every static the readouts keep (cached texts, the pending level-up
    /// summary) is cleared here. Chained from ProgressionHooks.ResetStatics, which Game.ResetStatics already calls.
    /// </summary>
    public static class ProgressionReadoutStaticsH8S
    {
        public static void ResetStatics()
        {
            LevelUpReadoutH8S.ResetStatics();
            GearMenu.ResetReadoutH8S();
            SkillsPage.ResetPreviewH8S();
        }
    }
}
