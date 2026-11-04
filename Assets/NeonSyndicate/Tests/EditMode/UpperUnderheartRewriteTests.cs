using System.Collections.Generic;
using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    /// <summary>Offline rules retain the original walk network and reject unknown, vehicle, deep and unsafe decorative spans.</summary>
    public sealed class UpperUnderheartRewriteTests
    {
        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        public void OnlyExistingUpperCellsAreEligibleInEveryVariant(int variant)
        {
            UnderheartLayout layout = UnderheartLayout.Create(variant);
            UpperUnderheartRewriteRules rules = new UpperUnderheartRewriteRules();
            List<string> snapshots = new List<string>();
            foreach (UhCell cell in layout.Cells) snapshots.Add(cell.ToString());
            int selected = 0;
            foreach (UhCell cell in layout.Cells)
            {
                if (!rules.Eligible(cell)) continue;
                selected++;
                Assert.That(cell.Region == UnderheartIds.ServiceLevel || cell.Region == UnderheartIds.LanternRefuge, Is.True);
                Assert.That(cell.Vehicle, Is.False);
                float spring, rise;
                if (rules.RibProfile(cell, out spring, out rise))
                {
                    Assert.That(spring, Is.GreaterThanOrEqualTo(2.8f));
                    Assert.That(spring + rise + .13f, Is.LessThan(cell.Height));
                    Assert.That(cell.Sloped || cell.Rock, Is.False);
                }
            }
            Assert.That(selected, Is.EqualTo(21));
            for (int i = 0; i < layout.Cells.Count; i++) Assert.That(layout.Cells[i].ToString(), Is.EqualTo(snapshots[i]));
            Assert.That(UnderheartRules.Validate(layout), Is.Empty);
        }

        [TestCase("Underheart Concrete", "Masonry")]
        [TestCase("Underheart Service Floor", "Masonry")]
        [TestCase("Underheart Transit Tile", "CarvedTrim")]
        [TestCase("Underheart Hazard Stripes", "CarvedTrim")]
        [TestCase("Underheart Copper Pipe", "AmethystBronze")]
        [TestCase("Underheart Safety Rail", "AmethystBronze")]
        [TestCase("Underheart Riveted Plate", "SlateTimber")]
        [TestCase("Underheart Heat Ring", "")]
        [TestCase("Underheart Crystal 1", "")]
        [TestCase("Underheart Rock 2", "")]
        [TestCase("Unrecognized owner material", "")]
        public void OnlyKnownIndustrialSurfacesAreRemapped(string original, string expected)
        {
            Assert.That(new UpperUnderheartRewriteRules().MaterialRole(original), Is.EqualTo(expected));
        }

        [Test]
        public void InvalidOrUnknownOwnershipKeepsOriginal()
        {
            UpperUnderheartRewriteRules rules = new UpperUnderheartRewriteRules();
            Assert.That(rules.Eligible(null), Is.False);
            UhCell cell = UnderheartLayout.Create(0).Cells[0];
            cell.Region = "unknown_owner";
            Assert.That(rules.Eligible(cell), Is.False);
            cell.Region = UnderheartIds.ServiceLevel; cell.Vehicle = true;
            Assert.That(rules.Eligible(cell), Is.False);
            cell.Vehicle = false; cell.OpenTop = true;
            Assert.That(rules.Eligible(cell), Is.False);
        }

        [Test]
        public void RibsNeverEnterSlopesRockOrLowHeadroom()
        {
            UpperUnderheartRewriteRules rules = new UpperUnderheartRewriteRules();
            UnderheartLayout layout = UnderheartLayout.Create(0);
            float spring, rise;
            Assert.That(rules.RibProfile(layout.Cells[layout.StairCell], out spring, out rise), Is.False);
            Assert.That(rules.RibProfile(layout.Cells[layout.CellIndex("service.hollow")], out spring, out rise), Is.False);
            Assert.That(rules.RibProfile(layout.Cells[layout.CellIndex("service.panel")], out spring, out rise), Is.False);
            Assert.That(rules.RibProfile(layout.Cells[layout.CellIndex("service.hall")], out spring, out rise), Is.True);
        }
    }
}
