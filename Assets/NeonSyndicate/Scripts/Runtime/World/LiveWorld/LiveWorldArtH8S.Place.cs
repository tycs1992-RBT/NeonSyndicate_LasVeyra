using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Outdoor placement for live world art: the protected context, near and route copies, spawning and the physical probes.</summary>
    public sealed partial class LiveWorldArtH8S
    {
        private WorldRouteStopH8S[] _stops;
        private readonly List<Vector3> _stopPoints = new List<Vector3>();
        private readonly List<bool> _stopResolved = new List<bool>();
        private List<WorldRouteGuardH8S> _routeGuards = new List<WorldRouteGuardH8S>();
        private readonly List<LiveWorldDiscH8S> _discs = new List<LiveWorldDiscH8S>();
        private readonly Vector3[] _probes = new Vector3[5];
        private readonly Vector3[] _corners = new Vector3[8];
        private readonly Collider[] _overlaps = new Collider[32];

        /// <summary>The story route (stops, legs and walks), every quest place and giver, and every interaction point: what pieces keep clear of.</summary>
        private void BuildContext()
        {
            _stops = WorldRouteH8S.Stops();
            _stopPoints.Clear();
            _stopResolved.Clear();
            List<Vector3> doors = new List<Vector3>();
            for (int i = 0; i < _stops.Length; i++)
            {
                Vector3 p, door = Vector3.zero;
                string why;
                bool ok = ResolvePlace(_stops[i].Token, out p, out why);
                if (ok && _stops[i].DoorToken != null) ok = ResolvePlace(_stops[i].DoorToken, out door, out why);
                _stopPoints.Add(p);
                doors.Add(door);
                _stopResolved.Add(ok);
            }
            List<List<Vector3>> walks = new List<List<Vector3>>();
            int previous = -1;
            for (int i = 0; i < _stops.Length; i++)
            {
                if (!_stopResolved[i]) continue;
                if (previous >= 0)
                {
                    bool walked;
                    walks.Add(WorldRouteH8S.LegPolyline(_layout, _stopPoints[previous], _stops[previous].Interior, doors[previous], _stopPoints[i], _stops[i].Interior, doors[i], out walked));
                }
                previous = i;
            }
            _routeGuards = WorldRouteH8S.Guards(_stopPoints, _stopResolved, _stops, walks);
            _discs.Clear();
            if (Game.Quests != null)
            {
                QuestPlaces.TokenResolve tokens = (string token, out Vector3 pos) =>
                {
                    float yaw;
                    string why;
                    return CampaignPlacesRuntime04E.TryResolve(token, out pos, out yaw, out why);
                };
                LiveWorldSafetyH8S.QuestDiscs(_layout, Game.Quests.Quests, (int)_layout.Revision04O, tokens, _discs);
            }
            for (int i = 0; i < Interactable.All.Count; i++)
            {
                Interactable it = Interactable.All[i];
                if (it == null || !it.isActiveAndEnabled) continue;
                _discs.Add(new LiveWorldDiscH8S { At = it.transform.position, Radius = it.Radius + LiveWorldSafetyH8S.InteractMargin, Label = "interaction " + it.name });
            }
        }

        /// <summary>A place's point: Realm anchors through the layout, 04E tokens through the live resolver, site keys and POI names through QuestPlaces.</summary>
        private bool ResolvePlace(string place, out Vector3 point, out string why)
        {
            point = Vector3.zero;
            why = null;
            if (string.IsNullOrEmpty(place)) { why = "no place was given."; return false; }
            if (place.StartsWith("Realm:", StringComparison.Ordinal))
            {
                if (_layout.ResolveRealmAnchor04OC(place, out point)) return true;
                why = place + " is not in this world.";
                return false;
            }
            if (CampaignRules04E.IsPlaceToken(place))
            {
                float yaw;
                if (CampaignPlacesRuntime04E.TryResolve(place, out point, out yaw, out why)) return true;
                why = why ?? (place + " could not be found now.");
                return false;
            }
            if (QuestPlaces.Resolve(_layout, place, 0f, 0f, Vector3.zero, Vector3.zero, false, Vector3.zero, 0, out point)) return true;
            why = "there is no place called \"" + place + "\" in this world (use a Realm, Venue or Cast token, a site key or a shop or person's name).";
            return false;
        }

        private IReadOnlyList<VenueBinding04C> Venues { get { return Game.Inhabited != null ? Game.Inhabited.Venues : null; } }

        private void RunNear(Job job)
        {
            Piece piece = job.Piece;
            LiveWorldOrderH8S order = job.Order;
            Vector3 place;
            string why;
            if (!ResolvePlace(order.Place, out place, out why))
            {
                Skip(piece, "line " + order.Line + ": " + why);
                return;
            }
            GameObject visual;
            LiveWorldPaletteH8S palette = LiveWorldSurfacesH8S.Resolve(order.Palette, LiveWorldSurfacesH8S.PaletteFor(order.Place));
            GameObject holder = Spawn(piece, palette, out visual, out why);
            if (holder == null)
            {
                Skip(piece, "line " + order.Line + ": " + why);
                return;
            }
            LiveWorldRulesH8S rules = LiveWorldSafetyH8S.AreaRules(_layout, place, _routeGuards, Venues, WorldApertures.All, _discs, _placed);
            string first;
            List<LiveWorldSpotH8S> spots = LiveWorldPlannerH8S.NearSpots(place, order.Offset, order.Turn, order.Scale, piece.Min, piece.Max, rules, _layout, out first);
            string last = first ?? "no candidate spot passed the safety rules";
            for (int i = 0; i < spots.Count; i++)
            {
                Vector3 at;
                Vector3 min, max;
                string physical = Ground(spots[i].Position, spots[i].Yaw, order.Scale, piece, rules, out at, out min, out max);
                if (physical != null)
                {
                    last = physical;
                    continue;
                }
                Settle(piece, holder, at, spots[i].Yaw, order.Scale, min, max, false, "near " + order.Place);
                return;
            }
            Destroy(holder);
            Skip(piece, "line " + order.Line + ": no safe spot near " + order.Place + " (" + (spots.Count == 0 ? "every candidate within 15 m was refused; the requested spot is " + last : "the last candidate: " + last) + ").");
        }

        private void RunRoute(Job job)
        {
            Piece piece = job.Piece;
            LiveWorldOrderH8S order = job.Order;
            if (_stops == null)
            {
                Skip(piece, "line " + order.Line + ": the story route is not available.");
                return;
            }
            int placed = 0;
            int[] perStop = new int[_stops.Length];
            string last = "no route stop had room beside it";
            for (int s = 0; s < _stops.Length && placed < order.Count; s++)
            {
                if (!_stopResolved[s] || _stops[s].Interior) continue;
                LiveWorldRulesH8S rules = LiveWorldSafetyH8S.AreaRules(_layout, _stopPoints[s], _routeGuards, Venues, WorldApertures.All, _discs, _placed);
                LiveWorldPaletteH8S palette = LiveWorldSurfacesH8S.Resolve(order.Palette, LiveWorldSurfacesH8S.PaletteFor(_stops[s].Region));
                if (!piece.Measured)
                {
                    GameObject probe;
                    string why;
                    GameObject h = Spawn(piece, palette, out probe, out why);
                    if (h == null)
                    {
                        Skip(piece, "line " + order.Line + ": " + why);
                        return;
                    }
                    Destroy(h);
                }
                List<WorldRouteSlotH8S> slots = LiveWorldPlannerH8S.RouteSpots(_stopPoints, _stopResolved, _stops, _routeGuards, order.Scale, piece.Min, piece.Max, order.Count, rules, _layout);
                for (int i = 0; i < slots.Count && placed < order.Count; i++)
                {
                    WorldRouteSlotH8S slot = slots[i];
                    if (slot.Stop != s || perStop[s] >= WorldRouteH8S.MaxDressingPerStop || _copies >= MaxCopies) continue;
                    Vector3 at, min, max;
                    string physical = Ground(slot.Position, slot.Yaw, order.Scale, piece, rules, out at, out min, out max);
                    if (physical != null)
                    {
                        last = physical;
                        continue;
                    }
                    GameObject visual;
                    string why;
                    GameObject holder = Spawn(piece, palette, out visual, out why);
                    if (holder == null)
                    {
                        Skip(piece, "line " + order.Line + ": " + why);
                        return;
                    }
                    Settle(piece, holder, at, slot.Yaw, order.Scale, min, max, false, "beside the route stop " + _stops[s].Id);
                    rules.Blocked.Add(_placed[_placed.Count - 1]);
                    perStop[s]++;
                    placed++;
                }
            }
            if (placed == 0) Skip(piece, "line " + order.Line + ": no copy fitted beside the route (" + last + ").");
            else if (placed < order.Count) Line(piece.Entry.Name + ": " + placed + " of " + order.Count + " route copies fitted safely.");
        }

        /// <summary>
        /// A visual-only copy of a piece under a new holder at the origin, with its surfaces applied, measured once per piece.
        /// Null with the reason when the model is missing, carries anything but visual parts, or has an unusable size.
        /// </summary>
        private GameObject Spawn(Piece piece, LiveWorldPaletteH8S palette, out GameObject visual, out string why)
        {
            visual = null;
            why = null;
            GameObject source = piece.Entry.Model;
            if (source == null)
            {
                why = "its model is missing or did not import (" + (string.IsNullOrEmpty(piece.Entry.ModelPath) ? "no model file" : piece.Entry.ModelPath) + ").";
                return null;
            }
            Component[] parts = source.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] == null || !ImportedArt04OH.VisualComponent(parts[i].GetType()))
                {
                    why = "the model carries a " + (parts[i] == null ? "missing script" : parts[i].GetType().Name) + "; export meshes only (no cameras, lights or rigs).";
                    return null;
                }
            GameObject holder = new GameObject(piece.Entry.Name + " (live world art)");
            holder.transform.SetParent(_root, false);
            visual = Instantiate(source, holder.transform, false);
            visual.name = piece.Entry.Name;
            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                animators[i].enabled = false;
                animators[i].applyRootMotion = false;
            }
            ApplySurfaces(piece, visual, palette);
            Layers.SetRecursive(holder, Layers.IgnoreRaycast);
            if (!piece.Measured)
            {
                if (!Measure(holder, out piece.Min, out piece.Max))
                {
                    Destroy(holder);
                    why = "the model has no visible mesh.";
                    return null;
                }
                Vector3 size = piece.Max - piece.Min;
                float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
                if (!LiveWorldSafetyH8S.FiniteBounds(piece.Min, piece.Max) || largest > 150f || largest < 0.05f)
                {
                    Destroy(holder);
                    why = "the model measures " + largest.ToString("0.###") + " m across; check the export scale in Blender (Apply Scalings: FBX All, Apply Transform).";
                    return null;
                }
                piece.Measured = true;
            }
            return holder;
        }

        /// <summary>The rendered bounds of everything under holder relative to its pivot (holder at the origin, unrotated, scale 1).</summary>
        private bool Measure(GameObject holder, out Vector3 min, out Vector3 max)
        {
            Transform t = holder.transform;
            t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;
            Renderer[] renderers = holder.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Mesh mesh = null;
                MeshFilter filter = renderers[r].GetComponent<MeshFilter>();
                if (filter != null) mesh = filter.sharedMesh;
                SkinnedMeshRenderer skin = renderers[r] as SkinnedMeshRenderer;
                if (skin != null) mesh = skin.sharedMesh;
                if (mesh == null) continue;
                Bounds b = mesh.bounds;
                ForgeGalleryFitH8S.Corners(b.min, b.max, _corners);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = t.InverseTransformPoint(renderers[r].transform.TransformPoint(_corners[i]));
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }
            return any;
        }

        /// <summary>
        /// Null when a copy can stand at this plan spot: existing level floor under its centre and corners, still clear of
        /// the rules at the real floor height, no solid geometry in its base and no trigger volume in its space. Otherwise why
        /// not. at is the pivot's resting point; min and max the copy's world bounds there.
        /// </summary>
        private string Ground(Vector3 want, float yaw, float scale, Piece piece, LiveWorldRulesH8S rules, out Vector3 at, out Vector3 min, out Vector3 max)
        {
            at = want;
            min = max = want;
            RaycastHit hit;
            if (!Physics.Raycast(want + Vector3.up * 2.5f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return "no existing floor near the place's height";
            if (hit.normal.y < 0.8f) return "the floor there is too steep";
            float ground = hit.point.y;
            WorldRouteH8S.YawedBounds(piece.Min * scale, piece.Max * scale, yaw, new Vector3(want.x, ground, want.z), out min, out max);
            string conflict = LiveWorldSafetyH8S.Conflict(min, max, ground, rules);
            if (conflict != null) return "inside the protected " + conflict;
            float tolerance = LiveWorldPlannerH8S.GroundTolerance(min, max), low = ground;
            LiveWorldPlannerH8S.ProbePoints(min, max, 0.6f, ground, _probes);
            for (int i = 0; i < 5; i++)
            {
                if (!Physics.Raycast(_probes[i] + Vector3.up * 2.5f, Vector3.down, out hit, 2.5f + tolerance + 1f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
                    return "the ground under it ends at an edge";
                if (Mathf.Abs(hit.point.y - ground) > tolerance) return "the ground under it is too uneven";
                low = Mathf.Min(low, hit.point.y);
            }
            Vector3 c = (min + max) * 0.5f, h = (max - min) * 0.5f;
            Vector3 baseCentre = new Vector3(c.x, ground + 1.15f, c.z);
            Vector3 baseHalf = new Vector3(Mathf.Max(0.1f, h.x * 0.6f), 0.85f, Mathf.Max(0.1f, h.z * 0.6f));
            if (Physics.CheckBox(baseCentre, baseHalf, Quaternion.identity, Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore))
                return "existing solid geometry occupies its base";
            float halfHeight = Mathf.Max(0.5f, (max.y - min.y) * 0.5f);
            int count = Physics.OverlapBoxNonAlloc(new Vector3(c.x, ground + halfHeight, c.z), new Vector3(Mathf.Max(0.1f, h.x), halfHeight, Mathf.Max(0.1f, h.z)), _overlaps,
                Quaternion.identity, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
                if (_overlaps[i] != null && _overlaps[i].isTrigger) return "a trigger volume (" + _overlaps[i].name + ") overlaps it";
            // Rest the lowest rendered point on the lowest floor probe; parts modelled below the pivot (roots, footings) sink in.
            at = new Vector3(want.x, low - Mathf.Max(0f, piece.Min.y * scale) - 0.03f, want.z);
            WorldRouteH8S.YawedBounds(piece.Min * scale, piece.Max * scale, yaw, at, out min, out max);
            return null;
        }

        /// <summary>Moves a spawned copy to its spot, adds distance culling, records its footprint and reports it.</summary>
        private void Settle(Piece piece, GameObject holder, Vector3 at, float yaw, float scale, Vector3 min, Vector3 max, bool indoor, string how)
        {
            holder.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));
            holder.transform.localScale = Vector3.one * scale;
            AddCulling(holder, indoor);
            if (!indoor) LiveWorldSafetyH8S.AddPlaced(_placed, min, max, piece.Entry.Name);
            if (!indoor) _visits.Add(new Visit { At = at, Label = piece.Entry.Name + " " + how });
            Placed(piece, holder, how, at);
        }

        private void Placed(Piece piece, GameObject holder, string how, Vector3 at)
        {
            holder.SetActive(!_hidden);
            piece.Placed++;
            _copies++;
            Log.Info(LiveWorldMessagesH8S.Placed(piece.Entry.Name, how, at));
            Line(piece.Entry.Name + ": placed " + how + " at " + LiveWorldMessagesH8S.Point(at) + ".");
        }

        /// <summary>Distance culling through one LODGroup (an imported LODGroup is kept and only given a culling distance).</summary>
        private static void AddCulling(GameObject holder, bool indoor)
        {
            float cull = LiveWorldPlannerH8S.CullHeight(indoor);
            LODGroup existing = holder.GetComponentInChildren<LODGroup>(true);
            if (existing != null)
            {
                LOD[] lods = existing.GetLODs();
                for (int i = lods.Length - 1, step = 0; i >= 0; i--, step++)
                    lods[i].screenRelativeTransitionHeight = Mathf.Max(lods[i].screenRelativeTransitionHeight, cull + step * 0.001f);
                existing.SetLODs(lods);
                return;
            }
            Renderer[] renderers = holder.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;
            LODGroup group = holder.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[] { new LOD(cull, renderers) });
            group.RecalculateBounds();
        }
    }
}
