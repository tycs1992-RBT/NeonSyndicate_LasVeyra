using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>One near/far material-batched garden group, with owned output meshes and shared source meshes.</summary>
    internal sealed class CrystalCity02DBatch
    {
        private readonly GameObject _root;
        private readonly CrystalCityStyle _style;
        private readonly Dictionary<Material, List<CombineInstance>> _near = new Dictionary<Material, List<CombineInstance>>();
        private readonly Dictionary<Material, List<CombineInstance>> _far = new Dictionary<Material, List<CombineInstance>>();
        public Transform Transform { get { return _root.transform; } }

        public CrystalCity02DBatch(string name, Transform parent, Vector3 origin, CrystalCityStyle style)
        {
            _style = style;
            _root = new GameObject(name);
            _root.transform.SetParent(parent, false);
            _root.transform.position = origin;
        }

        public void Part(string model, string material, Vector3 local, Vector3 scale, Quaternion rotation, bool far = true)
        {
            Material mat = CrystalCity02DMaterials.Get(_style, material);
            if (mat == null) return;
            Matrix4x4 matrix = Matrix4x4.TRS(local, rotation, scale);
            Add(_near, mat, CrystalCity02DGeometry.Get(model, Settings.Graphics == GraphicsPreset.Low), matrix);
            if (far) Add(_far, mat, CrystalCity02DGeometry.Get(model, true), matrix);
        }

        private static void Add(Dictionary<Material, List<CombineInstance>> batches, Material mat, Mesh mesh, Matrix4x4 matrix)
        {
            if (mesh == null) return;
            List<CombineInstance> parts;
            if (!batches.TryGetValue(mat, out parts)) { parts = new List<CombineInstance>(); batches.Add(mat, parts); }
            parts.Add(new CombineInstance { mesh = mesh, transform = matrix });
        }

        public void Finish()
        {
            Renderer[] near = Emit(_near, "Detail");
            Renderer[] far = Emit(_far, "Silhouette");
            LODGroup group = _root.AddComponent<LODGroup>();
            group.fadeMode = LODFadeMode.None;
            group.SetLODs(new[] { new LOD(.06f, near), new LOD(.016f, far) });
            group.RecalculateBounds();
            _near.Clear(); _far.Clear();
        }

        private Renderer[] Emit(Dictionary<Material, List<CombineInstance>> batches, string name)
        {
            List<Renderer> output = new List<Renderer>();
            foreach (KeyValuePair<Material, List<CombineInstance>> pair in batches)
            {
                if (pair.Value.Count == 0) continue;
                GameObject child = new GameObject(name + " / " + pair.Key.name);
                child.transform.SetParent(_root.transform, false);
                Mesh mesh = OwnedMeshes.Track(child, new Mesh { name = "Crystal City 02D garden " + name, indexFormat = IndexFormat.UInt32 });
                mesh.CombineMeshes(pair.Value.ToArray(), true, true, false);
                mesh.RecalculateBounds(); mesh.RecalculateTangents();
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                MeshRenderer renderer = child.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = pair.Key;
                renderer.shadowCastingMode = name == "Detail" && Settings.Graphics >= GraphicsPreset.High ? ShadowCastingMode.On : ShadowCastingMode.Off;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                mesh.UploadMeshData(true);
                output.Add(renderer);
            }
            return output.ToArray();
        }

        public void Abort()
        {
            if (_root == null) return;
            _root.SetActive(false);
            UnityEngine.Object.Destroy(_root);
        }
    }
}
