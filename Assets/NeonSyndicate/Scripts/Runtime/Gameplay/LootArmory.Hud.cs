using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The loot HUD overlay: the compact pickup card next to a gun on the ground (with green and red differences and the
    /// device's own button prompts), the heat bar of a crystal caster and the charge bar of a charge shot under the
    /// crosshair, and name plates over champions.
    /// </summary>
    public sealed partial class LootArmory
    {
        private void DrawHud(float w, float h)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            DrawChampionPlates(w, h);
            DrawWeaponBars(w, h, p);
            Drop d = FocusedDrop();
            if (d != null && d.Gun != null && !p.InVehicle) DrawPickupCard(w, h, d.Gun);
        }

        private void DrawPickupCard(float w, float h, LootGun g)
        {
            string cmpLabel;
            CardStats cmp = CompareFor(g, out cmpLabel);
            float cw = 430f;
            Rect r = new Rect(w - cw - 40f, h * 0.5f - 250f, cw, 470f + g.Traits.Count * 26f);
            float bottom = DrawCard(r, g, cmp, cmpLabel, 17);
            string prompt = GInput.Prompt("Press E to take, hold to equip") + (BackpackFull ? "  (backpack full)" : "");
            UIKit.Text(new Rect(r.x + 14f, bottom + 6f, r.width - 28f, 26f), prompt, 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            // H8S: where holding puts it and the gear score before and after.
            DrawPickupWhatIfH8S(r, bottom + 32f, g);
        }

        private static void DrawWeaponBars(float w, float h, PlayerController p)
        {
            WeaponController wc = p.Weapons;
            WeaponDef d = wc.Current;
            if (d == null || !d.IsLoot) return;
            float bw = 140f;
            float y = h * 0.5f + 46f;
            if (d.Overheats && (wc.Heat > 0.01f || wc.Overheated))
            {
                Rect r = new Rect(w * 0.5f - bw * 0.5f, y, bw, 8f);
                Color c = wc.Overheated ? (Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.6f, 0.2f)) : Color.Lerp(new Color(0.4f, 0.9f, 1f), new Color(1f, 0.35f, 0.15f), wc.Heat);
                UIKit.Bar(r, wc.Heat, c, new Color(0f, 0f, 0f, 0.5f));
                if (wc.Overheated) UIKit.Text(new Rect(r.x - 40f, r.yMax + 2f, bw + 80f, 20f), "VENTING", 14, c, TextAnchor.UpperCenter, FontStyle.Bold);
                y += 26f;
            }
            if (wc.Charge > 0.01f)
            {
                Rect r = new Rect(w * 0.5f - bw * 0.5f, y, bw, 8f);
                UIKit.Bar(r, wc.Charge, wc.Charge >= 0.99f ? Color.white : UIKit.Gold, new Color(0f, 0f, 0f, 0.5f));
            }
            if (wc.CritStacks > 0)
                UIKit.Text(new Rect(w * 0.5f - 100f, h * 0.5f - 70f, 200f, 20f), "CRIT x" + wc.CritStacks, 14, new Color(1f, 0.85f, 0.3f), TextAnchor.UpperCenter, FontStyle.Bold);
        }

        private static bool ToGui(Camera cam, Vector3 world, out Vector2 gui)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0.2f)
            {
                gui = Vector2.zero;
                return false;
            }
            float s = UIKit.Scale;
            gui = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
            return true;
        }

        private void DrawChampionPlates(float w, float h)
        {
            if (_champions.Count == 0 || Game.Cam == null || Game.Cam.Cam == null) return;
            Camera cam = Game.Cam.Cam;
            Vector3 eye = cam.transform.position;
            Color c = GearSystem.RarityColor(Rarity.Epic);
            for (int i = 0; i < _champions.Count; i++)
            {
                NpcController n = _champions[i];
                if (n == null || n.IsDead) continue;
                Vector3 head = n.transform.position + Vector3.up * 2.25f;
                float dist = (head - eye).magnitude;
                if (dist > 55f) continue;
                Vector2 g;
                if (!ToGui(cam, head, out g)) continue;
                if (g.x < -100f || g.x > w + 100f || g.y < -50f || g.y > h + 50f) continue;
                float a = Mathf.Clamp01(1.4f - dist / 55f);
                float bw = 180f;
                UIKit.Text(new Rect(g.x - 150f, g.y - 54f, 300f, 24f), n.DisplayName, 18, new Color(c.r, c.g, c.b, a), TextAnchor.LowerCenter, FontStyle.Bold);
                UIKit.Text(new Rect(g.x - 150f, g.y - 32f, 300f, 18f), "CHAMPION: " + ChampionRules.AffixLine(n.ChampionAffixes), 12, new Color(1f, 1f, 1f, 0.85f * a), TextAnchor.LowerCenter, FontStyle.Bold);
                Rect bar = new Rect(g.x - bw * 0.5f, g.y - 12f, bw, 7f);
                UIKit.Bar(bar, n.Health.Health / Mathf.Max(1f, n.Health.MaxHealth), new Color(c.r, c.g, c.b, a), new Color(0f, 0f, 0f, 0.55f * a));
                // Armour over health (blue over red), like every enemy name plate.
                if (n.Health.Armor > 0.5f) UIKit.Bar(new Rect(bar.x, bar.y - 6f, bw, 4f), n.Health.Armor / Mathf.Max(1f, n.Health.MaxArmor), new Color(0.4f, 0.72f, 1f, a), new Color(0f, 0f, 0f, 0.4f * a));
            }
        }
    }
}
