using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Geometry and deformation contracts for 04F art. These do not assert rendered visual quality.</summary>
    public sealed class CharacterArt04FTests
    {
        [Test]
        public void FingerHierarchyAppendsWithoutRenumberingBody()
        {
            Assert.That(RigLayout.Head, Is.EqualTo(4));
            Assert.That(RigLayout.HandL, Is.EqualTo(7));
            Assert.That(RigLayout.HandR, Is.EqualTo(10));
            Assert.That(RigLayout.FootR, Is.EqualTo(16));
            Assert.That(RigLayout.BoneCount, Is.EqualTo(47));
            Assert.That(RigLayout.Parent.Length, Is.EqualTo(RigLayout.BoneCount));
            Assert.That(RigLayout.Names.Length, Is.EqualTo(RigLayout.BoneCount));
            for (int side = 0; side < 2; side++)
                for (int f = 0; f < 5; f++)
                    for (int j = 0; j < 3; j++)
                    {
                        int bone = RigLayout.Finger(side, f, j);
                        Assert.That(RigLayout.Parent[bone], Is.EqualTo(j == 0 ?
                            side == 0 ? RigLayout.HandL : RigLayout.HandR : bone - 1));
                        Assert.That(RigLayout.Parent[bone], Is.LessThan(bone));
                    }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void StandardAndHeroMeshesActuallyWeightEveryFingerJoint(int detail)
        {
            Appearance look = Appearance.Malik();
            BodyData data = BodyBuilder.Build(look, RigLayout.From(look), detail);
            TestContext.WriteLine("04F Malik detail " + detail + ": " + data.Vertices.Count + " vertices, " +
                data.Triangles.Count / 3 + " triangles, one skin renderer.");
            int[] influence = new int[RigLayout.BoneCount];
            foreach (BoneWeight w in data.Weights)
            {
                if (w.weight0 > 0.01f) influence[w.boneIndex0]++;
                if (w.weight1 > 0.01f) influence[w.boneIndex1]++;
            }
            for (int i = RigLayout.BodyBoneCount; i < RigLayout.BoneCount; i++)
                Assert.That(influence[i], Is.GreaterThan(2), "Unweighted digit bone " + RigLayout.Names[i]);
        }

        [Test]
        public void HandGripsAreMirroredAndOpenActuallyDiffersFromClosed()
        {
            Quaternion left = HandPose.Rotation(0, 1, 1, HandGrip.Handle);
            Quaternion right = HandPose.Rotation(1, 1, 1, HandGrip.Handle);
            Vector3 l = left * Vector3.down, r = right * Vector3.down;
            Assert.That(l.x, Is.EqualTo(-r.x).Within(0.0001f));
            Assert.That(l.y, Is.EqualTo(r.y).Within(0.0001f));
            Vector3 open = HandPose.Rotation(0, 1, 1, HandGrip.Open) * Vector3.down;
            Assert.That((open - l).magnitude, Is.GreaterThan(0.6f));
            Assert.That(l.x, Is.GreaterThan(0.4f), "Left fingers curl toward palm");
            Assert.That(r.x, Is.LessThan(-0.4f), "Right fingers curl toward palm");
        }

        [Test]
        public void HandleRadiusStopsDeepCurlAndIndexCanExtend()
        {
            HandGrip empty = HandGrip.Fist, wide = empty;
            wide.HandleRadius = 0.045f;
            Vector3 fist = HandPose.Rotation(0, 1, 1, empty) * Vector3.down;
            Vector3 grasp = HandPose.Rotation(0, 1, 1, wide) * Vector3.down;
            Assert.That(grasp.y, Is.LessThan(fist.y), "A larger handle leaves the finger less closed");
            HandGrip focus = HandGrip.Handle;
            focus.IndexCurl = 0f;
            Vector3 index = HandPose.Rotation(0, 0, 1, focus) * Vector3.down;
            Assert.That(index.y, Is.EqualTo(-1f).Within(0.0001f));
        }

        [TestCase(-1f, false)]
        [TestCase(0.25f, false)]
        [TestCase(0.55f, true)]
        [TestCase(0.85f, false)]
        public void PairedTalonsKeepBothGripsAndFinitePose(float attackT, bool mirror)
        {
            HumanoidPose pose = new HumanoidPose(RigLayout.From(Appearance.Malik()));
            PoseInput input = PoseInput.Idle();
            input.Hold = HoldType.Melee;
            input.Blade = true;
            input.PairedBlades = true;
            input.Ready = 1f;
            input.AttackT = attackT;
            input.AttackMirror = mirror;
            input.AttackAnim = MeleeAnim.SlashRight;
            for (int i = 0; i < 20; i++) pose.Solve(ref input, 1f / 60f);
            Assert.That(pose.GripL, Is.EqualTo(HandHold.Handle));
            Assert.That(pose.GripR, Is.EqualTo(HandHold.Handle));
            foreach (Quaternion q in pose.Local)
                Assert.That(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w), Is.False);
        }

        [TestCase(false, 0.94f)]
        [TestCase(true, 0.28f)]
        public void BladeIsClosedFacetedGeometryWithThicknessAndDistalTaper(bool dagger, float length)
        {
            MeshBuilder mesh = new MeshBuilder(3);
            WeaponModels.BuildBlade04F(mesh, dagger);
            Assert.That(mesh.VertexCount, Is.InRange(1300, 3000));
            Assert.That(mesh.HasGeometry(0) && mesh.HasGeometry(1) && mesh.HasGeometry(2), Is.True);
            float bodyWidth = 0f, tipWidth = 0f, thickness = 0f, maxZ = float.MinValue;
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                Vector3 p = mesh.Vertices[i];
                Assert.That(TestData.Finite(p), Is.True);
                Assert.That(mesh.Normals[i].magnitude, Is.InRange(0.999f, 1.001f));
                maxZ = Mathf.Max(maxZ, p.z);
                if (p.z > length * 0.4f && p.z < length * 0.6f)
                {
                    bodyWidth = Mathf.Max(bodyWidth, Mathf.Abs(p.x));
                    thickness = Mathf.Max(thickness, Mathf.Abs(p.y));
                }
                if (p.z > length * 0.995f) tipWidth = Mathf.Max(tipWidth, Mathf.Abs(p.x));
            }
            Assert.That(maxZ, Is.EqualTo(length).Within(0.0001f));
            Assert.That(thickness, Is.GreaterThan(0.003f));
            Assert.That(tipWidth, Is.LessThan(bodyWidth * 0.2f));
        }
    }
}
