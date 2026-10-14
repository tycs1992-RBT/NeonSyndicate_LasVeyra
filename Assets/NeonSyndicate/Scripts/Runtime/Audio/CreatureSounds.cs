using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>What a creature is doing when it makes a sound.</summary>
    public enum CreatureCry
    {
        Attack,
        Roar,
        Death,
        Step,
        Wings,
        Rumble,
        Slam,
        Hurt
    }

    /// <summary>
    /// Synthesized creature voices, built from the same oscillators, filtered noise and envelopes as AudioSynth (no
    /// audio files): snarls, howls, roars, screeches, hisses, mite chitter, golem grinding, yelps and groans, heavy
    /// footfalls, wing beats, the wyrm's rumble and slam impacts. Pure C# float maths (tested offline); CreatureAudio
    /// turns them into clips and plays them, pitched by body size.
    /// </summary>
    public static class CreatureSounds
    {
        public enum Clip { Snarl, Howl, Roar, Screech, Hiss, Chitter, Grind, Yelp, Groan, Thud, Wings, Rumble, Slam, Whisper, Count }

        public static float[] Make(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.Snarl:
                    b = AudioSynth.Buf(0.55f);
                    AudioSynth.Noise(b, 0f, 0.5f, 0.8f, 0.03f, 0.25f, 900f, 120f, 7101, 0.5f);
                    AudioSynth.Tone(b, 0f, 0.5f, 120f, 85f, 0.35f, 0.02f, 0.3f, AudioSynth.Wave.Saw, 28f, 0.25f);
                    Tremolo(b, 26f, 0.6f);
                    AudioSynth.Drive(b, 0.4f);
                    break;
                case Clip.Howl:
                    b = AudioSynth.Buf(1.7f);
                    AudioSynth.Tone(b, 0f, 0.7f, 380f, 640f, 0.5f, 0.12f, 1.2f, AudioSynth.Wave.Sine, 5f, 0.02f);
                    AudioSynth.Tone(b, 0.65f, 1f, 640f, 430f, 0.5f, 0.05f, 0.7f, AudioSynth.Wave.Sine, 5.5f, 0.025f);
                    AudioSynth.Tone(b, 0f, 1.6f, 760f, 1100f, 0.08f, 0.2f, 0.9f, AudioSynth.Wave.Triangle, 5f, 0.02f);
                    AudioSynth.Noise(b, 0f, 1.5f, 0.12f, 0.1f, 0.8f, 2400f, 600f, 7111);
                    AudioSynth.Echo(b, 0.21f, 0.3f, 3);
                    break;
                case Clip.Roar:
                    b = AudioSynth.Buf(1.5f);
                    AudioSynth.Noise(b, 0f, 1.4f, 0.9f, 0.08f, 0.7f, 650f, 60f, 7121, 0.8f);
                    AudioSynth.Tone(b, 0f, 1.4f, 78f, 52f, 0.5f, 0.1f, 0.8f, AudioSynth.Wave.Saw, 9f, 0.08f);
                    AudioSynth.Tone(b, 0.05f, 1.2f, 156f, 110f, 0.2f, 0.1f, 0.6f, AudioSynth.Wave.Square, 7f, 0.05f);
                    AudioSynth.Drive(b, 0.6f);
                    AudioSynth.Echo(b, 0.17f, 0.25f, 2);
                    break;
                case Clip.Screech:
                    b = AudioSynth.Buf(0.95f);
                    AudioSynth.Tone(b, 0f, 0.9f, 1900f, 1150f, 0.45f, 0.02f, 0.45f, AudioSynth.Wave.Saw, 32f, 0.04f);
                    AudioSynth.Tone(b, 0f, 0.8f, 2900f, 1700f, 0.15f, 0.02f, 0.35f, AudioSynth.Wave.Triangle, 30f, 0.05f);
                    AudioSynth.Noise(b, 0f, 0.85f, 0.25f, 0.02f, 0.4f, 7000f, 1800f, 7131);
                    AudioSynth.LowPass(b, 6500f);
                    break;
                case Clip.Hiss:
                    b = AudioSynth.Buf(0.65f);
                    AudioSynth.Noise(b, 0f, 0.6f, 0.8f, 0.06f, 0.35f, 9000f, 2800f, 7141);
                    Tremolo(b, 11f, 0.3f);
                    break;
                case Clip.Chitter:
                    b = AudioSynth.Buf(0.38f);
                    for (int i = 0; i < 9; i++)
                    {
                        float t = i * 0.038f + MathUtil.Hash01(i, 7151) * 0.012f;
                        AudioSynth.Noise(b, t, 0.008f, 0.7f, 0.0005f, 0.004f, 9000f, 3500f, 7152 + i);
                        AudioSynth.Tone(b, t, 0.012f, 3200f + i * 90f, 2800f, 0.25f, 0.001f, 0.006f, AudioSynth.Wave.Square);
                    }
                    break;
                case Clip.Grind:
                    b = AudioSynth.Buf(0.85f);
                    AudioSynth.Noise(b, 0f, 0.8f, 0.8f, 0.05f, 0.5f, 420f, 30f, 7161, 0.9f);
                    AudioSynth.Tone(b, 0f, 0.8f, 46f, 38f, 0.35f, 0.05f, 0.6f, AudioSynth.Wave.Square, 6f, 0.05f);
                    for (int i = 0; i < 6; i++) AudioSynth.Noise(b, 0.05f + i * 0.12f, 0.02f, 0.5f, 0.001f, 0.01f, 5000f, 1500f, 7170 + i);
                    AudioSynth.Drive(b, 0.3f);
                    break;
                case Clip.Yelp:
                    b = AudioSynth.Buf(0.42f);
                    AudioSynth.Tone(b, 0f, 0.4f, 920f, 480f, 0.6f, 0.01f, 0.18f, AudioSynth.Wave.Sine, 18f, 0.03f);
                    AudioSynth.Noise(b, 0f, 0.3f, 0.2f, 0.01f, 0.12f, 3000f, 500f, 7181);
                    break;
                case Clip.Groan:
                    b = AudioSynth.Buf(1.3f);
                    AudioSynth.Tone(b, 0f, 1.25f, 150f, 68f, 0.6f, 0.06f, 0.8f, AudioSynth.Wave.Saw, 4f, 0.04f);
                    AudioSynth.Noise(b, 0f, 1.2f, 0.3f, 0.1f, 0.7f, 520f, 60f, 7191, 0.6f);
                    AudioSynth.LowPass(b, 1800f);
                    break;
                case Clip.Thud:
                    b = AudioSynth.Buf(0.32f);
                    AudioSynth.Tone(b, 0f, 0.3f, 62f, 34f, 0.9f, 0.002f, 0.11f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.12f, 0.45f, 0.002f, 0.05f, 320f, 30f, 7201, 0.8f);
                    break;
                case Clip.Wings:
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Noise(b, 0f, 0.28f, 0.8f, 0.12f, 0.12f, 700f, 70f, 7211, 0.4f);
                    AudioSynth.Noise(b, 0.3f, 0.28f, 0.65f, 0.12f, 0.12f, 650f, 70f, 7212, 0.4f);
                    break;
                case Clip.Rumble:
                    b = AudioSynth.Buf(1.6f);
                    AudioSynth.Noise(b, 0f, 1.55f, 0.9f, 0.4f, 1.2f, 140f, 18f, 7221, 1f);
                    AudioSynth.Tone(b, 0f, 1.5f, 33f, 29f, 0.5f, 0.4f, 1f, AudioSynth.Wave.Sine, 2f, 0.1f);
                    break;
                case Clip.Slam:
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Tone(b, 0f, 0.8f, 55f, 28f, 0.9f, 0.002f, 0.3f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.5f, 0.7f, 0.002f, 0.18f, 900f, 40f, 7231, 0.7f);
                    for (int i = 0; i < 7; i++) AudioSynth.Noise(b, 0.04f + i * 0.07f + MathUtil.Hash01(i, 7240) * 0.03f, 0.015f, 0.35f - i * 0.03f, 0.001f, 0.008f, 6000f, 1800f, 7241 + i);
                    AudioSynth.Echo(b, 0.12f, 0.25f, 2);
                    break;
                case Clip.Whisper:
                    b = AudioSynth.Buf(0.95f);
                    AudioSynth.Noise(b, 0f, 0.9f, 0.7f, 0.15f, 0.5f, 2600f, 600f, 7251);
                    AudioSynth.Tone(b, 0f, 0.9f, 210f, 160f, 0.18f, 0.15f, 0.5f, AudioSynth.Wave.Triangle, 6f, 0.06f);
                    Tremolo(b, 7f, 0.55f);
                    AudioSynth.Echo(b, 0.23f, 0.35f, 3);
                    break;
                default:
                    b = AudioSynth.Buf(0.1f);
                    break;
            }
            Clean(b);
            AudioSynth.Normalize(b, 0.85f);
            return b;
        }

        /// <summary>The clip a species uses for a cry (golems grind, birds screech, shades whisper).</summary>
        public static Clip For(string speciesId, CreatureCry cry)
        {
            switch (cry)
            {
                case CreatureCry.Step: return Clip.Thud;
                case CreatureCry.Wings: return Clip.Wings;
                case CreatureCry.Rumble: return Clip.Rumble;
                case CreatureCry.Slam: return Clip.Slam;
            }
            bool death = cry == CreatureCry.Death;
            bool roar = cry == CreatureCry.Roar;
            switch (speciesId)
            {
                case "neon_jackal": return death ? Clip.Yelp : (roar ? Clip.Howl : Clip.Snarl);
                case "frost_stalker": return death ? Clip.Groan : (roar ? Clip.Roar : Clip.Snarl);
                case "crystal_basilisk": return death ? Clip.Groan : (roar ? Clip.Roar : Clip.Hiss);
                case "violet_wyvern":
                case "rime_griffin":
                case "sun_griffin": return death ? Clip.Groan : Clip.Screech;
                case "quartz_drake":
                case "dune_wyrm": return death ? Clip.Groan : Clip.Roar;
                case "hollow_shade":
                case "hollow":
                case "snow_hollow":
                case "mirror_wraith":
                case "wraith_queen": return Clip.Whisper;
                case "glass_mite": return Clip.Chitter;
                case "shard_golem":
                case "sand_golem":
                case "golem_foreman":
                case "glacier_titan": return death ? Clip.Slam : Clip.Grind;
                case "mirage_djinn": return death ? Clip.Groan : Clip.Roar;
                default: return death ? Clip.Groan : Clip.Snarl;
            }
        }

        /// <summary>Playback pitch for a body size: small creatures sound high, big ones deep.</summary>
        public static float PitchFor(float size)
        {
            return Mathf.Clamp(1.25f / Mathf.Sqrt(Mathf.Max(0.05f, size)), 0.5f, 1.9f);
        }

        private static void Tremolo(float[] b, float hz, float depth)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)AudioSynth.Rate;
                b[i] *= 1f - depth * (0.5f + 0.5f * Mathf.Sin(t * hz * Mathf.PI * 2f));
            }
        }

        private static void Clean(float[] b)
        {
            for (int i = 0; i < b.Length; i++) if (float.IsNaN(b[i]) || float.IsInfinity(b[i])) b[i] = 0f;
        }
    }

    /// <summary>
    /// Plays creature voices: the synthesized clips built once at boot and a small pool of 3D sources (pitch by body size,
    /// a light per-clip rate limit so a swarm or a stampede never floods the mix).
    /// </summary>
    public static class CreatureAudio
    {
        private static AudioClip[] _clips;
        private static AudioSource[] _pool;
        private static float[] _lastPlayed;
        private static int _next;

        public static void ResetStatics()
        {
            _clips = null;
            _pool = null;
            _lastPlayed = null;
            _next = 0;
        }

        public static bool Ready { get { return _clips != null && _pool != null; } }

        /// <summary>Builds the clips and the source pool under parent (called once by the creature director).</summary>
        public static void Init(Transform parent)
        {
            int n = (int)CreatureSounds.Clip.Count;
            _clips = new AudioClip[n];
            _lastPlayed = new float[n];
            for (int i = 0; i < n; i++)
            {
                float[] data = CreatureSounds.Make((CreatureSounds.Clip)i);
                AudioClip c = AudioClip.Create("Creature " + (CreatureSounds.Clip)i, data.Length, 1, AudioSynth.Rate, false);
                c.SetData(data, 0);
                _clips[i] = c;
                _lastPlayed[i] = -9f;
            }
            _pool = new AudioSource[10];
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject go = new GameObject("Creature Voice " + i);
                go.transform.SetParent(parent, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.dopplerLevel = 0.2f;
                _pool[i] = s;
            }
        }

        public static void Play(CreatureSpecies sp, CreatureCry cry, Vector3 pos, float size)
        {
            if (!Ready || sp == null) return;
            CreatureSounds.Clip c = CreatureSounds.For(sp.Id, cry);
            int ci = (int)c;
            float gap = c == CreatureSounds.Clip.Thud || c == CreatureSounds.Clip.Chitter ? 0.09f : 0.2f;
            if (Time.time - _lastPlayed[ci] < gap) return;
            if (Game.Cam != null && (Game.Cam.transform.position - pos).sqrMagnitude > 120f * 120f) return;
            _lastPlayed[ci] = Time.time;
            AudioSource s = null;
            for (int i = 0; i < _pool.Length; i++)
            {
                AudioSource o = _pool[(_next + i) % _pool.Length];
                if (!o.isPlaying) { s = o; _next = (_next + i + 1) % _pool.Length; break; }
            }
            if (s == null)
            {
                s = _pool[_next];
                _next = (_next + 1) % _pool.Length;
            }
            bool big = size > 2.2f;
            float vol = cry == CreatureCry.Step ? Mathf.Clamp(size * 0.18f, 0.15f, 0.7f) : (cry == CreatureCry.Roar ? 0.95f : 0.75f);
            s.transform.position = pos;
            s.clip = _clips[ci];
            s.volume = Mathf.Clamp01(vol) * Settings.SfxVolume;
            s.pitch = CreatureSounds.PitchFor(size) * Random.Range(0.93f, 1.07f) * (Time.timeScale < 0.8f ? 0.7f : 1f);
            s.minDistance = big ? 8f : 2.5f;
            s.maxDistance = big ? 160f : 60f;
            s.Play();
        }
    }
}
