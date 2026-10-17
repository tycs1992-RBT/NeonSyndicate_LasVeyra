using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Shared immutable art sources. Readable CMH1 geometry avoids platform-dependent OBJ import failure.</summary>
    public static class VisualRebirth04AAssets
    {
        [Serializable] private sealed class SurfaceCatalog { public Surface[] surfaces; }
        [Serializable] private sealed class Surface
        {
            public int style;
            public string role, family;
            public float[] tint, emission;
            public float metallic, smoothness, normalStrength;
        }
        private sealed class Source
        {
            public Vector3[] Positions, Normals;
            public Vector2[] Uvs;
            public int[] Indices;
        }
        private const string Root = "VisualRebirth04A/";
        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Surface> Surfaces = new Dictionary<string, Surface>(StringComparer.Ordinal);
        private static bool _catalogLoaded;
        public static int SourceMeshCount { get { return Meshes.Count; } }
        public static int MaterialCount { get { return Materials.Count; } }

        public static Mesh Mesh(string model, bool low)
        {
            if (string.IsNullOrEmpty(model) || model.IndexOf('/') >= 0 || model.IndexOf('.') >= 0)
                throw new ArgumentException("Invalid 04A mesh identifier", "model");
            string key = model + (low ? "_low" : "_high");
            Mesh cached;
            if (Meshes.TryGetValue(key, out cached) && cached != null) return cached;
            TextAsset data = Resources.Load<TextAsset>(Root + "Meshes/" + key);
            if (data == null) throw new FileNotFoundException("04A source missing: " + key);
            Mesh result = CrystalCity02HMeshFallback.Decode(data.bytes, "04A / " + key);
            try
            {
                result.name = "Visual Rebirth 04A / " + key;
                // Sources remain readable solely for the spatial material batches. Final outputs release CPU data.
                result.RecalculateTangents();
                Meshes.Add(key, result);
                return result;
            }
            catch { Release(result); throw; }
        }

        /// <summary>Uses the active pipeline's native Lit shader. No new pipeline or renderer dependency.</summary>
        public static Material Material(int style, string role)
        {
            role = CanonicalRole(role);
            style = Mathf.Clamp(style, 0, 3);
            string key = style + "/" + role;
            Material cached;
            if (Materials.TryGetValue(key, out cached) && cached != null) return cached;
            EnsureCatalog();
            Surface surface;
            if (!Surfaces.TryGetValue(key, out surface)) throw new InvalidDataException("Unknown 04A surface: " + key);
            Shader shader = Mats.StandardShader;
            if (shader == null) throw new InvalidOperationException("04A requires the existing Lit shader.");
            Material material = new Material(shader) { name = "Visual Rebirth 04A / " + key, enableInstancing = true };
            try
            {
                Texture2D color = Texture(surface.family, "Albedo");
                Texture2D normal = Texture(surface.family, "Normal");
                Texture2D mask = Texture(surface.family, "Mask");
                SetTexture(material, "_MainTex", color); SetTexture(material, "_BaseMap", color);
                SetTexture(material, "_BumpMap", normal);
                SetTexture(material, "_MetallicGlossMap", mask); SetTexture(material, "_OcclusionMap", mask);
                SetColor(material, "_Color", ColorOf(surface.tint)); SetColor(material, "_BaseColor", ColorOf(surface.tint));
                SetFloat(material, "_Metallic", surface.metallic);
                SetFloat(material, "_Glossiness", surface.smoothness);
                SetFloat(material, "_Smoothness", 1f); // Mask alpha contains the authored smoothness on URP.
                SetFloat(material, "_GlossMapScale", 1f);
                SetFloat(material, "_BumpScale", surface.normalStrength);
                SetFloat(material, "_OcclusionStrength", .65f);
                SetFloat(material, "_SmoothnessTextureChannel", 0f);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword(RenderPath.IsURP ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                material.EnableKeyword("_OCCLUSIONMAP");
                Color emission = ColorOf(surface.emission);
                if (emission.maxColorComponent > .0001f)
                {
                    SetTexture(material, "_EmissionMap", Texture(surface.family, "Emission"));
                    SetColor(material, "_EmissionColor", emission);
                    material.EnableKeyword("_EMISSION");
                }
                // Phase 2: paving, stone, roofs and soil get the shared detail maps on High and up (one metre per repeat).
                if (Mats.DetailEnabled && (role == "paving" || role == "soil")) Mats.ApplyDetail(material, SurfaceKind.Grain, 6f, 0.4f);
                else if (Mats.DetailEnabled && (role == "stone" || role == "roof")) Mats.ApplyDetail(material, SurfaceKind.Pores, 6f, 0.4f);
                material.renderQueue = 2000;
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                Materials.Add(key, material);
                return material;
            }
            catch { Release(material); throw; }
        }

        private static string CanonicalRole(string role)
        {
            if (role == "dark") return "roof";
            if (role == "foliage") return "leaf";
            if (role == "bloom") return "flower";
            if (role == "bronze") return "metal";
            return role;
        }

        private static void EnsureCatalog()
        {
            if (_catalogLoaded) return;
            TextAsset source = Resources.Load<TextAsset>(Root + "material_manifest");
            if (source == null) throw new FileNotFoundException("04A material manifest is missing.");
            SurfaceCatalog catalog = JsonUtility.FromJson<SurfaceCatalog>(source.text);
            if (catalog == null || catalog.surfaces == null) throw new InvalidDataException("04A material manifest is invalid.");
            Dictionary<string, Surface> pending = new Dictionary<string, Surface>(StringComparer.Ordinal);
            foreach (Surface surface in catalog.surfaces)
            {
                if (surface == null || string.IsNullOrEmpty(surface.role) || string.IsNullOrEmpty(surface.family) ||
                    surface.tint == null || surface.tint.Length != 3 || surface.emission == null || surface.emission.Length != 3)
                    throw new InvalidDataException("04A surface data is incomplete.");
                pending.Add(surface.style + "/" + surface.role, surface);
            }
            foreach (KeyValuePair<string, Surface> pair in pending) Surfaces.Add(pair.Key, pair.Value);
            _catalogLoaded = true;
        }

        private static Texture2D Texture(string family, string kind)
        {
            string key = family + "_" + kind;
            Texture2D texture;
            if (Textures.TryGetValue(key, out texture) && texture != null) return texture;
            texture = Resources.Load<Texture2D>(Root + "Textures/" + key);
            if (texture == null) throw new FileNotFoundException("04A surface map missing: " + key);
            Textures.Add(key, texture);
            return texture;
        }

        /// <summary>Append transformed vertices to the existing city chunk, retaining each authored vertex normal.</summary>
        public static void Append(MeshBuilder target, string model, Vector3 at, Quaternion rotation, Vector3 scale, bool low = true)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f) throw new ArgumentOutOfRangeException("scale");
            string key = model + (low ? "_low" : "_high");
            Source source;
            if (!Sources.TryGetValue(key, out source))
            {
                Mesh mesh = Mesh(model, low);
                source = new Source { Positions = mesh.vertices, Normals = mesh.normals, Uvs = mesh.uv, Indices = mesh.triangles };
                Sources.Add(key, source);
            }
            Matrix4x4 matrix = Matrix4x4.TRS(at, rotation, scale);
            Matrix4x4 normals = matrix.inverse.transpose;
            for (int i = 0; i < source.Indices.Length; i += 3)
            {
                int a = source.Indices[i], b = source.Indices[i + 1], c = source.Indices[i + 2];
                int start = target.VertexCount;
                target.Triangle(0, matrix.MultiplyPoint3x4(source.Positions[a]), matrix.MultiplyPoint3x4(source.Positions[b]),
                    matrix.MultiplyPoint3x4(source.Positions[c]), normals.MultiplyVector(source.Normals[a]).normalized,
                    source.Uvs[a], source.Uvs[b], source.Uvs[c]);
                target.Normals[start + 1] = normals.MultiplyVector(source.Normals[b]).normalized;
                target.Normals[start + 2] = normals.MultiplyVector(source.Normals[c]).normalized;
            }
        }

        private static Color ColorOf(float[] data) { return new Color(data[0], data[1], data[2], 1f); }
        private static void SetColor(Material m, string key, Color value) { if (m.HasProperty(key)) m.SetColor(key, value); }
        private static void SetFloat(Material m, string key, float value) { if (m.HasProperty(key)) m.SetFloat(key, value); }
        private static void SetTexture(Material m, string key, Texture value) { if (m.HasProperty(key)) m.SetTexture(key, value); }
        public static void ResetStatics()
        {
            foreach (Mesh mesh in Meshes.Values) Release(mesh);
            foreach (Material material in Materials.Values) Release(material);
            Meshes.Clear(); Sources.Clear(); Materials.Clear(); Textures.Clear(); Surfaces.Clear(); _catalogLoaded = false;
        }
        private static void Release(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj); else UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
