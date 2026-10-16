using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The crystal sound recipes, pure float maths on AudioSynth's primitives (no engine calls), so the offline tests can
    /// check every clip: one distinct shot per family (Shardwake's glassy tink, Prism Thread's ignite and cooling release,
    /// Gyre Crescent's whirring throw, Sunderheart's dense stone thoom, Mirage Choir's staggered five-voice chirp,
    /// Mirrorwake's precision crack with a mirrored ring), the charge-ready chime, the vent hiss with cooling ticks, the
    /// denied blip, the reward chord, six impacts and two seamless loops (the beam's sustain and the charge hum).
    /// </summary>
    public static partial class CrystalSounds
    {
        public enum Clip
        {
            ShardShot, ThreadIgnite, ThreadRelease, GyreThrow, SunderLaunch, ChoirVolley, MirrorPulse,
            ChargeReady, Vent, Denied, Reward,
            ImpactStone, ImpactArmor, ImpactWeakPoint, ImpactCrystal, ImpactFlesh, ImpactAir,
            BeamLoop, ChargeLoop,
            Count
        }

        /// <summary>Peak level of every one-shot clip (playback volumes stay at or below 0.8 on top of this).</summary>
        public const float OneShotPeak = 0.8f;
        /// <summary>Peak level of the loop clips (they sit under the transients).</summary>
        public const float LoopPeak = 0.5f;
        /// <summary>Length of a loop clip before its crossfade (seconds), and the crossfade.</summary>
        public const float LoopSeconds = 1f;
        public const float LoopFade = 0.1f;

        public static bool IsLoop(Clip c)
        {
            return c == Clip.BeamLoop || c == Clip.ChargeLoop;
        }

        /// <summary>The shot clip of a family (every family has its own).</summary>
        public static Clip ShotClip(CrystalBehavior b)
        {
            switch (b)
            {
                case CrystalBehavior.PrismThread: return Clip.ThreadIgnite;
                case CrystalBehavior.GyreCrescent: return Clip.GyreThrow;
                case CrystalBehavior.Sunderheart: return Clip.SunderLaunch;
                case CrystalBehavior.MirageChoir: return Clip.ChoirVolley;
                case CrystalBehavior.Mirrorwake: return Clip.MirrorPulse;
                default: return Clip.ShardShot;
            }
        }

        /// <summary>The impact clip for what a hit struck (every kind has its own).</summary>
        public static Clip ImpactClip(CrystalImpact k)
        {
            switch (k)
            {
                case CrystalImpact.Armor: return Clip.ImpactArmor;
                case CrystalImpact.WeakPoint: return Clip.ImpactWeakPoint;
                case CrystalImpact.Crystal: return Clip.ImpactCrystal;
                case CrystalImpact.Flesh: return Clip.ImpactFlesh;
                case CrystalImpact.Air: return Clip.ImpactAir;
                default: return Clip.ImpactStone;
            }
        }

        /// <summary>The loop a family's StartLoop plays: the beam's sustain for Prism Thread, the charge hum otherwise.</summary>
        public static Clip LoopClip(CrystalBehavior b)
        {
            return b == CrystalBehavior.PrismThread ? Clip.BeamLoop : Clip.ChargeLoop;
        }

        /// <summary>Synthesizes one clip (mono, AudioSynth.Rate).</summary>
        public static float[] Make(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.ShardShot:
                    b = AudioSynth.Buf(0.32f);
                    AudioSynth.Noise(b, 0f, 0.03f, 0.4f, 0.0005f, 0.012f, 11000f, 3000f, 4101);
                    AudioSynth.Tone(b, 0f, 0.26f, 2900f, 1700f, 0.45f, 0.001f, 0.06f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.18f, 4350f, 2600f, 0.18f, 0.001f, 0.04f, AudioSynth.Wave.Triangle);
                    AudioSynth.Tone(b, 0f, 0.08f, 320f, 120f, 0.25f, 0.001f, 0.03f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.045f, 0.18f, 1);
                    break;
                case Clip.ThreadIgnite:
                    b = AudioSynth.Buf(0.36f);
                    AudioSynth.Tone(b, 0f, 0.32f, 600f, 2400f, 0.25f, 0.02f, 0.2f, AudioSynth.Wave.Saw);
                    AudioSynth.LowPass(b, 6000f);
                    AudioSynth.Tone(b, 0f, 0.3f, 1200f, 4800f, 0.2f, 0.02f, 0.18f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.08f, 0.3f, 0.001f, 0.04f, 12000f, 2000f, 4111);
                    break;
                case Clip.ThreadRelease:
                    b = AudioSynth.Buf(0.55f);
                    AudioSynth.Tone(b, 0f, 0.4f, 2200f, 500f, 0.35f, 0.002f, 0.2f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.5f, 0.3f, 0.01f, 0.25f, 7000f, 1500f, 4121, 0.2f);
                    AudioSynth.Tone(b, 0.08f, 0.05f, 3500f, 3500f, 0.12f, 0.001f, 0.02f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.17f, 0.05f, 3100f, 3100f, 0.11f, 0.001f, 0.02f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.27f, 0.05f, 2700f, 2700f, 0.1f, 0.001f, 0.02f, AudioSynth.Wave.Sine);
                    break;
                case Clip.GyreThrow:
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Tone(b, 0f, 0.5f, 220f, 520f, 0.35f, 0.01f, 0.3f, AudioSynth.Wave.Triangle, 28f, 0.25f);
                    AudioSynth.Tone(b, 0.01f, 0.5f, 1650f, 1650f, 0.18f, 0.002f, 0.25f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.01f, 0.4f, 2475f, 2475f, 0.1f, 0.002f, 0.2f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.4f, 0.35f, 0.03f, 0.15f, 3000f, 400f, 4131, 0.3f);
                    break;
                case Clip.SunderLaunch:
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Tone(b, 0f, 0.6f, 95f, 42f, 0.7f, 0.002f, 0.18f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.35f, 190f, 85f, 0.25f, 0.002f, 0.09f, AudioSynth.Wave.Triangle);
                    AudioSynth.Noise(b, 0f, 0.4f, 0.5f, 0.002f, 0.12f, 900f, 60f, 4141, 0.6f);
                    AudioSynth.Tone(b, 0.01f, 0.8f, 620f, 610f, 0.12f, 0.003f, 0.35f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.01f, 0.7f, 931f, 925f, 0.08f, 0.003f, 0.3f, AudioSynth.Wave.Sine);
                    AudioSynth.Drive(b, 1.4f);
                    break;
                case Clip.ChoirVolley:
                    {
                        b = AudioSynth.Buf(0.6f);
                        float[] f = { 1760f, 2093f, 2349f, 2637f, 3136f };
                        for (int i = 0; i < f.Length; i++) AudioSynth.Tone(b, i * 0.035f, 0.2f, f[i], f[i] * 1.25f, 0.22f, 0.002f, 0.08f, AudioSynth.Wave.Sine);
                        AudioSynth.Noise(b, 0f, 0.4f, 0.15f, 0.05f, 0.2f, 4000f, 1500f, 4151, 0.2f);
                        AudioSynth.Echo(b, 0.07f, 0.22f, 2);
                    }
                    break;
                case Clip.MirrorPulse:
                    b = AudioSynth.Buf(0.7f);
                    AudioSynth.Noise(b, 0f, 0.03f, 0.5f, 0.0005f, 0.01f, 0f, 4000f, 4161);
                    AudioSynth.Tone(b, 0f, 0.2f, 5200f, 900f, 0.4f, 0.001f, 0.07f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.15f, 260f, 110f, 0.35f, 0.001f, 0.06f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.02f, 0.5f, 1320f, 1320f, 0.15f, 0.003f, 0.3f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.02f, 0.45f, 1980f, 1980f, 0.1f, 0.003f, 0.25f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.11f, 0.3f, 2);
                    break;
                case Clip.ChargeReady:
                    b = AudioSynth.Buf(0.5f);
                    AudioSynth.Tone(b, 0f, 0.3f, 1568f, 1568f, 0.3f, 0.002f, 0.2f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.07f, 0.35f, 2349f, 2349f, 0.28f, 0.002f, 0.25f, AudioSynth.Wave.Triangle);
                    AudioSynth.Echo(b, 0.09f, 0.2f, 1);
                    break;
                case Clip.Vent:
                    {
                        b = AudioSynth.Buf(1.0f);
                        AudioSynth.Noise(b, 0f, 0.95f, 0.55f, 0.02f, 0.5f, 8000f, 1800f, 4171, 0.2f);
                        AudioSynth.Tone(b, 0f, 0.8f, 1600f, 400f, 0.15f, 0.01f, 0.5f, AudioSynth.Wave.Sine);
                        float[] at = { 0.12f, 0.23f, 0.37f, 0.48f, 0.61f, 0.76f };
                        float[] f = { 3600f, 2900f, 4200f, 3100f, 3800f, 2800f };
                        for (int i = 0; i < at.Length; i++) AudioSynth.Tone(b, at[i], 0.04f, f[i], f[i], 0.1f, 0.001f, 0.015f, AudioSynth.Wave.Sine);
                    }
                    break;
                case Clip.Denied:
                    b = AudioSynth.Buf(0.22f);
                    AudioSynth.Tone(b, 0f, 0.07f, 330f, 330f, 0.25f, 0.002f, 0.04f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.09f, 0.08f, 260f, 260f, 0.25f, 0.002f, 0.04f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 2200f);
                    break;
                case Clip.Reward:
                    {
                        b = AudioSynth.Buf(1.6f);
                        float[] n = { 659f, 831f, 988f, 1319f, 1661f };
                        for (int i = 0; i < n.Length; i++) AudioSynth.Tone(b, i * 0.06f, 1.1f, n[i], n[i], 0.25f, 0.005f, 0.9f, AudioSynth.Wave.Triangle, 5f, 0.003f);
                        AudioSynth.Tone(b, 0.3f, 1.2f, 2637f, 2637f, 0.1f, 0.01f, 1.0f, AudioSynth.Wave.Sine, 6f, 0.004f);
                        AudioSynth.Echo(b, 0.16f, 0.3f, 3);
                    }
                    break;
                case Clip.ImpactStone:
                    b = AudioSynth.Buf(0.3f);
                    AudioSynth.Noise(b, 0f, 0.06f, 0.7f, 0.0005f, 0.02f, 5000f, 300f, 4181);
                    AudioSynth.Noise(b, 0f, 0.2f, 0.3f, 0.002f, 0.07f, 2500f, 200f, 4182, 0.5f);
                    AudioSynth.Tone(b, 0f, 0.1f, 2600f, 2500f, 0.15f, 0.001f, 0.03f, AudioSynth.Wave.Sine);
                    break;
                case Clip.ImpactArmor:
                    b = AudioSynth.Buf(0.35f);
                    AudioSynth.Noise(b, 0f, 0.03f, 0.4f, 0.0005f, 0.012f, 0f, 2500f, 4191);
                    AudioSynth.Tone(b, 0f, 0.3f, 1180f, 1170f, 0.4f, 0.001f, 0.09f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.2f, 2950f, 2940f, 0.2f, 0.001f, 0.05f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.15f, 4130f, 4120f, 0.1f, 0.001f, 0.04f, AudioSynth.Wave.Sine);
                    break;
                case Clip.ImpactWeakPoint:
                    b = AudioSynth.Buf(0.55f);
                    AudioSynth.Noise(b, 0f, 0.04f, 0.7f, 0.0005f, 0.015f, 0f, 3500f, 4201);
                    AudioSynth.Tone(b, 0f, 0.4f, 2093f, 2000f, 0.35f, 0.001f, 0.15f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.01f, 0.35f, 3136f, 3100f, 0.2f, 0.001f, 0.12f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.12f, 180f, 90f, 0.3f, 0.001f, 0.04f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.06f, 0.2f, 1);
                    break;
                case Clip.ImpactCrystal:
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Noise(b, 0f, 0.3f, 0.6f, 0.0005f, 0.08f, 12000f, 4000f, 4211);
                    for (int i = 0; i < 5; i++) AudioSynth.Tone(b, i * 0.02f, 0.4f, 2400f + i * 530f, 2350f + i * 510f, 0.14f, 0.001f, 0.12f, AudioSynth.Wave.Sine);
                    break;
                case Clip.ImpactFlesh:
                    b = AudioSynth.Buf(0.25f);
                    AudioSynth.Tone(b, 0f, 0.15f, 140f, 70f, 0.5f, 0.001f, 0.04f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.12f, 0.4f, 0.001f, 0.03f, 1200f, 80f, 4221, 0.6f);
                    AudioSynth.Tone(b, 0.01f, 0.06f, 3000f, 3000f, 0.06f, 0.001f, 0.02f, AudioSynth.Wave.Sine);
                    break;
                case Clip.ImpactAir:
                    b = AudioSynth.Buf(0.4f);
                    AudioSynth.Noise(b, 0f, 0.35f, 0.35f, 0.005f, 0.15f, 9000f, 2500f, 4231);
                    AudioSynth.Tone(b, 0f, 0.25f, 1900f, 700f, 0.12f, 0.002f, 0.1f, AudioSynth.Wave.Sine);
                    break;
                case Clip.BeamLoop:
                    return BeamLoop();
                case Clip.ChargeLoop:
                    return ChargeLoop();
                default:
                    b = AudioSynth.Buf(0.1f);
                    break;
            }
            FadeTail(b, 0.015f);
            AudioSynth.Normalize(b, OneShotPeak);
            return b;
        }

        /// <summary>Fades the last few milliseconds to silence so no echo tail is cut off with a click.</summary>
        private static void FadeTail(float[] b, float seconds)
        {
            int n = Mathf.Min(b.Length, Mathf.Max(1, (int)(seconds * AudioSynth.Rate)));
            for (int i = 0; i < n; i++) b[b.Length - 1 - i] *= i / (float)n;
        }

        /// <summary>
        /// The beam's sustain: a hum with harmonics, a 10 Hz beating partial, a slow shimmer and a fine noise band. Every
        /// frequency is a multiple of 10 Hz and every tremolo a multiple of 10/9 Hz, so after the 0.1 s crossfade the head
        /// and tail line up in phase and the loop is seamless (no click, no dip).
        /// </summary>
        private static float[] BeamLoop()
        {
            float[] b = AudioSynth.Buf(LoopSeconds);
            AddPartial(b, 220f, 0.3f, 0f, 0f);
            AddPartial(b, 440f, 0.18f, 10f / 9f * 3f, 0.25f);
            AddPartial(b, 660f, 0.08f, 0f, 0f);
            AddPartial(b, 1320f, 0.06f, 10f / 9f * 6f, 0.5f);
            AddPartial(b, 1330f, 0.05f, 0f, 0f);
            AddPartial(b, 2640f, 0.025f, 10f / 9f * 9f, 0.6f);
            AudioSynth.Noise(b, 0f, LoopSeconds, 0.08f, 0f, 0f, 8000f, 3000f, 4301);
            float[] o = AudioSynth.Looped(b, LoopFade);
            AudioSynth.Normalize(o, LoopPeak);
            return o;
        }

        /// <summary>The charge hum (pitched up by the charge at runtime): a reedy chord with a slow tremolo and a whine.</summary>
        private static float[] ChargeLoop()
        {
            float[] b = AudioSynth.Buf(LoopSeconds);
            AddPartial(b, 330f, 0.25f, 10f / 9f * 3f, 0.3f);
            AddPartial(b, 660f, 0.15f, 0f, 0f);
            AddPartial(b, 990f, 0.08f, 10f / 9f * 3f, 0.4f);
            AddPartial(b, 1650f, 0.05f, 10f / 9f * 6f, 0.6f);
            AudioSynth.Noise(b, 0f, LoopSeconds, 0.05f, 0f, 0f, 6000f, 2000f, 4311);
            float[] o = AudioSynth.Looped(b, LoopFade);
            AudioSynth.Normalize(o, LoopPeak);
            return o;
        }

        /// <summary>Adds a steady sine partial across the whole buffer with an optional tremolo (no envelope, for loops).</summary>
        private static void AddPartial(float[] b, float hz, float amp, float tremHz, float tremDepth)
        {
            double w = 2.0 * System.Math.PI * hz / AudioSynth.Rate;
            double tw = 2.0 * System.Math.PI * tremHz / AudioSynth.Rate;
            for (int i = 0; i < b.Length; i++)
            {
                float trem = tremHz > 0f ? 1f - tremDepth * 0.5f * (1f - (float)System.Math.Cos(tw * i)) : 1f;
                b[i] += (float)System.Math.Sin(w * i) * amp * trem;
            }
        }
    }
}
