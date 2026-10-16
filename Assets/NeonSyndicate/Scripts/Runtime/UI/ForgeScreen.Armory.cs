using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Forge's ARMORY tab for loot guns: infuse or change an element with an elemental weapon core, ascend a gun one
    /// rarity step (one part rerolled, every trait kept), raise its item level to yours, and scrap guns into cores and
    /// materials. Every change shows the gun now and after side by side before anything is paid, so no stat ever drops
    /// by surprise. Costs scale with rarity; the Forgewrights' fee discount applies to the cash.
    /// </summary>
    public sealed partial class ForgeScreen
    {
        private static readonly string[] ArmoryModes = { "INFUSE", "ASCEND", "RAISE LEVEL", "SCRAP" };
        private int _armoryUid;
        private int _armoryMode;
        private Element _armoryElement = Element.Volt;
        private int _armoryPage;
        private int _armoryScrapUid;
        private float _armoryScrapUntil;
        private readonly List<LootGun> _armoryGuns = new List<LootGun>();

        private void DrawArmory(Rect left, Rect right, CraftingSystem c)
        {
            LootArmory la = Game.Loot;
            if (la == null)
            {
                UIKit.Text(new Rect(left.x, left.y, left.width, 30f), "The armory is not available.", 20, Color.white, TextAnchor.UpperLeft);
                return;
            }
            _armoryGuns.Clear();
            for (int i = 0; i < la.Slots.Length; i++) if (la.Slots[i] != null) _armoryGuns.Add(la.Slots[i]);
            _armoryGuns.AddRange(la.Backpack);
            int waiting = Game.Arsenal != null ? Game.Arsenal.PendingCount : 0;
            Label(left.x, left.y, left.width, "YOUR LOOT GUNS (" + _armoryGuns.Count + ")" + (waiting > 0 ? "   " + waiting + " CRYSTAL REWARD" + (waiting == 1 ? "" : "S") + " WAITING" : ""));
            const int perPage = 9;
            int pages = Mathf.Max(1, (_armoryGuns.Count + perPage - 1) / perPage);
            _armoryPage = Mathf.Clamp(_armoryPage, 0, pages - 1);
            float y = left.y + 30f;
            LootGun sel = la.Find(_armoryUid);
            for (int k = _armoryPage * perPage; k < _armoryGuns.Count && k < (_armoryPage + 1) * perPage; k++)
            {
                LootGun g = _armoryGuns[k];
                Rect row = new Rect(left.x, y, left.width, 50f);
                bool hot, focused;
                if (ProgressionUI.Hotspot(row, out hot, out focused)) _armoryUid = g.Uid;
                Color rc = GearSystem.RarityColor(g.Rarity);
                UIKit.Rect(row, new Color(1f, 1f, 1f, g == sel ? 0.14f : 0.05f));
                UIKit.Rect(new Rect(row.x, row.y, 5f, row.height), rc);
                if (focused) UIKit.Frame(row, UIKit.Cyan, 3f);
                int slot = la.SlotOf(g);
                UIKit.Text(new Rect(row.x + 14f, row.y + 3f, row.width - 24f, 24f), g.Name + (slot >= 0 ? "  <color=#9fd7ff>(slot " + (slot + 1) + ")</color>" : ""), 17, rc, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                UIKit.Text(new Rect(row.x + 14f, row.y + 27f, row.width - 24f, 20f), LootArmory.KindLine(g) + (g.Element != Element.None ? ", " + GearSystem.ElementName(g.Element) : ""), 13, new Color(0.78f, 0.78f, 0.82f), TextAnchor.UpperLeft);
                y += 56f;
            }
            if (_armoryGuns.Count == 0) UIKit.Text(new Rect(left.x, y, left.width, 60f), "No loot guns yet. Enemies drop them in beams of light.", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            if (pages > 1)
            {
                float py = left.y + 30f + perPage * 56f;
                if (UIKit.Button(new Rect(left.x, py, 130f, 40f), "Previous", false, 15)) _armoryPage = (_armoryPage - 1 + pages) % pages;
                UIKit.Text(new Rect(left.x + 140f, py + 8f, 120f, 26f), "Page " + (_armoryPage + 1) + " / " + pages, 16, Color.white, TextAnchor.UpperCenter);
                if (UIKit.Button(new Rect(left.x + 270f, py, 130f, 40f), "Next", false, 15)) _armoryPage = (_armoryPage + 1) % pages;
            }

            float bx = right.x + 16f;
            float bw = (right.width - 32f - 30f) / 4f;
            for (int i = 0; i < ArmoryModes.Length; i++)
                if (UIKit.Button(new Rect(bx + i * (bw + 10f), right.y + 12f, bw, 42f), ArmoryModes[i], _armoryMode == i, 16)) _armoryMode = i;
            Rect body = new Rect(right.x + 16f, right.y + 66f, right.width - 32f, right.height - 78f);
            if (sel == null)
            {
                UIKit.Text(new Rect(body.x, body.y, body.width, 120f), "Pick a gun on the left.\n\nINFUSE gives it an element with an elemental weapon core. ASCEND raises its rarity one step. RAISE LEVEL brings it up to your level. SCRAP breaks it into cores and materials.", 18, new Color(0.85f, 0.85f, 0.88f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                DrawCrystalCollect(new Rect(body.x, body.y + 140f, body.width, body.height - 140f));
                return;
            }
            switch (_armoryMode)
            {
                case 0: DrawInfuse(body, c, la, sel); break;
                case 1: DrawAscend(body, c, la, sel); break;
                case 2: DrawLevelUp(body, c, la, sel); break;
                default: DrawScrapGun(body, la, sel); break;
            }
        }

        /// <summary>Now and after side by side: names, kinds, stats (after shows the differences) and the traits.</summary>
        private static float DrawBeforeAfter(Rect r, LootGun now, LootGun after)
        {
            float colW = (r.width - 20f) * 0.5f;
            float ya = r.y, yb = r.y;
            UIKit.Text(new Rect(r.x, ya, colW, 22f), "NOW", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + colW + 20f, yb, colW, 22f), "AFTER", 16, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            ya += 24f;
            yb += 24f;
            UIKit.Text(new Rect(r.x, ya, colW, 44f), now.Name + "\n" + LootArmory.KindLine(now), 15, GearSystem.RarityColor(now.Rarity), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            UIKit.Text(new Rect(r.x + colW + 20f, yb, colW, 44f), after.Name + "\n" + LootArmory.KindLine(after), 15, GearSystem.RarityColor(after.Rarity), TextAnchor.UpperLeft, FontStyle.Bold, true, true);
            ya += 48f;
            yb += 48f;
            ya = LootArmory.DrawStatRows(r.x, ya, colW, LootArmory.StatsOf(now), new LootArmory.CardStats(), 15);
            yb = LootArmory.DrawStatRows(r.x + colW + 20f, yb, colW, LootArmory.StatsOf(after), LootArmory.StatsOf(now), 15);
            string en = now.Element != Element.None ? GearSystem.ElementName(now.Element) + " " + Mathf.RoundToInt(now.Stats.ElementChance * 100f) + "%" : "No element";
            string ea = after.Element != Element.None ? GearSystem.ElementName(after.Element) + " " + Mathf.RoundToInt(after.Stats.ElementChance * 100f) + "%" : "No element";
            UIKit.Text(new Rect(r.x, ya + 2f, colW, 20f), en, 15, now.Element != Element.None ? GearSystem.ElementColor(now.Element) : Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            UIKit.Text(new Rect(r.x + colW + 20f, yb + 2f, colW, 20f), ea, 15, after.Element != Element.None ? GearSystem.ElementColor(after.Element) : Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            ya += 22f;
            yb += 22f;
            UIKit.Text(new Rect(r.x, ya, colW, 40f), now.Traits.Count > 0 ? LootGenerator.TraitLine(now) : "No traits", 14, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            string ta = "";
            for (int i = 0; i < after.Traits.Count; i++)
            {
                string t = LootGenerator.TraitName(after.Traits[i]);
                if (!now.HasTrait(after.Traits[i])) t = "<color=#" + ProgressionUI.Hex(ProgressionUI.Good) + ">" + t + " (new)</color>";
                ta += (i > 0 ? ", " : "") + t;
            }
            UIKit.Text(new Rect(r.x + colW + 20f, yb, colW, 40f), ta.Length > 0 ? ta : "No traits", 14, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float bottom = Mathf.Max(ya, yb) + 46f;
            // 04D: a crystal focus keeps its fire behavior through every Forge change; say how it fires and where it came from.
            CrystalCardLines fl = CrystalCardRules.Lines(after);
            if (fl.Valid)
            {
                string src = CrystalArsenal.SourceOf(now);
                string line = "Crystal focus: " + fl.FireLine + " " + fl.DpsLine + "." + (string.IsNullOrEmpty(src) ? "" : " Source: " + src + ".");
                UIKit.Text(new Rect(r.x, bottom - 6f, r.width, 62f), line, 14, new Color(0.6f, 0.92f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                bottom += 58f;
            }
            return bottom;
        }

        private bool PayAndApply(CraftingSystem c, ForgeCost cost, LootArmory la, LootGun now, LootGun after, string done)
        {
            // 04D: a change that would lose a crystal focus's behavior or give it a trait is refused before any payment.
            if (!CrystalForgeRules.KeepsBehavior(now, after))
            {
                Status("Refused before payment: a crystal focus keeps its behavior and takes no traits.", false);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.4f);
                return false;
            }
            if (c == null || !ForgeMath.TryPay(cost, c.Wallet))
            {
                Status("You are missing something for that (see COST).", false);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIBack, 0.4f);
                return false;
            }
            la.Replace(now, after);
            _armoryUid = after.Uid;
            Status(done, true);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.7f, 1.1f);
            return true;
        }

        private void DrawInfuse(Rect body, CraftingSystem c, LootArmory la, LootGun g)
        {
            float bw = (body.width - 30f) / 4f;
            for (int i = 1; i <= 4; i++)
            {
                Element e = (Element)i;
                if (UIKit.Button(new Rect(body.x + (i - 1) * (bw + 10f), body.y, bw, 40f), GearSystem.ElementName(e), _armoryElement == e, 16)) _armoryElement = e;
            }
            float y = body.y + 50f;
            if (!LootForgeMath.CanInfuse(g, _armoryElement))
            {
                UIKit.Text(new Rect(body.x, y, body.width, 40f), g.Name + " already carries " + GearSystem.ElementName(_armoryElement) + ". Pick another element to change it.", 17, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            LootGun after = LootForgeMath.Infused(g, _armoryElement);
            string infuseNote;
            if (!CrystalForgeRules.Check(g, CrystalForgeOp.Infuse, out infuseNote))
            {
                UIKit.Text(new Rect(body.x, y, body.width, 40f), infuseNote, 17, ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                return;
            }
            if (infuseNote.Length > 0)
            {
                UIKit.Text(new Rect(body.x, y, body.width, 22f), infuseNote, 15, new Color(0.6f, 0.92f, 1f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                y += 24f;
            }
            y = DrawBeforeAfter(new Rect(body.x, y, body.width, body.height), g, after);
            ForgeCost cost = ForgeMath.WithDiscount(LootForgeMath.InfuseCost(g, _armoryElement), c != null ? c.FeeDiscount : 0f);
            ForgeCost basePrice = LootForgeMath.InfuseCost(g, _armoryElement);
            y = DrawCost(body.x, y, body.width * 0.6f, cost, basePrice, c != null ? c.Wallet : null);
            if (UIKit.Button(new Rect(body.xMax - 220f, body.yMax - 50f, 220f, 48f), "INFUSE", false, 20))
                PayAndApply(c, cost, la, g, after, after.Name + " now carries " + GearSystem.ElementName(_armoryElement) + ".");
        }

        private void DrawAscend(Rect body, CraftingSystem c, LootArmory la, LootGun g)
        {
            if (!LootForgeMath.CanAscend(g))
            {
                UIKit.Text(new Rect(body.x, body.y, body.width, 40f), g.Name + " is already Mythic: nothing is rarer.", 18, Color.white, TextAnchor.UpperLeft);
                return;
            }
            LootGun after = LootForgeMath.Ascended(g);
            int part = LootForgeMath.AscendPart(g);
            string ascendNote;
            if (!CrystalForgeRules.Check(g, CrystalForgeOp.Ascend, out ascendNote))
            {
                UIKit.Text(new Rect(body.x, body.y, body.width, 40f), ascendNote, 17, ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                return;
            }
            string traitNote = ascendNote.Length > 0 ? " " + ascendNote : " Every trait is kept.";
            UIKit.Text(new Rect(body.x, body.y, body.width, 26f), "Rerolls the " + LootGenerator.PartNames[part].ToLowerInvariant() + ": " + LootGenerator.PartName((LootPart)part, g.Parts[part], g.Archetype) + " becomes " + LootGenerator.PartName((LootPart)part, after.Parts[part], g.Archetype) + "." + traitNote, 15, new Color(0.85f, 0.85f, 0.9f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
            float y = DrawBeforeAfter(new Rect(body.x, body.y + 30f, body.width, body.height), g, after);
            ForgeCost basePrice = LootForgeMath.AscendCost(g);
            ForgeCost cost = ForgeMath.WithDiscount(basePrice, c != null ? c.FeeDiscount : 0f);
            DrawCost(body.x, y, body.width * 0.6f, cost, basePrice, c != null ? c.Wallet : null);
            if (UIKit.Button(new Rect(body.xMax - 220f, body.yMax - 50f, 220f, 48f), "ASCEND", false, 20))
                PayAndApply(c, cost, la, g, after, g.Name + " ascended to " + GearSystem.RarityName(after.Rarity) + ".");
        }

        private void DrawLevelUp(Rect body, CraftingSystem c, LootArmory la, LootGun g)
        {
            int target = Mathf.Clamp(LootArmory.PlayerLevel, 1, LootGenerator.MaxLevel);
            if (target <= g.Level)
            {
                UIKit.Text(new Rect(body.x, body.y, body.width, 60f), g.Name + " is level " + g.Level + ", already at or above your level (" + LootArmory.PlayerLevel + "). Level up and come back.", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, true, true);
                return;
            }
            LootGun after = LootForgeMath.Leveled(g, target);
            float y = DrawBeforeAfter(body, g, after);
            ForgeCost basePrice = LootForgeMath.LevelCost(g, target);
            ForgeCost cost = ForgeMath.WithDiscount(basePrice, c != null ? c.FeeDiscount : 0f);
            DrawCost(body.x, y, body.width * 0.6f, cost, basePrice, c != null ? c.Wallet : null);
            if (UIKit.Button(new Rect(body.xMax - 220f, body.yMax - 50f, 220f, 48f), "RAISE TO " + target, false, 18))
                PayAndApply(c, cost, la, g, after, g.Name + " is now level " + target + ".");
        }

        private void DrawScrapGun(Rect body, LootArmory la, LootGun g)
        {
            ForgeCost y = LootForgeMath.ScrapYield(g);
            float ty = body.y;
            Label(body.x, ty, body.width, "SCRAPPING " + g.Name.ToUpperInvariant() + " GIVES");
            ty += 28f;
            foreach (KeyValuePair<string, int> kv in y.Materials)
            {
                MaterialDef m = MaterialDb.Get(kv.Key);
                UIKit.Text(new Rect(body.x + 12f, ty, body.width, 24f), kv.Value + " x " + (m != null ? ProgressionUI.Colored(m.Name, m.RarityColor) : kv.Key), 18, Color.white, TextAnchor.UpperLeft);
                ty += 26f;
            }
            if (g.Favorite)
            {
                UIKit.Text(new Rect(body.x, ty + 10f, body.width, 30f), "This gun is a favorite. Unmark it in the ARSENAL tab to scrap it.", 17, ProgressionUI.Bad, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                return;
            }
            bool armed = _armoryScrapUid == g.Uid && Time.unscaledTime < _armoryScrapUntil;
            if (UIKit.Button(new Rect(body.xMax - 220f, body.yMax - 50f, 220f, 48f), armed ? "CONFIRM SCRAP" : "SCRAP", armed, 18))
            {
                if (!armed)
                {
                    _armoryScrapUid = g.Uid;
                    _armoryScrapUntil = Time.unscaledTime + 3f;
                    Status("Press CONFIRM SCRAP to break down " + g.Name + "." + (CrystalForgeRules.NeedsGrantedOnceConfirm(g) ? " " + CrystalForgeRules.GrantedOnce : ""), false);
                }
                else
                {
                    string n = g.Name;
                    if (la.Scrap(g)) Status("Scrapped " + n + ".", true);
                    _armoryScrapUid = 0;
                    _armoryUid = 0;
                }
            }
        }

        /// <summary>
        /// 04D: pending crystal rewards can be collected at the Forge (into the backpack, or a free loot slot when the
        /// backpack is full). Nothing is replaced; with no room the reward simply keeps waiting.
        /// </summary>
        private void DrawCrystalCollect(Rect r)
        {
            CrystalArsenal a = Game.Arsenal;
            if (a == null) return;
            List<CrystalClaimView> views = a.Claims();
            float y = r.y;
            bool any = false;
            for (int i = 0; i < views.Count && y < r.yMax - 50f; i++)
            {
                CrystalClaimView v = views[i];
                if (!v.Pending) continue;
                if (!any)
                {
                    Label(r.x, y, r.width, "CRYSTAL REWARDS WAITING");
                    y += 30f;
                    any = true;
                }
                LootUniqueDef u = LootUniques.Get(v.Unique);
                UIKit.Text(new Rect(r.x, y + 8f, r.width - 240f, 26f), v.Title + "  <color=#cfcfd6>" + v.Source + "</color>", 17, u != null ? GearSystem.RarityColor(u.Rarity) : Color.white, TextAnchor.UpperLeft, FontStyle.Bold, true, true);
                if (UIKit.Button(new Rect(r.xMax - 220f, y, 220f, 42f), "COLLECT", false, 17))
                {
                    string msg;
                    bool ok = a.Claim(v.Token, CrystalClaimAction.ToBackpack, -1, out msg) || a.Claim(v.Token, CrystalClaimAction.EquipFree, -1, out msg);
                    if (!ok) msg = "No room: free a backpack space or a loot slot, then collect it here, at the case or in ARSENAL.";
                    Status(msg, ok);
                }
                y += 50f;
            }
        }
    }
}
