using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// The camera's image pipeline (Built-in render pipeline, OnRenderImage), in this order: volumetrics (the
    /// NeonPost.Volumetrics partial), ambient occlusion (horizon based, half resolution, NeonPost.Ao), temporal
    /// anti-aliasing (NeonPost.Taa), depth of field for the flyover, cutscenes and the pause menu (NeonPost.Dof), HDR
    /// bloom through a mip chain (NeonPost.Bloom), eye adaptation, then one composite pass: chromatic aberration,
    /// motion blur (camera reprojection on Medium, per-object motion vectors on High and Ultra), bloom, ACES tone
    /// mapping with the shared PostGrade grade, split tone, vignette and film grain, and FXAA where no multisampling
    /// or temporal anti-aliasing runs. It also keeps a time-sliced live reflection probe around the camera so armour,
    /// cars and wet streets reflect the neon city. Each effect follows the graphics preset and the settings menu; if the
    /// shader is missing or unsupported the camera simply renders without post effects. The HUD and menus are drawn
    /// by IMGUI after this pass, so no effect here (grain, blur, depth of field) ever touches them.
    /// When the Universal Render Pipeline renders the camera (RenderPath.IsURP) OnRenderImage never runs: this component
    /// then only keeps the reflection probe and the character rim light, and adds NeonUrpPost (the URP volume stack).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed partial class NeonPost : MonoBehaviour
    {
        private const int PassPrefilter = 0, PassDown = 1, PassUp = 2, PassAO = 3, PassAOBlur = 4, PassComposite = 5, PassFXAA = 6, PassLum = 7, PassAdapt = 8;
        private const int PassTaa = 9, PassApplyAO = 10, PassDofCoc = 11, PassDofDir = 12, PassDofHex = 13, PassDofComposite = 14;
        private const int LumSize = 64, LumTopMip = 6;
        /// <summary>Beyond this eye depth the occlusion and the depth of field leave the image alone (sky, far city).</summary>
        private const float FarEffectLimit = 180f;

        private Camera _cam;
        private Material _mat;
        private bool _failed;
        private Matrix4x4 _prevViewProj;
        private bool _hasPrev;
        private ReflectionProbe _probe;
        private float _probeTimer;
        private int _probeRender = -1;
        private Color _rimSent = new Color(-1f, 0f, 0f, 0f);
        private readonly RenderTexture[] _adapt = new RenderTexture[2];
        private int _adaptIndex;
        private bool _adaptValid;
        private IRenderPathPost _urp;
        /// <summary>The camera's projection without the temporal jitter, captured each frame in OnPreCull.</summary>
        private Matrix4x4 _unjitteredProj;
        private int _frame;

        private static readonly int IdBloomTex = Shader.PropertyToID("_BloomTex");
        private static readonly int IdAOTex = Shader.PropertyToID("_AOTex");
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
        private static readonly int IdMotionBlurParams = Shader.PropertyToID("_MotionBlurParams");
        private static readonly int IdSharpen = Shader.PropertyToID("_Sharpen");
        private static readonly int IdProj = Shader.PropertyToID("_Proj");
        private static readonly int IdViewToWorld = Shader.PropertyToID("_ViewToWorld");
        private static readonly int IdPrevViewProj = Shader.PropertyToID("_PrevViewProj");
        private static readonly int IdLumTex = Shader.PropertyToID("_LumTex");
        private static readonly int IdAdaptTex = Shader.PropertyToID("_AdaptTex");
        private static readonly int IdAdaptRate = Shader.PropertyToID("_AdaptRate");
        private static readonly int IdAutoExposure = Shader.PropertyToID("_AutoExposure");
        private static readonly int IdPreExposure = Shader.PropertyToID("_PreExposure");

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

        private bool PostActive { get { return !_failed && _mat != null && Settings.Bloom; } }
        private bool WantAO { get { return PostActive && Settings.AmbientOcclusion && Settings.Graphics >= GraphicsPreset.High; } }
        private bool WantMotionBlur { get { return PostActive && Settings.MotionBlur && Settings.Graphics >= GraphicsPreset.Medium; } }
        /// <summary>High and Ultra blur from the motion vector texture (moving objects too); Medium reprojects the camera only.</summary>
        private bool WantObjectMotionBlur { get { return WantMotionBlur && Settings.Graphics >= GraphicsPreset.High && MotionVectorsSupported; } }
        private static bool MotionVectorsSupported { get { return SystemInfo.supportsMotionVectors && SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGHalf); } }

        private void OnPreCull()
        {
            if (_cam == null) return;
            _frame++;
            bool taa = WantTaa;
            bool vectors = taa || WantObjectMotionBlur;
            // The depth texture costs a depth pre-pass in forward rendering, so only ask for it when an effect reads it.
            bool depth = vectors || WantAO || WantMotionBlur || WantDof || WantVolumetrics;
            DepthTextureMode mode = _cam.depthTextureMode;
            if (depth) mode |= DepthTextureMode.Depth; else mode &= ~DepthTextureMode.Depth;
            if (vectors) mode |= DepthTextureMode.MotionVectors; else mode &= ~DepthTextureMode.MotionVectors;
            _cam.depthTextureMode = mode;
            ApplyJitter(taa);
        }

        private void OnPostRender()
        {
            // Gameplay (aim rays, screen to world) always sees the camera's clean projection: the jitter lives only
            // between OnPreCull and the end of this camera's rendering.
            ClearJitter();
        }

        private void Update()
        {
            UpdateRim();
            UpdateProbe();
            UpdateUnderground();
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

        /// <summary>Eases PostGrade.Underground toward where the camera is, so interiors get their adaptation headroom gently.</summary>
        private void UpdateUnderground()
        {
            float target = WorldBounds.IsUnderground(transform.position) ? 1f : 0f;
            PostGrade.Underground = Mathf.MoveTowards(PostGrade.Underground, target, Time.unscaledDeltaTime * 1.5f);
        }

        // ------------------------------------------------------------------ live reflections

        private void UpdateProbe()
        {
            // Phase 1's per-area probes replace this single camera probe whenever they are active.
            if (ReflectionProbes.Active)
            {
                if (_probe != null && _probe.gameObject.activeSelf) _probe.gameObject.SetActive(false);
                return;
            }
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
            if (!PostActive)
            {
                Graphics.Blit(src, dst);
                ReleaseHistory();
                return;
            }
            GraphicsPreset q = Settings.Graphics;
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            int w = src.width, h = src.height;
            RenderTextureFormat hdr = src.format == RenderTextureFormat.ARGBHalf || src.format == RenderTextureFormat.DefaultHDR
                ? src.format : (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.DefaultHDR) ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            PostGrade.Values g = PostGrade.Compute(night, q);
            Matrix4x4 proj = _unjitteredProj;
            _mat.SetVector(IdProj, new Vector4(proj.m00, proj.m11, 0f, 0f));

            // The image this frame works on. It starts as the camera's target and moves to our own textures as the
            // stages run; every stage reads the current one and never writes to the camera's target.
            RenderTexture scene = src;

            // ---- volumetric light and fog (Phase 1's partial; the texture it returns belongs to it)
            scene = ApplyVolumetrics(scene);

            // ---- ambient occlusion at half resolution; with temporal anti-aliasing it is applied before the resolve,
            // so the temporal filter also settles the occlusion's sampling noise
            bool taa = _taaThisFrame;
            RenderTexture ao = RenderAmbientOcclusion(scene, q, w, h, taa);
            RenderTexture aoApplied = null;
            if (ao != null && taa)
            {
                aoApplied = RenderTexture.GetTemporary(w, h, 0, hdr);
                aoApplied.filterMode = FilterMode.Bilinear;
                _mat.SetTexture(IdAOTex, ao);
                Graphics.Blit(scene, aoApplied, _mat, PassApplyAO);
                scene = aoApplied;
            }

            // ---- temporal anti-aliasing (the result is also next frame's history)
            if (taa)
            {
                RenderTexture resolved = ResolveTaa(scene, w, h, hdr);
                if (resolved != null) scene = resolved;
            }
            else ReleaseHistory();

            // ---- depth of field on the flyover, in cutscenes and behind the pause menu
            RenderTexture dof = RenderDepthOfField(scene, w, h, hdr);
            if (dof != null) scene = dof;

            // ---- bloom from the settled image
            RenderTexture bloom = RenderBloom(scene, q, g, w, h, hdr);

            // ---- occlusion straight in the composite when no temporal pass applied it
            if (ao != null && !taa)
            {
                _mat.SetTexture(IdAOTex, ao);
                _mat.SetFloat(IdAOStrength, 1f);
            }
            else
            {
                _mat.SetTexture(IdAOTex, Texture2D.whiteTexture);
                _mat.SetFloat(IdAOStrength, 0f);
            }

            // ---- motion blur: Medium reprojects the camera through the depth buffer (the player stays sharp by distance),
            // High and Ultra read the motion vector texture, so moving cars and people blur too
            SetupMotionBlur(q, proj);

            // ---- eye adaptation: measure the scene's average brightness and ease the exposure toward it
            UpdateAdaptation(scene, g);

            // ---- grade: shared with the URP path through PostGrade (Outer City day and night, Frostline, Inner World)
            _mat.SetTexture(IdBloomTex, bloom != null ? (Texture)bloom : Texture2D.blackTexture);
            _mat.SetFloat(IdBloomIntensity, g.BloomIntensity);
            _mat.SetVector(IdBloomTint, g.BloomTint);
            _mat.SetFloat(IdExposure, g.Exposure);
            _mat.SetFloat(IdPreExposure, PostGrade.AcesPreExposure);
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

            // ---- anti-aliasing: temporal on High and Ultra, else multisampling with a light sharpen, else FXAA
            bool msaa = QualitySettings.antiAliasing >= 4 && _cam.allowMSAA;
            bool fxaa = !taa && !msaa;
            float sharpen = taa ? (q >= GraphicsPreset.Ultra ? 0.3f : 0.24f) : (msaa ? (q >= GraphicsPreset.High ? 0.22f : 0.12f) : 0f);
            _mat.SetFloat(IdSharpen, sharpen);
            if (fxaa)
            {
                RenderTexture mid = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default);
                Graphics.Blit(scene, mid, _mat, PassComposite);
                _mat.SetFloat(IdSharpen, q >= GraphicsPreset.Medium ? 0.3f : 0.18f);
                Graphics.Blit(mid, dst, _mat, PassFXAA);
                RenderTexture.ReleaseTemporary(mid);
            }
            else Graphics.Blit(scene, dst, _mat, PassComposite);

            if (bloom != null) RenderTexture.ReleaseTemporary(bloom);
            if (dof != null) RenderTexture.ReleaseTemporary(dof);
            if (aoApplied != null) RenderTexture.ReleaseTemporary(aoApplied);
            if (ao != null) RenderTexture.ReleaseTemporary(ao);
            _prevViewProj = proj * _cam.worldToCameraMatrix;
            _hasPrev = true;
        }

        private void SetupMotionBlur(GraphicsPreset q, Matrix4x4 proj)
        {
            float scale = PostMath.MotionBlurScale(Time.unscaledDeltaTime);
            int mode = 0;
            if (WantObjectMotionBlur) mode = 2;
            else if (WantMotionBlur && _hasPrev) mode = 1;
            // Reprojection works in the same OpenGL-style clip space the shader rebuilds positions in, from the
            // unjittered projection so the temporal jitter never reads as motion.
            _mat.SetMatrix(IdViewToWorld, _cam.cameraToWorldMatrix);
            _mat.SetMatrix(IdPrevViewProj, _prevViewProj);
            int samples = q >= GraphicsPreset.Ultra ? 12 : 8;
            _mat.SetVector(IdMotionBlurParams, new Vector4(mode == 0 ? 0f : scale, mode, samples, 0.04f));
        }

        private void UpdateAdaptation(RenderTexture scene, PostGrade.Values g)
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
            Graphics.Blit(scene, lum, _mat, PassLum);
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

            // The target, limits and strength come from the shared grade, so URP adapts the same way.
            _mat.SetTexture(IdAdaptTex, next);
            _mat.SetVector(IdAutoExposure, new Vector4(g.AdaptStrength, g.AdaptKey, g.AdaptMinEv, g.AdaptMaxEv));
        }

        private void OnDisable()
        {
            _hasPrev = false;
            _adaptValid = false;
            _taaValid = false;
            ClearJitter();
        }

        /// <summary>Forget the previous frame (camera cuts, teleports, cinematics) so motion blur and the temporal history do not streak.</summary>
        public void ResetHistory()
        {
            _hasPrev = false;
            _adaptValid = false;
            _taaValid = false;
            if (_urp != null) _urp.ResetHistory();
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
            if (_probe != null) Destroy(_probe.gameObject);
            for (int i = 0; i < 2; i++) if (_adapt[i] != null) { _adapt[i].Release(); Destroy(_adapt[i]); _adapt[i] = null; }
            ReleaseHistory();
        }
    }
}
