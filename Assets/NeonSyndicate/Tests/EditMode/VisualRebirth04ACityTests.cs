using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>Pure parcel, variation and budget contracts. These do not certify Unity rendering or performance.</summary>
    public sealed class VisualRebirth04ACityTests
    {
        [TestCase(-181f, 0f, 2)]
        [TestCase(-180f, 0f, 0)]
        [TestCase(180f, 0f, 0)]
        [TestCase(181f, 0f, 1)]
        [TestCase(-500f, -381f, 3)]
        [TestCase(500f, -380f, 1)]
        public void RegionalBoundariesAreStable(float x, float z, int expected)
        {
            Assert.That(VisualRebirth04ACityPlan.StyleAt(x,z),Is.EqualTo(expected));
        }

        [Test]
        public void EveryFacadeArchetypeHasFiveDeterministicVariations()
        {
            foreach (FacadeStyle style in Enum.GetValues(typeof(FacadeStyle)))
            {
                HashSet<int> variants = new HashSet<int>();
                for (int seed = -100; seed < 200; seed++)
                {
                    int value = VisualRebirth04ACityPlan.Variant(seed,style);
                    Assert.That(value,Is.InRange(0,4));
                    Assert.That(VisualRebirth04ACityPlan.Variant(seed,style),Is.EqualTo(value));
                    variants.Add(value);
                }
                Assert.That(variants.Count,Is.EqualTo(5),style.ToString());
                Assert.That(VisualRebirth04ACityPlan.Variant(int.MinValue,style),Is.InRange(0,4));
            }
        }

        [Test]
        public void OrdinaryGeometryHasExplicitWidthAndHeightBudgets()
        {
            foreach (FacadeStyle style in Enum.GetValues(typeof(FacadeStyle)))
            {
                Assert.That(VisualRebirth04ACityPlan.Bays(10000f,style,false),Is.LessThanOrEqualTo(3));
                Assert.That(VisualRebirth04ACityPlan.Bays(float.NaN,style,false),Is.Zero);
                Assert.That(VisualRebirth04ACityPlan.Bays(float.PositiveInfinity,style,false),Is.Zero);
            }
            Assert.That(VisualRebirth04ACityPlan.Registers(10000f,false),Is.EqualTo(2));
            Assert.That(VisualRebirth04ACityPlan.Registers(float.NaN,false),Is.Zero);
            Assert.That(VisualRebirth04ACityPlan.Registers(1f,false),Is.Zero);
        }

        [Test]
        public void EveryEligibleArchetypeIsThemedRegardlessOfRoofAndLotType()
        {
            Lot lot = new Lot { Use = LotUse.Building };
            foreach (FacadeStyle style in Enum.GetValues(typeof(FacadeStyle)))
                foreach (RoofType roof in Enum.GetValues(typeof(RoofType)))
                {
                    Building building = new Building { Footprint = new RectF(0f,0f,16f,18f), Height = 40f, Style = style, Roof = roof };
                    Assert.That(VisualRebirth04ACityPlan.CanTheme(building,lot),Is.True,style+"/"+roof);
                }
            Building protectedBuilding = new Building { Footprint = new RectF(0f,0f,16f,18f), Height = 40f };
            lot.CustomBuilt = true;
            Assert.That(VisualRebirth04ACityPlan.CanTheme(protectedBuilding,lot),Is.False);
            Assert.That(VisualRebirth04ACityPlan.Exclusion(protectedBuilding,lot),Is.EqualTo("custom-authored-lot"));
        }

        [Test]
        public void CrownEnvelopeLeavesCentralLandingClearAndStaysInsideParcel()
        {
            for (int span = 10; span <= 160; span++)
            {
                RectF roof = new RectF(-span*.5f,-span*.5f,span*.5f,span*.5f);
                Building building = new Building { Height = span*2f, Footprint = roof, Roof = RoofType.FlatWithProps };
                float radius = VisualRebirth04ACityPlan.PerimeterRadius(building,roof);
                float center = span*.5f-radius-.25f;
                // Full round base bounds every rotated square roof module.
                Assert.That(center+radius,Is.LessThanOrEqualTo(span*.5f));
                Assert.That((center-radius)*2f,Is.GreaterThanOrEqualTo(VisualRebirth04ACityPlan.MinimumClearRoofSpan));
                for (int style = 0; style < 4; style++)
                    Assert.That(VisualRebirth04ACityPlan.CrownHeight(building,radius,style),Is.InRange(0f,12f));
            }
        }

        [Test]
        public void HelipadsReceiveFacadeTreatmentButNoRoofCrowns()
        {
            Building building = new Building { Height = 60f, Footprint = new RectF(0f,0f,30f,30f), Roof = RoofType.Helipad };
            Assert.That(VisualRebirth04ACityPlan.CanTheme(building,new Lot()),Is.True);
            Assert.That(VisualRebirth04ACityPlan.PerimeterRadius(building,building.Footprint),Is.Zero);
        }
    }
}
