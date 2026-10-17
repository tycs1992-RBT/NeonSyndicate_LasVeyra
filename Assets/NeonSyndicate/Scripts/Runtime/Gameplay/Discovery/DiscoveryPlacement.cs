using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Probe only existing ground. Never moves a road, building, POI, collider or player.</summary>
    public static class DiscoveryPlacement
    {
        public static Vector3 Offset(int node)
        {
            switch (node)
            {
                case 1: return new Vector3(-1.85f, 0f, 1.45f);
                case 2: return new Vector3(0f, 0f, 2.65f);
                case 3: return new Vector3(1.85f, 0f, 1.45f);
                default: return Vector3.zero;
            }
        }
        /// <summary>Capped candidates nearest the corresponding landmark, from existing pedestrian ground and parks.</summary>
        public static List<Vector3> Candidates(CityLayout layout, Vector3 anchor)
        {
            List<Vector3> points = new List<Vector3>();
            List<Vector3> parks = new List<Vector3>();
            if (layout == null) return points;
            foreach (WalkNode node in layout.WalkNodes)
                if (node != null && MathUtil.FlatSqrDistance(node.Pos, anchor) < 260f * 260f) points.Add(node.Pos);
            foreach (Block block in layout.Blocks)
            {
                if (block == null) continue;
                foreach (Lot lot in block.Lots)
                {
                    if (lot == null || lot.CustomBuilt || (lot.Use != LotUse.Park && lot.Use != LotUse.Plaza) ||
                        !lot.Area.Valid || lot.Area.Width < 12f || lot.Area.Depth < 12f) continue;
                    Vector3 c = lot.Area.Center(CityLayout.CurbHeight);
                    if (MathUtil.FlatSqrDistance(c, anchor) > 300f * 300f) continue;
                    for (int x = -1; x <= 1; x++)
                        for (int z = -1; z <= 1; z++)
                            parks.Add(c + new Vector3(x * Mathf.Min(6f, lot.Area.Width * .2f), 0f, z * Mathf.Min(6f, lot.Area.Depth * .2f)));
                }
            }
            points.Sort(delegate(Vector3 a, Vector3 b)
            {
                int order = MathUtil.FlatSqrDistance(a, anchor).CompareTo(MathUtil.FlatSqrDistance(b, anchor));
                if (order != 0) return order;
                order = a.x.CompareTo(b.x); return order != 0 ? order : a.z.CompareTo(b.z);
            });
            if (points.Count > 80) points.RemoveRange(80, points.Count - 80);
            parks.Sort(delegate(Vector3 a, Vector3 b)
            {
                int order = MathUtil.FlatSqrDistance(a, anchor).CompareTo(MathUtil.FlatSqrDistance(b, anchor));
                if (order != 0) return order;
                order = a.x.CompareTo(b.x); return order != 0 ? order : a.z.CompareTo(b.z);
            });
            if (parks.Count > 160) parks.RemoveRange(160, parks.Count - 160);
            // Reserve independent park/plaza candidates; crowded sidewalks must not starve the open-ground fallback.
            points.AddRange(parks);
            return points;
        }

        /// <summary>Four reachable standing points, with separate collider, traffic, objective and height checks.</summary>
        public static bool Probe(CityLayout layout, Vector3 at, Quaternion rotation, IList<Vector3> reserved, out Vector3[] ground)
        {
            ground = null;
            if (layout == null || !CrystalCity02FPlanner.CityPoint(at) || Mathf.Abs(at.y) > 2f) return false;
            foreach (Vector3 occupied in reserved)
                if (MathUtil.FlatSqrDistance(at, occupied) < 14f * 14f) return false;
            Vector3[] points = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = at + rotation * Offset(i);
                if (layout.IsInsideBuilding(p, .8f) || layout.RoadAt(p, .85f) != null) return false;
                if (WorldApertures.Contains(p, 2f)) return false; // 04E: never in a ground opening
                foreach (Poi poi in layout.Pois)
                    if (poi != null && MathUtil.FlatSqrDistance(p, poi.Position) < 5.5f * 5.5f) return false;
                foreach (Vector3 parking in layout.ParkingSpots)
                    if (MathUtil.FlatSqrDistance(p, parking) < 4f * 4f) return false;
                foreach (Vector3 pickup in layout.PickupSpots)
                    if (MathUtil.FlatSqrDistance(p, pickup) < 3f * 3f) return false;
                foreach (Vector3 pickup in layout.CrystalShardSpots)
                    if (MathUtil.FlatSqrDistance(p, pickup) < 3f * 3f) return false;
                foreach (Interactable it in Interactable.All)
                    if (it != null && MathUtil.FlatSqrDistance(p, it.transform.position) < (it.Radius + 2f) * (it.Radius + 2f)) return false;
                RaycastHit hit;
                if (!Physics.Raycast(p + Vector3.up * 2.5f, Vector3.down, out hit, 5f, Layers.WorldMask, QueryTriggerInteraction.Ignore) ||
                    hit.normal.y < .93f || Mathf.Abs(hit.point.y - at.y) > .65f) return false;
                p.y = hit.point.y;
                if (Physics.CheckCapsule(p + Vector3.up * .6f, p + Vector3.up * 1.7f, .43f,
                    Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
                points[i] = p;
            }
            for (int i = 1; i < 4; i++)
                if (Physics.Linecast(points[0] + Vector3.up * .7f, points[i] + Vector3.up * .7f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            // Require an unobstructed approach to an existing pedestrian node, not just an isolated clear pocket.
            bool approach = false;
            foreach (WalkNode node in layout.WalkNodes)
            {
                if (node == null || Mathf.Abs(node.Pos.y - points[0].y) > 1f ||
                    MathUtil.FlatSqrDistance(node.Pos, points[0]) > 18f * 18f) continue;
                if (!Physics.Linecast(points[0] + Vector3.up * 1.2f, node.Pos + Vector3.up * 1.2f,
                    Layers.WorldMask, QueryTriggerInteraction.Ignore)) { approach = true; break; }
            }
            if (!approach) return false;
            ground = points; return true;
        }
    }
}
