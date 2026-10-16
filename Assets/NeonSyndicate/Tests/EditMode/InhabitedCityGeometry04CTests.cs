using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Native resource construction and physical aperture checks. Require the pinned Unity Editor.</summary>
    public sealed class InhabitedCityGeometry04CTests
    {
        private GameObject _root;
        private List<VenueBinding04C> _bindings;

        [SetUp]
        public void Setup()
        {
            TextAsset catalog = Resources.Load<TextAsset>("InhabitedCity04C/venues");
            Assert.That(catalog, Is.Not.Null);
            CityLayout city = new CityLayout(61724); city.Generate();
            _bindings = InhabitedCityBindings04C.Select(city, JsonUtility.FromJson<VenueCatalog04C>(catalog.text));
            _root = new GameObject("04C native geometry test owner");
        }

        [TearDown]
        public void Cleanup()
        {
            if (_root != null) UnityEngine.Object.DestroyImmediate(_root);
            ArtInhabitedCity04C.ResetStatics();
        }

        [TestCase("lofts"), TestCase("clinic"), TestCase("forge"), TestCase("inn")]
        [TestCase("archive"), TestCase("observatory"), TestCase("garage"), TestCase("depot")]
        [Category("NeedsEngine")]
        public void ActualResourcesBuildVisibleCollisionAndAnOpenDoor(string id)
        {
            VenueBinding04C binding = _bindings.Find(venue => venue.Definition.Id == id);
            Assert.That(binding.Building, Is.Not.Null, id);
            VenueView04C view = ArtInhabitedCity04C.Build(binding, _root.transform);
            binding.View = view;
            Assert.That(view, Is.Not.Null); Assert.That(view.ColliderCount, Is.GreaterThan(0));
            Assert.That(view.NearTriangles, Is.GreaterThan(0)); Assert.That(view.FarTriangles, Is.GreaterThan(0));
            Assert.That(view.Contains(view.WorldPoint("service")), Is.True, "Service volume: " + id);
            Physics.SyncTransforms();
            foreach (float z in new[] { -.8f, .15f, .9f })
            {
                Vector3 feet = view.transform.TransformPoint(new Vector3(0f, .012f, z));
                bool blocked = Physics.CheckCapsule(feet + Vector3.up * .5f, feet + Vector3.up * 1.75f,
                    .4f, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                Assert.That(blocked, Is.False, "Actual clear doorway at local z=" + z + " in " + id);
            }
        }

        [Test, Category("NeedsEngine")]
        public void MissingLayoutDoesNotLeaveAnActiveReplacementRoot()
        {
            VenueBinding04C binding = _bindings[0];
            binding.Definition.Layout = "missing_layout_for_failure_test";
            int before = _root.transform.childCount;
            Assert.Throws<FileNotFoundException>(() => ArtInhabitedCity04C.Build(binding, _root.transform));
            Assert.That(_root.transform.childCount, Is.EqualTo(before));
        }
    }
}
