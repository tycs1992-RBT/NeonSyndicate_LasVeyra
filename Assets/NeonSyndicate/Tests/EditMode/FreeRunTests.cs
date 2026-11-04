using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// Free-running (FreeRunRules and the free-running poses): climbable surfaces, the climb input and step, wall jumps,
    /// ledge grabs, wall run gating and arcs, running leaps, landing rolls and what they forgive, the dive and its
    /// landings, and the canal water test. Pure maths, so they run offline; the pose checks solve the real solver.
    /// </summary>
    public sealed class FreeRunTests
    {
        private const float Dt = 1f / 60f;

        [Test]
        public void WallsAreClimbableAndFloorsAreNot()
        {
            Assert.That(FreeRunRules.WallClimbable(0f), Is.True, "a vertical wall");
            Assert.That(FreeRunRules.WallClimbable(0.3f), Is.True, "a steep stone face");
            Assert.That(FreeRunRules.WallClimbable(-0.2f), Is.True, "an overhanging face");
            Assert.That(FreeRunRules.WallClimbable(0.7f), Is.False, "a roof slope");
            Assert.That(FreeRunRules.WallClimbable(1f), Is.False, "a floor");
        }

        [Test]
        public void ClimbInputIsClampedAndMovesAlongTheWall()
        {
            Assert.That(FreeRunRules.ClimbInput(1f, 1f), Is.EqualTo(1f), "pushing into the wall climbs");
            Assert.That(FreeRunRules.ClimbInput(-1f, -1f), Is.EqualTo(-1f), "pulling away descends");
            Assert.That(FreeRunRules.ClimbInput(0f, 1f), Is.EqualTo(0.6f).Within(1e-5f), "the stick's forward axis helps");
            Vector3 n = Vector3.back;
            Vector3 tangent = Vector3.right;
            Vector3 up = FreeRunRules.ClimbStep(1f, 0f, 0f, n, tangent, 1f);
            Assert.That(up.y, Is.EqualTo(FreeRunRules.ClimbUpSpeed).Within(1e-4f));
            Assert.That(up.z, Is.GreaterThan(0f), "presses into the wall (the wall is at +z, its normal points back)");
            Vector3 down = FreeRunRules.ClimbStep(-1f, 0f, 0f, n, tangent, 1f);
            Assert.That(down.y, Is.EqualTo(-FreeRunRules.ClimbDownSpeed).Within(1e-4f));
            Assert.That(FreeRunRules.ClimbDownSpeed, Is.GreaterThan(FreeRunRules.ClimbUpSpeed), "down is quicker than up");
            Vector3 side = FreeRunRules.ClimbStep(0f, 1f, 0f, n, tangent, 1f);
            Assert.That(side.x, Is.EqualTo(FreeRunRules.ClimbSideSpeed).Within(1e-4f));
            Vector3 burst = FreeRunRules.ClimbStep(1f, 0f, 1f, n, tangent, 1f);
            Assert.That(burst.y, Is.GreaterThan(up.y * 2f), "a burst hops well faster than the climb");
        }

        [Test]
        public void WallJumpsLeaveTheWallUpward()
        {
            Vector3 n = Vector3.back;
            float vy;
            Vector3 v = FreeRunRules.WallJumpVelocity(n, Vector3.right, 0f, false, 0f, out vy);
            Assert.That(Vector3.Dot(v, n), Is.GreaterThan(4f), "away from the wall");
            Assert.That(vy, Is.GreaterThan(5f), "upward");
            Vector3 steered = FreeRunRules.WallJumpVelocity(n, Vector3.right, 1f, false, 0f, out vy);
            Assert.That(steered.x, Is.GreaterThan(3f), "steered along the stick");
            Vector3 run = FreeRunRules.WallJumpVelocity(n, Vector3.right, 0f, true, 7f, out vy);
            Assert.That(run.x, Is.GreaterThan(5f), "a wall run jump keeps running speed");
            Assert.That(vy, Is.GreaterThan(6f));
        }

        [Test]
        public void MantleAndGrabHeights()
        {
            Assert.That(FreeRunRules.CanMantle(1.5f), Is.True);
            Assert.That(FreeRunRules.CanMantle(0.3f), Is.False, "a kerb is walked over, not mantled");
            Assert.That(FreeRunRules.CanMantle(3f), Is.False, "out of reach");
            Assert.That(FreeRunRules.LedgeGrabbable(2f, -5f, 1f), Is.True, "falling past a ledge toward the wall");
            Assert.That(FreeRunRules.LedgeGrabbable(2f, 6f, 1f), Is.False, "still shooting up");
            Assert.That(FreeRunRules.LedgeGrabbable(2f, -5f, 0f), Is.False, "not moving toward the wall");
            Assert.That(FreeRunRules.LedgeGrabbable(3.5f, -5f, 1f), Is.False, "too high");
            Vector3 root = FreeRunRules.HangRoot(new Vector3(0f, 10f, 5f), Vector3.back, 0.32f);
            Assert.That(root.y, Is.EqualTo(10f - FreeRunRules.HangDrop).Within(1e-4f), "feet hang below the ledge");
            Assert.That(root.z, Is.LessThan(5f), "off the wall");
            Assert.That(FreeRunRules.HangDrop, Is.InRange(1.8f, 2.05f), "hands up at a standing reach");
        }

        [Test]
        public void WallRunGatingAndArc()
        {
            Assert.That(FreeRunRules.WallRunAllowed(6f, 0.1f, 0f, 5f, false), Is.True);
            Assert.That(FreeRunRules.WallRunAllowed(2f, 0.1f, 0f, 5f, false), Is.False, "too slow");
            Assert.That(FreeRunRules.WallRunAllowed(6f, 0.9f, 0f, 5f, false), Is.False, "running into the wall, not along it");
            Assert.That(FreeRunRules.WallRunAllowed(6f, 0.1f, -9f, 5f, false), Is.False, "falling too hard");
            Assert.That(FreeRunRules.WallRunAllowed(6f, 0.1f, 0f, 0.1f, false), Is.False, "just jumped off a wall");
            Assert.That(FreeRunRules.WallRunAllowed(6f, 0.1f, 0f, 0.5f, true), Is.False, "the same wall again too soon");
            Assert.That(FreeRunRules.WallRunAllowed(6f, 0.1f, 0f, 0.5f, false), Is.True, "another wall right away");
            float vy = FreeRunRules.WallRunLift;
            float t = 0f;
            while (t < FreeRunRules.WallRunSeconds)
            {
                vy = FreeRunRules.WallRunVertical(vy, Dt);
                t += Dt;
            }
            Assert.That(vy, Is.GreaterThanOrEqualTo(FreeRunRules.WallRunSink), "never drops fast");
            Assert.That(vy, Is.LessThan(0f), "arcs back down by the end");
            float speed = FreeRunRules.WallRunForward(4f, 0f, Dt);
            Assert.That(speed, Is.GreaterThan(4f), "brought up to running speed");
            float late = FreeRunRules.WallRunForward(FreeRunRules.WallRunSpeed, 1f, Dt);
            Assert.That(late, Is.LessThan(FreeRunRules.WallRunSpeed), "bleeds off late in the run");
            Vector3 tangent = FreeRunRules.WallTangent(new Vector3(1f, 0f, 1f), Vector3.back);
            Assert.That(tangent.z, Is.EqualTo(0f).Within(1e-5f), "no part into the wall");
            Assert.That(tangent.x, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void RunningLeapsOnlyAtAnEdge()
        {
            Assert.That(FreeRunRules.EdgeLeapWanted(true, 8f, false), Is.True);
            Assert.That(FreeRunRules.EdgeLeapWanted(true, 8f, true), Is.False, "ground ahead: an ordinary jump");
            Assert.That(FreeRunRules.EdgeLeapWanted(false, 8f, false), Is.False, "not sprinting");
            float vy;
            Vector3 v = FreeRunRules.EdgeLeapVelocity(Vector3.forward, 8f, out vy);
            Assert.That(v.magnitude, Is.EqualTo(FreeRunRules.EdgeLeapSpeed).Within(1e-4f), "a slow sprint is boosted");
            Assert.That(vy, Is.GreaterThan(5.3f), "higher than the ordinary jump");
            // Range under ordinary gravity: well over a street's width of air time.
            float range = 2f * vy / TravelMath.FallGravity * v.magnitude;
            Assert.That(range, Is.GreaterThan(6f));
        }

        [Test]
        public void LandingRollsForgiveLongDrops()
        {
            Assert.That(FreeRunRules.WantsRoll(14f, 6f, 99f), Is.True, "running into the landing");
            Assert.That(FreeRunRules.WantsRoll(14f, 0f, 99f), Is.False, "standing still, no tap");
            Assert.That(FreeRunRules.WantsRoll(14f, 0f, 0.2f), Is.True, "crouch tapped just before landing");
            Assert.That(FreeRunRules.WantsRoll(5f, 6f, 0.2f), Is.False, "a hop never rolls");
            Assert.That(FreeRunRules.RollDamage(20f), Is.EqualTo(0f), "a 20 m/s drop (about 20 m) is free with a roll");
            Assert.That(FreeRunRules.RollDamage(FreeRunRules.RollSafeFall), Is.EqualTo(0f));
            Assert.That(FreeRunRules.RollDamage(30f), Is.GreaterThan(0f).And.LessThan(60f), "past the limit it hurts a little");
            // Without the roll the ordinary landing hurts from 12 m/s, so the roll matters from the first storey.
            Assert.That(FreeRunRules.RollMinFall, Is.LessThan(12f));
            Assert.That(FreeRunRules.RollSpeed(1f), Is.EqualTo(FreeRunRules.RollSpeedMin));
            Assert.That(FreeRunRules.RollSpeed(20f), Is.EqualTo(FreeRunRules.RollSpeedMax));
            Assert.That(FreeRunRules.RollMotion(0f), Is.EqualTo(1f));
            Assert.That(FreeRunRules.RollMotion(1f), Is.LessThan(0.5f), "slows to a jog as he comes up");
        }

        [Test]
        public void DivesNeedADropAndEndSafely()
        {
            Assert.That(FreeRunRules.CanDive(10f, -2f), Is.True);
            Assert.That(FreeRunRules.CanDive(2f, -2f), Is.False, "too low to dive");
            Assert.That(FreeRunRules.CanDive(10f, 6f), Is.False, "still rising");
            float vy = -1f;
            float t = 0f;
            while (t < 4f)
            {
                vy = FreeRunRules.DiveVertical(vy, Dt);
                t += Dt;
            }
            Assert.That(vy, Is.EqualTo(-FreeRunRules.DiveTerminal).Within(1e-3f), "reaches the terminal speed and holds it");
            Assert.That(FreeRunRules.DiveTerminal, Is.LessThan(TravelMath.TerminalFall + 1f), "no faster than a free fall's cap");
            Assert.That(FreeRunRules.DivePitch(-2f), Is.LessThan(FreeRunRules.DivePitch(-30f)), "steeper head-down with speed");
            Assert.That(FreeRunRules.DiveFlip(10f), Is.EqualTo(0f), "head down high up");
            Assert.That(FreeRunRules.DiveFlip(0f), Is.EqualTo(1f), "feet first at the ground");
            Assert.That(FreeRunRules.HeroLandingWeight(FreeRunRules.DiveTerminal), Is.EqualTo(1f));
            Assert.That(FreeRunRules.HeroLandingWeight(5f), Is.EqualTo(0f));
        }

        [Test]
        public void TheCanalIsTheOnlyWater()
        {
            Assert.That(FreeRunRules.IsCanalWater(0f, CityLayout.CanalZ, FreeRunRules.WaterSurfaceY), Is.True, "the canal");
            Assert.That(FreeRunRules.IsCanalWater(300f, CityLayout.CanalZ + 5f, -5f), Is.True, "across the channel");
            Assert.That(FreeRunRules.IsCanalWater(0f, CityLayout.CanalZ, 0f), Is.False, "a bridge deck at street level");
            Assert.That(FreeRunRules.IsCanalWater(0f, 0f, -5f), Is.False, "the city streets");
            Assert.That(FreeRunRules.IsCanalWater(0f, CityLayout.CanalZ + CityLayout.CanalHalfWidth + 2f, -5f), Is.False, "the embankment walk");
            Assert.That(FreeRunRules.IsCanalWater(1200f, CityLayout.CanalZ, -5f), Is.False, "the desert past the core");
        }

        [TestCase(TravelPose.WallClimb)]
        [TestCase(TravelPose.LedgeHang)]
        [TestCase(TravelPose.WallRun)]
        [TestCase(TravelPose.LandRoll)]
        [TestCase(TravelPose.Dive)]
        public void FreeRunningPosesSolveFinite(TravelPose pose)
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            HumanoidPose solver = new HumanoidPose(L);
            for (int k = 0; k < 120; k++)
            {
                float t = k * Dt;
                PoseInput p = PoseInput.Idle();
                p.Grounded = pose == TravelPose.LandRoll;
                p.Travel = pose;
                p.TravelBlend = 1f;
                switch (pose)
                {
                    case TravelPose.WallClimb: p.TravelLean = new Vector3(1f, 0.3f, t * 3f); break;
                    case TravelPose.LedgeHang: p.TravelLean = new Vector3(0f, 0.5f, 0f); break;
                    case TravelPose.WallRun: p.TravelLean = new Vector3(1f, t * 7f, 1f); break;
                    case TravelPose.LandRoll: p.TravelLean = new Vector3(Mathf.Clamp01(t / 0.55f), 0f, 0f); break;
                    case TravelPose.Dive: p.TravelLean = new Vector3(Mathf.Lerp(95f, 150f, Mathf.Clamp01(t)), Mathf.Clamp01(t - 1.2f), 0f); break;
                }
                solver.Solve(ref p, Dt);
                for (int i = 0; i < solver.Local.Length; i++)
                {
                    Quaternion q = solver.Local[i];
                    if (!TestData.Finite(q)) Assert.Fail(pose + ": bone " + RigLayout.Names[i] + " is NaN at " + t.ToString("0.00"));
                    float len = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
                    if (Mathf.Abs(len - 1f) > 0.01f) Assert.Fail(pose + ": bone " + RigLayout.Names[i] + " is not normalised at " + t.ToString("0.00"));
                }
                Assert.That(TestData.Finite(solver.HipsPosition), Is.True, pose + ": hips");
            }
        }

        [Test]
        public void HangPutsTheHandsUpAtTheLedge()
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            HumanoidPose solver = new HumanoidPose(L);
            PoseInput p = PoseInput.Idle();
            p.Grounded = false;
            p.Travel = TravelPose.LedgeHang;
            p.TravelBlend = 1f;
            for (int k = 0; k < 90; k++) solver.Solve(ref p, Dt);
            Quaternion[] rot;
            Vector3[] w = TestData.PoseWorld(solver, Vector3.zero, 0f, out rot);
            Assert.That(w[RigLayout.HandL].y, Is.GreaterThan(w[RigLayout.Head].y), "left hand above the head");
            Assert.That(w[RigLayout.HandR].y, Is.GreaterThan(w[RigLayout.Head].y), "right hand above the head");
            Assert.That(w[RigLayout.HandL].y, Is.GreaterThan(FreeRunRules.HangDrop - 0.35f), "reaching the ledge height");
            Assert.That(w[RigLayout.FootL].y, Is.GreaterThan(-0.05f), "feet hang above the root, not below it");
        }

        [Test]
        public void ClimbKeepsHandsAndFeetOnTheWallSide()
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            HumanoidPose solver = new HumanoidPose(L);
            PoseInput p = PoseInput.Idle();
            p.Grounded = false;
            p.Travel = TravelPose.WallClimb;
            p.TravelBlend = 1f;
            p.TravelLean = new Vector3(1f, 0f, 0.2f);
            for (int k = 0; k < 90; k++) solver.Solve(ref p, Dt);
            Quaternion[] rot;
            Vector3[] w = TestData.PoseWorld(solver, Vector3.zero, 0f, out rot);
            // The wall is ahead of the root (+z): the hands reach forward and up, the feet are forward of the hips.
            Assert.That(w[RigLayout.HandL].z, Is.GreaterThan(0.15f), "left hand on the wall");
            Assert.That(w[RigLayout.HandR].z, Is.GreaterThan(0.15f), "right hand on the wall");
            Assert.That(w[RigLayout.HandL].y, Is.GreaterThan(w[RigLayout.Chest].y), "hands above the chest");
            Assert.That(w[RigLayout.FootL].z, Is.GreaterThan(w[RigLayout.Hips].z), "feet against the wall");
            Assert.That(w[RigLayout.FootL].y, Is.GreaterThan(0.1f), "feet off the ground");
        }
    }
}
