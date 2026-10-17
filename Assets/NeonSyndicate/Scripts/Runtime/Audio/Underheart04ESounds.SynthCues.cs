using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The 04E one-shot recipes (pure maths). Each family has its own voice so a cue is readable without looking: the
    /// interface is short bright tones; the companion speaks in small chirps with recall a rising whistle, refusals a
    /// low double knock and injury a short yelp; fusion is a rising charge that locks with a chord and a falling sweep on
    /// separation; mechanisms are struck metal, grinding slides and steam; heat is a low pulse and a sizzle; bosses warn
    /// with a low swell and a tritone sting; each underground creature has its own call.
    /// </summary>
    public static partial class Underheart04ESounds
    {
        /// <summary>Synthesizes one 04E one-shot (mono, AudioSynth.Rate, normalized to OneShotPeak).</summary>
        public static float[] Make(UhSfx id)
        {
            float[] b;
            switch (id)
            {
                // ---------------------------------------------------------- interface
                case UhSfx.UiAccept:
                    // A bright rising "da-ding": confirm.
                    b = AudioSynth.Buf(0.42f);
                    Click(b, 0f, 0.3f, 51001);
                    AudioSynth.Tone(b, 0f, 0.1f, 784f, 784f, 0.45f, 0.002f, 0.05f, AudioSynth.Wave.Triangle);
                    Bell(b, 0.075f, 1175f, 0.5f, 0.11f);
                    AudioSynth.Tone(b, 0.075f, 0.25f, 2350f, 2350f, 0.05f, 0.002f, 0.06f, AudioSynth.Wave.Sine);
                    break;
                case UhSfx.UiTrack:
                    // A locator blip-blip that climbs: a lead is now tracked.
                    b = AudioSynth.Buf(0.5f);
                    Glide(b, 0f, 0.12f, 1500f, 1320f, 0.5f, 0.002f, 0.05f, AudioSynth.Wave.Sine);
                    Glide(b, 0.11f, 0.2f, 1980f, 1760f, 0.45f, 0.002f, 0.07f, AudioSynth.Wave.Sine);
                    AudioSynth.Echo(b, 0.09f, 0.25f, 1);
                    break;
                case UhSfx.UiClaim:
                    // A fast crystal arpeggio over a warm root: a reward is yours.
                    {
                        b = AudioSynth.Buf(0.95f);
                        float[] n = { 1047f, 1319f, 1568f, 2093f };
                        for (int i = 0; i < n.Length; i++) Bell(b, i * 0.05f, n[i], 0.42f, 0.17f);
                        AudioSynth.Tone(b, 0f, 0.7f, 523f, 523f, 0.18f, 0.01f, 0.32f, AudioSynth.Wave.Triangle);
                        AudioSynth.Noise(b, 0.14f, 0.4f, 0.07f, 0.02f, 0.15f, 9500f, 5000f, 51011);
                        AudioSynth.Echo(b, 0.12f, 0.2f, 1);
                    }
                    break;
                case UhSfx.UiDenied:
                    // A low "bonk-bonk" with a soft thud: not now.
                    b = AudioSynth.Buf(0.32f);
                    AudioSynth.Tone(b, 0f, 0.09f, 196f, 196f, 0.4f, 0.002f, 0.05f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.12f, 0.12f, 165f, 160f, 0.4f, 0.002f, 0.06f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 1500f);
                    Thump(b, 0f, 110f, 70f, 0.3f, 0.03f);
                    Thump(b, 0.12f, 100f, 65f, 0.3f, 0.03f);
                    break;
                case UhSfx.UiCard:
                    // A card sliding into view with a soft tick and a glint.
                    b = AudioSynth.Buf(0.38f);
                    SweepNoise(b, 0f, 0.22f, 0.5f, 900f, 5000f, 400f, 0.06f, 0.08f, 51021);
                    Click(b, 0.17f, 0.25f, 51022);
                    AudioSynth.Tone(b, 0.18f, 0.18f, 1320f, 1320f, 0.08f, 0.002f, 0.06f, AudioSynth.Wave.Sine);
                    break;

                // ---------------------------------------------------------- rescue, bond, companion
                case UhSfx.RescueClamp:
                    // Working a clamp: a click, a ringing iron jaw and a short ratchet.
                    b = AudioSynth.Buf(0.55f);
                    Click(b, 0f, 0.6f, 51101);
                    Ring(b, 0.005f, 740f, 0.45f, 0.12f);
                    for (int i = 0; i < 3; i++) Click(b, 0.12f + i * 0.045f, 0.35f - i * 0.06f, 51102 + i);
                    Thump(b, 0f, 180f, 110f, 0.25f, 0.03f);
                    break;
                case UhSfx.ClampBreak:
                    // The clamp snaps: a bright crack, a ringing spring and falling tinkles.
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Noise(b, 0f, 0.05f, 0.8f, 0.0005f, 0.012f, 0f, 2500f, 51111);
                    Ring(b, 0f, 1240f, 0.4f, 0.22f);
                    Glide(b, 0.02f, 0.45f, 300f, 900f, 0.22f, 0.005f, 0.2f, AudioSynth.Wave.Triangle, 16f, 0.12f);
                    Bell(b, 0.2f, 3136f, 0.12f, 0.06f);
                    Bell(b, 0.29f, 2637f, 0.1f, 0.06f);
                    Bell(b, 0.36f, 3520f, 0.08f, 0.05f);
                    Thump(b, 0f, 160f, 80f, 0.3f, 0.04f);
                    break;
                case UhSfx.DrakeWarn:
                    // A small drake's warning: a fluttering hiss and a rising trill. Keep your distance.
                    b = AudioSynth.Buf(0.75f);
                    AudioSynth.Noise(b, 0f, 0.6f, 0.45f, 0.04f, 0.25f, 8000f, 2500f, 51121);
                    Tremolo(b, 0f, 0.6f, 18f, 0.5f);
                    Glide(b, 0.05f, 0.5f, 900f, 1650f, 0.35f, 0.03f, 0.3f, AudioSynth.Wave.Triangle, 22f, 0.05f);
                    Glide(b, 0.05f, 0.45f, 1800f, 3300f, 0.08f, 0.03f, 0.25f, AudioSynth.Wave.Sine, 22f, 0.05f);
                    break;
                case UhSfx.BondChime:
                    // The bond motif: a warm rising fifth that blooms into a shimmering chord.
                    b = AudioSynth.Buf(1.8f);
                    AudioSynth.Tone(b, 0f, 1.5f, 440f, 440f, 0.2f, 0.15f, 0.8f, AudioSynth.Wave.Triangle, 5f, 0.004f);
                    Bell(b, 0f, 880f, 0.4f, 0.45f);
                    Bell(b, 0.13f, 1319f, 0.36f, 0.45f);
                    Bell(b, 0.26f, 1760f, 0.3f, 0.5f);
                    Bell(b, 0.3f, 1976f, 0.14f, 0.45f);
                    AudioSynth.Tone(b, 0.3f, 1.2f, 2637f, 2637f, 0.05f, 0.05f, 0.6f, AudioSynth.Wave.Sine, 6f, 0.004f);
                    AudioSynth.Echo(b, 0.18f, 0.25f, 2);
                    break;
                case UhSfx.CompanionFollow:
                    // Two quick rising chirps: "coming along".
                    b = AudioSynth.Buf(0.32f);
                    Glide(b, 0f, 0.09f, 1100f, 1600f, 0.45f, 0.004f, 0.05f, AudioSynth.Wave.Sine);
                    Glide(b, 0.1f, 0.12f, 1300f, 2000f, 0.45f, 0.004f, 0.06f, AudioSynth.Wave.Sine);
                    Glide(b, 0.1f, 0.12f, 2600f, 4000f, 0.06f, 0.004f, 0.05f, AudioSynth.Wave.Sine);
                    break;
                case UhSfx.CompanionWait:
                    // One settling downward chirp and a soft plop: "staying here".
                    b = AudioSynth.Buf(0.38f);
                    Glide(b, 0f, 0.16f, 1500f, 880f, 0.45f, 0.004f, 0.07f, AudioSynth.Wave.Sine);
                    Thump(b, 0.12f, 150f, 90f, 0.35f, 0.04f);
                    AudioSynth.Noise(b, 0.12f, 0.06f, 0.08f, 0.002f, 0.02f, 1500f, 200f, 51131);
                    break;
                case UhSfx.CompanionRecall:
                    // A whistle that climbs and swirls into two crystal pings: "come back to me".
                    b = AudioSynth.Buf(0.8f);
                    Glide(b, 0f, 0.4f, 700f, 1900f, 0.42f, 0.02f, 0.3f, AudioSynth.Wave.Sine, 6f, 0.015f);
                    Glide(b, 0f, 0.4f, 1400f, 3800f, 0.08f, 0.02f, 0.25f, AudioSynth.Wave.Sine, 6f, 0.015f);
                    Fm(b, 0.25f, 0.4f, 1900f, 1.5f, 2.5f, 0.2f, 0.18f, 0.01f, 0.15f);
                    Bell(b, 0.38f, 2637f, 0.22f, 0.12f);
                    Bell(b, 0.46f, 3136f, 0.18f, 0.12f);
                    break;
                case UhSfx.AssistReveal:
                    // A sonar sweep of three rising pings with a shimmering wash: something is revealed.
                    b = AudioSynth.Buf(1.2f);
                    Bell(b, 0f, 880f, 0.35f, 0.3f);
                    Bell(b, 0.15f, 1320f, 0.3f, 0.3f);
                    Bell(b, 0.3f, 1760f, 0.28f, 0.35f);
                    SweepNoise(b, 0f, 0.9f, 0.12f, 3000f, 9000f, 2500f, 0.2f, 0.35f, 51141);
                    AudioSynth.Echo(b, 0.2f, 0.28f, 2);
                    break;
                case UhSfx.AssistBurst:
                    // The drakeling's crystal burst on a marked target: sharp, glassy, with a punch.
                    b = AudioSynth.Buf(0.6f);
                    AudioSynth.Noise(b, 0f, 0.04f, 0.7f, 0.0005f, 0.01f, 0f, 3000f, 51151);
                    Fm(b, 0f, 0.4f, 1800f, 1.41f, 4f, 0.4f, 0.4f, 0.001f, 0.12f);
                    Thump(b, 0f, 130f, 60f, 0.45f, 0.05f);
                    Crackle(b, 0.02f, 0.3f, 14, 0.18f, 9000f, 3500f, 51152, 0.12f);
                    break;
                case UhSfx.CompanionHurt:
                    // A short pained yelp, small and high: the companion was hit.
                    b = AudioSynth.Buf(0.34f);
                    Glide(b, 0f, 0.26f, 1450f, 800f, 0.5f, 0.006f, 0.1f, AudioSynth.Wave.Triangle, 30f, 0.04f);
                    AudioSynth.Noise(b, 0f, 0.12f, 0.22f, 0.002f, 0.04f, 6000f, 1800f, 51161);
                    Tremolo(b, 0f, 0.3f, 24f, 0.35f);
                    break;
                case UhSfx.CompanionDowned:
                    // A falling whimper over a dimming crystal tone: the companion is down and needs rest or care.
                    b = AudioSynth.Buf(1.2f);
                    Glide(b, 0f, 0.38f, 1100f, 620f, 0.4f, 0.01f, 0.2f, AudioSynth.Wave.Triangle, 6f, 0.03f);
                    Glide(b, 0.36f, 0.6f, 820f, 420f, 0.34f, 0.02f, 0.3f, AudioSynth.Wave.Triangle, 5f, 0.03f);
                    AudioSynth.Tone(b, 0.1f, 1f, 330f, 300f, 0.2f, 0.05f, 0.45f, AudioSynth.Wave.Sine);
                    Tremolo(b, 0.1f, 1f, 4f, 0.4f);
                    AudioSynth.LowPass(b, 4000f);
                    break;
                case UhSfx.CareChime:
                    // Two soothing low bells and a warm breath: cared for.
                    b = AudioSynth.Buf(1.1f);
                    Bell(b, 0f, 659f, 0.38f, 0.35f);
                    Bell(b, 0.18f, 988f, 0.32f, 0.4f);
                    AudioSynth.Tone(b, 0f, 0.9f, 330f, 330f, 0.14f, 0.12f, 0.4f, AudioSynth.Wave.Triangle);
                    AudioSynth.Noise(b, 0f, 0.8f, 0.07f, 0.25f, 0.3f, 1500f, 200f, 51171);
                    break;
                case UhSfx.GuardianBrace:
                    // The ash warden braces: a heavy shell clunk with ember crackle.
                    b = AudioSynth.Buf(0.7f);
                    Thump(b, 0f, 110f, 48f, 0.7f, 0.08f);
                    Ring(b, 0.005f, 380f, 0.28f, 0.1f);
                    AudioSynth.Noise(b, 0f, 0.2f, 0.45f, 0.001f, 0.05f, 2500f, 200f, 51181, 0.4f);
                    Crackle(b, 0.05f, 0.45f, 22, 0.16f, 9000f, 3000f, 51182, 0.25f);
                    break;
                case UhSfx.SkimmerArc:
                    // The stormskimmer's disruption: a zappy arc and its own quick chirp.
                    b = AudioSynth.Buf(0.5f);
                    Glide(b, 0f, 0.25f, 3000f, 400f, 0.35f, 0.002f, 0.1f, AudioSynth.Wave.Saw, 70f, 0.1f);
                    AudioSynth.LowPass(b, 8000f);
                    Crackle(b, 0f, 0.3f, 18, 0.25f, 10000f, 3000f, 51191, 0.15f);
                    Glide(b, 0.28f, 0.1f, 2400f, 3300f, 0.25f, 0.004f, 0.04f, AudioSynth.Wave.Sine);
                    break;

                // ---------------------------------------------------------- fusion
                case UhSfx.FuseStart:
                    // A rising charge that swells for a second: the fusion begins.
                    b = AudioSynth.Buf(1.15f);
                    Glide(b, 0f, 1.05f, 110f, 440f, 0.3f, 0.8f, 0f, AudioSynth.Wave.Saw);
                    AudioSynth.LowPass(b, 3000f);
                    Glide(b, 0f, 1.05f, 220f, 880f, 0.25f, 0.8f, 0f, AudioSynth.Wave.Sine);
                    SweepNoise(b, 0f, 1.05f, 0.25f, 600f, 6000f, 300f, 0.85f, 0f, 51201);
                    Fm(b, 0.7f, 0.4f, 1760f, 1.5f, 1f, 3f, 0.08f, 0.25f, 0f);
                    // Ease the last 90 ms so a refused fusion (no lock sound after it) does not stop dead.
                    FadeTail(b, 0.09f);
                    break;
                case UhSfx.FuseLock:
                    // A heavy lock-in thunk and a resonant major chord: fused.
                    b = AudioSynth.Buf(1f);
                    Thump(b, 0f, 90f, 42f, 0.8f, 0.1f);
                    Click(b, 0f, 0.6f, 51211);
                    Ring(b, 0.004f, 520f, 0.3f, 0.18f);
                    Bell(b, 0.05f, 587f, 0.22f, 0.4f);
                    Bell(b, 0.05f, 740f, 0.2f, 0.4f);
                    Bell(b, 0.05f, 880f, 0.2f, 0.4f);
                    AudioSynth.Echo(b, 0.13f, 0.22f, 1);
                    break;
                case UhSfx.SeparateStart:
                    // Unlatching clicks and a falling sweep with a hiss: separating.
                    b = AudioSynth.Buf(0.9f);
                    Click(b, 0.02f, 0.5f, 51221);
                    Click(b, 0.1f, 0.4f, 51222);
                    Glide(b, 0.05f, 0.75f, 600f, 150f, 0.3f, 0.02f, 0.35f, AudioSynth.Wave.Saw);
                    AudioSynth.LowPass(b, 3500f);
                    SweepNoise(b, 0.05f, 0.8f, 0.25f, 6000f, 700f, 200f, 0.03f, 0.3f, 51223);
                    break;
                case UhSfx.SeparateDone:
                    // A release puff, the companion's happy chirp pair and a soft landing.
                    b = AudioSynth.Buf(0.7f);
                    AudioSynth.Noise(b, 0f, 0.25f, 0.5f, 0.003f, 0.08f, 4000f, 300f, 51231);
                    Glide(b, 0.18f, 0.09f, 1200f, 1700f, 0.4f, 0.004f, 0.05f, AudioSynth.Wave.Sine);
                    Glide(b, 0.28f, 0.11f, 1500f, 2100f, 0.4f, 0.004f, 0.06f, AudioSynth.Wave.Sine);
                    Thump(b, 0.3f, 140f, 80f, 0.3f, 0.04f);
                    break;
                case UhSfx.FuseDenied:
                    // Two muted metal knocks over a low buzz: the fusion is refused.
                    b = AudioSynth.Buf(0.42f);
                    Ring(b, 0f, 300f, 0.35f, 0.04f);
                    Thump(b, 0f, 120f, 70f, 0.4f, 0.03f);
                    Ring(b, 0.13f, 270f, 0.35f, 0.04f);
                    Thump(b, 0.13f, 110f, 65f, 0.4f, 0.03f);
                    AudioSynth.Tone(b, 0f, 0.32f, 140f, 140f, 0.12f, 0.005f, 0.15f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 2500f);
                    break;
                case UhSfx.VentGlide:
                    // A vent puff, then a lifting whoosh carrying the vent's rising whistle: the vent glide.
                    b = AudioSynth.Buf(1.3f);
                    AudioSynth.Noise(b, 0f, 0.15f, 0.5f, 0.002f, 0.05f, 6000f, 700f, 51243);
                    SweepNoise(b, 0f, 1.25f, 0.5f, 900f, 3200f, 250f, 0.12f, 0.45f, 51241, 0.2f);
                    AudioSynth.Noise(b, 0.05f, 0.8f, 0.12f, 0.2f, 0.3f, 9000f, 4000f, 51242);
                    Glide(b, 0.04f, 0.85f, 620f, 940f, 0.16f, 0.1f, 0.4f, AudioSynth.Wave.Triangle, 7f, 0.01f);
                    Glide(b, 0.04f, 0.85f, 1240f, 1880f, 0.05f, 0.1f, 0.35f, AudioSynth.Wave.Sine, 7f, 0.01f);
                    break;
                case UhSfx.VentBurst:
                    // A pneumatic blast and a hissing tail: the vent burst.
                    b = AudioSynth.Buf(0.7f);
                    AudioSynth.Noise(b, 0f, 0.5f, 0.85f, 0.002f, 0.12f, 9000f, 800f, 51251);
                    Thump(b, 0f, 75f, 40f, 0.6f, 0.07f);
                    AudioSynth.Noise(b, 0.08f, 0.55f, 0.25f, 0.02f, 0.2f, 7000f, 2500f, 51252);
                    break;
                case UhSfx.BraceRam:
                    // A heavy armoured impact with stone debris: the brace ram.
                    b = AudioSynth.Buf(0.9f);
                    Thump(b, 0f, 72f, 30f, 0.9f, 0.14f);
                    AudioSynth.Noise(b, 0f, 0.4f, 0.65f, 0.001f, 0.12f, 2500f, 60f, 51261, 0.5f);
                    Crackle(b, 0.03f, 0.5f, 24, 0.25f, 5000f, 1200f, 51262, 0.2f);
                    Ring(b, 0.005f, 210f, 0.2f, 0.12f);
                    AudioSynth.Drive(b, 1.3f);
                    break;
                case UhSfx.ArcDash:
                    // An electric whip that snaps upward and crackles out: the arc dash.
                    b = AudioSynth.Buf(0.6f);
                    Glide(b, 0f, 0.14f, 300f, 2600f, 0.35f, 0.003f, 0.08f, AudioSynth.Wave.Saw, 60f, 0.08f);
                    Crackle(b, 0f, 0.45f, 26, 0.3f, 10000f, 2500f, 51271, 0.15f);
                    Glide(b, 0.12f, 0.35f, 2000f, 700f, 0.18f, 0.003f, 0.12f, AudioSynth.Wave.Saw, 45f, 0.06f);
                    AudioSynth.LowPass(b, 8500f);
                    break;

                // ---------------------------------------------------------- world
                case UhSfx.ShortcutOpen:
                    // A latch, a long grinding slide over a low rumble, the stop and a bright catch: a shortcut is open.
                    b = AudioSynth.Buf(1.9f);
                    Thump(b, 0f, 95f, 45f, 0.6f, 0.08f);
                    Ring(b, 0.003f, 260f, 0.3f, 0.12f);
                    AudioSynth.Noise(b, 0.15f, 1.15f, 0.45f, 0.1f, 0f, 600f, 40f, 51301, 0.7f);
                    Tremolo(b, 0.15f, 1.15f, 14f, 0.45f);
                    AudioSynth.Tone(b, 0.1f, 1.3f, 45f, 42f, 0.25f, 0.2f, 0f, AudioSynth.Wave.Sine);
                    Thump(b, 1.32f, 85f, 40f, 0.55f, 0.08f);
                    Ring(b, 1.33f, 330f, 0.35f, 0.1f);
                    Bell(b, 1.4f, 1400f, 0.35f, 0.15f);
                    Bell(b, 1.47f, 2100f, 0.2f, 0.12f);
                    break;
                case UhSfx.LiftClank:
                    // Chain clanks, a ratchet and a motor starting: the lift moves.
                    b = AudioSynth.Buf(0.95f);
                    Ring(b, 0f, 620f, 0.35f, 0.08f);
                    Ring(b, 0.11f, 830f, 0.3f, 0.07f);
                    Ring(b, 0.2f, 690f, 0.28f, 0.07f);
                    for (int i = 0; i < 5; i++) Click(b, 0.05f + i * 0.06f, 0.25f, 51311 + i);
                    Glide(b, 0.25f, 0.65f, 200f, 340f, 0.22f, 0.15f, 0f, AudioSynth.Wave.Saw);
                    AudioSynth.LowPass(b, 2500f);
                    break;
                case UhSfx.ValveTurn:
                    // A squeaking iron wheel, ratchet clicks and the first breath of steam: a valve turns.
                    b = AudioSynth.Buf(1.05f);
                    Glide(b, 0f, 0.8f, 420f, 520f, 0.3f, 0.05f, 0f, AudioSynth.Wave.Saw, 9f, 0.06f);
                    AudioSynth.LowPass(b, 2500f);
                    Tremolo(b, 0f, 0.8f, 7f, 0.5f);
                    for (int i = 0; i < 6; i++) Click(b, 0.03f + i * 0.12f, 0.3f, 51321 + i);
                    AudioSynth.Noise(b, 0.7f, 0.33f, 0.2f, 0.08f, 0.15f, 8000f, 2000f, 51328);
                    break;
                case UhSfx.SteamBurst:
                    // A loud steam release with a flutter and a faint whistle.
                    b = AudioSynth.Buf(1.3f);
                    AudioSynth.Noise(b, 0f, 1.25f, 0.8f, 0.01f, 0.5f, 9000f, 1500f, 51331);
                    Tremolo(b, 0f, 1.25f, 25f, 0.15f);
                    AudioSynth.Noise(b, 0f, 0.8f, 0.3f, 0.01f, 0.3f, 900f, 100f, 51332);
                    AudioSynth.Tone(b, 0.02f, 0.9f, 2600f, 2500f, 0.05f, 0.05f, 0.4f, AudioSynth.Wave.Sine);
                    break;
                case UhSfx.LavaWarn:
                    // Two low warning pulses over bubbling and sizzle: heat ahead.
                    b = AudioSynth.Buf(0.9f);
                    AudioSynth.Tone(b, 0f, 0.18f, 110f, 108f, 0.4f, 0.01f, 0.1f, AudioSynth.Wave.Square);
                    AudioSynth.Tone(b, 0.3f, 0.18f, 110f, 108f, 0.4f, 0.01f, 0.1f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 900f);
                    for (int i = 0; i < 3; i++) Glide(b, 0.08f + i * 0.21f, 0.1f, 180f, 80f, 0.25f, 0.005f, 0.04f, AudioSynth.Wave.Sine);
                    AudioSynth.Noise(b, 0f, 0.85f, 0.1f, 0.05f, 0.4f, 9000f, 3000f, 51341);
                    break;
                case UhSfx.HeatTick:
                    // A short sizzle tick: you are taking heat damage.
                    b = AudioSynth.Buf(0.18f);
                    AudioSynth.Noise(b, 0f, 0.15f, 0.6f, 0.002f, 0.04f, 9000f, 2500f, 51351);
                    AudioSynth.Tone(b, 0f, 0.05f, 900f, 700f, 0.25f, 0.001f, 0.02f, AudioSynth.Wave.Triangle);
                    break;
                case UhSfx.RestChime:
                    // A calm, low pad chord with a soft bell: rested and safe.
                    {
                        b = AudioSynth.Buf(1.9f);
                        float[] n = { 523f, 659f, 784f, 880f };
                        for (int i = 0; i < n.Length; i++) AudioSynth.Tone(b, i * 0.04f, 1.7f, n[i], n[i], 0.16f, 0.25f, 0.8f, AudioSynth.Wave.Triangle, 4.5f, 0.003f);
                        Bell(b, 0.25f, 1047f, 0.2f, 0.5f);
                        AudioSynth.Noise(b, 0f, 1.5f, 0.05f, 0.4f, 0.6f, 2000f, 300f, 51361);
                        AudioSynth.LowPass(b, 5000f);
                    }
                    break;
                case UhSfx.GateOpen:
                    // A grinding slide with a faint squeal and a stop: a gate opens.
                    b = AudioSynth.Buf(1.35f);
                    AudioSynth.Noise(b, 0f, 1.05f, 0.5f, 0.06f, 0f, 520f, 60f, 51371, 0.6f);
                    Tremolo(b, 0f, 1.05f, 9f, 0.4f);
                    Glide(b, 0.05f, 0.9f, 900f, 700f, 0.05f, 0.1f, 0f, AudioSynth.Wave.Sine, 5f, 0.01f);
                    Thump(b, 1.05f, 90f, 42f, 0.55f, 0.07f);
                    Ring(b, 1.06f, 300f, 0.2f, 0.08f);
                    break;
                case UhSfx.ReceiverPing:
                    // A receiver ping riding a crackle of static.
                    b = AudioSynth.Buf(0.7f);
                    Bell(b, 0f, 1760f, 0.4f, 0.12f);
                    Fm(b, 0.02f, 0.25f, 1320f, 2f, 3f, 0.3f, 0.15f, 0.002f, 0.08f);
                    AudioSynth.Noise(b, 0f, 0.6f, 0.25f, 0.01f, 0.2f, 4000f, 1000f, 51381);
                    Tremolo(b, 0.05f, 0.6f, 40f, 0.4f);
                    break;
                case UhSfx.ConduitPulse:
                    // A warm electric swell along the conduit with a few sparks.
                    b = AudioSynth.Buf(1f);
                    AudioSynth.Tone(b, 0f, 0.95f, 120f, 120f, 0.3f, 0.3f, 0.35f, AudioSynth.Wave.Saw);
                    AudioSynth.LowPass(b, 1400f);
                    AudioSynth.Tone(b, 0f, 0.95f, 60f, 60f, 0.25f, 0.3f, 0.35f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0.1f, 0.8f, 1200f, 1200f, 0.06f, 0.25f, 0.3f, AudioSynth.Wave.Sine, 8f, 0.005f);
                    Crackle(b, 0.15f, 0.6f, 8, 0.15f, 9000f, 3000f, 51391);
                    break;

                // ---------------------------------------------------------- bosses
                case UhSfx.BossTelegraph:
                    // An ominous low swell with a tritone underneath and a rising sting: an attack is coming.
                    b = AudioSynth.Buf(1.05f);
                    Glide(b, 0f, 0.95f, 55f, 73f, 0.35f, 0.55f, 0.3f, AudioSynth.Wave.Saw);
                    AudioSynth.LowPass(b, 1200f);
                    AudioSynth.Tone(b, 0f, 0.95f, 77.8f, 77.8f, 0.3f, 0.5f, 0.3f, AudioSynth.Wave.Sine);
                    SweepNoise(b, 0f, 0.9f, 0.25f, 300f, 3000f, 80f, 0.6f, 0.2f, 51401, 0.4f);
                    Glide(b, 0.5f, 0.45f, 1100f, 1650f, 0.45f, 0.02f, 0.15f, AudioSynth.Wave.Triangle);
                    Glide(b, 0.5f, 0.45f, 1556f, 2333f, 0.18f, 0.02f, 0.15f, AudioSynth.Wave.Sine);
                    break;
                case UhSfx.BossPhase:
                    // A huge impact, a roar and a rising sweep: the boss changes phase.
                    b = AudioSynth.Buf(2.2f);
                    Thump(b, 0f, 62f, 26f, 0.9f, 0.3f);
                    AudioSynth.Noise(b, 0f, 0.5f, 0.6f, 0.001f, 0.12f, 3000f, 60f, 51411);
                    AudioSynth.Noise(b, 0.05f, 1.6f, 0.55f, 0.1f, 0.7f, 700f, 50f, 51412, 0.8f);
                    AudioSynth.Tone(b, 0.05f, 1.5f, 70f, 55f, 0.3f, 0.1f, 0.7f, AudioSynth.Wave.Saw, 7f, 0.06f);
                    Glide(b, 0.3f, 1.2f, 110f, 330f, 0.18f, 0.6f, 0.4f, AudioSynth.Wave.Saw);
                    AudioSynth.Echo(b, 0.21f, 0.25f, 2);
                    AudioSynth.Drive(b, 1.2f);
                    break;
                case UhSfx.BossVulnerable:
                    // A glassy crack and a rising two-note ding: the weak point is open, hit it now.
                    b = AudioSynth.Buf(0.95f);
                    AudioSynth.Noise(b, 0f, 0.06f, 0.6f, 0.0005f, 0.015f, 0f, 3500f, 51421);
                    Fm(b, 0f, 0.5f, 1568f, 3.5f, 3f, 0.3f, 0.3f, 0.001f, 0.18f);
                    Bell(b, 0.12f, 1319f, 0.32f, 0.2f);
                    Bell(b, 0.24f, 1976f, 0.32f, 0.25f);
                    AudioSynth.Echo(b, 0.1f, 0.2f, 1);
                    break;
                case UhSfx.BossDefeat:
                    // A collapse, a long falling sweep and a resolving chord: victory.
                    {
                        b = AudioSynth.Buf(3.1f);
                        Thump(b, 0f, 56f, 24f, 0.9f, 0.4f);
                        AudioSynth.Noise(b, 0f, 2f, 0.6f, 0.002f, 0.6f, 900f, 40f, 51431, 0.7f);
                        Crackle(b, 0.1f, 1.2f, 40, 0.2f, 4000f, 800f, 51432, 0.5f);
                        Glide(b, 0.1f, 1.5f, 400f, 80f, 0.2f, 0.05f, 0.7f, AudioSynth.Wave.Saw);
                        AudioSynth.LowPass(b, 3000f);
                        float[] n = { 294f, 370f, 440f, 587f };
                        for (int i = 0; i < n.Length; i++) AudioSynth.Tone(b, 1.1f + i * 0.05f, 1.9f, n[i], n[i], 0.22f, 0.3f, 1f, AudioSynth.Wave.Triangle, 4f, 0.003f);
                        Bell(b, 1.3f, 1175f, 0.25f, 0.6f);
                        AudioSynth.Echo(b, 0.24f, 0.22f, 2);
                    }
                    break;
                case UhSfx.FurnaceRoar:
                    // A deep furnace roar with a heavy throb and embers.
                    b = AudioSynth.Buf(1.7f);
                    AudioSynth.Noise(b, 0f, 1.65f, 0.8f, 0.15f, 0.8f, 500f, 25f, 51441, 0.9f);
                    AudioSynth.Tone(b, 0f, 1.6f, 42f, 40f, 0.4f, 0.15f, 0.8f, AudioSynth.Wave.Sine);
                    AudioSynth.Tone(b, 0f, 1.6f, 84f, 80f, 0.2f, 0.15f, 0.7f, AudioSynth.Wave.Sine);
                    Tremolo(b, 0f, 1.6f, 7f, 0.35f);
                    Crackle(b, 0.1f, 1.3f, 30, 0.15f, 8000f, 2500f, 51442);
                    AudioSynth.Drive(b, 1.2f);
                    break;
                case UhSfx.BeamCharge:
                    // A rising whine that builds and cuts off: a beam is about to fire.
                    b = AudioSynth.Buf(1.3f);
                    Glide(b, 0f, 1.2f, 400f, 2400f, 0.35f, 1.05f, 0f, AudioSynth.Wave.Sine, 9f, 0.012f);
                    Glide(b, 0f, 1.2f, 800f, 4800f, 0.08f, 1.05f, 0f, AudioSynth.Wave.Triangle);
                    SweepNoise(b, 0f, 1.2f, 0.15f, 2000f, 9000f, 1500f, 1f, 0f, 51451);
                    Click(b, 1.18f, 0.3f, 51452);
                    break;
                case UhSfx.MachineStep:
                    // A heavy mechanical footfall: thud, clank and a hydraulic hiss.
                    b = AudioSynth.Buf(0.55f);
                    Thump(b, 0f, 78f, 38f, 0.8f, 0.07f);
                    AudioSynth.Noise(b, 0f, 0.15f, 0.4f, 0.001f, 0.04f, 1500f, 60f, 51461, 0.5f);
                    Ring(b, 0.02f, 410f, 0.25f, 0.08f);
                    AudioSynth.Noise(b, 0.08f, 0.3f, 0.2f, 0.02f, 0.1f, 8000f, 2000f, 51462);
                    break;
                case UhSfx.LureHum:
                    // A warbling hum that swells: the lure is active.
                    b = AudioSynth.Buf(1.3f);
                    AudioSynth.Tone(b, 0f, 1.25f, 330f, 330f, 0.3f, 0.2f, 0.6f, AudioSynth.Wave.Sine, 5.5f, 0.03f);
                    AudioSynth.Tone(b, 0f, 1.25f, 495f, 495f, 0.18f, 0.2f, 0.6f, AudioSynth.Wave.Triangle, 5.5f, 0.03f);
                    Tremolo(b, 0f, 1.25f, 8f, 0.4f);
                    break;
                case UhSfx.PlateCrack:
                    // An armour plate cracking: a crunch, a ring and splitting ticks.
                    b = AudioSynth.Buf(0.55f);
                    AudioSynth.Noise(b, 0f, 0.08f, 0.7f, 0.0005f, 0.02f, 7000f, 1500f, 51471);
                    Ring(b, 0f, 1700f, 0.3f, 0.08f);
                    Crackle(b, 0f, 0.1f, 10, 0.4f, 9000f, 2500f, 51472);
                    Thump(b, 0f, 140f, 70f, 0.4f, 0.04f);
                    break;

                // ---------------------------------------------------------- underground creatures
                case UhSfx.ScavengerChitter:
                    // Dry irregular clicks over a rasp: the ember scavenger pack.
                    b = AudioSynth.Buf(0.48f);
                    for (int i = 0; i < 10; i++)
                    {
                        float t = i * 0.036f + MathUtil.Hash01(i, 51501) * 0.02f;
                        AudioSynth.Noise(b, t, 0.01f, 0.6f, 0.0005f, 0.004f, 5000f, 1500f, 51502 + i);
                        AudioSynth.Tone(b, t, 0.014f, 1500f + MathUtil.Hash01(i, 51520) * 300f, 1250f, 0.25f, 0.001f, 0.006f, AudioSynth.Wave.Square);
                    }
                    AudioSynth.Noise(b, 0f, 0.42f, 0.25f, 0.02f, 0.2f, 3000f, 800f, 51530);
                    Tremolo(b, 0f, 0.42f, 40f, 0.5f);
                    break;
                case UhSfx.BurrowerEmerge:
                    // Earth bursting open with a rock spill and a short growl: an ash burrower surfaces.
                    b = AudioSynth.Buf(1.2f);
                    Thump(b, 0f, 52f, 28f, 0.8f, 0.2f);
                    AudioSynth.Noise(b, 0f, 1.15f, 0.6f, 0.02f, 0.35f, 600f, 30f, 51541, 0.8f);
                    Crackle(b, 0.05f, 0.85f, 45, 0.3f, 3000f, 500f, 51542, 0.35f);
                    AudioSynth.Tone(b, 0.1f, 0.5f, 92f, 75f, 0.2f, 0.05f, 0.25f, AudioSynth.Wave.Saw, 12f, 0.08f);
                    break;
                case UhSfx.GlasswingScreech:
                    // A glassy screech from above: a glasswing dives.
                    b = AudioSynth.Buf(0.85f);
                    Glide(b, 0f, 0.8f, 2300f, 1500f, 0.35f, 0.02f, 0.35f, AudioSynth.Wave.Saw, 35f, 0.04f);
                    Fm(b, 0f, 0.7f, 3000f, 1.5f, 2f, 0.5f, 0.15f, 0.02f, 0.3f);
                    AudioSynth.Noise(b, 0f, 0.75f, 0.15f, 0.02f, 0.3f, 9000f, 4000f, 51551);
                    AudioSynth.LowPass(b, 7500f);
                    break;
                case UhSfx.CasterCharge:
                    // A fiery whoosh that builds and thumps: a furnace caster readies a lob.
                    b = AudioSynth.Buf(0.95f);
                    SweepNoise(b, 0f, 0.85f, 0.8f, 400f, 3000f, 100f, 0.7f, 0.12f, 51561, 0.5f);
                    Crackle(b, 0.2f, 0.6f, 25, 0.35f, 8000f, 2500f, 51562);
                    Glide(b, 0f, 0.8f, 100f, 160f, 0.25f, 0.6f, 0f, AudioSynth.Wave.Saw);
                    Thump(b, 0.8f, 110f, 55f, 0.45f, 0.05f);
                    break;
                case UhSfx.SentinelScan:
                    // A robotic scanning sweep with pulse beeps: an extraction sentinel is scanning.
                    b = AudioSynth.Buf(1.1f);
                    Glide(b, 0f, 0.5f, 600f, 1800f, 0.3f, 0.02f, 0f, AudioSynth.Wave.Sine);
                    Glide(b, 0.5f, 0.5f, 1800f, 600f, 0.3f, 0f, 0.3f, AudioSynth.Wave.Sine);
                    for (int i = 0; i < 9; i++) AudioSynth.Tone(b, i * 0.11f, 0.03f, 2400f, 2400f, 0.15f, 0.002f, 0.01f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 6000f);
                    break;
                case UhSfx.RepairPulse:
                    // A warbling mend with sparks: a repair construct is fixing an ally.
                    b = AudioSynth.Buf(0.75f);
                    AudioSynth.Tone(b, 0f, 0.7f, 520f, 520f, 0.3f, 0.05f, 0.3f, AudioSynth.Wave.Sine, 12f, 0.08f);
                    AudioSynth.Tone(b, 0f, 0.7f, 1040f, 1040f, 0.12f, 0.05f, 0.3f, AudioSynth.Wave.Sine, 12f, 0.08f);
                    Glide(b, 0f, 0.5f, 300f, 700f, 0.15f, 0.05f, 0.2f, AudioSynth.Wave.Triangle);
                    Crackle(b, 0.05f, 0.6f, 16, 0.2f, 9000f, 3000f, 51571);
                    break;
                case UhSfx.CrabWarn:
                    // Sharp claw clacks and a bubbling hiss: a kiln crab warns you off.
                    b = AudioSynth.Buf(0.55f);
                    float[] clacks = { 0f, 0.08f, 0.16f, 0.3f };
                    for (int i = 0; i < clacks.Length; i++)
                    {
                        AudioSynth.Noise(b, clacks[i], 0.02f, 0.7f, 0.0003f, 0.005f, 3500f, 800f, 51581 + i);
                        AudioSynth.Tone(b, clacks[i], 0.04f, 1100f, 950f, 0.35f, 0.0005f, 0.012f, AudioSynth.Wave.Triangle);
                        Ring(b, clacks[i], 1600f + i * 70f, 0.25f, 0.02f);
                    }
                    AudioSynth.Noise(b, 0.05f, 0.45f, 0.12f, 0.05f, 0.2f, 5000f, 1500f, 51590);
                    Tremolo(b, 0.05f, 0.45f, 16f, 0.6f);
                    break;
                case UhSfx.MothFlutter:
                    // A soft flutter of wings and a faint shimmer: cinder moths drifting by.
                    b = AudioSynth.Buf(0.6f);
                    for (int i = 0; i < 8; i++) AudioSynth.Noise(b, i * 0.06f, 0.05f, 0.5f * (1f - i * 0.08f), 0.01f, 0.02f, 1200f, 150f, 51591 + i);
                    AudioSynth.Tone(b, 0.05f, 0.45f, 2400f, 2400f, 0.04f, 0.1f, 0.2f, AudioSynth.Wave.Sine, 7f, 0.01f);
                    break;
                case UhSfx.CompanionRefuse:
                    // A small nasal two-note "uh-uh" from the companion: it cannot do that (not a hurt yelp, not a stay chirp).
                    b = AudioSynth.Buf(0.42f);
                    Glide(b, 0f, 0.1f, 820f, 740f, 0.4f, 0.006f, 0.06f, AudioSynth.Wave.Triangle);
                    Glide(b, 0f, 0.1f, 1640f, 1480f, 0.1f, 0.006f, 0.05f, AudioSynth.Wave.Square);
                    Glide(b, 0.15f, 0.16f, 700f, 500f, 0.4f, 0.006f, 0.08f, AudioSynth.Wave.Triangle);
                    Glide(b, 0.15f, 0.16f, 1400f, 1000f, 0.1f, 0.006f, 0.07f, AudioSynth.Wave.Square);
                    AudioSynth.LowPass(b, 3200f);
                    AudioSynth.Noise(b, 0f, 0.3f, 0.05f, 0.01f, 0.1f, 2500f, 600f, 51601);
                    break;
                default:
                    b = AudioSynth.Buf(0.1f);
                    Click(b, 0f, 0.5f, 51999);
                    break;
            }
            return Finish(b, OneShotPeak);
        }
    }
}
