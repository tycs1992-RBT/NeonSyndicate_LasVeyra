using System.Collections.Generic;

namespace NeonSyndicate
{
    /// <summary>The three skill trees.</summary>
    public enum SkillTreeId
    {
        Body = 0,
        Mind = 1,
        Shard = 2
    }

    /// <summary>One change a skill makes per rank: add an amount to a stat, or multiply it.</summary>
    public struct SkillEffect
    {
        public SkillStat Stat;
        public float PerRank;
        public bool Multiply;

        public SkillEffect(SkillStat stat, float perRank, bool multiply)
        {
            Stat = stat;
            PerRank = perRank;
            Multiply = multiply;
        }
    }

    /// <summary>
    /// A node in a skill tree. Tier is the row (0 at the top), Lane the column (0..4). A node needs every node in
    /// Prereqs learned, and the tree must already hold SkillDb.TierPoints[Tier] spent points. Signature nodes change how
    /// you play (their special behaviour lives in SkillTree); the rest are plain stat nodes.
    /// </summary>
    public sealed class SkillNode
    {
        public string Id;
        public string Name;
        public SkillTreeId Tree;
        public int Tier;
        public int Lane;
        public int MaxRank = 1;
        /// <summary>Skill points per rank.</summary>
        public int Cost = 1;
        public bool Signature;
        /// <summary>What it does, in plain words (multi-rank nodes say "per rank").</summary>
        public string Text = "";
        public string[] Prereqs = new string[0];
        public SkillEffect[] Effects = new SkillEffect[0];
    }

    /// <summary>
    /// Every skill node. Ids are stable: saves store them. 30 nodes per tree in six tiers of five lanes.
    /// Body: melee, stamina, health, dodge and mobility, guard and parry. Mind: guns, crits, stealth against creatures and
    /// loot, economy, driving. Shard: crystal powers, energy, the Veil, elements, damage to creatures.
    /// </summary>
    public static class SkillDb
    {
        public const int Tiers = 6;
        public const int Lanes = 5;

        /// <summary>Points that must already be spent in a tree before its nodes of each tier can be learned.</summary>
        public static readonly int[] TierPoints = { 0, 2, 5, 9, 14, 20 };

        // Signature node ids that runtime code checks by name.
        public const string AdrenalSurge = "body_adrenal_surge";
        public const string SecondWind = "body_second_wind";
        public const string Momentum = "body_momentum";
        public const string Berserker = "body_berserker";
        public const string WeightlessRoll = "body_weightless_roll";
        public const string KillingRiposte = "body_killing_riposte";
        public const string TellingBlow = "mind_telling_blow";
        public const string BeastHunter = "mind_beast_hunter";
        public const string SilverTongue = "mind_silver_tongue";
        public const string GetawayDriver = "mind_getaway_driver";
        public const string GhostStep = "mind_ghost_step";
        public const string StillPoint = "mind_still_point";
        public const string CityRoyalty = "mind_city_royalty";
        public const string EmberWard = "shard_ember_ward";
        public const string SoulHarvest = "shard_soul_harvest";
        public const string VeilWalker = "shard_veil_walker";
        public const string ShardEcho = "shard_echo";
        public const string StormConduit = "shard_storm_conduit";
        public const string ApexResonance = "shard_apex_resonance";
        public const string PrismMind = "shard_prism_mind";
        public const string PrismaticInfusion = "shard_prismatic_infusion";
        public const string RadiantOverload = "shard_radiant_overload";
        public const string VeilSovereign = "shard_veil_sovereign";

        public static readonly SkillNode[] All = Build();

        private static Dictionary<string, SkillNode> _byId;

        /// <summary>Clears the id lookup cache (rebuilt on first use).</summary>
        public static void ResetStatics()
        {
            _byId = null;
        }

        public static SkillNode Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null)
            {
                Dictionary<string, SkillNode> d = new Dictionary<string, SkillNode>();
                foreach (SkillNode n in All) d[n.Id] = n;
                _byId = d;
            }
            SkillNode node;
            return _byId.TryGetValue(id, out node) ? node : null;
        }

        public static string TreeName(SkillTreeId t)
        {
            switch (t)
            {
                case SkillTreeId.Body: return "Body";
                case SkillTreeId.Mind: return "Mind";
                default: return "Shard";
            }
        }

        public static string TreeBlurb(SkillTreeId t)
        {
            switch (t)
            {
                case SkillTreeId.Body: return "Melee, stamina, health, dodging and the guard.";
                case SkillTreeId.Mind: return "Guns, critical hits, stealth, loot, money and driving.";
                default: return "Crystal powers, energy, the Veil, elements and creature hunting.";
            }
        }

        /// <summary>The node at a tree, tier and lane, or null.</summary>
        public static SkillNode At(SkillTreeId tree, int tier, int lane)
        {
            foreach (SkillNode n in All) if (n.Tree == tree && n.Tier == tier && n.Lane == lane) return n;
            return null;
        }

        // ------------------------------------------------------------------ the trees

        private static SkillEffect Add(SkillStat s, float perRank) { return new SkillEffect(s, perRank, false); }
        private static SkillEffect Mul(SkillStat s, float perRank) { return new SkillEffect(s, perRank, true); }
        private static string[] Req(params string[] ids) { return ids; }

        private static SkillNode N(string id, SkillTreeId tree, int tier, int lane, string name, int ranks, int cost, string text, string[] prereqs, params SkillEffect[] fx)
        {
            return new SkillNode { Id = id, Name = name, Tree = tree, Tier = tier, Lane = lane, MaxRank = ranks, Cost = cost, Text = text, Prereqs = prereqs ?? new string[0], Effects = fx ?? new SkillEffect[0] };
        }

        private static SkillNode Sig(string id, SkillTreeId tree, int tier, int lane, string name, int cost, string text, string[] prereqs, params SkillEffect[] fx)
        {
            SkillNode n = N(id, tree, tier, lane, name, 1, cost, text, prereqs, fx);
            n.Signature = true;
            return n;
        }

        private static SkillNode[] Build()
        {
            const SkillTreeId B = SkillTreeId.Body, M = SkillTreeId.Mind, S = SkillTreeId.Shard;
            List<SkillNode> l = new List<SkillNode>
            {
                // ================================================================ BODY
                // Tier 0
                N("body_heavy_hands", B, 0, 0, "Heavy Hands", 3, 1, "+4% melee damage per rank.", null, Add(SkillStat.MeleeDamage, 0.04f)),
                N("body_deep_lungs", B, 0, 1, "Deep Lungs", 3, 1, "+6 max stamina per rank.", null, Add(SkillStat.StaminaMax, 6f)),
                N("body_thick_skin", B, 0, 2, "Thick Skin", 3, 1, "+15 max health per rank.", null, Add(SkillStat.HealthMax, 15f)),
                N("body_light_step", B, 0, 3, "Light Step", 3, 1, "Dodge rolls cost 6% less stamina per rank.", null, Add(SkillStat.DodgeCost, -0.06f)),
                N("body_steady_guard", B, 0, 4, "Steady Guard", 3, 1, "Guarding costs 6% less stamina per rank.", null, Add(SkillStat.GuardCost, -0.06f)),
                // Tier 1
                N("body_brawler", B, 1, 0, "Brawler", 2, 1, "+6% melee damage per rank.", Req("body_heavy_hands"), Add(SkillStat.MeleeDamage, 0.06f)),
                N("body_second_breath", B, 1, 1, "Second Breath", 3, 1, "Stamina refills 8% faster per rank.", Req("body_deep_lungs"), Add(SkillStat.StaminaRegen, 0.08f)),
                N("body_field_medic", B, 1, 2, "Field Medic", 2, 1, "Shard Tonics heal 8% more per rank.", Req("body_thick_skin"), Add(SkillStat.TonicHeal, 0.08f)),
                N("body_quick_feet", B, 1, 3, "Quick Feet", 2, 1, "Sprint 3% faster per rank.", Req("body_light_step"), Add(SkillStat.MoveSpeed, 0.03f)),
                N("body_quick_reflexes", B, 1, 4, "Quick Reflexes", 2, 1, "The parry window lasts 20 ms longer per rank.", Req("body_steady_guard"), Add(SkillStat.ParryWindow, 0.02f)),
                // Tier 2
                N("body_brick_fists", B, 2, 0, "Brick Fists", 3, 1, "+5% melee damage per rank.", Req("body_brawler"), Add(SkillStat.MeleeDamage, 0.05f)),
                N("body_long_haul", B, 2, 1, "Long Haul", 2, 1, "+8 max stamina per rank.", Req("body_second_breath"), Add(SkillStat.StaminaMax, 8f)),
                N("body_hardened", B, 2, 2, "Hardened", 3, 1, "Take 3% less damage per rank.", Req("body_field_medic"), Add(SkillStat.DamageTaken, -0.03f)),
                N("body_ghost_roll", B, 2, 3, "Ghost Roll", 2, 1, "Dodge rolls stay untouchable 25 ms longer per rank.", Req("body_quick_feet"), Add(SkillStat.DodgeIFrames, 0.025f)),
                N("body_counter_cut", B, 2, 4, "Counter Cut", 2, 1, "Ripostes deal 15% more damage per rank.", Req("body_quick_reflexes"), Add(SkillStat.RiposteDamage, 0.15f)),
                // Tier 3
                N("body_savage", B, 3, 0, "Savage", 2, 1, "+6% melee damage per rank.", Req("body_brick_fists"), Add(SkillStat.MeleeDamage, 0.06f)),
                Sig(AdrenalSurge, B, 3, 1, "Adrenal Surge", 2, "Every takedown, people or creatures, refills 20 stamina on the spot.", Req("body_long_haul")),
                Sig(SecondWind, B, 3, 2, "Second Wind", 2, "Once per fight, dropping below 20% health restores all your stamina and 15% of your health.", Req("body_hardened")),
                Sig(Momentum, B, 3, 3, "Momentum", 2, "A perfect dodge makes your melee hits deal 30% more damage for 2.5 seconds.", Req("body_ghost_roll")),
                N("body_bulwark", B, 3, 4, "Bulwark", 2, 1, "Guarding costs 8% less stamina per rank.", Req("body_counter_cut"), Add(SkillStat.GuardCost, -0.08f)),
                // Tier 4
                N("body_crushing_blows", B, 4, 0, "Crushing Blows", 2, 1, "+5% melee damage per rank.", Req("body_savage"), Add(SkillStat.MeleeDamage, 0.05f)),
                N("body_tireless", B, 4, 1, "Tireless", 2, 1, "Stamina refills 10% faster per rank.", Req(AdrenalSurge), Add(SkillStat.StaminaRegen, 0.1f)),
                N("body_vital_reserve", B, 4, 2, "Vital Reserve", 1, 2, "+1 Shard Tonic charge.", Req(SecondWind), Add(SkillStat.TonicCharges, 1f)),
                N("body_gale_runner", B, 4, 3, "Gale Runner", 1, 2, "Sprint 4% faster and dodge rolls cost 8% less stamina.", Req(Momentum), Add(SkillStat.MoveSpeed, 0.04f), Add(SkillStat.DodgeCost, -0.08f)),
                N("body_perfect_guard", B, 4, 4, "Perfect Guard", 1, 2, "The parry window lasts 30 ms longer.", Req("body_bulwark"), Add(SkillStat.ParryWindow, 0.03f)),
                // Tier 5
                Sig(Berserker, B, 5, 0, "Berserker", 3, "While you are below 40% health your melee hits deal 25% more damage.", Req("body_crushing_blows")),
                N("body_unyielding", B, 5, 1, "Unyielding", 1, 3, "+20 max stamina and guarding costs 10% less stamina.", Req("body_tireless"), Add(SkillStat.StaminaMax, 20f), Add(SkillStat.GuardCost, -0.1f)),
                N("body_iron_body", B, 5, 2, "Iron Body", 1, 3, "+40 max health and take 5% less damage.", Req("body_vital_reserve"), Add(SkillStat.HealthMax, 40f), Add(SkillStat.DamageTaken, -0.05f)),
                Sig(WeightlessRoll, B, 5, 3, "Weightless Roll", 3, "For 3 seconds after a perfect dodge, dodge rolls cost no stamina at all.", Req("body_gale_runner")),
                Sig(KillingRiposte, B, 5, 4, "Killing Riposte", 3, "Ripostes deal 50% more damage.", Req("body_perfect_guard"), Add(SkillStat.RiposteDamage, 0.5f)),

                // ================================================================ MIND
                // Tier 0
                N("mind_trigger_discipline", M, 0, 0, "Trigger Discipline", 3, 1, "+4% gun damage per rank.", null, Add(SkillStat.GunDamage, 0.04f)),
                N("mind_keen_eye", M, 0, 1, "Keen Eye", 3, 1, "+2% critical hit chance per rank, for melee and guns.", null, Add(SkillStat.CritChance, 0.02f)),
                N("mind_soft_tread", M, 0, 2, "Soft Tread", 3, 1, "Creatures notice you from 6% closer per rank.", null, Add(SkillStat.CreatureSense, -0.06f)),
                N("mind_street_smarts", M, 0, 3, "Street Smarts", 3, 1, "+4% cash from jobs, pickups and rewards per rank.", null, Add(SkillStat.CashGain, 0.04f)),
                N("mind_road_feel", M, 0, 4, "Road Feel", 3, 1, "+3% tire grip in vehicles you drive per rank.", null, Add(SkillStat.VehicleGrip, 0.03f)),
                // Tier 1
                N("mind_marksman", M, 1, 0, "Marksman", 2, 1, "+5% gun damage per rank.", Req("mind_trigger_discipline"), Add(SkillStat.GunDamage, 0.05f)),
                N("mind_weak_spots", M, 1, 1, "Weak Spots", 2, 1, "+3% critical hit chance per rank.", Req("mind_keen_eye"), Add(SkillStat.CritChance, 0.03f)),
                N("mind_scavenger", M, 1, 2, "Scavenger", 3, 1, "+5% chance on every creature loot roll per rank.", Req("mind_soft_tread"), Add(SkillStat.MaterialFind, 0.05f)),
                N("mind_haggler", M, 1, 3, "Haggler", 3, 1, "4% off at shops and the Forge per rank.", Req("mind_street_smarts"), Add(SkillStat.Discount, 0.04f)),
                N("mind_nitro_tuning", M, 1, 4, "Nitro Tuning", 2, 1, "Nitro pushes 8% harder per rank.", Req("mind_road_feel"), Add(SkillStat.VehicleBoost, 0.08f)),
                // Tier 2
                N("mind_gunsmith", M, 2, 0, "Gunsmith", 2, 1, "+5% gun damage per rank.", Req("mind_marksman"), Add(SkillStat.GunDamage, 0.05f)),
                Sig(TellingBlow, M, 2, 1, "Telling Blow", 2, "Critical hits deal double damage instead of one and a half times.", Req("mind_weak_spots"), Add(SkillStat.CritDamage, 0.5f)),
                N("mind_tracker", M, 2, 2, "Tracker", 2, 1, "Creatures notice you from 8% closer per rank.", Req("mind_scavenger"), Add(SkillStat.CreatureSense, -0.08f)),
                N("mind_lumen_siphon", M, 2, 3, "Lumen Siphon", 3, 1, "+5% Lumen from defeated enemies per rank.", Req("mind_haggler"), Add(SkillStat.LumenGain, 0.05f)),
                N("mind_corner_carver", M, 2, 4, "Corner Carver", 2, 1, "+4% tire grip per rank.", Req("mind_nitro_tuning"), Add(SkillStat.VehicleGrip, 0.04f)),
                // Tier 3
                N("mind_close_range", M, 3, 0, "Close Range", 2, 1, "+10% gun damage within 8 m per rank.", Req("mind_gunsmith"), Add(SkillStat.GunPointBlank, 0.1f)),
                N("mind_lethal_precision", M, 3, 1, "Lethal Precision", 2, 1, "+4% critical hit chance per rank.", Req(TellingBlow), Add(SkillStat.CritChance, 0.04f)),
                Sig(BeastHunter, M, 3, 2, "Beast Hunter", 2, "+15% damage to creatures and +15% chance on every creature loot roll.", Req("mind_tracker"), Add(SkillStat.CreatureDamage, 0.15f), Add(SkillStat.MaterialFind, 0.15f)),
                Sig(SilverTongue, M, 3, 3, "Silver Tongue", 2, "Reputation gains are 25% bigger, and killing gang members costs only half the reputation.", Req("mind_lumen_siphon"), Add(SkillStat.RepGain, 0.25f)),
                Sig(GetawayDriver, M, 3, 4, "Getaway Driver", 2, "While you drive, a nitro charge refills every 90 seconds (up to three).", Req("mind_corner_carver")),
                // Tier 4
                N("mind_long_shot", M, 4, 0, "Long Shot", 2, 1, "+10% gun damage beyond 30 m per rank.", Req("mind_close_range"), Add(SkillStat.GunLongShot, 0.1f)),
                N("mind_cutthroat", M, 4, 1, "Cutthroat", 1, 2, "+5% critical hit chance.", Req("mind_lethal_precision"), Add(SkillStat.CritChance, 0.05f)),
                Sig(GhostStep, M, 4, 2, "Ghost Step", 2, "While crouched, creatures notice you from only half as far.", Req(BeastHunter)),
                N("mind_fence", M, 4, 3, "Fence", 2, 1, "Forgewrights pay 15% more for your materials per rank.", Req(SilverTongue), Add(SkillStat.SalvageBonus, 0.15f)),
                N("mind_road_veteran", M, 4, 4, "Road Veteran", 1, 2, "+5% tire grip and nitro pushes 10% harder.", Req(GetawayDriver), Add(SkillStat.VehicleGrip, 0.05f), Add(SkillStat.VehicleBoost, 0.1f)),
                // Tier 5
                Sig(StillPoint, M, 5, 0, "Still Point", 3, "While the Third Eye is open, your guns deal 25% more damage.", Req("mind_long_shot")),
                N("mind_cold_read", M, 5, 1, "Cold Read", 1, 3, "+8% critical hit chance.", Req("mind_cutthroat"), Add(SkillStat.CritChance, 0.08f)),
                N("mind_predators_patience", M, 5, 2, "Predator's Patience", 1, 3, "+10% damage to creatures, and creatures notice you from 10% closer.", Req(GhostStep), Add(SkillStat.CreatureDamage, 0.1f), Add(SkillStat.CreatureSense, -0.1f)),
                Sig(CityRoyalty, M, 5, 3, "City Royalty", 3, "Faction shop discounts are doubled, and you earn 10% more cash.", Req("mind_fence"), Add(SkillStat.CashGain, 0.1f)),
                N("mind_phantom_driver", M, 5, 4, "Phantom Driver", 1, 3, "+6% tire grip and nitro pushes 15% harder.", Req("mind_road_veteran"), Add(SkillStat.VehicleGrip, 0.06f), Add(SkillStat.VehicleBoost, 0.15f)),

                // ================================================================ SHARD
                // Tier 0
                N("shard_focused_light", S, 0, 0, "Focused Light", 3, 1, "+5% crystal power damage per rank.", null, Add(SkillStat.PowerDamage, 0.05f)),
                N("shard_deep_well", S, 0, 1, "Deep Well", 3, 1, "+8 max crystal energy per rank.", null, Add(SkillStat.EnergyMax, 8f)),
                N("shard_veil_sense", S, 0, 2, "Veil Sense", 3, 1, "The Veil drains 8% less energy per rank.", null, Add(SkillStat.VeilDrain, -0.08f)),
                N("shard_elemental_affinity", S, 0, 3, "Elemental Affinity", 3, 1, "+4% chance per rank for your weapon's element to trigger on a hit.", null, Add(SkillStat.ElementProc, 0.04f)),
                N("shard_hunters_resonance", S, 0, 4, "Hunter's Resonance", 3, 1, "+3% damage to creatures per rank.", null, Add(SkillStat.CreatureDamage, 0.03f)),
                // Tier 1
                N("shard_efficient_casting", S, 1, 0, "Efficient Casting", 3, 1, "Crystal powers cost 5% less energy per rank.", Req("shard_focused_light"), Add(SkillStat.PowerCost, -0.05f)),
                N("shard_swift_recharge", S, 1, 1, "Swift Recharge", 3, 1, "Crystal energy refills 10% faster per rank.", Req("shard_deep_well"), Add(SkillStat.EnergyRegen, 0.1f)),
                N("shard_thin_veil", S, 1, 2, "Thin Veil", 2, 1, "Shifting into the Veil costs 10% less energy per rank.", Req("shard_veil_sense"), Add(SkillStat.VeilShiftCost, -0.1f)),
                N(EmberWard, S, 1, 3, "Ember Ward", 1, 1, "Fire on you burns out twice as fast.", Req("shard_elemental_affinity")),
                N(SoulHarvest, S, 1, 4, "Soul Harvest", 1, 1, "Every creature you kill restores 6 crystal energy.", Req("shard_hunters_resonance")),
                // Tier 2
                N("shard_overcharge", S, 2, 0, "Overcharge", 2, 1, "+6% crystal power damage per rank.", Req("shard_efficient_casting"), Add(SkillStat.PowerDamage, 0.06f)),
                N("shard_conduit", S, 2, 1, "Conduit", 2, 1, "Every takedown restores 3 more crystal energy per rank.", Req("shard_swift_recharge"), Add(SkillStat.KillEnergy, 3f)),
                Sig(VeilWalker, S, 2, 2, "Veil Walker", 2, "The Veil drains half as much energy, and shifting into it costs 40% less.", Req("shard_thin_veil"), Mul(SkillStat.VeilDrain, 0.5f), Mul(SkillStat.VeilShiftCost, 0.6f)),
                N("shard_frost_skin", S, 2, 3, "Frost Skin", 2, 1, "Take 8% less damage deep in the Frostline per rank.", Req(EmberWard), Add(SkillStat.FrostResist, 0.08f)),
                N("shard_crystal_hunter", S, 2, 4, "Crystal Hunter", 2, 1, "+4% damage to creatures per rank.", Req(SoulHarvest), Add(SkillStat.CreatureDamage, 0.04f)),
                // Tier 3
                N("shard_power_surge", S, 3, 0, "Power Surge", 2, 1, "+6% crystal power damage per rank.", Req("shard_overcharge"), Add(SkillStat.PowerDamage, 0.06f)),
                Sig(ShardEcho, S, 3, 1, "Shard Echo", 2, "Perfect dodges refund 15 crystal energy.", Req("shard_conduit")),
                N("shard_deep_veil", S, 3, 2, "Deep Veil", 2, 1, "The Veil drains 10% less energy per rank.", Req(VeilWalker), Add(SkillStat.VeilDrain, -0.1f)),
                Sig(StormConduit, S, 3, 3, "Storm Conduit", 2, "Melee takedowns with an elemental weapon restore 10 crystal energy.", Req("shard_frost_skin")),
                Sig(ApexResonance, S, 3, 4, "Apex Resonance", 2, "Every creature you kill heals you for 6% of your max health.", Req("shard_crystal_hunter")),
                // Tier 4
                Sig(PrismMind, S, 4, 0, "Prism Mind", 2, "While the Third Eye is open, crystal powers cost 25% less energy.", Req("shard_power_surge")),
                N("shard_reservoir", S, 4, 1, "Reservoir", 2, 1, "+12 max crystal energy per rank.", Req(ShardEcho), Add(SkillStat.EnergyMax, 12f)),
                N("shard_veilborn", S, 4, 2, "Veilborn", 1, 2, "The Veil drains 10% less energy and shifting costs 10% less.", Req("shard_deep_veil"), Add(SkillStat.VeilDrain, -0.1f), Add(SkillStat.VeilShiftCost, -0.1f)),
                Sig(PrismaticInfusion, S, 4, 3, "Prismatic Infusion", 2, "Forge infusions also add +10% elemental trigger chance to your Weapon Core (up to +30%).", Req(StormConduit)),
                N("shard_titan_slayer", S, 4, 4, "Titan Slayer", 1, 2, "+6% damage to creatures.", Req(ApexResonance), Add(SkillStat.CreatureDamage, 0.06f)),
                // Tier 5
                Sig(RadiantOverload, S, 5, 0, "Radiant Overload", 3, "While your crystal energy is nearly full, crystal powers deal 30% more damage.", Req(PrismMind)),
                N("shard_infinite_well", S, 5, 1, "Infinite Well", 1, 3, "+25 max crystal energy, and energy refills 15% faster.", Req("shard_reservoir"), Add(SkillStat.EnergyMax, 25f), Add(SkillStat.EnergyRegen, 0.15f)),
                Sig(VeilSovereign, S, 5, 2, "Veil Sovereign", 3, "Inside the Veil you take 15% less damage and your crystal powers deal 15% more.", Req("shard_veilborn")),
                N("shard_elemental_mastery", S, 5, 3, "Elemental Mastery", 1, 3, "+10% chance for your weapon's element to trigger on a hit.", Req(PrismaticInfusion), Add(SkillStat.ElementProc, 0.1f)),
                N("shard_beastbane", S, 5, 4, "Beastbane", 1, 3, "+8% damage to creatures.", Req("shard_titan_slayer"), Add(SkillStat.CreatureDamage, 0.08f))
            };
            return l.ToArray();
        }
    }
}
