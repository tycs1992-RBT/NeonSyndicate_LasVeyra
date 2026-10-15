using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Automatic, bounded 02F dressing under the existing city root. No gameplay state is registered or changed.</summary>
    public static class CrystalCity02FWorld
    {
        public const string RootName = "Crystal City 02F | Astral Landmarks";

        public static IEnumerator Build(CityLayout layout, Transform parent)
        {
            // Protect the base city builder even from an unexpected optional-art enumeration failure.
            IEnumerator pass = BuildInternal(layout, parent);
            while (true)
            {
                bool moved = false, failed = false;
                object current = null;
                try { moved = pass.MoveNext(); if (moved) current = pass.Current; }
                catch (Exception error) { failed = true; Log.Exception("02F optional pass stopped; continuing base city boot", error); }
                if (failed || !moved) break;
                yield return current;
            }
            IDisposable disposable = pass as IDisposable;
            if (disposable != null) disposable.Dispose();
        }

        private static IEnumerator BuildInternal(CityLayout layout, Transform parent)
        {
            if (layout == null || parent == null || parent.Find(RootName) != null) yield break;
            CrystalCity02FCatalog catalog = null;
            bool ready = false;
            try
            {
                catalog = CrystalCity02FAssets.Catalog();
                ready = true;
            }
            catch (Exception error) { Log.Exception("02F preflight failed; the approved city is kept", error); }
            if (!ready) yield break;
            // Yield between source imports so this optional pass does not monopolize one loading frame.
            foreach (string model in catalog.models)
            {
                try
                {
                    CrystalCity02FAssets.Model(model, false);
                    CrystalCity02FAssets.Model(model, true);
                }
                catch (Exception error) { ready = false; Log.Exception("02F model preflight; original city kept", error); }
                if (!ready) yield break;
                yield return null;
            }
            // Diagnostic is issued only after the full preflight has succeeded.
            // It distinguishes an actual art recovery from the original-city fallback.
            if (CrystalCity02FAssets.FallbackCount > 0)
                Log.Warn("Crystal City 02H | Recovered " + CrystalCity02FAssets.FallbackCount +
                    " unreadable Unity OBJ imports using included source geometry.");
            else
                Log.Info("Crystal City 02H | All 02F model imports passed validation.");
            GameObject root = new GameObject(RootName);
            root.transform.SetParent(parent, false);
            CrystalCity02FMotion motion = root.AddComponent<CrystalCity02FMotion>();
            int heroes = 0, crests = 0, lamps = 0, planters = 0, fountains = 0, inlays = 0, glades = 0, failures = 0, renderers = 0;
            List<CrystalCity02FRoofSite> roofs = CrystalCity02FPlanner.SelectRoofs(layout);
            foreach (CrystalCity02FRoofSite site in roofs)
            {
                CrystalCity02FComposer group = null;
                try
                {
                    CrystalCity02FBlueprint blueprint = catalog.Find(site.BlueprintId);
                    // Existing detailed roofs already have tall crowns. Float above their full silhouette.
                    Vector3 landmarkPosition = site.Position + (DistrictArchitecture.Contains(site.Building) ? Vector3.up * 22f : Vector3.zero);
                    group = new CrystalCity02FComposer(blueprint.title, root.transform, landmarkPosition, site.Style, motion);
                    group.Append(blueprint, landmarkPosition, Quaternion.Euler(0f, site.Building.Seed % 360, 0f), site.Scale, true);
                    group.Finish(true);
                    motion.AddMapMarker(group.Transform, blueprint.title, site.Style);
                    motion.AddLight(landmarkPosition + Vector3.up * (site.Scale * 2f), site.Style);
                    DistrictCrystalFx.Attach(group.Transform, Vector3.up * site.Scale * 2f, .8f, site.Style == CrystalCityStyle.AmethystWard);
                    renderers += group.Renderers; heroes++;
                    Log.Info("Crystal City 02F | " + blueprint.title + " at " + landmarkPosition.ToString("F1") +
                        (site.NearArrival ? " (selected near the existing Veyra Lofts arrival)" : "") + ". Visual landmark, not a new mission or flying platform.");
                }
                catch (Exception error)
                {
                    if (group != null) group.Abort();
                    failures++; Log.Exception("Optional 02F roof landmark (old roof kept)", error);
                }
                yield return null;
            }

            // Guaranteed names from the existing game, not new spawn points or imported scenes.
            string[] arrivals = { "Veyra Lofts", "Mara Solane Street Clinic" };
            foreach (string name in arrivals)
            {
                Poi poi = layout.FindPoi(name);
                if (poi == null || !CrystalCity02FPlanner.CityPoint(poi.Position)) continue;
                CrystalCity02FComposer group = null;
                try
                {
                    Vector3 facing = MathUtil.Flat(poi.Facing);
                    if (facing.sqrMagnitude < .01f) facing = Vector3.forward;
                    facing.Normalize();
                    // Elevated crest remains outside the central interaction volume.
                    Vector3 at = poi.Position - facing * .12f + Vector3.up * 3.7f;
                    Quaternion yaw = Quaternion.LookRotation(facing, Vector3.up);
                    CrystalCityStyle style = CrystalCity02DMaterials.StyleAt(at);
                    group = new CrystalCity02FComposer("Arrival crest / " + name, root.transform, at, style, motion);
                    group.Append(catalog.Find("arrival_crest"), at, yaw, 1f, false);
                    group.Finish(false); renderers += group.Renderers; crests++;
                    motion.AddLight(at + facing * .45f, style);
                }
                catch (Exception error)
                {
                    if (group != null) group.Abort();
                    failures++; Log.Exception("Optional 02F arrival crest", error);
                }
                yield return null;
            }

            // Group all small stationary parts by position and palette. Never one renderer per leaf.
            Dictionary<string, CrystalCity02FComposer> streets = new Dictionary<string, CrystalCity02FComposer>(StringComparer.Ordinal);
            HashSet<string> cells = new HashSet<string>(StringComparer.Ordinal);
            List<PropSpot> props = new List<PropSpot>(layout.Props);
            Poi home = layout.FindPoi("Veyra Lofts");
            Vector3 focus = home == null ? Vector3.zero : home.Position;
            props.Sort(delegate(PropSpot a, PropSpot b)
            {
                int order = MathUtil.FlatSqrDistance(a.Pos, focus).CompareTo(MathUtil.FlatSqrDistance(b.Pos, focus));
                if (order != 0) return order;
                order = a.Pos.x.CompareTo(b.Pos.x); if (order != 0) return order;
                order = a.Pos.z.CompareTo(b.Pos.z); if (order != 0) return order;
                return ((int)a.Kind).CompareTo((int)b.Kind);
            });
            int budget = CrystalCity02FPlanner.StreetBudget(Settings.Graphics), processed = 0;
            foreach (PropSpot prop in props)
            {
                if (!CrystalCity02FPlanner.CityPoint(prop.Pos)) continue;
                string id = null;
                Vector3 at = prop.Pos;
                Quaternion yaw = Quaternion.Euler(0f, prop.Yaw, 0f);
                float scale = 1f;
                if (prop.Kind == PropKind.StreetLamp && lamps < budget)
                {
                    id = "lantern_pair";
                    at += Vector3.up * 7.18f + yaw * Vector3.forward * 2.15f;
                }
                else if (prop.Kind == PropKind.Planter && planters < budget / 2)
                {
                    id = "planter_bloom"; at += Vector3.up * .815f; scale = .38f;
                }
                else if (prop.Kind == PropKind.Fountain && fountains < 6)
                {
                    id = "fountain_halo"; scale = prop.Variant == 1 ? .80f : 1.0f;
                }
                if (id == null) continue;
                string cell = id + "/" + Mathf.FloorToInt(prop.Pos.x / 55f) + "/" + Mathf.FloorToInt(prop.Pos.z / 55f);
                if (!cells.Add(cell)) continue;
                try
                {
                    CrystalCity02FComposer batch = StreetBatch(streets, root.transform, at, motion);
                    batch.Append(catalog.Find(id), at, yaw, scale, false);
                    if (id == "lantern_pair") lamps++; else if (id == "planter_bloom") planters++; else fountains++;
                }
                catch (Exception error) { failures++; if (failures <= 4) Log.Exception("Optional 02F street ornament", error); }
                processed++;
                if (processed % 6 == 0) yield return null;
            }
            foreach (Block block in layout.Blocks)
            {
                if (inlays >= 8) break;
                if (block == null) continue;
                foreach (Lot lot in block.Lots)
                {
                    if (inlays >= 8) break;
                    if (lot == null || lot.CustomBuilt || lot.Use != LotUse.Plaza || lot.Area.Width < 16f || lot.Area.Depth < 16f) continue;
                    Vector3 at = lot.Area.Center(CityLayout.CurbHeight + .018f);
                    if (!CrystalCity02FPlanner.CityPoint(at) || !ClearInlay(layout, at)) continue;
                    try
                    {
                        StreetBatch(streets, root.transform, at, motion).Append(catalog.Find("plaza_inlay"), at, Quaternion.identity, 1f, false);
                        inlays++;
                    }
                    catch (Exception error) { failures++; if (failures <= 4) Log.Exception("Optional 02F plaza inlay", error); }
                }
            }
            int gladeBudget = Settings.Graphics == GraphicsPreset.Low ? 3 : (Settings.Graphics == GraphicsPreset.Medium ? 6 : 9);
            foreach (Block block in layout.Blocks)
            {
                if (glades >= gladeBudget) break;
                if (block == null) continue;
                foreach (Lot lot in block.Lots)
                {
                    if (glades >= gladeBudget) break;
                    if (lot == null || lot.CustomBuilt || lot.Use != LotUse.Park || lot.Area.Width < 44f || lot.Area.Depth < 44f) continue;
                    // Mid-diagonal pockets are separated from 02D's corner groves and central cross paths.
                    Vector3 at = new Vector3(lot.Area.CenterX + lot.Area.Width * .20f, CityLayout.CurbHeight + .005f,
                        lot.Area.CenterZ - lot.Area.Depth * .20f);
                    if (!CrystalCity02FPlanner.ClearGardenPocket(layout, lot, at, 2.3f)) continue;
                    try
                    {
                        StreetBatch(streets, root.transform, at, motion).Append(catalog.Find("moonwell_glade"), at, Quaternion.identity, 1f, false);
                        glades++;
                    }
                    catch (Exception error) { failures++; if (failures <= 4) Log.Exception("Optional 02F moonwell glade", error); }
                }
            }
            foreach (CrystalCity02FComposer batch in streets.Values)
            {
                try { batch.Finish(false); renderers += batch.Renderers; }
                catch (Exception error) { batch.Abort(); failures++; Log.Exception("02F street batch not installed", error); }
                yield return null;
            }
            Log.Info("Crystal City 02F | Ready. Landmark roofs " + heroes + ", arrival crests " + crests +
                ", lantern pairs " + lamps + ", planted boxes " + planters + ", fountains " + fountains +
                ", plaza inlays " + inlays + ", moonwell glades " + glades + ", material/LOD renderers " + renderers +
                ", rotating parts " + motion.RotorCount + ", art failures " + failures +
                ". No roads, colliders, quests or controls were changed.");
            if (heroes == 0) Log.Warn("02F found no suitable ordinary roof. Existing world retained; inspect city seed and roof eligibility.");
        }

        private static CrystalCity02FComposer StreetBatch(Dictionary<string, CrystalCity02FComposer> batches, Transform parent, Vector3 at, CrystalCity02FMotion motion)
        {
            int x = Mathf.FloorToInt(at.x / 160f), z = Mathf.FloorToInt(at.z / 160f);
            CrystalCityStyle style = CrystalCity02DMaterials.StyleAt(at);
            string key = x + "/" + z + "/" + (int)style;
            CrystalCity02FComposer batch;
            if (batches.TryGetValue(key, out batch)) return batch;
            batch = new CrystalCity02FComposer("Street detail " + key, parent, new Vector3(x * 160f, 0f, z * 160f), style, motion);
            batches.Add(key, batch); return batch;
        }

        private static bool ClearInlay(CityLayout layout, Vector3 at)
        {
            foreach (Poi poi in layout.Pois)
                if (poi != null && MathUtil.FlatSqrDistance(poi.Position, at) < 5f * 5f) return false;
            foreach (Vector3 park in layout.ParkingSpots)
                if (MathUtil.FlatSqrDistance(park, at) < 5f * 5f) return false;
            // Inlay is thin and collision-free. These checks protect visual readability, not navigation.
            return true;
        }
    }
}
