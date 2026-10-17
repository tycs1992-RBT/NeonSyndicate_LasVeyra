using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Shared, instancing-enabled materials for the demonstration district. Texture data is authored offline and
    /// loaded once from Resources. Assign with Renderer.sharedMaterial, never Renderer.material.
    /// </summary>
    public static class DistrictMaterials
    {
        private const string ResourceRoot = "ArtDistrict/Textures/";
        private static readonly Dictionary<string, Material> Cache = new Dictionary<string, Material>(StringComparer.Ordinal);

        /// <summary>
        /// Stable material keys: sandstone, basalt, promenade, bronze, ceramic, ceramicViolet, glass, crystal, crystalWarm,
        /// crystalViolet, foliage, bark, fabric, dragonHide, dragonWing and particle. Glass is opaque glazing.
        /// </summary>
        public static Material Get(string key)
        {
            if (string.IsNullOrEmpty(key)) key = "sandstone";
            if (Cache.TryGetValue(key, out Material cached) && cached != null) return cached;

            Material material;
            if (key == "crystal" || key == "crystalWarm" || key == "crystalViolet") material = CreateCrystal(key);
            else if (key == "particle") material = CreateParticle();
            else material = CreateSurface(key);
            material.name = "CM District / " + key;
            material.enableInstancing = true;
            Cache[key] = material;
            return material;
        }

        /// <summary>Called by Game.ResetStatics for clean repeat entry when domain reload is disabled.</summary>
        public static void ResetStatics()
        {
            foreach (KeyValuePair<string, Material> pair in Cache)
            {
                if (pair.Value == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(pair.Value);
                else UnityEngine.Object.DestroyImmediate(pair.Value);
            }
            Cache.Clear();
        }

        private static Material CreateSurface(string key)
        {
            string textureKey = key;
            Color fallback = Color.white;
            Color tint = Color.white;
            float normalStrength = 0.75f;
            float smoothnessScale = 1f;
            switch (key)
            {
                case "sandstone": fallback = new Color(0.70f, 0.66f, 0.57f); normalStrength = 0.65f; break;
                case "basalt": fallback = new Color(0.075f, 0.105f, 0.15f); normalStrength = 0.5f; break;
                case "promenade":
                    textureKey = "basalt";
                    tint = new Color(0.88f, 0.92f, 1f);
                    fallback = new Color(0.066f, 0.097f, 0.15f);
                    normalStrength = 0.30f;
                    smoothnessScale = 0.94f;
                    break;
                case "bronze": fallback = new Color(0.47f, 0.36f, 0.23f); normalStrength = 0.4f; break;
                case "ceramic": fallback = new Color(0.09f, 0.24f, 0.28f); normalStrength = 0.55f; break;
                case "ceramicViolet":
                    textureKey = "ceramic";
                    tint = new Color(1.7f, 0.6f, 1.22f);
                    fallback = new Color(0.153f, 0.144f, 0.342f);
                    normalStrength = 0.55f;
                    break;
                case "glass": fallback = new Color(0.055f, 0.105f, 0.17f); normalStrength = 0.12f; break;
                case "foliage": fallback = new Color(0.10f, 0.255f, 0.19f); normalStrength = 0.5f; break;
                case "bark": fallback = new Color(0.15f, 0.18f, 0.175f); break;
                case "fabric": fallback = new Color(0.29f, 0.19f, 0.35f); normalStrength = 0.35f; break;
                case "dragonHide": fallback = new Color(0.62f, 0.69f, 0.73f); normalStrength = 0.8f; break;
                case "dragonWing": fallback = new Color(0.32f, 0.28f, 0.40f); normalStrength = 0.5f; break;
                default: textureKey = "sandstone"; fallback = new Color(0.70f, 0.66f, 0.57f); break;
            }

            Texture2D albedo = Resources.Load<Texture2D>(ResourceRoot + textureKey + "_Albedo");
            Texture2D normal = Resources.Load<Texture2D>(ResourceRoot + textureKey + "_Normal");
            Texture2D mask = Resources.Load<Texture2D>(ResourceRoot + textureKey + "_Mask");
            Material material = Mats.Textured("District Surface", albedo, albedo != null ? tint : fallback,
                mask != null ? 1f : (key == "promenade" ? 0.68f : 0.35f), key == "bronze" ? 0.88f : 0f, normal, normalStrength);

            if (mask != null && material.HasProperty("_MetallicGlossMap"))
            {
                material.SetTexture("_MetallicGlossMap", mask);
                material.EnableKeyword(RenderPath.IsURP ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                if (material.HasProperty("_GlossMapScale")) material.SetFloat("_GlossMapScale", smoothnessScale);
                if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothnessScale);
                if (material.HasProperty("_SmoothnessTextureChannel")) material.SetFloat("_SmoothnessTextureChannel", 0f);
            }
            if (mask != null && material.HasProperty("_OcclusionMap"))
            {
                material.SetTexture("_OcclusionMap", mask);
                material.SetFloat("_OcclusionStrength", 0.8f);
                if (RenderPath.IsURP) material.EnableKeyword("_OCCLUSIONMAP");
            }
            // Phase 2: the large stone surfaces get the shared plaster-pore detail maps on High and up (one metre per repeat).
            if (Mats.DetailEnabled && (key == "sandstone" || key == "basalt" || key == "promenade")) Mats.ApplyDetail(material, SurfaceKind.Pores, 6f, 0.4f);
            // All surface materials use opaque rendering and depth writes, including dark shop glazing.
            material.renderQueue = 2000;
            return material;
        }

        private static Material CreateCrystal(string key)
        {
            Color body = new Color(0.105f, 0.25f, 0.34f);
            Color depth = new Color(0.12f, 0.47f, 0.56f);
            Color glow = new Color(0.21f, 0.83f, 1f);
            if (key == "crystalWarm")
            {
                body = new Color(0.27f, 0.18f, 0.09f);
                depth = new Color(0.48f, 0.31f, 0.14f);
                glow = new Color(1f, 0.70f, 0.30f);
            }
            else if (key == "crystalViolet")
            {
                body = new Color(0.14f, 0.075f, 0.24f);
                depth = new Color(0.35f, 0.14f, 0.48f);
                glow = new Color(0.66f, 0.38f, 1f);
            }
            Texture2D veins = Resources.Load<Texture2D>(ResourceRoot + "crystal_Emission");
            Shader shader = Resources.Load<Shader>("ArtDistrict/Shaders/DistrictCrystal");
            if (shader == null || !shader.isSupported)
            {
                Material fallback = Mats.Standard("District Crystal Fallback", body, 0.86f, 0.08f);
                Mats.SetEmission(fallback, glow * 0.42f, veins);
                fallback.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                return fallback;
            }
            Material material = new Material(shader);
            material.SetColor("_Color", body);
            material.SetColor("_DepthColor", depth);
            material.SetColor("_RimColor", glow);
            material.SetTexture("_MainTex", veins);
            material.SetFloat("_EmissionStrength", 1.10f);
            material.SetFloat("_InternalStrength", 0.45f);
            material.SetFloat("_RimPower", 4.2f);
            material.SetFloat("_PulseAmplitude", 0.045f);
            material.SetFloat("_PulseSpeed", 0.55f);
            material.SetFloat("_Glossiness", 0.88f);
            material.SetFloat("_Metallic", 0.08f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return material;
        }

        private static Material CreateParticle()
        {
            Shader shader = Resources.Load<Shader>("ArtDistrict/Shaders/DistrictMote");
            if (shader == null || !shader.isSupported)
                return new Material(Mats.Glow(new Color(0.12f, 0.75f, 1f, 0.3f)));
            Material material = new Material(shader);
            material.SetTexture("_MainTex", Resources.Load<Texture2D>(ResourceRoot + "mote"));
            material.SetColor("_TintColor", new Color(0.75f, 0.9f, 1f, 0.6f));
            return material;
        }
    }
}
