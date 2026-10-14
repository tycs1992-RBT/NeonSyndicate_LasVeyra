using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Generated character bodies: valid geometry, unit normals, skin weights that sum to one, feet on the ground.</summary>
    public sealed class BodyBuilderTests
    {
        private static readonly int[] MaxVertices = { 4200, 8200, 13500 };

        [TestCase("malik", 0)]
        [TestCase("malik", 2)]
        [TestCase("luma", 1)]
        [TestCase("police", 1)]
        [TestCase("swat", 2)]
        [TestCase("armoured", 1)]
        [TestCase("random1", 0)]
        [TestCase("random2", 1)]
        [TestCase("random3", 2)]
        [TestCase("random4", 1)]
        public void BodyIsValid(string look, int detail)
        {
            Appearance a = TestData.Look(look);
            RigLayout rig = RigLayout.From(a);
            BodyData d = BodyBuilder.Build(a, rig, detail);
            int n = d.Vertices.Count;
            Assert.That(n, Is.GreaterThan(1500).And.LessThan(MaxVertices[detail]), "vertex budget for detail " + detail);
            Assert.That(d.Normals.Count, Is.EqualTo(n), "normals");
            Assert.That(d.Weights.Count, Is.EqualTo(n), "skin weights");
            Assert.That(d.Tangents.Count, Is.EqualTo(n), "tangents");
            Assert.That(d.Palette.Count, Is.EqualTo(n), "palette coordinates");
            Assert.That(d.Detail.Count, Is.EqualTo(n), "detail coordinates");
            int badPos = 0, badNormal = 0, badWeight = 0;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < n; i++)
            {
                Vector3 v = d.Vertices[i];
                if (!TestData.Finite(v)) badPos++;
                minY = Mathf.Min(minY, v.y);
                maxY = Mathf.Max(maxY, v.y);
                Vector3 nm = d.Normals[i];
                if (!TestData.Finite(nm) || Mathf.Abs(nm.magnitude - 1f) > 0.01f) badNormal++;
                BoneWeight w = d.Weights[i];
                float sum = w.weight0 + w.weight1 + w.weight2 + w.weight3;
                bool okSum = Mathf.Abs(sum - 1f) <= 0.01f && w.weight0 >= 0f && w.weight1 >= 0f && w.weight2 >= 0f && w.weight3 >= 0f;
                bool okBones = ValidBone(w.boneIndex0, w.weight0) && ValidBone(w.boneIndex1, w.weight1) && ValidBone(w.boneIndex2, w.weight2) && ValidBone(w.boneIndex3, w.weight3);
                if (!okSum || !okBones) badWeight++;
            }
            Assert.That(badPos, Is.EqualTo(0), "vertices with NaN or infinite positions");
            Assert.That(badNormal, Is.EqualTo(0), "normals that are not unit length");
            Assert.That(badWeight, Is.EqualTo(0), "vertices whose weights do not sum to one or use a bone that does not exist");
            Assert.That(d.Triangles.Count % 3, Is.EqualTo(0), "whole triangles");
            Assert.That(d.Triangles.Count, Is.GreaterThan(0), "triangles");
            int badIndex = 0;
            foreach (int t in d.Triangles) if (t < 0 || t >= n) badIndex++;
            Assert.That(badIndex, Is.EqualTo(0), "triangle indices outside the vertex list");
            Assert.That(minY, Is.InRange(-0.02f, 0.02f), "soles on the ground");
            Assert.That(maxY, Is.InRange(1.45f * rig.Scale, 2.05f * rig.Scale), "standing height");
        }

        [Test]
        public void SameLookBuildsTheSameBody()
        {
            Appearance a = TestData.Look("random7");
            BodyData x = BodyBuilder.Build(a, RigLayout.From(a), 1);
            BodyData y = BodyBuilder.Build(a, RigLayout.From(a), 1);
            Assert.That(y.Vertices.Count, Is.EqualTo(x.Vertices.Count));
            Assert.That(y.Triangles.Count, Is.EqualTo(x.Triangles.Count));
            for (int i = 0; i < x.Vertices.Count; i += 97) Assert.That((x.Vertices[i] - y.Vertices[i]).sqrMagnitude, Is.LessThan(1e-10f), "vertex " + i);
        }

        /// <summary>Every influence slot must name a real bone, even unused ones (the GPU skins with all four).</summary>
        private static bool ValidBone(int index, float weight)
        {
            return weight >= 0f && index >= 0 && index < RigLayout.BoneCount;
        }
    }
}
