using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Boots the whole game from code: settings, physics layers, the procedural city, synthesized audio, every
    /// gameplay system and the UI. Then runs the title screen, New Game / Continue / Load / Free Roam, the WASTED
    /// and BUSTED sequences, autosaves and the story ending. Created automatically when you press Play in any
    /// scene (the generated NeonSyndicate scene also contains one).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class GameRoot : MonoBehaviour
    {
        public bool GodMode;
        public bool FreeRoam;
        /// <summary>Where the player last used a safehouse (zero until they do). Saves put you back here.</summary>
        public Vector3 LastSafehouse;
        /// <summary>
        /// 04E: the last safehouse chosen on the surface (zero until one is). An Underheart rest point changes
        /// LastSafehouse but never this, so a save made on the surface stores this instead of the rest point
        /// (SafehouseRules04E.SavePosition). Set it through SetSafehouse.
        /// </summary>
        public Vector3 LastSurfaceSafehouse;

        private float _progress;
        private string _status = "Starting up";
        private bool _booting = true;
        private bool _fatal;
        private bool _transition;
        /// <summary>True while a session start, title return or TravelTo fade owns the screen (lifts read it).</summary>
        public bool InTransition { get { return _transition; } }
        private readonly List<string> _errors = new List<string>();
        private float _lastAutosave = -999f;
        private int _tip;
        private float _tipTimer = 5f;
        private Transform _systems;
        private Transform _world;
        private float _bootStart;
        private CityRevision04O _revision04O;
        private SaveData _resume04O;
        private bool _resumeSession04O, _resumeFree04O, _resumeSkip04O;

        private static readonly string[] Tips =
        {
            "Hold Tab for the weapon wheel. The mouse wheel or keys 1-8 cycle weapons instantly.",
            "Press F next to a car to get in. If somebody's driving, Malik pulls them out.",
            "Tap E in an emergency vehicle for the siren. Hold it for the horn.",
            "Press J in a taxi, police vehicle or ambulance to start side jobs.",
            "Break line of sight and leave the flashing search area to lose your wanted level.",
            "Resprays wipe your wanted level if no officer is watching.",
            "Caps Lock opens the Third Eye: bullet time, powered by the yellow crystal bar.",
            "Click anywhere on the pause map (M) to drop a GPS waypoint.",
            "Payphones with a green P are ringing. Iris Moon pays well for quiet work.",
            "Street races are marked with a white F. Drive into the marker to race Nico's crew.",
            "Aim a gun at a store clerk to rob the register. Expect company.",
            "Buy properties (green $) for daily income, paid every morning at 6:00.",
            "Type cheat codes while playing. Docs/CHEATS.md has the full list.",
            "Fifty data chips, twenty crystal shards, ten spell stones and ten Echo Relics are hidden around Las Veyra.",
            "Vehicles take visible damage. Smoke means trouble, fire means get out now.",
            "F12 takes a screenshot at any time. The phone camera (P) works too."
        };

        // ================================================================== auto boot

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsOnLoad()
        {
            Game.ResetStatics();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoBoot()
        {
            if (FindAnyObjectByType<GameRoot>() != null) return;
            GameObject go = new GameObject("Neon Syndicate");
            go.AddComponent<GameRoot>();
        }

        private void Awake()
        {
            CityRevision04O requested; SaveData resume; bool free, skip;
            if (CityRebirthBoot04O.Take(out requested, out resume, out free, out skip))
            {
                // Old scene objects have been unloaded. Clear every original registry before the new boot.
                Game.ResetStatics();
                _revision04O = requested; _resume04O = resume; _resumeFree04O = free; _resumeSkip04O = skip; _resumeSession04O = true;
            }
            // One game (owner decision 2026-10-23): with no save or menu request the boot and title play the newest world.
            else { _revision04O = OneGameH8S.World; _defaultBootH8S = true; }
            if (Game.Root != null && Game.Root != this)
            {
                Destroy(gameObject);
                return;
            }
            Game.Root = this;
            Game.State = GameState.Loading;
            _bootStart = Time.realtimeSinceStartup;
            Application.runInBackground = true;
            Log.Info(Game.Title + " " + Game.Version + " booting (Unity " + Application.unityVersion + ").");
            DisableSceneLeftovers();
            try { Settings.Load(); } catch (Exception e) { Fail("Settings", e); }
            try { Settings.ApplyGraphics(); } catch (Exception e) { Fail("Graphics settings", e); }
            try { Layers.ConfigureCollisions(); } catch (Exception e) { Fail("Physics layers", e); }
            Time.timeScale = 1f;
            Time.fixedDeltaTime = GameTime.BaseFixedDelta;
            Time.maximumDeltaTime = 0.1f;
            _systems = new GameObject("Systems").transform;
            _systems.SetParent(transform, false);
            _world = new GameObject("Las Veyra (World)").transform;
            try { Game.Cam = PlayerCamera.Create(); } catch (Exception e) { Fail("Camera", e); }
            GInput.SetCursorLocked(false);
            StartCoroutine(Boot());
        }

        private void OnDestroy()
        {
            if (Game.Root == this) Game.Root = null;
        }

        /// <summary>Turns off cameras, listeners and directional lights a template scene may contain; the game makes its own.</summary>
        private static void DisableSceneLeftovers()
        {
            foreach (Camera c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c != null && c.GetComponent<PlayerCamera>() == null) c.gameObject.SetActive(false);
            foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
                if (l != null && l.GetComponent<PlayerCamera>() == null) l.enabled = false;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l != null && l.type == LightType.Directional) l.gameObject.SetActive(false);
        }

        // ================================================================== boot sequence

        private void SetProgress(float p, string status)
        {
            _progress = Mathf.Clamp01(Mathf.Max(_progress, p));
            if (!string.IsNullOrEmpty(status)) _status = status;
        }

        private void Fail(string what, Exception e)
        {
            Log.Exception("Boot: " + what, e);
            _errors.Add(what + ": " + e.Message);
        }

        private void Step(string what, Action a)
        {
            try { a(); }
            catch (Exception e) { Fail(what, e); }
        }

        /// <summary>A boot step that also records the module on the STATUS page (Loaded, or Failed with the error).</summary>
        private void StepModule(string id, string title, Action a)
        {
            try
            {
                a();
                ModuleRecord r = ModuleStatus.Find(id);
                if (r == null || r.State == ModuleState.NotStarted) ModuleStatus.Report(id, title, ModuleState.Loaded, "");
                else if (string.IsNullOrEmpty(r.Title) || r.Title == id) r.Title = title;
            }
            catch (Exception e)
            {
                Fail(title, e);
                ModuleStatus.Report(id, title, ModuleState.Failed, e.Message, "The rest of the game still runs. Send the Console error along.");
            }
        }

        /// <summary>Rows for the earlier modules the 04E opening builds on, from their live state (not their catalogs).</summary>
        private void ReportLegacyModules()
        {
            InhabitedCitySystem ic = Game.Inhabited;
            if (ic == null) ModuleStatus.Report("inhabited_04c", "Inhabited city (04C)", ModuleState.Failed, "The venue system did not start.");
            else if (!ic.EnabledAtBoot) ModuleStatus.Report("inhabited_04c", "Inhabited city (04C)", ModuleState.Disabled, "Turned off in Settings (applies at the next start).", "Settings > Inhabited city, then restart.");
            else
            {
                int ready = 0;
                foreach (VenueBinding04C v in ic.Venues) if (v != null && v.Available) ready++;
                ModuleStatus.Report("inhabited_04c", "Inhabited city (04C)", ic.IsOperational ? (ready == ic.Venues.Count ? ModuleState.Loaded : ModuleState.Partial) : ModuleState.Failed,
                    ready == ic.Venues.Count ? "" : (ic.Venues.Count - ready) + " venue(s) could not be built on their parcels.");
                ModuleStatus.SetCount("inhabited_04c", ready, "enterable venues built");
            }
            ModuleStatus.Report("arsenal_04d", "Crystal arsenal (04D)", Game.Arsenal != null ? ModuleState.Loaded : ModuleState.Failed, Game.Arsenal != null ? "" : "The crystal arsenal did not start.");
            ModuleStatus.Report("quests", "Quests", Game.Quests != null ? (Game.Quests.LoadProblems.Count == 0 ? ModuleState.Loaded : ModuleState.Partial) : ModuleState.Failed,
                Game.Quests == null ? "The quest book did not start." : (Game.Quests.LoadProblems.Count == 0 ? "" : Game.Quests.LoadProblems.Count + " quest file problem(s); F2 > Quests lists them."));
            if (Game.Quests != null) ModuleStatus.SetCount("quests", Game.Quests.Quests.Count, "quests loaded");
        }

        private T AddSystem<T>(string name) where T : Component
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_systems, false);
            return go.AddComponent<T>();
        }

        /// <summary>Runs a nested coroutine (like Unity does) but survives exceptions, so a broken step can't freeze loading.</summary>
        private IEnumerator Run(IEnumerator root, string stage)
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                bool moved;
                try { moved = top.MoveNext(); }
                catch (Exception e)
                {
                    Fail(stage, e);
                    yield break;
                }
                if (!moved)
                {
                    stack.Pop();
                    continue;
                }
                IEnumerator sub = top.Current as IEnumerator;
                if (sub != null)
                {
                    stack.Push(sub);
                    continue;
                }
                yield return top.Current;
            }
        }

        private IEnumerator Boot()
        {
            yield return null;
            SetProgress(0.01f, "Synthesizing sound effects");
            Step("Audio", () => Game.Audio = AddSystem<AudioManager>("Audio"));
            if (Game.Audio != null) yield return Run(Game.Audio.BuildClips(p => SetProgress(0.01f + p * 0.07f, "Synthesizing sound effects")), "Audio clips");
            StepModule("sounds_04e", "Underheart sounds", Underheart04ESounds.Init);

            SetProgress(0.09f, "Planning the streets of Las Veyra");
            yield return null;
            CityLayout layout = null;
            try
            {
                layout = new CityLayout(Game.Seed);
                layout.Generate();
            }
            catch (Exception e)
            {
                Fail("City layout", e);
                layout = null;
            }
            if (layout == null)
            {
                _fatal = true;
                _status = "Las Veyra could not be generated. Check the Console for the error and send it along.";
                yield break;
            }
            Game.Layout = layout;
            Step("Inhabited city parcel plan", () => { Game.Inhabited = AddSystem<InhabitedCitySystem>("Inhabited City 04C"); Game.Inhabited.Prepare(layout); });
            if (_revision04O != CityRevision04O.Legacy04N)
            {
                bool accepted = false, returned = false;
                Step("04O parcel revision", () => { accepted = layout.ApplyProfile04O(_revision04O, Game.Inhabited != null ? Game.Inhabited.Venues : null); returned = true; });
                // A clean refusal changed nothing, so the default boot may keep the original city (UseOriginalCityH8S).
                if (!accepted && !(returned && UseOriginalCityH8S(layout))) { _fatal = true; _status = "The preview could not protect its service parcels. Your saved files are unchanged. Restart for the legacy city."; yield break; }
            }
            Step("Active world identity", () => Log.Info(WorldIdentity04OB.Describe()));
            // 04E: the Underheart binds its entrances to parcels and registers the ground openings before the city is built.
            StepModule("underheart", "Underheart world", () => { Game.Underheart = AddSystem<UnderheartWorld>("Underheart 04E"); Game.Underheart.Prepare(layout); });

            SetProgress(0.13f, "Drawing the map");
            yield return null;
            Step("Map texture", () => Game.MapTexture = MapTexture.Build(layout));

            CityBuilder builder = null;
            try { builder = new CityBuilder(layout, _world); }
            catch (Exception e) { Fail("City builder", e); }
            if (builder != null)
            {
                Game.City = builder;
                yield return Run(builder.Build((p, s) => SetProgress(0.15f + p * 0.62f, s)), "City build");
            }

            SetProgress(0.78f, "Starting the city systems");
            yield return null;
            Dictionary<int, MeshRenderer> signals = builder != null ? builder.SignalRenderers : new Dictionary<int, MeshRenderer>();
            List<PropSpot> breakables = builder != null ? builder.Breakables : new List<PropSpot>();
            Step("Traffic signals", () => { Game.Signals = AddSystem<TrafficLights>("Traffic Signals"); Game.Signals.Init(layout, signals); });
            Step("Street lights", () => { Game.Lights = AddSystem<LightPool>("Street Lights"); Game.Lights.Init(layout); });
            Step("Breakable props", () => { Game.Props = AddSystem<PropStreamer>("Breakable Props"); Game.Props.Init(breakables); });
            Step("Atmosphere", () => { Game.Atmo = AddSystem<Atmosphere>("Atmosphere"); Game.Atmo.Init(); });
            Step("Asterfall art lighting", () =>
            {
                if (Game.Atmo == null || DistrictArchitecture.BuildingCount == 0) return;
                DistrictAtmosphere district = AddSystem<DistrictAtmosphere>("Asterfall Light and Haze");
                district.Init(DistrictArchitecture.LightAnchors());
                Game.Atmo.ArtDistrict = district;
            });
            Step("Inner World", () => { Game.Inner = AddSystem<InnerWorld>("Inner World"); Game.Inner.Init(); });
            Step("Effects", () => { Game.Fx = AddSystem<Fx>("Effects"); Game.Fx.Init(); });
            Step("Explosions", () => Game.Explosions = AddSystem<Explosions>("Explosions"));
            Step("Radio", () => { Game.Radio = AddSystem<RadioSystem>("Radio"); Game.Radio.Init(); });
            yield return null;

            SetProgress(0.84f, "Waking up the citizens");
            Step("Stats", () => Game.Stats = AddSystem<Stats>("Stats"));
            Step("Economy", () => Game.Economy = AddSystem<Economy>("Economy"));
            Step("GPS", () => Game.Gps = AddSystem<Gps>("GPS"));
            Step("Wanted system", () => Game.Wanted = AddSystem<WantedSystem>("Wanted System"));
            Step("Police", () => Game.Police = AddSystem<PoliceManager>("Police"));
            Step("Traffic", () => Game.Traffic = AddSystem<TrafficManager>("Traffic"));
            Step("Pedestrians", () => { Game.Peds = AddSystem<PedManager>("Pedestrians"); Game.Peds.Init(layout); });
            Step("Pickups", () => { Game.Pickups = AddSystem<Pickups>("Pickups"); Game.Pickups.Init(layout); });
            Step("Collectibles", () => Game.Collectibles = AddSystem<Collectibles>("Collectibles"));
            Step("Abilities", () => Game.Abilities = AddSystem<Abilities>("Crystal Powers"));
            Step("Combat", CombatDirector.Ensure);
            Step("Resonance", ResonanceSystem.Ensure);
            Step("Cover tactics", () => { Game.Cover = AddSystem<CoverDirector>("Cover Tactics"); Game.Cover.Init(); });
            Step("Progression", () => { Game.Progress = AddSystem<Progression>("Progression"); Game.Progress.Init(layout); });
            Step("Mythic gear", () => { Game.Gear = AddSystem<GearSystem>("Mythic Gear"); Game.Gear.Init(); });
            Step("Materials", () => { Game.Materials = AddSystem<MaterialStore>("Materials"); Game.Materials.Init(); });
            Step("Bestiary", () => { Game.Compendium = AddSystem<Compendium>("Bestiary"); Game.Compendium.Init(); });
            Step("Skill trees", () => { Game.Skills = AddSystem<SkillTree>("Skill Trees"); Game.Skills.Init(); });
            Step("Factions", () => { Game.Factions = AddSystem<FactionSystem>("Factions"); Game.Factions.Init(); });
            Step("Forge", () => { Game.Crafting = AddSystem<CraftingSystem>("Forge"); Game.Crafting.Init(layout); });
            Step("Loot armory", () => { Game.Loot = AddSystem<LootArmory>("Loot Armory"); Game.Loot.Init(); });
            Step("Travel powers", () => { Game.Travel = AddSystem<TravelPowers>("Travel Powers"); Game.Travel.Init(); });
            Step("Loot world", () => { Game.LootWorld = AddSystem<LootWorld>("Loot World"); Game.LootWorld.Init(layout); });
            Step("Gadgets", () => { Game.Gadgets = AddSystem<GadgetSystem>("Gadgets"); Game.Gadgets.Init(); });
            StepModule("builds", "Builds, wards and rewards", () => { Game.Builds = AddSystem<BuildSystem>("Builds 04E"); Game.Builds.Init(); });
            StepModule("garage", "Personal garage", () => { Game.Garage = AddSystem<PersonalGarage>("Personal Garage 04E"); Game.Garage.Init(); });
            StepModule("fusion", "Creature-vehicle fusion", () => { Game.Fusion = AddSystem<FusionSystem>("Fusion 04E"); Game.Fusion.Init(); });
            Step("Crystal weapons", CrystalShots.Ensure);
            yield return null;

            SetProgress(0.92f, "Writing the story");
            Step("Missions", () => { Game.Missions = AddSystem<MissionManager>("Missions"); Game.Missions.Init(); });
            // One game: in revision 10 the story race runs on that world's real roads (StoryMissions.OneGameH8S).
            Step("Story routes for this world", () => StoryMissions.FitWorldH8S(Game.Missions, layout));
            Step("Activities", () => { Game.Activities = AddSystem<Activities>("Side Activities"); Game.Activities.Init(layout); });
            Step("World services", () => { Game.Services = AddSystem<WorldServices>("World Services"); Game.Services.Init(layout); });
            Step("Creatures", () => { Game.Creatures = AddSystem<CreatureDirector>("Creatures"); Game.Creatures.Init(layout); });
            Step("Starter creature habitat", () =>
            {
                if (Game.Creatures != null) AddSystem<StarterHabitat>("Starter Creature Habitat").Init(layout);
            });
            StepModule("bonds", "Bonded companions", () => { Game.Bonds = AddSystem<BondSystem>("Bonds 04E"); Game.Bonds.Init(); });
            StepModule("ecology_04e", "Underground ecology", () => { Game.Ecology04E = AddSystem<UnderheartEcology>("Underheart Ecology 04E"); Game.Ecology04E.Init(); });
            StepModule("bosses_04e", "Underheart bosses", () => { Game.UnderBosses = AddSystem<UnderheartBosses>("Underheart Bosses 04E"); Game.UnderBosses.Init(); });
            Step("Quests", () => { Game.Quests = AddSystem<QuestBook>("Quests"); Game.Quests.Init(layout); });
            Step("World events", () => { Game.Events = AddSystem<WorldEvents>("World Events"); Game.Events.Init(layout); });
            Step("Crystal arsenal", () => { Game.Arsenal = AddSystem<CrystalArsenal>("Crystal Arsenal 04D"); Game.Arsenal.Init(layout); });
            StepModule("crystal_combat_04f", "Crystal combat and Resonance Charge (04F)", () => { Game.CrystalCombat = AddSystem<CrystalCombat04F>("Crystal Combat 04F"); Game.CrystalCombat.Init(); });
            Step("Crystal encounters", () => { Game.Encounters = AddSystem<CrystalEncounters>("Crystal Encounters 04D"); Game.Encounters.Init(layout); });
            Step("Crystal city discovery", () => AddSystem<CityDiscoverySystem>("Crystal City Discovery").Init(layout));
            Step("Crystal strongholds", () => { Game.Strongholds = AddSystem<StrongholdSystem>("Crystal Strongholds"); Game.Strongholds.Init(layout); });
            if (Game.Strongholds != null) yield return Game.Strongholds.BuildWorld();
            if (Game.Strongholds != null) yield return VisualRebirth04ANeighborhoods.Build(layout, Game.Strongholds);
            Step("Living crystal territories", () => { Game.Territories = AddSystem<TerritorySystem>("Living Territories 04B"); Game.Territories.Init(layout); });
            if (Game.Territories != null) yield return Game.Territories.BuildWorld();
            if (Game.Underheart != null) yield return Run(Game.Underheart.BuildWorld(), "Underheart build");
            Step("Inhabited city services", () => { if (Game.Inhabited != null) Game.Inhabited.ActivateWorld(); });
            Step("City interiors", () => { Game.Interiors = AddSystem<CityInteriors>("City Interiors"); Game.Interiors.Init(layout); Game.Interiors.ActivateWorld(); });
            StepModule("arrival_04e", "Arrival neighborhood", () => ArrivalScene04E.Build(_world));
            StepModule("campaign", "Campaign guidance and cast", () => { Game.Campaign = AddSystem<CampaignDirector>("Campaign 04E"); Game.Campaign.Init(layout); });
            // H8S live world art: the modeled modules and props placed live in revision 10 (visual only; skips safely when unprepared).
            StepModule(LiveWorldArtH8S.ModuleId, LiveWorldArtH8S.ModuleTitle, () => AddSystem<LiveWorldArtH8S>("Live World Art H8S").Init(layout));
            Step("Cheats", () => Game.Cheats = AddSystem<Cheats>("Cheats"));
            yield return null;

            SetProgress(0.97f, "Tuning the HUD");
            Step("Minimap", () => { Game.Minimap = AddSystem<Minimap>("Minimap"); if (Game.MapTexture != null) Game.Minimap.Init(Game.MapTexture); });
            Step("HUD", () => Game.Hud = AddSystem<Hud>("HUD"));
            Step("Menus", () => Game.Menus = AddSystem<MenuSystem>("Menus"));
            Step("Phone", () => Game.Phone = AddSystem<Phone>("Phone"));
            Step("Module status", ReportLegacyModules);
            MenuPages.Register(new MenuPage { Title = "WORLD", Order = 108, Draw = DrawWorld04O });
            SetProgress(1f, "Las Veyra is ready");
            Log.Info("Boot finished in " + (Time.realtimeSinceStartup - _bootStart).ToString("0.0") + " s with " + _errors.Count + " problem(s).");
            yield return new WaitForSecondsRealtime(0.35f);
            _booting = false;
            if (_resumeSession04O)
            {
                SaveData resume = _resume04O; _resume04O = null; _resumeSession04O = false;
                if (_resumeSkip04O && Game.Missions != null) Game.Missions.RequestPrologueSkip();
                _resumeSkip04O = false;
                yield return BeginSession(resume, _resumeFree04O);
            }
            else EnterTitle();
        }

        private void EnterTitle()
        {
            Game.State = GameState.Title;
            if (Game.Atmo != null)
            {
                Game.Atmo.SetWeather(Weather.Clear, true);
                Game.Atmo.SetTime(16.4f); // sunny afternoon flyover
            }
            if (Game.Menus != null) Game.Menus.ShowTitle();
            if (Game.Hud != null) Game.Hud.FadeTo(0f, 1.2f);
            if (Game.Radio != null)
            {
                Game.Radio.PlayerInVehicle = true;
                if (Game.Radio.Station < 0) Game.Radio.Tune(0);
            }
            GInput.SetCursorLocked(false);
        }

        // ================================================================== sessions

        public void NewGame(bool freeRoam = false)
        {
            if (_transition || _booting) return;
            // One game (owner decision 2026-10-23): the story and Free Roam both start in the newest world (revision 10).
            if (NeedsOneGameReloadH8S)
            { CityRebirthBoot04O.Request(OneGameH8S.World, null, freeRoam); return; }
            StartCoroutine(BeginSession(null, freeRoam));
        }

        public void ContinueGame()
        {
            if (_transition || _booting) return;
            // Continue reads the newest world's bank even when an older save's world is loaded (it reloads into world 10).
            int bank = ContinueBankH8S;
            int slot = MostRecentSlotH8S(bank);
            if (slot < 0)
            {
                NewGame();
                return;
            }
            if (!LoadGame(slot, bank)) _errors.Add("The most recent save (slot " + slot + ") could not be read.");
        }

        public bool LoadGame(int slot) { return LoadGame(slot, SaveSystem.ActiveBankRevision04OB); }

        public bool LoadGame(int slot, int bankRevision)
        {
            if (_transition || _booting) return false;
            SaveData d = SaveSystem.Read(slot, bankRevision);
            if (d == null) return false;
            if (!CityRebirthRules04O.Known(d.WorldRevision04O))
            {
                if (Game.Hud != null) Game.Hud.Notify("This save uses a newer world layout. It was not loaded or changed.", 7f);
                return false;
            }
            if (Game.Layout != null && d.WorldRevision04O != (int)Game.Layout.Revision04O)
                return CityRebirthBoot04O.Request((CityRevision04O)d.WorldRevision04O, d, d.FreeRoam);
            StartCoroutine(BeginSession(d, d.FreeRoam));
            return true;
        }

        public void ReturnToTitle()
        {
            if (_transition || _booting) return;
            StartCoroutine(TitleSequence());
        }

        public void Quit()
        {
            try { Settings.Save(); }
            catch (Exception e) { Log.Exception("Settings save", e); }
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator BeginSession(SaveData data, bool freeRoam)
        {
            _transition = true;
            if (Game.Menus != null) Game.Menus.HideAll();
            if (Game.Phone != null) Game.Phone.Close();
            GInput.GameplayBlocked = true;
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.35f);
            yield return new WaitForSecondsRealtime(0.4f);
            Game.State = GameState.Loading;
            ResetWorldState();
            yield return null;

            FreeRoam = data != null ? data.FreeRoam : freeRoam;
            Vector3 pos;
            float yaw;
            bool underheartMoved = false;
            if (data != null)
            {
                pos = new Vector3(data.PosX, data.PosY, data.PosZ);
                yaw = data.Yaw;
                Vector3 venueReturn; float venueYaw;
                if (Game.Inhabited != null && Game.Inhabited.TryResolveSavedExterior(data, out venueReturn, out venueYaw))
                { pos = venueReturn; yaw = venueYaw; }
                // 04E: a save made on the surface by an earlier 04E build could hold an underground rest point as its
                // safehouse. It was made on the surface, so it resumes at the surface safehouse (or the home spawn).
                if (SafehouseRules04E.SurfaceSaveHeldRestPoint(data.AtSafehouse, pos, data.Underheart != null ? data.Underheart.SavedRegion : null))
                {
                    Vector3 surface = SafehouseRules04E.LoadedSurfaceSafehouse(Vector3.zero, data.HasSurfaceSafehouse, new Vector3(data.SurfaceSafeX, data.SurfaceSafeY, data.SurfaceSafeZ));
                    if (surface != Vector3.zero) pos = surface;
                    else pos = HomeSpawn(out yaw);
                    Log.Info("Save resume: a surface save held an underground rest point; resuming at the surface safehouse.");
                }
                // 04E: an underground save resumes at a verified Underheart anchor (or the world moves it to a disclosed fallback).
                bool underground = false;
                if (Game.Underheart != null)
                {
                    Vector3 before = pos;
                    try { underground = Game.Underheart.TryResolveSavedPosition(data, ref pos, ref yaw); }
                    catch (Exception e) { Log.Exception("Underheart saved position", e); underground = false; }
                    // Remembered for the safehouse: a resume point the Underheart chose is not the saved safehouse.
                    underheartMoved = underground || (pos - before).sqrMagnitude > 1e-4f;
                }
                if (!MathUtil.IsFinite(pos) || (!underground && Game.Layout != null && Game.Layout.IsInsideBuilding(pos, 0.2f)))
                {
                    pos = HomeSpawn(out yaw);
                    underheartMoved = false;
                }
            }
            else if (FreeRoam)
            {
                pos = HomeSpawn(out yaw);
                SetSafehouse(pos);
            }
            else
            {
                MissionDef first = Game.Missions != null && Game.Missions.Story.Count > 0 ? Game.Missions.Story[0] : null;
                pos = first != null && first.StartPosition != null ? first.StartPosition() : HomeSpawn(out yaw);
                yaw = 0f;
            }
            PlacePlayer(pos, yaw);
            if (Game.Player != null)
            {
                Step("Apply game state", () =>
                {
                    if (data != null) ApplySave(data, pos, underheartMoved);
                    else ApplyNewGame(FreeRoam);
                });
            }
            if (Game.Activities != null) Game.Activities.OnLoaded();
            if (StarterHabitat.Instance != null && Game.Player != null)
                Step("Populate starter habitat", () => StarterHabitat.Instance.BeginSession(Game.PlayerPosition));
            if (Game.Radio != null) Game.Radio.OnExitVehicle();
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);

            Game.State = GameState.Playing;
            GInput.GameplayBlocked = false;
            GInput.SetCursorLocked(true);
            if (Game.Cam != null) Game.Cam.SnapBehindPlayer();
            _transition = false;
            if (data == null && !FreeRoam && Game.Missions != null && Game.Missions.Story.Count > 0)
            {
                // The first mission fades in by itself with its opening cinematic.
                Game.Missions.StartMission(Game.Missions.Story[0]);
            }
            else if (Game.Hud != null)
            {
                Game.Hud.FadeTo(0f, 1.2f);
                if (data != null)
                {
                    MissionDef next = Game.Missions != null ? Game.Missions.NextStory : null;
                    Game.Hud.Banner("WELCOME BACK", FreeRoam ? "Free roam. Las Veyra is all yours." : (next != null ? "Next: " + next.Title + " (" + next.Contact + ", yellow marker)" : "Story complete. The city is yours."), UIKit.Cyan, 4.5f);
                }
                else Game.Hud.Banner("FREE ROAM", "Every side activity is open. Press M for the map, P for the phone, F1 for controls.", UIKit.Cyan, 6f);
            }
            if (Game.CrystalCombat != null) Step("Crystal combat session", () => Game.CrystalCombat.BeginSession(data != null, FreeRoam));
            if (Game.Campaign != null) Step("Campaign session", () => Game.Campaign.OnSessionBegan(data != null, FreeRoam));
        }

        private IEnumerator TitleSequence()
        {
            _transition = true;
            if (Game.Menus != null) Game.Menus.HideAll();
            if (Game.Phone != null) Game.Phone.Close();
            GInput.GameplayBlocked = true;
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.4f);
            yield return new WaitForSecondsRealtime(0.45f);
            Game.State = GameState.Loading;
            ResetWorldState();
            if (Game.Player != null)
            {
                Destroy(Game.Player.gameObject);
                Game.Player = null;
            }
            yield return null;
            _transition = false;
            EnterTitle();
        }

        /// <summary>Clears everything a session touched so New Game / Load / Title start from a clean city.</summary>
        private void ResetWorldState()
        {
            StarterSafety.ResetSession();
            WorldContext.ResetSession();
            // New game, load and title always start in the Outer City (the Veil state is never saved).
            if (Game.Inner != null) Step("Reset Inner World", () => Game.Inner.Shift(false, true));
            GameTime.ReleaseAll();
            GameTime.SetPaused(false);
            if (Game.Audio != null) Game.Audio.SetSlowMotion(false);
            PlayerController.MoonJump = false;
            GodMode = false;
            FreeRoam = false;
            LastSafehouse = Vector3.zero;
            LastSurfaceSafehouse = Vector3.zero;
            if (Game.Abilities != null) Step("Reset abilities", ResetAbilities);
            if (Game.Progress != null) Step("Reset progression", () => Game.Progress.ResetProgress());
            if (Game.Gear != null) Step("Reset gear", () => Game.Gear.ResetAll());
            Step("Reset world systems", WorldSystems.ResetAll);
            Step("Clear crystal shots", () => CrystalShots.ClearAll("session"));
            Step("Clear NPC crystal shots", NpcCrystal04FProjectile.ClearAll);
            if (Game.Missions != null) Step("Reset missions", () => Game.Missions.ResetProgress());
            if (Game.Activities != null) Step("Reset activities", () => Game.Activities.ResetAll());
            if (Game.Wanted != null)
            {
                Game.Wanted.Suppressed = false;
                Step("Reset wanted", () => Game.Wanted.ClearAll());
            }
            if (Game.Police != null) Step("Reset police", () => Game.Police.ClearAll());
            Step("Clear vehicles", ClearVehicles);
            if (Game.Peds != null) Step("Clear pedestrians", () => Game.Peds.ClearAround(Vector3.zero, 100000f));
            if (Game.Pickups != null) Step("Clear pickups", () => Game.Pickups.ClearDropped());
            if (Game.Services != null) Step("Reset services", () => Game.Services.ResetAll());
            if (Game.Economy != null)
            {
                Game.Economy.Money = 1500;
                Game.Economy.OwnedProperties.Clear();
                Game.Economy.PendingIncome = 0;
                Game.Economy.SnapDisplay();
            }
            if (Game.Stats != null)
            {
                Array.Clear(Game.Stats.Values, 0, Game.Stats.Values.Length);
                Game.Stats.Unlocked.Clear();
            }
            if (Game.Collectibles != null)
            {
                Game.Collectibles.Chips.Clear();
                Game.Collectibles.Shards.Clear();
                Game.Collectibles.Stones.Clear();
                Game.Collectibles.Relics.Clear();
                Game.Collectibles.Jumps.Clear();
            }
            Gangs.ResetStatics();
            if (Game.Menus != null) Game.Menus.CurrentOutfit = 0;
            if (Game.Gps != null)
            {
                Game.Gps.ClearWaypoint();
                Game.Gps.ClearMissionTarget();
            }
            if (Game.Hud != null)
            {
                Game.Hud.ClearObjective();
                Game.Hud.ClearTimer();
                Game.Hud.ClearCounter();
                Game.Hud.ClearBigMessage();
                Game.Hud.ClearSubtitle();
            }
            if (Game.Cam != null && Game.Cam.InCinematic) Game.Cam.ClearCinematic();
            if (Game.Atmo != null)
            {
                Game.Atmo.Day = 1;
                Game.Atmo.SetWeather(Weather.Clear, true);
                Game.Atmo.SetTime(19.5f);
            }
        }

        private static void ResetAbilities()
        {
            Abilities a = Game.Abilities;
            if (a.ThirdEyeActive) a.SetThirdEye(false);
            a.ThirdEyeUnlocked = false;
            for (int i = 0; i < a.Unlocked.Length; i++) a.Unlocked[i] = false;
            a.MaxEnergy = 100f;
            a.BaseMaxEnergy = 100f;
            a.Energy = 60f;
            a.Selected = Abilities.SunboltIndex;
            a.Law = "";
        }

        private void ClearVehicles()
        {
            PlayerController p = Game.Player;
            if (p != null && p.InVehicle) p.ExitVehicleImmediate(false);
            if (Game.Traffic != null) Game.Traffic.ClearAll();
            Vehicle[] all = Vehicle.All.ToArray();
            foreach (Vehicle v in all)
            {
                if (v == null) continue;
                foreach (CharacterBase c in v.Occupants) if (c != null && !c.IsPlayer) Destroy(c.gameObject);
                Destroy(v.gameObject);
            }
        }

        private void ApplyNewGame(bool freeRoam)
        {
            PlayerController p = Game.Player;
            WeaponInventory inv = p.Weapons.Inventory;
            inv.Clear();
            p.Health.MaxHealth = 200f;
            p.Health.MaxArmor = 100f;
            p.Health.Revive(p.Health.MaxHealth);
            p.Health.Armor = 0f;
            p.Health.Invulnerable = false;
            p.Rig.ChangeLook(MenuSystem.Outfit(0));
            if (Game.Progress != null) Game.Progress.ApplyToPlayer(true);
            if (p.Combat != null)
            {
                p.Combat.ResetState();
                p.Combat.RefillTonics();
            }
            if (Game.Economy != null)
            {
                Game.Economy.Money = freeRoam ? 5000 : 1500;
                Game.Economy.SnapDisplay();
            }
            if (freeRoam)
            {
                inv.Give(WeaponId.SluggerBat, 0);
                inv.Give(WeaponId.VectorSidearm, 72);
                inv.Give(WeaponId.VoltKatana, 0);
                if (Game.Gear != null) Game.Gear.GrantStarterKit();
                if (Game.Abilities != null)
                {
                    Game.Abilities.ThirdEyeUnlocked = true;
                    Game.Abilities.Unlock(Abilities.SunboltIndex);
                    Game.Abilities.AddEnergy(999f);
                }
                if (Game.Atmo != null) Game.Atmo.SetTime(15.5f);
            }
            else if (Game.Atmo != null) Game.Atmo.SetTime(20.5f);
            p.Weapons.RefreshModel();
        }

        /// <summary>Applies a loaded save. underheartMoved: the Underheart replaced the saved position with its resume point.</summary>
        private void ApplySave(SaveData d, Vector3 pos, bool underheartMoved)
        {
            PlayerController p = Game.Player;
            if (Game.Atmo != null)
            {
                Game.Atmo.Day = Mathf.Max(1, d.Day);
                Game.Atmo.SetWeather(Enum.IsDefined(typeof(Weather), d.Weather) ? (Weather)d.Weather : Weather.Clear, true);
                Game.Atmo.SetTime(d.Hours);
            }
            if (Game.Economy != null)
            {
                Game.Economy.Money = Mathf.Max(0, d.Money);
                Game.Economy.OwnedProperties.Clear();
                foreach (string s in d.Properties) if (Economy.FindProperty(s) != null) Game.Economy.OwnedProperties.Add(s);
                Game.Economy.SnapDisplay();
            }
            if (Game.Activities != null)
            {
                Game.Activities.Completed.Clear();
                foreach (string s in d.Activities) if (!string.IsNullOrEmpty(s)) Game.Activities.Completed.Add(s);
            }
            bool medic = Game.Activities != null && Game.Activities.Completed.Contains(Progression.ParamedicMilestone);
            bool vigilante = Game.Activities != null && Game.Activities.Completed.Contains("job_vigilante_10");
            p.Health.MaxHealth = 200f + (medic ? Progression.ParamedicHealthBonus : 0f);
            p.Health.MaxArmor = vigilante ? 150f : 100f;
            p.Health.Revive(Mathf.Clamp(d.Health, 1f, p.Health.MaxHealth));
            p.Health.Armor = Mathf.Clamp(d.Armor, 0f, p.Health.MaxArmor);

            WeaponInventory inv = p.Weapons.Inventory;
            inv.Clear();
            foreach (int w in d.Weapons) if (w > 0 && w < (int)WeaponId.Count) inv.Give((WeaponId)w, 0);
            for (int i = 0; i < inv.Clip.Length && i < d.Clips.Count; i++) inv.Clip[i] = Mathf.Max(0, d.Clips[i]);
            for (int i = 0; i < inv.Ammo.Length && i < d.Ammo.Count; i++) inv.Ammo[i] = Mathf.Max(0, d.Ammo[i]);
            if (d.CurrentWeapon >= 0 && d.CurrentWeapon < (int)WeaponId.Count && inv.Has((WeaponId)d.CurrentWeapon)) inv.Select((WeaponId)d.CurrentWeapon);

            if (Game.Stats != null)
            {
                for (int i = 0; i < Game.Stats.Values.Length && i < d.Stats.Count; i++) Game.Stats.Values[i] = d.Stats[i];
                foreach (string a in d.Achievements) if (!string.IsNullOrEmpty(a)) Game.Stats.Unlocked.Add(a);
            }
            if (Game.Missions != null)
            {
                Game.Missions.StoryProgress = Mathf.Clamp(d.StoryProgress, 0, Game.Missions.Story.Count);
                foreach (string f in d.Flags) if (!string.IsNullOrEmpty(f)) Game.Missions.Flags.Add(f);
            }
            if (Game.Collectibles != null)
            {
                Game.Collectibles.Chips.UnionWith(d.Chips);
                Game.Collectibles.Shards.UnionWith(d.Shards);
                Game.Collectibles.Stones.UnionWith(d.Stones);
                Game.Collectibles.Relics.UnionWith(d.Relics);
                Game.Collectibles.Jumps.UnionWith(d.Jumps);
            }
            Abilities ab = Game.Abilities;
            if (ab != null)
            {
                ab.BaseMaxEnergy = d.MaxEnergy > 0f ? d.MaxEnergy : 100f;
                ab.MaxEnergy = ab.BaseMaxEnergy;
                ab.Energy = Mathf.Clamp(d.Energy, 0f, ab.MaxEnergy);
                ab.ThirdEyeUnlocked = d.ThirdEye;
                for (int i = 0; i < ab.Unlocked.Length && i < d.Spells.Count; i++) ab.Unlocked[i] = d.Spells[i];
                ab.Selected = Mathf.Clamp(d.SelectedSpell, 0, Abilities.Spells.Length - 1);
                ab.Law = d.Law ?? "";
            }
            for (int i = 0; i < Gangs.Heat.Length && i < d.GangHeat.Count; i++) Gangs.Heat[i] = Mathf.Clamp01(d.GangHeat[i]);
            // The Law of Power ended every gang grudge for good.
            if (Game.Missions != null && (ab == null || ab.Law != "Power")) Gangs.ApplyStory(Game.Missions.Flags);
            int outfit = Mathf.Clamp(d.Outfit, 0, 7);
            if (Game.Menus != null) Game.Menus.CurrentOutfit = outfit;
            p.Rig.ChangeLook(MenuSystem.Outfit(outfit));
            p.Weapons.RefreshModel();
            // 04E: a Free Roam save records the Lofts (the safehouse) as its position while the Underheart block says the
            // player was underground. The resume point the Underheart picks is not a safehouse: keep the saved one, or the
            // next surface save would resume underground for good.
            if (d.AtSafehouse) LastSafehouse = SafehouseRules04E.LoadedSafehouse(new Vector3(d.PosX, d.PosY, d.PosZ), pos, underheartMoved);
            // The surface safehouse survives an underground save (appended fields; older saves fall back to a surface
            // safehouse they hold, else zero, and surface saves then use the home spawn).
            LastSurfaceSafehouse = SafehouseRules04E.LoadedSurfaceSafehouse(LastSafehouse, d.HasSurfaceSafehouse, new Vector3(d.SurfaceSafeX, d.SurfaceSafeY, d.SurfaceSafeZ));
            if (Game.Gear != null) Game.Gear.Restore(d);
            if (Game.Progress != null) Game.Progress.Restore(d);
            WorldSystems.RestoreAll(d);
            // Max health and energy are only final now (levels, gear, skills, Forge upgrades, the paramedic milestone), and
            // the steps above rescaled health by fraction: put back exactly what the save held.
            p.Health.Revive(Mathf.Clamp(d.Health, 1f, p.Health.MaxHealth));
            if (ab != null) ab.Energy = Mathf.Clamp(d.Energy, 0f, ab.MaxEnergy);
        }

        // ================================================================== spawning helpers

        /// <summary>Ground under a point (works for street level and raised spots like the canyon).</summary>
        public static Vector3 FindGround(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 9f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            // 04E: a point below the street looks for its own floor; the high fallback would snap it to the street above.
            // That holds inside an Underheart cell (a longer drop is allowed) and also just outside one, which the
            // probe does not count as underground (a short drop only, then the point itself).
            if (!GroundRules04E.MayUseHighFallback(p.y))
            {
                float reach = WorldBounds.IsUnderground(p) ? 40f : 12f;
                if (Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out hit, reach, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
                return p;
            }
            if (Physics.Raycast(new Vector3(p.x, p.y + 120f, p.z), Vector3.down, out hit, 300f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        /// <summary>A spot on the sidewalk just outside a building's door.</summary>
        public static Vector3 SpawnOutside(Poi poi)
        {
            Vector3 f = poi.Facing.sqrMagnitude > 0.01f ? poi.Facing.normalized : Vector3.forward;
            return FindGround(poi.Position + f * 3.5f);
        }

        private Vector3 HomeSpawn(out float yaw)
        {
            yaw = 0f;
            CityLayout L = Game.Layout;
            if (L == null) return Vector3.up;
            Poi home = L.FindPoi("Veyra Lofts");
            if (home == null) home = L.NearestPoi(Vector3.zero, PoiType.Safehouse);
            if (home != null)
            {
                yaw = MathUtil.YawOf(home.Facing);
                return SpawnOutside(home);
            }
            return FindGround(MissionManager.RoadNear(Vector3.zero));
        }

        private void PlacePlayer(Vector3 pos, float yaw)
        {
            Vector3 ground = FindGround(pos) + Vector3.up * 0.05f;
            try
            {
                if (Game.Player == null) Game.Player = PlayerController.Spawn(ground, yaw);
                else Game.Player.Respawn(ground, yaw);
            }
            catch (Exception e)
            {
                Fail("Player spawn", e);
                return;
            }
            Game.Player.ControlsEnabled = true;
            Game.Player.Health.Invulnerable = GodMode;
            if (Game.Cam != null)
            {
                Game.Cam.Yaw = yaw;
                Game.Cam.SnapBehindPlayer();
            }
        }

        public void MarkSafehouse(Poi poi)
        {
            if (poi == null) return;
            SetSafehouse(SpawnOutside(poi));
        }

        /// <summary>
        /// Records the safehouse saves return to. A point on the surface also becomes the surface safehouse; an
        /// underground rest point changes only LastSafehouse, so it is the resume point for saves made underground only
        /// (SafehouseRules04E). Zero or non-finite points are ignored.
        /// </summary>
        public void SetSafehouse(Vector3 point)
        {
            if (point == Vector3.zero || !MathUtil.IsFinite(point)) return;
            LastSafehouse = point;
            if (SafehouseRules04E.IsSurface(point)) LastSurfaceSafehouse = point;
        }

        /// <summary>The surface safehouse a surface save stores: the last one chosen, else the home spawn (zero without a city).</summary>
        public Vector3 SurfaceSafehouseOrHome()
        {
            if (LastSurfaceSafehouse != Vector3.zero) return LastSurfaceSafehouse;
            if (Game.Layout == null) return Vector3.zero;
            float yaw;
            return HomeSpawn(out yaw);
        }

        public void TeleportPlayer(Vector3 pos)
        {
            PlayerController p = Game.Player;
            if (p == null) return;
            Vector3 g = FindGround(pos);
            if (p.InVehicle)
            {
                Vector3 road = FindGround(MissionManager.RoadNear(g));
                p.Teleport(road + Vector3.up * 0.2f, MissionManager.RoadYawNear(road));
            }
            else
            {
                float yaw = Game.Cam != null ? Game.Cam.Yaw : p.transform.eulerAngles.y;
                p.Teleport(g + Vector3.up * 0.05f, yaw);
            }
            if (Game.Traffic != null) Game.Traffic.ClearAround(g, 18f);
            if (Game.Cam != null) Game.Cam.SnapBehindPlayer();
        }

        /// <summary>Where to put a player who fell out of the world.</summary>
        public Vector3 SafeRespawnPoint(Vector3 from)
        {
            // 04E: a player who fell out of an underground cell goes back to an underground rest point, not a street.
            if (Game.Underheart != null && WorldContext.HasGrounded && WorldContext.LastGroundedRegion == WorldRegion.Underworld)
            {
                Vector3 rest; float restYaw;
                try
                {
                    if (Game.Underheart.TryNearestRestPoint(WorldContext.LastGroundedPosition, out rest, out restYaw)) return rest + Vector3.up * 0.1f;
                }
                catch (Exception e) { Log.Exception("Underheart rest point", e); }
            }
            CityLayout L = Game.Layout;
            if (L != null)
            {
                int n = L.NearestWalkNode(from, 500f);
                if (n >= 0) return FindGround(L.WalkNodes[n].Pos) + Vector3.up * 0.1f;
                return FindGround(MissionManager.RoadNear(from)) + Vector3.up * 0.1f;
            }
            return LastSafehouse != Vector3.zero ? LastSafehouse : Vector3.up * 2f;
        }

        private static Poi NearestOf(Vector3 p, PoiType a, PoiType b)
        {
            CityLayout L = Game.Layout;
            if (L == null) return null;
            Poi best = null;
            float bd = float.MaxValue;
            foreach (Poi x in L.Pois)
            {
                if (x.Type != a && x.Type != b) continue;
                float d = MathUtil.FlatDistance(x.Position, p);
                if (d < bd)
                {
                    bd = d;
                    best = x;
                }
            }
            return best;
        }

        // ================================================================== wasted / busted

        public void OnPlayerWasted(DamageInfo info)
        {
            if (_transition || _booting) return;
            if (Game.State != GameState.Playing && Game.State != GameState.Cutscene) return;
            StartCoroutine(WastedSequence());
        }

        private IEnumerator WastedSequence()
        {
            _transition = true;
            Game.State = GameState.Wasted;
            Vector3 deathPos = Game.PlayerPosition;
            if (Game.Phone != null) Game.Phone.Close();
            if (Game.Menus != null && Game.Menus.IsOpen) Game.Menus.Close();
            if (Game.Cam != null && Game.Cam.InCinematic) Game.Cam.ClearCinematic();
            GameTime.Request("wasted", 0.35f);
            if (Game.Audio != null)
            {
                Game.Audio.SetSlowMotion(true);
                Game.Audio.Play2D(SfxId.Wasted, 1f);
            }
            if (Game.Hud != null) Game.Hud.BigMessage("FLATLINED", "", new Color(0.85f, 0.12f, 0.12f), 4.2f);
            yield return new WaitForSecondsRealtime(3.4f);
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.7f);
            yield return new WaitForSecondsRealtime(0.8f);
            GameTime.Release("wasted");
            if (Game.Audio != null) Game.Audio.SetSlowMotion(false);
            if (Game.Wanted != null) Game.Wanted.ClearAll();
            if (Game.Police != null) Game.Police.ClearAll();
            if (Game.Progress != null) Game.Progress.OnPlayerDied(deathPos);
            bool clinicOpen = FreeRoam || (Game.Missions != null && Game.Missions.StoryProgress >= 1);
            Poi hospital = clinicOpen ? NearestOf(deathPos, PoiType.Hospital, PoiType.Clinic) : NearestOf(deathPos, PoiType.Hospital, PoiType.Hospital);
            // Wake at the last shrine you rested at, if it is closer than a hospital (or there is no hospital).
            Shrine shrine = Game.Progress != null ? Game.Progress.ShrineById(Game.Progress.LastShrine) : null;
            if (shrine != null && hospital != null && MathUtil.FlatDistance(shrine.Position, deathPos) > MathUtil.FlatDistance(hospital.Position, deathPos) * 2.5f) shrine = null;
            Vector3 spawn = shrine != null ? FindGround(shrine.Position + Vector3.forward * 2.2f) : (hospital != null ? SpawnOutside(hospital) : SafeRespawnPoint(deathPos));
            float yaw = shrine != null ? 180f : (hospital != null ? MathUtil.YawOf(hospital.Facing) : 0f);
            // 04E: dying underground wakes the player at the nearest unlocked Underheart rest point, with a route back.
            bool underRest = false;
            if (Game.Underheart != null && (WorldBounds.IsUnderground(deathPos) || (WorldContext.HasGrounded && WorldContext.LastGroundedRegion == WorldRegion.Underworld)))
            {
                Vector3 rest; float restYaw;
                try
                {
                    if (Game.Underheart.TryNearestRestPoint(WorldBounds.IsUnderground(deathPos) ? deathPos : WorldContext.LastGroundedPosition, out rest, out restYaw))
                    {
                        spawn = rest;
                        yaw = restYaw;
                        shrine = null;
                        underRest = true;
                    }
                }
                catch (Exception e) { Log.Exception("Underheart respawn", e); }
            }
            PlacePlayer(spawn, yaw);
            if (Game.Player != null) Game.Player.Health.Armor = 0f;
            int bill = 0;
            if (shrine == null && !underRest && Game.Economy != null && !(hospital != null && hospital.Type == PoiType.Clinic)) bill = Game.Economy.Charge(500);
            if (Game.Stats != null) Game.Stats.Add(StatId.Deaths, 1);
            yield return null;
            Game.State = GameState.Playing;
            _transition = false;
            if (!OverlayOpen()) GInput.SetCursorLocked(true);
            if (Game.Missions != null) Game.Missions.OnPlayerWasted();
            if (Game.Hud != null)
            {
                Game.Hud.FadeTo(0f, 1f);
                if (underRest) Game.Hud.Notify("The refuge keepers carried you to a safe rest point. Your companion and gear are with you." + (Game.Progress != null && Game.Progress.EchoActive ? " Your Lumen waits where you fell." : ""), 5f);
                else if (shrine != null) Game.Hud.Notify(shrine.Name + ": the crystal pulled you back." + (Game.Progress != null && Game.Progress.EchoActive ? " Your Lumen waits where you fell." : ""), 4.5f);
                else
                {
                    string where = hospital != null ? hospital.Name : "Saint Lumen Medical Center";
                    Game.Hud.Notify(where + ": patched up. " + (bill > 0 ? "Hospital bill " + MathUtil.Money(bill) + "." : "Mara didn't charge you.") + (Game.Progress != null && Game.Progress.EchoActive ? " Your Lumen waits where you fell." : ""), 4.5f);
                }
            }
        }

        public void OnPlayerBusted()
        {
            if (_transition || _booting || Game.State != GameState.Playing) return;
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            StartCoroutine(BustedSequence());
        }

        private IEnumerator BustedSequence()
        {
            _transition = true;
            Game.State = GameState.Busted;
            PlayerController p = Game.Player;
            int stars = Game.Wanted != null ? Mathf.Max(1, Game.Wanted.Stars) : 1;
            Vector3 where = Game.PlayerPosition;
            p.ControlsEnabled = false;
            if (Game.Phone != null) Game.Phone.Close();
            if (Game.Menus != null && Game.Menus.IsOpen) Game.Menus.Close();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Busted, 1f);
            if (Game.Hud != null) Game.Hud.BigMessage("CUFFED", "", new Color(0.35f, 0.55f, 1f), 4f);
            GameTime.Request("busted", 0.6f);
            yield return new WaitForSecondsRealtime(3f);
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.7f);
            yield return new WaitForSecondsRealtime(0.8f);
            GameTime.Release("busted");
            if (Game.Wanted != null) Game.Wanted.ClearAll();
            if (Game.Police != null) Game.Police.ClearAll();
            Poi station = Game.Layout != null ? Game.Layout.NearestPoi(where, PoiType.Police) : null;
            Vector3 spawn = station != null ? SpawnOutside(station) : SafeRespawnPoint(where);
            float yaw = station != null ? MathUtil.YawOf(station.Facing) : 0f;
            PlacePlayer(spawn, yaw);
            int fine = Game.Economy != null ? Game.Economy.Charge(250 * stars) : 0;
            p = Game.Player;
            if (p != null)
            {
                WeaponInventory inv = p.Weapons.Inventory;
                for (int i = 1; i < inv.Ammo.Length; i++) inv.Ammo[i] /= 2;
                p.ControlsEnabled = true;
            }
            if (Game.Stats != null) Game.Stats.Add(StatId.Busts, 1);
            yield return null;
            Game.State = GameState.Playing;
            _transition = false;
            if (!OverlayOpen()) GInput.SetCursorLocked(true);
            if (Game.Missions != null) Game.Missions.OnPlayerBusted();
            if (Game.Hud != null)
            {
                Game.Hud.FadeTo(0f, 1f);
                Game.Hud.Notify((station != null ? station.Name : "LVPD") + ": released. Fine " + MathUtil.Money(fine) + ", and half your ammo went missing in the evidence room.", 5f);
            }
        }

        // ================================================================== saves and story end

        public void Autosave()
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead || _transition) return;
            if (Game.State != GameState.Playing && Game.State != GameState.Cutscene) return;
            // A running mission blocks autosaves, except a 04E campaign quest at a committed checkpoint (it resumes there).
            if (Game.Missions != null && Game.Missions.Active != null && !(Game.Quests != null && Game.Quests.CheckpointSafeNow)) return;
            if (Time.unscaledTime - _lastAutosave < 4f) return;
            _lastAutosave = Time.unscaledTime;
            bool ok = SaveSystem.Save(0);
            if (Game.Hud != null) Game.Hud.Notify(ok ? "Autosaved." : "Autosave failed (see the Console).", 1.8f);
        }

        public void OnStoryComplete()
        {
            if (Game.Hud != null)
                Game.Hud.Banner("LAS VEYRA IS YOURS", "Story complete. Races, rampages, contracts, properties and every hidden collectible are still out there.", UIKit.Gold, 9f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.9f, 0.8f);
        }

        // ================================================================== frame

        private void Update()
        {
            BodyFactory.Tick();
            if (GInput.Down(Keys.Screenshot)) Phone.TakePhoto();
            if (_booting)
            {
                _tipTimer -= Time.unscaledDeltaTime;
                if (_tipTimer <= 0f)
                {
                    _tipTimer = 5f;
                    _tip = (_tip + 1) % Tips.Length;
                }
                return;
            }
            if (Game.State == GameState.Playing && !_transition && Cursor.lockState != CursorLockMode.Locked && !OverlayOpen() && GInput.MouseDown(0))
            {
                // The click that recaptures the mouse shouldn't also fire a shot.
                GInput.SetCursorLocked(true);
                GInput.LatchMouse();
            }
        }

        private static bool OverlayOpen()
        {
            return (Game.Menus != null && (Game.Menus.IsOpen || Game.Menus.TitleOpen)) || (Game.Phone != null && Game.Phone.IsOpen) || (Game.Hud != null && Game.Hud.WheelOpen);
        }

        /// <summary>Fast travel between lit shrines: fade out, move, fade in.</summary>
        public void TravelTo(Vector3 pos, float yaw)
        {
            if (_transition) return;
            StartCoroutine(TravelSequence(pos, yaw));
        }

        private IEnumerator TravelSequence(Vector3 pos, float yaw)
        {
            _transition = true;
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.4f);
            yield return new WaitForSecondsRealtime(0.45f);
            PlayerController p = Game.Player;
            if (p != null)
            {
                if (p.InVehicle) p.ExitVehicleImmediate(false);
                p.Teleport(FindGround(pos) + Vector3.up * 0.05f, yaw);
            }
            if (Game.Traffic != null) Game.Traffic.ClearAround(pos, 25f);
            if (Game.Cam != null)
            {
                Game.Cam.Yaw = yaw;
                Game.Cam.SnapBehindPlayer();
            }
            yield return new WaitForSecondsRealtime(0.25f);
            if (Game.Hud != null) Game.Hud.FadeTo(0f, 0.6f);
            _transition = false;
            if (!OverlayOpen()) GInput.SetCursorLocked(true);
        }

        private void OnApplicationQuit()
        {
            Pad.Silence();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus) Pad.Silence();
            if (focus && Game.State == GameState.Playing && !_transition && !OverlayOpen()) GInput.SetCursorLocked(true);
        }

        private void OnGUI()
        {
            if (_booting) DrawLoading();
            else if (Game.State == GameState.Title && _errors.Count > 0) DrawErrors(false);
        }

        private void DrawLoading()
        {
            GUI.depth = -100;
            UIKit.BeginScaled();
            float W = UIKit.Width, H = UIKit.Height;
            UIKit.Rect(new Rect(0f, 0f, W, H), new Color(0.02f, 0.02f, 0.04f, 1f));
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.6f);
            UIKit.Tex(new Rect(0f, H * 0.5f, W, H * 0.5f), UIKit.Gradient, new Color(0.55f, 0.08f, 0.45f, 0.25f + 0.1f * pulse));
            UIKit.Text(new Rect(0f, H * 0.2f, W, 120f), "NEON SYNDICATE", 110, UIKit.Pink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Text(new Rect(0f, H * 0.2f + 112f, W, 60f), "L A S   V E Y R A", 46, UIKit.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            Rect bar = new Rect(W * 0.2f, H * 0.68f, W * 0.6f, 14f);
            UIKit.Bar(bar, _progress, _fatal ? new Color(1f, 0.3f, 0.3f) : UIKit.Cyan, new Color(1f, 1f, 1f, 0.1f));
            UIKit.Text(new Rect(bar.x, bar.yMax + 14f, bar.width, 34f), _status + (_fatal ? "" : "   " + Mathf.RoundToInt(_progress * 100f) + "%"), 24, _fatal ? new Color(1f, 0.5f, 0.5f) : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true, true);
            if (!_fatal) UIKit.Text(new Rect(W * 0.15f, H * 0.82f, W * 0.7f, 70f), Tips[_tip], 22, new Color(0.8f, 0.8f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Normal, true, true);
            UIKit.Text(new Rect(20f, H - 36f, W - 40f, 28f), Game.Version + "  |  everything you see is generated in code at startup", 16, new Color(1f, 1f, 1f, 0.45f), TextAnchor.MiddleLeft);
            if (_errors.Count > 0) DrawErrors(true);
        }

        private void DrawErrors(bool loading)
        {
            GUI.depth = -100;
            UIKit.BeginScaled();
            float W = UIKit.Width;
            float y = loading ? 20f : UIKit.Height - 150f;
            int shown = Mathf.Min(4, _errors.Count);
            UIKit.Rect(new Rect(20f, y, W * 0.5f, 30f + shown * 24f), new Color(0.35f, 0f, 0f, 0.75f));
            UIKit.Text(new Rect(30f, y + 4f, W * 0.5f - 20f, 24f), _errors.Count + " startup problem(s), the game is still playable. Details are in the Console:", 18, new Color(1f, 0.75f, 0.75f), TextAnchor.UpperLeft, FontStyle.Bold);
            for (int i = 0; i < shown; i++)
                UIKit.Text(new Rect(30f, y + 28f + i * 24f, W * 0.5f - 20f, 24f), _errors[_errors.Count - shown + i], 16, Color.white, TextAnchor.UpperLeft);
        }
    }

    /// <summary>
    /// Which point a save stores and a loaded save remembers as the safehouse. Pure (no engine calls). An Underheart rest
    /// point is the respawn point underground and the resume point for saves made underground only; a save made on the
    /// surface always stores a surface safehouse.
    /// </summary>
    public static class SafehouseRules04E
    {
        /// <summary>True for a point at street level or above (a safehouse door), false for an underground rest point.</summary>
        public static bool IsSurface(Vector3 p)
        {
            return p.y > WorldBounds.UndergroundCeiling;
        }

        /// <summary>A point that can serve as a safehouse: set (not zero) and finite.</summary>
        public static bool Usable(Vector3 p)
        {
            return p != Vector3.zero && MathUtil.IsFinite(p);
        }

        /// <summary>
        /// The position a save stores, and whether it is the safehouse (AtSafehouse). Without a usable safehouse it is the
        /// player's position. A save made underground keeps the last safehouse (a rest point, or a surface safehouse the
        /// Underheart load path replaces by its saved anchor). A save made on the surface never stores an underground rest
        /// point: it stores the surface safehouse (the last one chosen, else the home spawn the caller passes), or the
        /// player's own position when even that is missing.
        /// </summary>
        public static Vector3 SavePosition(Vector3 player, bool savedUnderground, Vector3 lastSafehouse, Vector3 surfaceSafehouse, out bool atSafehouse)
        {
            atSafehouse = false;
            if (!Usable(lastSafehouse)) return player;
            atSafehouse = true;
            if (savedUnderground || IsSurface(lastSafehouse)) return lastSafehouse;
            if (Usable(surfaceSafehouse) && IsSurface(surfaceSafehouse)) return surfaceSafehouse;
            atSafehouse = false;
            return player;
        }

        /// <summary>
        /// The surface safehouse after a load: the loaded safehouse when it is on the surface (the newest, already verified),
        /// else the surface safehouse the save recorded, else zero (an older save whose safehouse was a rest point; surface
        /// saves then use the home spawn).
        /// </summary>
        public static Vector3 LoadedSurfaceSafehouse(Vector3 loadedSafehouse, bool recorded, Vector3 recordedSurface)
        {
            if (Usable(loadedSafehouse) && IsSurface(loadedSafehouse)) return loadedSafehouse;
            if (recorded && Usable(recordedSurface) && IsSurface(recordedSurface)) return recordedSurface;
            return Vector3.zero;
        }

        /// <summary>
        /// True for a save that stored an underground rest point as its safehouse although it was made on the surface (its
        /// Underheart block names no saved region). Only an earlier 04E build wrote such saves; they resume on the surface.
        /// </summary>
        public static bool SurfaceSaveHeldRestPoint(bool atSafehouse, Vector3 savedPosition, string savedRegion)
        {
            return atSafehouse && Usable(savedPosition) && !IsSurface(savedPosition) && string.IsNullOrEmpty(savedRegion);
        }

        /// <summary>
        /// A save made with AtSafehouse stores the safehouse as its position. When the Underheart replaced that position
        /// on load (an underground resume, or its surface fallback), a safehouse on the surface stays exactly as saved;
        /// only a safehouse that was itself underground (a rest point) follows the verified point the Underheart chose.
        /// Without a replacement the resolved position is the safehouse, as before.
        /// </summary>
        public static Vector3 LoadedSafehouse(Vector3 saved, Vector3 resolved, bool underheartMoved)
        {
            if (!underheartMoved || !MathUtil.IsFinite(saved)) return resolved;
            return saved.y > WorldBounds.UndergroundCeiling ? saved : resolved;
        }
    }

    /// <summary>Ground search rules for GameRoot.FindGround. Pure.</summary>
    public static class GroundRules04E
    {
        /// <summary>The long ray from 120 m above is only for points at street level or higher; never below the street.</summary>
        public static bool MayUseHighFallback(float y)
        {
            return y >= WorldBounds.UndergroundCeiling;
        }
    }
}
