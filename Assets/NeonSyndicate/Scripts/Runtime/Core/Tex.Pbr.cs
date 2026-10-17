using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The texture side of the procedural PBR surfaces (Phase 2 of the visual upgrade): texture sizes per graphics
    /// preset, the albedo, normal and mask textures of a SurfaceSet, the facade relief, the shared detail maps for
    /// large flat surfaces and the car paint flake normal. Everything is cached in the Tex cache like the rest, built
    /// once and never per frame. Low never calls any of this (Mats keeps the old materials on Low).
    /// </summary>
    public static partial class Tex
    {
        /// <summary>The kinds that get 1024 textures on Ultra (the ground and walls the camera is closest to most often).</summary>
        public static bool IsHero(SurfaceKind kind)
        {
            switch (kind)
            {
                case SurfaceKind.Asphalt:
                case SurfaceKind.Pavers:
                case SurfaceKind.Concrete:
                case SurfaceKind.Sand:
                case SurfaceKind.Rock:
                case SurfaceKind.SnowCrust:
                case SurfaceKind.UhRock:
                case SurfaceKind.UhConcrete:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Texture size of a kind for a preset: Medium 256 (128 for small-object kinds), High 512 (256), Ultra 1024 for
        /// the hero kinds and 512 for the other large ones (256 small). Low is not asked (it keeps the old maps).
        /// </summary>
        public static int PbrSizeFor(GraphicsPreset preset, SurfaceKind kind)
        {
            bool large = SurfaceHeight.IsLarge(kind);
            switch (preset)
            {
                case GraphicsPreset.Low:
                case GraphicsPreset.Medium:
                    return large ? 256 : 128;
                case GraphicsPreset.High:
                    return large ? 512 : 256;
                default:
                    return IsHero(kind) ? 1024 : (large ? 512 : 256);
            }
        }

        /// <summary>Size of a facade relief map for a preset (the painted facade albedo stays 256).</summary>
        public static int FacadeReliefSizeFor(GraphicsPreset preset)
        {
            switch (preset)
            {
                case GraphicsPreset.Low:
                case GraphicsPreset.Medium:
                    return 256;
                default:
                    return 512;
            }
        }

        /// <summary>The preset size capped for the machine: the maps are generated in C# at boot, so a small machine keeps its boot short over the finest maps.</summary>
        public static int PbrSizeFor(GraphicsPreset preset, SurfaceKind kind, int cap)
        {
            return Mathf.Min(PbrSizeFor(preset, kind), Mathf.Max(128, cap));
        }

        /// <summary>Largest generated PBR texture for a core count: 256 on one or two cores, 512 on three or four, 1024 above.</summary>
        public static int SizeCapForCores(int cores)
        {
            return cores <= 2 ? 256 : (cores <= 4 ? 512 : 1024);
        }

        private static int _sizeCap;

        /// <summary>This machine's size cap (see SizeCapForCores), read once.</summary>
        public static int PbrSizeCap
        {
            get
            {
                if (_sizeCap == 0) _sizeCap = SizeCapForCores(SystemInfo.processorCount);
                return _sizeCap;
            }
        }

        public static int PbrSize(SurfaceKind kind)
        {
            return PbrSizeFor(Settings.Graphics, kind, PbrSizeCap);
        }

        private static Texture2D Upload(string key, int w, int h, Color32[] px, bool linear, TextureFormat format)
        {
            Texture2D t = new Texture2D(w, h, format, true, linear);
            t.name = key;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Trilinear;
            t.anisoLevel = 6;
            t.SetPixels32(px);
            t.Apply(true, true);
            _cache[key] = t;
            return t;
        }

        private static bool TryCached(string key, out Texture2D t)
        {
            return _cache.TryGetValue(key, out t) && t != null;
        }

        // ------------------------------------------------------------------ surface textures

        /// <summary>The albedo of a surface in a palette (palette 0 is precomputed by the worker; others are coloured here).</summary>
        public static Texture2D SurfaceAlbedo(SurfaceSet set, int palette)
        {
            string key = "pbr_" + set.Kind + "_" + palette + "_albedo";
            Texture2D t;
            if (TryCached(key, out t)) return t;
            Color32[] px = palette == 0 && set.AlbedoPixels != null ? set.AlbedoPixels : SurfaceColor.Albedo(set.Kind, set.Fields, set.Shade, palette);
            if (palette == 0) set.AlbedoPixels = null;
            return Upload(key, set.Size, set.Size, px, false, TextureFormat.RGB24);
        }

        /// <summary>The tangent-space normal map of a surface (linear, RGB; the shader reads x from R times A, A is 1).</summary>
        public static Texture2D SurfaceNormal(SurfaceSet set)
        {
            string key = "pbr_" + set.Kind + "_normal";
            Texture2D t;
            if (TryCached(key, out t)) return t;
            Color32[] px = set.NormalPixels ?? SurfaceMaps.Normal(set.Fields.Height, set.Size, SurfaceColor.NormalStrength(set.Kind, set.Size));
            set.NormalPixels = null;
            return Upload(key, set.Size, set.Size, px, true, TextureFormat.RGB24);
        }

        /// <summary>
        /// The Standard and URP Lit mask of a surface for one material: R metallic (the nominal value, lowered by rust
        /// and raised by chips where the kind says so), G occlusion, A smoothness (nominal plus the kind's variation;
        /// capped on wet surfaces so the rain scale stays at or under 1). Cached per kind, metallic and smoothness.
        /// </summary>
        public static Texture2D SurfaceMask(SurfaceSet set, float metallic, float smoothness, bool wet)
        {
            string key = "pbr_" + set.Kind + "_mask_" + Mathf.RoundToInt(metallic * 100f) + "_" + Mathf.RoundToInt(smoothness * 100f) + (wet ? "_wet" : "");
            Texture2D t;
            if (TryCached(key, out t)) return t;
            float[] smooth = SurfaceColor.Smoothness(smoothness, set.GlossVariation, wet);
            float[] metal = SurfaceColor.MetallicVariation(set.Kind, set.Fields, metallic);
            Color32[] px = SurfaceMaps.PackMask(set.Fields.Length, metal, metallic, set.Occlusion, smooth, smoothness);
            return Upload(key, set.Size, set.Size, px, true, TextureFormat.RGBA32);
        }

        /// <summary>An emission map from a surface feature: Mask (which = 0) or Aux (which = 1) times a colour (for rock veins, lava, ember specks).</summary>
        public static Texture2D SurfaceGlow(SurfaceSet set, int which, Color color, string name)
        {
            string key = "pbr_" + set.Kind + "_glow_" + name;
            Texture2D t;
            if (TryCached(key, out t)) return t;
            float[] src = which == 0 ? set.Fields.Mask : set.Fields.Aux;
            Color32[] px = new Color32[set.Fields.Length];
            for (int i = 0; i < px.Length; i++)
            {
                float k = src != null ? src[i] : 0f;
                px[i] = new Color32(SurfaceMaps.ToByte(color.r * k), SurfaceMaps.ToByte(color.g * k), SurfaceMaps.ToByte(color.b * k), 255);
            }
            return Upload(key, set.Size, set.Size, px, false, TextureFormat.RGB24);
        }

        // ------------------------------------------------------------------ facades

        /// <summary>
        /// Relief maps for a facade style, sized by the preset: a normal map (windows recessed, frames raised, brick
        /// courses or stucco grain) and a mask (glass smooth and a little metallic, wall rough, recesses occluded).
        /// The bay geometry repeats Tex.Facade's, so the relief sits under the painted windows.
        /// </summary>
        public static void FacadeRelief(FacadeStyle style, float wallSmoothness, float wallMetallic, out Texture2D normal, out Texture2D mask)
        {
            string key = "facade_" + style;
            Texture2D n, m;
            if (TryCached(key + "_relief", out n) && TryCached(key + "_mask", out m))
            {
                normal = n;
                mask = m;
                return;
            }
            float winW, winH;
            bool glassCurtain;
            FacadeWindow(style, out winW, out winH, out glassCurtain);
            int size = Mathf.Min(FacadeReliefSizeFor(Settings.Graphics), PbrSizeCap);
            SurfaceFields f = SurfaceHeight.Facade(size, winW, winH, glassCurtain, style == FacadeStyle.Brick, (int)style * 97 + 5);
            SurfaceShade shade = SurfaceMaps.Shade(f.Height, size, 5f);
            Color32[] npx = SurfaceMaps.Normal(f.Height, size, 2.2f * size / 256f);
            float glassSmooth = glassCurtain ? 0.9f : 0.84f;
            float glassMetal = glassCurtain ? 0.35f : 0.2f;
            Color32[] mpx = new Color32[f.Length];
            for (int i = 0; i < mpx.Length; i++)
            {
                float window = f.Mask[i];
                float smooth = Mathf.Lerp(wallSmoothness + shade.Convex[i] * 0.1f - shade.Cavity[i] * 0.1f + (f.Tone[i] - 0.5f) * 0.06f, glassSmooth, window);
                float metal = Mathf.Lerp(wallMetallic, glassMetal, window);
                float occ = SurfaceMaps.Occlusion(shade.Cavity[i], 0.8f);
                mpx[i] = new Color32(SurfaceMaps.ToByte(metal), SurfaceMaps.ToByte(occ), 0, SurfaceMaps.ToByte(smooth));
            }
            normal = Upload(key + "_relief", size, size, npx, true, TextureFormat.RGB24);
            mask = Upload(key + "_mask", size, size, mpx, true, TextureFormat.RGBA32);
        }

        /// <summary>The window bay proportions of a facade style (shared by the painted facade and its relief).</summary>
        public static void FacadeWindow(FacadeStyle style, out float winW, out float winH, out bool glassCurtain)
        {
            glassCurtain = false;
            switch (style)
            {
                case FacadeStyle.OfficeGlass: winW = 0.92f; winH = 0.8f; glassCurtain = true; break;
                case FacadeStyle.DarkGlass: winW = 0.94f; winH = 0.82f; glassCurtain = true; break;
                case FacadeStyle.GoldGlass: winW = 0.9f; winH = 0.8f; glassCurtain = true; break;
                case FacadeStyle.Apartment: winW = 0.45f; winH = 0.5f; break;
                case FacadeStyle.Brick: winW = 0.4f; winH = 0.55f; break;
                case FacadeStyle.Stucco: winW = 0.38f; winH = 0.45f; break;
                case FacadeStyle.StuccoPink: winW = 0.4f; winH = 0.45f; break;
                case FacadeStyle.Warehouse: winW = 0.6f; winH = 0.18f; break;
                case FacadeStyle.Casino: winW = 0.8f; winH = 0.6f; break;
                case FacadeStyle.Hospital: winW = 0.7f; winH = 0.5f; break;
                default: winW = 0.6f; winH = 0.5f; break;
            }
        }

        // ------------------------------------------------------------------ detail maps and flakes

        /// <summary>
        /// A detail albedo for the Standard shader's multiply-by-two detail slot: the micro kind's albedo centred on
        /// mid grey, so it only adds texture and never changes the average colour. 256 texels, shared by every user.
        /// </summary>
        public static Texture2D DetailAlbedo(SurfaceKind micro)
        {
            string key = "detail_" + micro + "_albedo";
            Texture2D t;
            if (TryCached(key, out t)) return t;
            SurfaceSet set = Surface(micro);
            if (set == null || set.Error != null) return null;
            Color32[] src = set.AlbedoPixels ?? SurfaceColor.Albedo(set.Kind, set.Fields, set.Shade, 0);
            Color32[] px = new Color32[src.Length];
            double mean = 0.0;
            for (int i = 0; i < src.Length; i++) mean += src[i].r;
            float m = (float)(mean / src.Length) / 255f;
            for (int i = 0; i < src.Length; i++)
            {
                float v = Mathf.Clamp01(0.5f + (src[i].r / 255f - m) * 0.8f);
                byte b = SurfaceMaps.ToByte(v);
                px[i] = new Color32(b, b, b, 255);
            }
            return Upload(key, set.Size, set.Size, px, false, TextureFormat.RGB24);
        }

        /// <summary>The detail normal of a micro kind: its own normal map, tiled finely by the material.</summary>
        public static Texture2D DetailNormal(SurfaceKind micro)
        {
            SurfaceSet set = Surface(micro);
            if (set == null || set.Error != null) return null;
            return SurfaceNormal(set);
        }

        /// <summary>Metallic flake normal for car paint (256, linear): tiny tilted facets that sparkle when tiled finely.</summary>
        public static Texture2D FlakeNormal()
        {
            Texture2D t;
            if (TryCached("flakes", out t)) return t;
            const int n = 256;
            return Upload("flakes", n, n, SurfaceMaps.FlakeNormal(n, 2, 0.35f, 0.45f, 71), true, TextureFormat.RGB24);
        }
    }
}
