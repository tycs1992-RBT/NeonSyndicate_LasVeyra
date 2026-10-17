using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart bosses' HUD layer: the boss bar (name, phase or window line, health with the phase marks, the
    /// live mechanic underneath), the move winding up (its name, a filling bar and a "!!" over the boss), the open
    /// punish window (a shrinking bar and rings on the parts to strike), targets such as lures and furnace vents, the
    /// boundary countdown, the seal withdrawal and the exit after a victory, and the Choir mechanism panel. Shapes
    /// and words carry the information, never colour alone. The boss block and the mechanism panel sit in the shared
    /// encounter slot under the banners (HudLayout04E), because a boss fight always runs inside a quest whose objective
    /// and tracker own the bottom centre.
    /// </summary>
    public static class UnderBossHud
    {
        public const string OverlayKey = "uboss04e";

        private static readonly Color Tell = new Color(1f, 0.62f, 0.12f);
        private static readonly Color Open = new Color(1f, 0.86f, 0.35f);
        private static readonly Color Calm = new Color(0.55f, 0.9f, 1f);
        private static readonly Color Good = new Color(0.45f, 1f, 0.55f);
        private static readonly List<Vector3> _points = new List<Vector3>();
        private static readonly List<string> _labels = new List<string>();

        public static void ResetStatics()
        {
            _points.Clear();
            _labels.Clear();
        }

        public static void Draw(float w, float h)
        {
            UnderheartBosses sys = Game.UnderBosses;
            PlayerController p = Game.Player;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (sys == null || p == null || p.IsDead || cam == null) return;
            UnderBossFight f = sys.ActiveFight;
            if (f != null && f.Profile != null) DrawFight(f, cam, w, h);
            ChoirMechanism m = sys.ActiveMechanism;
            if (m != null) DrawMechanism(m, cam, w, h);
        }

        private static bool ToGui(Camera cam, Vector3 world, float w, float h, out Vector2 gui)
        {
            gui = Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return gui.x > -40f && gui.x < w + 40f && gui.y > -40f && gui.y < h + 40f;
        }

        // ================================================================== boss

        private static void DrawFight(UnderBossFight f, Camera cam, float w, float h)
        {
            Vector3 pp = Game.PlayerPosition;
            float d = MathUtil.FlatDistance(pp, f.ArenaCenter);
            if (d > f.ArenaRadius + 140f) return;
            float cx = w * 0.5f;
            Rect slot = HudLayout04E.EncounterSlot(w, h);
            if (f.State == UnderBossFight.FightState.Dormant)
            {
                if (d > f.ArenaRadius + 45f) return;
                UIKit.Text(new Rect(slot.x, slot.y, slot.width, 30f), "Cross the glowing boundary to face " + f.Profile.Title, 22, Calm, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Text(new Rect(slot.x, slot.y + 32f, slot.width, 48f), f.Profile.Counter, 17, new Color(0.85f, 0.92f, 1f), TextAnchor.UpperCenter, FontStyle.Normal, true, true);
                return;
            }
            if (f.State == UnderBossFight.FightState.Victory)
            {
                Vector3 exit = f.ExitPoint;
                Vector2 g;
                if (exit != Vector3.zero && ToGui(cam, exit + Vector3.up * 2.5f, w, h, out g))
                {
                    UIKit.Tex(new Rect(g.x - 18f, g.y - 18f, 36f, 36f), UIKit.Ring, Good);
                    UIKit.Text(new Rect(g.x - 80f, g.y + 18f, 160f, 22f), "EXIT", 17, Good, TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                return;
            }
            if (!f.Running) return;
            // The bar: name and phase above it, then one status line (the boundary or seal warning wins over the mechanic).
            Rect bar = HudLayout04E.BossBar(w, h);
            float bw = bar.width, bx = bar.x, top = slot.y;
            UIKit.Text(new Rect(bx, top, bw, 28f), f.Profile.Title, 24, new Color(1f, 0.9f, 0.8f), TextAnchor.LowerLeft, FontStyle.Bold);
            UIKit.Text(new Rect(bx, top, bw, 28f), f.PhaseLabel, 18, f.WindowOpen ? Open : new Color(1f, 0.75f, 0.45f), TextAnchor.LowerRight, FontStyle.Bold);
            UIKit.Rect(new Rect(bx - 3f, bar.y - 3f, bw + 6f, 20f), new Color(0f, 0f, 0f, 0.7f));
            UIKit.Rect(new Rect(bx, bar.y, bw * f.Health01, bar.height), f.WindowOpen ? new Color(0.95f, 0.55f, 0.12f) : new Color(0.7f, 0.08f, 0.1f));
            float[] marks = f.Profile.PhaseThresholds;
            if (marks != null) for (int i = 0; i < marks.Length; i++) UIKit.Rect(new Rect(bx + bw * marks[i] - 1f, bar.y, 2f, bar.height), new Color(1f, 1f, 1f, 0.5f));
            Rect line = new Rect(bx, bar.yMax + 4f, bw, 24f);
            float wp = f.WithdrawProgress;
            if (f.LeavingWarning)
                UIKit.Text(line, "LEAVING THE ARENA: RETURN WITHIN " + Mathf.CeilToInt(f.LeavingSecondsLeft) + " S OR THE FIGHT RESETS", 20, Tell, TextAnchor.UpperCenter, FontStyle.Bold);
            else if (wp > 0.02f)
            {
                UIKit.Text(line, "WITHDRAWING THROUGH THE SEAL (THE FIGHT RESETS)", 19, Calm, TextAnchor.UpperCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 150f, line.yMax, 300f, 4f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 150f, line.yMax, 300f * wp, 4f), Calm);
            }
            else
            {
                string mech = f.MechanicLine;
                if (!string.IsNullOrEmpty(mech)) UIKit.Text(line, mech, 17, new Color(0.95f, 0.9f, 0.75f), TextAnchor.UpperCenter, FontStyle.Bold);
            }
            // The move winding up, or the open window.
            float ty = top + 80f;
            if (f.Telling)
            {
                float k = f.TellProgress;
                UIKit.Text(new Rect(bx, ty, bw, 26f), UnderBossProfiles.MoveLabel(f.CurrentMove), 20, Tell, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 200f, ty + 28f, 400f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 200f, ty + 28f, 400f * k, 6f), Tell);
                Vector2 g;
                if (ToGui(cam, f.HeadPoint, w, h, out g))
                {
                    float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f));
                    UIKit.Text(new Rect(g.x - 60f, g.y - 30f, 120f, 40f), "!!", Mathf.RoundToInt(Mathf.Lerp(24f, 38f, k)), new Color(Tell.r, Tell.g, Tell.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }
            else if (f.WindowOpen)
            {
                float k = f.PunishWindow.Remaining01(Time.time);
                UIKit.Text(new Rect(bx, ty, bw, 26f), f.PunishWindow.Label, 20, Open, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 200f, ty + 28f, 400f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 200f * k, ty + 28f, 400f * k, 6f), Open);
            }
            // Targets: rings on what to strike or shut down.
            f.Targets(_points, _labels);
            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 g;
                if (!ToGui(cam, _points[i], w, h, out g)) continue;
                float s = 30f + 6f * Mathf.Sin(Time.unscaledTime * 7f + i);
                UIKit.Tex(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), UIKit.Ring, new Color(Open.r, Open.g, Open.b, 0.95f));
                string label = i < _labels.Count ? _labels[i] : "";
                if (!string.IsNullOrEmpty(label)) UIKit.Text(new Rect(g.x - 90f, g.y + s * 0.5f, 180f, 20f), label, 15, Open, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        // ================================================================== Choir mechanism

        private static void DrawMechanism(ChoirMechanism m, Camera cam, float w, float h)
        {
            if (MathUtil.FlatDistance(Game.PlayerPosition, m.Anchor) > 120f) return;
            // The shared encounter slot under the banners (HudLayout04E): the right-hand loot card never covers it.
            Rect panel = HudLayout04E.MechanismPanel(w, h);
            float pw = panel.width, px = panel.x, py = panel.y;
            UIKit.Rect(panel, UIKit.Panel);
            UIKit.Rect(new Rect(px, py, 4f, panel.height), Tell);
            UIKit.Text(new Rect(px + 14f, py + 6f, pw - 24f, 26f), ChoirRules04E.Title(m.Id).ToUpper(), 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(px + 14f, py + 34f, pw - 24f, 44f), m.Handle != null ? m.Handle.Objective : "", 16, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float prog = m.Progress01;
            UIKit.Rect(new Rect(px + 14f, py + 82f, pw - 28f, 8f), new Color(0f, 0f, 0f, 0.6f));
            UIKit.Rect(new Rect(px + 14f, py + 82f, (pw - 28f) * prog, 8f), Good);
            string extra = m.ExtraLine;
            if (!string.IsNullOrEmpty(extra)) UIKit.Text(new Rect(px + 14f, py + 94f, pw - 24f, 22f), extra, 15, new Color(1f, 0.85f, 0.6f), TextAnchor.UpperLeft);
            m.Targets(_points, _labels);
            for (int i = 0; i < _points.Count; i++)
            {
                Vector2 g;
                if (!ToGui(cam, _points[i], w, h, out g)) continue;
                UIKit.Tex(new Rect(g.x - 14f, g.y - 14f, 28f, 28f), UIKit.Ring, new Color(Calm.r, Calm.g, Calm.b, 0.9f));
                string label = i < _labels.Count ? _labels[i] : "";
                if (!string.IsNullOrEmpty(label)) UIKit.Text(new Rect(g.x - 90f, g.y + 14f, 180f, 20f), label, 15, Calm, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }
    }
}
