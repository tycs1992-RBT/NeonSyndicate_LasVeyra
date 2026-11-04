#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class KingdomStreetWR02GTests
    {
        [Test]
        public void FlagstonesStayOnTheSupportedSurfaceAndClearTheOriginalOpening()
        {
            List<HeartStreetPieceWR02D> route = new List<HeartStreetPieceWR02D>
            {
                new HeartStreetPieceWR02D(new List<Vector2> { new Vector2(0,0), new Vector2(12,0), new Vector2(12,6), new Vector2(0,6) }, false)
            };
            List<RectF> openings = new List<RectF> { new RectF(4,2,8,4) };
            Assert.That(HeartPlanFR01.KingdomLegacyStreetClearWR02G(route, openings), Is.False);
            Assert.That(HeartPlanFR01.KingdomLegacyStreetClearWR02G(route, new List<RectF>()), Is.True);
            List<KingdomStreetStoneWR02G> result = HeartPlanFR01.KingdomStreetStonesWR02G(route, openings);
            Assert.That(result.Count, Is.GreaterThan(10));
            foreach (KingdomStreetStoneWR02G stone in result) foreach (Vector2 point in stone.Points)
            {
                Assert.That(point.x, Is.InRange(-.001f, 12.001f)); Assert.That(point.y, Is.InRange(-.001f, 6.001f));
                Assert.That(point.x > 4 && point.x < 8 && point.y > 2 && point.y < 4, Is.False);
            }
        }

        [Test]
        public void MissingRouteAndInvalidOpeningRefuseTheCandidate()
        {
            float height;
            Assert.That(HeartPlanFR01.KingdomStreetPlanWR02G(new List<RealmRoute04OB>(), out height), Is.Empty);
            List<HeartStreetPieceWR02D> route = new List<HeartStreetPieceWR02D>
            {
                new HeartStreetPieceWR02D(new List<Vector2> { Vector2.zero, Vector2.right, Vector2.one, Vector2.up }, false)
            };
            Assert.That(HeartPlanFR01.KingdomStreetStonesWR02G(route, new List<RectF> { new RectF(0,0,float.PositiveInfinity,1) }), Is.Empty);
        }
    }
}
#endif
