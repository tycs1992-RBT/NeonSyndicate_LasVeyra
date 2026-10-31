using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Live world art inside the Forge: a replaced anvil fitted inside each original Forge station's own BoxCollider (the
    /// H8S gallery review's exact fit; only the four primitive anvil renderers it replaces are hidden, never the station,
    /// its collider or its E or Y use) and furnishings placed against a venue room's wall by the gallery's floor-plan rules.
    /// </summary>
    public sealed partial class LiveWorldArtH8S
    {
        private const string StationPrefix = "Forge (";
        private const string DanteStation = "Forge (Dante Rook's forge)";
        private const float LargeColliderArea = 30f;
        private const float StationWaitSeconds = 12f;
        private readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
        private readonly List<bool> _hiddenWasEnabled = new List<bool>();
        private readonly Dictionary<string, List<PlanRectH8S>> _roomTaken = new Dictionary<string, List<PlanRectH8S>>();

        /// <summary>Every original Forge station collider under the Forge (CraftingSystem).</summary>
        private static List<BoxCollider> Stations()
        {
            List<BoxCollider> found = new List<BoxCollider>();
            if (Game.Crafting == null) return found;
            BoxCollider[] all = Game.Crafting.GetComponentsInChildren<BoxCollider>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].enabled && !all[i].isTrigger && all[i].gameObject.name.StartsWith(StationPrefix, StringComparison.Ordinal)) found.Add(all[i]);
            return found;
        }

        private bool RunReplace(Job job, float waited)
        {
            List<BoxCollider> stations = Stations();
            if (stations.Count == 0)
            {
                if (waited < GiveUpSeconds) return false;
                Skip(job.Piece, "line " + job.Order.Line + ": no Forge station was built in this world.");
                return true;
            }
            int fitted = 0;
            string last = "";
            for (int i = 0; i < stations.Count && _copies < MaxCopies; i++)
            {
                string why = FitAnvil(job.Piece, stations[i]);
                if (why == null) fitted++;
                else last = stations[i].gameObject.name + ": " + why;
            }
            if (fitted == 0) Skip(job.Piece, "line " + job.Order.Line + ": no Forge station could take it (" + last + ").");
            else if (fitted < stations.Count) Line(job.Piece.Entry.Name + ": " + fitted + " of " + stations.Count + " Forge stations took the new anvil; the others keep the original (" + last + ").");
            return true;
        }

        /// <summary>The station's direct child with this name and its renderer (null when absent or doubled).</summary>
        private static Renderer StationPiece(Transform station, string name)
        {
            Renderer found = null;
            for (int i = 0; i < station.childCount; i++)
            {
                Transform t = station.GetChild(i);
                if (t.name != name) continue;
                if (found != null) return null;
                found = t.GetComponent<Renderer>();
            }
            return found;
        }

        /// <summary>Null when the anvil was fitted inside this station's collider and the primitive anvil hidden, otherwise why not (nothing changed).</summary>
        private string FitAnvil(Piece piece, BoxCollider station)
        {
            Transform frame = station.transform;
            Vector3 s = frame.lossyScale;
            if (s.x <= 0f || Mathf.Abs(s.x - s.y) > 1e-3f || Mathf.Abs(s.x - s.z) > 1e-3f) return "the station is not uniformly scaled";
            Renderer block = StationPiece(frame, "Block");
            Renderer[] replaced = { StationPiece(frame, "Waist"), StationPiece(frame, "Face"), StationPiece(frame, "Horn"), StationPiece(frame, "Glow") };
            if (block == null || replaced[0] == null || replaced[1] == null || replaced[2] == null || replaced[3] == null) return "its original anvil pieces are not the expected ones";
            Vector3 blockMin, blockMax;
            if (!LocalBounds(block.gameObject, frame, out blockMin, out blockMax)) return "its support block has no mesh";
            GameObject visual;
            string why;
            GameObject holder = Spawn(piece, LiveWorldPaletteH8S.Gothic, out visual, out why);
            if (holder == null) return why;
            float stationScale = s.x;
            holder.transform.SetPositionAndRotation(frame.position, frame.rotation);
            holder.transform.localScale = Vector3.one * stationScale;
            Vector3 localMin, localMax;
            if (!LocalBounds(visual, frame, out localMin, out localMax))
            {
                Destroy(holder);
                return "the model has no visible mesh";
            }
            ForgeFitH8S fit = LiveWorldPlannerH8S.AnvilFit(localMin, localMax, station.center, station.size, blockMax.y,
                new Vector2((blockMin.x + blockMax.x) * 0.5f, (blockMin.z + blockMax.z) * 0.5f));
            if (!fit.Ok)
            {
                Destroy(holder);
                return "it does not fit inside the original collider (" + fit.Reason + ")";
            }
            string clash = KeepClear(StationPiece(frame, "Bowl"), fit, frame) ?? KeepClear(StationPiece(frame, "Embers"), fit, frame);
            if (clash != null)
            {
                Destroy(holder);
                return clash;
            }
            holder.transform.SetPositionAndRotation(frame.TransformPoint(fit.Pivot), frame.rotation * Quaternion.Euler(0f, 90f * fit.QuarterTurns, 0f));
            holder.transform.localScale = Vector3.one * (stationScale * fit.Scale);
            Vector3 placedMin, placedMax;
            LocalBounds(visual, frame, out placedMin, out placedMax);
            Vector3 boxMin = station.center - station.size * 0.5f + Vector3.one * (ForgeGalleryFitH8S.BoxMargin - 0.0005f);
            Vector3 boxMax = station.center + station.size * 0.5f - Vector3.one * (ForgeGalleryFitH8S.BoxMargin - 0.0005f);
            if (placedMin.x < boxMin.x || placedMin.y < boxMin.y || placedMin.z < boxMin.z || placedMax.x > boxMax.x || placedMax.y > boxMax.y || placedMax.z > boxMax.z)
            {
                Destroy(holder);
                return "after placement a rendered corner would stick out of the original collider";
            }
            // Proven inside: only now hide the four primitive pieces it replaces, remembering each state.
            for (int i = 0; i < replaced.Length; i++)
            {
                _hiddenRenderers.Add(replaced[i]);
                _hiddenWasEnabled.Add(replaced[i].enabled);
                replaced[i].enabled = _hidden ? replaced[i].enabled : false;
            }
            AddCulling(holder, true);
            Placed(piece, holder, "as the anvil of " + frame.gameObject.name, holder.transform.position);
            return null;
        }

        private string KeepClear(Renderer piece, ForgeFitH8S fit, Transform frame)
        {
            if (piece == null) return null;
            Vector3 min, max;
            if (!LocalBounds(piece.gameObject, frame, out min, out max)) return null;
            float depth = ForgeGalleryFitH8S.OverlapDepth(fit.Min, fit.Max, min, max);
            return depth > ForgeGalleryFitH8S.TouchTolerance ? "it would cut " + depth.ToString("0.000") + " m into the original " + piece.name : null;
        }

        /// <summary>The rendered bounds (eight mesh-bound corners per mesh) of everything under root, in frame's local space.</summary>
        private bool LocalBounds(GameObject root, Transform frame, out Vector3 min, out Vector3 max)
        {
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filters.Length; f++)
            {
                if (filters[f].sharedMesh == null) continue;
                Bounds b = filters[f].sharedMesh.bounds;
                ForgeGalleryFitH8S.Corners(b.min, b.max, _corners);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = frame.InverseTransformPoint(filters[f].transform.TransformPoint(_corners[i]));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }
            return any;
        }

        /// <summary>Puts the hidden primitive anvil pieces back exactly as they were.</summary>
        private void RestoreReplaced()
        {
            for (int i = 0; i < _hiddenRenderers.Count; i++)
                if (_hiddenRenderers[i] != null) _hiddenRenderers[i].enabled = _hiddenWasEnabled[i];
        }

        /// <summary>Shows or hides the replaced primitive pieces to match the live art (hidden art shows the originals).</summary>
        private void ApplyReplacedVisibility()
        {
            for (int i = 0; i < _hiddenRenderers.Count; i++)
                if (_hiddenRenderers[i] != null) _hiddenRenderers[i].enabled = _hidden ? _hiddenWasEnabled[i] : false;
        }

        private bool RunRoom(Job job, float waited)
        {
            string id = job.Order.Place;
            VenueBinding04C venue = Game.Inhabited != null && Game.Inhabited.IsOperational ? Game.Inhabited.FindVenue(id) : null;
            if (venue == null || !venue.Available || venue.View == null || venue.View.Composition == null)
            {
                if (waited < GiveUpSeconds && Game.Inhabited != null && !Game.Inhabited.IsOperational) return false;
                Skip(job.Piece, "line " + job.Order.Line + ": the " + id + " room is not built in this world.");
                return true;
            }
            BoxCollider station = null;
            if (id == "forge")
            {
                List<BoxCollider> stations = Stations();
                for (int i = 0; i < stations.Count; i++) if (stations[i].gameObject.name == DanteStation) station = stations[i];
                if (station == null && waited < StationWaitSeconds) return false;
                if (station != null && !venue.View.Contains(station.transform.position)) station = null;
            }
            for (int copy = 0; copy < job.Order.Count && _copies < MaxCopies; copy++)
            {
                string why = PlaceInRoom(job, venue.View, station);
                if (why == null) continue;
                Skip(job.Piece, "line " + job.Order.Line + ": no safe spot in the " + id + " room (" + why + ").");
                break;
            }
            return true;
        }

        /// <summary>Null when one copy was placed against a wall of the room, otherwise why not.</summary>
        private string PlaceInRoom(Job job, VenueView04C view, BoxCollider station)
        {
            Transform room = view.transform;
            VenueExpanded04C composition = view.Composition;
            GameObject visual;
            string why;
            GameObject holder = Spawn(job.Piece, LiveWorldPaletteH8S.Gothic, out visual, out why);
            if (holder == null) return why;
            holder.transform.SetPositionAndRotation(room.position, room.rotation);
            Vector3 min, max;
            if (!LocalBounds(visual, room, out min, out max))
            {
                Destroy(holder);
                return "the model has no visible mesh";
            }
            float floorY = view.LocalAnchor("service").y;
            List<PlanCircleH8S> interactions = new List<PlanCircleH8S>();
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null || !it.isActiveAndEnabled) continue;
                Vector3 local = room.InverseTransformPoint(it.transform.position);
                if (Mathf.Abs(local.y - floorY) > it.HeightTolerance + 0.5f) continue;
                interactions.Add(new PlanCircleH8S(new Vector2(local.x, local.z), it.Radius + GalleryPlanH8S.InteractMargin, "interaction " + it.name));
            }
            Vector3 giver = view.LocalAnchor("staff");
            if (job.Order.Place == "forge" && Game.Campaign != null)
            {
                NpcController dante = Game.Campaign.CastActor(CastIds04E.Dante);
                if (dante != null && view.Contains(dante.transform.position)) giver = room.InverseTransformPoint(dante.transform.position);
            }
            Vector3 service = station != null ? room.InverseTransformPoint(station.transform.position) : view.LocalAnchor("service");
            PlanRectH8S stationRect = station != null ? PlanOf(station.center, station.size, station.transform, room, "Forge station collider") : new PlanRectH8S();
            GalleryRulesH8S rules = LiveWorldPlannerH8S.RoomRules(composition, view.LocalAnchor("exit"), giver, service, floorY, interactions, station != null, stationRect);
            Vector3 centre = room.TransformPoint(new Vector3(0f, floorY + 1.1f, composition.roomDepth * 0.5f));
            Vector3 halfExtents = new Vector3(composition.roomWidth * 0.5f, 0.95f, composition.roomDepth * 0.5f);
            Collider[] colliders = Physics.OverlapBox(centre, halfExtents, room.rotation, Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider c = colliders[i];
                if (c == null || c == station) continue;
                PlanRectH8S plan = PlanOf(c.bounds.center, c.bounds.size, null, room, "collider " + c.name);
                if (plan.Size.x * plan.Size.y > LargeColliderArea) continue;
                rules.Blocked.Add(plan.Expanded(GalleryPlanH8S.ColliderMargin));
            }
            List<PlanRectH8S> taken;
            if (!_roomTaken.TryGetValue(job.Order.Place, out taken)) _roomTaken.Add(job.Order.Place, taken = new List<PlanRectH8S>());
            for (int i = 0; i < taken.Count; i++) rules.Blocked.Add(taken[i]);
            Vector3 size = max - min;
            List<GallerySpotH8S> spots = GalleryPlanH8S.Spots(new Vector2(size.x, size.z), size.y, rules, GalleryPlanH8S.GridStep);
            string last = spots.Count == 0 ? "no wall spot passes the room's rules" : "";
            for (int s = 0; s < spots.Count && s < 60; s++)
            {
                float floor;
                string physical = RoomFloor(room, spots[s].Footprint, floorY, size.y, out floor);
                if (physical != null)
                {
                    last = physical;
                    continue;
                }
                int turns = LiveWorldPlannerH8S.RoomTurns(spots[s], job.Order.Turn);
                Vector3 pivot = GalleryPlanH8S.PivotFor(spots[s].Center, floor, min, max, turns);
                holder.transform.SetPositionAndRotation(room.TransformPoint(pivot), room.rotation * Quaternion.Euler(0f, 90f * turns, 0f));
                PlanRectH8S used = spots[s].Footprint.Expanded(GalleryPlanH8S.ItemGap);
                used.Label = "placed " + job.Piece.Entry.Name;
                taken.Add(used);
                AddCulling(holder, true);
                Placed(job.Piece, holder, "against the " + spots[s].Wall + " of the " + job.Order.Place + " room", holder.transform.position);
                return null;
            }
            Destroy(holder);
            return last;
        }

        /// <summary>Null when the footprint stands on the room's own level floor and no collider occupies its volume, otherwise why not.</summary>
        private static string RoomFloor(Transform room, PlanRectH8S footprint, float floorY, float height, out float floor)
        {
            floor = float.MinValue;
            const float inset = 0.08f;
            for (int i = 0; i < 5; i++)
            {
                float x = i == 4 ? footprint.Center.x : (i & 1) == 0 ? footprint.Min.x + inset : footprint.Max.x - inset;
                float z = i == 4 ? footprint.Center.y : (i & 2) == 0 ? footprint.Min.y + inset : footprint.Max.y - inset;
                RaycastHit hit;
                if (!Physics.Raycast(room.TransformPoint(new Vector3(x, floorY + 1f, z)), -room.up, out hit, 1.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    return "no room floor under a corner";
                Vector3 local = room.InverseTransformPoint(hit.point);
                if (Mathf.Abs(local.y - floorY) > 0.06f) return "something other than the room floor lies under a corner";
                if (Vector3.Dot(hit.normal, room.up) < 0.95f) return "the floor under a corner is not level";
                floor = Mathf.Max(floor, local.y);
            }
            Vector2 c = footprint.Center, h = footprint.Size * 0.5f;
            Vector3 centre = room.TransformPoint(new Vector3(c.x, floor + height * 0.5f + 0.05f, c.y));
            Vector3 half = new Vector3(Mathf.Max(0.01f, h.x - 0.03f), Mathf.Max(0.01f, height * 0.5f - 0.03f), Mathf.Max(0.01f, h.y - 0.03f));
            if (Physics.CheckBox(centre, half, room.rotation, ~((1 << Layers.IgnoreRaycast) | (1 << Layers.Minimap)), QueryTriggerInteraction.Ignore))
                return "an existing collider (furniture, wall, character or vehicle) occupies the spot";
            return null;
        }

        /// <summary>The room-local plan rectangle covering a box given in another transform's space, or a world box when space is null.</summary>
        private PlanRectH8S PlanOf(Vector3 center, Vector3 size, Transform space, Transform room, string label)
        {
            ForgeGalleryFitH8S.Corners(center - size * 0.5f, center + size * 0.5f, _corners);
            Vector2 lo = new Vector2(float.MaxValue, float.MaxValue), hi = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 8; i++)
            {
                Vector3 world = space != null ? space.TransformPoint(_corners[i]) : _corners[i];
                Vector3 local = room.InverseTransformPoint(world);
                lo = Vector2.Min(lo, new Vector2(local.x, local.z));
                hi = Vector2.Max(hi, new Vector2(local.x, local.z));
            }
            return new PlanRectH8S(lo, hi, label);
        }
    }
}
