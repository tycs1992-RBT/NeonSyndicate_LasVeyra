using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Asterfall and bounded starter frontages. Selection never edits the city layout or physics.</summary>
    public static class DistrictArchitecture
    {
        public const string Name = "Asterfall Promenade";
        public static Vector3 Arrival { get { return new Vector3(42f, 0.2f, 0f); } }
        private static readonly Dictionary<string, MeshFilter[]> Models = new Dictionary<string, MeshFilter[]>();
        private static readonly List<Vector3> Anchors = new List<Vector3>();
        private static readonly HashSet<Building> StarterBuildings = new HashSet<Building>();
        private static readonly HashSet<Building> CityLandmarks = new HashSet<Building>();
        private static readonly List<Vector3> StarterFocus = new List<Vector3>();
        private static Vector3[] _starterAnchorArray = new Vector3[0];
        private static bool _starterSelectionReady;
        private static int _buildings;
        private static bool _missingReported;
        private static bool _drakePlaced;
        public static Vector3 DrakePosition { get; private set; }
        public static int BuildingCount { get { return _buildings; } }
        public static int StarterBuildingCount { get { EnsureStarterSelection(); return StarterBuildings.Count; } }

        public static void ResetStatics()
        {
            CrystalCity02FAssets.ResetStatics();
            CrystalCity02EAssets.ResetStatics();
            CrystalCity02DGeometry.ResetStatics();
            CrystalCity02DMaterials.ResetStatics();
            CrystalGardenDressing.ResetStatics();
            Models.Clear();
            Anchors.Clear();
            StarterBuildings.Clear();
            CityLandmarks.Clear();
            StarterFocus.Clear();
            _starterAnchorArray = new Vector3[0];
            _starterSelectionReady = false;
            _buildings = 0;
            _missingReported = false;
            _drakePlaced = false;
            DrakePosition = Vector3.zero;
        }

        /// <summary>Retains Asterfall and dresses at most six existing starter frontages. No layout is moved.</summary>
        public static bool Contains(Building building)
        {
            if (!Eligible(building)) return false;
            EnsureStarterSelection();
            return OriginalStreet(building) || StarterBuildings.Contains(building) || CityLandmarks.Contains(building);
        }

        private static bool Eligible(Building building)
        {
            return building != null && building.Roof != RoofType.Helipad && building.Footprint.Valid &&
                building.Footprint.Width >= 6f && building.Footprint.Depth >= 6f;
        }

        private static bool OriginalStreet(Building building)
        {
            RectF f = building.Footprint;
            if (f.CenterX < 22f || f.CenterX > 238f) return false;
            return (f.Z0 >= 8f && f.Z0 <= 48f) || (f.Z1 <= -8f && f.Z1 >= -48f);
        }

        public static Vector3[] StarterAnchors() { EnsureStarterSelection(); return _starterAnchorArray; }

        private static void EnsureStarterSelection()
        {
            if (_starterSelectionReady || Game.Layout == null) return;
            _starterSelectionReady = true;
            SelectStarter(Game.Layout.FindPoi("Veyra Lofts"), 4);
            SelectStarter(Game.Layout.FindPoi("Mara Solane Street Clinic"), 2);
            SelectCityLandmarks(Game.Layout);
            _starterAnchorArray = StarterFocus.ToArray();
        }

        /// <summary>
        /// Distribute the existing full-facade art system across the city rather than
        /// limiting it to one avenue. One architectural landmark per ~112m cell,
        /// with a strict 210-building cap to bound startup and draw calls.
        /// The small synthetic layouts used by EditMode tests are unchanged.
        /// </summary>
        private static void SelectCityLandmarks(CityLayout layout)
        {
            if (layout.Blocks.Count < 100) return;
            // Respect existing graphics settings; a large combined facade mesh
            // is significantly more expensive than an ordinary city shell.
            int budget = Settings.Graphics == GraphicsPreset.Low ? 35 :
                (Settings.Graphics == GraphicsPreset.Medium ? 80 :
                (Settings.Graphics == GraphicsPreset.High ? 145 : 210));
            const float cell = 112f;
            HashSet<long> used = new HashSet<long>();
            for (int i = 0; i < layout.Blocks.Count && CityLandmarks.Count < budget; i++)
            {
                Block block = layout.Blocks[i];
                foreach (Lot lot in block.Lots)
                {
                    if (lot.CustomBuilt || lot.Use == LotUse.Park || lot.Use == LotUse.Parking) continue;
                    foreach (Building candidate in lot.Buildings)
                    {
                        if (!Eligible(candidate) || OriginalStreet(candidate) || StarterBuildings.Contains(candidate)) continue;
                        RectF footprint = candidate.Footprint;
                        if (Mathf.Abs(footprint.CenterX) > CityLayout.CoreHalf - 30f ||
                            Mathf.Abs(footprint.CenterZ) > CityLayout.CoreHalf - 30f) continue;
                        if (footprint.Width < 11f || footprint.Depth < 11f || candidate.Height < 14f) continue;
                        int cx = Mathf.FloorToInt(footprint.CenterX / cell);
                        int cz = Mathf.FloorToInt(footprint.CenterZ / cell);
                        long key = ((long)cx << 32) ^ (uint)cz;
                        if (used.Contains(key)) continue;
                        used.Add(key);
                        CityLandmarks.Add(candidate);
                        if (CityLandmarks.Count >= budget) return;
                    }
                }
            }
        }

        private static void SelectStarter(Poi poi, int budget)
        {
            if (poi == null) return;
            Vector3 facing = poi.Facing.sqrMagnitude > 0.01f ? poi.Facing.normalized : Vector3.forward;
            Vector3 focus = poi.Position + facing * 3.5f;
            StarterFocus.Add(focus);
            // Looking out from the unchanged spawn should reveal a fantasy frontage across the road.
            Vector3 composition = focus + facing * 17f;
            for (int pick = 0; pick < budget; pick++)
            {
                Building best = null;
                float bestScore = float.PositiveInfinity;
                foreach (Block block in Game.Layout.Blocks)
                    foreach (Lot lot in block.Lots)
                    {
                        if (lot.CustomBuilt) continue;
                        foreach (Building candidate in lot.Buildings)
                        {
                            if (!Eligible(candidate) || StarterBuildings.Contains(candidate) || OriginalStreet(candidate)) continue;
                            RectF f = candidate.Footprint;
                            float dx = Mathf.Max(f.X0 - focus.x, Mathf.Max(0f, focus.x - f.X1));
                            float dz = Mathf.Max(f.Z0 - focus.z, Mathf.Max(0f, focus.z - f.Z1));
                            if (dx * dx + dz * dz > 70f * 70f) continue;
                            Vector3 delta = f.Center(0f) - composition;
                            float score = delta.x * delta.x + delta.z * delta.z;
                            if (score >= bestScore) continue;
                            best = candidate;
                            bestScore = score;
                        }
                    }
                if (best == null) break;
                StarterBuildings.Add(best);
            }
        }

        internal static Vector3 ViewFocus(Building building)
        {
            Vector3 result = new Vector3(building.Footprint.CenterX, 0f, 0f);
            if (!StarterBuildings.Contains(building)) return result;
            float nearest = float.PositiveInfinity;
            foreach (Vector3 focus in StarterFocus)
            {
                float distance = (focus - building.Footprint.Center(0f)).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance;
                result = focus;
            }
            return result;
        }

        internal static MeshFilter[] Model(string name)
        {
            MeshFilter[] filters;
            if (Models.TryGetValue(name, out filters)) return filters;
            GameObject model = Resources.Load<GameObject>("ArtDistrict/Models/" + name);
            filters = model == null ? new MeshFilter[0] : model.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
                if (filters[i].sharedMesh == null || !filters[i].sharedMesh.isReadable) filters = new MeshFilter[0];
            Models[name] = filters;
            return filters;
        }

        internal static void Register(Vector3 anchor)
        {
            _buildings++;
            if (_buildings == 1) Log.Info("Crystal City 02D architecture is active. West: Verdant Reach. Centre: Amethyst Ward. East: Dawnstone Court.");
            // Spread lights along the street; do not add one per window or crystal.
            bool close = false;
            for (int i = 0; i < Anchors.Count; i++) if ((Anchors[i] - anchor).sqrMagnitude < 30f * 30f) close = true;
            if (!close && Anchors.Count < 32) Anchors.Add(anchor);
        }

        internal static bool ClaimDrake(Vector3 position)
        {
            if (_drakePlaced) return false;
            _drakePlaced = true;
            DrakePosition = position;
            return true;
        }

        public static Vector3[] LightAnchors()
        {
            List<Vector3> result = new List<Vector3>();
            foreach (Vector3 focus in StarterFocus)
            {
                int best = -1;
                float distance = 90f * 90f;
                for (int i = 0; i < Anchors.Count; i++)
                {
                    if (result.Contains(Anchors[i])) continue;
                    float candidate = (Anchors[i] - focus).sqrMagnitude;
                    if (candidate >= distance) continue;
                    distance = candidate;
                    best = i;
                }
                if (best >= 0) result.Add(Anchors[best]);
            }
            for (int i = 0; i < Anchors.Count && result.Count < 4; i++)
                if (!result.Contains(Anchors[i])) result.Add(Anchors[i]);
            return result.ToArray();
        }

        internal static bool Ready()
        {
            string[] required = { "block", "arch_frame", "arch_frame_lod", "arch_infill", "window_frame", "window_infill", "lattice_panel", "pilaster", "cornice", "parapet", "crystal_cluster", "crystal_shard", "dome", "balcony", "canopy", "canopy_bracket", "lantern", "lantern_core", "planter", "tree_trunk", "tree_canopy", "rosette" };
            for (int i = 0; i < required.Length; i++)
            {
                if (Model(required[i]).Length > 0) continue;
                if (!_missingReported) Log.Warn("Art district kit is missing or unreadable. Original buildings remain visible. Use Art District > Repair Import Settings.");
                _missingReported = true;
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Builds facade modules inside the existing tier envelopes. Two material-batched LODs per building;
    /// no colliders, triggers, scripts on individual modules, or new gameplay state.
    /// </summary>
    public sealed partial class DistrictBuilding
    {
        private readonly GameObject _root;
        private readonly Building _building;
        private readonly Vector3 _origin;
        private readonly Dictionary<Material, List<CombineInstance>> _near = new Dictionary<Material, List<CombineInstance>>();
        private readonly Dictionary<Material, List<CombineInstance>> _far = new Dictionary<Material, List<CombineInstance>>();
        private readonly List<Mesh> _temporary = new List<Mesh>();
        private readonly bool _warm;
        private Vector3 _beacon;
        private Vector3 _drakeSpot;
        private bool _hasDrakeSpot;

        private DistrictBuilding(Building building, Transform parent)
        {
            _building = building;
            _warm = (building.Seed & 1) == 0;
            _fantasyStyle = CrystalCity02DMaterials.StyleAt(building.Footprint.Center(0f));
            _origin = building.Footprint.Center(0f);
            _root = new GameObject("Crystal City 02D | " + CrystalCity02DMaterials.StyleName(_fantasyStyle) + " | " + building.Seed);
            _root.transform.SetParent(parent, false);
            _root.transform.position = _origin;
        }

        public static DistrictBuilding TryBegin(Building building, Transform parent)
        {
            try
            {
                if (!DistrictArchitecture.Contains(building) || !DistrictArchitecture.Ready()) return null;
                return new DistrictBuilding(building, parent);
            }
            catch (Exception error)
            {
                Log.Exception("Art district preparation (original building retained)", error);
                return null;
            }
        }

        private void Part(string model, string material, Vector3 world, Quaternion rotation, Vector3 scale, bool distant = false, bool detailed = true)
        {
            // The same art parts now have district-specific stone, glass and foliage.
            if (model == "tree_trunk" || model == "tree_canopy")
            {
                CrystalPart(model == "tree_trunk" ? "trunk" : "leaves", material, world, rotation, scale, distant);
                return;
            }
            Material mat = CrystalCity02DMaterials.Get(_fantasyStyle, material);
            if (mat == null) return;
            MeshFilter[] parts = DistrictArchitecture.Model(model);
            Matrix4x4 placement = Matrix4x4.TRS(world - _origin, rotation, scale);
            for (int i = 0; i < parts.Length; i++)
            {
                Mesh mesh = parts[i].sharedMesh;
                Matrix4x4 imported = parts[i].transform.localToWorldMatrix;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    CombineInstance ci = new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = placement * imported };
                    if (detailed) Add(_near, mat, ci);
                    if (distant) Add(_far, mat, ci);
                }
            }
        }

        private static void Add(Dictionary<Material, List<CombineInstance>> batches, Material material, CombineInstance part)
        {
            List<CombineInstance> items;
            if (!batches.TryGetValue(material, out items)) { items = new List<CombineInstance>(); batches.Add(material, items); }
            items.Add(part);
        }

        private void Box(string material, Vector3 center, Vector3 size, Quaternion rotation, bool distant)
        {
            // The module has its origin at the bottom, with unit dimensions.
            Part("block", material, center - rotation * Vector3.up * size.y * 0.5f, rotation, size, distant);
        }

        private void Shell(RectF f, float bottom, float top)
        {
            MeshBuilder b = new MeshBuilder(1);
            b.Tile = 3f;
            b.Box(0, f.Center((bottom + top) * 0.5f) - _origin, new Vector3(f.Width - 0.08f, top - bottom, f.Depth - 0.08f), UVMode.World);
            Mesh mesh = b.ToMesh("Asterfall recessed mineral shell");
            mesh.RecalculateTangents();
            _temporary.Add(mesh);
            CombineInstance ci = new CombineInstance { mesh = mesh, transform = Matrix4x4.identity };
            Material mat = CrystalCity02DMaterials.Get(_fantasyStyle, "basalt");
            Add(_near, mat, ci);
            Add(_far, mat, ci);
        }

        public void AddTier(RectF f, float bottom, float top, int tier, bool last)
        {
            if (top <= bottom) return;
            Shell(f, bottom, top);
            Box("basalt", f.Center(top - 0.05f), new Vector3(f.Width, 0.1f, f.Depth), Quaternion.identity, true);
            for (int side = 0; side < 4; side++) Face(f, bottom, top, tier, side);
            if (tier == 0)
            {
                Vector3 focus = DistrictArchitecture.ViewFocus(_building);
                Vector3 delta = focus - f.Center(0f);
                int faceSide = Mathf.Abs(delta.x) > Mathf.Abs(delta.z) ? (delta.x > 0f ? 1 : 3) : (delta.z > 0f ? 0 : 2);
                Quaternion face = Quaternion.Euler(0f, faceSide * 90f, 0f);
                Vector3 faceCenter = FaceCenter(f, bottom, faceSide);
                float faceWidth = faceSide % 2 == 0 ? f.Width : f.Depth;
                _beacon = faceCenter + face * new Vector3(-faceWidth * 0.29f,
                    Mathf.Max(1f, Mathf.Min(top - bottom - 3.4f, 4.4f)), 0.06f);
                Part("crystal_cluster", _warm ? "crystalViolet" : "crystal", _beacon, face,
                    new Vector3(0.7f, 1.18f, 0.30f), true);
            }
            if (last && f.Width > 10f && f.Depth > 10f)
            {
                float radius = Mathf.Min(3.4f, Mathf.Min(f.Width, f.Depth) * 0.2f);
                Vector3 crown = f.Center(top + 0.08f);
                Part("dome", "ceramic", crown, Quaternion.identity, new Vector3(radius, radius, radius), true);
                Part("crystal_cluster", _warm ? "crystalViolet" : "crystal", crown + Vector3.up * radius * 0.38f, Quaternion.Euler(0f, _building.Seed % 360, 0f), new Vector3(0.82f, 1.05f, 0.82f), true);
                RoofGarden(f, top);
                // 02B: Automatic skyline transformation. The crystalline crest is
                // authored from the existing imported crystal meshes, so it appears
                // in procedurally generated Free Story districts without an editor
                // placement step or any new package dependency.
                SkylineCrest(f, top);
                if (f.Width >= 14f && f.Depth >= 14f && top < 100f)
                {
                    _hasDrakeSpot = true;
                    _drakeSpot = new Vector3(f.CenterX, top + 0.12f, f.CenterZ > 0f ? f.Z0 + 3.8f : f.Z1 - 3.8f);
                }
            }
        }

        private void SkylineCrest(RectF f, float top)
        {
            LandmarkRoof02D(f, top);
        }

        private static Vector3 FaceCenter(RectF f, float bottom, int side)
        {
            if (side == 0) return new Vector3(f.CenterX, bottom, f.Z1);
            if (side == 1) return new Vector3(f.X1, bottom, f.CenterZ);
            if (side == 2) return new Vector3(f.CenterX, bottom, f.Z0);
            return new Vector3(f.X0, bottom, f.CenterZ);
        }

        private void Face(RectF f, float bottom, float top, int tier, int side)
        {
            float h = top - bottom;
            if (h < 2f) return;
            Quaternion rot = Quaternion.Euler(0f, side * 90f, 0f);
            float width = side % 2 == 0 ? f.Width : f.Depth;
            Vector3 center = FaceCenter(f, bottom, side);
            string mineral = _warm ? "crystalViolet" : "crystal";
            // 01C changes the building's large shapes. A few soaring arcades replace the old
            // repetitive four-metre window grid. Physics and the tier's solid envelope are retained.
            float bayWidth = _fantasyStyle == CrystalCityStyle.DawnstoneCourt ? 6.8f :
                (_fantasyStyle == CrystalCityStyle.VerdantReach ? 11f : 9f);
            int bays = Mathf.Clamp(Mathf.FloorToInt(width / bayWidth), 1, 8);
            float spacing = width / bays;
            float rowHeight = _fantasyStyle == CrystalCityStyle.DawnstoneCourt ? 16f :
                (_fantasyStyle == CrystalCityStyle.VerdantReach ? 28f : 24f);
            int registers = Mathf.Clamp(Mathf.CeilToInt(h / rowHeight), 1, 3);
            float registerHeight = h / registers;
            for (int row = 0; row < registers; row++)
            {
                float y = row * registerHeight + 0.6f;
                float arcadeHeight = Mathf.Max(1f, registerHeight - 1.3f);
                for (int bay = 0; bay < bays; bay++)
                {
                    float x = (bay + 0.5f) * spacing - width * 0.5f;
                    Vector3 at = center + rot * new Vector3(x, y, 0.21f);
                    Vector3 scale = new Vector3(Mathf.Min(2.4f, spacing / 3.4f), arcadeHeight / 4f, 1.3f);
                    // Both LODs keep the hero profile. Opaque pane front remains ahead of the shell.
                    Part("arch_frame", "sandstone", at, rot, scale);
                    Part("arch_frame_lod", "sandstone", at, rot, scale, true, false);
                    Part("arch_infill", "glass", at + rot * new Vector3(0f, 0f, 0.005f), rot, scale, true);
                    // Central mineral lancet reads as an inlaid structural window, never a billboard.
                    Vector3 inlay = at + rot * new Vector3(0f, arcadeHeight * 0.17f, 0.02f);
                    Part("window_frame", "bronze", inlay, rot,
                        new Vector3(scale.x * 0.36f, arcadeHeight * 0.19f, 1f));
                    Part("window_infill", mineral, inlay + rot * new Vector3(0f, 0f, 0.015f), rot,
                        new Vector3(scale.x * 0.36f, arcadeHeight * 0.19f, 1f), true);
                    if (tier == 0 && row == 0 && bay == bays / 2 && h > 6f)
                    {
                        // Retain a human-scale sheltered threshold beneath the monumental facade.
                        Vector3 threshold = center + rot * new Vector3(x, 0.2f, 0.34f);
                        Part("window_frame", "bronze", threshold, rot, new Vector3(1.12f, 1.15f, 1f));
                        Part("window_infill", "glass", threshold + rot * new Vector3(0f, 0f, 0.005f), rot,
                            new Vector3(1.12f, 1.15f, 1f));
                        Part("canopy", "fabric", threshold + Vector3.up * 3.15f, rot, new Vector3(1.1f, 1f, 0.72f));
                        Part("canopy_bracket", "bronze", threshold + Vector3.up * 3.15f, rot, new Vector3(1.1f, 1f, 0.72f));
                        Vector3 lantern = threshold + rot * new Vector3(1.65f, 2.7f, 0.15f);
                        Part("lantern", "bronze", lantern, rot, Vector3.one * 0.66f);
                        Part("lantern_core", "crystalWarm", lantern, rot, Vector3.one * 0.66f);
                    }
                }
                // Quiet shadow band separates large arcades, without strip-mall window ornament.
                Box("ceramic", center + rot * new Vector3(0f, (row + 1) * registerHeight - 0.15f, 0.10f),
                    new Vector3(width, 0.20f, 0.30f), rot, true);
            }
            for (int pier = 0; pier <= bays; pier++)
            {
                float x = pier * spacing - width * 0.5f;
                // Edge piers remain within the footprint in X and project less than a metre in Z.
                x = Mathf.Clamp(x, -width * 0.5f + 0.52f, width * 0.5f - 0.52f);
                Vector3 at = center + rot * new Vector3(x, 0f, 0.07f);
                Part("pilaster", "ceramic", at, rot, new Vector3(1.5f, h / 4f, 1.45f), true);
                if (h > 5f && (pier == 0 || pier == bays || pier % 2 == 0))
                {
                    // A single long mineral fin grows from each selected load-bearing pier.
                    // All growth begins above pedestrian head height; no freestanding street debris.
                    Vector3 seam = at + rot * new Vector3(0f, 3f, 0.33f);
                    Part("crystal_shard", mineral, seam, rot,
                        new Vector3(1.9f, h - 3.2f, 0.70f), true);
                }
            }
            FacadeRelief02D(center, rot, width, h, tier);
            int roofBays = Mathf.Max(1, Mathf.CeilToInt(width / 8f));
            for (int i = 0; i < roofBays; i++)
            {
                float x = (i + 0.5f) * width / roofBays - width * 0.5f;
                Vector3 at = center + rot * new Vector3(x, h - 0.12f, -0.1f);
                Part("cornice", "sandstone", at, rot, new Vector3(width / roofBays / 4f, 1f, 1.2f), true);
                Part("parapet", "ceramic", at + Vector3.up * 0.25f, rot,
                    new Vector3(width / roofBays / 4f, 0.85f, 1f), true);
            }
        }

        private void RoofGarden(RectF f, float top)
        {
            // Rooftop gardens hint at the future mystical forest. They do not occupy roads or walk nodes.
            for (int i = 0; i < 2; i++)
            {
                Vector3 at = new Vector3(i == 0 ? f.X0 + 2.3f : f.X1 - 2.3f, top + 0.1f, f.CenterZ);
                Part("planter", "basalt", at, Quaternion.identity, Vector3.one);
                Part("tree_trunk", "bark", at + Vector3.up * 0.45f, Quaternion.Euler(0f, 67f * i, 0f), Vector3.one * 0.75f);
                Part("tree_canopy", "foliage", at + Vector3.up * 0.45f, Quaternion.Euler(0f, 67f * i, 0f), Vector3.one * 0.75f);
                // Mineral growth overlaps the existing root, rather than floating beside the planter.
                Part("crystal_cluster", i == 0 ? "crystal" : "crystalViolet", at + new Vector3(0.12f, 0.54f, 0.07f),
                    Quaternion.Euler(0f, 67f * i, 16f), new Vector3(0.20f, 0.28f, 0.20f));
            }
        }

        private Renderer[] Emit(Dictionary<Material, List<CombineInstance>> batches, string name)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in batches)
            {
                if (pair.Value.Count == 0) continue;
                Mesh combined = new Mesh { name = name + " " + pair.Key.name, indexFormat = IndexFormat.UInt32 };
                combined.CombineMeshes(pair.Value.ToArray(), true, true, false);
                combined.RecalculateBounds();
                combined.RecalculateTangents();
                GameObject go = new GameObject(combined.name);
                go.transform.SetParent(_root.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = OwnedMeshes.Track(go, combined);
                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                combined.UploadMeshData(true);
                renderers.Add(renderer);
            }
            return renderers.ToArray();
        }

        /// <summary>Discard a failed optional art pass before falling back to the original building.</summary>
        public void Abort()
        {
            for (int i = 0; i < _temporary.Count; i++)
                if (_temporary[i] != null) UnityEngine.Object.Destroy(_temporary[i]);
            _temporary.Clear();
            if (_root != null) { _root.SetActive(false); UnityEngine.Object.Destroy(_root); }
        }

        public void Complete()
        {
            Renderer[] near = Emit(_near, "Detail");
            Renderer[] far = Emit(_far, "Silhouette");
            LODGroup lod = _root.AddComponent<LODGroup>();
            // A non-fading transition works on Standard and the optional URP path without keyword stripping issues.
            lod.fadeMode = LODFadeMode.None;
            lod.SetLODs(new[] { new LOD(0.17f, near), new LOD(0.006f, far) });
            lod.RecalculateBounds();
            for (int i = 0; i < _temporary.Count; i++) UnityEngine.Object.Destroy(_temporary[i]);
            _temporary.Clear();
            _near.Clear();
            _far.Clear();
            // One capped, distance-gated crystal emitter per building, no gameplay hitbox.
            DistrictCrystalFx.Attach(_root.transform, _beacon - _origin, 0.6f, _warm);
            DistrictArchitecture.Register(_beacon);
            if (_hasDrakeSpot && DistrictArchitecture.ClaimDrake(_drakeSpot))
            {
                try { DistrictDrakeDisplay.Attach(_root.transform, _drakeSpot, _drakeSpot.z > 0f ? 180f : 0f); }
                catch (Exception error) { Log.Exception("Cosmetic district drake", error); }
            }
        }
    }
}
