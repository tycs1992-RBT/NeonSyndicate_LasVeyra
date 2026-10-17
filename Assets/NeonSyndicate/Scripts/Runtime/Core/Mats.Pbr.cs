using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The PBR side of the material library (Phase 2 of the visual upgrade). A family asks for a surface kind and gets
    /// albedo, normal, metallic, smoothness and occlusion maps generated from one height field (Tex.Surface), with
    /// detail maps on large flat surfaces from High up. Low keeps the old materials untouched: every family checks
    /// PbrEnabled first. The same property names drive URP Lit through MatsUrp (UrpApplyPbrKeywords).
    /// </summary>
    public static partial class Mats
    {
        /// <summary>0 Low (old materials, no new maps), 1 Medium (256 normal and mask maps), 2 High (512 plus detail maps), 3 Ultra (1024 on the hero kinds).</summary>
        public static int PbrLevelFor(GraphicsPreset preset)
        {
            return (int)preset;
        }

        public static int PbrLevel { get { return PbrLevelFor(Settings.Graphics); } }
        public static bool PbrEnabled { get { return PbrLevel >= 1; } }
        public static bool DetailEnabled { get { return PbrLevel >= 2; } }

        /// <summary>The Built-in foliage shader (Resources/NeonFoliage.shader), or null where it is unsupported or on URP.</summary>
        private static Shader _foliage;
        private static bool _foliageTried;

        private static void ResetPbrStatics()
        {
            _foliage = null;
            _foliageTried = false;
            _wet.Clear();
        }

        // ------------------------------------------------------------------ surface materials

        /// <summary>
        /// A Standard (or URP Lit) material with the full map set of a surface kind: albedo in the palette, normal map,
        /// metallic, smoothness and occlusion mask; optional detail maps (High and up) from a micro kind tiled
        /// detailTiling times per main texture repeat. wet marks a surface the rain sheen drives (its smoothness map is
        /// capped so the sheen can scale it to 1). Falls back to a plain Standard material if the surface failed.
        /// </summary>
        private static Material Pbr(string name, SurfaceKind kind, int palette, Color tint, float smoothness, float metallic, bool wet, float bumpScale,
            SurfaceKind detail = SurfaceKind.Grain, float detailTiling = 0f, float detailBump = 0.6f)
        {
            Material m = Standard(name, tint, smoothness, metallic);
            Tex.SurfaceSet set = Tex.Surface(kind);
            if (set == null || set.Error != null) return m;
            m.mainTexture = Tex.SurfaceAlbedo(set, palette);
            ApplyRelief(m, Tex.SurfaceNormal(set), bumpScale, Tex.SurfaceMask(set, metallic, smoothness, wet), 1f);
            if (detailTiling > 0f && DetailEnabled) ApplyDetail(m, detail, detailTiling, detailBump);
            return m;
        }

        /// <summary>A flat-colour family upgraded with a micro surface (brushed metal, cast grain, pores): the kind's near-white albedo tinted by the colour.</summary>
        private static Material Micro(string name, Color color, float smoothness, float metallic, SurfaceKind micro, float bumpScale)
        {
            return Pbr(name, micro, 0, color, smoothness, metallic, false, bumpScale);
        }

        /// <summary>Sets a normal map and a mask (R metallic, G occlusion, A smoothness) on a Standard or URP Lit material.</summary>
        public static void ApplyRelief(Material m, Texture2D normal, float bumpScale, Texture2D mask, float occlusionStrength)
        {
            if (m == null) return;
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                if (m.HasProperty("_BumpScale")) m.SetFloat("_BumpScale", bumpScale);
                m.EnableKeyword("_NORMALMAP");
            }
            if (mask == null) return;
            if (m.HasProperty("_MetallicGlossMap"))
            {
                m.SetTexture("_MetallicGlossMap", mask);
                // Smoothness now comes from the map's alpha: the scale is 1 (the rain sheen raises it, see SetWetness).
                if (m.HasProperty("_GlossMapScale")) m.SetFloat("_GlossMapScale", 1f);
                if (m.HasProperty("_SmoothnessTextureChannel")) m.SetFloat("_SmoothnessTextureChannel", 0f);
                if (IsUrpSurface(m)) UrpApplyMaskKeywords(m);
                else m.EnableKeyword("_METALLICGLOSSMAP");
            }
            if (m.HasProperty("_OcclusionMap"))
            {
                m.SetTexture("_OcclusionMap", mask);
                if (m.HasProperty("_OcclusionStrength")) m.SetFloat("_OcclusionStrength", occlusionStrength);
                if (IsUrpSurface(m)) m.EnableKeyword("_OCCLUSIONMAP");
            }
        }

        /// <summary>
        /// Detail albedo and normal from a micro kind, tiled detailTiling times per main texture repeat (Standard's
        /// multiply-by-two detail mode, same on URP Lit), so light raking across a large flat surface shows texture.
        /// </summary>
        public static void ApplyDetail(Material m, SurfaceKind micro, float tiling, float normalScale)
        {
            if (m == null || !m.HasProperty("_DetailNormalMap") || !m.HasProperty("_DetailAlbedoMap")) return;
            Texture2D dn = Tex.DetailNormal(micro);
            Texture2D da = Tex.DetailAlbedo(micro);
            if (dn == null || da == null) return;
            m.SetTexture("_DetailNormalMap", dn);
            if (m.HasProperty("_DetailNormalMapScale")) m.SetFloat("_DetailNormalMapScale", normalScale);
            m.SetTexture("_DetailAlbedoMap", da);
            m.SetTextureScale("_DetailAlbedoMap", new Vector2(tiling, tiling));
            if (m.HasProperty("_DetailAlbedoMapScale")) m.SetFloat("_DetailAlbedoMapScale", 1f);
            if (m.HasProperty("_UVSec")) m.SetFloat("_UVSec", 0f);
            m.EnableKeyword("_DETAIL_MULX2");
        }

        // ------------------------------------------------------------------ foliage

        private static Shader FoliageShader
        {
            get
            {
                if (_foliage == null && !_foliageTried)
                {
                    _foliageTried = true;
                    if (!RenderPath.IsURP)
                    {
                        try
                        {
                            _foliage = Resources.Load<Shader>("NeonFoliage");
                            if (_foliage == null) _foliage = Shader.Find("NeonSyndicate/Foliage");
                        }
                        catch (System.Exception) { _foliage = null; }
                        if (_foliage != null && !_foliage.isSupported) _foliage = null;
                    }
                }
                return _foliage;
            }
        }

        /// <summary>
        /// Leaves, fronds, needles and weeds: on the Built-in pipeline (Medium and up) the NeonSyndicate/Foliage shader,
        /// which adds wrap lighting and a back-light translucency tint to the Standard lighting and alpha-tests the
        /// weed cards; otherwise the Standard material it replaces (URP keeps Lit, see Docs/VisualUpgrade). The leaf
        /// micro normal gives veins and cell relief.
        /// </summary>
        private static Material Foliage(string name, Texture2D albedo, Color tint, float smoothness, Color translucency, bool cutout, System.Func<Material> fallback)
        {
            Shader sh = PbrEnabled ? FoliageShader : null;
            if (sh == null) return fallback();
            Material m = new Material(sh);
            m.name = name;
            m.color = tint;
            if (albedo != null) m.mainTexture = albedo;
            m.SetFloat("_Glossiness", smoothness);
            m.SetColor("_Translucency", translucency);
            m.SetFloat("_Cutoff", cutout ? 0.5f : 0.01f);
            Tex.SurfaceSet leaf = Tex.Surface(SurfaceKind.Leaf);
            if (leaf != null && leaf.Error == null)
            {
                m.SetTexture("_BumpMap", Tex.SurfaceNormal(leaf));
                m.SetFloat("_BumpScale", cutout ? 0.25f : 0.6f);
            }
            m.renderQueue = cutout ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest : (int)UnityEngine.Rendering.RenderQueue.Geometry;
            m.enableInstancing = true;
            return m;
        }

        // ------------------------------------------------------------------ rain sheen

        private sealed class WetEntry
        {
            public Material M;
            public string Prop;
            public float Dry;
            public float Wet;
        }

        private static readonly List<WetEntry> _wet = new List<WetEntry>();

        /// <summary>
        /// Registers a road or pavement for the rain sheen. A plain material lerps its smoothness to WetTarget; a
        /// material with a mask scales the map's smoothness (its alpha was capped at nominal over WetTarget, so the
        /// scale WetTarget over nominal lands exactly on 1 for the glossiest texel).
        /// </summary>
        private static void RegisterWet(Material m, float nominalSmoothness)
        {
            if (m == null) return;
            bool mapped = m.IsKeywordEnabled("_METALLICGLOSSMAP") || m.IsKeywordEnabled("_METALLICSPECGLOSSMAP");
            string prop = mapped ? (IsUrpSurface(m) ? UrpSmoothness : "_GlossMapScale") : WetSmoothnessProperty;
            if (!m.HasProperty(prop)) return;
            WetEntry e = new WetEntry { M = m, Prop = prop, Dry = m.GetFloat(prop) };
            e.Wet = mapped ? Mathf.Max(1f, SurfaceColor.WetTarget / Mathf.Max(0.02f, nominalSmoothness)) : SurfaceColor.WetTarget;
            _wet.Add(e);
        }

        /// <summary>0 = dry, 1 = soaked. Raises smoothness of roads and pavements for a rain sheen.</summary>
        public static void SetWetness(float wet)
        {
            for (int i = 0; i < _wet.Count; i++)
            {
                WetEntry e = _wet[i];
                if (e.M == null) continue;
                e.M.SetFloat(e.Prop, Mathf.Lerp(e.Dry, e.Wet, wet));
            }
        }
    }
}
