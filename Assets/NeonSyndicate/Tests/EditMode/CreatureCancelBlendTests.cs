using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The visual fade out of a cut creature action (Phase 5 of the visual upgrade): an action that is cancelled at
    /// full weight no longer drops its pose in one frame, the fade is over within CancelBlendSeconds, an action that
    /// runs to its end never starts one, and a reset clears it.
    /// </summary>
    public sealed class CreatureCancelBlendTests
    {
        private const float Dt = 1f / 60f;

        private static CreaturePose NewPose(int seed)
        {
            return new CreaturePose(CreatureBuilder.Build(CreatureLooks.For("quartz_drake", seed), 0));
        }

        private static CreaturePoseInput Input(CreatureAction action, float t)
        {
            CreaturePoseInput p = CreaturePoseInput.Idle(Vector3.zero, 0f);
            p.Grounded = true;
            p.Action = action;
            p.ActionT = t;
            p.Windup = 0.35f;
            p.Strike = 0.65f;
            return p;
        }

        private static float MaxBoneStep(Quaternion[] a, Quaternion[] b)
        {
            float worst = 0f;
            for (int i = 0; i < a.Length; i++)
            {
                float d = Mathf.Clamp(Mathf.Abs(QMath.Dot(QMath.Normalize(a[i]), QMath.Normalize(b[i]))), 0f, 1f);
                float ang = 2f * Mathf.Acos(d) * Mathf.Rad2Deg;
                if (ang > worst) worst = ang;
            }
            return worst;
        }

        private static Quaternion[] Copy(Quaternion[] a)
        {
            Quaternion[] c = new Quaternion[a.Length];
            for (int i = 0; i < a.Length; i++) c[i] = a[i];
            return c;
        }

        [Test]
        public void CuttingAStrikeFadesInsteadOfSnapping()
        {
            CreaturePose blended = NewPose(7);
            // Settle, then wind up and strike until the pose carries full weight.
            for (int i = 0; i < 60; i++) { CreaturePoseInput idle = Input(CreatureAction.None, -1f); blended.Solve(ref idle, Dt); }
            for (int i = 0; i <= 30; i++) { CreaturePoseInput atk = Input(CreatureAction.Bite, i / 60f); blended.Solve(ref atk, Dt); }
            Quaternion[] before = Copy(blended.LocalRotations);
            Assert.That(blended.CancelBlendWeight, Is.EqualTo(0f), "no fade while the action runs");
            // The cut: back to the gait with no action.
            CreaturePoseInput cut = Input(CreatureAction.None, -1f);
            blended.Solve(ref cut, Dt);
            Assert.That(blended.CancelBlendWeight, Is.GreaterThan(0.5f), "a strike cut at full weight starts a strong fade");
            float firstStep = MaxBoneStep(before, blended.LocalRotations);
            Assert.That(firstStep, Is.LessThan(12f), "the first frame after the cut moves no bone far");
            // The fade ends within its window and the output is then the plain solve again.
            int frames = 0;
            while (blended.CancelBlendWeight > 0f && frames < 60)
            {
                CreaturePoseInput idle = Input(CreatureAction.None, -1f);
                blended.Solve(ref idle, Dt);
                frames++;
            }
            Assert.That(frames * Dt, Is.LessThanOrEqualTo(CreaturePose.CancelBlendSeconds + Dt + 1e-4f));
            Assert.That(blended.CancelBlendWeight, Is.EqualTo(0f));
        }

        [Test]
        public void TheFadeIsMonotonicAndEveryFrameStaysFinite()
        {
            CreaturePose pose = NewPose(11);
            for (int i = 0; i <= 30; i++) { CreaturePoseInput atk = Input(CreatureAction.Claw, i / 60f); pose.Solve(ref atk, Dt); }
            float last = 2f;
            for (int i = 0; i < 20; i++)
            {
                CreaturePoseInput idle = Input(CreatureAction.None, -1f);
                pose.Solve(ref idle, Dt);
                float w = pose.CancelBlendWeight;
                Assert.That(w, Is.LessThanOrEqualTo(last + 1e-5f), "fade weight only falls");
                last = w;
                for (int b = 0; b < pose.LocalRotations.Length; b++)
                {
                    Quaternion q = pose.LocalRotations[b];
                    Assert.That(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w), Is.False);
                    Assert.That(Mathf.Abs(QMath.Dot(q, q) - 1f), Is.LessThan(1e-3f), "unit rotation");
                    Vector3 p = pose.LocalPositions[b];
                    Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z), Is.False);
                }
            }
        }

        [Test]
        public void AnActionThatRunsToItsEndNeverFades()
        {
            CreaturePose pose = NewPose(3);
            for (int i = 0; i < 90; i++)
            {
                CreaturePoseInput atk = Input(CreatureAction.Bite, i / 89f);
                pose.Solve(ref atk, Dt);
                Assert.That(pose.CancelBlendWeight, Is.EqualTo(0f), "frame " + i);
            }
            for (int i = 0; i < 10; i++)
            {
                CreaturePoseInput idle = Input(CreatureAction.None, -1f);
                pose.Solve(ref idle, Dt);
                Assert.That(pose.CancelBlendWeight, Is.EqualTo(0f), "after the action");
            }
        }

        [Test]
        public void ChainingIntoAnotherActionAndRestartingBothFade()
        {
            CreaturePose pose = NewPose(5);
            for (int i = 0; i <= 30; i++) { CreaturePoseInput atk = Input(CreatureAction.Bite, i / 60f); pose.Solve(ref atk, Dt); }
            CreaturePoseInput next = Input(CreatureAction.Stagger, 0f);
            pose.Solve(ref next, Dt);
            Assert.That(pose.CancelBlendWeight, Is.GreaterThan(0.5f), "a strike cut by a stagger fades");
            CreaturePose again = NewPose(5);
            for (int i = 0; i <= 30; i++) { CreaturePoseInput atk = Input(CreatureAction.Bite, i / 60f); again.Solve(ref atk, Dt); }
            CreaturePoseInput restart = Input(CreatureAction.Bite, 0f);
            again.Solve(ref restart, Dt);
            Assert.That(again.CancelBlendWeight, Is.GreaterThan(0.5f), "the same action restarted fades");
        }

        [Test]
        public void ResetAndTeleportClearTheFade()
        {
            CreaturePose pose = NewPose(9);
            for (int i = 0; i <= 30; i++) { CreaturePoseInput atk = Input(CreatureAction.Bite, i / 60f); pose.Solve(ref atk, Dt); }
            CreaturePoseInput cut = Input(CreatureAction.None, -1f);
            pose.Solve(ref cut, Dt);
            Assert.That(pose.CancelBlendWeight, Is.GreaterThan(0f));
            pose.Reset(Vector3.zero, 0f);
            Assert.That(pose.CancelBlendWeight, Is.EqualTo(0f));
            for (int i = 0; i <= 30; i++) { CreaturePoseInput atk = Input(CreatureAction.Bite, i / 60f); pose.Solve(ref atk, Dt); }
            CreaturePoseInput far = Input(CreatureAction.None, -1f);
            far.RootPosition = new Vector3(500f, 0f, 500f);
            pose.Solve(ref far, Dt);
            Assert.That(pose.CancelBlendWeight, Is.EqualTo(0f), "a teleport resets the pose instead of fading it");
        }
    }
}
