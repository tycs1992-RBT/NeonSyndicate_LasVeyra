using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Travel powers (TravelMath, DoubleTap, TravelSounds): everyday speeds, the Surge ramp and turning circle, leap
    /// heights from the charge, the glide's sink, flight speeds and the soft ceiling, double-tap timing, the unlock
    /// rules, landings, the save block defaults and the synthesized sounds. Pure maths, so they run offline.
    /// </summary>
    public sealed class TravelTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void EverydaySpeedsMatchTheTargets()
        {
            Assert.That(TravelMath.JogSpeed, Is.InRange(4.4f * 0.9f, 4.4f * 1.1f), "jog about 4.4 m/s");
            Assert.That(TravelMath.SprintSpeed, Is.InRange(8.2f * 0.9f, 8.2f * 1.1f), "sprint about 8.2 m/s");
            Assert.That(TravelMath.SprintSpeed, Is.GreaterThan(TravelMath.JogSpeed));
            Assert.That(TravelMath.JogSpeed, Is.GreaterThan(TravelMath.WalkSpeed));
            // Quicker than before (16 up, 22 down): a standing start reaches a full sprint in under 0.4 s.
            Assert.That(TravelMath.SprintSpeed / TravelMath.GroundAccel, Is.LessThan(0.4f));
            Assert.That(TravelMath.GroundAccel, Is.GreaterThan(16f));
            Assert.That(TravelMath.GroundDecel, Is.GreaterThan(22f));
        }

        [Test]
        public void SurgeRampsToTopSpeedInAboutOnePointTwoSeconds()
        {
            float v = TravelMath.SprintSpeed;
            float t = 0f;
            while (v < TravelMath.SurgeTopSpeed && t < 5f)
            {
                v = TravelMath.SurgeStep(v, TravelMath.SurgeTopSpeed, Dt);
                t += Dt;
            }
            Assert.That(t, Is.InRange(1.0f, 1.5f), "ramp time");
            Assert.That(v, Is.EqualTo(TravelMath.SurgeTopSpeed).Within(1e-4f), "reaches the top exactly, no overshoot");
            Assert.That(TravelMath.SurgeTopSpeed, Is.InRange(27f, 33f), "about 30 m/s");
            Assert.That(TravelMath.SurgeStep(TravelMath.SurgeTopSpeed, TravelMath.SurgeTopSpeed, Dt), Is.EqualTo(TravelMath.SurgeTopSpeed));
            // The ramp is fastest at the start (ease-out).
            Assert.That(TravelMath.SurgeAccel(TravelMath.SprintSpeed), Is.GreaterThan(TravelMath.SurgeAccel(25f)));
            Assert.That(TravelMath.SurgeAccel(TravelMath.SurgeTopSpeed), Is.EqualTo(TravelMath.SurgeMinAccel));
            // A light push surges at a sprint, a full push at the top; a lower target brakes without undershooting.
            Assert.That(TravelMath.SurgeTargetSpeed(1f), Is.EqualTo(TravelMath.SurgeTopSpeed).Within(1e-4f));
            Assert.That(TravelMath.SurgeTargetSpeed(0.2f), Is.EqualTo(TravelMath.SprintSpeed).Within(1e-4f));
            float s = TravelMath.SurgeStep(30f, 20f, Dt);
            Assert.That(s, Is.LessThan(30f).And.GreaterThanOrEqualTo(20f));
        }

        [Test]
        public void SurgeTurnsWiderAtSpeed()
        {
            float r10 = TravelMath.SurgeTurnRadius(10f);
            float rTop = TravelMath.SurgeTurnRadius(TravelMath.SurgeTopSpeed);
            Assert.That(rTop, Is.GreaterThan(r10 * 3f), "much wider at top speed");
            Assert.That(rTop, Is.InRange(15f, 25f), "about 20 m at top speed");
            Assert.That(r10, Is.LessThan(3f), "tight at 10 m/s");
            Assert.That(TravelMath.SurgeTurnRate(0.5f), Is.EqualTo(TravelMath.SurgeMaxTurnRate), "low speed turning is capped");
            for (float v = 9f; v <= TravelMath.SurgeTopSpeed; v += 1f)
                Assert.That(v * TravelMath.SurgeTurnRate(v), Is.LessThanOrEqualTo(TravelMath.SurgeLateralAccel + 1e-3f), "sideways grip at " + v);
        }

        [Test]
        public void LeapHeightFollowsTheCharge()
        {
            Assert.That(TravelMath.LeapHeight(0f), Is.EqualTo(TravelMath.LeapMinHeight).Within(1e-4f), "a tap");
            Assert.That(TravelMath.LeapHeight(1f), Is.InRange(25f, 30f), "a full charge reaches 25 to 30 m");
            Assert.That(TravelMath.LeapHeight(2f), Is.EqualTo(TravelMath.LeapHeight(1f)), "the charge is capped");
            Assert.That(TravelMath.LeapHeight(0.5f), Is.GreaterThan(TravelMath.LeapHeight(0.25f)));
            Assert.That(TravelMath.LeapChargeAfter(TravelMath.LeapChargeSeconds), Is.EqualTo(1f));
            Assert.That(TravelMath.LeapChargeAfter(TravelMath.LeapChargeSeconds * 0.5f), Is.EqualTo(0.5f).Within(1e-4f));
            foreach (float c in new[] { 0f, 0.5f, 1f })
            {
                // Simulated the way the game moves (speed first, then position), the rise tops out at the asked height.
                float h = TravelMath.LeapHeight(c);
                float vy = TravelMath.LaunchSpeed(h, TravelMath.LeapGravity);
                float y = 0f;
                float peak = 0f;
                while (vy > 0f)
                {
                    vy -= TravelMath.LeapGravity * Dt;
                    y += vy * Dt;
                    peak = Mathf.Max(peak, y);
                }
                Assert.That(peak, Is.EqualTo(h).Within(h * 0.03f), "apex for charge " + c);
                Assert.That(TravelMath.ApexHeight(TravelMath.LaunchSpeed(h, TravelMath.LeapGravity), TravelMath.LeapGravity), Is.EqualTo(h).Within(1e-3f));
            }
            Assert.That(TravelMath.SurgeLeapHeight(TravelMath.SurgeTopSpeed), Is.EqualTo(TravelMath.SurgeLeapMaxHeight).Within(1e-4f));
            Assert.That(TravelMath.SurgeLeapHeight(TravelMath.SprintSpeed), Is.EqualTo(TravelMath.SurgeLeapMinHeight).Within(1e-4f));

            // A leap out of a Surge at top speed covers a long arc.
            float vx = TravelMath.SurgeTopSpeed * 0.92f;
            float vz = TravelMath.LaunchSpeed(TravelMath.SurgeLeapHeight(TravelMath.SurgeTopSpeed), TravelMath.LeapGravity);
            float x = 0f, yy = 0f;
            int steps = 0;
            do
            {
                vz -= (vz > 0f ? TravelMath.LeapGravity : TravelMath.FallGravity) * Dt;
                yy += vz * Dt;
                vx = Mathf.MoveTowards(vx, TravelMath.AirSpeed, TravelMath.AirDrag * Dt);
                x += vx * Dt;
                steps++;
            } while (yy > 0f && steps < 10000);
            Assert.That(x, Is.GreaterThan(50f), "Surge leap distance");
        }

        [Test]
        public void GlideSettlesAtItsSinkSpeed()
        {
            Assert.That(TravelMath.GlideSink, Is.InRange(3f, 4f), "sink about 3.5 m/s");
            Assert.That(TravelMath.GlideForward, Is.InRange(12f, 16f), "forward about 14 m/s");
            float vy = -30f;
            float t = 0f;
            while (vy < -TravelMath.GlideSink && t < 5f)
            {
                vy = TravelMath.GlideVertical(vy, Dt);
                t += Dt;
            }
            Assert.That(t, Is.LessThan(2f), "a fast fall is braked within two seconds");
            for (int i = 0; i < 120; i++) vy = TravelMath.GlideVertical(vy, Dt);
            Assert.That(vy, Is.EqualTo(-TravelMath.GlideSink).Within(1e-3f));
            vy = 0f;
            for (int i = 0; i < 120; i++)
            {
                vy = TravelMath.GlideVertical(vy, Dt);
                Assert.That(vy, Is.GreaterThanOrEqualTo(-TravelMath.GlideSink - 1e-4f), "never sinks faster than the glide");
            }
            Assert.That(vy, Is.EqualTo(-TravelMath.GlideSink).Within(1e-3f), "from the top of a leap it settles into the sink");
        }

        [Test]
        public void FlightSpeedsAndCeiling()
        {
            Assert.That(TravelMath.FlightSpeed(false, false), Is.EqualTo(20f), "cruise");
            Assert.That(TravelMath.FlightSpeed(true, false), Is.EqualTo(45f), "fast");
            Assert.That(TravelMath.FlightSpeed(true, true), Is.EqualTo(TravelMath.FlyAimSpeed), "aiming slows to a hover");
            Assert.That(TravelMath.FlightClimbSpeed(true, false), Is.GreaterThan(TravelMath.FlightClimbSpeed(false, false)));
            Assert.That(TravelMath.FlyFast / TravelMath.FlyFastAccel, Is.InRange(1f, 2f), "about 1.5 s to full speed");

            Assert.That(TravelMath.CeilingClamp(300f, 12f), Is.EqualTo(12f), "no limit well below");
            float inBand = TravelMath.CeilingClamp(TravelMath.SoftCeiling - 10f, 12f);
            Assert.That(inBand, Is.GreaterThan(0f).And.LessThan(12f), "climbing fades near the ceiling");
            Assert.That(TravelMath.CeilingClamp(TravelMath.SoftCeiling, 12f), Is.LessThan(0f));
            Assert.That(TravelMath.CeilingClamp(TravelMath.SoftCeiling + 20f, 5f), Is.LessThan(-20f), "pushed back down above it");
            Assert.That(TravelMath.CeilingClamp(TravelMath.SoftCeiling - 5f, -8f), Is.EqualTo(-8f), "sinking is never limited");
            float y = 380f;
            for (int i = 0; i < 60 * 60; i++) y += TravelMath.CeilingClamp(y, TravelMath.FlyFastClimb) * Dt;
            Assert.That(y, Is.InRange(TravelMath.SoftCeiling - 5f, TravelMath.SoftCeiling + 0.5f), "a minute of climbing settles at the ceiling");

            Assert.That(TravelMath.FlightPitch(PlayerCamera.FootPitch), Is.EqualTo(0f), "the resting camera flies level");
            Assert.That(TravelMath.FlightPitch(40f), Is.GreaterThan(20f), "looking down dives");
            Assert.That(TravelMath.FlightPitch(-30f), Is.LessThan(-20f), "looking up climbs");
            Assert.That(TravelMath.FlightPitch(78f), Is.LessThanOrEqualTo(85f));

            Assert.That(TravelMath.FlightBodyPitch(0f, 0f, 0f), Is.EqualTo(0f).Within(1e-3f), "hovering upright");
            Assert.That(TravelMath.FlightBodyPitch(45f, 0f, 0f), Is.EqualTo(90f).Within(1f), "flat out is horizontal");
            Assert.That(TravelMath.FlightBodyPitch(20f, 0f, 0f), Is.InRange(60f, 70f), "cruising tilts into the path");
            Assert.That(TravelMath.FlightBodyPitch(0f, 18f, 0f), Is.LessThan(5f), "climbing straight up stays upright");
            Assert.That(TravelMath.FlightBodyPitch(20f, -20f, 0f), Is.GreaterThan(90f), "diving pitches past flat");
            Assert.That(TravelMath.BankFromTurn(1000f, 0.3f, 50f), Is.EqualTo(50f));
            Assert.That(TravelMath.BankFromTurn(-60f, 0.3f, 50f), Is.EqualTo(-18f).Within(1e-4f), "a left turn banks left");
        }

        [Test]
        public void DoubleTapTiming()
        {
            DoubleTap d = new DoubleTap();
            Assert.That(d.Press(10f), Is.False);
            Assert.That(d.Press(10.2f), Is.True, "two presses 0.2 s apart");
            Assert.That(d.Press(10.3f), Is.False, "a third quick press starts a new pair");
            Assert.That(d.Press(10.5f), Is.True);
            d.Clear();
            Assert.That(d.Press(20f), Is.False);
            Assert.That(d.Press(20f + TravelMath.DoubleTapWindow + 0.05f), Is.False, "too slow");
            Assert.That(d.Press(21f), Is.False);
            Assert.That(d.Press(21f + TravelMath.DoubleTapWindow - 0.01f), Is.True, "just inside the window");
            d.Clear();
            Assert.That(d.Press(5f), Is.False);
            Assert.That(d.Press(4f), Is.False, "a clock that went backwards is not a double tap");
            Assert.That(TravelMath.DoubleTapWindow, Is.InRange(0.25f, 0.4f));
        }

        [Test]
        public void UnlockRulesFollowTheStory()
        {
            Assert.That(TravelMath.SurgeUnlocked(), Is.True, "Surge from the start");
            Assert.That(TravelMath.LeapUnlocked(0, false, false, false), Is.False);
            Assert.That(TravelMath.FlightUnlocked(0, false, false, false), Is.False);
            Assert.That(TravelMath.LeapUnlocked(1, false, false, false), Is.True, "after the first story mission");
            Assert.That(TravelMath.FlightUnlocked(1, false, false, false), Is.False);
            Assert.That(TravelMath.FlightUnlocked(2, false, false, false), Is.True, "after the second");
            Assert.That(TravelMath.LeapUnlocked(0, true, false, false) && TravelMath.FlightUnlocked(0, true, false, false), Is.True, "Free Roam has everything");
            Assert.That(TravelMath.LeapUnlocked(0, false, true, false), Is.True, "a saved unlock is kept");
            Assert.That(TravelMath.FlightUnlocked(0, false, false, true), Is.True, "debug unlock");
            Assert.That(TravelMath.Unlocked(TravelPower.Glide, 1, false, false, false, false), Is.True, "Glide comes with the Super Leap");
            Assert.That(TravelMath.Unlocked(TravelPower.Surge, 0, false, false, false, false), Is.True);
            Assert.That(TravelMath.Unlocked(TravelPower.Flight, 1, false, false, false, false), Is.False);
            Assert.That(TravelMath.Unlocked(TravelPower.Flight, 0, false, false, true, false), Is.True);
        }

        [Test]
        public void LandingsAndSaveDefaults()
        {
            Assert.That(TravelMath.IsHeavyLanding(20f, 0f), Is.True, "a fall from a leap");
            Assert.That(TravelMath.IsHeavyLanding(3.5f, TravelMath.HeavyLandingSpeed + 1f), Is.True, "a fast touch-down along the ground");
            Assert.That(TravelMath.IsHeavyLanding(TravelMath.GlideSink, TravelMath.GlideForward), Is.False, "a glide lands softly");
            Assert.That(TravelMath.IsHeavyLanding(TravelMath.FlyClimb, 0f), Is.False, "sinking onto the ground in flight lands softly");
            TravelSave s = new TravelSave();
            Assert.That(s.LeapUnlocked || s.FlightUnlocked || s.DebugUnlocked, Is.False, "old saves start locked; their story progress decides");
            Assert.That(s.SurgeUses + s.LeapUses + s.GlideUses + s.FlightUses, Is.EqualTo(0));
            Assert.That(new SaveData().Travel, Is.Not.Null);
        }

        [Test]
        public void TravelSoundsAreFiniteAndAudible()
        {
            for (int i = 0; i < (int)TravelSounds.Clip.Count; i++)
            {
                float[] b = TravelSounds.Make((TravelSounds.Clip)i);
                Assert.That(b.Length, Is.GreaterThan(AudioSynth.Rate / 10), ((TravelSounds.Clip)i).ToString());
                float peak = 0f;
                int bad = 0;
                for (int k = 0; k < b.Length; k++)
                {
                    if (float.IsNaN(b[k]) || float.IsInfinity(b[k])) bad++;
                    else peak = Mathf.Max(peak, Mathf.Abs(b[k]));
                }
                Assert.That(bad, Is.EqualTo(0), ((TravelSounds.Clip)i) + " has no broken samples");
                Assert.That(peak, Is.InRange(0.3f, 1f), ((TravelSounds.Clip)i) + " peak");
            }
        }
    }
}
