using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum WeaponSlot
    {
        Unarmed = 0,
        Melee = 1,
        Handgun = 2,
        SMG = 3,
        Shotgun = 4,
        Rifle = 5,
        Sniper = 6,
        Heavy = 7,
        Throwable = 8
    }

    public enum AmmoType
    {
        None,
        Pistol,
        SMG,
        Shell,
        Rifle,
        Sniper,
        Rocket,
        Grenade40,
        Grenade,
        Molotov,
        Minigun
    }

    public enum FireKind
    {
        Melee,
        Semi,
        Auto,
        Projectile,
        Throw,
        Spin
    }

    public enum WeaponId
    {
        Fists = 0,
        Switchblade,
        SluggerBat,
        TireIron,
        VectorSidearm,
        BrassHammer,
        CoilBurst,
        MicroSpray,
        GlassShot,
        RiotDrum,
        ArcCarbine,
        CinderLMG,
        LongWatch,
        PulseLauncher,
        Hammerhead,
        Buzzsaw,
        Frag,
        FireBottle,
        VoltKatana,
        CrystalMaul,
        ArcGauntlets,
        /// <summary>The four equipped loot slots: Weapons.Get returns the live definition of the gun in the slot.</summary>
        Loot1,
        Loot2,
        Loot3,
        Loot4,
        Count
    }

    public sealed class WeaponDef
    {
        public WeaponId Id;
        public string Name;
        public string Blurb;
        public WeaponSlot Slot;
        public AmmoType Ammo;
        public FireKind Fire;
        public HoldType Hold;
        public float Damage;
        public float Interval;
        public int Pellets = 1;
        public float SpreadHip;
        public float SpreadAim;
        public float Recoil;
        public float Range;
        public int ClipSize;
        public int MaxAmmo;
        public float ReloadTime;
        public bool ShellByShell;
        public float ProjectileSpeed;
        public float BlastRadius;
        public float MeleeRange;
        public float MeleeForce;
        public int Price;
        public int AmmoPrice;
        public int AmmoPerPurchase;
        public SfxId Sound;
        public float MuzzleSize = 1f;
        public bool DriveBy;
        public float ScopeFov;
        public int UnlockRank;

        /// <summary>The generated loot gun behind a loot slot's definition (null for the fixed arsenal).</summary>
        public LootGun Loot;
        public Element Element;
        /// <summary>Chance per hit to apply the element's status effect (loot guns).</summary>
        public float ElementChance;
        public float ElementPower;
        public float CritChance;
        public float CritMultiplier = 1f;
        public float Handling = 1f;
        /// <summary>Crystal-tech guns heat up instead of using a magazine: heat added per shot (1 overheats them).</summary>
        public float HeatPerShot;
        /// <summary>Bit per LootTrait the gun carries.</summary>
        public int TraitMask;

        public bool HasTrait(LootTrait t) { return (TraitMask & (1 << (int)t)) != 0; }
        public bool IsLoot { get { return Loot != null; } }
        public bool Overheats { get { return HeatPerShot > 0f; } }
        public bool UsesAmmo { get { return Ammo != AmmoType.None; } }
        public bool IsMythic { get { return Id == WeaponId.VoltKatana || Id == WeaponId.CrystalMaul || Id == WeaponId.ArcGauntlets; } }
        public bool IsGun { get { return Fire == FireKind.Semi || Fire == FireKind.Auto || Fire == FireKind.Spin || Fire == FireKind.Projectile; } }
    }

    /// <summary>The Neon Syndicate arsenal (names are fictional), plus the four loot slots filled by the loot armory.</summary>
    public static class Weapons
    {
        public static readonly WeaponDef[] All = Build();
        /// <summary>Number of equipped loot slots (WeaponId.Loot1 to Loot4).</summary>
        public const int LootSlots = 4;
        private static readonly WeaponDef[] _loot = new WeaponDef[LootSlots];

        /// <summary>Empties the loot slots (Enter Play Mode without domain reload, new game).</summary>
        public static void ResetStatics()
        {
            Array.Clear(_loot, 0, _loot.Length);
        }

        /// <summary>A weapon's definition. A filled loot slot returns the live definition of the gun in it.</summary>
        public static WeaponDef Get(WeaponId id)
        {
            int i = (int)id;
            int l = i - (int)WeaponId.Loot1;
            if (l >= 0 && l < LootSlots && _loot[l] != null) return _loot[l];
            return i >= 0 && i < All.Length ? All[i] : All[0];
        }

        public static bool IsLoot(WeaponId id) { return id >= WeaponId.Loot1 && id <= WeaponId.Loot4; }
        public static int LootIndex(WeaponId id) { return (int)id - (int)WeaponId.Loot1; }
        public static WeaponId LootId(int index) { return (WeaponId)((int)WeaponId.Loot1 + Mathf.Clamp(index, 0, LootSlots - 1)); }

        /// <summary>True when the loot slot holds a gun.</summary>
        public static bool LootFilled(WeaponId id)
        {
            int l = (int)id - (int)WeaponId.Loot1;
            return l >= 0 && l < LootSlots && _loot[l] != null;
        }

        /// <summary>Puts a gun's definition in a loot slot (null empties it). Called by the loot armory.</summary>
        public static void SetLoot(int index, WeaponDef def)
        {
            if (index < 0 || index >= LootSlots) return;
            if (def != null) def.Id = LootId(index);
            _loot[index] = def;
        }

        private static WeaponDef[] Build()
        {
            WeaponDef[] w = new WeaponDef[(int)WeaponId.Count];
            w[(int)WeaponId.Fists] = new WeaponDef { Id = WeaponId.Fists, Name = "Fists", Blurb = "Bare knuckles.", Slot = WeaponSlot.Unarmed, Fire = FireKind.Melee, Hold = HoldType.None, Damage = 9f, Interval = 0.42f, MeleeRange = 1.4f, MeleeForce = 3f, Sound = SfxId.Punch };
            w[(int)WeaponId.Switchblade] = new WeaponDef { Id = WeaponId.Switchblade, Name = "Switchblade", Blurb = "Quick, quiet, close.", Slot = WeaponSlot.Melee, Fire = FireKind.Melee, Hold = HoldType.Melee, Damage = 36f, Interval = 0.48f, MeleeRange = 1.5f, MeleeForce = 2f, Sound = SfxId.KnifeSlash, Price = 150 };
            w[(int)WeaponId.SluggerBat] = new WeaponDef { Id = WeaponId.SluggerBat, Name = "Slugger Bat", Blurb = "Home-run swings that knock people flat.", Slot = WeaponSlot.Melee, Fire = FireKind.Melee, Hold = HoldType.Melee, Damage = 30f, Interval = 0.8f, MeleeRange = 1.9f, MeleeForce = 9f, Sound = SfxId.BatHit, Price = 100 };
            w[(int)WeaponId.TireIron] = new WeaponDef { Id = WeaponId.TireIron, Name = "Tire Iron", Blurb = "Every mechanic's argument settler.", Slot = WeaponSlot.Melee, Fire = FireKind.Melee, Hold = HoldType.Melee, Damage = 25f, Interval = 0.6f, MeleeRange = 1.6f, MeleeForce = 6f, Sound = SfxId.MetalHit, Price = 80 };
            w[(int)WeaponId.VectorSidearm] = new WeaponDef { Id = WeaponId.VectorSidearm, Name = "Vector Sidearm", Blurb = "Accurate starter pistol.", Slot = WeaponSlot.Handgun, Ammo = AmmoType.Pistol, Fire = FireKind.Semi, Hold = HoldType.Pistol, Damage = 26f, Interval = 0.17f, SpreadHip = 1.4f, SpreadAim = 0.35f, Recoil = 3f, Range = 120f, ClipSize = 15, MaxAmmo = 360, ReloadTime = 1.2f, Price = 300, AmmoPrice = 60, AmmoPerPurchase = 45, Sound = SfxId.Pistol, MuzzleSize = 0.7f, DriveBy = true };
            w[(int)WeaponId.BrassHammer] = new WeaponDef { Id = WeaponId.BrassHammer, Name = "Brass Hammer .50", Blurb = "Six rounds of very bad news.", Slot = WeaponSlot.Handgun, Ammo = AmmoType.Pistol, Fire = FireKind.Semi, Hold = HoldType.Pistol, Damage = 72f, Interval = 0.55f, SpreadHip = 1.8f, SpreadAim = 0.25f, Recoil = 9f, Range = 140f, ClipSize = 6, MaxAmmo = 240, ReloadTime = 2f, Price = 850, AmmoPrice = 80, AmmoPerPurchase = 24, Sound = SfxId.Revolver, MuzzleSize = 1f, DriveBy = true, UnlockRank = 2 };
            w[(int)WeaponId.CoilBurst] = new WeaponDef { Id = WeaponId.CoilBurst, Name = "Coil Burst SMG", Blurb = "Fast close-range pressure.", Slot = WeaponSlot.SMG, Ammo = AmmoType.SMG, Fire = FireKind.Auto, Hold = HoldType.Pistol, Damage = 17f, Interval = 0.075f, SpreadHip = 3f, SpreadAim = 1.3f, Recoil = 2.2f, Range = 90f, ClipSize = 30, MaxAmmo = 600, ReloadTime = 1.5f, Price = 1400, AmmoPrice = 120, AmmoPerPurchase = 90, Sound = SfxId.SMG, MuzzleSize = 0.8f, DriveBy = true, UnlockRank = 1 };
            w[(int)WeaponId.MicroSpray] = new WeaponDef { Id = WeaponId.MicroSpray, Name = "Micro Spray", Blurb = "Pocket-sized bullet hose.", Slot = WeaponSlot.SMG, Ammo = AmmoType.SMG, Fire = FireKind.Auto, Hold = HoldType.Pistol, Damage = 13f, Interval = 0.058f, SpreadHip = 4.2f, SpreadAim = 2.2f, Recoil = 1.8f, Range = 70f, ClipSize = 24, MaxAmmo = 600, ReloadTime = 1.3f, Price = 900, AmmoPrice = 100, AmmoPerPurchase = 72, Sound = SfxId.SMG, MuzzleSize = 0.7f, DriveBy = true };
            w[(int)WeaponId.GlassShot] = new WeaponDef { Id = WeaponId.GlassShot, Name = "Glass Shot Pump", Blurb = "Heavy room-clearing blast.", Slot = WeaponSlot.Shotgun, Ammo = AmmoType.Shell, Fire = FireKind.Semi, Hold = HoldType.Rifle, Damage = 15f, Pellets = 8, Interval = 0.85f, SpreadHip = 6.5f, SpreadAim = 4.5f, Recoil = 11f, Range = 45f, ClipSize = 8, MaxAmmo = 120, ReloadTime = 0.42f, ShellByShell = true, Price = 1200, AmmoPrice = 90, AmmoPerPurchase = 16, Sound = SfxId.Shotgun, MuzzleSize = 1.5f, UnlockRank = 1 };
            w[(int)WeaponId.RiotDrum] = new WeaponDef { Id = WeaponId.RiotDrum, Name = "Riot Drum Auto Shotgun", Blurb = "A drum magazine full of crowd control.", Slot = WeaponSlot.Shotgun, Ammo = AmmoType.Shell, Fire = FireKind.Auto, Hold = HoldType.Rifle, Damage = 11f, Pellets = 7, Interval = 0.27f, SpreadHip = 7.5f, SpreadAim = 5.5f, Recoil = 7f, Range = 40f, ClipSize = 16, MaxAmmo = 160, ReloadTime = 2.4f, Price = 3600, AmmoPrice = 120, AmmoPerPurchase = 24, Sound = SfxId.Shotgun, MuzzleSize = 1.4f, UnlockRank = 3 };
            w[(int)WeaponId.ArcCarbine] = new WeaponDef { Id = WeaponId.ArcCarbine, Name = "Arc Carbine", Blurb = "Mid-range precision assault rifle.", Slot = WeaponSlot.Rifle, Ammo = AmmoType.Rifle, Fire = FireKind.Auto, Hold = HoldType.Rifle, Damage = 31f, Interval = 0.11f, SpreadHip = 2.4f, SpreadAim = 0.55f, Recoil = 3.4f, Range = 220f, ClipSize = 30, MaxAmmo = 450, ReloadTime = 1.8f, Price = 3000, AmmoPrice = 150, AmmoPerPurchase = 90, Sound = SfxId.Rifle, MuzzleSize = 1.1f, UnlockRank = 2 };
            w[(int)WeaponId.CinderLMG] = new WeaponDef { Id = WeaponId.CinderLMG, Name = "Cinder LMG", Blurb = "A hundred-round belt of Ash Underpass diplomacy.", Slot = WeaponSlot.Rifle, Ammo = AmmoType.Rifle, Fire = FireKind.Auto, Hold = HoldType.Rifle, Damage = 28f, Interval = 0.085f, SpreadHip = 3.4f, SpreadAim = 1.4f, Recoil = 3f, Range = 200f, ClipSize = 100, MaxAmmo = 600, ReloadTime = 3.6f, Price = 6500, AmmoPrice = 250, AmmoPerPurchase = 150, Sound = SfxId.Rifle, MuzzleSize = 1.2f, UnlockRank = 4 };
            w[(int)WeaponId.LongWatch] = new WeaponDef { Id = WeaponId.LongWatch, Name = "Long Watch Sniper", Blurb = "Bolt-action, scoped, patient.", Slot = WeaponSlot.Sniper, Ammo = AmmoType.Sniper, Fire = FireKind.Semi, Hold = HoldType.Rifle, Damage = 160f, Interval = 1.2f, SpreadHip = 7f, SpreadAim = 0f, Recoil = 14f, Range = 700f, ClipSize = 5, MaxAmmo = 80, ReloadTime = 2.4f, Price = 5000, AmmoPrice = 120, AmmoPerPurchase = 10, Sound = SfxId.Sniper, MuzzleSize = 1.2f, ScopeFov = 13f, UnlockRank = 3 };
            w[(int)WeaponId.PulseLauncher] = new WeaponDef { Id = WeaponId.PulseLauncher, Name = "Pulse Launcher", Blurb = "Lobs 40mm crowd-control grenades.", Slot = WeaponSlot.Heavy, Ammo = AmmoType.Grenade40, Fire = FireKind.Projectile, Hold = HoldType.Rifle, Damage = 170f, Interval = 0.9f, SpreadHip = 1.5f, SpreadAim = 0.4f, Recoil = 9f, Range = 120f, ClipSize = 6, MaxAmmo = 36, ReloadTime = 2.8f, ProjectileSpeed = 38f, BlastRadius = 6.5f, Price = 7500, AmmoPrice = 300, AmmoPerPurchase = 6, Sound = SfxId.GrenadeLauncher, MuzzleSize = 1.4f, UnlockRank = 4 };
            w[(int)WeaponId.Hammerhead] = new WeaponDef { Id = WeaponId.Hammerhead, Name = "Hammerhead RPG", Blurb = "Point at problem. Remove problem.", Slot = WeaponSlot.Heavy, Ammo = AmmoType.Rocket, Fire = FireKind.Projectile, Hold = HoldType.Heavy, Damage = 280f, Interval = 1.6f, SpreadHip = 1.2f, SpreadAim = 0.2f, Recoil = 12f, Range = 400f, ClipSize = 1, MaxAmmo = 20, ReloadTime = 2.5f, ProjectileSpeed = 55f, BlastRadius = 8.5f, Price = 12000, AmmoPrice = 500, AmmoPerPurchase = 2, Sound = SfxId.RocketLaunch, MuzzleSize = 2f, UnlockRank = 5 };
            w[(int)WeaponId.Buzzsaw] = new WeaponDef { Id = WeaponId.Buzzsaw, Name = "Buzzsaw Minigun", Blurb = "Six spinning barrels. No subtlety.", Slot = WeaponSlot.Heavy, Ammo = AmmoType.Minigun, Fire = FireKind.Spin, Hold = HoldType.Heavy, Damage = 20f, Interval = 0.035f, SpreadHip = 3.2f, SpreadAim = 2.5f, Recoil = 1f, Range = 160f, ClipSize = 0, MaxAmmo = 2000, ReloadTime = 0f, Price = 25000, AmmoPrice = 400, AmmoPerPurchase = 500, Sound = SfxId.Minigun, MuzzleSize = 1.3f, UnlockRank = 6 };
            w[(int)WeaponId.Frag] = new WeaponDef { Id = WeaponId.Frag, Name = "Frag Grenade", Blurb = "Cook, throw, take cover.", Slot = WeaponSlot.Throwable, Ammo = AmmoType.Grenade, Fire = FireKind.Throw, Hold = HoldType.Thrown, Damage = 220f, Interval = 1.1f, Range = 40f, ClipSize = 0, MaxAmmo = 25, BlastRadius = 7.5f, ProjectileSpeed = 17f, Price = 0, AmmoPrice = 250, AmmoPerPurchase = 1, Sound = SfxId.Throw, UnlockRank = 2 };
            w[(int)WeaponId.FireBottle] = new WeaponDef { Id = WeaponId.FireBottle, Name = "Fire Bottle", Blurb = "Glass, rag, regret.", Slot = WeaponSlot.Throwable, Ammo = AmmoType.Molotov, Fire = FireKind.Throw, Hold = HoldType.Thrown, Damage = 14f, Interval = 1.1f, Range = 35f, ClipSize = 0, MaxAmmo = 25, BlastRadius = 4.5f, ProjectileSpeed = 15f, Price = 0, AmmoPrice = 150, AmmoPerPurchase = 1, Sound = SfxId.Throw, UnlockRank = 1 };
            // Mythic melee weapons: never sold, only found (elite drops, shrines, story rewards).
            w[(int)WeaponId.VoltKatana] = new WeaponDef { Id = WeaponId.VoltKatana, Name = "Volt Katana", Blurb = "A mythic blade that stores lightning between cuts.", Slot = WeaponSlot.Melee, Fire = FireKind.Melee, Hold = HoldType.Melee, Damage = 42f, Interval = 0.5f, MeleeRange = 2.3f, MeleeForce = 3f, Sound = SfxId.KnifeSlash, UnlockRank = 0 };
            w[(int)WeaponId.CrystalMaul] = new WeaponDef { Id = WeaponId.CrystalMaul, Name = "Crystal Maul", Blurb = "A Frostline relic: a hammer head of living crystal.", Slot = WeaponSlot.Melee, Fire = FireKind.Melee, Hold = HoldType.Melee, Damage = 66f, Interval = 1.1f, MeleeRange = 2.4f, MeleeForce = 12f, Sound = SfxId.BatHit, UnlockRank = 0 };
            w[(int)WeaponId.ArcGauntlets] = new WeaponDef { Id = WeaponId.ArcGauntlets, Name = "Arc Gauntlets", Blurb = "Shard-forged knuckles that crackle on every hit.", Slot = WeaponSlot.Melee, Fire = FireKind.Melee, Hold = HoldType.None, Damage = 21f, Interval = 0.38f, MeleeRange = 1.6f, MeleeForce = 4f, Sound = SfxId.Punch, UnlockRank = 0 };
            // Empty loot slots: harmless stand-ins (like fists) that are never owned while empty.
            for (int s = 0; s < LootSlots; s++)
            {
                WeaponId id = (WeaponId)((int)WeaponId.Loot1 + s);
                w[(int)id] = new WeaponDef { Id = id, Name = "Empty Loot Slot " + (s + 1), Blurb = "Equip a loot gun here from the ARSENAL tab.", Slot = WeaponSlot.Unarmed, Fire = FireKind.Melee, Hold = HoldType.None, Damage = 9f, Interval = 0.42f, MeleeRange = 1.4f, MeleeForce = 3f, Sound = SfxId.Punch };
            }
            for (int i = 0; i < w.Length; i++) if (w[i] == null) w[i] = w[0];
            CrystalCombatPolicy04F.ApplyFixed(w);
            return w;
        }

        public static string SlotName(WeaponSlot s)
        {
            switch (s)
            {
                case WeaponSlot.Unarmed: return "Unarmed";
                case WeaponSlot.Melee: return "Melee";
                case WeaponSlot.Handgun: return "Shard foci";
                case WeaponSlot.SMG: return "Rapid foci";
                case WeaponSlot.Shotgun: return "Crescents";
                case WeaponSlot.Rifle: return "Beam foci";
                case WeaponSlot.Sniper: return "Precision lenses";
                case WeaponSlot.Heavy: return "Rupture foci";
                default: return "Vessels";
            }
        }
    }

    /// <summary>
    /// Classic open-world weapon inventory: one list per slot, shared ammo pools per ammo type, a loaded clip per gun.
    /// Pure logic (no scene access) so it is unit tested outside Unity.
    /// </summary>
    [Serializable]
    public sealed class WeaponInventory
    {
        public readonly List<WeaponId> Owned = new List<WeaponId>();
        public readonly int[] Clip = new int[(int)WeaponId.Count];
        public readonly int[] Ammo = new int[Enum.GetValues(typeof(AmmoType)).Length];
        public WeaponId Current = WeaponId.Fists;
        public event Action<WeaponId> Changed;

        public WeaponInventory()
        {
            Owned.Add(WeaponId.Fists);
        }

        public WeaponDef CurrentDef { get { return Weapons.Get(Current); } }

        public bool Has(WeaponId id)
        {
            return Owned.Contains(id);
        }

        /// <summary>Gives a weapon (or just its ammo if already owned). Returns true if it was new.</summary>
        public bool Give(WeaponId id, int ammo)
        {
            WeaponDef d = Weapons.Get(id);
            bool isNew = !Owned.Contains(id);
            if (isNew)
            {
                Owned.Add(id);
                SortOwned();
            }
            if (d.UsesAmmo)
            {
                AddAmmo(d.Ammo, ammo);
                if (d.ClipSize > 0 && Clip[(int)id] == 0) LoadClip(id);
            }
            return isNew;
        }

        /// <summary>Orders the owned weapons by slot, then id (loot slots move when their gun changes archetype).</summary>
        public void SortOwned()
        {
            Owned.Sort((a, b) => ((int)Weapons.Get(a).Slot * 100 + (int)a).CompareTo((int)Weapons.Get(b).Slot * 100 + (int)b));
        }

        public void Remove(WeaponId id)
        {
            if (id == WeaponId.Fists) return;
            Owned.Remove(id);
            Clip[(int)id] = 0;
            if (Current == id) Select(WeaponId.Fists);
        }

        public int MaxAmmoFor(AmmoType t)
        {
            int m = 0;
            foreach (WeaponDef d in Weapons.All) if (d.Ammo == t) m = Mathf.Max(m, d.MaxAmmo);
            return m;
        }

        public void AddAmmo(AmmoType t, int amount)
        {
            if (t == AmmoType.None) return;
            int i = (int)t;
            Ammo[i] = Mathf.Clamp(Ammo[i] + amount, 0, MaxAmmoFor(t));
        }

        public int Reserve(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            return d.UsesAmmo ? Ammo[(int)d.Ammo] : 0;
        }

        public int TotalAmmo(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            if (!d.UsesAmmo) return int.MaxValue;
            return Clip[(int)id] + Ammo[(int)d.Ammo];
        }

        public bool Usable(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            if (!d.UsesAmmo) return true;
            return TotalAmmo(id) > 0;
        }

        /// <summary>Moves ammo from the reserve into the clip. Returns rounds loaded.</summary>
        public int LoadClip(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            if (!d.UsesAmmo || d.ClipSize <= 0) return 0;
            int need = d.ClipSize - Clip[(int)id];
            int have = Ammo[(int)d.Ammo];
            int take = d.ShellByShell ? Mathf.Min(1, Mathf.Min(need, have)) : Mathf.Min(need, have);
            Clip[(int)id] += take;
            Ammo[(int)d.Ammo] -= take;
            return take;
        }

        /// <summary>Consumes one round. Returns false if the gun is empty.</summary>
        public bool ConsumeRound(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            if (!d.UsesAmmo) return true;
            if (d.ClipSize <= 0)
            {
                int i = (int)d.Ammo;
                if (Ammo[i] <= 0) return false;
                Ammo[i]--;
                return true;
            }
            if (Clip[(int)id] <= 0) return false;
            Clip[(int)id]--;
            return true;
        }

        public bool NeedsReload(WeaponId id)
        {
            WeaponDef d = Weapons.Get(id);
            return d.UsesAmmo && d.ClipSize > 0 && Clip[(int)id] < d.ClipSize && Ammo[(int)d.Ammo] > 0;
        }

        public void Select(WeaponId id)
        {
            if (!Owned.Contains(id)) return;
            if (Current == id) return;
            Current = id;
            if (Changed != null) Changed(id);
        }

        /// <summary>Cycles to the next or previous usable weapon (mouse wheel).</summary>
        public WeaponId Cycle(int direction)
        {
            if (Owned.Count == 0) return Current;
            int start = Owned.IndexOf(Current);
            if (start < 0) start = 0;
            for (int step = 1; step <= Owned.Count; step++)
            {
                int i = ((start + step * Math.Sign(direction == 0 ? 1 : direction)) % Owned.Count + Owned.Count) % Owned.Count;
                if (Usable(Owned[i]))
                {
                    Select(Owned[i]);
                    return Current;
                }
            }
            return Current;
        }

        public List<WeaponId> InSlot(WeaponSlot slot)
        {
            List<WeaponId> l = new List<WeaponId>();
            foreach (WeaponId id in Owned) if (Weapons.Get(id).Slot == slot) l.Add(id);
            if (slot == WeaponSlot.Melee || slot == WeaponSlot.Unarmed)
            {
                // Number key 1 cycles fists and melee weapons together.
                List<WeaponId> both = new List<WeaponId>();
                foreach (WeaponId id in Owned)
                {
                    WeaponSlot s = Weapons.Get(id).Slot;
                    if (s == WeaponSlot.Melee || s == WeaponSlot.Unarmed) both.Add(id);
                }
                return both;
            }
            return l;
        }

        /// <summary>Selects a slot; pressing the same slot again cycles weapons within it. Returns false if the slot is empty.</summary>
        public bool SelectSlot(WeaponSlot slot)
        {
            List<WeaponId> list = InSlot(slot);
            list.RemoveAll(id => !Usable(id));
            if (list.Count == 0) return false;
            int idx = list.IndexOf(Current);
            WeaponId pick = idx >= 0 ? list[(idx + 1) % list.Count] : list[0];
            Select(pick);
            return true;
        }

        public WeaponId BestInSlot(WeaponSlot slot)
        {
            List<WeaponId> list = InSlot(slot);
            for (int i = list.Count - 1; i >= 0; i--) if (Usable(list[i])) return list[i];
            return WeaponId.Fists;
        }

        public void Clear()
        {
            Owned.Clear();
            Owned.Add(WeaponId.Fists);
            Array.Clear(Clip, 0, Clip.Length);
            Array.Clear(Ammo, 0, Ammo.Length);
            Current = WeaponId.Fists;
        }

        public void GiveAll()
        {
            for (int i = 0; i < (int)WeaponId.Count; i++)
            {
                // An empty loot slot is not a weapon.
                if (Weapons.IsLoot((WeaponId)i) && !Weapons.LootFilled((WeaponId)i)) continue;
                WeaponDef d = Weapons.Get((WeaponId)i);
                Give(d.Id, d.MaxAmmo);
                if (d.ClipSize > 0) Clip[i] = d.ClipSize;
            }
            for (int t = 1; t < Ammo.Length; t++) Ammo[t] = MaxAmmoFor((AmmoType)t);
        }
    }
}
