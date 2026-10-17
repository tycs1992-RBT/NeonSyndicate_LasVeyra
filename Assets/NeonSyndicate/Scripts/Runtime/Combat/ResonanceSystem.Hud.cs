using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// What the Resonance loop shows: the glowing ring and crystal shell on every marked foe (pooled meshes that follow
    /// the body), the mark's name over it, the gold RESONANCE BURST marker at the crosshair, the crystal bar's pulse
    /// after an energy gain, the three first-time hints (each shown at most three times, remembered across sessions)
    /// and the F2 "Combat loop" page.
    /// </summary>
    public sealed partial class ResonanceSystem
    {
        private sealed class MarkVisual
        {
            public GameObject Root;
            public Transform Ring;
            public Transform Shell;
            public MeshRenderer RingRenderer;
            public MeshRenderer ShellRenderer;
            public ResonanceState State;
            public int Id;
        }

        /// <summary>Times each first-time hint may show, and the seconds between two showings of one.</summary>
        public const int HintShows = 3;
        public const float HintCooldown = 25f;
        private const string HintKey = "NSLV_reshint";

        private static readonly int[] _hintShown = new int[3];
        private static readonly float[] _hintAt = { -999f, -999f, -999f };
        private readonly List<MarkVisual> _visuals = new List<MarkVisual>();
        private static readonly List<int> _visIds = new List<int>();

        private static void ResetHudStatics()
        {
            for (int i = 0; i < _hintShown.Length; i++)
            {
                _hintShown[i] = 0;
                _hintAt[i] = -999f;
            }
            _visIds.Clear();
        }

        // ================================================================== mark visuals

        private void UpdateMarkVisuals(float now)
        {
            _visIds.Clear();
            _book.Ids(_visIds);
            int used = 0;
            for (int i = 0; i < _visIds.Count; i++)
            {
                CharacterBase c;
                if (!_bodies.TryGetValue(_visIds[i], out c) || c == null || c.IsDead) continue;
                ResonanceState s = _book.StateOf(_visIds[i], Time.time);
                if (s == ResonanceState.None) continue;
                MarkVisual v = VisualAt(used++);
                Place(v, c, s, _book.Remaining(_visIds[i], Time.time), now);
            }
            for (int i = used; i < _visuals.Count; i++) if (_visuals[i].Root.activeSelf) _visuals[i].Root.SetActive(false);
        }

        private MarkVisual VisualAt(int i)
        {
            while (_visuals.Count <= i)
            {
                MarkVisual v = new MarkVisual();
                v.Root = new GameObject("Resonance Mark");
                v.Root.transform.SetParent(transform, false);
                GameObject ring = Shapes.MeshObject("Ring", Shapes.CylinderMesh, new[] { Mats.Glow(Color.white) }, v.Root.transform, false);
                GameObject shell = Shapes.MeshObject("Shell", Shapes.SphereMesh, new[] { Mats.Marker(new Color(1f, 1f, 1f, 0.15f)) }, v.Root.transform, false);
                v.Ring = ring.transform;
                v.Shell = shell.transform;
                v.RingRenderer = ring.GetComponent<MeshRenderer>();
                v.ShellRenderer = shell.GetComponent<MeshRenderer>();
                v.State = ResonanceState.None;
                _visuals.Add(v);
            }
            return _visuals[i];
        }

        private static void Place(MarkVisual v, CharacterBase c, ResonanceState s, float left, float now)
        {
            if (!v.Root.activeSelf) v.Root.SetActive(true);
            if (v.State != s)
            {
                v.State = s;
                Color col = ResonanceRules.StateColor(s);
                v.RingRenderer.sharedMaterial = Mats.Glow(new Color(col.r, col.g, col.b, 0.85f));
                v.ShellRenderer.sharedMaterial = Mats.Marker(new Color(col.r, col.g, col.b, s == ResonanceState.Glassbound ? 0.3f : 0.16f));
            }
            bool boss = IsUnderBoss(c);
            float r = boss ? ResonanceRules.UnderBossMarkRadius(c.BodyRadius) : Mathf.Max(0.55f, c.BodyRadius * 1.6f + 0.35f);
            float h = c.BodyHeight > 0.1f ? c.BodyHeight : 1.8f;
            // The mark pulses faster as it runs out, so the player sees the window closing.
            float pulse = 1f + 0.08f * Mathf.Sin(now * (left < 0.3f ? 18f : 7f));
            Vector3 feet = c.transform.position;
            v.Root.transform.position = feet;
            v.Ring.localPosition = new Vector3(0f, 0.05f, 0f);
            v.Ring.localScale = new Vector3(r * 2f * pulse, 0.02f, r * 2f * pulse);
            v.Shell.position = c.ChestPosition;
            float sw = r * 1.5f * pulse;
            // 04E: on an Underheart boss the shell is a sphere around its lock point (an open weak point or the centre).
            v.Shell.localScale = new Vector3(sw, boss ? sw : Mathf.Max(sw, h * 0.75f), sw);
            v.Shell.localRotation = Quaternion.Euler(0f, now * 40f, 0f);
        }

        // ================================================================== HUD

        private static void DrawHud(float w, float h)
        {
            DrawMarkLabels(w, h);
            if (Time.unscaledTime < _burstMarkerUntil)
            {
                float t = (_burstMarkerUntil - Time.unscaledTime) / 0.9f;
                float a = Mathf.Clamp01(t * 2.2f);
                float pop = 1f + Mathf.Max(0f, t - 0.75f) * 3f;
                Color gold = new Color(1f, 0.82f, 0.3f, a);
                Color col = ResonanceRules.StateColor(_burstMarkerState);
                float cx = w * 0.5f, cy = h * 0.5f;
                float s = 46f * pop;
                UIKit.Tex(new Rect(cx - s * 0.5f, cy - s * 0.5f, s, s), UIKit.Ring, new Color(col.r, col.g, col.b, a));
                UIKit.Text(new Rect(cx - 300f, cy - 92f, 600f, 46f), "RESONANCE BURST", Mathf.RoundToInt(34f * pop), gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Text(new Rect(cx - 300f, cy - 54f, 600f, 26f), ResonanceRules.StateName(_burstMarkerState).ToUpper(), 18, new Color(col.r, col.g, col.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        /// <summary>The mark's name and a shrinking bar over every marked foe in view.</summary>
        private static void DrawMarkLabels(float w, float h)
        {
            if (Game.Cam == null || Game.Cam.Cam == null) return;
            Camera cam = Game.Cam.Cam;
            _visIds.Clear();
            _book.Ids(_visIds);
            for (int i = 0; i < _visIds.Count; i++)
            {
                CharacterBase c;
                if (!_bodies.TryGetValue(_visIds[i], out c) || c == null || c.IsDead) continue;
                ResonanceState s = _book.StateOf(_visIds[i], Time.time);
                if (s == ResonanceState.None) continue;
                Vector3 sp = cam.WorldToScreenPoint(c.HeadPosition + Vector3.up * 0.95f);
                if (sp.z <= 0.1f || sp.z > 60f) continue;
                float k = UIKit.Scale;
                Vector2 g = new Vector2(sp.x / k, (Screen.height - sp.y) / k);
                if (g.x < -60f || g.x > w + 60f || g.y < -40f || g.y > h + 40f) continue;
                Color col = ResonanceRules.StateColor(s);
                UIKit.Text(new Rect(g.x - 90f, g.y - 22f, 180f, 20f), ResonanceRules.StateName(s).ToUpper(), 14, col, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Bar(new Rect(g.x - 30f, g.y, 60f, 3f), _book.Remaining(_visIds[i], Time.time), col, new Color(0f, 0f, 0f, 0.5f));
            }
        }

        /// <summary>The crystal bar's pulse after an energy gain (HudCombat calls this with the bar's rectangle).</summary>
        public static void DrawEnergyPulse(Rect bar)
        {
            float p = _energyPulse;
            if (p <= 0.01f) return;
            Color c = new Color(0.55f, 0.9f, 1f, 0.55f * p);
            float grow = 4f * p;
            UIKit.Rect(new Rect(bar.x - grow, bar.y - grow, bar.width + grow * 2f, 2f), c);
            UIKit.Rect(new Rect(bar.x - grow, bar.yMax + grow - 2f, bar.width + grow * 2f, 2f), c);
            UIKit.Rect(new Rect(bar.x, bar.y, bar.width, bar.height), new Color(0.7f, 0.95f, 1f, 0.25f * p));
        }

        // ================================================================== first-time hints

        private static void LoadHints()
        {
            for (int i = 0; i < _hintShown.Length; i++)
            {
                try { _hintShown[i] = PlayerPrefs.GetInt(HintKey + i, 0); }
                catch (System.Exception) { _hintShown[i] = 0; }
            }
        }

        private static void Hint(int i, string text)
        {
            if (_hintShown[i] >= HintShows || Time.unscaledTime - _hintAt[i] < HintCooldown || Game.Hud == null) return;
            _hintAt[i] = Time.unscaledTime;
            _hintShown[i]++;
            try { PlayerPrefs.SetInt(HintKey + i, _hintShown[i]); }
            catch (System.Exception) { }
            Game.Hud.Notify(GInput.Prompt(text), 6f);
        }

        private static void HintCharged(Abilities ab)
        {
            if (ab != null && ab.AnyUnlocked) Hint(0, "Gun hits charge your crystal energy. Spend it: [B] cast a power.");
        }

        private static void HintPrimed()
        {
            Hint(1, "Marked! Shoot a glowing foe for a RESONANCE BURST: big damage, a splash and a full magazine.");
        }

        private static void HintBurst()
        {
            Hint(2, "Resonance Burst. Each power leaves its own mark: fire burns, lightning chains, gravity throws, glass shatters.");
        }

        /// <summary>F2: show every hint again.</summary>
        public static void DebugResetHints()
        {
            for (int i = 0; i < _hintShown.Length; i++)
            {
                _hintShown[i] = 0;
                _hintAt[i] = -999f;
                try { PlayerPrefs.SetInt(HintKey + i, 0); }
                catch (System.Exception) { }
            }
        }

        // ================================================================== F2 page

        private static void DrawDebug(Rect r)
        {
            float x = r.x, y = r.y;
            float bw = 270f, bh = 44f, gap = 14f;
            UIKit.Text(new Rect(x, y, r.width, 28f), "RESONANCE LOOP", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 32f;
            Abilities ab = Game.Abilities;
            string live = "Bursts " + _bursts + "   energy from hits " + Mathf.RoundToInt(_energyGained) + "   marked foes " + _book.Count
                + (ab != null ? "   crystal " + Mathf.RoundToInt(ab.Energy) + " / " + Mathf.RoundToInt(ab.MaxEnergy) : "");
            UIKit.Text(new Rect(x, y, r.width, 24f), live, 18, Color.white, TextAnchor.UpperLeft);
            y += 30f;
            for (int i = 1; i <= 4; i++)
            {
                ResonanceState s = (ResonanceState)i;
                if (UIKit.Button(new Rect(x + (i - 1) * (bw + gap), y, bw, bh), "Mark nearby: " + ResonanceRules.StateName(s), false, 17))
                    PrimeAround(Game.PlayerPosition, 30f, 6, s, false);
            }
            y += bh + gap;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Fill crystal energy", false, 17) && ab != null) ab.AddEnergy(ab.MaxEnergy);
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Show the hints again", false, 17)) DebugResetHints();
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Strip armour nearby", false, 17)) ArmorPlates.DebugStripNearby(30f);
            if (UIKit.Button(new Rect(x + (bw + gap) * 3f, y, bw, bh), "Break a part (nearest)", false, 17)) CreatureParts.DebugBreakNearest();
            y += bh + gap;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Spawn armoured squad", false, 17)) ArmorPlates.DebugSpawnSquad();
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Damage numbers: " + (Settings.DamageNumbers ? "on" : "off"), Settings.DamageNumbers, 17))
            {
                Settings.DamageNumbers = !Settings.DamageNumbers;
                Settings.Save();
            }
            y += bh + 18f;
            string rules = "Guns: " + ResonanceRules.ChargePerSecond + " energy per second of body hits (any gun), x" + ResonanceRules.CritChargeFactor
                + " on criticals and headshots, +" + ResonanceRules.GunKillBonus + " per gun kill, melee " + ResonanceRules.MeleeCharge + " a hit, at most "
                + ResonanceRules.MaxChargePerSecond + " a second. Marks last " + ResonanceRules.MarkSeconds + " s (bosses " + ResonanceRules.BossMarkSeconds
                + " s). Burst x" + ResonanceRules.BurstMultiplier + " (bosses x" + ResonanceRules.BossBurstMultiplier + "), part damage x" + ResonanceRules.BurstPartMultiplier
                + ". Ember Chain and Sunbolt: Kindled. Static Lance and Roc Call: Stormlit. Gravity Well: Unmoored. Glass Sight: Glassbound.";
            UIKit.Text(new Rect(x, y, r.width - 20f, 110f), rules, 16, new Color(0.78f, 0.78f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += 112f;
            string hints = "Hints shown: charge " + _hintShown[0] + ", mark " + _hintShown[1] + ", burst " + _hintShown[2] + " (of " + HintShows + " each)";
            UIKit.Text(new Rect(x, y, r.width, 24f), hints, 16, new Color(0.7f, 0.8f, 0.9f), TextAnchor.UpperLeft);
        }
    }
}
