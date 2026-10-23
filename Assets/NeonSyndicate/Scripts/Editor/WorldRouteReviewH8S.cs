#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NeonSyndicate.Editor
{
    /// <summary>
    /// Explicit, reversible VISUAL PREVIEW of the one coherent H8S route through revision 10 (Gothic Civic H5). In Play mode
    /// it resolves every WorldRouteH8S stop through the existing resolvers (CityLayout.ResolveRealmAnchor04OC for Realm
    /// anchors, CampaignPlacesRuntime04E.TryResolve for Venue and Cast tokens), measures each leg on the layout's walk graph,
    /// logs the time estimate against the 4 to 8 minute target, draws the route with line renderers, and places the H8R
    /// modules plus a few restrained crystal and root accents (reusing H8R assets only) wherever they pass the overlay
    /// clearance rule, stand on existing floor and keep clear of existing solid geometry. Everything new is on IgnoreRaycast
    /// with no collider. Floors, colliders, navigation, quests and saves are untouched. End, a failure part-way, leaving
    /// Play mode, closing the window or an assembly reload removes every new object and material.
    /// </summary>
    public sealed class WorldRouteReviewH8S : EditorWindow
    {
        private const string ModuleRoot = "Assets/NeonSyndicate/Art/WorldFoundationH8R/";
        private const string PreviewName = "H8S VISUAL PREVIEW revision 10 route (reversible, no collision)";
        private const string H8RPreviewName = "H8R reversible route art, native acceptance pending";
        private const string AccentModule = "RootMineralBank";
        private const float AccentScale = 0.7f;

        private GameObject _preview;
        private CityLayout _layout;
        private readonly List<Material> _materials = new List<Material>();
        private readonly Vector3[] _corners = new Vector3[8];
        private string _report = "VISUAL PREVIEW only, native Unity acceptance NOT RUN. Start Title > Gothic Civic H5 (revision 10), then use Neon Syndicate > 04OH > H8S Revision 10 route review.";
        private Vector2 _scroll;

        /// <summary>Opens the route review window and starts the preview.</summary>
        [MenuItem("Neon Syndicate/04OH/H8S Revision 10 route review", false, 3210)]
        public static void Open()
        {
            WorldRouteReviewH8S window = GetWindow<WorldRouteReviewH8S>("H8S route");
            window.Begin();
        }

        /// <summary>Ends every running route preview and removes everything it added.</summary>
        [MenuItem("Neon Syndicate/04OH/H8S End Revision 10 route review", false, 3211)]
        public static void EndFromMenu()
        {
            WorldRouteReviewH8S[] open = UnityEngine.Resources.FindObjectsOfTypeAll<WorldRouteReviewH8S>();
            if (open.Length == 0) Log.Info("H8S route review: nothing is running.");
            foreach (WorldRouteReviewH8S window in open) window.Stop("Ended from the menu. Every route preview object is removed.");
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlayState;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            EditorApplication.update += CheckSession;
        }

        private void OnDisable()
        {
            Stop("The review window closed. Every route preview object is removed.");
            EditorApplication.playModeStateChanged -= PlayState;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
            EditorApplication.update -= CheckSession;
        }

        private void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.ExitingEditMode)
                Stop("Play mode is changing. Every route preview object is removed.");
        }

        private void BeforeReload()
        {
            Stop("Scripts are reloading. Every route preview object is removed.");
        }

        private void CheckSession()
        {
            if (_preview == null && _materials.Count == 0) return;
            if (!EditorApplication.isPlaying || _preview == null || Game.Layout != _layout)
                Stop("The world changed, so the route preview ended and everything it added was removed.");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("VISUAL PREVIEW of the H8S route in revision 10. Existing floors, colliders, the walk graph, quest triggers, creatures and saves stay authoritative. Lines, H8R modules and accents have no collision and sit on IgnoreRaycast. Walking, camera, triggers and timing need your check in Unity.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                if (GUILayout.Button("Start or restart the route preview")) Begin();
            if (GUILayout.Button("End and remove the route preview")) Stop("Ended from the window. Every route preview object is removed.");
            EditorGUILayout.TextArea(_report, GUILayout.MinHeight(300));
            EditorGUILayout.EndScrollView();
        }

        private void Begin()
        {
            Stop(null);
            StringBuilder log = new StringBuilder();
            try
            {
                if (!EditorApplication.isPlaying || Game.Player == null || Game.Player.IsDead) throw new InvalidOperationException("Enter Play mode with a living player first.");
                if (Game.Layout == null || !Game.Layout.GothicCivicH5) throw new InvalidOperationException("Start Title > Gothic Civic H5 (world revision 10) first; this route belongs to revision 10 only.");
                if (GameObject.Find(H8RPreviewName) != null) throw new InvalidOperationException("The H8R existing-world art preview is showing; remove it first so the modules are not doubled.");
                _layout = Game.Layout;
                _preview = new GameObject(PreviewName);
                WorldRouteStopH8S[] stops = WorldRouteH8S.Stops();
                List<Vector3> points = new List<Vector3>(), doors = new List<Vector3>();
                List<bool> resolved = new List<bool>();
                int found = ResolveStops(stops, points, doors, resolved, log);
                List<List<Vector3>> legs = new List<List<Vector3>>();
                List<bool> walkedLegs = new List<bool>();
                List<float> lengths = new List<float>();
                MeasureLegs(stops, points, doors, resolved, legs, walkedLegs, lengths, log);
                WorldRouteEstimateH8S estimate = WorldRouteH8S.Estimate(lengths, stops);
                log.Append("\nEstimate from leg lengths and TravelMath speeds: ").Append(estimate.Meters.ToString("0")).Append(" m over ").Append(estimate.Legs).Append(" legs")
                    .Append(estimate.MissingLegs > 0 ? " (" + estimate.MissingLegs + " legs missing because a stop did not resolve)" : "")
                    .Append("; walking ").Append(WorldRouteH8S.Minutes(estimate.WalkSeconds)).Append(", default run ").Append(WorldRouteH8S.Minutes(estimate.RunSeconds))
                    .Append(", sprint ").Append(WorldRouteH8S.Minutes(estimate.SprintSeconds)).Append(", Surge lower bound ").Append(WorldRouteH8S.Minutes(estimate.SurgeSeconds))
                    .Append("; time at stops ").Append(WorldRouteH8S.Minutes(estimate.DwellSeconds)).Append(". Segment estimate (run plus stops) ")
                    .Append(WorldRouteH8S.Minutes(estimate.EstimateSeconds)).Append(estimate.WithinTarget ? ": inside" : ": outside").Append(" the 4 to 8 minute target. Sprint plus stops ")
                    .Append(WorldRouteH8S.Minutes(estimate.SprintSeconds + estimate.DwellSeconds)).Append(", Surge plus stops ").Append(WorldRouteH8S.Minutes(estimate.SurgeSeconds + estimate.DwellSeconds))
                    .Append(". An estimate, not a measured play time.");
                List<WorldRouteGuardH8S> guards = WorldRouteH8S.Guards(points, resolved, stops, legs);
                DrawRoute(stops, points, resolved, legs, walkedLegs);
                int modules = PlaceModules(guards, log);
                int accents = PlaceAccents(stops, points, resolved, guards, log);
                Layers.SetRecursive(_preview, Layers.IgnoreRaycast);
                if (_preview.GetComponentsInChildren<Collider>(true).Length != 0 || _preview.GetComponentsInChildren<Rigidbody>(true).Length != 0 ||
                    _preview.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("A route preview object gained physics or a script; refused.");
                _report = "VISUAL PREVIEW RUNNING in revision 10: " + found + " of " + stops.Length + " stops resolved, " + modules + " of 4 H8R modules and " + accents +
                    " accents passed the clearance rule. Physical: the existing floors, colliders, walk graph and quest triggers (untouched). Overlay: the gold route lines (red where no walk graph route exists), stop pins, modules and accents." +
                    log + "\n\nCheck in Unity (NOT RUN here): walk the route with The Hearth Witness active; each quest trigger fires at its stop; the camera never bumps an overlay; no overlay hides a path, stair, quest prop, creature or Dante; time the segment from the Forge to the return. End to remove.";
                Log.Info("H8S_ROUTE review started." + log.ToString().Replace('\n', ' '));
            }
            catch (Exception e)
            {
                Stop(null);
                _report = "Route review refused safely; everything it added was removed: " + e.Message + (log.Length > 0 ? "\nProgress before the refusal:" + log : "");
                Log.Warn("H8S_ROUTE review refused: " + e.Message);
            }
            Repaint();
        }

        private void Stop(string why)
        {
            bool active = _preview != null || _materials.Count > 0;
            if (_preview != null) DestroyImmediate(_preview);
            _preview = null;
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null) DestroyImmediate(_materials[i]);
            _materials.Clear();
            _layout = null;
            if (!active || why == null) return;
            _report = why + "\n\nLast preview:\n" + _report;
            Log.Info("H8S_ROUTE " + why);
            Repaint();
        }

        // ------------------------------------------------------------------ stops and legs

        private int ResolveStops(WorldRouteStopH8S[] stops, List<Vector3> points, List<Vector3> doors, List<bool> resolved, StringBuilder log)
        {
            int found = 0;
            for (int i = 0; i < stops.Length; i++)
            {
                Vector3 point, door = Vector3.zero;
                string resolver, why;
                bool ok = Resolve(stops[i].Token, out point, out resolver, out why);
                if (ok && stops[i].DoorToken != null)
                {
                    string doorResolver, doorWhy;
                    if (!Resolve(stops[i].DoorToken, out door, out doorResolver, out doorWhy))
                    {
                        ok = false;
                        why = "its door " + stops[i].DoorToken + " did not resolve: " + doorWhy;
                    }
                }
                points.Add(point);
                doors.Add(door);
                resolved.Add(ok);
                if (ok) found++;
                log.Append("\nStop ").Append(i + 1).Append(" ").Append(stops[i].Title).Append(" (").Append(stops[i].Token).Append(") ")
                    .Append(ok ? "resolved at " + point.ToString("F1") + " through " + resolver : "FAILED: " + why).Append(". ").Append(stops[i].Purpose);
                if (!ok) Log.Warn("H8S_ROUTE stop " + stops[i].Id + " (" + stops[i].Token + ") failed: " + why);
            }
            return found;
        }

        /// <summary>Resolves a place token through the existing owner that the quest runtime uses for it.</summary>
        private bool Resolve(string token, out Vector3 point, out string resolver, out string why)
        {
            point = Vector3.zero;
            why = null;
            if (token.StartsWith("Realm:", StringComparison.Ordinal))
            {
                resolver = "CityLayout.ResolveRealmAnchor04OC (as QuestPlaces.Resolve)";
                if (_layout.ResolveRealmAnchor04OC(token, out point)) return true;
                why = "the revision-10 layout has no anchor for it";
                return false;
            }
            resolver = "CampaignPlacesRuntime04E.TryResolve (QuestPlaces.TokenResolver)";
            float yaw;
            return CampaignPlacesRuntime04E.TryResolve(token, out point, out yaw, out why);
        }

        /// <summary>Measures every leg between consecutive resolved stops; each unresolved stop adds one missing leg (a negative length) to the estimate.</summary>
        private void MeasureLegs(WorldRouteStopH8S[] stops, List<Vector3> points, List<Vector3> doors, List<bool> resolved, List<List<Vector3>> legs, List<bool> walkedLegs,
            List<float> lengths, StringBuilder log)
        {
            int previous = -1;
            for (int i = 0; i < stops.Length; i++)
            {
                if (!resolved[i])
                {
                    lengths.Add(-1f);
                    continue;
                }
                if (previous >= 0)
                {
                    bool walked;
                    List<Vector3> line = WorldRouteH8S.LegPolyline(_layout, points[previous], stops[previous].Interior, doors[previous], points[i], stops[i].Interior, doors[i], out walked);
                    float straight = Vector3.Distance(points[previous], points[i]);
                    float followed = WorldRouteH8S.PolylineLength(line);
                    legs.Add(line);
                    walkedLegs.Add(walked);
                    lengths.Add(followed);
                    log.Append("\nLeg ").Append(stops[previous].Id).Append(" to ").Append(stops[i].Id).Append(": straight ").Append(straight.ToString("0.0")).Append(" m, followed ")
                        .Append(followed.ToString("0.0")).Append(" m ").Append(walked ? "on the walk graph (" + line.Count + " points)" : "STRAIGHT (no walk graph route between the outdoor ends)")
                        .Append(", about ").Append(WorldRouteH8S.Minutes(followed / TravelMath.JogSpeed)).Append(" at the default run.");
                }
                previous = i;
            }
        }

        // ------------------------------------------------------------------ drawing

        private void DrawRoute(WorldRouteStopH8S[] stops, List<Vector3> points, List<bool> resolved, List<List<Vector3>> legs, List<bool> walkedLegs)
        {
            // Gold where the leg follows the layout's walk graph, red where the outdoor part had to be drawn straight.
            Material walkLine = LineMaterial("walk", new Color(0.95f, 0.72f, 0.3f));
            Material straightLine = LineMaterial("straight", new Color(0.9f, 0.15f, 0.12f));
            for (int l = 0; l < legs.Count; l++)
            {
                List<Vector3> samples = new List<Vector3>();
                List<Vector3> line = legs[l];
                for (int i = 0; i < line.Count; i++)
                {
                    if (i > 0)
                    {
                        int steps = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(line[i - 1], line[i]) / 6f), 1, 64);
                        for (int k = 1; k < steps; k++) samples.Add(Grounded(Vector3.Lerp(line[i - 1], line[i], k / (float)steps)) + Vector3.up * 0.25f);
                    }
                    samples.Add(Grounded(line[i]) + Vector3.up * 0.25f);
                }
                bool walked = l < walkedLegs.Count && walkedLegs[l];
                LineRenderer lr = Line("VISUAL PREVIEW leg " + (l + 1) + (walked ? " (walk graph)" : " (straight, no walk graph route)"), walked ? walkLine : straightLine, 0.22f);
                lr.positionCount = samples.Count;
                lr.SetPositions(samples.ToArray());
            }
            for (int i = 0; i < stops.Length; i++)
            {
                if (!resolved[i]) continue;
                Vector3 foot = stops[i].Token.StartsWith("Realm:", StringComparison.Ordinal) ? Grounded(points[i]) : points[i];
                LineRenderer pin = Line("VISUAL PREVIEW stop " + (i + 1) + " " + stops[i].Id, LineMaterial(stops[i].Region.ToString(), CrystalColor(stops[i].Region) * 4f), 0.12f);
                pin.positionCount = 2;
                pin.SetPosition(0, foot);
                pin.SetPosition(1, foot + Vector3.up * 3f);
            }
        }

        private LineRenderer Line(string name, Material material, float width)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_preview.transform, false);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 0;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sharedMaterial = material;
            return lr;
        }

        private Material LineMaterial(string key, Color color)
        {
            string name = "H8S VISUAL PREVIEW route line " + key;
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null && _materials[i].name == name) return _materials[i];
            Material m = Mats.Unlit(name, color);
            _materials.Add(m);
            return m;
        }

        /// <summary>The existing floor just under a route point (probing from 1.2 m above so arcades and ceilings are not mistaken for it), or the point itself.</summary>
        private static Vector3 Grounded(Vector3 p)
        {
            RaycastHit hit;
            return Physics.Raycast(p + Vector3.up * 1.2f, Vector3.down, out hit, 3.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore) ? hit.point : p;
        }

        // ------------------------------------------------------------------ H8R modules and accents

        private int PlaceModules(List<WorldRouteGuardH8S> guards, StringBuilder log)
        {
            int placed = 0;
            if (PlaceModule("SanctuaryPortal", "Realm:gallery", new Vector3(0f, 0f, 6f), WorldRouteRegionH8S.Amethyst, guards, log)) placed++;
            if (PlaceModule("ShardwoodElder", "Realm:azure", new Vector3(5f, 0f, 3f), WorldRouteRegionH8S.Azure, guards, log)) placed++;
            if (PlaceModule("RootMineralBank", "Realm:storm-shrine", new Vector3(-5f, 0f, 3f), WorldRouteRegionH8S.Azure, guards, log)) placed++;
            if (PlaceModule("RootSpringArch", "Realm:storm-cavern", new Vector3(0f, 0f, 6f), WorldRouteRegionH8S.Azure, guards, log)) placed++;
            log.Append("\nSanctuaryStair is deliberately not placed: it needs climbable geometry and native grounding and camera proof.");
            return placed;
        }

        private bool PlaceModule(string module, string anchor, Vector3 preferred, WorldRouteRegionH8S region, List<WorldRouteGuardH8S> guards, StringBuilder log)
        {
            Vector3 at;
            if (!_layout.ResolveRealmAnchor04OC(anchor, out at))
            {
                log.Append("\n").Append(module).Append(" not placed: ").Append(anchor).Append(" does not resolve in this layout.");
                return false;
            }
            GameObject holder;
            GameObject visual = Module(module, region, out holder);
            Vector3 min, max;
            PivotBounds(visual, holder.transform, out min, out max);
            List<Vector3> offsets = WorldRouteH8S.CandidateOffsets(preferred);
            string first = null;
            for (int i = 0; i < offsets.Count; i++)
            {
                Vector3 ground;
                string why;
                if (!TryPlace(holder.transform, at + offsets[i], 0f, min, max, guards, out ground, out why))
                {
                    if (first == null) first = why;
                    continue;
                }
                holder.name = "VISUAL PREVIEW " + module + " near " + anchor;
                log.Append("\n").Append(module).Append(" placed near ").Append(anchor).Append(i == 0 ? " at its H8R offset" : " at offset " + offsets[i].ToString("F0") + " (H8R offset refused: " + first + ")")
                    .Append(", on existing floor at ").Append(ground.ToString("F1")).Append(".");
                return true;
            }
            DestroyImmediate(holder);
            log.Append("\n").Append(module).Append(" not placed near ").Append(anchor).Append(": none of ").Append(offsets.Count).Append(" candidate spots passed (H8R offset: ").Append(first).Append(").");
            return false;
        }

        private int PlaceAccents(WorldRouteStopH8S[] stops, List<Vector3> points, List<bool> resolved, List<WorldRouteGuardH8S> guards, StringBuilder log)
        {
            GameObject probeHolder;
            GameObject probe = Module(AccentModule, WorldRouteRegionH8S.Home, out probeHolder);
            probeHolder.transform.localScale = Vector3.one * AccentScale;
            Vector3 min, max;
            PivotBounds(probe, probeHolder.transform, out min, out max);
            DestroyImmediate(probeHolder);
            float radius = Mathf.Sqrt(Mathf.Max(min.x * min.x, max.x * max.x) + Mathf.Max(min.z * min.z, max.z * max.z));
            List<WorldRouteSlotH8S> slots = WorldRouteH8S.DressingSlots(points, resolved, stops, radius, guards);
            int[] perStop = new int[stops.Length];
            int placed = 0, refused = 0;
            for (int s = 0; s < slots.Count && placed < WorldRouteH8S.MaxDressing; s++)
            {
                WorldRouteSlotH8S slot = slots[s];
                if (perStop[slot.Stop] >= WorldRouteH8S.MaxDressingPerStop) continue;
                GameObject holder;
                Module(AccentModule, stops[slot.Stop].Region, out holder);
                holder.transform.localScale = Vector3.one * AccentScale;
                Vector3 ground;
                string why;
                if (!TryPlace(holder.transform, slot.Position, slot.Yaw, min, max, guards, out ground, out why))
                {
                    DestroyImmediate(holder);
                    refused++;
                    continue;
                }
                holder.name = "VISUAL PREVIEW accent beside " + stops[slot.Stop].Id;
                perStop[slot.Stop]++;
                placed++;
                log.Append("\nAccent (").Append(AccentModule).Append(" at ").Append(AccentScale.ToString("0.0")).Append(" scale, ").Append(stops[slot.Stop].Region).Append(" crystal) beside ")
                    .Append(stops[slot.Stop].Id).Append(" at ").Append(ground.ToString("F1")).Append(".");
            }
            log.Append("\nRoute dressing: ").Append(placed).Append(" accents placed, ").Append(refused).Append(" candidate spots refused by the floor, building or collider checks (at most ")
                .Append(WorldRouteH8S.MaxDressingPerStop).Append(" per stop and ").Append(WorldRouteH8S.MaxDressing).Append(" in all; none inside the Forge).");
            return placed;
        }

        /// <summary>Places a holder on existing floor at want with a yaw when its world bounds pass the clearance rule and its base is clear; otherwise says why.</summary>
        private bool TryPlace(Transform holder, Vector3 want, float yaw, Vector3 min, Vector3 max, List<WorldRouteGuardH8S> guards, out Vector3 ground, out string why)
        {
            ground = want;
            RaycastHit hit;
            if (!Physics.Raycast(want + Vector3.up * 2.5f, Vector3.down, out hit, 6f, Layers.WorldMask, QueryTriggerInteraction.Ignore))
            {
                why = "no existing floor near the anchor height";
                return false;
            }
            if (hit.normal.y < 0.8f)
            {
                why = "the floor there is too steep";
                return false;
            }
            ground = hit.point;
            Vector3 worldMin, worldMax;
            WorldRouteH8S.YawedBounds(min, max, yaw, ground, out worldMin, out worldMax);
            string conflict = WorldRouteH8S.OverlayConflict(worldMin, worldMax, guards);
            if (conflict != null)
            {
                why = "inside the protected " + conflict;
                return false;
            }
            if (_layout.IsInsideBuilding(ground, 0.5f))
            {
                why = "inside a building footprint";
                return false;
            }
            Vector3 c = (worldMin + worldMax) * 0.5f, h = (worldMax - worldMin) * 0.5f;
            Vector3 baseCentre = new Vector3(c.x, ground.y + 1.15f, c.z);
            Vector3 baseHalf = new Vector3(Mathf.Max(0.1f, h.x * 0.6f), 0.85f, Mathf.Max(0.1f, h.z * 0.6f));
            if (Physics.CheckBox(baseCentre, baseHalf, Quaternion.identity, Layers.WorldMask | (1 << Layers.Debris) | (1 << Layers.Vehicles), QueryTriggerInteraction.Ignore))
            {
                why = "existing solid geometry occupies its base";
                return false;
            }
            holder.SetPositionAndRotation(ground, Quaternion.Euler(0f, yaw, 0f));
            why = null;
            return true;
        }

        /// <summary>A visual-only copy of an H8R module under a new holder at the world origin, with the regional palette and no animation, physics or scripts.</summary>
        private GameObject Module(string module, WorldRouteRegionH8S region, out GameObject holder)
        {
            string path = ModuleRoot + module + ".fbx";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (source == null) throw new InvalidOperationException("Wait for the actual H8R model import, then retry: " + path);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null && importer.addCollider) throw new InvalidOperationException(module + ".fbx imports with generated colliders; the preview must add no collision.");
            foreach (Component c in source.GetComponentsInChildren<Component>(true))
                if (c == null || !ImportedArt04OH.VisualComponent(c.GetType())) throw new InvalidOperationException("H8R world art must contain visual components only: " + module);
            holder = new GameObject("VISUAL PREVIEW " + module);
            holder.transform.SetParent(_preview.transform, false);
            GameObject visual = (GameObject)Instantiate(source, holder.transform, false);
            visual.name = module + " (H8R module, VISUAL PREVIEW)";
            foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
                animator.applyRootMotion = false;
            }
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++) slots[i] = ModuleMaterial(slots[i] != null ? slots[i].name : "MineralStone", region);
                renderer.sharedMaterials = slots;
            }
            Layers.SetRecursive(holder, Layers.IgnoreRaycast);
            return visual;
        }

        /// <summary>The module's rendered bounds relative to its holder's pivot, in world axes and units (measured with the holder at the origin, unrotated).</summary>
        private void PivotBounds(GameObject visual, Transform holder, out Vector3 min, out Vector3 max)
        {
            holder.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool any = false;
            foreach (MeshFilter filter in visual.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds b = filter.sharedMesh.bounds;
                ForgeGalleryFitH8S.Corners(b.min, b.max, _corners);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 p = filter.transform.TransformPoint(_corners[i]);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }
            if (!any) throw new InvalidOperationException("The H8R module has no rendered mesh: " + visual.name);
        }

        /// <summary>
        /// One coherent palette: the H8R stone, edge, bark and leaf values, with the crystal tinted by region (amethyst for
        /// home and the investigation, ember at the Forge, azure in Shardwood). Runtime materials only, never saved.
        /// </summary>
        private Material ModuleMaterial(string role, WorldRouteRegionH8S region)
        {
            bool crystal = role.Contains("Amethyst") || role.Contains("Azure");
            string name = "H8S VISUAL PREVIEW " + (crystal ? "crystal " + region : role);
            for (int i = 0; i < _materials.Count; i++)
                if (_materials[i] != null && _materials[i].name == name) return _materials[i];
            Shader shader = Mats.StandardShader;
            if (shader == null) throw new InvalidOperationException("The original supported Lit shader is unavailable.");
            bool urp = RenderPath.IsURP;
            Color color = crystal ? CrystalColor(region) : role.Contains("Bark") ? new Color(0.065f, 0.046f, 0.034f) : role.Contains("Leaf") ? new Color(0.025f, 0.095f, 0.059f) :
                role.Contains("WeatheredEdge") ? new Color(0.16f, 0.15f, 0.14f) : new Color(0.065f, 0.077f, 0.091f);
            Material m = new Material(shader) { name = name };
            _materials.Add(m);
            m.SetColor(urp ? "_BaseColor" : "_Color", color);
            m.SetFloat(urp ? "_Smoothness" : "_Glossiness", crystal ? 0.55f : 0.18f);
            if (crystal)
            {
                m.SetColor("_EmissionColor", color * 0.12f);
                m.EnableKeyword("_EMISSION");
            }
            return m;
        }

        private static Color CrystalColor(WorldRouteRegionH8S region)
        {
            switch (region)
            {
                case WorldRouteRegionH8S.Azure: return new Color(0.025f, 0.19f, 0.22f);
                case WorldRouteRegionH8S.Forge: return new Color(0.24f, 0.11f, 0.035f);
                default: return new Color(0.15f, 0.035f, 0.22f);
            }
        }
    }
}
#endif
