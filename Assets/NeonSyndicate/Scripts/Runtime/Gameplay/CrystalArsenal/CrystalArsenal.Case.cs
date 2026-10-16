using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The resonance case and the practice lane near the Veyra Lofts arrival. The spots are audited once per boot, the
    /// first time the player is near the arrival in play (so streamed props and parked cars are present): candidate
    /// offsets along the Lofts frontage (beside the door, never in front of it), each tested for ground, buildings, roads,
    /// other doors and services, the pedestrian paths and a physics overlap against World, Debris and Vehicles. The lane
    /// (three targets and a real backstop wall) gets the same audit over its whole footprint. Every choice and every
    /// failure is logged; a failed case leaves the offers claimable in ARSENAL, a failed lane opens Prism Thread directly.
    /// </summary>
    public sealed partial class CrystalArsenal
    {
        private const float PlaceTriggerRadius = 95f;
        private const int MaxPlaceAttempts = 3;

        private bool _casePlaced;
        private bool _caseFailed;
        private bool _lanePlaced;
        private bool _laneFailed;
        private bool _homeResolved;
        private Vector3 _door;
        private Vector3 _doorFacing = Vector3.forward;
        private Vector3 _arrival;
        private Vector3 _casePos;
        private int _placeAttempts;
        private float _nextPlaceAt;
        private string _placeReport = "Not placed yet: the case is placed the first time you are near the Veyra Lofts arrival.";
        private CrystalCase _case;
        private Interactable _caseUse;
        private GameObject _laneRoot;
        private CrystalLaneLayout _lane;
        private readonly CrystalPracticeTarget[] _targets = new CrystalPracticeTarget[CrystalPracticeRules.TargetCount];
        private Blip _blip;
        private bool _inLane;
        private int _laneHits;
        private readonly List<Vector3> _linkA = new List<Vector3>();
        private readonly List<Vector3> _linkB = new List<Vector3>();
        private readonly List<int> _walkTmp = new List<int>();
        private readonly List<Vector3> _samples = new List<Vector3>();

        /// <summary>Where the case stands (valid when CasePlaced).</summary>
        public Vector3 CasePosition { get { return _casePlaced ? _casePos : _arrival; } }
        public bool CasePlaced { get { return _casePlaced; } }
        public bool LanePlaced { get { return _lanePlaced; } }
        /// <summary>04E: where a player stands to calibrate on the practice lane (the case when the lane is not placed).</summary>
        public Vector3 LaneShooter { get { return _lanePlaced ? _lane.Shooter : CasePosition; } }

        private static int BlockerMask { get { return Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles); } }

        // ------------------------------------------------------------------ per frame

        private void UpdateCase()
        {
            ResolveHome();
            if (!_casePlaced && !_caseFailed && _homeResolved && Game.IsPlaying && Game.Player != null && Time.time >= _nextPlaceAt &&
                MathUtil.FlatSqrDistance(Game.PlayerPosition, _arrival) < PlaceTriggerRadius * PlaceTriggerRadius)
                TryPlace();
            UpdateBlipAndCase();
        }

        private void ResolveHome()
        {
            if (_homeResolved || _layout == null) return;
            Poi home = _layout.FindPoi("Veyra Lofts");
            if (home == null) home = _layout.NearestPoi(Vector3.zero, PoiType.Safehouse);
            if (home == null)
            {
                _caseFailed = true;
                _laneFailed = true;
                _homeResolved = true;
                _placeReport = "No Veyra Lofts in this city: offers are claimable in ARSENAL.";
                Log.Warn("Crystal arsenal: " + _placeReport);
                _version++;
                return;
            }
            _door = home.Position;
            _doorFacing = CrystalCaseRules.FlatDir(home.Facing);
            _arrival = GameRoot.SpawnOutside(home);
            _homeResolved = true;
        }

        /// <summary>True while the case marker should call the player (an offer is open or a reward is waiting).</summary>
        private bool CaseBeckons
        {
            get
            {
                string r;
                return OfferOpen(CrystalEntitlementRules.StarterToken, out r) || OfferOpen(CrystalEntitlementRules.CalibrationToken, out r) || PendingCount > 0;
            }
        }

        private void UpdateBlipAndCase()
        {
            if (!_homeResolved || (_caseFailed && !_casePlaced)) return;
            bool beckon = CaseBeckons;
            if (_blip == null) _blip = Blip.Add(BlipKind.Activity, null, _casePlaced ? _casePos : _arrival, new Color(1f, 0.82f, 0.3f), "A", "Resonance case", beckon);
            _blip.Position = (_casePlaced ? _casePos : _arrival) + new Vector3(0f, 0.01f, 0f);
            _blip.EdgeClamp = beckon;
            _blip.Flash = beckon && !CrystalEntitlementRules.Has(_entitlements, CrystalEntitlementRules.StarterToken);
            _blip.Size = beckon ? 1.3f : 1f;
            _blip.Label = beckon ? "Resonance case (reward waiting)" : "Resonance case and contract board";
            if (_case != null)
            {
                string r;
                bool first = !CrystalEntitlementRules.Has(_entitlements, CrystalEntitlementRules.StarterToken);
                bool secondOpen = _calibrationDone || _laneFailed;
                bool second = OfferOpen(CrystalEntitlementRules.CalibrationToken, out r);
                _case.SetState(first, secondOpen, second, beckon);
            }
        }

        // ------------------------------------------------------------------ placement

        private void TryPlace()
        {
            _placeAttempts++;
            CollectLinks(_arrival, 110f);
            Vector3 spot;
            string why;
            if (!FindCaseSpot(out spot, out why))
            {
                if (_placeAttempts >= MaxPlaceAttempts)
                {
                    _caseFailed = true;
                    _laneFailed = true;
                    _placeReport = "Resonance case not placed after " + _placeAttempts + " audits (" + why + "). Offers are claimable in ARSENAL; Prism Thread is offered directly.";
                    Log.Warn("Crystal arsenal: " + _placeReport);
                    if (_blip != null) Blip.Remove(_blip);
                    _blip = null;
                }
                else
                {
                    _nextPlaceAt = Time.time + 8f;
                    Log.Info("Crystal arsenal: no clear case spot yet (" + why + "); retrying in 8 s.");
                }
                _version++;
                return;
            }
            _casePos = spot;
            _case = CrystalCase.Create(transform, spot, _doorFacing);
            _caseUse = _case.gameObject.AddComponent<Interactable>();
            _caseUse.Radius = 2.4f;
            _caseUse.HeightTolerance = 2.2f;
            _caseUse.Condition = () => Game.IsPlaying && Game.Player != null && !Game.Player.IsDead && !Game.Player.InVehicle;
            _caseUse.DynamicPrompt = CasePrompt;
            _caseUse.Use = () => OpenCase();
            _casePlaced = true;
            float lateral = Vector3.Dot(spot - _door, CrystalCaseRules.RightOf(_doorFacing));
            _placeReport = "Resonance case at " + Fmt(spot) + ", " + Mathf.Sqrt(MathUtil.FlatSqrDistance(spot, _arrival)).ToString("0.0") + " m from the Veyra Lofts arrival (" +
                           lateral.ToString("0.0") + " m along the frontage). ";
            CrystalLaneLayout lane;
            string laneWhy;
            if (FindLane(spot, out lane, out laneWhy))
            {
                BuildLane(lane);
                _placeReport += "Practice lane shooting spot " + Fmt(lane.Shooter) + ", facing " + Fmt(lane.Dir) + ", " + Mathf.Sqrt(MathUtil.FlatSqrDistance(lane.Shooter, spot)).ToString("0.0") + " m from the case.";
            }
            else
            {
                _laneFailed = true;
                _placeReport += "No safe practice lane (" + laneWhy + "): Prism Thread is offered in the case directly.";
            }
            Log.Info("Crystal arsenal: " + _placeReport);
            _version++;
        }

        private static string Fmt(Vector3 v)
        {
            return "(" + v.x.ToString("0.0") + ", " + v.y.ToString("0.0") + ", " + v.z.ToString("0.0") + ")";
        }

        private void CollectLinks(Vector3 around, float radius)
        {
            _linkA.Clear();
            _linkB.Clear();
            if (_layout == null) return;
            float r2 = radius * radius;
            foreach (WalkLink l in _layout.WalkLinks)
            {
                if (l.A < 0 || l.B < 0 || l.A >= _layout.WalkNodes.Count || l.B >= _layout.WalkNodes.Count) continue;
                Vector3 a = _layout.WalkNodes[l.A].Pos, b = _layout.WalkNodes[l.B].Pos;
                if (CrystalCaseRules.DistanceToSegment(around, a, b) * CrystalCaseRules.DistanceToSegment(around, a, b) > r2) continue;
                _linkA.Add(a);
                _linkB.Add(b);
            }
        }

        private bool FindCaseSpot(out Vector3 spot, out string why)
        {
            spot = _arrival;
            why = "no candidate";
            string last = "";
            List<Vector3> c = CrystalCaseRules.CaseCandidates(_door, _doorFacing);
            for (int i = 0; i < c.Count; i++)
            {
                Vector3 ground;
                if (CaseSpotOk(c[i], true, out ground, out last)) { spot = ground; return true; }
            }
            // The frontage is fully taken: try the open ground around the arrival, still clear of doors and paths.
            List<Vector3> ring = CrystalCaseRules.RingCandidates(_arrival, 4f, 3f, 8, 16);
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 ground;
                if (CaseSpotOk(ring[i], false, out ground, out last)) { spot = ground; return true; }
            }
            why = "last refusal: " + last;
            return false;
        }

        private bool CaseSpotOk(Vector3 c, bool frontage, out Vector3 ground, out string why)
        {
            ground = c;
            why = "";
            if (MathUtil.FlatSqrDistance(c, _door) < CrystalCaseRules.MinDoorDistance * CrystalCaseRules.MinDoorDistance) { why = "too close to the Lofts door"; return false; }
            if (_layout.IsInsideBuilding(c, 0.35f)) { why = "inside a building"; return false; }
            if (_layout.RoadAt(c, 1.2f) != null || _layout.IsOnRoadOrIntersection(c)) { why = "on or beside a road"; return false; }
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(c.x, _arrival.y + 3f, c.z), Vector3.down, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) { why = "no ground"; return false; }
            if (Mathf.Abs(hit.point.y - _arrival.y) > 1.2f || hit.normal.y < 0.85f) { why = "ground not level with the arrival"; return false; }
            ground = hit.point;
            foreach (Poi p in _layout.Pois)
                if (MathUtil.FlatSqrDistance(ground, p.Position) < 3.5f * 3.5f) { why = "blocks the door of " + p.Name; return false; }
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null || it == _caseUse) continue;
                float sep = it.Radius + 1.5f;
                if (MathUtil.FlatSqrDistance(ground, it.transform.position) < sep * sep) { why = "crowds the service " + it.name; return false; }
            }
            for (int i = 0; i < _linkA.Count; i++)
                if (CrystalCaseRules.DistanceToSegment(ground, _linkA[i], _linkB[i]) < 1.3f) { why = "on a pedestrian path"; return false; }
            if (Physics.CheckSphere(ground + Vector3.up * 1f, 0.75f, BlockerMask, QueryTriggerInteraction.Ignore)) { why = "something solid stands there"; return false; }
            if (Physics.Linecast(_arrival + Vector3.up * 1.5f, ground + Vector3.up * 1f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) { why = "not visible from the arrival"; return false; }
            if (!frontage && MathUtil.FlatSqrDistance(ground, _arrival) > 30f * 30f) { why = "too far from the arrival"; return false; }
            return true;
        }

        private bool FindLane(Vector3 casePos, out CrystalLaneLayout lane, out string why)
        {
            lane = new CrystalLaneLayout();
            why = "no candidate";
            string last = "";
            Vector3[] dirs = CrystalCaseRules.LaneDirections(_doorFacing);
            List<Vector3> spots = CrystalCaseRules.RingCandidates(casePos, 3f, 3f, 15, 16);
            for (int i = 0; i < spots.Count; i++)
            {
                RaycastHit hit;
                Vector3 s = spots[i];
                if (_layout.IsInsideBuilding(s, 0.8f) || _layout.RoadAt(s, 1f) != null) { last = "shooting spot blocked"; continue; }
                if (!Physics.Raycast(new Vector3(s.x, casePos.y + 3f, s.z), Vector3.down, out hit, 8f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) { last = "no ground"; continue; }
                if (Mathf.Abs(hit.point.y - casePos.y) > 1.5f) { last = "not level with the case"; continue; }
                for (int d = 0; d < dirs.Length; d++)
                {
                    CrystalLaneLayout L = CrystalCaseRules.Lane(hit.point, dirs[d]);
                    if (LaneOk(L, casePos, out last)) { lane = L; return true; }
                }
            }
            why = "last refusal: " + last;
            return false;
        }

        private bool LaneOk(CrystalLaneLayout L, Vector3 casePos, out string why)
        {
            why = "";
            if (CrystalCaseRules.InFootprint(L, casePos, 1.5f)) { why = "overlaps the case"; return false; }
            // Cheap hashed layout queries over the whole footprint first; the slower intersection test runs on the probes below.
            CrystalCaseRules.FootprintSamples(L, 1.5f, _samples);
            for (int i = 0; i < _samples.Count; i++)
            {
                Vector3 p = _samples[i];
                if (_layout.IsInsideBuilding(p, 0.8f)) { why = "touches a building"; return false; }
                if (_layout.RoadAt(p, 1f) != null) { why = "touches a road"; return false; }
            }
            foreach (Poi poi in _layout.Pois)
                if (CrystalCaseRules.InFootprint(L, poi.Position, 5f)) { why = "near the door of " + poi.Name; return false; }
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null || it == _caseUse) continue;
                if (CrystalCaseRules.InFootprint(L, it.transform.position, it.Radius + 2f)) { why = "crowds the service " + it.name; return false; }
            }
            Vector3 center = L.FootStart + L.Dir * (L.FootLength * 0.5f);
            float reach = Mathf.Sqrt(L.FootLength * L.FootLength * 0.25f + L.FootHalfWidth * L.FootHalfWidth) + 1.5f;
            _layout.WalkNodesNear(center, reach, _walkTmp);
            for (int i = 0; i < _walkTmp.Count; i++)
                if (CrystalCaseRules.InFootprint(L, _layout.WalkNodes[_walkTmp[i]].Pos, 1f)) { why = "on a pedestrian path"; return false; }
            for (int i = 0; i < _linkA.Count; i++)
                if (CrystalCaseRules.SegmentHitsRect(_linkA[i], _linkB[i], L.FootStart, L.Dir, L.FootLength, L.FootHalfWidth, 0.8f)) { why = "crosses a pedestrian path"; return false; }
            Vector3 left = -L.Right * L.FootHalfWidth, right = L.Right * L.FootHalfWidth;
            Vector3 far = L.Dir * L.FootLength;
            Vector3[] probes = { L.FootStart + left, L.FootStart + right, L.FootStart + far + left, L.FootStart + far + right, center, L.Targets[0], L.Targets[1], L.Targets[2], L.WallCenter };
            for (int i = 0; i < probes.Length; i++)
            {
                RaycastHit hit;
                Vector3 p = probes[i];
                if (!Physics.Raycast(new Vector3(p.x, L.Shooter.y + 3f, p.z), Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) { why = "ground missing under the lane"; return false; }
                if (Mathf.Abs(hit.point.y - L.Shooter.y) > 0.6f || hit.normal.y < 0.85f) { why = "lane ground is not level"; return false; }
                if (_layout.IsOnRoadOrIntersection(p)) { why = "touches an intersection"; return false; }
            }
            Vector3 half = new Vector3(L.FootHalfWidth, 1.75f, L.FootLength * 0.5f);
            if (Physics.CheckBox(center + Vector3.up * (0.35f + 1.75f), half, Quaternion.LookRotation(L.Dir, Vector3.up), BlockerMask, QueryTriggerInteraction.Ignore))
            {
                why = "something solid stands in the lane";
                return false;
            }
            return true;
        }

        // ------------------------------------------------------------------ the lane

        private void BuildLane(CrystalLaneLayout L)
        {
            _lane = L;
            _laneRoot = new GameObject("Crystal Practice Lane");
            _laneRoot.transform.SetParent(transform, false);
            _laneRoot.transform.position = L.Shooter;
            Quaternion rot = Quaternion.LookRotation(L.Dir, Vector3.up);
            // The wall reaches 0.4 m below the lane's ground so a gentle slope never leaves a gap under it.
            float wallBase = GroundUnder(L.WallCenter).y - 0.4f;
            float wallHeight = L.WallSize.y + 0.4f;
            GameObject wall = Shapes.MeshObject("Backstop", Shapes.CubeMesh, new[] { Mats.Solid(new Color(0.16f, 0.15f, 0.17f), 0.25f) }, _laneRoot.transform, true);
            wall.transform.position = new Vector3(L.WallCenter.x, wallBase + wallHeight * 0.5f, L.WallCenter.z);
            wall.transform.rotation = rot;
            wall.transform.localScale = new Vector3(L.WallSize.x, wallHeight, L.WallSize.z);
            wall.layer = Layers.World;
            wall.AddComponent<BoxCollider>();
            GameObject stripe = Shapes.MeshObject("Backstop Stripe", Shapes.CubeMesh, new[] { Mats.Emissive(new Color(0.35f, 0.85f, 1f), 1.6f) }, wall.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.18f, -0.52f);
            stripe.transform.localScale = new Vector3(0.96f, 0.035f, 0.05f);
            for (int i = 0; i < _targets.Length; i++)
                _targets[i] = CrystalPracticeTarget.Create(_laneRoot.transform, GroundUnder(L.Targets[i]), L.Shooter, i, OnTargetHit);
            WorldMarker.Create(L.Shooter, new Color(0.35f, 0.85f, 1f), 0.45f, _laneRoot.transform, false);
            _lanePlaced = true;
        }

        /// <summary>The static ground under a point (the point itself when nothing is found).</summary>
        private static Vector3 GroundUnder(Vector3 p)
        {
            RaycastHit hit;
            if (Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return hit.point;
            return p;
        }

        private void UpdatePractice()
        {
            bool inLane = false;
            PlayerController p = Game.Player;
            if (_lanePlaced && Game.IsPlaying && p != null && !p.IsDead && !p.InVehicle && CrystalCaseRules.InLaneZone(_lane, p.transform.position))
                inLane = true;
            if (inLane && !_inLane) _laneHits = 0;
            _inLane = inLane;
            bool focus = inLane && WeaponController.BehaviorOf(p.Weapons.Current) != CrystalBehavior.None;
            CrystalPractice.SuppressNoise = focus;
            for (int i = 0; i < _targets.Length; i++) if (_targets[i] != null) _targets[i].MarkCalibrated(_calibrationHits[i] || _calibrationDone);
        }

        /// <summary>A practice hit: counted, flashed, and part of calibration when a crystal focus made it.</summary>
        private void OnTargetHit(CrystalPracticeTarget t, DamageInfo info)
        {
            _laneHits++;
            if (!info.FromPlayer) return;
            bool focus = false;
            if (info.WeaponId >= 0 && info.WeaponId < (int)WeaponId.Count && Weapons.IsLoot((WeaponId)info.WeaponId))
                focus = WeaponController.BehaviorOf(Weapons.Get((WeaponId)info.WeaponId)) != CrystalBehavior.None;
            else if (Game.Player != null) focus = WeaponController.BehaviorOf(Game.Player.Weapons.Current) != CrystalBehavior.None;
            bool done = CrystalPracticeRules.RegisterHit(_calibrationHits, t.Index, focus);
            if (!done || _calibrationDone) return;
            _calibrationDone = true;
            _version++;
            Log.Info("Crystal arsenal: calibration complete; the case's second compartment (Prism Thread) is open.");
            CrystalSounds.PlayReward(t.transform.position);
            if (Game.Hud != null) Game.Hud.Banner("CALIBRATION COMPLETE", "The resonance case's second compartment is open: Prism Thread.", UIKit.Cyan, 5f);
        }

        // ------------------------------------------------------------------ the case

        private string CasePrompt()
        {
            string r;
            if (OfferOpen(CrystalEntitlementRules.StarterToken, out r)) return "Press E to open the resonance case (a crystal focus waits inside)";
            if (OfferOpen(CrystalEntitlementRules.CalibrationToken, out r)) return "Press E to open the resonance case (Prism Thread waits inside)";
            if (PendingCount > 0) return "Press E to open the resonance case (" + PendingCount + " reward" + (PendingCount == 1 ? "" : "s") + " waiting)";
            return "Press E to open the resonance case and contract board";
        }

        private void OpenCase()
        {
            if (_ui == null) return;
            _ui.Open(CrystalOfferUi.Mode.Case, null);
        }

        /// <summary>Points the GPS at the case (or the Lofts arrival before it is placed).</summary>
        public void TrackCase()
        {
            if (Game.Gps != null && _homeResolved) Game.Gps.SetWaypoint(_casePlaced ? _casePos : _arrival);
            Notify(_casePlaced ? "GPS: the resonance case beside the Veyra Lofts arrival." : "GPS: the Veyra Lofts arrival (the resonance case stands beside it).", 3f);
        }
    }
}
