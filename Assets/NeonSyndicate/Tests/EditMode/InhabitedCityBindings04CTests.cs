using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Actual city layout selection is deterministic and cannot mutate existing parcels.</summary>
    public sealed class InhabitedCityBindings04CTests
    {
        public sealed class CatalogInput { public string Version; public List<VenueDefinition04C> Venues = new List<VenueDefinition04C>(); }
        public static VenueCatalog04C ReadCatalog()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "Assets/NeonSyndicate/Resources/InhabitedCity04C/venues.json");
            List<string> errors = new List<string>();
            CatalogInput input = QuestJson.Read<CatalogInput>(File.ReadAllText(path), path, errors, null);
            Assert.That(errors, Is.Empty);
            return new VenueCatalog04C { Version = input.Version, Venues = input.Venues.ToArray() };
        }
        [TestCase(61724)]
        [TestCase(1)]
        [TestCase(2016)]
        [TestCase(77231)]
        [TestCase(92017)]
        public void SelectedParcelsAreUniqueStableFiniteAndUnchanged(int seed)
        {
            CityLayout layout = new CityLayout(seed); layout.Generate(); VenueCatalog04C catalog = ReadCatalog();
            Dictionary<Building, string> before = new Dictionary<Building, string>();
            foreach (Block block in layout.Blocks) foreach (Lot lot in block.Lots) foreach (Building building in lot.Buildings)
                before[building] = building.Footprint.ToString() + "/" + building.Height + "/" + building.Seed + "/" + building.Style + "/" + building.Roof;
            List<VenueBinding04C> first = InhabitedCityBindings04C.Select(layout, catalog), again = InhabitedCityBindings04C.Select(layout, catalog);
            Assert.That(first, Has.Count.EqualTo(8)); HashSet<Building> used = new HashSet<Building>();
            for (int i = 0; i < first.Count; i++)
            {
                VenueBinding04C binding = first[i]; Assert.That(binding.Building, Is.SameAs(again[i].Building), binding.Definition.Id);
                if (binding.Building == null) { Assert.That(binding.Status.StartsWith("Skipped:", StringComparison.Ordinal), Is.True); continue; }
                Assert.That(used.Add(binding.Building), Is.True); Assert.That(InhabitedCityBindings04C.Eligible(binding.Building), Is.True);
                Assert.That(MathUtil.IsFinite(binding.Origin) && MathUtil.IsFinite(binding.Outside) && MathUtil.IsFinite(binding.Outward), Is.True);
                Assert.That(binding.Width, Is.GreaterThanOrEqualTo(17f)); Assert.That(binding.Depth, Is.GreaterThanOrEqualTo(19f));
                Assert.That(binding.Outward.magnitude, Is.EqualTo(1f).Within(.0001f));
                Assert.That(binding.Available, Is.False, "Source binding alone is not an installed room.");
                if (!string.IsNullOrEmpty(binding.Definition.PoiName)) Assert.That(binding.Lot.PoiName, Is.EqualTo(binding.Definition.PoiName));
                else Assert.That(binding.Lot.Poi, Is.EqualTo(PoiType.None));
            }
            foreach (KeyValuePair<Building, string> entry in before)
                Assert.That(entry.Key.Footprint.ToString() + "/" + entry.Key.Height + "/" + entry.Key.Seed + "/" + entry.Key.Style + "/" + entry.Key.Roof, Is.EqualTo(entry.Value));
        }
        [Test]
        public void ShippedIdsAreStableAndCanonicalServicesAreNotDuplicated()
        {
            VenueCatalog04C catalog = ReadCatalog(); HashSet<string> ids = new HashSet<string>(), pois = new HashSet<string>();
            foreach (VenueDefinition04C venue in catalog.Venues)
            { Assert.That(ids.Add(venue.Id), Is.True); if (!string.IsNullOrEmpty(venue.PoiName)) Assert.That(pois.Add(venue.PoiName), Is.True); }
            Assert.That(ids, Is.EquivalentTo(new[] { "lofts", "clinic", "forge", "inn", "garage", "archive", "observatory", "depot" }));
            Assert.That(pois, Has.Count.EqualTo(5));
        }
        [TestCase("version")]
        [TestCase("null_entries")]
        [TestCase("duplicate")]
        [TestCase("null_definition")]
        [TestCase("unknown_kind")]
        [TestCase("unknown_layout")]
        [TestCase("nan_distance")]
        [TestCase("negative_distance")]
        [TestCase("nan_offset")]
        [TestCase("infinite_facing")]
        public void InvalidCatalogIsRejectedBeforeSelection(string mutation)
        {
            VenueCatalog04C catalog = ReadCatalog(); VenueDefinition04C selected = catalog.Venues[5];
            if (mutation == "version") catalog.Version = "future";
            else if (mutation == "null_entries") catalog.Venues = null;
            else if (mutation == "duplicate") selected.Id = catalog.Venues[0].Id;
            else if (mutation == "null_definition") catalog.Venues[5] = null;
            else if (mutation == "unknown_kind") selected.Kind = "invented";
            else if (mutation == "unknown_layout") selected.Layout = "missing_room";
            else if (mutation == "nan_distance") selected.MaxDistance = float.NaN;
            else if (mutation == "negative_distance") selected.MaxDistance = -100f;
            else if (mutation == "nan_offset") selected.OffsetX = float.NaN;
            else if (mutation == "infinite_facing") selected.FacingZ = float.PositiveInfinity;
            Assert.Throws<ArgumentException>(() => InhabitedCityBindings04C.Select(new CityLayout(61724), catalog));
        }
        [TestCase(1f, 0f, 30f, 20f, 270f)]
        [TestCase(-1f, 0f, 10f, 20f, 90f)]
        [TestCase(0f, 1f, 20f, 40f, 180f)]
        [TestCase(0f, -1f, 20f, 0f, 0f)]
        public void DoorMatchesVisibleFootprintEdge(float x, float z, float expectedX, float expectedZ, float expectedYaw)
        {
            Vector3 origin; float width, depth, yaw;
            InhabitedCityBindings04C.Door(new RectF(10f, 0f, 30f, 40f), new Vector3(x, 0f, z), out origin, out width, out depth, out yaw);
            Assert.That(origin.x, Is.EqualTo(expectedX)); Assert.That(origin.z, Is.EqualTo(expectedZ)); Assert.That(yaw, Is.EqualTo(expectedYaw));
            Assert.That(origin.y, Is.EqualTo(CityLayout.CurbHeight)); Assert.That(width * depth, Is.EqualTo(800f));
        }
        [Test]
        public void ProtectedRoofAndInstitutionTypesRemainIneligible()
        {
            Building building = new Building { Footprint = new RectF(0f, 0f, 20f, 20f), Height = 12f, Style = FacadeStyle.Apartment, Roof = RoofType.Flat };
            Assert.That(InhabitedCityBindings04C.Eligible(building), Is.True);
            building.Roof = RoofType.Helipad; Assert.That(InhabitedCityBindings04C.Eligible(building), Is.False);
            building.Roof = RoofType.Antenna; Assert.That(InhabitedCityBindings04C.Eligible(building), Is.False);
            building.Roof = RoofType.Flat; building.Style = FacadeStyle.Police; Assert.That(InhabitedCityBindings04C.Eligible(building), Is.False);
            building.Style = FacadeStyle.Casino; Assert.That(InhabitedCityBindings04C.Eligible(building), Is.False);
            Assert.That(InhabitedCityBindings04C.Eligible(null), Is.False);
        }
        [Test]
        public void PreferredSeedCannotBypassDistanceOrProtectedRoof()
        {
            CityLayout city = new CityLayout(61724); Block block = new Block(); Lot lot = new Lot { Use = LotUse.Building, Facing = Vector3.forward };
            city.Blocks.Add(block); block.Lots.Add(lot);
            Building near = new Building { Seed = 42, Footprint = new RectF(0f, 0f, 20f, 24f), Height = 12f, Style = FacadeStyle.Apartment, Roof = RoofType.Flat };
            Building preferred = new Building { Seed = 99, Footprint = new RectF(1000f, 1000f, 1020f, 1024f), Height = 12f, Style = FacadeStyle.Apartment, Roof = RoofType.Flat };
            lot.Buildings.Add(preferred); lot.Buildings.Add(near);
            VenueCatalog04C catalog = new VenueCatalog04C { Version = "04C", Venues = new[] { new VenueDefinition04C
                { Id = "archive", Kind = "archive", Layout = "archive", Title = "Archive", MaxDistance = 40f, FacingZ = 1f, PreferredBuildingSeed = 99 } } };
            Assert.That(InhabitedCityBindings04C.Select(city, catalog)[0].Building, Is.SameAs(near));
            preferred.Footprint = near.Footprint; preferred.Roof = RoofType.Helipad;
            Assert.That(InhabitedCityBindings04C.Select(city, catalog)[0].Building, Is.SameAs(near));
        }
        [Test]
        public void MissingCanonicalPoiProducesExplicitSkipWithoutInventedDuplicate()
        {
            List<VenueBinding04C> bindings = InhabitedCityBindings04C.Select(new CityLayout(61724), ReadCatalog());
            foreach (VenueBinding04C binding in bindings)
            {
                Assert.That(binding.Building, Is.Null); Assert.That(binding.Poi, Is.Null);
                Assert.That(binding.Available, Is.False); Assert.That(binding.Status.StartsWith("Skipped:", StringComparison.Ordinal), Is.True);
            }
        }
    }
}
