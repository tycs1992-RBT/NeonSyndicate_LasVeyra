using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// GPS: a player waypoint (set on the map) and a mission destination. Routes follow the road graph and are
    /// re-planned as the player moves; the minimap draws them and the HUD shows the distance.
    /// </summary>
    public sealed class Gps : MonoBehaviour
    {
        public bool HasWaypoint { get; private set; }
        public Vector3 Waypoint { get; private set; }
        public bool HasMissionTarget { get; private set; }
        public Vector3 MissionTarget { get; private set; }
        public Color RouteColor = new Color(0.95f, 0.85f, 0.2f);
        public readonly List<Vector3> Route = new List<Vector3>();
        public int Version { get; private set; }
        private float _timer;
        private Vector3 _lastPlan;
        private Vector3 _lastTarget;

        public Vector3 ActiveTarget { get { return HasMissionTarget ? MissionTarget : Waypoint; } }
        public bool HasTarget { get { return HasMissionTarget || HasWaypoint; } }

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

        public float DistanceToTarget
        {
            get
            {
                if (!HasTarget) return 0f;
                float d = 0f;
                if (Route.Count >= 2)
                {
                    for (int i = 1; i < Route.Count; i++) d += MathUtil.FlatDistance(Route[i - 1], Route[i]);
                    return d;
                }
                return MathUtil.FlatDistance(Game.PlayerPosition, ActiveTarget);
            }
        }

        private void Replan()
        {
            Route.Clear();
            Version++;
            if (!HasTarget || Game.Layout == null) return;
            Vector3 from = Game.PlayerPosition;
            Vector3 to = ActiveTarget;
            _lastPlan = from;
            _lastTarget = to;
            if (MathUtil.FlatDistance(from, to) < 60f)
            {
                Route.Add(from);
                Route.Add(to);
                return;
            }
            List<Vector3> r = Game.Layout.FindRoute(from, to);
            if (r.Count == 0)
            {
                Route.Add(from);
                Route.Add(to);
            }
            else Route.AddRange(r);
        }

        private void Update()
        {
            if (!HasTarget || Game.Player == null) return;
            if (HasWaypoint && !HasMissionTarget && MathUtil.FlatDistance(Game.PlayerPosition, Waypoint) < 12f)
            {
                HasWaypoint = false;
                Replan();
                if (Game.Hud != null) Game.Hud.Notify("You have arrived.", 2f);
                return;
            }
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.5f;
            Vector3 p = Game.PlayerPosition;
            // Trim passed points and re-plan when far off the route.
            if (Route.Count >= 2)
            {
                Route[0] = p;
                while (Route.Count > 2 && MathUtil.FlatDistance(p, Route[1]) < 14f) Route.RemoveAt(1);
                float off = DistanceToRoute(p);
                if (off > 35f || MathUtil.FlatDistance(_lastTarget, ActiveTarget) > 5f) Replan();
                else Version++;
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
                best = Mathf.Min(best, MathUtil.FlatDistance(c, p));
            }
            return best;
        }
    }
}
