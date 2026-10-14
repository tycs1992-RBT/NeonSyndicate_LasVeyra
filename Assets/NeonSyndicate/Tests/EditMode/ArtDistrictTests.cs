using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Integration gates for art-only replacement. Run in Unity; offline syntax checks are not these tests.</summary>
    public sealed class ArtDistrictTests
    {
        [Test]
        public void DefaultCityContainsASmallNonEmptyDistrict()
        {
            int total = 0, selected = 0;
            CityLayout city = TestData.City;
            foreach (Block block in city.Blocks)
                foreach (Lot lot in block.Lots)
                    foreach (Building building in lot.Buildings)
                    {
                        total++;
                        if (DistrictArchitecture.Contains(building)) selected++;
                    }
            Assert.That(selected, Is.GreaterThanOrEqualTo(2));
            Assert.That(selected, Is.LessThanOrEqualTo(40), "The art pass must remain one bounded street.");
            Assert.That(selected, Is.LessThan(total / 10));
            foreach (Lot lot in city.DesertLots)
                foreach (Building building in lot.Buildings)
                    Assert.That(DistrictArchitecture.Contains(building), Is.False, "Desert/story sites are excluded.");
        }

        [Test]
        public void HelipadsAndOutsideBuildingsAreNeverReplaced()
        {
            Building building = new Building { Footprint = new RectF(80f, 15f, 110f, 35f), Height = 20f };
            Assert.That(DistrictArchitecture.Contains(building), Is.True);
            building.Roof = RoofType.Helipad;
            Assert.That(DistrictArchitecture.Contains(building), Is.False);
            building.Roof = RoofType.Flat;
            building.Footprint = new RectF(445f, 1445f, 495f, 1495f);
            Assert.That(DistrictArchitecture.Contains(building), Is.False, "Crystal Manor retains story geometry.");
        }

        [Test]
        public void AllImportedModulesHaveUsableGeometryAndNoPhysics()
        {
            GameObject[] models = Resources.LoadAll<GameObject>("ArtDistrict/Models");
            Assert.That(models.Length, Is.GreaterThanOrEqualTo(23));
            foreach (GameObject model in models)
            {
                Assert.That(model.GetComponentsInChildren<Collider>(true).Length, Is.Zero, model.name);
                MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
                Assert.That(filters.Length, Is.GreaterThan(0), model.name);
                foreach (MeshFilter filter in filters)
                {
                    Mesh mesh = filter.sharedMesh;
                    Assert.That(mesh, Is.Not.Null, model.name);
                    Assert.That(mesh.isReadable, Is.True, model.name + " must support runtime combining in players");
                    Assert.That(mesh.vertexCount, Is.GreaterThan(0), model.name);
                    Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount), model.name);
                    Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount), model.name);
                    Assert.That(mesh.tangents.Length, Is.EqualTo(mesh.vertexCount), model.name + " normal-map tangents");
                    Assert.That(mesh.bounds.size.sqrMagnitude, Is.GreaterThan(0f), model.name);
                }
            }
        }

        [Test]
        public void SurfaceLibraryHasSharedMaterialsAndNormalMaps()
        {
            foreach (string key in new[] { "sandstone", "basalt", "bronze", "ceramic", "glass", "foliage", "bark", "fabric" })
            {
                Material a = DistrictMaterials.Get(key);
                Assert.That(a, Is.SameAs(DistrictMaterials.Get(key)), key + " material sharing");
                Assert.That(a.enableInstancing, Is.True, key);
                Assert.That(a.GetTexture("_BumpMap"), Is.Not.Null, key + " normal map");
                Assert.That(a.GetTexture("_MetallicGlossMap"), Is.Not.Null, key + " packed surface data");
                Assert.That(a.renderQueue, Is.EqualTo(2000), key + " opaque to prevent first-person sorting artifacts");
            }
        }
    }
}
