using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Every kind of small-scale dressing the Phase 4 visual pass scatters over the city: flat decals on roads and
    /// sidewalks, wall decals on facades, collider-less clutter, roof equipment and extra foliage cards.
    /// </summary>
    public enum DressKind
    {
        // Ground decals (flat quads, normal up).
        Grime,
        OilStain,
        DrainStain,
        Manhole,
        MarkingWear,
        Skid,
        Scorch,
        // Wall decals (vertical quads flush with a facade).
        PosterScrap,
        WaterStain,
        // Clutter (small merged meshes, never a collider).
        Bottle,
        Can,
        PaperBall,
        CardboardBox,
        Crate,
        Pallet,
        TyreStack,
        CableReel,
        WheelieBin,
        Cable,
        Planter,
        // Roof equipment.
        RoofVent,
        RoofPipe,
        RoofAc,
        RoofHatch,
        RoofDish,
        // Foliage cards.
        GrassTuft,
        ScrubTuft
    }

    /// <summary>One planned piece of dressing. Pos is the surface point (ground level for ground items).</summary>
    public sealed class DressItem
    {
        public DressKind Kind;
        public Vector3 Pos;
        /// <summary>Heading in degrees around Y (quad rotation for ground decals, facing for clutter).</summary>
        public float Yaw;
        /// <summary>Width and depth (ground decals), width and height (wall decals), or a scale for clutter.</summary>
        public Vector2 Size;
        /// <summary>Outward facade normal for wall decals; up for everything else.</summary>
        public Vector3 Normal = Vector3.up;
        /// <summary>The lowest preset that shows this item.</summary>
        public GraphicsPreset Tier;
        public int Variant;
        public int Seed;
        public bool IsWall { get { return Kind == DressKind.PosterScrap || Kind == DressKind.WaterStain; } }
        public bool IsDecal { get { return Kind <= DressKind.WaterStain; } }
        public bool IsRoof { get { return Kind >= DressKind.RoofVent && Kind <= DressKind.RoofDish; } }
        public bool IsFoliage { get { return Kind == DressKind.GrassTuft || Kind == DressKind.ScrubTuft; } }
    }

    /// <summary>A circle nothing may be placed in (a door, a quest point, a lamp post, a parked car).</summary>
    public struct Keepout
    {
        public float X, Z, Radius;
        public Keepout(float x, float z, float radius) { X = x; Z = z; Radius = radius; }
        public Keepout(Vector3 p, float radius) { X = p.x; Z = p.z; Radius = radius; }
    }

    /// <summary>
    /// The pure rules of the dressing pass: which preset shows how much, how the deterministic rank of an item maps
    /// to a preset tier, per-block caps and the keepout tests. Engine-free so the EditMode tests can prove them.
    /// </summary>
    public static class DressingRules
    {
        /// <summary>Share of the full (Ultra) set that Medium shows.</summary>
        public const float MediumShare = 0.3f;
        /// <summary>Share of the full (Ultra) set that High shows (Ultra shows everything).</summary>
        public const float HighShare = 0.72f;

        /// <summary>Most decals one block may plan in its full set (Ultra); the lower tiers are shares of it.</summary>
        public const int MaxDecalsPerBlock = 96;
        /// <summary>Most clutter pieces one block may plan in its full set.</summary>
        public const int MaxClutterPerBlock = 64;
        /// <summary>Most roof pieces one building may plan.</summary>
        public const int MaxRoofPerBuilding = 7;
        /// <summary>Most foliage cards one lot may plan.</summary>
        public const int MaxFoliagePerLot = 320;

        /// <summary>Clearance kept around a point of interest, a quest giver's door and a service door.</summary>
        public const float PoiClearance = 7f;
        /// <summary>Clearance kept around a lot entrance (the walk into a lot).</summary>
        public const float EntranceClearance = 3f;
        /// <summary>Clearance kept around a walk-in venue door.</summary>
        public const float VenueDoorClearance = 6f;
        /// <summary>Clearance kept around street furniture, lamps and trees.</summary>
        public const float FurnitureClearance = 1.2f;
        /// <summary>Clearance kept around a parking spot (a parked car stands there).</summary>
        public const float ParkingClearance = 3.6f;
        /// <summary>Distance a sidewalk piece keeps from the walk line in the middle of the sidewalk.</summary>
        public const float WalkLineClearance = 1.1f;
        /// <summary>Extra clearance around a ground opening (an Underheart entrance cut).</summary>
        public const float ApertureClearance = 2f;

        /// <summary>True when the preset shows any dressing at all (Low shows nothing new).</summary>
        public static bool Enabled(GraphicsPreset g)
        {
            return g >= GraphicsPreset.Medium;
        }

        /// <summary>The preset tier an item of deterministic rank r (0..1) belongs to.</summary>
        public static GraphicsPreset TierFor(float r)
        {
            if (r < MediumShare) return GraphicsPreset.Medium;
            if (r < HighShare) return GraphicsPreset.High;
            return GraphicsPreset.Ultra;
        }

        /// <summary>True when an item of this tier is shown on the preset.</summary>
        public static bool Shows(GraphicsPreset tier, GraphicsPreset preset)
        {
            return preset >= GraphicsPreset.Medium && tier <= preset;
        }

        /// <summary>How many of a full set of n items the preset shows (exact for the shares above).</summary>
        public static int CountFor(int n, GraphicsPreset g)
        {
            switch (g)
            {
                case GraphicsPreset.Low: return 0;
                case GraphicsPreset.Medium: return Mathf.RoundToInt(n * MediumShare);
                case GraphicsPreset.High: return Mathf.RoundToInt(n * HighShare);
                default: return n;
            }
        }

        /// <summary>How run-down a district looks for litter and stains (0 clean to 1 derelict).</summary>
        public static float Grit(District d)
        {
            switch (d)
            {
                case District.AshUnderpass: return 1f;
                case District.GraveFrequency: return 0.85f;
                case District.BrassProjects: return 0.8f;
                case District.OldSilver: return 0.7f;
                case District.SunkenRow: return 0.6f;
                case District.QuartzBadlands: return 0.5f;
                case District.SkyrailOrbit: return 0.3f;
                case District.LasVeyraCore: return 0.2f;
                case District.MirageStrip: return 0.12f;
                case District.GlassHeights: return 0.08f;
                default: return 0.1f;
            }
        }

        public static bool Clear(IList<Keepout> keepouts, float x, float z, float radius)
        {
            for (int i = 0; i < keepouts.Count; i++)
            {
                Keepout k = keepouts[i];
                float dx = k.X - x, dz = k.Z - z;
                float r = k.Radius + radius;
                if (dx * dx + dz * dz < r * r) return false;
            }
            return true;
        }

        public static bool Clear(IList<Keepout> keepouts, Vector3 p, float radius)
        {
            return Clear(keepouts, p.x, p.z, radius);
        }

        /// <summary>True when (x, z) is inside the rectangle grown by margin.</summary>
        public static bool Inside(RectF r, float x, float z, float margin)
        {
            return x > r.X0 - margin && x < r.X1 + margin && z > r.Z0 - margin && z < r.Z1 + margin;
        }

        /// <summary>True when the point is inside any building footprint of the block, grown by margin.</summary>
        public static bool InsideBuilding(Block b, float x, float z, float margin)
        {
            foreach (Lot lot in b.Lots)
                foreach (Building bld in lot.Buildings)
                    if (Inside(bld.Footprint, x, z, margin)) return true;
            return false;
        }

        /// <summary>
        /// True when a facade edge of a footprint lies along the block's inner edge (so it faces the sidewalk).
        /// side: 0 west (X0), 1 east (X1), 2 south (Z0), 3 north (Z1).
        /// </summary>
        public static bool FacesSidewalk(RectF footprint, RectF inner, int side)
        {
            const float eps = 0.9f;
            switch (side)
            {
                case 0: return Mathf.Abs(footprint.X0 - inner.X0) < eps;
                case 1: return Mathf.Abs(footprint.X1 - inner.X1) < eps;
                case 2: return Mathf.Abs(footprint.Z0 - inner.Z0) < eps;
                default: return Mathf.Abs(footprint.Z1 - inner.Z1) < eps;
            }
        }

        /// <summary>Most metres a facade may stand back from the sidewalk and still count as a street-facing wall.</summary>
        public const float MaxSetback = 7.5f;

        /// <summary>
        /// True when a footprint side looks out at the sidewalk across open lot ground: it is within MaxSetback of the
        /// block's inner edge and no other building of the block stands in the strip between them. This is the rule
        /// the facade dressing uses; most buildings stand one to three metres back from the sidewalk on a forecourt.
        /// </summary>
        public static bool FacesOpen(Block b, Building bld, int side)
        {
            RectF f = bld.Footprint;
            RectF inner = b.Inner;
            float gap;
            RectF strip;
            switch (side)
            {
                case 0: gap = f.X0 - inner.X0; strip = new RectF(inner.X0, f.Z0, f.X0, f.Z1); break;
                case 1: gap = inner.X1 - f.X1; strip = new RectF(f.X1, f.Z0, inner.X1, f.Z1); break;
                case 2: gap = f.Z0 - inner.Z0; strip = new RectF(f.X0, inner.Z0, f.X1, f.Z0); break;
                default: gap = inner.Z1 - f.Z1; strip = new RectF(f.X0, f.Z1, f.X1, inner.Z1); break;
            }
            if (gap > MaxSetback) return false;
            if (gap <= 0.05f) return true;
            foreach (Lot lot in b.Lots)
            {
                foreach (Building other in lot.Buildings)
                {
                    if (other == bld) continue;
                    if (other.Footprint.Overlaps(strip, -0.05f)) return false;
                }
            }
            return true;
        }

        /// <summary>Outward normal of a side (0 west, 1 east, 2 south, 3 north).</summary>
        public static Vector3 SideNormal(int side)
        {
            switch (side)
            {
                case 0: return Vector3.left;
                case 1: return Vector3.right;
                case 2: return Vector3.back;
                default: return Vector3.forward;
            }
        }

        /// <summary>Start and end of a footprint side at ground level.</summary>
        public static void SideSegment(RectF f, int side, out Vector3 a, out Vector3 b)
        {
            switch (side)
            {
                case 0: a = new Vector3(f.X0, 0f, f.Z0); b = new Vector3(f.X0, 0f, f.Z1); break;
                case 1: a = new Vector3(f.X1, 0f, f.Z0); b = new Vector3(f.X1, 0f, f.Z1); break;
                case 2: a = new Vector3(f.X0, 0f, f.Z0); b = new Vector3(f.X1, 0f, f.Z0); break;
                default: a = new Vector3(f.X0, 0f, f.Z1); b = new Vector3(f.X1, 0f, f.Z1); break;
            }
        }

        /// <summary>Keeps a point out of the walk line that runs through the middle of a block's sidewalk ring.</summary>
        public static bool OnWalkLine(Block b, float x, float z)
        {
            float half = CityLayout.SidewalkWidth * 0.5f;
            RectF w = b.Curb.Inset(half);
            bool nearX = Mathf.Abs(x - w.X0) < WalkLineClearance || Mathf.Abs(x - w.X1) < WalkLineClearance;
            bool nearZ = Mathf.Abs(z - w.Z0) < WalkLineClearance || Mathf.Abs(z - w.Z1) < WalkLineClearance;
            bool inX = x > w.X0 - WalkLineClearance && x < w.X1 + WalkLineClearance;
            bool inZ = z > w.Z0 - WalkLineClearance && z < w.Z1 + WalkLineClearance;
            return (nearX && inZ) || (nearZ && inX);
        }
    }
}
