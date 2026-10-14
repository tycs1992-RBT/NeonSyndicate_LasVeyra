using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The GADGETS pause tab: pick a slot, put any unlocked gadget variant in it, socket a gadget mod, salvage spare
    /// mods for Lumen. Every control is a UIKit button, so the mouse, the arrow keys and the controller all work.
    /// </summary>
    public sealed partial class GadgetSystem
    {
        private int _uiSlot;
        private int _uiGadget = -1;
        private int _uiModPage;
        private GadgetModSave _uiMod;
        private const int ModsPerPage = 6;

        private void DrawPage(Rect r)
        {
            float pad = 24f;
            float colW = Mathf.Min(380f, r.width * 0.28f);
            Rect left = new Rect(r.x + pad, r.y + pad, colW, r.height - pad * 2f);
            Rect mid = new Rect(left.xMax + pad, r.y + pad, colW + 30f, r.height - pad * 2f);
            Rect right = new Rect(mid.xMax + pad, r.y + pad, r.xMax - mid.xMax - pad * 2f, r.height - pad * 2f);
            DrawSlotColumn(left);
            DrawGadgetColumn(mid);
            DrawDetailColumn(right);
        }

        private void DrawSlotColumn(Rect r)
        {
            UIKit.Text(new Rect(r.x, r.y, r.width, 30f), "GADGET SLOTS", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float y = r.y + 36f;
            for (int s = 0; s < Slots.Length; s++)
            {
                GadgetSpec spec = SpecIn(s);
                string keys = s == 0 ? "key 1, hold D-pad left" : "key 2, hold D-pad right";
                string label = "Slot " + (s + 1) + "  <size=14>(" + keys + ")</size>\n<size=17>" +
                               (spec != null ? "<color=#" + ColorUtility.ToHtmlStringRGB(spec.Color) + ">" + spec.FullName + "</color>" : "<color=#888888>empty</color>") +
                               "</size>\n<size=14>" + (SlotMods[s] != null ? "Mod: " + ModName(SlotMods[s]) : "<color=#888888>no mod</color>") + "</size>";
                if (UIKit.Button(new Rect(r.x, y, r.width, 86f), label, _uiSlot == s, 18)) _uiSlot = s;
                y += 94f;
            }
            GadgetSpec cur = SpecIn(_uiSlot);
            y += 6f;
            if (cur != null)
            {
                int lvl = PlayerLevel;
                string stats = "<b>Slot " + (_uiSlot + 1) + " now</b>\n" +
                               "Cooldown " + CooldownFor(_uiSlot).ToString("0.0") + " s (base " + cur.Cooldown.ToString("0") + " s)\n" + StatLine(cur, _uiSlot, lvl);
                UIKit.Text(new Rect(r.x, y, r.width, 150f), stats, 16, new Color(0.9f, 0.9f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 150f;
                if (UIKit.Button(new Rect(r.x, y, r.width * 0.48f, 42f), "Empty slot", false, 17)) Equip(_uiSlot, -1);
                if (SlotMods[_uiSlot] != null && UIKit.Button(new Rect(r.x + r.width * 0.52f, y, r.width * 0.48f, 42f), "Remove mod", false, 17))
                {
                    SlotMods[_uiSlot] = null;
                    RaiseChanged();
                }
            }
            else UIKit.Text(new Rect(r.x, y, r.width, 60f), "Pick a gadget on the right to put it in this slot.", 16, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
        }

        private string StatLine(GadgetSpec s, int slot, int level)
        {
            float power = GadgetRules.Power(s, level, PowerBonus(slot));
            float life = GadgetRules.Duration(s, DurationBonus(slot));
            float radius = GadgetRules.Radius(s, RadiusBonus(slot));
            float reach = s.Reach * (1f + RadiusBonus(slot));
            switch (s.Kind)
            {
                case GadgetKind.Turret: return (s.Variant == 0 ? Mathf.RoundToInt(power) + " damage per shot, " + (1f / s.Interval).ToString("0") + " shots a second" : Mathf.RoundToInt(power / s.Interval) + " fire damage a second in a cone, sets enemies burning") + "\nLasts " + life.ToString("0") + " s, reach " + reach.ToString("0") + " m";
                case GadgetKind.Mine: return (s.Variant == 0 ? "" : GadgetRules.LitterCount + " seekers, ") + Mathf.RoundToInt(power) + " blast damage in " + radius.ToString("0.0") + " m\nSeeks " + reach.ToString("0") + " m for up to " + life.ToString("0") + " s";
                case GadgetKind.Drone: return (s.Variant == 0 ? Mathf.RoundToInt(power) + " damage per zap, reach " + reach.ToString("0") + " m" : Mathf.RoundToInt(power) + " health and " + GadgetRules.SutureArmorPerSecond.ToString("0") + " armor a second") + "\nLasts " + life.ToString("0") + " s";
                case GadgetKind.Barrier: return Mathf.RoundToInt(GadgetRules.ShieldHealth(s, level, PowerBonus(slot))) + " shield points\nLasts " + life.ToString("0") + " s";
                case GadgetKind.Scanner: return (s.Variant == 0 ? "Marks everyone within " + radius.ToString("0") + " m" : "Marks and exposes (+" + Mathf.RoundToInt(GadgetRules.ExposeBonus * 100f) + "% damage) along " + reach.ToString("0") + " m") + "\nMarks last " + life.ToString("0") + " s";
                default: return (s.Variant == 0 ? Mathf.RoundToInt(power) + " healing a second" : Mathf.RoundToInt(power / s.Interval) + " fire damage a second, sets enemies burning") + " in " + radius.ToString("0.0") + " m\nLasts " + life.ToString("0") + " s";
            }
        }

        private void DrawGadgetColumn(Rect r)
        {
            UIKit.Text(new Rect(r.x, r.y, r.width, 30f), "GADGETS", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            float y = r.y + 36f;
            float bw = (r.width - 10f) * 0.5f;
            for (int k = 0; k < GadgetRules.KindCount; k++)
            {
                GadgetSpec first = GadgetRules.Get((GadgetKind)k, 0);
                UIKit.Text(new Rect(r.x, y, r.width, 22f), first.Name, 16, first.Color, TextAnchor.UpperLeft, FontStyle.Bold);
                y += 22f;
                for (int v = 0; v < GadgetRules.VariantCount; v++)
                {
                    int id = GadgetRules.IdOf((GadgetKind)k, v);
                    GadgetSpec s = GadgetRules.Get(id);
                    bool open = IsUnlocked(id);
                    int inSlot = System.Array.IndexOf(Slots, id);
                    string label = open ? s.VariantName + (inSlot >= 0 ? "  <size=13>[slot " + (inSlot + 1) + "]</size>" : "") : "<color=#777777>" + s.VariantName + "  <size=13>level " + GadgetRules.UnlockLevel(id) + "</size></color>";
                    if (UIKit.Button(new Rect(r.x + v * (bw + 10f), y, bw, 40f), label, _uiGadget == id, 17))
                    {
                        if (_uiGadget == id && open) Equip(_uiSlot, id);
                        _uiGadget = id;
                    }
                }
                y += 48f;
            }
        }

        private void DrawDetailColumn(Rect r)
        {
            float y = r.y;
            GadgetSpec s = GadgetRules.Get(_uiGadget);
            if (s != null)
            {
                UIKit.Rect(new Rect(r.x, y, r.width, 4f), s.Color);
                UIKit.Text(new Rect(r.x, y + 8f, r.width, 30f), s.FullName, 24, s.Color, TextAnchor.UpperLeft, FontStyle.Bold);
                y += 40f;
                UIKit.Text(new Rect(r.x, y, r.width, 50f), s.Blurb, 16, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 52f;
                bool open = IsUnlocked(s.Id);
                UIKit.Text(new Rect(r.x, y, r.width, 44f), "Cooldown " + s.Cooldown.ToString("0") + " s.  " + (open ? "Unlocked." : "Unlocks at level " + GadgetRules.UnlockLevel(s.Id) + " (you are level " + PlayerLevel + ")."), 15, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 30f;
                if (open && UIKit.Button(new Rect(r.x, y, Mathf.Min(300f, r.width), 42f), "Put in slot " + (_uiSlot + 1), false, 18)) Equip(_uiSlot, s.Id);
                y += 54f;
            }
            else
            {
                UIKit.Text(new Rect(r.x, y, r.width, 60f), "Pick a gadget to see what it does. Click it again (or press A again) to put it in the chosen slot.", 16, new Color(0.8f, 0.8f, 0.8f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                y += 70f;
            }
            UIKit.Text(new Rect(r.x, y, r.width, 28f), "GADGET MODS (" + Mods.Count + "/" + GadgetRules.ModCap + ")", 20, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 30f;
            if (Mods.Count == 0)
            {
                UIKit.Text(new Rect(r.x, y, r.width, 60f), "Gadget mods drop from kills: elites, champions and bosses most often. Walk over the little chip to take it.", 15, new Color(0.75f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                return;
            }
            int pages = Mathf.Max(1, (Mods.Count + ModsPerPage - 1) / ModsPerPage);
            _uiModPage = Mathf.Clamp(_uiModPage, 0, pages - 1);
            for (int i = _uiModPage * ModsPerPage; i < Mathf.Min(Mods.Count, (_uiModPage + 1) * ModsPerPage); i++)
            {
                GadgetModSave m = Mods[i];
                int socketed = System.Array.IndexOf(SlotMods, m);
                string label = ModName(m) + "  <size=13>" + GadgetRules.StatText((GadgetModStat)m.Stat, m.Value) + (socketed >= 0 ? "  [slot " + (socketed + 1) + "]" : "") + "</size>";
                if (UIKit.Button(new Rect(r.x, y, r.width, 38f), label, _uiMod == m, 15)) _uiMod = m;
                y += 42f;
            }
            if (pages > 1)
            {
                if (UIKit.Button(new Rect(r.x, y, r.width * 0.48f, 36f), "Prev page", false, 16)) _uiModPage = (_uiModPage + pages - 1) % pages;
                if (UIKit.Button(new Rect(r.x + r.width * 0.52f, y, r.width * 0.48f, 36f), "Next page", false, 16)) _uiModPage = (_uiModPage + 1) % pages;
                y += 42f;
            }
            if (_uiMod != null && Mods.Contains(_uiMod))
            {
                float bw = (r.width - 10f) * 0.5f;
                if (UIKit.Button(new Rect(r.x, y, bw, 42f), "Socket in slot " + (_uiSlot + 1), false, 17)) Socket(_uiSlot, _uiMod);
                if (UIKit.Button(new Rect(r.x + bw + 10f, y, bw, 42f), "Salvage +" + GadgetRules.ModSalvageValue((Rarity)_uiMod.Rarity) + " Lumen", false, 16))
                {
                    SalvageMod(_uiMod);
                    _uiMod = null;
                }
            }
        }
    }
}
