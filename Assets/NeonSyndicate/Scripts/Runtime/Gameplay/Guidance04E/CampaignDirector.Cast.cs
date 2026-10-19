using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// CampaignDirector, the cast: each named person's post (a venue host station, the Lofts door, an Underheart anchor
    /// or a city place), streaming within about 70 m (always while a quest reserves them), the 04C host slot they keep
    /// so nobody appears twice, an honestly labeled message terminal when a body cannot be built, and the acknowledgement
    /// on approach (head turn, one wave, at most one line, then a long cooldown).
    /// </summary>
    public sealed partial class CampaignDirector
    {
        private sealed class CastSlot
        {
            public CastDef04E Def;
            /// <summary>Binding was attempted this session (BindAt: when to try again while unbound).</summary>
            public bool Bound;
            public float BindAt;
            public bool PostBound;
            public Vector3 Post;
            public float PostYaw;
            public string PostName = "";
            /// <summary>Why the post is not bound (shown on STATUS, the map card and CastAvailable).</summary>
            public string PostNote = "";
            public bool Indoors;
            public VenueBinding04C Venue;
            public Func<Vector3, bool> Contains;
            /// <summary>The venue host slot this person keeps while present (-1 for none).</summary>
            public string ReservedVenue = "";
            public int ReservedSlot = -1;
            public bool RemoteBound;
            public Vector3 RemotePoint;
            public float RemoteYaw;
            public NpcController Actor;
            public GameObject Terminal;
            public CastMarker04E Marker;
            public Interactable Talk;
            public bool Spawning;
            public int Failures;
            public float RetryAt;
            public bool Reported;
            public string SpawnFailure = "";
            public readonly HashSet<string> Reservations = new HashSet<string>();
            // Giver state, refreshed twice a second (CampaignDirector.Givers).
            public GiverState State;
            public string StateReason = "";
            public QuestDef Offer;
            public bool OfferResume;
            /// <summary>An on-demand person (Ion in person, June) is needed now by an offered or running quest.</summary>
            public bool Needed;
            // Acknowledgement.
            public float LastGreet = -1f;
            public bool WalkedAway = true;
            public bool HasLos;
            public float LosTimer;
            public float FaceYaw;
            public bool FacingPlayer;
            public float NearTime;

            public bool Present { get { return Actor != null || Terminal != null; } }
        }

        private readonly List<CastSlot> _slots = new List<CastSlot>();
        private readonly Dictionary<string, CastSlot> _slotById = new Dictionary<string, CastSlot>();
        private readonly List<NpcController> _dupes = new List<NpcController>();
        private float _castTimer;
        private float _reserveTimer;
        private float _reportTimer;
        private int _reportKey = -1;

        private void InitCast()
        {
            _slots.Clear();
            _slotById.Clear();
            foreach (CastDef04E d in CastRoster04E.All)
            {
                CastSlot s = new CastSlot { Def = d };
                _slots.Add(s);
                _slotById[d.Id] = s;
            }
        }

        private CastSlot Slot(string castId)
        {
            CastSlot s;
            return castId != null && _slotById.TryGetValue(castId, out s) ? s : null;
        }

        // ================================================================== posts

        private void EnsureBound(CastSlot s)
        {
            if (s.Bound && (s.PostBound || Time.time < s.BindAt)) return;
            try { BindPost(s); }
            catch (Exception e)
            {
                s.PostBound = false;
                s.PostNote = "the post could not be bound (" + e.Message + ")";
                LogOnce("bind " + s.Def.Id, e);
            }
            s.Bound = true;
            s.BindAt = Time.time + 10f;
        }

        private void BindPost(CastSlot s)
        {
            s.PostBound = false;
            s.PostNote = "";
            s.Venue = null;
            s.Contains = null;
            s.ReservedVenue = "";
            s.ReservedSlot = -1;
            s.Indoors = false;
            switch (s.Def.Post)
            {
                case CastPostKind.VenueStaff: BindVenueStaff(s); break;
                case CastPostKind.VenueDoorOutside: BindDoorOutside(s); break;
                case CastPostKind.UnderAnchor: BindUnder(s); break;
                case CastPostKind.PoiDoor:
                    if (!BindPoiDoor(s, s.Def.Poi)) s.PostNote = "the place \"" + s.Def.Poi + "\" is not on this map";
                    break;
            }
            BindRemote(s);
            s.FaceYaw = s.PostYaw;
        }

        private void BindVenueStaff(CastSlot s)
        {
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(s.Def.Venue) : null;
            string note;
            if (v != null && v.Available && VenueHasAnchor(v, s.Def.Anchor))
            {
                Vector3 p = v.Point(s.Def.Anchor);
                Vector3 g;
                if (FloorAt(p, 0.6f, 1.6f, out g) && Mathf.Abs(g.y - p.y) < 0.45f && v.View.Contains(g + Vector3.up * 0.2f))
                {
                    s.Post = g;
                    s.PostYaw = v.View.AnchorYaw(s.Def.Anchor);
                    s.PostBound = true;
                    s.Indoors = true;
                    s.Venue = v;
                    s.Contains = v.View.Contains;
                    s.ReservedVenue = s.Def.Venue;
                    s.ReservedSlot = 0;
                    s.PostName = VenueTitle(s.Def.Venue) + (p.y - v.Entrance.y > RoutePlan04E.UpperFloorThreshold ? ", upper floor" : "");
                    return;
                }
                note = "the host station in " + VenueTitle(s.Def.Venue) + " has no clear floor";
            }
            else if (v == null) note = VenueTitle(s.Def.Venue) + " is not in this build";
            else if (!v.Available) note = VenueTitle(s.Def.Venue) + " did not load (" + v.Status + ")";
            else note = VenueTitle(s.Def.Venue) + " has no \"" + s.Def.Anchor + "\" station";
            if (!string.IsNullOrEmpty(s.Def.Poi) && BindPoiDoor(s, s.Def.Poi))
            {
                s.PostName = "outside " + s.Def.Poi + " (" + note + ")";
                return;
            }
            s.PostNote = note;
        }

        /// <summary>
        /// Nessa: the arrival scene's planned post first (on the Lofts forecourt beside her dispatch board, facing the
        /// arrival; ArrivalScene04E.TryNessaPost), verified for floor and room here; otherwise a sidewalk spot beside the
        /// Veyra Lofts door, on the side toward the arrival point, facing it.
        /// </summary>
        private void BindDoorOutside(CastSlot s)
        {
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(s.Def.Venue) : null;
            if (v != null && v.Building != null && _layout != null && s.Def.Id == CastIds04E.Nessa && BindPlannedNessaPost(s, v)) return;
            if (v != null && v.Building != null && _layout != null)
            {
                Vector3 outward = MathUtil.Flat(v.Outward);
                outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
                Vector3 right = new Vector3(outward.z, 0f, -outward.x);
                Vector3 arrival = v.Poi != null ? GameRoot.SpawnOutside(v.Poi) : v.Outside + outward * 3f;
                float prefer = Vector3.Dot(arrival - v.Entrance, right) >= 0f ? 1f : -1f;
                float[] laterals = { 2.6f, 3.2f, 3.8f, 2.3f, 4.4f };
                float[] forwards = { 1.1f, 1.7f, 0.7f, 2.3f };
                for (int side = 0; side < 2; side++)
                {
                    float sign = side == 0 ? prefer : -prefer;
                    foreach (float lat in laterals)
                    foreach (float fwd in forwards)
                    {
                        Vector3 c = v.Entrance + outward * fwd + right * (lat * sign);
                        Vector3 g;
                        if (!OutdoorSpotOk(c, out g)) continue;
                        s.Post = g;
                        Vector3 toArrival = MathUtil.Flat(arrival - g);
                        s.PostYaw = toArrival.sqrMagnitude > 2f ? MathUtil.YawOf(toArrival.normalized) : MathUtil.YawOf(outward);
                        s.PostBound = true;
                        s.Venue = v;
                        s.ReservedVenue = s.Def.Venue;
                        s.ReservedSlot = 0;
                        s.PostName = "the " + VenueTitle(s.Def.Venue) + " door";
                        Vector3 post = g;
                        s.Contains = p => MathUtil.FlatSqrDistance(p, post) < 9f && Mathf.Abs(p.y - post.y) < 2f;
                        return;
                    }
                }
                s.PostNote = "no clear sidewalk spot beside the " + VenueTitle(s.Def.Venue) + " door";
            }
            if (!string.IsNullOrEmpty(s.Def.Poi) && BindPoiDoor(s, s.Def.Poi))
            {
                s.PostName = "outside " + s.Def.Poi;
                s.ReservedVenue = s.Def.Venue;
                s.ReservedSlot = 0;
                return;
            }
            if (s.PostNote.Length == 0) s.PostNote = VenueTitle(s.Def.Venue) + " is not on this map";
        }

        /// <summary>The arrival scene's post for Nessa when it was built and the spot is still clear standing ground.</summary>
        private bool BindPlannedNessaPost(CastSlot s, VenueBinding04C v)
        {
            Vector3 planned;
            float plannedYaw;
            if (!ArrivalScene04E.TryNessaPost(out planned, out plannedYaw)) return false;
            Vector3 g;
            if (!MathUtil.IsFinite(planned) || !OutdoorSpotOk(planned, out g) || Mathf.Abs(g.y - planned.y) > 0.5f) return false;
            s.Post = g;
            s.PostYaw = plannedYaw;
            s.PostBound = true;
            s.Venue = v;
            s.ReservedVenue = s.Def.Venue;
            s.ReservedSlot = 0;
            s.PostName = "the " + VenueTitle(s.Def.Venue) + " door";
            Vector3 post = g;
            s.Contains = p => MathUtil.FlatSqrDistance(p, post) < 9f && Mathf.Abs(p.y - post.y) < 2f;
            return true;
        }

        private void BindUnder(CastSlot s)
        {
            UnderheartWorld u = Game.Underheart;
            if (u == null) { s.PostNote = "the Underheart did not start"; return; }
            if (!u.Ready) { s.PostNote = "the Underheart is not built (" + u.Status + ")"; return; }
            Vector3 p;
            float yaw;
            if (!u.TryAnchor(s.Def.Anchor, out p, out yaw)) { s.PostNote = "the Underheart has no " + s.Def.Anchor + " anchor"; return; }
            Vector3 g;
            s.Post = FloorAt(p, 0.6f, 2f, out g) ? g : p;
            s.PostYaw = yaw;
            s.PostBound = true;
            s.PostName = UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(s.Def.Anchor)) + " (underground)";
            Vector3 post = s.Post;
            s.Contains = q => MathUtil.FlatSqrDistance(q, post) < 9f && Mathf.Abs(q.y - post.y) < 2f;
        }

        private bool BindPoiDoor(CastSlot s, string poiName)
        {
            if (_layout == null || string.IsNullOrEmpty(poiName)) return false;
            Poi poi = _layout.FindPoi(poiName);
            Vector3 p;
            if (poi == null || !QuestPlaces.GiverPosition(_layout, poiName, 0f, 0f, out p)) return false;
            Vector3 g = QuestWorld.OpenGround(p, poi.Position);
            s.Post = g;
            Vector3 f = MathUtil.Flat(poi.Facing);
            s.PostYaw = f.sqrMagnitude > 0.01f ? MathUtil.YawOf(f.normalized) : 0f;
            s.PostBound = true;
            s.Indoors = false;
            s.PostName = "outside " + poiName;
            Vector3 post = g;
            s.Contains = q => MathUtil.FlatSqrDistance(q, post) < 9f && Mathf.Abs(q.y - post.y) < 2f;
            return true;
        }

        private void BindRemote(CastSlot s)
        {
            s.RemoteBound = false;
            if (string.IsNullOrEmpty(s.Def.RemoteVenue)) return;
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(s.Def.RemoteVenue) : null;
            if (v == null || !v.Available || !VenueHasAnchor(v, s.Def.RemoteAnchor)) return;
            s.RemotePoint = v.Point(s.Def.RemoteAnchor);
            s.RemoteYaw = v.View.AnchorYaw(s.Def.RemoteAnchor);
            s.RemoteBound = true;
        }

        private static bool VenueHasAnchor(VenueBinding04C v, string anchor)
        {
            if (v == null || v.View == null || v.View.Composition == null || v.View.Composition.anchors == null || string.IsNullOrEmpty(anchor)) return false;
            foreach (VenueAnchor04C a in v.View.Composition.anchors) if (a != null && a.id == anchor) return true;
            return false;
        }

        /// <summary>The floor under a point (a short ray from just above it), on the World layers.</summary>
        private static bool FloorAt(Vector3 p, float up, float down, out Vector3 ground)
        {
            ground = p;
            RaycastHit hit;
            if (!Physics.Raycast(p + Vector3.up * up, Vector3.down, out hit, up + down, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.8f) return false;
            ground = hit.point + Vector3.up * 0.02f;
            return true;
        }

        /// <summary>A sidewalk spot a person can stand on: flat ground at curb height, not on a road, not inside a
        /// building, nothing solid in a person's capsule there (props, the arrival board, parked bikes).</summary>
        private bool OutdoorSpotOk(Vector3 c, out Vector3 ground)
        {
            ground = c;
            RaycastHit hit;
            if (!Physics.Raycast(c + Vector3.up * 2f, Vector3.down, out hit, 4f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.85f || Mathf.Abs(hit.point.y - CityLayout.CurbHeight) > 0.5f) return false;
            if (_layout.IsInsideBuilding(hit.point, 0.3f) || _layout.RoadAt(hit.point, 0.6f) != null) return false;
            int mask = Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.VehicleShell) | (1 << Layers.Vehicles);
            if (Physics.CheckCapsule(hit.point + Vector3.up * 0.45f, hit.point + Vector3.up * 1.7f, 0.36f, mask, QueryTriggerInteraction.Ignore)) return false;
            ground = hit.point + Vector3.up * 0.02f;
            return true;
        }

        /// <summary>The readable title of a 04C or 04E venue id (the live title when the venue is bound).</summary>
        private static string VenueTitle(string venueId)
        {
            VenueBinding04C v = Game.Inhabited != null ? Game.Inhabited.FindVenue(venueId) : null;
            if (v != null && v.Definition != null && !string.IsNullOrEmpty(v.Definition.Title)) return v.Definition.Title;
            switch (venueId)
            {
                case VenueIds04E.Dispatch: return "Cinder Dispatch";
                case VenueIds04E.Pressure: return "The Pressure House";
                case VenueIds04E.Bondkeeper: return "Bondkeeper's Refuge";
                case VenueIds04E.Office: return "The Resonance Office";
                case "lofts": return "Veyra Lofts";
                case "clinic": return "Mara Solane Street Clinic";
                case "forge": return "Dante Rook's Crystal Forge";
                case "garage": return "Jax Calder Customs";
                case "archive": return "Resonance Archive";
                case "observatory": return "Sundial Survey Observatory";
                case "inn": return "Cinder Diner";
                default: return string.IsNullOrEmpty(venueId) ? "the venue" : venueId;
            }
        }

        // ================================================================== presence rules

        private static bool NonCampaignMissionActive(out string title)
        {
            title = "";
            if (Game.Missions == null || Game.Missions.Active == null) return false;
            QuestRun run = Game.Quests != null ? Game.Quests.Running : null;
            if (run != null && run.C == Game.Missions.Active && QuestMeta04E.IsCampaign(run.Quest)) return false;
            title = Game.Missions.Active.Def != null ? Game.Missions.Active.Def.Title : "the current mission";
            return true;
        }

        /// <summary>Why a person cannot be present now ("" when they can).</summary>
        private string GateReason(CastSlot s)
        {
            bool freeRoam = Game.Root != null && Game.Root.FreeRoam;
            int story = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            if (!freeRoam && s.Def.StoryUnlock > 0 && story < s.Def.StoryUnlock)
                return s.Def.Name + " has not been met in the story yet (after story mission " + s.Def.StoryUnlock + ").";
            if (Game.Inner != null && Game.Inner.IsInner) return s.Def.Name + " is not inside the Veil.";
            if (s.Def.OnDemand && !s.Needed && s.Reservations.Count == 0) return s.Def.RemoteNote;
            string mission;
            if (s.Def.StoryIdentity && s.Reservations.Count == 0 && NonCampaignMissionActive(out mission))
                return s.Def.Name + " stays out of sight while \"" + mission + "\" runs.";
            if (!s.PostBound) return s.PostNote.Length > 0 ? Capitalize(s.PostNote) + "." : "Their post is not bound.";
            return "";
        }

        private static string Capitalize(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private bool IsResidentSlotReserved(string venueId, int slot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                CastSlot s = _slots[i];
                if (s.ReservedSlot == slot && s.ReservedVenue == venueId && (s.Actor != null || s.Terminal != null || s.Spawning)) return true;
            }
            return false;
        }

        // ================================================================== streaming

        private void TickCast(float dt)
        {
            TickActors(dt);
            _castTimer -= dt;
            if (_castTimer > 0f) return;
            _castTimer = 0.35f;
            Vector3 player = Game.PlayerPosition;
            bool spawned = false;
            for (int i = 0; i < _slots.Count; i++)
            {
                CastSlot s = _slots[i];
                EnsureBound(s);
                Maintain(s);
                bool reserved = s.Reservations.Count > 0;
                bool want = GateReason(s).Length == 0
                    && GuidanceRules04E.ShouldStream(GuidanceRules04E.LayerDistance(player, s.Post), s.Present, reserved, GuidanceRules04E.StreamOutFor(s.ReservedSlot >= 0));
                if (!want)
                {
                    if (s.Present) Despawn(s);
                    continue;
                }
                if (spawned) continue;
                if (!s.Present && Time.time >= s.RetryAt) { TrySpawn(s); spawned = true; }
                else if (s.Terminal != null && s.Actor == null && s.Failures < 3 && Time.time >= s.RetryAt) { TrySpawn(s); spawned = true; }
            }
            _reserveTimer -= 0.35f;
            if (_reserveTimer <= 0f)
            {
                _reserveTimer = 2f;
                DropStaleReservations();
            }
            _reportTimer -= 0.35f;
            if (_reportTimer <= 0f)
            {
                _reportTimer = 3f;
                int key = 0;
                foreach (CastSlot s in _slots) key = key * 7 + (s.PostBound ? 1 : 0) + (s.Actor != null ? 2 : 0) + (s.Terminal != null ? 4 : 0);
                if (key != _reportKey)
                {
                    _reportKey = key;
                    ReportModule();
                }
            }
        }

        /// <summary>A reservation lives only while its quest is running or accepted; anything else is released.</summary>
        private void DropStaleReservations()
        {
            QuestBook book = Game.Quests;
            string running = book != null && book.Running != null && book.Running.Quest != null ? book.Running.Quest.Id : "";
            string active = book != null ? book.ActiveId : "";
            List<string> drop = null;
            foreach (CastSlot s in _slots)
            {
                if (s.Reservations.Count == 0) continue;
                foreach (string q in s.Reservations)
                {
                    if (q == running || q == active) continue;
                    if (drop == null) drop = new List<string>();
                    drop.Add(q);
                }
                if (drop == null) continue;
                foreach (string q in drop) s.Reservations.Remove(q);
                drop.Clear();
            }
        }

        private void Maintain(CastSlot s)
        {
            if (s.Actor == null)
            {
                // Destroyed by something else (scene cleanup): forget the references so the slot can respawn.
                if (!ReferenceEquals(s.Actor, null)) { s.Actor = null; s.Marker = null; s.Talk = null; }
                return;
            }
            if (s.Actor.IsDead)
            {
                Despawn(s);
                s.RetryAt = Time.time + 30f;
                return;
            }
            if (s.Actor.IsRagdolled) return;
            Vector3 p = s.Actor.transform.position;
            if (MathUtil.FlatDistance(p, s.Post) > 3f || Mathf.Abs(p.y - s.Post.y) > 1.5f)
            {
                // Knocked away from the post: return while nobody is looking, never in view.
                bool seen = s.Actor.Rig != null && s.Actor.Rig.Body != null && s.Actor.Rig.Body.isVisible;
                if (!seen)
                {
                    s.Actor.transform.position = s.Post;
                    if (s.Actor.Anim != null) s.Actor.Anim.ResetFeet();
                }
            }
        }

        private void TrySpawn(CastSlot s)
        {
            s.Spawning = true;
            NpcController npc = null;
            try
            {
                RemoveDuplicateResident(s);
                Appearance look = CastRoster04E.Look(s.Def.Id);
                npc = NpcController.SpawnResidence04C(s.Post, s.PostYaw, look, "04e/cast/" + s.Def.Id);
                if (npc == null || npc.Rig == null || npc.Rig.Body == null || npc.Rig.Body.sharedMesh == null)
                    throw new InvalidOperationException("the body mesh did not build");
                // Named people get the detailed body unless the Low preset asks for less.
                if (Settings.Graphics != GraphicsPreset.Low && npc.Rig.Detail < 2)
                {
                    npc.Rig.Detail = 2;
                    npc.Rig.ChangeLook(look);
                }
                npc.transform.SetParent(_castRoot, true);
                npc.Persistent = true;
                npc.IgnoreThreats = true;
                npc.Pacifist = true;
                npc.Courage = 0f;
                npc.Hostile = false;
                npc.DropsLoot = false;
                npc.CashDrop = 0;
                npc.DisplayName = s.Def.Name;
                if (npc.Health != null) npc.Health.Invulnerable = true;
                Func<Vector3, bool> contains = s.Contains;
                if (contains == null)
                {
                    Vector3 post = s.Post;
                    contains = q => MathUtil.FlatSqrDistance(q, post) < 9f && Mathf.Abs(q.y - post.y) < 2f;
                }
                npc.ConfigureResidence04C(s.Post, s.Post, s.PostYaw, contains, _castRoot, CastSeed(s));
                try { CastCostumeBuilder04E.Dress(npc.Rig, s.Def.Costume); }
                catch (Exception e) { LogOnce("costume " + s.Def.Id, e); }
                if (s.Terminal != null)
                {
                    Destroy(s.Terminal);
                    s.Terminal = null;
                }
                s.Marker = CastMarker04E.Create(npc.transform, npc.Rig.Scale * 1.86f + 0.45f);
                s.Talk = CreateTalk(s, npc.transform);
                s.Actor = npc;
                s.FaceYaw = s.PostYaw;
                s.FacingPlayer = false;
                s.Failures = 0;
                s.SpawnFailure = "";
                s.State = GiverState.None;
            }
            catch (Exception e)
            {
                if (npc != null)
                {
                    npc.ClearResidence04C();
                    npc.gameObject.SetActive(false);
                    Destroy(npc.gameObject);
                }
                s.Failures++;
                s.SpawnFailure = "the body could not be built (" + e.Message + ")";
                s.RetryAt = Time.time + 20f;
                if (!s.Reported)
                {
                    s.Reported = true;
                    Log.Exception("Cast actor " + s.Def.Id + ". A labeled message terminal stands in", e);
                }
                if (s.Terminal == null) BuildTerminal(s);
            }
            finally
            {
                s.Spawning = false;
            }
        }

        private static int CastSeed(CastSlot s)
        {
            int h = 17;
            foreach (char c in s.Def.Id) h = h * 31 + c;
            return Mathf.Abs(h % 997);
        }

        /// <summary>A 04C host already standing in the reserved slot (from before the person arrived) leaves first.</summary>
        private void RemoveDuplicateResident(CastSlot s)
        {
            if (s.ReservedSlot < 0 || string.IsNullOrEmpty(s.ReservedVenue)) return;
            string tag = "04c/resident/" + s.ReservedVenue + "/" + s.ReservedSlot;
            _dupes.Clear();
            foreach (NpcController n in NpcController.All) if (n != null && n.MissionTag == tag) _dupes.Add(n);
            foreach (NpcController n in _dupes)
            {
                n.ClearResidence04C();
                n.gameObject.SetActive(false);
                Destroy(n.gameObject);
            }
            _dupes.Clear();
        }

        private void BuildTerminal(CastSlot s)
        {
            try
            {
                s.Terminal = MessageTerminal04E.Build(s.Post, s.PostYaw, s.Def.Name, _castRoot);
                s.Marker = CastMarker04E.Create(s.Terminal.transform, 2.05f);
                s.Talk = CreateTalk(s, s.Terminal.transform);
            }
            catch (Exception e)
            {
                if (s.Terminal != null) Destroy(s.Terminal);
                s.Terminal = null;
                LogOnce("terminal " + s.Def.Id, e);
            }
        }

        private Interactable CreateTalk(CastSlot s, Transform parent)
        {
            Interactable it = Interactable.Create("Talk " + s.Def.Name, parent.position, s.Indoors ? 3f : 2.6f, "", () => OnTalk(s), parent);
            it.HeightTolerance = s.Indoors ? 1.5f : 2f;
            // Available campaign actors outrank incidental desks and service terminals nearby.
            // The quest runtime itself still has the higher QuestPriority.
            it.Priority = Interactable.QuestPriority - 1;
            it.DynamicPrompt = () => TalkPrompt(s);
            it.Condition = () => TalkVisible(s);
            return it;
        }

        private void Despawn(CastSlot s)
        {
            if (s.Actor != null)
            {
                s.Actor.ClearResidence04C();
                s.Actor.OnKilled = null;
                s.Actor.gameObject.SetActive(false);
                Destroy(s.Actor.gameObject);
            }
            if (s.Terminal != null) Destroy(s.Terminal);
            s.Actor = null;
            s.Terminal = null;
            s.Marker = null;
            s.Talk = null;
            s.FacingPlayer = false;
            s.HasLos = false;
        }

        private void DespawnAll()
        {
            for (int i = 0; i < _slots.Count; i++) if (_slots[i].Present) Despawn(_slots[i]);
        }

        // ================================================================== acknowledgement

        private void TickActors(float dt)
        {
            PlayerController player = Game.Player;
            if (player == null) return;
            Vector3 pp = Game.PlayerPosition;
            Vector3 head = player.HeadPosition;
            bool cinematic = Game.Cam != null && Game.Cam.InCinematic;
            for (int i = 0; i < _slots.Count; i++)
            {
                CastSlot s = _slots[i];
                NpcController a = s.Actor;
                if (a == null || a.IsDead || a.Anim == null) continue;
                Vector3 ap = a.transform.position;
                float flat = MathUtil.FlatDistance(ap, pp);
                bool same = Mathf.Abs(pp.y - ap.y) < 3f;
                if (flat < GuidanceRules04E.LookRange && same && !a.IsRagdolled)
                {
                    s.LosTimer -= dt;
                    if (s.LosTimer <= 0f)
                    {
                        s.LosTimer = 0.3f;
                        Vector3 eye = ap + Vector3.up * 1.6f * (a.Rig != null ? a.Rig.Scale : 1f);
                        s.HasLos = !Physics.Linecast(eye, head, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                    }
                }
                else s.HasLos = false;
                bool look = s.HasLos && !cinematic;
                a.Anim.HeadLook = look;
                if (look) a.Anim.LookTarget = head;
                if (flat > GuidanceRules04E.GreetRearmDistance)
                {
                    s.WalkedAway = true;
                    if (s.FacingPlayer) Face(s, s.PostYaw, false);
                }
                if (!cinematic && !a.IsRagdolled && GuidanceRules04E.ShouldGreet(flat, s.HasLos, same, Time.time, s.LastGreet, s.WalkedAway))
                    Acknowledge(s, pp);
                if (flat < 3.5f && s.HasLos)
                {
                    s.NearTime += dt;
                    if (s.NearTime > 1.2f) Face(s, QuestWorld.YawToward(ap, pp), true);
                }
                else s.NearTime = 0f;
            }
        }

        private void Acknowledge(CastSlot s, Vector3 playerPos)
        {
            s.LastGreet = Time.time;
            s.WalkedAway = false;
            Face(s, QuestWorld.YawToward(s.Actor.transform.position, playerPos), true);
            s.Actor.Anim.Greet();
            // One line at most, and none over a running mission's own dialogue (the first-meeting line then waits for
            // the next meeting instead of being lost).
            if (Game.Hud == null || (Game.Missions != null && Game.Missions.Active != null)) return;
            bool first = !_save.Met.Contains(s.Def.Id);
            string line = first ? s.Def.Intro : GreetingFor(s.Def);
            string memory = SagaDirection04L.Acknowledgement(s.Def.Name, Game.Missions != null ? Game.Missions.StoryProgress : 0);
            if (!first && !string.IsNullOrEmpty(memory)) line = memory;
            s.Actor.Anim.Direct04L(s, SagaDirection04L.Emotion(s.Def.Name, -1), true, s.Def.Name == "Luma",
                Game.Player != null ? Game.Player.HeadPosition : playerPos + Vector3.up, SagaDirection04L.ReadSeconds(line));
            if (string.IsNullOrEmpty(line)) return;
            if (first) _save.Met.Add(s.Def.Id);
            int words = line.Split(' ').Length;
            Game.Hud.Subtitle(s.Def.Name, line, Mathf.Clamp(1.6f + words * 0.3f, 3f, 7.5f));
        }

        /// <summary>Turns the person (through their station routine, so they never slide) toward a yaw, only when it
        /// differs enough from where they already face.</summary>
        private void Face(CastSlot s, float yaw, bool towardPlayer)
        {
            if (s.Actor == null || s.Contains == null) return;
            s.FacingPlayer = towardPlayer;
            if (Mathf.Abs(Mathf.DeltaAngle(yaw, s.FaceYaw)) < 20f) return;
            s.FaceYaw = yaw;
            s.Actor.ConfigureResidence04C(s.Post, s.Post, yaw, s.Contains, _castRoot, CastSeed(s));
        }
    }
}
