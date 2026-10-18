using System;
using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>
    /// Every name the quest and event data may use (step types, fail types, looks, props, anchors, event kinds) and
    /// which fields each step type and event kind reads. Plain data with no engine calls: the runtime, the validator,
    /// the EditMode tests and the offline tools all read it, so they always agree.
    /// </summary>
    public static class QuestCatalog
    {
        // ------------------------------------------------------------------ names

        public static readonly string[] StepTypes =
        {
            "GoTo", "Talk", "Interact", "Kill", "Boss", "Collect", "Survive", "Defend", "Escort", "Hunt", "CloseRift", "Scan", "Shift", "Wait",
            "Spawn", "Ally", "Prop", "SetTime", "SetWeather", "Give", "GiveGun", "AddRep", "Message", "Cinematic", "Choice", "Venue", "Courier", "Attune",
            // Underheart and Bonds 04E (QuestSteps.Underheart*.cs)
            "Build", "Claim", "Equip", "Rescue", "BondRank", "Care", "Assist", "Fuse", "Course", "Park", "Skirmish", "UBoss", "Mechanism",
            "Shortcut", "Decide", "Branch", "Conduit", "Flag"
        };

        /// <summary>The 04E step types (campaign content); guild quests may use them too, but they need the 04E systems.</summary>
        public static readonly string[] CampaignStepTypes =
        {
            "Build", "Claim", "Equip", "Rescue", "BondRank", "Care", "Assist", "Fuse", "Course", "Park", "Skirmish", "UBoss", "Mechanism",
            "Shortcut", "Decide", "Branch", "Conduit", "Flag"
        };

        /// <summary>Campaign contract grammars (templates whose Guild is a campaign id, offered from the Lantern Refuge board).</summary>
        public static readonly string[] CampaignContractKinds =
        {
            "Salvage", "EliteHunt", "ValveDefense", "CreatureRescue", "LensCalibration", "ShortCourier", "SurveyorSearch", "ExtractionTrial"
        };

        public static readonly string[] FailTypes = { "AllyDies", "TimeLimit", "LeaveArea", "LeaveVeil", "Wanted", "Hour", "Vehicle" };

        /// <summary>People a step or event can spawn (QuestWorld.Look builds them).</summary>
        public static readonly string[] Looks =
        {
            "Civilian", "Patient", "Warden", "Mender", "Forgewright", "Seer", "Preacher", "Prospector", "Biker", "Pilgrim", "Echo", "Courier",
            "Hunter", "Worker", "Mara", "Ion", "Luma", "Iris", "Dante"
        };

        /// <summary>Set dressing a step or event can place (QuestWorld.Prop builds them).</summary>
        public static readonly string[] Props =
        {
            "Crate", "Cache", "Satchel", "Wreck", "Boat", "BusStop", "Brazier", "Shrine", "Jammer", "Crystal", "Feather", "Mote", "Board",
            "Altar", "Anvil", "Scaffold", "Cones", "Campfire", "Tracks", "Soapbox", "Bike", "Stall", "Totem", "Forge"
        };

        public static readonly string[] ContractKinds = { "Hunt", "Rift", "Escort", "Courier" };

        public static readonly string[] EventKinds =
        {
            "Rescue", "Loot", "Talk", "Brawl", "Relight", "Swarm", "Rift", "Escort", "Rising", "Pickup", "Sighting", "Impostor", "Sabotage", "Storm", "Holdout", "Fieldwork", "Skirmish", "CrystalDispute", "DrakeRescue", "CrystalCache"
        };

        /// <summary>Where an event may spawn. "Site:Key" (any site key) is also allowed.</summary>
        public static readonly string[] Anchors = { "Sidewalk", "Road", "Parking", "Park", "Plaza", "Lot", "Canal", "Lamp", "Desert", "Snow", "Any" };

        /// <summary>Location tokens a step's At or To may use besides POI names, site keys and "District:Name".</summary>
        public static readonly string[] PlaceTokens = { "", "Giver", "Player", "Previous", "World" };

        /// <summary>Creature habitats a contract can send the player to (QuestPlaces.HabitatPoint).</summary>
        public static readonly string[] ContractHabitats = { Habitat.Frost, Habitat.FrostLake, Habitat.Badlands, Habitat.Quarry, Habitat.Canyon, Habitat.Parks, Habitat.Construction, Habitat.Grave, Habitat.CityNight, Habitat.Inner };

        /// <summary>Tokens contract titles and briefs may contain.</summary>
        public static readonly string[] ContractTokens = { "{species}", "{count}", "{place}", "{dest}" };

        /// <summary>
        /// The named site each boss's arena belongs to, for a Boss step that gives no place (the validator and the
        /// offline checks use it; inner is true when the fight is inside the Inner World). The Boss step itself fights at
        /// the arena's exact centre: the creature director's own arena, or QuestPlaces.BossArenaOf. The Mirage Djinn's
        /// Mirage Flats (deep in the south-east Badlands) have no site of their own; the Starlite Motel is the nearest.
        /// </summary>
        public static string BossArena(string speciesId, out bool inner)
        {
            inner = false;
            switch (speciesId)
            {
                case "glacier_titan": return "TitanHollow";
                case "golem_foreman": return "Quarry";
                case "wraith_queen": inner = true; return "CrystalManor";
                case "mirage_djinn": return "StarliteMotel";
                default: return "";
            }
        }

        /// <summary>Props the player picks up and takes along: an Interact on one hides it once used.</summary>
        public static readonly string[] PickupProps = { "Cache", "Satchel", "Crystal", "Mote", "Feather" };

        /// <summary>True for a prop the player takes along (see PickupProps).</summary>
        public static bool IsPickupProp(string prop)
        {
            return Contains(PickupProps, prop);
        }

        public static bool Contains(string[] list, string s)
        {
            return Array.IndexOf(list, s) >= 0;
        }

        /// <summary>Drops the lazily built field tables (they are rebuilt on first use).</summary>
        public static void ResetStatics()
        {
            _stepFields = null;
            _stepRequired = null;
            _eventFields = null;
            _eventRequired = null;
        }

        // ------------------------------------------------------------------ step fields

        private static readonly string[] CommonStep = { "Type", "Objective", "Veil", "HoldVeil" };
        private static readonly string[] Where = { "At", "OffsetX", "OffsetZ", "Radius" };
        private static readonly string[] Point = { "At", "OffsetX", "OffsetZ" };
        private static readonly string[] Where2 = { "To", "ToOffsetX", "ToOffsetZ", "ToRadius" };
        private static readonly string[] Point2 = { "To", "ToOffsetX", "ToOffsetZ" };

        private static Dictionary<string, HashSet<string>> _stepFields;
        private static Dictionary<string, string[]> _stepRequired;

        /// <summary>The fields a step type reads (the common ones included), or null for an unknown type.</summary>
        public static HashSet<string> StepFields(string type)
        {
            if (_stepFields == null) BuildStepTables();
            HashSet<string> s;
            return type != null && _stepFields.TryGetValue(type, out s) ? s : null;
        }

        /// <summary>The fields a step type must set.</summary>
        public static string[] StepRequired(string type)
        {
            if (_stepRequired == null) BuildStepTables();
            string[] s;
            return type != null && _stepRequired.TryGetValue(type, out s) ? s : new string[0];
        }

        private static void BuildStepTables()
        {
            Dictionary<string, HashSet<string>> f = new Dictionary<string, HashSet<string>>();
            Dictionary<string, string[]> r = new Dictionary<string, string[]>();
            Def(f, "Attune", Point);
            Def(f, "Courier", Concat(Point, Point2), "Seconds");
            Def(f, "GoTo", Where, "OnFoot");
            Def(f, "Talk", Where, "Lines", "Name", "Look", "Prompt", "Prop");
            Def(f, "Interact", Where, "Prompt", "Prop", "Count", "Lines", "Text", "Name");
            Def(f, "Kill", Where, "Species", "Count", "MaxAlive", "Elite", "HealthScale", "DamageScale", "Name");
            Def(f, "Boss", Point, "Species", "Name", "HealthScale", "DamageScale", "AddSpecies", "AddCount", "Lines");
            Def(f, "Collect", Where, "Material", "Count", "Have", "Spend", "Caches", "Prop");
            Def(f, "Survive", Where, "Seconds", "Species", "Count", "Waves", "Elite");
            Def(f, "Defend", Where, "Species", "Count", "Waves", "Seconds", "Allies", "Look", "Name", "Elite");
            Def(f, "Escort", Concat(Point, Where2), "Name", "Look", "Health", "Pacifist", "Species", "Count", "Waves", "Lines");
            Def(f, "Hunt", Concat(Point, Point2), "Species", "Clues", "Name", "Elite", "HealthScale", "DamageScale", "Lines", "Radius");
            Def(f, "CloseRift", Where, "Count", "Species", "AddSpecies", "Seconds", "MaxAlive", "Health");
            Def(f, "Scan", Where, "Species", "Seconds", "Name", "Elite", "HealthScale");
            Def(f, "Shift", new string[0], "Inner", "Force");
            Def(f, "Wait", new string[0], "Seconds");
            Def(f, "Spawn", Where, "Species", "Count", "Elite", "HealthScale", "DamageScale", "Name");
            Def(f, "Ally", Point, "Name", "Look", "Health", "Pacifist");
            Def(f, "Prop", Where, "Prop", "Count");
            Def(f, "SetTime", new string[0], "Hour");
            Def(f, "SetWeather", new string[0], "Weather");
            Def(f, "Give", new string[0], "Material", "Count", "Cash", "Lumen", "Gear", "Rarity");
            Def(f, "GiveGun", new string[0], "Rarity", "Archetype", "Unique");
            Def(f, "AddRep", new string[0], "Faction", "Amount");
            Def(f, "Message", new string[0], "Text", "Banner", "Name", "Seconds");
            Def(f, "Cinematic", Concat(Point, Point2), "Height", "Seconds", "Lines");
            Def(f, "Choice", new string[0], "Text", "Name", "Options");
            Def(f, "Venue", new string[0], "VenueId", "ActionId", "Lines");
            // ---- Underheart and Bonds 04E
            Def(f, "Build", Where, "Prompt", "Text", "Lines");
            Def(f, "Claim", new string[0], "Reward", "Token", "Text");
            Def(f, "Equip", Point, "Calibrate", "Text");
            Def(f, "Rescue", Point, "Bond", "Points", "Name", "Lines");
            Def(f, "BondRank", new string[0], "Bond", "Rank", "Text");
            Def(f, "Care", Where, "Prompt", "Text", "Lines");
            Def(f, "Assist", Where, "Seconds", "Prompt", "Text", "Name", "Lines");
            Def(f, "Fuse", Where, "Form", "Text");
            Def(f, "Course", new string[0], "Course", "Count", "Lines");
            Def(f, "Park", Where, "Text");
            Def(f, "Skirmish", Where, "Roster", "Reinforce", "Room", "HealthScale", "DamageScale", "Name", "Lines");
            Def(f, "UBoss", Where, "Boss", "Name", "Lines");
            Def(f, "Mechanism", Point, "Mechanism", "Lines");
            Def(f, "Shortcut", Where, "Shortcut", "Prompt", "Text");
            Def(f, "Decide", new string[0], "Key", "Value", "Text", "Name", "Options", "Reask");
            Def(f, "Branch", new string[0], "Options");
            Def(f, "Conduit", new string[0], "Lit");
            Def(f, "Flag", new string[0], "Key");
            r["Venue"] = new[] { "VenueId", "ActionId" };
            r["Build"] = new[] { "At" };
            r["Claim"] = new[] { "Reward" };
            r["Rescue"] = new[] { "Bond", "At" };
            r["BondRank"] = new[] { "Bond", "Rank" };
            r["Care"] = new[] { "At" };
            r["Assist"] = new[] { "At" };
            r["Course"] = new[] { "Course" };
            r["Skirmish"] = new[] { "At", "Roster" };
            r["UBoss"] = new[] { "Boss", "At" };
            r["Mechanism"] = new[] { "Mechanism", "At" };
            r["Shortcut"] = new[] { "Shortcut" };
            r["Decide"] = new[] { "Key" };
            r["Branch"] = new[] { "Options" };
            r["Flag"] = new[] { "Key" };
            r["Talk"] = new[] { "Lines" };
            r["Kill"] = new[] { "Species" };
            r["Boss"] = new[] { "Species" };
            r["Collect"] = new[] { "Material", "Count" };
            r["Survive"] = new[] { "Seconds" };
            r["Defend"] = new[] { "Species" };
            r["Attune"] = new[] { "At" };
            r["Courier"] = new[] { "At", "To" };
            r["Escort"] = new[] { "To", "Name" };
            r["Hunt"] = new[] { "Species", "To" };
            r["Scan"] = new[] { "Species" };
            r["Wait"] = new[] { "Seconds" };
            r["Spawn"] = new[] { "Species" };
            r["Ally"] = new[] { "Name" };
            r["Prop"] = new[] { "Prop" };
            r["SetWeather"] = new[] { "Weather" };
            r["AddRep"] = new[] { "Faction", "Amount" };
            r["Message"] = new[] { "Text" };
            r["Choice"] = new[] { "Text", "Options" };
            _stepFields = f;
            _stepRequired = r;
        }

        private static void Def(Dictionary<string, HashSet<string>> f, string type, string[] place, params string[] fields)
        {
            HashSet<string> s = new HashSet<string>(CommonStep);
            foreach (string x in place) s.Add(x);
            foreach (string x in fields) s.Add(x);
            f[type] = s;
        }

        private static string[] Concat(string[] a, string[] b)
        {
            string[] c = new string[a.Length + b.Length];
            a.CopyTo(c, 0);
            b.CopyTo(c, a.Length);
            return c;
        }

        // ------------------------------------------------------------------ event fields

        private static readonly string[] CommonEvent =
        {
            "Id", "Title", "Kind", "Callout", "Objective", "Success", "Failure", "Weight", "Districts", "Veil", "HourMin", "HourMax",
            "Weathers", "Story", "ThirdEye", "Cooldown", "Duration", "Anchor", "Reward", "Lines", "ManualOnly"
        };

        private static Dictionary<string, HashSet<string>> _eventFields;
        private static Dictionary<string, string[]> _eventRequired;

        /// <summary>The fields an event kind reads (the common ones included), or null for an unknown kind.</summary>
        public static HashSet<string> EventFields(string kind)
        {
            if (_eventFields == null) BuildEventTables();
            HashSet<string> s;
            return kind != null && _eventFields.TryGetValue(kind, out s) ? s : null;
        }

        public static string[] EventRequired(string kind)
        {
            if (_eventRequired == null) BuildEventTables();
            string[] s;
            return kind != null && _eventRequired.TryGetValue(kind, out s) ? s : new string[0];
        }

        private static void BuildEventTables()
        {
            Dictionary<string, HashSet<string>> f = new Dictionary<string, HashSet<string>>();
            Dictionary<string, string[]> r = new Dictionary<string, string[]>();
            Ev(f, "Rescue", "Species", "Count", "Civilians", "Look", "FactionA", "Guards", "Prop", "Seconds", "Elite", "HealthScale");
            Ev(f, "Loot", "Species", "Count", "FactionA", "Guards", "Prop", "Points", "Elite");
            Ev(f, "Talk", "Look", "Name", "Prop");
            Ev(f, "Brawl", "FactionA", "FactionB", "Count", "Amount", "Seconds");
            Ev(f, "Relight", "Points", "Seconds", "Species", "Count", "Prop");
            Ev(f, "Swarm", "Species", "Count", "Species2", "Count2", "Elite", "HealthScale");
            Ev(f, "Rift", "Species", "Species2", "Health", "Count", "Seconds");
            Ev(f, "Escort", "Look", "Name", "To", "Species", "Count", "Health");
            Ev(f, "Rising", "Species", "Elite", "HealthScale", "Civilians", "Look", "Seconds", "Prop");
            Ev(f, "Pickup", "Prop", "Seconds", "Species", "Count", "FactionA", "Guards");
            Ev(f, "Sighting", "Species", "Elite", "HealthScale", "Prop");
            Ev(f, "Impostor", "Civilians", "Species", "Look", "Elite");
            Ev(f, "Sabotage", "Prop", "Health", "FactionA", "Guards", "Species", "Count");
            Ev(f, "Storm", "Points", "Seconds", "Species", "Count");
            Ev(f, "Holdout", "Seconds", "Species", "Count", "Civilians", "Look", "Points", "Prop");
            Ev(f, "Fieldwork", "Activity", "Points", "Seconds", "Species", "Look", "Name", "Prop");
            Ev(f, "Skirmish", "Activity", "Points", "Seconds", "Species", "Count", "HealthScale", "Prop");
            Ev(f, "CrystalDispute", "FactionA", "FactionB", "Count", "Seconds", "Prop");
            Ev(f, "DrakeRescue", "FactionA", "FactionB", "Count", "Seconds", "Prop");
            Ev(f, "CrystalCache", "FactionA", "FactionB", "Count", "Seconds", "Prop");
            r["CrystalDispute"] = new[] { "FactionA", "FactionB", "Count", "Seconds" };
            r["DrakeRescue"] = new[] { "FactionA", "FactionB", "Count", "Seconds" };
            r["CrystalCache"] = new[] { "FactionA", "FactionB", "Count", "Seconds" };
            r["Fieldwork"] = new[] { "Activity" };
            r["Skirmish"] = new[] { "Activity", "Species", "Count" };
            r["Rescue"] = new[] { "Civilians" };
            r["Loot"] = new string[0];
            r["Talk"] = new[] { "Look", "Name", "Lines" };
            r["Brawl"] = new[] { "FactionA", "FactionB" };
            r["Relight"] = new[] { "Points" };
            r["Swarm"] = new[] { "Species" };
            r["Rift"] = new[] { "Species" };
            r["Attune"] = new[] { "At" };
            r["Courier"] = new[] { "At", "To" };
            r["Escort"] = new[] { "Look", "Name" };
            r["Rising"] = new[] { "Species" };
            r["Pickup"] = new string[0];
            r["Sighting"] = new[] { "Species" };
            r["Impostor"] = new[] { "Civilians", "Species" };
            r["Sabotage"] = new string[0];
            r["Storm"] = new[] { "Points" };
            r["Holdout"] = new[] { "Seconds", "Species" };
            _eventFields = f;
            _eventRequired = r;
        }

        private static void Ev(Dictionary<string, HashSet<string>> f, string kind, params string[] fields)
        {
            HashSet<string> s = new HashSet<string>(CommonEvent);
            foreach (string x in fields) s.Add(x);
            f[kind] = s;
        }
    }
}
