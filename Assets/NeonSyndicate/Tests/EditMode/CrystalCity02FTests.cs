using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Editor contracts for imported assets and placement. They are not a visual-quality or frame-rate rating.</summary>
    public sealed class CrystalCity02FTests
    {
        [TearDown]
        public void Cleanup() { CrystalCity02FAssets.ResetStatics(); }

        [Test]
        public void ShippedCatalogContainsNineCompositionsAndFourteenModels()
        {
            CrystalCity02FCatalog catalog = CrystalCity02FAssets.Catalog();
            catalog.Validate();
            Assert.That(catalog.models.Length, Is.EqualTo(14));
            Assert.That(catalog.blueprints.Length, Is.EqualTo(9));
            Assert.That(catalog.Find("arrival_crest").category, Is.EqualTo("threshold"));
            Assert.That(catalog.Find("moonwell_glade").category, Is.EqualTo("garden"));
        }

        [Test]
        public void AllModelsImportWithReadableGeometryUvsAndCheaperLods()
        {
            foreach (string model in CrystalCity02FAssets.Catalog().models)
            {
                int detailed = 0, distant = 0;
                for (int level = 0; level < 2; level++)
                    foreach (MeshFilter filter in CrystalCity02FAssets.Model(model, level == 1))
                    {
                        Mesh mesh = filter.sharedMesh;
                        Assert.That(mesh.isReadable, Is.True, model);
                        Assert.That(mesh.vertexCount, Is.GreaterThan(0), model);
                        Assert.That(mesh.normals.Length, Is.EqualTo(mesh.vertexCount), model);
                        Assert.That(mesh.uv.Length, Is.EqualTo(mesh.vertexCount), model);
                        if (level == 0) detailed += mesh.triangles.Length; else distant += mesh.triangles.Length;
                        foreach (Vector3 vertex in mesh.vertices)
                            Assert.That(CrystalCity02FCatalog.Finite(vertex.x) && CrystalCity02FCatalog.Finite(vertex.y) && CrystalCity02FCatalog.Finite(vertex.z), Is.True, model);
                    }
                Assert.That(distant, Is.LessThan(detailed), model);
            }
        }

        [Test]
        public void ModelsAreSharedUntilReset()
        {
            MeshFilter[] first = CrystalCity02FAssets.Model("pavilion", false);
            Assert.That(CrystalCity02FAssets.Model("pavilion", false), Is.SameAs(first));
            CrystalCity02FAssets.ResetStatics();
            Assert.That(first[0].sharedMesh, Is.Not.Null, "Imported assets must not be destroyed by cache reset.");
            Assert.That(CrystalCity02FAssets.Model("pavilion", false)[0].sharedMesh, Is.SameAs(first[0].sharedMesh));
        }

        [Test]
        public void CatalogRejectsZeroScaleInsteadOfBuildingInvalidMatrices()
        {
            CrystalCity02FCatalog copy = JsonUtility.FromJson<CrystalCity02FCatalog>(JsonUtility.ToJson(CrystalCity02FAssets.Catalog()));
            copy.blueprints[0].nodes[0].scale[0] = 0f;
            Assert.Throws<InvalidOperationException>(delegate { copy.Validate(); });
        }

        [Test]
        public void CatalogRejectsUnknownModelsAndExtremeSpin()
        {
            CrystalCity02FCatalog copy = JsonUtility.FromJson<CrystalCity02FCatalog>(JsonUtility.ToJson(CrystalCity02FAssets.Catalog()));
            copy.blueprints[0].nodes[0].model = "missing";
            Assert.Throws<InvalidOperationException>(delegate { copy.Validate(); });
            copy = JsonUtility.FromJson<CrystalCity02FCatalog>(JsonUtility.ToJson(CrystalCity02FAssets.Catalog()));
            copy.blueprints[0].nodes[0].speed = 800f;
            Assert.Throws<InvalidOperationException>(delegate { copy.Validate(); });
        }

        [Test]
        public void HelipadsAntennasCustomLotsAndInvalidBuildingsAreNotSelected()
        {
            RectF roof;
            Assert.That(CrystalCity02FPlanner.TryTopRoof(null, out roof), Is.False);
            Building b = BuildingAt(0f, 0f, RoofType.Helipad);
            Assert.That(CrystalCity02FPlanner.TryTopRoof(b, out roof), Is.False);
            b.Roof = RoofType.Antenna;
            Assert.That(CrystalCity02FPlanner.TryTopRoof(b, out roof), Is.False);
            b.Roof = RoofType.Flat; b.Height = float.NaN;
            Assert.That(CrystalCity02FPlanner.TryTopRoof(b, out roof), Is.False);
            CityLayout layout = new CityLayout(61724);
            Block block = new Block(); Lot lot = new Lot { CustomBuilt = true };
            lot.Buildings.Add(BuildingAt(0f, 0f, RoofType.Flat)); block.Lots.Add(lot); layout.Blocks.Add(block);
            Assert.That(CrystalCity02FPlanner.SelectRoofs(layout).Count, Is.EqualTo(0));
        }

        [Test]
        public void ThreeStylesAreSelectedAndLayoutIsNotMutated()
        {
            CityLayout layout = new CityLayout(61724);
            Building[] buildings = { BuildingAt(-400f, 0f, RoofType.Flat), BuildingAt(0f, 0f, RoofType.Flat), BuildingAt(400f, 0f, RoofType.Flat) };
            foreach (Building b in buildings)
            {
                Block block = new Block(); Lot lot = new Lot(); lot.Buildings.Add(b); block.Lots.Add(lot); layout.Blocks.Add(block);
            }
            layout.Pois.Add(new Poi { Name = "Veyra Lofts", Position = new Vector3(0f, 0f, -20f), Facing = Vector3.forward });
            List<CrystalCity02FRoofSite> first = CrystalCity02FPlanner.SelectRoofs(layout);
            List<CrystalCity02FRoofSite> second = CrystalCity02FPlanner.SelectRoofs(layout);
            Assert.That(first.Count, Is.EqualTo(3));
            HashSet<Building> selected = new HashSet<Building>();
            for (int i = 0; i < first.Count; i++)
            {
                Assert.That(selected.Add(first[i].Building), Is.True);
                Assert.That(second[i].Building, Is.SameAs(first[i].Building));
                Assert.That(first[i].Position.y, Is.EqualTo(30f + CityLayout.CurbHeight + CrystalCity02FPlanner.RoofClearance).Within(.0001f));
                Assert.That(first[i].Building.Height, Is.EqualTo(30f));
                Assert.That(first[i].Scale * CrystalCity02FPlanner.BaseRadius, Is.LessThan(first[i].Roof.Width * .5f));
            }
            Assert.That(layout.Blocks.Count, Is.EqualTo(3));
        }

        [Test]
        public void TieredRoofScaleRemainsInsideActualTopFootprint()
        {
            for (int seed = -40; seed < 40; seed++)
            {
                Building b = BuildingAt(0f, 0f, RoofType.Flat); b.Seed = seed; b.Tiers = 3;
                RectF top;
                if (!CrystalCity02FPlanner.TryTopRoof(b, out top)) continue;
                float scale = CrystalCity02FPlanner.RoofScale(top);
                Assert.That(scale * CrystalCity02FPlanner.BaseRadius + 1f, Is.LessThan(top.Width * .5f));
                Assert.That(scale * CrystalCity02FPlanner.BaseRadius + 1f, Is.LessThan(top.Depth * .5f));
            }
        }

        [Test]
        public void GardenPocketRejectsPathSegmentsAndObjectives()
        {
            CityLayout layout = new CityLayout(61724); Lot lot = new Lot { Area = new RectF(-50f,-50f,50f,50f) };
            Vector3 p = new Vector3(10f, .15f, 10f);
            Assert.That(CrystalCity02FPlanner.ClearGardenPocket(layout, lot, p, 2.3f), Is.True);
            layout.WalkNodes.Add(new WalkNode { Pos = new Vector3(-20f,0f,10f) });
            layout.WalkNodes.Add(new WalkNode { Pos = new Vector3(40f,0f,10f) });
            layout.WalkLinks.Add(new WalkLink { A = 0, B = 1 });
            Assert.That(CrystalCity02FPlanner.ClearGardenPocket(layout, lot, p, 2.3f), Is.False);
            layout.WalkLinks.Clear(); layout.Pois.Add(new Poi { Position = p });
            Assert.That(CrystalCity02FPlanner.ClearGardenPocket(layout, lot, p, 2.3f), Is.False);
        }

        [Test]
        public void NightAndRotationMathIsBounded()
        {
            Assert.That(CrystalCity02FPlanner.NightLight(0f), Is.EqualTo(0f));
            Assert.That(CrystalCity02FPlanner.NightLight(1f), Is.EqualTo(1f));
            Assert.That(CrystalCity02FPlanner.SpinAngle(100f, -8f), Is.InRange(0f,360f));
            Assert.That(CrystalCity02FPlanner.SpinAngle(3600f, 5f), Is.EqualTo(0f).Within(.01f));
        }

        [Test]
        public void StreetBudgetsIncreaseWithoutRemovingLandmarks()
        {
            Assert.That(CrystalCity02FPlanner.MaximumHeroes, Is.EqualTo(3));
            Assert.That(CrystalCity02FPlanner.StreetBudget(GraphicsPreset.Low), Is.EqualTo(12));
            Assert.That(CrystalCity02FPlanner.StreetBudget(GraphicsPreset.High), Is.EqualTo(42));
        }

        [Test]
        public void LandmarkMapMarkerIsRemovedWithItsOwner()
        {
            int before = Blip.All.Count;
            GameObject owner = new GameObject("02F marker ownership test");
            try
            {
                CrystalCity02FMotion motion = owner.AddComponent<CrystalCity02FMotion>();
                motion.AddMapMarker(owner.transform, "Test landmark", CrystalCityStyle.AmethystWard);
                Assert.That(Blip.All.Count, Is.EqualTo(before + 1));
                Assert.That(Blip.All[Blip.All.Count - 1].ShowOnMinimap, Is.False);
                Assert.That(Blip.All[Blip.All.Count - 1].ShowOnMap, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
            Assert.That(Blip.All.Count, Is.EqualTo(before));
        }

        private static Building BuildingAt(float x, float z, RoofType type)
        {
            return new Building { Footprint = new RectF(x - 18f, z - 18f, x + 18f, z + 18f), Height = 30f, Tiers = 1, Roof = type, Seed = 31 };
        }
    }
}
