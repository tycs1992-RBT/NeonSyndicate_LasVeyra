using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Transactional, source-data-driven venue art. A failed build never suppresses the old city shell.</summary>
    public static class ArtInhabitedCity04C
    {
        private const string ResourceRoot = "InhabitedCity04C/";
        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);

        public static VenueLayout04C Load(string id)
        {
            if (string.IsNullOrEmpty(id) || id.IndexOf('/') >= 0 || id.IndexOf('.') >= 0) throw new ArgumentException("Invalid 04C layout identifier.");
            TextAsset source = Resources.Load<TextAsset>(ResourceRoot + "Layouts/" + id);
            if (source == null) throw new FileNotFoundException("04C venue source is unavailable: " + id);
            VenueLayout04C layout = JsonUtility.FromJson<VenueLayout04C>(source.text);
            if (layout == null || layout.version != "04C" || layout.id != id || layout.style < 0 || layout.style > 3)
                throw new InvalidDataException("04C venue source is invalid: " + id);
            return layout;
        }

        public static VenueView04C Build(VenueBinding04C binding, Transform parent)
        {
            if (binding == null || binding.Definition == null || binding.Building == null) throw new ArgumentNullException("binding");
            VenueLayout04C layout = Load(binding.Definition.Layout);
            VenueExpanded04C expanded = InhabitedCity04CComposition.Expand(layout, binding.Width, binding.Depth, binding.Building.Height);
            if (!VisualRebirth04AArchitecture.Ready()) throw new InvalidOperationException("04C requires the original 04A architecture kit.");
            // Resolve both LODs and every surface before any geometry can replace the original building.
            foreach (VenueNode04C node in expanded.nodes)
            {
                Surface(expanded.style, node.role);
                if (node.kind == "mesh") { Mesh(node.model, false, node.source); Mesh(node.model, true, node.source); }
            }
            GameObject root = new GameObject("Inhabited City 04C / " + binding.Definition.Title);
            root.transform.SetParent(parent, false);
            root.transform.position = binding.Origin;
            root.transform.rotation = Quaternion.Euler(0f, binding.Yaw, 0f);
            root.SetActive(false);
            try
            {
                VenueView04C view = root.AddComponent<VenueView04C>();
                view.Initialize(binding, expanded);
                root.SetActive(true);
                return view;
            }
            catch { Release(root); throw; }
        }

        public static Mesh Mesh(string model, bool low, string source)
        {
            if (source == "04A") return VisualRebirth04AAssets.Mesh(model, low);
            if (source == "04B") return ArtTerritories04B.Mesh(model, low, source);
            if (source != "04C" || string.IsNullOrEmpty(model) || model.IndexOf('/') >= 0 || model.IndexOf('.') >= 0)
                throw new ArgumentException("Invalid 04C model source.");
            string key = model + (low ? "_low" : "_high");
            Mesh cached;
            if (Meshes.TryGetValue(key, out cached) && cached != null) return cached;
            TextAsset data = Resources.Load<TextAsset>(ResourceRoot + "Meshes/" + key);
            if (data == null) throw new FileNotFoundException("Missing 04C original mesh: " + key);
            Mesh result = CrystalCity02HMeshFallback.Decode(data.bytes, "Inhabited City 04C / " + key);
            try { result.RecalculateTangents(); Meshes.Add(key, result); return result; }
            catch { Release(result); throw; }
        }

        public static Material Surface(int style, string role)
        {
            if (role != "timber" && role != "linen" && role != "lamp") return VisualRebirth04AAssets.Material(style, role);
            string key = style + "/" + role;
            Material cached;
            if (Materials.TryGetValue(key, out cached) && cached != null) return cached;
            Material result = new Material(VisualRebirth04AAssets.Material(style, role == "timber" ? "bark" : role == "linen" ? "cloth" : "crystal"));
            result.name = "Inhabited City 04C / " + role;
            Color tint = role == "timber" ? new Color(.72f,.52f,.32f) : role == "linen" ? new Color(.9f,.86f,.72f) : new Color(1f,.67f,.3f);
            if (result.HasProperty("_Color")) result.SetColor("_Color", tint);
            if (result.HasProperty("_BaseColor")) result.SetColor("_BaseColor", tint);
            if (role == "timber")
            {
                Texture2D albedo = Resources.Load<Texture2D>(ResourceRoot + "Textures/Timber_Albedo");
                if (albedo == null) { Release(result); throw new FileNotFoundException("04C timber texture is unavailable."); }
                if (result.HasProperty("_MainTex")) result.SetTexture("_MainTex", albedo);
                if (result.HasProperty("_BaseMap")) result.SetTexture("_BaseMap", albedo);
            }
            if (result.HasProperty("_EmissionColor")) result.SetColor("_EmissionColor", role == "lamp" ? new Color(.8f,.38f,.12f) : Color.black);
            if (role != "lamp") result.DisableKeyword("_EMISSION");
            if (result.HasProperty("_Metallic")) result.SetFloat("_Metallic", 0f);
            if (result.HasProperty("_Glossiness")) result.SetFloat("_Glossiness", role == "timber" ? .28f : .13f);
            if (result.HasProperty("_BumpScale")) result.SetFloat("_BumpScale", role == "timber" ? .25f : .35f);
            Materials.Add(key, result); return result;
        }

        public static Vector3 Vector(float[] value) { return new Vector3(value[0], value[1], value[2]); }
        public static void ResetStatics()
        {
            foreach (Mesh mesh in Meshes.Values) Release(mesh);
            foreach (Material material in Materials.Values) Release(material);
            Meshes.Clear(); Materials.Clear();
            VenueView04C.ResetStatics();
        }
        internal static void Release(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
