using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A compact, actual creature habitat near the first free-exploration spawn. Uses the existing creature director,
    /// interact input, bestiary and pose solver. No save schema, story position, objective panel or combat AI changes.
    /// </summary>
    public sealed class StarterHabitat : MonoBehaviour, IWorldSystem
    {
        private const string ResidentTag = "starter01c_habitat";
        private const string PracticeTag = "starter01c_training";
        public static StarterHabitat Instance { get; private set; }
        private CityLayout _layout;
        private readonly List<Creature> _residents = new List<Creature>();
        private readonly List<Vector3> _sites = new List<Vector3>();
        private readonly List<string> _species = new List<string>();
        private Vector3 _anchor;
        private Vector3 _practiceSite;
        private Creature _training;
        private Interactable _practicePrompt;
        private GameObject _practiceMarker;
        private Blip _sanctuaryBlip;
        private bool _pending;
        private bool _configured;
        private bool _loaded;
        private bool _hasPracticeSite;
        private bool _welcomed;
        private bool _loadedHadReady;
        private float _settle;
        private float _nextPractice;
        private float _practiceStarted;
        private float _loadedAt;
        private float _practiceNoiseUntil;
        private float _nextObserve;
        private int _loadAttempts;

        /// <summary>One allocated practice opponent, including its pending body build, until the winning hit.</summary>
        public static bool TrainingActive
        {
            get { return Instance != null && Instance._training != null && !Instance._training.IsDead &&
                !Instance._training.PracticeDefeated && !Instance._training.PracticePlayerDefeated; }
        }

        /// <summary>Includes the final shot callback and a short local finish grace. Actual crimes always override it.</summary>
        public static bool SuppressPracticeNoise
        {
            get
            {
                if (Instance == null) return false;
                if (Instance._training != null) return true;
                return Time.time < Instance._practiceNoiseUntil &&
                    MathUtil.FlatSqrDistance(Game.PlayerPosition, Instance._practiceSite) < 45f * 45f;
            }
        }

        public static bool IsStarterResident(CharacterBase body)
        {
            string tag = body != null ? CreatureDirector.TagOf(body) : null;
            return tag == ResidentTag || tag == PracticeTag;
        }

        public static void ResetStatics() { Instance = null; }

        /// <summary>Boot after CreatureDirector.Init. GameRoot starts a habitat session after placing the player.</summary>
        public void Init(CityLayout layout)
        {
            Instance = this;
            _layout = layout;
            WorldSystems.Register(this);
        }

        /// <summary>
        /// Defers construction until free exploration starts. The prologue can move the player before that point,
        /// so the final anchor is sampled then, not at the authored mission's initial spawn.
        /// </summary>
        public void BeginSession(Vector3 spawn)
        {
            ResetAll();
            _anchor = spawn;
            _pending = true;
        }

        public void ResetAll()
        {
            Unload();
            _sites.Clear();
            _species.Clear();
            _pending = _configured = _hasPracticeSite = _welcomed = false;
            _settle = _nextPractice = _practiceStarted = _nextObserve = 0f;
            _practiceNoiseUntil = 0f;
            _loadAttempts = 0;
        }

        // Scans are already saved by Compendium in SaveData.Creatures. Habitat placement and sparring are transient.
        public void Capture(SaveData data) { }
        public void Restore(SaveData data) { }

        private void OnDestroy()
        {
            Unload();
            if (Instance == this) Instance = null;
        }

        private bool FreeExploration
        {
            get { return Game.IsPlaying && Game.Player != null && !Game.Player.IsDead &&
                (Game.Missions == null || Game.Missions.Active == null) && !QaTools.Busy; }
        }

        private void Update()
        {
            if (_training != null)
            {
                bool dead = _training.IsDead || _training.PracticeDefeated;
                bool lost = _training.PracticePlayerDefeated;
                bool leave = Game.Player == null || Game.Player.IsDead || !Game.IsPlaying ||
                    (Game.Player != null && Game.Player.InVehicle) ||
                    (Game.Missions != null && Game.Missions.Active != null) || QaTools.Busy ||
                    MathUtil.FlatSqrDistance(Game.PlayerPosition, _practiceSite) > 38f * 38f;
                bool timeout = Time.time - _practiceStarted > 150f;
                bool buildFailed = !_training.Ready && Time.time - _practiceStarted > 20f;
                if (dead || lost || leave || timeout || buildFailed)
                    StopPractice(lost ? "Practice ended. The jackal wins this round; your last health point was protected." :
                        (dead ? "Practice complete. No loot or experience is awarded for sparring." :
                        (buildFailed ? "Practice could not load. You can try again shortly." : "Practice ended.")));
            }
            if (!FreeExploration) return;
            if (_pending)
            {
                _settle += Time.deltaTime;
                if (_settle < 0.75f) return;
                _anchor = Game.PlayerPosition;
                _pending = false;
                ConfigureSites();
            }
            if (!_configured) return;
            float distance = MathUtil.FlatSqrDistance(Game.PlayerPosition, _anchor);
            if (_loaded && distance > 125f * 125f) Unload();
            else if (!_loaded && distance < 85f * 85f && _loadAttempts < 3) Load();
            if (!_loaded) return;
            int ready = 0;
            for (int i = 0; i < _residents.Count; i++) if (_residents[i] != null && _residents[i].Ready) ready++;
            if (ready == 0 && !_loadedHadReady && _sites.Count > 0 && Time.time - _loadedAt > 20f)
            {
                Log.Warn("Starter habitat bodies did not materialize within 20 seconds. Retrying within the three-attempt cap.");
                Unload();
                return;
            }
            if (ready == 0) return;
            _loadedHadReady = true;
            _loadAttempts = 0;
            if (_welcomed) return;
            _welcomed = true;
            Notify(GInput.Prompt("C marks the nearby sanctuary. Approach a calm creature and press E to observe. Sparring is optional."), 5f);
        }

        private void ConfigureSites()
        {
            _sites.Clear();
            _species.Clear();
            AddSite("neon_jackal", 0.9f);
            AddSite("crystal_basilisk", 1.15f);
            // A griffin needs more room than a sidewalk. Use an existing smaller species if no clear roost fits.
            if (!AddSite("rime_griffin", 2.6f)) AddSite("frost_stalker", 1.25f);
            _hasPracticeSite = FindSite(1.1f, out _practiceSite);
            _configured = _sites.Count > 0 || _hasPracticeSite;
            if (_sites.Count < 3)
                Log.Warn("Starter habitat found " + _sites.Count + " of 3 safe ground sites. Review this seed in Unity; occupied roads and entrances were preserved.");
        }

        private bool AddSite(string species, float clearance)
        {
            Vector3 point;
            if (!FindSite(clearance, out point)) return false;
            _sites.Add(point);
            _species.Add(species);
            return true;
        }

        private bool FindSite(float clearance, out Vector3 point)
        {
            point = _anchor;
            if (_layout == null) return false;
            List<Vector3> candidates = new List<Vector3>();
            foreach (WalkNode node in _layout.WalkNodes)
            {
                float d = MathUtil.FlatSqrDistance(node.Pos, _anchor);
                if (d >= 7f * 7f && d <= 62f * 62f) candidates.Add(node.Pos);
            }
            // Ground between walk nodes gives large animals a chance to occupy an existing park or plaza.
            for (int ring = 1; ring <= 8; ring++)
            {
                float radius = ring * 7f;
                for (int i = 0; i < 24; i++)
                {
                    float angle = i * Mathf.PI * 2f / 24f;
                    candidates.Add(_anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
                }
            }
            candidates.Sort((a, b) => MathUtil.FlatSqrDistance(a, _anchor).CompareTo(MathUtil.FlatSqrDistance(b, _anchor)));
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 candidate = candidates[i];
                candidate.y = _anchor.y;
                if (_layout.IsInsideBuilding(candidate, clearance + 0.5f) || _layout.RoadAt(candidate, clearance + 0.3f) != null) continue;
                bool occupied = false;
                for (int j = 0; j < _sites.Count; j++)
                    if (MathUtil.FlatSqrDistance(candidate, _sites[j]) < 9f * 9f) { occupied = true; break; }
                if (occupied) continue;
                // Keep the existing services, mission contacts and player interaction prompts reachable.
                foreach (Interactable interactable in Interactable.All)
                {
                    if (interactable == null) continue;
                    float separation = interactable.Radius + clearance + 2f;
                    if (MathUtil.FlatSqrDistance(candidate, interactable.transform.position) < separation * separation)
                    { occupied = true; break; }
                }
                if (occupied) continue;
                float ground;
                if (!CreatureDirector.GroundAt(candidate, 2.5f, out ground) || Mathf.Abs(ground - _anchor.y) > 1.1f) continue;
                candidate.y = ground;
                if (Physics.CheckCapsule(candidate + Vector3.up * (clearance + 0.12f),
                    candidate + Vector3.up * (clearance + 1.8f), clearance, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(_anchor + Vector3.up * 1.2f, candidate + Vector3.up * 1.2f,
                    Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                point = candidate;
                return true;
            }
            return false;
        }

        private void Load()
        {
            if (Game.Creatures == null || _loaded) return;
            _loadAttempts++;
            _loaded = true;
            _loadedHadReady = false;
            _loadedAt = Time.time;
            for (int i = 0; i < _sites.Count; i++)
            {
                Creature creature = Game.Creatures.Spawn(_species[i], _sites[i], MathUtil.YawOf(_anchor - _sites[i]),
                    new CreatureSpawnOptions
                    {
                        Tag = ResidentTag, Hostile = false, Persistent = true, CalmUntilProvoked = true,
                        NoRewards = true, DamageScale = 0.25f, HealthScale = 0.70f, Seed = Game.Seed + 7301 + i * 7919
                    }) as Creature;
                if (creature == null) continue;
                _residents.Add(creature);
                Interactable interact = creature.gameObject.AddComponent<Interactable>();
                interact.Radius = 3.5f;
                interact.HeightTolerance = 2.2f;
                interact.DynamicPrompt = () => "Press E to observe " + creature.DisplayName;
                interact.Condition = () => FreeExploration && creature.Ready && !creature.IsDead && creature.CalmHabitatResident && !TrainingActive;
                interact.Use = () => Observe(creature);
            }
            Vector3 guide = _hasPracticeSite ? _practiceSite : (_sites.Count > 0 ? _sites[0] : _anchor);
            _sanctuaryBlip = Blip.Add(BlipKind.Activity, null, guide, new Color(0.35f, 0.82f, 0.90f), "C", "Sanctuary", true);
            if (!_hasPracticeSite) return;
            _practicePrompt = Interactable.Create("Sanctuary practice", _practiceSite, 2.7f,
                "Press E to begin a gentle sparring trial", TogglePractice, transform);
            _practicePrompt.Condition = () => FreeExploration && (_training != null || (Time.time >= _nextPractice && MayStartPractice));
            _practicePrompt.DynamicPrompt = () => _training != null ? "Press E to stop sparring" : "Press E to practice against one Neon Jackal (no rewards)";
            WorldMarker marker = WorldMarker.Create(_practiceSite, new Color(0.35f, 0.70f, 0.85f), 0.32f, transform, false);
            _practiceMarker = marker.gameObject;
        }

        private void Observe(Creature creature)
        {
            if (creature == null || !creature.Ready || !creature.CalmHabitatResident || Time.time < _nextObserve) return;
            _nextObserve = Time.time + 2.5f;
            creature.ObserveInHabitat(Game.Player);
            if (Game.Compendium != null) Game.Compendium.MarkScanned(creature.Species.Id, true);
            creature.HighlightUntil = Time.time + Compendium.HighlightSeconds;
            Notify(creature.DisplayName + " acknowledges you. Bestiary updated: " + creature.Species.Weak, 5.5f);
        }

        private void TogglePractice()
        {
            if (_training != null) { StopPractice("Practice stopped."); return; }
            if (!FreeExploration || !MayStartPractice || Time.time < _nextPractice || Game.Creatures == null) return;
            Vector3 start;
            if (!FindPracticeStart(out start))
            {
                Notify("Step away from the marker to leave room for the practice partner, then try again.", 4f);
                return;
            }
            // One tagged target at a time; it uses the normal bite, telegraph, dodge and damage loop.
            _practiceStarted = Time.time;
            _training = Game.Creatures.Spawn("neon_jackal", start,
                MathUtil.YawOf(Game.PlayerPosition - start), new CreatureSpawnOptions
                {
                    Tag = PracticeTag, Hostile = true, Persistent = true, NoRewards = true, PracticeTarget = true,
                    HealthScale = 0.65f, DamageScale = 0.35f, Seed = Game.Seed + 8111,
                    DisplayName = "Neon Jackal - Practice"
                }) as Creature;
            if (_training == null) { _nextPractice = Time.time + 10f; Notify("Practice could not load. Try again shortly.", 3f); return; }
            Notify(GInput.Prompt("Sparring started. Dodge its warning and strike. Return to this marker and press E to stop, or walk away."), 5f);
        }

        private bool FindPracticeStart(out Vector3 start)
        {
            start = _practiceSite;
            if (_layout == null) return false;
            // Do not materialize on the player's feet or on another sanctuary resident.
            for (int radius = 4; radius <= 12; radius += 2)
            {
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI * 2f / 16f;
                    Vector3 p = _practiceSite + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (MathUtil.FlatSqrDistance(p, Game.PlayerPosition) < 5f * 5f) continue;
                    if (_layout.IsInsideBuilding(p, 1.2f) || _layout.RoadAt(p, 1f) != null) continue;
                    bool occupied = false;
                    for (int j = 0; j < _residents.Count; j++)
                        if (_residents[j] != null && MathUtil.FlatSqrDistance(p, _residents[j].transform.position) < 5f * 5f)
                        { occupied = true; break; }
                    if (occupied) continue;
                    float ground;
                    if (!CreatureDirector.GroundAt(p, 2.5f, out ground) || Mathf.Abs(ground - _practiceSite.y) > 0.7f) continue;
                    p.y = ground;
                    if (Physics.CheckCapsule(p + Vector3.up, p + Vector3.up * 2f, 0.8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    if (Physics.Linecast(_practiceSite + Vector3.up * 1.1f, p + Vector3.up * 1.1f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    start = p;
                    return true;
                }
            }
            return false;
        }

        private void StopPractice(string message)
        {
            bool existed = _training != null;
            // Weapon hits are resolved before their Gunshot threat event. Keep the final shot inside the practice
            // noise scope until all callbacks finish; actual hits on ordinary people still revoke starter safety.
            if (existed) _practiceNoiseUntil = Time.time + 2f;
            if (_training != null) _training.StopHabitatPractice();
            if (Game.Creatures != null) Game.Creatures.DespawnTag(PracticeTag);
            _training = null;
            _nextPractice = Time.time + 12f;
            if (existed && Game.IsPlaying && !string.IsNullOrEmpty(message)) Notify(message, 3.5f);
        }

        private void Unload()
        {
            StopPractice(null);
            for (int i = 0; i < _residents.Count; i++)
                if (_residents[i] != null) _residents[i].gameObject.SetActive(false);
            if (Game.Creatures != null) Game.Creatures.DespawnTag(ResidentTag);
            _residents.Clear();
            if (_practicePrompt != null) { _practicePrompt.enabled = false; Destroy(_practicePrompt.gameObject); }
            if (_practiceMarker != null) Destroy(_practiceMarker);
            _practicePrompt = null;
            _practiceMarker = null;
            if (_sanctuaryBlip != null) Blip.Remove(_sanctuaryBlip);
            _sanctuaryBlip = null;
            _loaded = false;
        }

        private bool MayStartPractice
        {
            get { return !CombatDirector.InCombat && (Game.Wanted == null || Game.Wanted.Stars == 0) &&
                Game.Player != null && Game.Player.Health != null && Game.Player.Health.Health > 1.01f; }
        }

        private void Notify(string message, float seconds)
        {
            if (Game.Hud != null) Game.Hud.Notify(message, seconds);
        }
    }
}
