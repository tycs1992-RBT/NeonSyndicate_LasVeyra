using System;
using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Pooled 3D one-shots, looping emitters, UI sounds and weather/city ambience.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private AudioClip[] _clips;
        private AudioClip _snowStep;
        private AudioSource[] _pool;
        private float[] _poolStart;
        private AudioSource[] _ui;
        private int _uiNext;
        private AudioSource _rain;
        private AudioSource _wind;
        private AudioSource _city;
        private float _thunderDelay = -1f;
        private bool _slowMo;
        public bool Ready { get; private set; }

        public IEnumerator BuildClips(Action<float> progress)
        {
            int count = (int)SfxId.Count;
            _clips = new AudioClip[count];
            for (int i = 0; i < count; i++)
            {
                SfxId id = (SfxId)i;
                float[] data;
                try { data = AudioSynth.Make(id); }
                catch (Exception e)
                {
                    Log.Exception("AudioSynth " + id, e);
                    data = new float[64];
                }
                AudioClip clip = AudioClip.Create(id.ToString(), data.Length, 1, AudioSynth.Rate, false);
                clip.SetData(data, 0);
                _clips[i] = clip;
                if (i % 6 == 5)
                {
                    if (progress != null) progress(i / (float)count);
                    yield return null;
                }
            }
            try
            {
                float[] snow = SnowStepSound.Make();
                _snowStep = AudioClip.Create("SnowStep", snow.Length, 1, AudioSynth.Rate, false);
                _snowStep.SetData(snow, 0);
            }
            catch (Exception e) { Log.Exception("AudioSynth SnowStep", e); }
            BuildPool();
            Ready = true;
        }

        private void BuildPool()
        {
            int n = 48;
            _pool = new AudioSource[n];
            _poolStart = new float[n];
            for (int i = 0; i < n; i++)
            {
                GameObject go = new GameObject("SFX " + i);
                go.transform.SetParent(transform, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.dopplerLevel = 0.2f;
                s.spread = 30f;
                _pool[i] = s;
            }
            _ui = new AudioSource[6];
            for (int i = 0; i < _ui.Length; i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                s.ignoreListenerPause = true;
                _ui[i] = s;
            }
            _rain = Ambient(SfxId.RainLoop);
            _wind = Ambient(SfxId.WindLoop);
            _city = Ambient(SfxId.CityLoop);
        }

        private AudioSource Ambient(SfxId id)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.clip = Clip(id);
            s.loop = true;
            s.spatialBlend = 0f;
            s.volume = 0f;
            s.playOnAwake = false;
            s.Play();
            return s;
        }

        public AudioClip Clip(SfxId id)
        {
            if (_clips == null) return null;
            int i = (int)id;
            return i >= 0 && i < _clips.Length ? _clips[i] : null;
        }

        public void PlayAt(SfxId id, Vector3 pos, float volume = 1f, float pitch = 1f, float maxDistance = 90f)
        {
            if (!Ready) return;
            AudioClip clip = Clip(id);
            // Footsteps in the Frostline snow crunch.
            if (id == SfxId.Footstep && _snowStep != null && SnowStepSound.OnSnow(pos)) clip = _snowStep;
            if (clip == null) return;
            int best = -1;
            float oldest = float.MaxValue;
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying) { best = i; break; }
                if (_poolStart[i] < oldest) { oldest = _poolStart[i]; best = i; }
            }
            if (best < 0) return;
            AudioSource s = _pool[best];
            s.transform.position = pos;
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * Settings.SfxVolume;
            s.pitch = pitch * (_slowMo ? 0.6f : 1f);
            s.minDistance = Mathf.Lerp(2f, 8f, Mathf.Clamp01(volume));
            s.maxDistance = maxDistance;
            s.Play();
            _poolStart[best] = Time.time;
        }

        public void PlayVar(SfxId id, Vector3 pos, float volume, float pitchVariance, float maxDistance)
        {
            PlayAt(id, pos, volume, 1f + UnityEngine.Random.Range(-pitchVariance, pitchVariance), maxDistance);
        }

        public void Play2D(SfxId id, float volume = 1f, float pitch = 1f)
        {
            if (!Ready) return;
            AudioClip clip = Clip(id);
            if (clip == null) return;
            AudioSource s = _ui[_uiNext];
            _uiNext = (_uiNext + 1) % _ui.Length;
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * Settings.SfxVolume;
            s.pitch = pitch;
            s.Play();
        }

        /// <summary>Creates a looping 3D source attached to a transform (engines, sirens, rotors, fire).</summary>
        public AudioSource CreateLoop(Transform parent, SfxId id, float volume, float maxDistance)
        {
            AudioSource s = parent.gameObject.AddComponent<AudioSource>();
            s.clip = Clip(id);
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.minDistance = 3f;
            s.maxDistance = maxDistance;
            s.dopplerLevel = 0.4f;
            s.volume = volume * Settings.SfxVolume;
            if (s.clip != null) s.Play();
            return s;
        }

        public void SetWeatherAmbience(float rain, float storm, float sand)
        {
            if (!Ready) return;
            float sfx = Settings.SfxVolume;
            _rain.volume = Mathf.MoveTowards(_rain.volume, rain * 0.55f * sfx, Time.deltaTime * 0.3f);
            float windTarget = (sand * 0.7f + storm * 0.35f + 0.08f) * sfx;
            if (Game.PlayerPosition.y > 30f) windTarget += 0.25f * sfx;
            _wind.volume = Mathf.MoveTowards(_wind.volume, windTarget, Time.deltaTime * 0.3f);
            bool inCity = !CityLayout.IsWilderness(CityLayout.DistrictAt(Game.PlayerPosition));
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            float cityTarget = (inCity ? 0.35f : 0.06f) * (1f - night * 0.35f) * sfx;
            _city.volume = Mathf.MoveTowards(_city.volume, cityTarget, Time.deltaTime * 0.2f);
        }

        public void PlayThunder(float delay)
        {
            _thunderDelay = delay;
        }

        public void SetSlowMotion(bool on)
        {
            _slowMo = on;
        }

        private void Update()
        {
            AudioListener.volume = Settings.MasterVolume;
            if (_thunderDelay >= 0f)
            {
                _thunderDelay -= Time.deltaTime;
                if (_thunderDelay < 0f) Play2D(SfxId.Thunder, UnityEngine.Random.Range(0.5f, 0.9f), UnityEngine.Random.Range(0.8f, 1.1f));
            }
        }
    }
}
