using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The few entry points that shared game code (combat, progression, economy, shops) calls into the faction, Forge and
    /// skill systems. Every hook is null-safe and catches its own exceptions, so a progression bug can never break a
    /// fight, a shop or a level-up.
    /// </summary>
    public static class ProgressionHooks
    {
        private static float _gunCritTextAt = -99f;

        public static void ResetStatics()
        {
            _gunCritTextAt = -99f;
            ProgressionReadoutStaticsH8S.ResetStatics();
        }

        /// <summary>Progression.LevelUp: a new level means a new skill point (and, H8S, a "what changed" banner once the HUD shows).</summary>
        public static void OnLevelUp(int level)
        {
            try
            {
                LevelUpReadoutH8S.Begin(level);
            }
            catch (Exception e) { Log.Exception("Level up readout", e); }
            try
            {
                if (Game.Skills != null) Game.Skills.OnLevelUp(level);
            }
            catch (Exception e) { Log.Exception("Level up hook", e); }
        }

        /// <summary>Progression.OnEnemyKilled: the player killed a person (gang reputation, takedown skills).</summary>
        public static void OnEnemyKilled(NpcController n)
        {
            if (n == null) return;
            try
            {
                if (Game.Factions != null) Game.Factions.OnNpcKilled(n);
            }
            catch (Exception e) { Log.Exception("Faction kill hook", e); }
            try
            {
                if (Game.Skills != null) Game.Skills.OnTakedown(n);
            }
            catch (Exception e) { Log.Exception("Skill kill hook", e); }
            try
            {
                if (Game.Gadgets != null) Game.Gadgets.OnEnemyKilled(n);
            }
            catch (Exception e) { Log.Exception("Gadget kill hook", e); }
        }

        /// <summary>PlayerCombat: a perfect dodge slipped a hit (Shard Echo, Momentum, Weightless Roll).</summary>
        public static void OnPerfectDodge()
        {
            try
            {
                if (Game.Skills != null) Game.Skills.OnPerfectDodge();
            }
            catch (Exception e) { Log.Exception("Perfect dodge hook", e); }
        }

        /// <summary>PlayerCombat: a skill critical hit landed.</summary>
        public static void ShowMeleeCrit()
        {
            if (Game.Hud != null) Game.Hud.CombatText("Critical", new Color(1f, 0.82f, 0.3f));
        }

        /// <summary>WeaponController: the player's multiplier for one bullet at a distance (gun skills and the crit roll).</summary>
        public static float GunMultiplier(float distance)
        {
            bool crit;
            float k = SkillMods.GunRoll(distance, out crit);
            // Build sets: range bonuses and the gear critical (GearBuilds).
            bool gearCrit;
            k *= GearBuilds.GunHitMultiplier(distance, out gearCrit);
            if (gearCrit) crit = true;
            // Shotguns fire many pellets: show at most one "Critical" every half second.
            if (crit && Time.unscaledTime - _gunCritTextAt > 0.5f)
            {
                _gunCritTextAt = Time.unscaledTime;
                ShowMeleeCrit();
            }
            return k;
        }

        /// <summary>
        /// Discount at a named shop (gun shop, garage, clothing store): the best faction discount on that shop's turf
        /// combined with the skill discount.
        /// </summary>
        public static float ShopDiscount(string shopName)
        {
            float faction = 0f;
            try
            {
                if (Game.Factions != null && Game.Layout != null && !string.IsNullOrEmpty(shopName))
                {
                    Poi poi = Game.Layout.FindPoi(shopName);
                    if (poi != null) faction = Game.Factions.ShopDiscountAt(poi.District);
                }
            }
            catch (Exception e) { Log.Exception("Shop discount", e); }
            return ForgeMath.CombineDiscounts(faction, SkillMods.Discount);
        }

        /// <summary>One line for a shop header ("Ember Court turf: 10% off (Trusted)   Haggler: 8% off"), or empty when nothing is off.</summary>
        public static string ShopDiscountText(string shopName)
        {
            try
            {
                string line = "";
                if (Game.Factions != null && Game.Layout != null && !string.IsNullOrEmpty(shopName))
                {
                    Poi poi = Game.Layout.FindPoi(shopName);
                    if (poi != null) line = Game.Factions.ShopDiscountText(poi.District);
                }
                if (SkillMods.Discount > 0.001f) line += (line.Length > 0 ? "   " : "") + "Haggler: " + Mathf.RoundToInt(SkillMods.Discount * 100f) + "% off";
                if (line.Length > 0) line += "   (total " + Mathf.RoundToInt(ShopDiscount(shopName) * 100f) + "% off)";
                return line;
            }
            catch (Exception e)
            {
                Log.Exception("Shop discount text", e);
                return "";
            }
        }

        /// <summary>Economy.Earn: skill cash gain on most income (not the casino, cheats or Forge salvage, which has its own bonus).</summary>
        public static int CashGain(int amount, string reason)
        {
            if (amount <= 0 || SkillMods.CashGain == 1f) return amount;
            if (reason == "Slots" || reason == "Cheat" || reason == "Salvage") return amount;
            return Mathf.RoundToInt(amount * SkillMods.CashGain);
        }

        /// <summary>
        /// End of a progression system's Restore: recomputes every modifier with what has been restored so far and puts the
        /// saved tonic charges back (Progression capped them before the skill and Forge bonuses were known). Each of the
        /// three systems calls this, so the result is right whatever order they restore in.
        /// </summary>
        public static void AfterRestore(SaveData d)
        {
            try
            {
                if (Game.Skills != null) Game.Skills.RecomputeNow();
                PlayerController p = Game.Player;
                if (d != null && d.Tonics >= 0 && p != null && p.Combat != null) p.Combat.Tonics = Mathf.Min(d.Tonics, p.Combat.MaxTonics);
            }
            catch (Exception e) { Log.Exception("Progression restore", e); }
        }
    }
}
