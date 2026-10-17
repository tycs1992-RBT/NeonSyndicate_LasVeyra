using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// GPS: a mission destination, a player waypoint (set on the map) and, since 04E, a tracked campaign lead. Only one
    /// owns the route at a time: the active mission wins, then a waypoint the player set, then the tracked lead. Routes
    /// follow the road graph and are re-planned as the player moves; a target inside a building, upstairs or under the
    /// city is reached through its entrance, its stairs and the Underheart portals (GpsRoute04E, in the Guidance04E
    /// folder), never in a straight line through a floor. The minimap and map draw the route and the HUD the distance.
    /// </summary>
    public sealed partial class Gps : MonoBehaviour
    {
        public bool HasWaypoint { get; private set; }
        public Vector3 Waypoint { get; private set; }
        public bool HasMissionTarget { get; private set; }
        public Vector3 MissionTarget { get; private set; }
        /// <summary>04E: the tracked campaign lead (a quest giver's post or an upgrade station). Lowest priority.</summary>
        public bool HasLead { get; private set; }
        public Vector3 Lead { get; private set; }
        public string LeadLabel { get; private set; } = "";
        public string LeadKey { get; private set; } = "";
        public Color RouteColor = new Color(0.95f, 0.85f, 0.2f);
        public readonly List<Vector3> Route = new List<Vector3>();
        /// <summary>How close the player must come to pass each Route point (wide on streets, tight indoors and on stairs).</summary>
        public readonly List<float> RouteTrim = new List<float>();
        public int Version { get; private set; }
        private float _timer;
        private Vector3 _lastPlan;
        private Vector3 _lastTarget;

        public Vector3 ActiveTarget { get { return HasMissionTarget ? MissionTarget : (HasWaypoint ? Waypoint : Lead); } }
        public bool HasTarget { get { return HasMissionTarget || HasWaypoint || LeadOwnsRoute; } }
        /// <summary>True when the tracked lead owns the route right now: no mission target, no waypoint, and no mission
        /// running at all (a running mission owns the route even before it names a target).</summary>
        public bool LeadOwnsRoute { get { return HasLead && !HasMissionTarget && !HasWaypoint && (Game.Missions == null || Game.Missions.Active == null); } }
        private bool _leadOwned;

        public void SetWaypoint(Vector3 p)
        {
            Waypoint = new Vector3(p.x, 0f, p.z);
            HasWaypoint = true;
            Replan();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.Beep, 0.5f, 1.2f);
        }

        public void ClearWaypoint()
        {
            HasWaypoint = false;
            Replan();
        }

        public void SetMissionTarget(Vector3 p)
        {
            bool changed = !HasMissionTarget || (MissionTarget - p).sqrMagnitude > 4f;
            MissionTarget = p;
            HasMissionTarget = true;
            if (changed) Replan();
        }

        public void ClearMissionTarget()
        {
            if (!HasMissionTarget) return;
            HasMissionTarget = false;
            Replan();
        }

        /// <summary>Tracks a lead (keeps its height: a giver upstairs or under the city). Re-plans only when it moved.</summary>
        public void SetLead(Vector3 p, string label, string key)
        {
            bool changed = !HasLead || (Lead - p).sqrMagnitude > 4f || LeadKey != (key ?? "");
            Lead = p;
            HasLead = true;
            LeadLabel = label ?? "";
            LeadKey = key ?? "";
            if (changed && LeadOwnsRoute) Replan();
        }

        public void ClearLead()
        {
            if (!HasLead) return;
            bool owned = LeadOwnsRoute;
            HasLead = false;
            LeadLabel = "";
            LeadKey = "";
            if (owned) Replan();
        }

        public float DistanceToTarget
        {
            get
            {
                if (!HasTarget) return 0f;
                if (Route.Count >= 2) return RoutePlan04E.Length(Route);
                return MathUtil.FlatDistance(Game.PlayerPosition, ActiveTarget);
            }
        }

        private void Replan()
        {
            Route.Clear();
            RouteTrim.Clear();
            Version++;
            _leadOwned = LeadOwnsRoute;
            ClearRouteInfo();
            if (!HasTarget || Game.Layout == null) return;
            Vector3 from = Game.PlayerPosition;
            Vector3 to = ActiveTarget;
            _lastPlan = from;
            _lastTarget = to;
            PlanRoute04E(from, to);
        }

        private float TrimAt(int i)
        {
            return i < RouteTrim.Count ? RouteTrim[i] : RoutePlan04E.StreetTrim;
        }

        private void Update()
        {
            // Ownership can change without a call here (a mission starts or ends): re-plan once when it does.
            bool owned = LeadOwnsRoute;
            if (owned != _leadOwned)
            {
                _leadOwned = owned;
                Replan();
            }
            if (!HasTarget || Game.Player == null) return;
            Vector3 p = Game.PlayerPosition;
            if (HasWaypoint && !HasMissionTarget && MathUtil.FlatDistance(p, Waypoint) < 12f && Mathf.Abs(p.y - Waypoint.y) < 6f)
            {
                HasWaypoint = false;
                Replan();
                if (Game.Hud != null) Game.Hud.Notify("You have arrived.", 2f);
                return;
            }
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.5f;
            // Trim passed points (indoors and on stairs only once actually reached, on the same level) and re-plan when
            // far off the route or when the target moved.
            if (Route.Count >= 2)
            {
                Route[0] = p;
                while (Route.Count > 2 && RoutePlan04E.Passed(Route[1], TrimAt(1), p))
                {
                    Route.RemoveAt(1);
                    if (RouteTrim.Count > 1) RouteTrim.RemoveAt(1);
                }
                float off = DistanceToRoute(p);
                Vector3 target = ActiveTarget;
                if (off > 35f || MathUtil.FlatDistance(_lastTarget, target) > 5f || Mathf.Abs(_lastTarget.y - target.y) > 3f) Replan();
                else
                {
                    Version++;
                    RefreshRouteInfo(p);
                }
            }
            else Replan();
        }

        private float DistanceToRoute(Vector3 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < Route.Count; i++)
            {
                float t;
                Vector3 c = MathUtil.ClosestPointOnSegment(Route[i - 1], Route[i], p, out t);
                best = Mathf.Min(best, Vector3.Distance(c, p));
            }
            return best;
        }
    }
}
