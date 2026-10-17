using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One placed resident: which slot it fills, its body and where it lives.</summary>
    internal sealed class ResidentEntry04E
    {
        public string Key = "";
        public string Region = "";
        public string Anchor = "";
        public string Species = "";
        public int Index;
        public bool Venue;
        public Creature Body;
        public ResidentDriver04E Driver;
        public Vector3 Home;
    }

    public sealed partial class UnderheartEcology
    {
        /// <summary>Metres from the stair entrance within which the service level's residents are prepared from the street.</summary>
        private const float EntranceReach = 50f;
        /// <summary>Metres from the Pressure House nest within which its hatchlings exist.</summary>
        private const float NestReach = 45f;
        /// <summary>Residents the player can see are only removed beyond this distance (no popping in view).</summary>
        private const float VisibleKeep = 70f;

        private readonly List<ResidentEntry04E> _residents = new List<ResidentEntry04E>();
        private readonly List<string> _desired = new List<string>();
        // 04E choice ecology: the slots for the stored matron_fate and regent_end (ChoiceEcology04E), the keys they want,
        // and slots emptied by a death that wait before a new resident settles.
        private readonly List<ResidentSlot04E> _slots = new List<ResidentSlot04E>();
        private readonly HashSet<string> _wantedKeys = new HashSet<string>();
        private readonly Dictionary<string, float> _respawnAt = new Dictionary<string, float>();
        private string _slotsMatron, _slotsRegent, _slotsJuveniles;
        private float _residentWait = 2f;
        private int _anchorsTried, _anchorsMissing;
        private string _residentLine = "Waiting for the Underheart world.";

        /// <summary>Living residents placed by the ecology right now.</summary>
        public int ResidentCount { get { return _residents.Count; } }

        /// <summary>What the stored campaign decisions changed in the residents ("" before either is made).</summary>
        public string ChoiceEcologyLine { get { return ChoiceEcology04E.Describe(_slotsMatron, _slotsRegent); } }

        /// <summary>The resident slots in force now (the authored ones with the campaign decisions applied).</summary>
        public IList<ResidentSlot04E> EffectiveSlots
        {
            get
            {
                EnsureChoiceSlots();
                return _slots;
            }
        }

        /// <summary>A stored campaign decision ("" when not made or the quest book is missing).</summary>
        private static string StoredChoice(string key)
        {
            if (Game.Quests == null) return "";
            try { return Game.Quests.Choice(key) ?? ""; }
            catch (Exception e)
            {
                Log.Exception("Ecology choice " + key, e);
                return "";
            }
        }

        /// <summary>
        /// Rebuilds the slots in force when a decision changed (matron_fate, regent_end, or the juveniles clutch being
        /// guided home), with the set of resident keys they want. Residents whose key is no longer wanted leave off camera.
        /// </summary>
        private void EnsureChoiceSlots()
        {
            string m = StoredChoice(ChoiceEcology04E.MatronKey);
            string r = StoredChoice(ChoiceEcology04E.RegentKey);
            string j = HatchlingChoice();
            if (_slotsMatron == m && _slotsRegent == r && _slotsJuveniles == j) return;
            bool changed = _slotsMatron != null && (_slotsMatron != m || _slotsRegent != r);
            _slotsMatron = m;
            _slotsRegent = r;
            _slotsJuveniles = j;
            ChoiceEcology04E.EffectiveSlots(m, r, _slots);
            _wantedKeys.Clear();
            for (int i = 0; i < _slots.Count; i++)
                for (int k = 0; k < _slots[i].Count; k++) _wantedKeys.Add(ResidentRules04E.SlotKey(_slots[i], k));
            if (j == "guide")
            {
                ResidentSlot04E home = GuidedHomeSlot();
                for (int k = 0; k < home.Count; k++) _wantedKeys.Add(ResidentRules04E.SlotKey(home, k));
            }
            if (changed) Note("choice ecology: " + ChoiceEcology04E.Describe(m, r));
        }

        /// <summary>The guided clutch's slot at the service level's juvenile pocket.</summary>
        private static ResidentSlot04E GuidedHomeSlot()
        {
            return new ResidentSlot04E(UnderheartIds.ServiceLevel, ResidentRules04E.GuidedHomeAnchor, EnemyIds04E.EmberHatchling, ResidentRules04E.VenueNestHatchlings);
        }

        /// <summary>
        /// Once a second: residents in regions the player is no longer in or next to are removed (off camera), the cap
        /// (by graphics preset, halved while an encounter runs) is respected, and missing slots of the current region,
        /// then its neighbours, are filled, at most two bodies per tick. The Pressure House nest is handled on the street.
        /// </summary>
        private void TickResidents(float dt)
        {
            _residentWait -= dt;
            if (_residentWait > 0f) return;
            _residentWait = 1f;
            PruneResidents();
            EnsureChoiceSlots();
            if (Game.State == GameState.Title || Game.State == GameState.Loading)
            {
                if (_residents.Count > 0) ClearResidents(true);
                return;
            }
            if (Game.State != GameState.Playing || Game.Creatures == null || Game.Player == null) return;
            Vector3 pp = Game.PlayerPosition;
            bool inner = Game.Inner != null && Game.Inner.IsInner;
            bool underground = CreatureDirector.IsUndergroundPoint(pp);
            UnderheartWorld u = Game.Underheart;
            bool ready = u != null && u.Ready;
            _desired.Clear();
            if (ready && !inner)
            {
                bool nearEntrance = !underground && u.SurfaceEntrance != Vector3.zero && MathUtil.FlatDistance(pp, u.SurfaceEntrance) < EntranceReach;
                ResidentRules04E.DesiredRegions(underground ? u.PlayerRegion : "", nearEntrance, _desired);
            }
            _residentLine = !ready ? "Waiting for the Underheart world." : (_desired.Count == 0 ? "No region near the player." : "Regions: " + string.Join(", ", _desired.ToArray()));
            int cap = ResidentRules04E.Cap((int)Settings.Graphics, AnySkirmishActive);
            // Leave: regions out of range, anything over the cap (farthest first), anything inside a claimed arena.
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                ResidentEntry04E e = _residents[i];
                if (e.Venue) continue;
                bool keep = _desired.Contains(e.Region) && _wantedKeys.Contains(e.Key) && !ClaimedByEncounter(e.Home, e.Species);
                if (!keep) TryRemoveResident(e, false);
            }
            int placed = CountPlaced(false);
            while (placed > cap)
            {
                ResidentEntry04E far = FarthestResident(pp);
                if (far == null || !TryRemoveResident(far, true)) break;
                placed--;
            }
            TickVenueNest(pp, underground || inner);
            if (!ready || _desired.Count == 0) return;
            int budget = 2;
            _anchorsTried = 0;
            _anchorsMissing = 0;
            for (int r = 0; r < _desired.Count; r++)
            {
                string region = _desired[r];
                int inRegion = CountInRegion(region);
                // The slots in force: the authored ones with the campaign decisions applied (choice slots first).
                for (int s = 0; s < _slots.Count; s++)
                {
                    ResidentSlot04E slot = _slots[s];
                    if (slot.Region != region) continue;
                    FillSlot(u, slot, false, ref placed, ref inRegion, ref budget, cap);
                }
                // The guided clutch: chosen "guide" at the Pressure House, the hatchlings live at the service level's juvenile pocket.
                if (region == UnderheartIds.ServiceLevel && HatchlingChoice() == "guide")
                    FillSlot(u, GuidedHomeSlot(), true, ref placed, ref inRegion, ref budget, cap);
            }
        }

        private void FillSlot(UnderheartWorld u, ResidentSlot04E slot, bool guided, ref int placed, ref int inRegion, ref int budget, int cap)
        {
            Vector3 anchor;
            float yaw;
            _anchorsTried++;
            bool resolved;
            try { resolved = u.TryAnchor(slot.Anchor, out anchor, out yaw); }
            catch (Exception e)
            {
                Log.Exception("Underheart anchor " + slot.Anchor, e);
                resolved = false;
                anchor = Vector3.zero;
                yaw = 0f;
            }
            if (!resolved || !MathUtil.IsFinite(anchor))
            {
                _anchorsMissing++;
                return;
            }
            // A choice's new residents settle while nobody watches (no popping in view right after the decision).
            if (slot.FromChoice && SeenByCamera(anchor + Vector3.up * 0.6f) && MathUtil.FlatDistance(anchor, Game.PlayerPosition) < VisibleKeep) return;
            for (int i = 0; i < slot.Count; i++)
            {
                if (budget <= 0 || placed >= cap || inRegion >= ResidentRules04E.MaxPerRegion) return;
                string key = ResidentRules04E.SlotKey(slot, i);
                if (FindResident(key) != null) continue;
                float back;
                if (_respawnAt.TryGetValue(key, out back) && Time.time < back) continue;
                if (ClaimedByEncounter(anchor, slot.Species)) return;
                if (SpawnResident(slot, anchor, yaw, i, key, guided, null))
                {
                    placed++;
                    inRegion++;
                    budget--;
                }
                else budget--;
            }
        }

        /// <summary>Spawns one resident near its anchor (a free spot on the anchor's own floor) with its plan applied.</summary>
        private bool SpawnResident(ResidentSlot04E slot, Vector3 anchor, float yaw, int index, string key, bool guided, VenueView04C room)
        {
            CreatureDirector cd = Game.Creatures;
            if (cd == null) return false;
            bool venue = room != null;
            Vector3 p;
            if (!PlaceResident(anchor, slot.Anchor, index, room, out p)) return false;
            ResidentPlan04E plan = ResidentRules04E.Plan(slot.Species, slot.Region, slot.Anchor, venue);
            plan = ResidentRules04E.ApplyTemper(plan, slot.Temper, venue || ResidentRules04E.IsRefuge(slot.Region, slot.Anchor));
            CreatureSpawnOptions o = new CreatureSpawnOptions
            {
                Tag = "uh04e_res:" + key,
                Hostile = false,
                Persistent = true,
                CalmUntilProvoked = plan.Calm,
                NoRewards = true,
                Seed = (MathUtil.Hash(key.GetHashCode(), index + 11) & 0x7fffffff) | 1
            };
            CharacterBase body = cd.Spawn(slot.Species, p, yaw + index * 47f, o);
            Creature c = body as Creature;
            if (c == null)
            {
                if (body != null) Destroy(body.gameObject);
                return false;
            }
            ResidentDriver04E d = c.GetComponent<ResidentDriver04E>();
            if (d == null)
            {
                d = c.gameObject.AddComponent<ResidentDriver04E>();
                d.Init(c);
            }
            // A moth's home is its warm point on the floor; its drift loops 0.8 to 2.2 m above it (under low ceilings too).
            Vector3 home = slot.Species == EnemyIds04E.CinderMoth ? p + Vector3.up * 0.1f : p;
            d.Apply(plan, home, index);
            d.Sheltered = slot.Sheltered;
            d.Guided = guided;
            d.Room = room;
            _residents.Add(new ResidentEntry04E { Key = key, Region = slot.Region, Anchor = slot.Anchor, Species = slot.Species, Index = index, Venue = venue, Body = c, Driver = d, Home = home });
            return true;
        }

        /// <summary>
        /// A spot near the anchor: a short floor probe (never the ceiling or the street above), within 1.5 m of the
        /// anchor's height, room to stand, in sight of the anchor (not behind a wall), off damaging heat and inside the
        /// venue when there is one. Falls back to the anchor itself.
        /// </summary>
        private static bool PlaceResident(Vector3 anchor, string anchorId, int index, VenueView04C room, out Vector3 pos)
        {
            for (int attempt = 0; attempt < 4; attempt++)
            {
                Vector3 c = anchor + (room != null ? ResidentRules04E.SlotOffset(index + attempt, 0.55f + 0.2f * attempt) : ResidentRules04E.PlacementOffset(anchorId, index, attempt));
                if (ResidentSpotOk(anchor, c, room, true, out pos)) return true;
            }
            return ResidentSpotOk(anchor, anchor, room, false, out pos);
        }

        private static bool ResidentSpotOk(Vector3 anchor, Vector3 c, VenueView04C room, bool needSight, out Vector3 pos)
        {
            pos = anchor;
            float y;
            if (!CreatureDirector.GroundAt(c, UnderheartSpawnRules04E.ProbeUp, out y)) return false;
            if (Mathf.Abs(y - anchor.y) > 1.5f) return false;
            Vector3 p = new Vector3(c.x, y, c.z);
            if (room != null && !room.Contains(p + Vector3.up * 0.3f)) return false;
            if (Physics.CheckCapsule(p + Vector3.up * 0.4f, p + Vector3.up * 1.0f, 0.28f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (needSight && Physics.Linecast(anchor + Vector3.up * 0.8f, p + Vector3.up * 0.8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            float heat;
            if (Game.Underheart != null && Game.Underheart.HazardAt(p, out heat)) return false;
            pos = p;
            return true;
        }

        // ================================================================== the Pressure House nest

        /// <summary>The "juveniles" choice: "" (not decided), "shelter" or "guide".</summary>
        private static string HatchlingChoice()
        {
            if (Game.Quests == null) return "";
            try { return Game.Quests.Choice("juveniles") ?? ""; }
            catch (Exception e)
            {
                Log.Exception("Ecology juveniles choice", e);
                return "";
            }
        }

        /// <summary>
        /// The displaced nest in the Pressure House: three harmless hatchlings (cannot be hurt, give nothing) that follow
        /// warmth inside the room until the choice is made; "shelter" settles them in the nest, "guide" moves them to the
        /// service level's juvenile pocket. Only while the player is on the street within reach of the nest anchor.
        /// </summary>
        private void TickVenueNest(Vector3 pp, bool playerAway)
        {
            VenueBinding04C v = null;
            if (Game.Inhabited != null)
            {
                try { v = Game.Inhabited.FindVenue(VenueIds04E.Pressure); }
                catch (Exception e) { Log.Exception("Ecology nest venue", e); }
            }
            string choice = HatchlingChoice();
            bool want = false;
            Vector3 nest = Vector3.zero;
            if (v != null && v.Available && !playerAway && choice != "guide")
            {
                Vector3 local = v.View.LocalAnchor(ResidentRules04E.VenueNestAnchor);
                if (local != Vector3.zero)
                {
                    nest = v.View.WorldPoint(ResidentRules04E.VenueNestAnchor);
                    want = MathUtil.FlatDistance(pp, nest) < NestReach && Mathf.Abs(pp.y - nest.y) < 6f;
                }
            }
            bool sheltered = choice == "shelter";
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                ResidentEntry04E e = _residents[i];
                if (!e.Venue) continue;
                if (!want) TryRemoveResident(e, choice == "guide");
                else if (e.Driver != null) e.Driver.Sheltered = sheltered;
            }
            if (!want) return;
            ResidentSlot04E slot = new ResidentSlot04E("", ResidentRules04E.VenueNestAnchor, EnemyIds04E.EmberHatchling, ResidentRules04E.VenueNestHatchlings);
            for (int i = 0; i < slot.Count; i++)
            {
                string key = VenueIds04E.Pressure + ".nest#" + i;
                if (FindResident(key) != null) continue;
                if (SpawnResident(slot, nest, v.View.AnchorYaw(ResidentRules04E.VenueNestAnchor), i, key, false, v.View))
                {
                    ResidentEntry04E e = FindResident(key);
                    if (e != null && e.Driver != null) e.Driver.Sheltered = sheltered;
                }
                break;
            }
        }

        // ================================================================== bookkeeping

        private void PruneResidents()
        {
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                ResidentEntry04E e = _residents[i];
                if (e.Body == null || e.Body.IsDead)
                {
                    // A death (not a despawn) leaves the slot empty for a while: a defending crab is not replaced at once.
                    if (e.Body != null && !e.Venue) _respawnAt[e.Key] = Time.time + ResidentRules04E.RespawnSeconds;
                    _residents.RemoveAt(i);
                }
            }
        }

        private ResidentEntry04E FindResident(string key)
        {
            for (int i = 0; i < _residents.Count; i++) if (_residents[i].Key == key) return _residents[i];
            return null;
        }

        private int CountPlaced(bool venue)
        {
            int n = 0;
            for (int i = 0; i < _residents.Count; i++) if (_residents[i].Venue == venue) n++;
            return n;
        }

        private int CountInRegion(string region)
        {
            int n = 0;
            for (int i = 0; i < _residents.Count; i++) if (!_residents[i].Venue && _residents[i].Region == region) n++;
            return n;
        }

        private ResidentEntry04E FarthestResident(Vector3 pp)
        {
            ResidentEntry04E best = null;
            float bestD = -1f;
            for (int i = 0; i < _residents.Count; i++)
            {
                ResidentEntry04E e = _residents[i];
                if (e.Venue || e.Body == null) continue;
                float d = (e.Body.transform.position - pp).sqrMagnitude;
                if (d > bestD)
                {
                    bestD = d;
                    best = e;
                }
            }
            return best;
        }

        /// <summary>
        /// True when a running encounter owns this spot: any admission lease covers it, or (for the territorial crab) the
        /// quiet aftermath of a cleared room does. Residents never walk into someone else's fight.
        /// </summary>
        private static bool ClaimedByEncounter(Vector3 p, string species)
        {
            IList<QuietLease> leases = LocalQuiet.Leases;
            for (int i = 0; i < leases.Count; i++)
            {
                QuietLease l = leases[i];
                if (l == null || !l.Covers(p)) continue;
                if (l.Kind == QuietKind.Admission) return true;
                if (species == EnemyIds04E.KilnCrab) return true;
            }
            return false;
        }

        /// <summary>Removes a resident when the camera cannot see it or it is far away; force removes it regardless.</summary>
        private bool TryRemoveResident(ResidentEntry04E e, bool force)
        {
            Creature c = e.Body;
            if (c != null && !c.IsDead)
            {
                bool seen = SeenByCamera(c.transform.position + Vector3.up * 0.4f);
                float d = MathUtil.FlatDistance(c.transform.position, Game.PlayerPosition);
                if (seen && !force && d < VisibleKeep) return false;
                c.Despawn(seen);
            }
            _residents.Remove(e);
            return true;
        }

        /// <summary>Removes every resident now (despawn) or only forgets them (the system is shutting down).</summary>
        private void ClearResidents(bool despawn)
        {
            _respawnAt.Clear();
            _slotsMatron = _slotsRegent = _slotsJuveniles = null;
            if (despawn)
            {
                for (int i = 0; i < _residents.Count; i++)
                {
                    Creature c = _residents[i].Body;
                    if (c != null && !c.IsDead) c.Despawn(false);
                }
            }
            _residents.Clear();
        }

        /// <summary>A new encounter's room: the crabs and hatchlings inside it leave (moths stay; they are harmless).</summary>
        private void ClearResidentsIn(Vector3 center, float radius)
        {
            for (int i = _residents.Count - 1; i >= 0; i--)
            {
                ResidentEntry04E e = _residents[i];
                if (e.Species == EnemyIds04E.CinderMoth || e.Body == null) continue;
                Vector3 p = e.Body.transform.position;
                if (!WorldBounds.SameLayer(p, center) || MathUtil.FlatDistance(p, center) > radius) continue;
                bool seen = SeenByCamera(p + Vector3.up * 0.4f);
                if (!e.Body.IsDead) e.Body.Despawn(seen);
                _residents.RemoveAt(i);
            }
        }
    }
}
