using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Pure CityLayout selection. Original footprints remain authoritative outdoor spawn exclusions.</summary>
    public static class InhabitedCityBindings04C
    {
        public static void ResetStatics() { }
        public static List<VenueBinding04C> Select(CityLayout layout, VenueCatalog04C catalog)
        {
            if (layout == null || catalog == null || catalog.Version != "04C" || catalog.Venues == null)
                throw new ArgumentException("04C venue catalog or layout is missing.");
            List<VenueBinding04C> result = new List<VenueBinding04C>();
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            HashSet<Building> used = new HashSet<Building>();
            Poi home = layout.FindPoi("Veyra Lofts");
            foreach (VenueDefinition04C definition in catalog.Venues)
            {
                if (!ValidDefinition(definition) || !ids.Add(definition.Id))
                    throw new ArgumentException("04C venue IDs must be present and unique.");
                VenueBinding04C binding = new VenueBinding04C { Definition = definition };
                Poi poi = string.IsNullOrEmpty(definition.PoiName) ? null : layout.FindPoi(definition.PoiName);
                bool canonical = !string.IsNullOrEmpty(definition.PoiName);
                Vector3 desired = canonical && poi != null ? poi.Position :
                    (home == null ? Vector3.zero : home.Position) + new Vector3(definition.OffsetX, 0f, definition.OffsetZ);
                float best = float.MaxValue;
                foreach (Block block in layout.Blocks)
                foreach (Lot lot in block.Lots)
                {
                    if (lot.CustomBuilt || (canonical ? poi == null || lot.PoiName != definition.PoiName : lot.Poi != PoiType.None || !string.IsNullOrEmpty(lot.PoiName))) continue;
                    if (!canonical && definition.Kind == "depot" && lot.Use != LotUse.Warehouse) continue;
                    foreach (Building building in lot.Buildings)
                    {
                        if (used.Contains(building) || !Eligible(building)) continue;
                        Vector3 facing = canonical ? poi.Facing : new Vector3(definition.FacingX, 0f, definition.FacingZ);
                        if (facing.sqrMagnitude < .5f) facing = lot.Facing;
                        Vector3 origin; float width, depth, yaw;
                        Door(building.Footprint, facing, out origin, out width, out depth, out yaw);
                        if (width < 17f || depth < 19f || ((definition.Kind == "lofts" || definition.Kind == "observatory") && building.Height < 10f)) continue;
                        float distance = MathUtil.FlatSqrDistance(origin, desired);
                        if (!canonical && distance > definition.MaxDistance * definition.MaxDistance) continue;
                        float score = building.Seed == definition.PreferredBuildingSeed && !canonical ? -1f : distance;
                        if (score >= best) continue;
                        best = score; binding.Poi = poi; binding.Lot = lot; binding.Building = building;
                        binding.Origin = binding.Entrance = origin; binding.Yaw = yaw; binding.Width = width; binding.Depth = depth;
                        binding.Outward = MathUtil.DirFromYaw(yaw + 180f);
                        // Original sidewalk identities remain the navigation and fallback destination.
                        binding.Outside = canonical ? poi.Position + binding.Outward * .75f : origin + binding.Outward * 2.5f;
                    }
                }
                if (binding.Building == null) binding.Status = "Skipped: no suitable existing parcel; original city retained.";
                else used.Add(binding.Building);
                result.Add(binding);
            }
            return result;
        }
        public static bool Eligible(Building building)
        {
            return building != null && building.Footprint.Valid && building.Height >= 5.5f && building.Tiers == 1 &&
                building.Roof != RoofType.Helipad && building.Roof != RoofType.Antenna &&
                building.Style != FacadeStyle.Police && building.Style != FacadeStyle.Casino;
        }
        public static bool ValidDefinition(VenueDefinition04C definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id) || definition.Id != definition.Kind || definition.Id != definition.Layout ||
                string.IsNullOrEmpty(definition.Title) || definition.Title.Length > 80) return false;
            switch (definition.Id)
            {
                case "lofts": case "clinic": case "forge": case "inn": case "garage":
                case "archive": case "observatory": case "depot": break;
                default: return false;
            }
            foreach (float value in new[] { definition.OffsetX, definition.OffsetZ, definition.MaxDistance, definition.FacingX, definition.FacingZ })
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            if (definition.MaxDistance < 20f || definition.MaxDistance > 600f || Mathf.Abs(definition.OffsetX) > 700f || Mathf.Abs(definition.OffsetZ) > 700f) return false;
            if (string.IsNullOrEmpty(definition.PoiName))
                return (definition.Id == "archive" || definition.Id == "observatory" || definition.Id == "depot") &&
                    Mathf.Abs(definition.FacingX) + Mathf.Abs(definition.FacingZ) == 1f && (definition.FacingX == 0f || definition.FacingZ == 0f);
            return definition.PoiName.Length <= 100 && definition.Id != "archive" && definition.Id != "observatory" && definition.Id != "depot";
        }
        public static void Door(RectF footprint, Vector3 outward, out Vector3 origin, out float width, out float depth, out float yaw)
        {
            bool alongX = Mathf.Abs(outward.x) > Mathf.Abs(outward.z);
            float sign = alongX ? (outward.x >= 0f ? 1f : -1f) : (outward.z >= 0f ? 1f : -1f);
            width = alongX ? footprint.Depth : footprint.Width;
            depth = alongX ? footprint.Width : footprint.Depth;
            origin = new Vector3(alongX ? (sign > 0 ? footprint.X1 : footprint.X0) : footprint.CenterX,
                CityLayout.CurbHeight, alongX ? footprint.CenterZ : (sign > 0 ? footprint.Z1 : footprint.Z0));
            yaw = alongX ? (sign > 0 ? 270f : 90f) : (sign > 0 ? 180f : 0f);
        }
    }
}
