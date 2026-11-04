using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Phase 1 lighting, bounce light. There is no baked global illumination in a city generated at runtime, so this is
    /// the closest approximation: the ambient light comes from the sky model itself (SkyMath.SkyAmbient evaluates the
    /// same scattering the dome draws in 17 directions), the ground term comes from the real ground albedo under the
    /// camera (SkyGround samples the layout: asphalt, sidewalk, grass, sand, snow, water) lit by the sun and the sky, and
    /// on High and Ultra the ambient switches to Unity's Skybox mode, where the engine projects the rendered dome (ground
    /// half included) into spherical harmonics with DynamicGI.UpdateEnvironment, throttled to real changes of the sun,
    /// the weather and the ground. Below High, and whenever the player is underground, the Trilight ambient carries the
    /// same colours. What this cannot do: light bouncing off one wall onto the next, shadowed courtyards staying darker
    /// than open streets, or neon signs lighting the facade across the road; those need light probes or a real GI solution.
    /// </summary>
    public sealed partial class Atmosphere
    {
        /// <summary>The ground albedo under the camera, smoothed over time.</summary>
        public Color GroundAlbedo { get { return _groundAlbedo; } }
        /// <summary>The ground's bounce radiance this frame (what the dome shows below the horizon).</summary>
        public Color GroundRadiance { get; private set; } = new Color(0.1f, 0.09f, 0.08f);
        /// <summary>The ground kind under the camera, for the debug page.</summary>
        public GroundKind GroundUnderCamera { get; private set; }
        /// <summary>True while the ambient light comes from the rendered sky dome (Skybox ambient mode).</summary>
        public bool SkyAmbientActive { get; private set; }
        /// <summary>Counts environment refreshes (sky probe and default reflection); probes re-render when it changes.</summary>
        public int EnvironmentStamp { get; private set; }
        /// <summary>How sheltered the camera is inside a roofed venue, 0 outdoors to 1 (04C), for the height fog.</summary>
        public float InteriorShelter { get { return _interiorShelter; } }

        private Color _groundAlbedo = new Color(0.085f, 0.085f, 0.09f);
        private Color _skyUpper = new Color(0.3f, 0.4f, 0.6f), _skyHorizon = new Color(0.5f, 0.55f, 0.65f), _skyMean = new Color(0.3f, 0.38f, 0.55f);
        private Vector3 _modelSun = Vector3.zero;
        private float _modelHaze = -1f, _modelTimer, _groundTimer;
        private float _envTimer;
        private int _envStamp = int.MinValue;
        private bool _envForce = true;

        /// <summary>Refreshes the CPU sky model when the sun or the haze moved (at most four times a second).</summary>
        private void UpdateSkyModel(bool force)
        {
            _modelTimer -= Time.unscaledDeltaTime;
            bool moved = Vector3.Dot(_modelSun, SunDir) < Mathf.Cos(0.5f * Mathf.Deg2Rad) || Mathf.Abs(_modelHaze - Haze) > 0.05f;
            if (!force && (!moved || _modelTimer > 0f)) return;
            _modelTimer = 0.25f;
            _modelSun = SunDir;
            _modelHaze = Haze;
            try
            {
                SkyMath.SkyAmbient(SunDir, SkyMath.SunIrradiance(SkyMath.SunPeakIntensity), Haze, out _skyUpper, out _skyHorizon, out _skyMean);
            }
            catch (System.Exception e)
            {
                Log.Exception("SkyModel", e);
            }
        }

        /// <summary>Samples the ground under the camera twice a second and eases the albedo toward it.</summary>
        private void UpdateGroundAlbedo(bool force)
        {
            _groundTimer -= Time.unscaledDeltaTime;
            Vector3 at = FocusOverride ? FocusPoint : (Camera.main != null ? Camera.main.transform.position : Game.PlayerPosition);
            if (force || _groundTimer <= 0f)
            {
                _groundTimer = 0.5f;
                Color target = SkyGround.SampleAlbedo(Game.Layout, at.x, at.z, 9f);
                GroundUnderCamera = SkyGround.Classify(Game.Layout, at.x, at.z);
                if (force) _groundAlbedo = target;
                else _groundAlbedo = Color.Lerp(_groundAlbedo, target, 0.35f);
            }
        }

        /// <summary>
        /// The Trilight ambient from the model: the upper sky, the horizon band and the ground bounce, with the clouds,
        /// the night glow, the Frostline and the Veil layered on in that order. Also writes GroundRadiance.
        /// </summary>
        private void ApplyModelAmbient(float day, float cloud, float frost, float inner, float night, out Color skyCol, out Color equator, out Color ground)
        {
            float sinElev = Mathf.Sin(SunElevation * Mathf.Deg2Rad);
            Color sunCol = Sun != null ? Sun.color : Color.white;
            float sunClear = Settings.PhysicalSun ? SkyMath.SunIntensity(SunElevation, 0f, Haze) : SunStrength / Mathf.Max(0.45f, 1f - cloud * 0.55f);
            // H8S realism: dark gothic stone bounces less light than the old pale streets (RealismRulesH8S.FillGain).
            float fillGain = RealismRulesH8S.FillGain(night);
            Color upper = _skyUpper * fillGain, horizon = _skyHorizon * fillGain, mean = _skyMean * fillGain;
            if (cloud > 0f)
            {
                Color cloudCol = SkyMath.CloudRadiance(sunCol, sunClear, sinElev, mean);
                upper = Color.Lerp(upper, cloudCol, cloud * 0.85f);
                horizon = Color.Lerp(horizon, cloudCol * 0.9f, cloud * 0.7f);
                mean = Color.Lerp(mean, cloudCol * 0.95f, cloud * 0.8f);
            }
            // The night's neon glow: the old Trilight night values, so the night keeps its look.
            float glowGain = 1f + cloud * 0.6f;
            upper += new Color(0.13f, 0.12f, 0.24f) * (night * glowGain);
            horizon += new Color(0.16f, 0.12f, 0.24f) * (night * glowGain);
            mean += new Color(0.1f, 0.09f, 0.18f) * (night * glowGain);
            // What lights the ground: the sun (or the moon), the sky and the street's own neon at night.
            Color glow = new Color(0.12f, 0.1f, 0.2f) * (night * 0.6f);
            if (Moon != null && Moon.enabled) glow += Moon.color * (Moon.intensity * Mathf.Max(0f, MoonDir.y));
            Color groundRad = SkyMath.GroundRadiance(_groundAlbedo, sunCol, SunStrength, sinElev, mean, glow);
            if (frost > 0f)
            {
                upper = Color.Lerp(upper, new Color(0.56f, 0.68f, 0.9f) * Mathf.Lerp(0.3f, 1f, day), frost * 0.35f);
                horizon = Color.Lerp(horizon, new Color(0.5f, 0.56f, 0.66f) * Mathf.Lerp(0.3f, 1f, day), frost * 0.4f);
            }
            if (inner > 0f)
            {
                // Inner World: darker violet light from every side.
                upper = Color.Lerp(upper, new Color(0.34f, 0.1f, 0.36f), inner);
                horizon = Color.Lerp(horizon, new Color(0.24f, 0.06f, 0.24f), inner);
                groundRad = Color.Lerp(groundRad, new Color(0.09f, 0.02f, 0.08f), inner);
            }
            upper.a = 1f; horizon.a = 1f; groundRad.a = 1f;
            GroundRadiance = groundRad;
            skyCol = upper;
            equator = horizon;
            ground = groundRad;
        }

        /// <summary>Asks for the sky probe and default reflection to be re-rendered at the next chance (force: this frame).</summary>
        public void RequestEnvironmentRefresh(bool force)
        {
            if (force) _envForce = true;
            _envStamp = int.MinValue;
        }

        /// <summary>
        /// Runs last in Apply: chooses the ambient mode (the rendered dome on High and up outdoors, Trilight otherwise) and
        /// re-renders the environment when the sun, weather, zone or ground changed, no more often than the preset allows.
        /// </summary>
        private void UpdateAmbientModeAndEnvironment(float cloud, float fog, float sand, float frost, float inner, float night)
        {
            bool wantSky = Settings.SkyAmbient && Settings.Graphics >= GraphicsPreset.High && NeonSkyActive && _under <= 0.001f;
            AmbientMode mode = wantSky ? AmbientMode.Skybox : AmbientMode.Trilight;
            if (RenderSettings.ambientMode != mode)
            {
                RenderSettings.ambientMode = mode;
                _envForce = true;
            }
            SkyAmbientActive = wantSky;
            // In Skybox mode the dome stays physical and the urban bounce gain rides on the ambient intensity.
            // H8S realism: the dome is gamma correct now, so its intensity matches the Trilight sky level (RealismSkyAmbientIntensity).
            if (wantSky) RenderSettings.ambientIntensity = RealismSkyAmbientIntensity(night) + _lightning * 1.5f;
            else if (RenderSettings.ambientIntensity != 1f) RenderSettings.ambientIntensity = 1f;

            // A stamp of everything the dome depends on, quantised so small drifts do not trigger a render.
            int stamp = Mathf.RoundToInt(SunElevation) * 1000003
                ^ Mathf.RoundToInt(cloud * 20f) * 7919
                ^ Mathf.RoundToInt(fog * 20f) * 104729
                ^ Mathf.RoundToInt(sand * 20f) * 1299709
                ^ Mathf.RoundToInt(frost * 20f) * 15485863
                ^ Mathf.RoundToInt(inner * 10f) * 32452843
                ^ Mathf.RoundToInt(night * 20f) * 49979687
                ^ Mathf.RoundToInt(SkyMath.Luminance(_groundAlbedo) * 50f) * 67867967
                ^ Mathf.RoundToInt(Haze * 10f) * 86028121
                ^ (NeonSkyActive ? 1 : 0);
            _envTimer -= Time.unscaledDeltaTime;
            float minInterval = Settings.Graphics >= GraphicsPreset.Ultra ? 0.5f : (Settings.Graphics >= GraphicsPreset.High ? 1.5f : 4f);
            bool changed = stamp != _envStamp;
            if (!_envForce && (!changed || _envTimer > 0f)) return;
            _envForce = false;
            _envStamp = stamp;
            _envTimer = minInterval;
            EnvironmentStamp++;
            try { DynamicGI.UpdateEnvironment(); }
            catch (System.Exception) { }
        }
    }
}
