using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The Phase 4 visual pass: the dressing plan (decals, clutter, roof gear, foliage cards) is deterministic,
    /// preset-tiered, capped per block and kept out of doors, quest points, walk lines, buildings and parked cars;
    /// the ambient VFX budget rules pick the nearest spots within the preset's limits; the mesh kit shades curved
    /// primitives smoothly with unit normals.
    /// </summary>
    public sealed class Phase4DressingTests
    {
        private static DressingPlanner _plan;

        private static DressingPlanner Plan
        {
            get
            {
                if (_plan == null)
                {
                    _plan = new DressingPlanner(TestData.City, new DressContext());
                    _plan.Plan();
                }
                return _plan;
            }
        }

        private static Block BlockAt(CityLayout L, Vector3 p)
        {
            foreach (Block b in L.Blocks) if (b.Curb.Contains(p.x, p.z)) return b;
            return null;
        }

        // ------------------------------------------------------------------ rules

        [Test]
        public void TiersPartitionTheRankAndLowShowsNothing()
        {
            Assert.That(DressingRules.TierFor(0f), Is.EqualTo(GraphicsPreset.Medium));
            Assert.That(DressingRules.TierFor(DressingRules.MediumShare - 0.001f), Is.EqualTo(GraphicsPreset.Medium));
            Assert.That(DressingRules.TierFor(DressingRules.MediumShare), Is.EqualTo(GraphicsPreset.High));
            Assert.That(DressingRules.TierFor(DressingRules.HighShare - 0.001f), Is.EqualTo(GraphicsPreset.High));
            Assert.That(DressingRules.TierFor(DressingRules.HighShare), Is.EqualTo(GraphicsPreset.Ultra));
            Assert.That(DressingRules.TierFor(0.999f), Is.EqualTo(GraphicsPreset.Ultra));
            Assert.That(DressingRules.Enabled(GraphicsPreset.Low), Is.False);
            foreach (GraphicsPreset tier in new[] { GraphicsPreset.Medium, GraphicsPreset.High, GraphicsPreset.Ultra })
                Assert.That(DressingRules.Shows(tier, GraphicsPreset.Low), Is.False, tier + " on Low");
            Assert.That(DressingRules.Shows(GraphicsPreset.Medium, GraphicsPreset.Medium), Is.True);
            Assert.That(DressingRules.Shows(GraphicsPreset.High, GraphicsPreset.Medium), Is.False);
            Assert.That(DressingRules.Shows(GraphicsPreset.Ultra, GraphicsPreset.High), Is.False);
            Assert.That(DressingRules.Shows(GraphicsPreset.Medium, GraphicsPreset.Ultra), Is.True);
            Assert.That(DressingRules.CountFor(100, GraphicsPreset.Low), Is.EqualTo(0));
            Assert.That(DressingRules.CountFor(100, GraphicsPreset.Medium), Is.LessThan(DressingRules.CountFor(100, GraphicsPreset.High)));
            Assert.That(DressingRules.CountFor(100, GraphicsPreset.High), Is.LessThan(DressingRules.CountFor(100, GraphicsPreset.Ultra)));
            Assert.That(DressingRules.CountFor(100, GraphicsPreset.Ultra), Is.EqualTo(100));
        }

        [Test]
        public void KeepoutsAndWalkLineTests()
        {
            List<Keepout> keep = new List<Keepout> { new Keepout(10f, 10f, 3f) };
            Assert.That(DressingRules.Clear(keep, 10f, 10f, 0f), Is.False);
            Assert.That(DressingRules.Clear(keep, 12.5f, 10f, 0.4f), Is.False);
            Assert.That(DressingRules.Clear(keep, 13.5f, 10f, 0.4f), Is.True);
            Block b = new Block();
            b.Curb = new RectF(0f, 0f, 40f, 40f);
            float mid = CityLayout.SidewalkWidth * 0.5f;
            Assert.That(DressingRules.OnWalkLine(b, mid, 20f), Is.True, "the walk line runs through the sidewalk middle");
            Assert.That(DressingRules.OnWalkLine(b, 20f, 40f - mid), Is.True);
            Assert.That(DressingRules.OnWalkLine(b, 20f, 20f), Is.False, "the block interior is not a walk line");
            Assert.That(DressingRules.OnWalkLine(b, mid + DressingRules.WalkLineClearance - 0.1f, 20f), Is.True, "within clearance of the line");
            Assert.That(DressingRules.OnWalkLine(b, mid + DressingRules.WalkLineClearance + 0.1f, 20f), Is.False, "beyond the clearance");
            Assert.That(DressingRules.OnWalkLine(b, -3f, 20f), Is.False, "the road is not the walk line");
        }

        [Test]
        public void FacadeSideHelpersAgree()
        {
            RectF inner = new RectF(0f, 0f, 30f, 30f);
            RectF west = new RectF(0.2f, 5f, 10f, 15f);
            Assert.That(DressingRules.FacesSidewalk(west, inner, 0), Is.True);
            Assert.That(DressingRules.FacesSidewalk(west, inner, 1), Is.False);
            Vector3 a, e;
            DressingRules.SideSegment(west, 0, out a, out e);
            Assert.That(a.x, Is.EqualTo(0.2f).Within(1e-4f));
            Assert.That(e.z - a.z, Is.EqualTo(10f).Within(1e-4f));
            Assert.That(DressingRules.SideNormal(0), Is.EqualTo(Vector3.left));
            Assert.That(DressingRules.SideNormal(3), Is.EqualTo(Vector3.forward));
            // FacesOpen: a set-back facade counts while the strip to the sidewalk is open, not when another building fills it.
            Block b = new Block();
            b.Inner = inner;
            Lot lot = new Lot();
            Building back = new Building { Footprint = new RectF(4f, 10f, 14f, 20f) };
            lot.Buildings.Add(back);
            b.Lots.Add(lot);
            Assert.That(DressingRules.FacesOpen(b, back, 0), Is.True, "4 m setback, open strip");
            Assert.That(DressingRules.FacesOpen(b, back, 1), Is.False, "16 m to the east edge is more than the setback limit");
            Building front = new Building { Footprint = new RectF(0.5f, 12f, 3f, 18f) };
            lot.Buildings.Add(front);
            Assert.That(DressingRules.FacesOpen(b, back, 0), Is.False, "another building stands in the strip");
            Assert.That(DressingRules.FacesOpen(b, front, 0), Is.True, "the front building faces the sidewalk");
        }

        // ------------------------------------------------------------------ the plan

        [Test]
        public void PlanIsDeterministicAndSized()
        {
            DressingPlanner again = new DressingPlanner(TestData.City, new DressContext());
            again.Plan();
            Assert.That(again.Items.Count, Is.EqualTo(Plan.Items.Count), "same count from the same seed");
            Assert.That(Plan.Items.Count, Is.GreaterThan(4000), "the pass should dress the whole city");
            Assert.That(Plan.Items.Count, Is.LessThan(250000), "and stay within a sane budget");
            for (int i = 0; i < Plan.Items.Count; i += 97)
            {
                DressItem x = Plan.Items[i], y = again.Items[i];
                Assert.That(x.Kind, Is.EqualTo(y.Kind), "kind " + i);
                Assert.That(Vector3.Distance(x.Pos, y.Pos), Is.LessThan(1e-4f), "position " + i);
                Assert.That(x.Tier, Is.EqualTo(y.Tier), "tier " + i);
                Assert.That(x.Seed, Is.EqualTo(y.Seed), "seed " + i);
            }
        }

        [Test]
        public void EveryKindIsPlannedSomewhere()
        {
            HashSet<DressKind> seen = new HashSet<DressKind>();
            foreach (DressItem it in Plan.Items) seen.Add(it.Kind);
            foreach (DressKind k in System.Enum.GetValues(typeof(DressKind)))
                Assert.That(seen.Contains(k), Is.True, k + " never planned on the default city");
        }

        [Test]
        public void PresetTiersGrowFromMediumToUltra()
        {
            int medium = 0, high = 0, ultra = 0, ranked = 0;
            foreach (DressItem it in Plan.Items)
            {
                if (it.Tier == GraphicsPreset.Medium) medium++;
                else if (it.Tier == GraphicsPreset.High) high++;
                else ultra++;
                if (it.Kind != DressKind.Manhole) ranked++;
                Assert.That(it.Tier, Is.Not.EqualTo(GraphicsPreset.Low), "no item is tiered Low");
            }
            Assert.That(medium, Is.GreaterThan(0));
            Assert.That(high, Is.GreaterThan(medium), "High adds more than Medium shows");
            Assert.That(ultra, Is.GreaterThan(0));
            // Medium shows roughly its share of the ranked set (manholes are forced into Medium).
            float mediumShare = (medium - CountKind(DressKind.Manhole)) / (float)ranked;
            Assert.That(mediumShare, Is.EqualTo(DressingRules.MediumShare).Within(0.06f));
        }

        private static int CountKind(DressKind k)
        {
            int n = 0;
            foreach (DressItem it in Plan.Items) if (it.Kind == k) n++;
            return n;
        }

        [Test]
        public void PerBlockCapsHold()
        {
            foreach (KeyValuePair<int, int> kv in Plan.DecalsPerBlock)
                Assert.That(kv.Value, Is.LessThanOrEqualTo(DressingRules.MaxDecalsPerBlock), "decals in block " + kv.Key);
            foreach (KeyValuePair<int, int> kv in Plan.ClutterPerBlock)
                Assert.That(kv.Value, Is.LessThanOrEqualTo(DressingRules.MaxClutterPerBlock), "clutter in block " + kv.Key);
        }

        [Test]
        public void NothingSitsOnAPointOfInterestOrAnEntrance()
        {
            CityLayout L = TestData.City;
            float poi = DressingRules.PoiClearance - 0.01f;
            float door = DressingRules.EntranceClearance - 0.01f;
            PointGrid pois = new PointGrid();
            PointGrid doors = new PointGrid();
            foreach (Poi p in L.Pois) pois.Add(p.Position);
            foreach (Block b in L.Blocks) foreach (Lot lot in b.Lots) doors.Add(lot.Entrance);
            foreach (DressItem it in Plan.Items)
            {
                if (it.IsRoof || it.IsWall || it.IsFoliage) continue;
                Assert.That(pois.Nearest(it.Pos), Is.GreaterThanOrEqualTo(poi), it.Kind + " on a point of interest at " + it.Pos);
                Assert.That(doors.Nearest(it.Pos), Is.GreaterThanOrEqualTo(door), it.Kind + " on a lot entrance at " + it.Pos);
            }
        }

        /// <summary>A coarse grid of points so the clearance tests stay fast (cell 16 m, neighbours searched).</summary>
        private sealed class PointGrid
        {
            private readonly Dictionary<long, List<Vector3>> _cells = new Dictionary<long, List<Vector3>>();
            private const float Cell = 16f;

            private static long Key(int cx, int cz) { return ((long)cx << 32) ^ (uint)cz; }

            public void Add(Vector3 p)
            {
                long k = Key(Mathf.FloorToInt(p.x / Cell), Mathf.FloorToInt(p.z / Cell));
                List<Vector3> l;
                if (!_cells.TryGetValue(k, out l)) { l = new List<Vector3>(); _cells[k] = l; }
                l.Add(p);
            }

            /// <summary>The nearest flat distance to any point within one cell (or a large number).</summary>
            public float Nearest(Vector3 p)
            {
                int cx = Mathf.FloorToInt(p.x / Cell), cz = Mathf.FloorToInt(p.z / Cell);
                float best = 1e9f;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        List<Vector3> l;
                        if (!_cells.TryGetValue(Key(cx + dx, cz + dz), out l)) continue;
                        for (int i = 0; i < l.Count; i++) best = Mathf.Min(best, MathUtil.FlatDistance(l[i], p));
                    }
                return best;
            }
        }

        [Test]
        public void ClutterKeepsOffParkedCarsAndStreetFurniture()
        {
            CityLayout L = TestData.City;
            float park = DressingRules.ParkingClearance - 0.01f;
            float lamp = DressingRules.FurnitureClearance - 0.01f;
            PointGrid cars = new PointGrid();
            PointGrid furniture = new PointGrid();
            foreach (Vector3 p in L.ParkingSpots) cars.Add(p);
            foreach (PropSpot p in L.Props) furniture.Add(p.Pos);
            int checkedItems = 0;
            foreach (DressItem it in Plan.Items)
            {
                if (it.IsDecal || it.IsRoof || it.IsFoliage) continue;
                checkedItems++;
                Assert.That(cars.Nearest(it.Pos), Is.GreaterThanOrEqualTo(park), it.Kind + " under a parked car at " + it.Pos);
                Assert.That(furniture.Nearest(it.Pos), Is.GreaterThanOrEqualTo(lamp), it.Kind + " inside street furniture at " + it.Pos);
            }
            Assert.That(checkedItems, Is.GreaterThan(500));
        }

        [Test]
        public void GroundPiecesStayOutOfBuildingsAndOffTheWalkLine()
        {
            CityLayout L = TestData.City;
            foreach (DressItem it in Plan.Items)
            {
                if (it.IsRoof || it.IsWall) continue;
                Block b = BlockAt(L, it.Pos);
                if (b == null) continue;
                Assert.That(DressingRules.InsideBuilding(b, it.Pos.x, it.Pos.z, -0.05f), Is.False, it.Kind + " inside a building at " + it.Pos);
                bool litter = it.Kind == DressKind.Bottle || it.Kind == DressKind.Can || it.Kind == DressKind.PaperBall || it.Kind == DressKind.CardboardBox || it.Kind == DressKind.Planter;
                if (litter && !b.Inner.Contains(it.Pos.x, it.Pos.z))
                    Assert.That(DressingRules.OnWalkLine(b, it.Pos.x, it.Pos.z), Is.False, it.Kind + " on the walk line at " + it.Pos);
            }
        }

        [Test]
        public void RoofGearOnlyOnPlainFlatRoofs()
        {
            CityLayout L = TestData.City;
            int roofs = 0;
            foreach (DressItem it in Plan.Items)
            {
                if (!it.IsRoof) continue;
                roofs++;
                Block b = BlockAt(L, it.Pos);
                Assert.That(b, Is.Not.Null, "roof piece outside every block at " + it.Pos);
                bool found = false;
                foreach (Lot lot in b.Lots)
                {
                    foreach (Building bld in lot.Buildings)
                    {
                        if (!bld.Footprint.Contains(it.Pos.x, it.Pos.z)) continue;
                        // Footprints can overlap inside a lot; the piece belongs to the eligible roof at its own height.
                        if (!DressingPlanner.RoofEligible(bld)) continue;
                        if (Mathf.Abs(it.Pos.y - (CityLayout.CurbHeight + bld.Height)) > 0.01f) continue;
                        found = true;
                    }
                }
                Assert.That(found, Is.True, "roof piece over no plain flat roof at its height, at " + it.Pos);
            }
            Assert.That(roofs, Is.GreaterThan(50), "the plain flat roofs should get equipment");
        }

        [Test]
        public void WallDecalsFaceOutward()
        {
            int walls = 0;
            foreach (DressItem it in Plan.Items)
            {
                if (!it.IsWall) continue;
                walls++;
                Assert.That(it.Normal.magnitude, Is.EqualTo(1f).Within(1e-3f));
                Assert.That(Mathf.Abs(it.Normal.y), Is.LessThan(1e-3f), "a wall normal is horizontal");
                Assert.That(it.Pos.y, Is.GreaterThan(CityLayout.CurbHeight + 0.9f), "wall decals sit above the ground");
            }
            Assert.That(walls, Is.GreaterThan(50));
        }

        [Test]
        public void ContextKeepsVenuesAndTheirDoorsClear()
        {
            CityLayout L = TestData.City;
            // Pretend the first large lot building is a walk-in venue: nothing may be planned inside it or at its door.
            Building venue = null;
            Lot venueLot = null;
            foreach (Block b in L.Blocks)
            {
                foreach (Lot lot in b.Lots)
                {
                    foreach (Building bld in lot.Buildings)
                    {
                        if (bld.Footprint.Width > 14f && bld.Footprint.Depth > 14f) { venue = bld; venueLot = lot; break; }
                    }
                    if (venue != null) break;
                }
                if (venue != null) break;
            }
            Assert.That(venue, Is.Not.Null);
            RectF fp = venue.Footprint.Inset(-1.5f);
            DressContext ctx = new DressContext();
            ctx.InsideVenue = p => fp.Contains(p.x, p.z);
            ctx.SkipBuilding = b => b == venue;
            Vector3 doorPoint = venueLot.Entrance + venueLot.Facing * -2f;
            ctx.VenueDoors.Add(doorPoint);
            DressingPlanner p2 = new DressingPlanner(L, ctx);
            p2.Plan();
            foreach (DressItem it in p2.Items)
            {
                if (it.IsRoof) Assert.That(venue.Footprint.Contains(it.Pos.x, it.Pos.z), Is.False, "roof gear on the venue");
                if (it.IsWall || it.IsRoof) continue;
                Assert.That(fp.Contains(it.Pos.x, it.Pos.z), Is.False, it.Kind + " inside the venue footprint at " + it.Pos);
                if (!it.IsFoliage)
                    Assert.That(MathUtil.FlatDistance(doorPoint, it.Pos), Is.GreaterThanOrEqualTo(DressingRules.VenueDoorClearance - 0.01f), it.Kind + " at the venue door");
            }
        }

        // ------------------------------------------------------------------ ambient vfx rules

        [Test]
        public void AmbientBudgetsScaleWithPresetAndLowGetsNothing()
        {
            Assert.That(AmbientVfxRules.MaxActive(GraphicsPreset.Low), Is.EqualTo(0));
            Assert.That(AmbientVfxRules.MaxActive(GraphicsPreset.Medium), Is.LessThan(AmbientVfxRules.MaxActive(GraphicsPreset.High)));
            Assert.That(AmbientVfxRules.MaxActive(GraphicsPreset.High), Is.LessThan(AmbientVfxRules.MaxActive(GraphicsPreset.Ultra)));
            Assert.That(AmbientVfxRules.CullDistance(GraphicsPreset.Medium), Is.LessThan(AmbientVfxRules.CullDistance(GraphicsPreset.Ultra)));
            foreach (AmbientVfxKind k in System.Enum.GetValues(typeof(AmbientVfxKind)))
            {
                Assert.That(AmbientVfxRules.FamilyOn(k, GraphicsPreset.Low), Is.False, k + " on Low");
                Assert.That(AmbientVfxRules.FamilyOn(k, GraphicsPreset.High), Is.True, k + " on High");
                Assert.That(AmbientVfxRules.ParticleCap(k, GraphicsPreset.Medium), Is.LessThanOrEqualTo(AmbientVfxRules.ParticleCap(k, GraphicsPreset.Ultra)));
                Assert.That(AmbientVfxRules.ParticleCap(k, GraphicsPreset.Ultra), Is.LessThanOrEqualTo(260), k + " cap stays small");
            }
            Assert.That(AmbientVfxRules.FamilyOn(AmbientVfxKind.HeatHaze, GraphicsPreset.Medium), Is.False, "Medium skips the haze family");
        }

        [Test]
        public void AmbientSelectionPicksNearestWithinBudgetAndCull()
        {
            List<float> d = new List<float>();
            List<AmbientVfxKind> k = new List<AmbientVfxKind>();
            List<bool> on = new List<bool>();
            for (int i = 0; i < 40; i++)
            {
                float dist = 2f + i * 3f;
                d.Add(dist * dist);
                k.Add(i % 5 == 0 ? AmbientVfxKind.HeatHaze : AmbientVfxKind.DustMotes);
                on.Add(i != 1);
            }
            List<int> pick = new List<int>();
            AmbientVfxRules.Select(d, k, on, GraphicsPreset.Low, pick);
            Assert.That(pick.Count, Is.EqualTo(0), "Low never starts an emitter");
            AmbientVfxRules.Select(d, k, on, GraphicsPreset.Medium, pick);
            Assert.That(pick.Count, Is.LessThanOrEqualTo(AmbientVfxRules.MaxActive(GraphicsPreset.Medium)));
            Assert.That(pick.Contains(0), Is.False, "index 0 is a haze spot, off on Medium");
            Assert.That(pick.Contains(1), Is.False, "disabled spot never picked");
            Assert.That(pick[0], Is.EqualTo(2), "nearest allowed first");
            for (int i = 1; i < pick.Count; i++) Assert.That(d[pick[i]], Is.GreaterThanOrEqualTo(d[pick[i - 1]]), "sorted by distance");
            float cull = AmbientVfxRules.CullDistance(GraphicsPreset.Medium);
            foreach (int i in pick) Assert.That(d[i], Is.LessThanOrEqualTo(cull * cull), "inside the cull distance");
            AmbientVfxRules.Select(d, k, on, GraphicsPreset.Ultra, pick);
            Assert.That(pick.Count, Is.LessThanOrEqualTo(AmbientVfxRules.MaxActive(GraphicsPreset.Ultra)));
            Assert.That(pick.Contains(0), Is.True, "haze runs on Ultra");
        }

        // ------------------------------------------------------------------ mesh kit

        [Test]
        public void CylindersAndSpheresShadeSmoothWithUnitNormals()
        {
            MeshBuilder b = new MeshBuilder(1);
            b.Cylinder(0, Vector3.zero, 0.5f, 0.3f, 2f, 12, Quaternion.identity, false);
            int sideVerts = 12 * 4;
            Assert.That(b.VertexCount, Is.EqualTo(sideVerts), "four vertices per side face");
            MeshBuilder capped = new MeshBuilder(1);
            capped.Cylinder(0, Vector3.zero, 0.5f, 0.3f, 2f, 12, Quaternion.identity, true);
            Assert.That(capped.VertexCount, Is.EqualTo(sideVerts + 12 * 6), "sides plus flat caps");
            HashSet<Vector3> distinct = new HashSet<Vector3>();
            for (int i = 0; i < sideVerts; i++)
            {
                Vector3 n = b.Normals[i];
                Assert.That(n.magnitude, Is.EqualTo(1f).Within(1e-3f), "unit normal " + i);
                Assert.That(n.y, Is.GreaterThan(0f), "a narrowing frustum's side normal tilts up");
                distinct.Add(new Vector3(Mathf.Round(n.x * 1000f), Mathf.Round(n.y * 1000f), Mathf.Round(n.z * 1000f)));
            }
            Assert.That(distinct.Count, Is.EqualTo(12), "one shared normal per rim vertex: smooth shading");
            // The neighbouring faces share their edge normals.
            Assert.That(Vector3.Distance(b.Normals[2], b.Normals[4]), Is.LessThan(1e-4f), "face i's right edge equals face i+1's left edge");
            MeshBuilder flat = new MeshBuilder(1);
            flat.Cylinder(0, Vector3.zero, 0.5f, 0.5f, 1f, 4, Quaternion.identity, false);
            Assert.That(flat.Normals[0], Is.EqualTo(flat.Normals[1]), "a 4 sided post keeps flat faces");
            Assert.That(Vector3.Distance(flat.Normals[2], flat.Normals[4]), Is.GreaterThan(0.5f), "flat faces do not share normals");
            MeshBuilder s = new MeshBuilder(1);
            s.Sphere(0, Vector3.zero, new Vector3(1f, 0.5f, 1f), 6, 12, Quaternion.identity);
            for (int i = 0; i < s.VertexCount; i++)
            {
                Vector3 n = s.Normals[i];
                Assert.That(n.magnitude, Is.EqualTo(1f).Within(1e-3f), "sphere normal " + i);
                // An ellipsoid normal points outward: positive dot with the position.
                Assert.That(Vector3.Dot(n, s.Vertices[i]), Is.GreaterThan(0f), "outward " + i);
            }
        }
    }
}
