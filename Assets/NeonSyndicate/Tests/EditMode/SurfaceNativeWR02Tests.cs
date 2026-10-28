using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    [Category("NeedsEngine")]
    public sealed class SurfaceNativeWR02Tests
    {
        [Test]
        public void SameCallerBatchesAndOtherOwnersRemainSeparate()
        {
            GameObject root = new GameObject("WR02 native test"); Material material = new Material(Shader.Find("Standard"));
            try
            {
                HeartPresentationFR01 heart = new HeartPresentationFR01(new CityLayout(61724), root.transform);
                MeshBuilder a = heart.SurfaceBufferWR02(Vector3.zero, material, 3, "BuildRoadEdge", HeartSurfaceKindWR02.Causeway, false);
                Assert.That(heart.SurfaceBufferWR02(new Vector3(10, 0, 10), material, 3, "BuildRoadEdge", HeartSurfaceKindWR02.Causeway, false), Is.SameAs(a));
                Assert.That(heart.SurfaceBufferWR02(Vector3.zero, material, 3, "BuildGround", HeartSurfaceKindWR02.Ground, false), Is.Not.SameAs(a));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(material); }
        }

        [Test]
        public void IsolationNeverRecreatesOrDisablesAnOriginalCollider()
        {
            GameObject root = new GameObject("WR02 physical owner"); Material material = new Material(Shader.Find("Standard"));
            try
            {
                BoxCollider solid = root.AddComponent<BoxCollider>(); solid.size = new Vector3(6, .3f, 6); int identity = solid.GetInstanceID();
                HeartPresentationFR01 heart = new HeartPresentationFR01(new CityLayout(61724), root.transform);
                MeshBuilder buffer = heart.SurfaceBufferWR02(Vector3.zero, material, 3, "BuildRoadEdge", HeartSurfaceKindWR02.Causeway, false);
                buffer.FlatRect(0, -3, -3, 3, 3, .15f, UVMode.World);
                heart.SealSurfaceSourcesWR02((at, mesh, original) => Assert.Fail("Valid source unexpectedly failed isolation."));
                Assert.That(solid.GetInstanceID(), Is.EqualTo(identity)); Assert.That(solid.enabled, Is.True);
                Assert.That(root.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(1));
                Assert.That(heart.SurfaceBufferWR02(Vector3.zero, material, 3, "BuildRoadEdge", HeartSurfaceKindWR02.Causeway, false), Is.Null);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(material); }
        }

        [Test]
        public void MissingCatalogActivationKeepsOriginalSourceVisible()
        {
            GameObject root = new GameObject("WR02 missing catalog"); Material material = new Material(Shader.Find("Standard"));
            try
            {
                HeartPresentationFR01 heart = new HeartPresentationFR01(new CityLayout(61724), root.transform);
                MeshBuilder buffer = heart.SurfaceBufferWR02(Vector3.zero, material, 3, "BuildRoadEdge", HeartSurfaceKindWR02.Causeway, false); buffer.FlatRect(0, -3, -3, 3, 3, 0, UVMode.World);
                heart.SealSurfaceSourcesWR02((at, mesh, original) => Assert.Fail("Valid source unexpectedly failed isolation."));
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                foreach (string field in new[] { "_fantasyPreviewV1", "_previewHeartV1", "_surfaceBuiltWR02" }) typeof(HeartPresentationFR01).GetField(field, flags).SetValue(heart, true);
                typeof(HeartPresentationFR01).GetField("_suspended", flags).SetValue(heart, false);
                typeof(HeartPresentationFR01).GetMethod("ApplySurfaceWR02", flags).Invoke(heart, null);
                Assert.That(root.GetComponentInChildren<MeshRenderer>().gameObject.activeInHierarchy, Is.True);
                Assert.That(heart.SurfaceStatusWR02, Does.Contain("1 refused"));
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(material); }
        }
    }
}
