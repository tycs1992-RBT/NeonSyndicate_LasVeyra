using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Hero powers (HeroPowerRules, the Shard Gaze entry in Abilities.Spells, and the hero poses): the dive slam's
    /// shockwave, the beam's ramp and limits, the rush strike's gating, line and corridor, the throw and carry
    /// geometry, and that the spell table grew only at its end. Pure maths, offline.
    /// </summary>
    public sealed class HeroPowerTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void SlamGrowsWithTheDive()
        {
            Assert.That(HeroPowerRules.SlamRadius(10f), Is.EqualTo(HeroPowerRules.SlamMinRadius));
            Assert.That(HeroPowerRules.SlamRadius(FreeRunRules.DiveTerminal), Is.EqualTo(HeroPowerRules.SlamMaxRadius));
            float r = HeroPowerRules.SlamRadius(30f);
            Assert.That(HeroPowerRules.SlamDamage(30f, 0f, r), Is.GreaterThan(HeroPowerRules.SlamDamage(30f, r * 0.9f, r)), "weaker at the rim");
            Assert.That(HeroPowerRules.SlamDamage(30f, r + 1f, r), Is.EqualTo(0f), "nothing outside");
            Assert.That(HeroPowerRules.SlamDamage(FreeRunRules.DiveTerminal, 0f, 9f), Is.EqualTo(HeroPowerRules.SlamMaxDamage));
            Vector3 push = HeroPowerRules.SlamPush(new Vector3(1f, 0.3f, 0f), 1f, r, 30f);
            Assert.That(push.x, Is.GreaterThan(3f), "thrown outward");
            Assert.That(push.y, Is.GreaterThan(2f), "and up");
            Assert.That(HeroPowerRules.SlamEnergy, Is.GreaterThan(0f));
            Assert.That(HeroPowerRules.SlamGravityScale, Is.GreaterThan(1f));
        }

        [Test]
        public void BeamRampsOnOneTargetAndStopsWhenReleasedOrSpent()
        {
            float first = HeroPowerRules.BeamTickDamage(0f, HeroPowerRules.BeamTick);
            float ramped = HeroPowerRules.BeamTickDamage(2f, HeroPowerRules.BeamTick);
            Assert.That(first, Is.EqualTo(HeroPowerRules.BeamDps * HeroPowerRules.BeamTick).Within(1e-4f));
            Assert.That(ramped, Is.EqualTo(first * HeroPowerRules.BeamRampMax).Within(1e-4f));
            Assert.That(HeroPowerRules.BeamContinues(true, 50f, 1f), Is.True);
            Assert.That(HeroPowerRules.BeamContinues(false, 50f, 1f), Is.False, "released");
            Assert.That(HeroPowerRules.BeamContinues(true, 0f, 1f), Is.False, "the bar is empty");
            Assert.That(HeroPowerRules.BeamContinues(true, 50f, HeroPowerRules.BeamMaxSeconds + 0.1f), Is.False, "the limit is up");
            // A full bar of 100 lasts several seconds of beam.
            Assert.That(100f / HeroPowerRules.BeamEnergyPerSecond, Is.GreaterThan(5f));
        }

        [Test]
        public void RushStrikeGatingLineAndCorridor()
        {
            Assert.That(HeroPowerRules.RushAllowed(20f, 10f, 50f), Is.True);
            Assert.That(HeroPowerRules.RushAllowed(10f, 10f, 50f), Is.False, "too slow");
            Assert.That(HeroPowerRules.RushAllowed(20f, 1f, 50f), Is.False, "on cooldown");
            Assert.That(HeroPowerRules.RushAllowed(20f, 10f, 5f), Is.False, "no energy");
            Assert.That(HeroPowerRules.RushProgress(0f), Is.EqualTo(0f));
            Assert.That(HeroPowerRules.RushProgress(1f), Is.EqualTo(HeroPowerRules.RushDistance).Within(1e-4f));
            Assert.That(HeroPowerRules.RushProgress(0.5f), Is.GreaterThan(HeroPowerRules.RushDistance * 0.5f), "fast out of the gate");
            Vector3 dir = Vector3.forward;
            Assert.That(HeroPowerRules.InRushCorridor(new Vector3(0.5f, 0f, 5f), dir, 4f, 6f, 0.3f), Is.True, "in the swept stretch");
            Assert.That(HeroPowerRules.InRushCorridor(new Vector3(3f, 0f, 5f), dir, 4f, 6f, 0.3f), Is.False, "too far to the side");
            Assert.That(HeroPowerRules.InRushCorridor(new Vector3(0f, 0f, 12f), dir, 4f, 6f, 0.3f), Is.False, "not reached yet");
            Assert.That(HeroPowerRules.InRushCorridor(new Vector3(0f, 3f, 5f), dir, 4f, 6f, 0.3f), Is.False, "on a balcony above");
            Vector3 push = HeroPowerRules.RushPush(dir, 1f);
            Assert.That(push.z, Is.GreaterThan(5f), "thrown along the line");
            Assert.That(push.x, Is.GreaterThan(0f), "and to the side they were on");
            Assert.That(push.y, Is.GreaterThan(0f));
        }

        [Test]
        public void ThrowsAndCarries()
        {
            Vector3 flat = HeroPowerRules.ThrowVelocity(Vector3.forward, HeroPowerRules.ThrowSpeed);
            Assert.That(flat.y, Is.EqualTo(HeroPowerRules.ThrowMinUp), "a flat aim still lifts the throw");
            Vector3 up = HeroPowerRules.ThrowVelocity(new Vector3(0f, 1f, 1f), HeroPowerRules.ThrowSpeed);
            Assert.That(up.y, Is.GreaterThan(HeroPowerRules.ThrowMinUp));
            Assert.That(up.magnitude, Is.EqualTo(HeroPowerRules.ThrowSpeed).Within(1e-3f));
            Vector3 carry = HeroPowerRules.CarryPoint(new Vector3(10f, 2f, 10f), Vector3.right, HeroPowerRules.CarryHeight);
            Assert.That(carry.y, Is.EqualTo(2f + HeroPowerRules.CarryHeight).Within(1e-4f), "overhead");
            Assert.That(carry.x, Is.GreaterThan(10f), "a little forward");
            Assert.That(HeroPowerRules.CarryHeight, Is.GreaterThan(1.9f), "a carried body clears the head");
            Assert.That(HeroPowerRules.VehicleLiftable(1200f, 0, false, false, 0f), Is.True, "a light empty car");
            Assert.That(HeroPowerRules.VehicleLiftable(1200f, 1, false, false, 0f), Is.False, "someone inside");
            Assert.That(HeroPowerRules.VehicleLiftable(4000f, 0, false, false, 0f), Is.False, "a truck");
            Assert.That(HeroPowerRules.VehicleLiftable(1200f, 0, false, true, 0f), Is.False, "a mission vehicle");
            Assert.That(HeroPowerRules.VehicleLiftable(1200f, 0, false, false, 5f), Is.False, "still moving");
            Assert.That(HeroPowerRules.VehicleCarryHeight(1.5f) - 0.75f, Is.GreaterThan(1.85f), "its underside clears the head");
        }

        [Test]
        public void ShardGazeIsAppendedToTheSpellTable()
        {
            Assert.That(Abilities.Spells.Length, Is.EqualTo(12));
            Assert.That(Abilities.ShardGazeIndex, Is.EqualTo(11));
            Assert.That(Abilities.Spells[Abilities.ShardGazeIndex].Id, Is.EqualTo("SHARD_GAZE"));
            Assert.That(Abilities.Spells[Abilities.SunboltIndex].Id, Is.EqualTo("SUNBOLT"), "the old indices are untouched");
            Assert.That(Abilities.Spells[0].Id, Is.EqualTo("EMBER_CHAIN"));
            Assert.That(ResonanceRules.StateForSpell("SHARD_GAZE"), Is.EqualTo(ResonanceState.Kindled), "the beam primes like the other fire powers");
        }

        [TestCase(TravelPose.Grab)]
        [TestCase(TravelPose.Beam)]
        [TestCase(TravelPose.RushStrike)]
        public void HeroPosesSolveFinite(TravelPose pose)
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            HumanoidPose solver = new HumanoidPose(L);
            for (int k = 0; k < 120; k++)
            {
                float t = k * Dt;
                PoseInput p = PoseInput.Idle();
                p.Grounded = pose != TravelPose.RushStrike;
                p.Travel = pose;
                p.TravelBlend = 1f;
                switch (pose)
                {
                    case TravelPose.Grab: p.TravelLean = new Vector3(Mathf.Clamp01(t / 0.35f), Mathf.Clamp01((t - 1.2f) / 0.3f), 0f); break;
                    case TravelPose.Beam: p.TravelLean = new Vector3(Mathf.Clamp01(t / 0.3f), Mathf.Sin(t) * 50f, 0f); break;
                    case TravelPose.RushStrike: p.TravelLean = new Vector3(Mathf.Repeat(t, 0.3f) / 0.3f, 0f, 0f); break;
                }
                solver.Solve(ref p, Dt);
                for (int i = 0; i < solver.Local.Length; i++)
                {
                    if (!TestData.Finite(solver.Local[i])) Assert.Fail(pose + ": bone " + RigLayout.Names[i] + " is NaN at " + t.ToString("0.00"));
                }
                Assert.That(TestData.Finite(solver.HipsPosition), Is.True);
            }
        }

        [Test]
        public void BeamHandsReachForwardTogether()
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            HumanoidPose solver = new HumanoidPose(L);
            PoseInput p = PoseInput.Idle();
            p.Travel = TravelPose.Beam;
            p.TravelBlend = 1f;
            p.TravelLean = new Vector3(1f, 0f, 0f);
            for (int k = 0; k < 90; k++) solver.Solve(ref p, Dt);
            Quaternion[] rot;
            Vector3[] w = TestData.PoseWorld(solver, Vector3.zero, 0f, out rot);
            Assert.That(w[RigLayout.HandL].z, Is.GreaterThan(0.3f), "left hand forward");
            Assert.That(w[RigLayout.HandR].z, Is.GreaterThan(0.3f), "right hand forward");
            Assert.That(Vector3.Distance(w[RigLayout.HandL], w[RigLayout.HandR]), Is.LessThan(0.35f), "hands together");
            Assert.That(w[RigLayout.HandL].y, Is.GreaterThan(1f), "at the chest line");
        }
    }
}
