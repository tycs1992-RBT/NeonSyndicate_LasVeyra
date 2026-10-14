#if NS_URP
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NeonSyndicate
{
    /// <summary>
    /// Maps the graphics preset onto the active URP pipeline asset at runtime, from the same numbers
    /// Settings.ApplyGraphics gives the Built-in pipeline: shadow distance, cascade count and splits, shadow map sizes,
    /// render scale, the per-object light limit, HDR and Low Dynamic Range grading (what UrpGradeMapping assumes), MSAA off
    /// while URP's post-processing anti-aliasing (SMAA or TAA, NeonUrpPost) runs, screen space ambient occlusion on or off,
    /// and the sun's soft shadow filter. The asset's own values are saved the first time and put back when the game quits
    /// (in the editor: when Play mode ends), so playing never permanently edits the asset.
    /// </summary>
    public static class UrpGraphics
    {
        /// <summary>Render scale on the Low preset (URP upscales the frame); the other presets render at full size.</summary>
        public const float LowRenderScale = 0.85f;

        private sealed class SavedAsset
        {
            public UniversalRenderPipelineAsset Asset;
            public float ShadowDistance;
            public int ShadowCascades;
            public float Cascade2Split;
            public Vector3 Cascade4Split;
            public int MainLightShadowResolution;
            public int AdditionalLightShadowResolution;
            public float RenderScale;
            public int Msaa;
            public int MaxAdditionalLights;
            public bool Hdr;
            public ColorGradingMode Grading;
        }

        private static SavedAsset _saved;
        private static readonly List<ScriptableRendererFeature> _features = new List<ScriptableRendererFeature>();
        private static readonly List<bool> _featuresWereActive = new List<bool>();
        private static bool _quitHooked;

        public static void ResetStatics()
        {
            if (_quitHooked) Application.quitting -= Restore;
            _quitHooked = false;
            _saved = null;
            _features.Clear();
            _featuresWereActive.Clear();
        }

        private static UniversalRenderPipelineAsset ActiveAsset
        {
            get { return GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset; }
        }

        /// <summary>Called by Settings.ApplyGraphics (through RenderPath) right after it set the quality settings.</summary>
        public static void Apply()
        {
            UniversalRenderPipelineAsset asset = ActiveAsset;
            if (asset == null) return;
            Remember(asset);
            GraphicsPreset q = Settings.Graphics;
            asset.shadowDistance = QualitySettings.shadowDistance;
            asset.shadowCascadeCount = Mathf.Clamp(QualitySettings.shadowCascades, 1, 4);
            asset.cascade2Split = QualitySettings.shadowCascade2Split;
            asset.cascade4Split = QualitySettings.shadowCascade4Split;
            // The Built-in preset uses Very High shadow resolution on High and Ultra, Medium below.
            asset.mainLightShadowmapResolution = q >= GraphicsPreset.High ? 4096 : 1024;
            asset.additionalLightsShadowmapResolution = q >= GraphicsPreset.High ? 2048 : 1024;
            asset.renderScale = q == GraphicsPreset.Low ? LowRenderScale : 1f;
            // The Built-in pixel light count; URP's per-object limit is 8 (the Forward+ renderer has no such limit).
            asset.maxAdditionalLightsCount = Mathf.Clamp(QualitySettings.pixelLightCount, 0, 8);
            asset.supportsHDR = true;
            asset.colorGradingMode = ColorGradingMode.LowDynamicRange;
            Sync();
        }

        /// <summary>
        /// The parts of the preset that follow settings changed without ApplyGraphics (the post effects and ambient
        /// occlusion toggles). NeonUrpPost calls this every frame; it only writes what changed.
        /// </summary>
        public static void Sync()
        {
            UniversalRenderPipelineAsset asset = ActiveAsset;
            if (asset == null) return;
            Remember(asset);
            GraphicsPreset q = Settings.Graphics;
            // URP's anti-aliasing (SMAA or TAA) is part of its post-processing, and TAA needs MSAA off. With the post
            // effects switched off the preset's MSAA comes back, so edges stay smooth as on the Built-in path. URP keeps
            // the quality setting equal to the asset's sample count, as it does when the pipeline starts.
            int msaa = Settings.Bloom ? 1 : PresetMsaa(q);
            if (asset.msaaSampleCount != msaa) asset.msaaSampleCount = msaa;
            int quality = msaa > 1 ? msaa : 0;
            if (QualitySettings.antiAliasing != quality) QualitySettings.antiAliasing = quality;
            SetAmbientOcclusion(asset, Settings.Bloom && Settings.AmbientOcclusion && q >= GraphicsPreset.High);
            SetSunSoftShadows(q);
        }

        /// <summary>The Built-in preset's MSAA sample count.</summary>
        public static int PresetMsaa(GraphicsPreset q)
        {
            switch (q)
            {
                case GraphicsPreset.Low: return 1;
                case GraphicsPreset.Medium: return 2;
                case GraphicsPreset.High: return 4;
                default: return 8;
            }
        }

        private static void SetAmbientOcclusion(UniversalRenderPipelineAsset asset, bool on)
        {
            ReadOnlySpan<ScriptableRendererData> renderers = asset.rendererDataList;
            for (int i = 0; i < renderers.Length; i++)
            {
                ScriptableRendererData data = renderers[i];
                if (data == null) continue;
                List<ScriptableRendererFeature> features = data.rendererFeatures;
                for (int f = 0; f < features.Count; f++)
                {
                    ScriptableRendererFeature feature = features[f];
                    if (!(feature is ScreenSpaceAmbientOcclusion) || feature.isActive == on) continue;
                    if (!_features.Contains(feature))
                    {
                        _features.Add(feature);
                        _featuresWereActive.Add(feature.isActive);
                    }
                    feature.SetActive(on);
                }
            }
        }

        /// <summary>
        /// The Built-in Low preset forces hard shadows. URP's per-light filter has no hard setting (its global soft
        /// shadow switch is not public API), so Low gets URP's cheapest soft filter and the presets above step it up.
        /// </summary>
        private static void SetSunSoftShadows(GraphicsPreset q)
        {
            Light sun = RenderSettings.sun;
            if (sun == null || sun.shadows == LightShadows.None) return;
            UniversalAdditionalLightData data = sun.GetUniversalAdditionalLightData();
            SoftShadowQuality want = q == GraphicsPreset.Low ? SoftShadowQuality.Low : (q == GraphicsPreset.Medium ? SoftShadowQuality.Medium : SoftShadowQuality.High);
            if (data != null && data.softShadowQuality != want) data.softShadowQuality = want;
        }

        private static void Remember(UniversalRenderPipelineAsset asset)
        {
            if (_saved != null && _saved.Asset == asset) return;
            // Another asset became active (a quality level with its own asset): put the previous one back first.
            Restore();
            _saved = new SavedAsset
            {
                Asset = asset,
                ShadowDistance = asset.shadowDistance,
                ShadowCascades = asset.shadowCascadeCount,
                Cascade2Split = asset.cascade2Split,
                Cascade4Split = asset.cascade4Split,
                MainLightShadowResolution = asset.mainLightShadowmapResolution,
                AdditionalLightShadowResolution = asset.additionalLightsShadowmapResolution,
                RenderScale = asset.renderScale,
                Msaa = asset.msaaSampleCount,
                MaxAdditionalLights = asset.maxAdditionalLightsCount,
                Hdr = asset.supportsHDR,
                Grading = asset.colorGradingMode
            };
            if (!_quitHooked)
            {
                Application.quitting += Restore;
                _quitHooked = true;
            }
        }

        /// <summary>Puts the URP asset and its renderer features back the way they were before the game changed them.</summary>
        public static void Restore()
        {
            SavedAsset s = _saved;
            _saved = null;
            if (s != null && s.Asset != null)
            {
                s.Asset.shadowDistance = s.ShadowDistance;
                s.Asset.shadowCascadeCount = Mathf.Clamp(s.ShadowCascades, 1, 4);
                s.Asset.cascade2Split = s.Cascade2Split;
                s.Asset.cascade4Split = s.Cascade4Split;
                s.Asset.mainLightShadowmapResolution = s.MainLightShadowResolution;
                s.Asset.additionalLightsShadowmapResolution = s.AdditionalLightShadowResolution;
                s.Asset.renderScale = s.RenderScale;
                s.Asset.msaaSampleCount = s.Msaa;
                s.Asset.maxAdditionalLightsCount = s.MaxAdditionalLights;
                s.Asset.supportsHDR = s.Hdr;
                s.Asset.colorGradingMode = s.Grading;
            }
            for (int i = 0; i < _features.Count; i++)
            {
                if (_features[i] != null) _features[i].SetActive(_featuresWereActive[i]);
            }
            _features.Clear();
            _featuresWereActive.Clear();
        }
    }
}
#endif
