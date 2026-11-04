using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The skin cover rule behind "their backs come through their shirts" (LiveSkinCover): on a skin sphere with a garment
    /// shell over its upper half, the covered upper triangles go, the bare lower ones and the protected ones stay, nothing
    /// goes without a garment, and the material and bone roles read as intended. Engine-free; native Unity is NOT RUN here.
    /// </summary>
    public sealed class LiveSkinCoverTests
    {
        /// <summary>A UV sphere (rings by segments) of the given radius; normals point outward.</summary>
        private static void Sphere(float radius, int rings, int segments, float maxPolar, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
        {
            int start = vertices.Count;
            for (int r = 0; r <= rings; r++)
            {
                float polar = maxPolar * r / rings;
                for (int s = 0; s <= segments; s++)
                {
                    float az = 2f * Mathf.PI * s / segments;
                    Vector3 n = new Vector3(Mathf.Sin(polar) * Mathf.Cos(az), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(az));
                    vertices.Add(n * radius);
                    normals.Add(n);
                }
            }
            for (int r = 0; r < rings; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = start + r * (segments + 1) + s, b = a + segments + 1;
                    triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
                    triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
                }
        }

        [Test]
        public void CoveredUpperHalfGoesBareLowerHalfStays()
        {
            List<Vector3> skin = new List<Vector3>(), normals = new List<Vector3>();
            List<int> skinTris = new List<int>();
            Sphere(0.15f, 24, 32, Mathf.PI, skin, normals, skinTris);
            List<Vector3> cloth = new List<Vector3>(), clothNormals = new List<Vector3>();
            List<int> clothTris = new List<int>();
            // A garment 2 cm outside the skin over the upper half only.
            Sphere(0.17f, 24, 32, Mathf.PI * 0.5f, cloth, clothNormals, clothTris);
            bool[] covered = LiveSkinCover.CoveredTriangles(skin, normals, skinTris, null, cloth, clothTris);
            int upperHidden = 0, upper = 0, lowerHidden = 0, lower = 0;
            for (int t = 0; t < covered.Length; t++)
            {
                float y = (skin[skinTris[t * 3]].y + skin[skinTris[t * 3 + 1]].y + skin[skinTris[t * 3 + 2]].y) / 3f;
                if (y > 0.03f) { upper++; if (covered[t]) upperHidden++; }
                else if (y < -0.03f) { lower++; if (covered[t]) lowerHidden++; }
            }
            Assert.That(upperHidden, Is.GreaterThan(upper * 9 / 10), "skin under the garment is hidden");
            Assert.That(lowerHidden, Is.EqualTo(0), "bare skin is never hidden");
        }

        [Test]
        public void ProtectedVerticesAndMissingClothingKeepEverything()
        {
            List<Vector3> skin = new List<Vector3>(), normals = new List<Vector3>();
            List<int> skinTris = new List<int>();
            Sphere(0.15f, 12, 16, Mathf.PI, skin, normals, skinTris);
            List<Vector3> cloth = new List<Vector3>(), clothNormals = new List<Vector3>();
            List<int> clothTris = new List<int>();
            Sphere(0.17f, 12, 16, Mathf.PI, cloth, clothNormals, clothTris);
            bool[] all = new bool[skin.Count];
            for (int i = 0; i < all.Length; i++) all[i] = true;
            foreach (bool c in LiveSkinCover.CoveredTriangles(skin, normals, skinTris, all, cloth, clothTris)) Assert.That(c, Is.False);
            foreach (bool c in LiveSkinCover.CoveredTriangles(skin, normals, skinTris, null, new List<Vector3>(), new List<int>())) Assert.That(c, Is.False);
        }

        [Test]
        public void GarmentFarAwayDoesNotCover()
        {
            List<Vector3> skin = new List<Vector3>(), normals = new List<Vector3>();
            List<int> skinTris = new List<int>();
            Sphere(0.15f, 12, 16, Mathf.PI, skin, normals, skinTris);
            List<Vector3> cloth = new List<Vector3>(), clothNormals = new List<Vector3>();
            List<int> clothTris = new List<int>();
            Sphere(0.15f + LiveSkinCover.Reach + 0.05f, 12, 16, Mathf.PI, cloth, clothNormals, clothTris);
            foreach (bool c in LiveSkinCover.CoveredTriangles(skin, normals, skinTris, null, cloth, clothTris)) Assert.That(c, Is.False);
        }

        [Test]
        public void KeepDropsOnlyFlaggedTriangles()
        {
            int[] kept = LiveSkinCover.Keep(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 }, new[] { false, true, false });
            Assert.That(kept, Is.EqualTo(new[] { 0, 1, 2, 6, 7, 8 }));
        }

        [Test]
        public void SegmentTriangleCrossing()
        {
            Vector3 p0 = new Vector3(-1f, 0f, -1f), p1 = new Vector3(1f, 0f, -1f), p2 = new Vector3(0f, 0f, 1f);
            Assert.That(LiveSkinCover.SegmentTriangle(new Vector3(0f, -1f, 0f), new Vector3(0f, 1f, 0f), p0, p1, p2), Is.True);
            Assert.That(LiveSkinCover.SegmentTriangle(new Vector3(0f, 0.5f, 0f), new Vector3(0f, 1f, 0f), p0, p1, p2), Is.False);
            Assert.That(LiveSkinCover.SegmentTriangle(new Vector3(3f, -1f, 0f), new Vector3(3f, 1f, 0f), p0, p1, p2), Is.False);
        }

        [Test]
        public void MaterialAndBoneRoles()
        {
            Assert.That(LiveSkinCover.RoleOf("skin"), Is.EqualTo(LiveCoverRole.Skin));
            Assert.That(LiveSkinCover.RoleOf("courtdefenderskinandunderlayer"), Is.EqualTo(LiveCoverRole.Skin));
            Assert.That(LiveSkinCover.RoleOf("suit"), Is.EqualTo(LiveCoverRole.Cloth));
            Assert.That(LiveSkinCover.RoleOf("coat"), Is.EqualTo(LiveCoverRole.Cloth));
            foreach (string other in new[] { "hair", "beard", "eyes", "teeth", "tongue", "mask", "" })
                Assert.That(LiveSkinCover.RoleOf(other), Is.EqualTo(LiveCoverRole.Other), other);
            Assert.That(LiveSkinCover.ProtectedBone("Head"), Is.True);
            Assert.That(LiveSkinCover.ProtectedBone("neck_01"), Is.True);
            Assert.That(LiveSkinCover.ProtectedBone("hand_l") && LiveSkinCover.ProtectedBone("index_02_r"), Is.True);
            Assert.That(LiveSkinCover.ProtectedBone("spine_03") || LiveSkinCover.ProtectedBone("upperarm_l") || LiveSkinCover.ProtectedBone(""), Is.False);
            Assert.That(LiveCastRules.KeepsOwnLength(RigLayout.Neck) && LiveCastRules.KeepsOwnLength(RigLayout.Head), Is.True);
            Assert.That(LiveCastRules.KeepsOwnLength(RigLayout.Chest) || LiveCastRules.KeepsOwnLength(RigLayout.Hips), Is.False);
        }
    }
}
