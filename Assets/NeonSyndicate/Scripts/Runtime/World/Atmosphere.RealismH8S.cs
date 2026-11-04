using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// H8S visual realism, the atmosphere side (every number lives in RealismRulesH8S). ApplyRealismH8S runs once per
    /// Apply after the district and 04A ambience layers and before the interior shelter and the Underheart layers, so
    /// those still win where they apply. It cools and thickens the clear-day haze, softens the bright sky's sheen on every
    /// surface, warms the day sun a touch, hides the NeonSky horizon edge a little more and gives Unity's procedural sky
    /// (the fallback when NeonSky is off) a deeper, greyer day. Each value is recomputed from the frame's base state, so
    /// nothing accumulates across frames, and the Veil, the Frostline, weather and night keep their own looks.
    /// </summary>
    public sealed partial class Atmosphere
    {
        private static readonly int IdFallbackThickness = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int IdFallbackTint = Shader.PropertyToID("_SkyTint");
        private float _realismAmbientIntensity = -1f;
        private int _realismGamma = -1;

        /// <summary>True when the project renders in the gamma colour space (read once; it cannot change while playing).</summary>
        private bool RealismGammaSpace
        {
            get
            {
                if (_realismGamma < 0) _realismGamma = QualitySettings.activeColorSpace == ColorSpace.Gamma ? 1 : 0;
                return _realismGamma == 1;
            }
        }

        /// <summary>The night glow multiplier the NeonSky dome gets this frame (RealismRulesH8S.NightGlowScale in the gamma colour space).</summary>
        private float RealismNightGlow { get { return RealismGammaSpace ? RealismRulesH8S.NightGlowScale : 1f; } }

        /// <summary>The sun direction on the leaning H8S path for the clock's sun angle (clamped like the old light rotation).</summary>
        private static Vector3 RealismSunDirection(float sunAngle)
        {
            return RealismRulesH8S.SunDirection(Mathf.Clamp(sunAngle, -10f, 190f), SkyMath.SunYaw, RealismRulesH8S.SunPathTiltDegrees, RealismRulesH8S.SunPathSide);
        }

        /// <summary>
        /// Skybox ambient intensity that matches the Trilight sky colour (RealismRulesH8S.SkyAmbientIntensity), from the
        /// final Trilight sky colour of this frame and the dome's mean radiance (the sky model plus the night glow the
        /// shader adds), quantised so small drifts do not churn the setting.
        /// </summary>
        private float RealismSkyAmbientIntensity(float night)
        {
            Color glow = Color.Lerp(NightZenith, NightHorizon, 0.1f) * (Mathf.Clamp01(night) * (1f + _cloudAmount * 0.6f) * RealismNightGlow);
            Color dome = _skyMean + glow;
            float k = RealismRulesH8S.SkyAmbientIntensity(RenderSettings.ambientSkyColor, dome);
            k = Mathf.Round(k * 50f) / 50f;
            _realismAmbientIntensity = k;
            return k;
        }

        /// <summary>The Skybox ambient intensity chosen by the last frame (debug pages), or -1 before the first Skybox frame.</summary>
        public float RealismAmbientIntensity { get { return _realismAmbientIntensity; } }

        private void ApplyRealismH8S(float day, float cloud, float fog, float sand, float frost, float inner)
        {
            float clear = (1f - Mathf.Clamp01(cloud)) * (1f - Mathf.Clamp01(fog)) * (1f - Mathf.Clamp01(sand));
            float outer = (1f - Mathf.Clamp01(inner)) * (1f - Mathf.Clamp01(frost));

            // The gothic haze: a cooler, greyer fog colour and denser clear-day air (distance reads as depth).
            RenderSettings.fogColor = RealismRulesH8S.HazeColor(RenderSettings.fogColor, day, clear, inner, frost);
            RenderSettings.fogDensity += RealismRulesH8S.ExtraFogDensity(day, inner, frost);

            // The environment reflections (Apply only sets them when the night step changes, so this recomputes the whole value).
            RenderSettings.reflectionIntensity = Mathf.Lerp(Mathf.Lerp(0.12f, 1f, day), 0.5f, Mathf.Clamp01(inner)) * RealismRulesH8S.ReflectionScale(1f - day);

            // A touch of afternoon amber on the day sun (Apply rebuilds the colour every frame from the atmosphere model).
            if (Sun != null && Sun.enabled)
                Sun.color = Color.Lerp(Sun.color, RealismRulesH8S.WarmSun, RealismRulesH8S.SunWarmth * Mathf.Clamp01(day) * outer * clear);

            float w = Mathf.Clamp01(day) * outer * clear;
            if (NeonSkyActive)
            {
                // The gamma-correct dome keeps a deep night: its night glow and night cloud light are scaled down
                // (ApplyNeonSky set the base values earlier this frame).
                float glow = RealismNightGlow;
                if (glow < 1f)
                {
                    _neonSky.SetFloat(IdNight, NightFactor * glow);
                    float k = Mathf.Lerp(1f, glow, NightFactor);
                    _neonSky.SetColor(IdCloudColor, _neonSky.GetColor(IdCloudColor) * k);
                    _neonSky.SetColor(IdCloudShade, _neonSky.GetColor(IdCloudShade) * k);
                }
                // Hide the horizon edge a little more in the haze.
                if (w > 0.001f) _neonSky.SetFloat(IdHazeBlend, Mathf.Clamp01(_neonSky.GetFloat(IdHazeBlend) + RealismRulesH8S.SkyHazeBlendBoost * w));
                return;
            }
            if (w <= 0.001f) return;
            if (_sky != null)
            {
                // Unity's procedural sky: a little more air, a cooler tint and less exposure by day (Apply set the base values).
                _sky.SetFloat(IdFallbackThickness, _sky.GetFloat(IdFallbackThickness) + RealismRulesH8S.FallbackSkyThicknessAdd * w);
                _sky.SetColor(IdFallbackTint, Color.Lerp(_sky.GetColor(IdFallbackTint), RealismRulesH8S.FallbackSkyTint, 0.5f * w));
                _sky.SetFloat(IdExposure, _sky.GetFloat(IdExposure) * Mathf.Lerp(1f, RealismRulesH8S.FallbackSkyExposureScale, w));
            }
        }
    }
}
