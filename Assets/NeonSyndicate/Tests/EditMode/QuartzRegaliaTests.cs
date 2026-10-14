using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Regression gates for the art pass: appearance may change, collision, rig and combat actions may not.</summary>
    public sealed class QuartzRegaliaTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RegaliaPreservesSkeletonHitboxesAndCoreDimensions(int detail)
        {
            CreatureLook look = CreatureLooks.For("quartz_drake", 61724);
            CreatureLook previous = look.Clone();
            previous.CrystalRegalia = false;
            CreatureBody before = CreatureBuilder.Build(previous, detail);
            CreatureBody after = CreatureBuilder.Build(look, detail);
            CollectionAssert.AreEqual(before.Skeleton.Names, after.Skeleton.Names);
            CollectionAssert.AreEqual(before.Skeleton.Parents, after.Skeleton.Parents);
            CollectionAssert.AreEqual(before.Skeleton.LocalPositions, after.Skeleton.LocalPositions);
            CollectionAssert.AreEqual(before.HitVolumes, after.HitVolumes);
            Assert.That(after.Radius, Is.EqualTo(before.Radius));
            Assert.That(after.Height, Is.EqualTo(before.Height));
            Assert.That(after.AimPoint, Is.EqualTo(before.AimPoint));
            Assert.That(after.HeadPoint, Is.EqualTo(before.HeadPoint));
            Assert.That(after.MouthPoint, Is.EqualTo(before.MouthPoint));
            Assert.That(after.Mesh.Vertices.Count, Is.GreaterThan(before.Mesh.Vertices.Count));
            int budget = detail == 0 ? 200 : (detail == 1 ? 4500 : 12000);
            Assert.That(after.Mesh.Vertices.Count - before.Mesh.Vertices.Count, Is.LessThan(budget));
            // Appending decoration must not alter an existing source vertex or index, including membrane weights.
            for (int i = 0; i < before.Mesh.Vertices.Count; i++)
            {
                Assert.That(after.Mesh.Vertices[i], Is.EqualTo(before.Mesh.Vertices[i]), "source vertex " + i);
                Assert.That(after.Mesh.Weights[i], Is.EqualTo(before.Mesh.Weights[i]), "source weights " + i);
            }
            for (int i = 0; i < before.Mesh.Triangles.Count; i++)
                Assert.That(after.Mesh.Triangles[i], Is.EqualTo(before.Mesh.Triangles[i]), "source index " + i);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RegaliaGeometryIsDeterministicAndHasValidSkinData(int detail)
        {
            CreatureLook look = CreatureLooks.For("quartz_drake", 742);
            CreatureBody a = CreatureBuilder.Build(look, detail);
            CreatureBody b = CreatureBuilder.Build(look.Clone(), detail);
            CollectionAssert.AreEqual(a.Mesh.Vertices, b.Mesh.Vertices);
            CollectionAssert.AreEqual(a.Mesh.Triangles, b.Mesh.Triangles);
            Assert.That(a.Mesh.Vertices.Count, Is.EqualTo(a.Mesh.Normals.Count));
            Assert.That(a.Mesh.Vertices.Count, Is.EqualTo(a.Mesh.Tangents.Count));
            for (int i = 0; i < a.Mesh.Vertices.Count; i++)
            {
                Vector3 v = a.Mesh.Vertices[i];
                Assert.That(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
                    float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z), Is.False);
                BoneWeight w = a.Mesh.Weights[i];
                Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1f).Within(0.0001f));
                Assert.That(w.boneIndex0, Is.InRange(0, a.Skeleton.Count - 1));
            }
            foreach (int index in a.Mesh.Triangles)
                Assert.That(index, Is.InRange(0, a.Mesh.Vertices.Count - 1));
        }

        [TestCase(CreatureAction.Breath)]
        [TestCase(CreatureAction.Roar)]
        [TestCase(CreatureAction.Death)]
        public void SecondaryMotionLeavesAuthoredCombatActionsUnchanged(CreatureAction action)
        {
            CreatureLook look = CreatureLooks.For("quartz_drake", 41);
            CreatureLook old = look.Clone();
            old.CrystalRegalia = false;
            CreaturePose before = new CreaturePose(CreatureBuilder.Build(old, 0));
            CreaturePose after = new CreaturePose(CreatureBuilder.Build(look, 0));
            for (int frame = 0; frame < 90; frame++)
            {
                CreaturePoseInput input = new CreaturePoseInput
                {
                    RootPosition = Vector3.zero, Grounded = true, Action = action,
                    ActionT = frame / 89f, Windup = 0.35f, Strike = 0.65f
                };
                CreaturePoseInput copy = input;
                before.Solve(ref input, 1f / 60f);
                after.Solve(ref copy, 1f / 60f);
                for (int bone = 0; bone < after.LocalRotations.Length; bone++)
                {
                    Assert.That(after.LocalRotations[bone], Is.EqualTo(before.LocalRotations[bone]), "action bone " + bone);
                    Assert.That(after.LocalPositions[bone], Is.EqualTo(before.LocalPositions[bone]), "action position " + bone);
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuietPerformancePreservesRootAndProducesFinitePose(bool flying)
        {
            CreatureLook look = CreatureLooks.For("quartz_drake", 18);
            CreatureLook old = look.Clone();
            old.CrystalRegalia = false;
            CreatureBody beforeBody = CreatureBuilder.Build(old, 0);
            CreatureBody afterBody = CreatureBuilder.Build(look, 0);
            CreaturePose before = new CreaturePose(beforeBody);
            CreaturePose after = new CreaturePose(afterBody);
            for (int frame = 0; frame < 240; frame++)
            {
                CreaturePoseInput input = new CreaturePoseInput
                {
                    RootPosition = flying ? Vector3.up * 8f : Vector3.zero,
                    Grounded = !flying, Flying = flying, Altitude = flying ? 8f : 0f,
                    Action = CreatureAction.None, ActionT = -1f, Windup = 0.35f, Strike = 0.65f
                };
                CreaturePoseInput copy = input;
                before.Solve(ref input, 1f / 60f);
                after.Solve(ref copy, 1f / 60f);
                int root = afterBody.Skeleton.Root;
                Assert.That(after.LocalPositions[root], Is.EqualTo(before.LocalPositions[root]));
                Assert.That(after.LocalRotations[root], Is.EqualTo(before.LocalRotations[root]));
                for (int bone = 0; bone < after.LocalRotations.Length; bone++)
                {
                    Quaternion q = after.LocalRotations[bone];
                    float norm = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
                    Assert.That(norm, Is.EqualTo(1f).Within(0.001f), "finite unit rotation " + bone);
                }
                foreach (LegChain leg in afterBody.Skeleton.Legs)
                    Assert.That(after.ModelPosition(leg.Toe), Is.EqualTo(before.ModelPosition(leg.Toe)),
                        "visual-only motion leaves foot targets unchanged");
            }
        }

        [Test]
        public void OnlyQuartzDrakeEnablesRegalia()
        {
            foreach (string species in CreatureLooks.Species)
                Assert.That(CreatureLooks.For(species, 61724).CrystalRegalia,
                    Is.EqualTo(species == "quartz_drake"), species);
        }
    }
}
