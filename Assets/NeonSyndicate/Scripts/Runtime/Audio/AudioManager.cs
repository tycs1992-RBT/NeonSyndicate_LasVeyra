using System;
using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Pooled 3D one-shots, looping emitters, UI sounds and weather/city ambience. The 04E cues share the pools through
    /// PlayClipAt and PlayClip2D, and the street layers fade by Underheart04ESounds.SurfaceGain (exactly 1 on the surface).
    /// </summary>
    public sealed partial class AudioManager : MonoBehaviour
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
            int best = PickVoice();
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

        /// <summary>A free 3D pool voice, else the one that started longest ago.</summary>
        private int PickVoice()
        {
            int best = -1;
            float oldest = float.MaxValue;
            for (int i = 0; i < _pool.Length; i++)
            {
                if (!_pool[i].isPlaying) { best = i; break; }
                if (_poolStart[i] < oldest) { oldest = _poolStart[i]; best = i; }
            }
            return best;
        }

        /// <summary>
        /// Plays any synthesized clip through the shared 3D pool (the 04E cues use it, so they share voices, the volume
        /// setting and the slow-motion pitch with every other one-shot). minDistance is the full-volume radius.
        /// </summary>
        public void PlayClipAt(AudioClip clip, Vector3 pos, float volume, float pitch, float minDistance, float maxDistance)
        {
            if (!Ready || clip == null) return;
            int best = PickVoice();
            if (best < 0) return;
            AudioSource s = _pool[best];
            s.transform.position = pos;
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * Settings.SfxVolume;
            s.pitch = pitch * (_slowMo ? 0.6f : 1f);
            s.minDistance = Mathf.Max(0.5f, minDistance);
            s.maxDistance = Mathf.Max(s.minDistance + 1f, maxDistance);
            s.Play();
            _poolStart[best] = Time.time;
        }

        /// <summary>Plays any synthesized clip on the interface sources (2D, heard while paused), like Play2D.</summary>
        public void PlayClip2D(AudioClip clip, float volume, float pitch)
        {
            if (!Ready || clip == null) return;
            AudioSource s = _ui[_uiNext];
            _uiNext = (_uiNext + 1) % _ui.Length;
            s.clip = clip;
            s.volume = Mathf.Clamp01(volume) * Settings.SfxVolume;
            s.pitch = pitch;
            s.Play();
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
            bool sheltered = Game.Inhabited != null && Game.Inhabited.isActiveAndEnabled && Game.Inhabited.IsOperational && Game.Inhabited.Roofed;
            if (LivingCity04CQa.FocusOverride && Game.Inhabited != null && Game.Inhabited.IsOperational)
            {
                sheltered = false;
                foreach (VenueBinding04C venue in Game.Inhabited.Venues)
                    if (venue.Available && venue.View.IsRoofed(LivingCity04CQa.FocusPoint)) { sheltered = true; break; }
            }
            float exposure = sheltered ? 0.16f : 1f;
            // 04E zones: the street layers fade in service interiors and go silent underground. SurfaceGain is exactly 1 on
            // the surface, so street audio there is unchanged; the 04E zone beds play from Underheart04ESounds.
            float zone = Underheart04ESounds.SurfaceGain;
            _rain.volume = Mathf.MoveTowards(_rain.volume, rain * 0.55f * sfx * exposure * zone, Time.deltaTime * 0.3f);
            float windTarget = (sand * 0.7f + storm * 0.35f + 0.08f) * sfx;
            if (Game.PlayerPosition.y > 30f) windTarget += 0.25f * sfx;
            _wind.volume = Mathf.MoveTowards(_wind.volume, windTarget * exposure * zone, Time.deltaTime * 0.3f);
            bool inCity = !CityLayout.IsWilderness(CityLayout.DistrictAt(Game.PlayerPosition));
            float night = Game.Atmo != null ? Game.Atmo.NightFactor : 0f;
            float cityTarget = (inCity ? 0.35f : 0.06f) * (1f - night * 0.35f) * sfx;
            _city.volume = Mathf.MoveTowards(_city.volume, cityTarget * (1f-Mathf.Max(_oathGain04OG,_stormGain04OG)*.85f) * (sheltered ? 0.35f : 1f) * zone, Time.deltaTime * 0.2f);
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
            TickFrontier04OG();
            if (_thunderDelay >= 0f)
            {
                _thunderDelay -= Time.deltaTime;
                if (_thunderDelay < 0f)
                {
                    // 04E: thunder is muffled in service interiors and not heard underground (exactly as before on the surface).
                    float zone = Underheart04ESounds.SurfaceGain;
                    float vol = UnityEngine.Random.Range(0.5f, 0.9f) * zone;
                    float pitch = UnityEngine.Random.Range(0.8f, 1.1f);
                    if (vol > 0.02f) Play2D(SfxId.Thunder, vol, pitch);
                }
            }
        }
    }
}
