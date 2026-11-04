#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class HeartHouseFallbackWR02FTests
    {
        private static bool Switch(GameObject art, GameObject original, bool active, out string reason)
        {
            MethodInfo method = typeof(HeartPresentationFR01).GetMethod("TryShowHouseFR01", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            object[] args = { art, original, active, "" };
            bool result = (bool)method.Invoke(null, args);
            reason = (string)args[3]; return result;
        }

        [Test]
        public void MissingHouseMeshRestoresOnlyItsOriginalAndPreviewOffKeepsIt()
        {
            GameObject art = new GameObject("candidate"), original = new GameObject("original");
            try
            {
                original.SetActive(false); string reason;
                Assert.That(Switch(art, original, true, out reason), Is.False);
                Assert.That(reason, Does.Contain("no mesh"));
                Assert.That(art.activeSelf, Is.False); Assert.That(original.activeSelf, Is.True);
                Assert.That(Switch(art, original, false, out reason), Is.True);
                Assert.That(art.activeSelf, Is.False); Assert.That(original.activeSelf, Is.True);
            }
            finally { Object.DestroyImmediate(art); Object.DestroyImmediate(original); }
        }

        [Test]
        public void MissingOriginalCannotActivateCandidate()
        {
            GameObject art = new GameObject("candidate");
            try
            {
                string reason;
                Assert.That(Switch(art, null, true, out reason), Is.False);
                Assert.That(reason, Does.Contain("Original building")); Assert.That(art.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(art); }
        }

        [Test]
        public void LostNormalMapRestoresOriginalAfterValidActivation()
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null || !shader.isSupported) Assert.Ignore("Supported Standard shader is required for this native material test.");
            GameObject art = new GameObject("candidate"), original = new GameObject("original");
            Mesh mesh = new Mesh(); Material material = new Material(shader); Texture2D texture = new Texture2D(2, 2);
            try
            {
                mesh.vertices = new[] { Vector3.zero, Vector3.right, Vector3.up };
                mesh.triangles = new[] { 0, 1, 2 };
                art.AddComponent<MeshFilter>().sharedMesh = mesh;
                art.AddComponent<MeshRenderer>().sharedMaterial = material;
                material.mainTexture = texture; material.SetTexture("_BumpMap", texture); material.SetTexture("_MetallicGlossMap", texture);
                art.SetActive(false); string reason;
                Assert.That(Switch(art, original, true, out reason), Is.True, reason);
                Assert.That(art.activeSelf, Is.True); Assert.That(original.activeSelf, Is.False);
                material.SetTexture("_BumpMap", null);
                Assert.That(Switch(art, original, true, out reason), Is.False);
                Assert.That(reason, Does.Contain("normal map"));
                Assert.That(art.activeSelf, Is.False); Assert.That(original.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(art); Object.DestroyImmediate(original); Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(material); Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
