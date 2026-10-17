using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The BESTIARY pause page: every species as a button (undiscovered ones as question marks), the chosen entry's
    /// details (behaviour and lore once seen; weak point, element weakness and resistance, health, poise and drops once
    /// scanned; kills), and the material satchel. Every control is a UIKit button, so the mouse and the pad (PadNav)
    /// both work.
    /// </summary>
    public static class BestiaryPage
    {
        private static int _selected;
        private static bool _satchel;
        private static readonly List<KeyValuePair<MaterialDef, int>> _held = new List<KeyValuePair<MaterialDef, int>>();

        public static void ResetStatics()
        {
            _selected = 0;
            _satchel = false;
            _held.Clear();
        }

        public static void Register()
        {
            MenuPages.Register(new MenuPage { Title = "BESTIARY", Order = 30, Draw = Draw });
        }

        public static void Draw(Rect body)
        {
            UIKit.Rect(body, UIKit.Panel);
            Compendium c = Game.Compendium;
            CreatureSpecies[] all = CreatureSpeciesDb.All;
            float x = body.x + 24f, y = body.y + 18f;
            int seen = c != null ? c.DiscoveredCount : 0;
            int scanned = c != null ? c.ScannedCount : 0;
            UIKit.Text(new Rect(x, y, 700f, 34f), "BESTIARY", 30, UIKit.Cyan, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(x + 210f, y + 8f, 600f, 26f), seen + " / " + all.Length + " found, " + scanned + " scanned", 18, new Color(0.8f, 0.9f, 1f, 0.8f), TextAnchor.UpperLeft);
            if (UIKit.Button(new Rect(body.xMax - 470f, y, 210f, 40f), "CREATURES", !_satchel, 18)) _satchel = false;
            if (UIKit.Button(new Rect(body.xMax - 250f, y, 210f, 40f), "MATERIAL SATCHEL", _satchel, 16)) _satchel = true;
            y += 56f;
            Rect content = new Rect(x, y, body.width - 48f, body.yMax - y - 16f);
            if (_satchel) DrawSatchel(content);
            else DrawCreatures(content, c, all);
        }

        private static void DrawCreatures(Rect r, Compendium c, CreatureSpecies[] all)
        {
            _selected = Mathf.Clamp(_selected, 0, all.Length - 1);
            // Two columns of entries on the left, the chosen entry on the right.
            int perCol = Mathf.CeilToInt(all.Length / 2f);
            float rowH = Mathf.Clamp((r.height - 10f) / perCol, 30f, 44f);
            float colW = Mathf.Min(250f, r.width * 0.2f);
            for (int i = 0; i < all.Length; i++)
            {
                CreatureSpecies sp = all[i];
                bool known = c != null && c.IsDiscovered(sp.Id);
                int col = i / perCol, row = i % perCol;
                Rect br = new Rect(r.x + col * (colW + 8f), r.y + row * rowH, colW, rowH - 6f);
                string label = known ? sp.Name : "? ? ?";
                if (UIKit.Button(br, label, i == _selected, rowH > 38f ? 17 : 15)) _selected = i;
                if (known && c.IsScanned(sp.Id)) UIKit.Tex(new Rect(br.xMax - 14f, br.y + 4f, 9f, 9f), UIKit.Circle, UIKit.Cyan);
            }
            Rect d = new Rect(r.x + 2f * (colW + 8f) + 18f, r.y, r.xMax - (r.x + 2f * (colW + 8f) + 18f), r.height);
            DrawEntry(d, c, all[_selected]);
        }

        private static string TierName(CreatureTier t)
        {
            switch (t)
            {
                case CreatureTier.OuterCity: return "Outer City";
                case CreatureTier.InnerWorld: return "Inner World";
                case CreatureTier.Frostline: return "The Frostline";
                default: return "Quartz Badlands";
            }
        }

        /// <summary>The region label of a species: the 04E species live in the Underheart (their stored tier stays Badlands).</summary>
        public static string RegionName(CreatureSpecies sp)
        {
            if (sp == null) return "";
            return UnderheartBestiary04E.IsUnderground(sp.Id) ? UnderheartBestiary04E.RegionName : TierName(sp.Tier);
        }

        /// <summary>The kind label of a species (boss, great beast, temperament, or an Underheart resident).</summary>
        public static string KindName(CreatureSpecies sp)
        {
            if (sp == null) return "";
            if (sp.Boss) return "Boss";
            if (sp.GreatBeast) return "Great beast";
            return UnderheartBestiary04E.IsUnderground(sp.Id) ? UnderheartBestiary04E.KindLabel(sp.Id, sp.Temperament) : sp.Temperament.ToString();
        }

        private static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Volt: return "Volt (lightning)";
                case Element.Ember: return "Ember (fire)";
                case Element.Frost: return "Frost";
                case Element.Void: return "Void";
                default: return "None";
            }
        }

        public static string When(CreatureSpecies sp)
        {
            if (sp.Boss) return "Boss";
            if (UnderheartBestiary04E.IsUnderground(sp.Id)) return UnderheartBestiary04E.WhenLabel;
            if (sp.Day && sp.Night) return "Day and night";
            return sp.Day ? "By day" : "At night";
        }

        private static void DrawEntry(Rect d, Compendium c, CreatureSpecies sp)
        {
            UIKit.Rect(d, new Color(0f, 0f, 0f, 0.35f));
            bool known = c != null && c.IsDiscovered(sp.Id);
            bool scanned = known && c.IsScanned(sp.Id);
            float x = d.x + 24f, y = d.y + 18f, w = d.width - 48f;
            // A glowing emblem in the species' colour (a dim silhouette while undiscovered).
            Rect icon = new Rect(d.xMax - 150f, d.y + 18f, 120f, 120f);
            Color tint = known ? sp.HitColor : new Color(0.25f, 0.27f, 0.3f);
            UIKit.Tex(icon, UIKit.Circle, new Color(tint.r * 0.35f, tint.g * 0.35f, tint.b * 0.35f, 0.9f));
            UIKit.Tex(new Rect(icon.x + 10f, icon.y + 10f, 100f, 100f), UIKit.Ring, new Color(tint.r, tint.g, tint.b, known ? 0.9f : 0.4f));
            UIKit.Text(icon, known ? sp.Name.Substring(0, 1) : "?", 64, new Color(tint.r, tint.g, tint.b, known ? 1f : 0.6f), TextAnchor.MiddleCenter, FontStyle.Bold);
            if (!known)
            {
                UIKit.Text(new Rect(x, y, w - 160f, 40f), "Undiscovered", 30, new Color(0.7f, 0.72f, 0.78f), TextAnchor.UpperLeft, FontStyle.Bold);
                y += 48f;
                UIKit.Text(new Rect(x, y, w - 160f, 28f), "Region: " + RegionName(sp), 20, new Color(0.75f, 0.8f, 0.9f), TextAnchor.UpperLeft);
                y += 36f;
                UIKit.Text(new Rect(x, y, w - 160f, 80f), sp.Boss ? "Something ancient waits in an arena of its own." : "See one up close (within 40 m) to start its entry. Scan it with the Third Eye to learn its weak point.", 19, new Color(0.8f, 0.82f, 0.88f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            UIKit.Text(new Rect(x, y, w - 160f, 40f), sp.Name, 32, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 44f;
            string kind = KindName(sp) + "  |  " + RegionName(sp) + "  |  " + When(sp);
            UIKit.Text(new Rect(x, y, w - 160f, 26f), kind, 18, sp.HitColor, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            int kills = c != null ? c.Kills(sp.Id) : 0;
            UIKit.Text(new Rect(x, y, w - 160f, 26f), "Defeated: " + kills + (scanned ? "   |   Scanned" : "   |   Not scanned"), 18, new Color(0.85f, 0.9f, 1f, 0.85f), TextAnchor.UpperLeft);
            y += 40f;
            // 04E: the Underheart species say where below the street they are met (they have no surface habitat): one
            // compact line (two at most) under the counts, clear of the emblem, so long entries still fit the page.
            string where = UnderheartBestiary04E.HabitatLine(sp.Id);
            if (where.Length > 0)
            {
                string line = "Where found: " + where;
                GUIStyle ws = UIKit.Style(17, FontStyle.Normal, TextAnchor.UpperLeft, true);
                float wh = Mathf.Clamp(ws.CalcHeight(new GUIContent(line), w - 160f), 22f, 44f);
                UIKit.Text(new Rect(x, y - 8f, w - 160f, wh + 2f), line, 17, new Color(0.75f, 0.86f, 0.98f, 0.92f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += wh + 4f;
            }
            y = Para(x, y, w, "Behaviour", sp.Behaviour);
            y = Para(x, y, w, "Lore", sp.Lore);
            y = Para(x, y, w, "Breakable parts", CreaturePartsUi.BestiaryLine(sp, c));
            if (!scanned)
            {
                UIKit.Text(new Rect(x, y, w, 50f), "Hold the Third Eye on one for a moment to scan it: weak point, element and drops.", 18, new Color(0.3f, 0.95f, 1f, 0.9f), TextAnchor.UpperLeft, FontStyle.Italic, true, true);
                return;
            }
            y = Para(x, y, w, "Weak point", sp.Weak);
            string stats = "Weak to " + ElementName(sp.Weakness) + "   |   Resists " + ElementName(sp.Resists) + "   |   Health " + Mathf.RoundToInt(sp.Health) + "   |   Poise " + Mathf.RoundToInt(sp.Poise) + "   |   Lumen " + sp.Lumen;
            UIKit.Text(new Rect(x, y, w, 50f), stats, 17, new Color(1f, 0.85f, 0.45f), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            y += 52f;
            string drops = "";
            for (int i = 0; i < sp.Loot.Length; i++)
            {
                LootRoll l = sp.Loot[i];
                if (drops.Length > 0) drops += ",  ";
                drops += MaterialDb.NameOf(l.Material) + " " + Mathf.RoundToInt(l.Chance * 100f) + "%";
            }
            Para(x, y, w, "Drops", drops.Length > 0 ? drops : "Nothing");
        }

        private static float Para(float x, float y, float w, string head, string text)
        {
            UIKit.Text(new Rect(x, y, w, 24f), head.ToUpper(), 15, new Color(0.6f, 0.75f, 0.9f), TextAnchor.UpperLeft, FontStyle.Bold);
            y += 22f;
            GUIStyle st = UIKit.Style(19, FontStyle.Normal, TextAnchor.UpperLeft, true);
            float h = Mathf.Max(26f, st.CalcHeight(new GUIContent(text), w));
            UIKit.Text(new Rect(x, y, w, h + 4f), text, 19, new Color(0.92f, 0.94f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            return y + h + 14f;
        }

        private static void DrawSatchel(Rect r)
        {
            _held.Clear();
            if (Game.Materials != null) _held.AddRange(Game.Materials.Held());
            if (_held.Count == 0)
            {
                UIKit.Text(new Rect(r.x, r.y, r.width, 60f), "Your satchel is empty. Creatures drop materials when they fall; the Forge turns them into gear.", 20, new Color(0.85f, 0.9f, 1f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            int cols = r.width > 1300f ? 3 : 2;
            float cw = (r.width - (cols - 1) * 12f) / cols;
            float ch = 74f;
            int perPage = Mathf.Max(1, cols * Mathf.FloorToInt(r.height / (ch + 10f)));
            for (int i = 0; i < _held.Count && i < perPage; i++)
            {
                MaterialDef m = _held[i].Key;
                int n = _held[i].Value;
                Rect cell = new Rect(r.x + (i % cols) * (cw + 12f), r.y + (i / cols) * (ch + 10f), cw, ch);
                UIKit.Rect(cell, new Color(0f, 0f, 0f, 0.45f));
                UIKit.Rect(new Rect(cell.x, cell.y, 4f, cell.height), m.RarityColor);
                UIKit.Text(new Rect(cell.x + 14f, cell.y + 6f, cell.width - 100f, 26f), m.Name, 20, m.RarityColor, TextAnchor.UpperLeft, FontStyle.Bold);
                UIKit.Text(new Rect(cell.xMax - 96f, cell.y + 6f, 84f, 26f), "x" + n, 22, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
                UIKit.Text(new Rect(cell.x + 14f, cell.y + 34f, cell.width - 28f, 38f), m.RarityName + ". " + m.Description, 15, new Color(0.85f, 0.88f, 0.95f, 0.85f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            }
            if (_held.Count > perPage)
                UIKit.Text(new Rect(r.x, r.yMax - 24f, r.width, 22f), "+" + (_held.Count - perPage) + " more kinds (see the Forge)", 16, new Color(0.8f, 0.85f, 0.95f, 0.7f), TextAnchor.UpperLeft);
        }
    }
}
