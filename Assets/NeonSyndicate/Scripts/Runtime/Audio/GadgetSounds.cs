using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Synthesized gadget sounds (no audio files), made with AudioSynth like the loot sounds: the deploy clack, the
    /// turret's dry shot, the flame hiss, the mine's chirp, the drone's zap, the shield's shimmer, ping and shatter, the
    /// scanner's sonar sweep, the canister burst, the heal chime, the denied buzz, the ready ding and the mod pickup.
    /// Make is pure float maths; Init turns the buffers into clips played through a small pool of sources.
    /// </summary>
    public static class GadgetSounds
    {
        public enum Clip { Deploy, TurretShot, Flame, Chirp, Zap, ShieldUp, ShieldHit, ShieldBreak, Pulse, Burst, Heal, Denied, Ready, ModPickup, Count }

        private static AudioClip[] _clips;
        private static AudioSource[] _pool;
        private static int _next;

        public static void ResetStatics()
        {
            _clips = null;
            _pool = null;
            _next = 0;
        }

        public static bool Ready { get { return _clips != null && _pool != null; } }

        public static float[] Make(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.Deploy:
                    b = AudioSynth.Buf(0.55f);
                    AudioSynth.Noise(b, 0f, 0.05f, 0.7f, 0.001f, 0.02f, 6000f, 800f, 7101);
                    AudioSynth.Tone(b, 0.02f, 0.08f, 180f, 90f, 0.5f, 0.001f, 0.04f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.1f, 0.4f, 300f, 1400f, 0.25f, 0.02f, 0.25f, AudioSynth.Wave.Saw, 12f, 0.01f);
                    AudioSynth.LowPass(b, 7000f);
                    break;
                case Clip.TurretShot:
                    b = AudioSynth.Buf(0.16f);
                    AudioSynth.Noise(b, 0f, 0.1f, 0.8f, 0.001f, 0.03f, 7000f, 600f, 7111);
                    AudioSynth.Tone(b, 0f, 0.1f, 900f, 260f, 0.45f, 0.001f, 0.04f, AudioSynth.Wave.Square);
                    AudioSynth.Drive(b, 1.6f);
                    break;
                case Clip.Flame:
                    b = AudioSynth.Buf(0.5f);
                    AudioSynth.Noise(b, 0f, 0.48f, 0.7f, 0.04f, 0.3f, 2600f, 250f, 7121, 0.6f);
                    AudioSynth.Noise(b, 0f, 0.3f, 0.25f, 0.01f, 0.1f, 8000f, 3000f, 7122);
                    break;
                case Clip.Chirp:
                    b = AudioSynth.Buf(0.2f);
                    AudioSynth.Tone(b, 0f, 0.07f, 2400f, 3200f, 0.4f, 0.002f, 0.05f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.09f, 0.07f, 2400f, 3600f, 0.35f, 0.002f, 0.05f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 6000f);
                    break;
                case Clip.Zap:
                    b = AudioSynth.Buf(0.25f);
                    AudioSynth.Tone(b, 0f, 0.22f, 2200f, 500f, 0.45f, 0.002f, 0.1f, AudioSynth.Wave.Saw, 60f, 0.08f);
                    AudioSynth.Noise(b, 0f, 0.12f, 0.35f, 0.001f, 0.05f, 10000f, 3000f, 7131);
                    AudioSynth.LowPass(b, 9000f);
                    break;
                case Clip.ShieldUp:
                    b = AudioSynth.Buf(0.8f);
                    AudioSynth.Tone(b, 0f, 0.7f, 260f, 520f, 0.35f, 0.05f, 0.4f, AudioSynth.Wave.Triangle, 7f, 0.02f);
                    AudioSynth.Tone(b, 0.05f, 0.7f, 780f, 1560f, 0.2f, 0.05f, 0.4f, AudioSynth.Wave.Sine, 9f, 0.02f);
                    AudioSynth.Echo(b, 0.09f, 0.3f, 2);
                    break;
                case Clip.ShieldHit:
                    b = AudioSynth.Buf(0.3f);
                    AudioSynth.Tone(b, 0f, 0.25f, 1600f, 1500f, 0.4f, 0.001f, 0.12f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.2f, 2450f, 2400f, 0.2f, 0.001f, 0.08f, AudioSynth.Wave.Sine);
                    break;
                case Clip.ShieldBreak:
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Noise(b, 0f, 0.6f, 0.8f, 0.001f, 0.25f, 11000f, 2000f, 7141);
                    for (int i = 0; i < 5; i++) AudioSynth.Tone(b, i * 0.04f, 0.5f, 1800f + i * 430f, 1700f + i * 400f, 0.18f, 0.001f, 0.25f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.11f, 0.25f, 2);
                    break;
                case Clip.Pulse:
                    b = AudioSynth.Buf(1.3f);
                    AudioSynth.Tone(b, 0f, 1.1f, 1200f, 300f, 0.45f, 0.005f, 0.7f, AudioSynth.Wave.Sine, 3f, 0.01f);
                    AudioSynth.Tone(b, 0f, 0.5f, 2400f, 600f, 0.15f, 0.005f, 0.3f, AudioSynth.Wave.Triangle);
                    AudioSynth.Echo(b, 0.22f, 0.4f, 3);
                    break;
                case Clip.Burst:
                    b = AudioSynth.Buf(0.7f);
                    AudioSynth.Noise(b, 0f, 0.06f, 0.8f, 0.001f, 0.03f, 4000f, 300f, 7151);
                    AudioSynth.Noise(b, 0.03f, 0.6f, 0.5f, 0.05f, 0.35f, 5000f, 800f, 7152, 0.4f);
                    break;
                case Clip.Heal:
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Tone(b, 0f, 0.4f, 660f, 660f, 0.3f, 0.01f, 0.3f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.08f, 0.4f, 990f, 990f, 0.25f, 0.01f, 0.3f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.1f, 0.25f, 2);
                    break;
                case Clip.Denied:
                    b = AudioSynth.Buf(0.3f);
                    AudioSynth.Tone(b, 0f, 0.1f, 140f, 140f, 0.45f, 0.002f, 0.08f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.14f, 0.1f, 110f, 110f, 0.45f, 0.002f, 0.08f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 2500f);
                    break;
                case Clip.Ready:
                    b = AudioSynth.Buf(0.45f);
                    AudioSynth.Tone(b, 0f, 0.15f, 1320f, 1320f, 0.3f, 0.002f, 0.1f, AudioSynth.Wave.Triangle);
                    AudioSynth.Tone(b, 0.1f, 0.3f, 1760f, 1760f, 0.3f, 0.002f, 0.2f, AudioSynth.Wave.Triangle);
                    break;
                default:
                    b = AudioSynth.Buf(0.6f);
                    float[] f = { 1046f, 1318f, 1568f };
                    for (int i = 0; i < f.Length; i++) AudioSynth.Tone(b, i * 0.06f, 0.35f, f[i], f[i], 0.28f, 0.003f, 0.25f, AudioSynth.Wave.Triangle);
                    AudioSynth.Echo(b, 0.12f, 0.25f, 2);
                    break;
            }
            AudioSynth.Normalize(b, 0.85f);
            return b;
        }

        /// <summary>Builds the clips and the source pool under parent (called once by the gadget system).</summary>
        public static void Init(Transform parent)
        {
            int n = (int)Clip.Count;
            _clips = new AudioClip[n];
            for (int i = 0; i < n; i++)
            {
                float[] data = Make((Clip)i);
                AudioClip c = AudioClip.Create("Gadget " + (Clip)i, data.Length, 1, AudioSynth.Rate, false);
                c.SetData(data, 0);
                _clips[i] = c;
            }
            _pool = new AudioSource[8];
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject go = new GameObject("Gadget Sound " + i);
                go.transform.SetParent(parent, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.dopplerLevel = 0f;
                _pool[i] = s;
            }
        }

        /// <summary>Plays a clip in the world (spatial 1) or on the player (spatial 0).</summary>
        public static void Play(Clip c, Vector3 pos, float volume, float pitch, float spatial)
        {
            if (!Ready) return;
            AudioSource s = null;
            for (int i = 0; i < _pool.Length; i++)
            {
                AudioSource o = _pool[(_next + i) % _pool.Length];
                if (!o.isPlaying)
                {
                    s = o;
                    _next = (_next + i + 1) % _pool.Length;
                    break;
                }
            }
            if (s == null)
            {
                s = _pool[_next];
                _next = (_next + 1) % _pool.Length;
            }
            s.transform.position = pos;
            s.clip = _clips[(int)c];
            s.volume = Mathf.Clamp01(volume) * Settings.SfxVolume;
            s.pitch = pitch * (Time.timeScale < 0.8f ? 0.75f : 1f);
            s.spatialBlend = spatial;
            s.minDistance = 3f;
            s.maxDistance = 90f;
            s.Play();
        }

        public static void Play2D(Clip c, float volume, float pitch)
        {
            Play(c, Game.Cam != null ? Game.Cam.transform.position : Vector3.zero, volume, pitch, 0f);
        }
    }
}
