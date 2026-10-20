using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Live crystal functions for stable saved weapon ids. No owned record, unique index or ammo array is rewritten.</summary>
    public static class CrystalCombatPolicy04F
    {
        public static void ResetStatics() { }

        public static CrystalBehavior Behavior(WeaponDef d)
        {
            if (d == null || !d.IsGun) return CrystalBehavior.None;
            if (d.Loot != null) return LootBehavior(d.Loot);
            switch (d.Id)
            {
                case WeaponId.VectorSidearm:
                case WeaponId.CoilBurst:
                case WeaponId.MicroSpray: return CrystalBehavior.Shardwake;
                case WeaponId.BrassHammer:
                case WeaponId.LongWatch: return CrystalBehavior.Mirrorwake;
                case WeaponId.GlassShot: return CrystalBehavior.GyreCrescent;
                case WeaponId.RiotDrum: return CrystalBehavior.MirageChoir;
                case WeaponId.PulseLauncher:
                case WeaponId.Hammerhead: return CrystalBehavior.Sunderheart;
                default: return CrystalBehavior.PrismThread;
            }
        }

        public static CrystalBehavior LootBehavior(LootGun g)
        {
            if (g == null || RegionalArsenal04OE.IsWeapon(g)) return CrystalBehavior.None;
            CrystalBehavior signature = CrystalFocusDb.BehaviorOfUnique(g.Unique);
            if (signature != CrystalBehavior.None) return signature;
            switch (g.Archetype)
            {
                case LootArchetype.Pistol:
                case LootArchetype.Smg: return CrystalBehavior.Shardwake;
                case LootArchetype.HandCannon:
                case LootArchetype.Sniper: return CrystalBehavior.Mirrorwake;
                case LootArchetype.Shotgun: return CrystalBehavior.GyreCrescent;
                case LootArchetype.Launcher: return CrystalBehavior.Sunderheart;
                default: return CrystalBehavior.PrismThread;
            }
        }

        /// <summary>Converts only the live definition. A signature keeps its exact 04D tuning and identity.</summary>
        public static WeaponDef Convert(WeaponDef d)
        {
            CrystalBehavior b = Behavior(d);
            if (b == CrystalBehavior.None) return d;
            bool signature = d.Loot != null && CrystalFocusDb.BehaviorOfUnique(d.Loot.Unique) != CrystalBehavior.None;
            CrystalProfile p = CrystalFocusDb.Get(b);
            if (p == null) return d;
            if (!signature)
            {
                float oldDps = d.Damage * Mathf.Max(1, d.Pellets) / Mathf.Max(0.15f, d.Interval);
                // A beam pays one tenth of its per-second budget per admitted grid tick.
                if (b == CrystalBehavior.PrismThread) d.Damage = Mathf.Clamp(oldDps * 0.065f, 5f, 22f);
                else if (b == CrystalBehavior.GyreCrescent) d.Damage = Mathf.Clamp(oldDps * 0.28f, 25f, 85f);
                else if (b == CrystalBehavior.Sunderheart) d.Damage = Mathf.Clamp(d.Damage * 0.4f, 35f, 110f);
                else if (b == CrystalBehavior.Shardwake) d.Damage = Mathf.Clamp(d.Damage, 16f, 55f);
                else if (b == CrystalBehavior.Mirrorwake) d.Damage = Mathf.Clamp(d.Damage * 0.8f, 40f, 145f);
                d.Interval = p.Interval;
                d.Range = p.Range;
                d.HeatPerShot = b == CrystalBehavior.PrismThread ? p.Heat * CrystalFocusDb.BeamTick : p.Heat;
                d.Hold = b == CrystalBehavior.Shardwake || b == CrystalBehavior.GyreCrescent ? HoldType.Pistol : (b == CrystalBehavior.Sunderheart ? HoldType.Heavy : HoldType.Rifle);
                d.Fire = b == CrystalBehavior.PrismThread ? FireKind.Auto : FireKind.Semi;
                d.Recoil = Mathf.Min(d.Recoil, b == CrystalBehavior.Mirrorwake ? 5f : 2.2f);
                d.Blurb = p.FireLine + " Uses Resonance Charge. Melee and charging stations restore it. Reload vents heat only.";
            }
            d.Ammo = AmmoType.None;
            d.ClipSize = 0;
            d.MaxAmmo = 0;
            d.AmmoPrice = 0;
            d.AmmoPerPurchase = 0;
            d.ShellByShell = false;
            return d;
        }

        public static void ApplyFixed(WeaponDef[] defs)
        {
            for (int i = 0; i < defs.Length; i++)
            {
                WeaponDef d = defs[i];
                if (d == null) continue;
                // Empty loot slots have fallback definitions. They are not real owned foci.
                if (Weapons.IsLoot(d.Id)) continue;
                Convert(d);
                switch (d.Id)
                {
                    case WeaponId.VoltKatana: d.Name = "Asterblade"; d.Blurb = "Three-cut crystal longsword. Guard, parry and counter to open a foe. Melee restores charge."; d.Damage = 32f; break;
                    case WeaponId.Switchblade: d.Name = "Riftfang Pair"; d.Blurb = "Paired mineral talons. Quick cuts and a long dash strike; fragile guard."; d.Damage = 23f; break;
                    case WeaponId.CrystalMaul: d.Name = "Basalt Crown"; d.Blurb = "A committed two-hand maul. Heavy breaks poise; the wider shatter is limited by a six-second recovery."; d.Damage = 48f; break;
                    case WeaponId.SluggerBat: d.Name = "Geode Staff"; break;
                    case WeaponId.TireIron: d.Name = "Forge Hook"; break;
                    case WeaponId.VectorSidearm: d.Name = "Wayfarer Shard Focus"; break;
                    case WeaponId.BrassHammer: d.Name = "Sunspike Lens"; break;
                    case WeaponId.CoilBurst: d.Name = "Stormseed Focus"; break;
                    case WeaponId.MicroSpray: d.Name = "Quartz Bud"; break;
                    case WeaponId.GlassShot: d.Name = "Waystone Crescent"; break;
                    case WeaponId.RiotDrum: d.Name = "Choir Reliquary"; break;
                    case WeaponId.ArcCarbine: d.Name = "Threadweaver Lens"; break;
                    case WeaponId.CinderLMG: d.Name = "Ember Conduit"; break;
                    case WeaponId.LongWatch: d.Name = "Horizon Mirror"; break;
                    case WeaponId.PulseLauncher: d.Name = "Gravity Heart"; break;
                    case WeaponId.Hammerhead: d.Name = "Rupture Heart"; break;
                    case WeaponId.Buzzsaw: d.Name = "Prism Loom"; break;
                }
            }
        }

        public static float ShotCost(CrystalBehavior b, bool charged)
        {
            switch (b)
            {
                case CrystalBehavior.Shardwake: return charged ? 10f : 4f;
                case CrystalBehavior.PrismThread: return 1.2f;
                case CrystalBehavior.GyreCrescent: return 12f;
                case CrystalBehavior.Sunderheart: return charged ? 22f : 16f;
                case CrystalBehavior.MirageChoir: return 15f;
                case CrystalBehavior.Mirrorwake: return 18f;
                default: return 0f;
            }
        }
    }
}
