using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    /// <summary>
    /// The H8S creature look: the palette rows it adds (dark bodies, restrained crystal light, glow only where it should
    /// be), the shard cluster planner (varied, hexagonal, deterministic, never a comb), the hide relief fields (tileable,
    /// in range), the species mapping (every species has a family, companions are untouched) and the builder pass
    /// (rig, hit volumes, radius, height and the aim points of every species are exactly those of the plain body, the
    /// added geometry stays within its budget and carries valid skin data).
    /// </summary>
    public sealed class ObsidianCreaturesH8STests
    {
        // ------------------------------------------------------------------ palette

        [Test]
        public void TheThreeRowsAreOwnedAndEveryCellIsInRange()
        {
            Assert.That(ObsidianPaletteH8S.Owns(13), Is.True);
            Assert.That(ObsidianPaletteH8S.Owns(15), Is.True);
            Assert.That(ObsidianPaletteH8S.Owns(12), Is.False);
            for (int row = 13; row <= 15; row++)
                for (int col = 0; col < 16; col++)
                {
                    Color a = CharacterPaletteData.Albedo(row, col), m = CharacterPaletteData.MetalDetailSmooth(row, col), e = CharacterPaletteData.Emission(row, col);
                    foreach (Color c in new[] { a, m, e })
                    {
                        Assert.That(c.r, Is.InRange(0f, 1f), row + "," + col);
                        Assert.That(c.g, Is.InRange(0f, 1f), row + "," + col);
                        Assert.That(c.b, Is.InRange(0f, 1f), row + "," + col);
                        Assert.That(c.a, Is.InRange(0f, 1f), row + "," + col);
                    }
                }
            // Rows 8 to 12 are untouched by the new rows (a spot check of each).
            Assert.That(CharacterPaletteData.Albedo(8, 0), Is.EqualTo(CharacterPaletteData.CreatureHide[0]));
            Assert.That(CharacterPaletteData.Albedo(9, 3), Is.EqualTo(CharacterPaletteData.CreatureCrystal[3]));
            Assert.That(CharacterPaletteData.Albedo(12, 2), Is.EqualTo(CharacterPaletteData.Stone[2]));
        }

        [Test]
        public void BodiesAreDarkAndDoNotGlowAndCrystalLightIsRestrained()
        {
            for (int col = 0; col < 16; col++)
            {
                Color a = CharacterPaletteData.Albedo(13, col);
                float lum = RealismRulesH8S.Luminance(a);
                if (col != ObsidianPaletteH8S.DullBone && col != ObsidianPaletteH8S.WeatheredStone) Assert.That(lum, Is.LessThan(0.36f), "body " + col + " is dark");
                Assert.That(CharacterPaletteData.Emission(13, col), Is.EqualTo(Color.black), "body " + col + " does not glow");
                Assert.That(CharacterPaletteData.MetalDetailSmooth(13, col).g, Is.GreaterThan(0.25f), "body " + col + " takes the relief");
            }
            for (int col = 0; col < 15; col++)
            {
                Color a = CharacterPaletteData.Albedo(14, col), e = CharacterPaletteData.Emission(14, col);
                float ratio = RealismRulesH8S.Luminance(e) / Mathf.Max(1e-4f, RealismRulesH8S.Luminance(a));
                Assert.That(ratio, Is.LessThan(0.2f), "crystal " + col + " has only an inner light");
                Assert.That(ratio, Is.GreaterThan(0.03f), "crystal " + col + " is not dead glass");
                Assert.That(CharacterPaletteData.MetalDetailSmooth(14, col).g, Is.EqualTo(0f), "crystal facets stay flat");
                Assert.That(CharacterPaletteData.MetalDetailSmooth(14, col).a, Is.GreaterThan(0.85f), "crystal is glassy");
            }
            // Tips (the light shade) glow more than roots (the deep shade), per family.
            for (int f = 0; f < 5; f++)
                Assert.That(RealismRulesH8S.Luminance(CharacterPaletteData.Emission(14, f * 3 + 2)), Is.GreaterThan(RealismRulesH8S.Luminance(CharacterPaletteData.Emission(14, f * 3))), "family " + f);
            Assert.That(CharacterPaletteData.Emission(14, ObsidianPaletteH8S.ObsidianHorn), Is.EqualTo(Color.black), "horn does not glow");
            // Eyes glow fully, seams dimly, the mouth dark and the pupil not at all.
            for (int f = 0; f < 5; f++)
            {
                CrystalFamilyH8S fam = (CrystalFamilyH8S)f;
                Vector2Int eye = ObsidianPaletteH8S.GlowCell(fam), dim = ObsidianPaletteH8S.DimCell(fam);
                Assert.That(RealismRulesH8S.Luminance(CharacterPaletteData.Emission(eye.y, eye.x)), Is.GreaterThan(0.4f), fam + " eye");
                Assert.That(RealismRulesH8S.Luminance(CharacterPaletteData.Emission(dim.y, dim.x)), Is.LessThan(0.2f), fam + " seam");
            }
            Assert.That(CharacterPaletteData.Emission(15, ObsidianPaletteH8S.MouthDark), Is.EqualTo(Color.black));
            Assert.That(CharacterPaletteData.Emission(15, ObsidianPaletteH8S.PupilGlass), Is.EqualTo(Color.black));
        }

        [Test]
        public void CrystalFamiliesMatchTheCanonSwatches()
        {
            AssertNear(CharacterPaletteData.Albedo(14, 0), RealismSurfacesH8S.AzureMid, "azure deep is the canon mid teal");
            AssertNear(CharacterPaletteData.Albedo(14, 1), RealismSurfacesH8S.AzureGlow, "azure mid");
            AssertNear(CharacterPaletteData.Albedo(14, 2), RealismSurfacesH8S.AzureLight, "azure light");
            AssertNear(CharacterPaletteData.Albedo(14, 3), RealismSurfacesH8S.AmethystMid, "amethyst deep");
            AssertNear(CharacterPaletteData.Albedo(14, 4), RealismSurfacesH8S.AmethystGlow, "amethyst mid");
            AssertNear(CharacterPaletteData.Albedo(14, 5), RealismSurfacesH8S.AmethystLight, "amethyst light");
            AssertNear(CharacterPaletteData.Albedo(13, ObsidianPaletteH8S.WeatheredStone), RealismSurfacesH8S.HeartWeathered, "weathered stone");
            // Ember is the red family, darker than every other family's deep shade.
            Color ember = CharacterPaletteData.Albedo(14, 9);
            Assert.That(ember.r, Is.GreaterThan(ember.g * 2f));
            Assert.That(RealismRulesH8S.Luminance(ember), Is.LessThan(RealismRulesH8S.Luminance(CharacterPaletteData.Albedo(14, 0))));
        }

        // ------------------------------------------------------------------ clusters

        [Test]
        public void ClustersFanAtVariedAnglesAndSizesNeverAComb()
        {
            CrystalPrismH8S[] prisms = new CrystalPrismH8S[12];
            foreach (uint seed in new uint[] { 1u, 77u, 4242u, 999999u, 3141592u })
                foreach (int count in new[] { 3, 5, 7 })
                {
                    int n = CrystalClusterH8S.Plan(seed, count, 0.2f, 0.1f, 0.5f, prisms);
                    Assert.That(n, Is.EqualTo(count));
                    for (int i = 0; i < n; i++)
                    {
                        Assert.That(prisms[i].Length, Is.InRange(0.2f * CrystalClusterH8S.MinLength * 0.85f, 0.2f));
                        Assert.That(prisms[i].Radius, Is.GreaterThan(0f));
                        Assert.That(prisms[i].Dir.magnitude, Is.EqualTo(1f).Within(1e-4f));
                        Assert.That(prisms[i].Dir.z, Is.GreaterThan(0f), "every shard grows out of the surface");
                        Assert.That(new Vector2(prisms[i].Base.x, prisms[i].Base.y).magnitude, Is.LessThanOrEqualTo(0.1f + 1e-4f));
                        for (int j = i + 1; j < n; j++)
                        {
                            float a = prisms[i].Length, b = prisms[j].Length;
                            Assert.That(Mathf.Abs(a - b) / Mathf.Max(a, b), Is.GreaterThan(0.05f), "seed " + seed + ": shards " + i + " and " + j + " differ in length");
                            float ang = Vector3.Angle(prisms[i].Dir, prisms[j].Dir);
                            Assert.That(ang, Is.GreaterThan(CrystalClusterH8S.MinFanDegrees * 0.5f), "seed " + seed + ": shards " + i + " and " + j + " differ in angle");
                        }
                    }
                    Assert.That(prisms[0].Length, Is.GreaterThanOrEqualTo(prisms[1].Length), "the leader is the longest");
                    if (count >= 5) Assert.That(CrystalClusterH8S.FanDegrees(prisms, n), Is.GreaterThan(25f), "seed " + seed + " fans");
                }
        }

        [Test]
        public void ClustersAreDeterministicAndPrismsAreClosedHexagons()
        {
            CrystalPrismH8S[] a = new CrystalPrismH8S[12], b = new CrystalPrismH8S[12];
            CrystalClusterH8S.Plan(31337u, 6, 0.3f, 0.12f, 0.3f, a);
            CrystalClusterH8S.Plan(31337u, 6, 0.3f, 0.12f, 0.3f, b);
            for (int i = 0; i < 6; i++)
            {
                Assert.That(a[i].Base, Is.EqualTo(b[i].Base));
                Assert.That(a[i].Dir, Is.EqualTo(b[i].Dir));
                Assert.That(a[i].Length, Is.EqualTo(b[i].Length));
            }
            List<Vector3> v = new List<Vector3>(), n = new List<Vector3>();
            List<int> kind = new List<int>(), tris = new List<int>();
            CrystalPrismH8S placed = CrystalClusterH8S.Place(a[0], new Vector3(1f, 2f, 3f), Vector3.right, Vector3.forward, Vector3.up);
            Assert.That(placed.Dir.y, Is.GreaterThan(0f), "placed along the surface normal");
            CrystalClusterH8S.Faces(placed, 5u, 0.14f, v, n, kind, tris);
            Assert.That(v.Count, Is.EqualTo(CrystalClusterH8S.PrismVertices));
            Assert.That(tris.Count, Is.EqualTo(CrystalClusterH8S.PrismTriangles * 3));
            int tips = 0;
            for (int i = 0; i < kind.Count; i++) if (kind[i] == 1) tips++;
            Assert.That(tips, Is.EqualTo(CrystalClusterH8S.Sides * 3), "six tip triangles");
            Vector3 axisMid = placed.Base + placed.Dir * (placed.Length * 0.4f);
            for (int i = 0; i + 2 < tris.Count; i += 3)
            {
                Vector3 p0 = v[tris[i]], p1 = v[tris[i + 1]], p2 = v[tris[i + 2]];
                Vector3 face = Vector3.Cross(p1 - p0, p2 - p0);
                Assert.That(face.sqrMagnitude, Is.GreaterThan(0f), "no degenerate face");
                Assert.That(Vector3.Dot(face, n[tris[i]]), Is.GreaterThan(0f), "wound along its normal");
                Assert.That(Vector3.Dot(n[tris[i]], (p0 + p1 + p2) / 3f - axisMid), Is.GreaterThan(-1e-4f), "faces point outward");
            }
            v.Clear(); n.Clear(); kind.Clear(); tris.Clear();
            CrystalClusterH8S.Socket(9u, 7, 0.1f, 0.02f, v, n, kind, tris);
            Assert.That(v.Count, Is.EqualTo(CrystalClusterH8S.SocketVertices(7)));
            foreach (int k in kind) Assert.That(k, Is.EqualTo(2));
        }

        // ------------------------------------------------------------------ hide relief

        [TestCase(CreatureHideH8S.ChitinPlates)]
        [TestCase(CreatureHideH8S.Scales)]
        [TestCase(CreatureHideH8S.Basalt)]
        public void HideReliefIsTileableInRangeAndHasRelief(int family)
        {
            const int n = 128;
            float[] h = CreatureHideH8S.Height(family, n);
            Assert.That(h.Length, Is.EqualTo(n * n));
            float lo = 1f, hi = 0f;
            foreach (float x in h) { lo = Mathf.Min(lo, x); hi = Mathf.Max(hi, x); Assert.That(x, Is.InRange(0f, 1f)); }
            Assert.That(hi - lo, Is.GreaterThan(0.3f), "visible relief");
            // Tileable: no step across the wrap (last column to first, last row to first) is larger than the largest
            // step between neighbouring texels inside the tile (plate edges and shingle lips are hard steps everywhere;
            // a field that did not close on itself would show a step of most of its range at the border).
            float seamX = 0f, innerX = 0f, seamY = 0f, innerY = 0f;
            for (int y = 0; y < n; y++)
            {
                seamX = Mathf.Max(seamX, Mathf.Abs(h[y * n] - h[y * n + n - 1]));
                for (int x = 0; x < n - 1; x++) innerX = Mathf.Max(innerX, Mathf.Abs(h[y * n + x] - h[y * n + x + 1]));
            }
            for (int x = 0; x < n; x++)
            {
                seamY = Mathf.Max(seamY, Mathf.Abs(h[x] - h[(n - 1) * n + x]));
                for (int y = 0; y < n - 1; y++) innerY = Mathf.Max(innerY, Mathf.Abs(h[y * n + x] - h[(y + 1) * n + x]));
            }
            Assert.That(seamX, Is.LessThanOrEqualTo(innerX + 0.02f), "tiles across");
            Assert.That(seamY, Is.LessThanOrEqualTo(innerY + 0.02f), "tiles down");
            // And the control: a field that does not tile fails the same check.
            float[] ramp = new float[n * n];
            for (int i = 0; i < ramp.Length; i++) ramp[i] = (i % n) / (float)n;
            float rampSeam = Mathf.Abs(ramp[0] - ramp[n - 1]), rampInner = 1f / n;
            Assert.That(rampSeam, Is.GreaterThan(rampInner + 0.02f), "the check can tell a seam");
            Color32[] normal = CreatureHideH8S.Normal(family, n);
            Assert.That(normal.Length, Is.EqualTo(n * n));
            double mx = 0, my = 0;
            foreach (Color32 c in normal) { mx += c.r / 255.0 - 0.5; my += c.g / 255.0 - 0.5; Assert.That(c.b, Is.GreaterThan(127)); Assert.That(c.a, Is.EqualTo(255)); }
            Assert.That(mx / normal.Length, Is.EqualTo(0).Within(0.02), "the relief is level on average");
            Assert.That(my / normal.Length, Is.EqualTo(0).Within(0.02));
        }

        // ------------------------------------------------------------------ species mapping

        [Test]
        public void EverySpeciesTakesTheLookAndCompanionsDoNot()
        {
            foreach (string id in CreatureLooks.Species)
            {
                CreatureLook k = ObsidianLookH8S.For(id, 61724);
                Assert.That(k.ObsidianH8S, Is.True, id);
                Assert.That((int)k.FamilyH8S, Is.InRange(0, 4), id);
                Assert.That(k.SurfaceH8S, Is.InRange(0, CreatureHideH8S.FamilyCount - 1), id);
                Assert.That(k.ClusterH8S, Is.InRange(0.2f, 1f), id);
                Assert.That(k.Spines, Is.EqualTo(0), id + " grows clusters, not a comb");
                Assert.That(k.Glow, Is.EqualTo(ObsidianPaletteH8S.GlowCell(k.FamilyH8S)), id + " eyes in the family colour");
                Assert.That(k.Crystal.y, Is.EqualTo(ObsidianPaletteH8S.CrystalRow), id + " crystal from the regional row");
                Assert.That(k.Hide.y, Is.GreaterThanOrEqualTo(13), id + " body from the new rows");
                // The classic proportions are kept: only colours and the pass flags change.
                CreatureLook plain = CreatureLooks.For(id, 61724);
                Assert.That(k.Plan, Is.EqualTo(plain.Plan));
                Assert.That(k.BodyLength, Is.EqualTo(plain.BodyLength));
                Assert.That(k.ShoulderHeight, Is.EqualTo(plain.ShoulderHeight));
                Assert.That(k.HeadStyle, Is.EqualTo(plain.HeadStyle));
                Assert.That(k.TailStyle, Is.EqualTo(plain.TailStyle));
            }
            Assert.That(ObsidianLookH8S.Family("neon_jackal"), Is.EqualTo(CrystalFamilyH8S.Azure));
            Assert.That(ObsidianLookH8S.Family("glass_mite"), Is.EqualTo(CrystalFamilyH8S.Amethyst));
            Assert.That(ObsidianLookH8S.Family("hollow_shade"), Is.EqualTo(CrystalFamilyH8S.Veil));
            Assert.That(ObsidianLookH8S.Family("dune_wyrm"), Is.EqualTo(CrystalFamilyH8S.Ember));
            Assert.That(ObsidianLookH8S.Family("frost_stalker"), Is.EqualTo(CrystalFamilyH8S.Rime));
            foreach (string archetype in BondIds.Archetypes)
                Assert.That(CompanionLooks.For(archetype, 4242, 1).ObsidianH8S, Is.False, archetype + " keeps its own look");
            ObsidianLookH8S.Classic = true;
            try { Assert.That(ObsidianLookH8S.For("neon_jackal", 1).ObsidianH8S, Is.False, "the classic switch"); }
            finally { ObsidianLookH8S.ResetStatics(); }
            Assert.That(ObsidianLookH8S.Classic, Is.False);
        }

        // ------------------------------------------------------------------ the builder pass

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ThePassKeepsRigHitsAndMeasuresAndStaysInBudget(int detail)
        {
            foreach (string id in CreatureLooks.Species)
            {
                CreatureLook plain = CreatureLooks.For(id, 61724);
                CreatureLook look = ObsidianLookH8S.For(id, 61724);
                // Compare against the plain body with the same comb and plate settings, so only the pass is measured.
                CreatureLook reference = look.Clone();
                reference.ObsidianH8S = false;
                CreatureBody before = CreatureBuilder.Build(reference, detail);
                CreatureBody after = CreatureBuilder.Build(look, detail);
                CollectionAssert.AreEqual(before.Skeleton.Names, after.Skeleton.Names, id);
                CollectionAssert.AreEqual(before.Skeleton.Parents, after.Skeleton.Parents, id);
                CollectionAssert.AreEqual(before.Skeleton.LocalPositions, after.Skeleton.LocalPositions, id);
                CollectionAssert.AreEqual(before.HitVolumes, after.HitVolumes, id);
                Assert.That(after.Radius, Is.EqualTo(before.Radius), id + " radius");
                Assert.That(after.Height, Is.EqualTo(before.Height), id + " height");
                Assert.That(after.AimPoint, Is.EqualTo(before.AimPoint), id);
                Assert.That(after.HeadPoint, Is.EqualTo(before.HeadPoint), id);
                Assert.That(after.MouthPoint, Is.EqualTo(before.MouthPoint), id);
                // The plain species body with its old comb is the same rig too.
                CreatureBody classic = CreatureBuilder.Build(plain, detail);
                CollectionAssert.AreEqual(classic.Skeleton.LocalPositions, after.Skeleton.LocalPositions, id + " classic rig");
                CollectionAssert.AreEqual(classic.HitVolumes, after.HitVolumes, id + " classic hits");
                int added = after.Mesh.Vertices.Count - before.Mesh.Vertices.Count;
                Assert.That(added, Is.GreaterThan(id == "glass_mite" ? 40 : 150), id + " grows real geometry");
                int budget = detail == 0 ? 1400 : (detail == 1 ? 3200 : 5600);
                if (id == "quartz_drake" || id == "dune_wyrm") budget += 1200;
                Assert.That(added, Is.LessThan(budget), id + " detail " + detail + " added " + added);
                AssertValidSkin(after, id);
                Bounds pb = after.PoseBounds.size.sqrMagnitude > 1e-6f ? after.PoseBounds : after.Bounds;
                for (int i = 0; i < after.Mesh.Vertices.Count; i++) Assert.That(Inside(pb, after.Mesh.Vertices[i]), Is.True, id + " renderer bounds hold vertex " + i);
            }
        }

        [Test]
        public void ThePassIsDeterministic()
        {
            foreach (string id in new[] { "neon_jackal", "dune_wyrm", "hollow_shade", "glass_mite", "rime_griffin" })
            {
                CreatureBody a = CreatureBuilder.Build(ObsidianLookH8S.For(id, 99), 1);
                CreatureBody b = CreatureBuilder.Build(ObsidianLookH8S.For(id, 99), 1);
                CollectionAssert.AreEqual(a.Mesh.Vertices, b.Mesh.Vertices, id);
                CollectionAssert.AreEqual(a.Mesh.Triangles, b.Mesh.Triangles, id);
            }
        }

        [Test]
        public void NoFleshGlowsOnlyCrystalEyesAndSeams()
        {
            // Every vertex on an emitting cell sits on a crystal row, the glow row (eyes, cores, dim seams) or the
            // legacy glow row used by the weak point gem; no hide or body cell emits.
            foreach (string id in CreatureLooks.Species)
            {
                CreatureBody body = CreatureBuilder.Build(ObsidianLookH8S.For(id, 7), 1);
                int lit = 0;
                foreach (Vector2 uv in body.Mesh.Palette)
                {
                    int row = Mathf.FloorToInt(uv.y * 16f), col = Mathf.FloorToInt(uv.x * 16f);
                    Color e = CharacterPaletteData.Emission(row, col);
                    if (e.maxColorComponent <= 0f) continue;
                    lit++;
                    Assert.That(row == 9 || row == 10 || row == 14 || row == 15, Is.True, id + " emits from a hide cell at row " + row);
                }
                Assert.That(lit, Is.GreaterThan(0), id + " has eyes or crystal that light");
            }
        }

        // ------------------------------------------------------------------ boss regalia and part shards

        [Test]
        public void BossRegaliaClustersBuildOnPaletteCellsForTheFourBosses()
        {
            foreach (string id in new[] { "golem_foreman", "wraith_queen", "mirage_djinn", "glacier_titan" }) Assert.That(BossRegaliaH8S.Wears(id), Is.True, id);
            Assert.That(BossRegaliaH8S.Wears("veilglass_castellan"), Is.False, "the regional bosses keep their own regalia");
            Assert.That(ObsidianLookH8S.BossFamily("mirage_djinn"), Is.EqualTo(CrystalFamilyH8S.Ember));
            Assert.That(ObsidianLookH8S.BossFamily("glacier_titan"), Is.EqualTo(CrystalFamilyH8S.Rime));
            Assert.That(ObsidianLookH8S.BossFamily("wraith_queen"), Is.EqualTo(CrystalFamilyH8S.Veil));
            MeshBuilder mb = new MeshBuilder(1);
            BossRegaliaH8S.BuildCluster(mb, new Vector3(0f, 0.15f, 0f), Vector3.up, Vector3.back, 0.5f, 0.2f, 4, 77u, CrystalFamilyH8S.Ember, true);
            // MeshBuilder writes three vertices per triangle (flat faces): the socket's 14 and each prism's 18 triangles.
            int socketVerts = 7 * 2 * 3, prismVerts = CrystalClusterH8S.PrismTriangles * 3;
            Assert.That(mb.VertexCount, Is.EqualTo(socketVerts + 4 * prismVerts));
            Assert.That(mb.Normals.Count, Is.EqualTo(mb.VertexCount));
            Assert.That(mb.UVs.Count, Is.EqualTo(mb.VertexCount));
            int crystal = 0, basalt = 0;
            for (int i = 0; i < mb.UVs.Count; i++)
            {
                int row = Mathf.FloorToInt(mb.UVs[i].y * 16f), col = Mathf.FloorToInt(mb.UVs[i].x * 16f);
                if (row == ObsidianPaletteH8S.CrystalRow) { crystal++; Assert.That(col, Is.InRange(9, 11), "ember shades"); }
                else { basalt++; Assert.That(row, Is.EqualTo(ObsidianPaletteH8S.BodyRow)); Assert.That(col, Is.EqualTo(ObsidianPaletteH8S.Basalt)); }
                Assert.That(mb.Vertices[i].y, Is.GreaterThan(-0.2f), "nothing grows far below the bone");
            }
            Assert.That(crystal, Is.EqualTo(4 * prismVerts));
            Assert.That(basalt, Is.EqualTo(socketVerts));
            MeshBuilder twin = new MeshBuilder(1);
            BossRegaliaH8S.BuildCluster(twin, new Vector3(0f, 0.15f, 0f), Vector3.up, Vector3.back, 0.5f, 0.2f, 4, 77u, CrystalFamilyH8S.Ember, true);
            CollectionAssert.AreEqual(mb.Vertices, twin.Vertices, "deterministic");
        }

        // ------------------------------------------------------------------ helpers

        private static void AssertValidSkin(CreatureBody body, string id)
        {
            BodyData d = body.Mesh;
            Assert.That(d.Normals.Count, Is.EqualTo(d.Vertices.Count), id);
            Assert.That(d.Tangents.Count, Is.EqualTo(d.Vertices.Count), id);
            Assert.That(d.Weights.Count, Is.EqualTo(d.Vertices.Count), id);
            Assert.That(d.Palette.Count, Is.EqualTo(d.Vertices.Count), id);
            int bones = body.Skeleton.Count;
            for (int i = 0; i < d.Vertices.Count; i++)
            {
                Vector3 v = d.Vertices[i];
                Assert.That(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z), Is.False, id + " vertex " + i);
                BoneWeight w = d.Weights[i];
                Assert.That(w.weight0 + w.weight1 + w.weight2 + w.weight3, Is.EqualTo(1f).Within(1e-4f), id + " weights " + i);
                Assert.That(w.boneIndex0, Is.InRange(0, bones - 1), id);
                Assert.That(w.boneIndex1, Is.InRange(0, bones - 1), id);
                Assert.That(w.boneIndex2, Is.InRange(0, bones - 1), id);
                Assert.That(w.boneIndex3, Is.InRange(0, bones - 1), id);
            }
            foreach (int t in d.Triangles) Assert.That(t, Is.InRange(0, d.Vertices.Count - 1), id);
        }

        private static bool Inside(Bounds b, Vector3 p)
        {
            const float eps = 1e-3f;
            return p.x >= b.min.x - eps && p.x <= b.max.x + eps && p.y >= b.min.y - eps && p.y <= b.max.y + eps && p.z >= b.min.z - eps && p.z <= b.max.z + eps;
        }

        private static void AssertNear(Color a, Color b, string what)
        {
            Assert.That(a.r, Is.EqualTo(b.r).Within(0.01f), what);
            Assert.That(a.g, Is.EqualTo(b.g).Within(0.01f), what);
            Assert.That(a.b, Is.EqualTo(b.b).Within(0.01f), what);
        }
    }
}
