#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace NeonSyndicate.Editor
{
    /// <summary>
    /// LiveCastSetup, drop-in materials. One generated material per FBX material name, on the project's own lit shader
    /// (Mats.Standard: Built-in Standard, or URP Lit on the URP path). Textures come from the folder by name: the material
    /// name plus _Albedo (or _BaseColor, _Diffuse, _Color), _Normal, _Emission and _Metallic, or the files livecast.json
    /// names. The look starts from LiveCastFolderRules.DefaultSurface and livecast.json can override mode, smoothness,
    /// metalness, normal strength and colours. Emission is always enabled (black when none is authored) so the original
    /// hit flash shows on the imported character.
    /// </summary>
    public static partial class LiveCastSetup
    {
        private const float Cutoff = 0.3f;

        private static Material DropInMaterial(DropInSource s, string imported, Dictionary<string, Material> made)
        {
            string key = LiveCastFolderRules.MaterialKey(imported);
            if (key.Length == 0) key = "material";
            Material existing;
            if (made.TryGetValue(key, out existing)) return existing;
            DropInSurface hint = HintFor(s, key);
            LiveSurfaceHint look = LiveCastFolderRules.DefaultSurface(key);
            LiveSurfaceMode mode;
            if (hint != null && LiveCastFolderRules.ParseMode(hint.mode, out mode)) look.Mode = mode;
            if (hint != null && hint.smoothness >= 0f) look.Smoothness = Mathf.Clamp01(hint.smoothness);
            if (hint != null && hint.metalness >= 0f) look.Metallic = Mathf.Clamp01(hint.metalness);
            if (hint != null && hint.bump >= 0f) look.Bump = Mathf.Clamp(hint.bump, 0f, 2f);
            Texture2D albedo = Map(s, key, LiveTextureRole.Albedo, hint != null ? hint.albedo : "");
            Texture2D normal = Map(s, key, LiveTextureRole.Normal, hint != null ? hint.normal : "");
            Texture2D glow = Map(s, key, LiveTextureRole.Emission, hint != null ? hint.emission : "");
            Texture2D metal = Map(s, key, LiveTextureRole.Metallic, hint != null ? hint.metallic : "");
            // A cutout without alpha would draw nothing or everything: it falls back to an opaque surface.
            if (look.Mode == LiveSurfaceMode.Cutout && (albedo == null || !HasAlpha(albedo))) look.Mode = LiveSurfaceMode.Opaque;
            Color color = albedo != null ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            if (look.Mode == LiveSurfaceMode.Transparent && albedo == null) color = new Color(0.8f, 0.85f, 0.9f, 0.05f);
            Color parsed;
            if (hint != null && !string.IsNullOrWhiteSpace(hint.color) && ColorUtility.TryParseHtmlString(hint.color.Trim(), out parsed)) color = parsed;
            Material m = Mats.Standard(s.Name + " " + (imported.Length > 0 ? imported : key), color, look.Smoothness, look.Metallic);
            if (albedo != null) m.mainTexture = albedo;
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", look.Bump);
                m.EnableKeyword("_NORMALMAP");
            }
            if (metal != null && m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", metal);
                if (RenderPath.IsURP)
                {
                    m.EnableKeyword("_METALLICSPECGLOSSMAP");
                    if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 1f);
                }
                else
                {
                    m.EnableKeyword("_METALLICGLOSSMAP");
                    if (m.HasProperty("_GlossMapScale")) m.SetFloat("_GlossMapScale", 1f);
                }
            }
            Color emission = glow != null ? Color.white : Color.black;
            if (hint != null && !string.IsNullOrWhiteSpace(hint.emissionColor) && ColorUtility.TryParseHtmlString(hint.emissionColor.Trim(), out parsed)) emission = parsed;
            Mats.SetEmission(m, emission, glow);
            if (look.Mode == LiveSurfaceMode.Cutout) MakeCutout(m);
            else if (look.Mode == LiveSurfaceMode.Transparent) Mats.MakeTransparent(m, false);
            AssetDatabase.CreateAsset(m, s.Prepared + "/" + s.Safe + "_" + key + ".mat");
            made.Add(key, m);
            return m;
        }

        /// <summary>The livecast.json entry for a material (matched by the same key rule), or null.</summary>
        private static DropInSurface HintFor(DropInSource s, string key)
        {
            foreach (DropInSurface hint in s.Config.materials)
                if (hint != null && LiveCastFolderRules.MaterialKey(hint.material) == key) return hint;
            return null;
        }

        /// <summary>The explicit file from livecast.json, else the folder texture named after the material and role, or null.</summary>
        private static Texture2D Map(DropInSource s, string key, LiveTextureRole role, string fileName)
        {
            string asset = ImageNamed(s, fileName);
            if (asset == null) s.ByKeyRole.TryGetValue(key + "|" + (int)role, out asset);
            if (asset == null) return null;
            Texture2D texture = HumanoidPrepH9.LoadTexture(asset);
            if (texture == null) throw new InvalidOperationException("Unity has not imported " + asset + " as a texture.");
            return texture;
        }

        private static bool HasAlpha(Texture2D texture)
        {
            TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
            return importer != null && importer.DoesSourceTextureHaveAlpha();
        }

        /// <summary>Alpha-tested surface on either pipeline (URP through the project's own helper).</summary>
        private static void MakeCutout(Material m)
        {
            if (RenderPath.IsURP)
            {
                Mats.MakeUrpCutout(m, Cutoff);
                return;
            }
            m.SetFloat("_Mode", 1f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", Cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }
    }
}
#endif
