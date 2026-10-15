using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>Transactional two-level visual composition, grouped by material with bounded mesh sizes.</summary>
    public sealed class VisualRebirth04ABatch
    {
        private readonly GameObject _root;
        private readonly int _style;
        private readonly Dictionary<Material, List<CombineInstance>> _near = new Dictionary<Material, List<CombineInstance>>();
        private readonly Dictionary<Material, List<CombineInstance>> _far = new Dictionary<Material, List<CombineInstance>>();
        private bool _finished;
        private int _parts;
        public Transform Transform { get { return _root.transform; } }
        public int PartCount { get { return _parts; } }
        public int RendererCount { get; private set; }
        public int NearTriangles { get; private set; }
        public int FarTriangles { get; private set; }

        public VisualRebirth04ABatch(string name, Transform parent, Vector3 origin, int style)
        {
            _style = Mathf.Clamp(style, 0, 3);
            _root = new GameObject("Visual Rebirth 04A / " + name);
            _root.transform.SetParent(parent, false);
            _root.transform.localPosition = origin;
            _root.SetActive(false);
        }

        public void Part(string model, string role, Vector3 local, Vector3 scale, Quaternion rotation, bool far = true)
        {
            if (_finished) throw new InvalidOperationException("04A composition is already finished.");
            if (++_parts > 1600) throw new InvalidOperationException("04A composition exceeded its safe part budget.");
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f) throw new ArgumentOutOfRangeException("scale");
            Material material = VisualRebirth04AAssets.Material(_style, role);
            Matrix4x4 matrix = Matrix4x4.TRS(local, rotation, scale);
            Mesh nearMesh = VisualRebirth04AAssets.Mesh(model, Settings.Graphics == GraphicsPreset.Low);
            Add(_near, material, nearMesh, matrix);
            NearTriangles += (int)nearMesh.GetIndexCount(0) / 3;
            if (far)
            {
                Mesh farMesh = VisualRebirth04AAssets.Mesh(model, true);
                Add(_far, material, farMesh, matrix);
                FarTriangles += (int)farMesh.GetIndexCount(0) / 3;
            }
        }

        private static void Add(Dictionary<Material, List<CombineInstance>> map, Material material, Mesh mesh, Matrix4x4 matrix)
        {
            if (mesh == null || !mesh.isReadable || mesh.vertexCount < 3)
                throw new InvalidOperationException("04A source mesh cannot be combined.");
            List<CombineInstance> parts;
            if (!map.TryGetValue(material, out parts)) { parts = new List<CombineInstance>(); map.Add(material, parts); }
            parts.Add(new CombineInstance { mesh = mesh, transform = matrix });
        }

        public void Finish()
        {
            if (_finished) return;
            try
            {
                Renderer[] near = Emit(_near, "detail");
                Renderer[] far = Emit(_far, "silhouette");
                if (near.Length == 0) throw new InvalidOperationException("04A composition has no visible geometry.");
                LODGroup lod = _root.AddComponent<LODGroup>();
                lod.fadeMode = LODFadeMode.None;
                lod.SetLODs(new[] { new LOD(.11f, near), new LOD(.006f, far.Length > 0 ? far : near) });
                lod.RecalculateBounds();
                _near.Clear(); _far.Clear(); _finished = true;
                RendererCount = near.Length + far.Length;
                _root.SetActive(true);
            }
            catch { Abort(); throw; }
        }

        private Renderer[] Emit(Dictionary<Material, List<CombineInstance>> map, string detail)
        {
            List<Renderer> renderers = new List<Renderer>();
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in map)
            {
                List<CombineInstance> chunk = new List<CombineInstance>();
                int vertices = 0;
                foreach (CombineInstance part in pair.Value)
                {
                    if (vertices + part.mesh.vertexCount > 180000 && chunk.Count > 0)
                    {
                        renderers.Add(EmitChunk(pair.Key, chunk, detail)); chunk.Clear(); vertices = 0;
                    }
                    chunk.Add(part); vertices += part.mesh.vertexCount;
                }
                if (chunk.Count > 0) renderers.Add(EmitChunk(pair.Key, chunk, detail));
            }
            return renderers.ToArray();
        }

        private Renderer EmitChunk(Material material, List<CombineInstance> parts, string detail)
        {
            GameObject node = new GameObject(detail + " / " + material.name);
            node.transform.SetParent(_root.transform, false);
            Mesh mesh = OwnedMeshes.Track(node, new Mesh { name = "04A " + detail, indexFormat = IndexFormat.UInt32 });
            mesh.CombineMeshes(parts.ToArray(), true, true, false);
            mesh.RecalculateBounds(); mesh.RecalculateTangents();
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = node.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = Settings.Graphics >= GraphicsPreset.Medium && detail == "detail"
                ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            mesh.UploadMeshData(true);
            return renderer;
        }

        public void Abort()
        {
            _near.Clear(); _far.Clear();
            if (_root == null) return;
            _root.SetActive(false);
            if (Application.isPlaying) UnityEngine.Object.Destroy(_root); else UnityEngine.Object.DestroyImmediate(_root);
        }
    }
}
