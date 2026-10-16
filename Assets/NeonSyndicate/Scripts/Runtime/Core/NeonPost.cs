using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The camera's image pipeline (Built-in render pipeline, OnRenderImage): HDR bloom through a mip chain, screen
    /// space ambient occlusion, camera motion blur, chromatic aberration, eye adaptation (auto exposure), ACES filmic
    /// tonemapping with a day and night grade, vignette, film grain, FXAA and sharpening. Also keeps a time-sliced live reflection probe around the
    /// camera so armour, cars and wet streets reflect the neon city. Each effect follows the graphics preset and the
    /// settings menu; if the shader is missing or unsupported the camera simply renders without post effects.
    /// When the Universal Render Pipeline renders the camera (RenderPath.IsURP) OnRenderImage never runs: this component
    /// then only keeps the reflection probe and the character rim light, and adds NeonUrpPost (the URP volume stack).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class NeonPost : MonoBehaviour
    {
        private const int PassPrefilter = 0, PassDown = 1, PassUp = 2, PassAO = 3, PassAOBlur = 4, PassComposite = 5, PassFXAA = 6, PassLum = 7, PassAdapt = 8;
        private const int LumSize = 64, LumTopMip = 6;

        private Camera _cam;
        private Material _mat;
        private bool _failed;
        private Matrix4x4 _prevViewProj;
        private bool _hasPrev;
        private readonly RenderTexture[] _mips = new RenderTexture[8];
        private ReflectionProbe _probe;
        private float _probeTimer;
        private int _probeRender = -1;
        private Color _rimSent = new Color(-1f, 0f, 0f, 0f);
        private readonly RenderTexture[] _adapt = new RenderTexture[2];
        private int _adaptIndex;
        private bool _adaptValid;
        private IRenderPathPost _urp;

        private static readonly int IdBloomBase = Shader.PropertyToID("_BloomBase");
        private static readonly int IdBloomTex = Shader.PropertyToID("_BloomTex");
        private static readonly int IdAOTex = Shader.PropertyToID("_AOTex");
        private static readonly int IdThreshold = Shader.PropertyToID("_Threshold");
        private static readonly int IdScatter = Shader.PropertyToID("_Scatter");
        private static readonly int IdBloomIntensity = Shader.PropertyToID("_BloomIntensity");
        private static readonly int IdBloomTint = Shader.PropertyToID("_BloomTint");
        private static readonly int IdExposure = Shader.PropertyToID("_Exposure");
        private static readonly int IdSaturation = Shader.PropertyToID("_Saturation");
        private static readonly int IdContrast = Shader.PropertyToID("_Contrast");
        private static readonly int IdLift = Shader.PropertyToID("_Lift");
        private static readonly int IdGamma = Shader.PropertyToID("_Gamma");
        private static readonly int IdGain = Shader.PropertyToID("_Gain");
        private static readonly int IdShadowTint = Shader.PropertyToID("_ShadowTint");
        private static readonly int IdHighlightTint = Shader.PropertyToID("_HighlightTint");
        private static readonly int IdVignette = Shader.PropertyToID("_Vignette");
        private static readonly int IdChromatic = Shader.PropertyToID("_Chromatic");
        private static readonly int IdGrain = Shader.PropertyToID("_Grain");
        private static readonly int IdGrainTime = Shader.PropertyToID("_GrainTime");
        private static readonly int IdAOStrength = Shader.PropertyToID("_AOStrength");
        private static readonly int IdAORadius = Shader.PropertyToID("_AORadius");
        private static readonly int IdMotionBlur = Shader.PropertyToID("_MotionBlur");
        private static readonly int IdSharpen = Shader.PropertyToID("_Sharpen");
        private static readonly int IdBlurDir = Shader.PropertyToID("_BlurDir");
        private static readonly int IdProj = Shader.PropertyToID("_Proj");
        private static readonly int IdViewToWorld = Shader.PropertyToID("_ViewToWorld");
        private static readonly int IdPrevViewProj = Shader.PropertyToID("_PrevViewProj");
        private static readonly int IdLumTex = Shader.PropertyToID("_LumTex");
        private static readonly int IdAdaptTex = Shader.PropertyToID("_AdaptTex");
        private static readonly int IdAdaptRate = Shader.PropertyToID("_AdaptRate");
        private static readonly int IdAutoExposure = Shader.PropertyToID("_AutoExposure");

        private void OnEnable()
        {
            _cam = GetComponent<Camera>();
            if (RenderPath.IsURP)
            {
                // URP draws this camera: its volume stack does the image effects (NeonUrpPost), so no material here.
                if (_urp == null) _urp = RenderPath.AttachPost(gameObject);
                return;
            }
            if (_mat != null || _failed) return;
            try
            {
                Shader sh = Resources.Load<Shader>("NeonPost");
                if (sh == null) sh = Shader.Find("Hidden/NeonSyndicate/Post");
                if (sh == null || !sh.isSupported) { _failed = true; return; }
                _mat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            }
            catch (System.Exception e)
            {
                _failed = true;
                Debug.LogWarning("Post effects disabled: " + e.Message);
            }
        }

        private bool WantAO { get { return Settings.Bloom && Settings.AmbientOcclusion && Settings.Graphics >= GraphicsPreset.High; } }
        private bool WantMotionBlur { get { return Settings.Bloom && Settings.MotionBlur && Settings.Graphics >= GraphicsPreset.Medium; } }

        private void OnPreCull()
        {
            if (_cam == null) return;
            // The depth texture costs a depth pre-pass in forward rendering, so only ask for it when an effect reads it.
            bool depth = !_failed && _mat != null && (WantAO || WantMotionBlur);
            if (depth) _cam.depthTextureMode |= DepthTextureMode.Depth;
            else _cam.depthTextureMode &= ~DepthTextureMode.Depth;
        }

        private void Update()
        {
            UpdateRim();
            UpdateProbe();
        }

        // ------------------------------------------------------------------ character rim light by time of day

        private void UpdateRim()
        {
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            // Warm sky bounce by day, cool neon edge light at night.
            Color rim = Color.Lerp(new Color(1f, 0.86f, 0.66f, 0.1f), new Color(0.35f, 0.75f, 1f, 0.22f), night);
            if (Mathf.Abs(rim.r - _rimSent.r) + Mathf.Abs(rim.a - _rimSent.a) < 0.01f) return;
            _rimSent = rim;
            Mats.SetCharacterRim(rim);
        }

        // ------------------------------------------------------------------ live reflections

        private void UpdateProbe()
        {
            bool want = Settings.Graphics >= GraphicsPreset.High && Game.Layout != null;
            if (!want)
            {
                if (_probe != null) _probe.gameObject.SetActive(false);
                return;
            }
            if (_probe == null)
            {
                GameObject go = new GameObject("City Reflections");
                _probe = go.AddComponent<ReflectionProbe>();
                _probe.mode = ReflectionProbeMode.Realtime;
                _probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
                _probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                _probe.size = new Vector3(400f, 200f, 400f);
                _probe.boxProjection = false;
                _probe.importance = 10;
                _probe.hdr = true;
                _probe.shadowDistance = 0f;
                _probe.nearClipPlane = 0.5f;
                _probe.farClipPlane = 220f;
                _probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                _probe.cullingMask = Layers.WorldMask | (1 << Layers.Vehicles);
                _probe.intensity = 1f;
            }
            _probe.gameObject.SetActive(true);
            _probe.resolution = Settings.Graphics >= GraphicsPreset.Ultra ? 256 : 128;
            _probeTimer -= Time.unscaledDeltaTime;
            bool busy = _probeRender >= 0 && !_probe.IsFinishedRendering(_probeRender);
            if (_probeTimer <= 0f && !busy)
            {
                // Follow the camera at street height so puddles and car paint see the right neon; the probe only
                // moves between captures so its time-sliced faces always come from one spot.
                Vector3 c = transform.position;
                _probe.transform.position = new Vector3(c.x, WorldBounds.IsUnderground(c) ? c.y - 1.2f : Mathf.Max(c.y - 1.2f, 1.5f), c.z);
                _probeTimer = Settings.Graphics >= GraphicsPreset.Ultra ? 1.2f : 2.5f;
                _probeRender = _probe.RenderProbe();
            }
        }

        // ------------------------------------------------------------------ image effects

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_mat == null || _failed || !Settings.Bloom)
            {
                Graphics.Blit(src, dst);
                return;
            }
            GraphicsPreset q = Settings.Graphics;
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            int w = src.width, h = src.height;
            RenderTextureFormat hdr = src.format == RenderTextureFormat.ARGBHalf || src.format == RenderTextureFormat.DefaultHDR
                ? src.format : (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR) ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

            // ---- bloom: prefilter at half resolution, then down and back up a mip chain
            int levels = q == GraphicsPreset.Low ? 4 : (q == GraphicsPreset.Medium ? 5 : 6);
            float threshold = Mathf.Lerp(1.05f, 0.82f, night);
            _mat.SetVector(IdThreshold, new Vector4(threshold, threshold * 0.5f, 18f, 0f));
            _mat.SetFloat(IdScatter, 0.72f);
            int bw = Mathf.Max(1, w / 2), bh = Mathf.Max(1, h / 2);
            int count = 0;
            for (int i = 0; i < levels; i++)
            {
                if (bw < 4 || bh < 4) break;
                _mips[i] = RenderTexture.GetTemporary(bw, bh, 0, hdr);
                _mips[i].filterMode = FilterMode.Bilinear;
                if (i == 0) Graphics.Blit(src, _mips[0], _mat, PassPrefilter);
                else Graphics.Blit(_mips[i - 1], _mips[i], _mat, PassDown);
                count++;
                bw /= 2;
                bh /= 2;
            }
            for (int i = count - 1; i > 0; i--)
            {
                RenderTexture up = RenderTexture.GetTemporary(_mips[i - 1].width, _mips[i - 1].height, 0, hdr);
                up.filterMode = FilterMode.Bilinear;
                _mat.SetTexture(IdBloomBase, _mips[i - 1]);
                Graphics.Blit(_mips[i], up, _mat, PassUp);
                RenderTexture.ReleaseTemporary(_mips[i]);
                RenderTexture.ReleaseTemporary(_mips[i - 1]);
                _mips[i] = null;
                _mips[i - 1] = up;
            }
            RenderTexture bloom = count > 0 ? _mips[0] : null;
            _mips[0] = null;

            // ---- ambient occlusion at half resolution with a depth-aware blur
            Matrix4x4 proj = _cam.projectionMatrix;
            _mat.SetVector(IdProj, new Vector4(proj.m00, proj.m11, 0f, 0f));
            RenderTexture ao = null;
            if (WantAO)
            {
                RenderTextureFormat aoFmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf : RenderTextureFormat.ARGBHalf;
                int aw = q >= GraphicsPreset.Ultra ? w : w / 2, ah = q >= GraphicsPreset.Ultra ? h : h / 2;
                ao = RenderTexture.GetTemporary(aw, ah, 0, aoFmt, RenderTextureReadWrite.Linear);
                RenderTexture blur = RenderTexture.GetTemporary(aw, ah, 0, aoFmt, RenderTextureReadWrite.Linear);
                _mat.SetFloat(IdAOStrength, 1.15f);
                _mat.SetFloat(IdAORadius, 0.55f);
                Graphics.Blit(src, ao, _mat, PassAO);
                _mat.SetVector(IdBlurDir, new Vector4(1f, 0f, 0f, 0f));
                Graphics.Blit(ao, blur, _mat, PassAOBlur);
                _mat.SetVector(IdBlurDir, new Vector4(0f, 1f, 0f, 0f));
                Graphics.Blit(blur, ao, _mat, PassAOBlur);
                RenderTexture.ReleaseTemporary(blur);
                _mat.SetTexture(IdAOTex, ao);
                _mat.SetFloat(IdAOStrength, 1f);
            }
            else
            {
                _mat.SetTexture(IdAOTex, Texture2D.whiteTexture);
                _mat.SetFloat(IdAOStrength, 0f);
            }

            // ---- camera motion blur by reprojection
            // Reprojection works in the same OpenGL-style clip space the shader rebuilds positions in.
            Matrix4x4 viewProj = proj * _cam.worldToCameraMatrix;
            bool blurOn = WantMotionBlur && _hasPrev;
            _mat.SetMatrix(IdViewToWorld, _cam.cameraToWorldMatrix);
            _mat.SetMatrix(IdPrevViewProj, _prevViewProj);
            float fpsScale = Mathf.Clamp(Time.unscaledDeltaTime * 60f, 0.5f, 1.5f);
            _mat.SetFloat(IdMotionBlur, blurOn ? 0.45f / fpsScale : 0f);

            // ---- eye adaptation: measure the scene's average brightness and ease the exposure toward it
            UpdateAdaptation(src, night);

            // ---- grade: shared with the URP path through PostGrade (Outer City day and night, Frostline, Inner World)
            PostGrade.Values g = PostGrade.Compute(night, q);
            _mat.SetTexture(IdBloomTex, bloom != null ? (Texture)bloom : Texture2D.blackTexture);
            _mat.SetFloat(IdBloomIntensity, g.BloomIntensity);
            _mat.SetVector(IdBloomTint, g.BloomTint);
            _mat.SetFloat(IdExposure, g.Exposure);
            _mat.SetFloat(IdSaturation, g.Saturation);
            _mat.SetFloat(IdContrast, g.Contrast);
            _mat.SetVector(IdLift, g.Lift);
            _mat.SetVector(IdGamma, new Vector4(1f, 1f, 1f, 1f));
            _mat.SetVector(IdGain, g.Gain);
            _mat.SetVector(IdShadowTint, g.ShadowTint);
            _mat.SetVector(IdHighlightTint, g.HighlightTint);
            _mat.SetFloat(IdVignette, g.Vignette);
            _mat.SetFloat(IdChromatic, g.Chromatic);
            _mat.SetFloat(IdGrain, g.Grain);
            _mat.SetFloat(IdGrainTime, Time.unscaledTime % 10f);
            bool fxaa = QualitySettings.antiAliasing < 4 || !_cam.allowMSAA;
            _mat.SetFloat(IdSharpen, fxaa ? 0f : (q >= GraphicsPreset.High ? 0.22f : 0.12f));

            if (fxaa)
            {
                RenderTexture mid = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);
                Graphics.Blit(src, mid, _mat, PassComposite);
                _mat.SetFloat(IdSharpen, q >= GraphicsPreset.Medium ? 0.3f : 0.18f);
                Graphics.Blit(mid, dst, _mat, PassFXAA);
                RenderTexture.ReleaseTemporary(mid);
            }
            else Graphics.Blit(src, dst, _mat, PassComposite);

            if (bloom != null) RenderTexture.ReleaseTemporary(bloom);
            if (ao != null) RenderTexture.ReleaseTemporary(ao);
            _prevViewProj = viewProj;
            _hasPrev = true;
        }

        private void UpdateAdaptation(RenderTexture src, float night)
        {
            RenderTextureFormat fmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf) ? RenderTextureFormat.RGHalf
                : (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default);
            if (fmt == RenderTextureFormat.Default || !Settings.AutoExposure)
            {
                _mat.SetVector(IdAutoExposure, Vector4.zero);
                _mat.SetTexture(IdAdaptTex, Texture2D.blackTexture);
                _adaptValid = false;
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                if (_adapt[i] != null && _adapt[i].IsCreated()) continue;
                if (_adapt[i] != null) _adapt[i].Release();
                _adapt[i] = new RenderTexture(1, 1, 0, fmt, RenderTextureReadWrite.Linear) { name = "Eye Adaptation", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
                _adapt[i].Create();
                _adaptValid = false;
            }

            // Log luminance into a small target, then let its mip chain average it down to one texel.
            RenderTextureDescriptor d = new RenderTextureDescriptor(LumSize, LumSize, fmt, 0)
            {
                useMipMap = true,
                autoGenerateMips = false,
                sRGB = false
            };
            RenderTexture lum = RenderTexture.GetTemporary(d);
            lum.filterMode = FilterMode.Bilinear;
            Graphics.Blit(src, lum, _mat, PassLum);
            lum.GenerateMips();

            // Brighter scenes settle quickly (stepping out into the light); dark ones open up more slowly.
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.1f);
            float up = _adaptValid ? 1f - Mathf.Exp(-dt * 2.6f) : 1f;
            float down = _adaptValid ? 1f - Mathf.Exp(-dt * 1.1f) : 1f;
            _mat.SetTexture(IdLumTex, lum);
            _mat.SetVector(IdAdaptRate, new Vector4(up, down, LumTopMip, 0f));
            RenderTexture prev = _adapt[_adaptIndex], next = _adapt[1 - _adaptIndex];
            Graphics.Blit(prev, next, _mat, PassAdapt);
            _adaptIndex = 1 - _adaptIndex;
            _adaptValid = true;
            RenderTexture.ReleaseTemporary(lum);

            // Night aims lower than day so it still feels like night, just readable. Partial strength keeps a dark
            // alley darker than a lit plaza instead of flattening everything to the same brightness.
            float key = Mathf.Log(Mathf.Lerp(0.2f, 0.085f, night), 2f);
            _mat.SetTexture(IdAdaptTex, next);
            _mat.SetVector(IdAutoExposure, new Vector4(0.75f, key, -1.2f, Mathf.Lerp(0.6f, 2f, night)));
        }

        private void OnDisable()
        {
            _hasPrev = false;
            _adaptValid = false;
        }

        /// <summary>Forget the previous frame (camera cuts, teleports, cinematics) so motion blur does not streak.</summary>
        public void ResetHistory()
        {
            _hasPrev = false;
            _adaptValid = false;
            if (_urp != null) _urp.ResetHistory();
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (_probe != null) Destroy(_probe.gameObject);
            for (int i = 0; i < 2; i++) if (_adapt[i] != null) { _adapt[i].Release(); Destroy(_adapt[i]); _adapt[i] = null; }
        }
    }
}
