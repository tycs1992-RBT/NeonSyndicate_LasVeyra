using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 1 lighting, the sun and moon: the sun's colour comes from the solar spectrum filtered by the atmosphere
    /// (SkyMath.SunColor, the same transmittance the sky shader uses for the disc), its strength from how much light the
    /// atmosphere lets through at that elevation (SkyMath.SunIntensity), and its shadows follow the graphics preset
    /// (soft shadows, resolution, cascades and distance from Settings.ApplyGraphics). The Frostline cools the light and
    /// the Veil keeps its low crimson sun, exactly as before. Settings.PhysicalSun turns the model off and restores the
    /// hand-tuned warm and noon colours.
    /// </summary>
    public sealed partial class Atmosphere
    {
        /// <summary>The sun's elevation in degrees for the current clock (negative at night).</summary>
        public float SunElevation { get; private set; }
        /// <summary>The apparent colour temperature of the sun light in Kelvin (for the debug page and the docs).</summary>
        public float SunKelvin { get; private set; } = SkyMath.SunKelvin;
        /// <summary>Unit vector toward the sun as lit (after the Veil's override).</summary>
        public Vector3 SunDir { get; private set; } = Vector3.up;
        /// <summary>Unit vector toward the moon.</summary>
        public Vector3 MoonDir { get; private set; } = Vector3.down;
        /// <summary>The sun light's intensity before the Underheart cut (0 at night).</summary>
        public float SunStrength { get; private set; }
        /// <summary>The moon light's intensity before the Underheart cut.</summary>
        public float MoonStrength { get; private set; }
        /// <summary>The haze factor handed to the sky model this frame (1 clear, higher in fog, rain, dust and snow).</summary>
        public float Haze { get; private set; } = 1f;

        private float _sunKelvinTimer;

        /// <summary>Places and colours the sun and moon for the clock, weather and zone weights. Returns the sun's elevation in degrees.</summary>
        private float ApplySunAndMoon(float sunAngle, float day, float dusk, float cloud, float frost, float inner, float fog, float rain, float sand)
        {
            // H8S realism: the sun follows a circle leaning RealismRulesH8S.SunPathTiltDegrees from the zenith (same sunrise
            // and sunset points, so night timing is unchanged); the light, the sky model and the dome all use this direction.
            float elevation = RealismRulesH8S.SunElevationDegrees(sunAngle, RealismRulesH8S.SunPathTiltDegrees);
            SunElevation = elevation;
            Haze = 1f + cloud * 0.8f + fog * 2.5f + sand * 3.5f + rain * 0.6f + frost * 0.5f;
            Vector3 sunDir = RealismSunDirection(sunAngle);
            if (Sun != null)
            {
                Quaternion sunRot = Quaternion.LookRotation(-sunDir, Vector3.up);
                float intensity;
                Color sunCol;
                if (Settings.PhysicalSun)
                {
                    intensity = SkyMath.SunIntensity(elevation, cloud, Haze);
                    sunCol = SkyMath.SunColor(elevation, Haze);
                }
                else
                {
                    intensity = Mathf.Lerp(0f, SkyMath.SunPeakIntensity, day) * (1f - cloud * 0.55f);
                    sunCol = Color.Lerp(new Color(1f, 0.95f, 0.86f), new Color(1f, 0.55f, 0.32f), dusk);
                }
                // Snow light is colder and whiter.
                if (frost > 0f) sunCol = Color.Lerp(sunCol, new Color(0.92f, 0.96f, 1f), frost * 0.45f);
                if (inner > 0f)
                {
                    // The Inner World keeps a low crimson sun at every hour of the day.
                    sunRot = Quaternion.Slerp(sunRot, Quaternion.Euler(14f, SkyMath.SunYaw, 0f), inner);
                    sunDir = Vector3.Slerp(sunDir, SkyMath.DirectionFromAngle(14f, SkyMath.SunYaw), inner).normalized;
                    intensity = Mathf.Lerp(intensity, 0.85f, inner);
                    sunCol = Color.Lerp(sunCol, new Color(1f, 0.2f, 0.26f), inner);
                }
                Sun.transform.rotation = sunRot;
                Sun.intensity = intensity;
                Sun.enabled = intensity > 0.01f;
                Sun.color = sunCol;
                Sun.shadowStrength = Mathf.Lerp(0.35f, 0.82f, 1f - cloud) * Mathf.Lerp(1f, 0.9f, inner);
                SunStrength = intensity;
                // The Kelvin readout is for people, so it only needs refreshing now and then.
                _sunKelvinTimer -= Time.unscaledDeltaTime;
                if (_sunKelvinTimer <= 0f)
                {
                    _sunKelvinTimer = 2f;
                    SunKelvin = SkyMath.ApparentKelvin(sunCol);
                }
            }
            SunDir = sunDir;
            if (Moon != null)
            {
                float moonAngle = Mathf.Repeat(sunAngle + 180f, 360f);
                float pitch = Mathf.Clamp(moonAngle, 10f, 170f);
                Moon.transform.rotation = Quaternion.Euler(pitch, 215f, 0f);
                MoonDir = SkyMath.DirectionFromAngle(pitch, 215f);
                float phase = SkyMath.MoonPhase(Day);
                // A full moon is about three times a half moon.
                float fullness = Mathf.Lerp(0.35f, 1f, 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 2f));
                Moon.intensity = Mathf.Lerp(0f, 0.48f, NightFactor) * (1f - cloud * 0.5f) * (1f - inner) * (Settings.PhysicalSun ? fullness : 1f);
                Moon.enabled = Moon.intensity > 0.01f;
                Moon.color = Settings.PhysicalSun ? SkyMath.NormalizeLuminance(SkyMath.KelvinToRgb(SkyMath.MoonKelvin)) * 0.9f : new Color(0.45f, 0.55f, 0.85f);
                MoonStrength = Moon.intensity;
            }
            return elevation;
        }

        /// <summary>Shadow quality on the sun light for the preset (called from Init and whenever the preset changes).</summary>
        public void ApplySunQuality()
        {
            if (Sun == null) return;
            GraphicsPreset q = Settings.Graphics;
            Sun.shadows = q == GraphicsPreset.Low ? LightShadows.Hard : LightShadows.Soft;
            Sun.shadowResolution = q >= GraphicsPreset.High ? UnityEngine.Rendering.LightShadowResolution.VeryHigh
                : (q == GraphicsPreset.Medium ? UnityEngine.Rendering.LightShadowResolution.High : UnityEngine.Rendering.LightShadowResolution.Medium);
            // A longer shadow distance needs a touch more bias to keep acne off the far cascades.
            Sun.shadowBias = q >= GraphicsPreset.Ultra ? 0.04f : 0.05f;
            Sun.shadowNormalBias = q >= GraphicsPreset.High ? 0.35f : 0.4f;
            Sun.shadowNearPlane = 0.2f;
        }
    }
}
