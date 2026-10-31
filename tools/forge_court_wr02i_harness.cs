// Executes the actual source mesh recipe with the shipped collision meshes. No native Unity calls or playtest.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NeonSyndicate;
using UnityEngine;

public static class ForgeCourtHarnessWR02I
{
    private static int checks;
    private static void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    private static string Point(Vector3 p) { return p.x.ToString("R", CultureInfo.InvariantCulture) + " " + p.y.ToString("R", CultureInfo.InvariantCulture) + " " + p.z.ToString("R", CultureInfo.InvariantCulture); }
    public static int Main(string[] args)
    {
        try
        {
            string root = args[0], output = args[1]; Directory.CreateDirectory(output);
            VenueBinding04C binding = new VenueBinding04C { Definition = new VenueDefinition04C { Id = "forge" },
                Building = new Building { Seed = 584924, Height = 24f }, Width = 30f, Depth = 30f };
            MeshBuilder structure = new MeshBuilder(1); GothicHeartH3.Structure(structure, binding);
            List<ArtPlacementH2> parts = GothicHeartH3.ServicePlan(binding);
            Check(parts.Count == 36, "Original Forge piece count changed.");
            Dictionary<string, MeshBuilder> meshes = new Dictionary<string, MeshBuilder>();
            foreach (ArtPlacementH2 part in parts)
                if (!meshes.ContainsKey(part.Model)) meshes.Add(part.Model, AscensionArtH2.Decode(File.ReadAllBytes(
                    Path.Combine(root, "Assets/NeonSyndicate/Resources/AscensionH2/Meshes/" + part.Model + "_low.bytes"))));
            ForgeCourtArtWR02I art = HeartPlanFR01.ForgeCourtBuildWR02I(structure, parts, name => meshes[name]);
            Check(HeartPlanFR01.ForgeCourtFrameWR02I("forge", 30, 30, false), "Expected frame refused.");
            foreach (string id in new[] { "lofts", "archive", "", null }) Check(!HeartPlanFR01.ForgeCourtFrameWR02I(id, 30, 30, false), "Other venue accepted.");
            Check(!HeartPlanFR01.ForgeCourtFrameWR02I("forge", float.NaN, 30, false), "NaN accepted.");
            Check(!HeartPlanFR01.ForgeCourtFrameWR02I("forge", 30, 30, true), "Receipt roof accepted.");
            Check(!HeartPlanFR01.ForgeCourtFrameWR02I("forge", 31, 30, false), "Wrong dimensions accepted.");
            Check(art.Near.Keys.Count == 5 && art.Near.ContainsKey("iron") && art.Near.ContainsKey("slate"), "Required architectural groups missing.");
            Check(art.MaximumRelief <= .022001f, "Unsupported relief depth.");
            int originalTriangles = structure.Triangles(0).Count / 3;
            foreach (ArtPlacementH2 part in parts) originalTriangles += meshes[part.Model].Triangles(0).Count / 3;
            Check(art.SourceTriangles == originalTriangles, "Physical source triangle lost.");
            Check(art.Far.Values.Sum(m => m.Triangles(0).Count / 3) == originalTriangles, "Far support lost.");
            // Independent backing triangle comparison, including indices, using the original H3 transform path.
            MeshBuilder expected = new MeshBuilder(1); expected.Append(structure);
            foreach (ArtPlacementH2 part in parts) AscensionArtH2.Append(expected, meshes[part.Model], part.Position, part.Yaw, part.Scale);
            HashSet<string> expectedFaces = new HashSet<string>();
            IReadOnlyList<int> ei = expected.Triangles(0);
            for (int i = 0; i < ei.Count; i += 3) expectedFaces.Add(Face(expected, ei, i));
            foreach (MeshBuilder mesh in art.Far.Values)
            {
                IReadOnlyList<int> indices = mesh.Triangles(0);
                for (int i = 0; i < indices.Count; i += 3) Check(expectedFaces.Contains(Face(mesh, indices, i)), "A far face does not match the actual H3 physical source.");
            }
            foreach (KeyValuePair<string, MeshBuilder> entry in art.Near)
            {
                MeshBuilder mesh = entry.Value; IReadOnlyList<int> indices = mesh.Triangles(0);
                for (int i = 0; i < indices.Count; i += 3)
                {
                    Vector3 a = mesh.Vertices[indices[i]], b = mesh.Vertices[indices[i + 1]], c = mesh.Vertices[indices[i + 2]];
                    Check(MathUtil.IsFinite(a) && MathUtil.IsFinite(b) && MathUtil.IsFinite(c), "Non-finite artwork.");
                    Check(Vector3.Cross(b - a, c - a).sqrMagnitude > 1e-12f, "Degenerate artwork.");
                }
            }
            Export(art.Near, Path.Combine(output, "forge-near.obj")); Export(art.Far, Path.Combine(output, "forge-far.obj"));
            Export(new Dictionary<string, MeshBuilder> { { "support", expected } }, Path.Combine(output, "forge-support.obj"));
            bool rejected = false; try { HeartPlanFR01.ForgeCourtBuildWR02I(structure, new List<ArtPlacementH2>(), name => meshes[name]); } catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Partial original plan must refuse.");
            Console.WriteLine("PASS " + checks + " managed geometry assertions; source triangles " + art.SourceTriangles + ", relief panels " + art.ReliefPanels + ", relief vertices " + art.ReliefVertices + ", near triangles " + art.Near.Values.Sum(m => m.Triangles(0).Count / 3) + ". Native Unity NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static string Face(MeshBuilder mesh, IReadOnlyList<int> indices, int i)
    { return Point(mesh.Vertices[indices[i]]) + "/" + Point(mesh.Vertices[indices[i + 1]]) + "/" + Point(mesh.Vertices[indices[i + 2]]); }
    private static void Export(Dictionary<string, MeshBuilder> groups, string path)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            int offset = 1;
            foreach (KeyValuePair<string, MeshBuilder> entry in groups)
            {
                writer.WriteLine("g " + entry.Key); writer.WriteLine("usemtl " + entry.Key);
                foreach (Vector3 p in entry.Value.Vertices) writer.WriteLine("v " + Point(p));
                IReadOnlyList<int> triangles = entry.Value.Triangles(0);
                for (int i = 0; i < triangles.Count; i += 3) writer.WriteLine("f " + (offset + triangles[i]) + " " + (offset + triangles[i + 1]) + " " + (offset + triangles[i + 2]));
                offset += entry.Value.VertexCount;
            }
        }
    }
}
