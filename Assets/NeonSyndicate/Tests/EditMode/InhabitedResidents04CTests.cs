using NUnit.Framework;

namespace NeonSyndicate.Tests
{
    public sealed class InhabitedResidents04CTests
    {
        [Test]
        public void AdmissionsCountExistingHumanoidsAndPendingCreatureBodies()
        {
            for (int preset = 0; preset < 4; preset++)
            {
                Assert.IsTrue(InhabitedResidentRules04C.CanAllocate(preset, 0, 0, 0));
                Assert.IsFalse(InhabitedResidentRules04C.CanAllocate(preset, InhabitedResidentRules04C.ResidentCap(preset), 0, 0));
                Assert.IsFalse(InhabitedResidentRules04C.CanAllocate(preset, 0, InhabitedResidentRules04C.HumanoidCap(preset), 0));
                Assert.IsFalse(InhabitedResidentRules04C.CanAllocate(preset, 0, 1, InhabitedResidentRules04C.CombinedCap(preset) - 1));
                Assert.LessOrEqual(InhabitedResidentRules04C.VenueCap(preset), InhabitedResidentRules04C.ResidentCap(preset));
            }
        }

        [Test]
        public void OccupancyOverridesDistanceButHysteresisDoesNotLoadDistantRooms()
        {
            Assert.IsTrue(InhabitedResidentRules04C.ShouldLoad(false, true, 1000f));
            Assert.IsFalse(InhabitedResidentRules04C.ShouldLoad(false, false, 70f));
            Assert.IsTrue(InhabitedResidentRules04C.ShouldLoad(true, false, 70f));
            Assert.IsFalse(InhabitedResidentRules04C.ShouldLoad(true, false, 95f));
            Assert.IsFalse(InhabitedResidentRules04C.ShouldLoad(false, false, float.NaN));
        }

        [Test]
        public void FloorProbeRejectsAnotherStoreyAndInvalidValues()
        {
            Assert.IsTrue(InhabitedResidentRules04C.FloorMatches(4f, 4.025f));
            Assert.IsFalse(InhabitedResidentRules04C.FloorMatches(4f, 7f));
            Assert.IsFalse(InhabitedResidentRules04C.FloorMatches(0f, float.NaN));
            Assert.IsFalse(InhabitedResidentRules04C.FloorMatches(float.PositiveInfinity, float.PositiveInfinity));
        }

        [Test]
        public void AppearanceSeedsAreStablePerVenueSlotAndWorld()
        {
            int seed = InhabitedResidentRules04C.Seed(61724, "veyra_lofts", 0);
            Assert.AreEqual(seed, InhabitedResidentRules04C.Seed(61724, "veyra_lofts", 0));
            Assert.AreNotEqual(seed, InhabitedResidentRules04C.Seed(61724, "veyra_lofts", 1));
            Assert.AreNotEqual(seed, InhabitedResidentRules04C.Seed(61725, "veyra_lofts", 0));
        }

        [Test]
        public void OriginalRoomTonesAreBoundedAndPeriodicallyJoined()
        {
            foreach (string kind in new[] { "lofts", "clinic", "forge", "inn", "archive", "observatory", "garage", "depot" })
            {
                float[] data = InhabitedResidentRules04C.RoomTone(kind, 22050);
                Assert.AreEqual(88200, data.Length);
                double energy = 0;
                foreach (float value in data)
                {
                    Assert.IsFalse(float.IsNaN(value) || float.IsInfinity(value));
                    Assert.LessOrEqual(System.Math.Abs(value), 0.3f);
                    energy += value * value;
                }
                Assert.Greater(energy / data.Length, 0.001);
                Assert.Less(System.Math.Abs(data[0] - data[data.Length - 1]), 0.03f);
            }
        }
    }
}
