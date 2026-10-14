using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Pure selection invariants. These tests do not claim that the district renders correctly.</summary>
    public sealed class ArtDistrictStarterSelectionTests
    {
        private CityLayout _previous;

        [SetUp]
        public void SetUp()
        {
            _previous = Game.Layout;
            Game.Layout = new CityLayout(123);
            DistrictArchitecture.ResetStatics();
        }

        [TearDown]
        public void TearDown()
        {
            DistrictArchitecture.ResetStatics();
            Game.Layout = _previous;
        }

        private static Building AddBuilding(CityLayout layout, float x, float z, bool custom = false)
        {
            Block block = new Block();
            Lot lot = new Lot { CustomBuilt = custom };
            Building building = new Building { Footprint = new RectF(x, z, x + 12f, z + 12f), Height = 24f };
            lot.Buildings.Add(building);
            block.Lots.Add(lot);
            layout.Blocks.Add(block);
            return building;
        }

        [Test]
        public void StarterSelectionIsCappedAndPreservesFootprints()
        {
            Game.Layout.Pois.Add(new Poi { Name = "Veyra Lofts", Position = new Vector3(500f, 0f, 500f), Facing = Vector3.forward });
            Game.Layout.Pois.Add(new Poi { Name = "Mara Solane Street Clinic", Position = new Vector3(-500f, 0f, -500f), Facing = Vector3.left });
            for (int i = 0; i < 8; i++)
            {
                AddBuilding(Game.Layout, 470f + (i % 4) * 16f, 520f + (i / 4) * 18f);
                AddBuilding(Game.Layout, -540f + (i % 4) * 16f, -520f + (i / 4) * 18f);
            }
            Building distant = AddBuilding(Game.Layout, 1400f, 1400f);
            RectF before = distant.Footprint;
            Assert.That(DistrictArchitecture.StarterBuildingCount, Is.EqualTo(6));
            Assert.That(DistrictArchitecture.Contains(distant), Is.False);
            Assert.That(distant.Footprint.X0, Is.EqualTo(before.X0));
            Assert.That(distant.Footprint.Z1, Is.EqualTo(before.Z1));
            Assert.That(distant.Height, Is.EqualTo(24f));
        }

        [Test]
        public void AnchorsMatchExistingSpawnMathAndAreCached()
        {
            Game.Layout.Pois.Add(new Poi { Name = "Veyra Lofts", Position = new Vector3(500f, 0.15f, 500f), Facing = Vector3.forward });
            Vector3[] anchors = DistrictArchitecture.StarterAnchors();
            Assert.That(anchors.Length, Is.EqualTo(1));
            Assert.That(anchors[0], Is.EqualTo(new Vector3(500f, 0.15f, 503.5f)));
            Assert.That(DistrictArchitecture.StarterAnchors(), Is.SameAs(anchors));
        }

        [Test]
        public void OriginalStreetRemainsAndSpecialGeometryIsExcluded()
        {
            Game.Layout.Pois.Add(new Poi { Name = "Veyra Lofts", Position = new Vector3(500f, 0f, 500f), Facing = Vector3.forward });
            Building helipad = AddBuilding(Game.Layout, 480f, 510f);
            helipad.Roof = RoofType.Helipad;
            Building custom = AddBuilding(Game.Layout, 500f, 510f, true);
            Building street = AddBuilding(Game.Layout, 40f, 15f);
            Assert.That(DistrictArchitecture.Contains(street), Is.True);
            Assert.That(DistrictArchitecture.Contains(helipad), Is.False);
            Assert.That(DistrictArchitecture.Contains(custom), Is.False);
        }

        [Test]
        public void MissingLayoutDoesNotPermanentlyCacheEmptySelection()
        {
            CityLayout later = Game.Layout;
            Game.Layout = null;
            Assert.That(DistrictArchitecture.StarterAnchors().Length, Is.EqualTo(0));
            later.Pois.Add(new Poi { Name = "Veyra Lofts", Position = new Vector3(500f, 0f, 500f), Facing = Vector3.forward });
            Game.Layout = later;
            Assert.That(DistrictArchitecture.StarterAnchors().Length, Is.EqualTo(1));
            DistrictArchitecture.ResetStatics();
            Game.Layout = null;
            Assert.That(DistrictArchitecture.StarterBuildingCount, Is.EqualTo(0));
        }
    }
}
