using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>The broad kind of a 04E reward (display and the economy sheet).</summary>
    public enum RewardKind04E
    {
        Ward = 0,
        Kit = 1,
        GearChoice = 2,
        GearPiece = 3,
        KeyItem = 4,
        Materials = 5,
        Aspect = 6,
        Recipe = 7,
        Fusion = 8,
        Trim = 9,
        Chassis = 10,
        Loadout = 11,
        Bond = 12,
        Cosmetic = 13,
        Module = 14,
        ModChoice = 15,
        Pinnacle = 16
    }

    /// <summary>What one delivered part of a reward is (each goes through an existing inventory or unlock API).</summary>
    public enum RewardPartKind04E
    {
        /// <summary>A gear piece (GearSystem), rebuilt exactly from its seed.</summary>
        Gear = 0,
        /// <summary>A loot gun (LootArmory backpack), rebuilt exactly from its seed.</summary>
        Gun = 1,
        /// <summary>Crafting materials (MaterialStore).</summary>
        Material = 2,
        /// <summary>A gadget mod (GadgetSystem).</summary>
        Mod = 3,
        /// <summary>A Forge recipe granted for good (CraftingSystem.Grant).</summary>
        Recipe = 4,
        /// <summary>The starter ward entitlement (BuildSystem, opens the Buckler early).</summary>
        Ward = 5,
        /// <summary>A ward aspect (BuildSystem).</summary>
        Aspect = 6,
        /// <summary>The Fracture Lattice focus module (BuildSystem, fitted to a focus).</summary>
        Module = 7,
        /// <summary>A key item the BUILD page lists (the crystal coupler).</summary>
        KeyItem = 8,
        /// <summary>A BuildSystem unlock flag (second loadout, ward visual, survey notes, home display, Kiln Crown).</summary>
        Unlock = 9,
        /// <summary>The persistent fusion entitlement (PersonalGarage.UnlockFusion).</summary>
        Fusion = 10,
        /// <summary>A fusion form (PersonalGarage.UnlockForm).</summary>
        Form = 11,
        /// <summary>A chassis trim (PersonalGarage.UnlockTrim, then ApplyTrim).</summary>
        Trim = 12,
        /// <summary>An owned chassis class (PersonalGarage.GrantChassis).</summary>
        Chassis = 13,
        /// <summary>A companion raised to at least a rank (BondSystem.RaiseRank).</summary>
        BondRank = 14,
        /// <summary>A companion's cosmetic evolution stage (CompanionRecord.Evolution, never lowered).</summary>
        BondEvolve = 15
    }

    /// <summary>
    /// One part of an offer. Everything needed to rebuild the exact item is stored here (seed, slot, rarity, power,
    /// forced affixes), so a pending reward always resolves to the same thing however often it is opened.
    /// </summary>
    public sealed class RewardPart04E
    {
        public RewardPartKind04E Kind;
        /// <summary>Material, recipe, aspect, key item, unlock, form, trim or chassis id, or a companion archetype.</summary>
        public string Id = "";
        /// <summary>Material count, companion rank or evolution stage.</summary>
        public int Count = 1;
        public int Seed = 1;
        public Rarity Rarity;
        // Gear.
        public GearSlot Slot;
        public int Power = 1;
        public Element Element;
        public AffixType[] Forced = new AffixType[0];
        /// <summary>A starter kit piece: skipped (not a failure) when the player already owns as good a piece.</summary>
        public bool KitPiece;
        // Guns.
        public int Archetype = -1;
        public int Level = 1;
        // Gadget mods.
        public int ModStat;
        public float ModValue;

        /// <summary>The gear piece this part stands for (a fresh object each call, uid 0).</summary>
        public GearItem BuildGear()
        {
            return GearSystem.GenerateSeeded(Slot, Rarity, Power, Seed, Forced, Element, -1);
        }

        /// <summary>The loot gun this part stands for (a fresh object each call, uid 0).</summary>
        public LootGun BuildGun()
        {
            return LootGenerator.Generate(Seed, UnityEngine.Mathf.Clamp(Level, 1, LootGenerator.MaxLevel), Rarity, Archetype, -1, -1);
        }

        /// <summary>The gadget mod this part stands for (uid assigned by the gadget system).</summary>
        public GadgetModSave BuildMod()
        {
            return new GadgetModSave { Stat = ModStat, Rarity = (int)Rarity, Value = ModValue };
        }
    }

    /// <summary>One offer of a reward (a single reward has one; a choice has two or three).</summary>
    public sealed class RewardOffer04E
    {
        public string Title = "";
        /// <summary>Lines describing the offer (affixes, effect, materials).</summary>
        public string Detail = "";
        public readonly List<RewardPart04E> Parts = new List<RewardPart04E>();
        /// <summary>Lumen from salvaging the gear pieces and gadget mods (GearSystem.SalvageValue, GadgetRules.ModSalvageValue).</summary>
        public int SalvageLumen;
        /// <summary>Cash from selling the loot guns and materials (LootEconomy.SellValue, a Forgewright's MaterialDef.Value).</summary>
        public int SellCash;
        /// <summary>Both valuations added, only for "is there anything to sell" checks (never shown as one number: the currencies differ).</summary>
        public int SalvageValue { get { return SalvageLumen + SellCash; } }
        /// <summary>The gear slot it compares against (-1 when it is not a gear piece).</summary>
        public int CompareSlot = -1;
    }

    /// <summary>A catalog entry: one of the 32 RewardIds04E definitions.</summary>
    public sealed class RewardDef04E
    {
        public string Id;
        public string Title;
        public RewardKind04E Kind;
        /// <summary>The intended level and power band, in words.</summary>
        public string PowerBand = "";
        /// <summary>What it is and what it changes in play.</summary>
        public string Blurb = "";
        /// <summary>Where it comes from in the campaign (the economy sheet).</summary>
        public string Source = "";
        /// <summary>How it can be improved later (upgrade path).</summary>
        public string Upgrade = "";
        public int OfferCount = 1;
        /// <summary>A signature reward: granted once per save by its first-win or quest token.</summary>
        public bool Signature;

        public bool IsChoice { get { return OfferCount > 1; } }
    }
}
