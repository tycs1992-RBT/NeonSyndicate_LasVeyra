using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The GiveGun step: a quest reward that is a loot gun (a rarity, maybe a type, or a named unique).</summary>
    public static partial class QuestSteps
    {
        /// <summary>
        /// Rolls the reward gun with the loot armory at the player's level (as a champion drop: item level +2) and puts it
        /// in the backpack, or at the player's feet in a light beam when the backpack is full. Unique wins over Rarity and
        /// Archetype; an empty Rarity means Rare.
        /// </summary>
        private static void GiveGunAction(QuestRun r, QuestStep s)
        {
            LootArmory loot = Game.Loot;
            if (loot == null)
            {
                Log.Warn("Quest " + r.Quest.Id + ": GiveGun skipped, the loot armory is not running.");
                return;
            }
            Rarity rarity;
            if (string.IsNullOrEmpty(s.Rarity) || !Enum.TryParse(s.Rarity, out rarity)) rarity = Rarity.Rare;
            int archetype = -1;
            LootArchetype a;
            if (!string.IsNullOrEmpty(s.Archetype) && Enum.TryParse(s.Archetype, out a)) archetype = (int)a;
            int unique = LootWorldRules.UniqueByName(s.Unique);
            if (unique >= 0) rarity = LootUniques.All[unique].Rarity;
            LootGun g = loot.NewGun(rarity, LootSource.Champion, archetype, unique);
            if (loot.AddToBackpack(g, false)) LootArmory.GiveStarterAmmo(g);
            else
            {
                Vector3 at = Game.PlayerPosition + (Game.Player != null ? Game.Player.transform.forward * 1.6f : Vector3.forward) + Vector3.up * 0.4f;
                loot.SpawnDrop(g, at, false);
                if (Game.Hud != null) Game.Hud.Notify("Your backpack is full: " + g.Name + " is on the ground in front of you.", 4f);
            }
            Color c = GearSystem.RarityColor(g.Rarity);
            if (Game.Hud != null) Game.Hud.Banner(g.Rarity >= Rarity.Legendary ? "UNIQUE REWARD" : "GUN REWARD", g.Name + (string.IsNullOrEmpty(g.Flavor) ? "" : ": " + g.Flavor), c, 4.5f);
            if (g.Rarity >= Rarity.Legendary) Pad.Rumble(0.3f, 0.5f, 0.3f);
        }
    }
}
