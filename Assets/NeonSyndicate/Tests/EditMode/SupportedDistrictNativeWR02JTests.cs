using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    [Category("NeedsEngine")]
    public sealed class SupportedDistrictNativeWR02JTests
    {
        [TestCase("SupportedDwellingWR02J", false)] [TestCase("SupportedArchiveWR02J", false)]
        [TestCase("SupportedDwellingWR02J", true)] [TestCase("SupportedArchiveWR02J", true)]
        public void ExistingTransactionRestoresOriginalAndNeverOwnsCollision(string family, bool loseMap)
        {
            GameObject root = null; Mesh mesh = null; Material material = null;
            try
            {
                root = new GameObject("Supported district native fixture");
                BoxCollider physical = root.AddComponent<BoxCollider>(); physical.size = new Vector3(3, 1, 3);
                mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
                mesh.RecalculateBounds(); mesh.RecalculateNormals();
                material = new Material(Shader.Find(RenderPath.IsURP ? "Universal Render Pipeline/Lit" : "Standard"));
                material.mainTexture = Texture2D.whiteTexture; material.SetTexture("_BumpMap", Texture2D.normalTexture);
                material.SetTexture("_MetallicGlossMap", Texture2D.whiteTexture);
                GameObject original = Visual(root, mesh, material, "Original"), candidate = Visual(root, mesh, material, "Candidate");
                candidate.SetActive(false);
                HeartPresentationFR01 heart = new HeartPresentationFR01(null, root.transform); heart.State(false, false);
                string reason;
                Assert.True(heart.RegisterRewriteVisual("native-wr02j", family, original, candidate, "Exact source fixture", out reason), reason);
                heart.BeginFantasyPreviewV1(); Assert.True(candidate.activeSelf); Assert.False(original.activeSelf);
                root.SetActive(false); root.SetActive(true); Assert.True(candidate.activeInHierarchy);
                if (loseMap)
                {
                    material.SetTexture("_BumpMap", null);
                    heart.FillRewriteProofWR01(new FantasyProofSnapshotV1(), null);
                    Assert.False(candidate.activeSelf); Assert.True(original.activeSelf);
                }
                heart.EndFantasyPreviewV1(); Assert.False(candidate.activeSelf); Assert.True(original.activeSelf);
                Assert.True(physical.enabled); Assert.AreEqual(new Vector3(3, 1, 3), physical.size);
                Assert.AreEqual(1, root.GetComponentsInChildren<Collider>(true).Length);
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                if (mesh != null) Object.DestroyImmediate(mesh);
                if (material != null) Object.DestroyImmediate(material);
            }
        }

        private static GameObject Visual(GameObject root, Mesh mesh, Material material, string name)
        {
            GameObject child = new GameObject(name); child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh; child.AddComponent<MeshRenderer>().sharedMaterial = material;
            return child;
        }
    }
}
