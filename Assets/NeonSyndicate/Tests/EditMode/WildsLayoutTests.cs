using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The wilds (WildsLayout, WildsSites, WildsShapes, WildsMap) on the default world (Gothic Civic H5 on seed 61724): the
    /// same seed gives the same plan; all seventeen named places exist once, in their own land, far enough apart and
    /// beyond the beltway; no piece stands on a road, a story site, a desert lot, a collectible or creature spot or a
    /// reserved point; everything stays inside the walls; pieces on a swell sit on its surface; every piece kind builds
    /// finite geometry with outward faces; the map paint only touches the open desert. Engine-free; Unity NOT RUN here.
    /// </summary>
    public sealed class WildsLayoutTests
    {
        private static CityLayout World()
        {
            CityLayout L = new CityLayout(61724);
            L.Generate();
            L.ApplyProfile04O(CityRevision04O.GothicCivicH5, InhabitedCityBindings04C.Select(L, InhabitedCityBindings04CTests.ReadCatalog()));
            return L;
        }

        [Test]
        public void SamePlanEveryTime()
        {
            WildsPlan a = WildsLayout.Plan(World(), null), b = WildsLayout.Plan(World(), null);
            Assert.That(b.Pieces.Count, Is.EqualTo(a.Pieces.Count));
            for (int i = 0; i < a.Sites.Count; i++) Assert.That(b.Sites[i].Id, Is.EqualTo(a.Sites[i].Id));
            for (int i = 0; i < a.Pieces.Count; i += 97) Assert.That(b.Pieces[i].Position, Is.EqualTo(a.Pieces[i].Position));
        }

        [Test]
        public void EveryNamedPlaceOnceInItsOwnLand()
        {
            WildsPlan plan = WildsLayout.Plan(World(), null);
            int expected = 0;
            foreach (WildSiteKind[] kinds in WildsLayout.SitesByBiome) expected += kinds.Length;
            Assert.That(plan.Sites.Count, Is.EqualTo(expected), "every named place found room");
            HashSet<string> ids = new HashSet<string>(), names = new HashSet<string>();
            foreach (WildSite s in plan.Sites)
            {
                Assert.That(ids.Add(s.Id), Is.True, s.Id);
                Assert.That(names.Add(s.Name), Is.True, s.Name);
                Assert.That(WildsLayout.BiomeAt(s.Center.x, s.Center.z), Is.EqualTo(s.Biome), s.Name + " in its land");
                Assert.That(Mathf.Max(Mathf.Abs(s.Center.x), Mathf.Abs(s.Center.z)), Is.GreaterThan(CityLayout.RingHalf + s.Radius), s.Name + " beyond the beltway");
                foreach (WildSite o in plan.Sites)
                    if (o != s) Assert.That(MathUtil.FlatDistance(o.Center, s.Center), Is.GreaterThanOrEqualTo(WildsLayout.SiteSpacing - 0.01f));
                Assert.That(s.Name.IndexOf('\u2014') < 0 && s.Name.IndexOf('\u2013') < 0, Is.True);
            }
        }

        [Test]
        public void NothingLandsOnRoadsSitesLotsOrSpots()
        {
            CityLayout L = World();
            WildsPlan plan = WildsLayout.Plan(L, null);
            Assert.That(plan.Pieces.Count, Is.GreaterThan(10000), "the wilds are full");
            foreach (WildPiece p in plan.Pieces)
            {
                Assert.That(Mathf.Abs(p.Position.x) < CityLayout.MapHalf && Mathf.Abs(p.Position.z) < CityLayout.MapHalf, Is.True, "inside the walls");
                if (p.Site >= 0) continue;
                Assert.That(WildsLayout.InWilds(p.Position.x, p.Position.z), Is.True, p.Kind + " in the wilds band");
                float foot = Mathf.Max(p.Size.x, p.Size.z) * 0.5f;
                if (p.Kind == WildPieceKind.GroundPatch) continue;
                foreach (RoadEdge e in L.Edges)
                {
                    float d = SegmentDistance(p.Position, L.Nodes[e.A].Pos, L.Nodes[e.B].Pos);
                    Assert.That(d, Is.GreaterThan(e.Width * 0.5f + 1f), p.Kind + " clear of the road");
                }
                foreach (SpecialSite s in L.Sites) Assert.That(MathUtil.FlatDistance(p.Position, s.Position), Is.GreaterThan(s.Radius), p.Kind + " clear of " + s.Name);
                foreach (Vector3 c in L.CreatureSpots) Assert.That(MathUtil.FlatDistance(p.Position, c), Is.GreaterThan(3f));
                foreach (Vector3 c in L.DataChipSpots) Assert.That(MathUtil.FlatDistance(p.Position, c), Is.GreaterThan(2f));
                foreach (Vector3 c in L.CrystalShardSpots) Assert.That(MathUtil.FlatDistance(p.Position, c), Is.GreaterThan(2f));
                foreach (Lot lot in L.DesertLots)
                {
                    RectF r = lot.Area;
                    bool inside = p.Position.x > r.X0 - 2f && p.Position.x < r.X1 + 2f && p.Position.z > r.Z0 - 2f && p.Position.z < r.Z1 + 2f;
                    Assert.That(inside, Is.False, p.Kind + " clear of a desert lot");
                }
            }
        }

        [Test]
        public void ReservedSpotsStayClear()
        {
            CityLayout L = World();
            WildsPlan first = WildsLayout.Plan(L, null);
            Vector3 taken = first.Sites[0].Center;
            WildsPlan second = WildsLayout.Plan(L, new[] { new WildsLayout.Reserved(taken, 60f) });
            foreach (WildPiece p in second.Pieces)
            {
                if (p.Kind == WildPieceKind.GroundPatch) continue;
                Assert.That(MathUtil.FlatDistance(p.Position, taken), Is.GreaterThan(60f), p.Kind + " kept clear of the reserved spot");
            }
        }

        [Test]
        public void PiecesOnASwellSitOnItsSurface()
        {
            WildsPlan plan = WildsLayout.Plan(World(), null);
            Assert.That(plan.Swells.Count, Is.GreaterThan(100), "the land rolls");
            int lifted = 0;
            foreach (WildPiece p in plan.Pieces)
            {
                if (p.Kind == WildPieceKind.Swell || p.Kind == WildPieceKind.GroundPatch || p.Site >= 0) continue;
                Assert.That(p.Position.y, Is.EqualTo(WildsLayout.SurfaceAt(plan, p.Position.x, p.Position.z)).Within(1e-3f));
                if (p.Position.y > 0.2f) lifted++;
            }
            Assert.That(lifted, Is.GreaterThan(0), "some pieces stand on rises");
            Assert.That(WildsLayout.SurfaceAt(plan, 0f, 0f), Is.EqualTo(0f), "the city stays flat");
        }

        [Test]
        public void EveryPieceKindBuildsFiniteOutwardGeometry()
        {
            GRand r = new GRand(5);
            foreach (WildPieceKind kind in System.Enum.GetValues(typeof(WildPieceKind)))
            {
                MeshBuilder m = new MeshBuilder(WildsShapes.MatCount), c = new MeshBuilder(1);
                WildPiece p = new WildPiece { Kind = kind, Position = new Vector3(10f, 0f, 20f), Yaw = 30f, Size = WildsLayout.SizeOf(kind, 6f, r), Seed = 1234, Solid = true, Site = -1, Biome = WildBiome.HoodooBadlands };
                if (kind == WildPieceKind.Pond || kind == WildPieceKind.SaltPan) p.Size = new Vector3(20f, 0.1f, 14f);
                WildsShapes.Build(p, m, c);
                Assert.That(m.VertexCount, Is.GreaterThan(0), kind + " builds");
                foreach (Vector3 v in m.Vertices) Assert.That(float.IsNaN(v.x) || float.IsInfinity(v.y), Is.False, kind + " finite");
                foreach (Vector3 n in m.Normals) Assert.That(n.magnitude, Is.EqualTo(1f).Within(0.02f), kind + " unit normals");
            }
            // A boulder's faces point away from its middle.
            MeshBuilder rock = new MeshBuilder(WildsShapes.MatCount);
            WildsShapes.Rock(rock, null, Vector3.zero, new Vector3(4f, 3f, 4f), QMath.Identity, new GRand(9), WildMat.GreyRock, 1, 0.2f);
            int outward = 0, total = 0;
            IReadOnlyList<int> t = rock.Triangles((int)WildMat.GreyRock);
            for (int i = 0; i < t.Count; i += 3)
            {
                Vector3 a = rock.Vertices[t[i]], b = rock.Vertices[t[i + 1]], cc = rock.Vertices[t[i + 2]];
                Vector3 n = Vector3.Cross(b - a, cc - a);
                if (Vector3.Dot(n, (a + b + cc) / 3f - new Vector3(0f, 1.35f, 0f)) > 0f) outward++;
                total++;
            }
            Assert.That(outward, Is.GreaterThan(total * 0.9f), "front faces point out (MeshBuilder winding)");
        }

        [Test]
        public void MapPaintOnlyTouchesTheOpenDesert()
        {
            WildsPlan plan = WildsLayout.Plan(World(), null);
            const int size = 256;
            Color32[] px = new Color32[size * size];
            Color32 road = new Color32(150, 152, 165, 255);
            for (int i = 0; i < px.Length; i++) px[i] = WildsMap.Desert;
            px[size * (size / 2) + size / 2] = road;
            WildsMap.Paint(px, size, plan);
            Assert.That(px[size * (size / 2) + size / 2].r, Is.EqualTo(road.r), "a road pixel in the city keeps its colour");
            int tinted = 0;
            foreach (Color32 c in px) if (c.r != WildsMap.Desert.r || c.g != WildsMap.Desert.g) tinted++;
            Assert.That(tinted, Is.GreaterThan(px.Length / 3), "the lands show on the map");
        }

        private static float SegmentDistance(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 P = new Vector2(p.x, p.z), A = new Vector2(a.x, a.z), B = new Vector2(b.x, b.z);
            Vector2 ab = B - A;
            float t = ab.sqrMagnitude > 1e-6f ? Mathf.Clamp01(Vector2.Dot(P - A, ab) / ab.sqrMagnitude) : 0f;
            return (A + ab * t - P).magnitude;
        }
    }
}
