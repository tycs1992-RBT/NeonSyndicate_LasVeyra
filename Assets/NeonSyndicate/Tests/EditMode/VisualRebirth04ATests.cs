using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Native import, resource ownership and geometry tests. Run in Unity, not claimed by the offline audit.</summary>
    [Category("NeedsEngine")]
    public sealed class VisualRebirth04ATests
    {
        [SetUp]
        public void StartWithEmptySourceCache()
        {
            VisualRebirth04AAssets.ResetStatics();
        }

        [TearDown]
        public void ReleaseTestArtSources()
        {
            VisualRebirth04AAssets.ResetStatics();
            VisualRebirth04AQa.ResetStatics();
        }

        [Test]
        public void EveryAuthoredPrototypeImportsWithFiniteTrianglesNormalsUvsAndTangents()
        {
            TextAsset[] sources = Resources.LoadAll<TextAsset>("VisualRebirth04A/Meshes");
            Assert.That(sources.Length, Is.GreaterThanOrEqualTo(60), "04A source kit was not imported.");
            foreach (TextAsset source in sources)
            {
                bool low = source.name.EndsWith("_low", StringComparison.Ordinal);
                string suffix = low ? "_low" : "_high";
                Assert.That(source.name.EndsWith(suffix, StringComparison.Ordinal), Is.True, source.name);
                string id = source.name.Substring(0, source.name.Length - suffix.Length);
                Mesh mesh = VisualRebirth04AAssets.Mesh(id, low);
                Assert.That(mesh.isReadable, Is.True, source.name);
                Assert.That(VisualRebirth04AAssets.Mesh(id, low), Is.SameAs(mesh), "Immutable sources should be shared.");
                Vector3[] vertices = mesh.vertices, normals = mesh.normals;
                Vector2[] uvs = mesh.uv;
                Vector4[] tangents = mesh.tangents;
                int[] triangles = mesh.triangles;
                Assert.That(vertices.Length, Is.GreaterThan(0), source.name);
                Assert.That(normals.Length, Is.EqualTo(vertices.Length), source.name);
                Assert.That(uvs.Length, Is.EqualTo(vertices.Length), source.name);
                Assert.That(tangents.Length, Is.EqualTo(vertices.Length), source.name);
                Assert.That(triangles.Length % 3, Is.Zero, source.name);
                for (int i = 0; i < vertices.Length; i++)
                {
                    Assert.That(Finite(vertices[i].x) && Finite(vertices[i].y) && Finite(vertices[i].z), Is.True, source.name);
                    Assert.That(Finite(uvs[i].x) && Finite(uvs[i].y), Is.True, source.name);
                    Assert.That(normals[i].sqrMagnitude, Is.InRange(.999f, 1.001f), source.name);
                    Assert.That(Finite(tangents[i].x) && Finite(tangents[i].y) && Finite(tangents[i].z) && Finite(tangents[i].w), Is.True, source.name);
                    Vector3 expandedMin = mesh.bounds.min - Vector3.one * .001f;
                    Vector3 expandedMax = mesh.bounds.max + Vector3.one * .001f;
                    Assert.That(vertices[i].x, Is.InRange(expandedMin.x, expandedMax.x), source.name);
                    Assert.That(vertices[i].y, Is.InRange(expandedMin.y, expandedMax.y), source.name);
                    Assert.That(vertices[i].z, Is.InRange(expandedMin.z, expandedMax.z), source.name);
                }
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    Assert.That(a, Is.InRange(0, vertices.Length - 1), source.name);
                    Assert.That(b, Is.InRange(0, vertices.Length - 1), source.name);
                    Assert.That(c, Is.InRange(0, vertices.Length - 1), source.name);
                    Vector3 cross = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                    Assert.That(cross.sqrMagnitude, Is.GreaterThan(1e-18f), source.name);
                    Assert.That(Vector3.Dot(cross, normals[a]), Is.GreaterThan(0f), source.name);
                }
                if (!low)
                    Assert.That(VisualRebirth04AAssets.Mesh(id, true).vertexCount, Is.LessThanOrEqualTo(mesh.vertexCount), source.name);
            }
        }

        [Test]
        public void EveryPaletteAndSurfaceHasPipelineCompatibleMapsAndSharedMaterial()
        {
            string[] roles = { "stone", "trim", "roof", "metal", "gilt", "bark", "leaf", "cloth", "crystal", "water", "glass", "paving", "soil", "flower" };
            for (int style = 0; style < 4; style++)
                foreach (string role in roles)
                {
                    Material material = VisualRebirth04AAssets.Material(style, role);
                    Assert.That(material, Is.Not.Null, style + "/" + role);
                    Assert.That(material.shader, Is.Not.Null, material.name);
                    Assert.That(material.enableInstancing, Is.True, material.name);
                    Assert.That(material.GetTexture("_BumpMap"), Is.Not.Null, material.name);
                    Assert.That(material.GetTexture("_MetallicGlossMap"), Is.Not.Null, material.name);
                    Assert.That(material.GetTexture(RenderPath.IsURP ? "_BaseMap" : "_MainTex"), Is.Not.Null, material.name);
                    Assert.That(VisualRebirth04AAssets.Material(style, role), Is.SameAs(material), material.name);
                    Assert.That(material.renderQueue, Is.EqualTo(2000), "Surface kit should not create transparent overdraw.");
                }
            Assert.That(VisualRebirth04AAssets.MaterialCount, Is.EqualTo(56));
        }

        [Test]
        public void ResetReleasesGeneratedMeshesAndMaterialsAndAllowsCleanRecreation()
        {
            TextAsset[] sources = Resources.LoadAll<TextAsset>("VisualRebirth04A/Meshes");
            Assert.That(sources.Length, Is.GreaterThan(0));
            string key = sources[0].name;
            bool low = key.EndsWith("_low", StringComparison.Ordinal);
            string id = key.Substring(0, key.Length - (low ? 4 : 5));
            Mesh first = VisualRebirth04AAssets.Mesh(id, low);
            Material surface = VisualRebirth04AAssets.Material(0, "stone");
            VisualRebirth04AAssets.ResetStatics();
            Assert.That(first == null, Is.True, "EditMode should destroy the owned runtime mesh immediately.");
            Assert.That(surface == null, Is.True, "EditMode should destroy the owned runtime material immediately.");
            Assert.That(VisualRebirth04AAssets.SourceMeshCount, Is.Zero);
            Assert.That(VisualRebirth04AAssets.MaterialCount, Is.Zero);
            Assert.That(VisualRebirth04AAssets.Mesh(id, low), Is.Not.Null);
            Assert.That(VisualRebirth04AAssets.Material(0, "stone"), Is.Not.Null);
        }

        [Test]
        public void MissingOrInvalidMeshDoesNotPoisonSourceCache()
        {
            VisualRebirth04AAssets.ResetStatics();
            Assert.Throws<ArgumentException>(delegate { VisualRebirth04AAssets.Mesh("../bad", false); });
            Assert.Throws<FileNotFoundException>(delegate { VisualRebirth04AAssets.Mesh("missing_04a_fixture", false); });
            Assert.That(VisualRebirth04AAssets.SourceMeshCount, Is.Zero);
        }

        [Test]
        public void CaptureRouteFollowsActualFortPlacementAndOmitsDestroyedAnchors()
        {
            VisualRebirth04AQa.ResetStatics();
            List<GoldenView> baseline = GoldenViews.Build(null);
            Assert.That(VisualRebirth04AQa.BuildViews(null).Count, Is.EqualTo(2));
            GameObject fort = new GameObject("04A QA test fort");
            try
            {
                fort.transform.position = new Vector3(167f, 2f, -83f);
                fort.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                VisualRebirth04AQa.RegisterStronghold("hearthspire", fort.transform);
                VisualRebirth04AQa.RegisterStronghold("not_a_saved_site", fort.transform);
                List<GoldenView> views = VisualRebirth04AQa.BuildViews(null);
                Assert.That(views.Count, Is.EqualTo(6));
                GoldenView entry = views.Find(delegate(GoldenView view) { return view.Id == "04a_hearthspire_entry"; });
                Vector3 expected = fort.transform.TransformPoint(new Vector3(0f, 1.7f, -37f));
                Assert.That(Vector3.Distance(entry.Position, expected), Is.LessThan(.0001f));
                Assert.That(entry.FollowCamera, Is.True);
                fort.SetActive(false);
                Assert.That(VisualRebirth04AQa.BuildViews(null).Count, Is.EqualTo(2));
                fort.SetActive(true);
                UnityEngine.Object.DestroyImmediate(fort);
                Assert.That(VisualRebirth04AQa.BuildViews(null).Count, Is.EqualTo(2));
                List<GoldenView> retained = GoldenViews.Build(null);
                Assert.That(retained.Count, Is.EqualTo(baseline.Count));
                for (int i = 0; i < baseline.Count; i++)
                {
                    Assert.That(retained[i].Id, Is.EqualTo(baseline[i].Id));
                    Assert.That(retained[i].Position, Is.EqualTo(baseline[i].Position));
                }
            }
            finally
            {
                if (fort != null) UnityEngine.Object.DestroyImmediate(fort);
                VisualRebirth04AQa.ResetStatics();
            }
        }

        [Test]
        public void NeighborhoodModelsFitRuntimeSurveyBoundsOnBothDetailLevels()
        {
            foreach (string name in new[] { "sanctuary", "workshop", "resource" })
            {
                VisualRebirth04ANeighborhoods.Garden garden = VisualRebirth04ANeighborhoods.Load(name);
                Assert.That(garden.nodes.Length, Is.GreaterThan(20), name);
                foreach (VisualRebirth04ANeighborhoods.Node node in garden.nodes)
                    foreach (bool low in new[] { false, true })
                    {
                        Mesh mesh = VisualRebirth04AAssets.Mesh(node.model, low);
                        VisualRebirth04ANeighborhoods.ValidateBounds(mesh.bounds,
                            VisualRebirth04ANeighborhoods.Vector(node.position),
                            Quaternion.Euler(VisualRebirth04ANeighborhoods.Vector(node.euler)),
                            VisualRebirth04ANeighborhoods.Vector(node.scale));
                    }
            }
        }

        [Test]
        public void NeighborhoodSurveyRejectsInvalidAndEscapingTransforms()
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
            Assert.Throws<InvalidDataException>(delegate {
                VisualRebirth04ANeighborhoods.ValidateBounds(bounds, Vector3.right * 1000f, Quaternion.identity, Vector3.one);
            });
            Assert.Throws<InvalidDataException>(delegate {
                VisualRebirth04ANeighborhoods.ValidateBounds(bounds, Vector3.zero, Quaternion.identity, Vector3.zero);
            });
            Assert.Throws<InvalidDataException>(delegate {
                VisualRebirth04ANeighborhoods.Vector(new[] { float.NaN, 0f, 0f });
            });
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
