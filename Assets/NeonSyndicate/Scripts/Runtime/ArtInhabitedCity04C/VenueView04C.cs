using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Owns one exact shell, its visible collision, local source anchors and earned home display.</summary>
    public sealed partial class VenueView04C : MonoBehaviour
    {
        private VenueExpanded04C _layout;
        private readonly Dictionary<string, VenueAnchor04C> _anchors = new Dictionary<string, VenueAnchor04C>(StringComparer.Ordinal);
        private readonly List<GameObject> _progress = new List<GameObject>();
        private readonly List<int> _progressThresholds = new List<int>();
        private static readonly List<VenueView04C> LiveViews = new List<VenueView04C>();
        private readonly List<Light> _lights = new List<Light>();
        private float _lightClock;
        private bool _ambience;
        public Transform Root { get { return transform; } }
        public float Width { get { return _layout.width; } }
        public float Depth { get { return _layout.depth; } }
        public float IndoorHeight { get { return _layout.occupiedHeight; } }
        public VenueExpanded04C Composition { get { return _layout; } }
        public int ColliderCount { get; private set; }
        public int PartCount { get; private set; }
        public int NearTriangles { get; private set; }
        public int FarTriangles { get; private set; }
        public int RendererCount { get; private set; }

        public void Initialize(VenueBinding04C binding, VenueExpanded04C layout)
        {
            if (_layout != null) throw new InvalidOperationException("04C venue view is already initialized.");
            if(Game.Layout!=null && Game.Layout.Gothic04OB)GothicRoomComposition04OB.Prepare(layout);
            _layout = layout;
            _ambience = Game.Inhabited != null ? Game.Inhabited.AmbienceAtBoot : InhabitedCitySettings04C.Ambience;
            foreach (VenueAnchor04C anchor in layout.anchors) _anchors.Add(anchor.id, anchor);
            VisualRebirth04ABatch batch = new VisualRebirth04ABatch("04C " + layout.id, transform, Vector3.zero, layout.style);
            List<Mesh> temporary = new List<Mesh>();
            try
            {
                foreach (VenueNode04C node in layout.nodes)
                {
                    if (node.kind == "upper04A") { Upper(binding, node, batch, temporary); continue; }
                    if (node.progress > 0) { Progress(node); continue; }
                    Vector3 at = ArtInhabitedCity04C.Vector(node.position), size = ArtInhabitedCity04C.Vector(node.scale);
                    Quaternion rotation = Quaternion.Euler(ArtInhabitedCity04C.Vector(node.euler));
                    Material material = Game.Layout!=null && Game.Layout.Gothic04OB ? GothicRealmMaterials04OB.For(node.role) : ArtInhabitedCity04C.Surface(layout.style, node.role);
                    if (node.kind == "box")
                    {
                        MeshBuilder builder = new MeshBuilder(1); builder.Tile = 2f;
                        builder.Box(0, Vector3.zero, size, UVMode.World);
                        Mesh mesh = builder.ToMesh("04C structural / " + node.id); temporary.Add(mesh);
                        batch.PartMeshes(mesh, mesh, material, at, Vector3.one, rotation);
                        if (node.solid) BoxCollider(node.id, at, size, rotation);
                    }
                    else if (node.kind == "mesh")
                    {
                        Mesh low = ArtInhabitedCity04C.Mesh(node.model, true, node.source);
                        Mesh high = ArtInhabitedCity04C.Mesh(node.model, false, node.source);
                        ValidateMesh(node, high.bounds); ValidateMesh(node, low.bounds);
                        batch.PartMeshes(Settings.Graphics == GraphicsPreset.Low ? low : high, node.far || node.solid ? low : null, material, at, size, rotation);
                        if (node.solid)
                        {
                            GameObject collision = new GameObject("04C visible source collision / " + node.id);
                            collision.transform.SetParent(transform, false); collision.transform.localPosition = at;
                            collision.transform.localRotation = rotation; collision.transform.localScale = size; collision.layer = Layers.World;
                            collision.AddComponent<MeshCollider>().sharedMesh = low; collision.isStatic = true; ColliderCount++;
                        }
                    }
                    else throw new InvalidOperationException("Unknown 04C geometry kind: " + node.kind);
                }
                if(Game.Layout!=null && Game.Layout.Gothic04OB)GothicShell04OB(binding,batch);
                else Ascension04I(batch, temporary);
                batch.Finish(); PartCount += batch.PartCount; NearTriangles += batch.NearTriangles;
                FarTriangles += batch.FarTriangles; RendererCount += batch.RendererCount;
                Lights(); Labels(); SetProgress(0, false);
            }
            catch { batch.Abort(); throw; }
            finally { foreach (Mesh mesh in temporary) ArtInhabitedCity04C.Release(mesh); }
        }

        private void ValidateMesh(VenueNode04C node, Bounds bounds)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(ArtInhabitedCity04C.Vector(node.position), Quaternion.Euler(ArtInhabitedCity04C.Vector(node.euler)), ArtInhabitedCity04C.Vector(node.scale));
            Vector3 min = bounds.min, max = bounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(new Vector3((corner & 1) == 0 ? min.x : max.x, (corner & 2) == 0 ? min.y : max.y, (corner & 4) == 0 ? min.z : max.z));
                if (!MathUtil.IsFinite(point) || point.x < -Width * .5f - .15f || point.x > Width * .5f + .15f || point.z < -1.8f || point.z > Depth + .15f || point.y < -.15f || point.y > IndoorHeight + .5f)
                    throw new InvalidOperationException("04C source mesh exceeds its bounded shell: " + node.id);
            }
        }

        private void BoxCollider(string id, Vector3 at, Vector3 size, Quaternion rotation)
        {
            GameObject collision = new GameObject("04C structural collision / " + id);
            collision.transform.SetParent(transform, false); collision.transform.localPosition = at;
            collision.transform.localRotation = rotation; collision.layer = Layers.World;
            collision.AddComponent<BoxCollider>().size = size; collision.isStatic = true; ColliderCount++;
        }

        private void Upper(VenueBinding04C binding, VenueNode04C node, VisualRebirth04ABatch batch, List<Mesh> temporary)
        {
            float bottom = node.position[1], top = binding.Building.Height;
            RectF footprint = new RectF(-Width * .5f, 0f, Width * .5f, Depth);
            Building original = binding.Building;
            Building local = new Building { Footprint = footprint, Height = original.Height, Seed = original.Seed, Style = original.Style,
                GroundShops = original.GroundShops, Roof = original.Roof, Tiers = original.Tiers, NeonColor = original.NeonColor };
            // Selected source candidates are one-tier parcels. Refuse a future tiered binding rather than flatten its original silhouette.
            if (local.Tiers != 1 || local.Roof == RoofType.Helipad) throw new InvalidOperationException("04C cannot replace protected or tiered roof geometry.");
            Dictionary<Material, MeshBuilder> near = new Dictionary<Material, MeshBuilder>(), far = new Dictionary<Material, MeshBuilder>();
            Action<bool, Dictionary<Material, MeshBuilder>> append = (detailed, map) =>
            {
                Func<Material, MeshBuilder> get = material =>
                {
                    MeshBuilder result;
                    if (!map.TryGetValue(material, out result)) { result = new MeshBuilder(1); map.Add(material, result); }
                    return result;
                };
                VisualRebirth04AArchitecture.Tier(local, footprint, bottom, top, 0, _layout.style, detailed, get);
                VisualRebirth04AArchitecture.Crown(local, footprint, top, _layout.style, detailed, get);
            };
            append(true, near); append(false, far);
            foreach (KeyValuePair<Material, MeshBuilder> pair in near)
            {
                Mesh high = pair.Value.ToMesh("04C preserved upper detail"); temporary.Add(high);
                MeshBuilder lowBuilder; Mesh low = null;
                if (far.TryGetValue(pair.Key, out lowBuilder)) { low = lowBuilder.ToMesh("04C preserved upper silhouette"); temporary.Add(low); }
                batch.PartMeshes(Settings.Graphics == GraphicsPreset.Low && low != null ? low : high, low ?? high, pair.Key, Vector3.zero, Vector3.one, Quaternion.identity);
            }
            BoxCollider("preserved_upper_volume_and_roof", new Vector3(0f, (bottom + top) * .5f, Depth * .5f), new Vector3(Width, top - bottom, Depth), Quaternion.identity);
        }

        private void Progress(VenueNode04C node)
        {
            VisualRebirth04ABatch batch = new VisualRebirth04ABatch("04C earned display " + node.progress, transform, Vector3.zero, _layout.style);
            Mesh low = ArtInhabitedCity04C.Mesh(node.model, true, node.source);
            Mesh high = ArtInhabitedCity04C.Mesh(node.model, false, node.source);
            ValidateMesh(node, high.bounds); ValidateMesh(node, low.bounds);
            batch.PartMeshes(high, low, ArtInhabitedCity04C.Surface(_layout.style, node.role), ArtInhabitedCity04C.Vector(node.position), ArtInhabitedCity04C.Vector(node.scale), Quaternion.Euler(ArtInhabitedCity04C.Vector(node.euler)));
            batch.Finish(); _progress.Add(batch.Transform.gameObject); _progressThresholds.Add(node.progress);
            PartCount += batch.PartCount; NearTriangles += batch.NearTriangles;
            FarTriangles += batch.FarTriangles; RendererCount += batch.RendererCount;
        }

        private void Lights()
        {
            if (_layout.lights == null) return;
            if (_layout.lights.Length > 6) throw new InvalidOperationException("04C practical light budget exceeded.");
            foreach (VenueLight04C source in _layout.lights)
            {
                GameObject fixture = new GameObject("04C practical / " + source.id); fixture.transform.SetParent(transform, false);
                fixture.transform.localPosition = ArtInhabitedCity04C.Vector(source.position);
                Light light = fixture.AddComponent<Light>(); light.type = LightType.Point;
                light.color = new Color(source.color[0], source.color[1], source.color[2]);
                light.range = Mathf.Clamp(source.range, 2f, 11f); light.intensity = Mathf.Clamp(source.intensity, .2f, 1.7f);
                light.shadows = LightShadows.None; light.renderMode = LightRenderMode.Auto;
                light.enabled = false; _lights.Add(light);
            }
        }

        private void Labels()
        {
            Font font = Mats.UIFont;
            if (font == null) throw new InvalidOperationException("04C cannot create its physical entrance name without the existing game font.");
            foreach (VenueLabel04C source in _layout.labels)
            {
                GameObject label = new GameObject("04C physical sign / " + source.id); label.transform.SetParent(transform, false);
                label.transform.localPosition = ArtInhabitedCity04C.Vector(source.position);
                label.transform.localRotation = Quaternion.Euler(0f, source.yaw, 0f);
                TextMesh text = label.AddComponent<TextMesh>(); text.font = font; text.text = source.text;
                text.fontSize = 64; text.characterSize = source.characterSize;
                text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
                text.color = new Color(1f, .93f, .73f);
                MeshRenderer renderer = label.GetComponent<MeshRenderer>(); renderer.sharedMaterial = font.material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false;
                RendererCount++;
            }
        }

        private void OnEnable() { if (!LiveViews.Contains(this)) LiveViews.Add(this); }
        private void OnDisable()
        {
            LiveViews.Remove(this);
            foreach (Light light in _lights) if (light != null) light.enabled = false;
        }
        public static void ResetStatics() { LiveViews.Clear(); }

        private void Update()
        {
            _lightClock -= Time.unscaledDeltaTime;
            if (_lightClock > 0f) return;
            _lightClock = .35f;
            VenueView04C nearest = null; float best = 45f * 45f;
            Vector3 player = LivingCity04CQa.FocusOverride ? LivingCity04CQa.FocusPoint : Game.Player != null ? Game.Player.transform.position : Vector3.zero;
            if ((Game.Player != null || LivingCity04CQa.FocusOverride) && Game.State != GameState.Title && Game.State != GameState.Loading)
            {
                foreach (VenueView04C view in LiveViews)
                {
                    if (view == null || view._layout == null) continue;
                    Vector3 local = view.transform.InverseTransformPoint(player);
                    // Distance to the occupied volume, not a high-rise renderer's enormous bounds.
                    Vector3 closest = new Vector3(Mathf.Clamp(local.x, -7.7f, 7.7f), Mathf.Clamp(local.y, 0f, view.IndoorHeight), Mathf.Clamp(local.z, .15f, view._layout.roomDepth-.3f));
                    float distance = (local - closest).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = view; }
                }
            }
            int budget = Settings.Graphics == GraphicsPreset.Low || !_ambience ? 1 : 3;
            foreach (Light light in _lights) if (light != null) light.enabled = false;
            if (nearest != this) return;
            foreach (VenueView04C other in LiveViews)
                if (other != null && other != this)
                    foreach (Light light in other._lights) if (light != null) light.enabled = false;
            // Across all rooms, only the closest venue enables at most three shadowless practicals.
            // Low and optional ambience-off retain one nearest functional work light on either floor.
            for (int slot = 0; slot < budget; slot++)
            {
                Light chosen = null; float distance = float.MaxValue;
                foreach (Light light in _lights)
                {
                    if (light == null || light.enabled) continue;
                    float d = (light.transform.position - player).sqrMagnitude;
                    if (d < distance) { distance = d; chosen = light; }
                }
                if (chosen != null) chosen.enabled = true;
            }
        }

        public Vector3 LocalAnchor(string id)
        {
            VenueAnchor04C anchor;
            if (_anchors.TryGetValue(id, out anchor)) return ArtInhabitedCity04C.Vector(anchor.position);
            if (id != null && id.EndsWith("_next", StringComparison.Ordinal) && _anchors.TryGetValue(id.Substring(0, id.Length - 5), out anchor))
                return ArtInhabitedCity04C.Vector(anchor.position);
            return Vector3.zero;
        }
        public Vector3 WorldPoint(string id) { return transform.TransformPoint(LocalAnchor(id)); }
        public float AnchorYaw(string id)
        {
            VenueAnchor04C anchor;
            return transform.eulerAngles.y + (_anchors.TryGetValue(id, out anchor) ? anchor.yaw : 0f);
        }
        public bool Contains(Vector3 world)
        {
            if (_layout == null || !MathUtil.IsFinite(world)) return false;
            Vector3 point = transform.InverseTransformPoint(world);
            return Mathf.Abs(point.x) < 7.7f && point.z > .15f && point.z < _layout.roomDepth - .3f && point.y >= -.15f && point.y < IndoorHeight - .15f;
        }
        public bool IsRoofed(Vector3 world) { return Contains(world); }
        public void SetProgress(int ownedRelays, bool quietSignalDone)
        {
            for (int i = 0; i < _progress.Count; i++)
                if (_progress[i] != null) _progress[i].SetActive(_progressThresholds[i] == 10 ? quietSignalDone : ownedRelays >= _progressThresholds[i]);
        }
    }
}
