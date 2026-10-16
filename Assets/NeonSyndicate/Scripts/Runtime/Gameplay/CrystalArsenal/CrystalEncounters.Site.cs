using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>The surveyed breach site: where it is, its anchors and whether it may be used right now.</summary>
    internal sealed class EncounterSite04D
    {
        public bool Valid;
        public string Reason = "";
        public Vector3 Center;
        /// <summary>Flat direction from the centre toward the Lofts arrival (where the player comes from).</summary>
        public Vector3 Approach = Vector3.forward;
        /// <summary>[0] warden, [1] refractor, [2] skirmisher, [3] and [4] reinforcement arrival points.</summary>
        public readonly List<Vector3> Anchors = new List<Vector3>();
        public int CandidateIndex = -1;
        public int CandidateCount;
        public float DistanceFromArrival;
    }

    public sealed partial class CrystalEncounters
    {
        private EncounterSite04D _site;
        private readonly List<Vector3> _candidates = new List<Vector3>();
        private readonly List<Vector3> _anchorScratch = new List<Vector3>();
        private CityLayout _candidatesFor;
        private Vector3 _arrival;
        private bool _hasArrival;
        private float _siteCheckedAt = -99f;
        private long _surveyMs;
        private string _lastSiteLog = "";

        private static int SpawnBlockMask { get { return Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles); } }

        /// <summary>
        /// Surveys the site once the world exists: the engine-free candidates (EncounterSiteRules04D, computed once per
        /// city) and then the real ground and colliders. force re-checks the colliders now (a start); otherwise a valid
        /// site is kept and an invalid one is re-checked every 10 s.
        /// </summary>
        private void EnsureSite(bool force)
        {
            CityLayout L = Game.Layout ?? _layout;
            if (L == null)
            {
                _site = new EncounterSite04D { Valid = false, Reason = "The city is not ready." };
                return;
            }
            if (Game.State == GameState.Loading) return;
            if (!force && _site != null && (_site.Valid || Time.unscaledTime - _siteCheckedAt < 10f)) return;
            BuildCandidates(L);
            _siteCheckedAt = Time.unscaledTime;
            _site = Survey(L);
            string summary = _site.Valid
                ? "ENC04D site chosen: candidate " + _site.CandidateIndex + " of " + _site.CandidateCount + " at " + _site.Center + ", "
                  + _site.DistanceFromArrival.ToString("0") + " m from the Lofts arrival"
                : "ENC04D site unavailable: " + _site.Reason;
            if (summary != _lastSiteLog) Log.Info(summary);
            _lastSiteLog = summary;
            if (_run == null)
            {
                if (!_site.Valid && EncounterRules04D.CanStart(_state)) SetState(EncounterState.Unavailable, _site.Reason);
                else if (_site.Valid && _state == EncounterState.Unavailable) SetState(ContractCleared(ResonanceBreach) ? EncounterState.Cleared : EncounterState.Available, "the site is clear again");
            }
        }

        /// <summary>The engine-free candidate centres, once per city (Init does it during the boot).</summary>
        private void BuildCandidates(CityLayout L)
        {
            if (L == null || _candidatesFor == L) return;
            Stopwatch sw = Stopwatch.StartNew();
            _candidatesFor = L;
            _hasArrival = EncounterSiteRules04D.Arrival(L, out _arrival);
            if (_hasArrival) EncounterSiteRules04D.Candidates(L, _arrival, _candidates);
            else _candidates.Clear();
            sw.Stop();
            _surveyMs = sw.ElapsedMilliseconds;
            Log.Info("ENC04D site candidates: " + _candidates.Count + " in " + _surveyMs + " ms" + (_hasArrival ? " around the Lofts arrival " + _arrival : " (no Veyra Lofts)"));
        }

        private EncounterSite04D Survey(CityLayout L)
        {
            EncounterSite04D s = new EncounterSite04D();
            s.CandidateCount = _candidates.Count;
            if (!_hasArrival)
            {
                s.Reason = "Veyra Lofts was not found in this city.";
                return s;
            }
            if (_candidates.Count == 0)
            {
                s.Reason = "No open ground 160 to 320 m from Veyra Lofts in this city.";
                return s;
            }
            string first = null;
            for (int i = 0; i < _candidates.Count; i++)
            {
                string why = TrySite(L, _candidates[i], s);
                if (why == null)
                {
                    s.Valid = true;
                    s.Reason = "";
                    s.CandidateIndex = i;
                    return s;
                }
                if (first == null) first = why;
                if (s.Valid == false && _lastSiteLog.Length == 0) Log.Info("ENC04D site candidate " + i + " at " + _candidates[i] + " rejected: " + why);
            }
            s.Valid = false;
            s.Anchors.Clear();
            s.Reason = "The breach ground near Veyra Lofts is blocked right now (" + first + ").";
            return s;
        }

        /// <summary>Checks one candidate on the real ground and colliders and fills the anchors. Null when it can be used.</summary>
        private string TrySite(CityLayout L, Vector3 candidate, EncounterSite04D s)
        {
            float groundY;
            Vector3 normal;
            if (!GroundAt(candidate, _arrival.y, out groundY, out normal)) return "no flat street-level ground";
            Vector3 c = new Vector3(candidate.x, groundY, candidate.z);
            if (Physics.CheckSphere(c + Vector3.up * 1.6f, 2.2f, SpawnBlockMask, QueryTriggerInteraction.Ignore)) return "the centre is cluttered";
            if (PedManager.ExcludedVenue04C(c)) return "inside an inhabited venue";
            if (NearArena(c)) return "too close to a boss arena";
            Vector3 approach = MathUtil.Flat(_arrival - c);
            if (approach.sqrMagnitude < 0.01f) approach = Vector3.forward;
            approach.Normalize();
            EncounterSiteRules04D.Anchors(c, approach, _anchorScratch);
            s.Anchors.Clear();
            for (int i = 0; i < _anchorScratch.Count; i++)
            {
                Vector3 a;
                if (!PlaceAnchor(L, c, _anchorScratch[i], out a)) return "no clear spot for anchor " + i;
                s.Anchors.Add(a);
            }
            s.Center = c;
            s.Approach = approach;
            s.DistanceFromArrival = MathUtil.FlatDistance(c, _arrival);
            return null;
        }

        /// <summary>The authored anchor or the first nudged alternative that stands on clear ground with a line to the centre.</summary>
        private static bool PlaceAnchor(CityLayout L, Vector3 center, Vector3 authored, out Vector3 anchor)
        {
            anchor = authored;
            for (int attempt = 0; attempt <= EncounterSiteRules04D.NudgeCount; attempt++)
            {
                Vector3 p = EncounterSiteRules04D.Nudge(center, authored, attempt);
                float y;
                Vector3 n;
                if (!GroundAt(p, center.y, out y, out n)) continue;
                p.y = y;
                if (Mathf.Abs(y - center.y) > 1.5f) continue;
                if (L.IsInsideBuilding(p, 1.5f) || L.RoadAt(p, 1.5f) != null) continue;
                if (Mathf.Abs(p.z - CityLayout.CanalZ) < CityLayout.CanalBandHalf + 2f) continue;
                if (PedManager.ExcludedVenue04C(p)) continue;
                if (Physics.CheckSphere(p + Vector3.up * 1.15f, 0.9f, SpawnBlockMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(center + Vector3.up * 1.2f, p + Vector3.up * 1.2f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) continue;
                anchor = p;
                return true;
            }
            return false;
        }

        /// <summary>Street-level ground under p (one ray from above): flat enough and within 2.5 m of the reference height.</summary>
        private static bool GroundAt(Vector3 p, float referenceY, out float y, out Vector3 normal)
        {
            y = referenceY;
            normal = Vector3.up;
            RaycastHit hit;
            if (!Physics.Raycast(new Vector3(p.x, referenceY + 60f, p.z), Vector3.down, out hit, 200f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            if (hit.normal.y < 0.8f) return false;
            if (Mathf.Abs(hit.point.y - referenceY) > 2.5f) return false;
            y = hit.point.y;
            normal = hit.normal;
            return true;
        }

        private static bool NearArena(Vector3 p)
        {
            CreatureDirector cd = Game.Creatures;
            if (cd == null || cd.Arenas == null) return false;
            for (int i = 0; i < cd.Arenas.Count; i++)
            {
                BossArena a = cd.Arenas[i];
                if (a != null && MathUtil.FlatDistance(a.Center, p) < a.Radius + EncounterSiteRules04D.SiteMargin) return true;
            }
            return false;
        }
    }
}
