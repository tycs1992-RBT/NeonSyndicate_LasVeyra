using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Armour plates for human enemies (ArmorPlates, ArmorRules): police wear a vest and helmet, SWAT the full set,
    /// some gang members a vest, mission enemies a vest and helmet; Shard-Touched elites add shoulder guards and double
    /// the pool, champions wear everything at three times it. Init puts the role's armour on, MakeElite and
    /// MakeChampion call RefreshArmor, and EquipArmor lets other systems (enemy roles, missions) choose a loadout.
    /// Creatures never wear plates (they have breakable parts instead).
    /// </summary>
    public sealed partial class NpcController
    {
        /// <summary>The armour plates this NPC wears (null when none).</summary>
        public ArmorPlates Plates { get { return Health != null ? Health.Plates : null; } }

        /// <summary>Puts the armour of this NPC's role and tier on (called at the end of Init).</summary>
        private void InitArmor()
        {
            RefreshArmor();
        }

        /// <summary>Re-dresses the NPC for its current role and tier (after it becomes an elite or a champion).</summary>
        public void RefreshArmor()
        {
            if (IsCreature || Health == null || Rig == null) return;
            // The same NPC always rolls the same: whether a gang member wears a vest is part of who they are.
            float roll = (float)new System.Random(GetInstanceID() * 7919 + 17).NextDouble();
            ArmorLoadout l = ArmorRules.LoadoutFor(Role, Elite, IsChampion, IsChampion && HasAffix(ChampionAffix.Armored), (int)Settings.Difficulty, roll);
            EquipArmor(l);
        }

        /// <summary>Wears a given loadout (an empty one removes the plates). Enemy roles and missions may call this.</summary>
        public void EquipArmor(ArmorLoadout loadout)
        {
            if (IsCreature) return;
            try { ArmorPlates.Equip(this, loadout, ArmorTrim()); }
            catch (System.Exception e) { Log.Exception("Armour plates", e); }
        }

        /// <summary>The glowing trim on the plates: blue police, deep blue SWAT, red mission crews, purple champions, cyan elites.</summary>
        private Color ArmorTrim()
        {
            if (IsChampion) return GearSystem.RarityColor(Rarity.Epic);
            if (Elite) return new Color(0.3f, 0.95f, 1f);
            switch (Role)
            {
                case NpcRole.Police: return new Color(0.3f, 0.55f, 1f);
                case NpcRole.Swat: return new Color(0.2f, 0.35f, 0.95f);
                case NpcRole.Gang: return new Color(1f, 0.6f, 0.2f);
                default: return new Color(1f, 0.3f, 0.25f);
            }
        }
    }
}
