using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Synthesized loot sounds (no audio files), built with AudioSynth's primitives like the creature voices: the crystal
    /// caster's bolt and its vent hiss, a glass chime for Common to Rare drops, a rising arpeggio for Epic and a bright
    /// fanfare for Legendary and Mythic. Pure float maths in Make; Init turns them into clips played through a small
    /// pool of sources of our own.
    /// </summary>
    public static class LootSounds
    {
        public enum Clip { Zap, Vent, Chime, EpicChime, Fanfare, Count }

        private static AudioClip[] _clips;
        private static AudioSource[] _pool;
        private static int _next;
        private static float _lastZap;

        public static void ResetStatics()
        {
            _clips = null;
            _pool = null;
            _next = 0;
            _lastZap = 0f;
        }

        public static bool Ready { get { return _clips != null && _pool != null; } }

        public static float[] Make(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.Zap:
                    b = AudioSynth.Buf(0.22f);
                    AudioSynth.Tone(b, 0f, 0.2f, 1800f, 600f, 0.5f, 0.002f, 0.12f, AudioSynth.Wave.Saw, 40f, 0.05f);
                    AudioSynth.Tone(b, 0f, 0.15f, 3600f, 2400f, 0.2f, 0.002f, 0.08f, AudioSynth.Wave.Triangle);
                    AudioSynth.Noise(b, 0f, 0.08f, 0.35f, 0.001f, 0.05f, 9000f, 2500f, 9101);
                    AudioSynth.LowPass(b, 8000f);
                    break;
                case Clip.Vent:
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Noise(b, 0f, 0.85f, 0.6f, 0.03f, 0.6f, 7000f, 1500f, 9111, 0.3f);
                    AudioSynth.Tone(b, 0f, 0.7f, 1400f, 300f, 0.2f, 0.01f, 0.5f, AudioSynth.Wave.Sine);
                    break;
                case Clip.Chime:
                    b = AudioSynth.Buf(0.8f);
                    AudioSynth.Tone(b, 0f, 0.6f, 1320f, 1320f, 0.4f, 0.003f, 0.5f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.08f, 0.6f, 1980f, 1980f, 0.3f, 0.003f, 0.5f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.12f, 0.25f, 2);
                    break;
                case Clip.EpicChime:
                    {
                        b = AudioSynth.Buf(1.2f);
                        float[] f = { 880f, 1108f, 1320f, 1760f };
                        for (int i = 0; i < f.Length; i++) AudioSynth.Tone(b, i * 0.07f, 0.7f, f[i], f[i], 0.32f, 0.003f, 0.6f, AudioSynth.Wave.Triangle, 5f, 0.004f);
                        AudioSynth.Echo(b, 0.15f, 0.3f, 3);
                    }
                    break;
                default:
                    {
                        b = AudioSynth.Buf(2.2f);
                        float[] n = { 523f, 659f, 784f, 1046f, 1318f };
                        for (int i = 0; i < n.Length; i++) AudioSynth.Tone(b, i * 0.09f, 1.6f, n[i], n[i], 0.28f, 0.01f, 1.2f, AudioSynth.Wave.Triangle, 5.5f, 0.006f);
                        AudioSynth.Tone(b, 0.45f, 1.5f, 2093f, 2093f, 0.18f, 0.005f, 1.2f, AudioSynth.Wave.Sine);
                        AudioSynth.Noise(b, 0.4f, 1.6f, 0.1f, 0.2f, 1.2f, 12000f, 5000f, 9131);
                        AudioSynth.Echo(b, 0.18f, 0.35f, 3);
                    }
                    break;
            }
            AudioSynth.Normalize(b, 0.85f);
            return b;
        }

        /// <summary>Builds the clips and the source pool under parent (called once by the loot armory).</summary>
        public static void Init(Transform parent)
        {
            int n = (int)Clip.Count;
            _clips = new AudioClip[n];
            for (int i = 0; i < n; i++)
            {
                float[] data = Make((Clip)i);
                AudioClip c = AudioClip.Create("Loot " + (Clip)i, data.Length, 1, AudioSynth.Rate, false);
                c.SetData(data, 0);
                _clips[i] = c;
            }
            _pool = new AudioSource[6];
            for (int i = 0; i < _pool.Length; i++)
            {
                GameObject go = new GameObject("Loot Sound " + i);
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
            s.maxDistance = maxDistance;
            s.Play();
        }

        /// <summary>The crystal caster's own bolt sound. Returns false for guns that use their archetype's normal sound.</summary>
        public static bool PlayShot(WeaponDef d, Vector3 muzzle)
        {
            if (d == null || d.Loot == null || d.Loot.Archetype != LootArchetype.CrystalCaster || !Ready) return false;
            if (Time.time - _lastZap < 0.035f) return true;
            _lastZap = Time.time;
            Play(Clip.Zap, muzzle, 0.55f, 0.9f + 0.05f * (int)d.Loot.Maker + Random.Range(-0.04f, 0.04f), 0.7f, 120f);
            return true;
        }

        public static void PlayVent(Vector3 pos)
        {
            Play(Clip.Vent, pos, 0.6f, 1f, 0.6f, 40f);
        }

        /// <summary>The drop chime by rarity: Legendary and Mythic fanfares carry across the block.</summary>
        public static void PlayDrop(Rarity r, Vector3 pos)
        {
            if (r >= Rarity.Legendary) Play(Clip.Fanfare, pos, 0.9f, r == Rarity.Mythic ? 1.12f : 1f, 0.35f, 400f);
            else if (r == Rarity.Epic) Play(Clip.EpicChime, pos, 0.7f, 1f, 0.6f, 150f);
            else Play(Clip.Chime, pos, 0.35f + 0.08f * (int)r, 0.9f + 0.1f * (int)r, 0.85f, 60f);
        }
    }
}
