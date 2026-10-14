using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Fire light for the Frost Wardens' braziers: real point lights that switch on only near the camera, flicker like
    /// open flame (stronger after dark) and throw a few embers when the player is close.
    /// </summary>
    public sealed class BrazierLights : MonoBehaviour
    {
        public const float LightRange = 170f;
        public const float EmberRange = 70f;
        private readonly List<Light> _lights = new List<Light>();
        private float _emberTimer;

        public void Init(List<Light> lights)
        {
            _lights.Clear();
            if (lights != null) _lights.AddRange(lights);
        }

        private void Update()
        {
            Camera cam = Game.Cam != null ? Game.Cam.Cam : Camera.main;
            if (cam == null || _lights.Count == 0) return;
            Vector3 cp = cam.transform.position;
            float t = Time.time;
            _emberTimer -= Time.deltaTime;
            bool embers = _emberTimer <= 0f;
            if (embers) _emberTimer = 0.09f;
            float dark = Game.Atmo != null ? Mathf.Lerp(0.6f, 1f, Game.Atmo.NightFactor) : 1f;
            for (int i = 0; i < _lights.Count; i++)
            {
                Light l = _lights[i];
                if (l == null) continue;
                float d2 = (l.transform.position - cp).sqrMagnitude;
                bool on = d2 < LightRange * LightRange;
                if (l.enabled != on) l.enabled = on;
                if (!on) continue;
                float flicker = 0.78f + 0.32f * Mathf.PerlinNoise(t * 7.3f + i * 11.1f, i * 3.7f);
                l.intensity = 2.3f * flicker * dark;
                if (embers && d2 < EmberRange * EmberRange && Game.Fx != null)
                    Game.Fx.FirePuff(l.transform.position + Vector3.down * 0.6f + Random.insideUnitSphere * 0.25f, 0.4f);
            }
        }
    }
}
