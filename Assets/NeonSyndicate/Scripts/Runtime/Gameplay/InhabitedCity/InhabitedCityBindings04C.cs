using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Pure CityLayout selection. Original footprints remain authoritative outdoor spawn exclusions.</summary>
    public static class InhabitedCityBindings04C
    {
        /// <summary>
        /// 04E: how far a venue door may sit from its block's sidewalk ring, in metres. The four 04E venues only take a
        /// facade whose door opens onto a public sidewalk (never an alley or the back of a block).
        /// </summary>
        public const float SidewalkReach04E = 4.5f;

        /// <summary>04E: the closest a new venue door may stand to an existing point of interest door, in metres.</summary>
        public const float PoiClearance04E = 6f;

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
                // 04E venues read their offset and facing in the Veyra Lofts frame, so they keep their relation to the arrival in any city.
                bool frame04E = VenueIds04E.Has(definition.Id);
                Vector3 desired = canonical && poi != null ? poi.Position :
                    frame04E ? FramePoint04E(home, definition.OffsetX, definition.OffsetZ) :
                    (home == null ? Vector3.zero : home.Position) + new Vector3(definition.OffsetX, 0f, definition.OffsetZ);
                float best = float.MaxValue;
                foreach (Block block in layout.Blocks)
                foreach (Lot lot in block.Lots)
                {
                    if (lot.CustomBuilt || (canonical ? poi == null || lot.PoiName != definition.PoiName : lot.Poi != PoiType.None || !string.IsNullOrEmpty(lot.PoiName))) continue;
                    if (!canonical && definition.Kind == "depot" && lot.Use != LotUse.Warehouse) continue;
                    foreach (Building building in lot.Buildings)
                    {
                        if (used.Contains(building) || !EligibleFor(definition, building)) continue;
                        Vector3 facing = canonical ? poi.Facing : frame04E ? FrameDirection04E(home, definition.FacingX, definition.FacingZ) :
                            new Vector3(definition.FacingX, 0f, definition.FacingZ);
                        if (facing.sqrMagnitude < .5f) facing = lot.Facing;
                        Vector3 origin; float width, depth, yaw;
                        Door(building.Footprint, facing, out origin, out width, out depth, out yaw);
                        if (width < 17f || depth < 19f || ((definition.Kind == "lofts" || definition.Kind == "observatory") && building.Height < 10f)) continue;
                        if (frame04E && !DoorOnSidewalk04E(block, origin, facing)) continue;
                        if (frame04E && NearPoiDoor04E(layout, origin)) continue;
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

        /// <summary>
        /// The 04C rule for every venue, with one 04E extension: Cinder Dispatch may take a one-tier building whose roof
        /// carries an antenna, because the dispatch rebuilds that roof mast as its own crystal receiver crown (see
        /// ArrivalScene04E), so the silhouette is kept rather than flattened. Helipads stay protected for everyone.
        /// </summary>
        public static bool EligibleFor(VenueDefinition04C definition, Building building)
        {
            if (Eligible(building)) return true;
            if (definition == null || definition.Id != VenueIds04E.Dispatch || building == null || building.Roof != RoofType.Antenna) return false;
            return building.Footprint.Valid && building.Height >= 5.5f && building.Tiers == 1 &&
                building.Style != FacadeStyle.Police && building.Style != FacadeStyle.Casino;
        }

        public static bool ValidDefinition(VenueDefinition04C definition)
        {
            if (definition == null || string.IsNullOrEmpty(definition.Id) || definition.Id != definition.Kind || definition.Id != definition.Layout ||
                string.IsNullOrEmpty(definition.Title) || definition.Title.Length > 80) return false;
            bool new04E = VenueIds04E.Has(definition.Id);
            switch (definition.Id)
            {
                case "lofts": case "clinic": case "forge": case "inn": case "garage":
                case "archive": case "observatory": case "depot": break;
                default: if (!new04E) return false; break;
            }
            foreach (float value in new[] { definition.OffsetX, definition.OffsetZ, definition.MaxDistance, definition.FacingX, definition.FacingZ })
                if (float.IsNaN(value) || float.IsInfinity(value)) return false;
            if (definition.MaxDistance < 20f || definition.MaxDistance > 600f || Mathf.Abs(definition.OffsetX) > 700f || Mathf.Abs(definition.OffsetZ) > 700f) return false;
            if (string.IsNullOrEmpty(definition.PoiName))
                return (definition.Id == "archive" || definition.Id == "observatory" || definition.Id == "depot" || new04E) &&
                    Mathf.Abs(definition.FacingX) + Mathf.Abs(definition.FacingZ) == 1f && (definition.FacingX == 0f || definition.FacingZ == 0f);
            // The four 04E venues are new civic uses of ordinary parcels: they never claim a story point of interest.
            return definition.PoiName.Length <= 100 && definition.Id != "archive" && definition.Id != "observatory" && definition.Id != "depot" && !new04E;
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

        // ------------------------------------------------------------------ 04E frame and venue-local geometry (pure)

        /// <summary>The Veyra Lofts frame: forward is straight out of the Lofts door, right is its right hand (world +Z and +X without a home).</summary>
        public static void HomeFrame04E(Poi home, out Vector3 origin, out Vector3 forward, out Vector3 right)
        {
            origin = home == null ? Vector3.zero : new Vector3(home.Position.x, 0f, home.Position.z);
            forward = home == null ? Vector3.forward : new Vector3(home.Facing.x, 0f, home.Facing.z);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            right = new Vector3(forward.z, 0f, -forward.x);
        }

        /// <summary>A point (x to the right, z straight out of the Lofts door) in the Veyra Lofts frame, in world space.</summary>
        public static Vector3 FramePoint04E(Poi home, float x, float z)
        {
            Vector3 origin, forward, right;
            HomeFrame04E(home, out origin, out forward, out right);
            return origin + right * x + forward * z;
        }

        /// <summary>A direction given in the Veyra Lofts frame, in world space (axis aligned when the input is).</summary>
        public static Vector3 FrameDirection04E(Poi home, float x, float z)
        {
            Vector3 origin, forward, right;
            HomeFrame04E(home, out origin, out forward, out right);
            Vector3 d = right * x + forward * z;
            // Snap to the nearest world axis: city blocks are axis aligned and Door() picks a footprint edge by axis.
            return Mathf.Abs(d.x) >= Mathf.Abs(d.z) ? new Vector3(Mathf.Sign(d.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(d.z));
        }

        /// <summary>True when the door opens onto the block's public sidewalk ring within SidewalkReach04E metres.</summary>
        public static bool DoorOnSidewalk04E(Block block, Vector3 origin, Vector3 outward)
        {
            if (block == null) return false;
            Vector3 f = new Vector3(outward.x, 0f, outward.z);
            if (f.sqrMagnitude < 1e-4f) return false;
            f.Normalize();
            for (float k = .5f; k <= SidewalkReach04E + .01f; k += .25f)
            {
                Vector3 p = origin + f * k;
                if (block.Curb.Contains(p) && !block.Inner.Contains(p)) return true;
            }
            return false;
        }

        /// <summary>True when an existing point of interest door is closer than PoiClearance04E to this door.</summary>
        public static bool NearPoiDoor04E(CityLayout layout, Vector3 origin)
        {
            if (layout == null) return false;
            foreach (Poi poi in layout.Pois)
                if (poi != null && MathUtil.FlatSqrDistance(poi.Position, origin) < PoiClearance04E * PoiClearance04E) return true;
            return false;
        }

        /// <summary>A venue's horizontal frame: forward goes into the building, right is the room's +x (matches Quaternion.Euler(0, yaw, 0)).</summary>
        public static void VenueAxes(float yaw, out Vector3 forward, out Vector3 right)
        {
            forward = MathUtil.DirFromYaw(yaw);
            right = new Vector3(forward.z, 0f, -forward.x);
        }

        /// <summary>A venue-local point (x right, y up, z into the room) in world space. Pure (no transforms).</summary>
        public static Vector3 LocalToWorld(Vector3 origin, float yaw, Vector3 local)
        {
            Vector3 forward, right;
            VenueAxes(yaw, out forward, out right);
            return origin + right * local.x + Vector3.up * local.y + forward * local.z;
        }

        /// <summary>A world point in a venue's local frame (x right, y up, z into the room). Pure.</summary>
        public static Vector3 WorldToLocal(Vector3 origin, float yaw, Vector3 world)
        {
            Vector3 forward, right;
            VenueAxes(yaw, out forward, out right);
            Vector3 d = world - origin;
            return new Vector3(Vector3.Dot(d, right), d.y, Vector3.Dot(d, forward));
        }

        /// <summary>
        /// The kept-clear zone of a venue door: the doorway width plus a margin, from the curb in front of it to the back
        /// of the room. Placements (the 04D case, lane and breach anchors, arrival dressing) never stand in it.
        /// </summary>
        public static bool InDoorZone04E(Vector3 origin, float yaw, Vector3 world, float radius)
        {
            Vector3 l = WorldToLocal(origin, yaw, world);
            float r = Mathf.Max(0f, radius);
            if (Mathf.Abs(l.x) <= 2.6f + r && l.z >= -7.5f - r && l.z <= .5f + r) return true; // the approach from the street
            return Mathf.Abs(l.x) <= 8f + r && l.z > .5f && l.z <= 18f + r;                   // the room itself
        }
    }
}
