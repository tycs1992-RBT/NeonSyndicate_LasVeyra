using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Combat HUD: health with a trailing damage ghost, stamina and crystal energy bars (violet with a VEIL tag inside the
    /// Inner World), Shard Tonic charges, Lumen, lock-on reticle, enemy health bars, the elite (boss) bar, floating damage
    /// numbers, combat callouts (PARRY, Perfect dodge, Guard broken), loot cards and the Veil hold ring.
    /// </summary>
    public sealed partial class Hud
    {
        private sealed class Callout
        {
            public string Text;
            public Color Color;
            public float Time;
        }

        private sealed class DamageNum
        {
            public Vector3 World;
            public float Amount;
            public float Time;
            public bool Crit;
        }

        private sealed class LootCard
        {
            public GearItem Item;
            public float Time;
        }

        private readonly List<Callout> _callouts = new List<Callout>();
        private readonly List<DamageNum> _numbers = new List<DamageNum>();
        private readonly List<LootCard> _loot = new List<LootCard>();
        private float _ghostHp = -1f;
        private float _ghostDelay;
        private int _lumenGain;
        private float _lumenTime;
        private float _lumenShown;
        private static readonly Vector3[] _corners = new Vector3[4];

        public void CombatText(string text, Color color)
        {
            for (int i = 0; i < _callouts.Count; i++)
            {
                if (_callouts[i].Text == text) { _callouts[i].Time = 1.4f; return; }
            }
            _callouts.Add(new Callout { Text = text, Color = color, Time = 1.4f });
            if (_callouts.Count > HudLayout04E.MaxCallouts) _callouts.RemoveAt(0);
        }

        public void LumenGained(int amount)
        {
            _lumenGain += amount;
            _lumenTime = 2.5f;
        }

        public void LootPopup(GearItem it)
        {
            _loot.Add(new LootCard { Item = it, Time = 4f });
            if (_loot.Count > 3) _loot.RemoveAt(0);
        }

        public void DamageNumber(Vector3 world, float amount, bool crit)
        {
            if (amount < 0.5f) return;
            _numbers.Add(new DamageNum { World = world + Random.insideUnitSphere * 0.15f, Amount = amount, Time = 0.9f, Crit = crit });
            if (_numbers.Count > 14) _numbers.RemoveAt(0);
        }

        private void UpdateCombatHud(float dt)
        {
            for (int i = _callouts.Count - 1; i >= 0; i--) { _callouts[i].Time -= dt; if (_callouts[i].Time <= 0f) _callouts.RemoveAt(i); }
            for (int i = _numbers.Count - 1; i >= 0; i--) { _numbers[i].Time -= dt; if (_numbers[i].Time <= 0f) _numbers.RemoveAt(i); }
            for (int i = _loot.Count - 1; i >= 0; i--) { _loot[i].Time -= dt; if (_loot[i].Time <= 0f) _loot.RemoveAt(i); }
            _lumenTime -= dt;
            if (_lumenTime <= 0f) _lumenGain = 0;
            PlayerController p = Game.Player;
            if (p == null) return;
            float hp = p.Health.Health;
            if (_ghostHp < 0f || hp > _ghostHp) { _ghostHp = hp; _ghostDelay = 0f; }
            else if (hp < _ghostHp)
            {
                _ghostDelay += dt;
                if (_ghostDelay > 0.6f) _ghostHp = Mathf.MoveTowards(_ghostHp, hp, dt * p.Health.MaxHealth * 0.5f);
            }
            float lumen = Game.Progress != null ? Game.Progress.Lumen : 0f;
            _lumenShown = Mathf.MoveTowards(_lumenShown, lumen, Mathf.Max(30f, Mathf.Abs(lumen - _lumenShown) * 4f) * dt);
        }

        // ------------------------------------------------------------------ drawing

        private void DrawCombatHud(float W, float H, PlayerController p)
        {
            DrawVitals(p);
            DrawWorldBars(W, H, p);
            DrawLockOn(W, H, p);
            DrawBossBar(W, H, p);
            DrawNumbers(W, H);
            DrawCallouts(W, H, p);
            DrawLumen(W, H, p);
            DrawLoot(W, H);
            DrawVeilHold(W, H);
        }

        private void DrawVitals(PlayerController p)
        {
            float x = 40f, y = 34f;
            float maxHp = Mathf.Max(1f, p.Health.MaxHealth);
            float hw = Mathf.Clamp(300f + maxHp * 0.7f, 420f, 760f);
            // Health with the trailing ghost that shows the chunk you just lost.
            UIKit.Rect(new Rect(x - 3f, y - 3f, hw + 6f, 22f), new Color(0f, 0f, 0f, 0.6f));
            float ghost = Mathf.Clamp01(_ghostHp / maxHp);
            float hpf = Mathf.Clamp01(p.Health.Health / maxHp);
            UIKit.Rect(new Rect(x, y, hw * ghost, 16f), new Color(1f, 0.85f, 0.55f, 0.85f));
            Color hc = hpf < 0.25f ? Color.Lerp(new Color(0.65f, 0.05f, 0.05f), new Color(1f, 0.15f, 0.12f), Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : new Color(0.78f, 0.1f, 0.12f);
            UIKit.Rect(new Rect(x, y, hw * hpf, 16f), hc);
            UIKit.Rect(new Rect(x, y, hw * hpf, 4f), new Color(1f, 0.5f, 0.5f, 0.35f));
            // Armor as a thin strip under the health.
            if (p.Health.Armor > 0.5f)
                UIKit.Rect(new Rect(x, y + 17f, hw * Mathf.Clamp01(p.Health.Armor / Mathf.Max(1f, p.Health.MaxArmor)), 3f), UIKit.Armor);
            y += 26f;
            // Stamina.
            PlayerCombat c = p.Combat;
            if (c != null)
            {
                float sw = Mathf.Clamp(220f + c.MaxStamina * 1.6f, 360f, 640f);
                UIKit.Rect(new Rect(x - 3f, y - 3f, sw + 6f, 16f), new Color(0f, 0f, 0f, 0.6f));
                Color sc = c.Exhausted ? new Color(0.55f, 0.6f, 0.2f) : new Color(0.2f, 0.75f, 0.32f);
                UIKit.Rect(new Rect(x, y, sw * Mathf.Clamp01(c.Stamina / Mathf.Max(1f, c.MaxStamina)), 10f), sc);
                y += 20f;
            }
            // Crystal energy.
            Abilities ab = Game.Abilities;
            if (ab != null)
            {
                float ew = Mathf.Clamp(220f + ab.MaxEnergy * 1.4f, 320f, 640f);
                UIKit.Rect(new Rect(x - 3f, y - 3f, ew + 6f, 14f), new Color(0f, 0f, 0f, 0.6f));
                bool veil = Game.Inner != null && Game.Inner.IsInner;
                UIKit.Rect(new Rect(x, y, ew * Mathf.Clamp01(ab.Energy / Mathf.Max(1f, ab.MaxEnergy)), 8f), ab.ThirdEyeActive ? UIKit.Cyan : (veil ? VeilViolet : new Color(0.3f, 0.65f, 1f)));
                ResonanceSystem.DrawEnergyPulse(new Rect(x, y, ew, 8f));
                if (veil) DrawVeilTag(x + ew + 14f, y, ab);
                y += 18f;
            }
            // Shard Tonic charges.
            if (c != null)
            {
                y += 6f;
                string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.West) : "[Q]";
                for (int i = 0; i < c.MaxTonics; i++)
                {
                    bool full = i < c.Tonics;
                    UIKit.Tex(new Rect(x + i * 22f, y, 18f, 26f), UIKit.Circle, full ? new Color(0.35f, 1f, 0.85f, 0.95f) : new Color(0.3f, 0.35f, 0.35f, 0.6f));
                }
                UIKit.Text(new Rect(x + c.MaxTonics * 22f + 8f, y + 2f, 300f, 24f), "Shard Tonic " + key, 17, new Color(0.75f, 1f, 0.95f), TextAnchor.UpperLeft);
            }
        }

        private bool ToScreen(Vector3 world, float W, float H, out Vector2 gui)
        {
            gui = Vector2.zero;
            if (Game.Cam == null || Game.Cam.Cam == null) return false;
            Vector3 sp = Game.Cam.Cam.WorldToScreenPoint(world);
            if (sp.z <= 0.1f) return false;
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return gui.x > -50f && gui.x < W + 50f && gui.y > -50f && gui.y < H + 50f;
        }

        private void DrawWorldBars(float W, float H, PlayerController p)
        {
            Vector3 pp = p.transform.position;
            CharacterBase locked = p.Combat != null ? p.Combat.LockTarget : null;
            foreach (NpcController n in NpcController.All)
            {
                if (n.IsDead || n.InVehicle || n == locked) continue;
                bool recent = Time.time - n.Health.LastDamageTime < 6f && n.Health.Health < n.Health.MaxHealth;
                if (!recent && !(n.Elite && n.IsHostileToPlayer)) continue;
                float d = (n.transform.position - pp).sqrMagnitude;
                if (d > 35f * 35f) continue;
                Vector2 g;
                if (!ToScreen(n.HeadPosition + Vector3.up * 0.45f, W, H, out g)) continue;
                float w = n.Elite ? 120f : 80f;
                float f = Mathf.Clamp01(n.Health.Health / Mathf.Max(1f, n.Health.MaxHealth));
                UIKit.Rect(new Rect(g.x - w * 0.5f - 1f, g.y - 1f, w + 2f, 7f), new Color(0f, 0f, 0f, 0.65f));
                UIKit.Rect(new Rect(g.x - w * 0.5f, g.y, w * f, 5f), n.Elite ? new Color(1f, 0.55f, 0.15f) : new Color(0.85f, 0.15f, 0.15f));
                ArmorPlates.DrawPlateBar(n, g.x, g.y, w);
                if (n.IsStaggered) UIKit.Text(new Rect(g.x - 60f, g.y - 24f, 120f, 20f), n.CanBeRiposted ? "OPEN" : "Staggered", 15, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        private void DrawLockOn(float W, float H, PlayerController p)
        {
            if (p.Combat == null || p.Combat.LockTarget == null) return;
            CharacterBase t = p.Combat.LockTarget;
            Vector2 g;
            if (!ToScreen(t.ChestPosition, W, H, out g)) return;
            // A small white diamond on the chest, a bar above the head.
            float s = 9f + Mathf.Sin(Time.unscaledTime * 6f) * 1.2f;
            Matrix4x4 m = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, g * UIKit.Scale);
            UIKit.Rect(new Rect(g.x - s * 0.5f, g.y - s * 0.5f, s, s), new Color(1f, 1f, 1f, 0.95f));
            UIKit.Rect(new Rect(g.x - s * 0.3f, g.y - s * 0.3f, s * 0.6f, s * 0.6f), new Color(0.9f, 0.2f, 0.25f, 0.9f));
            GUI.matrix = m;
            NpcController n = t as NpcController;
            Vector2 h;
            if (ToScreen(t.HeadPosition + Vector3.up * 0.5f, W, H, out h))
            {
                float w = 110f;
                float f = Mathf.Clamp01(t.Health.Health / Mathf.Max(1f, t.Health.MaxHealth));
                UIKit.Rect(new Rect(h.x - w * 0.5f - 1f, h.y - 1f, w + 2f, 8f), new Color(0f, 0f, 0f, 0.7f));
                UIKit.Rect(new Rect(h.x - w * 0.5f, h.y, w * f, 6f), new Color(0.85f, 0.15f, 0.15f));
                ArmorPlates.DrawPlateBar(t, h.x, h.y, w);
                if (n != null)
                {
                    float poise = Mathf.Clamp01(n.Poise / Mathf.Max(1f, n.MaxPoise));
                    UIKit.Rect(new Rect(h.x - w * 0.5f, h.y + 7f, w * poise, 2f), new Color(1f, 0.85f, 0.45f, 0.8f));
                    if (n.IsWindingUp) UIKit.Text(new Rect(h.x - 70f, h.y - 26f, 140f, 22f), "!", 22, new Color(1f, 0.3f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                }
                Creature cr = t as Creature;
                if (cr != null)
                {
                    // Creatures: the poise strip, and a warning while a blow winds up (orange when it cannot be blocked).
                    float poise = Mathf.Clamp01(cr.Poise / Mathf.Max(1f, cr.MaxPoise));
                    UIKit.Rect(new Rect(h.x - w * 0.5f, h.y + 7f, w * poise, 2f), new Color(1f, 0.85f, 0.45f, 0.8f));
                    if (cr.IsWindingUp) UIKit.Text(new Rect(h.x - 70f, h.y - 26f, 140f, 22f), cr.CurrentAttackUnblockable ? "!!" : "!", 22, cr.CurrentAttackUnblockable ? new Color(1f, 0.62f, 0.12f) : new Color(1f, 0.3f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    if (cr.IsStaggered) UIKit.Text(new Rect(h.x - 70f, h.y + 12f, 140f, 20f), "Staggered", 15, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter, FontStyle.Bold);
                }
            }
            if (p.Combat.HasRiposteTarget)
            {
                string key = Pad.Connected && Pad.LastUsed ? Pad.Tag(PadButton.RB) : "[LMB]";
                UIKit.Text(new Rect(g.x - 150f, g.y + 26f, 300f, 26f), "RIPOSTE " + key, 22, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        private void DrawBossBar(float W, float H, PlayerController p)
        {
            // Creature bosses and great beasts first (with their phase), then the elite people.
            CharacterBase cb;
            float cPoise;
            string cLabel;
            if (CreatureHud.BossBar(p, out cb, out cPoise, out cLabel) && cb != null && !cb.IsDead)
            {
                float bw = Mathf.Min(900f, W * 0.55f);
                float bx = W * 0.5f - bw * 0.5f, by = H - 150f;
                UIKit.Text(new Rect(bx, by - 30f, bw, 28f), cb.DisplayName, 24, new Color(1f, 0.9f, 0.8f), TextAnchor.LowerLeft, FontStyle.Bold);
                if (!string.IsNullOrEmpty(cLabel)) UIKit.Text(new Rect(bx, by - 30f, bw, 28f), cLabel, 18, new Color(1f, 0.75f, 0.45f), TextAnchor.LowerRight, FontStyle.Bold);
                UIKit.Rect(new Rect(bx - 3f, by - 3f, bw + 6f, 20f), new Color(0f, 0f, 0f, 0.7f));
                UIKit.Rect(new Rect(bx, by, bw * Mathf.Clamp01(cb.Health.Health / Mathf.Max(1f, cb.Health.MaxHealth)), 14f), new Color(0.7f, 0.08f, 0.1f));
                // Phase marks at two thirds and one third.
                UIKit.Rect(new Rect(bx + bw * 0.33f - 1f, by, 2f, 14f), new Color(1f, 1f, 1f, 0.45f));
                UIKit.Rect(new Rect(bx + bw * 0.66f - 1f, by, 2f, 14f), new Color(1f, 1f, 1f, 0.45f));
                UIKit.Rect(new Rect(bx, by + 15f, bw * cPoise, 3f), new Color(1f, 0.8f, 0.4f, 0.85f));
                return;
            }
            NpcController boss = p.Combat != null ? p.Combat.LockTarget as NpcController : null;
            if (boss == null || !boss.Elite)
            {
                boss = null;
                float bd = 30f * 30f;
                foreach (NpcController n in NpcController.All)
                {
                    if (!n.Elite || n.IsDead || n.Target != p) continue;
                    float d = (n.transform.position - p.transform.position).sqrMagnitude;
                    if (d < bd) { bd = d; boss = n; }
                }
            }
            if (boss == null || boss.IsDead) return;
            float w = Mathf.Min(900f, W * 0.55f);
            float x = W * 0.5f - w * 0.5f, y = H - 150f;
            UIKit.Text(new Rect(x, y - 30f, w, 28f), boss.DisplayName, 24, new Color(1f, 0.9f, 0.8f), TextAnchor.LowerLeft, FontStyle.Bold);
            UIKit.Rect(new Rect(x - 3f, y - 3f, w + 6f, 20f), new Color(0f, 0f, 0f, 0.7f));
            UIKit.Rect(new Rect(x, y, w * Mathf.Clamp01(boss.Health.Health / Mathf.Max(1f, boss.Health.MaxHealth)), 14f), new Color(0.7f, 0.08f, 0.1f));
            UIKit.Rect(new Rect(x, y + 15f, w * Mathf.Clamp01(boss.Poise / Mathf.Max(1f, boss.MaxPoise)), 3f), new Color(1f, 0.8f, 0.4f, 0.85f));
        }

        private void DrawNumbers(float W, float H)
        {
            foreach (DamageNum d in _numbers)
            {
                Vector2 g;
                float rise = (0.9f - d.Time) * 0.9f;
                if (!ToScreen(d.World + Vector3.up * rise, W, H, out g)) continue;
                float a = Mathf.Clamp01(d.Time * 2.2f);
                int size = d.Crit ? 30 : 22;
                UIKit.Text(new Rect(g.x - 80f, g.y - 16f, 160f, 32f), Mathf.RoundToInt(d.Amount).ToString(), size, d.Crit ? new Color(1f, 0.82f, 0.3f, a) : new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        /// <summary>
        /// The callout stack (three lines at most) below the crosshair zone. Its place comes from HudLayout04E: H * 0.6 at
        /// the default scale, raised on short screens, and the third line always ends above the Underheart heat row.
        /// </summary>
        private void DrawCallouts(float W, float H, PlayerController p)
        {
            float y = HudLayout04E.CalloutTop(H);
            foreach (Callout c in _callouts)
            {
                float a = Mathf.Clamp01(c.Time * 2f);
                float pop = 1f + Mathf.Max(0f, c.Time - 1.2f) * 2.5f;
                UIKit.Text(new Rect(0f, y, W, HudLayout04E.CalloutHeight), c.Text, Mathf.RoundToInt(34f * pop), new Color(c.Color.r, c.Color.g, c.Color.b, a), TextAnchor.MiddleCenter, FontStyle.Bold);
                y += HudLayout04E.CalloutStep;
            }
        }

        private void DrawLumen(float W, float H, PlayerController p)
        {
            if (Game.Progress == null) return;
            float x = W - 40f;
            float y = H - (p.InVehicle && Settings.ShowSpeedometer ? 210f : 70f);
            string txt = "LUMEN  " + Mathf.RoundToInt(_lumenShown).ToString("N0");
            UIKit.Rect(new Rect(x - 270f, y, 270f, 40f), new Color(0f, 0f, 0f, 0.55f));
            UIKit.Text(new Rect(x - 260f, y + 6f, 250f, 30f), txt, 24, new Color(0.85f, 0.7f, 1f), TextAnchor.UpperRight, FontStyle.Bold);
            if (_lumenGain > 0 && _lumenTime > 0f)
                UIKit.Text(new Rect(x - 260f, y - 30f, 250f, 28f), "+" + _lumenGain, 22, new Color(0.9f, 0.8f, 1f, Mathf.Clamp01(_lumenTime)), TextAnchor.UpperRight, FontStyle.Bold);
            if (Game.Progress.EchoActive)
                UIKit.Text(new Rect(x - 420f, y + 42f, 410f, 24f), "Echo waiting: " + Game.Progress.EchoLumen.ToString("N0") + " Lumen", 17, new Color(0.8f, 0.6f, 1f), TextAnchor.UpperRight);
        }

        private void DrawLoot(float W, float H)
        {
            float y = H * 0.36f;
            foreach (LootCard c in _loot)
            {
                float a = Mathf.Clamp01(c.Time * 1.5f);
                Color rc = GearSystem.RarityColor(c.Item.Rarity);
                Rect r = new Rect(W - 470f, y, 430f, 70f);
                UIKit.Rect(r, new Color(0f, 0f, 0f, 0.7f * a));
                UIKit.Rect(new Rect(r.x, r.y, 5f, r.height), new Color(rc.r, rc.g, rc.b, a));
                UIKit.Text(new Rect(r.x + 16f, r.y + 6f, r.width - 24f, 24f), (GearSystem.RarityName(c.Item.Rarity) + " " + GearSystem.SlotName(c.Item.Slot)).ToUpper(), 16, new Color(rc.r, rc.g, rc.b, a), TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(r.x + 16f, r.y + 30f, r.width - 24f, 30f), c.Item.Name, 22, new Color(1f, 1f, 1f, a), TextAnchor.UpperLeft, FontStyle.Bold);
                y += 78f;
            }
        }
    }
}
