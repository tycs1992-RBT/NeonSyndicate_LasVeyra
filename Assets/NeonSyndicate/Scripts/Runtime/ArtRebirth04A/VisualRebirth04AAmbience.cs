using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// One scheduler for the optional 04A moving ornaments, plant crowns, tiny pollen fields, local lights and
    /// non-combat roosts. All artwork stays with its owner; rebuilding a fort cannot leave effects behind.
    /// No navigation, physics surfaces, creature AI, rewards, saves, camera or post-processing are changed.
    /// </summary>
    public sealed class VisualRebirth04AAmbience : MonoBehaviour
    {
        public const int MaxMotions = 96;
        public const int MaxLights = 24;
        public const int MaxMoteFields = 12;
        public const int MaxPerches = 3;
        private static VisualRebirth04AAmbience _instance;
        private readonly List<Motion> _motions = new List<Motion>(MaxMotions);
        private readonly List<Light> _lights = new List<Light>(MaxLights);
        private readonly List<MoteField> _fields = new List<MoteField>(MaxMoteFields);
        private readonly List<Perch> _perches = new List<Perch>(MaxPerches);
        private readonly List<int> _nearLights = new List<int>(3);
        private readonly List<int> _nearFields = new List<int>(3);
        private Material _moteMaterial;
        private Texture2D _moteTexture;
        private float _clock, _nextBudget, _nextMotion;

        private sealed class Motion
        {
            public Transform Target;
            public Quaternion Rest;
            public Vector3 Axis;
            public float Speed, Amplitude, Phase;
            public bool Plant;
        }
        private sealed class MoteField
        {
            public ParticleSystem Particles;
            public ParticleSystemRenderer Renderer;
        }
        private sealed class Perch
        {
            public Transform Root;
            public CreatureRig Rig;
            public float Phase, NextPose;
        }

        public static VisualRebirth04AAmbience Instance { get { return _instance; } }
        public int MotionCount { get { return _motions.Count; } }
        public int LightCount { get { return _lights.Count; } }
        public int FieldCount { get { return _fields.Count; } }
        public int PerchCount { get { return _perches.Count; } }
        public int ActiveLights { get; private set; }
        public int ActiveFields { get; private set; }
        public int ActiveMotions { get; private set; }

        /// <summary>Returns the world's single controller, including when several forts rebuild in one frame.</summary>
        public static VisualRebirth04AAmbience Add(Transform parent, CrystalCityStyle style)
        {
            if (_instance != null) return _instance;
            GameObject root = new GameObject("Visual Rebirth 04A / shared ambience scheduler");
            // A fort decoration root can be replaced during customization; never attach the scheduler to it.
            if (Game.Root != null) root.transform.SetParent(Game.Root.transform, false);
            else if (Game.City != null) root.transform.SetParent(Game.City.Root, false);
            _instance = root.AddComponent<VisualRebirth04AAmbience>();
            Log.Info("Visual Rebirth 04A | shared ambience online. Low: static plants/rings, no pollen or accent lights. Decorative roosts grant no rewards.");
            return _instance;
        }

        public static void ResetStatics()
        {
            if (_instance != null)
            {
                _instance.enabled = false;
                Release(_instance.gameObject);
            }
            _instance = null;
        }

        public void RegisterRotor(Transform target, Vector3 axis, float degreesPerSecond)
        {
            RegisterMotion(target, axis, Mathf.Clamp(degreesPerSecond, -8f, 8f), 0f, 0f, false);
        }

        /// <summary>Register only non-colliding foliage/canopy decoration, never a physical walkway or tree trunk.</summary>
        public void RegisterPlant(Transform target, float amplitude = 1.2f, float phase = 0f)
        {
            RegisterMotion(target, Vector3.forward, .8f, Mathf.Clamp(amplitude, 0f, 2.5f), phase, true);
        }

        /// <summary>
        /// Keep selected non-colliding ring/vine master models out of a static batch so they can actually move.
        /// Returns false on Low or budget exhaustion; the caller must keep its normal static batched part instead.
        /// Source meshes/materials are the same cached 04A assets, with their own near/far LOD and culling.
        /// </summary>
        public bool TryAddMovingPart(Transform parent, string model, string role, Vector3 localPosition,
            Vector3 scale, Quaternion rotation, int style)
        {
            if (parent == null || (model != "sky_ring" && model != "vine") ||
                Settings.Graphics == GraphicsPreset.Low || !VisualRebirth04ASettings.Enabled ||
                !VisualRebirth04ASettings.Atmosphere || (model == "vine" && !VisualRebirth04ASettings.Gardens)) return false;
            _motions.RemoveAll(m => m.Target == null);
            if (_motions.Count >= MaxMotions) return false;
            Mesh near = VisualRebirth04AAssets.Mesh(model, false);
            Mesh far = VisualRebirth04AAssets.Mesh(model, true);
            Material material = VisualRebirth04AAssets.Material(style, role);
            if (near == null || far == null || material == null) return false;
            GameObject root = new GameObject("04A / moving " + model);
            try
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = localPosition;
                root.transform.localRotation = rotation;
                root.transform.localScale = scale;
                Renderer nearRenderer = MovingMesh(root.transform, near, material, "detail", true);
                Renderer farRenderer = MovingMesh(root.transform, far, material, "silhouette", false);
                LODGroup lod = root.AddComponent<LODGroup>();
                lod.fadeMode = LODFadeMode.None;
                lod.SetLODs(new[] { new LOD(.08f, new[] { nearRenderer }), new LOD(.012f, new[] { farRenderer }) });
                lod.RecalculateBounds();
                if (model == "vine") RegisterPlant(root.transform, 1.1f, localPosition.x * .31f + localPosition.z * .19f);
                else RegisterRotor(root.transform, Vector3.up, (style % 2 == 0 ? 1f : -1f) * 1.8f);
                return true;
            }
            catch
            {
                root.SetActive(false);
                Release(root);
                throw;
            }
        }

        private static Renderer MovingMesh(Transform parent, Mesh mesh, Material material, string name, bool shadows)
        {
            GameObject part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows && Settings.Graphics >= GraphicsPreset.High ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return renderer;
        }

        private void RegisterMotion(Transform target, Vector3 axis, float speed, float amplitude, float phase, bool plant)
        {
            if (target == null || axis.sqrMagnitude < .001f) return;
            _motions.RemoveAll(m => m.Target == null);
            foreach (Motion m in _motions) if (m.Target == target) return;
            if (_motions.Count >= MaxMotions) return;
            _motions.Add(new Motion { Target = target, Rest = target.localRotation, Axis = axis.normalized,
                Speed = speed, Amplitude = amplitude, Phase = phase, Plant = plant });
        }

        /// <summary>Parent ownership is essential for effects rebuilt with a fort's appearance.</summary>
        public void AddAccent(Vector3 world, CrystalCityStyle style, Transform owner = null)
        {
            _lights.RemoveAll(light => light == null);
            if (_lights.Count >= MaxLights) return;
            for (int i = 0; i < _lights.Count; i++)
                if ((_lights[i].transform.position - world).sqrMagnitude < .01f && _lights[i].transform.parent == owner) return;
            GameObject source = new GameObject("04A / restrained mineral bounce");
            source.transform.SetParent(owner != null ? owner : transform, false);
            source.transform.position = world;
            Light light = source.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = AccentColor(style);
            light.range = 11f;
            light.intensity = 0f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            light.bounceIntensity = 0f;
            light.enabled = false;
            _lights.Add(light);
        }

        public void AddMotes(Transform parent, Vector3 localCenter, CrystalCityStyle style, int seed)
        {
            if (parent == null) return;
            _fields.RemoveAll(field => field.Particles == null);
            if (_fields.Count >= MaxMoteFields || !PrepareMoteMaterial()) return;
            foreach (MoteField field in _fields)
                if (field.Particles.transform.parent == parent && (field.Particles.transform.localPosition - localCenter).sqrMagnitude < .01f) return;
            GameObject root = new GameObject("04A / tiny mineral pollen (24 particles maximum)");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localCenter;
            try
            {
                _fields.Add(CreateMoteField(root, style, seed));
            }
            catch
            {
                root.SetActive(false);
                Release(root);
                throw;
            }
        }

        private MoteField CreateMoteField(GameObject root, CrystalCityStyle style, int seed)
        {
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.useAutoRandomSeed = false;
            particles.randomSeed = unchecked((uint)seed) | 1u;
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = 24;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(.035f, .085f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(.08f, .18f);
            main.startColor = Color.Lerp(Color.white, AccentColor(style), .48f) * .52f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.cullingMode = ParticleSystemCullingMode.PauseAndCatchup;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 2.5f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5.5f, 2f, 5.5f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-.06f, .06f);
            velocity.y = new ParticleSystem.MinMaxCurve(.08f, .13f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.06f, .06f);
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.75f, .25f), new GradientAlphaKey(.65f, .7f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = _moteMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;
            return new MoteField { Particles = particles, Renderer = renderer };
        }

        /// <summary>
        /// A bounded cosmetic pose of the existing quartz drake. It is not a spawned Creature: no AI, physics,
        /// combat target, NPC faction, interaction, encounter, loot or companion ownership is registered.
        /// </summary>
        public bool AddPerchedDrake(Transform parent, Vector3 localPosition, float yaw, int seed)
        {
            if (parent == null || !VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.Gardens) return false;
            _perches.RemoveAll(perch => perch.Root == null);
            if (_perches.Count >= MaxPerches) return false;
            GameObject root = new GameObject("04A / decorative quartz drake roost");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            try
            {
                CreatureBody body = CreatureBuilder.Build(CreatureLooks.For("quartz_drake", seed), 1);
                CreatureRig rig = CreatureRig.Build(root, body, null, Settings.Graphics >= GraphicsPreset.High);
                foreach (Collider collider in rig.Colliders) if (collider != null) collider.enabled = false;
                // All pose updates belong to this scheduler, not one Update per decorative creature.
                rig.enabled = false;
                Perch perch = new Perch { Root = root.transform, Rig = rig, Phase = Mathf.Abs(seed % 1000) * .017f };
                SolvePerch(perch, null, 1f / 30f);
                _perches.Add(perch);
                return true;
            }
            catch (Exception e)
            {
                root.SetActive(false);
                Release(root);
                Log.Warn("04A optional decorative drake skipped: " + e.Message);
                return false;
            }
        }

        private void Update()
        {
            if (Game.State == GameState.Loading) return;
            bool enabledArt = VisualRebirth04ASettings.Enabled && VisualRebirth04ASettings.Atmosphere;
            Transform viewer = Game.Cam != null ? Game.Cam.transform : null;
            if (Time.deltaTime > 0f) _clock = Mathf.Repeat(_clock + Time.deltaTime, 3600f);
            if (Time.unscaledTime >= _nextBudget)
            {
                _nextBudget = Time.unscaledTime + .3f;
                ApplyBudgets(viewer, enabledArt);
            }
            if (Time.deltaTime <= 0f || Time.unscaledTime < _nextMotion) return;
            // 30 Hz near art, only 15 Hz on Medium; Low retains the authored resting silhouettes.
            _nextMotion = Time.unscaledTime + (Settings.Graphics == GraphicsPreset.Medium ? 1f / 15f : 1f / 30f);
            ActiveMotions = 0;
            for (int i = 0; i < _motions.Count; i++)
            {
                Motion motion = _motions[i];
                if (motion.Target == null || !motion.Target.gameObject.activeInHierarchy) continue;
                bool active = enabledArt && Settings.Graphics != GraphicsPreset.Low && viewer != null &&
                    (!motion.Plant || VisualRebirth04ASettings.Gardens) &&
                    (motion.Target.position - viewer.position).sqrMagnitude < (motion.Plant ? 85f * 85f : 220f * 220f);
                float angle = active ? (motion.Plant ? Mathf.Sin(_clock * motion.Speed + motion.Phase) * motion.Amplitude :
                    Mathf.Repeat(_clock * motion.Speed, 360f)) : 0f;
                motion.Target.localRotation = motion.Rest * Quaternion.AngleAxis(angle, motion.Axis);
                if (active) ActiveMotions++;
            }
            for (int i = 0; i < _perches.Count; i++)
            {
                Perch perch = _perches[i];
                if (perch.Root == null || perch.Rig == null || perch.Rig.Renderer == null) continue;
                float distanceSq = viewer != null ? (viewer.position - perch.Root.position).sqrMagnitude : float.MaxValue;
                bool visible = VisualRebirth04ASettings.Enabled && VisualRebirth04ASettings.Gardens && distanceSq < 140f * 140f;
                perch.Rig.Renderer.enabled = visible;
                if (!visible || !enabledArt || Time.unscaledTime < perch.NextPose || Settings.Graphics == GraphicsPreset.Low) continue;
                float interval = distanceSq < 30f * 30f ? 1f / 30f : .12f;
                perch.NextPose = Time.unscaledTime + interval;
                SolvePerch(perch, distanceSq < 18f * 18f ? viewer : null, interval);
            }
        }

        private void ApplyBudgets(Transform viewer, bool art)
        {
            _motions.RemoveAll(m => m.Target == null);
            _lights.RemoveAll(light => light == null);
            _fields.RemoveAll(field => field.Particles == null);
            _perches.RemoveAll(perch => perch.Root == null);
            int budget = Settings.Graphics == GraphicsPreset.Low ? 0 : (Settings.Graphics == GraphicsPreset.Medium ? 1 : 3);
            float outer = Game.Atmo != null ? (1f - Mathf.Clamp01(Game.Atmo.InnerBlend)) * (1f - Mathf.Clamp01(Game.Atmo.FrostAmount)) : 1f;
            float night = Game.Atmo != null ? Mathf.Clamp01(Game.Atmo.NightFactor) : 0f;
            if (!art || viewer == null || outer < .01f) budget = 0;
            _nearLights.Clear();
            _nearFields.Clear();
            for (int pick = 0; pick < budget; pick++)
            {
                int best = -1;
                float nearest = 75f * 75f;
                for (int i = 0; i < _lights.Count; i++)
                {
                    Light light = _lights[i];
                    if (!light.gameObject.activeInHierarchy || _nearLights.Contains(i)) continue;
                    float distance = (viewer.position - light.transform.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; best = i; }
                }
                if (best >= 0) _nearLights.Add(best);
                best = -1; nearest = 65f * 65f;
                for (int i = 0; i < _fields.Count; i++)
                {
                    ParticleSystem particles = _fields[i].Particles;
                    if (!particles.gameObject.activeInHierarchy || _nearFields.Contains(i)) continue;
                    float distance = (viewer.position - particles.transform.position).sqrMagnitude;
                    if (distance < nearest) { nearest = distance; best = i; }
                }
                if (best >= 0) _nearFields.Add(best);
            }
            ActiveLights = ActiveFields = 0;
            for (int i = 0; i < _lights.Count; i++)
            {
                Light light = _lights[i];
                float distance = viewer != null ? Vector3.Distance(viewer.position, light.transform.position) : 1000f;
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(35f, 75f, distance));
                light.intensity = _nearLights.Contains(i) ? fade * outer * Mathf.Lerp(.04f, .72f, night) : 0f;
                light.enabled = light.intensity > .025f;
                if (light.enabled) ActiveLights++;
            }
            for (int i = 0; i < _fields.Count; i++)
            {
                MoteField field = _fields[i];
                bool active = _nearFields.Contains(i) && VisualRebirth04ASettings.Gardens;
                field.Renderer.enabled = active;
                if (active)
                {
                    if (!field.Particles.isPlaying) field.Particles.Play();
                    ActiveFields++;
                }
                else if (field.Particles.isPlaying || field.Particles.particleCount > 0)
                    field.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void SolvePerch(Perch perch, Transform viewer, float delta)
        {
            CreaturePoseInput pose = new CreaturePoseInput
            {
                RootPosition = perch.Root.position, Yaw = perch.Root.eulerAngles.y,
                Grounded = true, Flying = false, Action = CreatureAction.None, ActionT = -1f,
                Windup = .35f, Strike = .65f, Time = _clock + perch.Phase
            };
            if (viewer != null) { pose.HasLookTarget = true; pose.LookTarget = viewer.position; }
            perch.Rig.Pose.Solve(ref pose, delta);
            perch.Rig.ApplyPose();
        }

        private bool PrepareMoteMaterial()
        {
            if (_moteMaterial != null) return true;
            if (Mats.ParticleShader == null) return false;
            const int size = 32;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2f - 1f, dy = (y + .5f) / size * 2f - 1f;
                    float radial = Mathf.Clamp01(1f - dx * dx - dy * dy);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, radial * radial * radial);
                }
            try
            {
                _moteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "04A / original soft pollen alpha", wrapMode = TextureWrapMode.Clamp };
                _moteTexture.SetPixels32(pixels);
                _moteTexture.Apply(false, true);
                _moteMaterial = Mats.Additive("04A / shared mineral pollen", Color.white, _moteTexture);
                return _moteMaterial != null;
            }
            catch
            {
                Release(_moteTexture); Release(_moteMaterial);
                _moteTexture = null; _moteMaterial = null;
                throw;
            }
        }

        public static Color AccentColor(CrystalCityStyle style)
        {
            if ((int)style == 3) return new Color(1f, .41f, .16f);
            if (style == CrystalCityStyle.DawnstoneCourt) return new Color(1f, .78f, .48f);
            if (style == CrystalCityStyle.VerdantReach) return new Color(.34f, .78f, .65f);
            return new Color(.60f, .67f, 1f);
        }

        /// <summary>
        /// Called exactly once after Atmosphere computes its base state. A restrained warm/cool separation brings
        /// carved stone out of the old pink ambient. No accumulation, weather override, exposure or bloom changes.
        /// </summary>
        public static void ApplyAtmosphere(Atmosphere atmosphere)
        {
            if (atmosphere == null || !VisualRebirth04ASettings.Enabled || !VisualRebirth04ASettings.Atmosphere) return;
            Vector3 focus = atmosphere.FocusOverride ? atmosphere.FocusPoint :
                (Game.Cam != null ? Game.Cam.transform.position : Game.PlayerPosition);
            float city = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(850f, 1480f, Mathf.Max(Mathf.Abs(focus.x), Mathf.Abs(focus.z))));
            float outer = (1f - Mathf.Clamp01(atmosphere.InnerBlend)) * (1f - Mathf.Clamp01(atmosphere.FrostAmount));
            float clear = Mathf.Lerp(FairWeather(atmosphere.Current), FairWeather(atmosphere.Target), Mathf.Clamp01(atmosphere.WeatherBlend));
            float weight = city * outer * clear;
            if (weight < .001f) return;
            float night = Mathf.Clamp01(atmosphere.NightFactor);
            float verdant = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-320f, -100f, focus.x));
            float rift = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-580f, -300f, focus.z));
            Color daySky = Color.Lerp(new Color(.58f, .67f, .79f), new Color(.57f, .70f, .68f), verdant * .5f);
            Color nightSky = Color.Lerp(new Color(.17f, .21f, .34f), new Color(.14f, .25f, .25f), verdant * .5f);
            Color ground = Color.Lerp(new Color(.25f, .23f, .22f), new Color(.27f, .21f, .18f), rift * .5f);
            Color horizon = Color.Lerp(new Color(.66f, .70f, .76f), new Color(.09f, .12f, .20f), night);
            RenderSettings.ambientSkyColor = Color.Lerp(RenderSettings.ambientSkyColor, Color.Lerp(daySky, nightSky, night), weight * .24f);
            RenderSettings.ambientGroundColor = Color.Lerp(RenderSettings.ambientGroundColor, Color.Lerp(ground, new Color(.055f, .06f, .085f), night), weight * .20f);
            RenderSettings.ambientEquatorColor = Color.Lerp(RenderSettings.ambientEquatorColor, horizon * .55f, weight * .18f);
            RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, horizon, weight * .17f);
            RenderSettings.fogDensity += .00006f * weight;
            if (atmosphere.Sun != null)
                atmosphere.Sun.color = Color.Lerp(atmosphere.Sun.color, new Color(1f, .91f, .79f), weight * (1f - night) * .10f);
        }

        private static float FairWeather(Weather weather) { return weather == Weather.Clear ? 1f : (weather == Weather.Cloudy ? .25f : 0f); }

        private void OnDisable()
        {
            foreach (Light light in _lights) if (light != null) light.enabled = false;
            foreach (MoteField field in _fields)
                if (field.Particles != null) field.Particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (Motion motion in _motions) if (motion.Target != null) motion.Target.localRotation = motion.Rest;
            ActiveFields = ActiveLights = ActiveMotions = 0;
        }

        private void OnDestroy()
        {
            Release(_moteMaterial); Release(_moteTexture);
            _moteMaterial = null; _moteTexture = null;
            if (_instance == this) _instance = null;
        }

        private static void Release(UnityEngine.Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
