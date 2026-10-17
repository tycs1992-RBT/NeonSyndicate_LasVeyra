using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04E synthesis toolkit: pure float maths on top of AudioSynth's primitives (no engine calls, safe on a worker
    /// thread and in the offline tests). One-shot helpers (exponential glides, two-operator FM, struck-metal rings, bells,
    /// clicks, crackle, swept noise, tremolo) and seamless-loop helpers: tones snapped to a whole number of cycles over the
    /// loop, noise whose filters run twice around the buffer so their state wraps, and events written circularly. A loop
    /// built only from these wraps without a click or a level jump and needs no crossfade.
    /// </summary>
    public static partial class Underheart04ESounds
    {
        private const int Rate = AudioSynth.Rate;
        private const double TwoPi = System.Math.PI * 2.0;

        /// <summary>Peak of every one-shot (playback volumes stay at or below 1 on top of this).</summary>
        public const float OneShotPeak = 0.8f;
        /// <summary>Peak of the positional loops.</summary>
        public const float LoopPeak = 0.5f;
        /// <summary>Peak of the zone ambience beds (they sit under everything).</summary>
        public const float BedPeak = 0.45f;
        /// <summary>Peak of the ambience details.</summary>
        public const float DetailPeak = 0.7f;
        /// <summary>Peak of the encounter score layers.</summary>
        public const float MusicPeak = 0.5f;

        /// <summary>Deterministic xorshift noise for the recipes (each recipe passes its own seed).</summary>
        private sealed class SynthRng
        {
            private uint _s;

            public SynthRng(int seed)
            {
                _s = (uint)seed * 2654435761u + 1u;
                if (_s == 0) _s = 1u;
            }

            /// <summary>Uniform in [-1, 1).</summary>
            public float Next()
            {
                _s ^= _s << 13;
                _s ^= _s >> 17;
                _s ^= _s << 5;
                return (_s & 0xFFFFFF) / 8388608f - 1f;
            }

            /// <summary>Uniform in [0, 1).</summary>
            public float Unit()
            {
                return Next() * 0.5f + 0.5f;
            }
        }

        // ------------------------------------------------------------------ one-shot helpers

        /// <summary>Attack ramp, exponential decay and a 10 ms release (the same shape as AudioSynth's envelope).</summary>
        private static float Env(float t, float dur, float attack, float decay)
        {
            if (t < 0f || t > dur) return 0f;
            float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
            float d = t < attack || decay <= 0f ? 1f : Mathf.Exp(-(t - attack) / decay);
            float release = Mathf.Clamp01((dur - t) / 0.01f);
            return a * d * release;
        }

        private static float Osc(double phase, AudioSynth.Wave wave)
        {
            float p = (float)(phase - System.Math.Floor(phase));
            switch (wave)
            {
                case AudioSynth.Wave.Square: return p < 0.5f ? 1f : -1f;
                case AudioSynth.Wave.Saw: return p * 2f - 1f;
                case AudioSynth.Wave.Triangle: return p < 0.5f ? p * 4f - 1f : 3f - p * 4f;
                default: return (float)System.Math.Sin(p * TwoPi);
            }
        }

        /// <summary>An oscillator gliding exponentially from f0 to f1 (glides and chirps sound natural this way), with optional vibrato.</summary>
        private static void Glide(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay, AudioSynth.Wave wave, float vibHz = 0f, float vibDepth = 0f)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            double phase = 0;
            float lr = Mathf.Log(Mathf.Max(1f, f1) / Mathf.Max(1f, f0));
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx < 0 || idx >= b.Length) continue;
                float t = i / (float)Rate;
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                float f = Mathf.Max(1f, f0) * Mathf.Exp(lr * k);
                if (vibHz > 0f) f *= 1f + Mathf.Sin(t * vibHz * Mathf.PI * 2f) * vibDepth;
                phase += f / Rate;
                b[idx] += Osc(phase, wave) * amp * Env(t, dur, attack, decay);
            }
        }

        /// <summary>Two-operator FM: a sine carrier fc modulated at fc * ratio with an index sweeping i0 to i1 (bells, metal, glass).</summary>
        private static void Fm(float[] b, float start, float dur, float fc, float ratio, float i0, float i1, float amp, float attack, float decay)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            double pc = 0, pm = 0;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx < 0 || idx >= b.Length) continue;
                float t = i / (float)Rate;
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                float index = Mathf.Lerp(i0, i1, k);
                pc += fc / Rate;
                pm += fc * ratio / Rate;
                double v = System.Math.Sin(pc * TwoPi + index * System.Math.Sin(pm * TwoPi));
                b[idx] += (float)v * amp * Env(t, dur, attack, decay);
            }
        }

        private static readonly float[] RingRatios = { 1f, 2.756f, 5.404f, 8.933f };
        private static readonly float[] RingAmps = { 1f, 0.5f, 0.26f, 0.12f };
        private static readonly float[] RingDecays = { 1f, 0.62f, 0.36f, 0.2f };

        /// <summary>A struck metal part: four inharmonic modes, the higher ones dying faster (partials above 10 kHz are skipped).</summary>
        private static void Ring(float[] b, float start, float f, float amp, float decay)
        {
            for (int m = 0; m < RingRatios.Length; m++)
            {
                float fm = f * RingRatios[m];
                if (fm > 10000f) break;
                float d = decay * RingDecays[m];
                AudioSynth.Tone(b, start, Mathf.Min(d * 6f, 3f), fm, fm * 0.998f, amp * RingAmps[m], 0.0008f, d, AudioSynth.Wave.Sine);
            }
        }

        /// <summary>A bell or crystal: fundamental plus the 2.76 and 5.4 partials.</summary>
        private static void Bell(float[] b, float start, float f, float amp, float decay)
        {
            float len = Mathf.Min(decay * 5f, 3f);
            AudioSynth.Tone(b, start, len, f, f, amp, 0.002f, decay, AudioSynth.Wave.Sine);
            if (f * 2.76f < 10000f) AudioSynth.Tone(b, start, len * 0.75f, f * 2.76f, f * 2.76f, amp * 0.25f, 0.002f, decay * 0.5f, AudioSynth.Wave.Sine);
            if (f * 5.4f < 10000f) AudioSynth.Tone(b, start, len * 0.5f, f * 5.4f, f * 5.4f, amp * 0.1f, 0.002f, decay * 0.25f, AudioSynth.Wave.Sine);
        }

        /// <summary>A short mechanical click.</summary>
        private static void Click(float[] b, float start, float amp, int seed)
        {
            AudioSynth.Noise(b, start, 0.012f, amp, 0.0003f, 0.0025f, 8000f, 1200f, seed);
            AudioSynth.Tone(b, start, 0.02f, 2800f, 2200f, amp * 0.3f, 0.0003f, 0.004f, AudioSynth.Wave.Square);
        }

        /// <summary>A low body hit: a sine falling from f0 to f1.</summary>
        private static void Thump(float[] b, float start, float f0, float f1, float amp, float decay)
        {
            Glide(b, start, Mathf.Min(decay * 6f, 2.5f), f0, f1, amp, 0.002f, decay, AudioSynth.Wave.Sine);
        }

        /// <summary>count tiny noise ticks at random times inside the window (fire, gravel, sparks, rattles).</summary>
        private static void Crackle(float[] b, float start, float dur, int count, float amp, float lpHz, float hpHz, int seed, float fadeOut = 0f)
        {
            SynthRng r = new SynthRng(seed);
            for (int i = 0; i < count; i++)
            {
                float u = r.Unit();
                float t = start + u * dur;
                float a = amp * (0.35f + 0.65f * r.Unit());
                if (fadeOut > 0f) a *= Mathf.Exp(-(u * dur) / fadeOut);
                AudioSynth.Noise(b, t, 0.006f + r.Unit() * 0.01f, a, 0.0003f, 0.0025f, lpHz, hpHz, seed * 31 + i);
            }
        }

        /// <summary>Noise through a low-pass that sweeps exponentially from lp0 to lp1 (whooshes, rising steam, charges).</summary>
        private static void SweepNoise(float[] b, float start, float dur, float amp, float lp0, float lp1, float hpHz, float attack, float decay, int seed, float color = 0f)
        {
            SynthRng r = new SynthRng(seed);
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            float lr = Mathf.Log(Mathf.Max(10f, lp1) / Mathf.Max(10f, lp0));
            float hpA = hpHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * hpHz / Rate) : 0f;
            float lp = 0f, hpLow = 0f, brown = 0f;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx < 0 || idx >= b.Length) continue;
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                float cut = Mathf.Max(10f, lp0) * Mathf.Exp(lr * k);
                float lpA = 1f - Mathf.Exp(-2f * Mathf.PI * Mathf.Min(cut, 10500f) / Rate);
                float x = r.Next();
                if (color > 0f)
                {
                    brown = brown * 0.985f + x * 0.12f;
                    x = Mathf.Lerp(x, brown * 4f, color);
                }
                lp += lpA * (x - lp);
                float y = lp;
                if (hpA > 0f)
                {
                    hpLow += hpA * (y - hpLow);
                    y -= hpLow;
                }
                b[idx] += y * amp * Env(i / (float)Rate, dur, attack, decay);
            }
        }

        /// <summary>Amplitude wobble over part of the buffer.</summary>
        private static void Tremolo(float[] b, float start, float dur, float hz, float depth)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx < 0 || idx >= b.Length) continue;
                float t = i / (float)Rate;
                b[idx] *= 1f - depth * (0.5f + 0.5f * Mathf.Sin(t * hz * Mathf.PI * 2f));
            }
        }

        /// <summary>Replaces any broken sample with silence.</summary>
        private static void Clean(float[] b)
        {
            for (int i = 0; i < b.Length; i++) if (float.IsNaN(b[i]) || float.IsInfinity(b[i])) b[i] = 0f;
        }

        /// <summary>Fades the last seconds to silence so a tail is never cut with a click.</summary>
        private static void FadeTail(float[] b, float seconds)
        {
            int n = Mathf.Min(b.Length, Mathf.Max(1, (int)(seconds * Rate)));
            for (int i = 0; i < n; i++) b[b.Length - 1 - i] *= i / (float)n;
        }

        /// <summary>Fades the first samples in from silence.</summary>
        private static void FadeHead(float[] b, float seconds)
        {
            int n = Mathf.Min(b.Length, Mathf.Max(1, (int)(seconds * Rate)));
            for (int i = 0; i < n; i++) b[i] *= i / (float)n;
        }

        /// <summary>Cleans, fades both ends and normalizes a one-shot to its peak.</summary>
        private static float[] Finish(float[] b, float peak)
        {
            Clean(b);
            FadeHead(b, 0.0005f);
            FadeTail(b, 0.015f);
            AudioSynth.Normalize(b, peak);
            return b;
        }

        // ------------------------------------------------------------------ seamless loop helpers

        /// <summary>Whole cycles of hz that fit in n samples (at least one).</summary>
        private static int Cycles(float hz, int n)
        {
            return Mathf.Max(1, Mathf.RoundToInt(hz * n / (float)Rate));
        }

        /// <summary>A sine partial snapped to a whole number of cycles over the buffer, with an optional snapped tremolo.</summary>
        private static void LoopTone(float[] b, float hz, float amp, float amHz = 0f, float amDepth = 0f, float phase01 = 0f, float amPhase01 = 0f)
        {
            int n = b.Length;
            int c = Cycles(hz, n);
            int ca = amHz > 0f ? Cycles(amHz, n) : 0;
            for (int i = 0; i < n; i++)
            {
                double x = (double)c * i / n + phase01;
                float am = 1f;
                if (ca > 0) am = 1f - amDepth * 0.5f * (1f - (float)System.Math.Cos(TwoPi * ((double)ca * i / n + amPhase01)));
                b[i] += (float)System.Math.Sin(TwoPi * x) * amp * am;
            }
        }

        /// <summary>A band-limited buzz (harmonics 1 to count at 1/k) snapped to the loop, with an optional snapped tremolo.</summary>
        private static void LoopBuzz(float[] b, float hz, float amp, int count, float amHz = 0f, float amDepth = 0f)
        {
            for (int k = 1; k <= count; k++)
            {
                if (hz * k > 9000f) break;
                LoopTone(b, hz * k, amp / k, amHz, amDepth, k * 0.13f);
            }
        }

        /// <summary>
        /// Filtered noise that loops seamlessly: the noise sequence repeats with the buffer and the filters run once around
        /// to settle, then a second time to write, so the filter state at the end matches the start. An optional snapped
        /// tremolo shapes it (breathing, gurgle, throb).
        /// </summary>
        private static void LoopNoise(float[] b, float amp, float lpHz, float hpHz, int seed, float color = 0f, float amHz = 0f, float amDepth = 0f, float amPhase01 = 0f)
        {
            int n = b.Length;
            SynthRng r = new SynthRng(seed);
            float[] w = new float[n];
            for (int i = 0; i < n; i++) w[i] = r.Next();
            float lpA = lpHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * lpHz / Rate) : 1f;
            float hpA = hpHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * hpHz / Rate) : 0f;
            int ca = amHz > 0f ? Cycles(amHz, n) : 0;
            float lp = 0f, hpLow = 0f, brown = 0f;
            for (int lap = 0; lap < 2; lap++)
            {
                for (int i = 0; i < n; i++)
                {
                    float x = w[i];
                    if (color > 0f)
                    {
                        brown = brown * 0.985f + x * 0.12f;
                        x = Mathf.Lerp(x, brown * 4f, color);
                    }
                    lp += lpA * (x - lp);
                    float y = lp;
                    if (hpA > 0f)
                    {
                        hpLow += hpA * (y - hpLow);
                        y -= hpLow;
                    }
                    if (lap == 0) continue;
                    float am = 1f;
                    if (ca > 0) am = 1f - amDepth * 0.5f * (1f - (float)System.Math.Cos(TwoPi * ((double)ca * i / n + amPhase01)));
                    b[i] += y * amp * am;
                }
            }
        }

        /// <summary>Adds an event buffer into a loop at a time, wrapping past the end (so an event near the end continues at the start).</summary>
        private static void LoopAdd(float[] b, float[] ev, float at)
        {
            int n = b.Length;
            int s0 = ((int)(at * Rate) % n + n) % n;
            for (int i = 0; i < ev.Length; i++) b[(s0 + i) % n] += ev[i];
        }

        /// <summary>Scatters count tiny crackles through a loop (circularly), away from nothing in particular: a texture, not events.</summary>
        private static void LoopCrackle(float[] b, int count, float amp, float lpHz, float hpHz, int seed)
        {
            SynthRng r = new SynthRng(seed);
            float len = b.Length / (float)Rate;
            float[] ev = AudioSynth.Buf(0.02f);
            for (int i = 0; i < count; i++)
            {
                System.Array.Clear(ev, 0, ev.Length);
                float a = amp * (0.3f + 0.7f * r.Unit() * r.Unit());
                AudioSynth.Noise(ev, 0f, 0.005f + r.Unit() * 0.01f, a, 0.0003f, 0.0025f, lpHz, hpHz, seed * 17 + i);
                LoopAdd(b, ev, r.Unit() * len);
            }
        }

        /// <summary>Normalizes a loop buffer (cleaned) to its peak.</summary>
        private static float[] FinishLoop(float[] b, float peak)
        {
            Clean(b);
            AudioSynth.Normalize(b, peak);
            return b;
        }
    }
}
