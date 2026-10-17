using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Builds the Underheart's scene geometry from an UnderheartLayout: inward-facing shells (floors, walls with doorways,
    /// ceilings, rock relief), the stair and the surface portal, door frames, gates, lift cages, heat-zone visuals, region
    /// dressing, landmarks and practical lights. Everything sits under one root placed at the Pressure House door with the
    /// venue's quarter-turn yaw, so the layout's venue-frame boxes are built directly in local space. Colliders live under
    /// an always-active root; renderers, lights and particles under one root per render group (each region, plus the
    /// stair entrance and the surface portal), which the runtime streams. Geometry is merged per group and material.
    /// Partials: Shell (floors, walls, ceilings, stairs), Props (the prop kit), Dressing (per cell), Landmarks, Fixtures
    /// (entrances, gates, lifts, heat zones).
    /// </summary>
    public sealed partial class UnderheartBuilder
    {
        /// <summary>A practical light and the render group it belongs to.</summary>
        public sealed class LightRef
        {
            public Light Light;
            public string Group = "";
            public Vector3 World;
        }

        /// <summary>A shortcut gate's sliding door.</summary>
        public sealed class GateRef
        {
            public string Shortcut = "";
            public Transform Door;
            public Vector3 Closed, Open;
            public Collider Blocker;
        }

        /// <summary>A heat zone's live visuals (warning ring and its particles).</summary>
        public sealed class HazardVisual
        {
            public UhHazard Hazard;
            public Renderer Ring;
            public ParticleSystem Particles;
            public string Group = "";
            public Color RingColor;
        }

        public const string GroupEntrance = "entrance";
        public const string GroupPortal = "portal";

        private readonly UnderheartLayout L;
        private readonly UnderheartFrame F;
        public Transform Root { get; private set; }
        public Transform ColliderRoot { get; private set; }
        public readonly Dictionary<string, Transform> Groups = new Dictionary<string, Transform>(StringComparer.Ordinal);
        public readonly List<LightRef> Lights = new List<LightRef>();
        public readonly List<GateRef> Gates = new List<GateRef>();
        public readonly List<HazardVisual> HazardVisuals = new List<HazardVisual>();
        public readonly List<Mesh> OwnedMeshes = new List<Mesh>();

        private readonly Dictionary<string, MeshBuilder> _buckets = new Dictionary<string, MeshBuilder>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _bucketMats = new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _bucketGroups = new Dictionary<string, string>(StringComparer.Ordinal);
        private string _group = "";
        private int _flush;
        private GRand _rand = new GRand(4011);

        public int ColliderCount { get; private set; }
        public int RendererCount { get; private set; }
        public int TriangleCount { get; private set; }
        public int PropCount { get; private set; }
        public int CellsBuilt { get; private set; }
        public readonly List<string> Problems = new List<string>();

        public UnderheartBuilder(UnderheartLayout layout, UnderheartFrame frame, Transform parent)
        {
            L = layout;
            F = frame;
            GameObject root = new GameObject("Underheart 04E");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(frame.OriginX, 0f, frame.OriginZ);
            root.transform.rotation = Quaternion.Euler(0f, frame.Yaw, 0f);
            Root = root.transform;
            GameObject cols = new GameObject("Colliders (always on)");
            cols.transform.SetParent(Root, false);
            ColliderRoot = cols.transform;
        }

        /// <summary>The render group of a cell: the stair entrance and the surface portal stream with the surface.</summary>
        public static string GroupOf(UhCell c)
        {
            if (c.Id == "service.stair") return GroupEntrance;
            if (c.Id == "portal.cut" || c.Id == "portal.ramp") return GroupPortal;
            return c.Region;
        }

        public Transform Group(string id)
        {
            Transform t;
            if (Groups.TryGetValue(id, out t)) return t;
            GameObject g = new GameObject("Render " + id);
            g.transform.SetParent(Root, false);
            Groups[id] = g.transform;
            return g.transform;
        }

        // ================================================================== build

        public IEnumerator Build(Action<string> progress)
        {
            foreach (UhCell c in L.Cells) Group(GroupOf(c));
            Step("vista window", PlanVistaWindow);
            for (int i = 0; i < L.Cells.Count; i++)
            {
                UhCell c = L.Cells[i];
                _group = GroupOf(c);
                try
                {
                    BuildShell(c);
                    CellsBuilt++;
                }
                catch (Exception e)
                {
                    Problems.Add("shell " + c.Id + ": " + e.Message);
                    Log.Exception("Underheart shell " + c.Id, e);
                }
                if (i % 6 == 5)
                {
                    if (progress != null) progress("Carving the Underheart (" + (i + 1) + "/" + L.Cells.Count + ")");
                    yield return null;
                }
            }
            Step("doorways", BuildDoorways);
            Step("entrances", BuildEntrances);
            Step("gates and lifts", BuildShortcutFixtures);
            Step("heat zones", BuildHazards);
            yield return null;
            for (int i = 0; i < L.Cells.Count; i++)
            {
                UhCell c = L.Cells[i];
                _group = GroupOf(c);
                try { Dress(c); }
                catch (Exception e)
                {
                    Problems.Add("dressing " + c.Id + ": " + e.Message);
                    Log.Exception("Underheart dressing " + c.Id, e);
                }
                if (i % 8 == 7) yield return null;
            }
            Step("landmarks", BuildLandmarks);
            yield return null;
            FlushAll();
        }

        private void Step(string what, Action a)
        {
            try { a(); }
            catch (Exception e)
            {
                Problems.Add(what + ": " + e.Message);
                Log.Exception("Underheart " + what, e);
            }
        }

        // ================================================================== merged geometry

        /// <summary>The mesh builder for a material in the current render group.</summary>
        public MeshBuilder Get(Material m, float tile)
        {
            string key = _group + "|" + m.GetInstanceID();
            MeshBuilder b;
            if (_buckets.TryGetValue(key, out b) && b.VertexCount > 60000)
            {
                Emit(key, b);
                b = null;
            }
            if (b == null)
            {
                b = new MeshBuilder(1);
                _buckets[key] = b;
                _bucketMats[key] = m;
                _bucketGroups[key] = _group;
            }
            b.Tile = tile;
            return b;
        }

        private void Emit(string key, MeshBuilder b)
        {
            if (b.VertexCount == 0) return;
            Material m = _bucketMats[key];
            string group = _bucketGroups[key];
            Mesh mesh = b.ToMesh("Underheart " + group + " " + m.name + " " + _flush++);
            OwnedMeshes.Add(mesh);
            bool nearSurface = group == GroupEntrance || group == GroupPortal || group == UnderheartIds.ServiceLevel;
            GameObject go = Shapes.MeshObject(m.name, mesh, new[] { m }, Group(group), false);
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            // Near the street the shells must block sunlight that would otherwise fall through the cut ground slab.
            mr.shadowCastingMode = nearSurface ? ShadowCastingMode.TwoSided : ShadowCastingMode.Off;
            mr.receiveShadows = nearSurface;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            go.isStatic = true;
            RendererCount++;
            TriangleCount += mesh.triangles.Length / 3;
            _buckets.Remove(key);
        }

        private void FlushAll()
        {
            List<string> keys = new List<string>(_buckets.Keys);
            foreach (string k in keys) Emit(k, _buckets[k]);
            _buckets.Clear();
        }

        // ================================================================== colliders

        /// <summary>A static box collider in the venue frame (local to the root).</summary>
        public BoxCollider Box(Vector3 center, Vector3 size, Quaternion rotation, string name = "c")
        {
            if (size.x <= 0.01f || size.y <= 0.01f || size.z <= 0.01f) return null;
            GameObject go = new GameObject(name);
            go.transform.SetParent(ColliderRoot, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rotation;
            go.layer = Layers.World;
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            go.isStatic = true;
            ColliderCount++;
            return bc;
        }

        public BoxCollider Box(Vector3 center, Vector3 size) { return Box(center, size, Quaternion.identity); }

        /// <summary>An axis-aligned collider from min and max corners.</summary>
        public BoxCollider BoxMinMax(Vector3 min, Vector3 max)
        {
            return Box((min + max) * 0.5f, max - min, Quaternion.identity);
        }

        // ================================================================== lights

        /// <summary>A shadowless practical point light (created off; the runtime keeps the nearest few on).</summary>
        public Light AddLight(Vector3 local, Color color, float range, float intensity)
        {
            GameObject go = new GameObject("Underheart light");
            go.transform.SetParent(Group(_group), false);
            go.transform.localPosition = local;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
            l.enabled = false;
            Lights.Add(new LightRef { Light = l, Group = _group, World = Root.TransformPoint(local) });
            return l;
        }

        /// <summary>A world-space position for a venue-frame point.</summary>
        public Vector3 World(Vector3 local) { return Root.TransformPoint(local); }

        /// <summary>Releases the generated meshes (the world is being torn down).</summary>
        public void ReleaseMeshes()
        {
            foreach (Mesh m in OwnedMeshes) if (m != null) UnityEngine.Object.Destroy(m);
            OwnedMeshes.Clear();
        }

        private float Rand(float a, float b) { return _rand.Range(a, b); }
    }
}
