using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One small controller for the entire 02F pass. No per-leaf updates or global environment edits.</summary>
    public sealed class CrystalCity02FMotion : MonoBehaviour
    {
        private sealed class Rotor
        {
            public Transform Pivot;
            public Quaternion Rest;
            public Vector3 Axis;
            public float Speed;
        }
        private readonly List<Rotor> _rotors = new List<Rotor>(8);
        private readonly List<Light> _lights = new List<Light>(5);
        private readonly List<int> _activeLights = new List<int>(2);
        private readonly List<Blip> _markers = new List<Blip>(3);
        private float _clock;
        private float _nextLightCheck;
        public int RotorCount { get { return _rotors.Count; } }

        public void AddRotor(Transform pivot, Vector3 axis, float speed)
        {
            if (pivot == null || _rotors.Count >= 8 || axis.sqrMagnitude < .001f) return;
            _rotors.Add(new Rotor { Pivot = pivot, Rest = pivot.localRotation, Axis = axis.normalized, Speed = Mathf.Clamp(speed, -12f, 12f) });
        }

        public void AddMapMarker(Transform target, string title, CrystalCityStyle style)
        {
            if (target == null || _markers.Count >= 3) return;
            Blip marker = Blip.Add(BlipKind.Service, target, target.position,
                CrystalCity02DMaterials.Glow(style), "*", title + " (02F visual landmark)", false);
            marker.ShowOnMap = true; marker.ShowOnMinimap = false; marker.Size = 1.1f;
            _markers.Add(marker);
        }

        public void AddLight(Vector3 world, CrystalCityStyle style)
        {
            if (_lights.Count >= 5) return;
            GameObject source = new GameObject("02F local night accent");
            source.transform.SetParent(transform, false); source.transform.position = world;
            Light light = source.AddComponent<Light>();
            light.type = LightType.Point; light.color = CrystalCity02DMaterials.Glow(style);
            light.range = 10f; light.intensity = 0f; light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto; light.bounceIntensity = 0f; light.enabled = false;
            _lights.Add(light);
        }

        private void Update()
        {
            if (Game.State == GameState.Loading) return;
            float delta = Time.deltaTime;
            if (delta > 0f) _clock = Mathf.Repeat(_clock + delta, 3600f);
            Transform viewer = Game.Cam != null ? Game.Cam.transform : null;
            // Low quality retains the same models in a stationary pose.
            if (delta > 0f && viewer != null && Settings.Graphics != GraphicsPreset.Low)
                foreach (Rotor rotor in _rotors)
                {
                    if (rotor.Pivot == null) continue;
                    if ((viewer.position - rotor.Pivot.position).sqrMagnitude > 600f * 600f) continue;
                    rotor.Pivot.localRotation = rotor.Rest * Quaternion.AngleAxis(CrystalCity02FPlanner.SpinAngle(_clock, rotor.Speed), rotor.Axis);
                }
            if (Time.unscaledTime < _nextLightCheck) return;
            _nextLightCheck = Time.unscaledTime + .25f;
            float night = Game.Atmo != null ? CrystalCity02FPlanner.NightLight(Game.Atmo.NightFactor) : 0f;
            float realm = Game.Atmo != null ? 1f - Mathf.Clamp01(Game.Atmo.InnerBlend) : 1f;
            int budget = Settings.Graphics == GraphicsPreset.Low ? 0 : (Settings.Graphics == GraphicsPreset.Medium ? 1 : 2);
            _activeLights.Clear();
            if (viewer != null && night > .001f)
                for (int pick = 0; pick < budget; pick++)
                {
                    float nearest = 90f * 90f; int best = -1;
                    for (int i = 0; i < _lights.Count; i++)
                    {
                        if (_lights[i] == null || _activeLights.Contains(i)) continue;
                        float distance = (viewer.position - _lights[i].transform.position).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; best = i; }
                    }
                    if (best >= 0) _activeLights.Add(best);
                }
            for (int i = 0; i < _lights.Count; i++)
            {
                Light light = _lights[i];
                if (light == null) continue;
                float distance = viewer == null ? 1000f : Vector3.Distance(viewer.position, light.transform.position);
                float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(45f, 90f, distance));
                light.intensity = _activeLights.Contains(i) ? night * realm * fade * .85f : 0f;
                light.enabled = light.intensity > .02f;
            }
        }
        private void OnDisable()
        {
            foreach (Light light in _lights) if (light != null) light.enabled = false;
            foreach (Blip marker in _markers) if (marker != null) marker.Visible = false;
        }
        private void OnEnable()
        {
            foreach (Blip marker in _markers) if (marker != null) marker.Visible = true;
        }
        private void OnDestroy()
        {
            foreach (Blip marker in _markers) Blip.Remove(marker);
            _markers.Clear();
        }
    }
}
