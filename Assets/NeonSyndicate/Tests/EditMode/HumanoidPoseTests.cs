using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The procedural pose solver over a long scripted session (walk, jog, sprint, stop, turn, strafe, backpedal, crouch,
    /// jump, attacks, a roll, guard, parry and a stagger): no NaN, rotations stay normalised, feet never sink into the
    /// ground, and once the character stands still both feet are planted flat on the ground.
    /// </summary>
    public sealed class HumanoidPoseTests
    {
        private const float Dt = 1f / 60f;
        private const float Duration = 30f;

        [TestCase("malik")]
        [TestCase("luma")]
        [TestCase("random5")]
        public void LongSessionStaysFiniteAndGrounded(string look)
        {
            RigLayout L = RigLayout.From(TestData.Look(look));
            HumanoidPose pose = new HumanoidPose(L);
            float ankle0 = L.World[RigLayout.FootL].y;
            Vector3 root = Vector3.zero;
            float yaw = 0f;
            float minAnkle = float.MaxValue;
            int steps = Mathf.RoundToInt(Duration / Dt);
            Vector3[] world = null;
            Quaternion[] rot = null;
            for (int k = 0; k < steps; k++)
            {
                float t = k * Dt;
                PoseInput p = PoseInput.Idle();
                Vector3 localVelocity = Script(ref p, t, ref yaw);
                p.RootYaw = yaw;
                p.RootPosition = root;
                p.Velocity = QMath.AY(yaw) * localVelocity;
                pose.Solve(ref p, Dt);
                for (int i = 0; i < pose.Local.Length; i++)
                {
                    Quaternion q = pose.Local[i];
                    if (!TestData.Finite(q)) Assert.Fail("bone " + RigLayout.Names[i] + " rotation is NaN at " + t.ToString("0.00") + " s");
                    float len = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
                    if (Mathf.Abs(len - 1f) > 0.01f) Assert.Fail("bone " + RigLayout.Names[i] + " rotation is not normalised at " + t.ToString("0.00") + " s (" + len + ")");
                }
                if (!TestData.Finite(pose.HipsPosition)) Assert.Fail("hips position is NaN at " + t.ToString("0.00") + " s");
                world = TestData.PoseWorld(pose, root, yaw, out rot);
                if (p.Grounded) minAnkle = Mathf.Min(minAnkle, Mathf.Min(world[RigLayout.FootL].y, world[RigLayout.FootR].y));
                Vector3 v = p.Velocity;
                root += new Vector3(v.x, 0f, v.z) * Dt;
            }
            Assert.That(minAnkle, Is.GreaterThan(ankle0 - 0.015f), "feet never sink into the ground");
            // The script ends with three seconds of standing still: both soles (heel and ball) rest on the ground.
            for (int f = 0; f < 2; f++)
            {
                int bone = f == 0 ? RigLayout.FootL : RigLayout.FootR;
                Vector3 heel = world[bone] + rot[bone] * new Vector3(0f, -ankle0, -0.075f * L.Scale);
                Vector3 ball = world[bone] + rot[bone] * new Vector3(0f, -ankle0, 0.125f * L.Scale);
                string side = f == 0 ? "left" : "right";
                Assert.That(heel.y, Is.InRange(-0.02f, 0.03f), side + " heel on the ground when standing");
                Assert.That(ball.y, Is.InRange(-0.02f, 0.03f), side + " ball of the foot on the ground when standing");
                Assert.That(Vector3.Distance(new Vector3(world[bone].x, 0f, world[bone].z), new Vector3(root.x, 0f, root.z)), Is.LessThan(0.6f * L.Scale), side + " foot under the body");
            }
        }

        [Test]
        public void TeleportBringsTheFeetAlong()
        {
            HumanoidPose pose = new HumanoidPose(RigLayout.From(Appearance.Malik()));
            PoseInput p = PoseInput.Idle();
            for (int i = 0; i < 60; i++) pose.Solve(ref p, Dt);
            p.RootPosition = new Vector3(500f, 0f, -300f);
            pose.Solve(ref p, Dt);
            Quaternion[] rot;
            Vector3[] w = TestData.PoseWorld(pose, p.RootPosition, 0f, out rot);
            Assert.That(Vector3.Distance(w[RigLayout.FootL], p.RootPosition), Is.LessThan(0.6f), "left foot");
            Assert.That(Vector3.Distance(w[RigLayout.FootR], p.RootPosition), Is.LessThan(0.6f), "right foot");
        }

        [Test]
        public void EveryAttackStaysFinite()
        {
            RigLayout L = RigLayout.From(Appearance.Malik());
            foreach (MeleeAnim anim in System.Enum.GetValues(typeof(MeleeAnim)))
            {
                HumanoidPose pose = new HumanoidPose(L);
                for (int k = 0; k < 90; k++)
                {
                    float t = k * Dt;
                    PoseInput p = PoseInput.Idle();
                    p.Hold = HoldType.Melee;
                    p.Blade = true;
                    p.Ready = 1f;
                    if (t >= 0.2f && t <= 1.2f)
                    {
                        p.AttackT = (t - 0.2f) / 1f;
                        p.AttackAnim = anim;
                        p.AttackWindup = 0.3f;
                        p.AttackActive = 0.14f;
                    }
                    pose.Solve(ref p, Dt);
                    for (int i = 0; i < pose.Local.Length; i++)
                        if (!TestData.Finite(pose.Local[i])) Assert.Fail(anim + ": bone " + RigLayout.Names[i] + " rotation is NaN at " + t.ToString("0.00") + " s");
                    if (!TestData.Finite(pose.HipsPosition)) Assert.Fail(anim + ": hips position is NaN");
                }
            }
        }

        /// <summary>The scripted session: fills the action inputs for time t and returns the velocity in root space.</summary>
        private static Vector3 Script(ref PoseInput p, float t, ref float yaw)
        {
            if (t < 2f) return Vector3.zero;
            if (t < 5f) return new Vector3(0f, 0f, 1.45f);
            if (t < 8f) return new Vector3(0f, 0f, 3.9f);
            if (t < 11f) return new Vector3(0f, 0f, 6.8f);
            if (t < 13f) return Vector3.zero;
            if (t < 15f)
            {
                yaw += 140f * Dt;
                return Vector3.zero;
            }
            if (t < 17f)
            {
                p.Ready = 1f;
                return new Vector3(2.6f, 0f, 0f);
            }
            if (t < 19f)
            {
                p.Ready = 1f;
                return new Vector3(0f, 0f, -2f);
            }
            if (t < 21f)
            {
                p.Crouch = 1f;
                return new Vector3(0f, 0f, 1.9f);
            }
            if (t < 21.7f)
            {
                p.Grounded = false;
                p.VerticalVelocity = 5f - 14f * (t - 21f);
                return new Vector3(0f, 0f, 3.9f);
            }
            if (t < 23f) return new Vector3(0f, 0f, 3.9f);
            p.Hold = HoldType.Melee;
            p.Blade = true;
            p.Ready = 1f;
            if (t < 24.4f)
            {
                float u = (t - 23f) / 0.7f;
                p.AttackAnim = u < 1f ? MeleeAnim.SlashRight : MeleeAnim.Overhead;
                p.AttackT = u < 1f ? u : u - 1f;
                p.AttackWindup = 0.3f;
                p.AttackActive = 0.14f;
                return Vector3.zero;
            }
            if (t < 25.04f)
            {
                p.DodgeT = (t - 24.4f) / 0.64f;
                p.DodgeDir = new Vector3(0.6f, 0f, 0.8f);
                return Vector3.zero;
            }
            if (t < 26.5f)
            {
                p.Guard = 1f;
                if (t > 25.8f && t < 26.12f) p.ParryT = (t - 25.8f) / 0.32f;
                return Vector3.zero;
            }
            if (t < 27f)
            {
                p.StaggerT = (t - 26.5f) / 0.5f;
                p.StaggerSide = -0.6f;
                return Vector3.zero;
            }
            // Calm down: weapon away, standing still until the end.
            p.Hold = HoldType.None;
            p.Blade = false;
            p.Ready = 0f;
            return Vector3.zero;
        }
    }
}
