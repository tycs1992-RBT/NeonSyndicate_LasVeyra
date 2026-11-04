using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Synthesized combat-loop sounds (no audio files), built with AudioSynth's primitives like the loot sounds: the
    /// Resonance Burst (a deep crystal boom under a rising chime and a glassy crack), the soft chime of a power's mark,
    /// a plate cracking, a plate breaking off and a creature part shattering. H8S adds the readability cues: a boss phase
    /// bell, the kill confirmation (a crisp crystal chord over a soft thump, and a heavier one for elites and great
    /// beasts), the crystal focus heat warnings (two rising ticks when heat runs high, three sharp ones when the next shot
    /// overheats) and the cool release when a forced vent ends. Pure float maths in Make; Init turns them into clips
    /// played through a small pool of sources of our own.
    /// </summary>
    public static partial class CombatSounds
    {
        public enum Clip { Burst, Prime, ArmorCrack, PlateBreak, PartBreak, PhaseShift, Kill, KillHeavy, HeatWarn, HeatCritical, VentReady, Count }

        private static AudioClip[] _clips;
        private static AudioSource[] _pool;
        private static int _next;
        private static float _lastCrack;

        public static void ResetStatics()
        {
            _clips = null;
            _pool = null;
            _next = 0;
            _lastCrack = 0f;
        }

        public static bool Ready { get { return _clips != null && _pool != null; } }

        public static float[] Make(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.Burst:
                    b = AudioSynth.Buf(1.3f);
                    // The thump, a falling sub tone, the crystal crack and a bright rising chime on top.
                    AudioSynth.Tone(b, 0f, 0.7f, 140f, 38f, 0.9f, 0.002f, 0.32f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.35f, 0.7f, 0.001f, 0.12f, 5200f, 180f, 7301, 0.4f);
                    AudioSynth.Noise(b, 0.01f, 0.25f, 0.4f, 0.001f, 0.06f, 12000f, 3500f, 7307);
                    AudioSynth.Tone(b, 0.02f, 0.9f, 880f, 1760f, 0.28f, 0.004f, 0.45f, AudioSynth.Wave.Triangle, 6f, 0.01f);
                    AudioSynth.Tone(b, 0.06f, 0.9f, 1320f, 2640f, 0.18f, 0.004f, 0.4f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.11f, 0.3f, 3);
                    AudioSynth.Drive(b, 1.4f);
                    break;
                case Clip.Prime:
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Tone(b, 0f, 0.5f, 1046f, 1046f, 0.35f, 0.004f, 0.3f, AudioSynth.Wave.Sine, 7f, 0.006f);
                    AudioSynth.Tone(b, 0.05f, 0.45f, 1568f, 1568f, 0.25f, 0.004f, 0.28f, AudioSynth.Wave.Triangle);
                    AudioSynth.Echo(b, 0.09f, 0.25f, 2);
                    break;
                case Clip.ArmorCrack:
                    b = AudioSynth.Buf(0.25f);
                    AudioSynth.Noise(b, 0f, 0.2f, 0.7f, 0.0005f, 0.04f, 9000f, 1600f, 7331);
                    AudioSynth.Tone(b, 0f, 0.18f, 2400f, 1500f, 0.25f, 0.001f, 0.06f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 9000f);
                    break;
                case Clip.PlateBreak:
                    b = AudioSynth.Buf(0.7f);
                    AudioSynth.Noise(b, 0f, 0.4f, 0.8f, 0.0005f, 0.1f, 7000f, 900f, 7341);
                    AudioSynth.Tone(b, 0f, 0.35f, 620f, 300f, 0.45f, 0.001f, 0.12f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.03f, 0.5f, 2900f, 2100f, 0.2f, 0.002f, 0.2f, AudioSynth.Wave.Triangle, 30f, 0.02f);
                    AudioSynth.Noise(b, 0.08f, 0.5f, 0.25f, 0.01f, 0.2f, 4000f, 1200f, 7349, 0.3f);
                    AudioSynth.Echo(b, 0.07f, 0.2f, 2);
                    break;
                case Clip.PhaseShift:
                case Clip.Kill:
                case Clip.KillHeavy:
                case Clip.HeatWarn:
                case Clip.HeatCritical:
                case Clip.VentReady:
                    b = MakeH8S(c);
                    break;
                default:
                    b = AudioSynth.Buf(1.1f);
                    // A crystal limb shattering: a cracking burst, tinkling shards and a low groan of the body.
                    AudioSynth.Noise(b, 0f, 0.45f, 0.85f, 0.0005f, 0.14f, 11000f, 1200f, 7351);
                    AudioSynth.Tone(b, 0f, 0.6f, 180f, 70f, 0.55f, 0.003f, 0.25f, AudioSynth.Wave.Saw);
                    for (int i = 0; i < 7; i++)
                    {
                        float f = 2200f + i * 410f;
                        AudioSynth.Tone(b, 0.05f + i * 0.06f, 0.3f, f, f * 0.96f, 0.14f, 0.001f, 0.12f, AudioSynth.Wave.Sine);
                    }
                    AudioSynth.Echo(b, 0.13f, 0.28f, 3);
                    AudioSynth.LowPass(b, 10000f);
                    break;
            }
            AudioSynth.Normalize(b, 0.9f);
            return b;
        }

        /// <summary>Builds the clips and the source pool under parent (called once by the Resonance system).</summary>
        public static void Init(Transform parent)
        {
            int n = (int)Clip.Count;
            _clips = new AudioClip[n];
            for (int i = 0; i < n; i++)
            {
                float[] data = Make((Clip)i);
                AudioClip c = AudioClip.Create("Combat " + (Clip)i, data.Length, 1, AudioSynth.Rate, false);
                c.SetData(data, 0);
                _clips[i] = c;
            }
            _pool = new AudioSource[8];
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject go = new GameObject("Combat Sound " + i);
                go.transform.SetParent(parent, false);
                AudioSource s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.dopplerLevel = 0f;
                _pool[i] = s;
            }
        }

        public static void Play(Clip c, Vector3 pos, float volume, float pitch, float spatial, float maxDistance)
        {
            if (!Ready) return;
            if (c == Clip.ArmorCrack)
            {
                // Fast guns crack plates many times a second: one crack every 60 ms is plenty.
                if (Time.time - _lastCrack < 0.06f) return;
                _lastCrack = Time.time;
            }
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
            s.pitch = pitch * (Time.timeScale < 0.8f ? 0.8f : 1f);
            s.spatialBlend = spatial;
            s.minDistance = 3f;
            s.maxDistance = maxDistance;
            s.Play();
        }
    }
}
