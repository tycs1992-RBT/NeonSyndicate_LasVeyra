using System;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S visual realism, the material side: upgrades the materials the revision-10 Gothic Heart is built from, in their
    /// original owners (one hook line each), toward the canon palette. The 04O-B kit gets generated gothic ashlar, worn
    /// flagstones, tarnished bronze and glassy amethyst and turquoise crystal (RealismSurfacesH8S); the ancient module
    /// masonry, slate, basalt and iron (AscensionArtH2) get neutral weathered tints, macro weathering and restrained
    /// crystal glow; the 04A venue kit gets darker weathered stone and paving and calmer crystals; the open ground becomes
    /// weathered mineral earth instead of bright striped sand. Large surfaces get a macro map in the detail slot that
    /// breaks visible tiling. Medium and up only (Low keeps its old materials), every step guarded so a failure keeps the
    /// original material. Only Standard keyword sets the editor setup already keeps in builds are used.
    /// </summary>
    public static class RealismMaterialsH8S
    {
        /// <summary>Medium and up (the presets that already use the generated PBR maps).</summary>
        public static bool Enabled { get { return Mats.PbrEnabled; } }

        // ------------------------------------------------------------------ the strongest dials (see the owner guide)

        /// <summary>Crystal glow strength by surface (multiplies the emission map). Lower is calmer.</summary>
        public const float CrystalGlow = 0.85f;
        /// <summary>The old 04A and H2 crystal emission is multiplied by this (their glow was a flat neon wash).</summary>
        public const float LegacyCrystalGlowScale = 0.5f;
        /// <summary>Macro tiling per main texture repeat (one macro period spans 1 / tiling repeats): stone, paving, ground, module masonry.</summary>
        public const float MacroTilingStone = 0.083f, MacroTilingPaving = 0.071f, MacroTilingGround = 0.143f, MacroTilingModules = 0.11f;
        /// <summary>The H3 module masonry tint (was 0.52, 0.48, 0.55, a purple cast): neutral weathered stone over the photo's own variation.</summary>
        public static readonly Color ModuleMasonryTint = new Color(0.58f, 0.58f, 0.64f);
        /// <summary>The H5 civic iron tint (the bright rust photo is tarnished down to dark bronze iron).</summary>
        public static readonly Color ModuleIronTint = new Color(0.62f, 0.6f, 0.62f);

        // ------------------------------------------------------------------ the 04O-B kit (GothicRealmMaterials04OB.For)

        /// <summary>
        /// Replaces the maps and colour of a 04O-B material key with the generated H8S surface. Returns true when it took
        /// over (the caller then skips its own periodic relief maps), false to keep the original path.
        /// </summary>
        public static bool UpgradeGothic04OB(Material m, string key)
        {
            if (m == null || !Enabled) return false;
            try
            {
                switch (key)
                {
                    case "stone": return Surface(m, "ashlar", SurfaceKind.Rock, RealismSurfacesH8S.Ashlar, 1f, 0.75f, RealismSurfacesH8S.MacroStone, MacroTilingStone, 0f);
                    case "paving": return Surface(m, "flagstone", SurfaceKind.Pavers, RealismSurfacesH8S.Flagstone, 1f, 0.8f, RealismSurfacesH8S.MacroPaving, MacroTilingPaving, 0f);
                    case "metal": return Surface(m, "bronze", SurfaceKind.Brushed, RealismSurfacesH8S.Bronze, 1f, 0.6f, -1, 0f, 0f);
                    case "amethyst": return Surface(m, "amethyst", SurfaceKind.Ice, n => RealismSurfacesH8S.Crystal(n, false), 1f, 0.7f, -1, 0f, CrystalGlow);
                    case "teal": return Surface(m, "turquoise", SurfaceKind.Ice, n => RealismSurfacesH8S.Crystal(n, true), 1f, 0.7f, -1, 0f, CrystalGlow);
                    case "lamp":
                        // Amber practical lantern light (canon), a touch warmer and calmer than before.
                        SetColor(m, new Color(1f, 0.64f, 0.3f));
                        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(0.95f, 0.55f, 0.22f) * 0.75f);
                        return false;
                }
            }
            catch (Exception e) { Log.Exception("H8S realism 04O-B " + key, e); }
            return false;
        }

        // ------------------------------------------------------------------ ancient modules (AscensionArtH2.Surface)

        /// <summary>Retints, weathers and calms an ancient module material (masonry, basalt, slate, iron, amethyst, azure).</summary>
        public static void UpgradeAscensionH2(Material m, string role, bool heart, bool civic)
        {
            if (m == null || !Enabled) return;
            try
            {
                switch (role)
                {
                    case "masonry":
                        if (heart) SetColor(m, ModuleMasonryTint);
                        ApplyMacro(m, RealismSurfacesH8S.MacroStone, MacroTilingModules);
                        break;
                    case "basalt":
                        ApplyMacro(m, RealismSurfacesH8S.MacroStone, MacroTilingModules);
                        break;
                    case "slate":
                        // Roofs stay distinct from walls: dark blue-grey slate, a little weathering.
                        SetColor(m, new Color(0.9f, 0.92f, 0.98f));
                        ApplyMacro(m, RealismSurfacesH8S.MacroStone, MacroTilingModules);
                        break;
                    case "iron":
                        if (civic) SetColor(m, ModuleIronTint);
                        break;
                    case "amethyst":
                        Surface(m, "amethyst", SurfaceKind.Ice, n => RealismSurfacesH8S.Crystal(n, false), 1f, 0.7f, -1, 0f, CrystalGlow);
                        break;
                    case "azure":
                        Surface(m, "turquoise", SurfaceKind.Ice, n => RealismSurfacesH8S.Crystal(n, true), 1f, 0.7f, -1, 0f, CrystalGlow);
                        break;
                }
            }
            catch (Exception e) { Log.Exception("H8S realism module " + role, e); }
        }

        /// <summary>
        /// A per-instance UV shift for an ancient module placed at a position: the tiling masonry slides by a fraction of a
        /// repeat (whole repeats move only the macro map), so identical modules side by side no longer show identical
        /// stones. Deterministic from the position, so both LODs and every rebuild agree. Pure.
        /// </summary>
        public static Vector2 ModuleUvShift(Vector3 at)
        {
            int x = Mathf.RoundToInt(at.x * 4f), z = Mathf.RoundToInt(at.z * 4f), y = Mathf.RoundToInt(at.y * 4f);
            float a = HeightField.Hash01(x, z, 1301 + y), b = HeightField.Hash01(z, x, 1307 + y);
            float whole = Mathf.Floor(HeightField.Hash01(x + 17, z - 5, 1311) * 9f);
            return new Vector2(a + whole, b + Mathf.Floor(HeightField.Hash01(x - 3, z + 11, 1319) * 9f));
        }

        // ------------------------------------------------------------------ the 04A venue kit (VisualRebirth04AAssets.Material)

        /// <summary>Darker weathered stone and paving, tarnished gilt and calmer crystal for a 04A kit material.</summary>
        public static void UpgradeRebirth04A(Material m, int style, string role)
        {
            if (m == null || !Enabled) return;
            try
            {
                Color tint = GetColor(m);
                switch (role)
                {
                    case "stone":
                    case "trim":
                        // Pale limestone and sandstone become weathered Gothic Heart stone; the basalt style stays charcoal.
                        if (style != 3) SetColor(m, Color.Lerp(tint, RealismSurfacesH8S.HeartWeathered * 1.25f, 0.45f) * 0.82f);
                        ApplyMacro(m, RealismSurfacesH8S.MacroStone, MacroTilingModules);
                        break;
                    case "paving":
                        SetColor(m, Color.Lerp(tint, RealismSurfacesH8S.HeartWeathered * 1.2f, 0.55f) * 0.86f);
                        ApplyMacro(m, RealismSurfacesH8S.MacroPaving, MacroTilingModules);
                        break;
                    case "soil":
                        SetColor(m, tint * 0.85f);
                        ApplyMacro(m, RealismSurfacesH8S.MacroGround, MacroTilingModules);
                        break;
                    case "gilt":
                        SetColor(m, tint * 0.78f);
                        break;
                    case "crystal":
                    case "glass":
                    {
                        // Keep each style's hue, lose the neon: less saturated body, half the glow, glassier surface.
                        float l = RealismRulesH8S.Luminance(tint);
                        SetColor(m, Color.Lerp(tint, new Color(l, l, l), 0.3f) * 0.85f);
                        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", m.GetColor("_EmissionColor") * LegacyCrystalGlowScale);
                        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.9f);
                        break;
                    }
                }
            }
            catch (Exception e) { Log.Exception("H8S realism 04A " + role, e); }
        }

        // ------------------------------------------------------------------ the open ground (Mats.Sand)

        /// <summary>
        /// The weathered mineral ground that replaces the bright desert sand on Medium and up (Mats.Sand keeps its name and
        /// cache key), or null when generation failed (the caller then builds the old sand).
        /// </summary>
        public static Material Ground()
        {
            if (!Enabled) return null;
            try
            {
                Texture2D a, n, k, e;
                if (!RealismTexturesH8S.Maps("ground", RealismTexturesH8S.SizeFor(SurfaceKind.Sand), RealismSurfacesH8S.Ground, out a, out n, out k, out e)) return null;
                Material m = Mats.Standard("Weathered Ground (H8S)", Color.white, 0.08f, 0f);
                m.mainTexture = a;
                Mats.ApplyRelief(m, n, 0.9f, k, 0.8f);
                ApplyMacro(m, RealismSurfacesH8S.MacroGround, MacroTilingGround);
                return m;
            }
            catch (Exception ex)
            {
                Log.Exception("H8S realism ground", ex);
                return null;
            }
        }

        // ------------------------------------------------------------------ helpers

        private static bool Surface(Material m, string key, SurfaceKind like, Func<int, RealismMapsH8S> build, float bump, float occlusion, int macro, float macroTiling, float glow)
        {
            Texture2D a, n, k, e;
            if (!RealismTexturesH8S.Maps(key, RealismTexturesH8S.SizeFor(like), build, out a, out n, out k, out e)) return false;
            SetColor(m, Color.white);
            if (m.HasProperty("_MainTex")) { m.SetTexture("_MainTex", a); m.SetTextureScale("_MainTex", Vector2.one); }
            if (m.HasProperty("_BaseMap")) { m.SetTexture("_BaseMap", a); m.SetTextureScale("_BaseMap", Vector2.one); }
            // Metallic and smoothness now come from the mask (ApplyRelief enables the map keyword and sets the scale).
            Mats.ApplyRelief(m, n, bump, k, occlusion);
            if (macro >= 0) ApplyMacro(m, macro, macroTiling);
            else ClearDetail(m);
            if (e != null && glow > 0f)
            {
                Mats.SetEmission(m, Color.white * glow, e);
            }
            else if (m.HasProperty("_EmissionColor"))
            {
                m.SetColor("_EmissionColor", Color.black);
                m.DisableKeyword("_EMISSION");
            }
            return true;
        }

        /// <summary>
        /// Puts a macro map in the detail slot (multiply by two, so 0.5 is neutral), tiled a fraction of once per main
        /// repeat with an offset so its period never lines up with the main map; the detail normal is cleared (flat).
        /// Requires the material's normal and metallic maps, so the keyword set is one the build keeps.
        /// </summary>
        public static void ApplyMacro(Material m, int kind, float tiling)
        {
            if (m == null || !m.HasProperty("_DetailAlbedoMap")) return;
            Texture2D macro = RealismTexturesH8S.Macro(kind);
            if (macro == null) return;
            m.SetTexture("_DetailAlbedoMap", macro);
            m.SetTextureScale("_DetailAlbedoMap", new Vector2(tiling, tiling));
            m.SetTextureOffset("_DetailAlbedoMap", new Vector2(0.37f, 0.71f));
            if (m.HasProperty("_DetailAlbedoMapScale")) m.SetFloat("_DetailAlbedoMapScale", 1f);
            if (m.HasProperty("_DetailNormalMap")) m.SetTexture("_DetailNormalMap", null);
            if (m.HasProperty("_DetailNormalMapScale")) m.SetFloat("_DetailNormalMapScale", 0f);
            if (m.HasProperty("_UVSec")) m.SetFloat("_UVSec", 0f);
            m.EnableKeyword("_DETAIL_MULX2");
        }

        private static void ClearDetail(Material m)
        {
            if (m.HasProperty("_DetailAlbedoMap")) m.SetTexture("_DetailAlbedoMap", null);
            if (m.HasProperty("_DetailNormalMap")) m.SetTexture("_DetailNormalMap", null);
            m.DisableKeyword("_DETAIL_MULX2");
        }

        private static Color GetColor(Material m)
        {
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return Color.white;
        }

        private static void SetColor(Material m, Color c)
        {
            c.a = 1f;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }
    }
}
