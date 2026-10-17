using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 1 lighting, volumetric light (Resources/NeonShafts.shader): radial light shafts from the sun by day and the
    /// moon on clear nights, seeded only by sky pixels (depth-based occlusion, so buildings and people cut the rays),
    /// blurred toward the light at quarter resolution, plus analytic height fog with a ground fog band at dawn and in
    /// the Frostline. NeonPost.OnRenderImage calls ApplyVolumetrics(src) once before the bloom pass; it returns src
    /// unchanged when the effect is off (Low and Medium, the Settings switches, URP, a missing shader or no depth
    /// texture), otherwise a texture this partial owns with the shafts and fog composited in. The depth texture is
    /// requested by NeonPost.OnPreCull for the occlusion and motion blur effects; when both are turned off this pass
    /// skips itself rather than read a stale depth buffer.
    /// </summary>
    public sealed partial class NeonPost
    {
        private const int ShaftPassMask = 0, ShaftPassRadial = 1, ShaftPassComposite = 2;

        private Material _volMat;
        private bool _volFailed;
        private RenderTexture _volOut;
        private float _volGroundY;
        private float _volGroundTimer;
        private static string _volStatus = "not run";

        private static readonly int IdLightUV = Shader.PropertyToID("_LightUV");
        private static readonly int IdShaftColor = Shader.PropertyToID("_ShaftColor");
        private static readonly int IdRadial = Shader.PropertyToID("_Radial");
        private static readonly int IdFogParams = Shader.PropertyToID("_FogParams");
        private static readonly int IdFogColor = Shader.PropertyToID("_FogColor");
        private static readonly int IdFogBand = Shader.PropertyToID("_FogBand");
        private static readonly int IdCamPos = Shader.PropertyToID("_CamPos");
        private static readonly int IdShaftTex = Shader.PropertyToID("_ShaftTex");
        private static readonly int IdMaxSeed = Shader.PropertyToID("_MaxSeed");
        private static readonly int IdVolProj = Shader.PropertyToID("_Proj");
        private static readonly int IdVolViewToWorld = Shader.PropertyToID("_ViewToWorld");

        /// <summary>Plain-language state of the last frame (debug pages).</summary>
        public static string VolumetricStatus { get { return _volStatus; } }

        /// <summary>True when the preset and settings ask for shafts or height fog on this render path.</summary>
        internal bool WantVolumetrics
        {
            get { return Settings.Bloom && !_failed && !RenderPath.IsURP && Settings.Graphics >= GraphicsPreset.High && (Settings.VolumetricLight || Settings.HeightFog); }
        }

        /// <summary>
        /// Adds light shafts and height fog to the frame. Returns src itself when the effect is off, otherwise a render
        /// texture owned by this component that stays valid until the next call.
        /// </summary>
        internal RenderTexture ApplyVolumetrics(RenderTexture src)
        {
            if (src == null || !WantVolumetrics || _cam == null) { _volStatus = "off"; return src; }
            if ((_cam.depthTextureMode & DepthTextureMode.Depth) == 0) { _volStatus = "no depth texture (occlusion and motion blur are both off)"; return src; }
            if (_volMat == null && !_volFailed)
            {
                try
                {
                    Shader sh = Resources.Load<Shader>("NeonShafts");
                    if (sh == null) sh = Shader.Find("Hidden/NeonSyndicate/Shafts");
                    if (sh == null || !sh.isSupported) { _volFailed = true; _volStatus = "shader missing"; return src; }
                    _volMat = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
                    // The material and the output texture are released with the camera object (NeonPost.cs keeps its
                    // own OnDestroy, so a small sibling component does the clean-up for this partial).
                    NeonPostVolumetricsOwner owner = gameObject.GetComponent<NeonPostVolumetricsOwner>();
                    if (owner == null) owner = gameObject.AddComponent<NeonPostVolumetricsOwner>();
                    owner.Post = this;
                }
                catch (System.Exception e)
                {
                    _volFailed = true;
                    _volStatus = "shader failed: " + e.Message;
                    return src;
                }
            }
            if (_volMat == null) return src;
            Atmosphere atmo = Game.Atmo;
            if (atmo == null) { _volStatus = "no atmosphere"; return src; }

            // ---- which light, and where it is on screen
            float under = atmo.UndergroundAmount;
            float night = atmo.NightFactor;
            float inner = atmo.InnerBlend;
            Vector3 lightDir = Vector3.zero;
            Color lightCol = Color.white;
            float strength = 0f, radius = 0.55f;
            bool shafts = Settings.VolumetricLight && under < 0.99f;
            if (shafts)
            {
                if (atmo.SunStrength > 0.02f && atmo.SunDir.y > -0.03f)
                {
                    lightDir = atmo.SunDir;
                    lightCol = atmo.Sun != null ? atmo.Sun.color : Color.white;
                    strength = (Settings.Graphics >= GraphicsPreset.Ultra ? 0.7f : 0.6f) * Mathf.Clamp01(atmo.SunStrength / 0.4f);
                }
                else if (night > 0.5f && atmo.MoonStrength > 0.02f && atmo.MoonDir.y > 0.02f && atmo.RainAmount < 0.5f)
                {
                    lightDir = atmo.MoonDir;
                    lightCol = atmo.Moon != null ? atmo.Moon.color : new Color(0.7f, 0.8f, 1f);
                    strength = 0.35f * Mathf.Clamp01((night - 0.5f) * 2f);
                    radius = 0.3f;
                }
                strength *= Mathf.Lerp(1f, 0.6f, inner) * (1f - under);
            }
            Vector4 lightUv = new Vector4(0.5f, 0.5f, 0f, radius);
            if (strength > 0.001f && lightDir.sqrMagnitude > 0.5f)
            {
                Vector3 vp = _cam.WorldToViewportPoint(_cam.transform.position + lightDir * 1000f);
                if (vp.z > 0f)
                {
                    float e = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f));
                    float on = 1f - QMath.SmoothRange(0.5f, 0.95f, e);
                    lightUv = new Vector4(vp.x, vp.y, on * strength, radius);
                }
            }
            bool drawShafts = lightUv.z > 0.001f;

            // ---- height fog parameters (metres), off underground and inside roofed venues
            UpdateGroundHeight();
            float shelter = atmo.InteriorShelter;
            float outdoors = (1f - under) * (1f - shelter);
            float rain = atmo.RainAmount, frost = atmo.FrostAmount;
            float elev = atmo.SunElevation;
            Vector4 fogParams = Vector4.zero, fogBand = Vector4.zero;
            if (Settings.HeightFog && outdoors > 0.001f)
            {
                float density = 0.00035f * (1f + rain * 0.8f) * (1f + night * 0.4f) * outdoors * Mathf.Lerp(1f, 0.5f, inner);
                fogParams = new Vector4(density, _volGroundY, 0.07f, Mathf.Min(Settings.ViewDistance, 2500f));
                // The dawn band rises from about -4 to 3 degrees of sun and is gone by 12; the Frostline keeps its own.
                float dawn = QMath.SmoothRange(-4f, 3f, elev) * (1f - QMath.SmoothRange(3f, 12f, elev));
                float band = dawn * 0.0045f * (1f - rain * 0.7f) * (1f - inner) + frost * 0.0035f;
                if (band > 0.00001f) fogBand = new Vector4(band * outdoors, _volGroundY + Mathf.Lerp(1.2f, 1.8f, frost), Mathf.Lerp(2.5f, 3.5f, frost), 0f);
            }
            bool drawFog = fogParams.x > 0f || fogBand.x > 0f;
            if (!drawShafts && !drawFog) { _volStatus = "idle"; return src; }

            // ---- render
            int w = src.width, h = src.height;
            if (_volOut == null || _volOut.width != w || _volOut.height != h || _volOut.format != src.format || !_volOut.IsCreated())
            {
                if (_volOut != null) { _volOut.Release(); Destroy(_volOut); }
                _volOut = new RenderTexture(w, h, 0, src.format) { name = "Volumetrics", hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear };
                _volOut.Create();
            }
            Matrix4x4 proj = _cam.projectionMatrix;
            _volMat.SetVector(IdVolProj, new Vector4(proj.m00, proj.m11, 0f, 0f));
            _volMat.SetMatrix(IdVolViewToWorld, _cam.cameraToWorldMatrix);
            _volMat.SetVector(IdCamPos, _cam.transform.position);
            _volMat.SetVector(IdLightUV, lightUv);
            _volMat.SetFloat(IdMaxSeed, 6f);
            RenderTexture shaft = null;
            if (drawShafts)
            {
                RenderTextureFormat fmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf) ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.Default;
                int qw = Mathf.Max(64, w / 4), qh = Mathf.Max(36, h / 4);
                shaft = RenderTexture.GetTemporary(qw, qh, 0, fmt);
                RenderTexture pong = RenderTexture.GetTemporary(qw, qh, 0, fmt);
                shaft.filterMode = FilterMode.Bilinear;
                pong.filterMode = FilterMode.Bilinear;
                Graphics.Blit(src, shaft, _volMat, ShaftPassMask);
                // Two radial passes: a short one that smooths the seed, then a long one that draws the rays.
                _volMat.SetVector(IdRadial, new Vector4(0.35f, 0.9f, 1f / 7.2f, 12f));
                Graphics.Blit(shaft, pong, _volMat, ShaftPassRadial);
                _volMat.SetVector(IdRadial, new Vector4(0.9f, 0.94f, 1f / 8.1f, 12f));
                Graphics.Blit(pong, shaft, _volMat, ShaftPassRadial);
                RenderTexture.ReleaseTemporary(pong);
                _volMat.SetTexture(IdShaftTex, shaft);
                Color tint = Color.Lerp(Color.white, SkyMath.NormalizeLuminance(lightCol), 0.5f);
                _volMat.SetColor(IdShaftColor, tint);
            }
            else
            {
                _volMat.SetTexture(IdShaftTex, Texture2D.blackTexture);
                _volMat.SetColor(IdShaftColor, Color.black);
            }
            _volMat.SetVector(IdFogParams, fogParams);
            _volMat.SetVector(IdFogBand, fogBand);
            _volMat.SetColor(IdFogColor, RenderSettings.fogColor);
            Graphics.Blit(src, _volOut, _volMat, ShaftPassComposite);
            if (shaft != null) RenderTexture.ReleaseTemporary(shaft);
            _volStatus = (drawShafts ? "shafts " + lightUv.z.ToString("0.00") : "no shafts") + (drawFog ? ", height fog " + (fogParams.x * 1000f).ToString("0.00") + (fogBand.x > 0f ? " + band" : "") : ", no fog");
            return _volOut;
        }

        /// <summary>The ground height under the camera (a short ray twice a second; the city floor when nothing is hit).</summary>
        private void UpdateGroundHeight()
        {
            _volGroundTimer -= Time.unscaledDeltaTime;
            if (_volGroundTimer > 0f) return;
            _volGroundTimer = 0.5f;
            Vector3 from = _cam.transform.position + Vector3.up * 2f;
            RaycastHit hit;
            if (Physics.Raycast(from, Vector3.down, out hit, 400f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) _volGroundY = hit.point.y;
            else _volGroundY = Mathf.Min(_volGroundY, from.y - 2f);
        }

        internal void ReleaseVolumetrics()
        {
            if (_volMat != null) { Destroy(_volMat); _volMat = null; }
            if (_volOut != null) { _volOut.Release(); Destroy(_volOut); _volOut = null; }
        }

        /// <summary>Clears the static status line (Enter Play Mode without domain reload).</summary>
        public static void ResetVolumetricStatics()
        {
            _volStatus = "not run";
        }
    }

    /// <summary>Releases the volumetrics material and texture when the camera object goes away.</summary>
    internal sealed class NeonPostVolumetricsOwner : MonoBehaviour
    {
        public NeonPost Post;

        private void OnDestroy()
        {
            if (Post != null) Post.ReleaseVolumetrics();
        }
    }
}
