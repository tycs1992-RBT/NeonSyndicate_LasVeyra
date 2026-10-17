using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The Universal Render Pipeline side of the material library. When RenderPath.IsURP is true the shader getters
    /// and helpers in Mats.cs hand out URP shaders and set them up the way URP's own material inspector would
    /// (URP 17.3 BaseShaderGUI.SetupMaterialBlendMode and SetMaterialKeywords), so every procedural material looks
    /// the same as its Built-in Standard counterpart. Only engine API and shader property names are used here, so
    /// this file compiles with or without the URP package; on the Built-in pipeline none of it runs.
    /// Property mapping (Standard to URP Lit): _Color to _BaseColor and _MainTex to _BaseMap (through Material.color and
    /// mainTexture), _Glossiness to _Smoothness, _Metallic, _BumpMap, _BumpScale, _EmissionColor and _EmissionMap keep
    /// their names, _METALLICGLOSSMAP becomes _METALLICSPECGLOSSMAP, and the Fade, Transparent and Cutout modes become
    /// URP surface type, blend mode and alpha clipping. The Phase 2 PBR maps use the same names on both shaders:
    /// _BumpMap and _BumpScale (_NORMALMAP), _MetallicGlossMap with the smoothness in its alpha (Standard scales it by
    /// _GlossMapScale, Lit by _Smoothness, both set to 1 here), _OcclusionMap and _OcclusionStrength (Lit also needs
    /// _OCCLUSIONMAP), and _DetailAlbedoMap, _DetailNormalMap, _DetailNormalMapScale with the _DetailAlbedoMap tiling
    /// under _DETAIL_MULX2 (Lit adds _DetailAlbedoMapScale). Mats.ApplyRelief and ApplyDetail set them all; only the
    /// keywords differ, through UrpApplyMaskKeywords.
    /// </summary>
    public static partial class Mats
    {
        public const string UrpLitShaderName = "Universal Render Pipeline/Lit";
        public const string UrpUnlitShaderName = "Universal Render Pipeline/Unlit";
        public const string UrpParticlesUnlitShaderName = "Universal Render Pipeline/Particles/Unlit";
        public const string UrpCharacterShaderName = "NeonSyndicate/Character URP";

        /// <summary>
        /// The legacy additive particle shader outputs 2 x tint x vertex colour x texture and clamps alpha after doubling
        /// it. URP's particle shader cannot clamp, so the tint colour is doubled and its alpha scaled by this factor,
        /// the least-squares match of saturate(2a) against k*a over the soft dot texture (k = 1.71; equal energy is 1.84).
        /// </summary>
        public const float UrpAdditiveAlphaScale = 1.75f;

        private const string UrpSmoothness = "_Smoothness";

        // URP surface options (UnityEditor.BaseShaderGUI.SurfaceType and BlendMode).
        private const float UrpSurfaceOpaque = 0f, UrpSurfaceTransparent = 1f;
        private const float UrpBlendAlpha = 0f, UrpBlendAdditive = 2f;

        private static Shader _urpLit;
        private static Shader _urpUnlit;
        private static Shader _urpParticles;

        private static void ResetUrpStatics()
        {
            _urpLit = null;
            _urpUnlit = null;
            _urpParticles = null;
        }

        // ------------------------------------------------------------------ URP shaders

        /// <summary>URP Lit, or the active pipeline's default shader if Lit cannot be found.</summary>
        private static Shader UrpLitShader
        {
            get
            {
                if (_urpLit == null) _urpLit = Shader.Find(UrpLitShaderName);
                if (_urpLit == null && GraphicsSettings.currentRenderPipeline != null) _urpLit = GraphicsSettings.currentRenderPipeline.defaultShader;
                return _urpLit;
            }
        }

        /// <summary>URP Unlit (colour and texture), falling back to URP Lit.</summary>
        private static Shader UrpUnlitShader
        {
            get
            {
                if (_urpUnlit == null) _urpUnlit = Shader.Find(UrpUnlitShaderName);
                if (_urpUnlit == null) _urpUnlit = UrpLitShader;
                return _urpUnlit;
            }
        }

        /// <summary>URP Particles/Unlit (vertex colour times base colour times texture), falling back to URP Unlit.</summary>
        private static Shader UrpParticleShader
        {
            get
            {
                if (_urpParticles == null) _urpParticles = Shader.Find(UrpParticlesUnlitShaderName);
                if (_urpParticles == null) _urpParticles = UrpUnlitShader;
                return _urpParticles;
            }
        }

        /// <summary>
        /// True for a material on one of URP's own shaders while URP renders (they all have the Surface Type option), so
        /// the URP setup applies. A Standard material keeps the Standard setup on either pipeline (the editor builds the
        /// Built-in shader keep materials that way even while URP is active).
        /// </summary>
        private static bool IsUrpSurface(Material m)
        {
            return RenderPath.IsURP && m != null && m.HasProperty("_Surface");
        }

        /// <summary>The smoothness property the wet-road sheen drives: _Smoothness on URP Lit, _Glossiness on Standard.</summary>
        private static string WetSmoothnessProperty { get { return RenderPath.IsURP ? UrpSmoothness : "_Glossiness"; } }

        // ------------------------------------------------------------------ URP material setup

        /// <summary>
        /// A metallic and smoothness mask on URP Lit (Mats.ApplyRelief): the metallic workflow keyword and a smoothness
        /// scale of 1, so the map's alpha is the smoothness as on Standard (the rain sheen scales _Smoothness later).
        /// </summary>
        private static void UrpApplyMaskKeywords(Material m)
        {
            if (m == null) return;
            if (m.HasProperty("_WorkflowMode")) m.SetFloat("_WorkflowMode", 1f);
            if (m.HasProperty(UrpSmoothness)) m.SetFloat(UrpSmoothness, 1f);
            m.EnableKeyword("_METALLICSPECGLOSSMAP");
            m.DisableKeyword("_SPECULAR_SETUP");
        }

        /// <summary>Standard() on URP Lit: smoothness and metallic (base colour is set through Material.color).</summary>
        private static void UrpLitSurface(Material m, float smoothness, float metallic)
        {
            if (m.HasProperty(UrpSmoothness)) m.SetFloat(UrpSmoothness, smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
        }

        /// <summary>
        /// MakeTransparent() on URP: Built-in Fade (everything fades with alpha) is URP Alpha blending; Built-in
        /// Transparent (premultiplied diffuse, reflections and highlights stay) is URP Alpha blending with Preserve
        /// Specular, which premultiplies in the shader (_ALPHAPREMULTIPLY_ON) and blends One, OneMinusSrcAlpha.
        /// </summary>
        private static void UrpMakeTransparent(Material m, bool fade)
        {
            UrpSetTransparent(m, UrpBlendAlpha, !fade);
        }

        /// <summary>
        /// Alpha clipping on a URP Lit material, as the weeds use it (no effect on other materials). Public for the
        /// editor, which makes the URP shader keep materials with the same setup as the game.
        /// </summary>
        public static void MakeUrpCutout(Material m, float cutoff)
        {
            if (IsUrpSurface(m)) UrpMakeCutout(m, cutoff);
        }

        /// <summary>Alpha-tested (cutout) opaque surface, as URP sets up Alpha Clipping.</summary>
        private static void UrpMakeCutout(Material m, float cutoff)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", UrpSurfaceOpaque);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
            if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", cutoff);
            m.EnableKeyword("_ALPHATEST_ON");
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHAMODULATE_ON");
            SetBlendFactors(m, BlendMode.One, BlendMode.Zero, BlendMode.One, BlendMode.Zero);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 1f);
            if (m.HasProperty("_AlphaToMask")) m.SetFloat("_AlphaToMask", 1f);
            m.SetOverrideTag("RenderType", "TransparentCutout");
            m.SetShaderPassEnabled("DepthOnly", true);
            m.renderQueue = (int)RenderQueue.AlphaTest;
        }

        /// <summary>
        /// Runtime copy of URP's transparent surface setup (BaseShaderGUI.SetupMaterialBlendModeInternal) for the
        /// Alpha and Additive blend modes: blend factors, ZWrite off, _SURFACE_TYPE_TRANSPARENT, _ALPHAPREMULTIPLY_ON for
        /// Preserve Specular, the Transparent render type and queue, and no depth prepass.
        /// </summary>
        private static void UrpSetTransparent(Material m, float blendMode, bool preserveSpecular)
        {
            if (m == null) return;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", UrpSurfaceTransparent);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", blendMode);
            if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 0f);
            bool preserve = preserveSpecular && m.HasProperty("_BlendModePreserveSpecular");
            if (m.HasProperty("_BlendModePreserveSpecular")) m.SetFloat("_BlendModePreserveSpecular", preserve ? 1f : 0f);
            BlendMode src, dst, srcA, dstA;
            if (blendMode == UrpBlendAdditive)
            {
                src = BlendMode.SrcAlpha;
                dst = BlendMode.One;
                srcA = BlendMode.One;
                dstA = BlendMode.One;
            }
            else
            {
                src = BlendMode.SrcAlpha;
                dst = BlendMode.OneMinusSrcAlpha;
                srcA = BlendMode.One;
                dstA = BlendMode.OneMinusSrcAlpha;
            }
            // Preserve Specular lifts the alpha multiply from the blend unit into the shader.
            if (preserve) src = BlendMode.One;
            SetBlendFactors(m, src, dst, srcA, dstA);
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_AlphaToMask")) m.SetFloat("_AlphaToMask", 0f);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            if (preserve) m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            else m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.DisableKeyword("_ALPHAMODULATE_ON");
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetShaderPassEnabled("DepthOnly", false);
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetBlendFactors(Material m, BlendMode src, BlendMode dst, BlendMode srcA, BlendMode dstA)
        {
            if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)src);
            if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)dst);
            if (m.HasProperty("_SrcBlendAlpha")) m.SetFloat("_SrcBlendAlpha", (float)srcA);
            if (m.HasProperty("_DstBlendAlpha")) m.SetFloat("_DstBlendAlpha", (float)dstA);
        }

        /// <summary>
        /// Additive() on URP: Particles/Unlit in transparent additive mode (SrcAlpha, One) with the legacy shader's
        /// doubled tint, so muzzle flashes, sparks, fire, glows and underglow keep their Built-in brightness.
        /// </summary>
        private static Material UrpAdditive(string name, Color color, Texture2D tex)
        {
            Material m = new Material(ParticleShader);
            m.name = name;
            m.color = new Color(color.r * 2f, color.g * 2f, color.b * 2f, color.a * UrpAdditiveAlphaScale);
            if (tex != null) m.mainTexture = tex;
            UrpSetTransparent(m, UrpBlendAdditive, false);
            return m;
        }

        /// <summary>
        /// The character material on URP: Resources/NeonCharacterURP.shader (the hand-written URP twin of the Built-in
        /// character shader: same properties, URP PBR lighting). Falls back to URP Lit with the same palette maps.
        /// </summary>
        private static Material UrpCharacterMaterial()
        {
            Shader sh = null;
            try
            {
                sh = Resources.Load<Shader>("NeonCharacterURP");
                if (sh == null) sh = Shader.Find(UrpCharacterShaderName);
            }
            catch (System.Exception) { sh = null; }
            if (sh != null && sh.isSupported)
            {
                Material m = new Material(sh) { name = "Character" };
                m.SetTexture("_MainTex", Tex.CharacterPalette());
                m.SetTexture("_MGTex", Tex.CharacterMG());
                m.SetTexture("_EmTex", Tex.CharacterEmission());
                m.SetTexture("_DetailTex", Tex.CharacterDetail());
                return m;
            }
            // Like the Built-in fallback: palette albedo, metal (R) and smoothness (A) from the MG map, palette emission.
            // URP Lit scales the map's smoothness by _Smoothness, as Standard scales it by _GlossMapScale (1).
            Material f = Textured("Character (URP Lit)", Tex.CharacterPalette(), Color.white, 1f, 0f, null, 0f);
            if (f.HasProperty("_MetallicGlossMap"))
            {
                f.SetTexture("_MetallicGlossMap", Tex.CharacterMG());
                f.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            SetEmission(f, Color.white * 2.4f, Tex.CharacterEmission());
            return f;
        }
    }
}
