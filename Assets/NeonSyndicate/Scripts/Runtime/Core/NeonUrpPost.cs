#if NS_URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NeonSyndicate
{
    /// <summary>
    /// The game camera's image pipeline on the Universal Render Pipeline, the URP twin of NeonPost's OnRenderImage chain.
    /// NeonPost adds it when URP renders the camera. It owns a global Volume whose profile is built at runtime (Bloom,
    /// ACES Tonemapping, Color Adjustments, Lift Gamma Gain, Shadows Midtones Highlights, Vignette, Film Grain, Chromatic
    /// Aberration, Motion Blur, Depth Of Field) and fills it every frame from the numbers NeonPost uses: PostGrade.Compute
    /// (with the Inner World, Frostline and rift weights) and the settings menu, converted to URP's units by
    /// UrpGradeMapping. It also sets the camera's URP options (post-processing on or off, SMAA or TAA by preset and the
    /// temporal setting, dithering, HDR), eases the exposure toward the scene brightness NeonPostFeature measures
    /// (NeonPost's eye adaptation rates, with the target and limits from PostGrade), obeys the same depth of field
    /// request as NeonPost (flyover, cinematics, pause menu), and keeps the per-frame part of the preset mapping
    /// (UrpGraphics.Sync).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class NeonUrpPost : MonoBehaviour, IRenderPathPost
    {
        /// <summary>Above any Volume a scene might add, so the game's grade always wins.</summary>
        private const float VolumePriority = 100f;
        /// <summary>Contrast adaptive sharpening after TAA (Ultra), standing in for NeonPost's sharpen on the composite.</summary>
        private const float TaaSharpening = 0.25f;

        private Camera _cam;
        private UniversalAdditionalCameraData _data;
        private GameObject _volumeObject;
        private Volume _volume;
        private VolumeProfile _profile;
        private Bloom _bloom;
        private Tonemapping _tonemapping;
        private ColorAdjustments _color;
        private LiftGammaGain _liftGammaGain;
        private ShadowsMidtonesHighlights _shadowsMidtonesHighlights;
        private Vignette _vignette;
        private FilmGrain _grain;
        private ChromaticAberration _chromatic;
        private MotionBlur _motionBlur;
        private DepthOfField _depthOfField;
        private float _adaptLog;
        private bool _adaptValid;
        private bool _adaptSnap;

        /// <summary>True when eye adaptation is on, so NeonPostFeature measures the scene for this camera.</summary>
        public bool MeasuresExposure { get { return Settings.AutoExposure; } }

        private void OnEnable()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null && RenderPath.IsURP)
            {
                _data = _cam.GetUniversalAdditionalCameraData();
                // The profile changes every frame, so the camera's volume stack must update every frame too.
                _cam.SetVolumeFrameworkUpdateMode(VolumeFrameworkUpdateMode.EveryFrame);
            }
            if (_profile == null) BuildVolume();
            if (_volume != null) _volume.enabled = true;
            _adaptValid = false;
        }

        private void OnDisable()
        {
            if (_volume != null) _volume.enabled = false;
            _adaptValid = false;
        }

        private void OnDestroy()
        {
            if (_volumeObject != null) CoreUtils.Destroy(_volumeObject);
            if (_profile != null)
            {
                for (int i = 0; i < _profile.components.Count; i++)
                {
                    if (_profile.components[i] != null) CoreUtils.Destroy(_profile.components[i]);
                }
                CoreUtils.Destroy(_profile);
            }
            _volumeObject = null;
            _volume = null;
            _profile = null;
        }

        /// <summary>Forget the previous frame (camera cuts, teleports, cinematics): TAA and motion blur history, and a fresh exposure.</summary>
        public void ResetHistory()
        {
            _adaptSnap = true;
            NeonEyeAdaptation.Invalidate();
            if (_data != null) _data.resetHistory = true;
        }

        private void BuildVolume()
        {
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "Neon Syndicate Grade";
            _bloom = _profile.Add<Bloom>(true);
            _tonemapping = _profile.Add<Tonemapping>(true);
            _color = _profile.Add<ColorAdjustments>(true);
            _liftGammaGain = _profile.Add<LiftGammaGain>(true);
            _shadowsMidtonesHighlights = _profile.Add<ShadowsMidtonesHighlights>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);
            _chromatic = _profile.Add<ChromaticAberration>(true);
            _motionBlur = _profile.Add<MotionBlur>(true);
            _depthOfField = _profile.Add<DepthOfField>(true);

            // Fixed parts: the filmic tonemapper NeonPost fits, the classic bloom chain the mapping is fitted to.
            _tonemapping.mode.value = TonemappingMode.ACES;
            _bloom.filter.value = BloomFilterMode.Gaussian;
            _bloom.downscale.value = BloomDownscaleMode.Half;
            _bloom.clamp.value = UrpGradeMapping.BloomClamp;
            _bloom.dirtIntensity.value = 0f;
            _color.colorFilter.value = Color.white;
            _color.hueShift.value = 0f;
            _vignette.color.value = Color.black;
            _vignette.center.value = new Vector2(0.5f, 0.5f);
            _vignette.rounded.value = false;
            _motionBlur.mode.value = MotionBlurMode.CameraOnly;
            _depthOfField.mode.value = DepthOfFieldMode.Bokeh;
            _depthOfField.focalLength.value = 50f;
            _depthOfField.bladeCount.value = 6;
            _depthOfField.active = false;

            _volumeObject = new GameObject("Neon URP Volume");
            _volumeObject.transform.SetParent(transform, false);
            _volume = _volumeObject.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = VolumePriority;
            _volume.weight = 1f;
            _volume.sharedProfile = _profile;
            if (_data != null) _data.volumeLayerMask = _data.volumeLayerMask | (1 << _volumeObject.layer);
        }

        private void LateUpdate()
        {
            if (_volume == null) return;
            if (!RenderPath.IsURP)
            {
                // The pipeline was switched off while playing: leave the camera to NeonPost.
                _volume.enabled = false;
                return;
            }
            if (!_volume.enabled) _volume.enabled = true;
            GraphicsPreset q = Settings.Graphics;
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            UpdateCamera(q);
            UpdateGrade(q, night);
            UrpGraphics.Sync();
        }

        private void UpdateCamera(GraphicsPreset q)
        {
            if (_data == null) return;
            if (_cam != null) _cam.allowHDR = true;
            // NeonPost's "Post effects" switch turns the whole stack off, anti-aliasing included.
            _data.renderPostProcessing = Settings.Bloom;
            _data.dithering = true;
            // Temporal anti-aliasing on High and Ultra when the setting is on (as NeonPost does on Built-in), SMAA otherwise.
            if (q >= GraphicsPreset.High && Settings.TemporalAA)
            {
                _data.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                ref TemporalAA.Settings taa = ref _data.taaSettings;
                taa.quality = q >= GraphicsPreset.Ultra ? TemporalAAQuality.High : TemporalAAQuality.Medium;
                taa.contrastAdaptiveSharpening = TaaSharpening;
            }
            else
            {
                _data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                _data.antialiasingQuality = q == GraphicsPreset.Low ? AntialiasingQuality.Low
                    : (q == GraphicsPreset.Medium ? AntialiasingQuality.Medium : AntialiasingQuality.High);
            }
        }

        private void UpdateGrade(GraphicsPreset q, float night)
        {
            PostGrade.Values g = PostGrade.Compute(night, q);
            bool blur = Settings.Bloom && Settings.MotionBlur && q >= GraphicsPreset.Medium;
            float fpsScale = Mathf.Clamp(Time.unscaledDeltaTime * 60f, 0.5f, 1.5f);
            UrpGradeMapping.Result r = UrpGradeMapping.Compute(g, q, night, EyeAdaptationEv(g), blur ? 0.45f / fpsScale : 0f);

            _bloom.threshold.value = r.BloomThreshold;
            _bloom.intensity.value = r.BloomIntensity;
            _bloom.scatter.value = r.BloomScatter;
            _bloom.tint.value = r.BloomTint;
            _bloom.maxIterations.value = r.BloomMaxIterations;
            _bloom.highQualityFiltering.value = r.BloomHighQuality;

            _color.postExposure.value = r.PostExposure;
            _color.contrast.value = r.Contrast;
            _color.saturation.value = r.Saturation;

            _liftGammaGain.lift.value = r.Lift;
            _liftGammaGain.gamma.value = r.Gamma;
            _liftGammaGain.gain.value = r.Gain;

            _shadowsMidtonesHighlights.shadows.value = r.Shadows;
            _shadowsMidtonesHighlights.midtones.value = r.Midtones;
            _shadowsMidtonesHighlights.highlights.value = r.Highlights;
            _shadowsMidtonesHighlights.shadowsStart.value = r.ShadowsStart;
            _shadowsMidtonesHighlights.shadowsEnd.value = r.ShadowsEnd;
            _shadowsMidtonesHighlights.highlightsStart.value = r.HighlightsStart;
            _shadowsMidtonesHighlights.highlightsEnd.value = r.HighlightsEnd;

            // NeonPost's own vignette runs in NeonPostFeature after tonemapping; URP's stands in if the feature is missing.
            NeonEyeAdaptation.VignetteAmount = r.ExactVignette;
            _vignette.intensity.value = NeonEyeAdaptation.VignetteFeatureActive ? 0f : r.VignetteIntensity;
            _vignette.smoothness.value = r.VignetteSmoothness;

            _chromatic.intensity.value = r.ChromaticIntensity;

            _grain.type.value = (FilmGrainLookup)r.GrainType;
            _grain.intensity.value = r.GrainIntensity;
            _grain.response.value = r.GrainResponse;

            _motionBlur.intensity.value = r.MotionBlurIntensity;
            _motionBlur.clamp.value = r.MotionBlurClamp;
            _motionBlur.quality.value = q >= GraphicsPreset.Ultra ? MotionBlurQuality.High
                : (q == GraphicsPreset.High ? MotionBlurQuality.Medium : MotionBlurQuality.Low);
            // Moving objects blur too on High and Ultra, as NeonPost's motion vector blur does.
            _motionBlur.mode.value = q >= GraphicsPreset.High ? MotionBlurMode.CameraAndObjects : MotionBlurMode.CameraOnly;

            UpdateDepthOfField();
        }

        /// <summary>
        /// The same request NeonPost obeys: depth of field only on the flyover, in cinematic shots and behind the pause
        /// menu. URP's bokeh is driven by a 50 mm lens; the aperture stands in for NeonPost's blur range (an approximation,
        /// the two kernels differ).
        /// </summary>
        private void UpdateDepthOfField()
        {
            float focus, range;
            bool pause;
            bool want = NeonPost.DepthOfFieldRequest(out focus, out range, out pause);
            _depthOfField.active = want;
            if (!want) return;
            _depthOfField.focusDistance.value = Mathf.Max(focus, 0.1f);
            _depthOfField.aperture.value = pause ? 4f : 2.8f;
        }

        /// <summary>
        /// NeonPost's eye adaptation on the measured scene brightness: brighter scenes settle quickly (stepping out into the
        /// light), dark ones open up more slowly; night aims lower than day so it still feels like night, and partial
        /// strength keeps a dark alley darker than a lit plaza. Returns the exposure change in EV.
        /// </summary>
        private float EyeAdaptationEv(PostGrade.Values g)
        {
            if (!Settings.Bloom || !Settings.AutoExposure || !NeonEyeAdaptation.CanMeasure)
            {
                _adaptValid = false;
                return 0f;
            }
            float measured;
            if (NeonEyeAdaptation.TryGetLatest(out measured))
            {
                if (!_adaptValid || _adaptSnap)
                {
                    _adaptLog = measured;
                    _adaptValid = true;
                    _adaptSnap = false;
                }
                else
                {
                    float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.1f);
                    float rate = measured > _adaptLog ? 1f - Mathf.Exp(-dt * 2.6f) : 1f - Mathf.Exp(-dt * 1.1f);
                    _adaptLog = Mathf.Lerp(_adaptLog, measured, rate);
                }
            }
            if (!_adaptValid) return 0f;
            return Mathf.Clamp(g.AdaptKey - _adaptLog, g.AdaptMinEv, g.AdaptMaxEv) * g.AdaptStrength;
        }
    }
}
#endif
