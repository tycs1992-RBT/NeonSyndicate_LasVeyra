using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Coverage and geometric contracts, not a substitute for the actual Free Story playtest.</summary>
    public sealed class CrystalCity02ETests
    {
        [Test]
        public void NormalBuildingsAreCoveredAtEveryCityPositionAndSeed()
        {
            Lot lot = new Lot();
            float[] positions = { -1400f,-800f,-300f,0f,300f,800f,1400f };
            foreach (float x in positions)
                foreach (float z in positions)
                    for (int seed = 0; seed < 8; seed++)
                    {
                        Building b = new Building { Footprint = new RectF(x,z,x+20f,z+24f), Height=32f, Seed=seed };
                        Assert.That(CrystalCity02ECoverage.CanTheme(b,lot), Is.True);
                    }
        }

        [Test]
        public void HelipadWallsCanChangeButLandingSurfaceCannotReceiveRoofOrnament()
        {
            Building b = new Building { Footprint = new RectF(0f,0f,30f,30f), Height=40f, Roof=RoofType.Helipad };
            Assert.That(CrystalCity02ECoverage.CanTheme(b,new Lot()), Is.True);
            Assert.That(CrystalCity02ECoverage.AllowRoofOrnament(b,b.Footprint), Is.False);
        }

        [Test]
        public void CustomSitesAndBadDimensionsAreNotClaimedAsCovered()
        {
            Building b = new Building { Footprint = new RectF(0f,0f,20f,20f), Height=18f };
            Assert.That(CrystalCity02ECoverage.CanTheme(b,new Lot { CustomBuilt=true }), Is.False);
            Assert.That(CrystalCity02ECoverage.CanTheme(null,new Lot()), Is.False);
            Assert.That(CrystalCity02ECoverage.CanTheme(b,null), Is.False);
            b.Height=float.NaN;
            Assert.That(CrystalCity02ECoverage.CanTheme(b,new Lot()), Is.False);
            b.Height=1f; b.Footprint=new RectF(0f,0f,1.5f,1.5f);
            Assert.That(CrystalCity02ECoverage.CanTheme(b,new Lot()), Is.False);
        }

        [Test]
        public void FacadeCostHasHardPerFaceLimits()
        {
            foreach (CrystalCityStyle style in new[] {CrystalCityStyle.AmethystWard,CrystalCityStyle.DawnstoneCourt,CrystalCityStyle.VerdantReach})
                for (int width=3;width<200;width++)
                {
                    Assert.That(CrystalCity02ECoverage.Bays(width,style), Is.InRange(1,4));
                    Assert.That(CrystalCity02ECoverage.Registers(width), Is.InRange(1,2));
                }
            Assert.That(CrystalCity02ECoverage.Bays(float.NaN,CrystalCityStyle.AmethystWard), Is.EqualTo(0));
            Assert.That(CrystalCity02ECoverage.Registers(float.PositiveInfinity), Is.EqualTo(0));
        }

        [Test]
        public void GeneratedLayoutDoesNotLeaveOrdinaryBuildingsBehindADetailBudget()
        {
            CityLayout city=new CityLayout(61724);
            city.Generate();
            int counted=0;
            foreach (Block block in city.Blocks)
                foreach (Lot lot in block.Lots)
                    foreach (Building b in lot.Buildings)
                    {
                        if (lot.CustomBuilt) continue;
                        Assert.That(CrystalCity02ECoverage.CanTheme(b,lot), Is.True, b.Footprint.ToString());
                        counted++;
                    }
            foreach (Lot lot in city.DesertLots)
                foreach (Building b in lot.Buildings)
                    if (!lot.CustomBuilt)
                    {
                        Assert.That(CrystalCity02ECoverage.CanTheme(b,lot), Is.True, b.Footprint.ToString());
                        counted++;
                    }
            Assert.That(counted, Is.GreaterThan(100));
        }

        [Test]
        public void StampedGeometryIsFiniteAndUsesSharedLowDetailSource()
        {
            MeshBuilder output=new MeshBuilder(1);
            CrystalCity02EAssets.Append(output,"leaves",new Vector3(8f,0f,14f),Quaternion.Euler(0f,33f,0f),new Vector3(.8f,1.2f,.8f));
            Assert.That(output.VertexCount, Is.GreaterThan(0));
            foreach (Vector3 p in output.Vertices)
                Assert.That(!float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z), Is.True);
            foreach (Vector3 n in output.Normals) Assert.That(n.sqrMagnitude, Is.InRange(.98f,1.02f));
            int before=output.VertexCount;
            CrystalCity02EAssets.Append(output,"leaves",Vector3.zero,Quaternion.identity,new Vector3(-1f,1f,1f));
            Assert.That(output.VertexCount, Is.EqualTo(before));
        }

        [TearDown]
        public void Cleanup()
        {
            CrystalCity02EAssets.ResetStatics();
            CrystalCity02DGeometry.ResetStatics();
        }
    }
}
