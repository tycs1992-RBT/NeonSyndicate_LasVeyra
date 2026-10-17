using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04E looping and ambience recipes (pure maths). Loops, beds and score layers are built only from the seamless
    /// loop helpers, so each wraps without a click or a level jump. The beds are steady textures (hum, air, steam, water,
    /// drone, wind, roar); the events that make a place feel alive (drips, valve clanks, chimes, hammers, gusts, booms)
    /// are separate detail clips scattered around the listener at random times, so the ambience never repeats on a loop.
    /// </summary>
    public static partial class Underheart04ESounds
    {
        /// <summary>Length of each zone bed (seconds).</summary>
        public const float BedSeconds = 6f;
        /// <summary>Tempo of the encounter score (beats per minute) and its length in beats.</summary>
        public const float MusicBpm = 100f;
        public const int MusicBeats = 8;

        /// <summary>Synthesizes one positional loop (seamless, normalized to LoopPeak).</summary>
        public static float[] MakeLoop(UhLoop id)
        {
            float[] b;
            switch (id)
            {
                case UhLoop.FormHum:
                    // The fused form's living hum: a resonant body, a slow breath and a faint crystal shimmer.
                    b = AudioSynth.Buf(2f);
                    LoopTone(b, 110f, 0.35f, 2f, 0.25f);
                    LoopTone(b, 220f, 0.2f, 2f, 0.3f, 0.2f, 0.5f);
                    LoopTone(b, 330f, 0.1f, 3f, 0.4f, 0.4f);
                    LoopTone(b, 1320f, 0.03f, 3f, 0.6f);
                    LoopTone(b, 1980f, 0.02f, 4f, 0.6f, 0.3f, 0.25f);
                    LoopNoise(b, 0.12f, 800f, 80f, 52001, 0.3f, 1f, 0.4f);
                    break;
                case UhLoop.Furnace:
                    // A furnace: deep rushing roar, a slow throb and scattered embers.
                    b = AudioSynth.Buf(3f);
                    LoopNoise(b, 0.8f, 400f, 30f, 52011, 0.9f, 1.333f, 0.35f);
                    LoopTone(b, 41f, 0.3f, 1.333f, 0.5f);
                    LoopTone(b, 82f, 0.12f, 1.333f, 0.5f, 0.1f);
                    LoopCrackle(b, 45, 0.18f, 3500f, 1200f, 52012);
                    break;
                case UhLoop.Steam:
                    // Steam escaping: a bright hiss with a flutter, a body of air and a faint whistle.
                    b = AudioSynth.Buf(2f);
                    LoopNoise(b, 0.6f, 8000f, 1800f, 52021, 0f, 11f, 0.2f);
                    LoopNoise(b, 0.3f, 1200f, 150f, 52022, 0.3f);
                    LoopTone(b, 2700f, 0.025f, 1f, 0.5f);
                    break;
                case UhLoop.Lava:
                    // Molten rock: a low rumble, slow gloops bubbling up and a thin sizzle.
                    {
                        b = AudioSynth.Buf(3f);
                        LoopNoise(b, 0.6f, 250f, 20f, 52031, 1f, 0.667f, 0.3f);
                        LoopNoise(b, 0.07f, 9000f, 3000f, 52032);
                        SynthRng r = new SynthRng(52033);
                        float[] ev = AudioSynth.Buf(0.18f);
                        for (int i = 0; i < 16; i++)
                        {
                            System.Array.Clear(ev, 0, ev.Length);
                            float dur = 0.08f + r.Unit() * 0.08f;
                            Glide(ev, 0f, dur, 200f + r.Unit() * 80f, 70f, 0.35f * (0.5f + r.Unit() * 0.5f), 0.004f, dur * 0.4f, AudioSynth.Wave.Sine);
                            LoopAdd(b, ev, 0.15f + r.Unit() * 2.45f);
                        }
                    }
                    break;
                case UhLoop.Receiver:
                    // The crystal receiver mast: a mains-like hum, a faint static band and soft data chirps.
                    {
                        b = AudioSynth.Buf(2f);
                        LoopBuzz(b, 120f, 0.3f, 5, 0.5f, 0.15f);
                        LoopNoise(b, 0.06f, 4000f, 1200f, 52041, 0f, 6f, 0.5f);
                        float[] ev = AudioSynth.Buf(0.15f);
                        float[] at = { 0.2f, 0.55f, 1.15f, 1.45f };
                        for (int i = 0; i < at.Length; i++)
                        {
                            System.Array.Clear(ev, 0, ev.Length);
                            Bell(ev, 0f, i % 2 == 0 ? 1760f : 2093f, 0.12f, 0.03f);
                            LoopAdd(b, ev, at[i]);
                        }
                    }
                    break;
                case UhLoop.BeamLane:
                    // A sustained beam: a harsh harmonic buzz with a fast flutter and a crackling edge.
                    b = AudioSynth.Buf(1.5f);
                    LoopBuzz(b, 180f, 0.4f, 7, 30f, 0.3f);
                    LoopTone(b, 900f, 0.08f, 0f, 0f);
                    LoopNoise(b, 0.1f, 9000f, 3000f, 52051, 0f, 15f, 0.5f);
                    LoopCrackle(b, 20, 0.15f, 9000f, 3000f, 52052);
                    break;
                case UhLoop.LiftMotor:
                    // A lift motor: a whine with a slow wobble over a geared rumble.
                    b = AudioSynth.Buf(2f);
                    LoopTone(b, 240f, 0.2f, 4f, 0.2f);
                    LoopTone(b, 480f, 0.12f, 4f, 0.2f, 0.3f);
                    LoopTone(b, 720f, 0.06f, 4f, 0.2f, 0.6f);
                    LoopTone(b, 1500f, 0.025f, 2f, 0.5f);
                    LoopNoise(b, 0.4f, 300f, 30f, 52061, 0.8f, 6f, 0.4f);
                    break;
                default:
                    b = AudioSynth.Buf(1f);
                    LoopNoise(b, 0.2f, 500f, 50f, 52099);
                    break;
            }
            return FinishLoop(b, LoopPeak);
        }

        /// <summary>Synthesizes the ambience bed of a zone (seamless, BedSeconds long, normalized to BedPeak). The surface has no bed.</summary>
        public static float[] MakeBed(UhZone zone)
        {
            float[] b = AudioSynth.Buf(BedSeconds);
            switch (zone)
            {
                case UhZone.ServiceInterior:
                    // A muffled service room: a mains hum with its buzzing harmonics, boxed-in air and a faint fan.
                    LoopTone(b, 60f, 0.16f, 0.333f, 0.15f);
                    LoopBuzz(b, 120f, 0.2f, 6, 0.333f, 0.2f);
                    LoopTone(b, 180f, 0.05f, 0.5f, 0.3f, 0.2f);
                    LoopNoise(b, 0.18f, 380f, 60f, 52101, 0.6f, 0.333f, 0.15f, 0.5f);
                    LoopNoise(b, 0.09f, 1500f, 400f, 52102, 0f, 2f, 0.2f);
                    break;
                case UhZone.Refuge:
                    // Lantern Refuge: cool moving air, a low cave depth and a soft pad that feels safe.
                    LoopNoise(b, 0.3f, 3500f, 400f, 52111, 0f, 0.1667f, 0.35f);
                    LoopNoise(b, 0.3f, 200f, 20f, 52112, 1f);
                    LoopTone(b, 220f, 0.05f, 0.333f, 0.5f);
                    LoopTone(b, 330f, 0.035f, 0.5f, 0.6f, 0.2f, 0.4f);
                    LoopTone(b, 440f, 0.02f, 0.1667f, 0.7f, 0.5f, 0.7f);
                    break;
                case UhZone.Canals:
                    // Ember Canals: steam hissing in slow surges, gurgling water and a low pipe drone.
                    LoopNoise(b, 0.25f, 8000f, 2000f, 52121, 0f, 0.5f, 0.45f);
                    LoopNoise(b, 0.45f, 900f, 120f, 52122, 0.5f, 3.333f, 0.25f);
                    LoopNoise(b, 0.15f, 2400f, 600f, 52123, 0f, 1.833f, 0.5f, 0.3f);
                    LoopTone(b, 55f, 0.12f, 0.1667f, 0.3f);
                    LoopTone(b, 110f, 0.06f, 0.1667f, 0.3f, 0.3f);
                    break;
                case UhZone.Gardens:
                    // Pyre Gardens: crystal crackling all around, a living drone and a high shimmer that swells and fades.
                    LoopTone(b, 55f, 0.1f, 0.1667f, 0.3f);
                    LoopTone(b, 110f, 0.08f, 0.333f, 0.4f, 0.3f);
                    LoopTone(b, 165f, 0.06f, 0.1667f, 0.4f, 0.6f, 0.5f);
                    LoopTone(b, 220f, 0.04f, 0.5f, 0.6f, 0.1f, 0.3f);
                    LoopCrackle(b, 260, 0.6f, 9000f, 2500f, 52131);
                    LoopCrackle(b, 90, 0.35f, 4000f, 1200f, 52133);
                    LoopTone(b, 1760f, 0.045f, 0.5f, 0.8f);
                    LoopTone(b, 2637f, 0.035f, 0.333f, 0.8f, 0f, 0.5f);
                    LoopTone(b, 3520f, 0.02f, 0.1667f, 0.9f, 0.3f, 0.25f);
                    LoopNoise(b, 0.05f, 600f, 40f, 52132, 0.6f);
                    break;
                case UhZone.Workshops:
                    // Crucible Workshops: a machine hum with a thin whine, a heavy room tone and metal ticking as iron cools.
                    {
                        LoopNoise(b, 0.16f, 320f, 40f, 52141, 0.7f);
                        LoopBuzz(b, 90f, 0.18f, 8, 1f, 0.3f);
                        LoopTone(b, 360f, 0.05f, 0.5f, 0.4f);
                        LoopTone(b, 1080f, 0.015f, 0.333f, 0.6f, 0.2f);
                        SynthRng r = new SynthRng(52142);
                        float[] ev = AudioSynth.Buf(0.2f);
                        for (int i = 0; i < 22; i++)
                        {
                            System.Array.Clear(ev, 0, ev.Length);
                            Ring(ev, 0f, 1700f + r.Unit() * 1300f, 0.25f + r.Unit() * 0.2f, 0.022f);
                            LoopAdd(b, ev, 0.15f + r.Unit() * 5.6f);
                        }
                    }
                    break;
                case UhZone.Reliquary:
                    // Ash Reliquary: wind through the columns in long swells and resonant column tones that come and go.
                    LoopNoise(b, 0.5f, 900f, 150f, 52151, 0.4f, 0.1667f, 0.5f);
                    LoopNoise(b, 0.12f, 2600f, 900f, 52152, 0f, 0.333f, 0.7f, 0.5f);
                    LoopTone(b, 196f, 0.06f, 0.1667f, 0.9f);
                    LoopTone(b, 293.7f, 0.05f, 0.333f, 0.9f, 0.2f, 0.33f);
                    LoopTone(b, 392f, 0.04f, 0.5f, 0.9f, 0.4f, 0.66f);
                    LoopTone(b, 1568f, 0.012f, 0.1667f, 0.9f, 0f, 0.5f);
                    break;
                case UhZone.Crown:
                    // Foundry Crown: a deep furnace roar you hear as rushing fire, a body-felt throb once a second and embers.
                    LoopNoise(b, 0.45f, 300f, 20f, 52161, 1f, 1f, 0.4f);
                    LoopTone(b, 36f, 0.3f, 1f, 0.55f);
                    LoopTone(b, 72f, 0.15f, 1f, 0.55f, 0.1f);
                    LoopNoise(b, 0.4f, 1100f, 220f, 52162, 0.3f, 0.333f, 0.45f);
                    LoopNoise(b, 0.08f, 3500f, 1200f, 52164, 0f, 1f, 0.5f, 0.25f);
                    LoopCrackle(b, 90, 0.25f, 6000f, 2000f, 52163);
                    break;
                default:
                    // The surface keeps the existing city ambience: no bed.
                    LoopNoise(b, 0.01f, 200f, 20f, 52199);
                    break;
            }
            return FinishLoop(b, BedPeak);
        }

        /// <summary>Synthesizes one ambience detail (a one-shot, normalized to DetailPeak).</summary>
        public static float[] MakeDetail(UhAudioDetail d)
        {
            float[] b;
            switch (d)
            {
                case UhAudioDetail.PipeTick:
                    b = AudioSynth.Buf(0.3f);
                    Ring(b, 0f, 1800f, 0.4f, 0.05f);
                    Click(b, 0f, 0.3f, 52201);
                    break;
                case UhAudioDetail.RelayClick:
                    b = AudioSynth.Buf(0.35f);
                    Click(b, 0f, 0.6f, 52211);
                    AudioSynth.Tone(b, 0.01f, 0.2f, 100f, 100f, 0.2f, 0.005f, 0.08f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 1500f);
                    Click(b, 0.22f, 0.4f, 52212);
                    break;
                case UhAudioDetail.Drip:
                    b = AudioSynth.Buf(0.6f);
                    Glide(b, 0f, 0.035f, 1200f, 2400f, 0.5f, 0.001f, 0.012f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.01f, 0.2f, 1800f, 1750f, 0.15f, 0.002f, 0.05f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.11f, 0.3f, 2);
                    break;
                case UhAudioDetail.Murmur:
                    {
                        // Distant voices: muffled syllables, never words.
                        b = AudioSynth.Buf(1.7f);
                        SynthRng r = new SynthRng(52221);
                        float t = 0.05f;
                        for (int i = 0; i < 9 && t < 1.4f; i++)
                        {
                            float len = 0.08f + r.Unit() * 0.12f;
                            float f = 180f + r.Unit() * 90f;
                            Glide(b, t, len, f, f * (0.85f + r.Unit() * 0.3f), 0.25f, 0.02f, len * 0.6f, AudioSynth.Wave.Saw, 6f, 0.02f);
                            AudioSynth.Noise(b, t, len, 0.08f, 0.02f, len * 0.5f, 1200f, 300f, 52222 + i);
                            t += len + 0.03f + r.Unit() * 0.12f;
                        }
                        AudioSynth.LowPass(b, 700f);
                        AudioSynth.LowPass(b, 900f);
                    }
                    break;
                case UhAudioDetail.ValveClank:
                    b = AudioSynth.Buf(0.85f);
                    Click(b, 0f, 0.5f, 52231);
                    Ring(b, 0f, 520f, 0.4f, 0.14f);
                    Ring(b, 0.01f, 780f, 0.25f, 0.1f);
                    AudioSynth.Echo(b, 0.14f, 0.25f, 2);
                    break;
                case UhAudioDetail.SteamPuff:
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Noise(b, 0f, 0.55f, 0.6f, 0.02f, 0.2f, 8000f, 1500f, 52241);
                    AudioSynth.Noise(b, 0f, 0.4f, 0.2f, 0.02f, 0.15f, 1000f, 150f, 52242);
                    break;
                case UhAudioDetail.WaterGurgle:
                    {
                        b = AudioSynth.Buf(0.75f);
                        SynthRng r = new SynthRng(52251);
                        for (int i = 0; i < 6; i++)
                        {
                            float t = i * 0.09f + r.Unit() * 0.05f;
                            float f = 280f + r.Unit() * 200f;
                            Glide(b, t, 0.06f, f, f * 2.2f, 0.3f, 0.004f, 0.025f, AudioSynth.Wave.Sine);
                        }
                        AudioSynth.Noise(b, 0f, 0.6f, 0.15f, 0.05f, 0.25f, 900f, 100f, 52252, 0.5f);
                    }
                    break;
                case UhAudioDetail.CrystalCrackle:
                    b = AudioSynth.Buf(0.55f);
                    Crackle(b, 0f, 0.3f, 16, 0.5f, 10000f, 3000f, 52261, 0.15f);
                    Bell(b, 0.05f, 3136f, 0.18f, 0.08f);
                    break;
                case UhAudioDetail.Chime:
                    {
                        // Three crystal bells drawn from a pentatonic set, each detail a little different.
                        b = AudioSynth.Buf(1.6f);
                        float[] set = { 1760f, 2217f, 2637f, 2960f, 3520f };
                        Bell(b, 0f, set[0], 0.3f, 0.35f);
                        Bell(b, 0.11f, set[2], 0.26f, 0.35f);
                        Bell(b, 0.27f, set[3], 0.2f, 0.4f);
                        AudioSynth.Echo(b, 0.17f, 0.3f, 2);
                    }
                    break;
                case UhAudioDetail.MetalTick:
                    b = AudioSynth.Buf(0.4f);
                    Ring(b, 0f, 2300f, 0.3f, 0.03f);
                    Ring(b, 0.09f, 2650f, 0.22f, 0.03f);
                    break;
                case UhAudioDetail.DistantHammer:
                    b = AudioSynth.Buf(1.7f);
                    for (int i = 0; i < 3; i++)
                    {
                        Ring(b, i * 0.42f, 900f, 0.35f, 0.08f);
                        Thump(b, i * 0.42f, 160f, 90f, 0.35f, 0.03f);
                    }
                    AudioSynth.LowPass(b, 2500f);
                    AudioSynth.Echo(b, 0.25f, 0.3f, 2);
                    break;
                case UhAudioDetail.ChainRattle:
                    b = AudioSynth.Buf(0.85f);
                    Crackle(b, 0f, 0.55f, 22, 0.45f, 6000f, 1500f, 52271, 0.4f);
                    Ring(b, 0.02f, 1300f, 0.15f, 0.06f);
                    Ring(b, 0.2f, 1150f, 0.12f, 0.06f);
                    break;
                case UhAudioDetail.WindGust:
                    b = AudioSynth.Buf(2.1f);
                    SweepNoise(b, 0f, 2f, 0.6f, 600f, 1500f, 120f, 0.7f, 0.7f, 52281, 0.4f);
                    AudioSynth.Tone(b, 0.3f, 1.4f, 880f, 930f, 0.03f, 0.5f, 0.5f, AudioSynth.Wave.Sine, 3f, 0.01f);
                    break;
                case UhAudioDetail.ResonantTone:
                    b = AudioSynth.Buf(2.3f);
                    AudioSynth.Tone(b, 0f, 2.2f, 196f, 196f, 0.3f, 0.45f, 1.1f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 2.2f, 197.5f, 197.5f, 0.2f, 0.45f, 1.1f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.05f, 2.1f, 392f, 392f, 0.12f, 0.5f, 0.9f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 1.5f, 0.05f, 0.4f, 0.5f, 1500f, 300f, 52291);
                    break;
                case UhAudioDetail.FurnaceThrob:
                    b = AudioSynth.Buf(1.9f);
                    AudioSynth.Tone(b, 0f, 1.8f, 40f, 38f, 0.6f, 0.2f, 0.8f, AudioSynth.Wave.Sine);
                    Tremolo(b, 0f, 1.8f, 1.6f, 0.7f);
                    AudioSynth.Noise(b, 0f, 1.88f, 0.4f, 0.3f, 0.5f, 350f, 25f, 52301, 0.9f);
                    break;
                case UhAudioDetail.DeepBoom:
                    b = AudioSynth.Buf(2.4f);
                    Thump(b, 0f, 46f, 24f, 0.8f, 0.4f);
                    AudioSynth.Noise(b, 0f, 1.75f, 0.5f, 0.01f, 0.5f, 400f, 20f, 52311, 0.9f);
                    AudioSynth.Echo(b, 0.3f, 0.3f, 2);
                    AudioSynth.LowPass(b, 1200f);
                    break;
                default:
                    b = AudioSynth.Buf(0.1f);
                    Click(b, 0f, 0.3f, 52399);
                    break;
            }
            return Finish(b, DetailPeak);
        }

        /// <summary>
        /// Synthesizes an encounter score layer: MusicBeats beats at MusicBpm, every event on the beat grid and written
        /// circularly, so it loops seamlessly and the two layers stay in step when started together. The pulse is a low D
        /// drone, a soft kick on each beat, offbeat ticks and a minor pad; the drive adds a sixteenth-note bass ostinato and
        /// low toms for boss fights.
        /// </summary>
        public static float[] MakeMusic(UhAudioMusic layer)
        {
            float beat = 60f / MusicBpm;
            float[] b = AudioSynth.Buf(beat * MusicBeats);
            float[] ev = AudioSynth.Buf(0.6f);
            if (layer == UhAudioMusic.Drive)
            {
                // D minor ostinato: D2 D3 F2 D3 A2 D3 C3 D3, two beats per bar of eight sixteenths.
                float[] notes = { 73.4f, 146.8f, 87.3f, 146.8f, 110f, 146.8f, 130.8f, 146.8f };
                float step = beat / 4f;
                for (int i = 0; i < MusicBeats * 4; i++)
                {
                    System.Array.Clear(ev, 0, ev.Length);
                    float f = notes[i % notes.Length];
                    AudioSynth.Tone(ev, 0f, step * 0.9f, f, f, 0.22f, 0.004f, step * 0.5f, AudioSynth.Wave.Saw);
                    AudioSynth.Tone(ev, 0f, step * 0.9f, f * 0.5f, f * 0.5f, 0.15f, 0.004f, step * 0.6f, AudioSynth.Wave.Sine);
                    AudioSynth.LowPass(ev, 900f);
                    LoopAdd(b, ev, i * step);
                }
                float[] toms = { 1.5f, 3f, 5.5f, 6.5f, 7f, 7.5f };
                for (int i = 0; i < toms.Length; i++)
                {
                    System.Array.Clear(ev, 0, ev.Length);
                    Thump(ev, 0f, 130f - i * 6f, 70f, 0.4f, 0.09f);
                    AudioSynth.Noise(ev, 0f, 0.1f, 0.15f, 0.002f, 0.03f, 2500f, 200f, 52411 + i);
                    LoopAdd(b, ev, toms[i] * beat);
                }
            }
            else
            {
                LoopTone(b, 73.4f, 0.16f, 0.25f, 0.4f);
                LoopTone(b, 110f, 0.08f, 0.125f, 0.5f, 0.3f);
                LoopTone(b, 293.7f, 0.035f, 0.125f, 0.7f);
                LoopTone(b, 349.2f, 0.03f, 0.125f, 0.7f, 0.2f, 0.33f);
                LoopTone(b, 440f, 0.025f, 0.125f, 0.7f, 0.4f, 0.66f);
                for (int i = 0; i < MusicBeats; i++)
                {
                    System.Array.Clear(ev, 0, ev.Length);
                    Thump(ev, 0f, 62f, 40f, i % 4 == 0 ? 0.5f : 0.38f, 0.1f);
                    LoopAdd(b, ev, i * beat);
                    System.Array.Clear(ev, 0, ev.Length);
                    AudioSynth.Noise(ev, 0f, 0.05f, 0.08f, 0.001f, 0.015f, 0f, 6000f, 52401 + i);
                    LoopAdd(b, ev, (i + 0.5f) * beat);
                }
            }
            return FinishLoop(b, MusicPeak);
        }
    }
}
