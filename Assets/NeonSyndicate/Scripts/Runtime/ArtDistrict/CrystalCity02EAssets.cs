using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Shared muted wall palettes and cached source-mesh arrays for the city-wide low-cost art path.</summary>
    public static class CrystalCity02EAssets
    {
        private sealed class Source
        {
            public Vector3[] Positions;
            public Vector3[] Normals;
            public Vector2[] Uvs;
            public int[] Indices;
        }

        private static readonly Dictionary<string, Material> Surfaces = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Source> Sources = new Dictionary<string, Source>(StringComparer.Ordinal);

        public static Material Surface(Vector3 at, string role)
        {
            CrystalCityStyle style = CrystalCity02DMaterials.StyleAt(at);
            if (role == "trim") return CrystalCity02DMaterials.Get(style, "bronze");
            if (role == "crystal") return CrystalCity02DMaterials.Get(style, "crystal");
            if (role == "leaf") return CrystalCity02DMaterials.Get(style, "foliage");
            if (role == "bark") return CrystalCity02DMaterials.Get(style, "bark");
            string key = ((int)style).ToString() + "/" + role;
            Material cached;
            if (Surfaces.TryGetValue(key, out cached) && cached != null) return cached;
            string originalKey = role == "pane" ? "glass" : (role == "roof" ? "ceramic" : "sandstone");
            Material source = DistrictMaterials.Get(originalKey);
            if (source == null) throw new InvalidOperationException("Crystal City 02E source material missing: " + originalKey);
            Material m = new Material(source) { name = "Crystal City 02E / " + style + " / " + role, enableInstancing = true };
            Color tint;
            if (role == "pane")
            {
                tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(.34f,.24f,.11f) :
                    (style == CrystalCityStyle.VerdantReach ? new Color(.035f,.20f,.14f) : new Color(.12f,.045f,.22f));
                // Quiet glass avoids turning every square metre of the city into a bright emissive mural.
                m.mainTexture = null;
                Mats.SetEmission(m, CrystalCity02DMaterials.Glow(style) * .055f, null);
            }
            else if (role == "roof")
                tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(1.4f,1.2f,.82f) :
                    (style == CrystalCityStyle.VerdantReach ? new Color(.55f,1.15f,.79f) : new Color(.92f,.51f,1.05f));
            else if (role == "paving") tint = new Color(.52f,.53f,.55f);
            else
                tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(.96f,.87f,.67f) :
                    (style == CrystalCityStyle.VerdantReach ? new Color(.29f,.47f,.36f) : new Color(.35f,.27f,.44f));
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", role == "pane" ? .58f : .28f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", role == "pane" ? .58f : .28f);
            m.renderQueue = 2000;
            Surfaces[key] = m;
            return m;
        }

        /// <summary>Append shared low-detail geometry to an existing spatial/material chunk. No per-part renderer or collider.</summary>
        public static void Append(MeshBuilder target, string model, Vector3 at, Quaternion rotation, Vector3 scale)
        {
            if (target == null || scale.x <= 0f || scale.y <= 0f || scale.z <= 0f) return;
            Source source;
            if (!Sources.TryGetValue(model, out source))
            {
                Mesh mesh = CrystalCity02DGeometry.Get(model, true);
                if (mesh == null || !mesh.isReadable) throw new InvalidOperationException("02E source mesh not readable: " + model);
                source = new Source { Positions = mesh.vertices, Normals = mesh.normals, Uvs = mesh.uv, Indices = mesh.triangles };
                Sources.Add(model, source);
            }
            Matrix4x4 transform = Matrix4x4.TRS(at, rotation, scale);
            Matrix4x4 normalMatrix = transform.inverse.transpose;
            for (int i = 0; i < source.Indices.Length; i += 3)
            {
                int a = source.Indices[i], b = source.Indices[i + 1], c = source.Indices[i + 2];
                Vector3 n = normalMatrix.MultiplyVector(source.Normals[a]).normalized;
                target.Triangle(0, transform.MultiplyPoint3x4(source.Positions[a]),
                    transform.MultiplyPoint3x4(source.Positions[b]), transform.MultiplyPoint3x4(source.Positions[c]),
                    n, source.Uvs[a], source.Uvs[b], source.Uvs[c]);
            }
        }

        /// <summary>Called by DistrictArchitecture.ResetStatics, before its shared geometry cache is cleared.</summary>
        public static void ResetStatics()
        {
            foreach (Material material in Surfaces.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            Surfaces.Clear(); Sources.Clear();
        }
    }
}
