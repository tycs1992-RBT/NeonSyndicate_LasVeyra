using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class ForgeCourtWR02ITests
    {
        [TestCase("forge", 30f, 30f, false, true)]
        [TestCase("lofts", 30f, 30f, false, false)]
        [TestCase("archive", 30f, 48f, false, false)]
        [TestCase("forge", 30f, 30f, true, false)]
        [TestCase("forge", 30f, 31f, false, false)]
        [TestCase("forge", float.NaN, 30f, false, false)]
        public void OnlyTheKnownUnchangedForgeFrameIsEligible(string id, float width, float depth, bool receipt, bool expected)
        { Assert.AreEqual(expected, HeartPlanFR01.ForgeCourtFrameWR02I(id, width, depth, receipt)); }

        [Test] public void PartialShellCannotBecomeAReplacement()
        {
            Assert.Throws<InvalidOperationException>(() => HeartPlanFR01.ForgeCourtBuildWR02I(new MeshBuilder(1),
                new List<ArtPlacementH2>(), model => null));
        }

        [Test] public void ShippedPhysicalMeshesProduceCompleteBoundedArchitecture()
        {
            VenueBinding04C binding = new VenueBinding04C { Definition = new VenueDefinition04C { Id = "forge" },
                Building = new Building { Seed = 584924, Height = 16 }, Width = 30, Depth = 30 };
            MeshBuilder structure = new MeshBuilder(1); GothicHeartH3.Structure(structure, binding);
            List<ArtPlacementH2> parts = GothicHeartH3.ServicePlan(binding);
            Dictionary<string, MeshBuilder> sources = new Dictionary<string, MeshBuilder>();
            int triangleCount = structure.Triangles(0).Count / 3;
            foreach (ArtPlacementH2 part in parts)
            {
                if (!sources.ContainsKey(part.Model)) sources.Add(part.Model, AscensionArtH2.Decode(File.ReadAllBytes(
                    "Assets/NeonSyndicate/Resources/AscensionH2/Meshes/" + part.Model + "_low.bytes")));
                triangleCount += sources[part.Model].Triangles(0).Count / 3;
            }
            ForgeCourtArtWR02I art = HeartPlanFR01.ForgeCourtBuildWR02I(structure, parts, model => sources[model]);
            Assert.AreEqual(triangleCount, art.SourceTriangles);
            int farTriangles = 0; foreach (MeshBuilder mesh in art.Far.Values) farTriangles += mesh.Triangles(0).Count / 3;
            Assert.AreEqual(triangleCount, farTriangles, "Roof/portal/floor support must stay visible at distance.");
            Assert.Greater(art.ReliefPanels, 300); Assert.LessOrEqual(art.ReliefVertices, 360000);
            Assert.LessOrEqual(art.MaximumRelief, .022001f);
            CollectionAssert.AreEquivalent(new[] { "Masonry", "CarvedTrim", "slate", "iron", "amethyst" }, art.Near.Keys);
        }
    }

    [Category("NeedsEngine")]
    public sealed class ForgeCourtNativeWR02ITests
    {
        private GameObject _root, _legacy, _candidate;
        private Mesh _mesh;
        private Material _material;
        private HeartPresentationFR01 _heart;
        private BoxCollider _originalCollision;

        [SetUp] public void SetUp()
        {
            _root = new GameObject("Forge native transaction fixture");
            _originalCollision = _root.AddComponent<BoxCollider>();
            _mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
            _mesh.RecalculateBounds(); _mesh.RecalculateNormals();
            Shader shader = Shader.Find(RenderPath.IsURP ? "Universal Render Pipeline/Lit" : "Standard");
            Assert.IsNotNull(shader);
            _material = new Material(shader); _material.mainTexture = Texture2D.whiteTexture;
            _material.SetTexture("_BumpMap", Texture2D.normalTexture);
            _material.SetTexture("_MetallicGlossMap", Texture2D.whiteTexture);
            _legacy = Visual("Original"); _candidate = Visual("Candidate"); _candidate.SetActive(false);
            _heart = new HeartPresentationFR01(null, _root.transform); _heart.State(false, false);
        }
        private GameObject Visual(string name)
        {
            GameObject node = new GameObject(name); node.transform.SetParent(_root.transform, false);
            node.AddComponent<MeshFilter>().sharedMesh = _mesh; node.AddComponent<MeshRenderer>().sharedMaterial = _material;
            return node;
        }
        private bool Register(out string reason)
        { return _heart.RegisterRewriteVisual("forge-native-fixture", "ForgeCourtWR02I", _legacy, _candidate, "Fixture", out reason); }

        [Test] public void PreviewAndStreamingRetainTheSeparatePhysicalOwner()
        {
            string reason; Assert.True(Register(out reason), reason); _heart.BeginFantasyPreviewV1();
            Assert.True(_candidate.activeSelf); Assert.False(_legacy.activeSelf); Assert.True(_originalCollision.enabled);
            _root.SetActive(false); _root.SetActive(true);
            Assert.True(_candidate.activeInHierarchy); Assert.True(_originalCollision.enabled);
            _heart.EndFantasyPreviewV1(); Assert.True(_legacy.activeSelf); Assert.False(_candidate.activeSelf);
        }
        [Test] public void LostMapRestoresOriginalAtActivation()
        {
            string reason; Assert.True(Register(out reason), reason); _material.SetTexture("_BumpMap", null);
            _heart.BeginFantasyPreviewV1(); Assert.True(_legacy.activeSelf); Assert.False(_candidate.activeSelf);
            Assert.True(_originalCollision.enabled);
        }
        [Test] public void AColliderCannotEnterTheVisualTransaction()
        {
            _candidate.AddComponent<BoxCollider>(); string reason;
            Assert.False(Register(out reason)); Assert.True(_legacy.activeSelf); Assert.False(_candidate.activeSelf);
        }
        [TearDown] public void TearDown()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            if (_mesh != null) UnityEngine.Object.DestroyImmediate(_mesh);
            if (_material != null) UnityEngine.Object.DestroyImmediate(_material);
        }
    }
}
