using UnityEngine;
using UnityEngine.Rendering;

namespace NeonSyndicate
{
    /// <summary>
    /// Sparse depth-tested world-space crystal motes. The effect never draws a canvas, touches gameplay colliders,
    /// adds lights, modifies the camera or animates shared materials. Distance checks run four times per second.
    /// </summary>
    public sealed class DistrictCrystalFx : MonoBehaviour
    {
        public const int MaximumActiveEmitters = 12;
        public const int ParticlesPerEmitter = 24;
        public const float ActiveDistance = 58f;
        private static int _activeEmitters;
        private ParticleSystem _particles;
        private Transform _cameraTransform;
        private float _nextCheck;
        private bool _ownsSlot;

        /// <summary>Create a small visual emitter under an existing district prop. Scale is clamped to 0.3 to 3.</summary>
        public static DistrictCrystalFx Attach(Transform parent, Vector3 localPosition, float scale = 1f, bool warm = false)
        {
            GameObject effect = new GameObject("Crystal resonance motes");
            effect.transform.SetParent(parent, false);
            effect.transform.localPosition = localPosition;
            DistrictCrystalFx component = effect.AddComponent<DistrictCrystalFx>();
            component.Init(Mathf.Clamp(scale, 0.3f, 3f), warm);
            return component;
        }

        /// <summary>Called by Game.ResetStatics, before new district emitters are created.</summary>
        public static void ResetStatics()
        {
            _activeEmitters = 0;
        }

        private void Init(float scale, bool warm)
        {
            _particles = gameObject.AddComponent<ParticleSystem>();
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _particles.useAutoRandomSeed = false;
            _particles.randomSeed = 0xC17A1u;
            ParticleSystem.MainModule main = _particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.maxParticles = ParticlesPerEmitter;
            main.duration = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f * scale, 0.065f * scale);
            main.startColor = warm ? new Color(0.63f, 0.30f, 1f, 0.48f) : new Color(0.24f, 0.84f, 1f, 0.48f);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            main.cullingMode = ParticleSystemCullingMode.Pause;

            ParticleSystem.EmissionModule emission = _particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 5f;
            ParticleSystem.ShapeModule shape = _particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.48f * scale;
            ParticleSystem.VelocityOverLifetimeModule velocity = _particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.y = 0.16f;
            velocity.orbitalY = warm ? -0.42f : 0.42f;
            ParticleSystem.SizeOverLifetimeModule size = _particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.2f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f)));
            Gradient gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.2f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = _particles.colorOverLifetime;
            color.enabled = true;
            color.color = gradient;

            // Short curved resonance strokes on the existing particles, not another emitter or screen overlay.
            ParticleSystem.TrailModule trails = _particles.trails;
            trails.enabled = true;
            trails.ratio = 0.25f;
            trails.lifetime = 0.22f;
            trails.minVertexDistance = 0.06f;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f,
                AnimationCurve.Linear(0f, 0.45f, 1f, 0f));
            trails.dieWithParticles = true;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.worldSpace = false;

            ParticleSystemRenderer renderer = _particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = DistrictMaterials.Get("particle");
            renderer.trailMaterial = renderer.sharedMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.maxParticleSize = 0.018f;
            renderer.sortMode = ParticleSystemSortMode.None;
            // The initial check is intentionally deferred until the gameplay camera exists.
            _nextCheck = 0f;
        }

        private void Update()
        {
            if (_particles == null || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.25f;
            ParticleSystem.TrailModule trails = _particles.trails;
            trails.enabled = Settings.Graphics >= GraphicsPreset.Medium;
            if (Game.Cam != null) _cameraTransform = Game.Cam.transform;
            if (_cameraTransform == null)
            {
                Camera fallbackCamera = Camera.main;
                if (fallbackCamera != null) _cameraTransform = fallbackCamera.transform;
            }
            float distanceSquared = _cameraTransform != null
                ? (_cameraTransform.position - transform.position).sqrMagnitude : float.PositiveInfinity;
            // Avoid a halo filling a first-person camera when a player stands inside a crystal decoration.
            bool visibleRange = distanceSquared < ActiveDistance * ActiveDistance && distanceSquared > 1.5f * 1.5f;
            if (!visibleRange) ReleaseSlot();
            else if (!_ownsSlot && _activeEmitters < MaximumActiveEmitters)
            {
                _ownsSlot = true;
                _activeEmitters++;
                _particles.Play(false);
            }
        }

        private void OnDisable()
        {
            ReleaseSlot();
        }

        private void ReleaseSlot()
        {
            if (!_ownsSlot) return;
            _ownsSlot = false;
            _activeEmitters = Mathf.Max(0, _activeEmitters - 1);
            if (_particles != null) _particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
