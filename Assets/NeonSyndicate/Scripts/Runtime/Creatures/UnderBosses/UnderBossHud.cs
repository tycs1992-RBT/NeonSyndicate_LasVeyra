using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Underheart bosses' HUD layer: the boss bar (name, phase or window line, health with the phase marks, the
    /// live mechanic underneath), the move winding up (its name, a filling bar and a "!!" over the boss), the open
    /// punish window (a shrinking bar and rings on the parts to strike), targets such as lures and furnace vents, the
    /// boundary countdown, the seal withdrawal and the exit after a victory, and the Choir mechanism panel. Shapes
    /// and words carry the information, never colour alone.
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
            if (f.State == UnderBossFight.FightState.Dormant)
            {
                if (d > f.ArenaRadius + 45f) return;
                UIKit.Text(new Rect(0f, h * 0.7f, w, 30f), "Cross the glowing boundary to face " + f.Profile.Title, 22, Calm, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Text(new Rect(0f, h * 0.7f + 30f, w, 24f), f.Profile.Counter, 17, new Color(0.85f, 0.92f, 1f), TextAnchor.MiddleCenter);
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
            // The bar.
            float bw = Mathf.Min(900f, w * 0.55f);
            float bx = cx - bw * 0.5f, by = h - 150f;
            UIKit.Text(new Rect(bx, by - 30f, bw, 28f), f.Profile.Title, 24, new Color(1f, 0.9f, 0.8f), TextAnchor.LowerLeft, FontStyle.Bold);
            UIKit.Text(new Rect(bx, by - 30f, bw, 28f), f.PhaseLabel, 18, f.WindowOpen ? Open : new Color(1f, 0.75f, 0.45f), TextAnchor.LowerRight, FontStyle.Bold);
            UIKit.Rect(new Rect(bx - 3f, by - 3f, bw + 6f, 20f), new Color(0f, 0f, 0f, 0.7f));
            UIKit.Rect(new Rect(bx, by, bw * f.Health01, 14f), f.WindowOpen ? new Color(0.95f, 0.55f, 0.12f) : new Color(0.7f, 0.08f, 0.1f));
            float[] marks = f.Profile.PhaseThresholds;
            if (marks != null) for (int i = 0; i < marks.Length; i++) UIKit.Rect(new Rect(bx + bw * marks[i] - 1f, by, 2f, 14f), new Color(1f, 1f, 1f, 0.5f));
            string mech = f.MechanicLine;
            if (!string.IsNullOrEmpty(mech)) UIKit.Text(new Rect(bx, by + 18f, bw, 24f), mech, 17, new Color(0.95f, 0.9f, 0.75f), TextAnchor.UpperCenter, FontStyle.Bold);
            // The move winding up, or the open window.
            float ty = h - 214f;
            if (f.Telling)
            {
                float k = f.TellProgress;
                UIKit.Text(new Rect(cx - 520f, ty - 30f, 1040f, 28f), UnderBossProfiles.MoveLabel(f.CurrentMove), 20, Tell, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 200f, ty, 400f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 200f, ty, 400f * k, 6f), Tell);
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
                UIKit.Text(new Rect(cx - 520f, ty - 30f, 1040f, 28f), f.PunishWindow.Label, 20, Open, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 200f, ty, 400f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 200f * k, ty, 400f * k, 6f), Open);
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
            // Boundary and seal.
            if (f.LeavingWarning)
                UIKit.Text(new Rect(0f, h * 0.32f, w, 34f), "LEAVING THE ARENA: RETURN WITHIN " + Mathf.CeilToInt(f.LeavingSecondsLeft) + " S OR THE FIGHT RESETS", 24, Tell, TextAnchor.MiddleCenter, FontStyle.Bold);
            float wp = f.WithdrawProgress;
            if (wp > 0.02f)
            {
                UIKit.Text(new Rect(0f, h * 0.36f, w, 30f), "WITHDRAWING THROUGH THE SEAL (THE FIGHT RESETS)", 20, Calm, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Rect(new Rect(cx - 150f, h * 0.36f + 32f, 300f, 6f), new Color(0f, 0f, 0f, 0.6f));
                UIKit.Rect(new Rect(cx - 150f, h * 0.36f + 32f, 300f * wp, 6f), Calm);
            }
        }

        // ================================================================== Choir mechanism

        private static void DrawMechanism(ChoirMechanism m, Camera cam, float w, float h)
        {
            if (MathUtil.FlatDistance(Game.PlayerPosition, m.Anchor) > 120f) return;
            float pw = 440f, px = w - pw - 36f, py = h * 0.3f;
            UIKit.Rect(new Rect(px, py, pw, 118f), UIKit.Panel);
            UIKit.Rect(new Rect(px, py, 4f, 118f), Tell);
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
