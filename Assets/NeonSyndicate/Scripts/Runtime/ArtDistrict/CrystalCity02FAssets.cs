using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Read-only imported source assets, owned material clones, and one validated catalog per world.</summary>
    public static class CrystalCity02FAssets
    {
        public const string ResourceRoot = "CrystalCity02F/";
        private static readonly Dictionary<string, MeshFilter[]> Models = new Dictionary<string, MeshFilter[]>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>(StringComparer.Ordinal);
        private static CrystalCity02FCatalog _catalog;
        public static int FallbackCount { get; private set; }


        public static CrystalCity02FCatalog Catalog()
        {
            if (_catalog != null) return _catalog;
            TextAsset data = Resources.Load<TextAsset>(ResourceRoot + "city02f_blueprints");
            if (data == null) throw new InvalidOperationException("02F blueprint asset is missing. Original city remains intact.");
            CrystalCity02FCatalog result = JsonUtility.FromJson<CrystalCity02FCatalog>(data.text);
            if (result == null) throw new InvalidOperationException("02F blueprint JSON could not be parsed.");
            result.Validate();
            _catalog = result;
            return result;
        }

        public static MeshFilter[] Model(string key, bool low)
        {
            string name = key + (low ? "_lod" : "");
            MeshFilter[] result;
            if (Models.TryGetValue(name, out result)) return result;
            // Unity 6 can import a valid OBJ as an empty or non-readable MeshFilter
            // despite an isReadable flag in the associated .meta file. Preserve the
            // original model whenever its imported geometry can be batched safely.
            // Otherwise decode the byte-for-byte geometry data shipped in 02H.
            GameObject asset = Resources.Load<GameObject>(ResourceRoot + "Models/" + name);
            result = asset == null ? new MeshFilter[0] : asset.GetComponentsInChildren<MeshFilter>(true);
            bool valid = result.Length > 0;
            foreach (MeshFilter filter in result)
            {
                if (filter == null || filter.sharedMesh == null ||
                    !filter.sharedMesh.isReadable || filter.sharedMesh.vertexCount == 0)
                {
                    valid = false;
                    break;
                }
            }
            if (!valid)
            {
                result = CrystalCity02HMeshFallback.Load(name);
                FallbackCount++;
            }
            Models.Add(name, result);
            return result;
        }

        public static Material Surface(CrystalCityStyle style, string role)
        {
            if (!CrystalCity02FCatalog.MaterialRole(role)) throw new ArgumentException("Unknown 02F material role: " + role);
            if (role == "crystal") return CrystalCity02DMaterials.Get(style, "crystal");
            if (role == "bark") return CrystalCity02DMaterials.Get(style, "bark");
            string key = (int)style + "/" + role;
            Material cached;
            if (Materials.TryGetValue(key, out cached) && cached != null) return cached;
            string texture = role == "gilt" ? "gilt" : ((role == "leaf" || role == "cloth") ? "leaf" :
                (style == CrystalCityStyle.VerdantReach ? "verdant" : "moonstone"));
            Texture2D albedo = LoadTexture(texture, "Albedo"), normal = LoadTexture(texture, "Normal"), mask = LoadTexture(texture, "Mask");
            Color tint = Color.white;
            if (role == "stone" && style == CrystalCityStyle.AmethystWard) tint = new Color(.74f,.63f,.95f);
            if (role == "stone" && style == CrystalCityStyle.DawnstoneCourt) tint = new Color(1.22f,1.10f,.84f);
            if (role == "leaf" && style == CrystalCityStyle.AmethystWard) tint = new Color(.82f,.70f,1.15f);
            if (role == "cloth") tint = style == CrystalCityStyle.DawnstoneCourt ? new Color(1.35f,.90f,.42f) : new Color(.78f,.62f,1.10f);
            Material result = Mats.Textured("Crystal City 02F / " + key, albedo, tint, .42f, role == "gilt" ? .88f : 0f, normal, .6f);
            if (result == null) throw new InvalidOperationException("02F material creation failed.");
            result.enableInstancing = true;
            if (result.HasProperty("_MetallicGlossMap"))
            {
                result.SetTexture("_MetallicGlossMap", mask);
                result.EnableKeyword(RenderPath.IsURP ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            }
            if (result.HasProperty("_GlossMapScale")) result.SetFloat("_GlossMapScale", 1f);
            if (result.HasProperty("_Smoothness")) result.SetFloat("_Smoothness", 1f);
            if (result.HasProperty("_OcclusionMap"))
            {
                result.SetTexture("_OcclusionMap", mask);
                if (result.HasProperty("_OcclusionStrength")) result.SetFloat("_OcclusionStrength", .65f);
                if (RenderPath.IsURP) result.EnableKeyword("_OCCLUSIONMAP");
            }
            // Phase 2: stone walls and floors get the shared plaster-pore detail maps on High and up.
            if (role == "stone" && Mats.DetailEnabled) Mats.ApplyDetail(result, SurfaceKind.Pores, 6f, 0.4f);
            if (role == "gilt" || role == "leaf") Mats.SetEmission(result, CrystalCity02DMaterials.Glow(style) * (role == "gilt" ? .13f : .10f), LoadTexture(texture, "Emission"));
            result.renderQueue = 2000;
            result.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            Materials.Add(key, result);
            return result;
        }
        private static Texture2D LoadTexture(string id, string suffix)
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + "Textures/" + id + "_" + suffix);
            if (texture == null) throw new InvalidOperationException("02F surface map missing: " + id + "_" + suffix);
            return texture;
        }

        /// <summary>Imported models/textures belong to Unity. Destroy only the material objects created here.</summary>
        public static void ResetStatics()
        {
            foreach (Material material in Materials.Values)
            {
                if (material == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(material);
                else UnityEngine.Object.DestroyImmediate(material);
            }
            CrystalCity02HMeshFallback.ResetStatics();
            Models.Clear(); Materials.Clear(); _catalog = null;
            FallbackCount = 0;
        }
    }
}
