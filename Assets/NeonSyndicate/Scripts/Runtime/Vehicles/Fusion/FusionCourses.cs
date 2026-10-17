using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pure course building for the fusion handling courses: an out-and-back road loop from Jax's garage door along the
    /// real road network (CityLayout.FindRoute), sampled into checkpoints, with a hard-brake checkpoint at the turn and
    /// an ability checkpoint on the first straight. Engine-free, so the default city's courses are tested offline.
    /// </summary>
    public static class FusionCourses
    {
        public const string JaxHandling = "jax_handling";
        public const string JaxProfileTwo = "jax_profile_two";
        public const string CanalCrossing = "canal_crossing";

        /// <summary>The Underheart anchors the canal crossing runs through, in order (the broad transit tunnel).</summary>
        public static readonly string[] CanalAnchors = { "canals.portal_bottom", "canals.transit", "canals.crossing", "canals.exit_gardens" };

        public static int AbilityUsesFor(string id)
        {
            switch (id)
            {
                case JaxHandling: return 1;
                case JaxProfileTwo: return 2;
                case CanalCrossing: return 1;
                default: return 0;
            }
        }

        public static string TextFor(string id)
        {
            switch (id)
            {
                case JaxHandling: return "Ride Jax's handling loop: use the form ability on the first straight, brake hard at the turn, then ride back to the garage.";
                case JaxProfileTwo: return "Ride the second test loop on the other handling profile: two ability uses, a hard stop at the turn, back to the bay.";
                case CanalCrossing: return "Cross the Ember Canals transit tunnel on the fused chassis.";
                default: return "";
            }
        }

        /// <summary>
        /// Points along a polyline every spacing metres (the last point always included), at most maxPoints. The start
        /// itself is skipped (the rider is already there).
        /// </summary>
        public static List<Vector3> Sample(IList<Vector3> path, float spacing, int maxPoints)
        {
            List<Vector3> pts = new List<Vector3>();
            if (path == null || path.Count < 2 || spacing <= 0f) return pts;
            float acc = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 a = path[i - 1], b = path[i];
                float len = MathUtil.FlatDistance(a, b);
                float t = 0f;
                while (acc + (len - t) >= spacing)
                {
                    t += spacing - acc;
                    acc = 0f;
                    pts.Add(Vector3.Lerp(a, b, len > 1e-4f ? t / len : 0f));
                }
                acc += len - t;
            }
            Vector3 last = path[path.Count - 1];
            if (pts.Count == 0 || MathUtil.FlatDistance(pts[pts.Count - 1], last) > spacing * 0.35f) pts.Add(last);
            else pts[pts.Count - 1] = last;
            if (maxPoints > 1 && pts.Count > maxPoints)
            {
                List<Vector3> thin = new List<Vector3>();
                for (int i = 0; i < maxPoints; i++) thin.Add(pts[Mathf.RoundToInt(i * (pts.Count - 1) / (float)(maxPoints - 1))]);
                pts = thin;
            }
            return pts;
        }

        /// <summary>
        /// A road loop from the garage door: out along a direction (variant 0 straight out from the door, 1 to the
        /// door's right) about reach metres by road, and back to the door. False when the roads give no route.
        /// </summary>
        public static bool BuildRoadCourse(CityLayout layout, string id, Vector3 start, Vector3 outward, int variant, out FusionCourse course)
        {
            course = null;
            if (layout == null) return false;
            Vector3 dir = MathUtil.Flat(outward);
            if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward;
            dir.Normalize();
            if (variant == 1) dir = Vector3.Cross(Vector3.up, dir);
            float reach = variant == 1 ? 230f : 200f;
            Vector3 target = start + dir * reach;
            List<Vector3> outbound = layout.FindRoute(start, target);
            if (outbound.Count < 3) return false;
            // The route's last point is the raw target (it may lie off the road); turn at its projection on the road.
            outbound.RemoveAt(outbound.Count - 1);
            Vector3 turn = outbound[outbound.Count - 1];
            List<Vector3> back = layout.FindRoute(turn, start);
            if (back.Count < 2) return false;
            List<Vector3> loop = new List<Vector3>(outbound);
            for (int i = 1; i < back.Count; i++) loop.Add(back[i]);
            List<Vector3> pts = Sample(loop, 45f, 9);
            if (pts.Count < 3) return false;
            int brake = 0;
            float far = -1f;
            for (int i = 0; i < pts.Count; i++)
            {
                float d = MathUtil.FlatDistance(pts[i], start);
                if (d > far) { far = d; brake = i; }
            }
            course = new FusionCourse
            {
                Id = id,
                Points = pts.ToArray(),
                Radius = 7f,
                AbilityUses = AbilityUsesFor(id),
                Text = TextFor(id),
                BrakeAt = brake,
                AbilityAt = Mathf.Min(1, pts.Count - 1)
            };
            return true;
        }
    }
}
