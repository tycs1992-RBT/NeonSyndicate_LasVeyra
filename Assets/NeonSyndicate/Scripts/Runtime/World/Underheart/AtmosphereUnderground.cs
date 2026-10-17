using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The Atmosphere's underground mode (04E). The Underheart world writes UndergroundTarget (0 on the street, 0.85 on the
    /// service level, 1 deeper) and UndergroundRegion every frame; this layer blends toward it after every other layer has
    /// been applied: the sun and moon go out, the ambient light and fog take the region's palette and density, rain, dust
    /// and snow stop (what was already falling is cleared once enclosed), reflections dim, and thunder and the weather
    /// ambience fall silent. The weather state, the clock and wetness are never touched, so leaving the Underheart, dying,
    /// loading or returning to the title restores the street sky on its own.
    /// </summary>
    public sealed partial class Atmosphere
    {
        /// <summary>0 on the street, 1 deep underground (written by the Underheart world from the player's cell).</summary>
        public float UndergroundTarget;
        /// <summary>The Underheart region whose palette the underground mood uses.</summary>
        public string UndergroundRegion = "";
        /// <summary>The smoothed underground blend the last frame used (0 street, 1 enclosed).</summary>
        public float UndergroundAmount { get { return _under; } }

        private float _under;
        private Color _uhSky = new Color(0.16f, 0.17f, 0.18f), _uhEquator = new Color(0.12f, 0.12f, 0.13f), _uhGround = new Color(0.07f, 0.07f, 0.075f);
        private Color _uhFog = new Color(0.08f, 0.085f, 0.09f);
        private float _uhDensity = 0.012f;
        private float _uhReflectBase = -1f, _uhReflectApplied = -1f;
        private bool _uhCleared;

        /// <summary>The region palettes: ambient sky, equator and ground light, fog colour and fog density (exponential squared).</summary>
        public static void UndergroundPalette(string region, out Color sky, out Color equator, out Color ground, out Color fog, out float density)
        {
            switch (region)
            {
                case UnderheartIds.LanternRefuge: // cool mineral light
                    sky = new Color(0.12f, 0.2f, 0.22f); equator = new Color(0.09f, 0.15f, 0.16f); ground = new Color(0.05f, 0.08f, 0.08f);
                    fog = new Color(0.06f, 0.11f, 0.12f); density = 0.010f; break;
                case UnderheartIds.EmberCanals: // ember glow off basalt
                    sky = new Color(0.22f, 0.12f, 0.07f); equator = new Color(0.17f, 0.09f, 0.05f); ground = new Color(0.12f, 0.05f, 0.025f);
                    fog = new Color(0.2f, 0.09f, 0.04f); density = 0.014f; break;
                case UnderheartIds.PyreGardens: // amber cavern, thin haze so the far wall reads
                    sky = new Color(0.24f, 0.15f, 0.07f); equator = new Color(0.16f, 0.1f, 0.05f); ground = new Color(0.1f, 0.05f, 0.02f);
                    fog = new Color(0.22f, 0.12f, 0.05f); density = 0.008f; break;
                case UnderheartIds.CrucibleWorkshops: // sooty iron
                    sky = new Color(0.14f, 0.12f, 0.11f); equator = new Color(0.11f, 0.09f, 0.08f); ground = new Color(0.07f, 0.05f, 0.04f);
                    fog = new Color(0.12f, 0.09f, 0.07f); density = 0.013f; break;
                case UnderheartIds.AshReliquary: // pale ash in the air
                    sky = new Color(0.2f, 0.2f, 0.21f); equator = new Color(0.15f, 0.15f, 0.16f); ground = new Color(0.09f, 0.09f, 0.09f);
                    fog = new Color(0.18f, 0.18f, 0.19f); density = 0.016f; break;
                case UnderheartIds.FoundryCrown: // red-orange heat, thin haze for the vast chamber
                    sky = new Color(0.3f, 0.12f, 0.05f); equator = new Color(0.2f, 0.08f, 0.03f); ground = new Color(0.14f, 0.04f, 0.015f);
                    fog = new Color(0.28f, 0.1f, 0.04f); density = 0.007f; break;
                default: // the service level: concrete and work lamps
                    sky = new Color(0.16f, 0.17f, 0.18f); equator = new Color(0.12f, 0.12f, 0.13f); ground = new Color(0.07f, 0.07f, 0.075f);
                    fog = new Color(0.08f, 0.085f, 0.09f); density = 0.012f; break;
            }
        }

        /// <summary>Jumps straight to the target blend and palette (teleports, loads, respawns, a new game).</summary>
        public void SnapUnderground04E()
        {
            _under = Mathf.Clamp01(UndergroundTarget);
            UndergroundPalette(UndergroundRegion, out _uhSky, out _uhEquator, out _uhGround, out _uhFog, out _uhDensity);
            if (_under > 0.5f) ClearWeatherParticles04E();
        }

        private void ClearWeatherParticles04E()
        {
            if (_rain != null) _rain.Clear(true);
            if (_dust != null) _dust.Clear(true);
            if (_snow != null) _snow.Clear(true);
            _uhCleared = true;
        }

        /// <summary>Runs at the end of Apply, after the district, rebirth and interior layers.</summary>
        private void ApplyUnderground04E()
        {
            float dt = Time.unscaledDeltaTime;
            _under = Mathf.MoveTowards(_under, Mathf.Clamp01(UndergroundTarget), dt * 0.7f);
            if (_under <= 0.0001f)
            {
                // Hand the reflections back to the sky (only if nothing else rewrote them meanwhile).
                if (_uhReflectApplied >= 0f && Mathf.Abs(RenderSettings.reflectionIntensity - _uhReflectApplied) < 0.0001f) RenderSettings.reflectionIntensity = _uhReflectBase;
                _uhReflectApplied = -1f;
                _uhCleared = false;
                return;
            }
            Color sky, equator, ground, fog;
            float density;
            UndergroundPalette(UndergroundRegion, out sky, out equator, out ground, out fog, out density);
            float k = 1f - Mathf.Exp(-dt * 1.6f);
            _uhSky = Color.Lerp(_uhSky, sky, k);
            _uhEquator = Color.Lerp(_uhEquator, equator, k);
            _uhGround = Color.Lerp(_uhGround, ground, k);
            _uhFog = Color.Lerp(_uhFog, fog, k);
            _uhDensity = Mathf.Lerp(_uhDensity, density, k);
            float u = _under;
            // The sun and the moon are gone well before the service level (shells only block them near the street).
            float cut = Mathf.Clamp01(u / 0.8f);
            if (Sun != null)
            {
                Sun.intensity *= 1f - cut;
                Sun.enabled = Sun.intensity > 0.01f;
            }
            if (Moon != null)
            {
                Moon.intensity *= 1f - cut;
                Moon.enabled = Moon.intensity > 0.01f;
            }
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, _uhSky, u);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, _uhEquator, u);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, _uhGround, u);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, _uhFog, u);
            RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, _uhDensity, u);
            // No weather falls underground; what was already falling is cleared once the player is enclosed.
            float keep = 1f - Mathf.Clamp01(u * 1.6f);
            if (_rain != null) { ParticleSystem.EmissionModule e = _rain.emission; e.rateOverTimeMultiplier *= keep; }
            if (_dust != null) { ParticleSystem.EmissionModule e = _dust.emission; e.rateOverTimeMultiplier *= keep; }
            if (_snow != null) { ParticleSystem.EmissionModule e = _snow.emission; e.rateOverTimeMultiplier *= keep; }
            if (u > 0.5f && !_uhCleared) ClearWeatherParticles04E();
            else if (u < 0.3f) _uhCleared = false;
            // Reflections from the sky probe would light wet metal underground with daylight.
            float cur = RenderSettings.reflectionIntensity;
            if (_uhReflectApplied < 0f || Mathf.Abs(cur - _uhReflectApplied) > 0.0001f) _uhReflectBase = cur;
            float want = _uhReflectBase * Mathf.Lerp(1f, 0.2f, u);
            if (Mathf.Abs(want - cur) > 0.0001f) RenderSettings.reflectionIntensity = want;
            _uhReflectApplied = want;
        }
    }
}
