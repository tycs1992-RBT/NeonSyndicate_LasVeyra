using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The ARSENAL pause tab (four equipped loot slots, a 30-gun backpack in rarity frames, a detail card, equip, favorite,
    /// compare and scrap, with full mouse and controller navigation) and the gun card drawing shared by the pickup card,
    /// the gun shop and the Forge's ARMORY tab.
    /// </summary>
    public sealed partial class LootArmory
    {
        /// <summary>The numbers a card shows and compares (works for loot guns and the fixed arsenal alike).</summary>
        public struct CardStats
        {
            public bool Valid;
            public bool Heat;
            public float Dps, Damage, Rate, Mag, Reload, Accuracy, Range, Crit;
        }

        public static CardStats StatsOf(LootGun g)
        {
            if (g == null) return new CardStats();
            LootStats s = g.Stats;
            bool heat = s.HeatPerShot > 0f;
            return new CardStats
            {
                Valid = true,
                Heat = heat,
                Dps = LootGenerator.BurstDps(g, true),
                Damage = s.Damage * Mathf.Max(1, s.Pellets),
                Rate = 60f / Mathf.Max(0.04f, LootGenerator.CycleTime(g)) * (g.HasTrait(LootTrait.BurstFire) ? 3f : 1f),
                Mag = heat ? Mathf.Ceil(1f / s.HeatPerShot) : s.Clip,
                Reload = s.Reload,
                Accuracy = 100f / (1f + 2f * s.SpreadAim),
                Range = s.Range,
                Crit = s.CritChance * 100f
            };
        }

        public static CardStats StatsOf(WeaponDef d)
        {
            if (d == null || !d.IsGun) return new CardStats();
            if (d.Loot != null) return StatsOf(d.Loot);
            return new CardStats
            {
                Valid = true,
                Dps = d.Damage * Mathf.Max(1, d.Pellets) / Mathf.Max(0.02f, d.Interval),
                Damage = d.Damage * Mathf.Max(1, d.Pellets),
                Rate = 60f / Mathf.Max(0.02f, d.Interval),
                Mag = d.ClipSize,
                Reload = d.ReloadTime,
                Accuracy = 100f / (1f + 2f * d.SpreadAim),
                Range = d.Range,
                Crit = 0f
            };
        }

        /// <summary>What a gun is compared with: the gun in hand, else the best loot gun of the same archetype.</summary>
        public CardStats CompareFor(LootGun g, out string label)
        {
            label = "";
            PlayerController p = Game.Player;
            if (p != null && p.Weapons.Current.IsGun && p.Weapons.Current.Loot != g)
            {
                label = p.Weapons.Current.Name;
                return StatsOf(p.Weapons.Current);
            }
            LootGun best = null;
            float bestDps = -1f;
            for (int i = 0; i < Slots.Length; i++) Consider(Slots[i], g, ref best, ref bestDps);
            foreach (LootGun b in Backpack) Consider(b, g, ref best, ref bestDps);
            if (best == null) return new CardStats();
            label = best.Name;
            return StatsOf(best);
        }

        private static void Consider(LootGun c, LootGun g, ref LootGun best, ref float bestDps)
        {
            if (c == null || c == g || c.Archetype != g.Archetype) return;
            float d = LootGenerator.BurstDps(c, true);
            if (d > bestDps)
            {
                bestDps = d;
                best = c;
            }
        }

        private static void StatRow(ref float y, float x, float w, string label, float v, float cmp, bool hasCmp, bool higherBetter, string fmt, string unit, int size)
        {
            UIKit.Text(new Rect(x, y, w * 0.5f, size + 6f), label, size, new Color(0.78f, 0.78f, 0.82f), TextAnchor.UpperLeft);
            string val = v.ToString(fmt) + unit;
            if (hasCmp)
            {
                float diff = v - cmp;
                float rel = Mathf.Abs(diff) / Mathf.Max(0.001f, Mathf.Abs(cmp));
                if (rel > 0.005f)
                {
                    bool better = higherBetter ? diff > 0f : diff < 0f;
                    string sign = diff > 0f ? "+" : "-";
                    val += "  <color=#" + ProgressionUI.Hex(better ? ProgressionUI.Good : ProgressionUI.Bad) + ">" + sign + Mathf.Abs(diff).ToString(fmt) + "</color>";
                }
            }
            UIKit.Text(new Rect(x + w * 0.42f, y, w * 0.58f, size + 6f), val, size, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
            y += size + 5f;
        }

        /// <summary>The stat rows of a card with green and red differences against cmp. Returns the y below them.</summary>
        public static float DrawStatRows(float x, float y, float w, CardStats s, CardStats cmp, int size)
        {
            bool c = cmp.Valid;
            StatRow(ref y, x, w, "Damage per second", s.Dps, cmp.Dps, c, true, "0", "", size);
            StatRow(ref y, x, w, "Damage", s.Damage, cmp.Damage, c, true, "0", "", size);
            StatRow(ref y, x, w, "Fire rate", s.Rate, cmp.Rate, c, true, "0", "/min", size);
            StatRow(ref y, x, w, s.Heat ? "Shots before overheating" : "Magazine", s.Mag, cmp.Mag, c && s.Heat == cmp.Heat, true, "0", "", size);
            StatRow(ref y, x, w, s.Heat ? "Vent time" : "Reload", s.Reload, cmp.Reload, c && s.Heat == cmp.Heat, false, "0.0", " s", size);
            StatRow(ref y, x, w, "Accuracy", s.Accuracy, cmp.Accuracy, c, true, "0", "", size);
            StatRow(ref y, x, w, "Range", s.Range, cmp.Range, c, true, "0", " m", size);
            StatRow(ref y, x, w, "Critical chance", s.Crit, cmp.Crit, c, true, "0", "%", size);
            return y;
        }

        /// <summary>The rarity, archetype and level line ("Epic Assault Rifle, level 12").</summary>
        public static string KindLine(LootGun g)
        {
            return GearSystem.RarityName(g.Rarity) + " " + LootGenerator.ArchetypeName(g.Archetype, LootGenerator.IsRocket(g)) + ", level " + g.Level;
        }

        /// <summary>
        /// A full gun card: name in its rarity colour, kind and maker, stats with differences against cmp, the element,
        /// the traits and the flavor line. Returns the y below the card's content.
        /// </summary>
        public static float DrawCard(Rect r, LootGun g, CardStats cmp, string cmpLabel, int size)
        {
            Color rc = GearSystem.RarityColor(g.Rarity);
            UIKit.Rect(r, new Color(0.03f, 0.04f, 0.06f, 0.92f));
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), rc);
            UIKit.Frame(r, new Color(rc.r, rc.g, rc.b, 0.6f), 2f);
            float x = r.x + 14f, w = r.width - 28f, y = r.y + 10f;
            UIKit.Text(new Rect(x, y, w, size + 14f), (g.Favorite ? "* " : "") + g.Name, size + 6, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += size + 12f;
            UIKit.Text(new Rect(x, y, w, size + 6f), KindLine(g) + "   " + LootGenerator.MakerFull[(int)g.Maker], size - 2, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            y += size + 8f;
            if (cmp.Valid && !string.IsNullOrEmpty(cmpLabel))
            {
                UIKit.Text(new Rect(x, y, w, size + 4f), "Compared with " + cmpLabel, size - 3, new Color(0.65f, 0.65f, 0.7f), TextAnchor.UpperLeft);
                y += size + 2f;
            }
            y = DrawStatRows(x, y, w, StatsOf(g), cmp, size);
            if (g.Element != Element.None)
            {
                Color ec = GearSystem.ElementColor(g.Element);
                UIKit.Text(new Rect(x, y + 2f, w, size + 6f), GearSystem.ElementName(g.Element) + " element: " + Mathf.RoundToInt(g.Stats.ElementChance * 100f) + "% per hit, power " + g.Stats.ElementPower.ToString("0.0"), size - 1, ec, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                y += size + 6f;
            }
            for (int i = 0; i < g.Traits.Count; i++)
            {
                string t = "<color=#" + ProgressionUI.Hex(UIKit.Gold) + ">" + LootGenerator.TraitName(g.Traits[i]) + ":</color> " + LootGenerator.TraitText(g.Traits[i], g.Archetype);
                float th = UIKit.Measure(t, size - 2).x > w ? (size - 2) * 2.4f : size + 4f;
                UIKit.Text(new Rect(x, y + 2f, w, th), t, size - 2, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += th + 2f;
            }
            if (g.IsUnique)
            {
                UIKit.Text(new Rect(x, y + 4f, w, size * 2.4f), "\"" + g.Flavor + "\"", size - 2, new Color(0.9f, 0.85f, 0.7f), TextAnchor.UpperLeft, FontStyle.Italic, true, true);
                y += size * 2.4f;
            }
            return y;
        }

        // ------------------------------------------------------------------ the ARSENAL tab

        private int _selUid;
        private int _scrapArmedUid;
        private float _scrapArmedUntil;
        private int _compareSlot = -1;
        private string _status = "";
        private float _statusUntil;
        private bool _statusGood;

        private void Status(string s, bool good)
        {
            _status = s ?? "";
            _statusGood = good;
            _statusUntil = Time.unscaledTime + 3.5f;
        }

        /// <summary>One gun cell: rarity frame, name, level, favorite star; clicking (or A on the pad) selects it.</summary>
        private void DrawCell(Rect c, LootGun g, string label, LootGun sel)
        {
            bool hot, focused;
            bool clicked = ProgressionUI.Hotspot(c, out hot, out focused);
            UIKit.Rect(c, g != null ? new Color(0.06f, 0.07f, 0.1f, 0.95f) : new Color(0f, 0f, 0f, 0.4f));
            if (g != null)
            {
                Color rc = GearSystem.RarityColor(g.Rarity);
                UIKit.Frame(c, rc, g.Rarity >= Rarity.Epic ? 3f : 2f);
                UIKit.Rect(new Rect(c.x, c.yMax - 5f, c.width, 5f), new Color(rc.r, rc.g, rc.b, 0.5f));
                UIKit.Text(new Rect(c.x + 6f, c.y + (label != null ? 20f : 4f), c.width - 12f, c.height - 26f), g.Name, 14, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                UIKit.Text(new Rect(c.x + 6f, c.yMax - 22f, c.width - 12f, 16f), "Lv " + g.Level + (g.Element != Element.None ? "  " + GearSystem.ElementName(g.Element) : ""), 12, new Color(0.8f, 0.8f, 0.85f), TextAnchor.UpperLeft);
                if (g.Favorite) UIKit.Tex(new Rect(c.xMax - 18f, c.y + 3f, 15f, 15f), UIKit.Star, UIKit.Gold);
            }
            else UIKit.Frame(c, new Color(1f, 1f, 1f, 0.1f), 1f);
            if (label != null) UIKit.Text(new Rect(c.x + 6f, c.y + 2f, c.width - 12f, 18f), label, 13, new Color(0.7f, 0.7f, 0.75f), TextAnchor.UpperLeft, FontStyle.Bold);
            if (g != null && sel == g) UIKit.Frame(new Rect(c.x - 3f, c.y - 3f, c.width + 6f, c.height + 6f), UIKit.Gold, 2f);
            if (focused) UIKit.Frame(new Rect(c.x - 5f, c.y - 5f, c.width + 10f, c.height + 10f), UIKit.Cyan, 3f);
            else if (hot) UIKit.Frame(new Rect(c.x - 4f, c.y - 4f, c.width + 8f, c.height + 8f), new Color(1f, 1f, 1f, 0.5f), 2f);
            if (clicked && g != null) _selUid = g.Uid;
        }

        private void DrawArsenal(Rect r)
        {
            LootGun sel = Find(_selUid);
            float leftW = Mathf.Min(820f, r.width * 0.55f);
            Rect left = new Rect(r.x, r.y, leftW, r.height);
            Rect right = new Rect(left.xMax + 24f, r.y, r.xMax - left.xMax - 24f, r.height);
            UIKit.Text(new Rect(left.x, left.y, left.width, 26f), "EQUIPPED LOOT SLOTS", 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float cw = (left.width - 30f) / 4f;
            for (int i = 0; i < Slots.Length; i++) DrawCell(new Rect(left.x + i * (cw + 10f), left.y + 30f, cw, 92f), Slots[i], "SLOT " + (i + 1), sel);
            float top = left.y + 140f;
            UIKit.Text(new Rect(left.x, top, left.width, 26f), "BACKPACK  " + Backpack.Count + " / " + BackpackSize, 18, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            // Phase 5 loot world tally (chests opened, satchels found, keys held, bounties), right-aligned on the same row.
            if (Game.LootWorld != null) Game.LootWorld.DrawArsenalTally(new Rect(left.x + 200f, top + 2f, left.width - 200f, 24f));
            const int cols = 6;
            float gw = (left.width - (cols - 1) * 8f) / cols;
            float gh = Mathf.Min(74f, (left.yMax - top - 40f) / 5f - 8f);
            for (int k = 0; k < BackpackSize; k++)
            {
                Rect c = new Rect(left.x + (k % cols) * (gw + 8f), top + 30f + (k / cols) * (gh + 8f), gw, gh);
                DrawCell(c, k < Backpack.Count ? Backpack[k] : null, null, sel);
            }

            if (sel == null)
            {
                string help = "Loot guns drop from enemies in light beams: the taller the beam, the rarer the gun.\n\n" +
                              GInput.Prompt("Press E near a gun to take it to the backpack; hold it to equip it at once.") + "\n\n" +
                              "Pick a gun here to see its card, equip it to one of the four loot slots, mark it as a favorite (favorites cannot be scrapped or sold) or scrap it for materials and weapon cores.\n\n" +
                              "Sell guns at any gun shop (Loot guns tab). Upgrade them at the Forge's ARMORY tab.";
                UIKit.Text(new Rect(right.x + 10f, right.y + 10f, right.width - 20f, right.height - 20f), help, 18, new Color(0.85f, 0.85f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                DrawStatus(right);
                return;
            }
            string cmpLabel;
            CardStats cmp = ArsenalCompare(sel, out cmpLabel);
            Rect card = new Rect(right.x, right.y, right.width, right.height - 190f);
            DrawCard(card, sel, cmp, cmpLabel, 18);
            float by = card.yMax + 10f;
            float bw = (right.width - 30f) / 4f;
            int at = SlotOf(sel);
            for (int i = 0; i < Slots.Length; i++)
            {
                string label = at == i ? "IN SLOT " + (i + 1) : "EQUIP " + (i + 1);
                if (UIKit.Button(new Rect(right.x + i * (bw + 10f), by, bw, 44f), label, at == i, 16) && at != i)
                {
                    Equip(sel, i, false);
                    Status(sel.Name + " is in loot slot " + (i + 1) + ".", true);
                }
            }
            by += 52f;
            if (at >= 0)
            {
                if (UIKit.Button(new Rect(right.x, by, bw, 44f), "UNEQUIP", false, 16))
                {
                    if (Unequip(at)) Status(sel.Name + " moved to the backpack.", true);
                    else Status("The backpack is full.", false);
                }
            }
            if (UIKit.Button(new Rect(right.x + (bw + 10f), by, bw, 44f), sel.Favorite ? "UNFAVORITE" : "FAVORITE", sel.Favorite, 16)) ToggleFavorite(sel);
            string cmpName = _compareSlot < 0 ? "AUTO" : "SLOT " + (_compareSlot + 1);
            if (UIKit.Button(new Rect(right.x + 2f * (bw + 10f), by, bw, 44f), "COMPARE: " + cmpName, false, 14)) _compareSlot = _compareSlot >= Slots.Length - 1 ? -1 : _compareSlot + 1;
            bool armed = _scrapArmedUid == sel.Uid && Time.unscaledTime < _scrapArmedUntil;
            if (UIKit.Button(new Rect(right.x + 3f * (bw + 10f), by, bw, 44f), armed ? "CONFIRM" : "SCRAP", armed, 16))
            {
                if (sel.Favorite) Status("Favorites cannot be scrapped.", false);
                else if (!armed)
                {
                    _scrapArmedUid = sel.Uid;
                    _scrapArmedUntil = Time.unscaledTime + 3f;
                    Status("Press CONFIRM to scrap " + sel.Name + ".", false);
                }
                else
                {
                    string n = sel.Name;
                    if (Scrap(sel)) Status("Scrapped " + n + ".", true);
                    _scrapArmedUid = 0;
                    _selUid = 0;
                    return;
                }
            }
            by += 54f;
            ForgeCost y = LootForgeMath.ScrapYield(sel);
            string gives = "";
            foreach (KeyValuePair<string, int> kv in y.Materials) gives += (gives.Length > 0 ? ", " : "") + kv.Value + " " + MaterialDb.NameOf(kv.Key);
            UIKit.Text(new Rect(right.x, by, right.width, 44f), "Sells for " + MathUtil.Money(LootEconomy.SellValue(sel)) + " at any gun shop.  Scrapping gives " + gives + ".", 15, new Color(0.75f, 0.75f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            DrawStatus(right);
        }

        private CardStats ArsenalCompare(LootGun sel, out string label)
        {
            label = "";
            if (_compareSlot >= 0)
            {
                LootGun c = Slots[_compareSlot];
                if (c == null || c == sel) return new CardStats();
                label = c.Name + " (slot " + (_compareSlot + 1) + ")";
                return StatsOf(c);
            }
            // Auto: the equipped gun of the same archetype, else the gun in hand.
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] != null && Slots[i] != sel && Slots[i].Archetype == sel.Archetype)
                {
                    label = Slots[i].Name + " (slot " + (i + 1) + ")";
                    return StatsOf(Slots[i]);
                }
            }
            PlayerController p = Game.Player;
            if (p != null && p.Weapons.Current.IsGun && p.Weapons.Current.Loot != sel)
            {
                label = p.Weapons.Current.Name;
                return StatsOf(p.Weapons.Current);
            }
            return new CardStats();
        }

        private void DrawStatus(Rect right)
        {
            if (Time.unscaledTime < _statusUntil && !string.IsNullOrEmpty(_status))
                UIKit.Text(new Rect(right.x, right.yMax - 28f, right.width, 26f), _status, 17, _statusGood ? ProgressionUI.Good : ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
        }
    }
}
