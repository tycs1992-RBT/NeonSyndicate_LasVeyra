using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    public enum GearSlot { Visor = 0, Shell = 1, Gauntlets = 2, Striders = 3, Implant = 4, Core = 5 }

    public enum Rarity { Common = 0, Uncommon = 1, Rare = 2, Epic = 3, Legendary = 4, Mythic = 5 }

    public enum Element { None = 0, Volt = 1, Ember = 2, Frost = 3, Void = 4 }

    /// <summary>
    /// Gear affixes. Saves store the number (GearSave.AffixTypes), so values are append-only: 04E appended MoveSpeed (18)
    /// after ElementPower (17). Count is never saved; it only sizes arrays and rejects unknown numbers on load.
    /// </summary>
    public enum AffixType
    {
        MaxHealth, MaxStamina, MaxEnergy,
        MeleeDamage, GunDamage, PowerDamage,
        Armor, BulletResist, MeleeResist, BlastResist,
        GuardEfficiency, ParryWindow, DodgeFrames,
        CritChance, LifeSteal, TonicPotency, LumenFind,
        ElementPower,
        /// <summary>04E: on-foot movement speed (fraction), capped by BuildStatRules04E.MaxMoveAffix.</summary>
        MoveSpeed,
        Count
    }

    [Serializable]
    public struct Affix
    {
        public AffixType Type;
        public float Value;
    }

    [Serializable]
    public sealed class GearItem
    {
        public int Uid;
        public GearSlot Slot;
        public Rarity Rarity;
        public int Power = 1;
        public int Upgrade;
        public int Set = -1;
        public Element Element;
        public int Seed;
        public string Name;
        public List<Affix> Affixes = new List<Affix>();

        public float UpgradeScale { get { return 1f + Upgrade * 0.12f; } }

        public float Get(AffixType t)
        {
            float v = 0f;
            for (int i = 0; i < Affixes.Count; i++) if (Affixes[i].Type == t) v += Affixes[i].Value;
            return v * UpgradeScale;
        }
    }

    /// <summary>Flat save record (JsonUtility friendly).</summary>
    [Serializable]
    public sealed class GearSave
    {
        public int Uid;
        public int Slot;
        public int Rarity;
        public int Power;
        public int Upgrade;
        public int Set;
        public int Element;
        public int Seed;
        public string Name;
        public List<int> AffixTypes = new List<int>();
        public List<float> AffixValues = new List<float>();
    }

    /// <summary>
    /// Mythic gear: six slots (Visor, Shell, Gauntlets, Striders, Implant, Weapon Core), six rarities from Common to
    /// Mythic, random affixes, elements that proc on melee hits, three four-piece sets, +5 upgrades at shrines,
    /// salvage for Lumen, loot beams dropped by enemies (always by Shard-Touched elites), and visible armour on Malik.
    /// </summary>
    public sealed partial class GearSystem : MonoBehaviour
    {
        public const int InventoryCap = 48;
        public const int SlotCount = 6;

        public readonly List<GearItem> Items = new List<GearItem>();
        public readonly GearItem[] Equipped = new GearItem[SlotCount];
        public event Action Changed;

        private int _nextUid = 1;
        private readonly float[] _totals = new float[(int)AffixType.Count];
        private readonly int[] _setCount = new int[SetNames.Length];
        private readonly List<LootDrop> _drops = new List<LootDrop>();
        private System.Random _rng = new System.Random(4242);
        private float _lifeStealCd;

        private sealed class LootDrop
        {
            public GearItem Item;
            public WeaponId Weapon = WeaponId.Count;
            public GameObject Go;
            public Transform Icon;
            public Vector3 Pos;
            public float Age;
        }

        // The first three are the classic melee sets (the Forge crafts them); Phase 6 appended the six build sets for gun,
        // gadget and armor play (indexes 3 to 8, numbers in GearBuildRules). Append only: saves store the index.
        public static readonly string[] SetNames =
        {
            "Neon Revenant", "Frostline Warden", "Rift Walker",
            "Longsight Lattice", "Stoneward Choir", "Gearwright's Wake", "Resonant Circuit", "Alley Saint", "Vanta Shroud"
        };
        public static readonly string[] SetBonus2 =
        {
            "+15% melee damage", "Guarding costs 25% less stamina", "+0.08 s of dodge invulnerability",
            "+12% gun damage beyond 25 m", "Kills restore 15 armor", "Gadget cooldowns 15% shorter",
            "Kills with a gun in hand restore 6 crystal energy", "+12% gun damage within 10 m", "Scanner marks last 50% longer"
        };
        /// <summary>Three piece bonuses: only the build sets have them (empty for the classic sets).</summary>
        public static readonly string[] SetBonus3 =
        {
            "", "", "",
            "Gun hits have an 8% chance to deal x1.6 (gear critical)", "Armor regenerates 4 a second after 4 s without being hit",
            "Gadgets hit, heal and shield 25% harder and last 20% longer", "+12% crystal power damage", "Kills heal 6% of your max health",
            "Marked enemies take 10% more damage"
        };
        public static readonly string[] SetBonus4 =
        {
            "Ripostes heal 20% of your health", "Parries release a frost nova that staggers nearby enemies", "Perfect dodges grant two seconds of free Third Eye",
            "Each gear critical cuts both gadget cooldowns by 0.4 s", "Take 15% less damage while your armor is above half",
            "Kills while a gadget is out cut both gadget cooldowns by 2 s", "Each Resonance Burst cuts both gadget cooldowns by 3 s",
            "Kills within 10 m refill the magazine of the gun in hand", "Killing a marked enemy sends a free Ripple pulse (20 m)"
        };

        /// <summary>The bonus text of a set for a piece count tier (2, 3 or 4), empty when there is none.</summary>
        public static string SetBonus(int set, int tier)
        {
            string[] a = tier == 2 ? SetBonus2 : (tier == 3 ? SetBonus3 : (tier == 4 ? SetBonus4 : null));
            return a != null && set >= 0 && set < a.Length ? a[set] : "";
        }

        public static void ResetStatics()
        {
        }

        public void Init()
        {
            Recalculate();
        }

        // ------------------------------------------------------------------ rarity look

        public static Color RarityColor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return new Color(0.4f, 0.95f, 0.45f);
                case Rarity.Rare: return new Color(0.35f, 0.6f, 1f);
                case Rarity.Epic: return new Color(0.72f, 0.4f, 1f);
                case Rarity.Legendary: return new Color(1f, 0.66f, 0.2f);
                case Rarity.Mythic: return new Color(0.35f, 1f, 0.95f);
                default: return new Color(0.82f, 0.82f, 0.85f);
            }
        }

        public static string RarityName(Rarity r) { return r.ToString(); }

        public static string SlotName(GearSlot s)
        {
            switch (s)
            {
                case GearSlot.Visor: return "Visor";
                case GearSlot.Shell: return "Shell";
                case GearSlot.Gauntlets: return "Gauntlets";
                case GearSlot.Striders: return "Striders";
                case GearSlot.Implant: return "Implant";
                default: return "Weapon Core";
            }
        }

        public static string ElementName(Element e)
        {
            switch (e)
            {
                case Element.Volt: return "Volt";
                case Element.Ember: return "Ember";
                case Element.Frost: return "Frost";
                case Element.Void: return "Void";
                default: return "";
            }
        }

        public static Color ElementColor(Element e)
        {
            switch (e)
            {
                case Element.Volt: return new Color(0.45f, 0.75f, 1f);
                case Element.Ember: return new Color(1f, 0.45f, 0.12f);
                case Element.Frost: return new Color(0.6f, 0.95f, 1f);
                case Element.Void: return new Color(0.75f, 0.25f, 1f);
                default: return Color.white;
            }
        }

        public static string AffixText(AffixType t, float v)
        {
            switch (t)
            {
                case AffixType.MaxHealth: return "+" + Mathf.RoundToInt(v) + " max health";
                case AffixType.MaxStamina: return "+" + Mathf.RoundToInt(v) + " stamina";
                case AffixType.MaxEnergy: return "+" + Mathf.RoundToInt(v) + " crystal energy";
                case AffixType.MeleeDamage: return "+" + Pct(v) + " melee damage";
                case AffixType.GunDamage: return "+" + Pct(v) + " gun damage";
                case AffixType.PowerDamage: return "+" + Pct(v) + " power damage";
                case AffixType.Armor: return Pct(v) + " less damage taken";
                case AffixType.BulletResist: return Pct(v) + " bullet resistance";
                case AffixType.MeleeResist: return Pct(v) + " melee resistance";
                case AffixType.BlastResist: return Pct(v) + " blast resistance";
                case AffixType.GuardEfficiency: return "Guarding costs " + Pct(v) + " less stamina";
                case AffixType.ParryWindow: return "+" + Mathf.RoundToInt(v * 1000f) + " ms parry window";
                case AffixType.DodgeFrames: return "+" + Mathf.RoundToInt(v * 1000f) + " ms dodge invulnerability";
                case AffixType.CritChance: return "+" + Pct(v) + " critical hit chance";
                case AffixType.LifeSteal: return Pct(v) + " of melee damage heals you";
                case AffixType.TonicPotency: return "Shard Tonic heals " + Pct(v) + " more";
                case AffixType.LumenFind: return "+" + Pct(v) + " Lumen from enemies";
                case AffixType.ElementPower: return "+" + Pct(v) + " elemental proc chance";
                case AffixType.MoveSpeed: return "+" + Pct(v) + " movement speed";
                default: return t.ToString();
            }
        }

        private static string Pct(float v)
        {
            return Mathf.RoundToInt(v * 100f) + "%";
        }

        // ------------------------------------------------------------------ generation

        private static readonly string[] Prefix =
        {
            "Glasswake", "Sable", "Neon", "Rift", "Hollow", "Static", "Quartz", "Ember", "Frost", "Volt", "Velvet", "Ninth",
            "Mirage", "Ashen", "Halcyon", "Obsidian", "Lumen", "Choir", "Vanta", "Saint"
        };

        private static readonly string[] SlotNouns =
        {
            "Visor|Halo|Crown|Mask|Lens",
            "Carapace|Shell|Mantle|Aegis|Rig",
            "Gauntlets|Grips|Knuckles|Bracers|Hands",
            "Striders|Greaves|Treads|Boots|Stilts",
            "Implant|Lattice|Spine|Engram|Seed",
            "Core|Heart|Coil|Prism|Battery"
        };

        private static readonly string[] Suffix =
        {
            "of the Ninth Rift", "of Sable Static", "of the Last Choir", "of Glass Saints", "of the Frostline",
            "of Hollow Light", "of the Dune King", "of Vanta Rain", "of the Inner City", "of Crystal Manor"
        };

        private static readonly AffixType[][] SlotPools =
        {
            new[] { AffixType.MaxEnergy, AffixType.PowerDamage, AffixType.CritChance, AffixType.ParryWindow, AffixType.LumenFind, AffixType.Armor },
            new[] { AffixType.MaxHealth, AffixType.Armor, AffixType.BulletResist, AffixType.MeleeResist, AffixType.BlastResist, AffixType.TonicPotency },
            new[] { AffixType.MeleeDamage, AffixType.GunDamage, AffixType.CritChance, AffixType.GuardEfficiency, AffixType.LifeSteal, AffixType.ParryWindow },
            // 04E appended MoveSpeed to the Striders pool (movement-oriented striders for the Riftstrider build).
            new[] { AffixType.MaxStamina, AffixType.DodgeFrames, AffixType.Armor, AffixType.BlastResist, AffixType.MaxHealth, AffixType.GuardEfficiency, AffixType.MoveSpeed },
            new[] { AffixType.MaxEnergy, AffixType.PowerDamage, AffixType.TonicPotency, AffixType.LumenFind, AffixType.MaxStamina, AffixType.LifeSteal },
            new[] { AffixType.MeleeDamage, AffixType.ElementPower, AffixType.CritChance, AffixType.GunDamage, AffixType.LifeSteal, AffixType.PowerDamage }
        };

        private static float Roll(AffixType t, float q, System.Random r)
        {
            float k = (float)(0.6 + r.NextDouble() * 0.4) * q;
            switch (t)
            {
                case AffixType.MaxHealth: return Mathf.Round(18f + 70f * k);
                case AffixType.MaxStamina: return Mathf.Round(6f + 22f * k);
                case AffixType.MaxEnergy: return Mathf.Round(8f + 30f * k);
                case AffixType.MeleeDamage:
                case AffixType.GunDamage:
                case AffixType.PowerDamage: return 0.04f + 0.18f * k;
                case AffixType.Armor: return 0.02f + 0.08f * k;
                case AffixType.BulletResist:
                case AffixType.MeleeResist:
                case AffixType.BlastResist: return 0.05f + 0.18f * k;
                case AffixType.GuardEfficiency: return 0.06f + 0.24f * k;
                case AffixType.ParryWindow: return 0.015f + 0.05f * k;
                case AffixType.DodgeFrames: return 0.015f + 0.06f * k;
                case AffixType.CritChance: return 0.03f + 0.12f * k;
                case AffixType.LifeSteal: return 0.02f + 0.07f * k;
                case AffixType.TonicPotency: return 0.08f + 0.3f * k;
                case AffixType.LumenFind: return 0.08f + 0.35f * k;
                case AffixType.ElementPower: return 0.06f + 0.24f * k;
                case AffixType.MoveSpeed: return 0.02f + 0.07f * k;
                default: return 0f;
            }
        }

        /// <summary>Rolls a new item. power scales stats (enemy toughness, story progress).</summary>
        public GearItem Generate(GearSlot slot, Rarity rarity, int power)
        {
            System.Random r = _rng;
            GearItem it = new GearItem { Uid = _nextUid++, Slot = slot, Rarity = rarity, Power = Mathf.Max(1, power), Seed = r.Next() };
            float q = (0.55f + 0.09f * (int)rarity) * (1f + Mathf.Min(10, power) * 0.05f);
            int affixes = 1 + Mathf.Min(4, (int)rarity) + (rarity == Rarity.Mythic ? 1 : 0);
            AffixType[] pool = SlotPools[(int)slot];
            List<AffixType> picked = new List<AffixType>();
            while (picked.Count < Mathf.Min(affixes, pool.Length))
            {
                AffixType t = pool[r.Next(pool.Length)];
                if (picked.Contains(t)) continue;
                picked.Add(t);
                it.Affixes.Add(new Affix { Type = t, Value = Roll(t, q, r) });
            }
            if (ElementRequired(slot) || (ElementAllowed(slot, rarity) && r.NextDouble() < 0.35))
                it.Element = (Element)(1 + r.Next(4));
            if (SetAllowed(slot, rarity) && r.NextDouble() < 0.4 + 0.1 * ((int)rarity - 2))
                it.Set = r.Next(SetNames.Length);
            it.Name = MakeName(it, r);
            return it;
        }

        /// <summary>Set pieces exist only as Rare or better Visors, Shells, Gauntlets and Striders (the Forge asks this too).</summary>
        public static bool SetAllowed(GearSlot slot, Rarity rarity)
        {
            return slot <= GearSlot.Striders && rarity >= Rarity.Rare;
        }

        /// <summary>Weapon Cores always carry an element; other slots only from Epic up.</summary>
        public static bool ElementAllowed(GearSlot slot, Rarity rarity)
        {
            return slot == GearSlot.Core || rarity >= Rarity.Epic;
        }

        public static bool ElementRequired(GearSlot slot)
        {
            return slot == GearSlot.Core;
        }

        /// <summary>
        /// The Forge's version of Generate: rolls the affixes exactly like a drop, then fixes the set and element the
        /// player chose (when the rules above allow them; a Weapon Core keeps its rolled element if none was chosen) and
        /// names the piece to match.
        /// </summary>
        public GearItem GenerateCrafted(GearSlot slot, Rarity rarity, int power, int set, Element element)
        {
            GearItem it = Generate(slot, rarity, power);
            it.Set = SetAllowed(slot, rarity) && set >= 0 && set < SetNames.Length ? set : -1;
            if (!ElementAllowed(slot, rarity)) it.Element = Element.None;
            else if (element != Element.None || !ElementRequired(slot)) it.Element = element;
            it.Name = MakeName(it, _rng);
            return it;
        }

        /// <summary>
        /// 04E: a deterministic piece for durable rewards and starter kits. The same seed and arguments always give the
        /// same affixes, element, set and name (its own random stream, so the drop generator's stream is untouched). forced
        /// affixes come first; the rest roll from the slot's pool like a drop. Uid stays 0 until GearSystem.AdoptUid.
        /// Engine-free.
        /// </summary>
        public static GearItem GenerateSeeded(GearSlot slot, Rarity rarity, int power, int seed, AffixType[] forced, Element element, int set)
        {
            System.Random r = new System.Random(seed);
            int pw = Mathf.Max(1, power);
            GearItem it = new GearItem { Uid = 0, Slot = slot, Rarity = rarity, Power = pw, Seed = seed };
            float q = (0.55f + 0.09f * (int)rarity) * (1f + Mathf.Min(10, pw) * 0.05f);
            int affixes = 1 + Mathf.Min(4, (int)rarity) + (rarity == Rarity.Mythic ? 1 : 0);
            AffixType[] pool = SlotPools[(int)slot];
            List<AffixType> picked = new List<AffixType>();
            if (forced != null)
            {
                foreach (AffixType t in forced)
                {
                    if (t < 0 || t >= AffixType.Count || picked.Contains(t)) continue;
                    picked.Add(t);
                    it.Affixes.Add(new Affix { Type = t, Value = Roll(t, q, r) });
                }
            }
            int want = Mathf.Max(affixes, picked.Count);
            int guard = 0;
            while (picked.Count < want && guard++ < 64)
            {
                AffixType t = pool[r.Next(pool.Length)];
                if (picked.Contains(t))
                {
                    bool free = false;
                    foreach (AffixType p in pool) if (!picked.Contains(p)) free = true;
                    if (!free) break;
                    continue;
                }
                picked.Add(t);
                it.Affixes.Add(new Affix { Type = t, Value = Roll(t, q, r) });
            }
            if (element != Element.None) it.Element = element;
            else if (ElementRequired(slot)) it.Element = (Element)(1 + r.Next(4));
            it.Set = SetAllowed(slot, rarity) && set >= 0 && set < SetNames.Length ? set : -1;
            it.Name = MakeName(it, r);
            return it;
        }

        /// <summary>The affix pool a slot rolls from (a copy; for reward previews and tests).</summary>
        public static AffixType[] PoolOf(GearSlot slot)
        {
            return (AffixType[])SlotPools[Mathf.Clamp((int)slot, 0, SlotCount - 1)].Clone();
        }

        private static string MakeName(GearItem it, System.Random r)
        {
            string[] nouns = SlotNouns[(int)it.Slot].Split('|');
            string noun = nouns[r.Next(nouns.Length)];
            if (it.Set >= 0) return SetNames[it.Set] + " " + noun;
            string pre = Prefix[r.Next(Prefix.Length)];
            if (it.Rarity >= Rarity.Legendary) return noun + " " + Suffix[r.Next(Suffix.Length)];
            if (it.Element != Element.None && r.NextDouble() < 0.6) pre = ElementName(it.Element);
            return pre + " " + noun;
        }

        public Rarity RollRarity(bool elite, float luck)
        {
            double x = _rng.NextDouble() * (1.0 - Mathf.Clamp01(luck) * 0.35);
            if (elite)
            {
                if (x < 0.07) return Rarity.Mythic;
                if (x < 0.32) return Rarity.Legendary;
                return Rarity.Epic;
            }
            if (x < 0.01) return Rarity.Legendary;
            if (x < 0.05) return Rarity.Epic;
            if (x < 0.18) return Rarity.Rare;
            if (x < 0.5) return Rarity.Uncommon;
            return Rarity.Common;
        }

        public int CurrentPower
        {
            get
            {
                int sp = Game.Missions != null ? Game.Missions.StoryProgress : 0;
                int lvl = Game.Progress != null ? Game.Progress.Level : 1;
                return 1 + sp / 2 + lvl / 5;
            }
        }

        // ------------------------------------------------------------------ inventory

        public bool Add(GearItem it, bool announce)
        {
            if (it == null) return false;
            if (Items.Count >= InventoryCap)
            {
                if (Game.Hud != null) Game.Hud.Notify("Gear inventory full. Salvage something at a shrine or in the Gear tab.", 3.5f);
                return false;
            }
            Items.Add(it);
            if (announce && Game.Hud != null) Game.Hud.LootPopup(it);
            Changed?.Invoke();
            return true;
        }

        /// <summary>04E: gives a piece built outside Generate (a seeded reward) its inventory uid. Idempotent.</summary>
        public void AdoptUid(GearItem it)
        {
            if (it != null && it.Uid <= 0) it.Uid = _nextUid++;
        }

        /// <summary>Room left in the gear inventory.</summary>
        public int FreeSpace { get { return Mathf.Max(0, InventoryCap - Items.Count); } }

        /// <summary>
        /// 04E: takes back a piece a reward just added (commit-or-rollback of a failed claim). No Lumen, no salvage: the
        /// piece simply leaves the inventory again. False when it is not in the inventory.
        /// </summary>
        public bool RemoveForRollback(GearItem it)
        {
            if (it == null || !Items.Contains(it)) return false;
            if (IsEquipped(it)) Equipped[(int)it.Slot] = null;
            Items.Remove(it);
            Recalculate();
            return true;
        }

        public GearItem FindUid(int uid)
        {
            if (uid <= 0) return null;
            for (int i = 0; i < Items.Count; i++) if (Items[i].Uid == uid) return Items[i];
            return null;
        }

        public void Equip(GearItem it)
        {
            if (it == null || !Items.Contains(it)) return;
            Equipped[(int)it.Slot] = it;
            Recalculate();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.WeaponSwitch, 0.5f, 0.9f);
        }

        public void Unequip(GearSlot slot)
        {
            Equipped[(int)slot] = null;
            Recalculate();
        }

        public bool IsEquipped(GearItem it)
        {
            return it != null && Equipped[(int)it.Slot] == it;
        }

        public static int SalvageValue(GearItem it)
        {
            return Mathf.RoundToInt((20 + 30 * (int)it.Rarity * (int)it.Rarity + it.Power * 6) * (1f + it.Upgrade * 0.5f));
        }

        public static int UpgradeCost(GearItem it)
        {
            return Mathf.RoundToInt((60 + 90 * (int)it.Rarity) * (it.Upgrade + 1) * (1f + it.Power * 0.08f));
        }

        public const int MaxUpgrade = 5;

        public bool Salvage(GearItem it)
        {
            if (it == null || !Items.Contains(it)) return false;
            if (IsEquipped(it)) Equipped[(int)it.Slot] = null;
            Items.Remove(it);
            if (Game.Progress != null) Game.Progress.AddLumen(SalvageValue(it));
            Recalculate();
            return true;
        }

        public bool TryUpgrade(GearItem it)
        {
            if (it == null || it.Upgrade >= MaxUpgrade || Game.Progress == null) return false;
            int cost = UpgradeCost(it);
            if (Game.Progress.Lumen < cost) return false;
            Game.Progress.Lumen -= cost;
            it.Upgrade++;
            Recalculate();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.MetalHit, 0.6f, 1.3f);
            return true;
        }

        // ------------------------------------------------------------------ totals

        public void Recalculate()
        {
            Array.Clear(_totals, 0, _totals.Length);
            Array.Clear(_setCount, 0, _setCount.Length);
            for (int s = 0; s < SlotCount; s++)
            {
                GearItem it = Equipped[s];
                if (it == null) continue;
                for (int i = 0; i < it.Affixes.Count; i++) _totals[(int)it.Affixes[i].Type] += it.Affixes[i].Value * it.UpgradeScale;
                if (it.Set >= 0) _setCount[it.Set]++;
            }
            // Skill trees: Elemental Affinity and Mastery add to the elemental proc chance (SkillTree calls Recalculate).
            _totals[(int)AffixType.ElementPower] += SkillMods.ElementProc;
            // Build sets (Phase 6): resolve the 2, 3 and 4 piece bonuses of the six build sets.
            GearBuilds.Refresh(_setCount);
            if (Game.Progress != null) Game.Progress.ApplyToPlayer(false);
            ApplyVisuals();
            Changed?.Invoke();
        }

        public float Total(AffixType t) { return _totals[(int)t]; }
        public int SetPieces(int set) { return set >= 0 && set < _setCount.Length ? _setCount[set] : 0; }
        private bool Set2(int s) { return _setCount[s] >= 2; }
        private bool Set4(int s) { return _setCount[s] >= 4; }

        public float BonusHealth { get { return Total(AffixType.MaxHealth); } }
        public float BonusStamina { get { return Total(AffixType.MaxStamina); } }
        public float BonusEnergy { get { return Total(AffixType.MaxEnergy); } }
        public float GunMultiplier { get { return 1f + Total(AffixType.GunDamage); } }
        public float MeleeMultiplierPreview { get { return 1f + Total(AffixType.MeleeDamage) + (Set2(0) ? 0.15f : 0f); } }
        public float PowerMultiplier { get { return 1f + Total(AffixType.PowerDamage) + GearBuilds.Current.PowerDamage; } }
        public float HealMultiplier { get { return 1f + Total(AffixType.TonicPotency); } }
        /// <summary>Gear parry window bonus; the 04E Frostglass ward aspect adds inside the same 0.12 s cap (BuildStatRules04E).</summary>
        public float ParryBonus { get { return BuildStatRules04E.ParryBonus(Total(AffixType.ParryWindow), Game.Builds != null && Game.Builds.FrostglassActive); } }

        /// <summary>
        /// 04E: the on-foot movement speed multiplier (MoveSpeed affixes plus a Void Screen window), from the one audited
        /// calculation in BuildStatRules04E. PlayerController multiplies its ground speed by this.
        /// </summary>
        public float MoveSpeedMultiplier
        {
            get { return BuildStatRules04E.MoveSpeedScale(Total(AffixType.MoveSpeed), Game.Gadgets != null && Game.Gadgets.VoidWindowActive); }
        }
        public float DodgeBonus { get { return Mathf.Min(0.16f, Total(AffixType.DodgeFrames) + (Set2(2) ? 0.08f : 0f)); } }
        public float LumenMultiplier { get { return 1f + Total(AffixType.LumenFind); } }

        public float GuardCostMultiplier
        {
            get { return Mathf.Max(0.35f, 1f - Total(AffixType.GuardEfficiency) - (Set2(1) ? 0.25f : 0f)); }
        }

        /// <summary>Overall damage taken (general armor), applied through CharacterHealth.DamageScale.</summary>
        public float ArmorMultiplier { get { return Mathf.Max(0.5f, 1f - Total(AffixType.Armor)) * GearBuilds.DamageTakenScale(); } }

        public float DamageTakenMultiplier(DamageType t)
        {
            float r = 0f;
            if (t == DamageType.Bullet) r = Total(AffixType.BulletResist);
            else if (t == DamageType.Melee) r = Total(AffixType.MeleeResist);
            else if (t == DamageType.Explosion) r = Total(AffixType.BlastResist);
            return Mathf.Max(0.4f, 1f - r);
        }

        public float MeleeMultiplier(WeaponId id)
        {
            float k = 1f + Total(AffixType.MeleeDamage) + (Set2(0) ? 0.15f : 0f);
            if (Total(AffixType.CritChance) > 0f && UnityEngine.Random.value < Total(AffixType.CritChance))
            {
                k *= 1.8f;
                _lastCrit = true;
            }
            else _lastCrit = false;
            return k;
        }

        private bool _lastCrit;

        /// <summary>Whether the last MeleeMultiplier roll was a gear critical (the skill crit roll skips such a hit).</summary>
        public bool LastCrit { get { return _lastCrit; } }

        public Element WeaponElement
        {
            get
            {
                GearItem core = Equipped[(int)GearSlot.Core];
                if (core != null && core.Element != Element.None) return core.Element;
                PlayerController p = Game.Player;
                if (p != null)
                {
                    WeaponId w = p.Weapons.Current.Id;
                    if (w == WeaponId.VoltKatana || w == WeaponId.ArcGauntlets) return Element.Volt;
                    if (w == WeaponId.CrystalMaul) return Element.Frost;
                }
                return Element.None;
            }
        }

        // ------------------------------------------------------------------ combat hooks

        /// <summary>Melee hit landed by the player: crit text, life steal, elemental procs, set effects.</summary>
        public void OnMeleeHit(CharacterBase target, MeleeMove move)
        {
            PlayerController p = Game.Player;
            if (p == null || target == null) return;
            if (_lastCrit && Game.Hud != null) Game.Hud.CombatText("Critical", new Color(1f, 0.85f, 0.3f));
            float dealt = target.Health.LastApplied;
            float steal = Total(AffixType.LifeSteal) + (WeaponElement == Element.Void ? 0.05f : 0f);
            if (steal > 0f && dealt > 0f) p.Health.Heal(dealt * steal);
            if (move.Kind == MoveKind.Riposte && Set4(0)) p.Health.Heal(p.Health.MaxHealth * 0.2f);
            Element e = WeaponElement;
            if (e == Element.None || target.IsDead) return;
            float chance = 0.18f + Total(AffixType.ElementPower) + (move.Kind == MoveKind.Heavy ? 0.2f : 0f);
            if (UnityEngine.Random.value > chance) return;
            Color c = ElementColor(e);
            if (Game.Fx != null) Game.Fx.Flash(target.ChestPosition, c, 3f, 5f, 0.25f);
            switch (e)
            {
                case Element.Volt:
                {
                    // Lightning arcs to the nearest other enemy.
                    if (Game.Fx != null) Game.Fx.Sparks(target.ChestPosition, Vector3.up * 2f, 10);
                    CharacterBase best = null;
                    float bd = 36f;
                    foreach (NpcController n in NpcController.All)
                    {
                        if (n == target || n.IsDead || !n.IsHostileToPlayer) continue;
                        float d = (n.transform.position - target.transform.position).sqrMagnitude;
                        if (d < bd) { bd = d; best = n; }
                    }
                    for (int ci = 0; ci < Creature.All.Count; ci++)
                    {
                        Creature cr = Creature.All[ci];
                        if (cr == target || !cr.Targetable) continue;
                        float d = (cr.transform.position - target.transform.position).sqrMagnitude;
                        if (d < bd) { bd = d; best = cr; }
                    }
                    if (best != null)
                    {
                        if (Game.Fx != null) Game.Fx.Tracer(target.ChestPosition, best.ChestPosition);
                        DamageInfo info = DamageInfo.Make(14f * (1f + Total(AffixType.ElementPower)), best.ChestPosition, best.ChestPosition - target.ChestPosition, p.gameObject, DamageType.Power);
                        info.Poise = 20f;
                        best.Health.TakeDamage(info);
                    }
                    break;
                }
                case Element.Ember:
                    target.Health.Burning = Mathf.Max(target.Health.Burning, 3.5f);
                    break;
                case Element.Frost:
                {
                    NpcController n = target as NpcController;
                    if (n != null) n.Poise -= n.MaxPoise * 0.4f;
                    Creature cr = target as Creature;
                    if (cr != null) cr.DrainPoise(cr.MaxPoise * 0.4f);
                    break;
                }
                case Element.Void:
                    p.Health.Heal(8f);
                    break;
            }
        }

        public void OnParry(NpcController attacker)
        {
            if (!Set4(1) || Game.Player == null) return;
            Vector3 c = Game.Player.transform.position;
            if (Game.Fx != null) Game.Fx.Flash(c + Vector3.up, ElementColor(Element.Frost), 6f, 9f, 0.5f);
            foreach (NpcController n in NpcController.All)
            {
                if (n.IsDead || n == attacker || !n.IsHostileToPlayer) continue;
                if ((n.transform.position - c).sqrMagnitude > 5f * 5f) continue;
                n.OnParried(Game.Player);
            }
            for (int i = 0; i < Creature.All.Count; i++)
            {
                Creature cr = Creature.All[i];
                if (!cr.Targetable) continue;
                float r = 5f + cr.Radius;
                if ((cr.transform.position - c).sqrMagnitude > r * r) continue;
                cr.OnParried(Game.Player);
            }
        }

        public void OnPerfectDodge()
        {
            if (!Set4(2) || Game.Abilities == null) return;
            CombatDirector.Slow(2f, 0.4f);
        }

        // ------------------------------------------------------------------ drops

        public void OnEnemyKilled(NpcController n)
        {
            if (n == null) return;
            float chance;
            switch (n.Role)
            {
                case NpcRole.Gang: chance = 0.07f; break;
                case NpcRole.Police: chance = 0.04f; break;
                case NpcRole.Swat: chance = 0.1f; break;
                case NpcRole.Enemy: chance = 0.09f; break;
                default: chance = 0f; break;
            }
            Vector3 at = n.transform.position;
            if (n.Elite)
            {
                SpawnDrop(at, Generate((GearSlot)_rng.Next(SlotCount), GearScore.Bump(RollRarity(true, Total(AffixType.LumenFind))), CurrentPower + 2 + GearScoreRules.GearPowerBonus(GearScore.Tier)), WeaponId.Count);
                // Shard-Touched sometimes carry a mythic weapon.
                if (UnityEngine.Random.value < 0.18f && Game.Player != null)
                {
                    WeaponId[] mythics = { WeaponId.VoltKatana, WeaponId.CrystalMaul, WeaponId.ArcGauntlets };
                    WeaponId w = mythics[_rng.Next(mythics.Length)];
                    if (!Game.Player.Weapons.Inventory.Has(w)) SpawnDrop(at + Vector3.right * 1.2f, null, w);
                }
                return;
            }
            if (UnityEngine.Random.value < chance * (1f + Total(AffixType.LumenFind)))
                SpawnDrop(at, Generate((GearSlot)_rng.Next(SlotCount), GearScore.Bump(RollRarity(false, Total(AffixType.LumenFind))), CurrentPower + GearScoreRules.GearPowerBonus(GearScore.Tier)), WeaponId.Count);
        }

        /// <summary>A light beam in the item's rarity colour: walk into it to pick it up.</summary>
        public void SpawnDrop(Vector3 pos, GearItem it, WeaponId weapon)
        {
            Vector3 g = PedManager.Ground(pos);
            Color c = it != null ? RarityColor(it.Rarity) : RarityColor(Rarity.Mythic);
            GameObject go = new GameObject(it != null ? "Loot " + it.Name : "Loot " + weapon);
            go.transform.position = g;
            GameObject beam = Shapes.MeshObject("Beam", Shapes.CylinderMesh, new[] { Mats.Glow(new Color(c.r, c.g, c.b, 0.5f)) }, go.transform, false);
            beam.transform.localPosition = new Vector3(0f, 3f, 0f);
            beam.transform.localScale = new Vector3(0.18f, 3f, 0.18f);
            GameObject icon = Shapes.MeshObject("Item", ShrineMeshes.Crystal, new[] { Mats.Emissive(c, 2.4f) }, go.transform, false);
            icon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            icon.transform.localScale = Vector3.one * 0.35f;
            _drops.Add(new LootDrop { Item = it, Weapon = weapon, Go = go, Icon = icon.transform, Pos = g });
            if (Game.Audio != null) Game.Audio.PlayAt(SfxId.CrystalChime, g, it != null && it.Rarity >= Rarity.Legendary ? 0.8f : 0.45f, 1.2f, 60f);
        }

        public void GrantStarterKit()
        {
            Add(Generate(GearSlot.Shell, Rarity.Rare, 2), false);
            Add(Generate(GearSlot.Gauntlets, Rarity.Uncommon, 2), false);
            Add(Generate(GearSlot.Core, Rarity.Epic, 2), false);
            foreach (GearItem it in Items.ToArray()) Equip(it);
        }

        private void Update()
        {
            TickFittings04N();
            PlayerController p = Game.Player;
            float t = Time.time;
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                LootDrop d = _drops[i];
                d.Age += Time.deltaTime;
                if (d.Go == null) { _drops.RemoveAt(i); continue; }
                d.Icon.localRotation = Quaternion.Euler(0f, t * 120f, 0f);
                d.Icon.localPosition = new Vector3(0f, 0.8f + Mathf.Sin(t * 3f + i) * 0.1f, 0f);
                if (d.Age > 300f) { Destroy(d.Go); _drops.RemoveAt(i); continue; }
                if (p == null || p.IsDead || p.InVehicle) continue;
                if ((p.transform.position - d.Pos).sqrMagnitude > 1.9f * 1.9f) continue;
                bool taken = false;
                if (d.Item != null) taken = Add(d.Item, true);
                else if (d.Weapon != WeaponId.Count)
                {
                    p.Weapons.Inventory.Give(d.Weapon, 0);
                    if (Game.Hud != null) Game.Hud.Banner("MYTHIC WEAPON", Weapons.Get(d.Weapon).Name + ": " + Weapons.Get(d.Weapon).Blurb, RarityColor(Rarity.Mythic), 4.5f);
                    taken = true;
                }
                if (taken)
                {
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.6f, 1.1f);
                    Pad.Rumble(0.2f, 0.4f, 0.15f);
                    Destroy(d.Go);
                    _drops.RemoveAt(i);
                }
            }
            if (_lifeStealCd > 0f) _lifeStealCd -= Time.deltaTime;
        }

        // ------------------------------------------------------------------ visuals

        /// <summary>Equipped armour shows on Malik: visor, shell plates, gauntlets, striders and the glow colour of the best piece.</summary>
        public Appearance MergeVisuals(Appearance look)
        {
            look.VisorStyle = Equipped[(int)GearSlot.Visor] != null ? 1 + (Equipped[(int)GearSlot.Visor].Seed & 1) : 0;
            look.ShellStyle = Equipped[(int)GearSlot.Shell] != null ? 1 + (Equipped[(int)GearSlot.Shell].Seed & 0x7fffffff) % 3 : 0;
            look.GauntletStyle = Equipped[(int)GearSlot.Gauntlets] != null ? 1 : 0;
            look.StriderStyle = Equipped[(int)GearSlot.Striders] != null ? 1 : 0;
            Rarity best = Rarity.Common;
            bool any = false;
            for (int s = 0; s < SlotCount; s++) if (Equipped[s] != null) { any = true; if (Equipped[s].Rarity > best) best = Equipped[s].Rarity; }
            if (any)
            {
                look.Glow = 1 + GlowPaletteFor(best);
                look.ArmorColor = ArmorPaletteFor(best);
            }
            return look;
        }

        public void ApplyVisuals()
        {
            PlayerController p = Game.Player;
            if (p == null || p.Rig == null) return;
            Appearance cur = p.Rig.Look;
            Appearance look = MergeVisuals(cur);
            if (look.VisorStyle == cur.VisorStyle && look.ShellStyle == cur.ShellStyle && look.GauntletStyle == cur.GauntletStyle && look.StriderStyle == cur.StriderStyle && look.Glow == cur.Glow && look.ArmorColor == cur.ArmorColor) return;
            p.Rig.ChangeLook(look);
            p.Weapons.RefreshModel();
        }

        /// <summary>Armour plate colour (palette row 5) for each rarity: gunmetal up to iridescent obsidian.</summary>
        public static int ArmorPaletteFor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return 5;
                case Rarity.Rare: return 7;
                case Rarity.Epic: return 8;
                case Rarity.Legendary: return 4;
                case Rarity.Mythic: return 13;
                default: return 0;
            }
        }

        /// <summary>Neon palette column used for the suit seams of each rarity.</summary>
        public static int GlowPaletteFor(Rarity r)
        {
            switch (r)
            {
                case Rarity.Uncommon: return 3;
                case Rarity.Rare: return 1;
                case Rarity.Epic: return 4;
                case Rarity.Legendary: return 2;
                case Rarity.Mythic: return 5;
                default: return 0;
            }
        }

        // ------------------------------------------------------------------ save

        public void Capture(SaveData d)
        {
            d.Gear = new List<GearSave>();
            foreach (GearItem it in Items)
            {
                GearSave g = new GearSave { Uid = it.Uid, Slot = (int)it.Slot, Rarity = (int)it.Rarity, Power = it.Power, Upgrade = it.Upgrade, Set = it.Set, Element = (int)it.Element, Seed = it.Seed, Name = it.Name };
                foreach (Affix a in it.Affixes)
                {
                    g.AffixTypes.Add((int)a.Type);
                    g.AffixValues.Add(a.Value);
                }
                d.Gear.Add(g);
            }
            d.Equipped = new List<int>();
            for (int s = 0; s < SlotCount; s++) d.Equipped.Add(Equipped[s] != null ? Equipped[s].Uid : 0);
        }

        public void Restore(SaveData d)
        {
            ResetAll();
            if (d.Gear == null) return;
            int maxUid = 0;
            foreach (GearSave g in d.Gear)
            {
                GearItem it = new GearItem
                {
                    Uid = g.Uid, Slot = (GearSlot)Mathf.Clamp(g.Slot, 0, SlotCount - 1), Rarity = (Rarity)Mathf.Clamp(g.Rarity, 0, 5), Power = Mathf.Max(1, g.Power),
                    Upgrade = Mathf.Clamp(g.Upgrade, 0, MaxUpgrade), Set = g.Set >= 0 && g.Set < SetNames.Length ? g.Set : -1, Element = (Element)Mathf.Clamp(g.Element, 0, 4), Seed = g.Seed, Name = string.IsNullOrEmpty(g.Name) ? "Relic" : g.Name
                };
                for (int i = 0; i < g.AffixTypes.Count && i < g.AffixValues.Count; i++)
                {
                    if (g.AffixTypes[i] < 0 || g.AffixTypes[i] >= (int)AffixType.Count) continue;
                    it.Affixes.Add(new Affix { Type = (AffixType)g.AffixTypes[i], Value = g.AffixValues[i] });
                }
                Items.Add(it);
                maxUid = Mathf.Max(maxUid, it.Uid);
            }
            _nextUid = maxUid + 1;
            if (d.Equipped != null)
            {
                for (int s = 0; s < SlotCount && s < d.Equipped.Count; s++)
                {
                    int uid = d.Equipped[s];
                    if (uid == 0) continue;
                    GearItem it = Items.Find(x => x.Uid == uid);
                    if (it != null && (int)it.Slot == s) Equipped[s] = it;
                }
            }
            Recalculate();
        }

        public void ResetAll()
        {
            ClearFittings04N();
            Items.Clear();
            for (int s = 0; s < SlotCount; s++) Equipped[s] = null;
            foreach (LootDrop dr in _drops) if (dr.Go != null) Destroy(dr.Go);
            _drops.Clear();
            _nextUid = 1;
            Recalculate();
        }
    }
}
