using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    // =====================================================================================================================
    // Underheart and Bonds 04E: every stable identifier the parallel work streams share. Saves, quest data and tests refer
    // to these strings, so they are append-only: never rename or reorder an entry. Engine-free.
    // =====================================================================================================================

    /// <summary>The four 04E venues (04C catalog entries) and the anchors their layouts must provide.</summary>
    public static class VenueIds04E
    {
        public const string Dispatch = "dispatch";      // Cinder Dispatch: Ion's receiver, the dispatcher, the fitting worktable
        public const string Pressure = "pressure";      // The Pressure House: utility workroom with the basement stair down
        public const string Bondkeeper = "bondkeeper";  // Bondkeeper's Refuge: creature care room and training yard
        public const string Office = "office";          // The Resonance Office: faction records and service interior

        public static readonly string[] All = { Dispatch, Pressure, Bondkeeper, Office };

        /// <summary>Anchors every 04E venue layout provides in addition to the 04C required set (service, activity, staff, visitor1, visitor2, exit).</summary>
        public static string[] ExtraAnchors(string venueId)
        {
            switch (venueId)
            {
                case Dispatch: return new[] { "receiver_a", "receiver_b", "fitting", "ion_screen" };
                case Pressure: return new[] { "fuse_cabinet", "nest", "vent_valve", "stair_top" };
                case Bondkeeper: return new[] { "care_station", "habitat", "training", "perch" };
                case Office: return new[] { "records", "terminal", "back_room", "testimony" };
                case "garage": return new[] { "bay" };
                default: return new string[0];
            }
        }

        public static bool Has(string id) { return Array.IndexOf(All, id) >= 0; }

        /// <summary>The Pressure House stairwell in the venue's local frame (x right, z into the building, y up from the floor):
        /// the floor opening rectangle, the top tread start and the landing at the bottom (y = -6.15 below the floor).</summary>
        public const float StairX0 = 4.3f, StairX1 = 7.1f, StairZ0 = 3.0f, StairZ1 = 14.6f;
        public const float StairTopZ = 3.2f, StairBottomZ = 14.2f, StairDrop = 6.15f;
    }

    /// <summary>The Underheart network: region ids, named anchors (quest places "Under:id"), shortcuts and rest points.</summary>
    public static class UnderheartIds
    {
        public const string ServiceLevel = "service_level";
        public const string LanternRefuge = "lantern_refuge";
        public const string EmberCanals = "ember_canals";
        public const string PyreGardens = "pyre_gardens";
        public const string CrucibleWorkshops = "crucible_workshops";
        public const string AshReliquary = "ash_reliquary";
        public const string FoundryCrown = "foundry_crown";

        /// <summary>The connector under the Pressure House, then the six named subregions from refuge to depth.</summary>
        public static readonly string[] Regions = { ServiceLevel, LanternRefuge, EmberCanals, PyreGardens, CrucibleWorkshops, AshReliquary, FoundryCrown };

        public static string RegionTitle(string id)
        {
            switch (id)
            {
                case ServiceLevel: return "Pressure House Service Level";
                case LanternRefuge: return "Lantern Refuge";
                case EmberCanals: return "Ember Canals";
                case PyreGardens: return "Pyre Gardens";
                case CrucibleWorkshops: return "Crucible Workshops";
                case AshReliquary: return "Ash Reliquary";
                case FoundryCrown: return "Foundry Crown";
                default: return "Underheart";
            }
        }

        /// <summary>Every named anchor. The prefix before the dot names the region (service, refuge, canals, gardens,
        /// workshops, reliquary, crown) or the surface portal (portal).</summary>
        public static readonly string[] Anchors =
        {
            // service level (under the Pressure House)
            "service.stair_bottom", "service.construct_room", "service.service_panel", "service.vent", "service.juveniles",
            "service.gate", "service.clamp_hollow", "service.clamp_a", "service.clamp_b", "service.clamp_c", "service.tunnel_top",
            // Lantern Refuge
            "refuge.entry", "refuge.caretaker", "refuge.fitting", "refuge.rest", "refuge.ledger", "refuge.map_board", "refuge.lift",
            "refuge.sample_drop", "refuge.exit_canals", "refuge.residents",
            // Ember Canals
            "canals.entry", "canals.crossing", "canals.assist_route", "canals.valve_room", "canals.pressure_valve", "canals.gate",
            "canals.sample_vent", "canals.actuator", "canals.transit", "canals.portal_bottom", "canals.salvage_crew",
            "canals.branch_cache", "canals.rest", "canals.cool_pocket", "canals.exit_gardens",
            // Pyre Gardens
            "gardens.entry", "gardens.caretaker", "gardens.study_a", "gardens.study_b", "gardens.study_c", "gardens.cool_route_a",
            "gardens.cool_route_b", "gardens.seed_core", "gardens.boundary", "gardens.basilisk", "gardens.rescue_nest",
            "gardens.matron_gate", "gardens.matron_arena", "gardens.lure_a", "gardens.lure_b", "gardens.lure_c", "gardens.rest",
            "gardens.vista", "gardens.habitat", "gardens.exit_workshops",
            // Crucible Workshops
            "workshops.entry", "workshops.store_room", "workshops.hoist", "workshops.records", "workshops.vault_gate",
            "workshops.vault_arena", "workshops.craft_point", "workshops.coupler_room", "workshops.convoy_start",
            "workshops.convoy_obstacle", "workshops.convoy_end", "workshops.storm_relay", "workshops.rest", "workshops.exit_reliquary",
            // Ash Reliquary
            "reliquary.entry", "reliquary.archive_hall", "reliquary.columns", "reliquary.elite_detour", "reliquary.revelation",
            "reliquary.platform_route", "reliquary.safe_route", "reliquary.window", "reliquary.surveyor", "reliquary.rest",
            "reliquary.exit_crown",
            // Foundry Crown
            "crown.staging", "crown.valve_mech", "crown.reflector_mech", "crown.escort_start", "crown.escort_end", "crown.regent_gate",
            "crown.regent_arena", "crown.lift", "crown.vista",
            // the surface vehicle portal (a ramp from a city lot down to the Ember Canals transit tunnel)
            "portal.top"
        };

        /// <summary>Return shortcuts the player unlocks (at least three are required by the brief).</summary>
        public static readonly string[] Shortcuts = { "sc_refuge_lift", "sc_canals_gate", "sc_workshops_hoist", "sc_crown_lift" };

        /// <summary>Rest points: safe places to recover; resting sets the save resume point there.</summary>
        public static readonly string[] RestPoints = { "refuge.rest", "canals.rest", "gardens.rest", "workshops.rest", "reliquary.rest", "crown.staging" };

        private static HashSet<string> _anchorSet;

        public static bool HasAnchor(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (_anchorSet == null) _anchorSet = new HashSet<string>(Anchors);
            return _anchorSet.Contains(id);
        }

        public static bool HasShortcut(string id) { return Array.IndexOf(Shortcuts, id) >= 0; }

        /// <summary>The region an anchor belongs to ("" for the surface portal or an unknown prefix).</summary>
        public static string RegionOfAnchor(string anchorId)
        {
            if (string.IsNullOrEmpty(anchorId)) return "";
            int dot = anchorId.IndexOf('.');
            string prefix = dot > 0 ? anchorId.Substring(0, dot) : anchorId;
            switch (prefix)
            {
                case "service": return ServiceLevel;
                case "refuge": return LanternRefuge;
                case "canals": return EmberCanals;
                case "gardens": return PyreGardens;
                case "workshops": return CrucibleWorkshops;
                case "reliquary": return AshReliquary;
                case "crown": return FoundryCrown;
                default: return "";
            }
        }

        public static void ResetStatics() { _anchorSet = null; }
    }

    /// <summary>Bonded companion archetypes and bond ranks.</summary>
    public static class BondIds
    {
        public const string QuartzGuide = "quartz_guide";    // Quartz Drakeling: guide (route reveal, marked-target burst)
        public const string AshWarden = "ash_warden";        // Ashshell Warden: guardian (brace a mechanism, directional protection)
        public const string StormSkimmer = "storm_skimmer";  // Stormskimmer: glider (bridge a relay, limited disruption)

        public static readonly string[] Archetypes = { QuartzGuide, AshWarden, StormSkimmer };

        public const int RankStray = 0, RankBonded = 1, RankAttuned = 2, RankHarmonized = 3, RankAwakened = 4;
        public static readonly string[] RankNames = { "Rescued", "Bonded", "Attuned", "Harmonized", "Awakened" };
        /// <summary>The first safe fusion needs this rank.</summary>
        public const int FusionRank = RankAttuned;

        public static bool Has(string id) { return Array.IndexOf(Archetypes, id) >= 0; }

        public static string Title(string id)
        {
            switch (id)
            {
                case QuartzGuide: return "Quartz Drakeling";
                case AshWarden: return "Ashshell Warden";
                case StormSkimmer: return "Stormskimmer";
                default: return "Companion";
            }
        }

        public static string DefaultName(string id)
        {
            switch (id)
            {
                case QuartzGuide: return "Glint";
                case AshWarden: return "Hob";
                case StormSkimmer: return "Skein";
                default: return "Friend";
            }
        }
    }

    /// <summary>Creature-vehicle fusion forms and compatible personal chassis classes.</summary>
    public static class FusionIds
    {
        public const string Ventwing = "ventwing";      // quartz_guide + bike: glide and vent burst
        public const string Kilnback = "kilnback";      // ash_warden + bike: mineral armour and brace ram
        public const string Coilrunner = "coilrunner";  // storm_skimmer + bike: arc dash and disruption

        public static readonly string[] Forms = { Ventwing, Kilnback, Coilrunner };

        /// <summary>Personal chassis classes: the three ownable hover bikes (the police bike is never personal).</summary>
        public const string Sport = "sport", Cruiser = "cruiser", Skiff = "skiff";
        public static readonly string[] ChassisClasses = { Sport, Cruiser, Skiff };

        public static string FormFor(string archetype)
        {
            switch (archetype)
            {
                case BondIds.QuartzGuide: return Ventwing;
                case BondIds.AshWarden: return Kilnback;
                case BondIds.StormSkimmer: return Coilrunner;
                default: return "";
            }
        }

        public static string Title(string form)
        {
            switch (form)
            {
                case Ventwing: return "Ventwing";
                case Kilnback: return "Kilnback";
                case Coilrunner: return "Coilrunner";
                default: return "Fused form";
            }
        }

        public static bool Has(string id) { return Array.IndexOf(Forms, id) >= 0; }

        /// <summary>Handling courses run by the quest Course step.</summary>
        public static readonly string[] Courses = { "jax_handling", "jax_profile_two", "canal_crossing" };
    }

    /// <summary>The new principal encounters (the Underheart boss framework, not CreatureDirector bosses).</summary>
    public static class UnderBossIds
    {
        public const string ValveGuardian = "valve_guardian";   // miniboss of The First Descent
        public const string CinderMatron = "cinder_matron";
        public const string Vaultbreaker = "vaultbreaker";
        public const string KilnRegent = "kiln_regent";

        public static readonly string[] All = { ValveGuardian, CinderMatron, Vaultbreaker, KilnRegent };

        /// <summary>The three mechanisms of Break the Extraction Choir.</summary>
        public static readonly string[] Mechanisms = { "crown_valve", "crown_reflectors", "crown_escort" };

        public static bool Has(string id) { return Array.IndexOf(All, id) >= 0; }

        public static string Title(string id)
        {
            switch (id)
            {
                case ValveGuardian: return "Valve Guardian";
                case CinderMatron: return "The Cinder Matron";
                case Vaultbreaker: return "The Vaultbreaker";
                case KilnRegent: return "The Kiln Regent";
                default: return "Guardian";
            }
        }

        /// <summary>The durable first-win token for a boss.</summary>
        public static string Token(string id) { return "boss04e:" + id; }
    }

    /// <summary>Underground creature species (appended to CreatureSpeciesDb) and the non-hostile residents.</summary>
    public static class EnemyIds04E
    {
        public const string EmberScavenger = "ember_scavenger";
        public const string AshBurrower = "ash_burrower";
        public const string GlasswingAmbusher = "glasswing_ambusher";
        public const string FurnaceCaster = "furnace_caster";
        public const string ExtractionSentinel = "extraction_sentinel";
        public const string RepairConstruct = "repair_construct";
        public const string CinderMoth = "cinder_moth";        // harmless
        public const string KilnCrab = "kiln_crab";            // territorial nest defender: warns, then defends
        public const string EmberHatchling = "ember_hatchling"; // harmless juveniles (Sparks in the Walls)

        public static readonly string[] Hostile = { EmberScavenger, AshBurrower, GlasswingAmbusher, FurnaceCaster, ExtractionSentinel, RepairConstruct };
        public static readonly string[] Residents = { CinderMoth, KilnCrab, EmberHatchling };
    }

    /// <summary>Crafting materials appended to MaterialDb for the underground economy.</summary>
    public static class MaterialIds04E
    {
        public const string EmberCrystal = "ember_crystal";
        public const string AshResin = "ash_resin";
        public const string CoolantSalt = "coolant_salt";
        public const string RegulatorCog = "regulator_cog";
        public const string SeedCore = "seed_core";
        public const string MatronEmber = "matron_ember";
        public const string VaultCore = "vault_core";
        public const string RegentHeart = "regent_heart";

        public static readonly string[] All = { EmberCrystal, AshResin, CoolantSalt, RegulatorCog, SeedCore, MatronEmber, VaultCore, RegentHeart };
    }

    /// <summary>Durable reward definitions (the Builds reward catalog defines each; quests use them in Claim steps).</summary>
    public static class RewardIds04E
    {
        public static readonly string[] All =
        {
            "uh_ward_starter", "uh_kit_wardkeeper", "uh_kit_conduit", "uh_kit_riftstrider", "uh_gear_choice_early",
            "uh_descent_piece", "uh_coupler_part", "uh_fusion_license", "uh_trim_choice", "uh_augment_choice",
            "uh_recipe_coolant", "uh_matron_aspect", "uh_matron_gear_choice", "uh_vault_module", "uh_loadout_slot2",
            "uh_regent_pinnacle", "uh_bond_awaken", "uh_frostglass_aspect", "uh_volt_aspect", "uh_void_aspect",
            "uh_trim_second", "uh_handling_sidegrade", "uh_chassis_cruiser", "uh_care_kit", "uh_ward_visual",
            "uh_map_knowledge", "uh_elite_choice", "uh_home_display", "uh_utility_recipe", "uh_bond_storm_evolve",
            "uh_bond_ash_evolve", "uh_bond_drake_evolve"
        };

        public static bool Has(string id) { return Array.IndexOf(All, id) >= 0; }
    }

    /// <summary>Named campaign characters (the cast system spawns them as real people at their posts).</summary>
    public static class CastIds04E
    {
        public const string Nessa = "nessa";               // Nessa Vale, the Veyra Lofts host (the opening giver)
        public const string Dispatcher = "dispatcher";     // Corin Pell, Cinder Dispatch
        public const string Maintainer = "maintainer";     // Tamsin Reyes, Pressure House maintenance, speaks for Ion
        public const string Caretaker = "caretaker";       // Odile Marsh, Bondkeeper's Refuge
        public const string RefugeKeeper = "refuge_keeper"; // Ilse Varro, Lantern Refuge
        public const string GardensKeeper = "gardens_keeper"; // Bram Achterberg, Pyre Gardens caretaker
        public const string Clerk = "clerk";               // Ansel Whit, the Resonance Office
        public const string Ion = "ion";                   // Ion Solane (remote at first, in person at the observatory in Act IV)
        public const string Mara = "mara";                 // Mara Solane at her clinic
        public const string Jax = "jax";                   // Jax Calder at his garage
        public const string Dante = "dante";               // Dante Rook at his forge
        public const string Iris = "iris";                 // Iris Moon at the Resonance Archive
        public const string June = "june";                 // June Pyre (civic liaison, Act III)

        public static readonly string[] All = { Nessa, Dispatcher, Maintainer, Caretaker, RefugeKeeper, GardensKeeper, Clerk, Ion, Mara, Jax, Dante, Iris, June };

        public static bool Has(string id) { return Array.IndexOf(All, id) >= 0; }

        public static string Name(string id)
        {
            switch (id)
            {
                case Nessa: return "Nessa Vale";
                case Dispatcher: return "Corin Pell";
                case Maintainer: return "Tamsin Reyes";
                case Caretaker: return "Odile Marsh";
                case RefugeKeeper: return "Ilse Varro";
                case GardensKeeper: return "Bram Achterberg";
                case Clerk: return "Ansel Whit";
                case Ion: return "Ion Solane";
                case Mara: return "Mara Solane";
                case Jax: return "Jax Calder";
                case Dante: return "Dante Rook";
                case Iris: return "Iris Moon";
                case June: return "June Pyre";
                default: return id ?? "";
            }
        }
    }

    /// <summary>The campaign's quest ids (main, side) and replayable contract grammar ids.</summary>
    public static class QuestIds04E
    {
        public const string Campaign = "underheart";

        public static readonly string[] Main =
        {
            "uh05_01_knock", "uh05_02_sparks", "uh05_03_bond", "uh05_04_descent",
            "uh05_05_chassis", "uh05_06_pressure", "uh05_07_gardens", "uh05_08_matron",
            "uh05_09_ledger", "uh05_10_road", "uh05_11_vaultbreaker", "uh05_12_keep",
            "uh05_13_crown", "uh05_14_choir", "uh05_15_regent", "uh05_16_answers"
        };

        public static readonly string[] Side =
        {
            "uh05_s01_overheat", "uh05_s02_instrument", "uh05_s03_shelter",
            "uh05_s04_mismatch", "uh05_s05_profile", "uh05_s06_coupler",
            "uh05_s07_supply", "uh05_s08_habitat", "uh05_s09_treatment",
            "uh05_s10_shortcut", "uh05_s11_drone", "uh05_s12_entries"
        };

        public static readonly string[] Contracts = { "uhc_salvage", "uhc_elite_hunt", "uhc_valve_defense", "uhc_rescue", "uhc_lens", "uhc_courier", "uhc_surveyor", "uhc_trial" };

        /// <summary>Persistent campaign decisions (key and the allowed values).</summary>
        public static readonly string[] ChoiceKeys = { "juveniles", "constructs", "fusion_profile", "cooling_route", "matron_fate", "ledger_route", "civic_first", "descent_window", "regent_end" };

        public static string[] ChoiceValues(string key)
        {
            switch (key)
            {
                case "juveniles": return new[] { "shelter", "guide" };
                case "constructs": return new[] { "fight", "bypass" };
                case "fusion_profile": return new[] { "updraft", "ventbrake" };
                case "cooling_route": return new[] { "a", "b" };
                case "matron_fate": return new[] { "contain", "relocate" };
                case "ledger_route": return new[] { "testimony", "terminal" };
                case "civic_first": return new[] { "diner", "archive" };
                case "descent_window": return new[] { "early", "late" };
                case "regent_end": return new[] { "contain", "shutdown" };
                default: return new string[0];
            }
        }

        public static bool IsMain(string id) { return Array.IndexOf(Main, id) >= 0; }
        public static bool IsSide(string id) { return Array.IndexOf(Side, id) >= 0; }
    }

    /// <summary>The single reset hook for the 04E id caches.</summary>
    public static class Ids04EStatics
    {
        public static void ResetStatics() { UnderheartIds.ResetStatics(); }
    }
}
