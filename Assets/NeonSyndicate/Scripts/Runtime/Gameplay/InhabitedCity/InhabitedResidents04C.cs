using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Owns transient venue assistants and visitors. Services never depend on a rendered actor.</summary>
    public sealed class InhabitedResidents04C : MonoBehaviour
    {
        private sealed class Site
        {
            public VenueBinding04C Venue;
            public bool Loaded;
            public bool Reported;
            public bool AudioFailed;
            public float Distance;
            public readonly NpcController[] Bodies = new NpcController[3];
            public readonly bool[] Assigned = new bool[3];
            public readonly float[] Retry = new float[3];
            public AudioSource Ambience;
            public AudioClip Clip;
        }
        private readonly List<Site> _sites = new List<Site>();
        private readonly List<Site> _selected = new List<Site>();
        private InhabitedCitySystem _owner;
        private float _clock;
        private float _slice;
        public int ResidentCount
        {
            get { int count = 0; foreach (Site site in _sites) foreach (NpcController body in site.Bodies) if (body != null) count++; return count; }
        }
        public int LoadedCount
        {
            get { int count = 0; foreach (Site site in _sites) if (site.Loaded) count++; return count; }
        }

        public void Init(InhabitedCitySystem owner) { ResetForSession(); _owner = owner; }

        /// <summary>Called even on title. Pause keeps owned actors but freezes routines and admission timers.</summary>
        public void Tick(float dt)
        {
            if (_owner == null || !_owner.isActiveAndEnabled || !_owner.EnabledAtBoot || !_owner.IsOperational ||
                Game.State == GameState.Loading || Game.State == GameState.Title || Game.Player == null || Game.Player.IsDead ||
                (Game.Inner != null && Game.Inner.IsInner))
            { ClearPopulation(); return; }
            if (!Game.IsPlaying || GameTime.Paused || dt <= 0f) return;
            _clock += Mathf.Min(dt, 1f);
            _slice -= dt;
            if (_slice > 0f) return;
            _slice = InhabitedResidentRules04C.SpawnInterval;
            DiscoverSites();
            _selected.Clear();
            Vector3 focus = LivingCity04CQa.FocusOverride ? LivingCity04CQa.FocusPoint : Game.PlayerPosition;
            foreach (Site site in _sites)
            {
                VenueBinding04C venue = site.Venue;
                bool occupied = venue.Definition.Id == _owner.OccupiedId;
                site.Distance = occupied ? -1f : Vector3.Distance(focus, venue.Entrance);
                if (venue.Available && venue.View.gameObject.activeInHierarchy &&
                    InhabitedResidentRules04C.ShouldLoad(site.Loaded, occupied, site.Distance)) _selected.Add(site);
                else Unload(site);
                Maintain(site);
            }
            _selected.Sort(delegate(Site a, Site b)
            {
                int comparison = a.Distance.CompareTo(b.Distance);
                return comparison != 0 ? comparison : string.CompareOrdinal(a.Venue.Definition.Id, b.Venue.Definition.Id);
            });
            int preset = (int)Settings.Graphics;
            int cap = InhabitedResidentRules04C.VenueCap(preset);
            for (int i = _selected.Count - 1; i >= cap; i--) { Unload(_selected[i]); _selected.RemoveAt(i); }
            foreach (Site site in _selected)
            {
                site.Loaded = true;
                UpdateAudio(site);
                for (int slot = InhabitedResidentRules04C.VisitorsPerVenue(preset) + 1; slot < 3; slot++) ReleaseBody(site, slot, false);
            }
            int excess = ResidentCount - (_owner.ResidentsAtBoot ? InhabitedResidentRules04C.ResidentCap(preset) : 0);
            for (int slot = 2; slot >= 0 && excess > 0; slot--)
                for (int i = _selected.Count - 1; i >= 0 && excess > 0; i--)
                    if (_selected[i].Bodies[slot] != null) { ReleaseBody(_selected[i], slot, false); excess--; }
            if (!_owner.ResidentsAtBoot) return;
            if (!CanAdmitResidents()) return;
            // Hosts at all admitted venues are considered before optional visitors. One synchronous body build per slice.
            for (int slot = 0; slot <= InhabitedResidentRules04C.VisitorsPerVenue(preset); slot++)
                foreach (Site site in _selected) if (TrySpawn(site, slot, preset)) return;
        }

        private bool CanAdmitResidents()
        {
            // Only our matching live chapter may bypass mission admission suppression.
            QuestRun chapter = Game.Quests == null ? null : Game.Quests.Running;
            if (chapter != null && chapter.Quest != null && QuietSignal04C.IsChapterQuest(chapter.Quest.Id) &&
                Game.Missions != null && Game.Missions.Active != null && Game.Missions.Active.Def != null &&
                Game.Missions.Active.Def.Id == "quest_" + chapter.Quest.Id) return true;
            return (Game.Peds == null || !Game.Peds.Suppressed) && (Game.Missions == null || Game.Missions.Active == null);
        }

        private void DiscoverSites()
        {
            if (_owner.Venues == null) return;
            foreach (VenueBinding04C venue in _owner.Venues)
            {
                if (venue == null || venue.Definition == null) continue;
                bool found = false;
                foreach (Site site in _sites) if (site.Venue == venue) { found = true; break; }
                if (!found) _sites.Add(new Site { Venue = venue });
            }
        }

        private bool TrySpawn(Site site, int slot, int preset)
        {
            if (site.Venue.Definition.Kind == "depot") return false;
            if (site.Bodies[slot] != null || site.Assigned[slot] || _clock < site.Retry[slot]) return false;
            // 04E: a campaign cast member who stands in for this slot (Nessa at the Lofts door) is never spawned twice.
            if (ResidentReservations04E.Reserved(site.Venue.Definition.Id, slot)) return false;
            int humanoids = 0;
            foreach (NpcController body in NpcController.All) if (body != null && !body.IsCreature) humanoids++;
            int creatures = Game.Creatures != null ? Game.Creatures.Live.Count : 0;
            if (!InhabitedResidentRules04C.CanAllocate(preset, ResidentCount, humanoids, creatures)) return false;
            string anchor = slot == 0 ? "staff" : "visitor" + slot;
            VenueBinding04C venue = site.Venue;
            Vector3 first;
            if (!SafeStation(venue, venue.Point(anchor), out first)) { site.Retry[slot] = _clock + 12f; return false; }
            Vector3 second;
            if (!SafeStation(venue, venue.Point(anchor + "_next"), out second) || !SafeRoute(venue, first, second)) second = first;
            int seed = InhabitedResidentRules04C.Seed(Game.Seed, venue.Definition.Id, slot);
            NpcController npc = null;
            try
            {
                float yaw = venue.View.AnchorYaw(anchor);
                npc = NpcController.SpawnResidence04C(first, yaw, Look(venue.Definition.Kind, seed, slot),
                    "04c/resident/" + venue.Definition.Id + "/" + slot);
                if (npc == null || npc.Rig == null || npc.Rig.Body == null || npc.Rig.Body.sharedMesh == null)
                    throw new InvalidOperationException("Resident mesh initialization was incomplete.");
                npc.transform.SetParent(transform, true);
                npc.Persistent = true;
                npc.IgnoreThreats = true;
                npc.Pacifist = true;
                npc.Courage = 0f;
                npc.Hostile = false;
                npc.DropsLoot = false;
                npc.CashDrop = 0;
                npc.DisplayName = slot == 0 ? HostName(venue.Definition.Kind) : "Neighborhood visitor";
                npc.ConfigureResidence04C(first, second, yaw, venue.View.Contains, venue.View.Root, seed);
                site.Bodies[slot] = npc;
                site.Assigned[slot] = true;
                return true;
            }
            catch (Exception error)
            {
                if (npc != null) { npc.ClearResidence04C(); npc.gameObject.SetActive(false); Destroy(npc.gameObject); }
                site.Retry[slot] = _clock + InhabitedResidentRules04C.InjuredRetry;
                if (!site.Reported) { site.Reported = true; Log.Exception("04C resident " + venue.Definition.Id + ". Service remains available", error); }
                return true; // A failed synchronous body build also consumes this allocation slice.
            }
        }

        private static bool SafeStation(VenueBinding04C venue, Vector3 expected, out Vector3 point)
        {
            point = expected;
            if (venue.View == null || !venue.View.Contains(expected + Vector3.up * 0.2f)) return false;
            RaycastHit hit;
            if (!Physics.Raycast(expected + Vector3.up * 0.35f, Vector3.down, out hit, 0.75f, Layers.WorldMask, QueryTriggerInteraction.Ignore) ||
                !InhabitedResidentRules04C.FloorMatches(expected.y, hit.point.y)) return false;
            point.y = hit.point.y + 0.025f;
            if (Physics.CheckCapsule(point + Vector3.up * 0.4f, point + Vector3.up * 1.75f, 0.35f,
                Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell), QueryTriggerInteraction.Ignore)) return false;
            if (Game.Player != null && Vector3.Distance(Game.PlayerPosition, point) < 1.5f) return false;
            foreach (NpcController other in NpcController.All)
                if (other != null && Vector3.Distance(other.transform.position, point) < 1.1f) return false;
            string[] protectedAnchors = { "exit", "service", "activity" };
            foreach (string id in protectedAnchors)
                if (Vector3.Distance(venue.Point(id), point) < 1.3f) return false;
            return true;
        }

        private static bool SafeRoute(VenueBinding04C venue, Vector3 first, Vector3 second)
        {
            Vector3 delta = second - first;
            float length = delta.magnitude;
            if (Mathf.Abs(first.y - second.y) > 0.1f || length < 1.2f || length > 3f) return false;
            if (Physics.CapsuleCast(first + Vector3.up * 0.4f, first + Vector3.up * 1.75f, 0.35f, delta.normalized, length,
                Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell), QueryTriggerInteraction.Ignore)) return false;
            for (int i = 1; i < 5; i++)
            {
                Vector3 point;
                if (!SafeStation(venue, Vector3.Lerp(first, second, i / 5f), out point)) return false;
            }
            return true;
        }

        private void Maintain(Site site)
        {
            for (int slot = 0; slot < 3; slot++)
            {
                NpcController npc = site.Bodies[slot];
                if (site.Assigned[slot] && npc == null)
                { site.Assigned[slot] = false; site.Retry[slot] = _clock + InhabitedResidentRules04C.InjuredRetry; }
                if (npc == null) continue;
                bool broken = npc.Rig == null || npc.Rig.Body == null || npc.Rig.Body.sharedMesh == null;
                if (broken || (npc.IsDead && Time.time - npc.DeathTime > 25f) ||
                    (!npc.IsDead && !npc.IsRagdolled && site.Venue.View != null && !site.Venue.View.Contains(npc.transform.position + Vector3.up * 0.2f)))
                    ReleaseBody(site, slot, true);
            }
        }

        private void ReleaseBody(Site site, int slot, bool injured)
        {
            NpcController npc = site.Bodies[slot];
            if (npc != null)
            {
                injured |= npc.IsDead || (npc.Health != null && npc.Health.Health < npc.Health.MaxHealth);
                npc.ClearResidence04C();
                npc.OnKilled = null;
                npc.gameObject.SetActive(false);
                Destroy(npc.gameObject);
            }
            site.Bodies[slot] = null;
            site.Assigned[slot] = false;
            if (injured) site.Retry[slot] = _clock + InhabitedResidentRules04C.InjuredRetry;
        }

        private void UpdateAudio(Site site)
        {
            if (!_owner.AmbienceAtBoot)
            {
                ReleaseAudio(site);
                return;
            }
            if (site.Ambience == null && !site.AudioFailed && Game.Audio != null && Game.Audio.Ready)
            {
                GameObject source = null;
                try
                {
                    source = new GameObject("04C room tone " + site.Venue.Definition.Id);
                    source.transform.SetParent(transform, false);
                    source.transform.position = site.Venue.Point("staff") + Vector3.up;
                    site.Ambience = source.AddComponent<AudioSource>();
                    float[] data = InhabitedResidentRules04C.RoomTone(site.Venue.Definition.Kind, AudioSynth.Rate);
                    site.Clip = AudioClip.Create("Original 04C " + site.Venue.Definition.Kind + " room tone", data.Length, 1, AudioSynth.Rate, false);
                    if (!site.Clip.SetData(data, 0)) throw new InvalidOperationException("Room-tone sample upload failed.");
                    site.Ambience.clip = site.Clip;
                    site.Ambience.playOnAwake = false;
                    site.Ambience.loop = true;
                    site.Ambience.spatialBlend = 1f;
                    site.Ambience.minDistance = 2f;
                    site.Ambience.maxDistance = 17f;
                    site.Ambience.rolloffMode = AudioRolloffMode.Logarithmic;
                    site.Ambience.dopplerLevel = 0f;
                    site.Ambience.Play();
                }
                catch (Exception error)
                {
                    site.AudioFailed = true;
                    if (site.Ambience == null && source != null) Destroy(source);
                    ReleaseAudio(site);
                    Log.Exception("04C room audio " + site.Venue.Definition.Id + ". Residents and service remain available", error);
                }
            }
            if (site.Ambience != null)
                site.Ambience.volume = Settings.SfxVolume * (site.Venue.Definition.Id == _owner.OccupiedId ||
                    (LivingCity04CQa.FocusOverride && site.Venue.View.Contains(LivingCity04CQa.FocusPoint)) ? 0.18f : 0f);
        }

        private void Unload(Site site)
        {
            for (int slot = 0; slot < 3; slot++) ReleaseBody(site, slot, false);
            site.Loaded = false;
            ReleaseAudio(site);
        }

        private void ReleaseAudio(Site site)
        {
            if (site.Ambience != null) { site.Ambience.Stop(); Destroy(site.Ambience.gameObject); site.Ambience = null; }
            if (site.Clip != null) { Destroy(site.Clip); site.Clip = null; }
        }

        public void ClearPopulation() { foreach (Site site in _sites) Unload(site); _selected.Clear(); _slice = 0f; }
        /// <summary>Load/new game discards transient health and retry state; ordinary room unload keeps its cooldown.</summary>
        public void ResetForSession() { ClearPopulation(); _sites.Clear(); _clock = 0f; }
        private void OnDisable() { ClearPopulation(); }
        private void OnDestroy() { ClearPopulation(); _sites.Clear(); _owner = null; }

        public static Appearance Look(string kind, int seed, int slot)
        {
            Appearance look = Appearance.Random(new System.Random(seed));
            look.Height = Mathf.Clamp(look.Height, 0.93f, 1.02f);
            look.Glow = 0;
            look.Headwear = Headwear.None;
            look.ShellStyle = 0;
            if (slot == 0)
            {
                if (kind == "lofts") { look.Female = true; look.HairStyle = HairStyle.Bun; }
                look.Jacket = kind != "forge" && kind != "garage";
                look.LongSleeves = true;
                look.SuitStyle = 1;
                look.JacketRow = look.ShirtRow = 1;
                look.JacketColor = look.Shirt = kind == "clinic" ? 10 : kind == "archive" ? 8 : kind == "forge" ? 3 : 5;
            }
            return look;
        }

        private static string HostName(string kind)
        {
            switch (kind)
            {
                case "lofts": return "Nessa Vale";
                case "clinic": return "Clinic care assistant";
                case "forge": return "Workshop assistant";
                case "archive": return "Archive desk assistant";
                case "observatory": return "Survey attendant";
                case "garage": return "Bay service assistant";
                case "inn": return "Diner service assistant";
                default: return "House service assistant";
            }
        }
    }
}
