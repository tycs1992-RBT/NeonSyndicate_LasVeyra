using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Roof equipment for the plain flat roofs: today a single-tier Flat roof is an empty slab, the clearest "cheap"
    /// tell from any rooftop or flight. Vents, pipes, air units, hatches and dishes are planned per building from its
    /// seed and stay collider-less (the roof collider is untouched, so travel and landings do not change).
    /// </summary>
    public sealed partial class DressingPlanner
    {
        /// <summary>Roof pieces for every plain, single-tier flat roof of the block.</summary>
        private void PlanRoofs(Block b)
        {
            foreach (Lot lot in b.Lots)
            {
                foreach (Building bld in lot.Buildings)
                {
                    if (!RoofEligible(bld) || Skip(bld)) continue;
                    PlanRoof(bld, new GRand(bld.Seed * 41 + Salt + 3));
                }
            }
        }

        /// <summary>Plain flat roofs only: one tier (the roof is the footprint) and no helipad, antenna or props yet.</summary>
        public static bool RoofEligible(Building bld)
        {
            if (bld.Roof != RoofType.Flat || bld.Tiers > 1) return false;
            if (bld.Height < 6f || !bld.Footprint.Valid) return false;
            return bld.Footprint.Width >= 6f && bld.Footprint.Depth >= 6f;
        }

        private void PlanRoof(Building bld, GRand r)
        {
            RectF roof = bld.Footprint;
            float y = CityLayout.CurbHeight + bld.Height;
            int budget = Mathf.Min(DressingRules.MaxRoofPerBuilding, 2 + Mathf.FloorToInt(roof.Area / 140f));
            int placed = 0;
            // A pipe run along the longest edge, inset from the parapet.
            if (r.Chance(0.7f) && placed < budget)
            {
                bool alongX = roof.Width >= roof.Depth;
                float inset = 0.55f;
                float len = (alongX ? roof.Width : roof.Depth) - 2f;
                float side = r.Chance(0.5f) ? 1f : -1f;
                Vector3 c = alongX
                    ? new Vector3(roof.CenterX, y, side > 0f ? roof.Z1 - inset : roof.Z0 + inset)
                    : new Vector3(side > 0f ? roof.X1 - inset : roof.X0 + inset, y, roof.CenterZ);
                Add(DressKind.RoofPipe, c, alongX ? 90f : 0f, new Vector2(len * r.Range(0.6f, 1f), 0f), r, r.Range(0, 2));
                placed++;
            }
            int vents = r.Range(1, 4);
            for (int i = 0; i < vents && placed < budget; i++)
            {
                Vector3 p = RoofPoint(roof, y, r, 1.2f);
                Add(DressKind.RoofVent, p, r.Range(0f, 360f), new Vector2(r.Range(0.8f, 1.3f), 0f), r, r.Range(0, 3));
                placed++;
            }
            if (r.Chance(0.6f) && placed < budget)
            {
                Vector3 p = RoofPoint(roof, y, r, 1.4f);
                Add(DressKind.RoofAc, p, r.Range(0f, 4) * 90f, new Vector2(r.Range(0.9f, 1.25f), 0f), r, r.Range(0, 2));
                placed++;
            }
            if (r.Chance(0.5f) && placed < budget)
            {
                Vector3 p = RoofPoint(roof, y, r, 1.2f);
                Add(DressKind.RoofHatch, p, r.Range(0f, 4) * 90f, new Vector2(1f, 0f), r, 0);
                placed++;
            }
            if (r.Chance(0.25f) && placed < budget)
            {
                Vector3 p = RoofPoint(roof, y, r, 1f);
                Add(DressKind.RoofDish, p, r.Range(0f, 360f), new Vector2(r.Range(0.8f, 1.2f), 0f), r, 0);
                placed++;
            }
        }

        private static Vector3 RoofPoint(RectF roof, float y, GRand r, float margin)
        {
            return new Vector3(r.Range(roof.X0 + margin, roof.X1 - margin), y, r.Range(roof.Z0 + margin, roof.Z1 - margin));
        }
    }
}
