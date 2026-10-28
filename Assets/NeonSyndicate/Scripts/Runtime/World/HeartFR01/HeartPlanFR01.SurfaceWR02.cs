using System;
using UnityEngine;

namespace NeonSyndicate
{
    public enum HeartSurfaceKindWR02 { Preserve, Causeway, Ground, Strata, Bark, Canopy, Markings, Wire, Bronze, AuthoredCliff }

    public static partial class HeartPlanFR01
    {
        /// <summary>Fifteen existing 160 metre chunks around the home, services, grotto and original Azure walk.</summary>
        public static bool SurfaceRegionWR02(CityLayout layout, Vector3 point)
        {
            return SurfaceRegionWR02(layout != null && layout.GothicCivicH5, point);
        }

        public static bool SurfaceRegionWR02(bool revision10, Vector3 point)
        {
            if (!revision10 || !MathUtil.IsFinite(point)) return false;
            int x = Mathf.FloorToInt(point.x / CityBuilder.ChunkSize), z = Mathf.FloorToInt(point.z / CityBuilder.ChunkSize);
            return x >= -3 && x <= 1 && z >= -1 && z <= 1;
        }

        public static string SurfaceOwnerWR02(string caller)
        {
            if (caller.StartsWith("WR02/tree-", StringComparison.Ordinal)) return "CityBuilder.BuildFracturedWilds04OC / GothicRealmGeometry04OB.WildTree04OC";
            if (caller == "WR02/terrain") return "CityBuilder.BuildFracturedWilds04OC / GothicRealmGeometry04OB.WildTerrain04OC";
            if (caller.StartsWith("WR02/cliff", StringComparison.Ordinal)) return "CityBuilder.BuildRealmLandscape04OB / EmitRealmSolid04OB";
            return "CityBuilder." + caller;
        }

        /// <summary>Only audited original visual append paths qualify. Unrecognized owners always retain their batch.</summary>
        public static HeartSurfaceKindWR02 SurfaceKindWR02(string owner, bool lane, bool curb, bool grass)
        {
            if (owner == "WR02/tree-canopy") return HeartSurfaceKindWR02.Canopy;
            if (owner == "WR02/cliff") return HeartSurfaceKindWR02.AuthoredCliff;
            if (owner == "WR02/tree-trunk") return HeartSurfaceKindWR02.Bark;
            if (owner == "WR02/terrain") return grass ? HeartSurfaceKindWR02.Ground : HeartSurfaceKindWR02.Strata;
            if (owner == "Wire") return HeartSurfaceKindWR02.Wire;
            if (owner == "SignOnBuilding") return HeartSurfaceKindWR02.Bronze;
            if (owner == "BuildPowerLines") return HeartSurfaceKindWR02.Bark;
            if (owner == "EmitRealmSolid04OB") return HeartSurfaceKindWR02.Strata;
            if (owner == "BuildGround") return HeartSurfaceKindWR02.Ground;
            bool surface = owner == "BuildRoadEdge" || owner == "BuildIntersection" || owner == "BuildMedian" ||
                owner == "SolidLine" || owner == "DashedLine" || owner == "StopAndCrosswalk" ||
                owner == "ParkingLines" || owner == "TiledRectPiece" || owner == "BuildRealmLandscape04OB";
            if (!surface) return HeartSurfaceKindWR02.Preserve;
            if (lane) return HeartSurfaceKindWR02.Markings;
            return grass ? HeartSurfaceKindWR02.Ground : curb ? HeartSurfaceKindWR02.Strata : HeartSurfaceKindWR02.Causeway;
        }

        /// <summary>Stable source identity, not a renderer instance id or a new save identity.</summary>
        public static string SurfaceKeyWR02(string owner, Vector3 point, int slot)
        {
            return "revision:10/source:" + owner + "/chunk:" + Mathf.FloorToInt(point.x / 160f) + "," +
                Mathf.FloorToInt(point.z / 160f) + "/slot:" + slot;
        }

        public static bool SurfaceActiveWR02(bool session, bool selected, bool compare, bool suspended, bool ready)
        { return session && selected && !compare && !suspended && ready; }

        /// <summary>Only noncolliding natural foliage is fitted. Original branches, trunk and every physical surface stay.</summary>
        public static bool CanopyFitWR02(Bounds source, Bounds target, out Vector3 position, out Vector3 scale)
        {
            position = Vector3.zero; scale = Vector3.one;
            if (!MathUtil.IsFinite(source.center) || !MathUtil.IsFinite(source.size) || !MathUtil.IsFinite(target.center) || !MathUtil.IsFinite(target.size)) return false;
            if (source.size.x < .01f || source.size.y < .01f || source.size.z < .01f ||
                target.size.x < .2f || target.size.y < .2f || target.size.z < .2f ||
                target.size.x > 45f || target.size.y > 35f || target.size.z > 45f) return false;
            scale = new Vector3(target.size.x / source.size.x, target.size.y / source.size.y, target.size.z / source.size.z);
            position = target.min - Vector3.Scale(source.min, scale);
            return MathUtil.IsFinite(position) && MathUtil.IsFinite(scale);
        }

        public static int CanopyVariantWR02(int seed)
        { return (int)((uint)seed % 3u); }

        /// <summary>Exact existing source-specific cliff frames. Unknown seeds, shifted frames or changed bounds retain original geometry.</summary>
        public static bool CliffSourceWR02(int seed, Vector3 origin, Bounds bounds)
        {
            Vector3 expected;
            if (seed == 62346) expected = new Vector3(-133, .15f, 202);
            else if (seed == 63279) expected = new Vector3(137, .15f, 202);
            else if (seed == 61724) expected = new Vector3(-133, .15f, -22);
            else return false;
            return MathUtil.IsFinite(origin) && (origin - expected).sqrMagnitude < .0001f &&
                CliffCatalogBoundsWR02("Cliff" + seed, new Bounds(bounds.center - origin, bounds.size));
        }

        public static bool CliffCatalogBoundsWR02(string family, Bounds bounds)
        {
            if (!MathUtil.IsFinite(bounds.center) || !MathUtil.IsFinite(bounds.size)) return false;
            Vector3 min, max;
            switch (family)
            {
                case "Cliff62346": min = new Vector3(-5.63815f, 0, -18.71135f); max = new Vector3(7.29f, 34, 18.71135f); break;
                case "Cliff63279": min = new Vector3(-5.63815f, 0, -18.71135f); max = new Vector3(6.84f, 34, 18.71135f); break;
                case "Cliff61724": min = new Vector3(-5.63815f, 0, -18.711346f); max = new Vector3(6, 24, 18.89846f); break;
                default: return false;
            }
            return (bounds.min - min).sqrMagnitude < .0001f && (bounds.max - max).sqrMagnitude < .0001f;
        }
    }
}
