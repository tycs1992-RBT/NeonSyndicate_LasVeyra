using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class TerritoryPlacementTests
    {
        [Test]
        public void CandidatesAreStableUniqueAndBounded()
        {
            List<Vector3> first = TerritoryPlacement.Candidates(1150f, -800f);
            List<Vector3> second = TerritoryPlacement.Candidates(1150f, -800f);
            Assert.That(first.Count, Is.InRange(1, 225));
            Assert.That(second, Is.EqualTo(first));
            HashSet<Vector3> seen = new HashSet<Vector3>();
            foreach (Vector3 point in first)
            {
                Assert.That(seen.Add(point), Is.True);
                Assert.That(StrongholdPlacement.RegionAllowed(point), Is.True);
                Assert.That(Mathf.Abs(point.x - 1150f), Is.LessThanOrEqualTo(224f));
                Assert.That(Mathf.Abs(point.z + 800f), Is.LessThanOrEqualTo(224f));
            }
        }

        [Test]
        public void ReservationHonorsExactSeparationBoundary()
        {
            Vector3 center = new Vector3(1120f, 0f, -740f);
            List<Vector3> reserved = new List<Vector3> { center };
            Assert.That(TerritoryPlacement.CanReserve(center + Vector3.right * 87.99f, reserved), Is.False);
            Assert.That(TerritoryPlacement.CanReserve(center + Vector3.right * 88f, reserved), Is.True);
            Assert.That(TerritoryPlacement.CanReserve(center + Vector3.up * 100f, reserved), Is.False,
                "Height never bypasses the protected horizontal footprint");
        }

        [Test]
        public void InvalidCandidatesAndReservationsFailClosed()
        {
            Assert.That(TerritoryPlacement.Candidates(float.NaN, 0f), Is.Empty);
            Assert.That(TerritoryPlacement.Candidates(0f, float.PositiveInfinity), Is.Empty);
            Assert.That(TerritoryPlacement.Candidates(1000000f, 1000000f), Is.Empty);
            Vector3 candidate = new Vector3(1200f, 0f, -700f);
            Assert.That(TerritoryPlacement.CanReserve(candidate, null), Is.False);
            Assert.That(TerritoryPlacement.CanReserve(candidate, new[] { new Vector3(float.NaN, 0f, 0f) }), Is.False);
            Assert.That(TerritoryPlacement.CanReserve(new Vector3(0f, 0f, 0f), new List<Vector3>()), Is.False);
        }
    }
}
