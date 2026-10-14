using System;
using UnityEngine;

namespace NeonSyndicate
{
    public enum SfxId
    {
        Pistol, Revolver, SMG, Shotgun, Rifle, Sniper, Minigun, RocketLaunch, GrenadeLauncher, Silenced,
        Explosion, ExplosionSmall, Punch, BatHit, KnifeSlash, Swing, BulletImpact, BulletFlesh, Ricochet, MetalHit,
        GlassBreak, Crash, CrashHeavy, BodyFall, Reload, ReloadShotgun, EmptyClick, WeaponSwitch, Footstep, Jump,
        Land, DoorOpen, DoorClose, CarAlarm, Horn, HornTruck, Siren, SirenYelp, TireScreech, EngineSmall,
        EngineV8, EngineSport, EngineTruck, EngineBike, Helicopter, CashPickup, ItemPickup, HealthPickup, ArmorPickup, Collectible,
        MissionPassed, MissionFailed, Wasted, Busted, StarUp, StarsLost, UIClick, UIHover, UIBack, PhoneRing,
        PhoneOpen, Notify, Splash, Whoosh, CrystalChime, Power, Shield, Heartbeat, SlotSpin, SlotWin,
        Thunder, RainLoop, WindLoop, CityLoop, FireLoop, WaterSpray, Throw, Pin, Fuse, Beep,
        Count
    }

    /// <summary>
    /// Procedural sound effects. Every clip in the game is synthesized from oscillators, filtered noise, envelopes and
    /// echoes, so the project ships with no audio files. Pure C# float math (no engine calls) until the final clip.
    /// </summary>
    public static class AudioSynth
    {
        public const int Rate = 22050;

        public enum Wave { Sine, Square, Saw, Triangle }

        public static float[] Buf(float seconds)
        {
            return new float[Mathf.Max(1, (int)(seconds * Rate))];
        }

        private sealed class Rng
        {
            private uint _s;
            public Rng(int seed) { _s = (uint)seed * 2654435761u + 1u; }
            public float Next()
            {
                _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5;
                return (_s & 0xFFFFFF) / 8388608f - 1f;
            }
        }

        private static float Env(float t, float dur, float attack, float decay)
        {
            if (t < 0f || t > dur) return 0f;
            float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
            float d = decay > 0f ? Mathf.Exp(-(t - attack) / decay) : 1f;
            if (t < attack) d = 1f;
            float release = Mathf.Clamp01((dur - t) / 0.01f);
            return a * d * release;
        }

        /// <summary>Adds filtered noise. lpHz/hpHz of 0 disable that filter. color: 0 white, 1 brown-ish.</summary>
        public static void Noise(float[] b, float start, float dur, float amp, float attack, float decay, float lpHz, float hpHz, int seed, float color = 0f)
        {
            Rng r = new Rng(seed);
            float lpA = lpHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * lpHz / Rate) : 1f;
            float hpA = hpHz > 0f ? 1f - Mathf.Exp(-2f * Mathf.PI * hpHz / Rate) : 0f;
            float lp = 0f, hpLow = 0f, brown = 0f;
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx < 0 || idx >= b.Length) continue;
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

        public static void Tone(float[] b, float start, float dur, float f0, float f1, float amp, float attack, float decay, Wave wave, float vibratoHz = 0f, float vibratoDepth = 0f)
        {
            int s0 = (int)(start * Rate);
            int n = (int)(dur * Rate);
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                int idx = s0 + i;
                if (idx < 0 || idx >= b.Length) continue;
                float t = i / (float)Rate;
                float k = n > 1 ? i / (float)(n - 1) : 0f;
                float f = Mathf.Lerp(f0, f1, k);
                if (vibratoHz > 0f) f *= 1f + Mathf.Sin(t * vibratoHz * Mathf.PI * 2f) * vibratoDepth;
                phase += f / Rate;
                float p = (float)(phase - Math.Floor(phase));
                float v;
                switch (wave)
                {
                    case Wave.Square: v = p < 0.5f ? 1f : -1f; break;
                    case Wave.Saw: v = p * 2f - 1f; break;
                    case Wave.Triangle: v = p < 0.5f ? p * 4f - 1f : 3f - p * 4f; break;
                    default: v = Mathf.Sin(p * Mathf.PI * 2f); break;
                }
                b[idx] += v * amp * Env(t, dur, attack, decay);
            }
        }

        public static void Echo(float[] b, float delay, float gain, int taps)
        {
            int d = (int)(delay * Rate);
            if (d <= 0) return;
            for (int t = 0; t < taps; t++)
            {
                for (int i = b.Length - 1; i >= d; i--) b[i] += b[i - d] * gain;
            }
        }

        public static void LowPass(float[] b, float hz)
        {
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * hz / Rate);
            float y = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                y += a * (b[i] - y);
                b[i] = y;
            }
        }

        public static void Drive(float[] b, float amount)
        {
            float norm = (float)Math.Tanh(amount);
            for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * amount) / norm;
        }

        public static void Normalize(float[] b, float peak)
        {
            float m = 0f;
            for (int i = 0; i < b.Length; i++) m = Mathf.Max(m, Mathf.Abs(b[i]));
            if (m < 1e-6f) return;
            float g = peak / m;
            for (int i = 0; i < b.Length; i++) b[i] *= g;
        }

        /// <summary>Crossfades the tail into the head and trims it so looping clips do not click.</summary>
        public static float[] Looped(float[] b, float fadeSeconds)
        {
            int f = Mathf.Min(b.Length / 4, (int)(fadeSeconds * Rate));
            if (f <= 0) return b;
            float[] o = new float[b.Length - f];
            Array.Copy(b, o, o.Length);
            for (int i = 0; i < f; i++)
            {
                float t = i / (float)f;
                o[i] = b[i] * t + b[b.Length - f + i] * (1f - t);
            }
            return o;
        }

        // ------------------------------------------------------------------ recipes

        private static float[] Gun(float decay, float cutoff, float thump, float crack, float tail, int seed)
        {
            float[] b = Buf(Mathf.Max(0.5f, decay * 5f + tail * 1.5f));
            Noise(b, 0f, decay * 5f, 0.9f, 0.0008f, decay, cutoff, 80f, seed);
            Noise(b, 0f, 0.004f, crack, 0f, 0.0015f, 0f, 2000f, seed + 1);
            Tone(b, 0f, 0.18f, 160f, 45f, thump, 0.001f, 0.045f, Wave.Sine);
            if (tail > 0f)
            {
                float[] e = new float[b.Length];
                Array.Copy(b, e, b.Length);
                LowPass(e, 1200f);
                for (int i = 0; i < b.Length; i++)
                {
                    int d1 = i - (int)(0.09f * Rate);
                    int d2 = i - (int)(0.21f * Rate);
                    if (d1 >= 0) b[i] += e[d1] * 0.32f * tail;
                    if (d2 >= 0) b[i] += e[d2] * 0.18f * tail;
                }
            }
            Drive(b, 1.6f);
            Normalize(b, 0.95f);
            return b;
        }

        public static float[] Make(SfxId id)
        {
            float[] b;
            switch (id)
            {
                case SfxId.Pistol: return Gun(0.07f, 3800f, 0.6f, 0.8f, 0.8f, 11);
                case SfxId.Revolver: return Gun(0.13f, 2600f, 1.0f, 0.9f, 1.0f, 12);
                case SfxId.SMG: return Gun(0.045f, 4200f, 0.45f, 0.7f, 0.5f, 13);
                case SfxId.Shotgun: return Gun(0.2f, 1900f, 1.3f, 0.7f, 1.2f, 14);
                case SfxId.Rifle: return Gun(0.085f, 3300f, 0.75f, 1.0f, 0.9f, 15);
                case SfxId.Sniper: return Gun(0.3f, 2300f, 1.1f, 1.0f, 1.6f, 16);
                case SfxId.Minigun: return Gun(0.03f, 3600f, 0.4f, 0.6f, 0.2f, 17);
                case SfxId.Silenced:
                    b = Buf(0.25f);
                    Noise(b, 0f, 0.12f, 0.8f, 0.001f, 0.02f, 1800f, 200f, 18);
                    Tone(b, 0f, 0.08f, 220f, 90f, 0.5f, 0.001f, 0.02f, Wave.Sine);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.RocketLaunch:
                    b = Buf(1.2f);
                    Noise(b, 0f, 1.2f, 0.9f, 0.01f, 0.45f, 2200f, 60f, 19);
                    Tone(b, 0f, 0.5f, 90f, 40f, 0.8f, 0.002f, 0.15f, Wave.Sine);
                    Normalize(b, 0.95f);
                    return b;
                case SfxId.GrenadeLauncher:
                    b = Buf(0.5f);
                    Tone(b, 0f, 0.25f, 140f, 50f, 1f, 0.001f, 0.06f, Wave.Sine);
                    Noise(b, 0f, 0.15f, 0.5f, 0.001f, 0.03f, 900f, 50f, 20);
                    Normalize(b, 0.9f);
                    return b;
                case SfxId.Explosion:
                    b = Buf(2.6f);
                    Noise(b, 0f, 2.6f, 1f, 0.004f, 0.55f, 900f, 20f, 21, 0.85f);
                    Noise(b, 0f, 0.4f, 0.6f, 0.001f, 0.08f, 5000f, 300f, 22);
                    Tone(b, 0f, 1.4f, 70f, 28f, 1.2f, 0.002f, 0.35f, Wave.Sine);
                    Echo(b, 0.23f, 0.25f, 1);
                    Drive(b, 2.2f);
                    Normalize(b, 1f);
                    return b;
                case SfxId.ExplosionSmall:
                    b = Buf(1.2f);
                    Noise(b, 0f, 1.2f, 1f, 0.003f, 0.25f, 1400f, 40f, 23, 0.6f);
                    Tone(b, 0f, 0.6f, 90f, 40f, 0.8f, 0.002f, 0.15f, Wave.Sine);
                    Drive(b, 1.8f);
                    Normalize(b, 0.95f);
                    return b;
                case SfxId.Punch:
                    b = Buf(0.22f);
                    Tone(b, 0f, 0.12f, 110f, 60f, 1f, 0.001f, 0.03f, Wave.Sine);
                    Noise(b, 0f, 0.05f, 0.6f, 0.001f, 0.012f, 2500f, 200f, 24);
                    Normalize(b, 0.9f);
                    return b;
                case SfxId.BatHit:
                    b = Buf(0.35f);
                    Tone(b, 0f, 0.15f, 95f, 55f, 1f, 0.001f, 0.04f, Wave.Sine);
                    Tone(b, 0f, 0.2f, 620f, 580f, 0.4f, 0.001f, 0.05f, Wave.Triangle);
                    Noise(b, 0f, 0.06f, 0.5f, 0.001f, 0.015f, 3000f, 300f, 25);
                    Normalize(b, 0.9f);
                    return b;
                case SfxId.KnifeSlash:
                case SfxId.Swing:
                case SfxId.Whoosh:
                    b = Buf(0.3f);
                    Noise(b, 0f, 0.3f, 1f, 0.08f, 0.06f, id == SfxId.KnifeSlash ? 5000f : 2200f, 600f, 26);
                    Normalize(b, id == SfxId.Whoosh ? 0.6f : 0.75f);
                    return b;
                case SfxId.BulletImpact:
                    b = Buf(0.18f);
                    Noise(b, 0f, 0.1f, 0.9f, 0.0005f, 0.012f, 6000f, 800f, 27);
                    Tone(b, 0f, 0.06f, 1800f, 1300f, 0.25f, 0.0005f, 0.015f, Wave.Sine);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.BulletFlesh:
                    b = Buf(0.15f);
                    Tone(b, 0f, 0.08f, 140f, 80f, 0.9f, 0.001f, 0.02f, Wave.Sine);
                    Noise(b, 0f, 0.05f, 0.5f, 0.001f, 0.01f, 1200f, 100f, 28);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.Ricochet:
                    b = Buf(0.45f);
                    Tone(b, 0f, 0.4f, 3200f, 1400f, 0.6f, 0.002f, 0.15f, Wave.Sine, 30f, 0.03f);
                    Noise(b, 0f, 0.02f, 0.5f, 0.0005f, 0.006f, 0f, 2000f, 29);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.MetalHit:
                    b = Buf(0.6f);
                    Tone(b, 0f, 0.6f, 820f, 815f, 0.5f, 0.001f, 0.12f, Wave.Sine);
                    Tone(b, 0f, 0.6f, 1270f, 1265f, 0.35f, 0.001f, 0.09f, Wave.Sine);
                    Tone(b, 0f, 0.6f, 1910f, 1900f, 0.25f, 0.001f, 0.07f, Wave.Sine);
                    Noise(b, 0f, 0.03f, 0.6f, 0.0005f, 0.008f, 7000f, 500f, 30);
                    Normalize(b, 0.75f);
                    return b;
                case SfxId.GlassBreak:
                    b = Buf(0.8f);
                    Noise(b, 0f, 0.5f, 0.8f, 0.001f, 0.08f, 0f, 3000f, 31);
                    for (int k = 0; k < 9; k++) Tone(b, 0.02f + k * 0.045f, 0.25f, 2600f + k * 410f, 2600f + k * 400f, 0.25f, 0.001f, 0.05f, Wave.Sine);
                    Normalize(b, 0.8f);
                    return b;
                case SfxId.Crash:
                case SfxId.CrashHeavy:
                    b = Buf(id == SfxId.CrashHeavy ? 1.4f : 0.9f);
                    Noise(b, 0f, b.Length / (float)Rate, 1f, 0.002f, id == SfxId.CrashHeavy ? 0.3f : 0.15f, 2400f, 40f, 32, 0.4f);
                    Tone(b, 0f, 0.3f, 75f, 40f, 1f, 0.001f, 0.08f, Wave.Sine);
                    Tone(b, 0.01f, 0.5f, 690f, 680f, 0.3f, 0.001f, 0.1f, Wave.Sine);
                    Tone(b, 0.01f, 0.5f, 1050f, 1040f, 0.2f, 0.001f, 0.08f, Wave.Sine);
                    Drive(b, 1.5f);
                    Normalize(b, 0.95f);
                    return b;
                case SfxId.BodyFall:
                    b = Buf(0.35f);
                    Tone(b, 0f, 0.2f, 85f, 50f, 1f, 0.002f, 0.05f, Wave.Sine);
                    Noise(b, 0f, 0.12f, 0.4f, 0.002f, 0.03f, 900f, 60f, 33);
                    Normalize(b, 0.75f);
                    return b;
                case SfxId.Reload:
                    b = Buf(0.6f);
                    Click(b, 0.02f, 34, 0.9f);
                    Click(b, 0.32f, 35, 1f);
                    Click(b, 0.4f, 36, 0.6f);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.ReloadShotgun:
                    b = Buf(0.45f);
                    Click(b, 0.0f, 37, 0.8f);
                    Noise(b, 0.08f, 0.12f, 0.5f, 0.01f, 0.03f, 1800f, 300f, 38);
                    Click(b, 0.25f, 39, 1f);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.EmptyClick:
                    b = Buf(0.1f);
                    Click(b, 0f, 40, 1f);
                    Normalize(b, 0.6f);
                    return b;
                case SfxId.WeaponSwitch:
                    b = Buf(0.25f);
                    Click(b, 0f, 41, 0.6f);
                    Noise(b, 0.03f, 0.15f, 0.3f, 0.02f, 0.05f, 2500f, 400f, 42);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.Footstep:
                    b = Buf(0.12f);
                    Noise(b, 0f, 0.1f, 1f, 0.002f, 0.018f, 1500f, 120f, 43);
                    Tone(b, 0f, 0.05f, 120f, 70f, 0.4f, 0.001f, 0.012f, Wave.Sine);
                    Normalize(b, 0.45f);
                    return b;
                case SfxId.Jump:
                    b = Buf(0.2f);
                    Noise(b, 0f, 0.15f, 0.6f, 0.01f, 0.03f, 1200f, 200f, 44);
                    Normalize(b, 0.35f);
                    return b;
                case SfxId.Land:
                    b = Buf(0.25f);
                    Tone(b, 0f, 0.15f, 95f, 55f, 0.8f, 0.001f, 0.04f, Wave.Sine);
                    Noise(b, 0f, 0.12f, 0.7f, 0.001f, 0.025f, 1300f, 100f, 45);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.DoorOpen:
                case SfxId.DoorClose:
                    b = Buf(0.4f);
                    Click(b, 0f, 46, 0.7f);
                    Tone(b, id == SfxId.DoorClose ? 0.02f : 0.08f, 0.2f, 90f, 60f, id == SfxId.DoorClose ? 1f : 0.5f, 0.001f, 0.05f, Wave.Sine);
                    Noise(b, 0.02f, 0.12f, 0.4f, 0.002f, 0.04f, 1500f, 100f, 47);
                    Normalize(b, 0.65f);
                    return b;
                case SfxId.CarAlarm:
                    b = Buf(1.2f);
                    for (int k = 0; k < 6; k++) Tone(b, k * 0.2f, 0.19f, k % 2 == 0 ? 950f : 1350f, k % 2 == 0 ? 950f : 1350f, 0.6f, 0.005f, 0f, Wave.Square);
                    LowPass(b, 3500f);
                    Normalize(b, 0.6f);
                    return b;
                case SfxId.Horn:
                case SfxId.HornTruck:
                {
                    // Loop length holds a whole number of cycles of both notes, with no fade at either end, so a held horn is seamless.
                    float hf = id == SfxId.HornTruck ? 0.55f : 1f;
                    float len = id == SfxId.HornTruck ? 6f / 11f : 0.6f;
                    b = Buf(len);
                    Tone(b, 0f, len + 0.02f, 350f * hf, 350f * hf, 0.5f, 0f, 0f, Wave.Square);
                    Tone(b, 0f, len + 0.02f, 440f * hf, 440f * hf, 0.5f, 0f, 0f, Wave.Square);
                }
                    LowPass(b, 2200f);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.Siren:
                    b = Buf(4f);
                    {
                        double ph = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)Rate;
                            float f = 950f + 450f * Mathf.Sin(t / 4f * Mathf.PI * 2f - Mathf.PI * 0.5f);
                            ph += f / Rate;
                            b[i] = Mathf.Sin((float)(ph - Math.Floor(ph)) * Mathf.PI * 2f) * 0.6f + Mathf.Sin((float)((ph * 2) - Math.Floor(ph * 2)) * Mathf.PI * 2f) * 0.15f;
                        }
                    }
                    Normalize(b, 0.6f);
                    return b;
                case SfxId.SirenYelp:
                    b = Buf(1.6f);
                    {
                        double ph = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            float t = i / (float)Rate;
                            float k = Mathf.Repeat(t / 0.4f, 1f);
                            float f = 800f + 900f * (k < 0.5f ? k * 2f : 2f - k * 2f);
                            ph += f / Rate;
                            b[i] = Mathf.Sin((float)(ph - Math.Floor(ph)) * Mathf.PI * 2f) * 0.6f;
                        }
                    }
                    Normalize(b, 0.6f);
                    return b;
                case SfxId.TireScreech:
                    b = Buf(1f);
                    Noise(b, 0f, 1.02f, 1f, 0f, 0f, 3600f, 1600f, 48);
                    for (int i = 0; i < b.Length; i++) b[i] *= 0.7f + 0.3f * Mathf.Sin(i / (float)Rate * 37f * Mathf.PI * 2f);
                    Tone(b, 0f, 1.02f, 2100f, 2100f, 0.15f, 0f, 0f, Wave.Sine, 9f, 0.04f);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.EngineSmall: return Engine(32f, 0.6f, 0.25f, 49);
                case SfxId.EngineV8: return Engine(24f, 1.0f, 0.35f, 50);
                case SfxId.EngineSport: return Engine(38f, 0.8f, 0.2f, 51);
                case SfxId.EngineTruck: return Engine(18f, 1.1f, 0.45f, 52);
                case SfxId.EngineBike: return Engine(42f, 0.7f, 0.3f, 53);
                case SfxId.Helicopter:
                    b = Buf(2f);
                    for (int k = 0; k < 11; k++)
                    {
                        float t0 = k / 5.5f;
                        Noise(b, t0, 0.17f, 0.8f, 0.01f, 0.05f, 700f, 40f, 54 + k, 0.5f);
                        Tone(b, t0, 0.12f, 70f, 55f, 0.5f, 0.005f, 0.04f, Wave.Sine);
                    }
                    // Eleven blade beats in exactly two seconds: the loop keeps a steady rhythm without trimming.
                    Tone(b, 0f, 2.02f, 140f, 140f, 0.08f, 0f, 0f, Wave.Saw);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.CashPickup:
                    b = Buf(0.5f);
                    Bell(b, 0f, 1047f, 0.6f);
                    Bell(b, 0.07f, 1319f, 0.6f);
                    Bell(b, 0.14f, 1568f, 0.7f);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.ItemPickup:
                    b = Buf(0.3f);
                    Tone(b, 0f, 0.1f, 660f, 660f, 0.5f, 0.002f, 0.05f, Wave.Triangle);
                    Tone(b, 0.08f, 0.15f, 990f, 990f, 0.5f, 0.002f, 0.07f, Wave.Triangle);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.HealthPickup:
                    b = Buf(0.5f);
                    Tone(b, 0f, 0.45f, 440f, 880f, 0.6f, 0.01f, 0.2f, Wave.Sine);
                    Tone(b, 0f, 0.45f, 660f, 1320f, 0.3f, 0.01f, 0.2f, Wave.Sine);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.ArmorPickup:
                    b = Buf(0.6f);
                    Tone(b, 0f, 0.5f, 300f, 300f, 0.4f, 0.005f, 0.15f, Wave.Saw);
                    Bell(b, 0.05f, 1400f, 0.4f);
                    LowPass(b, 3000f);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.Collectible:
                case SfxId.CrystalChime:
                    b = Buf(1.4f);
                    float[] notes = { 1047f, 1319f, 1568f, 2093f, 2637f };
                    for (int k = 0; k < notes.Length; k++) Bell(b, k * 0.08f, notes[k] * (id == SfxId.CrystalChime ? 0.75f : 1f), 0.5f);
                    Echo(b, 0.18f, 0.3f, 2);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.MissionPassed:
                    b = Buf(3f);
                    Chord(b, 0f, 0.55f, new[] { 261.6f, 329.6f, 392f }, 0.35f);
                    Chord(b, 0.5f, 0.55f, new[] { 349.2f, 440f, 523.3f }, 0.35f);
                    Chord(b, 1.0f, 0.55f, new[] { 392f, 493.9f, 587.3f }, 0.35f);
                    Chord(b, 1.5f, 1.4f, new[] { 523.3f, 659.3f, 784f, 1046.5f }, 0.4f);
                    Echo(b, 0.25f, 0.25f, 2);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.MissionFailed:
                    b = Buf(2.5f);
                    Chord(b, 0f, 0.7f, new[] { 392f, 466.2f, 587.3f }, 0.35f);
                    Chord(b, 0.65f, 1.6f, new[] { 293.7f, 349.2f, 440f }, 0.35f);
                    Normalize(b, 0.65f);
                    return b;
                case SfxId.Wasted:
                    b = Buf(3f);
                    Tone(b, 0f, 2.8f, 220f, 55f, 0.6f, 0.01f, 1.2f, Wave.Saw);
                    Tone(b, 0f, 2.8f, 110f, 27f, 0.5f, 0.01f, 1.2f, Wave.Sine);
                    Noise(b, 0f, 1f, 0.5f, 0.002f, 0.3f, 600f, 30f, 60, 0.8f);
                    LowPass(b, 2000f);
                    Normalize(b, 0.8f);
                    return b;
                case SfxId.Busted:
                    b = Buf(2.5f);
                    Tone(b, 0f, 0.3f, 1200f, 800f, 0.5f, 0.005f, 0.1f, Wave.Square);
                    Tone(b, 0.3f, 0.3f, 1200f, 800f, 0.5f, 0.005f, 0.1f, Wave.Square);
                    Chord(b, 0.6f, 1.8f, new[] { 196f, 233.1f, 293.7f }, 0.4f);
                    LowPass(b, 3000f);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.StarUp:
                    b = Buf(0.5f);
                    Tone(b, 0f, 0.12f, 1500f, 1500f, 0.5f, 0.003f, 0.05f, Wave.Square);
                    Tone(b, 0.18f, 0.12f, 1500f, 1500f, 0.5f, 0.003f, 0.05f, Wave.Square);
                    LowPass(b, 4000f);
                    Normalize(b, 0.45f);
                    return b;
                case SfxId.StarsLost:
                    b = Buf(1.2f);
                    Bell(b, 0f, 1568f, 0.5f);
                    Bell(b, 0.15f, 1319f, 0.5f);
                    Bell(b, 0.3f, 1047f, 0.6f);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.UIClick:
                case SfxId.UIHover:
                case SfxId.UIBack:
                    b = Buf(0.08f);
                    Tone(b, 0f, 0.06f, id == SfxId.UIBack ? 500f : (id == SfxId.UIHover ? 1400f : 1000f), id == SfxId.UIBack ? 400f : 1000f, 0.6f, 0.001f, 0.015f, Wave.Triangle);
                    Normalize(b, id == SfxId.UIHover ? 0.2f : 0.4f);
                    return b;
                case SfxId.PhoneRing:
                    b = Buf(2f);
                    for (int k = 0; k < 2; k++)
                    {
                        Tone(b, k * 0.5f, 0.4f, 1300f, 1300f, 0.4f, 0.005f, 0f, Wave.Sine, 18f, 0.05f);
                        Tone(b, k * 0.5f, 0.4f, 1700f, 1700f, 0.3f, 0.005f, 0f, Wave.Sine, 18f, 0.05f);
                    }
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.PhoneOpen:
                case SfxId.Notify:
                case SfxId.Beep:
                    b = Buf(0.35f);
                    Bell(b, 0f, id == SfxId.PhoneOpen ? 880f : (id == SfxId.Beep ? 1760f : 1320f), 0.5f);
                    if (id == SfxId.Notify) Bell(b, 0.09f, 1760f, 0.4f);
                    Normalize(b, 0.4f);
                    return b;
                case SfxId.Splash:
                    b = Buf(0.9f);
                    Noise(b, 0f, 0.9f, 1f, 0.01f, 0.2f, 2500f, 150f, 61);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.Power:
                    b = Buf(0.9f);
                    Tone(b, 0f, 0.8f, 200f, 1200f, 0.5f, 0.02f, 0.4f, Wave.Saw);
                    Tone(b, 0f, 0.8f, 300f, 1800f, 0.3f, 0.02f, 0.4f, Wave.Sine);
                    LowPass(b, 3500f);
                    Normalize(b, 0.6f);
                    return b;
                case SfxId.Shield:
                    b = Buf(1f);
                    Tone(b, 0f, 1f, 110f, 110f, 0.5f, 0.05f, 0.5f, Wave.Saw);
                    for (int k = 0; k < 4; k++) Bell(b, k * 0.1f, 1800f + k * 300f, 0.25f);
                    LowPass(b, 3000f);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.Heartbeat:
                    b = Buf(0.8f);
                    Tone(b, 0f, 0.12f, 70f, 45f, 1f, 0.005f, 0.04f, Wave.Sine);
                    Tone(b, 0.22f, 0.12f, 65f, 42f, 0.8f, 0.005f, 0.04f, Wave.Sine);
                    Normalize(b, 0.7f);
                    return b;
                case SfxId.SlotSpin:
                    b = Buf(0.8f);
                    for (int k = 0; k < 8; k++) Click(b, k * 0.1f, 62 + k, 0.6f);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.SlotWin:
                    b = Buf(2f);
                    for (int k = 0; k < 10; k++) Bell(b, k * 0.1f, 1047f * Mathf.Pow(1.122f, k % 6), 0.4f);
                    Normalize(b, 0.6f);
                    return b;
                case SfxId.Thunder:
                    b = Buf(4f);
                    Noise(b, 0f, 0.3f, 0.8f, 0.002f, 0.08f, 3000f, 100f, 70);
                    Noise(b, 0.05f, 3.9f, 1f, 0.2f, 1.2f, 300f, 20f, 71, 0.9f);
                    Drive(b, 1.5f);
                    Normalize(b, 0.9f);
                    return b;
                case SfxId.RainLoop:
                    b = Buf(3f);
                    Noise(b, 0f, 3f, 0.6f, 0f, 0f, 5000f, 400f, 72);
                    {
                        Rng r = new Rng(73);
                        for (int k = 0; k < 160; k++) Noise(b, (r.Next() * 0.5f + 0.5f) * 2.9f, 0.02f, 0.4f, 0.001f, 0.005f, 0f, 2500f, 74 + k);
                    }
                    b = Looped(b, 0.2f);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.WindLoop:
                    b = Buf(4f);
                    Noise(b, 0f, 4f, 1f, 0f, 0f, 500f, 60f, 75, 0.6f);
                    for (int i = 0; i < b.Length; i++) b[i] *= 0.6f + 0.4f * Mathf.Sin(i / (float)Rate * 0.5f * Mathf.PI * 2f);
                    b = Looped(b, 0.3f);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.CityLoop:
                    b = Buf(6f);
                    Noise(b, 0f, 6f, 1f, 0f, 0f, 260f, 30f, 76, 0.8f);
                    Noise(b, 0f, 6f, 0.25f, 0f, 0f, 1800f, 500f, 77);
                    Tone(b, 1.3f, 0.35f, 400f, 400f, 0.06f, 0.02f, 0f, Wave.Square);
                    Tone(b, 4.1f, 0.25f, 520f, 520f, 0.05f, 0.02f, 0f, Wave.Square);
                    LowPass(b, 2500f);
                    b = Looped(b, 0.4f);
                    Normalize(b, 0.45f);
                    return b;
                case SfxId.FireLoop:
                    b = Buf(2f);
                    Noise(b, 0f, 2f, 0.7f, 0f, 0f, 1200f, 80f, 78, 0.5f);
                    {
                        Rng r = new Rng(79);
                        for (int k = 0; k < 60; k++) Noise(b, (r.Next() * 0.5f + 0.5f) * 1.95f, 0.015f, 0.6f, 0.001f, 0.004f, 0f, 1500f, 80 + k);
                    }
                    b = Looped(b, 0.15f);
                    Normalize(b, 0.55f);
                    return b;
                case SfxId.WaterSpray:
                    b = Buf(2f);
                    Noise(b, 0f, 2f, 1f, 0f, 0f, 7000f, 1500f, 81);
                    b = Looped(b, 0.15f);
                    Normalize(b, 0.4f);
                    return b;
                case SfxId.Throw:
                    b = Buf(0.3f);
                    Noise(b, 0f, 0.25f, 1f, 0.05f, 0.05f, 1500f, 300f, 82);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.Pin:
                    b = Buf(0.2f);
                    Click(b, 0f, 83, 0.8f);
                    Tone(b, 0.01f, 0.15f, 2400f, 2400f, 0.2f, 0.001f, 0.04f, Wave.Sine);
                    Normalize(b, 0.5f);
                    return b;
                case SfxId.Fuse:
                    b = Buf(1f);
                    Noise(b, 0f, 1f, 0.6f, 0.01f, 0f, 7000f, 2500f, 84);
                    b = Looped(b, 0.1f);
                    Normalize(b, 0.35f);
                    return b;
                default:
                    b = Buf(0.1f);
                    Click(b, 0f, 99, 0.5f);
                    return b;
            }
        }

        private static void Click(float[] b, float start, int seed, float amp)
        {
            Noise(b, start, 0.012f, amp, 0.0003f, 0.0025f, 8000f, 1200f, seed);
            Tone(b, start, 0.02f, 2800f, 2200f, amp * 0.3f, 0.0003f, 0.004f, Wave.Square);
        }

        private static void Bell(float[] b, float start, float freq, float amp)
        {
            Tone(b, start, 0.8f, freq, freq, amp, 0.002f, 0.25f, Wave.Sine);
            Tone(b, start, 0.6f, freq * 2.76f, freq * 2.76f, amp * 0.25f, 0.002f, 0.12f, Wave.Sine);
            Tone(b, start, 0.4f, freq * 5.4f, freq * 5.4f, amp * 0.1f, 0.002f, 0.06f, Wave.Sine);
        }

        private static void Chord(float[] b, float start, float dur, float[] freqs, float amp)
        {
            foreach (float f in freqs)
            {
                Tone(b, start, dur, f, f, amp / freqs.Length, 0.02f, dur * 0.6f, Wave.Saw);
                Tone(b, start, dur, f * 2f, f * 2f, amp * 0.3f / freqs.Length, 0.02f, dur * 0.5f, Wave.Sine);
            }
        }

        /// <summary>Engine loop: pulse train at the firing frequency with harmonics; pitched at runtime by RPM.</summary>
        private static float[] Engine(float fire, float grit, float noise, int seed)
        {
            int period = Mathf.Max(8, Mathf.RoundToInt(Rate / fire));
            int periods = Mathf.Max(4, Mathf.RoundToInt(0.6f * Rate / period));
            float[] b = new float[period * periods];
            Rng r = new Rng(seed);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float p = (i % period) / (float)period;
                float pulse = Mathf.Exp(-p * 9f) * 2f - 0.25f;
                float harm = Mathf.Sin(p * Mathf.PI * 2f) * 0.6f + Mathf.Sin(p * Mathf.PI * 4f) * 0.35f * grit + Mathf.Sin(p * Mathf.PI * 6f) * 0.2f * grit;
                float n = r.Next() * noise * (0.5f + Mathf.Exp(-p * 6f));
                float x = pulse * 0.5f + harm + n;
                lp += 0.35f * (x - lp);
                b[i] = lp;
            }
            Drive(b, 1.3f + grit);
            Normalize(b, 0.6f);
            return b;
        }

        public static bool IsLoop(SfxId id)
        {
            switch (id)
            {
                case SfxId.Siren:
                case SfxId.SirenYelp:
                case SfxId.TireScreech:
                case SfxId.EngineSmall:
                case SfxId.EngineV8:
                case SfxId.EngineSport:
                case SfxId.EngineTruck:
                case SfxId.EngineBike:
                case SfxId.Helicopter:
                case SfxId.RainLoop:
                case SfxId.WindLoop:
                case SfxId.CityLoop:
                case SfxId.FireLoop:
                case SfxId.WaterSpray:
                case SfxId.Horn:
                case SfxId.HornTruck:
                case SfxId.Fuse:
                case SfxId.CarAlarm:
                    return true;
                default:
                    return false;
            }
        }
    }
}
