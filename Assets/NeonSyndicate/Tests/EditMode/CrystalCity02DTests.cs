using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Geometry and palette contracts. Passing these does not establish frame rate or visual quality.</summary>
    public sealed class CrystalCity02DTests
    {
        [TestCase(-181f, CrystalCityStyle.VerdantReach)]
        [TestCase(-180f, CrystalCityStyle.AmethystWard)]
        [TestCase(0f, CrystalCityStyle.AmethystWard)]
        [TestCase(240f, CrystalCityStyle.AmethystWard)]
        [TestCase(241f, CrystalCityStyle.DawnstoneCourt)]
        public void DistrictPaletteUsesStableBoundaries(float x, CrystalCityStyle expected)
        {
            Assert.That(CrystalCity02DMaterials.StyleAt(new Vector3(x,0f,0f)), Is.EqualTo(expected));
        }

        [Test]
        public void AllMeshesHaveFiniteVerticesIndicesNormalsAndUvs()
        {
            foreach (string id in CrystalCity02DGeometry.Names)
                for (int lod = 0; lod < 2; lod++)
                {
                    Mesh mesh = CrystalCity02DGeometry.Get(id, lod == 1);
                    Assert.That(mesh, Is.Not.Null, id);
                    Vector3[] vertices = mesh.vertices;
                    Vector3[] normals = mesh.normals;
                    Vector2[] uvs = mesh.uv;
                    int[] triangles = mesh.triangles;
                    Assert.That(vertices.Length, Is.GreaterThan(0), id);
                    Assert.That(normals.Length, Is.EqualTo(vertices.Length), id);
                    Assert.That(uvs.Length, Is.EqualTo(vertices.Length), id);
                    Assert.That(triangles.Length % 3, Is.EqualTo(0), id);
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        Assert.That(Finite(vertices[i].x) && Finite(vertices[i].y) && Finite(vertices[i].z), Is.True, id);
                        Assert.That(normals[i].sqrMagnitude, Is.InRange(.98f,1.02f), id);
                        Assert.That(Finite(uvs[i].x) && Finite(uvs[i].y), Is.True, id);
                    }
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        for (int j = 0; j < 3; j++) Assert.That(triangles[i+j], Is.InRange(0,vertices.Length - 1), id);
                        Vector3 a = vertices[triangles[i]], b = vertices[triangles[i+1]], c = vertices[triangles[i+2]];
                        Vector3 cross = Vector3.Cross(b - a, c - a);
                        Assert.That(cross.sqrMagnitude, Is.GreaterThan(1e-14f), id);
                        Assert.That(Vector3.Dot(cross.normalized, normals[triangles[i]]), Is.GreaterThan(.98f), id);
                    }
                }
        }

        [Test]
        public void EveryLodIsCheaperAndMeshRequestsAreShared()
        {
            foreach (string id in CrystalCity02DGeometry.Names)
            {
                Mesh near = CrystalCity02DGeometry.Get(id);
                Assert.That(CrystalCity02DGeometry.Get(id), Is.SameAs(near), id);
                Assert.That(CrystalCity02DGeometry.Get(id, true).vertexCount, Is.LessThan(near.vertexCount), id);
            }
        }

        [Test]
        public void ResetClearsGardenCounters()
        {
            CrystalGardenDressing.ResetStatics();
            Assert.That(CrystalGardenDressing.GardenCount, Is.EqualTo(0));
        }

        [TearDown]
        public void Cleanup()
        {
            CrystalCity02DGeometry.ResetStatics();
            CrystalCity02DMaterials.ResetStatics();
            CrystalGardenDressing.ResetStatics();
        }

        private static bool Finite(float n) { return !float.IsNaN(n) && !float.IsInfinity(n); }
    }
}
