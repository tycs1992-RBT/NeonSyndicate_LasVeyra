using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace NeonSyndicate.Tests
{
    public sealed class SupportedDistrictWR02JTests
    {
        public static Building Dwelling(int seed)
        {
            return new Building { Seed = seed, Tiers = 1, Roof = RoofType.Flat,
                Footprint = seed == 961727 ? new RectF(94, -26, 116, 0) : new RectF(102, 145, 124, 171),
                Height = seed == 961727 ? 12 : 13, GothicFamily04OB = seed == 961727 ? 4 : 5 };
        }

        [TestCase(961727, true)] [TestCase(961728, true)] [TestCase(809556, false)]
        [TestCase(32874, false)] [TestCase(53496, false)] [TestCase(584924, false)]
        public void OnlyTheTwoKnownSmallSourceFramesQualify(int seed, bool expected)
        { Assert.AreEqual(expected, HeartPlanFR01.SupportedLotFrameWR02J(Dwelling(seed))); }

        [Test] public void ChangedFramesAndReceiptRoofsAreRefused()
        {
            Building b = Dwelling(961727); b.RoofPad04OB = true; Assert.False(HeartPlanFR01.SupportedLotFrameWR02J(b));
            b = Dwelling(961727); b.Height = float.NaN; Assert.False(HeartPlanFR01.SupportedLotFrameWR02J(b));
            b = Dwelling(961727); b.Footprint = new RectF(94, -26, 117, 0); Assert.False(HeartPlanFR01.SupportedLotFrameWR02J(b));
            b = Dwelling(961728); b.GothicFamily04OB = 4; Assert.False(HeartPlanFR01.SupportedLotFrameWR02J(b));
        }

        [TestCase(961727, true)] [TestCase(961727, false)]
        [TestCase(961728, true)] [TestCase(961728, false)]
        public void EveryActualStructuralTriangleSurvivesExactlyOnce(int seed, bool detailed)
        {
            Building b = Dwelling(seed);
            MeshBuilder[] sources = { new MeshBuilder(1), new MeshBuilder(1), new MeshBuilder(1) };
            GothicRealmGeometry04OB.Family(sources[0], sources[1], sources[2], b, detailed);
            for (int part = 0; part < 3; part++)
            {
                Dictionary<string, int> before = Triangles(sources[part]);
                Dictionary<string, MeshBuilder> art = HeartPlanFR01.SupportedLotArtWR02J(b, sources[part], part);
                MeshBuilder combined = new MeshBuilder(1); foreach (MeshBuilder mesh in art.Values) combined.Append(mesh);
                CollectionAssert.AreEquivalent(before, Triangles(combined), "Visible geometry must match the existing collision, including open halls and stairs.");
                CollectionAssert.AreEquivalent(before, Triangles(sources[part]), "Do not mutate the caller's collision source.");
                if (part == 0) { Assert.True(art.ContainsKey("SlateTimber")); Assert.True(art.ContainsKey("Masonry")); }
            }
        }

        [Test] public void ArchiveUsesAllOriginalSupportedRoofAndSpireTriangles()
        {
            VenueBinding04C binding = new VenueBinding04C { Definition = new VenueDefinition04C { Id = "archive" },
                Building = new Building { Seed = 479130, Height = 24 }, Width = 30, Depth = 48 };
            List<ArtPlacementH2> parts = GothicCivicH5.Plan(binding).FindAll(p => HeartPlanFR01.CrownPartWR01(p.Model));
            Dictionary<string, MeshBuilder> sources = new Dictionary<string, MeshBuilder>();
            MeshBuilder expected = new MeshBuilder(1);
            foreach (ArtPlacementH2 part in parts)
            {
                if (!sources.ContainsKey(part.Model)) sources.Add(part.Model, AscensionArtH2.Decode(File.ReadAllBytes(
                    "Assets/NeonSyndicate/Resources/AscensionH2/Meshes/" + part.Model + "_low.bytes")));
                AscensionArtH2.Append(expected, sources[part.Model], part.Position, part.Yaw, part.Scale);
            }
            Dictionary<string, MeshBuilder> art = HeartPlanFR01.SupportedArchiveArtWR02J(parts, model => sources[model]);
            MeshBuilder actual = new MeshBuilder(1); foreach (MeshBuilder mesh in art.Values) actual.Append(mesh);
            CollectionAssert.AreEquivalent(Triangles(expected), Triangles(actual));
            CollectionAssert.AreEquivalent(new[] { "SlateTimber", "AmethystBronze" }, art.Keys);
            parts.RemoveAt(0);
            Assert.Throws<InvalidOperationException>(() => HeartPlanFR01.SupportedArchiveArtWR02J(parts, model => sources[model]));
        }

        private static string Point(Vector3 p)
        { return p.x.ToString("R", CultureInfo.InvariantCulture) + "," + p.y.ToString("R", CultureInfo.InvariantCulture) + "," + p.z.ToString("R", CultureInfo.InvariantCulture); }
        private static Dictionary<string, int> Triangles(MeshBuilder mesh)
        {
            Dictionary<string, int> result = new Dictionary<string, int>(); IReadOnlyList<int> indices = mesh.Triangles(0);
            for (int i = 0; i < indices.Count; i += 3)
            {
                string key = Point(mesh.Vertices[indices[i]]) + "/" + Point(mesh.Vertices[indices[i + 1]]) + "/" + Point(mesh.Vertices[indices[i + 2]]);
                int count; result.TryGetValue(key, out count); result[key] = count + 1;
            }
            return result;
        }
    }
}
