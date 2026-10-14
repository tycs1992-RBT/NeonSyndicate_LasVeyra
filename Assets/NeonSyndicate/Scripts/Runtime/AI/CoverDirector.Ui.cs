using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Cover screens: the HUD (the cover prompt on the spot, the in-cover panel with its button hints and a flash when
    /// the cover soaks a hit, the dash target marker with its hold ring, the lean-out hint, enemy grenade warnings, and
    /// the debug spot markers) and the F2 debug page "Cover" (show cover spots, switch enemy tactics, spawn a squad of
    /// each role, live squad numbers).
    /// </summary>
    public sealed partial class CoverDirector
    {
        private static readonly Color LowColor = new Color(0.35f, 1f, 0.55f);
        private static readonly Color HighColor = new Color(0.35f, 0.7f, 1f);
        private static readonly Color DashColor = new Color(1f, 0.82f, 0.3f);
        private static readonly Color WarnColor = new Color(1f, 0.25f, 0.15f);

        private readonly List<CoverSpot> _debugSpots = new List<CoverSpot>();
        private readonly List<NpcController> _debugSquad = new List<NpcController>();
        private float _debugScanAt;
        private string _debugStatus = "";

        private void RegisterUi()
        {
            HudOverlays.Register("cover", DrawHud);
            DebugPages.Register(new MenuPage { Title = "Cover", Order = 50, Draw = DrawDebug });
        }

        private static bool ToGui(Camera cam, Vector3 world, float W, float H, out Vector2 gui)
        {
            gui = Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return gui.x > -40f && gui.x < W + 40f && gui.y > -40f && gui.y < H + 40f;
        }

        // ================================================================== HUD

        private void DrawHud(float W, float H)
        {
            PlayerController p = Game.Player;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (p == null || cam == null || p.IsDead || p.InVehicle) return;
            Vector2 g;
            if (ShowSpots) DrawDebugSpots(cam, W, H);
            if (p.CoverPromptVisible)
            {
                CoverSpot s = p.CoverPrompt;
                bool low = s.Height == CoverHeight.Low;
                if (ToGui(cam, s.Position + Vector3.up * (low ? 0.75f : 1.15f), W, H, out g))
                {
                    Color c = low ? LowColor : HighColor;
                    float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 6f);
                    UIKit.Tex(new Rect(g.x - 14f, g.y - 14f, 28f, 28f), UIKit.Ring, new Color(c.r, c.g, c.b, pulse));
                    UIKit.Tex(new Rect(g.x - 5f, g.y - 5f, 10f, 10f), UIKit.Circle, c);
                    string label = Key(PadButton.South, "X") + "  COVER" + (low ? " (low)" : " (high)");
                    UIKit.Text(new Rect(g.x - 150f, g.y + 16f, 300f, 26f), label, 18, c, TextAnchor.UpperCenter, FontStyle.Bold);
                }
            }
            if (p.CoverDashVisible && ToGui(cam, p.CoverDashSpot.Position + Vector3.up * 0.9f, W, H, out g))
            {
                float hold = p.CoverDashHold;
                float size = 40f;
                UIKit.Tex(new Rect(g.x - size * 0.5f, g.y - size * 0.5f, size, size), UIKit.Ring, new Color(DashColor.r, DashColor.g, DashColor.b, 0.85f));
                float inner = 8f + (size - 12f) * hold;
                UIKit.Tex(new Rect(g.x - inner * 0.5f, g.y - inner * 0.5f, inner, inner), UIKit.Circle, new Color(DashColor.r, DashColor.g, DashColor.b, 0.55f + 0.45f * hold));
                UIKit.Text(new Rect(g.x - 150f, g.y + 22f, 300f, 24f), "hold " + Key(PadButton.South, "X") + "  DASH", 16, DashColor, TextAnchor.UpperCenter, FontStyle.Bold);
            }
            if (p.InCover) DrawCoverPanel(p, W, H);
            DrawGrenadeWarning(p, cam, W, H);
        }

        private void DrawCoverPanel(PlayerController p, float W, float H)
        {
            CoverSpot s = p.CoverNow;
            bool low = s.Height == CoverHeight.Low;
            Color c = low ? LowColor : HighColor;
            float bw = 520f, bh = 52f;
            Rect r = new Rect(W * 0.5f - bw * 0.5f, H - 92f, bw, bh);
            UIKit.Rect(r, new Color(0f, 0f, 0f, 0.5f));
            UIKit.Rect(new Rect(r.x, r.y, 3f, r.height), c);
            string state = (low ? "LOW COVER" : "HIGH COVER") + (p.CoverPopped ? "  AIMING" : (p.CoverBlindFiring ? "  BLIND FIRE" : ""));
            UIKit.Text(new Rect(r.x + 14f, r.y + 3f, bw - 28f, 24f), state, 18, c, TextAnchor.UpperLeft, FontStyle.Bold);
            CoverDamageFilter f = p.CoverFilter;
            if (f != null && Time.time - f.LastTime < 0.45f)
                UIKit.Text(new Rect(r.x + 14f, r.y + 3f, bw - 28f, 24f), "PROTECTED  -" + Mathf.RoundToInt((1f - f.LastScale) * 100f) + "%", 18, UIKit.Cyan, TextAnchor.UpperRight, FontStyle.Bold);
            else if (s.AtEdge)
                UIKit.Text(new Rect(r.x + 14f, r.y + 3f, bw - 28f, 24f), (s.OpenLeft ? "< EDGE" : "") + (s.OpenLeft && s.OpenRight ? "   " : "") + (s.OpenRight ? "EDGE >" : ""), 16, new Color(1f, 1f, 1f, 0.75f), TextAnchor.UpperRight, FontStyle.Bold);
            string aim = Key(PadButton.LT, "RMB");
            string fire = Key(PadButton.RT, "LMB");
            string cover = Key(PadButton.South, "X");
            string leave = Key(PadButton.East, "X");
            string hints = aim + " pop out    " + fire + " blind fire    hold " + cover + " dash    " + leave + " leave";
            hints += "    " + Key(PadButton.South, "Space") + (low ? " vault" : " climb");
            UIKit.Text(new Rect(r.x + 14f, r.y + 27f, bw - 28f, 22f), hints, 14, new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperLeft);
            if (p.CoverEdgeHint)
                UIKit.Text(new Rect(W * 0.5f - 300f, r.y - 34f, 600f, 28f), "Move to an edge of high cover to lean out", 18, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
        }

        private void DrawGrenadeWarning(PlayerController p, Camera cam, float W, float H)
        {
            float age = Time.time - _grenadeTime;
            if (age > 3.3f || age < 0f) return;
            if ((p.transform.position - _grenadeAt).sqrMagnitude > 14f * 14f) return;
            float blink = Mathf.Repeat(Time.unscaledTime * 5f, 1f) < 0.6f ? 1f : 0.35f;
            Color c = new Color(WarnColor.r, WarnColor.g, WarnColor.b, blink);
            Vector2 g;
            if (ToGui(cam, _grenadeAt + Vector3.up * 0.4f, W, H, out g) && g.x > 0f && g.x < W && g.y > 0f && g.y < H)
            {
                UIKit.Tex(new Rect(g.x - 22f, g.y - 22f, 44f, 44f), UIKit.Ring, c);
                UIKit.Text(new Rect(g.x - 120f, g.y + 24f, 240f, 28f), "GRENADE", 22, c, TextAnchor.UpperCenter, FontStyle.Bold);
            }
            else UIKit.Text(new Rect(W * 0.5f - 200f, H * 0.3f, 400f, 34f), "GRENADE!", 28, c, TextAnchor.UpperCenter, FontStyle.Bold);
        }

        // ================================================================== debug spots

        private void UpdateDebugSpots()
        {
            if (!ShowSpots)
            {
                if (_debugSpots.Count > 0) _debugSpots.Clear();
                return;
            }
            if (Time.unscaledTime < _debugScanAt) return;
            _debugScanAt = Time.unscaledTime + 0.5f;
            PlayerController p = Game.Player;
            if (p == null) return;
            CoverFinder.Scan(p.transform.position, 16f, 32, _debugSpots, 32);
        }

        private void DrawDebugSpots(Camera cam, float W, float H)
        {
            Vector2 g;
            for (int i = 0; i < _debugSpots.Count; i++)
            {
                CoverSpot s = _debugSpots[i];
                if (!ToGui(cam, s.Position + Vector3.up * 0.5f, W, H, out g)) continue;
                Color c = s.Height == CoverHeight.Low ? LowColor : HighColor;
                UIKit.Tex(new Rect(g.x - 7f, g.y - 7f, 14f, 14f), UIKit.Circle, c);
                if (s.OpenLeft || s.OpenRight)
                    UIKit.Text(new Rect(g.x - 40f, g.y + 6f, 80f, 18f), (s.OpenLeft ? "<" : "") + (s.Height == CoverHeight.Low ? "L" : "H") + (s.OpenRight ? ">" : ""), 14, UIKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            }
            foreach (KeyValuePair<NpcController, Vector3> c in _claims)
            {
                if (c.Key == null || !ToGui(cam, c.Value + Vector3.up * 1.9f, W, H, out g)) continue;
                UIKit.Text(new Rect(g.x - 90f, g.y - 10f, 180f, 20f), NpcController.RoleName(c.Key.TacticalRole) + " cover", 14, WarnColor, TextAnchor.UpperCenter, FontStyle.Bold);
            }
        }

        // ================================================================== F2 debug page

        private void DrawDebug(Rect r)
        {
            float x = r.x + 20f, y = r.y + 10f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 30f), "COVER AND SQUAD TACTICS", 24, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 36f;
            PlayerController p = Game.Player;
            if (p == null)
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Start a game first.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            string cover = p.InCover
                ? (p.CoverNow.Height == CoverHeight.Low ? "LOW" : "HIGH") + " cover for " + p.CoverTime.ToString("0.0") + " s" + (p.CoverNow.OpenLeft ? ", open left" : "") + (p.CoverNow.OpenRight ? ", open right" : "") + (p.CoverPopped ? ", popped out" : "") + (p.CoverBlindFiring ? ", blind fire" : "")
                : (p.CoverPromptVisible ? "cover prompt showing (" + (p.CoverPrompt.Height == CoverHeight.Low ? "low" : "high") + ")" : "not in cover");
            UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Player: " + cover + (p.CoverDashVisible ? "   dash target marked" : ""), 17, Color.white, TextAnchor.UpperLeft);
            y += 26f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 26f), "Enemies using tactics " + EngagedCount + "   flanking " + FlankerCount + "   turrets " + TurretCount + "   next grenade in " + GrenadeIn.ToString("0") + " s   cover rays " + CoverFinder.RaysCast
                + "   difficulty " + Settings.Difficulty, 17, Color.white, TextAnchor.UpperLeft);
            y += 36f;
            float bw = 300f, bh = 44f, gap = 16f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Show cover spots: " + (ShowSpots ? "ON" : "OFF"), ShowSpots, 17))
            {
                ShowSpots = !ShowSpots;
                _debugScanAt = 0f;
            }
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Enemy cover tactics: " + (TacticsEnabled ? "ON" : "OFF"), TacticsEnabled, 17)) TacticsEnabled = !TacticsEnabled;
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Clear debug squads", false, 17))
            {
                ClearDebugSquads();
                _debugStatus = "Debug squads removed.";
            }
            y += bh + 14f;
            UIKit.Text(new Rect(x, y, r.width - 40f, 24f), "Spawn a squad of four about 24 m ahead of the camera (they attack at once):", 17, new Color(1f, 1f, 1f, 0.8f), TextAnchor.UpperLeft);
            y += 28f;
            EnemyRole[] roles = { EnemyRole.Rifleman, EnemyRole.Rusher, EnemyRole.Heavy, EnemyRole.Marksman, EnemyRole.Engineer };
            for (int i = 0; i < roles.Length; i++)
            {
                float bx = x + (i % 3) * (bw + gap);
                float by = y + (i / 3) * (bh + 10f);
                if (UIKit.Button(new Rect(bx, by, bw, bh), "Spawn " + NpcController.RoleName(roles[i]) + " squad", false, 17)) _debugStatus = SpawnSquad(roles[i]);
            }
            if (UIKit.Button(new Rect(x + 2f * (bw + gap), y + bh + 10f, bw, bh), "Spawn a mixed squad", false, 17)) _debugStatus = SpawnSquad(EnemyRole.None);
            y += (bh + 10f) * 2f + 8f;
            if (!string.IsNullOrEmpty(_debugStatus))
            {
                UIKit.Text(new Rect(x, y, r.width - 40f, 24f), _debugStatus, 17, UIKit.Cyan, TextAnchor.UpperLeft);
                y += 28f;
            }
            int shown = 0;
            foreach (KeyValuePair<NpcController, float> e in _engaged)
            {
                NpcController n = e.Key;
                if (n == null || shown >= 10) continue;
                float d = MathUtil.FlatDistance(n.transform.position, p.transform.position);
                string line = NpcController.RoleName(n.TacticalRole) + " (" + n.Role + ", " + n.Weapons.Current.Name + ")   " + n.CurrentTactic + (n.InTacticalCover ? "   in cover" : "") + (n.LaserOn ? "   LASER" : "") + "   " + d.ToString("0") + " m";
                UIKit.Text(new Rect(x, y, r.width - 40f, 22f), line, 16, Color.white, TextAnchor.UpperLeft);
                y += 22f;
                shown++;
            }
        }

        /// <summary>Four enemies of a role (None: one of each, Rifleman twice) ahead of the camera, attacking the player.</summary>
        private string SpawnSquad(EnemyRole role)
        {
            PlayerController p = Game.Player;
            if (p == null || p.InVehicle) return "Get out of the vehicle first.";
            Vector3 fwd = Game.Cam != null ? Game.Cam.FlatForward : p.transform.forward;
            Vector3 center = p.transform.position + fwd * 24f;
            Vector3 right = MathUtil.RightOf(fwd);
            System.Random rnd = new System.Random(Random.Range(1, 1000000));
            EnemyRole[] mixed = { EnemyRole.Rifleman, EnemyRole.Rusher, EnemyRole.Heavy, EnemyRole.Marksman, EnemyRole.Engineer };
            int count = role == EnemyRole.None ? 5 : 4;
            int made = 0;
            for (int i = 0; i < count; i++)
            {
                EnemyRole r = role == EnemyRole.None ? mixed[i] : role;
                float back = r == EnemyRole.Marksman ? 10f : 0f;
                Vector3 at = center + right * ((i - (count - 1) * 0.5f) * 3.2f) + fwd * back;
                at = QuestWorld.OpenGround(at, p.transform.position);
                NpcController n = NpcController.Spawn(at, QuestWorld.YawToward(at, p.transform.position), NpcRole.Enemy, Appearance.Random(rnd), -1);
                if (n == null) continue;
                n.Persistent = true;
                n.DisplayName = NpcController.RoleName(r) + " (debug)";
                n.ForceRole(r, true);
                n.Attack(p);
                _debugSquad.Add(n);
                made++;
            }
            return made + " enemies spawned (" + (role == EnemyRole.None ? "mixed squad" : NpcController.RoleName(role) + "s") + ").";
        }

        private void ClearDebugSquads()
        {
            foreach (NpcController n in _debugSquad)
            {
                if (n == null) continue;
                n.TacticsOff();
                Destroy(n.gameObject);
            }
            _debugSquad.Clear();
        }
    }
}
