using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Stealth (StealthRules and the takedown pose): the view cone, how fast sight and sound raise awareness, crouching
    /// and darkness, the noise of Malik's movement, awareness stepping and decay, giving up a hunt, which takedown is
    /// available from where, the approach paths, and the Third Eye's sight pulse. Pure maths, offline.
    /// </summary>
    public sealed class StealthTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void TheViewConeHasAFrontAPeripheryAndABlindBack()
        {
            Assert.That(StealthRules.ConeFactor(10f, 0f, 30f), Is.EqualTo(1f), "straight ahead");
            Assert.That(StealthRules.ConeFactor(10f, 60f, 30f), Is.EqualTo(1f), "inside the full cone");
            Assert.That(StealthRules.ConeFactor(10f, 90f, 30f), Is.EqualTo(StealthRules.PeripheralFactor), "to the side");
            Assert.That(StealthRules.ConeFactor(10f, 170f, 30f), Is.EqualTo(0f), "behind");
            Assert.That(StealthRules.ConeFactor(1.5f, 170f, 30f), Is.GreaterThan(0f), "a presence right behind is felt");
            Assert.That(StealthRules.ConeFactor(40f, 0f, 30f), Is.EqualTo(0f), "out of range");
        }

        [Test]
        public void SightIsQuickUpCloseAndSlowAtTheEdge()
        {
            float near = StealthRules.SightRate(3f, 30f, 1f, false, 2f, 0f);
            float far = StealthRules.SightRate(28f, 30f, 1f, false, 2f, 0f);
            Assert.That(near, Is.GreaterThan(2.5f), "a few tenths of a second at point blank");
            Assert.That(far, Is.LessThan(0.6f), "seconds at the edge of the range");
            Assert.That(1f / far, Is.GreaterThan(1.5f), "time to be noticed at the edge");
            Assert.That(StealthRules.SightRate(10f, 30f, 1f, true, 1f, 0f), Is.LessThan(StealthRules.SightRate(10f, 30f, 1f, false, 1f, 0f)), "crouching hides");
            Assert.That(StealthRules.SightRate(10f, 30f, 1f, false, 8f, 0f), Is.GreaterThan(StealthRules.SightRate(10f, 30f, 1f, false, 2f, 0f)), "sprinting shows");
            Assert.That(StealthRules.SightRate(20f, 30f, 1f, false, 2f, 1f), Is.LessThan(StealthRules.SightRate(20f, 30f, 1f, false, 2f, 0f)), "night hides at a distance");
            Assert.That(StealthRules.SightRate(5f, 30f, 1f, false, 2f, 1f), Is.EqualTo(StealthRules.SightRate(5f, 30f, 1f, false, 2f, 0f)).Within(1e-5f), "not up close");
            Assert.That(StealthRules.SightRate(10f, 30f, 0f, false, 2f, 0f), Is.EqualTo(0f), "nothing outside the cone");
        }

        [Test]
        public void MovementNoiseGrowsWithSpeed()
        {
            Assert.That(StealthRules.MovementNoise(false, 0f, false, false, false), Is.EqualTo(0f), "standing still is silent");
            float crouched = StealthRules.MovementNoise(true, 1.8f, false, false, false);
            float jog = StealthRules.MovementNoise(false, 4.4f, false, false, false);
            float sprint = StealthRules.MovementNoise(false, 8f, true, false, false);
            float surge = StealthRules.MovementNoise(false, 25f, false, true, false);
            Assert.That(crouched, Is.LessThan(jog));
            Assert.That(jog, Is.LessThan(sprint));
            Assert.That(sprint, Is.LessThan(surge));
            Assert.That(crouched, Is.LessThan(2f), "a crouched step is barely heard");
            Assert.That(StealthRules.LandingNoise(20f, true), Is.LessThan(StealthRules.LandingNoise(20f, false)), "a roll is quieter");
            Assert.That(StealthRules.NoiseRate(1f, 10f), Is.GreaterThan(StealthRules.NoiseRate(9f, 10f)), "louder when close");
            Assert.That(StealthRules.NoiseRate(12f, 10f), Is.EqualTo(0f), "out of earshot");
            Assert.That(StealthRules.NoiseCeiling(10f), Is.EqualTo(StealthRules.NoiseCap), "footsteps alone only make them suspicious");
            Assert.That(StealthRules.NoiseCeiling(2f), Is.EqualTo(StealthRules.Alert), "unless right beside them");
        }

        [Test]
        public void AwarenessStepsUpAndFadesAfterADelay()
        {
            float a = 0f;
            float t = 0f;
            while (a < StealthRules.Alert && t < 10f)
            {
                a = StealthRules.Step(a, 1f, StealthRules.Alert, 0f, Dt);
                t += Dt;
            }
            Assert.That(t, Is.InRange(0.9f, 1.1f), "a rate of one fills in about a second");
            Assert.That(StealthRules.Step(0.9f, 1f, StealthRules.NoiseCap, 0f, Dt), Is.EqualTo(0.9f), "never raised above a ceiling already passed");
            Assert.That(StealthRules.Step(0.5f, 0f, StealthRules.Alert, 0.5f, Dt), Is.EqualTo(0.5f), "no fade inside the delay");
            Assert.That(StealthRules.Step(0.5f, 0f, StealthRules.Alert, 3f, Dt), Is.LessThan(0.5f), "fades after it");
            Assert.That(StealthRules.Step(0.001f, 0f, StealthRules.Alert, 3f, Dt), Is.EqualTo(0f), "never below zero");
            Assert.That(StealthRules.Suspicious, Is.LessThan(StealthRules.Alert));
            Assert.That(StealthRules.ShoutAwareness, Is.GreaterThan(StealthRules.Suspicious).And.LessThan(StealthRules.Alert), "an alarm puts friends on guard, not straight into the fight");
        }

        [Test]
        public void HuntersGiveUpOutOfSightAndFarAway()
        {
            Assert.That(StealthRules.LostTarget(10f, 20f), Is.True);
            Assert.That(StealthRules.LostTarget(3f, 20f), Is.False, "not long enough");
            Assert.That(StealthRules.LostTarget(10f, 5f), Is.False, "too close to give up");
            Assert.That(StealthRules.LostAwareness, Is.GreaterThanOrEqualTo(StealthRules.Suspicious), "they keep looking around");
        }

        [Test]
        public void TakedownsFromBehindAboveAndTheAir()
        {
            Assert.That(StealthRules.Available(0f, 1.2f, -0.8f, 0.5f, false, true), Is.EqualTo(TakedownKind.Ground), "behind a suspicious guard");
            Assert.That(StealthRules.Available(0f, 1.2f, 0.9f, 0.1f, false, true), Is.EqualTo(TakedownKind.Ground), "in front of one who has not noticed at all");
            Assert.That(StealthRules.Available(0f, 1.2f, 0.9f, 0.6f, false, true), Is.EqualTo(TakedownKind.None), "in front of a suspicious one");
            Assert.That(StealthRules.Available(0f, 1.2f, -0.8f, 1f, false, true), Is.EqualTo(TakedownKind.None), "an alarmed one, even from behind");
            Assert.That(StealthRules.Available(0f, 3f, -0.8f, 0f, false, true), Is.EqualTo(TakedownKind.None), "out of reach");
            Assert.That(StealthRules.Available(3f, 1.5f, 0.5f, 1f, false, true), Is.EqualTo(TakedownKind.Drop), "from a roof edge, even on an alarmed one");
            Assert.That(StealthRules.Available(3f, 1.5f, 0.5f, 0f, false, false), Is.EqualTo(TakedownKind.None), "three metres up but not at an edge: nothing");
            Assert.That(StealthRules.Available(6f, 2f, 0.5f, 1f, true, false), Is.EqualTo(TakedownKind.Air), "out of the air");
            Assert.That(StealthRules.Available(20f, 2f, 0.5f, 0f, true, false), Is.EqualTo(TakedownKind.None), "too high");
            Assert.That(StealthRules.Available(0.5f, 1f, -0.8f, 0f, true, false), Is.EqualTo(TakedownKind.None), "a hop is not an air takedown");
            Assert.That(StealthRules.ApproachSeconds(TakedownKind.Ground, 0f), Is.EqualTo(StealthRules.GroundApproach));
            Assert.That(StealthRules.ApproachSeconds(TakedownKind.Drop, 6f), Is.GreaterThan(StealthRules.ApproachSeconds(TakedownKind.Drop, 2f)), "a longer drop takes longer");
            Vector3 mid = StealthRules.ApproachPoint(new Vector3(0f, 5f, 0f), new Vector3(2f, 0f, 0f), 0.5f, TakedownKind.Drop);
            Assert.That(mid.y, Is.GreaterThan(2.5f), "a drop starts slowly (ease in)");
            Assert.That(StealthRules.ApproachPoint(Vector3.zero, Vector3.one, 1f, TakedownKind.Ground), Is.EqualTo(Vector3.one));
            Assert.That(StealthRules.LethalAmount(400f, 200f), Is.GreaterThan(600f * 2f), "always lethal through armour");
        }

        [Test]
        public void TheSightPulseSpreadsAndFades()
        {
            Assert.That(StealthRules.PulseAlpha(-1f), Is.EqualTo(0f));
            Assert.That(StealthRules.PulseAlpha(1f), Is.EqualTo(1f));
            Assert.That(StealthRules.PulseAlpha(StealthRules.PulseSeconds - 0.5f), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(StealthRules.PulseAlpha(StealthRules.PulseSeconds + 0.1f), Is.EqualTo(0f));
            Assert.That(StealthRules.PulseReached(0.1f, 50f), Is.False, "the ring has not got there yet");
            Assert.That(StealthRules.PulseReached(StealthRules.PulseRingSeconds, 50f), Is.True);
            Assert.That(StealthRules.PulseRingRadius(StealthRules.PulseRingSeconds * 2f), Is.EqualTo(StealthRules.PulseRange));
        }

        [Test]
        public void TakedownPoseSolvesFinite()
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            HumanoidPose solver = new HumanoidPose(L);
            for (int k = 0; k < 120; k++)
            {
                float t = k * Dt;
                PoseInput p = PoseInput.Idle();
                p.Travel = TravelPose.Takedown;
                p.TravelBlend = 1f;
                p.TravelLean = new Vector3(Mathf.Clamp01(t / 1.5f), k % 2 == 0 ? 0f : 1f, 0f);
                solver.Solve(ref p, Dt);
                for (int i = 0; i < solver.Local.Length; i++)
                {
                    if (!TestData.Finite(solver.Local[i])) Assert.Fail("bone " + RigLayout.Names[i] + " is NaN at " + t.ToString("0.00"));
                }
            }
        }
    }
}
