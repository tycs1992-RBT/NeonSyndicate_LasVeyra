using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace NeonSyndicate
{
    public sealed class StationStyle
    {
        public string Name;
        public float Bpm;
        public bool Minor;
        public int Genre; // 0 synthwave, 1 rock, 2 electro, 3 dub, 4 house, 5 lofi
        public string[] Titles;
        public string[] Hosts;
    }

    /// <summary>
    /// Procedural music composer: drums, bass, chords and leads per station style. Runs on a worker thread
    /// (pure math, deterministic per seed) and returns PCM samples.
    /// </summary>
    public static class MusicSynth
    {
        private static readonly int[] MinorScale = { 0, 2, 3, 5, 7, 8, 10 };
        private static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };

        private sealed class R
        {
            private uint _s;
            public R(int seed) { _s = (uint)seed * 747796405u + 2891336453u; if (_s == 0) _s = 1; }
            public float F() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return (_s & 0xFFFFFF) / 16777216f; }
            public int I(int max) { return (int)(F() * max) % Math.Max(1, max); }
            public float N() { return F() * 2f - 1f; }
        }

        private static float Midi(float note)
        {
            return 440f * Mathf.Pow(2f, (note - 69f) / 12f);
        }

        private static void Note(float[] b, int rate, int start, int length, float freq, float amp, int wave, float attack, float decay, float detune = 0f)
        {
            double ph = 0, ph2 = 0.37;
            int end = Math.Min(b.Length, start + length);
            for (int i = Math.Max(0, start); i < end; i++)
            {
                float t = (i - start) / (float)rate;
                float env = (attack > 0f ? Mathf.Clamp01(t / attack) : 1f) * (decay > 0f ? Mathf.Exp(-t / decay) : 1f);
                float rel = Mathf.Clamp01((end - i) / (rate * 0.01f));
                ph += freq / rate;
                float p = (float)(ph - Math.Floor(ph));
                float v;
                switch (wave)
                {
                    case 1: v = p < 0.5f ? 0.8f : -0.8f; break;
                    case 2: v = p * 2f - 1f; break;
                    case 3: v = p < 0.5f ? p * 4f - 1f : 3f - p * 4f; break;
                    case 4: v = Mathf.Sin(p * 6.2831853f) + 0.35f * Mathf.Sin(p * 12.566f) + 0.15f * Mathf.Sin(p * 18.85f); break;
                    default: v = Mathf.Sin(p * 6.2831853f); break;
                }
                if (detune > 0f)
                {
                    ph2 += freq * (1f + detune) / rate;
                    float p2 = (float)(ph2 - Math.Floor(ph2));
                    v = (v + (wave == 2 ? p2 * 2f - 1f : Mathf.Sin(p2 * 6.2831853f))) * 0.5f;
                }
                b[i] += v * amp * env * rel;
            }
        }

        private static void Kick(float[] b, int rate, int start, float amp)
        {
            double ph = 0;
            int len = (int)(0.32f * rate);
            for (int i = 0; i < len && start + i < b.Length; i++)
            {
                if (start + i < 0) continue;
                float t = i / (float)rate;
                float f = 45f + 85f * Mathf.Exp(-t * 28f);
                ph += f / rate;
                b[start + i] += Mathf.Sin((float)(ph - Math.Floor(ph)) * 6.2831853f) * amp * Mathf.Exp(-t * 9f);
            }
        }

        private static void Snare(float[] b, int rate, int start, float amp, R r, float decay)
        {
            int len = (int)(decay * 5f * rate);
            float lp = 0f;
            double ph = 0;
            for (int i = 0; i < len && start + i < b.Length; i++)
            {
                if (start + i < 0) continue;
                float t = i / (float)rate;
                float n = r.N();
                lp += 0.6f * (n - lp);
                ph += 185f / rate;
                float tone = Mathf.Sin((float)(ph - Math.Floor(ph)) * 6.2831853f) * Mathf.Exp(-t * 30f) * 0.5f;
                b[start + i] += (lp * Mathf.Exp(-t / decay) + tone) * amp;
            }
        }

        private static void Hat(float[] b, int rate, int start, float amp, R r, float decay)
        {
            int len = (int)(decay * 6f * rate);
            float prev = 0f;
            for (int i = 0; i < len && start + i < b.Length; i++)
            {
                if (start + i < 0) continue;
                float t = i / (float)rate;
                float n = r.N();
                float hp = n - prev;
                prev = n;
                b[start + i] += hp * amp * Mathf.Exp(-t / decay);
            }
        }

        public static float[] Song(StationStyle style, int seed, int bars, int rate)
        {
            R r = new R(seed + style.Genre * 7919);
            float bpm = style.Bpm;
            int spb = (int)(rate * 60f / bpm);
            int s16 = spb / 4;
            int total = bars * 4 * spb;
            float[] drums = new float[total];
            float[] bass = new float[total];
            float[] chords = new float[total];
            float[] lead = new float[total];
            int[] scale = style.Minor ? MinorScale : MajorScale;
            int root = 45 + r.I(5) - 2;
            int[][] progressions = style.Minor
                ? new[] { new[] { 0, 5, 2, 6 }, new[] { 0, 3, 6, 2 }, new[] { 0, 5, 3, 4 }, new[] { 0, 6, 5, 6 } }
                : new[] { new[] { 0, 4, 5, 3 }, new[] { 0, 3, 4, 3 }, new[] { 0, 5, 3, 4 }, new[] { 0, 3, 0, 4 } };
            int[] prog = progressions[r.I(progressions.Length)];
            bool swing = style.Genre == 5;

            for (int bar = 0; bar < bars; bar++)
            {
                int barStart = bar * 4 * spb;
                int degree = prog[bar % 4];
                int chordRoot = root + scale[degree % 7] + (degree >= 7 ? 12 : 0);
                int third = root + scale[(degree + 2) % 7] + ((degree + 2) >= 7 ? 12 : 0);
                int fifth = root + scale[(degree + 4) % 7] + ((degree + 4) >= 7 ? 12 : 0);
                int seventh = root + scale[(degree + 6) % 7] + ((degree + 6) >= 7 ? 12 : 0);
                bool fill = bar % 8 == 7;
                for (int step = 0; step < 16; step++)
                {
                    int at = barStart + step * s16 + (swing && step % 2 == 1 ? s16 / 3 : 0);
                    switch (style.Genre)
                    {
                        case 0: // synthwave
                            if (step % 4 == 0) Kick(drums, rate, at, 0.9f);
                            if (step == 4 || step == 12) Snare(drums, rate, at, 0.7f, r, 0.12f);
                            if (step % 2 == 0) Hat(drums, rate, at, 0.12f, r, 0.02f);
                            if (fill && step >= 12) Snare(drums, rate, at, 0.35f, r, 0.05f);
                            if (step % 2 == 0) Note(bass, rate, at, s16 * 2 - 20, Midi(chordRoot - 12), 0.35f, 2, 0.003f, 0.12f);
                            break;
                        case 1: // rock
                            if (step == 0 || step == 8 || step == 10) Kick(drums, rate, at, 1f);
                            if (step == 4 || step == 12) Snare(drums, rate, at, 0.85f, r, 0.1f);
                            if (step % 2 == 0) Hat(drums, rate, at, 0.16f, r, 0.03f);
                            if (step % 2 == 0) Note(bass, rate, at, s16 * 2 - 10, Midi(chordRoot - 12 + (step % 8 == 6 ? 7 : 0)), 0.4f, 2, 0.002f, 0.2f);
                            if (step % 2 == 0)
                            {
                                Note(chords, rate, at, s16 * 2 - 30, Midi(chordRoot), 0.22f, 2, 0.002f, 0.25f, 0.006f);
                                Note(chords, rate, at, s16 * 2 - 30, Midi(chordRoot + 7), 0.18f, 2, 0.002f, 0.25f, 0.006f);
                            }
                            break;
                        case 2: // electro / breakbeat
                            if (step == 0 || step == 6 || step == 10) Kick(drums, rate, at, 0.95f);
                            if (step == 4 || step == 12) Snare(drums, rate, at, 0.75f, r, 0.08f);
                            if (step % 2 == 1) Hat(drums, rate, at, 0.14f, r, 0.015f);
                            if (step == 0 || step == 3 || step == 6 || step == 10 || step == 13) Note(bass, rate, at, s16 * 2, Midi(chordRoot - 12 + (step == 13 ? 12 : 0)), 0.38f, 1, 0.002f, 0.1f);
                            if (step == 2 || step == 9) { Note(chords, rate, at, s16, Midi(third + 12), 0.12f, 1, 0.002f, 0.06f); Note(chords, rate, at, s16, Midi(fifth + 12), 0.1f, 1, 0.002f, 0.06f); }
                            break;
                        case 3: // dub
                            if (step == 0 || step == 8) Kick(drums, rate, at, 0.9f);
                            if (step == 8) Snare(drums, rate, at, 0.55f, r, 0.15f);
                            if (step % 4 == 2) Hat(drums, rate, at, 0.1f, r, 0.04f);
                            if (step == 0) Note(bass, rate, at, s16 * 6, Midi(chordRoot - 12), 0.5f, 0, 0.01f, 0.6f);
                            if (step == 8) Note(bass, rate, at, s16 * 4, Midi(fifth - 12), 0.45f, 0, 0.01f, 0.5f);
                            if (step % 4 == 2)
                            {
                                Note(chords, rate, at, s16 - 40, Midi(chordRoot + 12), 0.1f, 3, 0.002f, 0.05f);
                                Note(chords, rate, at, s16 - 40, Midi(third + 12), 0.1f, 3, 0.002f, 0.05f);
                                Note(chords, rate, at, s16 - 40, Midi(fifth + 12), 0.1f, 3, 0.002f, 0.05f);
                            }
                            break;
                        case 4: // house
                            if (step % 4 == 0) Kick(drums, rate, at, 1f);
                            if (step == 4 || step == 12) Snare(drums, rate, at, 0.45f, r, 0.06f);
                            if (step % 4 == 2) Hat(drums, rate, at, 0.2f, r, 0.04f);
                            if (step % 4 == 2) Note(bass, rate, at, s16 * 2, Midi(chordRoot - 12), 0.42f, 3, 0.002f, 0.12f);
                            if (step == 3 || step == 7 || step == 11 || step == 14)
                            {
                                Note(chords, rate, at, s16 * 2, Midi(chordRoot + 12), 0.1f, 4, 0.002f, 0.18f);
                                Note(chords, rate, at, s16 * 2, Midi(third + 12), 0.1f, 4, 0.002f, 0.18f);
                                Note(chords, rate, at, s16 * 2, Midi(fifth + 12), 0.1f, 4, 0.002f, 0.18f);
                            }
                            break;
                        default: // lofi jazz
                            if (step == 0 || step == 10) Kick(drums, rate, at, 0.7f);
                            if (step == 4 || step == 12) Snare(drums, rate, at, 0.4f, r, 0.07f);
                            if (step % 2 == 0) Hat(drums, rate, at, 0.08f, r, 0.03f);
                            if (step % 4 == 0)
                            {
                                int[] walk = { chordRoot, third, fifth, seventh };
                                Note(bass, rate, at, spb - 30, Midi(walk[(step / 4) % 4] - 12), 0.4f, 0, 0.01f, 0.35f);
                            }
                            break;
                    }
                }
                // Sustained harmony per bar.
                int barLen = 4 * spb;
                switch (style.Genre)
                {
                    case 0:
                        Note(chords, rate, barStart, barLen, Midi(chordRoot), 0.12f, 2, 0.15f, 2.5f, 0.004f);
                        Note(chords, rate, barStart, barLen, Midi(third), 0.1f, 2, 0.15f, 2.5f, 0.005f);
                        Note(chords, rate, barStart, barLen, Midi(fifth), 0.1f, 2, 0.15f, 2.5f, 0.003f);
                        for (int s = 0; s < 16; s++)
                        {
                            int[] arp = { chordRoot + 12, third + 12, fifth + 12, third + 12 };
                            Note(lead, rate, barStart + s * s16, s16 - 10, Midi(arp[s % 4]), 0.09f, 1, 0.002f, 0.07f);
                        }
                        break;
                    case 2:
                        if (bar % 2 == 1)
                            for (int s = 0; s < 8; s++)
                                if (r.F() < 0.55f) Note(lead, rate, barStart + s * 2 * s16, s16 * 2, Midi(root + 24 + scale[r.I(7)]), 0.08f, 1, 0.002f, 0.1f);
                        break;
                    case 3:
                        if (r.F() < 0.4f) Note(lead, rate, barStart + spb, spb * 2, Midi(root + 24 + scale[r.I(5)]), 0.07f, 3, 0.05f, 0.6f);
                        break;
                    case 5:
                        Note(chords, rate, barStart, barLen, Midi(chordRoot + 12), 0.07f, 4, 0.02f, 1.6f);
                        Note(chords, rate, barStart, barLen, Midi(third + 12), 0.07f, 4, 0.02f, 1.6f);
                        Note(chords, rate, barStart, barLen, Midi(seventh + 12), 0.06f, 4, 0.02f, 1.6f);
                        if (r.F() < 0.6f) Note(lead, rate, barStart + spb * r.I(3), spb, Midi(root + 24 + scale[r.I(7)]), 0.06f, 0, 0.01f, 0.4f);
                        break;
                    case 4:
                        if (bar % 4 == 3) Note(lead, rate, barStart + spb * 2, spb * 2, Midi(root + 24 + scale[r.I(7)]), 0.06f, 4, 0.01f, 0.5f);
                        break;
                }
            }

            float[] mix = new float[total];
            float lp = 0f;
            float lpA = style.Genre == 5 ? 0.25f : 0.7f;
            for (int i = 0; i < total; i++)
            {
                float v = drums[i] * 0.85f + bass[i] * 0.9f + chords[i] + lead[i];
                if (style.Genre == 1) v = (float)Math.Tanh(v * 1.4f);
                lp += lpA * (v - lp);
                mix[i] = lp;
            }
            if (style.Genre == 0 || style.Genre == 3)
            {
                int d = (int)(spb * 0.75f);
                for (int i = d; i < total; i++) mix[i] += mix[i - d] * (style.Genre == 3 ? 0.4f : 0.22f);
            }
            if (style.Genre == 5)
            {
                R crackle = new R(seed + 5);
                for (int i = 0; i < total; i++) if (crackle.F() > 0.9993f) mix[i] += crackle.N() * 0.35f;
            }
            float peak = 0f;
            for (int i = 0; i < total; i++) peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
            float g = peak > 1e-5f ? 0.8f / peak : 1f;
            for (int i = 0; i < total; i++) mix[i] = (float)Math.Tanh(mix[i] * g * 1.1f) * 0.85f;
            return mix;
        }
    }

    /// <summary>Car radio: six stations from the original Neon Syndicate radio table, each with procedurally composed songs.</summary>
    public sealed class RadioSystem : MonoBehaviour
    {
        public static readonly StationStyle[] Stations =
        {
            new StationStyle { Name = "Pulse 88", Bpm = 104f, Minor = true, Genre = 0, Titles = new[] { "Rain on Chrome", "Glasshouse Loop", "Signal Fever" }, Hosts = new[] { "This city never sleeps; it idles loudly.", "Pulse 88. Neon after dark, all night long." } },
            new StationStyle { Name = "Midnight Torque", Bpm = 132f, Minor = true, Genre = 1, Titles = new[] { "Tire Smoke Hymn", "No Plate No Problem", "Overpass Saints" }, Hosts = new[] { "Traffic is a suggestion tonight.", "Midnight Torque. Turn it up, put it down." } },
            new StationStyle { Name = "Circuit Break", Bpm = 128f, Minor = true, Genre = 2, Titles = new[] { "Battery Acid Bass", "Signal Fever", "Blue Siren Dub" }, Hosts = new[] { "Reports of stunt driving are unconfirmed and frankly stylish.", "Circuit Break. Breaks, bass, bad decisions." } },
            new StationStyle { Name = "Harbor Static", Bpm = 78f, Minor = true, Genre = 3, Titles = new[] { "Blue Siren Dub", "Market After Dark", "Cinder Waltz" }, Hosts = new[] { "Keep both hands on the wheel and one eye on the mirror.", "Harbor Static. Low and slow from Sunken Row." } },
            new StationStyle { Name = "Glass FM", Bpm = 122f, Minor = false, Genre = 4, Titles = new[] { "Glasshouse Loop", "Market After Dark", "Rain on Chrome" }, Hosts = new[] { "If you see a spotlight, wave with your accelerator.", "Glass FM. Glass Heights, glass hearts, good times." } },
            new StationStyle { Name = "Old Mile AM", Bpm = 86f, Minor = false, Genre = 5, Titles = new[] { "Cinder Waltz", "Overpass Saints", "Tire Smoke Hymn" }, Hosts = new[] { "Old Mile AM. Late jazz for long drives.", "This is Old Mile AM, keeping Old Silver company since forever." } },
        };

        public int Station = -1;
        public bool PlayerInVehicle;
        private AudioSource _source;
        private readonly AudioClip[] _songs = new AudioClip[6];
        private readonly int[] _songIndex = new int[6];
        private readonly float[][] _pending = new float[6][];
        private readonly bool[] _generating = new bool[6];
        private readonly object _lock = new object();
        private float _songTime;
        public string NowPlaying { get; private set; }
        public float ShowTimer { get; private set; }

        public void Init()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.spatialBlend = 0f;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.volume = 0f;
            Station = UnityEngine.Random.Range(0, Stations.Length);
        }

        private void RequestSong(int station)
        {
            if (_generating[station]) return;
            _generating[station] = true;
            StationStyle style = Stations[station];
            int seed = station * 1000 + _songIndex[station] * 37 + 5;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                float[] data;
                try { data = MusicSynth.Song(style, seed, 16, AudioSynth.Rate); }
                catch (Exception) { data = new float[AudioSynth.Rate]; }
                lock (_lock) { _pending[station] = data; }
            });
        }

        public void Next()
        {
            Tune((Station + 2) % (Stations.Length + 1) - 1);
        }

        public void Previous()
        {
            int s = Station - 1;
            if (s < -1) s = Stations.Length - 1;
            Tune(s);
        }

        public void Tune(int station)
        {
            Station = Mathf.Clamp(station, -1, Stations.Length - 1);
            if (Station < 0)
            {
                NowPlaying = "Radio Off";
                ShowTimer = 2.5f;
                if (_source != null) _source.Stop();
                return;
            }
            if (_songs[Station] == null) RequestSong(Station);
            StartCurrent();
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIClick, 0.4f);
        }

        private void StartCurrent()
        {
            if (Station < 0 || _source == null) return;
            StationStyle s = Stations[Station];
            string title = s.Titles[_songIndex[Station] % s.Titles.Length];
            NowPlaying = s.Name + "  -  \"" + title + "\"\n" + s.Hosts[_songIndex[Station] % s.Hosts.Length];
            ShowTimer = 4f;
            AudioClip clip = _songs[Station];
            if (clip == null) { _source.Stop(); return; }
            _source.clip = clip;
            _source.time = Mathf.Repeat(Time.time * 0.37f, Mathf.Max(1f, clip.length));
            _source.Play();
            _songTime = 0f;
        }

        private void Update()
        {
            ShowTimer = Mathf.Max(0f, ShowTimer - Time.unscaledDeltaTime);
            for (int i = 0; i < 6; i++)
            {
                float[] data = null;
                lock (_lock)
                {
                    if (_pending[i] != null) { data = _pending[i]; _pending[i] = null; }
                }
                if (data == null) continue;
                _generating[i] = false;
                AudioClip clip = AudioClip.Create(Stations[i].Name + " " + _songIndex[i], data.Length, 1, AudioSynth.Rate, false);
                clip.SetData(data, 0);
                if (_songs[i] != null) Destroy(_songs[i]);
                _songs[i] = clip;
                if (i == Station && PlayerInVehicle) StartCurrent();
            }
            if (_source == null) return;
            float target = PlayerInVehicle && Station >= 0 ? Settings.MusicVolume * 0.7f : 0f;
            _source.volume = Mathf.MoveTowards(_source.volume, target, Time.unscaledDeltaTime * 1.5f);
            if (PlayerInVehicle && Station >= 0)
            {
                if (_songs[Station] == null) RequestSong(Station);
                else if (!_source.isPlaying) StartCurrent();
                _songTime += Time.unscaledDeltaTime;
                // After a song has looped a while, compose the next one.
                if (_songs[Station] != null && _songTime > _songs[Station].length * 2.2f && !_generating[Station])
                {
                    _songIndex[Station]++;
                    RequestSong(Station);
                    _songTime = 0f;
                }
            }
            else if (_source.isPlaying && _source.volume <= 0.001f)
            {
                _source.Pause();
            }
            _source.pitch = Time.timeScale < 0.9f ? Mathf.Lerp(0.7f, 1f, Time.timeScale) : 1f;
        }

        public void OnEnterVehicle(bool police)
        {
            PlayerInVehicle = true;
            if (Station >= 0)
            {
                if (_songs[Station] == null) RequestSong(Station);
                if (_source != null && _source.clip != null) _source.UnPause();
                StartCurrent();
            }
        }

        public void OnExitVehicle()
        {
            PlayerInVehicle = false;
        }
    }
}
