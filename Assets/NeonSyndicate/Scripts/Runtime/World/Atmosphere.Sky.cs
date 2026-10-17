using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 1 lighting, the sky dome: creates the NeonSky material (Resources/NeonSky.shader, the physically based
    /// procedural sky) on Medium and up when Settings.ProceduralSky is on, otherwise Unity's own procedural skybox as
    /// before, and feeds it the sun and moon, the haze of the weather, the clouds, the night glow, the ground bounce
    /// colour, the Frostline and the Veil weight every frame. The same numbers go to the CPU side of the model in
    /// Atmosphere.Bounce.cs, so what the ambient light assumes and what the dome shows agree.
    /// </summary>
    public sealed partial class Atmosphere
    {
        /// <summary>True while the NeonSky shader draws the sky (false on Low, when turned off, or if the shader failed).</summary>
        public bool NeonSkyActive { get { return _neonSky != null && RenderSettings.skybox == _neonSky; } }

        private Material _neonSky;
        private bool _neonSkyTried;
        private float _starRotation;

        private static readonly int IdSunDir = Shader.PropertyToID("_SunDir");
        private static readonly int IdSunIrradiance = Shader.PropertyToID("_SunIrradiance");
        private static readonly int IdSunDisc = Shader.PropertyToID("_SunDisc");
        private static readonly int IdSunSize = Shader.PropertyToID("_SunSize");
        private static readonly int IdHaze = Shader.PropertyToID("_Haze");
        private static readonly int IdMieTint = Shader.PropertyToID("_MieTint");
        private static readonly int IdCamHeight = Shader.PropertyToID("_CamHeight");
        private static readonly int IdMoonDir = Shader.PropertyToID("_MoonDir");
        private static readonly int IdMoonPhase = Shader.PropertyToID("_MoonPhase");
        private static readonly int IdMoonDisc = Shader.PropertyToID("_MoonDisc");
        private static readonly int IdNight = Shader.PropertyToID("_Night");
        private static readonly int IdDark = Shader.PropertyToID("_Dark");
        private static readonly int IdNightZenith = Shader.PropertyToID("_NightZenith");
        private static readonly int IdNightHorizon = Shader.PropertyToID("_NightHorizon");
        private static readonly int IdStars = Shader.PropertyToID("_Stars");
        private static readonly int IdStarRotation = Shader.PropertyToID("_StarRotation");
        private static readonly int IdCloud = Shader.PropertyToID("_Cloud");
        private static readonly int IdCloudColor = Shader.PropertyToID("_CloudColor");
        private static readonly int IdCloudShade = Shader.PropertyToID("_CloudShade");
        private static readonly int IdCloudOctaves = Shader.PropertyToID("_CloudOctaves");
        private static readonly int IdWind = Shader.PropertyToID("_Wind");
        private static readonly int IdGroundColor = Shader.PropertyToID("_GroundColor");
        private static readonly int IdHazeBlend = Shader.PropertyToID("_HazeBlend");
        private static readonly int IdVeil = Shader.PropertyToID("_Veil");
        private static readonly int IdFlash = Shader.PropertyToID("_Flash");
        private static readonly int IdExposure = Shader.PropertyToID("_Exposure");

        /// <summary>The night sky's floor at the zenith (scene radiance): the city's glow on thin haze, never black.</summary>
        public static readonly Color NightZenith = new Color(0.06f, 0.06f, 0.12f);
        /// <summary>The night sky near the horizon: neon bouncing off the air and the clouds.</summary>
        public static readonly Color NightHorizon = new Color(0.16f, 0.15f, 0.3f);

        /// <summary>Picks the sky material for the preset and settings (NeonSky on Medium and up, Unity's procedural sky otherwise).</summary>
        private void ChooseSkyMaterial()
        {
            // Built-in only: the dome is a classic CG skybox pass, so the URP beta keeps Unity's procedural skybox.
            bool wantNeon = Settings.ProceduralSky && Settings.Graphics >= GraphicsPreset.Medium && !RenderPath.IsURP;
            if (wantNeon && _neonSky == null && !_neonSkyTried)
            {
                _neonSkyTried = true;
                try
                {
                    Shader sh = Resources.Load<Shader>("NeonSky");
                    if (sh == null) sh = Shader.Find("Skybox/NeonSky");
                    if (sh != null && sh.isSupported)
                    {
                        _neonSky = new Material(sh) { name = "Las Veyra Sky (NeonSky)", hideFlags = HideFlags.HideAndDontSave };
                    }
                    else Log.Warn("NeonSky shader missing or unsupported; the sky falls back to Unity's procedural skybox.");
                }
                catch (System.Exception e)
                {
                    Log.Exception("NeonSky", e);
                    _neonSky = null;
                }
            }
            Material want = wantNeon && _neonSky != null ? _neonSky : _sky;
            if (want != null && RenderSettings.skybox != want)
            {
                RenderSettings.skybox = want;
                RequestEnvironmentRefresh(true);
            }
        }

        /// <summary>Feeds the NeonSky material for this frame (the fallback material keeps its old block in Apply).</summary>
        private void ApplyNeonSky(float day, float cloud, float fog, float sand, float rain, float frost, float inner, float night, float dark)
        {
            if (!NeonSkyActive) return;
            Material m = _neonSky;
            float sunIntensityClear = Settings.PhysicalSun ? SkyMath.SunIntensity(SunElevation, 0f, Haze) : SunStrength;
            m.SetVector(IdSunDir, SunDir);
            // The dome scatters the full sun; clouds are drawn on top of it.
            m.SetFloat(IdSunIrradiance, SkyMath.SunIrradiance(SkyMath.SunPeakIntensity) * Mathf.Lerp(1f, 0.55f, inner));
            m.SetFloat(IdSunDisc, Mathf.Lerp(40f, 12f, cloud) * Mathf.Lerp(1f, 0.6f, inner));
            m.SetFloat(IdSunSize, Mathf.Lerp(0.0095f, 0.012f, frost) * Mathf.Lerp(1f, 1.8f, inner));
            m.SetFloat(IdHaze, Haze);
            m.SetColor(IdMieTint, Color.Lerp(Color.white, new Color(1f, 0.72f, 0.45f), sand * (1f - frost)));
            Vector3 at = FocusOverride ? FocusPoint : (Camera.main != null ? Camera.main.transform.position : Game.PlayerPosition);
            m.SetFloat(IdCamHeight, Mathf.Clamp(at.y, 1f, 3000f));
            m.SetVector(IdMoonDir, MoonDir);
            m.SetFloat(IdMoonPhase, SkyMath.MoonPhase(Day));
            m.SetFloat(IdMoonDisc, 2.2f * (1f - cloud * 0.8f));
            m.SetFloat(IdNight, night);
            m.SetColor(IdNightZenith, Color.Lerp(NightZenith, new Color(0.07f, 0.085f, 0.13f), frost * 0.5f));
            m.SetColor(IdNightHorizon, Color.Lerp(NightHorizon, new Color(0.14f, 0.17f, 0.26f), frost * 0.5f));
            m.SetFloat(IdDark, dark);
            m.SetFloat(IdStars, (1f - fog) * (1f - sand) * (1f - rain * 0.8f));
            // The star field turns once per day about the pole.
            _starRotation = Hours / 24f * Mathf.PI * 2f;
            m.SetFloat(IdStarRotation, _starRotation);
            float cover = Mathf.Clamp01(cloud * 1.05f + fog * 0.3f + sand * 0.5f);
            m.SetFloat(IdCloud, cover);
            float sinElev = Mathf.Sin(SunElevation * Mathf.Deg2Rad);
            Color cloudLit = SkyMath.CloudRadiance(Sun != null ? Sun.color : Color.white, sunIntensityClear, sinElev, _skyMean);
            cloudLit += (NightHorizon * 0.9f) * night;
            Color cloudShade = cloudLit * Mathf.Lerp(0.32f, 0.2f, rain);
            if (sand > 0f) { cloudLit = Color.Lerp(cloudLit, new Color(0.7f, 0.5f, 0.3f) * Mathf.Lerp(0.3f, 1f, day), sand); cloudShade = Color.Lerp(cloudShade, new Color(0.45f, 0.3f, 0.18f) * Mathf.Lerp(0.3f, 1f, day), sand); }
            m.SetColor(IdCloudColor, cloudLit);
            m.SetColor(IdCloudShade, cloudShade);
            m.SetFloat(IdCloudOctaves, Settings.Graphics >= GraphicsPreset.High ? 4f : 3f);
            m.SetFloat(IdWind, 1f + rain * 1.5f + sand * 3f);
            m.SetColor(IdGroundColor, GroundRadiance);
            m.SetFloat(IdHazeBlend, Mathf.Clamp01(0.55f + fog * 0.4f + sand * 0.4f + rain * 0.2f + frost * 0.2f));
            m.SetFloat(IdVeil, inner);
            m.SetFloat(IdFlash, _lightning * 1.5f);
            m.SetFloat(IdExposure, 1f);
        }

        private void DestroySkyMaterial()
        {
            if (_neonSky != null) { Destroy(_neonSky); _neonSky = null; }
        }
    }
}
