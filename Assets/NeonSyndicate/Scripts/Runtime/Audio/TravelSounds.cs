using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Synthesized travel power sounds, built from AudioSynth's oscillators, filtered noise and envelopes (no audio
    /// files): the rushing wind loop, the soft boom at top speed, the take-off whoosh and the heavy and soft landings.
    /// Pure C# float maths, so it runs offline; TravelAudio turns the buffers into clips and plays them.
    /// </summary>
    public static class TravelSounds
    {
        public enum Clip { Wind, Boom, Whoosh, LandHeavy, LandSoft, Count }

        public static float[] Make(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.Wind:
                    b = AudioSynth.Buf(2.6f);
                    AudioSynth.Noise(b, 0f, 2.6f, 0.8f, 0f, 0f, 1400f, 120f, 9101, 0.35f);
                    AudioSynth.Noise(b, 0f, 2.6f, 0.3f, 0f, 0f, 4200f, 1500f, 9102);
                    Gusts(b, 0.7f, 0.35f);
                    b = AudioSynth.Looped(b, 0.3f);
                    break;
                case Clip.Boom:
                    b = AudioSynth.Buf(1.4f);
                    AudioSynth.Tone(b, 0f, 1.2f, 70f, 32f, 0.9f, 0.004f, 0.35f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 1.2f, 0.75f, 0.003f, 0.3f, 900f, 30f, 9111, 0.7f);
                    AudioSynth.Noise(b, 0.01f, 0.25f, 0.3f, 0.001f, 0.05f, 6000f, 1200f, 9112);
                    AudioSynth.Echo(b, 0.16f, 0.3f, 3);
                    AudioSynth.Drive(b, 0.8f);
                    break;
                case Clip.Whoosh:
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Noise(b, 0f, 0.85f, 0.7f, 0.25f, 0.3f, 900f, 200f, 9121);
                    AudioSynth.Noise(b, 0.1f, 0.7f, 0.5f, 0.25f, 0.25f, 2600f, 700f, 9122);
                    AudioSynth.Tone(b, 0f, 0.6f, 60f, 110f, 0.25f, 0.05f, 0.25f, AudioSynth.Wave.Sine);
                    break;
                case Clip.LandHeavy:
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Tone(b, 0f, 0.6f, 58f, 30f, 0.95f, 0.002f, 0.18f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.45f, 0.8f, 0.002f, 0.12f, 700f, 40f, 9131, 0.8f);
                    for (int i = 0; i < 6; i++) AudioSynth.Noise(b, 0.05f + i * 0.06f + MathUtil.Hash01(i, 9140) * 0.03f, 0.02f, 0.3f - i * 0.03f, 0.001f, 0.008f, 5000f, 1500f, 9141 + i);
                    break;
                case Clip.LandSoft:
                    b = AudioSynth.Buf(0.35f);
                    AudioSynth.Tone(b, 0f, 0.25f, 90f, 50f, 0.6f, 0.002f, 0.07f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.2f, 0.4f, 0.002f, 0.05f, 1600f, 80f, 9151, 0.5f);
                    break;
                default:
                    b = AudioSynth.Buf(0.1f);
                    break;
            }
            for (int i = 0; i < b.Length; i++) if (float.IsNaN(b[i]) || float.IsInfinity(b[i])) b[i] = 0f;
            AudioSynth.Normalize(b, c == Clip.Wind ? 0.7f : 0.9f);
            return b;
        }

        /// <summary>Slow swells in loudness so the wind breathes in gusts instead of hissing flat.</summary>
        private static void Gusts(float[] b, float hz, float depth)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                float g = 0.5f + 0.5f * Mathf.Sin(t * hz * Mathf.PI * 2f) * Mathf.Sin(t * hz * 0.37f * Mathf.PI * 2f + 1.1f);
                b[i] *= 1f - depth * g;
            }
        }
    }

    /// <summary>
    /// Plays the travel sounds for the player: one looping wind source that rises with speed and three one-shot
    /// sources. All are 2D (they belong to the player, who is always at the listener). Owned by TravelPowers, no statics.
    /// </summary>
    public sealed class TravelAudio
    {
        private AudioClip[] _clips;
        private AudioSource _wind;
        private AudioSource[] _shots;
        private int _next;
        private float _windLevel;

        /// <summary>Builds the clips and sources under parent.</summary>
        public void Init(Transform parent)
        {
            int n = (int)TravelSounds.Clip.Count;
            _clips = new AudioClip[n];
            for (int i = 0; i < n; i++)
            {
                float[] data = TravelSounds.Make((TravelSounds.Clip)i);
                AudioClip c = AudioClip.Create("Travel " + (TravelSounds.Clip)i, data.Length, 1, AudioSynth.Rate, false);
                c.SetData(data, 0);
                _clips[i] = c;
            }
            _wind = NewSource(parent, "Travel Wind");
            _wind.loop = true;
            _wind.clip = _clips[(int)TravelSounds.Clip.Wind];
            _shots = new AudioSource[3];
            for (int i = 0; i < _shots.Length; i++) _shots[i] = NewSource(parent, "Travel Sound " + i);
        }

        private static AudioSource NewSource(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        /// <summary>Wind rises with speed while a travel power is moving the player and fades away otherwise.</summary>
        public void Tick(TravelMode mode, float speed, float dt, bool playing)
        {
            if (_wind == null) return;
            float want = playing && mode != TravelMode.None ? Mathf.Clamp01((speed - 6f) / 36f) : 0f;
            _windLevel = Mathf.MoveTowards(_windLevel, want, dt * 1.5f);
            float vol = _windLevel * _windLevel * 0.85f * Settings.SfxVolume;
            if (vol > 0.002f)
            {
                if (!_wind.isPlaying) _wind.Play();
                _wind.volume = vol;
                _wind.pitch = 0.75f + _windLevel * 0.6f;
            }
            else if (_wind.isPlaying) _wind.Stop();
        }

        /// <summary>Plays a one-shot at a volume (0..1, scaled by the effects volume) and pitch.</summary>
        public void Play(TravelSounds.Clip c, float volume, float pitch)
        {
            if (_shots == null || _clips == null) return;
            AudioSource s = null;
            for (int i = 0; i < _shots.Length; i++)
            {
                AudioSource o = _shots[(_next + i) % _shots.Length];
                if (!o.isPlaying) { s = o; _next = (_next + i + 1) % _shots.Length; break; }
            }
            if (s == null)
            {
                s = _shots[_next];
                _next = (_next + 1) % _shots.Length;
            }
            s.clip = _clips[(int)c];
            s.volume = Mathf.Clamp01(volume) * Settings.SfxVolume;
            s.pitch = pitch * (Time.timeScale < 0.8f ? 0.75f : 1f);
            s.Play();
        }
    }
}
