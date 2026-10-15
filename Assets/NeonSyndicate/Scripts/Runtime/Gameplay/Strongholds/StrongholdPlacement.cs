using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Bounded, deterministic outskirts plots. Existing roads, sites and colliders are never moved.</summary>
    public static class StrongholdPlacement
    {
        public const float PlotHalf = 24f;
        public static Vector3 Socket(int index)
        {
            return new Vector3(index % 2 == 0 ? -11f : 11f, .07f, -8f + (index / 2) * 8f);
        }
        public static List<Vector3> Candidates(StrongholdDefinition def)
        {
            List<Vector3> points = new List<Vector3>(145);
            Vector3 origin = new Vector3(def.anchor[0], 0f, def.anchor[2]);
            points.Add(origin);
            for (int ring = 1; ring <= 6; ring++)
                for (int side = 0; side < 4; side++)
                    for (int k = -ring; k < ring; k++)
                    {
                        Vector3 offset;
                        if (side == 0) offset = new Vector3(k,0f,-ring);
                        else if (side == 1) offset = new Vector3(ring,0f,k);
                        else if (side == 2) offset = new Vector3(-k,0f,ring);
                        else offset = new Vector3(-ring,0f,-k);
                        points.Add(origin + offset * 32f);
                    }
            return points;
        }
        public static bool RegionAllowed(Vector3 p)
        {
            return Finite(p.x) && Finite(p.z) && Mathf.Abs(p.x) < CityLayout.MapHalf - 90f &&
                Mathf.Abs(p.z) < CityLayout.MapHalf - 90f &&
                (Mathf.Abs(p.x) > CityLayout.RingHalf + 70f || Mathf.Abs(p.z) > CityLayout.RingHalf + 70f) &&
                !CityLayout.IsFrostline(p.x - PlotHalf, p.z + PlotHalf);
        }
        public static bool Probe(CityLayout layout, Vector3 p, IList<Vector3> reserved, out Vector3 ground)
        {
            ground = Vector3.zero;
            if (layout == null || !RegionAllowed(p)) return false;
            foreach (Vector3 r in reserved) if (MathUtil.FlatSqrDistance(p,r) < 220f * 220f) return false;
            foreach (Lot lot in layout.DesertLots)
                if (lot != null && RectNear(p,lot.Area,PlotHalf + 8f)) return false;
            foreach (SpecialSite site in layout.Sites)
                if (site != null && MathUtil.FlatSqrDistance(p,site.Position) < (site.Radius + 42f) * (site.Radius + 42f)) return false;
            foreach (Poi poi in layout.Pois) if (poi != null && MathUtil.FlatSqrDistance(p,poi.Position) < 52f * 52f) return false;
            foreach (RoadEdge e in layout.Edges)
            {
                if (e == null) continue;
                if (e.A < 0 || e.B < 0 || e.A >= layout.Nodes.Count || e.B >= layout.Nodes.Count) return false;
                Vector3 a = layout.Nodes[e.A].Pos, b = layout.Nodes[e.B].Pos;
                float clearance = PlotHalf * 1.415f + e.Width * .5f + 5f;
                if (SegmentDistanceSq(p,a,b) < clearance * clearance) return false;
            }
            foreach (WalkNode n in layout.WalkNodes) if (n != null && MathUtil.FlatSqrDistance(p,n.Pos) < 35f * 35f) return false;
            if (NearAny(layout.PickupSpots,p,32f) || NearAny(layout.ParkingSpots,p,38f) ||
                NearAny(layout.CrystalShardSpots,p,32f) || NearAny(layout.SpellStoneSpots,p,32f) ||
                NearAny(layout.CreatureSpots,p,42f) || NearAny(layout.DataChipSpots,p,32f)) return false;
            foreach (StuntJumpDef jump in layout.StuntJumps) if (MathUtil.FlatSqrDistance(p,jump.Position) < 60f * 60f) return false;
            foreach (Interactable it in Interactable.All)
                if (it != null && MathUtil.FlatSqrDistance(p,it.transform.position) < (it.Radius + 35f) * (it.Radius + 35f)) return false;
            float min = float.PositiveInfinity, max = float.NegativeInfinity;
            for (int x = -2; x <= 2; x++)
                for (int z = -2; z <= 2; z++)
                {
                    Vector3 sample = p + new Vector3(x * 10f, 35f, z * 10f);
                    RaycastHit hit;
                    if (!Physics.Raycast(sample, Vector3.down, out hit, 65f, Layers.WorldMask, QueryTriggerInteraction.Ignore) || hit.normal.y < .985f) return false;
                    min = Mathf.Min(min,hit.point.y); max = Mathf.Max(max,hit.point.y);
                }
            if (max - min > .10f || Mathf.Abs(max) > 6f) return false;
            ground = new Vector3(p.x,max + .035f,p.z);
            // Exclude the ground itself, but reject furniture, rocks, walls and overhangs.
            if (Physics.CheckBox(ground + Vector3.up * 7.55f, new Vector3(23f,7.4f,23f),
                Quaternion.identity, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return false;
            return true;
        }
        public static float SegmentDistanceSq(Vector3 p, Vector3 a, Vector3 b)
        {
            float x = b.x - a.x, z = b.z - a.z, len = x*x + z*z;
            float t = len < .00001f ? 0f : Mathf.Clamp01(((p.x-a.x)*x+(p.z-a.z)*z)/len);
            x = p.x-a.x-t*x; z = p.z-a.z-t*z; return x*x+z*z;
        }
        private static bool RectNear(Vector3 p, RectF r, float margin)
        {
            return r.Valid && p.x > r.X0 - margin && p.x < r.X1 + margin && p.z > r.Z0 - margin && p.z < r.Z1 + margin;
        }
        private static bool NearAny(IList<Vector3> points, Vector3 p, float radius)
        {
            foreach (Vector3 other in points) if (MathUtil.FlatSqrDistance(p,other) < radius*radius) return true;
            return false;
        }
        private static bool Finite(float f) { return !float.IsNaN(f) && !float.IsInfinity(f); }
    }
}
