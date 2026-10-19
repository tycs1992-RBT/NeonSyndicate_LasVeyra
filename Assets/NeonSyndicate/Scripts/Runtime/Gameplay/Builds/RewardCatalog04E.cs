using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04E reward catalog: every RewardIds04E id with its kind, power band, display text, sources, upgrade path and a
    /// deterministic offer list. Offers depend only on what the ledger stored when the reward was granted (the seed from
    /// the token, the item power and loot level then, the build suggestion then), so reopening a pending reward, reloading
    /// or leaving never rerolls it. Engine-free, tested by Builds04ETests.
    /// </summary>
    public static class RewardCatalog04E
    {
        public const string KeyCoupler = "coupler";
        public const string ModuleFracture = "fracture_lattice";
        public const string UnlockLoadout2 = "loadout_slot2";
        public const string UnlockWardVisual = "ward_visual_mender";
        public const string UnlockSurvey = "map_survey";
        public const string UnlockHomeDisplay = "home_display";
        public const string UnlockKilnCrown = "kiln_crown";
        public const string RecipeCoolant = "brew_coolant_draught";
        public const string RecipeLantern = "brew_lantern_oil";

        /// <summary>
        /// The garage ids the chassis rewards unlock (PersonalGarage.UnlockTrim): the first appearance choice offers three
        /// looks, the second trim reward the other two, and the handling sidegrade the grip tune. Every id must be known to
        /// the garage (ChassisTrims.IsKnown), which a test checks; an unknown id would deliver nothing.
        /// </summary>
        public static readonly string[] TrimIds = { ChassisTrims.EmberFins, ChassisTrims.QuartzCrown, ChassisTrims.FrostPlates, ChassisTrims.StormRails, ChassisTrims.CouplerGlow, FusionRules.TuneGrip };

        private static Dictionary<string, RewardDef04E> _defs;

        public static void ResetStatics() { _defs = null; }

        private static Dictionary<string, RewardDef04E> Defs
        {
            get
            {
                if (_defs == null) _defs = Build();
                return _defs;
            }
        }

        public static RewardDef04E Get(string id)
        {
            RewardDef04E d;
            return id != null && Defs.TryGetValue(id, out d) ? d : null;
        }

        public static bool Has(string id) { return Get(id) != null; }

        public static int Count { get { return Defs.Count; } }

        private static RewardDef04E D(Dictionary<string, RewardDef04E> m, string id, string title, RewardKind04E kind, int offers, string band, string blurb, string source, string upgrade, bool signature = false)
        {
            RewardDef04E d = new RewardDef04E { Id = id, Title = title, Kind = kind, OfferCount = offers, PowerBand = band, Blurb = blurb, Source = source, Upgrade = upgrade, Signature = signature };
            m[id] = d;
            return d;
        }

        private static Dictionary<string, RewardDef04E> Build()
        {
            Dictionary<string, RewardDef04E> m = new Dictionary<string, RewardDef04E>();
            D(m, "uh_ward_starter", "Starter Ward", RewardKind04E.Ward, 1, "Early: the Buckler at 80% capacity until level 11, then full",
                "Unlocks the handheld Lattice Shield (Buckler) now, whatever your level, and puts it in a free gadget slot. Raise it to soak hits from the front.",
                "A Knock Beneath Home (Nessa, Cinder Dispatch)", "Gadget mods (Overclock) and the Kiln Crown raise its capacity; aspects add functions.");
            D(m, "uh_kit_wardkeeper", "Wardkeeper starter kit", RewardKind04E.Kit, 1, "Early: Rare Shell and Uncommon Gauntlets, item power 1 to 3",
                "A defensive Shell (health, armor, bullet resistance) and guard Gauntlets (guard cost, parry window). Pieces you already beat are skipped.",
                "First Wardkeeper fitting (once per save)", "Upgrade at a shrine with Lumen (+12% affixes per step, five steps).");
            D(m, "uh_kit_conduit", "Conduit starter kit", RewardKind04E.Kit, 1, "Early: Rare Implant and Uncommon Visor, item power 1 to 3, plus 4 Coolant Salt",
                "An energy Implant (crystal energy, power damage), a Visor (energy, critical chance) and Coolant Salt for the Coolant Draught. Pieces you already beat are skipped.",
                "First Conduit fitting (once per save)", "Upgrade at a shrine with Lumen.");
            D(m, "uh_kit_riftstrider", "Riftstrider starter kit", RewardKind04E.Kit, 1, "Early: Rare Striders and Uncommon Visor, item power 1 to 3",
                "Movement Striders (movement speed, dodge invulnerability, stamina) and a Visor (parry window, critical chance). Pieces you already beat are skipped.",
                "First Riftstrider fitting (once per save)", "Upgrade at a shrine with Lumen.");
            D(m, "uh_gear_choice_early", "Early gear choice", RewardKind04E.GearChoice, 2, "Early: Rare, item power 2 or more",
                "Choose one of two real pieces matched to your build suggestion (defensive or offensive), plus a small cache: 2 Ember Crystal and 2 Coolant Salt.",
                "Sparks in the Walls (the Pressure House)", "Upgrade at a shrine; salvage the one you outgrow.");
            D(m, "uh_descent_piece", "Descent progression piece", RewardKind04E.GearPiece, 1, "Early to mid: Rare (Epic from item power 6), item power +1, three guaranteed build affixes",
                "A guaranteed improvement for the build you chose: Shell for the Wardkeeper, Implant for the Conduit, Striders for the Riftstrider. Never below Rare.",
                "Valve Guardian first win (The First Descent)", "Upgrade at a shrine.", true);
            D(m, "uh_coupler_part", "Crystal coupler", RewardKind04E.KeyItem, 1, "Key item plus 2 Regulator Cog and 1 Ember Crystal",
                "The chassis-tuning coupler Jax needs for the first fusion, with spare cogs for chassis work.",
                "The First Descent (cooled sample returned)", "Jax fits it at the garage bay; it is never consumed or sold.");
            D(m, "uh_fusion_license", "Fusion entitlement", RewardKind04E.Fusion, 1, "Signature unlock",
                "Persistent permission to fuse your companion with your personal chassis away from Jax's bay, and the Ventwing form.",
                "Two Hearts, One Chassis (Jax)", "New forms come with new bonds.", true);
            D(m, "uh_trim_choice", "First chassis appearance", RewardKind04E.Trim, 3, "Cosmetic (no combat power)",
                "Pick a trim for your personal chassis: ember fins on the tail, a quartz crown on the nose and tail, or frostglass flank plates.",
                "Two Hearts, One Chassis (Jax)", "More trims from Jax's side work.");
            D(m, "uh_augment_choice", "Dante's augment", RewardKind04E.ModChoice, 3, "Mid: Rare gadget mod",
                "Choose one Rare gadget mod: Quickcharge (shorter cooldowns), Overclock (stronger gadgets and ward capacity) or Longcell (longer gadgets).",
                "Glass Under Pressure (Dante)", "Socket it in the GADGETS tab; salvage spares for Lumen.");
            D(m, "uh_recipe_coolant", "Coolant Draught recipe", RewardKind04E.Recipe, 1, "Utility recipe plus 3 Coolant Salt",
                "Teaches the Forge brew Coolant Draught: 8 minutes of 30% less heat damage and 6% less damage overall. Brewable anywhere.",
                "Glass Under Pressure (Dante)", "Brew it again with Coolant Salt and Ash Resin.");
            D(m, "uh_matron_aspect", "Ember Mantle", RewardKind04E.Aspect, 1, "Signature ward aspect",
                "A ward aspect that absorbs a bounded share of heat from a refilling buffer and smothers burning. Changes the ward's look.",
                "The Cinder Matron first win", "Switch aspects for free out of combat.", true);
            D(m, "uh_matron_gear_choice", "Matron's gear choice", RewardKind04E.GearChoice, 2, "Mid: Epic gear (item power +2) or a Legendary crystal caster (loot level +2)",
                "Choose a heat-hardened Epic Shell (health, armor, blast resistance, Ember) or a Legendary crystal caster. Either comes with 1 Matron Ember.",
                "The Cinder Matron first win (stored even with a full inventory)", "Shrine upgrades for the Shell; the Forge's ARMORY for the caster.", true);
            D(m, "uh_vault_module", "Fracture Lattice", RewardKind04E.Module, 1, "Signature focus module plus 1 Vaultbreaker Core",
                "A module fitted to one crystal focus: its direct hits split fracture shards into other foes you can see nearby. Changes how the focus fires.",
                "The Vaultbreaker first win", "Refit it to another focus for free out of combat (BUILD > WARD).", true);
            D(m, "uh_loadout_slot2", "Second loadout", RewardKind04E.Loadout, 1, "Utility unlock",
                "A second saved loadout on the BUILD page: switch between two complete setups out of combat.",
                "What We Choose to Keep", "None needed.");
            D(m, "uh_regent_pinnacle", "Regent pinnacle", RewardKind04E.Pinnacle, 3, "Pinnacle: Mythic",
                "Choose a Mythic Shell, the Mythic ember rifle Skyline Requiem, or the Kiln Crown (ward capacity +25% for good and a crown on the ward). Each comes with 1 Regent Heart.",
                "The Kiln Regent first win (recoverable from the home ledger)", "Mythic pieces still take shrine and ARMORY upgrades.", true);
            D(m, "uh_bond_awaken", "Final bond evolution", RewardKind04E.Bond, 3, "Signature bond rank",
                "Raise one bonded companion to Awakened, its final rank and evolution. A companion you have not bonded yet cannot be chosen until you have.",
                "The Kiln Regent first win", "Ranks never go down.", true);
            D(m, "uh_frostglass_aspect", "Frostglass", RewardKind04E.Aspect, 1, "Ward aspect",
                "A ward aspect with a short deflection window when raised and a slightly longer parry window. Faceted glass ward.",
                "Optional reward (campaign data decides where)", "Switch aspects for free out of combat.");
            D(m, "uh_volt_aspect", "Volt Lattice", RewardKind04E.Aspect, 1, "Ward aspect",
                "A ward aspect whose blocks charge a controlled discharge into nearby foes. Open lattice ward with coil nodes.",
                "Optional reward (campaign data decides where)", "Switch aspects for free out of combat.");
            D(m, "uh_void_aspect", "Void Screen", RewardKind04E.Aspect, 1, "Ward aspect",
                "A ward aspect that turns a break into a short repositioning window, at the cost of a longer recovery. Hollow ring ward.",
                "Optional reward (campaign data decides where)", "Switch aspects for free out of combat.");
            D(m, "uh_trim_second", "Second chassis trim", RewardKind04E.Trim, 2, "Cosmetic (no combat power)",
                "Pick a second chassis trim: Jax's storm rails low along the hull or a glowing coupler line on the fairing.",
                "Jax's Impossible Garage", "None needed.");
            D(m, "uh_handling_sidegrade", "Handling sidegrade", RewardKind04E.Trim, 1, "Sidegrade (a trade, not more power)",
                "The canal grip tune for your personal chassis: more grip and steadier braking, a little less top speed. It is switched on when claimed.",
                "Jax's Impossible Garage", "Switch it on or off at Jax's bay.");
            D(m, "uh_chassis_cruiser", "Cruiser chassis", RewardKind04E.Chassis, 1, "Second compatible vehicle class",
                "Adds the cruiser hover bike to your owned chassis.",
                "Jax's Impossible Garage", "Paint and upgrades at the garage.");
            D(m, "uh_care_kit", "Care kit", RewardKind04E.Materials, 1, "Consumables: 5 Coolant Salt, 2 Ash Resin, 2 Ember Crystal",
                "Treatment supplies for companion care and upgrades.",
                "Mara's Quiet Work", "Spent on bond upgrades and brews.");
            D(m, "uh_ward_visual", "Mender's Halo", RewardKind04E.Cosmetic, 1, "Cosmetic (no combat power)",
                "A soft halo ring on your ward. Toggle it on the BUILD page.",
                "Mara's Quiet Work", "None needed.");
            D(m, "uh_map_knowledge", "Survey notes", RewardKind04E.Cosmetic, 1, "Map knowledge",
                "The pause map marks rest points and shortcut gates in the Underheart regions you have discovered, and the optional elite site.",
                "Iris's Missing Maps", "None needed.");
            D(m, "uh_elite_choice", "Elite equipment choice", RewardKind04E.GearChoice, 3, "High: Legendary, item power or loot level +2",
                "Choose a Legendary piece for your build, a Legendary Weapon Core, or a Legendary loot gun. Each comes with 1 Pyre Seed-Core.",
                "Iris's Missing Maps (the optional elite site)", "Shrine and ARMORY upgrades.");
            D(m, "uh_home_display", "Home trophy display", RewardKind04E.Cosmetic, 1, "Home change (no combat power)",
                "A lit trophy case beside your home reward ledger that shows each Underheart guardian you have beaten.",
                "What We Choose to Keep", "Fills as you win.");
            D(m, "uh_utility_recipe", "Lantern Oil recipe", RewardKind04E.Recipe, 1, "Utility recipe plus 1 Ember Crystal and 2 Coolant Salt",
                "Teaches the Forge brew Lantern Oil: 10 minutes of better creature loot rolls and faster stamina recovery.",
                "The Home We Keep", "Brew it again with Ember Crystal and Coolant Salt.");
            D(m, "uh_bond_storm_evolve", "Stormskimmer evolution", RewardKind04E.Bond, 1, "Cosmetic evolution",
                "Your Stormskimmer's earned evolution stage (look only). Needs the Stormskimmer bonded.",
                "A Road for the Living arc", "Evolution never goes down.");
            D(m, "uh_bond_ash_evolve", "Ashshell Warden evolution", RewardKind04E.Bond, 1, "Cosmetic evolution",
                "Your Ashshell Warden's earned evolution stage (look only). Needs the Warden bonded.",
                "Gardens That Burn arc", "Evolution never goes down.");
            D(m, "uh_bond_drake_evolve", "Quartz Drakeling evolution", RewardKind04E.Bond, 1, "Cosmetic evolution",
                "Your drakeling's earned evolution stage (look only), shown during What We Choose to Keep.",
                "What We Choose to Keep", "Evolution never goes down.");
            D(m,VanguardRules04K.Reward,"Recovered Vanguard Caster",RewardKind04E.GearPiece,1,"Epic crystal caster",
                "A recovered, nonexclusive caster. A full backpack leaves it pending in BUILD rewards.",
                "The Fractured Bell expedition", "Upgrade or salvage through the existing Arsenal.");
            return m;
        }

        // ------------------------------------------------------------------ deterministic offers

        /// <summary>A stable positive seed for one part of one offer, mixed from the entry seed.</summary>
        public static int PartSeed(int seed, int offer, int part)
        {
            unchecked
            {
                uint h = 2166136261u;
                h = (h ^ (uint)seed) * 16777619u;
                h = (h ^ (uint)(offer + 1) * 2654435761u) * 16777619u;
                h = (h ^ (uint)(part + 7) * 40503u) * 16777619u;
                int s = (int)(h & 0x7fffffffu);
                return s == 0 ? 1 : s;
            }
        }

        /// <summary>The offers of a ledger entry (from the values it stored at grant time).</summary>
        public static List<RewardOffer04E> Offers(RewardEntrySave e)
        {
            if (e == null) return new List<RewardOffer04E>();
            return Offers(e.Reward, e.Seed, e.Power, e.Level, e.Preset);
        }

        /// <summary>
        /// The offers of a reward for a stored seed, item power, loot level and build suggestion. Same arguments, same
        /// offers, always. An unknown id gives an empty list.
        /// </summary>
        public static List<RewardOffer04E> Offers(string rewardId, int seed, int power, int level, string preset)
        {
            List<RewardOffer04E> list = new List<RewardOffer04E>();
            RewardDef04E def = Get(rewardId);
            if (def == null) return list;
            int pw = Mathf.Clamp(power, 1, 30);
            int lv = Mathf.Clamp(level, 1, LootGenerator.MaxLevel);
            int kit = Mathf.Clamp(pw, 1, 3);
            switch (rewardId)
            {
                case VanguardRules04K.Reward:
                    list.Add(Offer("Recovered crystal caster",seed,0,Gun(LootArchetype.CrystalCaster,Rarity.Epic,lv)));
                    break;
                case "uh_ward_starter":
                    list.Add(Single("Starter Ward: Lattice Shield (Buckler)", "Unlocks the handheld ward now. " + WardLine(), P(RewardPartKind04E.Ward, "buckler")));
                    break;
                case "uh_kit_wardkeeper":
                    list.Add(Offer("Wardkeeper kit", seed, 0,
                        Kit(GearSlot.Shell, Rarity.Rare, kit, AffixType.MaxHealth, AffixType.Armor, AffixType.BulletResist),
                        Kit(GearSlot.Gauntlets, Rarity.Uncommon, kit, AffixType.GuardEfficiency, AffixType.ParryWindow)));
                    break;
                case "uh_kit_conduit":
                    list.Add(Offer("Conduit kit", seed, 0,
                        Kit(GearSlot.Implant, Rarity.Rare, kit, AffixType.MaxEnergy, AffixType.PowerDamage),
                        Kit(GearSlot.Visor, Rarity.Uncommon, kit, AffixType.MaxEnergy, AffixType.CritChance),
                        Mat(MaterialIds04E.CoolantSalt, 4)));
                    break;
                case "uh_kit_riftstrider":
                    list.Add(Offer("Riftstrider kit", seed, 0,
                        Kit(GearSlot.Striders, Rarity.Rare, kit, AffixType.MoveSpeed, AffixType.DodgeFrames, AffixType.MaxStamina),
                        Kit(GearSlot.Visor, Rarity.Uncommon, kit, AffixType.ParryWindow, AffixType.CritChance)));
                    break;
                case "uh_gear_choice_early":
                {
                    GearSlot defSlot, offSlot;
                    AffixType[] defAff, offAff;
                    EarlySlots(preset, out defSlot, out defAff, out offSlot, out offAff);
                    int p2 = Mathf.Max(2, pw);
                    list.Add(Offer("Defensive", seed, 0, G(defSlot, Rarity.Rare, p2, Element.None, defAff), Mat(MaterialIds04E.EmberCrystal, 2), Mat(MaterialIds04E.CoolantSalt, 2)));
                    list.Add(Offer("Offensive", seed, 1, G(offSlot, Rarity.Rare, p2, Element.None, offAff), Mat(MaterialIds04E.EmberCrystal, 2), Mat(MaterialIds04E.CoolantSalt, 2)));
                    break;
                }
                case "uh_descent_piece":
                {
                    GearSlot s;
                    AffixType[] a;
                    SignaturePiece(preset, out s, out a);
                    Rarity r = pw + 1 >= 6 ? Rarity.Epic : Rarity.Rare;
                    list.Add(Offer(BuildTitle(preset) + " piece", seed, 0, G(s, r, pw + 1, Element.None, a)));
                    break;
                }
                case "uh_coupler_part":
                    list.Add(Single("Crystal coupler", "Key item for Jax's bay, plus 2 Regulator Cog and 1 Ember Crystal.",
                        P(RewardPartKind04E.KeyItem, KeyCoupler), Mat(MaterialIds04E.RegulatorCog, 2), Mat(MaterialIds04E.EmberCrystal, 1)));
                    break;
                case "uh_fusion_license":
                    list.Add(Single("Fusion entitlement", "Fuse with your personal chassis anywhere the fusion rules allow; the Ventwing form is unlocked.",
                        P(RewardPartKind04E.Fusion, "license"), P(RewardPartKind04E.Form, FusionIds.Ventwing)));
                    break;
                case "uh_trim_choice":
                    list.Add(Single(ChassisTrims.Title(ChassisTrims.EmberFins), "Copper ember fins rising from the tail (look only).", P(RewardPartKind04E.Trim, ChassisTrims.EmberFins)));
                    list.Add(Single(ChassisTrims.Title(ChassisTrims.QuartzCrown), "A crown of quartz spikes on the nose and tail (look only).", P(RewardPartKind04E.Trim, ChassisTrims.QuartzCrown)));
                    list.Add(Single(ChassisTrims.Title(ChassisTrims.FrostPlates), "Pale frostglass plates on both flanks (look only).", P(RewardPartKind04E.Trim, ChassisTrims.FrostPlates)));
                    break;
                case "uh_augment_choice":
                    list.Add(ModOffer(seed, 0, GadgetModStat.Cooldown));
                    list.Add(ModOffer(seed, 1, GadgetModStat.Power));
                    list.Add(ModOffer(seed, 2, GadgetModStat.Duration));
                    break;
                case "uh_recipe_coolant":
                    list.Add(Single("Coolant Draught recipe", "A Forge brew: 8 minutes of 30% less heat damage and 6% less damage overall. Plus 3 Coolant Salt.",
                        P(RewardPartKind04E.Recipe, RecipeCoolant), Mat(MaterialIds04E.CoolantSalt, 3)));
                    break;
                case "uh_matron_aspect":
                    list.Add(AspectOffer(WardRules04E.EmberMantle));
                    break;
                case "uh_matron_gear_choice":
                    list.Add(Offer("Defensive", seed, 0, G(GearSlot.Shell, Rarity.Epic, pw + 2, Element.Ember, AffixType.MaxHealth, AffixType.Armor, AffixType.BlastResist), Mat(MaterialIds04E.MatronEmber, 1)));
                    list.Add(Offer("Offensive", seed, 1, Gun(LootArchetype.CrystalCaster, Rarity.Legendary, lv + 2), Mat(MaterialIds04E.MatronEmber, 1)));
                    break;
                case "uh_vault_module":
                    list.Add(Single("Fracture Lattice module", FractureRules04E.Line(Rarity.Rare) + " Plus 1 Vaultbreaker Core.",
                        P(RewardPartKind04E.Module, ModuleFracture), Mat(MaterialIds04E.VaultCore, 1)));
                    break;
                case "uh_loadout_slot2":
                    list.Add(Single("Second loadout", "A second saved loadout on the BUILD page.", P(RewardPartKind04E.Unlock, UnlockLoadout2)));
                    break;
                case "uh_regent_pinnacle":
                {
                    list.Add(Offer("Mythic piece", seed, 0, G(GearSlot.Shell, Rarity.Mythic, pw + 3, Element.Ember, AffixType.MaxHealth, AffixType.Armor, AffixType.BlastResist, AffixType.BulletResist, AffixType.MeleeResist), Mat(MaterialIds04E.RegentHeart, 1)));
                    list.Add(Offer("Mythic rifle", seed, 1, Gun(LootArchetype.AssaultRifle, Rarity.Mythic, lv + 3), Mat(MaterialIds04E.RegentHeart, 1)));
                    list.Add(Single("Kiln Crown", "Ward capacity +" + Mathf.RoundToInt(BuildStatRules04E.CrownCapacityBonus * 100f) + "% for good, and a molten crown on the ward. Plus 1 Regent Heart.",
                        P(RewardPartKind04E.Unlock, UnlockKilnCrown), Mat(MaterialIds04E.RegentHeart, 1)));
                    break;
                }
                case "uh_bond_awaken":
                    foreach (string arch in BondIds.Archetypes)
                        list.Add(Single(BondIds.Title(arch) + ": Awakened", "Raises your " + BondIds.Title(arch) + " to Awakened, its final rank and evolution (needs it bonded).",
                            P(RewardPartKind04E.BondRank, arch, BondIds.RankAwakened)));
                    break;
                case "uh_frostglass_aspect":
                    list.Add(AspectOffer(WardRules04E.Frostglass));
                    break;
                case "uh_volt_aspect":
                    list.Add(AspectOffer(WardRules04E.VoltLattice));
                    break;
                case "uh_void_aspect":
                    list.Add(AspectOffer(WardRules04E.VoidScreen));
                    break;
                case "uh_trim_second":
                    list.Add(Single(ChassisTrims.Title(ChassisTrims.StormRails), "Jax's chrome rails with a glow strip low along the hull (look only).", P(RewardPartKind04E.Trim, ChassisTrims.StormRails)));
                    list.Add(Single(ChassisTrims.Title(ChassisTrims.CouplerGlow), "A crystal glow along the coupler line on both flanks of the fairing (look only).", P(RewardPartKind04E.Trim, ChassisTrims.CouplerGlow)));
                    break;
                case "uh_handling_sidegrade":
                    list.Add(Single("Canal grip tune", "More grip and steadier braking, a little less top speed. Switched on when claimed; toggle it at Jax's bay.", P(RewardPartKind04E.Trim, FusionRules.TuneGrip)));
                    break;
                case "uh_chassis_cruiser":
                    list.Add(Single("Cruiser chassis", "Adds the cruiser hover bike to your owned chassis.", P(RewardPartKind04E.Chassis, FusionIds.Cruiser)));
                    break;
                case "uh_care_kit":
                    list.Add(Single("Care kit", "5 Coolant Salt, 2 Ash Resin and 2 Ember Crystal.",
                        Mat(MaterialIds04E.CoolantSalt, 5), Mat(MaterialIds04E.AshResin, 2), Mat(MaterialIds04E.EmberCrystal, 2)));
                    break;
                case "uh_ward_visual":
                    list.Add(Single("Mender's Halo", "A halo ring on your ward (look only, toggle on the BUILD page).", P(RewardPartKind04E.Unlock, UnlockWardVisual)));
                    break;
                case "uh_map_knowledge":
                    list.Add(Single("Survey notes", "Rest points, shortcut gates and the optional elite site on the pause map (discovered regions only).", P(RewardPartKind04E.Unlock, UnlockSurvey)));
                    break;
                case "uh_elite_choice":
                {
                    GearSlot s;
                    AffixType[] a;
                    SignaturePiece(preset, out s, out a);
                    list.Add(Offer("Build piece", seed, 0, G(s, Rarity.Legendary, pw + 2, Element.None, a), Mat(MaterialIds04E.SeedCore, 1)));
                    list.Add(Offer("Weapon Core", seed, 1, G(GearSlot.Core, Rarity.Legendary, pw + 2, Element.None, AffixType.GunDamage, AffixType.CritChance), Mat(MaterialIds04E.SeedCore, 1)));
                    list.Add(Offer("Loot gun", seed, 2, Gun((LootArchetype)(PartSeed(seed, 2, 0) % 6), Rarity.Legendary, lv + 2), Mat(MaterialIds04E.SeedCore, 1)));
                    break;
                }
                case "uh_home_display":
                    list.Add(Single("Home trophy display", "A lit trophy case beside your home reward ledger.", P(RewardPartKind04E.Unlock, UnlockHomeDisplay)));
                    break;
                case "uh_utility_recipe":
                    list.Add(Single("Lantern Oil recipe", "A Forge brew: 10 minutes of +10% creature loot rolls and 15% faster stamina recovery. Plus 1 Ember Crystal and 2 Coolant Salt.",
                        P(RewardPartKind04E.Recipe, RecipeLantern), Mat(MaterialIds04E.EmberCrystal, 1), Mat(MaterialIds04E.CoolantSalt, 2)));
                    break;
                case "uh_bond_storm_evolve":
                    list.Add(Single("Stormskimmer evolution", "Evolution stage 1 for your Stormskimmer (look only).", P(RewardPartKind04E.BondEvolve, BondIds.StormSkimmer, 1)));
                    break;
                case "uh_bond_ash_evolve":
                    list.Add(Single("Ashshell Warden evolution", "Evolution stage 1 for your Ashshell Warden (look only).", P(RewardPartKind04E.BondEvolve, BondIds.AshWarden, 1)));
                    break;
                case "uh_bond_drake_evolve":
                    list.Add(Single("Quartz Drakeling evolution", "Evolution stage 1 for your drakeling (look only).", P(RewardPartKind04E.BondEvolve, BondIds.QuartzGuide, 1)));
                    break;
            }
            return list;
        }

        // ------------------------------------------------------------------ builders

        private static RewardPart04E P(RewardPartKind04E kind, string id, int count = 1)
        {
            return new RewardPart04E { Kind = kind, Id = id ?? "", Count = count };
        }

        private static RewardPart04E Mat(string id, int count)
        {
            return new RewardPart04E { Kind = RewardPartKind04E.Material, Id = id, Count = count };
        }

        private static RewardPart04E G(GearSlot slot, Rarity r, int power, Element e, params AffixType[] forced)
        {
            return new RewardPart04E { Kind = RewardPartKind04E.Gear, Slot = slot, Rarity = r, Power = Mathf.Max(1, power), Element = e, Forced = forced ?? new AffixType[0] };
        }

        private static RewardPart04E Kit(GearSlot slot, Rarity r, int power, params AffixType[] forced)
        {
            RewardPart04E p = G(slot, r, power, Element.None, forced);
            p.KitPiece = true;
            return p;
        }

        private static RewardPart04E Gun(LootArchetype a, Rarity r, int level)
        {
            return new RewardPart04E { Kind = RewardPartKind04E.Gun, Archetype = (int)a, Rarity = r, Level = Mathf.Clamp(level, 1, LootGenerator.MaxLevel) };
        }

        /// <summary>An offer whose item parts get their seeds from the entry seed (gear and guns rebuild exactly).</summary>
        private static RewardOffer04E Offer(string label, int seed, int offerIndex, params RewardPart04E[] parts)
        {
            RewardOffer04E o = new RewardOffer04E();
            StringBuilder detail = new StringBuilder();
            string main = "";
            int lumen = 0, cash = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                RewardPart04E p = parts[i];
                p.Seed = PartSeed(seed, offerIndex, i);
                o.Parts.Add(p);
                switch (p.Kind)
                {
                    case RewardPartKind04E.Gear:
                    {
                        GearItem it = p.BuildGear();
                        if (main.Length == 0) main = GearSystem.RarityName(it.Rarity) + " " + GearSystem.SlotName(it.Slot) + ": " + it.Name;
                        if (o.CompareSlot < 0) o.CompareSlot = (int)it.Slot;
                        if (detail.Length > 0) detail.Append('\n');
                        detail.Append(GearSystem.RarityName(it.Rarity)).Append(' ').Append(GearSystem.SlotName(it.Slot)).Append(" \"").Append(it.Name).Append("\" (power ").Append(it.Power).Append(", score ")
                            .Append(GearScoreRules.GearScore(it.Power, it.Upgrade, it.Rarity)).Append(')');
                        if (it.Element != Element.None) detail.Append(", ").Append(GearSystem.ElementName(it.Element));
                        foreach (Affix a in it.Affixes) detail.Append("\n  ").Append(GearSystem.AffixText(a.Type, a.Value));
                        if (p.KitPiece) detail.Append("\n  (skipped if you already own as good a ").Append(GearSystem.SlotName(it.Slot)).Append(')');
                        lumen += GearSystem.SalvageValue(it);
                        break;
                    }
                    case RewardPartKind04E.Gun:
                    {
                        LootGun g = p.BuildGun();
                        if (main.Length == 0) main = GearSystem.RarityName(g.Rarity) + " " + g.Name;
                        if (detail.Length > 0) detail.Append('\n');
                        detail.Append(GearSystem.RarityName(g.Rarity)).Append(' ').Append(LootGenerator.ArchetypeName(g.Archetype, LootGenerator.IsRocket(g))).Append(" \"").Append(g.Name)
                            .Append("\" (level ").Append(g.Level).Append(')');
                        if (g.Element != Element.None) detail.Append(", ").Append(GearSystem.ElementName(g.Element));
                        detail.Append("\n  ").Append(Mathf.RoundToInt(g.Stats.Damage)).Append(" damage, ").Append(Mathf.RoundToInt(LootGenerator.SustainedDps(g))).Append(" sustained per second");
                        string traits = LootGenerator.TraitLine(g);
                        if (!string.IsNullOrEmpty(traits)) detail.Append("\n  ").Append(traits);
                        detail.Append("\n  Arrives marked as a favourite (cannot be sold or scrapped by accident).");
                        cash += LootEconomy.SellValue(g);
                        break;
                    }
                    case RewardPartKind04E.Material:
                        if (detail.Length > 0) detail.Append('\n');
                        detail.Append('+').Append(p.Count).Append(' ').Append(MaterialDb.NameOf(p.Id));
                        MaterialDef md = MaterialDb.Get(p.Id);
                        if (md != null) cash += md.Value * p.Count;
                        break;
                }
            }
            o.Title = label + (main.Length > 0 ? ": " + main : "");
            o.Detail = detail.ToString();
            o.SalvageLumen = lumen;
            o.SellCash = cash;
            return o;
        }

        private static RewardOffer04E Single(string title, string detail, params RewardPart04E[] parts)
        {
            RewardOffer04E o = new RewardOffer04E { Title = title, Detail = detail };
            int cash = 0;
            foreach (RewardPart04E p in parts)
            {
                o.Parts.Add(p);
                if (p.Kind == RewardPartKind04E.Material)
                {
                    MaterialDef md = MaterialDb.Get(p.Id);
                    if (md != null) cash += md.Value * p.Count;
                }
            }
            o.SellCash = cash;
            return o;
        }

        private static RewardOffer04E AspectOffer(string aspect)
        {
            return Single(WardRules04E.AspectTitle(aspect) + " ward aspect", WardRules04E.AspectLine(aspect), P(RewardPartKind04E.Aspect, aspect));
        }

        private static RewardOffer04E ModOffer(int seed, int index, GadgetModStat stat)
        {
            float u = (PartSeed(seed, index, 0) % 1000) / 999f;
            float v = GadgetRules.RollModValue(stat, Rarity.Rare, u);
            RewardPart04E p = new RewardPart04E { Kind = RewardPartKind04E.Mod, ModStat = (int)stat, ModValue = v, Rarity = Rarity.Rare, Seed = PartSeed(seed, index, 0) };
            RewardOffer04E o = Single("Rare " + GadgetRules.StatName(stat) + " mod", GadgetRules.StatText(stat, v) + " in the gadget slot you socket it into.", p);
            o.SalvageLumen = GadgetRules.ModSalvageValue(Rarity.Rare);
            return o;
        }

        private static string WardLine()
        {
            return "Hold its gadget key to raise it: about " + Mathf.RoundToInt(WardRules04E.HalfAngle) + " degrees each side of your aim is covered, blasts by angle; it breaks when its capacity runs out and recovers on the gadget cooldown.";
        }

        /// <summary>The early choice's two slots for a build suggestion (defensive first).</summary>
        public static void EarlySlots(string preset, out GearSlot defSlot, out AffixType[] defAff, out GearSlot offSlot, out AffixType[] offAff)
        {
            switch (preset)
            {
                case BuildSystem.Conduit:
                    defSlot = GearSlot.Implant; defAff = new[] { AffixType.MaxEnergy, AffixType.MaxStamina };
                    offSlot = GearSlot.Core; offAff = new[] { AffixType.PowerDamage, AffixType.CritChance };
                    break;
                case BuildSystem.Riftstrider:
                    defSlot = GearSlot.Striders; defAff = new[] { AffixType.MoveSpeed, AffixType.DodgeFrames };
                    offSlot = GearSlot.Gauntlets; offAff = new[] { AffixType.GunDamage, AffixType.CritChance };
                    break;
                default:
                    defSlot = GearSlot.Shell; defAff = new[] { AffixType.MaxHealth, AffixType.Armor };
                    offSlot = GearSlot.Gauntlets; offAff = new[] { AffixType.GunDamage, AffixType.CritChance };
                    break;
            }
        }

        /// <summary>The build's signature piece (descent piece, elite and pinnacle build options).</summary>
        public static void SignaturePiece(string preset, out GearSlot slot, out AffixType[] affixes)
        {
            switch (preset)
            {
                case BuildSystem.Conduit:
                    slot = GearSlot.Implant; affixes = new[] { AffixType.PowerDamage, AffixType.MaxEnergy, AffixType.TonicPotency };
                    break;
                case BuildSystem.Riftstrider:
                    slot = GearSlot.Striders; affixes = new[] { AffixType.MoveSpeed, AffixType.DodgeFrames, AffixType.MaxStamina };
                    break;
                case BuildSystem.Wardkeeper:
                    slot = GearSlot.Shell; affixes = new[] { AffixType.MaxHealth, AffixType.Armor, AffixType.BlastResist };
                    break;
                default:
                    slot = GearSlot.Shell; affixes = new[] { AffixType.MaxHealth, AffixType.Armor, AffixType.BulletResist };
                    break;
            }
        }

        private static string BuildTitle(string preset)
        {
            PresetDef04E p = BuildPresetRules04E.Get(preset);
            return p != null ? p.Title : "Balanced";
        }

        /// <summary>Every material id any reward hands out (for the economy sheet and tests).</summary>
        public static List<string> MaterialsUsed()
        {
            List<string> ids = new List<string>();
            foreach (string id in RewardIds04E.All)
            {
                foreach (RewardOffer04E o in Offers(id, 12345, 3, 5, BuildSystem.Wardkeeper))
                    foreach (RewardPart04E p in o.Parts)
                        if (p.Kind == RewardPartKind04E.Material && !ids.Contains(p.Id)) ids.Add(p.Id);
            }
            return ids;
        }
    }
}
