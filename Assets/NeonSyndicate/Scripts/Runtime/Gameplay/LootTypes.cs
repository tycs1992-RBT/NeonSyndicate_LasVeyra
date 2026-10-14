using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>The kinds of loot gun. Each maps onto the existing ammo, hold and fire types.</summary>
    public enum LootArchetype
    {
        Pistol = 0,
        HandCannon = 1,
        Smg = 2,
        AssaultRifle = 3,
        Shotgun = 4,
        Sniper = 5,
        /// <summary>Grenade launcher, or rocket launcher when the body part is variant 2 or 3.</summary>
        Launcher = 6,
        /// <summary>Crystal-tech energy gun: no magazine, it heats up and vents instead.</summary>
        CrystalCaster = 7
    }

    /// <summary>The six invented Las Veyra gun makers. Each has a stat personality and a look of its own.</summary>
    public enum LootMaker
    {
        /// <summary>Precise: tight spread, big critical hits, a little slower.</summary>
        Saltglass = 0,
        /// <summary>Heavy hitting: big damage, slow, kicks hard.</summary>
        Brassjaw = 1,
        /// <summary>Elemental: always carries an element, and its element hits harder.</summary>
        Veyra = 2,
        /// <summary>Fast firing: high fire rate, lighter rounds, wilder spread.</summary>
        Cinderline = 3,
        /// <summary>High capacity: huge magazines, slower reloads.</summary>
        Dunehold = 4,
        /// <summary>Gimmick: one extra trait on every gun.</summary>
        Oddwire = 5
    }

    /// <summary>The seven part slots of a loot gun. Every part has four variants (0 is the plain one).</summary>
    public enum LootPart
    {
        Body = 0,
        Barrel = 1,
        Grip = 2,
        Stock = 3,
        Sight = 4,
        Magazine = 5,
        Accessory = 6
    }

    /// <summary>The special features ("traits") a loot gun can roll.</summary>
    public enum LootTrait
    {
        Ricochet = 0,
        Piercing = 1,
        ExplosiveRounds = 2,
        ChainLightning = 3,
        HomingRockets = 4,
        SplitShot = 5,
        BurstFire = 6,
        ChargeShot = 7,
        DoubleBarrel = 8,
        RefillOnKill = 9,
        LifeSteal = 10,
        CritCascade = 11,
        EmptyNova = 12,
        Penetrator = 13,
        SpinUp = 14
    }

    /// <summary>The final numbers of a loot gun (what the card shows and what the weapon definition uses).</summary>
    public sealed class LootStats
    {
        public float Damage;
        public float Interval;
        public int Pellets = 1;
        public float SpreadHip;
        public float SpreadAim;
        public float Recoil;
        public float Range;
        /// <summary>Rounds per magazine (0 for crystal casters, which overheat instead).</summary>
        public int Clip;
        /// <summary>Reload time, or the vent time of a crystal caster.</summary>
        public float Reload;
        public float ProjectileSpeed;
        public float BlastRadius;
        public float CritChance;
        public float CritMultiplier;
        public float ElementChance;
        public float ElementPower;
        public float Handling;
        /// <summary>Heat added per shot by a crystal caster (1 overheats it); 0 for magazine guns.</summary>
        public float HeatPerShot;
        public float ScopeFov;
        public bool Rocket;
    }

    /// <summary>
    /// One generated gun. Every field below Version is saved, so a gun rebuilds exactly; Name, Flavor and Stats are
    /// derived by LootGenerator.Refresh.
    /// </summary>
    public sealed class LootGun
    {
        public int Uid;
        public int Seed;
        public LootArchetype Archetype;
        public LootMaker Maker;
        public Rarity Rarity;
        public int Level = 1;
        public readonly int[] Parts = new int[LootGenerator.PartCount];
        public Element Element;
        public readonly List<LootTrait> Traits = new List<LootTrait>();
        public bool Favorite;
        /// <summary>Index into LootUniques.All for named Legendary and Mythic guns, -1 for generated guns.</summary>
        public int Unique = -1;
        /// <summary>How many times the Forge raised its rarity (picks which part the next ascension rerolls).</summary>
        public int Ascensions;
        /// <summary>Bumped on every change (Forge), so cached models and weapon definitions refresh. Not saved.</summary>
        public int Version;
        public string Name = "";
        public string Flavor = "";
        public LootStats Stats = new LootStats();

        public bool HasTrait(LootTrait t)
        {
            return Traits.Contains(t);
        }

        public bool IsUnique { get { return Unique >= 0; } }

        /// <summary>A deep copy (same uid), used for the Forge's before and after preview.</summary>
        public LootGun Clone()
        {
            LootGun g = new LootGun
            {
                Uid = Uid, Seed = Seed, Archetype = Archetype, Maker = Maker, Rarity = Rarity, Level = Level, Element = Element,
                Favorite = Favorite, Unique = Unique, Ascensions = Ascensions, Version = Version
            };
            for (int i = 0; i < Parts.Length; i++) g.Parts[i] = Parts[i];
            g.Traits.AddRange(Traits);
            LootGenerator.Refresh(g);
            return g;
        }
    }

    /// <summary>A named Legendary or Mythic gun: fixed name, flavor line, archetype, maker, element and signature traits.</summary>
    public sealed class LootUniqueDef
    {
        public string Name;
        public string Flavor;
        public Rarity Rarity;
        public LootArchetype Archetype;
        public LootMaker Maker;
        public Element Element;
        public LootTrait[] Traits;
        /// <summary>The creature boss whose defeat can drop it (empty for the general pool).</summary>
        public string BossId = "";
    }

    /// <summary>The named uniques. Indices are saved (LootGunSave.Unique): append only, never reorder.</summary>
    public static class LootUniques
    {
        public static readonly LootUniqueDef[] All =
        {
            U("Alley Saint", "It has heard every confession in the Brass Projects.", Rarity.Legendary, LootArchetype.Pistol, LootMaker.Saltglass, Element.None, LootTrait.Ricochet, LootTrait.CritCascade),
            U("Lucky Static", "Nobody knows why it works. Nobody wants to fix it.", Rarity.Legendary, LootArchetype.Pistol, LootMaker.Oddwire, Element.Volt, LootTrait.ChainLightning, LootTrait.BurstFire),
            U("Brass Magistrate", "Two rounds, one verdict.", Rarity.Legendary, LootArchetype.HandCannon, LootMaker.Brassjaw, Element.None, LootTrait.DoubleBarrel, LootTrait.Piercing),
            U("Gutter Choir", "Sixty voices, all singing at once.", Rarity.Legendary, LootArchetype.Smg, LootMaker.Cinderline, Element.Ember, LootTrait.SpinUp, LootTrait.LifeSteal),
            U("Moth Swarm", "Drawn to anything warm.", Rarity.Legendary, LootArchetype.Smg, LootMaker.Dunehold, Element.Void, LootTrait.HomingRockets, LootTrait.RefillOnKill),
            U("Canal Oath", "Sworn on the black water under the bridges.", Rarity.Legendary, LootArchetype.AssaultRifle, LootMaker.Saltglass, Element.Frost, LootTrait.Penetrator, LootTrait.CritCascade),
            U("Dusk Clock", "Clocks in at dusk and never clocks out.", Rarity.Legendary, LootArchetype.AssaultRifle, LootMaker.Dunehold, Element.None, LootTrait.ExplosiveRounds, LootTrait.RefillOnKill),
            U("Door Prize", "Everyone who opens the door wins something.", Rarity.Legendary, LootArchetype.Shotgun, LootMaker.Brassjaw, Element.Ember, LootTrait.DoubleBarrel, LootTrait.ExplosiveRounds),
            U("Quiet Lantern", "One light in the dark, and then none.", Rarity.Legendary, LootArchetype.Sniper, LootMaker.Saltglass, Element.Volt, LootTrait.Piercing, LootTrait.ChainLightning),
            U("Rooftop Sermon", "Preaches to the whole block at once.", Rarity.Legendary, LootArchetype.Launcher, LootMaker.Brassjaw, Element.Ember, LootTrait.ChainLightning, LootTrait.EmptyNova),
            U("Veil Whisper", "Its light arrives a moment before you pull the trigger.", Rarity.Legendary, LootArchetype.CrystalCaster, LootMaker.Veyra, Element.Void, LootTrait.ChargeShot, LootTrait.Piercing),
            U("Prism Riot", "Every bolt argues with every other bolt.", Rarity.Legendary, LootArchetype.CrystalCaster, LootMaker.Oddwire, Element.Volt, LootTrait.SplitShot, LootTrait.ChainLightning),
            B("Titan's Hoarfrost", "Cut from the Titan's heart. The cold keeps climbing.", LootArchetype.Sniper, LootMaker.Saltglass, Element.Frost, "glacier_titan", LootTrait.Piercing, LootTrait.CritCascade, LootTrait.Penetrator),
            B("Djinn's Thousand Wishes", "Each pellet is a wish someone regretted.", LootArchetype.Shotgun, LootMaker.Oddwire, Element.Void, "mirage_djinn", LootTrait.Ricochet, LootTrait.LifeSteal, LootTrait.EmptyNova),
            B("Foreman's Order", "It still gives orders. Everything it hits obeys.", LootArchetype.Launcher, LootMaker.Brassjaw, Element.Volt, "golem_foreman", LootTrait.ChainLightning, LootTrait.EmptyNova, LootTrait.RefillOnKill),
            B("Queen's Reflection", "Nothing reflects in it. Everything it hits does.", LootArchetype.CrystalCaster, LootMaker.Veyra, Element.Void, "wraith_queen", LootTrait.ChargeShot, LootTrait.Piercing, LootTrait.LifeSteal),
            B("Skyline Requiem", "Plays the whole city to sleep.", LootArchetype.AssaultRifle, LootMaker.Cinderline, Element.Ember, "", LootTrait.SpinUp, LootTrait.ExplosiveRounds, LootTrait.ChainLightning)
        };

        private static LootUniqueDef U(string name, string flavor, Rarity r, LootArchetype a, LootMaker m, Element e, params LootTrait[] traits)
        {
            return new LootUniqueDef { Name = name, Flavor = flavor, Rarity = r, Archetype = a, Maker = m, Element = e, Traits = traits };
        }

        private static LootUniqueDef B(string name, string flavor, LootArchetype a, LootMaker m, Element e, string boss, params LootTrait[] traits)
        {
            return new LootUniqueDef { Name = name, Flavor = flavor, Rarity = Rarity.Mythic, Archetype = a, Maker = m, Element = e, Traits = traits, BossId = boss };
        }

        public static LootUniqueDef Get(int index)
        {
            return index >= 0 && index < All.Length ? All[index] : null;
        }

        /// <summary>The Mythic unique themed on a creature boss, or -1.</summary>
        public static int ForBoss(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return -1;
            for (int i = 0; i < All.Length; i++) if (All[i].BossId == bossId) return i;
            return -1;
        }

        /// <summary>Indices of the uniques of one rarity (optionally one archetype; pass -1 for any).</summary>
        public static List<int> OfRarity(Rarity r, int archetype)
        {
            List<int> l = new List<int>();
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Rarity != r) continue;
                if (archetype >= 0 && (int)All[i].Archetype != archetype) continue;
                l.Add(i);
            }
            return l;
        }
    }
}
