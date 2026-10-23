using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S: the F2 debug page "Readability H8S" (Order 55, registered with the creature HUD). It shows the live state of
    /// the combat readability work so it can be checked in play: the impact cadence (gap, blows planned and blows that
    /// waited a beat), kill confirmations and feedback freezes, the focus heat band in hand, and the last phase banner;
    /// and it previews the phase banner and plays each new cue on demand. Debug only: like the other F2 pages it builds
    /// its lines while it is open.
    /// </summary>
    public static class CombatReadabilityDebugH8S
    {
        /// <summary>The page's tab title.</summary>
        public const string Title = "Readability H8S";

        /// <summary>Adds the page to the F2 debug menu.</summary>
        public static void Register()
        {
            DebugPages.Register(new MenuPage { Title = Title, Order = 55, Draw = Draw });
        }

        private static void Draw(Rect r)
        {
            float x = r.x, y = r.y;
            float bw = 250f, bh = 40f, gap = 12f;
            UIKit.Text(new Rect(x, y, r.width, 28f), "COMBAT READABILITY (H8S)", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            UIKit.Text(new Rect(x, y, r.width, 24f), "Blow spacing: gap " + CombatCadenceH8S.Gap.ToString("0.00") + " s (" + Settings.DifficultyName(Settings.Difficulty)
                + ")   planned " + CombatCadenceH8S.Plans + "   waited a beat " + CombatCadenceH8S.Waits + "   in the book " + CombatCadenceH8S.Booked, 18, Color.white, TextAnchor.UpperLeft);
            y += 28f;
            bool reduced = Game.CrystalCombat != null && Game.CrystalCombat.ReducedEffects;
            UIKit.Text(new Rect(x, y, r.width, 24f), "Kill confirmations " + CombatFeedbackH8S.Kills + "   feedback freezes " + CombatFeedbackH8S.Stops
                + "   reduced effects " + (reduced ? "on" : "off") + "   damage numbers " + (Settings.DamageNumbers ? "on" : "off"), 18, Color.white, TextAnchor.UpperLeft);
            y += 28f;
            PlayerController p = Game.Player;
            WeaponController wc = p != null ? p.Weapons : null;
            string focus = wc != null && wc.CrystalInHand != CrystalBehavior.None
                ? "Focus in hand: " + wc.CrystalInHand + "   heat " + Mathf.RoundToInt(wc.Heat * 100f) + "%   band " + wc.CrystalHeatBandH8S
                : "Focus in hand: none";
            UIKit.Text(new Rect(x, y, r.width, 24f), focus, 18, Color.white, TextAnchor.UpperLeft);
            y += 28f;
            UIKit.Text(new Rect(x, y, r.width, 24f), "Last phase banner: " + (CombatBannerH8S.Title.Length > 0 ? CombatBannerH8S.Title : "none yet"), 18, Color.white, TextAnchor.UpperLeft);
            y += 36f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Preview phase banner", false, 17))
                CombatBannerH8S.ShowPhase("Preview Boss", 1, 3, "A banner preview from the debug page");
            if (UIKit.Button(new Rect(x + (bw + gap), y, bw, bh), "Phase bell", false, 17)) Cue(CombatSounds.Clip.PhaseShift);
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Kill cue", false, 17)) Cue(CombatSounds.Clip.Kill);
            if (UIKit.Button(new Rect(x + (bw + gap) * 3f, y, bw, bh), "Heavy kill cue", false, 17)) Cue(CombatSounds.Clip.KillHeavy);
            y += bh + gap;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Heat high cue", false, 17)) Cue(CombatSounds.Clip.HeatWarn);
            if (UIKit.Button(new Rect(x + (bw + gap), y, bw, bh), "Next shot overheats cue", false, 17)) Cue(CombatSounds.Clip.HeatCritical);
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Vent ready cue", false, 17)) Cue(CombatSounds.Clip.VentReady);
            y += bh + 18f;
            UIKit.Text(new Rect(x, y, r.width, 96f),
                "Tells: red \"!\" = guard or parry it, orange \"!!\" = cannot be blocked (dodge or leave the marked ground). Ground rings end exactly where the blow hits; "
                + "sweeps get a ring round the body, pounces and charges a lane that stops following you at 70 percent of the wind-up. Blows on you land at least the gap apart.",
                16, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }

        private static void Cue(CombatSounds.Clip c)
        {
            Vector3 at = Game.Player != null ? Game.Player.ChestPosition : Vector3.zero;
            CombatSounds.Play(c, at, 0.7f, 1f, 0f, 60f);
        }
    }
}
