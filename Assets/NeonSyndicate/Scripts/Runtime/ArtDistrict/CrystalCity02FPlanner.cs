using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Deterministic placement arithmetic; no native scene operations or global random calls.</summary>
    public static class CrystalCity02FPlanner
    {
        public const float BaseRadius = 4.8f;
        public const float RoofClearance = 12f;
        public const int MaximumHeroes = 3;

        public static List<CrystalCity02FRoofSite> SelectRoofs(CityLayout layout)
        {
            List<CrystalCity02FRoofSite> result = new List<CrystalCity02FRoofSite>(MaximumHeroes);
            if (layout == null) return result;
            List<CrystalCity02FRoofSite> candidates = new List<CrystalCity02FRoofSite>();
            foreach (Block block in layout.Blocks)
            {
                if (block == null) continue;
                foreach (Lot lot in block.Lots)
                {
                    if (lot == null || lot.CustomBuilt) continue;
                    foreach (Building building in lot.Buildings)
                    {
                        RectF roof;
                        if (!TryTopRoof(building, out roof)) continue;
                        float scale = RoofScale(roof);
                        if (scale < .65f) continue;
                        candidates.Add(new CrystalCity02FRoofSite
                        {
                            Building = building, Roof = roof, Scale = scale,
                            Position = roof.Center(CityLayout.CurbHeight + building.Height + RoofClearance),
                            Style = CrystalCity02DMaterials.StyleAt(roof.Center(0f))
                        });
                    }
                }
            }
            Poi home = layout.FindPoi("Veyra Lofts");
            Vector3 arrival = home != null ? home.Position : new Vector3(42f, 0f, 0f);
            CrystalCity02FRoofSite first = Nearest(candidates, arrival, null);
            if (first != null) { first.NearArrival = true; result.Add(first); }
            for (int style = 0; style < 3; style++)
            {
                if (first != null && (int)first.Style == style) continue;
                Vector3 focus = style == (int)CrystalCityStyle.VerdantReach ? new Vector3(-460f, 0f, 80f) :
                    (style == (int)CrystalCityStyle.DawnstoneCourt ? new Vector3(470f, 0f, 50f) : new Vector3(40f, 0f, 70f));
                CrystalCity02FRoofSite site = Nearest(candidates, focus, (CrystalCityStyle)style);
                if (site != null) result.Add(site);
            }
            return result;
        }

        private static CrystalCity02FRoofSite Nearest(List<CrystalCity02FRoofSite> candidates, Vector3 focus, CrystalCityStyle? style)
        {
            CrystalCity02FRoofSite best = null;
            float score = float.PositiveInfinity;
            foreach (CrystalCity02FRoofSite candidate in candidates)
            {
                if (style.HasValue && candidate.Style != style.Value) continue;
                float next = MathUtil.FlatSqrDistance(candidate.Position, focus) + candidate.Building.Height * candidate.Building.Height * .12f;
                if (next > score) continue;
                if (next == score && best != null && !Before(candidate, best)) continue;
                score = next; best = candidate;
            }
            return best;
        }

        private static bool Before(CrystalCity02FRoofSite a, CrystalCity02FRoofSite b)
        {
            if (a.Building.Seed != b.Building.Seed) return a.Building.Seed < b.Building.Seed;
            if (a.Roof.X0 != b.Roof.X0) return a.Roof.X0 < b.Roof.X0;
            return a.Roof.Z0 < b.Roof.Z0;
        }

        public static bool TryTopRoof(Building b, out RectF roof)
        {
            roof = new RectF();
            if (b == null || b.Roof == RoofType.Helipad || b.Roof == RoofType.Antenna ||
                !b.Footprint.Valid || !CrystalCity02FCatalog.Finite(b.Height) || b.Height < 14f || b.Height > 115f ||
                !CityPoint(b.Footprint.Center(0f))) return false;
            // Consume the same seeded tier draws as CityBuilder, without modifying its random state.
            int tiers = Mathf.Clamp(b.Tiers, 1, 3);
            GRand r = new GRand(b.Seed);
            float inset = 0f;
            for (int t = 0; t < tiers; t++)
            {
                if (tiers > 1 && t != tiers - 1)
                {
                    if (t == 0) r.Range(.45f, .65f); else r.Range(.75f, .88f);
                }
                inset = t == 0 ? 0f : Mathf.Min(b.Footprint.Width, b.Footprint.Depth) * (.1f * t + r.Range(.02f, .06f));
                if (b.Footprint.Width - 2f * inset < 3f || b.Footprint.Depth - 2f * inset < 3f) return false;
            }
            // RectF sorts its endpoints. Check the raw width first to reject inverted insets.
            float width = b.Footprint.Width - 2f * inset, depth = b.Footprint.Depth - 2f * inset;
            if (width < 10f || depth < 10f) return false;
            roof = new RectF(b.Footprint.X0 + inset, b.Footprint.Z0 + inset, b.Footprint.X1 - inset, b.Footprint.Z1 - inset);
            return true;
        }

        public static float RoofScale(RectF roof)
        {
            return Mathf.Clamp((Mathf.Min(roof.Width, roof.Depth) - 2.4f) / (BaseRadius * 2f), .1f, 1.45f);
        }
        public static bool CityPoint(Vector3 p)
        {
            return CrystalCity02FCatalog.Finite(p.x) && CrystalCity02FCatalog.Finite(p.y) && CrystalCity02FCatalog.Finite(p.z) &&
                Mathf.Abs(p.x) <= CityLayout.CoreHalf && Mathf.Abs(p.z) <= CityLayout.CoreHalf;
        }
        public static bool ClearGardenPocket(CityLayout layout, Lot lot, Vector3 point, float radius)
        {
            if (layout == null || lot == null || !CityPoint(point) || radius <= 0f || !CrystalCity02FCatalog.Finite(radius)) return false;
            RectF area = lot.Area;
            if (point.x - radius < area.X0 + 1f || point.x + radius > area.X1 - 1f ||
                point.z - radius < area.Z0 + 1f || point.z + radius > area.Z1 - 1f) return false;
            foreach (Poi poi in layout.Pois)
                if (poi != null && MathUtil.FlatSqrDistance(point, poi.Position) < (radius + 6f) * (radius + 6f)) return false;
            foreach (Vector3 parking in layout.ParkingSpots)
                if (MathUtil.FlatSqrDistance(point, parking) < (radius + 4f) * (radius + 4f)) return false;
            foreach (Vector3 item in layout.CrystalShardSpots)
                if (MathUtil.FlatSqrDistance(point, item) < (radius + 3f) * (radius + 3f)) return false;
            foreach (Vector3 item in layout.PickupSpots)
                if (MathUtil.FlatSqrDistance(point, item) < (radius + 3f) * (radius + 3f)) return false;
            foreach (WalkLink link in layout.WalkLinks)
            {
                if (link == null || link.A < 0 || link.B < 0 || link.A >= layout.WalkNodes.Count || link.B >= layout.WalkNodes.Count) continue;
                if (layout.WalkNodes[link.A] == null || layout.WalkNodes[link.B] == null) continue;
                Vector3 a = layout.WalkNodes[link.A].Pos, b = layout.WalkNodes[link.B].Pos;
                Vector3 delta = MathUtil.Flat(b - a);
                float t = delta.sqrMagnitude < .0001f ? 0f : Mathf.Clamp01(Vector3.Dot(MathUtil.Flat(point - a), delta) / delta.sqrMagnitude);
                if (MathUtil.FlatSqrDistance(point, a + delta * t) < (radius + 2f) * (radius + 2f)) return false;
            }
            return true;
        }
        public static int StreetBudget(GraphicsPreset quality)
        {
            return quality == GraphicsPreset.Low ? 12 : (quality == GraphicsPreset.Medium ? 24 : (quality == GraphicsPreset.High ? 42 : 60));
        }
        public static float SpinAngle(float elapsed, float speed)
        {
            return Mathf.Repeat(elapsed * speed, 360f);
        }
        public static float NightLight(float night)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, .85f, night));
        }
    }
}
