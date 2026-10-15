using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Time-sliced population for surveyed 04B places. Uses the real director and NPC rig/pose stack.
    /// Ownership, resources and save state belong to TerritorySystem; this component owns only transient actors.
    /// </summary>
    public sealed class TerritoryEcology04B : MonoBehaviour
    {
        private sealed class Habitat
        {
            public TerritoryHabitat04B Data;
            public Vector3 Origin;
            public Transform Owner;
            public string Tag;
            public bool Loaded;
            public int NextSlot;
            public float Retry;
            public readonly List<Creature> Bodies = new List<Creature>();
        }

        private sealed class StaffSite
        {
            public string Id;
            public string Region;
            public Vector3 Origin;
            public Transform Owner;
            public bool Hearth;
            public bool Loaded;
            public int NextSlot;
            public float Retry;
            public readonly List<NpcController> Bodies = new List<NpcController>();
        }

        private readonly List<Habitat> _habitats = new List<Habitat>();
        private readonly List<StaffSite> _staff = new List<StaffSite>();
        private float _clock;
        private float _slice;
        private float _careCooldown;
        private int _cursor;
        private bool _initialised;

        public int RegisteredHabitats { get { return _habitats.Count; } }
        public int CreatureCount
        {
            get
            {
                int count = 0;
                foreach (Habitat habitat in _habitats) foreach (Creature body in habitat.Bodies)
                    if (body != null && !body.IsDead) count++;
                return count;
            }
        }
        public int StaffCount
        {
            get
            {
                int count = 0;
                foreach (StaffSite site in _staff) foreach (NpcController body in site.Bodies)
                    if (body != null && !body.IsDead) count++;
                return count;
            }
        }

        public void Init() { _initialised = true; }

        public bool RegisterHabitat(string habitatId, Vector3 origin, Transform owner)
        {
            if (!_initialised || owner == null || string.IsNullOrEmpty(habitatId)) return false;
            foreach (Habitat existing in _habitats) if (existing.Data.id == habitatId) return false;
            TextAsset asset = Resources.Load<TextAsset>("LivingTerritories04B/Habitats/" + habitatId);
            if (asset == null) { Log.Warn("04B missing habitat " + habitatId); return false; }
            TerritoryHabitat04B data = JsonUtility.FromJson<TerritoryHabitat04B>(asset.text);
            string error;
            if (!TerritoryEcologyRules04B.Valid(data, out error) || data.id != habitatId)
            { Log.Warn("04B rejected habitat " + habitatId + ": " + error); return false; }
            _habitats.Add(new Habitat { Data = data, Origin = origin, Owner = owner, Tag = "04b/habitat/" + data.id });
            Log.Info("04B_HABITAT " + data.id + " at " + origin + " role=" + data.role + " slots=" + data.slots.Length);
            return true;
        }

        /// <summary>Origin is the hub center, or the actual Hearthspire caretaker station when hearth is true.</summary>
        public bool RegisterStaff(string siteId, string region, Vector3 origin, Transform owner, bool hearth = false)
        {
            if (!_initialised || owner == null || string.IsNullOrEmpty(siteId) || _staff.Count >= 4) return false;
            foreach (StaffSite existing in _staff) if (existing.Id == siteId) return false;
            _staff.Add(new StaffSite { Id = siteId, Region = region, Origin = origin, Owner = owner, Hearth = hearth });
            return true;
        }

        /// <summary>Owner calls every frame, even on title; pause freezes all timers and does not duplicate actors.</summary>
        public void Tick(float dt)
        {
            if (!_initialised) return;
            if (Game.State == GameState.Title || Game.State == GameState.Loading || Game.Player == null || Game.Player.IsDead ||
                (Game.Inner != null && Game.Inner.IsInner))
            { ClearPopulation(); return; }
            if (!Game.IsPlaying || GameTime.Paused || dt <= 0f) return;
            _clock += Mathf.Min(dt, 1f);
            _slice -= dt;
            if (_slice > 0f) return;
            _slice = 0.75f;
            Vector3 focus = LivingTerritories04BQa.FocusOverride ? LivingTerritories04BQa.FocusPoint : Game.PlayerPosition;
            bool maySpawn = Game.Missions == null || Game.Missions.Active == null;
            // Existing suppression is respected. These actors never change police, gang or ambient ecology switches.
            if (Game.Peds != null && Game.Peds.Suppressed) maySpawn = false;
            foreach (Habitat habitat in _habitats)
            {
                if (habitat.Owner == null || !habitat.Owner.gameObject.activeInHierarchy ||
                    !TerritoryEcologyRules04B.ShouldLoad(habitat.Loaded, Vector3.Distance(focus, habitat.Origin))) Unload(habitat);
            }
            foreach (StaffSite site in _staff)
            {
                if (site.Owner == null || !site.Owner.gameObject.activeInHierarchy ||
                    !TerritoryEcologyRules04B.ShouldLoad(site.Loaded, Vector3.Distance(focus, site.Origin))) Unload(site);
            }
            // One actor allocation per slice globally. Pending creature bodies count against the cap.
            int total = _habitats.Count + _staff.Count;
            for (int i = 0; maySpawn && i < total; i++)
            {
                int index = _cursor++ % total;
                bool spawned = index < _habitats.Count ? Fill(_habitats[index], focus) : Fill(_staff[index - _habitats.Count], focus);
                if (spawned) break;
            }
            foreach (StaffSite site in _staff) MaintainStaff(site);
            foreach (Habitat habitat in _habitats) CheckPendingBodies(habitat);
        }

        private bool Fill(Habitat habitat, Vector3 focus)
        {
            if (habitat.Owner == null || !habitat.Owner.gameObject.activeInHierarchy || _clock < habitat.Retry || Game.Creatures == null ||
                !TerritoryEcologyRules04B.ShouldLoad(habitat.Loaded, Vector3.Distance(focus, habitat.Origin))) return false;
            int preset = (int)Settings.Graphics;
            if (!TerritoryEcologyRules04B.CanAllocateCreature(preset, CreatureCount, Game.Creatures.AliveCount)) return false;
            habitat.Loaded = true;
            int weather = Game.Atmo != null ? (int)Game.Atmo.Current : 0;
            float hours = Game.Atmo != null ? Game.Atmo.Hours : 12f;
            while (habitat.NextSlot < habitat.Data.slots.Length)
            {
                int slotIndex = habitat.NextSlot++;
                TerritoryHabitatSlot04B slot = habitat.Data.slots[slotIndex];
                if (!TerritoryEcologyRules04B.SlotEnabled(slot, preset, weather)) continue;
                string id = TerritoryEcologyRules04B.Species(slot, hours, weather);
                Vector3 local = new Vector3(slot.position[0], slot.position[1], slot.position[2]);
                Vector3 at;
                if (!SafePosition(habitat.Origin + local, habitat.Origin.y, slot.clearance, slot.aerial, out at))
                { Log.Info("04B_HABITAT_SKIP " + habitat.Data.id + " slot=" + slotIndex + " blocked surface or actor"); continue; }
                int seed = TerritoryEcologyRules04B.Seed(Game.Seed, habitat.Data.id, slotIndex);
                Creature creature = Game.Creatures.Spawn(id, at, slot.yaw, new CreatureSpawnOptions
                {
                    Tag = habitat.Tag, Persistent = true, Hostile = false, CalmUntilProvoked = true,
                    NoRewards = true, Seed = seed, HealthScale = 1f,
                    DamageScale = Settings.Difficulty == DifficultyLevel.Story ? 0.35f : 0.6f,
                    DisplayName = CreatureSpeciesDb.Get(id).Name + " - " + habitat.Data.title
                }) as Creature;
                if (creature == null) continue;
                creature.Home = at;
                creature.ConfigureTerritoryHabitat(slot.roam, slot.aerial);
                habitat.Bodies.Add(creature);
                Log.Info("04B_HABITAT_SPAWN " + habitat.Data.id + " species=" + id + " seed=" + seed + " position=" + at);
                return true;
            }
            return false;
        }

        private bool Fill(StaffSite site, Vector3 focus)
        {
            if (site.Owner == null || !site.Owner.gameObject.activeInHierarchy || _clock < site.Retry ||
                !TerritoryEcologyRules04B.ShouldLoad(site.Loaded, Vector3.Distance(focus, site.Origin))) return false;
            int preset = (int)Settings.Graphics;
            if (StaffCount >= TerritoryEcologyRules04B.StaffCap(preset)) return false;
            site.Loaded = true;
            int target = TerritoryEcologyRules04B.StaffPerSite(preset, site.Hearth);
            while (site.NextSlot < target)
            {
                int index = site.NextSlot++;
                Vector3 local = TerritoryEcologyRules04B.StaffOffset(index, site.Hearth);
                Vector3 at;
                if (!SafePosition(site.Origin + local, site.Origin.y, 0.75f, false, out at)) continue;
                int seed = TerritoryEcologyRules04B.Seed(Game.Seed, site.Id, index + 40);
                NpcController npc = NpcController.Spawn(at, TerritoryEcologyRules04B.StaffYaw(index), NpcRole.Clerk,
                    TerritoryEcologyRules04B.StaffLook(site.Region, seed), -1);
                if (npc == null) continue;
                npc.gameObject.name = "04B " + site.Id + " staff " + index;
                npc.Persistent = true;
                npc.MissionTag = "04b/staff/" + site.Id + "/" + index;
                npc.IgnoreThreats = true;
                npc.Pacifist = true;
                npc.Hostile = false;
                npc.Courage = 0f;
                npc.DropsLoot = false;
                npc.CashDrop = 0;
                npc.DisplayName = site.Hearth ? "Hearthspire Steward" : StaffName(site.Region, index);
                npc.GuardAt(at);
                site.Bodies.Add(npc);
                Log.Info("04B_STAFF_SPAWN " + site.Id + " role=" + npc.DisplayName + " seed=" + seed + " position=" + at);
                return true;
            }
            return false;
        }

        private void CheckPendingBodies(Habitat habitat)
        {
            foreach (Creature creature in habitat.Bodies)
            {
                if (creature == null || creature.Ready || creature.IsDead || Time.time - creature.SpawnTime < 25f) continue;
                // A failed worker/import is not retried continuously in front of the player. A new visit can retry.
                foreach (Creature body in habitat.Bodies) if (body != null) body.gameObject.SetActive(false);
                if (Game.Creatures != null) Game.Creatures.DespawnTag(habitat.Tag);
                else foreach (Creature body in habitat.Bodies) if (body != null) Destroy(body.gameObject);
                habitat.Bodies.Clear();
                habitat.NextSlot = habitat.Data.slots.Length;
                Log.Warn("04B habitat body build timed out at " + habitat.Data.id + ". Population disabled until a new visit.");
                return;
            }
        }

        private static string StaffName(string region, int index)
        {
            if (region == "verdant") return index == 0 ? "Rootwater Herbalist" : index == 1 ? "Canopy Watcher" : "Sanctuary Student";
            if (region == "obsidian") return index == 0 ? "Shatterworks Surveyor" : index == 1 ? "Frontier Lookout" : "Crystal Technician";
            return index == 0 ? "Resonance Archivist" : index == 1 ? "Court Watcher" : "Resonance Student";
        }

        private void MaintainStaff(StaffSite site)
        {
            // Real idle breathing and look poses are retained; an occasional two-metre walk uses the existing AI.
            // This is station work/conversation staging, not a claim of new craft or dialogue animation clips.
            for (int i = 0; i < site.Bodies.Count; i++)
            {
                NpcController npc = site.Bodies[i];
                if (npc == null || npc.IsDead || npc.IsRagdolled || npc.State == NpcState.Flee || npc.State == NpcState.Scripted) continue;
                if (npc.Health != null && Time.time - npc.Health.LastDamageTime < 16f) continue;
                if (npc.Target != null) continue;
                if (((int)_clock + i * 7) % 29 != 0) continue;
                Vector3 goal = npc.Home + (i % 2 == 0 ? Vector3.right : Vector3.left) * 1.2f;
                Vector3 at;
                if (SafePosition(goal, npc.Home.y, 0.45f, false, out at) && !Physics.Linecast(npc.ChestPosition, at + Vector3.up,
                    Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    npc.MoveTo(at, false, ReturnToStation);
            }
        }

        private static void ReturnToStation(NpcController npc)
        {
            if (npc != null && !npc.IsDead) npc.GuardAt(npc.Home);
        }

        private static bool SafePosition(Vector3 proposed, float expectedGround, float radius, bool aerial, out Vector3 result)
        {
            result = proposed;
            if (Game.Layout != null && (Game.Layout.RoadAt(proposed, radius + 1f) != null || Game.Layout.IsInsideBuilding(proposed, radius + 0.3f))) return false;
            float ground;
            Vector3 groundPoint = new Vector3(proposed.x, expectedGround, proposed.z);
            if (!CreatureDirector.GroundAt(groundPoint, 2.5f, out ground) || Mathf.Abs(ground - expectedGround) > 1.1f) return false;
            if (!aerial) result.y = ground;
            if ((Game.PlayerPosition - result).sqrMagnitude < (radius + 1.5f) * (radius + 1.5f)) return false;
            Vector3 bottom = result + Vector3.up * (radius + 0.15f);
            Vector3 top = result + Vector3.up * (radius + 1.5f);
            if (Physics.CheckCapsule(bottom, top, radius, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            foreach (Creature creature in Creature.All)
            {
                if (creature == null || creature.IsDead) continue;
                float separation = radius + Mathf.Max(0.7f, creature.Radius);
                if ((creature.transform.position - result).sqrMagnitude < separation * separation) return false;
            }
            foreach (NpcController npc in NpcController.All)
            {
                if (npc == null || npc.IsDead) continue;
                if ((npc.transform.position - result).sqrMagnitude < (radius + 0.4f) * (radius + 0.4f)) return false;
            }
            return true;
        }

        public bool FindCare(Vector3 playerPosition, out TerritoryCare04B care)
        {
            care = default(TerritoryCare04B);
            if (_clock < _careCooldown || Game.Player == null || Game.Player.IsDead || Game.Player.InVehicle || !Game.Player.IsGroundedPublic ||
                !Game.IsPlaying || GameTime.Paused || (Game.Wanted != null && Game.Wanted.Stars > 0) ||
                (Game.Missions != null && Game.Missions.Active != null) || (Game.Inner != null && Game.Inner.IsInner)) return false;
            float best = TerritoryEcologyRules04B.CareDistance * TerritoryEcologyRules04B.CareDistance;
            foreach (Habitat habitat in _habitats)
            {
                if (!habitat.Data.care) continue;
                foreach (Creature body in habitat.Bodies)
                {
                    if (body == null || !body.Ready || body.IsDead || !body.CalmHabitatResident || body.Species == null) continue;
                    float distance = (playerPosition - body.transform.position).sqrMagnitude;
                    if (distance >= best || Physics.Linecast(Game.Player.ChestPosition, body.ChestPosition, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                    best = distance;
                    care = new TerritoryCare04B
                    {
                        Id = "care_" + habitat.Data.id, Habitat = habitat.Data.id, Species = body.Species.Id,
                        Label = habitat.Data.careLabel, Position = body.transform.position
                    };
                }
            }
            return !string.IsNullOrEmpty(care.Id);
        }

        /// <summary>Revalidate at the physical animal. Caller persists the unique care ID before paying any cache.</summary>
        public bool CompleteCare(string careId)
        {
            TerritoryCare04B candidate;
            if (!FindCare(Game.PlayerPosition, out candidate) || candidate.Id != careId) return false;
            foreach (Habitat habitat in _habitats)
            {
                if (habitat.Data.id != candidate.Habitat) continue;
                foreach (Creature creature in habitat.Bodies)
                {
                    if (creature == null || !creature.Ready || creature.IsDead || !creature.CalmHabitatResident || creature.Species.Id != candidate.Species) continue;
                    if ((creature.transform.position - candidate.Position).sqrMagnitude > 0.1f) continue;
                    creature.ObserveInHabitat(Game.Player);
                    if (creature.Health != null) creature.Health.Heal(creature.Health.MaxHealth * 0.2f);
                    if (Game.Compendium != null) Game.Compendium.MarkScanned(candidate.Species, false);
                    if (Game.Audio != null) Game.Audio.Play2D(SfxId.CrystalChime, 0.35f, 1.12f);
                    _careCooldown = _clock + 12f;
                    return true;
                }
            }
            return false;
        }

        private void Unload(Habitat habitat)
        {
            if (!habitat.Loaded) return;
            foreach (Creature creature in habitat.Bodies) if (creature != null) creature.gameObject.SetActive(false);
            if (Game.Creatures != null) Game.Creatures.DespawnTag(habitat.Tag);
            else foreach (Creature creature in habitat.Bodies) if (creature != null) Destroy(creature.gameObject);
            habitat.Bodies.Clear();
            habitat.Loaded = false;
            habitat.NextSlot = 0;
            habitat.Retry = _clock + TerritoryEcologyRules04B.RetrySeconds;
        }

        private void Unload(StaffSite site)
        {
            if (!site.Loaded) return;
            foreach (NpcController npc in site.Bodies)
            {
                if (npc == null) continue;
                npc.OnScriptArrived = null;
                npc.gameObject.SetActive(false);
                Destroy(npc.gameObject);
            }
            site.Bodies.Clear();
            site.Loaded = false;
            site.NextSlot = 0;
            site.Retry = _clock + TerritoryEcologyRules04B.RetrySeconds;
        }

        /// <summary>Clear live actors but keep authored registrations for the next play session or restored save.</summary>
        public void ClearPopulation()
        {
            foreach (Habitat habitat in _habitats) { Unload(habitat); habitat.Retry = 0f; }
            foreach (StaffSite site in _staff) { Unload(site); site.Retry = 0f; }
            _slice = 0f;
            _cursor = 0;
            _careCooldown = 0f;
        }

        /// <summary>Use before the owner rebuilds its sites. No object survives its geometry owner.</summary>
        public void ClearSites()
        {
            ClearPopulation();
            _habitats.Clear();
            _staff.Clear();
            _clock = 0f;
        }

        private void OnDisable() { ClearPopulation(); }
        private void OnDestroy() { ClearSites(); }
    }
}
