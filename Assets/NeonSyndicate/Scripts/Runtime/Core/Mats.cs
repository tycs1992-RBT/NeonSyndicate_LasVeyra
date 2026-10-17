using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum FacadeStyle
    {
        OfficeGlass,
        DarkGlass,
        GoldGlass,
        Apartment,
        Brick,
        Stucco,
        StuccoPink,
        Warehouse,
        Casino,
        Hospital,
        Police
    }

    /// <summary>
    /// Procedural material and texture library. Everything is generated in code so the project needs no imported art.
    /// Built-in Render Pipeline Standard shader by default; when the Universal Render Pipeline is active
    /// (RenderPath.IsURP) the same helpers hand out URP Lit, Unlit and Particles materials (see MatsUrp.cs).
    /// </summary>
    public static partial class Mats
    {
        private static Shader _standard;
        private static Shader _unlitColor;
        private static Shader _unlitTexture;
        private static Shader _sprite;
        private static Shader _particle;
        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();
        private static readonly List<Material> _nightEmissive = new List<Material>();
        private static readonly List<Color> _nightEmissiveColors = new List<Color>();
        private static Font _font;
        private static bool _fontTried;

        public static readonly Color[] CarPaints =
        {
            new Color(0.75f, 0.05f, 0.06f), new Color(0.05f, 0.10f, 0.35f), new Color(0.92f, 0.92f, 0.94f), new Color(0.04f, 0.04f, 0.05f),
            new Color(0.55f, 0.56f, 0.60f), new Color(0.95f, 0.70f, 0.05f), new Color(0.08f, 0.35f, 0.18f), new Color(0.40f, 0.08f, 0.45f),
            new Color(0.90f, 0.35f, 0.05f), new Color(0.10f, 0.55f, 0.70f), new Color(0.30f, 0.20f, 0.12f), new Color(0.85f, 0.20f, 0.55f),
            new Color(0.22f, 0.24f, 0.28f), new Color(0.65f, 0.60f, 0.45f), new Color(0.15f, 0.70f, 0.55f), new Color(0.98f, 0.98f, 0.30f)
        };

        public static readonly Color[] SkinTones = CharacterPaletteData.Skin;

        public static readonly Color[] ClothColors = CharacterPaletteData.Cloth;

        public static readonly Color[] HairColors = CharacterPaletteData.Hair;

        public static readonly Color[] NeonColors = CharacterPaletteData.Neon;

        public static void ResetStatics()
        {
            _cache.Clear();
            _nightEmissive.Clear();
            _nightEmissiveColors.Clear();
            _standard = null;
            _unlitColor = null;
            _unlitTexture = null;
            _sprite = null;
            _particle = null;
            _font = null;
            _fontTried = false;
            ResetUrpStatics();
            ResetPbrStatics();
        }

        // ------------------------------------------------------------------ shaders

        public static Shader StandardShader
        {
            get
            {
                if (_standard == null && RenderPath.IsURP) _standard = UrpLitShader;
                if (_standard == null)
                {
                    _standard = Shader.Find("Standard");
                    if (_standard == null)
                    {
                        // Guaranteed fallback: the shader used by Unity's default primitive material.
                        GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Renderer r = probe.GetComponent<Renderer>();
                        if (r != null && r.sharedMaterial != null) _standard = r.sharedMaterial.shader;
                        Object.Destroy(probe);
                    }
                }
                return _standard;
            }
        }

        public static Shader UnlitColorShader
        {
            get
            {
                if (_unlitColor == null && RenderPath.IsURP) _unlitColor = UrpUnlitShader;
                if (_unlitColor == null) _unlitColor = Shader.Find("Unlit/Color");
                if (_unlitColor == null) _unlitColor = SpriteShader;
                return _unlitColor;
            }
        }

        public static Shader UnlitTextureShader
        {
            get
            {
                if (_unlitTexture == null && RenderPath.IsURP) _unlitTexture = UrpUnlitShader;
                if (_unlitTexture == null) _unlitTexture = Shader.Find("Unlit/Texture");
                if (_unlitTexture == null) _unlitTexture = SpriteShader;
                return _unlitTexture;
            }
        }

        /// <summary>
        /// Sprites/Default on both pipelines. Its pass has no LightMode tag, so URP's forward renderer draws it as
        /// SRPDefaultUnlit exactly as the Built-in pipeline does, and the minimap's per-renderer _Color property blocks
        /// keep working (URP's own shaders call the colour _BaseColor).
        /// </summary>
        public static Shader SpriteShader
        {
            get
            {
                if (_sprite == null) _sprite = Shader.Find("Sprites/Default");
                if (_sprite == null) _sprite = StandardShader;
                return _sprite;
            }
        }

        public static Shader ParticleShader
        {
            get
            {
                if (_particle == null && RenderPath.IsURP) _particle = UrpParticleShader;
                if (_particle == null) _particle = Shader.Find("Legacy Shaders/Particles/Additive");
                if (_particle == null) _particle = Shader.Find("Particles/Standard Unlit");
                if (_particle == null) _particle = SpriteShader;
                return _particle;
            }
        }

        public static Font UIFont
        {
            get
            {
                if (_font == null && !_fontTried)
                {
                    _fontTried = true;
                    try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                    catch (System.Exception) { _font = null; }
                    if (_font == null)
                    {
                        try { _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans", "DejaVu Sans" }, 32); }
                        catch (System.Exception) { _font = null; }
                    }
                }
                return _font;
            }
        }

        // ------------------------------------------------------------------ material helpers

        public static Material Standard(string name, Color color, float smoothness, float metallic)
        {
            Material m = new Material(StandardShader);
            m.name = name;
            m.color = color;
            if (IsUrpSurface(m)) UrpLitSurface(m, smoothness, metallic);
            else
            {
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            }
            m.enableInstancing = true;
            return m;
        }

        public static Material Textured(string name, Texture2D albedo, Color tint, float smoothness, float metallic, Texture2D normal, float bump)
        {
            Material m = Standard(name, tint, smoothness, metallic);
            if (albedo != null) m.mainTexture = albedo;
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.SetTexture("_BumpMap", normal);
                m.SetFloat("_BumpScale", bump);
                m.EnableKeyword("_NORMALMAP");
            }
            return m;
        }

        public static void SetEmission(Material m, Color emission, Texture2D emissionMap)
        {
            if (m == null || !m.HasProperty("_EmissionColor")) return;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (emissionMap != null && m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", emissionMap);
            m.SetColor("_EmissionColor", emission);
        }

        /// <summary>Standard shader in transparent (premultiplied) mode. On URP: Lit with the matching surface options.</summary>
        public static void MakeTransparent(Material m, bool fade)
        {
            if (m == null) return;
            if (IsUrpSurface(m))
            {
                UrpMakeTransparent(m, fade);
                return;
            }
            m.SetFloat("_Mode", fade ? 2f : 3f);
            m.SetInt("_SrcBlend", fade ? (int)BlendMode.SrcAlpha : (int)BlendMode.One);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            if (fade)
            {
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            else
            {
                m.DisableKeyword("_ALPHABLEND_ON");
                m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }
            m.renderQueue = (int)RenderQueue.Transparent;
        }

        public static Material Unlit(string name, Color color)
        {
            Material m = new Material(UnlitColorShader);
            m.name = name;
            m.color = color;
            return m;
        }

        public static Material UnlitTex(string name, Texture2D tex)
        {
            Material m = new Material(UnlitTextureShader);
            m.name = name;
            m.mainTexture = tex;
            return m;
        }

        public static Material SpriteMat(string name, Color color, Texture2D tex)
        {
            Material m = new Material(SpriteShader);
            m.name = name;
            m.color = color;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        public static Material Additive(string name, Color color, Texture2D tex)
        {
            if (RenderPath.IsURP) return UrpAdditive(name, color, tex);
            Material m = new Material(ParticleShader);
            m.name = name;
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", color);
            m.color = color;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        private static Material Cached(string key, System.Func<Material> build)
        {
            Material m;
            if (_cache.TryGetValue(key, out m) && m != null) return m;
            m = build();
            _cache[key] = m;
            return m;
        }

        private static void RegisterNight(Material m, Color nightEmission)
        {
            _nightEmissive.Add(m);
            _nightEmissiveColors.Add(nightEmission);
        }

        /// <summary>0 = full day, 1 = full night. Drives window glow, lamps and signs.</summary>
        public static void SetNightFactor(float night)
        {
            for (int i = 0; i < _nightEmissive.Count; i++)
            {
                Material m = _nightEmissive[i];
                if (m == null) continue;
                Color c = _nightEmissiveColors[i] * Mathf.Lerp(0.03f, 1f, night);
                m.SetColor("_EmissionColor", c);
            }
        }

        // ------------------------------------------------------------------ world materials

        // Each family keeps its old material on Low (PbrEnabled false) and gets the generated PBR map set from
        // Medium up (Mats.Pbr.cs): the second argument of RegisterWet is the nominal smoothness the rain sheen scales.
        public static Material Asphalt { get { return Cached("asphalt", () => { Material m = PbrEnabled ? Pbr("Asphalt", SurfaceKind.Asphalt, 0, Color.white, 0.18f, 0f, true, 0.7f, SurfaceKind.Grain, 6f, 0.5f) : Textured("Asphalt", Tex.Asphalt(), Color.white, 0.18f, 0f, Tex.NormalFrom(Tex.Asphalt(), 2.2f), 0.6f); RegisterWet(m, 0.18f); return m; }); } }
        public static Material Highway { get { return Cached("highway", () => { Material m = PbrEnabled ? Pbr("Highway", SurfaceKind.Asphalt, 0, new Color(0.82f, 0.82f, 0.86f), 0.2f, 0f, true, 0.5f, SurfaceKind.Grain, 6f, 0.4f) : Textured("Highway", Tex.Asphalt(), new Color(0.82f, 0.82f, 0.86f), 0.2f, 0f, null, 0f); RegisterWet(m, 0.2f); return m; }); } }
        public static Material LaneWhite { get { return Cached("lanewhite", () => { Material m = PbrEnabled ? Pbr("Lane White", SurfaceKind.Grain, 0, new Color(0.85f, 0.85f, 0.82f), 0.3f, 0f, true, 0.6f) : Standard("Lane White", new Color(0.85f, 0.85f, 0.82f), 0.3f, 0f); RegisterWet(m, 0.3f); return m; }); } }
        public static Material LaneYellow { get { return Cached("laneyellow", () => { Material m = PbrEnabled ? Pbr("Lane Yellow", SurfaceKind.Grain, 0, new Color(0.95f, 0.72f, 0.12f), 0.3f, 0f, true, 0.6f) : Standard("Lane Yellow", new Color(0.95f, 0.72f, 0.12f), 0.3f, 0f); RegisterWet(m, 0.3f); return m; }); } }
        public static Material Sidewalk { get { return Cached("sidewalk", () => { Material m = PbrEnabled ? Pbr("Sidewalk", SurfaceKind.Pavers, 0, Color.white, 0.12f, 0f, true, 0.7f, SurfaceKind.Grain, 4f, 0.5f) : Textured("Sidewalk", Tex.Pavers(), Color.white, 0.12f, 0f, Tex.NormalFrom(Tex.Pavers(), 2.5f), 0.5f); RegisterWet(m, 0.12f); return m; }); } }
        public static Material Curb { get { return Cached("curb", () => PbrEnabled ? Pbr("Curb", SurfaceKind.Concrete, 2, Color.white, 0.1f, 0f, false, 0.5f) : Standard("Curb", new Color(0.62f, 0.61f, 0.58f), 0.1f, 0f)); } }
        public static Material Concrete { get { return Cached("concrete", () => PbrEnabled ? Pbr("Concrete", SurfaceKind.Concrete, 0, Color.white, 0.1f, 0f, false, 0.6f, SurfaceKind.Pores, 3f, 0.5f) : Textured("Concrete", Tex.Concrete(), Color.white, 0.1f, 0f, Tex.NormalFrom(Tex.Concrete(), 1.5f), 0.4f)); } }
        public static Material Grass { get { return Cached("grass", () => PbrEnabled ? Pbr("Grass", SurfaceKind.Grass, 0, Color.white, 0.05f, 0f, false, 0.6f, SurfaceKind.Fibre, 6f, 0.5f) : Textured("Grass", Tex.Grass(), Color.white, 0.05f, 0f, null, 0f)); } }
        public static Material Sand { get { return Cached("sand", () => PbrEnabled ? Pbr("Desert Sand", SurfaceKind.Sand, 0, Color.white, 0.04f, 0f, false, 0.6f, SurfaceKind.Grain, 12f, 0.5f) : Textured("Desert Sand", Tex.Sand(), Color.white, 0.04f, 0f, Tex.NormalFrom(Tex.Sand(), 1.2f), 0.35f)); } }
        public static Material Dirt { get { return Cached("dirt", () => PbrEnabled ? Pbr("Dirt", SurfaceKind.Dirt, 0, Color.white, 0.04f, 0f, false, 0.7f, SurfaceKind.Grain, 3f, 0.5f) : Textured("Dirt", Tex.Dirt(), Color.white, 0.04f, 0f, null, 0f)); } }
        public static Material Rock { get { return Cached("rock", () => PbrEnabled ? Pbr("Rock", SurfaceKind.Rock, 0, Color.white, 0.08f, 0f, false, 1f, SurfaceKind.Grain, 4f, 0.6f) : Textured("Rock", Tex.Rock(), Color.white, 0.08f, 0f, Tex.NormalFrom(Tex.Rock(), 3f), 0.9f)); } }
        /// <summary>Canal water: transparent, with a ripple normal map from Medium up.</summary>
        public static Material Water
        {
            get
            {
                return Cached("water", () =>
                {
                    Material m = Standard("Canal Water", new Color(0.08f, 0.32f, 0.38f, 0.72f), 0.95f, 0.1f);
                    if (PbrEnabled)
                    {
                        Tex.SurfaceSet ripples = Tex.Surface(SurfaceKind.Ripples);
                        if (ripples != null && ripples.Error == null) ApplyRelief(m, Tex.SurfaceNormal(ripples), 0.35f, null, 1f);
                    }
                    MakeTransparent(m, false);
                    return m;
                });
            }
        }
        public static Material Roof { get { return Cached("roof", () => PbrEnabled ? Pbr("Tar Roof", SurfaceKind.Gravel, 0, Color.white, 0.05f, 0f, false, 0.7f) : Textured("Tar Roof", Tex.Gravel(), Color.white, 0.05f, 0f, null, 0f)); } }
        public static Material RoofTile { get { return Cached("rooftile", () => PbrEnabled ? Pbr("Roof Tile", SurfaceKind.RoofTiles, 0, Color.white, 0.12f, 0f, false, 0.8f) : Textured("Roof Tile", Tex.RoofTiles(), Color.white, 0.12f, 0f, Tex.NormalFrom(Tex.RoofTiles(), 2f), 0.5f)); } }
        public static Material Metal { get { return Cached("metal", () => PbrEnabled ? Micro("Metal", new Color(0.42f, 0.43f, 0.46f), 0.45f, 0.6f, SurfaceKind.Brushed, 0.5f) : Standard("Metal", new Color(0.42f, 0.43f, 0.46f), 0.45f, 0.6f)); } }
        public static Material DarkMetal { get { return Cached("darkmetal", () => PbrEnabled ? Micro("Dark Metal", new Color(0.12f, 0.12f, 0.13f), 0.4f, 0.5f, SurfaceKind.Brushed, 0.5f) : Standard("Dark Metal", new Color(0.12f, 0.12f, 0.13f), 0.4f, 0.5f)); } }
        public static Material Corrugated { get { return Cached("corrugated", () => PbrEnabled ? Pbr("Corrugated Metal", SurfaceKind.Corrugated, 0, Color.white, 0.35f, 0.4f, false, 0.9f) : Textured("Corrugated Metal", Tex.Corrugated(), Color.white, 0.35f, 0.4f, Tex.NormalFrom(Tex.Corrugated(), 3f), 0.8f)); } }
        public static Material Chrome { get { return Cached("chrome", () => PbrEnabled ? Micro("Chrome", new Color(0.8f, 0.8f, 0.82f), 0.92f, 1f, SurfaceKind.Brushed, 0.2f) : Standard("Chrome", new Color(0.8f, 0.8f, 0.82f), 0.92f, 1f)); } }
        public static Material Rubber { get { return Cached("rubber", () => PbrEnabled ? Micro("Rubber", new Color(0.03f, 0.03f, 0.035f), 0.15f, 0f, SurfaceKind.Pores, 0.8f) : Standard("Rubber", new Color(0.03f, 0.03f, 0.035f), 0.15f, 0f)); } }
        public static Material Wood { get { return Cached("wood", () => PbrEnabled ? Pbr("Wood", SurfaceKind.Wood, 0, Color.white, 0.15f, 0f, false, 0.6f) : Textured("Wood", Tex.Wood(), Color.white, 0.15f, 0f, null, 0f)); } }
        public static Material PalmTrunk { get { return Cached("palmtrunk", () => PbrEnabled ? Pbr("Palm Trunk", SurfaceKind.Bark, 0, Color.white, 0.05f, 0f, false, 0.9f) : Textured("Palm Trunk", Tex.Bark(), Color.white, 0.05f, 0f, null, 0f)); } }
        /// <summary>Cutout weed tufts (the tuft mesh carries both faces): the foliage shader from Medium up, Standard cutout otherwise and on URP.</summary>
        public static Material Weed
        {
            get
            {
                return Cached("weed", () => Foliage("Weeds", Tex.Weeds(), Color.white, 0.04f, new Color(0.45f, 0.6f, 0.2f), true, () =>
                {
                    Material m = Textured("Weeds", Tex.Weeds(), Color.white, 0.04f, 0f, null, 0f);
                    if (IsUrpSurface(m))
                    {
                        UrpMakeCutout(m, 0.5f);
                        return m;
                    }
                    m.SetFloat("_Mode", 1f);
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                    m.SetInt("_SrcBlend", (int)BlendMode.One);
                    m.SetInt("_DstBlend", (int)BlendMode.Zero);
                    m.SetInt("_ZWrite", 1);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.DisableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
                    m.renderQueue = (int)RenderQueue.AlphaTest;
                    return m;
                }));
            }
        }

        public static Material PalmLeaf { get { return Cached("palmleaf", () => Foliage("Palm Leaf", null, new Color(0.2f, 0.44f, 0.15f), 0.22f, new Color(0.5f, 0.7f, 0.25f), false, () => Standard("Palm Leaf", new Color(0.2f, 0.44f, 0.15f), 0.22f, 0f))); } }
        public static Material PalmDead { get { return Cached("palmdead", () => Foliage("Palm Dead Frond", null, new Color(0.46f, 0.37f, 0.23f), 0.1f, new Color(0.6f, 0.5f, 0.3f), false, () => Standard("Palm Dead Frond", new Color(0.46f, 0.37f, 0.23f), 0.1f, 0f))); } }
        public static Material TreeLeaf { get { return Cached("treeleaf", () => Foliage("Tree Leaf", null, new Color(0.16f, 0.36f, 0.12f), 0.08f, new Color(0.45f, 0.65f, 0.2f), false, () => Standard("Tree Leaf", new Color(0.16f, 0.36f, 0.12f), 0.08f, 0f))); } }
        public static Material Cactus { get { return Cached("cactus", () => Foliage("Cactus", null, new Color(0.22f, 0.42f, 0.2f), 0.1f, new Color(0.3f, 0.45f, 0.15f), false, () => Standard("Cactus", new Color(0.22f, 0.42f, 0.2f), 0.1f, 0f))); } }
        public static Material Glass { get { return Cached("glass", () => Standard("Glass", new Color(0.18f, 0.28f, 0.34f), 0.94f, 0.3f)); } }
        public static Material Fence { get { return Cached("fence", () => PbrEnabled ? Micro("Chainlink", new Color(0.45f, 0.46f, 0.48f), 0.4f, 0.6f, SurfaceKind.Brushed, 0.5f) : Standard("Chainlink", new Color(0.45f, 0.46f, 0.48f), 0.4f, 0.6f)); } }
        public static Material Trim { get { return Cached("trim", () => PbrEnabled ? Micro("Trim", new Color(0.28f, 0.28f, 0.3f), 0.25f, 0.1f, SurfaceKind.Grain, 0.6f) : Standard("Trim", new Color(0.28f, 0.28f, 0.3f), 0.25f, 0.1f)); } }
        public static Material Plastic { get { return Cached("plastic", () => PbrEnabled ? Micro("Plastic", new Color(0.2f, 0.45f, 0.25f), 0.35f, 0f, SurfaceKind.Grain, 0.6f) : Standard("Plastic", new Color(0.2f, 0.45f, 0.25f), 0.35f, 0f)); } }
        public static Material RedPlastic { get { return Cached("redplastic", () => PbrEnabled ? Micro("Red Plastic", new Color(0.7f, 0.06f, 0.05f), 0.4f, 0f, SurfaceKind.Grain, 0.6f) : Standard("Red Plastic", new Color(0.7f, 0.06f, 0.05f), 0.4f, 0f)); } }
        public static Material WhitePaint { get { return Cached("whitepaint", () => PbrEnabled ? Micro("White Paint", new Color(0.88f, 0.88f, 0.86f), 0.3f, 0f, SurfaceKind.Grain, 0.5f) : Standard("White Paint", new Color(0.88f, 0.88f, 0.86f), 0.3f, 0f)); } }
        public static Material OrangeCone { get { return Cached("cone", () => PbrEnabled ? Micro("Traffic Orange", new Color(0.95f, 0.38f, 0.05f), 0.3f, 0f, SurfaceKind.Grain, 0.6f) : Standard("Traffic Orange", new Color(0.95f, 0.38f, 0.05f), 0.3f, 0f)); } }
        public static Material Billboard { get { return Cached("billboard", () => { Material m = Textured("Billboard", Tex.Billboard(), Color.white, 0.4f, 0f, null, 0f); SetEmission(m, new Color(0.6f, 0.6f, 0.6f), Tex.Billboard()); RegisterNight(m, new Color(0.85f, 0.85f, 0.85f)); return m; }); } }
        public static Material Storefront { get { return Cached("storefront", () => { Material m = Textured("Storefront", Tex.Storefront(), Color.white, 0.8f, 0.1f, null, 0f); SetEmission(m, Color.black, Tex.StorefrontGlow()); RegisterNight(m, new Color(1f, 0.85f, 0.6f)); return m; }); } }

        public static Material Facade(FacadeStyle style)
        {
            return Cached("facade_" + style, () => BuildFacade(style));
        }

        private static Material BuildFacade(FacadeStyle style)
        {
            Texture2D albedo;
            Texture2D glow;
            Tex.Facade(style, out albedo, out glow);
            float smooth = 0.25f;
            float metal = 0f;
            switch (style)
            {
                case FacadeStyle.OfficeGlass:
                case FacadeStyle.DarkGlass:
                case FacadeStyle.GoldGlass:
                    smooth = 0.86f;
                    metal = 0.35f;
                    break;
                case FacadeStyle.Casino:
                    smooth = 0.6f;
                    break;
            }
            Material m;
            if (PbrEnabled)
            {
                // Relief and mask from Medium up: windows recessed, smooth and a little metallic; walls rough with the
                // style's wall finish (glass curtains keep metallic mullions); recesses occluded. Walls get a plaster
                // (or brick grain) detail map on High and up, one metre per repeat.
                bool glass = smooth > 0.8f;
                float wallSmooth = glass ? 0.5f : (style == FacadeStyle.Casino ? 0.5f : 0.22f);
                float wallMetal = glass ? 0.3f : 0f;
                m = Textured("Facade " + style, albedo, Color.white, wallSmooth, wallMetal, null, 0f);
                Texture2D relief;
                Texture2D mask;
                Tex.FacadeRelief(style, wallSmooth, wallMetal, out relief, out mask);
                ApplyRelief(m, relief, 0.6f, mask, 1f);
                if (DetailEnabled) ApplyDetail(m, style == FacadeStyle.Brick ? SurfaceKind.Grain : SurfaceKind.Pores, 16f, 0.45f);
            }
            else m = Textured("Facade " + style, albedo, Color.white, smooth, metal, Tex.NormalFrom(albedo, 1.2f), 0.35f);
            Color night = style == FacadeStyle.Casino ? new Color(1f, 0.75f, 0.95f) * 1.4f : new Color(1f, 0.82f, 0.55f);
            SetEmission(m, night * 0.03f, glow);
            RegisterNight(m, night);
            return m;
        }

        public static Material Neon(int index)
        {
            index = Mathf.Abs(index) % NeonColors.Length;
            return Cached("neon_" + index, () =>
            {
                Color c = NeonColors[index];
                Material m = Standard("Neon " + index, c * 0.6f, 0.7f, 0f);
                SetEmission(m, c * 1.2f, null);
                RegisterNight(m, c * 2.6f);
                return m;
            });
        }

        public static Material LampHead { get { return Cached("lamphead", () => { Material m = Standard("Street Lamp", new Color(1f, 0.85f, 0.6f), 0.6f, 0f); SetEmission(m, Color.black, null); RegisterNight(m, new Color(1f, 0.72f, 0.38f) * 2.2f); return m; }); } }
        public static Material WindowGlowWarm { get { return Cached("windowwarm", () => { Material m = Standard("Window Warm", new Color(0.3f, 0.28f, 0.22f), 0.9f, 0f); SetEmission(m, Color.black, null); RegisterNight(m, new Color(1f, 0.8f, 0.45f) * 1.3f); return m; }); } }

        public static Material SignalLamp(int colorIndex, bool on)
        {
            return Cached("signal_" + colorIndex + "_" + on, () =>
            {
                Color c = colorIndex == 0 ? new Color(1f, 0.08f, 0.05f) : colorIndex == 1 ? new Color(1f, 0.7f, 0.05f) : new Color(0.1f, 1f, 0.35f);
                Material m = Standard("Signal " + colorIndex + (on ? " On" : " Off"), on ? c : c * 0.12f, 0.6f, 0f);
                if (on) SetEmission(m, c * 2.4f, null);
                return m;
            });
        }

        // ------------------------------------------------------------------ vehicle materials

        /// <summary>
        /// Car paint. From Medium up it is a clearcoat approximation on the Standard shader: a near-white base map
        /// tinted by the colour that carries grime rising from the sills (v = 0 of every panel face), an orange-peel
        /// normal map, a mask whose smoothness is the 0.9 clearcoat top layer (dull over the grime and stone chips,
        /// which also show bare metal) and, on High and up, a metallic-flake detail normal tiled finely over the panel.
        /// </summary>
        public static Material CarPaint(Color c)
        {
            string key = "paint_" + ColorUtility.ToHtmlStringRGB(c);
            return Cached(key, () =>
            {
                if (!PbrEnabled) return Standard("Car Paint " + key, c, 0.78f, 0.35f);
                Material m = Pbr("Car Paint " + key, SurfaceKind.PaintPeel, 0, c, 0.9f, 0.35f, false, 0.35f);
                if (DetailEnabled && m.HasProperty("_DetailNormalMap"))
                {
                    Texture2D flakes = Tex.FlakeNormal();
                    Texture2D da = Tex.DetailAlbedo(SurfaceKind.Grain);
                    if (flakes != null && da != null)
                    {
                        m.SetTexture("_DetailNormalMap", flakes);
                        if (m.HasProperty("_DetailNormalMapScale")) m.SetFloat("_DetailNormalMapScale", 0.35f);
                        m.SetTexture("_DetailAlbedoMap", da);
                        m.SetTextureScale("_DetailAlbedoMap", new Vector2(12f, 12f));
                        if (m.HasProperty("_DetailAlbedoMapScale")) m.SetFloat("_DetailAlbedoMapScale", 0.15f);
                        if (m.HasProperty("_UVSec")) m.SetFloat("_UVSec", 0f);
                        m.EnableKeyword("_DETAIL_MULX2");
                    }
                }
                return m;
            });
        }

        public static Material CarGlass { get { return Cached("carglass", () => Standard("Car Glass", new Color(0.05f, 0.08f, 0.1f), 0.95f, 0.4f)); } }
        public static Material CarInterior { get { return Cached("carinterior", () => PbrEnabled ? Micro("Car Interior", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0f, SurfaceKind.Pores, 0.7f) : Standard("Car Interior", new Color(0.08f, 0.08f, 0.09f), 0.1f, 0f)); } }
        public static Material Headlight { get { return Cached("headlight", () => { Material m = Standard("Headlight", new Color(1f, 0.97f, 0.88f), 0.9f, 0f); SetEmission(m, new Color(0.5f, 0.48f, 0.42f), null); return m; }); } }
        public static Material HeadlightOn { get { return Cached("headlighton", () => { Material m = Standard("Headlight On", new Color(1f, 0.97f, 0.88f), 0.9f, 0f); SetEmission(m, new Color(1f, 0.95f, 0.8f) * 3f, null); return m; }); } }
        public static Material Taillight { get { return Cached("taillight", () => { Material m = Standard("Taillight", new Color(0.6f, 0.02f, 0.02f), 0.8f, 0f); SetEmission(m, new Color(0.5f, 0.0f, 0.0f), null); return m; }); } }
        public static Material BrakeLight { get { return Cached("brakelight", () => { Material m = Standard("Brake Light", new Color(1f, 0.05f, 0.03f), 0.8f, 0f); SetEmission(m, new Color(1f, 0.05f, 0.02f) * 3.5f, null); return m; }); } }
        public static Material PoliceRed { get { return Cached("policered", () => { Material m = Standard("Lightbar Red", new Color(1f, 0.05f, 0.05f), 0.8f, 0f); SetEmission(m, new Color(1f, 0.02f, 0.02f) * 4f, null); return m; }); } }
        public static Material PoliceBlue { get { return Cached("policeblue", () => { Material m = Standard("Lightbar Blue", new Color(0.05f, 0.15f, 1f), 0.8f, 0f); SetEmission(m, new Color(0.05f, 0.2f, 1f) * 4f, null); return m; }); } }
        public static Material LightOff { get { return Cached("lightoff", () => Standard("Light Off", new Color(0.2f, 0.2f, 0.22f), 0.8f, 0f)); } }

        // ------------------------------------------------------------------ characters

        public static Material CharacterPalette { get { return Cached("charpalette", () => Textured("Character Palette", Tex.CharacterPalette(), Color.white, 0.18f, 0f, null, 0f)); } }

        /// <summary>
        /// The shared material for every person: the NeonSyndicate/Character surface shader (palette albedo,
        /// per-cell metal and gloss, glowing seams and implants, fabric detail normals, rim light, hit flash).
        /// Falls back to the Standard shader with the same palette maps if the custom shader is unavailable.
        /// </summary>
        /// <summary>Edge light on every character (warm by day, neon blue at night); rgb colour, a strength.</summary>
        public static void SetCharacterRim(Color rim)
        {
            Material m = CharacterMaterial;
            if (m != null && m.HasProperty("_RimColor")) m.SetColor("_RimColor", rim);
        }

        public static Material CharacterMaterial
        {
            get
            {
                return Cached("character", () =>
                {
                    if (RenderPath.IsURP) return UrpCharacterMaterial();
                    Shader sh = null;
                    try
                    {
                        sh = Resources.Load<Shader>("NeonCharacter");
                        if (sh == null) sh = Shader.Find("NeonSyndicate/Character");
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
                    Material f = Textured("Character (Standard)", Tex.CharacterPalette(), Color.white, 0.3f, 0f, null, 0f);
                    if (f.HasProperty("_MetallicGlossMap"))
                    {
                        f.SetTexture("_MetallicGlossMap", Tex.CharacterMG());
                        f.EnableKeyword("_METALLICGLOSSMAP");
                    }
                    SetEmission(f, Color.white * 2.4f, Tex.CharacterEmission());
                    return f;
                });
            }
        }

        // ------------------------------------------------------------------ fx

        public static Material Blood { get { return Cached("blood", () => { Material m = Textured("Blood Decal", Tex.BloodSplat(), Color.white, 0.6f, 0f, null, 0f); MakeTransparent(m, true); return m; }); } }
        public static Material BulletHole { get { return Cached("bullethole", () => { Material m = Textured("Bullet Hole", Tex.BulletHole(), Color.white, 0.2f, 0f, null, 0f); MakeTransparent(m, true); return m; }); } }
        public static Material Scorch { get { return Cached("scorch", () => { Material m = Textured("Scorch", Tex.Scorch(), Color.white, 0.05f, 0f, null, 0f); MakeTransparent(m, true); return m; }); } }
        public static Material Skid { get { return Cached("skid", () => { Material m = Textured("Skid", Tex.Skid(), Color.white, 0.1f, 0f, null, 0f); MakeTransparent(m, true); return m; }); } }
        public static Material Tracer { get { return Cached("tracer", () => SpriteMat("Tracer", new Color(1f, 0.85f, 0.45f, 0.85f), null)); } }
        public static Material Muzzle { get { return Cached("muzzle", () => Additive("Muzzle Flash", new Color(1f, 0.75f, 0.35f, 1f), Tex.SoftDot())); } }
        public static Material Spark { get { return Cached("spark", () => Additive("Spark", new Color(1f, 0.8f, 0.4f, 1f), Tex.SoftDot())); } }
        public static Material Fire { get { return Cached("fire", () => Additive("Fire", new Color(1f, 0.5f, 0.15f, 1f), Tex.SmokePuff())); } }
        public static Material Smoke { get { return Cached("smoke", () => SpriteMat("Smoke", new Color(0.25f, 0.25f, 0.27f, 0.55f), Tex.SmokePuff())); } }
        public static Material Dust { get { return Cached("dust", () => SpriteMat("Dust", new Color(0.7f, 0.62f, 0.5f, 0.5f), Tex.SmokePuff())); } }
        public static Material BloodPuff { get { return Cached("bloodpuff", () => SpriteMat("Blood Puff", new Color(0.55f, 0.02f, 0.02f, 0.9f), Tex.SmokePuff())); } }
        public static Material WaterSpray { get { return Cached("waterspray", () => SpriteMat("Water Spray", new Color(0.75f, 0.85f, 0.95f, 0.6f), Tex.SoftDot())); } }
        public static Material Rain { get { return Cached("rain", () => SpriteMat("Rain", new Color(0.7f, 0.75f, 0.85f, 0.35f), Tex.SoftDot())); } }
        public static Material Glow(Color c)
        {
            string key = "glow_" + ColorUtility.ToHtmlStringRGBA(c);
            return Cached(key, () => Additive("Glow " + key, c, Tex.SoftDot()));
        }

        public static Material Marker(Color c)
        {
            string key = "marker_" + ColorUtility.ToHtmlStringRGBA(c);
            return Cached(key, () =>
            {
                Material m = Standard("Marker " + key, c, 0.5f, 0f);
                MakeTransparent(m, true);
                SetEmission(m, new Color(c.r, c.g, c.b) * 1.5f, null);
                return m;
            });
        }

        /// <summary>A plain coloured surface for props, gun bodies and vehicle accents. From Medium up it carries the shared cast-grain micro surface (one normal and mask texture for every colour).</summary>
        public static Material Solid(Color c, float smooth)
        {
            string key = "solid_" + ColorUtility.ToHtmlStringRGBA(c) + "_" + Mathf.RoundToInt(smooth * 100f);
            return Cached(key, () => PbrEnabled ? Micro("Solid " + key, c, smooth, 0f, SurfaceKind.Grain, 0.5f) : Standard("Solid " + key, c, smooth, 0f));
        }

        public static Material Emissive(Color c, float intensity)
        {
            string key = "emis_" + ColorUtility.ToHtmlStringRGBA(c) + "_" + Mathf.RoundToInt(intensity * 100f);
            return Cached(key, () =>
            {
                Material m = Standard("Emissive " + key, c, 0.6f, 0f);
                SetEmission(m, c * intensity, null);
                return m;
            });
        }

        public static Material MapMaterial(Texture2D tex)
        {
            return UnlitTex("City Map", tex);
        }
    }
}
