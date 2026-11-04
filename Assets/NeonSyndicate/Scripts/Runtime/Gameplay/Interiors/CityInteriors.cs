using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Saved interior progress (append-only block SaveData.Interiors; old saves start with nothing visited).</summary>
    [Serializable]
    public sealed class InteriorSave
    {
        /// <summary>Ids (InteriorRules.IdFor) of the interiors the player has walked into.</summary>
        public List<string> Visited = new List<string>();
        /// <summary>True once the reward for visiting every interior was paid.</summary>
        public bool RewardPaid;
    }

    /// <summary>
    /// Generated city interiors (Game.Interiors; blueprint: enterable interiors). InteriorRules picks 28 ordinary buildings
    /// and gives each a lit street door with a sign and a map marker. The use key (E, pad Y) at the door fades out and
    /// takes the player into a room built from InteriorRooms high above that building (so the map and every distance
    /// stay right), with its keeper and a few customers; the door inside leads back to the street. A room exists only
    /// while the player is in it. Rooms count as a roofed interior for the weather, the camera, the Veil and the world
    /// region. Partial files: Room (geometry, people, the counter) and Ui (the F2 page).
    /// </summary>
    public sealed partial class CityInteriors : MonoBehaviour, IWorldSystem
    {
        private sealed class Door
        {
            public InteriorSite Site;
            public GameObject Go;
            public Blip Blip;
        }

        private readonly List<InteriorSite> _sites = new List<InteriorSite>();
        private readonly List<Door> _doors = new List<Door>();
        private readonly HashSet<string> _visited = new HashSet<string>(StringComparer.Ordinal);
        private bool _rewardPaid;
        private bool _active;
        private bool _busy;
        private InteriorSite _current;
        private Transform _doorRoot;

        /// <summary>Every selected interior (also those whose door could not be placed).</summary>
        public IReadOnlyList<InteriorSite> Sites { get { return _sites; } }
        /// <summary>True while the player is in a generated room.</summary>
        public bool Inside { get { return _current != null && _room != null; } }
        /// <summary>The interior the player is in, or null.</summary>
        public InteriorSite Current { get { return _current; } }
        /// <summary>How many interiors have a door on the street.</summary>
        public int DoorCount { get { return _doors.Count; } }
        public int VisitedCount { get { return _visited.Count; } }
        public bool Visited(InteriorSite s) { return s != null && _visited.Contains(s.Id); }

        /// <summary>Picks the buildings (venue buildings excluded). The doors go up in ActivateWorld once the city is built.</summary>
        public void Init(CityLayout layout)
        {
            HashSet<Building> venues = new HashSet<Building>();
            if (Game.Inhabited != null)
                foreach (VenueBinding04C v in Game.Inhabited.Venues)
                    if (v != null && v.Building != null) venues.Add(v.Building);
            _sites.Clear();
            _sites.AddRange(InteriorRules.Select(layout, venues));
            WorldSystems.Register(this);
            RegisterPage();
            Log.Info("City interiors: " + _sites.Count + " interiors chosen across the districts.");
        }

        /// <summary>Puts a door on each chosen building's real facade (found by a short ray from the sidewalk).</summary>
        public void ActivateWorld()
        {
            if (_active) return;
            _active = true;
            Physics.SyncTransforms();
            _doorRoot = new GameObject("Interior doors").transform;
            _doorRoot.SetParent(transform, false);
            int skipped = 0;
            foreach (InteriorSite site in _sites)
            {
                try
                {
                    if (!PlaceDoor(site)) skipped++;
                }
                catch (Exception e)
                {
                    skipped++;
                    Log.Exception("Interior door " + site.Name, e);
                }
            }
            Log.Info("City interiors: " + _doors.Count + " street doors placed" + (skipped > 0 ? ", " + skipped + " skipped (no facade found at the door)" : "") + ".");
        }

        /// <summary>
        /// Finds the facade by a ray from the sidewalk into the building at door height; a door that finds no wall within
        /// reach (an arcade, a setback, a different building shape) is skipped rather than left floating.
        /// </summary>
        private bool PlaceDoor(InteriorSite site)
        {
            Vector3 inward = MathUtil.DirFromYaw(site.Yaw);
            Vector3 from = site.Outside + Vector3.up * 1.3f - inward * 1f;
            RaycastHit hit;
            if (!Physics.Raycast(from, inward, out hit, 9f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (Vector3.Dot(hit.normal, -inward) < 0.7f) return false;
            Vector3 ground = PedManager.Ground(new Vector3(hit.point.x, site.Door.y + 1f, hit.point.z) - inward * 0.6f);
            Vector3 face = new Vector3(hit.point.x, ground.y, hit.point.z);
            site.Door = face;
            site.Outside = face - inward * 2.2f;
            site.RoomOrigin = new Vector3(face.x, InteriorRules.RoomAltitude, face.z);
            GameObject go = BuildDoor(site, face, inward);
            InteriorSite s = site;
            Interactable use = Interactable.Create("Interior door " + site.Name, face - inward * 0.9f + Vector3.up * 0.2f, 2.3f, "", () => TryEnter(s), go.transform);
            use.DynamicPrompt = () => DoorPrompt(s);
            Blip blip = Blip.Add(BlipKind.Service, null, face - inward * 0.5f, NeonColor(site.Kind), "I", site.Name + " (" + InteriorRules.KindName(site.Kind) + ")", false);
            blip.ShowOnMinimap = true;
            _doors.Add(new Door { Site = site, Go = go, Blip = blip });
            return true;
        }

        private static Color NeonColor(InteriorKind kind)
        {
            Material m = Mats.Neon(InteriorRules.NeonIndex(kind));
            return m != null && m.HasProperty("_Color") ? m.color * 1.6f : Color.cyan;
        }

        /// <summary>The street door: a dark recessed slab, a lit frame in the kind's colour, a sign with the name and a small lamp.</summary>
        private GameObject BuildDoor(InteriorSite site, Vector3 face, Vector3 inward)
        {
            GameObject go = new GameObject("Interior door " + site.Name);
            go.transform.SetParent(_doorRoot, false);
            go.transform.SetPositionAndRotation(face, Quaternion.LookRotation(-inward, Vector3.up));
            Transform t = go.transform;
            Material frame = Mats.Neon(InteriorRules.NeonIndex(site.Kind));
            Shapes.Box("Door slab", t, new Vector3(0f, 1.3f, 0.03f), new Vector3(1.6f, 2.6f, 0.08f), Mats.Solid(new Color(0.07f, 0.06f, 0.07f), 0.35f), false);
            Shapes.Box("Door frame top", t, new Vector3(0f, 2.68f, 0.09f), new Vector3(1.9f, 0.14f, 0.1f), frame, false);
            Shapes.Box("Door frame left", t, new Vector3(-0.88f, 1.3f, 0.09f), new Vector3(0.14f, 2.6f, 0.1f), frame, false);
            Shapes.Box("Door frame right", t, new Vector3(0.88f, 1.3f, 0.09f), new Vector3(0.14f, 2.6f, 0.1f), frame, false);
            Shapes.Box("Step", t, new Vector3(0f, 0.04f, 0.45f), new Vector3(2f, 0.08f, 0.8f), Mats.Concrete, false);
            Shapes.Box("Sign board", t, new Vector3(0f, 3.25f, 0.08f), new Vector3(Mathf.Max(2.6f, site.Name.Length * 0.17f), 0.55f, 0.06f), Mats.Solid(new Color(0.05f, 0.05f, 0.06f), 0.4f), false);
            Font font = Mats.UIFont;
            if (font != null)
            {
                GameObject text = new GameObject("Sign text");
                text.transform.SetParent(t, false);
                text.transform.localPosition = new Vector3(0f, 3.25f, 0.13f);
                text.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                TextMesh tm = text.AddComponent<TextMesh>();
                tm.font = font;
                tm.text = site.Name.ToUpperInvariant();
                tm.fontSize = 64;
                tm.characterSize = 0.045f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = NeonColor(site.Kind);
                MeshRenderer mr = text.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = font.material;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            Light lamp = new GameObject("Door lamp").AddComponent<Light>();
            lamp.transform.SetParent(t, false);
            lamp.transform.localPosition = new Vector3(0f, 2.9f, 0.7f);
            lamp.type = LightType.Point;
            lamp.range = 5f;
            lamp.intensity = 1.2f;
            lamp.color = NeonColor(site.Kind);
            lamp.shadows = LightShadows.None;
            return go;
        }

        private string DoorPrompt(InteriorSite site)
        {
            string why;
            string label = site.Name + " (" + InteriorRules.KindName(site.Kind) + ")";
            if (!CanEnterNow(out why)) return label + ": " + why;
            return "Press E to go into " + label + (Visited(site) ? "" : " (new)");
        }

        private bool CanEnterNow(out string why)
        {
            PlayerController p = Game.Player;
            bool alive = p != null && !p.IsDead && !p.IsRagdolled;
            int stars = Game.Wanted != null ? Game.Wanted.Stars : 0;
            bool veil = Game.Inner != null && Game.Inner.IsInner;
            return InteriorRules.CanEnter(p != null && !p.InVehicle, alive, stars, CombatDirector.InCombat, veil, out why);
        }

        private void TryEnter(InteriorSite site)
        {
            string why;
            if (_busy || Inside) return;
            if (!CanEnterNow(out why))
            {
                if (Game.Hud != null) Game.Hud.Notify(why, 2.5f);
                return;
            }
            StartCoroutine(Enter(site));
        }

        private IEnumerator Enter(InteriorSite site)
        {
            _busy = true;
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.25f);
            yield return new WaitForSecondsRealtime(0.3f);
            try
            {
                BuildRoom(site);
                _current = site;
                Vector3 entry = RoomPoint(site, _plan.Entry);
                if (Game.Player != null) Game.Player.Teleport(entry, site.Yaw);
                FirstVisit(site);
            }
            catch (Exception e)
            {
                Log.Exception("Interior " + site.Name, e);
                ClearRoom();
                _current = null;
            }
            if (Game.Hud != null) Game.Hud.FadeTo(0f, 0.35f);
            _busy = false;
        }

        /// <summary>Leaves through the room's door: fade, back to the sidewalk facing the street, the room goes away.</summary>
        public void Leave()
        {
            if (_busy || _current == null) return;
            StartCoroutine(Exit());
        }

        private IEnumerator Exit()
        {
            _busy = true;
            InteriorSite site = _current;
            if (Game.Hud != null) Game.Hud.FadeTo(1f, 0.25f);
            yield return new WaitForSecondsRealtime(0.3f);
            if (Game.Player != null && site != null) Game.Player.Teleport(site.Outside, site.Yaw + 180f);
            ClearRoom();
            _current = null;
            if (Game.Hud != null) Game.Hud.FadeTo(0f, 0.35f);
            _busy = false;
        }

        private void FirstVisit(InteriorSite site)
        {
            if (!_visited.Add(site.Id))
            {
                if (Game.Hud != null) Game.Hud.Notify(site.Name, 2f);
                return;
            }
            if (Game.Hud != null) Game.Hud.Notify("Found: " + site.Name + " (" + _visited.Count + " of " + _doors.Count + " places)", 3.5f);
            if (!_rewardPaid && _doors.Count > 0 && _visited.Count >= _doors.Count && Game.Economy != null)
            {
                _rewardPaid = true;
                Game.Economy.Earn(InteriorRules.AllVisitedReward, "Every city interior visited");
                if (Game.Hud != null) Game.Hud.Notify("You know every door in Las Veyra: $" + InteriorRules.AllVisitedReward, 5f);
            }
        }

        private void Update()
        {
            if (_current == null || _busy) return;
            // Anything that moves the player away (death, a mission start, a load, a fast travel) closes the room quietly.
            if (Game.Player == null || Game.PlayerPosition.y < InteriorRules.RoomAltitude - 30f || MathUtil.FlatDistance(Game.PlayerPosition, _current.RoomOrigin) > 60f)
            {
                ClearRoom();
                _current = null;
            }
        }

        // ------------------------------------------------------------------ IWorldSystem

        public void ResetAll()
        {
            _visited.Clear();
            _rewardPaid = false;
            ClearRoom();
            _current = null;
        }

        public void Capture(SaveData d)
        {
            if (d.Interiors == null) d.Interiors = new InteriorSave();
            d.Interiors.Visited = new List<string>(_visited);
            d.Interiors.RewardPaid = _rewardPaid;
            // A save made in a room resumes on the sidewalk outside its door, never in the sky.
            if (_current != null && d.PosY > InteriorRules.RoomAltitude * 0.5f)
            {
                d.PosX = _current.Outside.x;
                d.PosY = _current.Outside.y;
                d.PosZ = _current.Outside.z;
                d.Yaw = _current.Yaw + 180f;
            }
        }

        public void Restore(SaveData d)
        {
            _visited.Clear();
            _rewardPaid = false;
            if (d == null || d.Interiors == null) return;
            if (d.Interiors.Visited != null) foreach (string id in d.Interiors.Visited) if (!string.IsNullOrEmpty(id)) _visited.Add(id);
            _rewardPaid = d.Interiors.RewardPaid;
        }

        private void OnDestroy()
        {
            ClearRoom();
            foreach (Door door in _doors) Blip.Remove(door.Blip);
            _doors.Clear();
        }
    }
}
