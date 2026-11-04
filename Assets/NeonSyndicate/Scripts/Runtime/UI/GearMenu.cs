using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Gear screen (pause menu tab and shrine menu): six equipment slots, the inventory for the chosen slot,
    /// item details compared with what is equipped, equip, salvage for Lumen and (at a shrine) upgrades.
    /// Every control is a UIKit button, so mouse, arrow keys and the controller all work.
    /// </summary>
    public static partial class GearMenu
    {
        private static GearSlot _slot = GearSlot.Shell;
        private static int _page;
        private static GearItem _selected;
        public static bool AtShrine;
        private const int PerPage = 8;

        public static void Draw(Rect r)
        {
            GearSystem g = Game.Gear;
            if (g == null)
            {
                UIKit.Text(new Rect(r.x + 40f, r.y + 40f, r.width - 80f, 40f), "Gear is not available.", 24, Color.white, TextAnchor.UpperLeft);
                return;
            }
            float pad = 24f;
            float colW = Mathf.Min(360f, r.width * 0.27f);
            Rect slots = new Rect(r.x + pad, r.y + pad, colW, r.height - pad * 2f);
            Rect list = new Rect(slots.xMax + pad, r.y + pad, colW + 40f, r.height - pad * 2f);
            Rect detail = new Rect(list.xMax + pad, r.y + pad, r.xMax - list.xMax - pad * 2f, r.height - pad * 2f);
            DrawSlots(slots, g);
            DrawList(list, g);
            DrawDetail(detail, g);
        }

        private static void DrawSlots(Rect r, GearSystem g)
        {
            UIKit.Text(new Rect(r.x, r.y, r.width, 30f), "EQUIPPED", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x, r.y + 2f, r.width, 28f), "GEAR SCORE " + GearScore.Current, 18, UIKit.Gold, TextAnchor.UpperRight, FontStyle.Bold);
            // H8S: how far the gear score has come toward the next city tier.
            ProgressionReadoutUiH8S.DrawTierBar(new Rect(r.x + r.width * 0.5f, r.y + 29f, r.width * 0.5f, 4f), GearScore.Current);
            float y = r.y + 36f;
            for (int s = 0; s < GearSystem.SlotCount; s++)
            {
                GearSlot slot = (GearSlot)s;
                GearItem it = g.Equipped[s];
                string label = GearSystem.SlotName(slot) + "\n<size=16>" + (it != null ? "<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(it.Rarity)) + ">" + it.Name + (it.Upgrade > 0 ? " +" + it.Upgrade : "") + "</color>" : "<color=#888888>empty</color>") + "</size>";
                if (UIKit.Button(new Rect(r.x, y, r.width, 62f), label, _slot == slot, 20))
                {
                    _slot = slot;
                    _page = 0;
                    _selected = it;
                }
                y += 70f;
            }
            // Character totals.
            PlayerController p = Game.Player;
            y += 8f;
            if (p != null)
            {
                string stats =
                    "Health " + Mathf.RoundToInt(p.Health.MaxHealth) + "   Stamina " + Mathf.RoundToInt(p.Combat != null ? p.Combat.MaxStamina : 100f) + "\n" +
                    "Melee x" + (g.MeleeMultiplierPreview * (Game.Progress != null ? Game.Progress.MeleeMultiplier : 1f)).ToString("0.00") + "   Guns x" + (g.GunMultiplier * (Game.Progress != null ? Game.Progress.GunMultiplier : 1f)).ToString("0.00") + "\n" +
                    "Damage taken x" + (g.ArmorMultiplier * CombatDirector.PlayerDamageTaken).ToString("0.00") + "   Tonic x" + g.HealMultiplier.ToString("0.00");
                int tier = GearScore.Tier;
                stats += "\n<color=#" + ColorUtility.ToHtmlStringRGB(GadgetSystem.TierColor(tier)) + "><b>" + GearScoreRules.TierLabel(tier) + "</b></color>" +
                         "\n<size=14>Enemy level " + GearScoreRules.EnemyLevel(Game.Progress != null ? Game.Progress.Level : 1, tier) + ", loot +" + GearScoreRules.LootLevelBonus(tier) + " item levels" +
                         TierHintH8S(GearScore.Current) + "</size>";
                for (int s = 0; s < GearSystem.SetNames.Length; s++)
                {
                    int n = g.SetPieces(s);
                    if (n <= 0) continue;
                    stats += "\n<color=#9fe8ff>" + GearSystem.SetNames[s] + SetCountTextH8S(s, n) + "</color>";
                }
                // Active set bonuses, one line each, so a build reads at a glance.
                List<string> active = GearBuildRules.ActiveLines(BuildPieces(g), GearSystem.SetNames, GearSystem.SetBonus2, GearSystem.SetBonus3, GearSystem.SetBonus4);
                if (active.Count > 0) stats += "\n<size=14><color=#6dff8a>ACTIVE: " + string.Join("\nACTIVE: ", active) + "</color></size>";
                UIKit.Text(new Rect(r.x, y, r.width, r.yMax - y), stats, 17, new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            }
        }

        private static readonly List<int> _pieces = new List<int>();

        private static List<int> BuildPieces(GearSystem g)
        {
            _pieces.Clear();
            for (int s = 0; s < GearSystem.SetNames.Length; s++) _pieces.Add(g.SetPieces(s));
            return _pieces;
        }

        private static readonly List<GearItem> _filtered = new List<GearItem>();

        private static void DrawList(Rect r, GearSystem g)
        {
            _filtered.Clear();
            foreach (GearItem it in g.Items) if (it.Slot == _slot) _filtered.Add(it);
            _filtered.Sort((a, b) => b.Rarity != a.Rarity ? ((int)b.Rarity).CompareTo((int)a.Rarity) : b.Power.CompareTo(a.Power));
            int pages = Mathf.Max(1, (_filtered.Count + PerPage - 1) / PerPage);
            _page = Mathf.Clamp(_page, 0, pages - 1);
            UIKit.Text(new Rect(r.x, r.y, r.width, 30f), GearSystem.SlotName(_slot).ToUpper() + "  (" + _filtered.Count + ")", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float y = r.y + 36f;
            if (_filtered.Count == 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 80f), "Nothing here yet. Shard-Touched elites always drop gear; gangs, police and hunters sometimes do.", 18, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
            }
            for (int i = _page * PerPage; i < Mathf.Min(_filtered.Count, (_page + 1) * PerPage); i++)
            {
                GearItem it = _filtered[i];
                bool eq = g.IsEquipped(it);
                string label = "<color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.RarityColor(it.Rarity)) + ">" + it.Name + (it.Upgrade > 0 ? " +" + it.Upgrade : "") + "</color>" + (eq ? "  <size=14>[equipped]</size>" : "");
                if (UIKit.Button(new Rect(r.x, y, r.width, 44f), label, _selected == it, 17)) _selected = it;
                y += 50f;
            }
            if (pages > 1)
            {
                float by = r.y + 36f + PerPage * 50f + 6f;
                if (UIKit.Button(new Rect(r.x, by, r.width * 0.48f, 40f), "Prev page", false, 18)) _page = (_page + pages - 1) % pages;
                if (UIKit.Button(new Rect(r.x + r.width * 0.52f, by, r.width * 0.48f, 40f), "Next page", false, 18)) _page = (_page + 1) % pages;
            }
        }

        private static void DrawDetail(Rect r, GearSystem g)
        {
            GearItem it = _selected;
            if (it == null || !g.Items.Contains(it))
            {
                _selected = null;
                UIKit.Text(new Rect(r.x, r.y, r.width, 120f), "Pick an item to see its stats.\n\nLumen: " + (Game.Progress != null ? Game.Progress.Lumen.ToString("N0") : "0"), 20, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                return;
            }
            Color rc = GearSystem.RarityColor(it.Rarity);
            UIKit.Rect(new Rect(r.x, r.y, r.width, 4f), rc);
            UIKit.Text(new Rect(r.x, r.y + 10f, r.width, 34f), it.Name + (it.Upgrade > 0 ? " +" + it.Upgrade : ""), 26, rc, TextAnchor.UpperLeft, FontStyle.Bold);
            string sub = GearSystem.RarityName(it.Rarity) + " " + GearSystem.SlotName(it.Slot) + "   Power " + it.Power + "   Score " + GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity);
            if (it.Element != Element.None) sub += "   <color=#" + ColorUtility.ToHtmlStringRGB(GearSystem.ElementColor(it.Element)) + ">" + GearSystem.ElementName(it.Element) + "</color>";
            UIKit.Text(new Rect(r.x, r.y + 46f, r.width, 26f), sub, 18, new Color(0.85f, 0.85f, 0.85f), TextAnchor.UpperLeft);
            float y = r.y + 82f;
            GearItem cur = g.Equipped[(int)it.Slot];
            y = DrawVerdictH8S(r.x + 8f, y, r.width - 16f, it, cur, g);
            foreach (Affix a in it.Affixes)
            {
                float v = a.Value * it.UpgradeScale;
                string line = GearSystem.AffixText(a.Type, v);
                if (cur != null && cur != it)
                {
                    float cv = cur.Get(a.Type);
                    float diff = v - cv;
                    if (Mathf.Abs(diff) > 0.0005f) line += diff > 0f ? "  <color=#6dff8a>(better)</color>" : "  <color=#ff7a6a>(worse)</color>";
                }
                UIKit.Text(new Rect(r.x + 8f, y, r.width - 16f, 26f), line, 19, Color.white, TextAnchor.UpperLeft);
                y += 28f;
            }
            if (it.Element != Element.None)
            {
                UIKit.Text(new Rect(r.x + 8f, y, r.width - 16f, 26f), ElementBlurb(it.Element), 17, GearSystem.ElementColor(it.Element), TextAnchor.UpperLeft);
                y += 28f;
            }
            if (it.Set >= 0)
            {
                int have = g.SetPieces(it.Set);
                y += 6f;
                string three = GearSystem.SetBonus(it.Set, 3);
                string setText = "<b>" + GearSystem.SetNames[it.Set] + "</b> set (" + have + "/4 worn)\n" + SetLine(2, have, GearSystem.SetBonus(it.Set, 2)) +
                                 (three.Length > 0 ? "\n" + SetLine(3, have, three) : "") + "\n" + SetLine(4, have, GearSystem.SetBonus(it.Set, 4));
                UIKit.Text(new Rect(r.x + 8f, y, r.width - 16f, 100f), setText, 16, new Color(0.62f, 0.9f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += three.Length > 0 ? 96f : 76f;
            }
            // H8S: what equipping it would take away (affixes, set bonuses) or turn on, clear of the Lumen line and buttons.
            DrawEquipChangesH8S(r.x + 8f, y + 4f, r.width - 16f, r.yMax - 100f);
            float by = r.yMax - 60f;
            float bw = Mathf.Min(220f, (r.width - 30f) / 3f);
            bool eq = g.IsEquipped(it);
            if (UIKit.Button(new Rect(r.x, by, bw, 50f), eq ? "Unequip" : "Equip", false, 20))
            {
                if (eq) g.Unequip(it.Slot);
                else g.Equip(it);
            }
            if (UIKit.Button(new Rect(r.x + bw + 15f, by, bw, 50f), "Salvage +" + GearSystem.SalvageValue(it), false, 18))
            {
                g.Salvage(it);
                _selected = null;
            }
            if (AtShrine)
            {
                int cost = GearSystem.UpgradeCost(it);
                bool max = it.Upgrade >= GearSystem.MaxUpgrade;
                if (UIKit.Button(new Rect(r.x + (bw + 15f) * 2f, by, bw, 50f), max ? "Fully upgraded" : "Upgrade (" + cost + ")", false, 18) && !max)
                {
                    if (!g.TryUpgrade(it) && Game.Hud != null) Game.Hud.CombatText("Not enough Lumen", new Color(0.9f, 0.6f, 0.6f));
                }
            }
            else UIKit.Text(new Rect(r.x + (bw + 15f) * 2f, by, bw, 50f), "Upgrade at a shrine", 16, new Color(0.7f, 0.7f, 0.7f), TextAnchor.MiddleCenter);
            UIKit.Text(new Rect(r.x, by - 34f, r.width, 26f), "Lumen: " + (Game.Progress != null ? Game.Progress.Lumen.ToString("N0") : "0"), 20, new Color(0.8f, 0.65f, 1f), TextAnchor.UpperLeft, FontStyle.Bold);
        }

        /// <summary>One set bonus line: green when that many pieces are worn, grey otherwise.</summary>
        private static string SetLine(int pieces, int have, string text)
        {
            return (have >= pieces ? "<color=#6dff8a>" : "<color=#8aa4ad>") + pieces + " pieces: " + text + "</color>";
        }

        private static string ElementBlurb(Element e)
        {
            switch (e)
            {
                case Element.Volt: return "Volt: melee hits can arc lightning into another enemy.";
                case Element.Ember: return "Ember: melee hits can set enemies burning.";
                case Element.Frost: return "Frost: melee hits can shatter an enemy's poise.";
                case Element.Void: return "Void: melee hits can drain health back to you.";
                default: return "";
            }
        }
    }
}
