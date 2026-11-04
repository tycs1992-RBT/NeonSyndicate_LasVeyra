namespace NeonSyndicate
{
    /// <summary>
    /// H8S combat readability cues, synthesized with AudioSynth like the rest of CombatSounds (pure float maths, no
    /// audio files): the boss phase bell (a low inharmonic bell under a rising crystal shimmer), the kill confirmation
    /// (a soft thump, a short glass crack and a bright two-note crystal chord; the heavy version for elites and great
    /// beasts is deeper with a third note and falling shards), the heat warning (two soft rising ticks), the critical heat
    /// warning (three sharper ticks) and the vent release (an airy hiss that resolves into a clear two-note chime).
    /// </summary>
    public static partial class CombatSounds
    {
        /// <summary>Builds one H8S clip's samples (Make normalizes them).</summary>
        private static float[] MakeH8S(Clip c)
        {
            float[] b;
            switch (c)
            {
                case Clip.PhaseShift:
                    b = AudioSynth.Buf(1.7f);
                    AudioSynth.Tone(b, 0f, 1.6f, 110f, 104f, 0.8f, 0.004f, 0.75f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 1.2f, 303.6f, 300f, 0.35f, 0.003f, 0.45f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 0.8f, 594f, 590f, 0.18f, 0.002f, 0.25f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.9f, 0.35f, 0.02f, 0.35f, 400f, 40f, 7401, 0.6f);
                    AudioSynth.Tone(b, 0.15f, 1.2f, 1320f, 2640f, 0.12f, 0.25f, 0.5f, AudioSynth.Wave.Triangle, 7f, 0.01f);
                    AudioSynth.Echo(b, 0.18f, 0.35f, 3);
                    AudioSynth.LowPass(b, 9000f);
                    break;
                case Clip.Kill:
                    b = AudioSynth.Buf(0.5f);
                    AudioSynth.Tone(b, 0f, 0.22f, 150f, 55f, 0.55f, 0.002f, 0.07f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.08f, 0.45f, 0.0005f, 0.02f, 12000f, 3500f, 7411);
                    AudioSynth.Tone(b, 0.005f, 0.4f, 1568f, 1568f, 0.3f, 0.002f, 0.14f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.012f, 0.38f, 2349f, 2349f, 0.2f, 0.002f, 0.12f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.06f, 0.2f, 2);
                    break;
                case Clip.KillHeavy:
                    b = AudioSynth.Buf(1f);
                    AudioSynth.Tone(b, 0f, 0.45f, 110f, 38f, 0.8f, 0.002f, 0.16f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.18f, 0.6f, 0.0005f, 0.05f, 11000f, 2500f, 7421);
                    AudioSynth.Tone(b, 0.01f, 0.8f, 1046.5f, 1046.5f, 0.3f, 0.003f, 0.3f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.02f, 0.8f, 1568f, 1568f, 0.25f, 0.003f, 0.28f, AudioSynth.Wave.Triangle);
                    AudioSynth.Tone(b, 0.03f, 0.75f, 2093f, 2093f, 0.16f, 0.003f, 0.25f, AudioSynth.Wave.Sine);
                    for (int i = 0; i < 5; i++)
                    {
                        float f = 2600f + i * 350f;
                        AudioSynth.Tone(b, 0.06f + i * 0.05f, 0.2f, f, f * 0.97f, 0.08f, 0.001f, 0.08f, AudioSynth.Wave.Sine);
                    }
                    AudioSynth.Echo(b, 0.1f, 0.28f, 3);
                    AudioSynth.Drive(b, 1.2f);
                    break;
                case Clip.HeatWarn:
                    b = AudioSynth.Buf(0.3f);
                    AudioSynth.Tone(b, 0f, 0.07f, 880f, 900f, 0.5f, 0.002f, 0.03f, AudioSynth.Wave.Triangle);
                    AudioSynth.Tone(b, 0.11f, 0.07f, 1175f, 1200f, 0.5f, 0.002f, 0.03f, AudioSynth.Wave.Triangle);
                    AudioSynth.LowPass(b, 6000f);
                    break;
                case Clip.HeatCritical:
                    b = AudioSynth.Buf(0.36f);
                    for (int i = 0; i < 3; i++)
                    {
                        float f = 1400f + i * 120f;
                        AudioSynth.Tone(b, i * 0.1f, 0.06f, f, f * 1.02f, 0.55f, 0.001f, 0.025f, AudioSynth.Wave.Square);
                    }
                    AudioSynth.LowPass(b, 5000f);
                    break;
                default:
                    // VentReady.
                    b = AudioSynth.Buf(0.7f);
                    AudioSynth.Noise(b, 0f, 0.4f, 0.35f, 0.02f, 0.15f, 7000f, 1500f, 7431);
                    AudioSynth.Tone(b, 0.18f, 0.45f, 1318.5f, 1318.5f, 0.35f, 0.003f, 0.18f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.24f, 0.4f, 1975.5f, 1975.5f, 0.22f, 0.003f, 0.16f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.08f, 0.2f, 2);
                    break;
            }
            return b;
        }
    }
}
