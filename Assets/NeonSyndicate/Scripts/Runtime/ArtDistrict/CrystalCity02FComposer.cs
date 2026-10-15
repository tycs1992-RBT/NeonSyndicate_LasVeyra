using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Combines imported art by material and LOD. Only a few landmark rings stay as rotating objects.</summary>
    internal sealed class CrystalCity02FComposer
    {
        private readonly GameObject _root;
        private readonly Vector3 _origin;
        private readonly CrystalCityStyle _style;
        private readonly CrystalCity02FMotion _motion;
        private readonly Dictionary<Material, List<CombineInstance>> _near = new Dictionary<Material, List<CombineInstance>>();
        private readonly Dictionary<Material, List<CombineInstance>> _far = new Dictionary<Material, List<CombineInstance>>();
        private readonly List<PendingRotor> _pending = new List<PendingRotor>();
        private bool _finished;
        public int Renderers { get; private set; }
        public int SourceParts { get; private set; }
        public Transform Transform { get { return _root.transform; } }

        private sealed class PendingRotor
        {
            public Transform Transform;
            public Vector3 Axis;
            public float Speed;
        }

        public CrystalCity02FComposer(string name, Transform parent, Vector3 origin, CrystalCityStyle style, CrystalCity02FMotion motion)
        {
            _origin = origin; _style = style; _motion = motion;
            _root = new GameObject("Crystal City 02F | " + name);
            _root.transform.SetParent(parent, false);
            _root.transform.position = origin;
            // Keep partial construction invisible until all parts for this group succeed.
            _root.SetActive(false);
        }

        public void Append(CrystalCity02FBlueprint blueprint, Vector3 world, Quaternion yaw, float scale, bool animated)
        {
            if (_finished || blueprint == null) throw new InvalidOperationException("02F composer cannot accept more geometry.");
            foreach (CrystalCity02FNode node in blueprint.nodes)
            {
                Material material = CrystalCity02FAssets.Surface(_style, node.material);
                Vector3 pos = world - _origin + yaw * (CrystalCity02FCatalog.Vector(node.position) * scale);
                Quaternion rotation = yaw * Quaternion.Euler(CrystalCity02FCatalog.Vector(node.euler));
                Vector3 size = CrystalCity02FCatalog.Vector(node.scale) * scale;
                if (animated && _motion != null && node.speed != 0f)
                {
                    GameObject pivot = new GameObject("Slow orbit | " + node.model);
                    pivot.transform.SetParent(_root.transform, false);
                    pivot.transform.localPosition = pos;
                    pivot.transform.localRotation = rotation;
                    pivot.transform.localScale = size;
                    Dictionary<Material, List<CombineInstance>> near = new Dictionary<Material, List<CombineInstance>>();
                    Dictionary<Material, List<CombineInstance>> far = new Dictionary<Material, List<CombineInstance>>();
                    Collect(near, node.model, material, Matrix4x4.identity, Settings.Graphics == GraphicsPreset.Low);
                    Collect(far, node.model, material, Matrix4x4.identity, true);
                    InstallLods(pivot, near, far, true);
                    _pending.Add(new PendingRotor { Transform = pivot.transform, Axis = CrystalCity02FCatalog.Vector(node.spin), Speed = node.speed });
                }
                else
                {
                    Matrix4x4 placement = Matrix4x4.TRS(pos, rotation, size);
                    Collect(_near, node.model, material, placement, Settings.Graphics == GraphicsPreset.Low);
                    Collect(_far, node.model, material, placement, true);
                }
                SourceParts++;
            }
        }

        private static void Collect(Dictionary<Material, List<CombineInstance>> batches, string model, Material material, Matrix4x4 placement, bool low)
        {
            MeshFilter[] filters = CrystalCity02FAssets.Model(model, low);
            List<CombineInstance> parts;
            if (!batches.TryGetValue(material, out parts)) { parts = new List<CombineInstance>(); batches.Add(material, parts); }
            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                Matrix4x4 matrix = placement * filter.transform.localToWorldMatrix;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    parts.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = matrix });
            }
        }

        public void Finish(bool hero)
        {
            if (_finished) return;
            GameObject body = new GameObject("Batched stationary art");
            body.transform.SetParent(_root.transform, false);
            InstallLods(body, _near, _far, hero);
            _near.Clear(); _far.Clear();
            _root.SetActive(true);
            foreach (PendingRotor pending in _pending) _motion.AddRotor(pending.Transform, pending.Axis, pending.Speed);
            _pending.Clear();
            _finished = true;
        }

        private void InstallLods(GameObject owner, Dictionary<Material, List<CombineInstance>> near, Dictionary<Material, List<CombineInstance>> far, bool hero)
        {
            Renderer[] nearRenderers = Emit(owner.transform, near, "Detail", false);
            Renderer[] farRenderers = Emit(owner.transform, far, "Silhouette", true);
            LODGroup group = owner.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[] { new LOD(hero ? .06f : .05f, nearRenderers), new LOD(hero ? .006f : .012f, farRenderers) });
            group.RecalculateBounds();
            // Conservative bound for tilted rings which can rotate after the initial import.
            if (hero) group.size *= 1.25f;
        }

        private Renderer[] Emit(Transform parent, Dictionary<Material, List<CombineInstance>> batches, string name, bool distant)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in batches)
            {
                if (pair.Value.Count == 0) continue;
                GameObject child = new GameObject(name + " / " + pair.Key.name);
                child.transform.SetParent(parent, false);
                Mesh combined = OwnedMeshes.Track(child, new Mesh { name = "02F " + name, indexFormat = IndexFormat.UInt32 });
                combined.CombineMeshes(pair.Value.ToArray(), true, true, false);
                combined.RecalculateBounds(); combined.RecalculateTangents();
                child.AddComponent<MeshFilter>().sharedMesh = combined;
                MeshRenderer renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = !distant && Settings.Graphics >= GraphicsPreset.High ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                combined.UploadMeshData(true);
                renderers.Add(renderer); Renderers++;
            }
            return renderers.ToArray();
        }

        public void Abort()
        {
            _pending.Clear(); _near.Clear(); _far.Clear();
            if (_root == null) return;
            _root.SetActive(false); UnityEngine.Object.Destroy(_root);
        }
    }
}
