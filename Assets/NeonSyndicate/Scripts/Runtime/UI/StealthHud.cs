using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Stealth HUD (a HudOverlays layer, registered by the player): an awareness eye over every person or creature who
    /// is starting to notice Malik (white, filling yellow to red as it builds, a red "!" flash when the alarm goes), the
    /// takedown prompt on the person a press of the use button would take down, the Third Eye's sight pulse (red marks
    /// on enemies and cyan marks on interactables through walls, lit as the pulse ring reaches them, with distances),
    /// and a small word under the crosshair while crouching: HIDDEN, WATCHED or SEEN. Rules in StealthRules.
    /// </summary>
    public static class StealthHud
    {
        private const float IndicatorRange = 45f;

        public static void Register()
        {
            HudOverlays.Register("stealth_h8s", Draw);
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

        private static Color AwarenessColor(float a)
        {
            if (a < StealthRules.Suspicious) return Color.Lerp(new Color(0.9f, 0.95f, 1f), new Color(1f, 0.85f, 0.2f), a / StealthRules.Suspicious);
            return Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.2f, 0.15f), (a - StealthRules.Suspicious) / (1f - StealthRules.Suspicious));
        }

        private static void Draw(float W, float H)
        {
            PlayerController p = Game.Player;
            Camera cam = Game.Cam != null ? Game.Cam.Cam : null;
            if (p == null || cam == null || p.IsDead) return;
            Vector3 pp = p.transform.position;
            bool anyAlarmed = false;
            float highest = 0f;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead || n.InVehicle) continue;
                float d = Vector3.Distance(n.transform.position, pp);
                if (d > IndicatorRange) continue;
                bool hostileRole = n.Role == NpcRole.Gang || n.Role == NpcRole.Enemy || n.Role == NpcRole.Police || n.Role == NpcRole.Swat;
                if (!hostileRole) continue;
                if (n.AlarmedH8S) anyAlarmed = true;
                highest = Mathf.Max(highest, n.AwarenessH8S);
                Indicator(cam, n.HeadPosition + Vector3.up * 0.35f, n.AwarenessH8S, n.AlertFlashH8S, d, W, H);
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || !c.Ready || c.IsDead) continue;
                float d = Vector3.Distance(c.transform.position, pp);
                if (d > IndicatorRange) continue;
                if (c.IsEngaged) anyAlarmed = true;
                highest = Mathf.Max(highest, c.AwarenessH8S);
                if (!c.IsEngaged) Indicator(cam, c.HeadPosition + Vector3.up * 0.4f, c.AwarenessH8S, c.AlertFlashH8S, d, W, H);
            }
            if (p.TakedownTarget != null) Prompt(cam, p.TakedownTarget, p.TakedownAvailable, W, H);
            if (p.SightPulseActive) Pulse(cam, p, W, H);
            if (p.Crouched && !p.InVehicle && (Game.Hud == null || !Game.Hud.Hidden)) StanceWord(W, H, anyAlarmed, highest);
        }

        /// <summary>The awareness eye: an outlined bar filling with the awareness, a "!" while the alarm flashes.</summary>
        private static void Indicator(Camera cam, Vector3 world, float awareness, float flash, float d, float W, float H)
        {
            if (flash <= 0f && (awareness < 0.04f || awareness >= StealthRules.Alert)) return;
            Vector2 g;
            if (!ToGui(cam, world, W, H, out g)) return;
            float fade = Mathf.Clamp01((IndicatorRange - d) / 10f);
            if (flash > 0f)
            {
                float k = Mathf.Clamp01(flash / 1.2f);
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 18f);
                UIKit.Text(new Rect(g.x - 30f, g.y - 34f, 60f, 36f), "!", 30, new Color(1f, 0.2f, 0.15f, fade * pulse * Mathf.Min(1f, k * 3f)), TextAnchor.MiddleCenter, FontStyle.Bold);
                return;
            }
            float w = 34f;
            Color c = AwarenessColor(awareness);
            UIKit.Rect(new Rect(g.x - w * 0.5f - 1f, g.y - 9f, w + 2f, 8f), new Color(0f, 0f, 0f, 0.6f * fade));
            UIKit.Rect(new Rect(g.x - w * 0.5f, g.y - 8f, w * Mathf.Clamp01(awareness), 6f), new Color(c.r, c.g, c.b, 0.95f * fade));
            // The eye: a ring over the bar that opens as the awareness climbs.
            float eye = 10f + 6f * awareness;
            UIKit.Tex(new Rect(g.x - eye * 0.5f, g.y - 11f - eye, eye, eye), UIKit.Ring, new Color(c.r, c.g, c.b, 0.85f * fade));
        }

        /// <summary>"TAKEDOWN" with the use button under the person it would hit.</summary>
        private static void Prompt(Camera cam, NpcController n, TakedownKind kind, float W, float H)
        {
            Vector2 g;
            if (!ToGui(cam, n.ChestPosition, W, H, out g)) return;
            string key = Pad.LastUsed && Pad.Connected ? Pad.Tag(PadButton.North) : "E";
            string what = kind == TakedownKind.Drop ? "DROP TAKEDOWN" : (kind == TakedownKind.Air ? "AIR TAKEDOWN" : "TAKEDOWN");
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 6f);
            UIKit.Rect(new Rect(g.x - 86f, g.y + 14f, 172f, 26f), new Color(0f, 0f, 0f, 0.55f));
            UIKit.Text(new Rect(g.x - 86f, g.y + 14f, 172f, 26f), what + "   " + key, 16, new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, pulse), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>The Third Eye's sight pulse: marks through walls on enemies (red) and interactables (cyan) the ring has reached.</summary>
        private static void Pulse(Camera cam, PlayerController p, float W, float H)
        {
            float age = p.SightPulseAge;
            float alpha = StealthRules.PulseAlpha(age);
            if (alpha <= 0f) return;
            Vector3 o = p.SightPulseOrigin;
            for (int i = 0; i < NpcController.All.Count; i++)
            {
                NpcController n = NpcController.All[i];
                if (n == null || n.IsDead) continue;
                bool hostile = n.Role == NpcRole.Gang || n.Role == NpcRole.Enemy || n.Role == NpcRole.Swat || (n.Role == NpcRole.Police && Game.Wanted != null && Game.Wanted.Stars > 0) || n.IsHostileToPlayer;
                if (!hostile) continue;
                float d = Vector3.Distance(n.transform.position, o);
                if (d > StealthRules.PulseRange || !StealthRules.PulseReached(age, d)) continue;
                Mark(cam, n.ChestPosition, d, new Color(1f, 0.3f, 0.25f), alpha, W, H);
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature c = Creature.All[i];
                if (c == null || !c.Ready || c.IsDead || !c.Targetable) continue;
                float d = Vector3.Distance(c.transform.position, o);
                if (d > StealthRules.PulseRange || !StealthRules.PulseReached(age, d)) continue;
                Mark(cam, c.ChestPosition, d, new Color(1f, 0.55f, 0.25f), alpha, W, H);
            }
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null || !it.isActiveAndEnabled || !it.OnFoot) continue;
                float d = Vector3.Distance(it.transform.position, o);
                if (d > StealthRules.PulseInteractableRange || !StealthRules.PulseReached(age, d)) continue;
                if (it.Condition != null)
                {
                    bool ok;
                    try { ok = it.Condition(); }
                    catch (System.Exception) { ok = false; }
                    if (!ok) continue;
                }
                Mark(cam, it.transform.position + Vector3.up * 1.2f, d, UIKit.Cyan, alpha * 0.9f, W, H);
            }
        }

        private static void Mark(Camera cam, Vector3 world, float d, Color c, float alpha, float W, float H)
        {
            Vector2 g;
            if (!ToGui(cam, world, W, H, out g)) return;
            float size = Mathf.Lerp(22f, 12f, Mathf.InverseLerp(5f, StealthRules.PulseRange, d));
            UIKit.Tex(new Rect(g.x - size * 0.5f, g.y - size * 0.5f, size, size), UIKit.Ring, new Color(c.r, c.g, c.b, alpha));
            UIKit.Rect(new Rect(g.x - 2f, g.y - 2f, 4f, 4f), new Color(c.r, c.g, c.b, alpha));
            UIKit.Text(new Rect(g.x - 40f, g.y + size * 0.5f, 80f, 18f), Mathf.RoundToInt(d) + " m", 12, new Color(c.r, c.g, c.b, alpha * 0.9f), TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>A word under the crosshair while sneaking: HIDDEN, WATCHED (someone is getting suspicious) or SEEN.</summary>
        private static void StanceWord(float W, float H, bool anyAlarmed, float highest)
        {
            string word;
            Color c;
            if (anyAlarmed)
            {
                word = "SEEN";
                c = new Color(1f, 0.25f, 0.2f, 0.9f);
            }
            else if (highest >= StealthRules.Suspicious)
            {
                word = "WATCHED";
                c = new Color(1f, 0.85f, 0.2f, 0.9f);
            }
            else
            {
                word = "HIDDEN";
                c = new Color(0.7f, 0.9f, 1f, 0.7f);
            }
            UIKit.Text(new Rect(W * 0.5f - 60f, H * 0.5f + 30f, 120f, 20f), word, 13, c, TextAnchor.MiddleCenter, FontStyle.Bold);
        }
    }
}
