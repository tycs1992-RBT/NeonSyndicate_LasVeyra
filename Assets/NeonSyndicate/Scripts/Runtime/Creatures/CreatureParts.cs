using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Where on the body a breakable part sits (resolved to bones by the creature's rig).</summary>
    public enum PartAnchor
    {
        Head,
        Chest,
        Back,
        Tail,
        /// <summary>Front legs of a beast (claws, talons); the arms of a humanoid creature.</summary>
        ForeLimbs,
        /// <summary>Hind legs of a beast; the legs of a humanoid creature.</summary>
        HindLimbs,
        Wings
    }

    /// <summary>What breaking a part does to the fight. A part may carry several.</summary>
    [System.Flags]
    public enum PartEffect
    {
        None = 0,
        /// <summary>Claws, fangs, talons, fists: melee reach and damage are halved.</summary>
        MeleeHalf = 1,
        /// <summary>The protective shell is gone: no more frontal damage cut (or the glass guard stays broken).</summary>
        ArmorLost = 2,
        /// <summary>Wings torn: it cannot take off again and is brought down at once.</summary>
        Grounded = 4,
        /// <summary>The core is exposed: weak-point criticals hit harder.</summary>
        CoreExposed = 8,
        /// <summary>Breath, gaze, spit and thrown attacks stop.</summary>
        NoRanged = 16,
        /// <summary>Legs or ring plates cracked: it moves at 70% speed.</summary>
        Slowed = 32,
        /// <summary>Its special trick stops (a Hollow's blink, a Snow Hollow's chilling touch).</summary>
        NoAbility = 64,
        /// <summary>Its armour is cracked all over: it takes 25% more damage.</summary>
        Weakened = 128
    }

    /// <summary>One breakable part of a species.</summary>
    public sealed class PartDef
    {
        public string Id;
        public string Name;
        public PartAnchor Anchor;
        /// <summary>Part health as a share of the creature's max health.</summary>
        public float HpShare;
        public PartEffect Effects;
        /// <summary>Damage multiplier for hits from the front while the part is intact (1 = no cut).</summary>
        public float FrontGuard = 1f;
        /// <summary>Material dropped when it breaks, and how many.</summary>
        public string Material;
        public int MaterialCount = 1;
        /// <summary>One line for the bestiary: what breaking it does.</summary>
        public string Text;
    }

    /// <summary>
    /// Breakable parts per species id (every species has at least one, bosses two or three). Keyed like
    /// CreatureSpeciesDb; ids are stable because the bestiary saves "species:part" for parts seen broken.
    /// </summary>
    public static class CreaturePartsDb
    {
        private static Dictionary<string, PartDef[]> _bySpecies;

        public static void ResetStatics()
        {
            _bySpecies = null;
        }

        private static PartDef P(string id, string name, PartAnchor a, float share, PartEffect fx, string mat, int count, string text, float front = 1f)
        {
            return new PartDef { Id = id, Name = name, Anchor = a, HpShare = share, Effects = fx, Material = mat, MaterialCount = count, Text = text, FrontGuard = front };
        }

        private static Dictionary<string, PartDef[]> Build()
        {
            Dictionary<string, PartDef[]> d = new Dictionary<string, PartDef[]>();
            d["glass_mite"] = new[] { P("carapace", "Glass Carapace", PartAnchor.Back, 0.4f, PartEffect.ArmorLost, "glass_chitin", 1, "Halves damage from the front until it cracks.", 0.5f) };
            d["neon_jackal"] = new[] { P("fangs", "Neon Fangs", PartAnchor.Head, 0.3f, PartEffect.MeleeHalf, "jackal_fang", 1, "Bite reach and damage are halved.") };
            d["shard_golem"] = new[] { P("core_plate", "Core Plate", PartAnchor.Chest, 0.25f, PartEffect.CoreExposed, "shard_chunk", 2, "The chest core lies bare: criticals hit far harder.") };
            d["hollow_shade"] = new[] { P("tendrils", "Drain Tendrils", PartAnchor.Tail, 0.35f, PartEffect.MeleeHalf, "shade_essence", 1, "Its touch reaches half as far, hurts half as much and no longer drains energy.") };
            d["hollow"] = new[] { P("blink_crystal", "Blink Crystal", PartAnchor.Head, 0.3f, PartEffect.NoAbility, "hollow_essence", 1, "It can no longer blink to your blind side.") };
            d["mirror_wraith"] = new[] { P("glass_mantle", "Glass Mantle", PartAnchor.Chest, 0.3f, PartEffect.ArmorLost, "mirror_glass", 1, "Its frontal glass guard is gone for good.") };
            d["crystal_basilisk"] = new[]
            {
                P("crown_gem", "Crown Gem", PartAnchor.Head, 0.2f, PartEffect.NoRanged, "crown_gem", 1, "The crown gaze stops."),
                P("tail_blade", "Tail Blade", PartAnchor.Tail, 0.25f, PartEffect.MeleeHalf, "basilisk_scale", 2, "Tail sweeps and bites lose half their reach and damage.")
            };
            d["violet_wyvern"] = new[]
            {
                P("wing_membrane", "Wing Membrane", PartAnchor.Wings, 0.25f, PartEffect.Grounded, "wyvern_membrane", 1, "It crashes and can never take off again."),
                P("fire_gland", "Fire Gland", PartAnchor.Head, 0.2f, PartEffect.NoRanged, "void_thread", 1, "No more violet fire.")
            };
            d["frost_stalker"] = new[] { P("claws", "Rime Claws", PartAnchor.ForeLimbs, 0.25f, PartEffect.MeleeHalf, "frost_fang", 1, "Claw and pounce reach and damage are halved.") };
            d["rime_griffin"] = new[]
            {
                P("wing_feathers", "Flight Feathers", PartAnchor.Wings, 0.22f, PartEffect.Grounded, "rime_feather", 2, "It is brought down and stays on the ground."),
                P("talons", "Talons", PartAnchor.ForeLimbs, 0.2f, PartEffect.MeleeHalf, "rime_feather", 1, "Talon strikes lose half their reach and damage.")
            };
            d["snow_hollow"] = new[] { P("rime_claws", "Rime Claws", PartAnchor.ForeLimbs, 0.3f, PartEffect.MeleeHalf | PartEffect.NoAbility, "frost_essence", 1, "Half reach and damage, and its touch no longer chills your stamina.") };
            d["glacier_titan"] = new[]
            {
                P("frost_crown", "Frost Crown", PartAnchor.Head, 0.08f, PartEffect.Weakened, "glacier_shard", 2, "Cracked all over: it takes 25% more damage."),
                P("knee_plates", "Knee Plates", PartAnchor.HindLimbs, 0.08f, PartEffect.Slowed, "glacier_shard", 1, "It moves at 70% speed."),
                P("core_plate", "Core Plate", PartAnchor.Chest, 0.1f, PartEffect.CoreExposed, "glacier_shard", 2, "The core lies bare: criticals hit far harder.")
            };
            d["dune_wyrm"] = new[]
            {
                P("throat_plate", "Throat Plate", PartAnchor.Head, 0.1f, PartEffect.CoreExposed, "ember_gland", 1, "The throat core lies bare: criticals hit far harder."),
                P("ring_plates", "Ring Plates", PartAnchor.Back, 0.12f, PartEffect.Slowed, "wyrm_scale", 2, "It travels at 70% speed.")
            };
            d["quartz_drake"] = new[]
            {
                P("chest_plates", "Chest Plates", PartAnchor.Chest, 0.12f, PartEffect.CoreExposed, "drake_plate", 2, "The heart crystal lies bare: criticals hit far harder."),
                P("wings", "Wing Membranes", PartAnchor.Wings, 0.12f, PartEffect.Grounded, "drake_plate", 1, "It crashes and fights on the ground for good."),
                P("ember_gland", "Ember Gland", PartAnchor.Head, 0.1f, PartEffect.NoRanged, "ember_gland", 1, "No more ember breath.")
            };
            d["sun_griffin"] = new[]
            {
                P("wing_feathers", "Flight Feathers", PartAnchor.Wings, 0.22f, PartEffect.Grounded, "sun_feather", 2, "It is brought down and stays on the ground."),
                P("beak", "Sun Beak", PartAnchor.Head, 0.2f, PartEffect.MeleeHalf, "sun_feather", 1, "Beak and talon reach and damage are halved.")
            };
            d["sand_golem"] = new[]
            {
                P("chest_plate", "Chest Plate", PartAnchor.Chest, 0.25f, PartEffect.CoreExposed, "sand_core", 1, "The core behind it lies bare: criticals hit far harder."),
                P("sand_fists", "Sand Fists", PartAnchor.ForeLimbs, 0.2f, PartEffect.NoRanged, "shard_chunk", 1, "It cannot throw packed sand any more.")
            };
            d["mirage_djinn"] = new[]
            {
                P("sand_crown", "Sand Crown", PartAnchor.Head, 0.08f, PartEffect.Weakened, "ember_gland", 1, "Cracked all over: it takes 25% more damage."),
                P("storm_bracers", "Storm Bracers", PartAnchor.ForeLimbs, 0.08f, PartEffect.MeleeHalf, "ember_gland", 1, "Its blows lose half their reach and damage.")
            };
            d["golem_foreman"] = new[]
            {
                P("foreman_crown", "Foreman Crown", PartAnchor.Head, 0.08f, PartEffect.Weakened, "golem_core", 1, "Cracked all over: it takes 25% more damage."),
                P("slam_gauntlets", "Slam Gauntlets", PartAnchor.ForeLimbs, 0.08f, PartEffect.MeleeHalf, "shard_chunk", 2, "Its blows lose half their reach and damage."),
                P("core_plate", "Core Plate", PartAnchor.Chest, 0.1f, PartEffect.CoreExposed, "golem_core", 1, "The core lies bare: criticals hit far harder.")
            };
            d["wraith_queen"] = new[]
            {
                P("glass_mantle", "Glass Mantle", PartAnchor.Chest, 0.08f, PartEffect.ArmorLost, "mirror_glass", 2, "Her own glass guard stays cracked."),
                P("mirror_crown", "Mirror Crown", PartAnchor.Head, 0.08f, PartEffect.Weakened, "queen_mirror", 1, "Cracked all over: she takes 25% more damage.")
            };
            // 04E underground species: foundation entries reuse their body's parts until the ECOLOGY stream authors their own.
            string[,] alias04E =
            {
                { "ember_scavenger", "neon_jackal" }, { "ash_burrower", "crystal_basilisk" }, { "glasswing_ambusher", "violet_wyvern" },
                { "furnace_caster", "hollow_shade" }, { "repair_construct", "hollow_shade" }, { "cinder_moth", "hollow_shade" },
                { "kiln_crab", "crystal_basilisk" }, { "ember_hatchling", "neon_jackal" }, { "extraction_sentinel", "shard_golem" }
            };
            for (int i = 0; i < alias04E.GetLength(0); i++)
            {
                PartDef[] t;
                if (!d.ContainsKey(alias04E[i, 0]) && d.TryGetValue(alias04E[i, 1], out t)) d[alias04E[i, 0]] = t;
            }
            return d;
        }

        /// <summary>The parts of a species (an empty array for an unknown id).</summary>
        public static PartDef[] For(string speciesId)
        {
            if (_bySpecies == null) _bySpecies = Build();
            PartDef[] p;
            return !string.IsNullOrEmpty(speciesId) && _bySpecies.TryGetValue(speciesId, out p) ? p : new PartDef[0];
        }

        /// <summary>A part by species and part id, or null.</summary>
        public static PartDef Find(string speciesId, string partId)
        {
            PartDef[] all = For(speciesId);
            for (int i = 0; i < all.Length; i++) if (all[i].Id == partId) return all[i];
            return null;
        }

        /// <summary>The bestiary key of a part seen broken.</summary>
        public static string Key(string speciesId, string partId)
        {
            return speciesId + ":" + partId;
        }
    }

    /// <summary>The numbers of the part fight (engine-free, tested offline).</summary>
    public static class CreaturePartRules
    {
        /// <summary>Melee reach and damage while MeleeHalf is broken.</summary>
        public const float MeleeHalfScale = 0.5f;
        /// <summary>Move speed while Slowed.</summary>
        public const float SlowedScale = 0.7f;
        /// <summary>Damage taken while Weakened.</summary>
        public const float WeakenedScale = 1.25f;
        /// <summary>Extra critical multiplier on weak points once the core is exposed.</summary>
        public const float CoreExposedCrit = 1.5f;
        /// <summary>Seconds of stagger when a part breaks (bosses reel for less).</summary>
        public const float BreakStagger = 1.4f;
        public const float BossBreakStagger = 0.9f;
        /// <summary>Extra loot-gun roll chance at death per broken part, and its cap.</summary>
        public const float BonusLootPerPart = 0.2f;
        public const float BonusLootCap = 0.6f;

        /// <summary>How much of a hit reaches the part it lands on, by damage kind (melee smashes, blasts spread).</summary>
        public static float PartDamageScale(DamageType t)
        {
            switch (t)
            {
                case DamageType.Bullet: return 1f;
                case DamageType.Melee: return 1.2f;
                case DamageType.Explosion: return 0.6f;
                case DamageType.Power: return 0.8f;
                case DamageType.Fire: return 0.5f;
                default: return 0f;
            }
        }

        /// <summary>Part damage of one hit: the health damage, scaled by kind, doubled by a Resonance Burst.</summary>
        public static float PartDamage(float amount, DamageType t, bool resonance)
        {
            return Mathf.Max(0f, amount) * PartDamageScale(t) * (resonance ? ResonanceRules.BurstPartMultiplier : 1f);
        }

        /// <summary>Part health from the creature's max health.</summary>
        public static float PartHealth(PartDef def, float maxHealth)
        {
            return def == null ? 0f : Mathf.Max(1f, def.HpShare * Mathf.Max(1f, maxHealth));
        }

        /// <summary>Chance of an extra loot-gun roll at death for this many broken parts.</summary>
        public static float BonusLootChance(int broken)
        {
            return Mathf.Min(BonusLootCap, Mathf.Max(0, broken) * BonusLootPerPart);
        }

        /// <summary>Melee scale for a set of broken effects.</summary>
        public static float MeleeScale(PartEffect broken)
        {
            return (broken & PartEffect.MeleeHalf) != 0 ? MeleeHalfScale : 1f;
        }

        public static float SpeedScale(PartEffect broken)
        {
            return (broken & PartEffect.Slowed) != 0 ? SlowedScale : 1f;
        }

        public static float DamageTakenScale(PartEffect broken)
        {
            return (broken & PartEffect.Weakened) != 0 ? WeakenedScale : 1f;
        }
    }

    /// <summary>
    /// The live state of one creature's parts as plain data: health per part, which are broken, and the combined
    /// effects of the broken ones. The runtime component feeds it hits and acts on the breaks.
    /// </summary>
    public sealed class CreaturePartBook
    {
        public readonly PartDef[] Defs;
        private readonly float[] _hp;
        private readonly float[] _max;
        private readonly bool[] _broken;

        public CreaturePartBook(PartDef[] defs, float maxHealth)
        {
            Defs = defs ?? new PartDef[0];
            _hp = new float[Defs.Length];
            _max = new float[Defs.Length];
            _broken = new bool[Defs.Length];
            for (int i = 0; i < Defs.Length; i++)
            {
                _max[i] = CreaturePartRules.PartHealth(Defs[i], maxHealth);
                _hp[i] = _max[i];
            }
        }

        public int Count { get { return Defs.Length; } }
        public float Hp(int i) { return _hp[i]; }
        public float Max(int i) { return _max[i]; }
        public bool Broken(int i) { return _broken[i]; }

        public int BrokenCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _broken.Length; i++) if (_broken[i]) n++;
                return n;
            }
        }

        /// <summary>Effects of every broken part combined.</summary>
        public PartEffect BrokenEffects
        {
            get
            {
                PartEffect e = PartEffect.None;
                for (int i = 0; i < Defs.Length; i++) if (_broken[i]) e |= Defs[i].Effects;
                return e;
            }
        }

        public bool Has(PartEffect e)
        {
            return (BrokenEffects & e) != 0;
        }

        /// <summary>Index of the first part on an anchor, or -1.</summary>
        public int IndexOf(PartAnchor a)
        {
            for (int i = 0; i < Defs.Length; i++) if (Defs[i].Anchor == a) return i;
            return -1;
        }

        /// <summary>Damages part i; returns true when this hit broke it.</summary>
        public bool Damage(int i, float amount)
        {
            if (i < 0 || i >= Defs.Length || _broken[i] || amount <= 0f) return false;
            _hp[i] = Mathf.Max(0f, _hp[i] - amount);
            if (_hp[i] > 0f) return false;
            _broken[i] = true;
            return true;
        }

        /// <summary>The frontal damage cut of the intact parts (the smallest FrontGuard among them).</summary>
        public float FrontGuard
        {
            get
            {
                float g = 1f;
                for (int i = 0; i < Defs.Length; i++) if (!_broken[i]) g = Mathf.Min(g, Defs[i].FrontGuard);
                return g;
            }
        }
    }
}
