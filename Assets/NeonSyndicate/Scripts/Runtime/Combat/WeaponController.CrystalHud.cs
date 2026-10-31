using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal focus crosshair layer (HudOverlays "crystal_weapons"): why a shot failed (blocked, overheated, too many
    /// shots), the charge of the fan, concentrated orb or precision pulse, the beam's concentration ramp, Mirage Choir
    /// locks (a real lock: gold diamond brackets; a lock being acquired: a thin filling ring), the Gyre Crescent bank
    /// preview while aiming, and the planted Mirrorwake focus. Shapes and words carry the meaning, never colour alone.
    /// </summary>
    public sealed partial class WeaponController
    {
        /// <summary>HUD overlay entry point (registered by CrystalShots.Ensure).</summary>
        public static void CrystalDrawHud(float w, float h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.Weapons == null || p.IsDead) return;
            p.Weapons.DrawCrystalHud(w, h, p);
        }

        private void DrawCrystalHud(float w, float h, PlayerController p)
        {
            float now = Time.unscaledTime;
            bool flash = now < _crFlashUntil && !string.IsNullOrEmpty(_crFlash);
            if (_crBehavior == CrystalBehavior.None && !flash) return;
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (_crBehavior == CrystalBehavior.GyreCrescent) DrawBankPreview(cam);
            if (_crBehavior == CrystalBehavior.MirageChoir) DrawLocks(cam, w, h);
            if (_crBehavior == CrystalBehavior.Mirrorwake) DrawFocus(cam, w, h);
            if (_crCharging || _crArmed)
            {
                float k = Mathf.Clamp01(_crCharge);
                bool ready = _crChargeReady;
                Color col = ready ? UIKit.Gold : new Color(0.7f, 0.9f, 1f, 0.5f + 0.5f * k);
                float s = Mathf.Lerp(46f, 30f, k);
                UIKit.Tex(new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s), UIKit.Ring, col);
                UIKit.Bar(new Rect(c.x - 26f, c.y + 22f, 52f, 4f), k, col, new Color(0f, 0f, 0f, 0.5f));
                string label = ready ? (_crBehavior == CrystalBehavior.Shardwake ? "FAN READY" : (_crBehavior == CrystalBehavior.Sunderheart ? "CONCENTRATED" : "PULSE READY")) : "CHARGING";
                UIKit.Text(new Rect(c.x - 120f, c.y + 28f, 240f, 20f), label, 14, col, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            if (_crBeamOn && _crBeam != null)
            {
                float ramp = _crBeam.RampProgress;
                if (ramp > 0.01f)
                {
                    UIKit.Bar(new Rect(c.x - 26f, c.y + 22f, 52f, 3f), ramp, new Color(1f, 0.85f, 0.45f), new Color(0f, 0f, 0f, 0.5f));
                    UIKit.Text(new Rect(c.x - 120f, c.y + 27f, 240f, 18f), ramp >= 0.99f ? "FOCUSED x1.25" : "FOCUSING", 13, new Color(1f, 0.85f, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                if (_crBeamBlocked && cam != null)
                {
                    Vector2 g;
                    if (CrystalToGui(cam, _crBeamEnd, out g)) UIKit.Tex(new Rect(g.x - 7f, g.y - 7f, 14f, 14f), UIKit.Ring, new Color(1f, 1f, 1f, 0.55f));
                }
            }
            if (_crBehavior == CrystalBehavior.GyreCrescent && (_crDiscWasOut || CrystalNow < _crDiscReadyAt))
            {
                string t = _crDiscWasOut ? "DISC OUT  (fire: recall)" : "CATCHING";
                UIKit.Text(new Rect(c.x - 160f, c.y + 26f, 320f, 20f), GInput.Prompt(t), 14, new Color(0.75f, 0.95f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            // H8S: the high-heat tick, shots or beam seconds left, the vent countdown and the band with the vent button.
            DrawHeatReadoutH8S(w, h);
            if (flash)
            {
                float a = Mathf.Clamp01((_crFlashUntil - now) / 0.25f);
                Color fc = new Color(_crFlashColor.r, _crFlashColor.g, _crFlashColor.b, a);
                // A crossed ring for refusals, so the meaning does not depend on the colour.
                UIKit.Tex(new Rect(c.x - 15f, c.y - 15f, 30f, 30f), UIKit.Ring, fc);
                UIKit.Rect(new Rect(c.x - 12f, c.y - 1f, 24f, 2f), fc);
                UIKit.Text(new Rect(c.x - 200f, c.y - 52f, 400f, 24f), _crFlash.ToUpper(), 18, fc, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        /// <summary>World to UIKit space; false when behind the camera.</summary>
        private static bool CrystalToGui(Camera cam, Vector3 world, out Vector2 g)
        {
            g = Vector2.zero;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float k = UIKit.Scale;
            g = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
            return true;
        }

        private void DrawBankPreview(Camera cam)
        {
            if (_crPreviewCount < 2 || cam == null) return;
            Color dot = new Color(0.7f, 0.95f, 1f, 0.75f);
            for (int s = 0; s + 1 < _crPreviewCount; s++)
            {
                Vector3 a = _crPreview[s], b = _crPreview[s + 1];
                float len = (b - a).magnitude;
                int steps = Mathf.Clamp(Mathf.CeilToInt(len / 1.2f), 1, 30);
                for (int i = 1; i <= steps; i++)
                {
                    Vector2 g;
                    if (!CrystalToGui(cam, Vector3.Lerp(a, b, i / (float)steps), out g)) continue;
                    UIKit.Tex(new Rect(g.x - 2.5f, g.y - 2.5f, 5f, 5f), UIKit.Circle, dot);
                }
                if (s + 1 < _crPreviewCount - 1)
                {
                    Vector2 bg;
                    if (CrystalToGui(cam, b, out bg)) UIKit.Tex(new Rect(bg.x - 9f, bg.y - 9f, 18f, 18f), UIKit.Ring, UIKit.Cyan);
                }
            }
        }

        private void DrawLocks(Camera cam, float w, float h)
        {
            if (cam == null) return;
            for (int i = 0; i < _crLockBodies.Length; i++)
            {
                CharacterBase b = _crLockBodies[i];
                if (b == null || b.IsDead) continue;
                Vector2 g;
                if (!CrystalToGui(cam, b.ChestPosition, out g)) continue;
                // A real lock: four gold corner brackets and the word LOCK.
                float s = 22f;
                Color gold = UIKit.Gold;
                UIKit.Rect(new Rect(g.x - s, g.y - s, 10f, 3f), gold);
                UIKit.Rect(new Rect(g.x - s, g.y - s, 3f, 10f), gold);
                UIKit.Rect(new Rect(g.x + s - 10f, g.y - s, 10f, 3f), gold);
                UIKit.Rect(new Rect(g.x + s - 3f, g.y - s, 3f, 10f), gold);
                UIKit.Rect(new Rect(g.x - s, g.y + s - 3f, 10f, 3f), gold);
                UIKit.Rect(new Rect(g.x - s, g.y + s - 10f, 3f, 10f), gold);
                UIKit.Rect(new Rect(g.x + s - 10f, g.y + s - 3f, 10f, 3f), gold);
                UIKit.Rect(new Rect(g.x + s - 3f, g.y + s - 10f, 3f, 10f), gold);
                UIKit.Text(new Rect(g.x - 40f, g.y - s - 18f, 80f, 16f), "LOCK " + (i + 1), 12, gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            CharacterBase cand = _crLockCandidate;
            if (cand != null && !cand.IsDead && _crLocks != null)
            {
                Vector2 g;
                if (CrystalToGui(cam, cand.ChestPosition, out g))
                {
                    float k = _crLocks.DwellProgress;
                    float s = Mathf.Lerp(48f, 26f, k);
                    UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, new Color(1f, 1f, 1f, 0.35f + 0.5f * k));
                }
            }
            int n = CrystalLockCount;
            if (n > 0)
            {
                // H8S: right of the crosshair, clear of the heat bar it used to sit on (h * 0.5 + 46), with a cached label.
                Vector2 c = new Vector2(w * 0.5f, h * 0.5f);
                UIKit.Text(new Rect(c.x + 30f, c.y - 9f, 170f, 18f), LockCountTextH8S(n), 13, UIKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            }
        }

        private void DrawFocus(Camera cam, float w, float h)
        {
            if (!_crFocusActive || cam == null) return;
            Vector2 g;
            if (!CrystalToGui(cam, _crFocusPos, out g)) return;
            Color col = _crAimOnFocus ? UIKit.Gold : new Color(0.7f, 0.85f, 1f, 0.85f);
            float s = _crAimOnFocus ? 30f : 22f;
            UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, col);
            UIKit.Tex(new Rect(g.x - 3f, g.y - 3f, 6f, 6f), UIKit.Circle, col);
            string label = FocusLabelH8S(_crAimOnFocus, CrystalFocusLeft);
            UIKit.Text(new Rect(g.x - 80f, g.y + s * 0.5f + 2f, 160f, 16f), label, 12, col, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }
}
