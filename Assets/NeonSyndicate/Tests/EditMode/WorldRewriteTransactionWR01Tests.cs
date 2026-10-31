using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Requires genuine native GameObject, shader and mesh behavior. Never counted as offline execution.</summary>
    [Category("NeedsEngine")]
    public sealed class WorldRewriteTransactionWR01Tests
    {
        private GameObject _root, _group, _legacy, _candidate;
        private Mesh _mesh;
        private Material _material;
        private HeartPresentationFR01 _heart;

        [SetUp] public void SetUp()
        {
            Shader shader = Shader.Find(RenderPath.IsURP ? "Universal Render Pipeline/Lit" : "Standard");
            Assert.IsNotNull(shader, "Run with a supported project renderer and shader preparation.");
            _material = new Material(shader);
            _mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
            _mesh.RecalculateNormals(); _mesh.RecalculateBounds();
            _root = new GameObject("WR01 native transaction fixture");
            _group = new GameObject("Original streamed parent"); _group.transform.SetParent(_root.transform, false);
            _legacy = Visual("Original", _group.transform); _candidate = Visual("Candidate", _group.transform); _candidate.SetActive(false);
            _heart = new HeartPresentationFR01(null, _root.transform); _heart.State(false, false);
        }

        private GameObject Visual(string name, Transform parent)
        {
            GameObject node = new GameObject(name); node.transform.SetParent(parent, false);
            node.AddComponent<MeshFilter>().sharedMesh = _mesh;
            node.AddComponent<MeshRenderer>().sharedMaterial = _material;
            return node;
        }

        private bool Register(out string reason)
        { return _heart.RegisterRewriteVisual("native-fixture", "test", _legacy, _candidate, "Exact shared test mesh, no physical ownership.", out reason); }

        [TearDown] public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_mesh != null) Object.DestroyImmediate(_mesh);
            if (_material != null) Object.DestroyImmediate(_material);
        }

        [Test] public void StreamedOutParentArmsWithoutPermanentRefusalAndLaterBecomesObserved()
        {
            _group.SetActive(false); string reason;
            Assert.True(Register(out reason), reason); _heart.BeginFantasyPreviewV1();
            Assert.True(_candidate.activeSelf); Assert.False(_candidate.activeInHierarchy); Assert.False(_legacy.activeSelf);
            FantasyProofSnapshotV1 hidden = new FantasyProofSnapshotV1(); _heart.FillRewriteProofWR01(hidden, null);
            Assert.AreEqual(1, hidden.worldRewrite.admitted); Assert.AreEqual(0, hidden.worldRewrite.active); Assert.AreEqual(0, hidden.worldRewrite.refused);
            _group.SetActive(true);
            FantasyProofSnapshotV1 visible = new FantasyProofSnapshotV1(); _heart.FillRewriteProofWR01(visible, null);
            Assert.AreEqual(1, visible.worldRewrite.active);
            Assert.AreEqual(0, hidden.worldRewrite.active, "Earlier native receipts must remain immutable.");
            Assert.False(hidden.worldRewrite.records[0].active, "Earlier per-replacement observations must not alias the live record.");
            _heart.EndFantasyPreviewV1(); Assert.True(_legacy.activeSelf); Assert.False(_candidate.activeSelf);
        }

        [Test] public void ExistingPhysicalOwnerOutsideVisualGroupsNeverSwitches()
        {
            BoxCollider original = _root.AddComponent<BoxCollider>(); string reason;
            Assert.True(Register(out reason), reason); _heart.BeginFantasyPreviewV1();
            Assert.True(original.enabled); Assert.True(original.gameObject.activeInHierarchy);
            _heart.BeginProofComparisonV1(true); Assert.True(original.enabled); Assert.True(_legacy.activeSelf);
            _heart.EndFantasyPreviewV1(); Assert.True(original.enabled); Assert.True(_legacy.activeSelf);
        }

        [Test] public void ColliderInCandidateRefusesBeforeOriginalIsSuppressed()
        {
            _candidate.AddComponent<BoxCollider>(); string reason;
            Assert.False(Register(out reason)); Assert.True(_legacy.activeSelf); Assert.False(_candidate.activeSelf);
        }

        [Test] public void MissingRequiredMaterialRefusesBeforeOriginalIsSuppressed()
        {
            _candidate.GetComponent<MeshRenderer>().sharedMaterial = null; string reason;
            Assert.False(Register(out reason)); Assert.True(_legacy.activeSelf); Assert.False(_candidate.activeSelf);
        }

        [Test] public void DuplicateIdentityCannotReplaceAnEarlierValidTransaction()
        {
            string reason; Assert.True(Register(out reason), reason);
            GameObject other = Visual("Duplicate candidate", _group.transform); other.SetActive(false);
            Assert.False(_heart.RegisterRewriteVisual("native-fixture", "test", _legacy, other, "Duplicate", out reason));
            _heart.BeginFantasyPreviewV1(); Assert.True(_candidate.activeSelf); Assert.False(other.activeSelf);
        }

        [Test] public void DestroyedCandidateKeepsOriginalDuringActivation()
        {
            string reason; Assert.True(Register(out reason), reason); Object.DestroyImmediate(_candidate); _candidate = null;
            _heart.BeginFantasyPreviewV1(); Assert.True(_legacy.activeSelf);
            FantasyProofSnapshotV1 proof = new FantasyProofSnapshotV1(); _heart.FillRewriteProofWR01(proof, null);
            Assert.AreEqual(0, proof.worldRewrite.active); Assert.AreEqual(1, proof.worldRewrite.refused);
        }
    }
}
