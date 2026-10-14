using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Owns Time.timeScale. Systems request slow motion by key (weapon wheel, Third Eye, wasted camera) and the
    /// slowest request wins; pause overrides everything. Physics steps are scaled too so slow motion stays smooth.
    /// </summary>
    public static class GameTime
    {
        public const float BaseFixedDelta = 1f / 60f;
        private static readonly Dictionary<string, float> _requests = new Dictionary<string, float>();
        public static bool Paused { get; private set; }
        public static float Scale { get; private set; } = 1f;

        public static void ResetStatics()
        {
            _requests.Clear();
            Paused = false;
            Scale = 1f;
        }

        public static void SetPaused(bool paused)
        {
            if (Paused == paused) return;
            Paused = paused;
            Apply();
        }

        public static void Request(string key, float scale)
        {
            _requests[key] = Mathf.Clamp(scale, 0.05f, 1f);
            Apply();
        }

        public static void Release(string key)
        {
            if (_requests.Remove(key)) Apply();
        }

        public static bool Has(string key)
        {
            return _requests.ContainsKey(key);
        }

        public static void ReleaseAll()
        {
            _requests.Clear();
            Apply();
        }

        private static void Apply()
        {
            float s = 1f;
            foreach (KeyValuePair<string, float> kv in _requests) s = Mathf.Min(s, kv.Value);
            Scale = s;
            Time.timeScale = Paused ? 0f : s;
            Time.fixedDeltaTime = BaseFixedDelta * Mathf.Max(0.05f, s);
            AudioListener.pause = Paused;
            if (Game.Audio != null) Game.Audio.SetSlowMotion(!Paused && s < 0.9f);
        }
    }
}
