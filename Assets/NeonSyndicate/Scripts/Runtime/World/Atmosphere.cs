using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    public enum Weather
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Fog,
        Sandstorm
    }

    /// <summary>
    /// 24-hour day/night cycle, sky, sun/moon, fog and weather (rain, storms with lightning, fog, desert sandstorms).
    /// One in-game hour lasts one real minute by default. Two layers sit on top of the base sky: the Frostline cold
    /// (colder light and fog, snowfall instead of rain, blending in with depth into the zone) and the Inner World
    /// override (red-violet sky, a low crimson sun, violet fog and ambient), which follows InnerWorld.Blend.
    /// </summary>
    public sealed partial class Atmosphere : MonoBehaviour
    {
        public float Hours = 15.5f;
        public float SecondsPerHour = 120f; // 2 real seconds per in-game minute
        public bool Frozen;
        public Weather Current = Weather.Clear;
        public Weather Target = Weather.Clear;
        public float WeatherBlend = 1f;
        public DistrictAtmosphere ArtDistrict;
        public Light Sun;
        public Light Moon;
        public int Day = 1;
        /// <summary>QA captures: when true the Frostline cold is measured at FocusPoint (the capture camera), not at the player.</summary>
        public bool FocusOverride;
        public Vector3 FocusPoint;

        private Material _sky;
        private ParticleSystem _rain;
        private ParticleSystem _dust;
        private ParticleSystem _snow;
        private float _frost;
        private float _lastInner = -1f;
        private float _lastInnerGi = -1f;
        private float _lastSnowStorm = -1f;
        private float _weatherTimer = 240f;
        private float _lightning;
        private float _nextLightning = 10f;
        private float _rainAmount;
        private float _fogAmount;
        private float _sandAmount;
        private float _cloudAmount;
        private float _lastNight = -1f;
        private float _lastWet = -1f;
        /// <summary>1 in the Outer City, 0 fully inside the Veil (1 - InnerWorld.Blend, kept from the last Apply).</summary>
        private float _dry = 1f;
        private Transform _followTarget;
        private bool _interiorRoofed;
        private float _interiorShelter;

        public float NightFactor { get; private set; }
        public bool IsNight { get { return NightFactor > 0.55f; } }
        public float RainAmount { get { return _rainAmount; } }
        /// <summary>How deep into the Frostline the player is, 0..1 (smoothed). Also written to PostGrade.Frost.</summary>
        public float FrostAmount { get { return _frost; } }
        /// <summary>The Inner World override weight used by the last frame (InnerWorld.Blend).</summary>
        public float InnerBlend { get; private set; }
        public string ClockText { get { return MathUtil.Clock(Hours); } }

        public void Init()
        {
            GameObject sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 0.8f;
            Sun.shadowBias = 0.05f;
            Sun.shadowNormalBias = 0.4f;
            ApplySunQuality();
            GameObject moonGo = new GameObject("Moon");
            moonGo.transform.SetParent(transform, false);
            Moon = moonGo.AddComponent<Light>();
            Moon.type = LightType.Directional;
            Moon.shadows = LightShadows.None;
            Moon.color = new Color(0.45f, 0.55f, 0.85f);

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                _sky = new Material(skyShader);
                _sky.name = "Las Veyra Sky";
                RenderSettings.skybox = _sky;
            }
            RenderSettings.sun = Sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            // Phase 1 lighting: the physically based dome (Medium and up) and the per-area reflection probes.
            ChooseSkyMaterial();
            try { ReflectionProbes.Create(transform); }
            catch (System.Exception e) { Log.Exception("ReflectionProbes", e); }
            try { SkyLightingDebug.Register(); }
            catch (System.Exception e) { Log.Exception("SkyLightingDebug", e); }
            BuildParticles();
            Apply(true);
        }

        /// <summary>The graphics preset changed: sun shadows, the sky material and the ambient mode follow it.</summary>
        public void OnGraphicsApplied()
        {
            ApplySunQuality();
            ChooseSkyMaterial();
            RequestEnvironmentRefresh(true);
        }

        private void OnDestroy()
        {
            DestroySkyMaterial();
        }

        public void SetFollowTarget(Transform t)
        {
            _followTarget = t;
        }

        private void BuildParticles()
        {
            _rain = MakeSystem("Rain", Mats.Rain, 4000, 0.9f, 22f);
            ParticleSystem.MainModule m = _rain.main;
            m.startSpeed = 0f;
            m.startSize3D = true;
            m.startSizeX = 0.02f;
            m.startSizeY = 0.7f;
            m.startSizeZ = 0.02f;
            ParticleSystem.VelocityOverLifetimeModule v = _rain.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(-1f, 1f);
            v.y = new ParticleSystem.MinMaxCurve(-24f, -20f);
            v.z = new ParticleSystem.MinMaxCurve(-1f, 1f);
            ParticleSystemRenderer rr = _rain.GetComponent<ParticleSystemRenderer>();
            rr.renderMode = ParticleSystemRenderMode.Stretch;
            rr.velocityScale = 0.04f;
            rr.lengthScale = 1f;

            _dust = MakeSystem("Sandstorm Dust", Mats.Dust, 1500, 4f, 35f);
            ParticleSystem.MainModule dm = _dust.main;
            dm.startSize = new ParticleSystem.MinMaxCurve(2f, 6f);
            dm.startSpeed = 0f;
            ParticleSystem.VelocityOverLifetimeModule dv = _dust.velocityOverLifetime;
            dv.enabled = true;
            dv.space = ParticleSystemSimulationSpace.World;
            dv.x = new ParticleSystem.MinMaxCurve(10f, 18f);
            dv.y = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            dv.z = new ParticleSystem.MinMaxCurve(3f, 8f);

            // Frostline snowfall: soft flakes drifting down around the camera, with a little flutter.
            _snow = MakeSystem("Snowfall", Mats.Snowflake, 6000, 9f, 26f);
            ParticleSystem.MainModule sm = _snow.main;
            sm.startSpeed = 0f;
            sm.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            ParticleSystem.NoiseModule sn = _snow.noise;
            sn.enabled = true;
            sn.strength = 0.45f;
            sn.frequency = 0.35f;
            sn.scrollSpeed = 0.2f;
            sn.octaveCount = 1;
            sn.quality = ParticleSystemNoiseQuality.Low;
            SetSnowWind(0f);
        }

        /// <summary>Snowflake velocity: a slow fall with light drift, driven sideways by storm wind.</summary>
        private void SetSnowWind(float storm)
        {
            if (_snow == null) return;
            ParticleSystem.VelocityOverLifetimeModule v = _snow.velocityOverLifetime;
            v.enabled = true;
            v.space = ParticleSystemSimulationSpace.World;
            v.x = new ParticleSystem.MinMaxCurve(-0.6f + storm * 4f, 0.9f + storm * 8f);
            v.y = new ParticleSystem.MinMaxCurve(-2.2f - storm * 1.5f, -1.2f - storm * 0.8f);
            v.z = new ParticleSystem.MinMaxCurve(-0.6f + storm * 1.5f, 0.6f + storm * 3f);
            _lastSnowStorm = storm;
        }

        private ParticleSystem MakeSystem(string name, Material mat, int max, float lifetime, float radius)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule m = ps.main;
            m.loop = true;
            m.maxParticles = max;
            m.startLifetime = lifetime;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.playOnAwake = false;
            ParticleSystem.EmissionModule e = ps.emission;
            e.rateOverTime = 0f;
            ParticleSystem.ShapeModule s = ps.shape;
            s.shapeType = ParticleSystemShapeType.Box;
            s.scale = new Vector3(radius * 2f, 1f, radius * 2f);
            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        public void SetTime(float hours)
        {
            Hours = Mathf.Repeat(hours, 24f);
            Apply(true);
        }

        /// <summary>Skips time forward (sleeping): pays property income for every 06:00 passed and advances the day.</summary>
        public void AdvanceHours(float hours)
        {
            float before = Hours, after = Hours + Mathf.Max(0f, hours);
            for (float six = 6f; six <= after; six += 24f)
                if (six > before && Game.Economy != null) Game.Economy.OnNewDay();
            Day += Mathf.FloorToInt(after / 24f);
            SetTime(after);
        }

        public void SetWeather(Weather w, bool instant)
        {
            Target = w;
            if (instant)
            {
                Current = w;
                WeatherBlend = 1f;
            }
            else
            {
                WeatherBlend = 0f;
            }
            _weatherTimer = Random.Range(300f, 600f);
        }

        private void Update()
        {
            if (!Frozen)
            {
                float before = Hours;
                Hours += Time.deltaTime / Mathf.Max(1f, SecondsPerHour);
                if (Hours >= 24f)
                {
                    Hours -= 24f;
                    Day++;
                }
                if (before < 6f && Hours >= 6f && Game.Economy != null) Game.Economy.OnNewDay();
            }
            Vector3 at = FocusOverride ? FocusPoint : Game.PlayerPosition;
            _frost = Mathf.MoveTowards(_frost, CityLayout.FrostAmount(at.x, at.z), Time.unscaledDeltaTime * 0.6f);
            PostGrade.Frost = _frost;
            _weatherTimer -= Time.deltaTime;
            if (_weatherTimer <= 0f) RollWeather();
            if (WeatherBlend < 1f)
            {
                WeatherBlend = Mathf.MoveTowards(WeatherBlend, 1f, Time.deltaTime / 25f);
                if (WeatherBlend >= 1f) Current = Target;
            }
            Apply(false);
            FollowCamera();
            UpdateLightning();
        }

        private void RollWeather()
        {
            Vector3 p = Game.PlayerPosition;
            District zone = CityLayout.DistrictAt(p);
            bool desert = zone == District.QuartzBadlands;
            float r = Random.value;
            Weather w;
            if (r < 0.45f) w = Weather.Clear;
            else if (r < 0.65f) w = Weather.Cloudy;
            else if (r < 0.78f) w = Weather.Rain;
            else if (r < 0.85f) w = Weather.Storm;
            else if (r < 0.93f) w = Weather.Fog;
            else w = desert ? Weather.Sandstorm : (zone == District.Frostline ? Weather.Storm : Weather.Cloudy);
            SetWeather(w, false);
        }

        private static float Weight(Weather w, Weather current, Weather target, float blend)
        {
            float a = current == w ? 1f : 0f;
            float b = target == w ? 1f : 0f;
            return Mathf.Lerp(a, b, blend);
        }

        private void Apply(bool force)
        {
            float rain = Weight(Weather.Rain, Current, Target, WeatherBlend) + Weight(Weather.Storm, Current, Target, WeatherBlend);
            float storm = Weight(Weather.Storm, Current, Target, WeatherBlend);
            float fog = Weight(Weather.Fog, Current, Target, WeatherBlend);
            float sand = Weight(Weather.Sandstorm, Current, Target, WeatherBlend);
            float cloud = Mathf.Clamp01(Weight(Weather.Cloudy, Current, Target, WeatherBlend) * 0.6f + rain * 0.9f + fog * 0.5f);
            _rainAmount = rain;
            _fogAmount = fog;
            _sandAmount = sand;
            _cloudAmount = cloud;
            float frost = _frost;
            float inner = Game.Inner != null ? Game.Inner.Blend : 0f;
            InnerBlend = inner;

            float sunAngle = (Hours / 24f) * 360f - 90f; // 6:00 = 0 deg (horizon), 12:00 = 90 deg
            float elevation = Mathf.Sin(sunAngle * Mathf.Deg2Rad);
            float day = Mathf.Clamp01(elevation * 3f + 0.15f);
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(elevation) * 4f) * (elevation > -0.3f ? 1f : 0f);
            NightFactor = 1f - day;

            // Phase 1 lighting: the sun's colour and strength come from the atmosphere model (Atmosphere.Sun.cs), the
            // ambient from the same model plus the ground under the camera (Atmosphere.Bounce.cs).
            ApplySunAndMoon(sunAngle, day, dusk, cloud, frost, inner, fog, rain, sand);
            // The neon glow follows the game's NightFactor (lamps and signs are on by sunset); stars and the moon wait
            // for the darker astronomical night.
            float night = NightFactor;
            float dark = SkyMath.NightFactor(SunElevation);
            UpdateSkyModel(force);
            UpdateGroundAlbedo(force);
            Color duskCol = new Color(0.85f, 0.42f, 0.35f);
            Color skyCol, equator, ground;
            ApplyModelAmbient(day, cloud, frost, inner, night, out skyCol, out equator, out ground);
            RenderSettings.ambientSkyColor = skyCol;
            RenderSettings.ambientEquatorColor = equator;
            RenderSettings.ambientGroundColor = ground;

            // The fog takes the colour of the horizon sky by day (so distance fades into the dome) and the old night
            // violet-blue after dark, before the weather and the zones recolour it.
            Color fogDay = Color.Lerp(new Color(0.66f, 0.72f, 0.8f), equator, 0.7f);
            Color fogCol = Color.Lerp(new Color(0.06f, 0.055f, 0.1f), fogDay, day);
            fogCol = Color.Lerp(fogCol, duskCol * 0.8f, dusk * 0.25f * (1f - cloud));
            fogCol = Color.Lerp(fogCol, new Color(0.45f, 0.46f, 0.5f) * Mathf.Lerp(0.2f, 1f, day), Mathf.Max(fog, rain * 0.6f));
            fogCol = Color.Lerp(fogCol, new Color(0.7f, 0.5f, 0.3f) * Mathf.Lerp(0.3f, 1f, day), sand * (1f - frost));
            if (frost > 0f) fogCol = Color.Lerp(fogCol, new Color(0.7f, 0.78f, 0.88f) * Mathf.Lerp(0.14f, 1f, day), frost * 0.7f);
            if (inner > 0f) fogCol = Color.Lerp(fogCol, new Color(0.3f, 0.07f, 0.3f), inner);
            RenderSettings.fogColor = fogCol;
            float density = 0.00085f + NightFactor * 0.0006f + rain * 0.0016f + fog * 0.0065f + sand * 0.011f * (1f - frost);
            // Snow air is a little thicker; a snowstorm becomes a whiteout.
            density += frost * (0.0009f + rain * 0.0022f);
            // The Inner World's violet haze: thick enough to feel enclosed, thin enough to show the far islands.
            density = Mathf.Lerp(density, 0.0017f + rain * 0.0004f, inner);
            // Never thinner than what hides the far clip plane, so the skyline fades out instead of popping in.
            RenderSettings.fogDensity = Mathf.Max(density, 2.1f / Mathf.Max(100f, Settings.ViewDistance));

            ApplyNeonSky(day, cloud, fog, sand, rain, frost, inner, night, dark);
            if (_sky != null && !NeonSkyActive)
            {
                float thickness = Mathf.Lerp(0.72f, 1.4f, dusk) + cloud * 0.3f;
                Color tint = Color.Lerp(new Color(0.44f, 0.52f, 0.68f), new Color(0.35f, 0.36f, 0.4f), cloud);
                Color skyGround = Color.Lerp(new Color(0.08f, 0.07f, 0.1f), new Color(0.42f, 0.36f, 0.3f), day);
                float exposure = Mathf.Lerp(0.18f, 1.25f, day) * (1f - cloud * 0.35f) + _lightning * 2f;
                if (frost > 0f)
                {
                    tint = Color.Lerp(tint, new Color(0.5f, 0.58f, 0.7f), frost * 0.4f);
                    skyGround = Color.Lerp(skyGround, new Color(0.6f, 0.64f, 0.7f) * Mathf.Lerp(0.2f, 1f, day), frost * 0.6f);
                }
                if (inner > 0f)
                {
                    // A red-violet sky: heavy scattering, crimson tint, dark plum horizon.
                    thickness = Mathf.Lerp(thickness, 2.4f, inner);
                    tint = Color.Lerp(tint, new Color(0.62f, 0.12f, 0.4f), inner);
                    skyGround = Color.Lerp(skyGround, new Color(0.12f, 0.02f, 0.1f), inner);
                    exposure = Mathf.Lerp(exposure, 0.85f, inner);
                }
                _sky.SetFloat("_SunSize", 0.045f);
                _sky.SetFloat("_AtmosphereThickness", thickness);
                _sky.SetColor("_SkyTint", tint);
                _sky.SetColor("_GroundColor", skyGround);
                _sky.SetFloat("_Exposure", exposure);
            }

            float nf = Mathf.Round(NightFactor * 50f) / 50f;
            float iq = Mathf.Round(inner * 25f) / 25f;
            bool nightChanged = force || Mathf.Abs(nf - _lastNight) > 0.001f;
            if (nightChanged || Mathf.Abs(iq - _lastInner) > 0.001f)
            {
                _lastNight = nf;
                _lastInner = iq;
                // Inside the Veil windows, signs and lamps glow as if it were night, and the lamps turn violet.
                Mats.SetLampTint(iq);
                Mats.SetNightFactor(Mathf.Max(Mathf.Clamp01(NightFactor + cloud * 0.25f), iq * 0.85f));
                // Keep reflections (glass, car paint, wet roads) in step with the runtime sky.
                RenderSettings.reflectionIntensity = Mathf.Lerp(Mathf.Lerp(0.12f, 1f, day), 0.5f, iq);
                // The environment probe itself is refreshed by UpdateAmbientModeAndEnvironment (Atmosphere.Bounce.cs),
                // which watches the sun, the weather, the zones and the ground together; a Veil step forces it.
                if (Mathf.Abs(iq - _lastInnerGi) >= 0.2f || ((iq <= 0f || iq >= 1f) && iq != _lastInnerGi))
                {
                    _lastInnerGi = iq;
                    RequestEnvironmentRefresh(true);
                }
            }
            // In the Frostline rain falls as snow; the Inner World is dry: no wet roads, rain, hiss, thunder or lightning.
            float dry = 1f - inner;
            _dry = dry;
            float wet = Mathf.Round(rain * (1f - frost) * dry * 25f) / 25f;
            if (force || Mathf.Abs(wet - _lastWet) > 0.001f)
            {
                _lastWet = wet;
                Mats.SetWetness(wet);
            }

            if (_rain != null)
            {
                ParticleSystem.EmissionModule e = _rain.emission;
                e.rateOverTime = rain * (1500f + storm * 1500f) * (1f - frost) * dry;
            }
            if (_dust != null)
            {
                ParticleSystem.EmissionModule e = _dust.emission;
                e.rateOverTime = sand * 300f * (1f - frost) * dry;
            }
            if (_snow != null)
            {
                ParticleSystem.EmissionModule e = _snow.emission;
                e.rateOverTime = frost * (220f + rain * 1300f + storm * 900f) * dry;
                if (Mathf.Abs(storm - _lastSnowStorm) > 0.02f) SetSnowWind(storm);
            }
            // Snow is quiet: no rain hiss, but wind that rises with the storm. The Veil (and the Underheart, 04E) silences the weather.
            float heard = dry * (1f - _under);
            if (Game.Audio != null) Game.Audio.SetWeatherAmbience(rain * (1f - frost) * heard, storm * (1f - frost * 0.5f) * heard, Mathf.Max(sand * (1f - frost), frost * (0.25f + rain * 0.55f)) * heard);
            if (ArtDistrict != null && ArtDistrict.isActiveAndEnabled) ArtDistrict.Apply(this);
            VisualRebirth04AAmbience.ApplyAtmosphere(this);
            ApplyInteriorShelter04C();
            ApplyUnderground04E();
            UpdateAmbientModeAndEnvironment(cloud, fog, sand, frost, inner, night);
        }

        /// <summary>Camera-local shelter. The live weather, day clock and outdoor wetness remain authoritative.</summary>
        private void ApplyInteriorShelter04C()
        {
            bool roofed = Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.IsOperational && Game.Inhabited.Roofed;
            if (LivingCity04CQa.FocusOverride && Game.Inhabited != null && Game.Inhabited.IsOperational)
            {
                roofed = false;
                foreach (VenueBinding04C venue in Game.Inhabited.Venues)
                    if (venue.Available && venue.View.IsRoofed(LivingCity04CQa.FocusPoint)) { roofed = true; break; }
            }
            _interiorShelter = Mathf.MoveTowards(_interiorShelter, roofed ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
            if (roofed && !_interiorRoofed)
            {
                // These systems use world-space particles. Stopping emission alone leaves existing snow in the room.
                if (_rain != null) _rain.Clear(true);
                if (_dust != null) _dust.Clear(true);
                if (_snow != null) _snow.Clear(true);
            }
            _interiorRoofed = roofed;
            if (roofed)
            {
                if (_rain != null) { ParticleSystem.EmissionModule emission = _rain.emission; emission.rateOverTime = 0f; }
                if (_dust != null) { ParticleSystem.EmissionModule emission = _dust.emission; emission.rateOverTime = 0f; }
                if (_snow != null) { ParticleSystem.EmissionModule emission = _snow.emission; emission.rateOverTime = 0f; }
            }
            RenderSettings.fogDensity *= Mathf.Lerp(1f, 0.25f, _interiorShelter);
        }

        private void FollowCamera()
        {
            Transform t = _followTarget;
            if (t == null && Camera.main != null) t = Camera.main.transform;
            if (t == null) return;
            if (_rain != null) _rain.transform.position = t.position + Vector3.up * 14f + t.forward * 6f;
            if (_dust != null) _dust.transform.position = t.position + Vector3.up * 4f - Vector3.right * 20f;
            if (_snow != null) _snow.transform.position = t.position + Vector3.up * 12f + t.forward * 8f;
        }

        private void UpdateLightning()
        {
            // No new strikes (flash or thunder) while the Veil is more than half open, or underground (04E).
            float storm = Weight(Weather.Storm, Current, Target, WeatherBlend) * _dry * (1f - _under);
            _lightning = Mathf.MoveTowards(_lightning, 0f, Time.deltaTime * 4f);
            if (storm < 0.5f) return;
            _nextLightning -= Time.deltaTime;
            if (_nextLightning <= 0f)
            {
                _nextLightning = Random.Range(6f, 18f);
                _lightning = 1f;
                if (Game.Audio != null) Game.Audio.PlayThunder(Random.Range(0.4f, 2.5f));
            }
            if (Sun != null && _lightning > 0.05f)
            {
                RenderSettings.ambientSkyColor += new Color(0.6f, 0.65f, 0.8f) * _lightning;
            }
        }
    }
}
