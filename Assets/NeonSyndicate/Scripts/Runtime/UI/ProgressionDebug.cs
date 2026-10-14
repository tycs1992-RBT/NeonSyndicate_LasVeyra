using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// "Progress" tab in the F2 debug menu: skill points, materials, recipes and reputation on demand, and buttons that
    /// open each kind of Forge, so the progression systems can be tried without hunting for hours.
    /// </summary>
    public static class ProgressionDebug
    {
        private static int _faction;

        public static void ResetStatics()
        {
            _faction = 0;
        }

        public static void Register()
        {
            DebugPages.Register(new MenuPage { Title = "Progress", Order = 40, Draw = Draw });
        }

        private static void Draw(Rect r)
        {
            float x = r.x, y = r.y;
            float bw = 300f, bh = 46f, gap = 20f;
            SkillTree st = Game.Skills;
            CraftingSystem forge = Game.Crafting;
            FactionSystem fs = Game.Factions;
            UIKit.Text(new Rect(x, y, r.width, 28f), "SKILLS AND FORGE", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "+5 skill points", false, 18) && st != null) st.Grant(5);
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Reset skill trees (free)", false, 18) && st != null) st.DebugReset();
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "+5,000 Lumen", false, 18) && Game.Progress != null) Game.Progress.AddLumen(5000);
            y += bh + 12f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "+20 of every material", false, 18) && Game.Materials != null)
                foreach (MaterialDef m in MaterialDb.All) Game.Materials.Add(m.Id, 20, false);
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Know every recipe", false, 18) && forge != null) forge.GrantAll();
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "+$20,000", false, 18) && Game.Economy != null) Game.Economy.Earn(20000, "Cheat");
            y += bh + 12f;
            if (UIKit.Button(new Rect(x, y, bw, bh), "Open Dante's forge", false, 18)) OpenForge(ForgeStation.Forgewright, "DANTE ROOK'S FORGE");
            if (UIKit.Button(new Rect(x + bw + gap, y, bw, bh), "Open the outpost forge", false, 18)) OpenForge(ForgeStation.Outpost, "FROST WARDENS' FORGE");
            if (UIKit.Button(new Rect(x + (bw + gap) * 2f, y, bw, bh), "Open a shrine forge", false, 18)) OpenForge(ForgeStation.Shrine, "SHRINE FORGE");
            y += bh + 30f;

            UIKit.Text(new Rect(x, y, r.width, 28f), "REPUTATION", 22, UIKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            y += 34f;
            _faction = Mathf.Clamp(_faction, 0, FactionDb.All.Length - 1);
            FactionDef f = FactionDb.All[_faction];
            if (UIKit.Button(new Rect(x, y, 60f, bh), "<", false, 22)) _faction = (_faction + FactionDb.All.Length - 1) % FactionDb.All.Length;
            int rep = fs != null ? fs.Rep(f.Id) : 0;
            UIKit.Text(new Rect(x + 70f, y + 10f, 360f, 30f), f.Name + "  " + rep + " (" + FactionRules.RankOf(rep) + ")", 20, f.Color, TextAnchor.UpperLeft, FontStyle.Bold);
            if (UIKit.Button(new Rect(x + 440f, y, 60f, bh), ">", false, 22)) _faction = (_faction + 1) % FactionDb.All.Length;
            float bx = x + 520f;
            int[] steps = { -20, -5, 5, 20 };
            foreach (int s in steps)
            {
                if (UIKit.Button(new Rect(bx, y, 90f, bh), (s > 0 ? "+" : "") + s, false, 20) && fs != null) fs.AddRep(f.Id, s, "debug");
                bx += 100f;
            }
            if (UIKit.Button(new Rect(bx, y, 150f, bh), "Set 0", false, 18) && fs != null) fs.SetRep(f.Id, 0);
            y += bh + 20f;
            string mods = "Melee x" + SkillMods.MeleeDamage.ToString("0.00") + "   Guns x" + SkillMods.GunDamage.ToString("0.00") + "   Powers x" + SkillMods.PowerDamage.ToString("0.00") +
                          "   Creatures x" + SkillMods.CreatureDamage.ToString("0.00") + "   Crit " + ProgressionUI.Pct(SkillMods.CritChance) + " at x" + SkillMods.CritDamage.ToString("0.0") +
                          "\nStamina +" + Mathf.RoundToInt(SkillMods.StaminaMaxBonus) + " regen x" + SkillMods.StaminaRegen.ToString("0.00") + "   Dodge cost x" + SkillMods.DodgeCost.ToString("0.00") +
                          "   Guard cost x" + SkillMods.GuardCost.ToString("0.00") + "   Parry +" + Mathf.RoundToInt(SkillMods.ParryWindowBonus * 1000f) + " ms   Health +" + Mathf.RoundToInt(SkillMods.HealthMaxBonus) +
                          "   Damage taken x" + SkillMods.DamageTaken.ToString("0.00") + "   Tonics +" + SkillMods.TonicChargesBonus + " heal x" + SkillMods.TonicHeal.ToString("0.00") +
                          "\nEnergy +" + Mathf.RoundToInt(SkillMods.EnergyMaxBonus) + " regen x" + SkillMods.EnergyRegen.ToString("0.00") + "   Power cost x" + SkillMods.PowerCost.ToString("0.00") +
                          "   Veil drain x" + SkillMods.VeilDrain.ToString("0.00") + " shift x" + SkillMods.VeilShiftCost.ToString("0.00") + "   Frost resist " + ProgressionUI.Pct(SkillMods.FrostResist) +
                          "\nLumen x" + SkillMods.LumenGain.ToString("0.00") + "   Cash x" + SkillMods.CashGain.ToString("0.00") + "   Discount " + ProgressionUI.Pct(SkillMods.Discount) +
                          "   Grip x" + SkillMods.VehicleGrip.ToString("0.00") + "   Boost x" + SkillMods.VehicleBoost.ToString("0.00") + "   Creature sense x" + SkillMods.CreatureSense.ToString("0.00") +
                          "   Loot +" + ProgressionUI.Pct(SkillMods.MaterialFind) + "   Rep gain +" + ProgressionUI.Pct(SkillMods.RepGain);
            UIKit.Text(new Rect(x, y, r.width, 120f), mods, 17, new Color(0.85f, 0.9f, 0.95f), TextAnchor.UpperLeft, FontStyle.Normal, true, true);
        }

        private static void OpenForge(ForgeStation s, string title)
        {
            if (Game.Crafting == null) return;
            if (Game.Menus != null) Game.Menus.Close();
            Game.Crafting.OpenForge(s, title);
        }
    }
}
