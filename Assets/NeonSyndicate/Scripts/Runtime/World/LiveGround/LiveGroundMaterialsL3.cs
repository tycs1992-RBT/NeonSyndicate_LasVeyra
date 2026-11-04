using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Shared material proxies on existing ground batches. No geometry, physics, save or frame-loop ownership.</summary>
    public static class LiveGroundMaterialsL3
    {
        private static readonly Dictionary<Material, Material> Proxies = new Dictionary<Material, Material>();
        private static LiveGroundCatalogL3 _catalog;
        private static Texture2D _mask;
        private static bool _tried, _selected, _comparison, _revision10, _faulted;
        private static string _reason = "Not selected.";
        private static LiveGroundCatalogL3 Catalog
        {
            get
            {
                if (!_tried) { _tried = true; _catalog = Resources.Load<LiveGroundCatalogL3>("LiveGroundPrepared/RedspireCatalog"); }
                return _catalog;
            }
        }
        public static Material Floor { get { return Wrap(Mats.Sand); } }
        public static bool Enabled { get { return !_faulted && LiveGroundRulesL3.Active(_selected, _comparison, _revision10, Catalog != null && Catalog.Ready); } }
        public static string Status { get { return "Redspire ground: " + (_selected ? "ON" : "OFF") + (_comparison ? " (temporary legacy)" : "") + ". " + _reason + " Shared ground materials " + Proxies.Count + "."; } }

        public static void ResetStatics()
        {
            foreach (Material proxy in Proxies.Values) Dispose(proxy);
            Proxies.Clear();
            Dispose(_mask); _mask = null; _catalog = null; _tried = false;
            _selected = _comparison = _revision10 = _faulted = false; _reason = "Not selected.";
            LiveGroundRulesL3.ResetStatics();
        }
        private static void Dispose(Object item)
        { if (item == null) return; if (Application.isPlaying) Object.Destroy(item); else Object.DestroyImmediate(item); }

        public static Material Wrap(Material source)
        {
            if (source == null) return source;
            Material proxy;
            if (!Proxies.TryGetValue(source, out proxy) || proxy == null)
            {
                proxy = new Material(source) { name = "L3 ground / " + source.name };
                Proxies[source] = proxy;
                try { Apply(source, proxy); }
                catch (System.Exception error) { _faulted = true; Restore(source, proxy); _reason = "Original fallback: " + error.Message; Log.Warn("LIVE_GROUND " + _reason); }
            }
            return proxy;
        }
        public static void Select(bool selected, bool comparison, bool revision10)
        {
            _selected = selected; _comparison = comparison; _revision10 = revision10; _faulted = false;
            try
            {
                foreach (KeyValuePair<Material, Material> pair in Proxies) Apply(pair.Key, pair.Value);
                _reason = Enabled ? "Two PBR families loaded on the original floor and wild ground. Native review pending."
                    : Catalog == null ? "Prepared catalog absent; original fallback. Stop Play and prepare Redspire ground."
                    : !Catalog.Ready ? "Map or supported shader missing; original fallback. " + Catalog.Reason
                    : !_revision10 ? "Original fallback outside revision 10." : "Original surface retained.";
            }
            catch (System.Exception error)
            {
                _faulted = true; _reason = "Original fallback: " + error.Message;
                foreach (KeyValuePair<Material, Material> pair in Proxies) Restore(pair.Key, pair.Value);
            }
            Log.Info("LIVE_GROUND " + Status);
        }
        private static void Restore(Material source, Material proxy)
        {
            if (source == null || proxy == null) return;
            proxy.shader = source.shader; proxy.CopyPropertiesFromMaterial(source); proxy.shaderKeywords = source.shaderKeywords;
        }
        private static void Apply(Material source, Material proxy)
        {
            Restore(source, proxy);
            if (!Enabled) return;
            if (_mask == null) BuildMask();
            LiveGroundCatalogL3 c = Catalog;
            bool urp = RenderPath.IsURP;
            proxy.shader = c.SurfaceShader;
            proxy.shaderKeywords = new string[0];
            proxy.SetTexture("_LegacyTex", source.GetTexture(urp ? "_BaseMap" : "_MainTex"));
            proxy.SetColor("_LegacyColor", source.GetColor(urp ? "_BaseColor" : "_Color"));
            Vector2 scale = source.mainTextureScale, offset = source.mainTextureOffset;
            proxy.SetVector("_LegacyTex_ST", new Vector4(scale.x, scale.y, offset.x, offset.y));
            proxy.SetFloat("_LegacySmoothness", source.HasProperty(urp ? "_Smoothness" : "_Glossiness") ? source.GetFloat(urp ? "_Smoothness" : "_Glossiness") : .1f);
            CopyLegacy(source, proxy, "_BumpMap", "_LegacyNormal");
            CopyLegacy(source, proxy, "_MetallicGlossMap", "_LegacyPacked");
            CopyLegacy(source, proxy, "_DetailAlbedoMap", "_LegacyDetail");
            CopyLegacy(source, proxy, "_DetailNormalMap", "_LegacyDetailNormal");
            bool packed = source.IsKeywordEnabled(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
            proxy.SetFloat("_LegacyHasMask", packed ? 1f : 0f);
            proxy.SetFloat("_LegacyMaskScale", source.HasProperty(urp ? "_Smoothness" : "_GlossMapScale") ? source.GetFloat(urp ? "_Smoothness" : "_GlossMapScale") : 1f);
            proxy.SetFloat("_LegacyBumpScale", source.HasProperty("_BumpScale") ? source.GetFloat("_BumpScale") : 1f);
            proxy.SetFloat("_LegacyHasNormal", source.IsKeywordEnabled("_NORMALMAP") ? 1f : 0f);
            proxy.SetFloat("_LegacyDetailEnabled", source.IsKeywordEnabled("_DETAIL_MULX2") || source.IsKeywordEnabled("_DETAIL_SCALED") ? 1f : 0f);
            proxy.SetFloat("_LegacyDetailBump", source.HasProperty("_DetailNormalMapScale") ? source.GetFloat("_DetailNormalMapScale") : 0f);
            proxy.SetFloat("_LegacyOcclusion", source.HasProperty("_OcclusionStrength") ? source.GetFloat("_OcclusionStrength") : 0f);
            Vector2 ds = source.GetTextureScale("_DetailAlbedoMap"), dt = source.GetTextureOffset("_DetailAlbedoMap");
            proxy.SetVector("_LegacyDetail_ST", new Vector4(ds.x,ds.y,dt.x,dt.y));
            proxy.SetTexture("_EarthColor", c.Earth[0]); proxy.SetTexture("_EarthNormal", c.Earth[1]); proxy.SetTexture("_EarthMask", c.Earth[2]);
            proxy.SetTexture("_SandColor", c.Sand[0]); proxy.SetTexture("_SandNormal", c.Sand[1]); proxy.SetTexture("_SandMask", c.Sand[2]);
            proxy.SetTexture("_RegionMask", _mask);
            proxy.SetFloat("_MapHalf", CityLayout.MapHalf); proxy.SetFloat("_TileMetres", LiveGroundRulesL3.TileMetres);
        }
        private static void CopyLegacy(Material source, Material proxy, string property, string target)
        { if (source.HasProperty(property)) proxy.SetTexture(target,source.GetTexture(property)); }
        private static void BuildMask()
        {
            const int size = 512;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float wx = ((x + .5f) / size * 2f - 1f) * CityLayout.MapHalf;
                float wz = ((y + .5f) / size * 2f - 1f) * CityLayout.MapHalf;
                pixels[y * size + x] = new Color(LiveGroundRulesL3.Weight(wx, wz), LiveGroundRulesL3.Stony(wx, wz, 1f), 0f, 1f);
            }
            _mask = new Texture2D(size, size, TextureFormat.RGBA32, true, true) { name = "L3 existing biome mask", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            _mask.SetPixels32(pixels); _mask.Apply(true, true);
        }
    }
}
