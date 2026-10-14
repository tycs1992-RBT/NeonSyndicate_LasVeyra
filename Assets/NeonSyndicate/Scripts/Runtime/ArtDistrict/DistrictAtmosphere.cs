using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// A restrained lighting accent for the demonstration street. Atmosphere calls Apply only after
    /// calculating the normal weather and realm lighting, so this component never accumulates a grade.
    /// There are no quality-setting, clock, camera, post-processing, gameplay or UI changes here.
    /// </summary>
    public sealed class DistrictAtmosphere : MonoBehaviour
    {
        private readonly Light[] _lights = new Light[4];
        private int _lightCount;
        private bool _initialized;

        /// <summary>
        /// Creates up to four facade accents at world-space anchors. Low quality uses emissive surfaces
        /// only, Medium permits two local lights, and High or Ultra permits four. No accent casts shadows.
        /// </summary>
        public void Init(Vector3[] anchors)
        {
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] == null) continue;
                _lights[i].enabled = false;
                Destroy(_lights[i].gameObject);
                _lights[i] = null;
            }
            _lightCount = anchors == null ? 0 : Mathf.Min(anchors.Length, _lights.Length);
            for (int i = 0; i < _lightCount; i++)
            {
                GameObject accent = new GameObject("Crystal district facade accent " + (i + 1));
                accent.transform.SetParent(transform, false);
                accent.transform.position = anchors[i];
                Light light = accent.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 12f;
                light.intensity = 0f;
                light.color = i % 2 == 0 ? new Color(0.22f, 0.73f, 1f) : new Color(0.58f, 0.30f, 1f);
                light.shadows = LightShadows.None;
                light.renderMode = LightRenderMode.Auto;
                light.bounceIntensity = 0f;
                light.enabled = false;
                _lights[i] = light;
            }
            _initialized = true;
        }

        /// <summary>
        /// Adds a geographically bounded desert-crystal tint to the freshly calculated atmosphere.
        /// Weather retains priority. The accent fades completely in the Frostline and the Inner World.
        /// Camera position also makes first person, third person, title flyovers and QA captures consistent.
        /// </summary>
        public void Apply(Atmosphere atmosphere)
        {
            if (!_initialized || atmosphere == null || !isActiveAndEnabled) return;
            Vector3 focus = atmosphere.FocusOverride ? atmosphere.FocusPoint :
                (Game.Cam != null ? Game.Cam.transform.position : Game.PlayerPosition);

            // A capsule around Core Avenue, with a gentle fade outside the dressed architecture.
            float dx = Mathf.Max(0f, Mathf.Abs(focus.x - 132f) - 65f);
            float distance = Mathf.Sqrt(dx * dx + focus.z * focus.z);
            float area = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35f, 110f, distance));
            // Starting blocks are resolved from the existing layout; the lighting follows their actual sites.
            Vector3[] starterAnchors = DistrictArchitecture.StarterAnchors();
            for (int a = 0; a < starterAnchors.Length; a++)
            {
                float localDistance = Mathf.Sqrt(MathUtil.FlatSqrDistance(focus, starterAnchors[a]));
                float localArea = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35f, 100f, localDistance));
                area = Mathf.Max(area, localArea);
            }
            area *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 100f, Mathf.Abs(focus.y)));
            float outer = (1f - Mathf.Clamp01(atmosphere.InnerBlend)) * (1f - Mathf.Clamp01(atmosphere.FrostAmount));
            float night = Mathf.Clamp01(atmosphere.NightFactor);

            float currentClear = atmosphere.Current == Weather.Clear ? 1f :
                (atmosphere.Current == Weather.Cloudy ? 0.35f : 0f);
            float targetClear = atmosphere.Target == Weather.Clear ? 1f :
                (atmosphere.Target == Weather.Cloudy ? 0.35f : 0f);
            float fairWeather = Mathf.Lerp(currentClear, targetClear, Mathf.Clamp01(atmosphere.WeatherBlend));
            float accentWeight = area * outer * fairWeather;

            if (accentWeight > 0.001f)
            {
                Color horizon = Color.Lerp(new Color(0.66f, 0.62f, 0.64f), new Color(0.07f, 0.085f, 0.19f), night);
                Color skyBounce = Color.Lerp(new Color(0.48f, 0.59f, 0.78f), new Color(0.075f, 0.12f, 0.24f), night);
                Color groundBounce = Color.Lerp(new Color(0.28f, 0.24f, 0.23f), new Color(0.025f, 0.036f, 0.065f), night);

                // Keep contrast and clear gameplay sightlines. The base fog still hides the far clip plane.
                RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, horizon, accentWeight * 0.36f);
                RenderSettings.fogDensity += accentWeight * Mathf.Lerp(0.00042f, 0.0007f, night);
                RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, skyBounce, accentWeight * 0.32f);
                RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, groundBounce, accentWeight * 0.30f);
                RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, horizon * 0.65f, accentWeight * 0.28f);

                // The owner resets sun colour every Apply, so warm direct light against cool indirect light
                // does not compound over frames or persist when the camera leaves the district.
                if (atmosphere.Sun != null)
                    atmosphere.Sun.color = Color.Lerp(atmosphere.Sun.color,
                        new Color(1f, 0.89f, 0.76f), accentWeight * (1f - night) * 0.18f);
            }

            int activeBudget = Settings.Graphics == GraphicsPreset.Low ? 0 :
                (Settings.Graphics == GraphicsPreset.Medium ? 2 : 4);
            for (int i = 0; i < _lightCount; i++)
            {
                Light light = _lights[i];
                if (light == null) continue;
                float viewDistance = Vector3.Distance(focus, light.transform.position);
                float near = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 90f, viewDistance));
                light.intensity = i < activeBudget ? near * outer * Mathf.Lerp(0.12f, 1.8f, night) : 0f;
                light.enabled = light.intensity > 0.025f;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _lightCount; i++)
                if (_lights[i] != null) _lights[i].enabled = false;

            // SetTime with the same value reapplies the owning atmosphere immediately. Its district hook
            // skips this disabled component, so even pausing or disabling it leaves no stale global tint.
            if (Game.Atmo != null && Game.Atmo.ArtDistrict == this && Game.Atmo.isActiveAndEnabled)
                Game.Atmo.SetTime(Game.Atmo.Hours);
        }
    }
}
