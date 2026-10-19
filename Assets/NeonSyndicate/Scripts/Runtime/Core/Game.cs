using UnityEngine;

namespace NeonSyndicate
{
    public enum GameState
    {
        Loading,
        Title,
        Playing,
        Paused,
        Wasted,
        Busted,
        Cutscene
    }

    /// <summary>
    /// Static access point to every live system. GameRoot fills it in during boot. Everything is null-checked by
    /// callers so systems degrade gracefully if one fails to start.
    /// </summary>
    public static class Game
    {
        public const string Title = "GideoVamesAI: Crystal Manor Saga";
        public const string Version = "Crystal Manor Saga / Living World Renaissance 04H";

        public static GameRoot Root;
        public static GameState State = GameState.Loading;
        public static int Seed = 61724;

        // World
        public static CityLayout Layout;
        public static CityBuilder City;
        public static TrafficLights Signals;
        public static LightPool Lights;
        public static PropStreamer Props;
        public static Atmosphere Atmo;
        /// <summary>The Veil: Outer City or Inner World, the shift blend and the gameplay hooks.</summary>
        public static InnerWorld Inner;
        public static Texture2D MapTexture;

        // Audio and effects
        public static AudioManager Audio;
        public static RadioSystem Radio;
        public static Fx Fx;
        public static Explosions Explosions;

        // Player
        public static PlayerController Player;
        public static PlayerCamera Cam;

        // Simulation
        public static WantedSystem Wanted;
        public static PoliceManager Police;
        public static TrafficManager Traffic;
        public static PedManager Peds;
        /// <summary>Phase 6: snap-to-cover and enemy squad tactics (cover claims, flankers, grenades, turrets, the cover HUD).</summary>
        public static CoverDirector Cover;

        // Gameplay
        public static Economy Economy;
        public static Stats Stats;
        public static MissionManager Missions;
        public static Activities Activities;
        public static WorldServices Services;
        public static Pickups Pickups;
        public static Collectibles Collectibles;
        public static Abilities Abilities;
        public static Progression Progress;
        public static GearSystem Gear;
        public static Cheats Cheats;
        public static Gps Gps;

        // UI
        public static Hud Hud;
        public static Minimap Minimap;
        public static MenuSystem Menus;
        public static Phone Phone;

        // Phase 3: the mythic layer (creatures, materials, factions, quests, events, crafting, skills).
        public static CreatureDirector Creatures;
        public static Compendium Compendium;
        public static MaterialStore Materials;
        public static FactionSystem Factions;
        public static QuestBook Quests;
        public static WorldEvents Events;
        public static CraftingSystem Crafting;
        public static SkillTree Skills;

        // Phase 4: randomized loot guns and weapon cores, and travel powers (super speed, leap and glide, flight).
        public static LootArmory Loot;
        public static TravelPowers Travel;
        // Phase 5: the loot world (chests, hidden satchels, Chest Keys, the Collector, chest bounties).
        public static LootWorld LootWorld;
        // Phase 6: gadgets (tech skills), build sets and the city tier.
        public static GadgetSystem Gadgets;
        public static StrongholdSystem Strongholds;
        public static TerritorySystem Territories;
        public static InhabitedCitySystem Inhabited;
        // 04D: the crystal arsenal (offers, rewards, entitlements) and finite crystal encounters (quiet aftermath).
        public static CrystalArsenal Arsenal;
        public static CrystalEncounters Encounters;
        public static CrystalCombat04F CrystalCombat;
        // 04E Underheart and Bonds: the underground network, campaign guidance and cast, bonded companions, the personal
        // garage and creature-vehicle fusion, builds and durable rewards, underground ecology and the new bosses.
        public static UnderheartWorld Underheart;
        public static CampaignDirector Campaign;
        public static BondSystem Bonds;
        public static PersonalGarage Garage;
        public static FusionSystem Fusion;
        public static BuildSystem Builds;
        public static UnderheartEcology Ecology04E;
        public static UnderheartBosses UnderBosses;

        public static bool IsPlaying { get { return State == GameState.Playing; } }

        /// <summary>Where the player is (their vehicle if driving), or the origin before the player exists.</summary>
        public static Vector3 PlayerPosition
        {
            get
            {
                if (Player == null) return Cam != null ? Cam.transform.position : Vector3.zero;
                if (Player.IsRagdolled && Player.Ragdoll != null && !Player.InVehicle) return Player.Ragdoll.HipsPosition;
                return Player.transform.position;
            }
        }

        public static Vehicle PlayerVehicle { get { return Player != null ? Player.CurrentVehicle : null; } }

        public static bool IsPlayer(GameObject go)
        {
            return go != null && Player != null && go == Player.gameObject;
        }

        /// <summary>Clears every static so Enter Play Mode without domain reload starts clean.</summary>
        public static void ResetStatics()
        {
            CrystalCombat = null;
            CrystalCombatPolicy04F.ResetStatics();
            GuidanceRules04F.ResetStatics();
            CombatResponse04G.ResetStatics();
            CompanionMotion04G.ResetStatics();
            EventDrakeRules04G.ResetStatics();
            EventDrakeBehavior04H.ResetStatics();
            CourierRules04G.ResetStatics();
            Attunement04H.ResetStatics();
            NpcCrystal04F.ResetStatics();
            CityCombatRules04F.ResetStatics();
            Root = null;
            State = GameState.Loading;
            Layout = null;
            City = null;
            Signals = null;
            Lights = null;
            Props = null;
            Atmo = null;
            Inner = null;
            MapTexture = null;
            Audio = null;
            Radio = null;
            Fx = null;
            Explosions = null;
            Player = null;
            Cam = null;
            Wanted = null;
            Police = null;
            Traffic = null;
            Peds = null;
            Cover = null;
            Economy = null;
            Stats = null;
            Missions = null;
            Activities = null;
            Services = null;
            Pickups = null;
            Collectibles = null;
            Abilities = null;
            Progress = null;
            Gear = null;
            Cheats = null;
            Gps = null;
            Hud = null;
            Minimap = null;
            Menus = null;
            Phone = null;
            Creatures = null;
            Compendium = null;
            Materials = null;
            Factions = null;
            Quests = null;
            Events = null;
            Crafting = null;
            Skills = null;
            Loot = null;
            Travel = null;
            LootWorld = null;
            Gadgets = null;
            Strongholds = null;
            Territories = null;
            Inhabited = null;
            InhabitedCitySettings04C.ResetStatics();
            InhabitedCityBindings04C.ResetStatics();
            InhabitedResidentRules04C.ResetStatics();
            QuietSignal04C.ResetStatics();
            ArtInhabitedCity04C.ResetStatics();
            LivingCity04CQa.ResetStatics();
            TerritorySettings04B.ResetStatics();
            TerritoryCatalog.ResetStatics();
            TerritoryPlacement.ResetStatics();
            TerritoryEcologyRules04B.ResetStatics();
            TerritoryEventRules.ResetStatics();
            LivingTerritories04BQa.ResetStatics();
            ArtTerritories04B.ResetStatics();
            VisualRebirth04ASettings.ResetStatics();
            VisualRebirth04AAssets.ResetStatics();
            VisualRebirth04AAmbience.ResetStatics();
            VisualRebirth04AQa.ResetStatics();
            VisualRebirth04ACityPlan.ResetStatics();
            VisualRebirth04AArchitecture.ResetStatics();
            VisualRebirth04ANeighborhoods.ResetStatics();
            DistrictArchitecture.ResetStatics();
            DistrictMaterials.ResetStatics();
            DistrictCrystalFx.ResetStatics();
            DressingLayers.ResetStatics();
            WorldSystems.ResetStatics();
            MenuPages.ResetStatics();
            DebugPages.ResetStatics();
            HudOverlays.ResetStatics();
            SkillMods.ResetStatics();
            SkillDb.ResetStatics();
            ForgeDb.ResetStatics();
            ProgressionHooks.ResetStatics();
            SkillsPage.ResetStatics();
            FactionsPage.ResetStatics();
            ProgressionDebug.ResetStatics();
            Log.ResetStatics();
            PostGrade.ResetStatics();
            GInput.ResetStatics();
            GameTime.ResetStatics();
            Mats.ResetStatics();
            Tex.ResetStatics();
            Shapes.ResetStatics();
            WeaponModels.ResetStatics();
            Weapons.ResetStatics();
            LootGunModels.ResetStatics();
            LootCombat.ResetStatics();
            LootSounds.ResetStatics();
            VehicleModels.ResetStatics();
            BikeModels.ResetStatics();
            Vehicle.ResetStatics();
            SkidMarks.ResetStatics();
            Threats.ResetStatics();
            Interactable.ResetStatics();
            FireArea.ResetStatics();
            NpcController.ResetStatics();
            Collectibles.ResetStatics();
            Gangs.ResetStatics();
            StarterSafety.ResetStatics();
            PoliceResponseRules.ResetStatics();
            AmbientEncounterRules.ResetStatics();
            Abilities.ResetStatics();
            Blip.ResetStatics();
            UIKit.ResetStatics();
            BodyFactory.ResetStatics();
            CombatDirector.ResetStatics();
            // Phase 6 combat: the Resonance loop, armour plates, damage numbers, breakable creature parts.
            ResonanceSystem.ResetStatics();
            CombatSounds.ResetStatics();
            ArmorPlates.ResetStatics();
            DamageNumbers.ResetStatics();
            CreatureParts.ResetStatics();
            CreaturePartsDb.ResetStatics();
            CoverFinder.ResetStatics();
            // 04D crystal arsenal: registry fields, profiles, save transaction and one reset hub per ownership area.
            Arsenal = null;
            Encounters = null;
            CrystalFocusDb.ResetStatics();
            SaveFileTransaction.ResetStatics();
            SaveSystem.ResetStatics();
            CrystalWeaponsStatics.ResetStatics();
            CrystalArsenalStatics.ResetStatics();
            CrystalArtStatics.ResetStatics();
            CrystalEncounterStatics.ResetStatics();
            BossProfileStatics.ResetStatics();
            // 04E Underheart and Bonds: registry fields, shared hooks and one reset hub per work area.
            Underheart = null;
            Campaign = null;
            Bonds = null;
            Garage = null;
            Fusion = null;
            Builds = null;
            Ecology04E = null;
            UnderBosses = null;
            WorldBounds.ResetStatics();
            WorldContext.ResetStatics();
            WorldApertures.ResetStatics();
            ModuleStatus.ResetStatics();
            MapOverlays.ResetStatics();
            PlayerDamageHooks.ResetStatics();
            ResidentReservations04E.ResetStatics();
            Ids04EStatics.ResetStatics();
            UnderworldStatics.ResetStatics();
            VenueStatics04E.ResetStatics();
            CityAscensionGeometry04I.ResetStatics();
            CityDrive04I.ResetStatics();
            CityIncidentRules04I.ResetStatics();
            GuidanceStatics04E.ResetStatics();
            CampaignStatics04E.ResetStatics();
            BondStatics.ResetStatics();
            FusionStatics.ResetStatics();
            BuildStatics04E.ResetStatics();
            EcologyStatics04E.ResetStatics();
            UnderBossStatics.ResetStatics();
            Underheart04ESounds.ResetStatics();
            ShrineMeshes.ResetStatics();
            GearSystem.ResetStatics();
            // Phase 6 gear: build sets, gear score and city tier, gadget models and sounds.
            GearBuilds.ResetStatics();
            GearScore.ResetStatics();
            GadgetModels.ResetStatics();
            GadgetSounds.ResetStatics();
            // Creatures and the bestiary.
            Creature.ResetStatics();
            CreatureRig.ResetStatics();
            CrystalDrakeSurface.ResetStatics();
            StarterHabitat.ResetStatics();
            CreatureAttackDb.ResetStatics();
            CreatureSpeciesDb.ResetStatics();
            GroundRing.ResetStatics();
            CreatureShot.ResetStatics();
            CreatureAudio.ResetStatics();
            BestiaryPage.ResetStatics();
            // Phase 3 quests and world events (QuestBook, QuestCatalog, QuestWorld, RiftTear).
            QuestCatalog.ResetStatics();
            QuestWorld.ResetStatics();
            RiftTear.ResetStatics();
            QuestPlaces.ResetStatics();
            QuestBook.ResetStatics();
            // Lookup caches, the render path hooks (URP graphics and eye adaptation too) and the QA tools: this is their
            // only reset hook, so nothing is reset twice.
            FactionDb.ResetStatics();
            MaterialDb.ResetStatics();
            ForgeScreen.ResetStatics();
            RenderPath.ResetStatics();
            QaTools.ResetStatics();
            CityRebirthBoot04O.ResetStatics();
            CityRebirthRules04O.ResetStatics();
            Settings.ResetRebirth04O();
            CityRebirthGeometry04O.ResetStatics();
            // Phase 1 lighting (reflection probes, lamp shafts, volumetrics) through one hub.
            SkyLighting.ResetStatics();
        }
    }
}
